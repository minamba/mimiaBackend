using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public partial class DicteeRepository : IDicteeRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public DicteeRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DicteeEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string? titre,
            string? etat,
            string texteDicte,
            string copie,
            string? remarque,
            CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte du modèle —
            // même garde que pour les fiches et les évaluations.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            var nouvelEtat = EtatDictee.Normaliser(etat);
            var cle = CleDictee(texteDicte);

            // RATTACHER À LA DICTÉE EN ATTENTE, PAS EN OUVRIR UNE SECONDE.
            //
            // Le modèle ne connaît pas l'identifiant de la ligne qu'il a
            // archivée la fois précédente — il ne connaît que le texte qu'il
            // a dicté, redonné à l'identique. On rapproche donc sur CE texte,
            // normalisé, et seulement parmi les dictées encore en attente de
            // cet élève et cette matière : une dictée déjà corrigée ne doit
            // plus jamais être réécrite par une nouvelle, même si le texte se
            // ressemble — deux dictées distinctes peuvent porter les mêmes
            // mots.
            var candidates = await _context.Dictees
                .Where(d =>
                    d.EleveId == eleveId
                    && d.MatiereId == conversation.MatiereId
                    && d.Etat == EtatDictee.EnAttente)
                .Select(d => new { d.Id, d.TexteDicte })
                .ToListAsync(ct);

            // UNE SEULE DICTÉE EN ATTENTE : C'EST FORCÉMENT ELLE, MÊME SI LE
            // TEXTE NE COLLE PAS AU MOT PRÈS.
            //
            // Le rapprochement par texte suppose que le modèle redonne le
            // texte dicté À L'IDENTIQUE au moment de la correction — mais
            // rien ne le lui impose : il le régénère de mémoire plutôt que
            // de le recopier depuis ce qu'il a lui-même dicté (le même
            // défaut, exactement, que celui rencontré sur le tableau — voir
            // la consigne sur les phrases recopiées, pas régénérées).
            //
            // Un seul mot qui change — « planté » pour « plantés » — et
            // `CleDictee` ne matchait plus rien : `existante` restait nul,
            // et ce qui suit ouvrait une SECONDE ligne au lieu de corriger
            // la première. L'élève continuait de voir sa dictée « en
            // attente de correction », pendant que la vraie correction
            // dormait dans une ligne à côté que personne n'affichait.
            //
            // Tant qu'il n'y a QU'UNE dictée en attente pour cet élève et
            // cette matière, il n'y a aucune ambiguïté à lever : c'est
            // forcément celle-là. Le rapprochement par texte ne reprend la
            // main que s'il y en a PLUSIEURS — là, et seulement là, il faut
            // vraiment distinguer laquelle des deux vient d'être corrigée.
            var existante = candidates.Count == 1
                ? candidates[0]
                : candidates.FirstOrDefault(c => CleDictee(c.TexteDicte) == cle);

            var dictee = existante is null
                ? null
                : await _context.Dictees.FirstOrDefaultAsync(d => d.Id == existante.Id, ct);

            var maintenant = DateTime.UtcNow;

            if (dictee is null)
            {
                // La classe est lue MAINTENANT, pas à la relecture — même
                // raison que sur Evaluation.NiveauScolaireId : le changement
                // de classe ne laisse aucune trace datée, ce qui n'est pas
                // capturé ici est perdu.
                var niveauId = await _context.Eleves
                    .AsNoTracking()
                    .Where(e => e.Id == eleveId)
                    .Select(e => e.NiveauScolaireId)
                    .FirstOrDefaultAsync(ct);

                dictee = new Dictee
                {
                    EleveId = eleveId,
                    MatiereId = conversation.MatiereId,
                    ConversationId = conversation.Id,
                    TexteDicte = texteDicte.Trim(),
                    DateCreation = maintenant,
                    NiveauScolaireId = niveauId == 0 ? null : niveauId,
                };

                _context.Dictees.Add(dictee);
            }

            dictee.Titre = Tronquer(titre, 300) ?? dictee.Titre ?? TitreParDefaut(texteDicte);
            dictee.Copie = copie.Trim();
            dictee.Remarque = Tronquer(remarque, 2000);
            dictee.Etat = nouvelEtat;
            dictee.DateMiseAJour = maintenant;

            await _context.SaveChangesAsync(ct);

            return await _context.Dictees
                .AsNoTracking()
                .Where(d => d.Id == dictee.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        /// <summary>
        /// L'élève est parti avant que la dictée en cours n'ait pu être
        /// archivée par le professeur lui-même — « Quitter le cours » ne
        /// produit aucun nouveau message, donc aucune occasion d'écrire
        /// [DICTEE_CORRIGEE]. On reconstitue alors ce qu'on peut, en
        /// `en_attente` : le texte dicté (le vrai, tel qu'il a été prononcé —
        /// pas reformulé par personne), et sa copie si elle est arrivée.
        ///
        /// SANS COPIE, ON N'ARCHIVE RIEN — même règle qu'ailleurs : rien à
        /// comparer, rien à montrer.
        ///
        /// SI CETTE DICTÉE EST DÉJÀ ARCHIVÉE (en attente ou corrigée), ON NE
        /// TOUCHE À RIEN — la correction, plus tard, la retrouvera par son
        /// texte, comme n'importe quelle autre dictée en attente. Ce chemin
        /// ne sert qu'à combler l'ABSENCE de ligne, jamais à en modifier une.
        /// </summary>
        public async Task<DicteeEleve?> ArchiverAbandonneeAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            // Une quarantaine de messages suffit largement à couvrir une
            // dictée et la tentative de correction qui l'a suivie — pas besoin
            // de remonter plus loin dans une conversation qui vit des mois.
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Id)
                .Take(40)
                .Select(m => new { m.Id, m.Role, m.Contenu })
                .ToListAsync(ct);

            messages.Reverse();

            string? texteDicte = null;
            var indexDictee = -1;

            for (var i = messages.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(messages[i].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    continue;

                var texte = TexteDicteOral(messages[i].Contenu);
                if (texte is null) continue;

                texteDicte = texte;
                indexDictee = i;
                break;
            }

            if (texteDicte is null) return null;

            // L'élève n'en voulait plus : on ne la recrée surtout pas à son
            // départ — c'est exactement la trace qu'il a demandé d'effacer.
            if (messages.Skip(indexDictee + 1)
                .Any(m => EstDuProfesseur(m.Role) && SupprimeLaDerniere(m.Contenu)))
                return null;

            var cle = CleDictee(texteDicte);

            var textesExistants = await _context.Dictees
                .AsNoTracking()
                .Where(d => d.EleveId == eleveId && d.MatiereId == conversation.MatiereId)
                .Select(d => d.TexteDicte)
                .ToListAsync(ct);

            // Déjà archivée — en attente ou corrigée, peu importe : ce chemin
            // ne comble qu'une ligne ABSENTE, jamais n'en modifie une.
            if (textesExistants.Any(t => CleDictee(t) == cle)) return null;

            // Sa copie, arrivée après la dictée — tapée, ou en photo.
            var messagesApres = messages.Skip(indexDictee + 1)
                .Where(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (messagesApres.Count == 0) return null;

            var idsApres = messagesApres.Select(m => m.Id).ToList();
            var idsAvecPiece = (await _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.MessageId != null && idsApres.Contains(p.MessageId.Value))
                .Select(p => p.MessageId!.Value)
                .ToListAsync(ct))
                .ToHashSet();

            // LA PHOTO PASSE AVANT TOUT AUTRE MESSAGE, MÊME PLUS LOIN.
            //
            // Sans cette priorité, un premier message de correction un peu
            // long — une réponse de l'élève à la question du professeur,
            // « il est placé avant » — passait pour la copie, et la vraie
            // photo envoyée juste après n'était jamais regardée. Relevé en
            // séance : la dictée archivée portait comme « copie » la réponse
            // d'un élève à une question, jamais son texte manuscrit.
            var messagePhoto = messagesApres.FirstOrDefault(m => idsAvecPiece.Contains(m.Id));

            string? copie = messagePhoto is not null
                ? "(La copie est arrivée en photo, mais n'a pas encore été relue par le professeur.)"
                : null;

            // SANS PHOTO, SEULE UNE COPIE RENDUE AU CLAVIER COMPTE — celle que
            // l'écran a marquée comme telle au moment du rendu.
            //
            // On retombait auparavant sur le premier message « juste assez
            // long » : une dictée interrompue pendant la lecture s'archivait
            // alors avec « tu peux répéter la dernière phrase ? » en guise de
            // copie. Depuis le 11/09/2026, une dictée sans copie est annulée,
            // pas archivée.
            copie ??= messagesApres
                .Where(m => EstCopieClavier(m.Contenu))
                .Select(m => TexteUtile(m.Contenu))
                .LastOrDefault(t => t is not null);

            // Rien n'est jamais arrivé : rien à archiver, comme si la dictée
            // n'avait pas eu lieu.
            if (copie is null) return null;

            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync(ct);

            var maintenant = DateTime.UtcNow;

            var dictee = new Dictee
            {
                EleveId = eleveId,
                MatiereId = conversation.MatiereId,
                ConversationId = conversation.Id,
                TexteDicte = texteDicte.Trim(),
                Titre = TitreParDefaut(texteDicte),
                Copie = copie,
                Remarque = "La séance s'est terminée avant la correction — elle reprendra au "
                    + "prochain cours.",
                Etat = EtatDictee.EnAttente,
                DateCreation = maintenant,
                DateMiseAJour = maintenant,
                NiveauScolaireId = niveauId == 0 ? null : niveauId,
            };

            _context.Dictees.Add(dictee);
            await _context.SaveChangesAsync(ct);

            return await _context.Dictees
                .AsNoTracking()
                .Where(d => d.Id == dictee.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        /// <summary>
        /// Combien de messages on relit pour retrouver la dernière dictée.
        /// Une dictée, sa relecture et sa correction tiennent dans bien moins ;
        /// la marge couvre une correction qui s'étire sur toute une séance.
        /// </summary>
        private const int FenetreDerniereDictee = 200;

        /// <summary>
        /// Au-delà de ce nombre de messages du professeur sans copie, la
        /// dictée n'a pas été interrompue : elle a été laissée de côté — l'élève
        /// a préféré autre chose, et le professeur a suivi.
        /// </summary>
        private const int MessagesProfesseurMaxSansCopie = 3;

        private sealed record MessageLu(int Id, string? Role, string? Contenu);

        private static bool EstDuProfesseur(string? role) =>
            string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase);

        private static bool Contient(string? contenu, string marqueur) =>
            contenu?.Contains(marqueur, StringComparison.OrdinalIgnoreCase) == true;

        [GeneratedRegex(@"\[DICTEE_SUPPRIMEE\](?<corps>.*?)\[/DICTEE_SUPPRIMEE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Suppression();

        /// <summary>
        /// Le professeur a déclaré que l'élève ne veut plus de la DERNIÈRE
        /// dictée — le bloc sans numéro. Un bloc numéroté vise une dictée
        /// archivée précise, pas celle en cours.
        /// </summary>
        private static bool SupprimeLaDerniere(string? contenu) =>
            !string.IsNullOrEmpty(contenu)
            && Suppression().Matches(contenu).Any(m => !m.Groups["corps"].Value.Any(char.IsDigit));

        /// <summary>Deux textes de dictée assez proches pour être la même.</summary>
        private static bool Ressemblent(HashSet<string> a, HashSet<string> b) =>
            a.Count > 0 && b.Count > 0
            && a.Count(b.Contains) * 2 >= Math.Min(a.Count, b.Count);

        /// <summary>
        /// La copie rendue au clavier : l'écran la marque au moment du rendu,
        /// et c'est le seul message de l'élève qui porte ce marqueur.
        /// </summary>
        private static bool EstCopieClavier(string? contenu) =>
            Contient(contenu, "[DICTÉE AU CLAVIER");

        /// <summary>
        /// La dernière dictée de la conversation : les messages qui l'ont
        /// suivie, et si sa copie est vraiment arrivée — rendue au clavier, ou
        /// en photo. Rien d'autre ne compte comme copie.
        /// </summary>
        private async Task<(bool Existe, List<MessageLu> Apres, bool CopieRecue)> LireDerniereDicteeAsync(
            int conversationId, int eleveId, CancellationToken ct)
        {
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId && m.Conversation!.EleveId == eleveId)
                .OrderByDescending(m => m.Id)
                .Take(FenetreDerniereDictee)
                .Select(m => new MessageLu(m.Id, m.Role, m.Contenu))
                .ToListAsync(ct);

            messages.Reverse();

            var index = messages.FindLastIndex(
                m => EstDuProfesseur(m.Role) && TexteDicteOral(m.Contenu) is not null);

            if (index < 0) return (false, [], false);

            var apres = messages.Skip(index + 1).ToList();
            var deLEleve = apres.Where(m => !EstDuProfesseur(m.Role)).ToList();

            if (deLEleve.Any(m => EstCopieClavier(m.Contenu))) return (true, apres, true);

            var ids = deLEleve.Select(m => m.Id).ToList();

            var photo = ids.Count > 0 && await _context.PiecesJointes
                .AsNoTracking()
                .AnyAsync(p => p.MessageId != null && ids.Contains(p.MessageId.Value), ct);

            return (true, apres, photo);
        }

        public async Task<InterruptionDictee> EtatInterruptionAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var (existe, apres, copieRecue) = await LireDerniereDicteeAsync(conversationId, eleveId, ct);
            if (!existe) return InterruptionDictee.Aucune;

            // Corrigée : elle est allée au bout.
            if (apres.Any(m => EstDuProfesseur(m.Role) && Contient(m.Contenu, "[DICTEE_CORRIGEE]")))
                return InterruptionDictee.Aucune;

            // L'élève n'en voulait plus : elle n'existe plus, rien à annoncer.
            if (apres.Any(m => EstDuProfesseur(m.Role) && SupprimeLaDerniere(m.Contenu)))
                return InterruptionDictee.Aucune;

            // Déjà déclarée perdue. Elle reste « à annoncer » tant que l'élève
            // n'a pas reparlé : c'est son retour qui doit l'apprendre, pas le
            // compte rendu écrit après son départ.
            var abandon = apres.FindLastIndex(
                m => EstDuProfesseur(m.Role) && Contient(m.Contenu, MarqueursDictee.Abandon));

            if (abandon >= 0)
            {
                return apres.Skip(abandon + 1).Any(m => !EstDuProfesseur(m.Role))
                    ? InterruptionDictee.Aucune
                    : InterruptionDictee.DejaNotee;
            }

            // Sa copie est là : ce n'est pas une interruption, c'est une
            // dictée en attente de correction — elle, s'archive.
            if (copieRecue) return InterruptionDictee.Aucune;

            if (apres.Count(m => EstDuProfesseur(m.Role)) > MessagesProfesseurMaxSansCopie)
                return InterruptionDictee.Aucune;

            return InterruptionDictee.ANoter;
        }

        public async Task<bool> PeutArchiverAsync(
            int conversationId, int eleveId, string texteDicte, CancellationToken ct = default)
        {
            var (existe, apres, copieRecue) = await LireDerniereDicteeAsync(conversationId, eleveId, ct);

            // Une dictée dont l'élève n'a plus voulu ne s'archive jamais, même
            // si sa copie était arrivée : elle a été supprimée de partout.
            var supprimee = apres.Any(m => EstDuProfesseur(m.Role) && SupprimeLaDerniere(m.Contenu));

            if (existe && copieRecue && !supprimee) return true;

            // Pas de copie pour la dernière : il peut encore s'agir de corriger
            // une dictée plus ancienne, restée en attente. On l'admet si le
            // texte déclaré lui ressemble — le modèle le redonne rarement au
            // mot près, jamais au point d'en changer la moitié.
            var matiereId = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => (int?)c.MatiereId)
                .FirstOrDefaultAsync(ct);

            if (matiereId is null) return false;

            var attendus = Mots(texteDicte);
            if (attendus.Count == 0) return false;

            var enAttente = await _context.Dictees
                .AsNoTracking()
                .Where(d => d.EleveId == eleveId
                            && d.MatiereId == matiereId
                            && d.Etat == EtatDictee.EnAttente)
                .Select(d => d.TexteDicte)
                .ToListAsync(ct);

            return enAttente.Any(texte => Ressemblent(Mots(texte), attendus));
        }

        public async Task<int> SupprimerAsync(
            int conversationId, int eleveId, int? dicteeId, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return 0;

            List<Dictee> cibles;

            if (dicteeId is int id)
            {
                cibles = await _context.Dictees
                    .Where(d => d.Id == id && d.EleveId == eleveId && d.Etat == EtatDictee.EnAttente)
                    .ToListAsync(ct);
            }
            else
            {
                // La dernière dictée de la conversation : si sa copie était
                // arrivée, elle a pu être archivée « en attente ». On la
                // retrouve par son texte. Si elle ne l'a jamais été, il n'y a
                // rien à supprimer — le bloc, resté dans le message, suffit à
                // ce qu'elle ne le soit jamais.
                var messages = await _context.Messages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId && m.Conversation!.EleveId == eleveId)
                    .OrderByDescending(m => m.Id)
                    .Take(FenetreDerniereDictee)
                    .Select(m => new { m.Role, m.Contenu })
                    .ToListAsync(ct);

                var texte = messages
                    .Where(m => EstDuProfesseur(m.Role))
                    .Select(m => TexteDicteOral(m.Contenu))
                    .FirstOrDefault(t => t is not null);

                if (texte is null) return 0;

                var attendus = Mots(texte);

                var enAttente = await _context.Dictees
                    .Where(d => d.EleveId == eleveId
                                && d.MatiereId == conversation.MatiereId
                                && d.Etat == EtatDictee.EnAttente)
                    .ToListAsync(ct);

                cibles = enAttente.Where(d => Ressemblent(Mots(d.TexteDicte), attendus)).ToList();
            }

            if (cibles.Count == 0) return 0;

            _context.Dictees.RemoveRange(cibles);
            await _context.SaveChangesAsync(ct);

            return cibles.Count;
        }

        /// <summary>
        /// UNE DICTÉE SANS TITRE PREND LE DÉBUT DE SON TEXTE.
        ///
        /// Relevé par Camara le 11/09/2026 : dans « Mes dictées », une ligne
        /// intitulée « Dictée », entre « Le chat qui dormait » et « Karim le
        /// jour de l'examen ». Le titre vient du bloc du professeur ; quand il
        /// ne l'écrit pas, les six premiers mots disent au moins de quoi il
        /// s'agit.
        /// </summary>
        private static string? TitreParDefaut(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var premierePhrase = texte.Split(['.', '!', '?', '\n'], 2)[0].Trim();
            var mots = premierePhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return mots.Length <= 6
                ? premierePhrase
                : string.Join(' ', mots.Take(6)) + "…";
        }

        private const string RemarqueCorrigeeEnSeance =
            "Corrigée en séance, avec les erreurs reprises une à une.";

        private const string DebutRemarqueSansCorrection = "La séance s'est terminée avant la correction";

        /// <summary>Deux textes de dictée QUASI identiques — plus strict que <see cref="Ressemblent"/>.</summary>
        private static bool Identiques(HashSet<string> a, HashSet<string> b) =>
            a.Count > 0 && b.Count > 0
            && a.Count(b.Contains) * 10 >= Math.Max(a.Count, b.Count) * 8;

        [GeneratedRegex(@"\[ARDOISE\](?<corps>.*?)\[/ARDOISE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"^\s*(la\s+)?dict[ée]e\s*:?\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex TitreDicteeTableau();

        [GeneratedRegex(@"^\s*(ta\s+)?copie\s*:?\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex TitreCopieTableau();

        /// <summary>
        /// La copie telle qu'elle a été posée la PREMIÈRE fois au tableau,
        /// sous « Ta copie » — au cahier, c'est la retranscription de la
        /// photo, faite avant toute correction.
        /// </summary>
        private static string? CopieDuTableau(IEnumerable<MessageLu> messages)
        {
            foreach (var message in messages.Where(m => EstDuProfesseur(m.Role)))
            {
                foreach (Match bloc in Ardoise().Matches(message.Contenu ?? ""))
                {
                    var lignes = bloc.Groups["corps"].Value.Split('\n');
                    var dictee = Array.FindIndex(lignes, l => TitreDicteeTableau().IsMatch(l));
                    if (dictee < 0) continue;

                    var copie = Array.FindIndex(lignes, dictee + 1, l => TitreCopieTableau().IsMatch(l));
                    if (copie < 0) continue;

                    var texte = string.Join('\n', lignes.Skip(copie + 1)).Trim();
                    if (!string.IsNullOrWhiteSpace(texte)) return texte;
                }
            }

            return null;
        }

        public async Task<DicteeEleve?> ConstaterCorrectionAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var (existe, apres, copieRecue) = await LireDerniereDicteeAsync(conversationId, eleveId, ct);
            if (!existe || !copieRecue) return null;

            if (apres.Any(m => EstDuProfesseur(m.Role) && SupprimeLaDerniere(m.Contenu))) return null;

            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            var texte = await TexteDicteCompletAsync(conversationId, eleveId, ct);
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var attendus = Mots(texte);
            var maintenant = DateTime.UtcNow;

            var siennes = await _context.Dictees
                .Where(d => d.EleveId == eleveId && d.MatiereId == conversation.MatiereId)
                .ToListAsync(ct);

            // Déjà corrigée : rien à faire.
            if (siennes.Any(d => d.Etat == EtatDictee.Corrigee && Identiques(Mots(d.TexteDicte), attendus)))
                return null;

            var copieTableau = CopieDuTableau(apres);

            // Archivée en attente — le plus souvent au départ de l'élève : elle
            // passe corrigée, et perd la remarque qui disait le contraire.
            var existante = siennes.FirstOrDefault(d =>
                d.Etat == EtatDictee.EnAttente && Ressemblent(Mots(d.TexteDicte), attendus));

            var dictee = existante;

            if (dictee is null)
            {
                var copie = apres
                    .Where(m => !EstDuProfesseur(m.Role) && EstCopieClavier(m.Contenu))
                    .Select(m => TexteUtile(m.Contenu))
                    .LastOrDefault(t => t is not null)
                    ?? copieTableau;

                if (copie is null) return null;

                var niveauId = await _context.Eleves
                    .AsNoTracking()
                    .Where(e => e.Id == eleveId)
                    .Select(e => e.NiveauScolaireId)
                    .FirstOrDefaultAsync(ct);

                dictee = new Dictee
                {
                    EleveId = eleveId,
                    MatiereId = conversation.MatiereId,
                    ConversationId = conversation.Id,
                    TexteDicte = texte.Trim(),
                    Copie = copie,
                    DateCreation = maintenant,
                    NiveauScolaireId = niveauId == 0 ? null : niveauId,
                };

                _context.Dictees.Add(dictee);
            }
            else if (dictee.Copie.StartsWith("(La copie est arrivée en photo", StringComparison.Ordinal)
                     && copieTableau is not null)
            {
                // Au cahier : la retranscription du tableau vaut mieux que la
                // mention « arrivée en photo » — c'est elle qui porte les badges.
                dictee.Copie = copieTableau;
            }

            dictee.Etat = EtatDictee.Corrigee;
            dictee.Titre ??= TitreParDefaut(dictee.TexteDicte);

            if (string.IsNullOrWhiteSpace(dictee.Remarque)
                || dictee.Remarque.StartsWith(DebutRemarqueSansCorrection, StringComparison.Ordinal))
            {
                dictee.Remarque = RemarqueCorrigeeEnSeance;
            }

            dictee.DateMiseAJour = maintenant;

            await _context.SaveChangesAsync(ct);

            return await _context.Dictees
                .AsNoTracking()
                .Where(d => d.Id == dictee.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        /// <summary>
        /// Le texte du DERNIER bloc [DICTEE] (ou [DICTEE:CLAVIER]) de ce
        /// message — celui, purement vocal, qui pilote le débit lent côté
        /// client (voir `ardoise.js`). Distinct du bloc [DICTEE_CORRIGEE] lu
        /// par <see cref="LecteurDictee"/> : ceci est le texte tel qu'il a
        /// été dicté à l'oral, jamais encore une archive.
        /// </summary>
        private static string? TexteDicteOral(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var correspondances = System.Text.RegularExpressions.Regex.Matches(
                message, @"\[DICTEE(?::CLAVIER)?\](?<corps>.*?)\[/DICTEE\]",
                System.Text.RegularExpressions.RegexOptions.Singleline
                    | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (correspondances.Count == 0) return null;

            var texte = correspondances[^1].Groups["corps"].Value.Trim();
            return string.IsNullOrWhiteSpace(texte) ? null : texte;
        }

        /// <summary>
        /// Ce qu'un message de l'élève porte de réellement écrit, une fois
        /// le marqueur technique de dictée au cahier retiré — ou null s'il ne
        /// reste rien d'assez substantiel pour être une copie.
        ///
        /// LE SEUIL EST DÉLIBÉRÉMENT COURT : mieux vaut archiver une copie un
        /// peu courte que n'archiver aucune dictée du tout, ce que le bloc
        /// [DICTEE_CORRIGEE] de la vraie correction viendra remplacer de
        /// toute façon dès qu'elle a lieu.
        /// </summary>
        private static string? TexteUtile(string? contenu)
        {
            if (string.IsNullOrWhiteSpace(contenu)) return null;

            var sansMarqueur = System.Text.RegularExpressions.Regex.Replace(
                contenu, @"\n?\[DICTÉE AU (?:CAHIER|CLAVIER)[^\]]*\]$", string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            return sansMarqueur.Length >= 15 ? sansMarqueur : null;
        }

        public async Task<IEnumerable<DicteeEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default) =>
            await _context.Dictees
                .AsNoTracking()
                .Where(d => d.EleveId == eleveId && d.MatiereId == matiereId)
                .OrderByDescending(d => d.DateMiseAJour)
                .Select(Projection)
                .ToListAsync(ct);

        public async Task<DicteeEleve?> GetDetailAsync(
            int dicteeId, int eleveId, CancellationToken ct = default) =>
            // Le filtre sur l'élève est la garde d'accès.
            await _context.Dictees
                .AsNoTracking()
                .Where(d => d.Id == dicteeId && d.EleveId == eleveId)
                .Select(d => new DicteeEleve
                {
                    Id = d.Id,
                    MatiereId = d.MatiereId,
                    MatiereLibelle = d.Matiere!.Libelle,
                    ProfPrenom = d.Matiere.ProfPrenom,
                    ProfCouleur = d.Matiere.ProfCouleur,
                    ConversationId = d.ConversationId,
                    Titre = d.Titre,
                    TexteDicte = d.TexteDicte,
                    Copie = d.Copie,
                    Remarque = d.Remarque,
                    Etat = d.Etat,
                    DateCreation = d.DateCreation,
                    DateMiseAJour = d.DateMiseAJour,
                    DateConsultation = d.DateConsultation,
                    ElevePrenom = d.Eleve!.Prenom,
                    EleveNom = d.Eleve.Nom,
                    EleveNiveau = d.Eleve.NiveauScolaire!.Libelle,
                })
                .FirstOrDefaultAsync(ct);

        public async Task<bool> MarquerVueAsync(
            int dicteeId, int eleveId, CancellationToken ct = default)
        {
            var dictee = await _context.Dictees
                .FirstOrDefaultAsync(d => d.Id == dicteeId && d.EleveId == eleveId, ct);

            if (dictee is null) return false;

            dictee.DateConsultation = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<IReadOnlyDictionary<int, CompteurDictees>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default)
        {
            var lignes = await _context.Dictees
                .AsNoTracking()
                .Where(d => d.EleveId == eleveId)
                .GroupBy(d => d.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    Total = g.Count(),
                    // Une dictée mise à jour (correction arrivée après coup)
                    // redevient « à consulter », même déjà lue une fois —
                    // même logique que les fiches de révision.
                    Nouveautes = g.Count(d =>
                        d.DateConsultation == null || d.DateConsultation < d.DateMiseAJour),
                })
                .ToListAsync(ct);

            return lignes.ToDictionary(
                x => x.MatiereId, x => new CompteurDictees(x.Total, x.Nouveautes));
        }

        private static readonly System.Linq.Expressions.Expression<Func<Dictee, DicteeEleve>> Projection =
            d => new DicteeEleve
            {
                Id = d.Id,
                MatiereId = d.MatiereId,
                MatiereLibelle = d.Matiere!.Libelle,
                ProfPrenom = d.Matiere.ProfPrenom,
                ProfCouleur = d.Matiere.ProfCouleur,
                ConversationId = d.ConversationId,
                Titre = d.Titre,
                TexteDicte = d.TexteDicte,
                Copie = d.Copie,
                Remarque = d.Remarque,
                Etat = d.Etat,
                DateCreation = d.DateCreation,
                DateMiseAJour = d.DateMiseAJour,
                DateConsultation = d.DateConsultation,
            };

        /// <summary>
        /// Le texte vient d'un modèle : rien ne garantit qu'il respecte la
        /// longueur de colonne. On tronque plutôt que de laisser SQL Server
        /// rejeter l'insertion et perdre la dictée.
        /// </summary>
        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }

        /// <summary>
        /// La clé de rapprochement de deux textes dictés.
        ///
        /// Accents et casse écartés, espaces multiples ramenés à un seul :
        /// des différences de frappe, pas de contenu. Contrairement à
        /// <c>FicheRepository.CleNotion</c>, les MOTS NE SONT PAS TRIÉS —
        /// l'ordre est le texte lui-même dans une dictée, le perdre ferait
        /// rapprocher deux dictées différentes qui partagent leurs mots.
        /// </summary>

        /// <summary>
        /// LE TEXTE VRAIMENT DICTE PENDANT CETTE DICTEE, RELU DANS LES MESSAGES.
        ///
        /// Releve le 11/09/2026 : une dictee de plusieurs phrases archivee avec
        /// UNE SEULE — la derniere, celle que le professeur venait de relire
        /// parce que l eleve ne l avait pas retenue. En archivant, il ne
        /// recopie que ce qu il a sous les yeux.
        ///
        /// Le texte, lui, est dans les messages : chaque passage dicte est
        /// entoure de sa balise, mot pour mot, puisque c est elle qui commande
        /// la voix. Il n y a donc rien a deviner, seulement a relire.
        ///
        /// LES RELECTURES NE DOIVENT PAS SE DOUBLER. Une phrase redite trois
        /// fois apparait trois fois dans les messages et ne compte qu une : on
        /// deduplique sur la meme cle normalisee que le rapprochement, en
        /// gardant l ordre de premiere apparition — c est celui de la dictee.
        ///
        /// DEUX BORNES, ET IL EN FAUT DEUX.
        ///
        /// Le dernier [DICTEE_CORRIGEE] d abord : ce qui precede appartient a
        /// une dictee close, meme dans la meme seance.
        ///
        /// Mais cette borne seule ne suffit pas, et l essai sur donnees
        /// reelles l a montre avant la mise en service : une conversation
        /// portait TROIS dictees et pas une correction — le professeur n avait
        /// jamais pose le bloc. La fenetre remontait alors au premier message
        /// du fil et fusionnait les trois textes en un seul.
        ///
        /// D ou la seconde borne, temporelle : une dictee se fait d une traite,
        /// dans une seance qui dure une heure. Au-dela de trois heures, ce
        /// n est plus la meme.
        /// </summary>
        /// <summary>En deca, c est une replique de l eleve, pas une copie.</summary>
        private const int MotsMinimumCopie = 12;

        /// <summary>Part des mots qu une copie partage au moins avec le texte dicte.</summary>
        private const int PourcentMotsCommuns = 35;

        /// <summary>Au-dela, ce n est plus la meme dictee — une seance dure une heure.</summary>
        private static readonly TimeSpan FenetreDictee = TimeSpan.FromHours(3);

        public async Task<string?> TexteDicteCompletAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var depuisQuand = DateTime.UtcNow - FenetreDictee;

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId
                            && m.Conversation!.EleveId == eleveId
                            && m.DateCreation >= depuisQuand)
                .OrderBy(m => m.DateCreation)
                .Select(m => new { m.Role, m.Contenu })
                .ToListAsync(ct);

            if (messages.Count == 0) return null;

            var depuis = messages.FindLastIndex(
                m => m.Contenu != null
                     && m.Contenu.Contains("[DICTEE_CORRIGEE]", StringComparison.OrdinalIgnoreCase));

            var phrases = new List<string>();
            var vues = new HashSet<string>();

            for (var i = depuis + 1; i < messages.Count; i++)
            {
                if (messages[i].Role != "assistant") continue;

                foreach (Match passage in Passages().Matches(messages[i].Contenu ?? ""))
                {
                    foreach (var phrase in Decouper(passage.Groups["texte"].Value))
                    {
                        if (vues.Add(CleDictee(phrase))) phrases.Add(phrase);
                    }
                }
            }

            return phrases.Count == 0 ? null : string.Join(" ", phrases);
        }

        [GeneratedRegex(@"\[DICTEE\](?<texte>.*?)\[/DICTEE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Passages();

        /// <summary>
        /// Une dictee, phrase par phrase.
        ///
        /// C est le grain de la relecture : le professeur redit UNE phrase, pas
        /// tout le passage. Deduplique au bloc entier, la phrase relue seule
        /// serait comptee comme un passage neuf et le texte doublerait.
        /// </summary>
        private static IEnumerable<string> Decouper(string passage)
        {
            var courante = new StringBuilder();

            foreach (var c in passage.Replace('\n', ' ').Replace('\r', ' '))
            {
                courante.Append(c);

                if (c is '.' or '!' or '?')
                {
                    var phrase = courante.ToString().Trim();
                    if (phrase.Length > 0) yield return phrase;
                    courante.Clear();
                }
            }

            // La derniere phrase n a pas toujours sa ponctuation : le professeur
            // relit parfois un fragment, et le perdre serait pire que le garder.
            var reste = courante.ToString().Trim();
            if (reste.Length > 0) yield return reste;
        }


        /// <summary>
        /// LA COPIE QUE L ELEVE A VRAIMENT RENDUE, RELUE DANS SES MESSAGES.
        ///
        /// Symetrique de <see cref="TexteDicteCompletAsync"/>, et pour la meme
        /// raison : le professeur ne recopie dans son bloc que ce qu il a sous
        /// les yeux. Releve le 11/09/2026 — la copie archivee tenait en une
        /// ligne quand l eleve en avait ecrit dix.
        ///
        /// LE PLUS LONG MESSAGE NE SUFFIT PAS COMME CRITERE. Une autre dictee
        /// avait archive « Il est place avant. » : une phrase de conversation
        /// prise pour une copie. On exige donc que le texte PARTAGE DES MOTS
        /// avec ce qui a ete dicte — une copie, meme fautive, reprend forcement
        /// l essentiel du vocabulaire du texte.
        ///
        /// Null si rien de credible n a ete trouve : mieux vaut garder ce que
        /// le professeur a ecrit que de lui substituer un bavardage.
        /// </summary>
        public async Task<string?> CopieCompleteAsync(
            int conversationId, int eleveId, string texteDicte, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(texteDicte)) return null;

            var depuisQuand = DateTime.UtcNow - FenetreDictee;

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId
                            && m.Conversation!.EleveId == eleveId
                            && m.DateCreation >= depuisQuand
                            && m.Role != "assistant")
                .OrderBy(m => m.DateCreation)
                .Select(m => m.Contenu)
                .ToListAsync(ct);

            var attendus = Mots(texteDicte);
            if (attendus.Count == 0) return null;

            string? meilleure = null;

            foreach (var brut in messages)
            {
                // Le marqueur du mode d ecriture voyage avec le message de
                // l eleve : il n appartient pas a sa copie.
                var texte = SansMarqueurs(brut);
                if (string.IsNullOrWhiteSpace(texte)) continue;

                // DOUZE MOTS AU MOINS : une copie de dictee est longue.
                //
                // C est ce seuil, plus que la ressemblance, qui ecarte les
                // repliques de l eleve. « Est-ce que tu peux repeter la
                // phrase ? » partage la moitie de ses mots avec le texte
                // dicte — elle passerait n importe quel test de vocabulaire.
                var mots = Mots(texte);
                if (mots.Count < MotsMinimumCopie) continue;

                // UN TIERS DE MOTS COMMUNS, PAS LA MOITIE.
                //
                // Mesure sur une copie reelle : 41 % de mots en commun avec le
                // texte dicte, et c etait bien une copie. Les fautes changent
                // les mots — « rit » pour « ri », « inatendu » pour
                // « inattendue » — et une exigence a la moitie rejetait
                // justement les copies les plus fautives, celles qui ont le
                // plus besoin d etre corrigees.
                var communs = mots.Count(attendus.Contains);
                if (communs * 100 < mots.Count * PourcentMotsCommuns) continue;

                if (meilleure is null || texte.Length > meilleure.Length) meilleure = texte;
            }

            return meilleure;
        }

        /// <summary>Les mots normalises d un texte, pour comparer sans l orthographe.</summary>
        private static HashSet<string> Mots(string texte) =>
            CleDictee(texte).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        /// <summary>
        /// Retire ce que l interface a joint au message de l eleve.
        ///
        /// Le marqueur du mode d ecriture est ajoute par l ecran, entre
        /// crochets et en fin de message. Le laisser gonflerait la copie d une
        /// phrase que l enfant n a jamais ecrite.
        /// </summary>
        private static string SansMarqueurs(string? texte) =>
            string.IsNullOrWhiteSpace(texte)
                ? string.Empty
                : Marqueur().Replace(texte, string.Empty).Trim();

        // AU CLAVIER aussi : ce marqueur voyageait avec la copie, et la copie
        // archivée le portait en clair, phrase technique comprise.
        [GeneratedRegex(@"\[[^\]]*(?:AU CAHIER|AU CLAVIER|DEMANDE_DOCUMENT)[^\]]*\]",
            RegexOptions.IgnoreCase)]
        private static partial Regex Marqueur();

        private static string CleDictee(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return string.Empty;

            var sansAccents = new StringBuilder();

            foreach (var c in texte.Normalize(NormalizationForm.FormD))
            {
                var categorie = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categorie == UnicodeCategory.NonSpacingMark) continue;

                sansAccents.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }

            var mots = sansAccents.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', mots);
        }
    }
}
