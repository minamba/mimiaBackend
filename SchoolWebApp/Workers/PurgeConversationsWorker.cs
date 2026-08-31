using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Efface les échanges verbatim des conversations arrivées à échéance.
    ///
    /// POURQUOI CE WORKER EXISTE
    /// -------------------------
    /// Chaque conversation reçoit à sa création une `DatePurge` fixée à douze
    /// mois, et un commentaire du code annonçait qu'« une tâche planifiée purge
    /// les messages ». Cette tâche n'existait pas. La base portait donc une
    /// promesse de conservation que rien n'appliquait — et la politique de
    /// confidentialité s'apprêtait à la publier.
    ///
    /// Une durée de conservation affichée et non tenue est un manquement au
    /// RGPD, et surtout un mensonge fait aux parents sur les mots de leurs
    /// enfants. Ce worker rend la promesse vraie.
    ///
    /// CE QU'IL EFFACE, ET CE QU'IL GARDE
    /// ----------------------------------
    /// Il efface les MESSAGES : les paroles de l'enfant et les réponses du
    /// professeur, mot pour mot. C'est la donnée sensible.
    ///
    /// Il conserve la conversation elle-même — son titre, ses dates — ainsi que
    /// les fiches de révision, les comptes rendus de séance et la maîtrise des
    /// compétences. Ce sont des travaux scolaires, pas des enregistrements de
    /// conversation : les effacer au bout d'un an priverait une famille du
    /// suivi de plusieurs années, qui est la raison d'être du produit. Cette
    /// distinction est écrite telle quelle dans la politique de
    /// confidentialité, et les deux doivent rester d'accord.
    /// </summary>
    public class PurgeConversationsWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<PurgeConversationsWorker> _logger;

        /// <summary>
        /// Une fois par jour. La précision n'a aucune importance ici — une
        /// échéance à douze mois ne se joue pas à l'heure près — et repasser
        /// plus souvent ne ferait qu'interroger la base pour rien.
        /// </summary>
        private static readonly TimeSpan Intervalle = TimeSpan.FromHours(24);

        /// <summary>
        /// Nombre de conversations traitées par LOT. Les messages d'une année
        /// entière peuvent se compter par milliers : tout supprimer en une
        /// transaction tiendrait la base verrouillée pendant qu'un enfant essaie
        /// de travailler.
        ///
        /// Ce n'est PAS un plafond journalier — voir <see cref="LotsMax"/>.
        /// </summary>
        private const int TailleLot = 50;

        /// <summary>
        /// Combien de lots enchaîner avant de se rendormir.
        ///
        /// LE LOT ÉTAIT DEVENU UN PLAFOND DE DÉBIT, ET C'ÉTAIT UN DÉFAUT.
        /// Un seul lot par passage, un passage par jour : la purge sortait
        /// cinquante documents par jour, quel que soit le nombre entré. À
        /// cinquante dépôts quotidiens — cent cinquante élèves qui envoient une
        /// photo tous les trois jours — l'équilibre était atteint ; au-delà, la
        /// base ne redescendait plus jamais. Sur SQL Server Express et ses dix
        /// gigaoctets, une base pleine n'accepte plus une écriture : c'est
        /// l'application entière qui s'arrête.
        ///
        /// Le lot reste, parce que sa raison d'être reste : ne pas verrouiller
        /// la base d'un bloc. C'est le fait de s'arrêter au premier qui était
        /// faux.
        ///
        /// Deux cents lots, soit dix mille conversations ou documents par
        /// passage. Le plafond n'est pas une politique de rétention, c'est un
        /// garde-fou : si un jour il est atteint, c'est qu'un compteur ment et
        /// que la boucle tourne à vide — d'où l'erreur journalisée.
        /// </summary>
        private const int LotsMax = 200;

        /// <summary>
        /// Le temps qu'on laisse à la base entre deux lots.
        ///
        /// Sans cette respiration, dix mille suppressions s'enchaînent sans
        /// interruption et la purge devient exactement ce que le découpage en
        /// lots cherchait à éviter : une charge continue pendant qu'un enfant
        /// travaille. Deux cents millisecondes ne coûtent rien à un travail qui
        /// n'est pressé par rien.
        /// </summary>
        private static readonly TimeSpan Respiration = TimeSpan.FromMilliseconds(200);

        public PurgeConversationsWorker(
            IServiceScopeFactory scopes,
            ILogger<PurgeConversationsWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Un délai au démarrage : le premier lancement coïncide avec les
            // migrations et le semis du référentiel, et rien ne presse.
            try { await Task.Delay(TimeSpan.FromMinutes(2), ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await PurgerAsync(ct);
                    await PurgerOrphelinesAsync(ct);
                    await AllegerDocumentsAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Un échec ne doit pas arrêter le worker : la purge
                    // reprendra demain, et les échéances ne sont pas à la
                    // journée près.
                    _logger.LogError(ex, "Echec de la purge des conversations.");
                }

                try { await Task.Delay(Intervalle, ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        /// <summary>
        /// La purge RGPD, jusqu'au bout de ce qui est échu.
        ///
        /// Elle enchaîne les lots au lieu de s'arrêter au premier : une échéance
        /// à douze mois arrive par vagues — une classe entière inscrite la même
        /// semaine échoit la même semaine — et un lot par jour mettrait un mois
        /// à écouler ce qu'un après-midi a produit.
        /// </summary>
        private async Task PurgerAsync(CancellationToken ct)
        {
            for (var lot = 0; lot < LotsMax; lot++)
            {
                if (await PurgerUnLotAsync(ct) == 0) return;

                if (lot == LotsMax - 1)
                {
                    _logger.LogError(
                        "Purge RGPD ARRETEE au plafond de {Lots} lots : quelque chose n'avance "
                        + "pas. Verifier les conversations echues a la main.", LotsMax);
                }

                try { await Task.Delay(Respiration, ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        /// <summary>Un lot. Rend le nombre de conversations réellement vidées.</summary>
        private async Task<int> PurgerUnLotAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SchoolWebAppDatabaseContext>();

            var maintenant = DateTime.UtcNow;

            // On ne retient que les conversations échues QUI PORTENT ENCORE DES
            // MESSAGES. Sans cette condition, la même liste reviendrait chaque
            // jour indéfiniment, et le worker rejouerait éternellement un
            // travail déjà fait.
            var aPurger = await db.Conversations
                .AsNoTracking()
                .Where(c => c.DatePurge != null
                            && c.DatePurge < maintenant
                            && db.Messages.Any(m => m.ConversationId == c.Id))
                .OrderBy(c => c.DatePurge)
                .Select(c => c.Id)
                .Take(TailleLot)
                .ToListAsync(ct);

            if (aPurger.Count == 0) return 0;

            // LES DOCUMENTS D'ABORD, ET CE N'EST PAS UN DÉTAIL D'ORDRE.
            //
            // Une pièce jointe référence son message. Supprimer les messages
            // en premier violerait la clé étrangère et ferait échouer toute la
            // purge — silencieusement, une fois par jour, jusqu'à ce que
            // quelqu'un lise les journaux.
            //
            // C'est aussi l'ordre qui a du sens : ce sont les données les plus
            // personnelles de la conversation. La copie d'un enfant porte son
            // nom, celui de son établissement, parfois son écriture.
            var documents = await db.PiecesJointes
                .Where(p => aPurger.Contains(p.ConversationId))
                .ExecuteDeleteAsync(ct);

            var effaces = await db.Messages
                .Where(m => aPurger.Contains(m.ConversationId))
                .ExecuteDeleteAsync(ct);

            _logger.LogInformation(
                "Purge RGPD : {Messages} message(s) et {Documents} document(s) effacé(s) "
                + "sur {Conversations} conversation(s) échue(s). "
                + "Fiches, rapports et progression conservés.",
                effaces, documents, aPurger.Count);

            // LE NOMBRE DE CONVERSATIONS RÉELLEMENT VIDÉES, et non celui qu'on
            // avait sélectionnées. La condition « qui portent encore des
            // messages » garantit aujourd'hui que les deux coïncident ; s'en
            // remettre à cette garantie, c'est signer pour une boucle infinie
            // le jour où elle changera.
            return effaces > 0 || documents > 0 ? aPurger.Count : 0;
        }

        /// <summary>
        /// Les documents déposés puis jamais envoyés.
        ///
        /// L'élève choisit une photo, change d'avis, ferme l'onglet : les
        /// octets restent en base sans message auquel se rattacher, et la purge
        /// des conversations ne les emportera qu'au bout d'un an. Vingt-quatre
        /// heures suffisent largement — au-delà, personne ne reprend un envoi
        /// abandonné.
        /// </summary>
        private async Task PurgerOrphelinesAsync(CancellationToken ct)
        {
            var total = 0;

            // DÉCOUPÉ EN LOTS, ALORS QU'IL NE L'ÉTAIT PAS.
            //
            // La suppression partait en un seul `DELETE` sans borne : trois
            // élèves abandonnent un envoi, ça ne se voit pas ; un incident
            // réseau qui en laisse dix mille en plan verrouille la table des
            // pièces jointes le temps de tout effacer — et c'est la table que
            // touche chaque envoi de document.
            for (var lot = 0; lot < LotsMax; lot++)
            {
                var effacees = await PurgerUnLotOrphelinesAsync(ct);
                total += effacees;

                if (effacees == 0) break;

                if (lot == LotsMax - 1)
                {
                    _logger.LogError(
                        "Purge des orphelines ARRETEE au plafond de {Lots} lots.", LotsMax);
                }

                try { await Task.Delay(Respiration, ct); }
                catch (OperationCanceledException) { break; }
            }

            if (total > 0)
            {
                _logger.LogInformation(
                    "{Documents} document(s) deposes puis jamais envoyes ont ete effaces.", total);
            }
        }

        private async Task<int> PurgerUnLotOrphelinesAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SchoolWebAppDatabaseContext>();

            var limite = DateTime.UtcNow - TimeSpan.FromHours(24);

            // EN DEUX TEMPS — SÉLECTION PUIS SUPPRESSION PAR CLÉS.
            //
            // Un `Take` posé directement sur un `ExecuteDelete` n'est pas
            // traduit par tous les fournisseurs, et échouerait à l'exécution,
            // pas à la compilation. C'est déjà la forme employée par la purge
            // des conversations, juste au-dessus.
            var ids = await db.PiecesJointes
                .AsNoTracking()
                .Where(p => p.MessageId == null && p.DateCreation < limite)
                .OrderBy(p => p.DateCreation)
                .Select(p => p.Id)
                .Take(TailleLot)
                .ToListAsync(ct);

            if (ids.Count == 0) return 0;

            return await db.PiecesJointes
                .Where(p => ids.Contains(p.Id))
                .ExecuteDeleteAsync(ct);
        }

        /// <summary>
        /// Allège les documents transcrits : les octets partent, le texte et la
        /// ligne restent.
        ///
        /// C'EST CETTE PURGE QUI REND LE STOCKAGE VIABLE. Sans elle, une photo
        /// de feuille occupe plusieurs mégaoctets pendant douze mois ; la base
        /// tournant sur SQL Server Express, plafonné à dix gigaoctets, elle
        /// serait pleine en quelques semaines — et une base pleine n'accepte
        /// plus une seule écriture, donc l'application entière s'arrête.
        ///
        /// Avec une conservation à trois jours, le volume ne croît plus : le
        /// résident vaut « dépôts par jour × jours de rétention », et ce qui
        /// entre remplace ce qui sort. Voir `JoursConservationDocuments`.
        /// </summary>
        private async Task AllegerDocumentsAsync(CancellationToken ct)
        {
            var documents = 0L;
            var octets = 0L;

            // C'EST ICI QUE LE PLAFOND FAISAIT LE PLUS DE MAL.
            //
            // Les octets des documents sont l'essentiel du volume de la base —
            // deux à trois mégaoctets la photo, contre deux kilo-octets sa
            // transcription. Cinquante par jour, c'était cent trente
            // mégaoctets sortis quotidiennement au mieux ; il en entre
            // davantage dès cinquante dépôts par jour.
            for (var lot = 0; lot < LotsMax; lot++)
            {
                var (faits, poids) = await AllegerUnLotAsync(ct);
                documents += faits;
                octets += poids;

                if (faits == 0) break;

                if (lot == LotsMax - 1)
                {
                    _logger.LogError(
                        "Allegement des documents ARRETE au plafond de {Lots} lots.", LotsMax);
                }

                try { await Task.Delay(Respiration, ct); }
                catch (OperationCanceledException) { break; }
            }

            if (documents > 0)
            {
                _logger.LogInformation(
                    "{Documents} document(s) alleges, {Mo:F1} Mo recuperes. "
                    + "Les transcriptions sont conservees.",
                    documents, octets / (1024.0 * 1024.0));
            }
        }

        private async Task<(int Documents, long Octets)> AllegerUnLotAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var conversations = scope.ServiceProvider
                .GetRequiredService<Domain.Services.IConversationService>();
            var options = scope.ServiceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<Services.OptionsClaude>>()
                .Value;

            return await conversations.PurgerOctetsAsync(
                TimeSpan.FromDays(options.JoursConservationDocuments), TailleLot, ct);
        }
    }
}
