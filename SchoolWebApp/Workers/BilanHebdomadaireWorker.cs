using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Envoie les bilans une fois par semaine, en ÉTALANT la série sur autant
    /// de jours qu'il en faut pour tenir dans le quota d'envoi quotidien.
    ///
    /// LE PROBLÈME QU'ON RÉSOUT ICI
    /// ----------------------------
    /// Un service d'envoi borne le nombre de courriels par JOUR. Tant qu'il y a
    /// moins d'enfants que ce plafond, tous les bilans partent le lundi et
    /// personne ne voit la contrainte. Le jour où l'on dépasse, la moitié de la
    /// liste est refusée — et RIEN NE LE DIT au parent qui ne reçoit rien. Le
    /// succès du produit deviendrait la panne du produit.
    ///
    /// La série est donc découpée en groupes, un par jour, à partir du jour
    /// d'envoi. Trois cents bilans par jour de budget et neuf cents enfants :
    /// lundi, mardi, mercredi.
    ///
    /// POURQUOI VERS L'AVANT ET NON VERS L'ARRIÈRE
    /// -------------------------------------------
    /// Étaler sur samedi-dimanche-lundi paraît séduisant — tout le monde
    /// « a reçu son bilan pour lundi ». Mais le bilan couvre la semaine ÉCOULÉE,
    /// close le dimanche soir. Un envoi le samedi ne peut porter que sur la
    /// semaine d'AVANT : deux parents payant le même prix recevraient, le même
    /// week-end, l'un un bilan vieux d'un jour et l'autre un bilan vieux de
    /// huit. Vers l'avant, tout le monde a le même contenu ; seul le jour de
    /// remise se décale d'un ou deux jours.
    ///
    /// Le déclenchement est vérifié toutes les heures plutôt que calculé une
    /// fois au démarrage : un serveur redémarré le dimanche soir raterait
    /// autrement l'envoi du lundi, et personne ne s'en apercevrait avant la
    /// semaine suivante.
    /// </summary>
    public class BilanHebdomadaireWorker : BackgroundService
    {
        private static readonly TimeSpan Intervalle = TimeSpan.FromHours(1);

        /// <summary>Lundi 7 h UTC : le bilan attend le parent au réveil, semaine close.</summary>
        private const DayOfWeek JourEnvoi = DayOfWeek.Monday;
        private const int HeureEnvoi = 7;

        /// <summary>
        /// Budget de bilans par jour, à défaut de configuration.
        ///
        /// DÉLIBÉRÉMENT SOUS LE PLAFOND DU FOURNISSEUR. Le quota quotidien est
        /// PARTAGÉ avec tout le reste du courrier : bienvenue, confirmation
        /// d'adresse, réinitialisation de mot de passe, alerte de quota,
        /// remboursement, réponse du support. Si les bilans consomment les 300
        /// courriels d'un plan gratuit, un parent qui perd son mot de passe ce
        /// lundi-là ne peut plus entrer chez lui — et la cause serait
        /// introuvable. Les cent restants sont cette marge.
        /// </summary>
        private const int BilansParJourParDefaut = 200;

        /// <summary>
        /// Une série ne peut pas déborder sur la suivante.
        ///
        /// Sept groupes de deux cents, c'est mille quatre cents bilans par
        /// semaine : au-delà, aucun étalement ne sauve un plan gratuit et il
        /// faut payer. On préfère alors dépasser le budget quotidien EN LE
        /// DISANT plutôt que de laisser un même sous-ensemble d'enfants ne
        /// jamais rien recevoir, semaine après semaine — ce que produirait un
        /// découpage tronqué, toujours au détriment des mêmes.
        /// </summary>
        private const int GroupesMax = 7;

        /// <summary>
        /// Au-delà de ce nombre de jours d'étalement, on alerte l'exploitant.
        ///
        /// LE SEUIL N'EST PAS UNE LIMITE TECHNIQUE, C'EST UNE DÉCISION
        /// COMMERCIALE. L'étalement absorbe la croissance tout seul jusqu'à
        /// sept jours ; rien ne casse à quatre. Mais chaque jour ajouté éloigne
        /// le dernier groupe de parents de son lundi, et à partir d'un certain
        /// point la bonne réponse n'est plus d'étaler davantage — c'est de payer
        /// une offre supérieure. Trois jours est le point où l'exploitant a
        /// choisi de payer plutôt que d'étirer.
        ///
        /// L'alerte part une seule fois par série, puisque le découpage n'est
        /// calculé qu'au premier jour du cycle.
        /// </summary>
        private const int JoursAvantAlerteParDefaut = 3;

        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BilanHebdomadaireWorker> _logger;

        /// <summary>
        /// L'état de la série en cours, en base.
        ///
        /// EN BASE, ET PLUS EN MÉMOIRE. Le garde-fou était un champ de cette
        /// classe : un redémarrage du serveur un lundi après 7 h le remettait à
        /// null, et les bilans repartaient. Pas seulement le coût — des
        /// COURRIELS EN DOUBLE chez les parents, ce qui ne se rattrape pas.
        ///
        /// Un déploiement le lundi matin est exactement le moment où l'on
        /// redémarre : le défaut se serait déclenché au pire moment possible.
        /// </summary>
        private const string CleCycle = "BILANS_CYCLE";

        /// <summary>
        /// Le nombre de groupes, FIGÉ au premier jour de la série.
        ///
        /// Le recalculer chaque matin serait un défaut sérieux : cinq enfants
        /// inscrits le mardi peuvent faire passer le découpage de trois groupes
        /// à quatre, et l'appartenance dépendant du reste de la division,
        /// TOUT LE MONDE change de groupe en cours de route. Certains
        /// recevraient deux bilans, d'autres aucun.
        /// </summary>
        private const string CleGroupes = "BILANS_GROUPES";

        /// <summary>Le prochain groupe à traiter dans la série en cours.</summary>
        private const string CleGroupeSuivant = "BILANS_GROUPE_SUIVANT";

        /// <summary>Le jour du dernier envoi : un groupe par jour, pas deux.</summary>
        private const string CleDernierJour = "BILANS_DERNIER_JOUR";

        public BilanHebdomadaireWorker(
            IServiceProvider services,
            IConfiguration configuration,
            ILogger<BilanHebdomadaireWorker> logger)
        {
            _services = services;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Bilans:Actif", true))
            {
                _logger.LogInformation("Envoi hebdomadaire des bilans desactive par configuration.");
                return;
            }

            // Laisse l'application finir de démarrer : migrations, seed, et le
            // premier appel au modèle qui suivrait un démarrage à froid.
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UnTourAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Les marques restent posées : on ne veut pas réessayer en
                    // boucle et envoyer deux fois aux parents dont le bilan est
                    // déjà parti avant l'erreur.
                    _logger.LogError(ex, "Echec de l'envoi hebdomadaire des bilans.");
                }

                try
                {
                    await Task.Delay(Intervalle, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task UnTourAsync(CancellationToken ct)
        {
            var maintenant = DateTime.UtcNow;

            // Avant l'heure, il n'y a rien à faire — et surtout rien à demander
            // à la base : ce test tourne vingt-quatre fois par jour.
            if (maintenant.Hour < HeureEnvoi) return;

            var lundi = LundiDeLaSerie(maintenant);

            // Portée dédiée : le worker est un singleton, les dépôts et le
            // DbContext sont à durée de requête.
            using var portee = _services.CreateScope();
            var reglages = portee.ServiceProvider.GetRequiredService<IReglageRepository>();
            var envoi = portee.ServiceProvider.GetRequiredService<IEnvoiBilansService>();
            var telegram = portee.ServiceProvider.GetRequiredService<ITelegramService>();

            var jour = maintenant.ToString("yyyy-MM-dd");

            // UN SEUL GROUPE PAR JOUR. Le worker se réveille toutes les heures :
            // sans cette marque, la série entière partirait le lundi entre 7 h
            // et minuit, ce qui est exactement ce qu'on cherche à éviter.
            if (await reglages.LireAsync(CleDernierJour, ct) == jour) return;

            var cycle = lundi.ToString("yyyy-MM-dd");
            var nombreDeGroupes = await NombreDeGroupesAsync(reglages, envoi, telegram, cycle, ct);

            if (nombreDeGroupes == 0) return;

            var suivant = int.TryParse(await reglages.LireAsync(CleGroupeSuivant, ct), out var g) ? g : 0;

            if (suivant >= nombreDeGroupes)
            {
                // Série terminée. On ne dit rien : ce serait un message par
                // heure jusqu'au lundi suivant.
                return;
            }

            // LES MARQUES SONT POSÉES AVANT L'ENVOI, PAS APRÈS.
            //
            // Si l'envoi échoue au milieu, on ne recommence pas : une partie
            // des parents a déjà reçu son bilan, et rejouer leur en enverrait
            // un second. Entre un bilan manquant et un bilan en double, le
            // premier se rattrape la semaine suivante ; le second, jamais.
            await reglages.EcrireAsync(CleDernierJour, jour, ct);
            await reglages.EcrireAsync(CleGroupeSuivant, (suivant + 1).ToString(), ct);

            // La semaine de référence est celle du LUNDI DE LA SÉRIE, pas celle
            // d'aujourd'hui. Sans cela, le groupe du mercredi porterait sur une
            // autre période que celui du lundi : deux parents recevraient le
            // même jour des bilans qui ne couvrent pas la même semaine.
            var resultat = await envoi.EnvoyerAsync(
                finSemaine: lundi,
                groupeDuJour: nombreDeGroupes > 1 ? suivant : null,
                nombreDeGroupes: nombreDeGroupes > 1 ? nombreDeGroupes : null,
                ct: ct);

            _logger.LogInformation(
                "Bilans : groupe {Groupe}/{Total} envoye, {Envoyes}/{Traites} parti(s).",
                suivant + 1, nombreDeGroupes, resultat.Envoyes, resultat.Traites);
        }

        /// <summary>
        /// Le nombre de groupes de la série en cours : lu s'il est déjà figé,
        /// calculé et figé si la série commence aujourd'hui.
        /// </summary>
        private async Task<int> NombreDeGroupesAsync(
            IReglageRepository reglages,
            IEnvoiBilansService envoi,
            ITelegramService telegram,
            string cycle,
            CancellationToken ct)
        {
            if (await reglages.LireAsync(CleCycle, ct) == cycle
                && int.TryParse(await reglages.LireAsync(CleGroupes, ct), out var connu))
            {
                return connu;
            }

            var total = await envoi.CompterAsync(ct);

            if (total == 0)
            {
                _logger.LogInformation("Bilans : aucun eleve eligible cette semaine.");

                // Le cycle est quand même ouvert, sans quoi on recompterait
                // toutes les heures pendant sept jours.
                await OuvrirLeCycleAsync(reglages, cycle, 0, ct);
                return 0;
            }

            var budget = Math.Max(1, _configuration.GetValue("Bilans:BilansParJour", BilansParJourParDefaut));
            var groupes = (int)Math.Ceiling(total / (double)budget);

            if (groupes > GroupesMax)
            {
                // On plafonne, et on le DIT — fort. Le contraire condamnerait
                // discrètement les mêmes enfants à ne jamais recevoir de bilan.
                var parJour = (int)Math.Ceiling(total / (double)GroupesMax);

                _logger.LogError(
                    "Bilans : {Total} bilans a envoyer, soit {Groupes} jours au budget de {Budget}/jour. "
                    + "Plafonne a {Max} jours, donc {ParJour}/jour : LE QUOTA QUOTIDIEN SERA DEPASSE et "
                    + "une partie des bilans sera refusee. Il faut augmenter Bilans:BilansParJour apres "
                    + "etre passe a une offre superieure chez le service d'envoi.",
                    total, groupes, budget, GroupesMax, parJour);

                groupes = GroupesMax;
            }

            _logger.LogInformation(
                "Bilans : serie du {Cycle} — {Total} bilan(s) repartis sur {Groupes} jour(s) "
                + "(budget {Budget}/jour).",
                cycle, total, groupes, budget);

            var seuil = Math.Max(1, _configuration.GetValue(
                "Bilans:JoursAvantAlerte", JoursAvantAlerteParDefaut));

            if (groupes > seuil)
            {
                _logger.LogWarning(
                    "Bilans : {Groupes} jours d'etalement, au-dela du seuil de {Seuil}. "
                    + "Il est temps de passer a une offre superieure chez le service d'envoi.",
                    groupes, seuil);

                // L'ALERTE NE DOIT PAS POUVOIR EMPÊCHER L'ENVOI.
                //
                // Un jeton Telegram périmé, une panne de leur API, et la série
                // entière des bilans ne partirait pas — pour une notification.
                // On prévient si on peut ; sinon on continue, et le journal
                // garde la trace.
                try
                {
                    await telegram.NotifierEtalementBilansAsync(total, groupes, budget, seuil);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bilans : alerte d'etalement non transmise.");
                }
            }

            await OuvrirLeCycleAsync(reglages, cycle, groupes, ct);
            return groupes;
        }

        private static async Task OuvrirLeCycleAsync(
            IReglageRepository reglages, string cycle, int groupes, CancellationToken ct)
        {
            await reglages.EcrireAsync(CleGroupes, groupes.ToString(), ct);
            await reglages.EcrireAsync(CleGroupeSuivant, "0", ct);

            // LE CYCLE EN DERNIER : c'est lui qui atteste que les deux autres
            // valeurs sont à jour. Écrit en premier, une coupure entre les deux
            // laisserait un cycle neuf avec le compteur de la semaine passée —
            // et la série reprendrait au milieu, privant les premiers groupes
            // de leur bilan.
            await reglages.EcrireAsync(CleCycle, cycle, ct);
        }

        /// <summary>
        /// Le jour d'envoi le plus récent, aujourd'hui compris. C'est l'ancre
        /// de la série ET la fin de la semaine couverte.
        /// </summary>
        private static DateTime LundiDeLaSerie(DateTime maintenant)
        {
            var recul = ((int)maintenant.DayOfWeek - (int)JourEnvoi + 7) % 7;
            return maintenant.Date.AddDays(-recul);
        }
    }
}
