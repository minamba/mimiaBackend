using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Tableau de bord d'administration.
    /// Toutes les routes exigent le rôle Admin, attribué au démarrage du
    /// serveur d'identité aux comptes listés dans `Admin:Emails`.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EstAdmin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------------------------------------------------------
        // Statistiques
        // ------------------------------------------------------------------
        [HttpGet("resume")]
        [SwaggerResponse(200, "Chiffres de tête.", typeof(ResumeAdmin))]
        public async Task<IActionResult> GetResume() => await Executer(() => _adminService.GetResumeAsync());

        /// <param name="granularite">jour | semaine | mois | annee</param>
        /// <param name="eleveId">Null = tous les élèves.</param>
        [HttpGet("stats/requetes")]
        [SwaggerResponse(200, "Requêtes envoyées à l'agent, par période.", typeof(IEnumerable<PointSerie>))]
        public async Task<IActionResult> GetRequetes(
            [FromQuery] string granularite = "jour",
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null,
            [FromQuery] int? eleveId = null)
        {
            var (g, d, f) = Fenetre(granularite, debut, fin);
            return await Executer(() => _adminService.GetSerieRequetesAsync(g, d, f, eleveId));
        }

        /// <summary>
        /// Le parc d'abonnements par période : actifs en fin de période,
        /// résiliations demandées, abonnements arrivés à terme.
        ///
        /// Une seule route pour les trois : ils se lisent ensemble, et trois
        /// appels séparés pourraient se désynchroniser au changement de filtre.
        /// </summary>
        /// <param name="eleveId">
        /// Null = tout le parc. Sinon, l'abonnement de la FAMILLE de cet
        /// élève : un contrat appartient à un parent, pas à un enfant.
        /// </param>
        [HttpGet("stats/abonnements")]
        [SwaggerResponse(200, "État des abonnements, par période.", typeof(IEnumerable<PointAbonnements>))]
        public async Task<IActionResult> GetAbonnements(
            [FromQuery] string granularite = "jour",
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null,
            [FromQuery] int? eleveId = null)
        {
            var (g, d, f) = Fenetre(granularite, debut, fin);
            return await Executer(() => _adminService.GetSerieAbonnementsAsync(g, d, f, eleveId));
        }

        /// <summary>
        /// La fréquentation du site public : combien de personnes distinctes,
        /// et comment elles se répartissent dans la période.
        ///
        /// LES DEUX CHIFFRES VIENNENT ENSEMBLE, ET C'EST NÉCESSAIRE. Le total
        /// n'est PAS la somme de la série : quelqu'un qui vient lundi et jeudi
        /// compte une fois sur la semaine et deux fois dans le détail par
        /// jour. Les calculer séparément, depuis deux appels, laisserait le
        /// bandeau et la courbe se contredire au moindre changement de fenêtre.
        /// </summary>
        [HttpGet("stats/visites")]
        [SwaggerResponse(200, "Visiteurs uniques du site public.", typeof(object))]
        public async Task<IActionResult> GetVisites(
            [FromQuery] string granularite = "jour",
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null)
        {
            var (g, d, f) = Fenetre(granularite, debut, fin);

            return await Executer(async () => new
            {
                points = await _adminService.GetSerieVisitesAsync(g, d, f),
                visiteurs = await _adminService.CompterVisiteursAsync(d, f),
            });
        }

        [HttpGet("stats/parents")]
        [SwaggerResponse(200, "Inscriptions de parents, par période.", typeof(IEnumerable<PointSerie>))]
        public async Task<IActionResult> GetParentsSerie(
            [FromQuery] string granularite = "jour",
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null)
        {
            var (g, d, f) = Fenetre(granularite, debut, fin);
            return await Executer(() => _adminService.GetSerieParentsAsync(g, d, f));
        }

        [HttpGet("stats/eleves")]
        [SwaggerResponse(200, "Créations de profils élèves, par période.", typeof(IEnumerable<PointSerie>))]
        public async Task<IActionResult> GetElevesSerie(
            [FromQuery] string granularite = "jour",
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null)
        {
            var (g, d, f) = Fenetre(granularite, debut, fin);
            return await Executer(() => _adminService.GetSerieElevesAsync(g, d, f));
        }

        // ------------------------------------------------------------------
        // Comptes
        // ------------------------------------------------------------------
        [HttpGet("parents")]
        [SwaggerResponse(200, "Comptes parents.", typeof(IEnumerable<ParentAdmin>))]
        public async Task<IActionResult> GetParents(
            [FromQuery] string? recherche = null,
            [FromQuery] string periode = "mois",
            [FromQuery] int decalage = 0)
        {
            var (debut, fin) = FenetreNommee(periode, decalage);
            return await Executer(() => _adminService.GetParentsAsync(recherche, debut, fin));
        }

        /// <summary>
        /// Ce que le produit a coûté sur une période.
        ///
        /// La période est nommée plutôt que datée — « jour », « semaine »,
        /// « mois », « annee ». C'est une question qu'on se pose en ces termes
        /// (« combien ce mois-ci ? »), et un intervalle de dates obligerait
        /// l'écran à calculer des bornes que le serveur connaît mieux que lui.
        ///
        /// La période est ENTIÈRE et non glissante : « mois » va du 1er au
        /// dernier jour, pas des trente derniers jours. C'est ce qu'on compare
        /// à des abonnements encaissés au mois.
        ///
        /// `decalage` recule ou avance d'autant de périodes : 0 pour la période
        /// courante, -1 pour la précédente. C'est ce que font les chevrons de
        /// l'écran. Un décalage plutôt que deux dates : le serveur sait seul où
        /// commence un mois ou une semaine, et deux écrans qui calculeraient
        /// ces bornes chacun de leur côté finiraient par ne plus s'accorder.
        /// </summary>
        [HttpGet("cout")]
        [SwaggerResponse(200, "Coût de la période.", typeof(CoutPeriode))]
        public async Task<IActionResult> GetCout(
            [FromQuery] string periode = "mois",
            [FromQuery] int decalage = 0)
        {
            var (debut, fin) = FenetreNommee(periode, decalage);
            return await Executer(() => _adminService.GetCoutAsync(debut, fin));
        }

        /// <summary>
        /// La place occupée par la base, et la part des documents.
        ///
        /// SANS CET ÉCRAN, LE CHIFFRE DEMANDE UN CLIENT SQL SUR LA PRODUCTION.
        /// Personne ne fait ça toutes les semaines — or sur Express, une base
        /// pleine n'accepte plus une écriture et l'application s'arrête net.
        /// </summary>
        [HttpGet("base")]
        [SwaggerResponse(200, "Place occupée par la base.", typeof(EtatBase))]
        public async Task<IActionResult> GetEtatBase() =>
            await Executer(() => _adminService.GetEtatBaseAsync());

        /// <summary>
        /// Les bornes d'une période nommée, décalée d'autant de crans.
        ///
        /// PARTAGÉE ENTRE LE RÉSUMÉ ET LE TABLEAU, et c'est tout son intérêt :
        /// le bandeau annonce « 22,87 $ ce mois-ci » et les lignes en dessous
        /// détaillent la même fenêtre. Deux calculs séparés finiraient par
        /// diverger d'un jour, et personne ne verrait lequel a raison.
        /// </summary>
        private static (DateTime Debut, DateTime Fin) FenetreNommee(string? periode, int decalage)
        {
            // Borné : un décalage arbitraire venu de l'URL ferait calculer des
            // fenêtres absurdes, et `AddYears(-100000)` lève une exception.
            decalage = Math.Clamp(decalage, -600, 0);

            var aujourdhui = DateTime.UtcNow.Date;

            return periode?.ToLowerInvariant() switch
            {
                "jour" => (
                    aujourdhui.AddDays(decalage),
                    aujourdhui.AddDays(decalage + 1)),

                // Lundi : la semaine scolaire commence là, et `DayOfWeek`
                // compte le dimanche comme zéro.
                "semaine" => (
                    aujourdhui.AddDays(-(((int)aujourdhui.DayOfWeek + 6) % 7) + 7 * decalage),
                    aujourdhui.AddDays(-(((int)aujourdhui.DayOfWeek + 6) % 7) + 7 * (decalage + 1))),

                "annee" or "année" => (
                    new DateTime(aujourdhui.Year + decalage, 1, 1),
                    new DateTime(aujourdhui.Year + decalage + 1, 1, 1)),

                _ => (
                    new DateTime(aujourdhui.Year, aujourdhui.Month, 1).AddMonths(decalage),
                    new DateTime(aujourdhui.Year, aujourdhui.Month, 1).AddMonths(decalage + 1)),
            };
        }

        [HttpGet("eleves")]
        [SwaggerResponse(200, "Profils élèves.", typeof(IEnumerable<EleveAdmin>))]
        public async Task<IActionResult> GetEleves(
            [FromQuery] int? parentId = null, [FromQuery] string? recherche = null) =>
            await Executer(() => _adminService.GetElevesAsync(parentId, recherche));

        /// <summary>
        /// Fiche détaillée d'un élève : identité, activité par matière,
        /// derniers cours et état des compétences.
        /// </summary>
        [HttpGet("eleves/{id:int}/fiche")]
        [SwaggerResponse(200, "Fiche de l'élève.", typeof(FicheEleve))]
        [SwaggerResponse(404, "Élève inexistant.")]
        public async Task<IActionResult> GetFicheEleve(int id)
        {
            try
            {
                var fiche = await _adminService.GetFicheEleveAsync(id);
                return fiche is null ? NotFound() : Ok(fiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la fiche de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Une tranche de l'historique des évaluations d'un élève.
        ///
        /// Le pendant administrateur de la route parent : la fiche ne porte que
        /// la première page, et l'écran est le même des deux côtés — sans ces
        /// deux routes, « voir plus » n'aurait rien à appeler en admin.
        /// </summary>
        [HttpGet("eleves/{id:int}/evaluations/historique")]
        [SwaggerResponse(200, "Une tranche d'évaluations.", typeof(PageHistorique<EvaluationEleve>))]
        public async Task<IActionResult> GetHistoriqueEvaluations(
            int id,
            [FromQuery] string? curseur,
            [FromQuery] int taille,
            [FromQuery] int? matiereId,
            [FromQuery] bool ancien,
            [FromServices] IEvaluationRepository evaluations) =>
            await Executer(() => evaluations.GetHistoriqueAsync(
                id, curseur, Borner(taille), matiereId, ancien));

        /// <summary>Une tranche de l'historique des comptes rendus d'un élève.</summary>
        [HttpGet("eleves/{id:int}/rapports/historique")]
        [SwaggerResponse(200, "Une tranche de comptes rendus.", typeof(PageHistorique<RapportEleve>))]
        public async Task<IActionResult> GetHistoriqueRapports(
            int id,
            [FromQuery] string? curseur,
            [FromQuery] int taille,
            [FromQuery] int? matiereId,
            [FromQuery] bool ancien,
            [FromServices] IRapportRepository rapports) =>
            await Executer(() => rapports.GetHistoriqueAsync(
                id, curseur, Borner(taille), matiereId, ancien));

        /// <summary>
        /// La taille de tranche demandée, ramenée dans le raisonnable : zéro
        /// (paramètre absent) retombe sur dix, et rien ne dépasse cinquante.
        /// </summary>
        private static int Borner(int taille) =>
            taille <= 0 ? 10 : Math.Min(taille, 50);

        /// <summary>
        /// Déclenche l'envoi des bilans sans attendre lundi.
        ///
        /// Réservé à l'administrateur, et volontairement paramétrable par élève :
        /// tester sur toute la base enverrait un vrai mail à chaque parent.
        /// </summary>
        [HttpPost("bilans/envoyer")]
        [SwaggerResponse(200, "Bilans traités.")]
        public async Task<IActionResult> EnvoyerBilans(
            [FromServices] IEnvoiBilansService envoi,
            [FromQuery] int? eleveId = null,
            [FromQuery] DateTime? finSemaine = null,
            CancellationToken ct = default)
        {
            try
            {
                var resultat = await envoi.EnvoyerAsync(finSemaine, eleveId, ct: ct);
                return Ok(resultat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec du declenchement manuel des bilans.");
                return StatusCode(500, new { message = "L'envoi a échoué, voir les journaux." });
            }
        }

        /// <summary>
        /// Rend le bilan d'un élève en HTML, sans l'envoyer. Permet de relire
        /// la mise en page dans un navigateur sans écrire à un vrai parent.
        /// </summary>
        [HttpGet("bilans/apercu")]
        [Produces("text/html")]
        public async Task<IActionResult> ApercuBilan(
            [FromServices] IEnvoiBilansService envoi,
            [FromQuery] int eleveId,
            [FromQuery] DateTime? finSemaine = null,
            CancellationToken ct = default)
        {
            var html = await envoi.ApercuAsync(eleveId, finSemaine, ct);
            return html is null ? NotFound() : Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// Lance une passe d'observation sans attendre le worker, en abaissant
        /// le seuil d'inactivité. Sert à vérifier la chaîne sur une séance
        /// qu'on vient tout juste de terminer.
        /// </summary>
        [HttpPost("observation/lancer")]
        [SwaggerResponse(200, "Séances analysées.")]
        public async Task<IActionResult> LancerObservation(
            [FromServices] IConversationRepository conversations,
            [FromServices] IObservateurCompetencesService observateur,
            [FromQuery] int inactiviteMinutes = 25,
            [FromQuery] int limite = 20,
            CancellationToken ct = default)
        {
            try
            {
                var seances = (await conversations.GetSeancesAObserverAsync(
                    TimeSpan.FromMinutes(inactiviteMinutes), limite, ct)).ToList();

                var total = 0;
                var detail = new List<object>();

                foreach (var seance in seances)
                {
                    var exploitable = observateur.Exploitable(seance);
                    var appliquees = 0;

                    if (exploitable)
                    {
                        appliquees = await observateur.ObserverAsync(seance, ct);
                        await conversations.MarquerObserveeAsync(seance.ConversationId, seance.Jusqua, ct);
                    }

                    total += appliquees;

                    var duree = seance.Messages.Count > 1
                        ? seance.Messages[^1].Date - seance.Messages[0].Date
                        : TimeSpan.Zero;

                    detail.Add(new
                    {
                        seance.ConversationId,
                        seance.ElevePrenom,
                        seance.NiveauLibelle,
                        Messages = seance.Messages.Count,
                        DureeMinutes = (int)duree.TotalMinutes,
                        Exploitable = exploitable,
                        Observations = appliquees,
                    });
                }

                return Ok(new { Seances = seances.Count, Observations = total, Detail = detail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec du declenchement manuel de l'observation.");
                return StatusCode(500, new { message = "L'observation a échoué, voir les journaux." });
            }
        }

        [HttpPut("parents/{id:int}")]
        [SwaggerResponse(200, "Compte mis à jour.", typeof(ParentAdmin))]
        [SwaggerResponse(404, "Compte inexistant.")]
        public async Task<IActionResult> ModifierParent(int id, [FromBody] ParentAdminRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var parent = await _adminService.ModifierParentAsync(id, model.Prenom, model.Nom, model.Mail);
                return parent is null ? NotFound() : Ok(parent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du parent {ParentId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// L'historique du pot d'heures supplémentaires d'un compte.
        ///
        /// CE QU'ON OUVRE QUAND UN PARENT CONTESTE. Achats payés et
        /// ajustements manuels, chacun daté et motivé. Sans cet écran, le
        /// motif saisi à l'ajustement n'était lisible qu'en SQL — autant dire
        /// jamais, et il ne justifiait donc rien.
        /// </summary>
        [HttpGet("parents/{id:int}/heures")]
        [SwaggerResponse(200, "Historique du pot d'heures.", typeof(IEnumerable<LigneHeures>))]
        public async Task<IActionResult> HistoriqueHeures(
            int id,
            [FromServices] IAbonnementRepository abonnements,
            CancellationToken ct) =>
            await Executer(() => abonnements.HistoriqueHeuresAsync(id, ct));

        /// <summary>
        /// Ajoute ou retire des heures à un compte, à la main.
        ///
        /// POURQUOI CETTE ROUTE EXISTE
        /// ---------------------------
        /// Trois situations qu'aucun automatisme ne couvre : dédommager après
        /// un incident, corriger une erreur de manipulation, et solder un
        /// remboursement PARTIEL — que le webhook laisse volontairement de
        /// côté, parce qu'un montant partiel ne dit pas combien d'heures
        /// retirer.
        ///
        /// Sans elle, la seule issue était une écriture SQL sur la production.
        ///
        /// LE MOTIF EST OBLIGATOIRE. Six mois plus tard, une ligne de
        /// +180 minutes sans explication est un cadeau que personne ne
        /// s'explique — et si un parent conteste, on n'a rien à lui opposer.
        /// </summary>
        [HttpPost("parents/{id:int}/heures")]
        [SwaggerResponse(200, "Heures ajustées.", typeof(EtatQuota))]
        [SwaggerResponse(400, "Minutes à zéro, motif absent, ou retrait supérieur aux heures ajoutées.")]
        [SwaggerResponse(404, "Compte sans abonnement en cours.")]
        public async Task<IActionResult> AjusterHeures(
            int id,
            [FromBody] AjustementHeuresRequest model,
            [FromServices] IAbonnementRepository abonnements,
            [FromServices] IParentRepository parents,
            [FromServices] SchoolWebApp.Domain.Emails.IServiceEmail email,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (model.Minutes == 0)
            {
                return BadRequest(new { message = "Indiquez un nombre de minutes non nul." });
            }

            if (string.IsNullOrWhiteSpace(model.Motif))
            {
                return BadRequest(new { message = "Le motif est obligatoire." });
            }

            try
            {
                var etat = await abonnements.AjusterHeuresAsync(id, model.Minutes, model.Motif, ct);

                if (etat is null)
                {
                    // Deux causes, et le message doit les couvrir sans mentir :
                    // le dépôt rend null pour un compte sans abonnement comme
                    // pour un retrait trop grand.
                    return BadRequest(new
                    {
                        message = "Ajustement refusé : compte sans abonnement en cours, "
                                  + "ou retrait supérieur aux heures supplémentaires disponibles. "
                                  + "Le forfait de la formule n'est jamais entamé.",
                    });
                }

                _logger.LogWarning(
                    "Heures ajustees A LA MAIN : {Minutes} min sur le compte {ParentId}. Motif : {Motif}",
                    model.Minutes, id, model.Motif);

                if (model.PrevenirLeParent)
                {
                    await PrevenirDeLAjustementAsync(id, model, etat, parents, email);
                }

                return Ok(etat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajustement des heures du parent {ParentId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Écrit au parent qu'on a touché à son forfait.
        ///
        /// POURQUOI DANS LES DEUX SENS
        /// ---------------------------
        /// Un ajout que personne ne voit n'est pas un geste : on aurait payé
        /// sans rien acheter. Un retrait que personne n'explique est pire — un
        /// solde qui baisse tout seul ne se lit pas comme une régularisation
        /// mais comme une panne, ou comme quelque chose qu'on a pris.
        ///
        /// LE MOTIF DEVIENT LE CORPS DU MESSAGE. Il est déjà obligatoire et
        /// déjà écrit ; demander un second texte pour le parent ajouterait de
        /// la friction à chaque geste, et ce second champ finirait recopié du
        /// premier.
        ///
        /// L'ÉCHEC EST AVALÉ. L'ajustement est enregistré et rendu à l'écran ;
        /// un SMTP en panne ne doit pas faire croire à l'administrateur que
        /// son geste a échoué, ni l'inciter à le refaire — ce qui doublerait
        /// les heures.
        /// </summary>
        private async Task PrevenirDeLAjustementAsync(
            int parentId,
            AjustementHeuresRequest model,
            EtatQuota etat,
            IParentRepository parents,
            SchoolWebApp.Domain.Emails.IServiceEmail email)
        {
            try
            {
                var parent = await parents.GetParentByIdAsync(parentId);
                if (string.IsNullOrWhiteSpace(parent?.Mail)) return;

                var ajout = model.Minutes > 0;
                var duree = EnHeures(Math.Abs(model.Minutes));

                await email.EnvoyerAsync(
                    parent.Mail!,
                    ajout
                        ? $"{duree} ajoutées à votre forfait"
                        : $"{duree} retirées de votre forfait",
                    "ajustement-heures",
                    new Dictionary<string, string>
                    {
                        ["titre"] = ajout
                            ? $"Nous avons ajouté {duree} à votre forfait"
                            : $"Nous avons retiré {duree} de votre forfait",

                        ["offre"] = etat.OffreLibelle ?? "—",
                        ["date"] = DateTime.UtcNow.ToString("dd/MM/yyyy"),

                        // Le motif tel qu'il a été saisi : c'est lui qui porte
                        // l'explication, et le reformuler ici le rendrait vague.
                        ["explication"] = model.Motif.Trim(),

                        ["mouvement"] = ajout
                            ? $"<strong>+{duree}</strong> ajoutées"
                            : $"<strong>−{duree}</strong> retirées",

                        ["restantes"] = EnHeures(etat.MinutesRestantes),
                        ["renouvellement"] = etat.PeriodeFin.ToString("dd/MM/yyyy"),
                        ["lien"] = "https://mimia.fr/profil",
                    });

                _logger.LogInformation(
                    "Courriel d'ajustement envoye a {Mail} ({Minutes} min).",
                    parent.Mail, model.Minutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Courriel d'ajustement NON ENVOYE au parent {ParentId}.", parentId);
            }
        }

        /// <summary>« 1 h 30 », « 45 min » — la même lecture que les écrans.</summary>
        private static string EnHeures(int minutes)
        {
            var h = minutes / 60;
            var m = minutes % 60;

            if (h == 0) return $"{m} min";
            return m == 0 ? $"{h} h" : $"{h} h {m:00}";
        }

        // ------------------------------------------------------------------
        // Messagerie : la boîte de support, lue et répondue depuis l'écran
        // ------------------------------------------------------------------

        /// <summary>Les derniers messages reçus sur support@mimia.fr.</summary>
        [HttpGet("messagerie")]
        [SwaggerResponse(200, "Messages reçus.", typeof(IEnumerable<SchoolWebApp.Domain.Emails.MessageRecu>))]
        public async Task<IActionResult> Messagerie(
            [FromServices] SchoolWebApp.Domain.Emails.IMessagerieService messagerie,
            CancellationToken ct)
        {
            if (!messagerie.Disponible)
            {
                // 200 et non 503 : une boîte non configurée n'est pas une
                // panne, c'est un état de déploiement. L'écran l'explique au
                // lieu d'afficher une erreur rouge.
                return Ok(new { configuree = false, messages = Array.Empty<object>() });
            }

            var resultat = await messagerie.ListerAsync(ct);

            return Ok(new
            {
                configuree = true,
                messages = resultat.Messages,
                erreur = resultat.Erreur,
            });
        }

        /// <summary>Un message avec son corps. Les images distantes sont bloquées par défaut.</summary>
        [HttpGet("messagerie/{identifiant:long}")]
        [SwaggerResponse(200, "Le message.", typeof(SchoolWebApp.Domain.Emails.MessageDetail))]
        [SwaggerResponse(404, "Message introuvable — supprimé entre-temps.")]
        public async Task<IActionResult> LireMessage(
            long identifiant,
            [FromServices] SchoolWebApp.Domain.Emails.IMessagerieService messagerie,
            CancellationToken ct,
            [FromQuery] bool images = false)
        {
            var message = await messagerie.LireAsync((uint)identifiant, images, ct);
            return message is null ? NotFound() : Ok(message);
        }

        /// <summary>Télécharge une pièce jointe reçue.</summary>
        [HttpGet("messagerie/{identifiant:long}/piece")]
        [SwaggerResponse(200, "Le fichier.")]
        [SwaggerResponse(404, "Pièce introuvable.")]
        public async Task<IActionResult> PieceJointeMessage(
            long identifiant,
            [FromQuery] string nom,
            [FromServices] SchoolWebApp.Domain.Emails.IMessagerieService messagerie,
            CancellationToken ct)
        {
            var piece = await messagerie.PieceJointeAsync((uint)identifiant, nom, ct);
            if (piece is null) return NotFound();

            return File(piece.Value.Donnees, piece.Value.TypeMime, piece.Value.NomFichier);
        }

        /// <summary>Marque un message lu ou non lu.</summary>
        [HttpPost("messagerie/{identifiant:long}/lu")]
        [SwaggerResponse(204, "Marqué.")]
        public async Task<IActionResult> MarquerMessageLu(
            long identifiant,
            [FromQuery] bool lu,
            [FromServices] SchoolWebApp.Domain.Emails.IMessagerieService messagerie,
            CancellationToken ct)
        {
            await messagerie.MarquerLuAsync((uint)identifiant, lu, ct);
            return NoContent();
        }

        /// <summary>
        /// Répond à un message, dans le fil et à la mise en page du site.
        ///
        /// LA RÉPONSE EST FORMATÉE COMME LES AUTRES COURRIELS. Un parent qui a
        /// reçu un mot de bienvenue et des bilans aux couleurs de Mimia
        /// recevrait sinon un texte nu — et douterait de qui lui écrit, ce qui
        /// est précisément le réflexe qu'on veut lui laisser face aux vraies
        /// usurpations.
        /// </summary>
        [HttpPost("messagerie/{identifiant:long}/repondre")]
        [SwaggerResponse(200, "Réponse envoyée.")]
        [SwaggerResponse(404, "Message d'origine introuvable.")]
        public async Task<IActionResult> RepondreMessage(
            long identifiant,
            [FromBody] ReponseMessageRequest model,
            [FromServices] SchoolWebApp.Domain.Emails.IMessagerieService messagerie,
            [FromServices] SchoolWebApp.Domain.Emails.IServiceEmail email,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model?.Texte))
            {
                return BadRequest(new { message = "La réponse est vide." });
            }

            // Le texte saisi devient des paragraphes, échappé comme celui d'une
            // diffusion : le même service, donc la même règle, et aucun HTML de
            // l'administrateur ne part tel quel.
            var diffusion = HttpContext.RequestServices.GetRequiredService<IDiffusionService>();
            var corps = diffusion.ComposerCorps(model.Texte, 0);

            // LA COMPOSITION EST PASSÉE AU SERVICE, PAS FAITE AVANT.
            //
            // Elle a besoin du message d'origine ; le lire ici obligeait à
            // ouvrir une connexion IMAP de plus, et la deuxième réponse
            // consécutive butait sur la limite de l'hébergeur.
            var envoye = await messagerie.RepondreAsync((uint)identifiant, async origine =>
            {
                var sujet = string.IsNullOrWhiteSpace(origine.Sujet)
                    ? "Votre message"
                    : origine.Sujet!;

                return await email.ComposerAsync(sujet, "reponse-support", new Dictionary<string, string>
                {
                    ["titre"] = sujet,

                    // LE PRÉNOM SEUL, PAS LE NOM COMPLET.
                    //
                    // « Bonjour Marie Dupont, » sonne comme un publipostage ;
                    // « Bonjour Marie, » sonne comme quelqu'un qui répond. Et
                    // quand l'expéditeur n'a pas de nom affiché — beaucoup de
                    // clients n'en envoient pas — on se contente de « Bonjour, »
                    // plutôt que d'écrire « Bonjour , ».
                    ["salutation"] = Salutation(origine.DeNom),

                    // Vrai pour tout le monde, y compris pour qui n'a pas de compte.
                    ["mentionPied"] =
                        "Vous recevez ce message en réponse à votre demande adressée au support de Mimia.",

                    ["corpsReponse"] = corps,
                    ["dateOrigine"] = origine.Date.ToString("dd/MM/yyyy"),

                    // Le message d'origine repris en citation. Tronqué : un fil
                    // de dix échanges recopié en entier ferait un courriel
                    // illisible, et le parent a déjà sa propre copie.
                    ["messageOrigine"] = System.Net.WebUtility.HtmlEncode(
                        Tronquer(origine.Texte ?? origine.Extrait ?? string.Empty, 600))
                        .Replace("\n", "<br>"),
                });
            }, ct);

            if (!envoye.Envoye)
            {
                return StatusCode(500, new
                {
                    message = "La réponse n'a pas pu être envoyée.",

                    // LE DÉTAIL TECHNIQUE REMONTE, ET C'EST ASSUMÉ. Seul un
                    // administrateur voit cet écran, et « réessayez dans un
                    // instant » ne dit pas s'il faut réessayer, changer un mot
                    // de passe, ou appeler l'hébergeur.
                    detail = envoye.Detail,
                });
            }

            _logger.LogWarning(
                "Reponse envoyee a {Destinataire}. Serveur : {Accuse}. Copie deposee : {Copie}",
                envoye.Destinataire, envoye.Accuse, envoye.CopieDeposee);

            return Ok(new
            {
                message = "Réponse envoyée.",
                destinataire = envoye.Destinataire,
                accuse = envoye.Accuse,
                copieDeposee = envoye.CopieDeposee,
            });
        }

        /// <summary>« Bonjour Marie, » ou « Bonjour, » — jamais « Bonjour , ».</summary>
        private static string Salutation(string? nomAffiche)
        {
            var prenom = nomAffiche?.Trim().Split(' ').FirstOrDefault();

            // Une adresse électronique en guise de nom — « jean.dupont@… » —
            // arrive quand le client de messagerie n'envoie pas de nom
            // affiché. L'utiliser donnerait « Bonjour jean.dupont@gmail.com, ».
            if (string.IsNullOrWhiteSpace(prenom) || prenom.Contains('@'))
            {
                return "Bonjour,";
            }

            return $"Bonjour {System.Net.WebUtility.HtmlEncode(prenom)},";
        }

        private static string Tronquer(string texte, int taille) =>
            texte.Length <= taille ? texte : texte[..taille] + "…";

        // ------------------------------------------------------------------
        // Diffusion d'un message à tous les parents
        // ------------------------------------------------------------------

        /// <summary>
        /// Cinq mégaoctets par requête, images et documents confondus.
        ///
        /// Ce n'est pas une limite de stockage mais de DÉLIVRABILITÉ : au-delà,
        /// beaucoup de messageries refusent le message ou le classent en
        /// indésirable. Un courriel de dix mégaoctets envoyé à trois cents
        /// parents, c'est aussi trois gigaoctets sortants.
        /// </summary>
        private const int TailleMaxDiffusion = 5 * 1024 * 1024;

        /// <summary>
        /// Rend le message en HTML sans l'envoyer à personne.
        ///
        /// L'APERÇU N'EST PAS UN CONFORT, C'EST LE GARDE-FOU.
        /// Une diffusion part chez tous les clients et ne se rattrape pas. Le
        /// seul moment où l'on peut encore corriger une image mal placée ou une
        /// coquille, c'est avant. Rendu par le MÊME code que l'envoi, sinon
        /// l'aperçu montrerait autre chose que ce qui part.
        /// </summary>
        [HttpPost("diffusion/apercu")]
        [RequestSizeLimit(TailleMaxDiffusion)]
        [SwaggerResponse(200, "Le message rendu en HTML.")]
        public async Task<IActionResult> ApercuDiffusion(
            [FromForm] string sujet,
            [FromForm] string titre,
            [FromForm] string texte,
            [FromServices] IDiffusionService diffusion,
            [FromServices] SchoolWebApp.Domain.Emails.IServiceEmail email,
            [FromForm] List<IFormFile>? images = null,
            [FromForm] List<IFormFile>? documents = null)
        {
            var valeurs = new Dictionary<string, string>
            {
                ["titre"] = sujet ?? string.Empty,
                ["titreMessage"] = titre ?? string.Empty,
                ["corpsMessage"] = diffusion.ComposerCorps(texte ?? string.Empty, images?.Count ?? 0),
                ["blocPiecesJointes"] = diffusion.ComposerPiecesJointes(
                    documents?.Select(d => d.FileName).ToList() ?? []),
            };

            var html = await email.RendreAsync(sujet ?? string.Empty, "diffusion", valeurs);

            // LES IMAGES, EN BASE64 POUR L'APERÇU SEULEMENT.
            //
            // Dans le courriel ce sont des pièces liées (`cid:`), que le
            // navigateur ne sait pas résoudre. Sans cette substitution l'aperçu
            // afficherait des cadres vides là où le parent verra des images —
            // et on croirait à un défaut.
            for (var i = 0; i < (images?.Count ?? 0); i++)
            {
                var image = images![i];
                using var memoire = new MemoryStream();
                await image.CopyToAsync(memoire);

                html = html.Replace(
                    $"cid:diffusion{i + 1}",
                    $"data:{image.ContentType};base64,{Convert.ToBase64String(memoire.ToArray())}");
            }

            return Content(html, "text/html");
        }

        /// <summary>
        /// Lance la diffusion à TOUS les parents.
        ///
        /// Rend immédiatement : l'envoi se fait en arrière-plan et l'écran suit
        /// son avancement. Une requête qui durerait une minute expirerait chez
        /// le navigateur, l'administrateur recliquerait, et tout le monde
        /// recevrait deux fois.
        /// </summary>
        [HttpPost("diffusion")]
        [RequestSizeLimit(TailleMaxDiffusion)]
        [SwaggerResponse(202, "Diffusion lancée.")]
        [SwaggerResponse(409, "Une diffusion est déjà en cours.")]
        public async Task<IActionResult> LancerDiffusion(
            [FromForm] string sujet,
            [FromForm] string titre,
            [FromForm] string texte,
            [FromServices] IDiffusionService diffusion,
            [FromForm] List<IFormFile>? images = null,
            [FromForm] List<IFormFile>? documents = null)
        {
            if (string.IsNullOrWhiteSpace(sujet) || string.IsNullOrWhiteSpace(texte))
            {
                return BadRequest(new { message = "L'objet et le message sont obligatoires." });
            }

            var lues = await LirePiecesAsync(images, "diffusion");
            var jointes = await LirePiecesAsync(documents, null);

            if (!diffusion.Lancer(sujet, titre ?? sujet, texte, lues, jointes))
            {
                return Conflict(new
                {
                    message = "Une diffusion est déjà en cours. Attendez qu'elle se termine.",
                });
            }

            _logger.LogWarning(
                "DIFFUSION LANCEE : « {Sujet} », {Images} image(s), {Docs} document(s).",
                sujet, lues.Count, jointes.Count);

            return Accepted(new { message = "Diffusion lancée." });
        }

        /// <summary>L'avancement, interrogé par l'écran pendant l'envoi.</summary>
        [HttpGet("diffusion/etat")]
        [SwaggerResponse(200, "État de la diffusion.", typeof(EtatDiffusion))]
        public IActionResult LireEtatDiffusion([FromServices] IDiffusionService diffusion) =>
            Ok(diffusion.Etat);

        /// <summary>
        /// Charge les fichiers en mémoire.
        ///
        /// Les octets ne sont NULLE PART persistés : une diffusion vit le temps
        /// d'un envoi. Les garder demanderait une table, une purge, et une
        /// réponse à « pendant combien de temps » — pour des fichiers qu'on ne
        /// renverra jamais.
        /// </summary>
        private static async Task<IReadOnlyList<SchoolWebApp.Domain.Emails.PieceMail>> LirePiecesAsync(
            List<IFormFile>? fichiers, string? prefixeReference)
        {
            if (fichiers is null || fichiers.Count == 0) return [];

            var pieces = new List<SchoolWebApp.Domain.Emails.PieceMail>();

            for (var i = 0; i < fichiers.Count; i++)
            {
                var fichier = fichiers[i];
                if (fichier.Length == 0) continue;

                using var memoire = new MemoryStream();
                await fichier.CopyToAsync(memoire);

                pieces.Add(new SchoolWebApp.Domain.Emails.PieceMail(
                    // La référence suit le rang : `[image:1]` dans le texte
                    // désigne la première image envoyée. C'est le contrat entre
                    // l'écran et le serveur, et il tient à cet ordre.
                    prefixeReference is null ? string.Empty : $"{prefixeReference}{i + 1}",
                    Path.GetFileName(fichier.FileName),
                    string.IsNullOrWhiteSpace(fichier.ContentType)
                        ? "application/octet-stream"
                        : fichier.ContentType,
                    memoire.ToArray()));
            }

            return pieces;
        }

        [HttpPut("eleves/{id:int}")]
        [SwaggerResponse(200, "Profil mis à jour.", typeof(EleveAdmin))]
        [SwaggerResponse(404, "Profil inexistant.")]
        public async Task<IActionResult> ModifierEleve(int id, [FromBody] EleveAdminRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var eleve = await _adminService.ModifierEleveAsync(
                    id, model.Prenom, model.Nom, model.Age, model.NiveauScolaireId, model.Sexe);

                return eleve is null ? NotFound() : Ok(eleve);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("parents/{id:int}")]
        [SwaggerResponse(204, "Compte supprimé, avec ses élèves et conversations.")]
        [SwaggerResponse(404, "Compte inexistant.")]
        public async Task<IActionResult> SupprimerParent(int id)
        {
            try
            {
                var supprime = await _adminService.SupprimerParentAsync(id);
                if (supprime)
                {
                    _logger.LogWarning("Compte parent {ParentId} supprime par un administrateur.", id);
                }
                return supprime ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du parent {ParentId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("eleves/{id:int}")]
        [SwaggerResponse(204, "Profil supprimé.")]
        [SwaggerResponse(404, "Profil inexistant.")]
        public async Task<IActionResult> SupprimerEleve(int id)
        {
            try
            {
                var supprime = await _adminService.SupprimerEleveAsync(id);
                if (supprime)
                {
                    _logger.LogWarning("Profil eleve {EleveId} supprime par un administrateur.", id);
                }
                return supprime ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private async Task<IActionResult> Executer<T>(Func<Task<T>> action)
        {
            try
            {
                return Ok(await action());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans le tableau de bord d'administration.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Normalise la fenêtre demandée. Chaque granularité a une profondeur
        /// par défaut qui donne un graphique lisible : 30 points en jour,
        /// 26 en semaine, 24 en mois, 6 en année.
        /// </summary>
        private static (Granularite, DateTime, DateTime) Fenetre(
            string granularite, DateTime? debut, DateTime? fin)
        {
            var g = granularite?.ToLowerInvariant() switch
            {
                "heure" => Granularite.Heure,
                "semaine" => Granularite.Semaine,
                "mois" => Granularite.Mois,
                "annee" or "année" => Granularite.Annee,
                _ => Granularite.Jour
            };

            var f = (fin ?? DateTime.UtcNow.Date.AddDays(1));

            var d = debut ?? g switch
            {
                Granularite.Heure => f.AddDays(-1),
                Granularite.Jour => f.AddDays(-30),
                Granularite.Semaine => f.AddDays(-7 * 26),
                Granularite.Mois => f.AddMonths(-24),
                _ => f.AddYears(-6)
            };

            // Une fenêtre inversée renverrait une série vide sans rien signaler.
            if (d >= f) d = f.AddDays(-1);

            return (g, d, f);
        }
    }
}
