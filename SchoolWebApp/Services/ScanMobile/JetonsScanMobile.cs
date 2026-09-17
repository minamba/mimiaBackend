using System.Collections.Concurrent;
using SchoolWebApp.Api.Auth;

namespace SchoolWebApp.Api.Services.ScanMobile
{
    /// <summary>Ce que la page du téléphone a le droit de savoir : à qui il envoie, rien de plus.</summary>
    /// <summary>
    /// Ce que le téléphone apprend du QR code avant d'afficher quoi que ce soit.
    ///
    /// <c>BlueSky</c> n'est pas du ressort du jeton — il ne le connaît pas et
    /// le laisse à sa valeur par défaut. C'est le contrôleur qui le pose, en
    /// lisant le réglage : le téléphone n'a ni compte ni mémoire, et cet
    /// aller-retour est le SEUL qu'il fait avant de se dessiner.
    /// </summary>
    public record InfoScanMobile(
        string? ProfPrenom, string? Matiere, DateTime ExpireLe, bool DejaEnvoye,
        bool BlueSky = false);

    /// <summary>Où en est un QR code, vu de l'ordinateur.</summary>
    public enum EtatJetonScan { Attente, Recu, Expire }

    /// <summary>
    /// LES QR CODES DU SCANNER, LE TEMPS QU'ILS SERVENT.
    ///
    /// Voulu par Camara le 13/09/2026 : sur l'ordinateur, un bouton affiche un
    /// QR code ; l'enfant le vise avec son téléphone, photographie sa copie,
    /// et la photo arrive au professeur. Le téléphone n'est connecté à rien —
    /// c'est tout l'intérêt : pas de code à retaper sur un petit écran.
    ///
    /// LE JETON EST LA SEULE AUTORISATION DU TÉLÉPHONE. Il est donc :
    ///   - IMPOSSIBLE À DEVINER : trente-deux octets tirés au sort, le même
    ///     générateur que les sessions d'élève ;
    ///   - COURT : dix minutes, le temps d'aller chercher la feuille ;
    ///   - À USAGE UNIQUE : une photo, et le QR code ne sert plus ;
    ///   - ÉTROIT : il ne permet QUE de déposer UN document dans UNE
    ///     conversation. Il ne lit rien — ni les messages, ni les documents,
    ///     ni le nom de l'enfant. La page du téléphone n'apprend que le prénom
    ///     du professeur et la matière.
    ///   - GARDÉ EN EMPREINTE : on ne conserve que son hachage.
    ///
    /// EN MÉMOIRE, ET C'EST ASSUMÉ. Un jeton vit dix minutes : une table, une
    /// migration et une purge pour ça seraient disproportionnés. Un
    /// redémarrage du serveur invalide les QR codes en cours — l'enfant en
    /// redemande un d'un clic. CELA SUPPOSE UNE SEULE INSTANCE DU SERVEUR,
    /// ce qui est le cas aujourd'hui (un conteneur sur le VPS) ; le jour où
    /// il y en aura plusieurs, ce registre devra passer en base.
    /// </summary>
    public sealed class JetonsScanMobile
    {
        public static readonly TimeSpan Validite = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Combien de temps un jeton livré ou expiré reste lisible par
        /// l'ordinateur : assez pour qu'un relevé en retard voie « reçu »
        /// plutôt que « expiré ».
        /// </summary>
        private static readonly TimeSpan Conservation = TimeSpan.FromMinutes(5);

        private sealed class Entree
        {
            public int ConversationId { get; init; }
            public string? ProfPrenom { get; init; }
            public string? Matiere { get; init; }
            public DateTime ExpireLe { get; init; }
            public bool EnCours { get; set; }

            // PLUSIEURS PHOTOS PAR QR CODE — Camara, le 16/09/2026. Le jeton
            // n'est plus « consommé » à la première : il reste ouvert tant que
            // le téléphone n'a pas dit « terminé », dans la limite du plafond.
            public List<int> PiecesJointes { get; } = new();
            public bool Termine { get; set; }
        }

        private readonly ConcurrentDictionary<string, Entree> _entrees = new();
        private readonly object _verrou = new();

        /// <summary>
        /// Un nouveau QR code pour cette conversation. Les anciens NON UTILISÉS
        /// de la même conversation sont retirés : un seul QR code valable à la
        /// fois, celui qui est à l'écran.
        /// </summary>
        public (string Jeton, DateTime ExpireLe) Creer(
            int conversationId, string? profPrenom, string? matiere, DateTime maintenant)
        {
            Nettoyer(maintenant);

            lock (_verrou)
            {
                foreach (var (cle, entree) in _entrees)
                {
                    if (entree.ConversationId == conversationId && entree.PiecesJointes.Count == 0 && !entree.EnCours)
                    {
                        _entrees.TryRemove(cle, out _);
                    }
                }

                var jeton = AuthentificationEleve.GenererJeton();
                var expireLe = maintenant + Validite;

                _entrees[AuthentificationEleve.Hacher(jeton)] = new Entree
                {
                    ConversationId = conversationId,
                    ProfPrenom = profPrenom,
                    Matiere = matiere,
                    ExpireLe = expireLe,
                };

                return (jeton, expireLe);
            }
        }

        /// <summary>Pour la page du téléphone. Null si le QR code est inconnu ou expiré.</summary>
        public InfoScanMobile? Lire(string? jeton, DateTime maintenant)
        {
            if (!TrouverValide(jeton, maintenant, out var entree)) return null;

            return new InfoScanMobile(
                entree!.ProfPrenom, entree.Matiere, entree.ExpireLe, entree.Termine);
        }

        /// <summary>
        /// Réserve le jeton pour UN envoi. Faux s'il est inconnu, expiré, déjà
        /// utilisé, ou si un envoi est déjà en cours — deux photos envoyées au
        /// même instant depuis deux onglets ne passent pas toutes les deux.
        /// </summary>
        public bool Reserver(string? jeton, DateTime maintenant, out int conversationId)
        {
            conversationId = 0;

            lock (_verrou)
            {
                if (!TrouverValide(jeton, maintenant, out var entree)) return false;
                // Un envoi à la fois — le téléphone les enchaîne —, plus rien
                // après « terminé », et jamais au-delà du plafond par message.
                if (entree!.Termine || entree.EnCours || entree.PiecesJointes.Count >= PiecesMax) return false;

                entree.EnCours = true;
                conversationId = entree.ConversationId;
                return true;
            }
        }

        /// <summary>L'envoi a réussi : le jeton est consommé, la pièce attend l'ordinateur.</summary>
        public void Livrer(string jeton, int pieceJointeId)
        {
            lock (_verrou)
            {
                if (!_entrees.TryGetValue(AuthentificationEleve.Hacher(jeton), out var entree)) return;

                entree.PiecesJointes.Add(pieceJointeId);
                entree.EnCours = false;
            }
        }

        /// <summary>L'envoi a échoué (fichier refusé, réseau) : le jeton redevient utilisable.</summary>
        public void Liberer(string jeton)
        {
            lock (_verrou)
            {
                if (_entrees.TryGetValue(AuthentificationEleve.Hacher(jeton), out var entree))
                {
                    entree.EnCours = false;
                }
            }
        }

        /// <summary>
        /// Pour l'ordinateur. Un jeton d'une AUTRE conversation se lit
        /// « expiré » : on ne dit jamais à quelqu'un qu'un jeton existe
        /// ailleurs.
        /// </summary>
        /// <summary>Le plafond de photos par QR code : celui d'un message.</summary>
        public const int PiecesMax = Request.EnvoyerMessageRequest.PiecesMax;

        /// <summary>
        /// Le téléphone a appuyé sur « Envoyer » : plus aucune photo n'entre,
        /// et c'est ce signal — pas la première photo — que l'ordinateur
        /// attend pour expédier le tout au professeur.
        /// </summary>
        public bool Terminer(string? jeton, DateTime maintenant)
        {
            lock (_verrou)
            {
                if (!TrouverValide(jeton, maintenant, out var entree)) return false;

                entree!.Termine = true;
                return true;
            }
        }

        public (EtatJetonScan Etat, IReadOnlyList<int> PiecesJointes, bool Termine) Etat(
            string? jeton, int conversationId, DateTime maintenant)
        {
            if (string.IsNullOrWhiteSpace(jeton)
                || !_entrees.TryGetValue(AuthentificationEleve.Hacher(jeton), out var entree)
                || entree.ConversationId != conversationId)
            {
                return (EtatJetonScan.Expire, Array.Empty<int>(), false);
            }

            int[] recues;
            bool termine;

            lock (_verrou)
            {
                recues = entree.PiecesJointes.ToArray();
                termine = entree.Termine;
            }

            // Reçu dès la première : l'ordinateur les montre au fur et à mesure.
            // Une photo arrivée n'expire pas — elle est déjà en base.
            if (recues.Length > 0) return (EtatJetonScan.Recu, recues, termine);

            return entree.ExpireLe > maintenant
                ? (EtatJetonScan.Attente, recues, termine)
                : (EtatJetonScan.Expire, recues, termine);
        }

        private bool TrouverValide(string? jeton, DateTime maintenant, out Entree? entree)
        {
            entree = null;
            if (string.IsNullOrWhiteSpace(jeton) || jeton.Length > 100) return false;

            return _entrees.TryGetValue(AuthentificationEleve.Hacher(jeton), out entree)
                && entree.ExpireLe > maintenant;
        }

        private void Nettoyer(DateTime maintenant)
        {
            foreach (var (cle, entree) in _entrees)
            {
                if (entree.ExpireLe + Conservation < maintenant) _entrees.TryRemove(cle, out _);
            }
        }
    }
}
