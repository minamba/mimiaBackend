using SchoolWebApp.Api.Services;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Garantit qu'une échéance de référentiel n'est jamais oubliée, ET va
    /// vérifier lui-même si le texte officiel a bougé — mais s'arrête là.
    ///
    /// CE QUE CE WORKER FAIT, ET CE QU'IL NE FAIT PAS
    /// ------------------------------------------------
    /// Voulu par Camara le 13/09/2026 : « un worker qui garantit que les
    /// référentiels sont à jour tous les ans », puis, en réponse à une
    /// première version purement calendaire : « le référentiel existe sur le
    /// site du gouvernement, il faut y aller requêter en fonction de la date
    /// d'échéance ». Elle avait raison sur un point précis — les textes
    /// (Bulletin officiel) vivent bien à des adresses réelles et stables, et
    /// une machine PEUT constater qu'une page existe ou a changé, sans
    /// intervention humaine. C'est ce que fait `IVeilleReferentielService`.
    ///
    /// CE QUI RESTE HORS DE PORTÉE, ET POUR LA MÊME RAISON QU'AVANT :
    /// transformer le TEXTE de cette page en lignes de compétences reste un
    /// travail de jugement humain. `ReferentielSeeder` documente trois passes
    /// de vérification qui ont chacune trouvé une vraie erreur — jusqu'à un
    /// script qui n'avait lu que la première annexe d'un arrêté qui en
    /// portait trois. Ce worker ne lit donc jamais le CONTENU de la page :
    /// il compare une empreinte à la précédente, et dit « ça a changé »,
    /// jamais « voici ce qui a changé » ou « j'ai corrigé le référentiel ».
    ///
    /// ET LE FLUX STRUCTURÉ DU MINISTÈRE ? Il existe — `data.education.gouv.fr`,
    /// jeux « programmes d'enseignement » — et il est PÉRIMÉ : vérifié le
    /// 13/09/2026, rien après la rentrée 2021, ni la refonte de 2025 ni les
    /// textes de 2026. S'y fier aurait dit « inchangé » pendant qu'un
    /// programme entier était réécrit. Il n'est pas utilisé, et c'est voulu.
    ///
    /// DEUX PASSES, CHAQUE JOUR :
    ///   1. le RELEVÉ — toutes les lignes qui ont une adresse, quelle que
    ///      soit leur date : l'écran d'administration montre un statut par
    ///      ligne, et un statut vieux d'un an n'en est pas un ;
    ///   2. l'ALERTE — les échéances qui approchent, et toute page qui a
    ///      changé, sentinelle comprise.
    ///
    /// Il alerte, il n'agit pas — et il n'arrête de le faire que lorsqu'un
    /// humain a posé <see cref="Dal.Entities.EcheanceReferentiel.TraiteeLe"/>,
    /// preuve que la vérification a réellement eu lieu.
    /// </summary>
    public class EcheanceReferentielWorker : BackgroundService
    {
        /// <summary>
        /// LA NUIT, À 3 H (HEURE DE PARIS). Une fois par jour suffit — une
        /// échéance se compte en mois — et Camara a demandé que ça tourne
        /// quand personne ne travaille. Non que la charge soit lourde : une
        /// douzaine de pages lues l'une après l'autre, un hachage de texte
        /// chacune, quelques courriels — moins qu'une seule séance d'élève.
        /// Mais un passage à 3 h ne partage jamais le serveur avec un enfant.
        /// </summary>
        private const int HeureDePassage = 3;

        private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

        /// <summary>
        /// On commence à alerter deux mois avant. Assez tôt pour laisser le
        /// temps de relire un texte officiel et de corriger le référentiel
        /// avant la rentrée qu'il annonce ; pas si tôt qu'une échéance
        /// lointaine se noie dans le bruit d'alertes trop précoces.
        /// </summary>
        private static readonly TimeSpan FenetreAlerte = TimeSpan.FromDays(60);

        /// <summary>
        /// Une semaine entre deux rappels de la MÊME échéance non traitée.
        /// Assez pour ne pas harceler, assez peu pour qu'elle ne se perde pas
        /// dans une boîte de réception.
        /// </summary>
        private static readonly TimeSpan EspaceEntreRappels = TimeSpan.FromDays(7);

        private readonly IServiceScopeFactory _scopes;
        private readonly IVeilleReferentielService _veille;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EcheanceReferentielWorker> _logger;

        /// <summary>
        /// PAS D'`IServiceEmail` INJECTÉ ICI, ET C'EST VOULU.
        ///
        /// Un worker vit en Singleton pour toute la durée du serveur ;
        /// `IServiceEmail` est Scoped (il porte le contexte de base). Le
        /// prendre dans ce constructeur ferait planter le démarrage entier du
        /// serveur — « Cannot consume scoped service … from singleton » — et
        /// pas seulement ce worker : ASP.NET Core valide TOUS les services au
        /// démarrage, avant qu'aucune route ne réponde.
        ///
        /// `IVeilleReferentielService`, lui, est enregistré via
        /// `AddHttpClient&lt;…&gt;`, donc Transient : un Singleton peut le
        /// consommer directement, sans ce problème.
        /// </summary>
        public EcheanceReferentielWorker(
            IServiceScopeFactory scopes, IVeilleReferentielService veille,
            IConfiguration configuration, ILogger<EcheanceReferentielWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _veille = veille ?? throw new ArgumentNullException(nameof(veille));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Laisse l'application finir de démarrer : migrations et semis,
            // dont les échéances que ce worker suppose déjà en place.
            try { await Task.Delay(TimeSpan.FromMinutes(2), ct); }
            catch (OperationCanceledException) { return; }

            // AU DÉMARRAGE, SEULEMENT CE QUI N'A JAMAIS ÉTÉ RELEVÉ. Une ligne
            // fraîchement semée afficherait sinon « pas encore vérifiée »
            // jusqu'à la nuit suivante — alors qu'un premier relevé ne coûte
            // qu'une lecture. Tout le reste attend 3 h du matin.
            try { await ReleverAsync(seulementJamaisRelevees: true, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { _logger.LogError(ex, "Echec du premier releve des echeances de referentiel."); }

            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(DelaiJusquaProchainPassage(DateTime.UtcNow), ct); }
                catch (OperationCanceledException) { break; }

                try
                {
                    await ReleverAsync(seulementJamaisRelevees: false, ct);
                    await SignalerAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec du passage sur les echeances de referentiel.");
                }
            }
        }

        /// <summary>
        /// Le temps qui reste avant la prochaine occurrence de 3 h à Paris —
        /// jamais moins d'une minute, pour qu'un passage qui finirait juste
        /// avant 3 h ne reparte pas aussitôt.
        /// </summary>
        internal static TimeSpan DelaiJusquaProchainPassage(DateTime maintenantUtc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(maintenantUtc, DateTimeKind.Utc), Paris);

            var prochain = local.Date.AddHours(HeureDePassage);
            if (prochain <= local) prochain = prochain.AddDays(1);

            var prochainUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(prochain, DateTimeKind.Unspecified), Paris);

            var delai = prochainUtc - maintenantUtc;
            return delai < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delai;
        }

        /// <summary>Première passe : relire chaque page connue et noter ce qu'on a vu.</summary>
        private async Task ReleverAsync(bool seulementJamaisRelevees, CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEcheanceReferentielRepository>();

            var maintenant = DateTime.UtcNow;
            var aVeiller = (await repository.GetAVeillerAsync(ct))
                .Where(e => !seulementJamaisRelevees || e.DernierHashPage is null)
                .ToList();

            foreach (var echeance in aVeiller)
            {
                if (string.IsNullOrWhiteSpace(echeance.Url)) continue;

                var releve = await _veille.RelireAsync(echeance.Url, ct);

                if (!releve.Reussi || releve.Hash is null)
                {
                    await repository.EnregistrerEchecReleveAsync(echeance.Id, maintenant, ct);
                    continue;
                }

                // NULL POUR LE PREMIER RELEVÉ, ET C'EST VOULU : il n'y a rien
                // à comparer, et écrire « inchangée » mentirait sur ce qui
                // vient d'être constaté. Voir EcheanceReferentiel.DernierStatutVeille.
                var statut = echeance.DernierHashPage is null
                    ? null
                    : echeance.DernierHashPage == releve.Hash
                        ? EcheanceReferentielRepository.StatutInchangee
                        : EcheanceReferentielRepository.StatutChangee;

                if (statut == EcheanceReferentielRepository.StatutChangee)
                {
                    // UN CHANGEMENT SE CONFIRME AVANT DE S'ÉCRIRE. Mesuré le
                    // 13/09/2026 sur la page-carrefour : dix lectures, une
                    // seule empreinte différente, isolée — un cache qui se
                    // réchauffe, une variante servie une fois. Sans cette
                    // relecture, ce genre d'écart aurait déclenché une alerte
                    // et posé un « mis à jour » collant sur une page qui
                    // n'avait pas bougé.
                    try { await Task.Delay(TimeSpan.FromSeconds(3), ct); }
                    catch (OperationCanceledException) { throw; }

                    var confirmation = await _veille.RelireAsync(echeance.Url, ct);

                    if (!confirmation.Reussi || confirmation.Hash != releve.Hash)
                    {
                        // Pas deux fois la même page : rien de confirmé. On
                        // garde l'empreinte de référence, on date le relevé,
                        // et demain retentera.
                        _logger.LogWarning(
                            "Veille referentiel : {Url} a rendu deux textes differents a trois secondes "
                            + "d'ecart — changement non confirme, ignore.", echeance.Url);

                        await repository.EnregistrerReleveAsync(
                            echeance.Id, echeance.DernierHashPage!,
                            EcheanceReferentielRepository.StatutInchangee, maintenant, ct);
                        continue;
                    }

                    _logger.LogInformation(
                        "Veille referentiel : la page de {Matiere} / {Niveaux} a change.",
                        echeance.MatiereLibelle, echeance.NiveauxConcernes);
                }

                // Enregistré même si RIEN n'a changé : c'est ce qui permet de
                // comparer au relevé SUIVANT, et non plus seulement au tout
                // premier.
                await repository.EnregistrerReleveAsync(echeance.Id, releve.Hash, statut, maintenant, ct);
            }
        }

        /// <summary>Seconde passe : écrire à qui de droit, avec ce que le relevé vient de constater.</summary>
        private async Task SignalerAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEcheanceReferentielRepository>();

            // Résolu ICI, dans le scope — jamais dans le constructeur. Voir
            // la remarque sur le constructeur pour pourquoi.
            var email = scope.ServiceProvider.GetRequiredService<IServiceEmail>();

            var maintenant = DateTime.UtcNow;
            var aSignaler = (await repository.GetASignalerAsync(
                maintenant, FenetreAlerte, EspaceEntreRappels, ct)).ToList();

            if (aSignaler.Count == 0) return;

            // LA MÊME LISTE QUI DONNE LE RÔLE ADMIN, ET NON UNE ADRESSE
            // RECOPIÉE ICI — voir `ComptesProteges`. Un second compte
            // administrateur ajouté plus tard reçoit l'alerte sans qu'il
            // faille toucher à ce fichier.
            var destinataires = _configuration.GetSection("Admin:Emails").Get<string[]>()
                ?? Array.Empty<string>();

            if (destinataires.Length == 0)
            {
                _logger.LogWarning(
                    "{Nombre} echeance(s) de referentiel a signaler, mais aucune adresse dans "
                    + "Admin:Emails.", aSignaler.Count);
                return;
            }

            foreach (var echeance in aSignaler)
            {
                var envoyeAUnDestinataire = false;

                foreach (var destinataire in destinataires)
                {
                    var envoye = await email.EnvoyerAsync(
                        destinataire,
                        SujetPour(echeance),
                        "echeance-referentiel",
                        new Dictionary<string, string>
                        {
                            ["matiere"] = echeance.MatiereLibelle,
                            ["niveaux"] = echeance.NiveauxConcernes,
                            ["message"] = MessagePour(echeance, maintenant),
                            ["detail"] = DetailPour(echeance),
                        },
                        ct);

                    envoyeAUnDestinataire = envoyeAUnDestinataire || envoye;
                }

                if (envoyeAUnDestinataire)
                {
                    await repository.MarquerAlerteEnvoyeeAsync(echeance.Id, maintenant, ct);

                    _logger.LogInformation(
                        "Echeance de referentiel signalee : {Matiere} / {Niveaux} ({Date:yyyy-MM-dd}).",
                        echeance.MatiereLibelle, echeance.NiveauxConcernes, echeance.DateEcheance);
                }
            }
        }

        private static bool ACHange(EcheanceASignaler echeance) =>
            echeance.DernierStatutVeille == EcheanceReferentielRepository.StatutChangee;

        private static string SujetPour(EcheanceASignaler echeance)
        {
            if (ACHange(echeance))
                return $"Texte officiel modifié — {echeance.MatiereLibelle} ({echeance.NiveauxConcernes})";

            return echeance.DateConnue
                ? $"Référentiel à revérifier avant le {echeance.DateEcheance:dd/MM/yyyy} — {echeance.MatiereLibelle}"
                : $"Texte officiel à surveiller — {echeance.MatiereLibelle}";
        }

        private static string MessagePour(EcheanceASignaler echeance, DateTime maintenant)
        {
            if (echeance.Sentinelle)
            {
                return "La page officielle qui recense tous les programmes a changé depuis le dernier "
                    + "relevé. C'est le signal qu'un texte a été publié ou modifié — peut-être pour "
                    + "une matière ou un niveau qu'aucune échéance ne suit encore. À regarder, puis "
                    + "à marquer vue dans l'administration.";
            }

            if (ACHange(echeance))
            {
                return "La page du texte officiel a changé depuis le dernier relevé. Le référentiel "
                    + "doit être confronté à la nouvelle version : c'est peut-être une coquille "
                    + "corrigée, c'est peut-être une notion ajoutée ou retirée.";
            }

            var enRetard = echeance.DateEcheance < maintenant;

            if (!echeance.DateConnue)
            {
                return "Le texte officiel n'était pas encore publié à la dernière vérification. "
                    + "C'est le moment d'aller voir s'il l'est, et si le référentiel doit changer.";
            }

            return enRetard
                ? "Cette échéance est dépassée. Le référentiel doit être revérifié contre le texte "
                    + "officiel dès que possible : des enfants peuvent travailler sur un programme "
                    + "qui n'est plus le leur."
                : "Cette échéance approche. Le référentiel doit être revérifié contre le texte "
                    + "officiel avant cette date, pour que les élèves concernés travaillent sur le "
                    + "vrai programme de leur année dès la rentrée.";
        }

        private static string DetailPour(EcheanceASignaler echeance)
        {
            var ligne = echeance.Sentinelle
                ? "Surveillance permanente, sans échéance."
                : echeance.DateConnue
                    ? $"Bascule prévue le {echeance.DateEcheance:dd MMMM yyyy}."
                    : $"Prochaine vérification prévue le {echeance.DateEcheance:dd MMMM yyyy}.";

            if (!string.IsNullOrWhiteSpace(echeance.TexteOfficiel)) ligne += $"<br>Texte : {echeance.TexteOfficiel}.";

            // CE QUE LE RELEVÉ AUTOMATIQUE A CONSTATÉ, DIT SANS DÉTOUR — et
            // jamais présenté comme une vérification du contenu : le worker
            // n'a fait que comparer du texte, pas le lire.
            if (!string.IsNullOrWhiteSpace(echeance.Url))
            {
                var constat = echeance.DernierStatutVeille switch
                {
                    EcheanceReferentielRepository.StatutChangee =>
                        "⚠ La page a CHANGÉ depuis le dernier relevé — c'est le signal le plus "
                        + "probable qu'il y a quelque chose de neuf à vérifier.",
                    EcheanceReferentielRepository.StatutInchangee =>
                        "Aucun changement constaté sur la page depuis le dernier relevé.",
                    EcheanceReferentielRepository.StatutInjoignable =>
                        "La page n'a pas pu être lue automatiquement (réseau, ou adresse "
                        + "indisponible) — à vérifier à la main.",
                    _ =>
                        "Premier relevé de cette page : rien à comparer encore, la prochaine "
                        + "alerte dira si elle a bougé.",
                };

                ligne += $"<br><a href=\"{echeance.Url}\">{echeance.Url}</a><br>{constat}";
            }

            if (!string.IsNullOrWhiteSpace(echeance.Notes)) ligne += $"<br>{echeance.Notes}";

            return ligne;
        }
    }
}
