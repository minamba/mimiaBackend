using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainConversation = SchoolWebApp.Domain.Models.Conversation;
using DomainMessage = SchoolWebApp.Domain.Models.Message;

// Alias plutôt qu'un using du namespace : `Conversation` et `Message` existent
// des deux côtés, et importer Domain.Models rendrait chaque usage ambigu.
using DomainPieceJointe = SchoolWebApp.Domain.Models.PieceJointe;
using MessageObserve = SchoolWebApp.Domain.Models.MessageObserve;
using SeanceAObserver = SchoolWebApp.Domain.Models.SeanceAObserver;
using ExerciceObserve = SchoolWebApp.Domain.Models.ExerciceObserve;
using QuestionEvaluation = SchoolWebApp.Domain.Models.QuestionEvaluation;

namespace SchoolWebApp.Dal.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ConversationRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<DomainConversation>> GetConversationsByEleveAsync(int eleveId)
        {
            var entities = await _context.Conversations
                .AsNoTracking()
                .Include(c => c.Matiere)
                .Where(c => c.EleveId == eleveId)
                .OrderByDescending(c => c.DateDernierMessage ?? c.DateCreation)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        public async Task<DomainConversation?> GetConversationByIdAsync(int id)
        {
            var entity = await _context.Conversations
                .AsNoTracking()
                .Include(c => c.Matiere)
                .FirstOrDefaultAsync(c => c.Id == id);

            return entity is null ? null : Map(entity);
        }

        public async Task<DomainConversation> AddConversationAsync(DomainConversation model)
        {
            // LA CLASSE DE L'ÉLÈVE AU MOMENT OÙ IL OUVRE LE COURS.
            //
            // C'est elle qui permettra de découper sa fiche par année. Lue ici
            // et pas à la relecture : le jour où le parent le fait passer en 3e,
            // les cours de 4e deviendraient sinon des cours de 3e. Rien
            // n'enregistre le changement de classe, donc ce qui n'est pas
            // capturé maintenant est perdu.
            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == model.EleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync();

            var entity = new Conversation
            {
                EleveId = model.EleveId,
                MatiereId = model.MatiereId,
                Titre = model.Titre,
                DateCreation = model.DateCreation == default ? DateTime.UtcNow : model.DateCreation,
                DatePurge = model.DatePurge,
                NiveauScolaireId = niveauId == 0 ? null : niveauId
            };

            _context.Conversations.Add(entity);
            await _context.SaveChangesAsync();

            return await GetConversationByIdAsync(entity.Id) ?? Map(entity);
        }

        /// <summary>
        /// L'historique envoyé au modèle, sur une fenêtre qui ne glisse QUE par
        /// paliers.
        ///
        /// Prendre bêtement les N derniers messages fait sortir le plus ancien
        /// à chaque tour : le préfixe envoyé à l'API n'est jamais deux fois le
        /// même, et la mise en cache le réécrit intégralement à chaque appel —
        /// mesuré à 4 129 tokens par tour, facturés 1,25×, pour être relus une
        /// seule fois. Le cache coûtait alors 37 % de plus que pas de cache.
        ///
        /// On fait donc varier la TAILLE de la fenêtre avec le total : elle
        /// enfle d'un message à chaque tour, puis se recale d'un coup. La borne
        /// basse, elle, ne bouge qu'une fois par palier, et entre deux
        /// recalages le préfixe ne fait que s'allonger — ce qui est exactement
        /// la forme qu'un cache sait exploiter.
        /// </summary>
        public async Task<IEnumerable<DomainMessage>> GetFenetreStableAsync(
            int conversationId, int plancher, int pas)
        {
            var total = await _context.Messages
                .CountAsync(m => m.ConversationId == conversationId);

            return await GetMessagesAsync(conversationId, plancher + (total % pas));
        }

        public async Task<IEnumerable<DomainMessage>> GetMessagesAsync(int conversationId, int limite)
        {
            // On prend les N derniers messages, puis on remet dans l'ordre
            // chronologique : l'API attend un historique du plus ancien au plus récent.
            var entities = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.DateCreation)
                .ThenByDescending(m => m.Id)
                .Take(limite)
                .ToListAsync();

            entities.Reverse();
            return entities.Select(Map).ToList();
        }

        public async Task<DomainMessage> AddMessageAsync(DomainMessage model)
        {
            var entity = new Message
            {
                ConversationId = model.ConversationId,
                Role = model.Role,
                Contenu = model.Contenu,
                Modele = model.Modele,
                TokensEntree = model.TokensEntree,
                TokensSortie = model.TokensSortie,
                TokensCacheLecture = model.TokensCacheLecture,
                TokensCacheEcriture = model.TokensCacheEcriture,
                TokensCacheEcriture1h = model.TokensCacheEcriture1h,
                DateCreation = model.DateCreation == default ? DateTime.UtcNow : model.DateCreation
            };

            _context.Messages.Add(entity);
            await _context.SaveChangesAsync();

            model.Id = entity.Id;
            model.DateCreation = entity.DateCreation;
            return model;
        }

        public async Task TouchConversationAsync(int conversationId, int eleveId)
        {
            var maintenant = DateTime.UtcNow;

            var conversation = await _context.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation is not null) conversation.DateDernierMessage = maintenant;

            var eleve = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == eleveId);
            if (eleve is not null) eleve.DerniereActivite = maintenant;

            await _context.SaveChangesAsync();
        }

        public async Task DefinirDureeChoisieAsync(int conversationId, int dureeMinutes, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

            if (conversation is null) return;

            conversation.DureeChoisieMinutes = dureeMinutes;
            await _context.SaveChangesAsync(ct);
        }

        public async Task DefinirModeSeanceAsync(
            int conversationId, string mode, int? controleId, string? epreuveCode, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

            if (conversation is null) return;

            // LES TROIS ENSEMBLE, TOUJOURS : un mode « cours » qui garderait le
            // contrôle de la séance précédente ferait reparler de ce contrôle.
            conversation.ModeSeance = mode;
            conversation.ModeControleId = controleId;
            conversation.ModeEpreuveCode = epreuveCode;
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Vrai s'il existe au moins un message ÉLÈVE postérieur au dernier
        /// compte rendu de cette conversation — ou, faute de compte rendu,
        /// postérieur à sa création.
        ///
        /// C'est la garde d'un départ anticipé : sans elle, cliquer « Quitter
        /// le cours » deux secondes après l'avoir ouvert, sans avoir rien dit,
        /// ferait quand même parler le professeur pour rien — un compte rendu
        /// sur un silence. Un seul message suffit en revanche à ne rien
        /// perdre : mieux vaut un bilan trop bref que pas de bilan du tout.
        /// </summary>
        public async Task<bool> ADuTravailNonConcluAsync(int conversationId, CancellationToken ct = default)
        {
            var dernierRapport = await _context.RapportsSeance
                .Where(r => r.ConversationId == conversationId)
                .OrderByDescending(r => r.DateCreation)
                .Select(r => (DateTime?)r.DateCreation)
                .FirstOrDefaultAsync(ct);

            var depuis = dernierRapport ?? DateTime.MinValue;

            return await _context.Messages.AnyAsync(
                m => m.ConversationId == conversationId
                    && m.Role == "user"
                    && m.DateCreation > depuis,
                ct);
        }

        public async Task<string?> GetIdentityUserIdAsync(
            int conversationId, CancellationToken ct = default) =>
            await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId)
                .Select(c => c.Eleve!.Parent!.IdentityUserId)
                .FirstOrDefaultAsync(ct);

        public async Task<SeanceAObserver?> GetSeanceAObserverAsync(
            int conversationId, CancellationToken ct = default)
        {
            var seances = await ChargerAsync(
                c => c.Id == conversationId
                     && c.DateDernierMessage != null
                     && (c.DateDerniereObservation == null
                         || c.DateDerniereObservation < c.DateDernierMessage),
                1, ct);

            return seances.FirstOrDefault();
        }

        public async Task<IEnumerable<SeanceAObserver>> GetSeancesAObserverAsync(
            TimeSpan inactivite, int limite, CancellationToken ct = default)
        {
            var limiteTemps = DateTime.UtcNow - inactivite;

            return await ChargerAsync(
                c => c.DateDernierMessage != null
                     && c.DateDernierMessage < limiteTemps
                     && (c.DateDerniereObservation == null
                         || c.DateDerniereObservation < c.DateDernierMessage),
                limite, ct);
        }

        private async Task<List<SeanceAObserver>> ChargerAsync(
            System.Linq.Expressions.Expression<Func<Conversation, bool>> filtre,
            int limite,
            CancellationToken ct)
        {
            var seances = await _context.Conversations
                .AsNoTracking()
                .Where(filtre)
                .OrderBy(c => c.DateDernierMessage)
                .Take(limite)
                .Select(c => new
                {
                    Seance = new SeanceAObserver
                    {
                        ConversationId = c.Id,
                        EleveId = c.EleveId,
                        ElevePrenom = c.Eleve!.Prenom,
                        MatiereId = c.MatiereId,
                        MatiereLibelle = c.Matiere!.Libelle,
                        NiveauLibelle = c.Eleve.NiveauScolaire!.Libelle,
                        NiveauOrdre = c.Eleve.NiveauScolaire.Ordre,
                        NiveauCode = c.Eleve.NiveauScolaire.Code,
                        ModeSeance = c.ModeSeance,
                        ModeControleId = c.ModeControleId,
                        ModeEpreuveCode = c.ModeEpreuveCode,
                        Jusqua = c.DateDernierMessage!.Value,
                    },
                    Depuis = c.DateDerniereObservation,
                })
                .ToListAsync(ct);

            foreach (var element in seances)
            {
                var depuis = element.Depuis;

                // Seulement les messages non encore analysés : sans ce filtre,
                // chaque passage réévaluerait toute la conversation et compterait
                // plusieurs fois les mêmes réussites.
                element.Seance.Messages = await _context.Messages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == element.Seance.ConversationId
                                && (depuis == null || m.DateCreation > depuis))
                    .OrderBy(m => m.DateCreation)
                    .Select(m => new MessageObserve(m.Role ?? "user", m.Contenu ?? "", m.DateCreation))
                    .ToListAsync(ct);

                element.Seance.Exercices =
                    await ExercicesAsync(element.Seance.ConversationId, depuis, ct);
            }

            return seances.Select(e => e.Seance).ToList();
        }

        /// <summary>
        /// Les exercices notés archivés pendant la fenêtre non encore analysée.
        ///
        /// ON LIT LES ARCHIVES, PAS LES MESSAGES. Le bloc que le professeur
        /// pose dans son message n est qu une des deux façons dont une archive
        /// naît : le rattrapage en écrit autant, depuis les messages bruts,
        /// quand le modèle a oublié de poser le bloc. Relevé le 10/09/2026 :
        /// quinze compréhensions orales en table pour cinq blocs en message,
        /// deux dictées pour aucun bloc. Chercher les exercices dans les
        /// messages en aurait donc raté les deux tiers.
        ///
        /// Même fenêtre que les messages — `depuis` — pour la même raison :
        /// sans elle, chaque passage du worker recompterait les mêmes copies.
        /// </summary>
        private async Task<List<ExerciceObserve>> ExercicesAsync(
            int conversationId, DateTime? depuis, CancellationToken ct)
        {
            var evaluations = await _context.Evaluations
                .AsNoTracking()
                .Where(e => e.ConversationId == conversationId
                            && (depuis == null || e.DateCreation > depuis))
                .Select(e => new ExerciceObserve
                {
                    Genre = "évaluation",
                    Titre = e.Notion,
                    Note = e.Note,
                    Bilan = e.ARevoir,
                    Date = e.DateCreation,
                    Attendu = e.Detail,
                })
                .ToListAsync(ct);

            // Le détail voyage en JSON dans la colonne : il est relu ici, pas
            // dans la requête — traduire une désérialisation en SQL est
            // impossible, et la faire côté base le serait tout autant.
            foreach (var evaluation in evaluations)
            {
                evaluation.Questions = Questions(evaluation.Attendu);
                evaluation.Attendu = null;
            }

            var dictees = await _context.Dictees
                .AsNoTracking()
                .Where(d => d.ConversationId == conversationId
                            && (depuis == null || d.DateCreation > depuis))
                .Select(d => new ExerciceObserve
                {
                    Genre = "dictée",
                    Titre = d.Titre,
                    Attendu = d.TexteDicte,
                    Production = d.Copie,
                    Bilan = d.Remarque,
                    Date = d.DateCreation,
                })
                .ToListAsync(ct);

            var comprehensions = await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.ConversationId == conversationId
                            && (depuis == null || c.DateCreation > depuis))
                .Select(c => new ExerciceObserve
                {
                    Genre = "compréhension orale",
                    Titre = c.Titre,
                    Langue = c.Langue,
                    Attendu = c.Passage,
                    Production = c.ReponseEleve,
                    Bilan = c.Comprehension,
                    Date = c.DateCreation,
                })
                .ToListAsync(ct);

            return evaluations
                .Concat(dictees)
                .Concat(comprehensions)
                .OrderBy(e => e.Date)
                .ToList();
        }

        /// <summary>Un détail illisible ne coûte que le détail, jamais l exercice.</summary>
        private static List<QuestionEvaluation> Questions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];

            try
            {
                return System.Text.Json.JsonSerializer
                    .Deserialize<List<QuestionEvaluation>>(json) ?? [];
            }
            catch (System.Text.Json.JsonException)
            {
                return [];
            }
        }

        // ------------------------------------------------------------------

        public async Task MarquerFermetureAsync(int conversationId)
        {
            // SANS L IDENTIFIANT DE L ELEVE, faute de jeton : l appel vient d une
            // page qui se ferme, et sendBeacon ne transporte pas d en-tete.
            // On ne pose qu une date de sortie, ce qui n ouvre rien.
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation is null) return;

            conversation.DateSortie = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task MarquerSortieAsync(int conversationId, int eleveId)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.EleveId == eleveId);

            if (conversation is null) return;

            conversation.DateSortie = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Consomme le marqueur de sortie : l'accueil de retour a été décidé,
        /// il ne doit plus l'être une seconde fois.
        ///
        /// POURQUOI DÈS LA DÉCISION, ET NON APRÈS L'ENREGISTREMENT
        /// -------------------------------------------------------
        /// La condition de retour s'éteignait toute seule — le message
        /// d'accueil devenait le dernier message, donc postérieur à la date de
        /// sortie. Mais elle ne s'éteignait qu'UNE FOIS LE MESSAGE ÉCRIT, à la
        /// fin de la génération. Deux requêtes rapprochées — le double montage
        /// de React en mode strict — lisaient donc toutes les deux « l'élève
        /// est parti », et l'élève recevait deux messages de bienvenue.
        ///
        /// Effacer le marqueur au moment de la décision ferme la fenêtre : la
        /// seconde requête retombe sur le délai de silence, le trouve trop
        /// court, et se tait.
        ///
        /// Rend vrai si le marqueur était bien présent — c'est ce booléen qui
        /// donne le droit d'accueillir.
        /// </summary>
        public async Task<bool> ConsommerSortieAsync(
            int conversationId, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

            if (conversation?.DateSortie is null) return false;

            conversation.DateSortie = null;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // ------------------------------------------------------------------
        // Pièces jointes
        // ------------------------------------------------------------------

        public async Task<int> AjouterPieceJointeAsync(
            DomainPieceJointe piece, CancellationToken ct = default)
        {
            var entity = new PieceJointe
            {
                ConversationId = piece.ConversationId,
                MessageId = piece.MessageId,
                NomFichier = piece.NomFichier,
                TypeMime = piece.TypeMime,
                Taille = piece.Taille,
                NombrePages = piece.NombrePages,
                Transcription = piece.Transcription,
                Donnees = piece.Donnees ?? Array.Empty<byte>(),
                DateCreation = piece.DateCreation == default ? DateTime.UtcNow : piece.DateCreation,
            };

            _context.PiecesJointes.Add(entity);
            await _context.SaveChangesAsync(ct);

            return entity.Id;
        }

        /// <summary>
        /// Sans les octets. La projection est explicite et non un Select sur
        /// l'entité entière : EF chargerait sinon le varbinary pour le jeter
        /// aussitôt, et un contrôle d'accès ferait transiter plusieurs
        /// mégaoctets pour lire un identifiant de conversation.
        /// </summary>
        public Task<DomainPieceJointe?> GetPieceJointeAsync(int id, CancellationToken ct = default) =>
            _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new DomainPieceJointe
                {
                    Id = p.Id,
                    ConversationId = p.ConversationId,
                    MessageId = p.MessageId,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                    NombrePages = p.NombrePages,
                    Transcription = p.Transcription,
                    DonneesEffaceesLe = p.DonneesEffaceesLe,
                    DateCreation = p.DateCreation,
                })
                .FirstOrDefaultAsync(ct);

        public async Task<DomainPieceJointe?> GetPieceJointeAvecDonneesAsync(
            int id, CancellationToken ct = default)
        {
            var entity = await _context.PiecesJointes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            return entity is null ? null : Map(entity, avecDonnees: true);
        }

        public async Task AttacherAuMessageAsync(
            int pieceJointeId, int messageId, CancellationToken ct = default)
        {
            var piece = await _context.PiecesJointes
                .FirstOrDefaultAsync(p => p.Id == pieceJointeId, ct);

            if (piece is null) return;

            piece.MessageId = messageId;
            await _context.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<DomainPieceJointe>> GetPiecesDesMessagesAsync(
            IEnumerable<int> messageIds, CancellationToken ct = default)
        {
            var ids = messageIds.ToList();
            if (ids.Count == 0) return Array.Empty<DomainPieceJointe>();

            var entities = await _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.MessageId != null && ids.Contains(p.MessageId.Value))
                .OrderBy(p => p.Id)
                .ToListAsync(ct);

            return entities.Select(e => Map(e, avecDonnees: true)).ToList();
        }

        public async Task<IEnumerable<DomainPieceJointe>> GetMetadonneesDesMessagesAsync(
            IEnumerable<int> messageIds, CancellationToken ct = default)
        {
            var ids = messageIds.ToList();
            if (ids.Count == 0) return Array.Empty<DomainPieceJointe>();

            return await _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.MessageId != null && ids.Contains(p.MessageId.Value))
                .OrderBy(p => p.Id)
                .Select(p => new DomainPieceJointe
                {
                    Id = p.Id,
                    ConversationId = p.ConversationId,
                    MessageId = p.MessageId,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                    NombrePages = p.NombrePages,
                    Transcription = p.Transcription,
                    DonneesEffaceesLe = p.DonneesEffaceesLe,
                    DateCreation = p.DateCreation,
                })
                .ToListAsync(ct);
        }

        public async Task<int> PurgerPiecesOrphelinesAsync(
            TimeSpan anciennete, CancellationToken ct = default)
        {
            var limite = DateTime.UtcNow - anciennete;

            return await _context.PiecesJointes
                .Where(p => p.MessageId == null && p.DateCreation < limite)
                .ExecuteDeleteAsync(ct);
        }

        public async Task<IEnumerable<DomainPieceJointe>> GetATranscrireAsync(
            int limite, CancellationToken ct = default)
        {
            var entities = await _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.Transcription == null && p.DonneesEffaceesLe == null)
                .OrderBy(p => p.DateCreation)
                .Take(limite)
                .ToListAsync(ct);

            return entities.Select(e => Map(e, avecDonnees: true)).ToList();
        }

        public async Task EnregistrerTranscriptionAsync(
            int pieceJointeId, string texte, CancellationToken ct = default)
        {
            // ExecuteUpdate plutôt qu'un chargement suivi d'un SaveChanges : la
            // ligne porte un varbinary de plusieurs mégaoctets, et EF le
            // ramènerait en mémoire pour écrire une colonne de texte.
            await _context.PiecesJointes
                .Where(p => p.Id == pieceJointeId)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.Transcription, texte), ct);
        }

        /// <summary>
        /// « ET DÉJÀ TRANSCRITS » n'est pas une précaution de plus, c'est LA
        /// condition. Effacer les octets d'un document dont le texte n'a pas
        /// encore été extrait le perdrait définitivement — et sans bruit.
        /// Un document que la transcription n'arrive pas à traiter reste donc
        /// en base : mieux vaut de la place occupée qu'une perte silencieuse.
        /// </summary>
        public async Task<(int Documents, long Octets)> PurgerOctetsAsync(
            TimeSpan anciennete, int limite, CancellationToken ct = default)
        {
            var limiteTemps = DateTime.UtcNow - anciennete;

            var cibles = await _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.DonneesEffaceesLe == null
                            && p.Transcription != null
                            && p.DateCreation < limiteTemps)
                .OrderBy(p => p.DateCreation)
                .Take(limite)
                .Select(p => new { p.Id, p.Taille })
                .ToListAsync(ct);

            if (cibles.Count == 0) return (0, 0);

            var ids = cibles.Select(c => c.Id).ToList();
            var maintenant = DateTime.UtcNow;

            await _context.PiecesJointes
                .Where(p => ids.Contains(p.Id))
                .ExecuteUpdateAsync(
                    m => m.SetProperty(p => p.Donnees, Array.Empty<byte>())
                          .SetProperty(p => p.DonneesEffaceesLe, maintenant),
                    ct);

            return (cibles.Count, cibles.Sum(c => (long)c.Taille));
        }

        private static DomainPieceJointe Map(PieceJointe entity, bool avecDonnees) => new()
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            MessageId = entity.MessageId,
            NomFichier = entity.NomFichier,
            TypeMime = entity.TypeMime,
            Taille = entity.Taille,
            NombrePages = entity.NombrePages,
            Transcription = entity.Transcription,
            DonneesEffaceesLe = entity.DonneesEffaceesLe,
            Donnees = avecDonnees ? entity.Donnees : null,
            DateCreation = entity.DateCreation,
        };

        public async Task MarquerObserveeAsync(
            int conversationId, DateTime jusqua, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

            if (conversation is null) return;

            conversation.DateDerniereObservation = jusqua;
            await _context.SaveChangesAsync(ct);
        }

        private static DomainConversation Map(Conversation entity) => new()
        {
            Id = entity.Id,
            EleveId = entity.EleveId,
            MatiereId = entity.MatiereId,
            Titre = entity.Titre,
            DateCreation = entity.DateCreation,
            DateDernierMessage = entity.DateDernierMessage,
            DatePurge = entity.DatePurge,
            DateSortie = entity.DateSortie,
            DureeChoisieMinutes = entity.DureeChoisieMinutes,
            ModeSeance = entity.ModeSeance,
            ModeControleId = entity.ModeControleId,
            ModeEpreuveCode = entity.ModeEpreuveCode,
            MatiereCode = entity.Matiere?.Code,
            MatiereLibelle = entity.Matiere?.Libelle,
            AgentSlug = entity.Matiere?.AgentSlug,
            ProfPrenom = entity.Matiere?.ProfPrenom,
            ProfAvatar = entity.Matiere?.ProfAvatar,
            ProfCouleur = entity.Matiere?.ProfCouleur
        };

        private static DomainMessage Map(Message entity) => new()
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            Role = entity.Role,
            Contenu = entity.Contenu,
            Modele = entity.Modele,
            TokensEntree = entity.TokensEntree,
            TokensSortie = entity.TokensSortie,
            TokensCacheLecture = entity.TokensCacheLecture,
            TokensCacheEcriture = entity.TokensCacheEcriture,
            TokensCacheEcriture1h = entity.TokensCacheEcriture1h,
            DateCreation = entity.DateCreation
        };
    }
}
