using System.Collections.Concurrent;
using SchoolWebApp.Api.Auth;

namespace SchoolWebApp.Api.Services.ScanMobile
{
    /// <summary>Ce que la page du téléphone a le droit de savoir : à qui il envoie, rien de plus.</summary>
    public record InfoScanMobile(string? ProfPrenom, string? Matiere, DateTime ExpireLe, bool DejaEnvoye);

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
            public int? PieceJointeId { get; set; }
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
                    if (entree.ConversationId == conversationId && entree.PieceJointeId is null && !entree.EnCours)
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
                entree!.ProfPrenom, entree.Matiere, entree.ExpireLe, entree.PieceJointeId is not null);
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
                if (entree!.PieceJointeId is not null || entree.EnCours) return false;

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

                entree.PieceJointeId = pieceJointeId;
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
        public (EtatJetonScan Etat, int? PieceJointeId) Etat(string? jeton, int conversationId, DateTime maintenant)
        {
            if (string.IsNullOrWhiteSpace(jeton)
                || !_entrees.TryGetValue(AuthentificationEleve.Hacher(jeton), out var entree)
                || entree.ConversationId != conversationId)
            {
                return (EtatJetonScan.Expire, null);
            }

            if (entree.PieceJointeId is int piece) return (EtatJetonScan.Recu, piece);

            return entree.ExpireLe > maintenant
                ? (EtatJetonScan.Attente, null)
                : (EtatJetonScan.Expire, null);
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
