using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Analyse les séances terminées et alimente la mémoire longue de l'élève.
    ///
    /// Volontairement hors du chemin de la conversation. Observer pendant la
    /// séance ajouterait une latence à chaque tour, dans un produit où l'enfant
    /// attend la voix de son professeur. Et une observation faite à chaud sur un
    /// seul échange conclurait sur trop peu.
    /// </summary>
    public class ObservationWorker : BackgroundService
    {
        /// <summary>Fréquence de passage.</summary>
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(10);

        /// <summary>Silence au-delà duquel on considère la séance terminée.</summary>
        private static readonly TimeSpan Inactivite = TimeSpan.FromMinutes(25);

        /// <summary>
        /// Passé ce délai sans un mot de plus, une séance trop courte ne
        /// grandira plus : on cesse de la représenter à chaque passage.
        /// </summary>
        private static readonly TimeSpan AbandonSeanceCourte = TimeSpan.FromHours(24);

        /// <summary>Séances traitées par passage : borne la facture d'un rattrapage.</summary>
        private const int ParPassage = 20;

        /// <summary>
        /// Observations menées de front à la sortie des cours.
        ///
        /// UN SEUL CONSOMMATEUR NE TIENT PAS L HEURE DE POINTE. La file était
        /// lue séance après séance, chacune attendant la fin d un appel au
        /// modèle — quelques secondes. Entre 17 h et 19 h, quand les enfants
        /// finissent tous dans la même fenêtre, ce rythme plafonne à quelques
        /// centaines de séances par heure ; au-delà, la file s allonge sans
        /// jamais se résorber et les fiches cessent de se mettre à jour, sans
        /// que rien ne le signale.
        ///
        /// Chaque observation prend sa propre portée d injection : elles ne
        /// partagent ni contexte de base ni état, et peuvent donc courir
        /// ensemble. Le degré reste borné — c est un appel facturé à chaque
        /// fois, pas une opération gratuite.
        /// </summary>
        private const int ObservationsDeFront = 6;

        /// <summary>
        /// Au-delà, le rattrapage a pris un retard qu il faut voir.
        ///
        /// Le balayage traite au plus ParPassage séances ; s il en trouve
        /// autant à chaque tour, c est qu il en reste derrière. Sans ce
        /// signal, le retard est invisible : la fiche reste plausible,
        /// simplement figée.
        /// </summary>
        private const int RetardSignale = ParPassage;

        private readonly IServiceProvider _services;
        private readonly IFileObservation _file;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ObservationWorker> _logger;

        public ObservationWorker(
            IServiceProvider services,
            IFileObservation file,
            IConfiguration configuration,
            ILogger<ObservationWorker> logger)
        {
            _services = services;
            _file = file;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Observation:Actif", true))
            {
                _logger.LogInformation("Observation des competences desactivee par configuration.");
                return;
            }

            // Deux sources en parallèle. La file traite les séances que l'élève
            // vient de quitter — visible dans la fiche en quelques secondes.
            // Le balayage rattrape celles qu'il a abandonnées en fermant
            // l'onglet, où aucun signal n'est jamais parvenu.
            await Task.WhenAll(
                BoucleFileAsync(stoppingToken),
                BouclePeriodiqueAsync(stoppingToken));
        }

        private async Task BoucleFileAsync(CancellationToken ct)
        {
            // Le portillon borne le nombre d observations simultanees sans
            // jamais bloquer la lecture de la file elle-meme.
            var portillon = new SemaphoreSlim(ObservationsDeFront);
            var enCours = new List<Task>();

            try
            {
                await foreach (var conversationId in _file.LireAsync(ct))
                {
                    await portillon.WaitAsync(ct);

                    // Les taches terminees sont retirees au fil de l eau :
                    // sinon la liste grandit autant que la file, indefiniment.
                    enCours.RemoveAll(t => t.IsCompleted);
                    enCours.Add(ObserverUneAsync(conversationId, portillon, ct));
                }
            }
            catch (OperationCanceledException)
            {
                // Arret de l application : rien a signaler.
            }
            finally
            {
                // ATTENDRE AVANT DE DETRUIRE LE PORTILLON.
                //
                // Sans ce finally, un arret pendant WaitAsync sortait du bloc
                // alors que six observations couraient encore : chacune
                // appelait Release() sur un objet deja detruit, et l exception
                // partait dans une tache que plus personne n observait.
                await Task.WhenAll(enCours);
                portillon.Dispose();
            }
        }

        /// <summary>
        /// Une observation demandee a la sortie d un cours.
        ///
        /// Elle n echoue jamais vers l appelant : une seance illisible ne doit
        /// arreter ni la file ni les cinq autres qui courent a cote.
        /// </summary>
        private async Task ObserverUneAsync(
            int conversationId, SemaphoreSlim portillon, CancellationToken ct)
        {
            try
            {
                using var portee = _services.CreateScope();
                var observateur = portee.ServiceProvider
                    .GetRequiredService<IObservateurCompetencesService>();

                var appliquees = await observateur.ObserverSeanceAsync(conversationId, ct);

                _logger.LogInformation(
                    "Seance {ConversationId} analysee a la sortie du cours : {Total} competence(s).",
                    conversationId, appliquees);
            }
            catch (OperationCanceledException)
            {
                // Arret en cours : cette observation s abandonne, pas la file.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observation immediate impossible pour {ConversationId}.", conversationId);
            }
            finally
            {
                portillon.Release();
            }
        }

        private async Task BouclePeriodiqueAsync(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);

            while (!ct.IsCancellationRequested)
            {
                var vues = 0;

                try
                {
                    vues = await UnPassageAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec du passage d'observation.");
                }

                // LE RATTRAPAGE NE S ENDORT PAS SUR DU RETARD.
                //
                // Le balayage prenait vingt seances puis dormait dix minutes,
                // quoi qu il reste derriere : cent-vingt seances par heure, un
                // plafond fixe qu aucune charge ne faisait bouger. Un millier
                // d enfants qui ferment leur onglet le meme soir mettaient huit
                // heures a etre lus, et la file ne se resorbait jamais aux
                // periodes chargees.
                //
                // Un passage revenu PLEIN veut dire qu il en reste : on
                // enchaine au lieu d attendre. Le debit suit alors la charge
                // reelle, et redescend tout seul des que la file se vide —
                // c est le nombre de seances en attente qui borne la facture,
                // pas une constante ecrite ici.
                if (vues >= RetardSignale)
                {
                    _logger.LogWarning(
                        "Observation : passage plein ({Vues} seances traitees), il en reste en attente. "
                        + "Enchainement immediat.",
                        vues);

                    continue;
                }

                try
                {
                    await Task.Delay(Intervalle, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task<int> UnPassageAsync(CancellationToken ct)
        {
            using var portee = _services.CreateScope();

            var conversations = portee.ServiceProvider.GetRequiredService<IConversationRepository>();
            var observateur = portee.ServiceProvider.GetRequiredService<IObservateurCompetencesService>();

            var seances = (await conversations.GetSeancesAObserverAsync(Inactivite, ParPassage, ct)).ToList();
            if (seances.Count == 0) return 0;

            var total = 0;

            // CE QUI COMPTE, C EST CE QUI A AVANCE — PAS CE QU ON A REGARDE.
            //
            // Une seance trop courte et recente ressort a chaque passage sans
            // que son marqueur bouge : c est voulu, elle peut encore grandir.
            // Mais l enchainement immediat se decide sur ce compteur-ci, et
            // non sur le nombre de lignes lues — sinon vingt seances courtes
            // suffiraient a relancer le balayage sans fin ni pause, a plein
            // regime, jusqu a leur cloture vingt-quatre heures plus tard.
            var avancees = 0;

            foreach (var seance in seances)
            {
                ct.ThrowIfCancellationRequested();

                // Séance trop courte : on la laisse en attente plutôt que de la
                // clore. L'élève peut la reprendre plus tard, et elle deviendra
                // alors assez longue pour être lue.
                //
                // MAIS PAS INDÉFINIMENT, ET C EST LA TOUTE LA DIFFERENCE.
                //
                // Le marqueur n avancait jamais pour ces seances-la, et le
                // balayage les reprend par date croissante, vingt au maximum.
                // Vingt seances courtes accumulees — un enfant qui ouvre un
                // cours et le referme — suffisaient donc a occuper toute la
                // fenetre, a chaque passage, pour toujours : plus AUCUNE seance
                // recente n etait analysee, et la fiche entiere gelait sans que
                // rien ne le signale. C est le pire defaut possible ici : la
                // fiche restait plausible, simplement figee.
                //
                // Passe ce delai, la seance ne grandira plus : on la clot.
                if (!observateur.Exploitable(seance))
                {
                    // TROP COURTE POUR LES COMPÉTENCES, PAS POUR LE VERDICT.
                    //
                    // Relevé par Camara le 13/09/2026 : une séance de deux
                    // minutes conclue par « tu es prêt pour ton contrôle » à
                    // l'oral. Ce `continue` l'écartait ici, avant l'observateur
                    // — donc avant le filet de conclusion, qui a pourtant son
                    // propre seuil et laisse le professeur juger. Le filet
                    // passe d'abord ; ce qui suit ne concerne que les
                    // compétences.
                    try
                    {
                        await observateur.ConclurePreparationAsync(seance, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Conclusion de preparation impossible pour la seance {SeanceId}.",
                            seance.ConversationId);
                    }

                    if (DateTime.UtcNow - seance.Jusqua > AbandonSeanceCourte)
                    {
                        await conversations.MarquerObserveeAsync(
                            seance.ConversationId, seance.Jusqua, ct);

                        _logger.LogInformation(
                            "Seance {SeanceId} trop courte et inactive depuis plus de {Heures} h : "
                            + "close sans observation.",
                            seance.ConversationId, AbandonSeanceCourte.TotalHours);

                        avancees++;
                    }

                    continue;
                }

                try
                {
                    total += await observateur.ObserverAsync(seance, ct);
                }
                catch (Exception ex)
                {
                    // Une séance illisible ne doit pas bloquer les suivantes.
                    _logger.LogError(ex, "Observation impossible pour la seance {SeanceId}.", seance.ConversationId);
                }

                // Le marqueur avance même sans observation retenue : sinon une
                // séance dont on ne peut rien tirer serait réanalysée à chaque
                // passage, indéfiniment et à chaque fois facturée.
                await conversations.MarquerObserveeAsync(seance.ConversationId, seance.Jusqua, ct);
                avancees++;
            }

            _logger.LogInformation(
                "Observation : {Seances} seance(s) analysee(s), {Total} competence(s) mise(s) a jour.",
                seances.Count, total);

            return avancees;
        }
    }
}
