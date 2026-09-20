using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Api.ViewModels;
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
        private readonly ComptesProteges _comptesProteges;
        private readonly ExemptionFacturation _exemptions;

        public AdminController(
            IAdminService adminService,
            ILogger<AdminController> logger,
            ComptesProteges comptesProteges,
            IOptions<ExemptionFacturation> exemptions)
        {
            _exemptions = exemptions?.Value ?? throw new ArgumentNullException(nameof(exemptions));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _comptesProteges = comptesProteges ?? throw new ArgumentNullException(nameof(comptesProteges));
        }

        /// <summary>
        /// Refuse toute reprise du compte super-administrateur.
        ///
        /// LE MÊME VERROU POUR LES TROIS ROUTES — modifier, supprimer,
        /// changer le rôle — parce que les trois mènent au même endroit.
        /// Supprimer le compte le fait disparaître ; changer son adresse le
        /// fait disparaître aussi, puisque c'est l'adresse qui porte le
        /// rôle. Le site se retrouverait alors sans personne pour rouvrir
        /// les Modes ni pour nommer un administrateur, et on ne ressortirait
        /// de cet état qu'en SQL sur le serveur.
        ///
        /// Rend null quand la route peut continuer.
        /// </summary>
        /// <summary>
        /// Les comptes de l'exploitant sont-ils cachés à cet appelant ?
        ///
        /// LE COMPTE DE DÉMONSTRATION N'EST PAS UN CLIENT. Il apparaissait
        /// dans la liste des parents et dans le fichier clients comme une
        /// famille ordinaire, ce qui gonflait les chiffres et donnait à tout
        /// administrateur la possibilité d'agir dessus.
        ///
        /// LA MÊME LISTE QUE L'EXEMPTION DE FACTURATION, et c'est voulu :
        /// « ne paie jamais » et « n'est pas un client » sont la même idée.
        /// Une seconde liste divergerait au premier compte ajouté.
        ///
        /// Le super-administrateur, lui, voit tout : sans quoi il ne pourrait
        /// plus administrer les comptes qui sont précisément les siens.
        /// </summary>
        private IReadOnlyCollection<string> MailsCaches() =>
            EstSuperAdministrateur()
                ? Array.Empty<string>()
                : _exemptions.ComptesExemptes
                      .Where(m => !string.IsNullOrWhiteSpace(m))
                      .Select(m => m.Trim())
                      .ToArray();

        private bool EstSuperAdministrateur() =>
            User.HasClaim("role", "SuperAdmin") || User.IsInRole("SuperAdmin");

        private async Task<IActionResult?> RefuserSiCompteProtegeAsync(int parentId, string geste)
        {
            var mail = await _adminService.MailDuParentAsync(parentId);
            if (!_comptesProteges.Protege(mail)) return null;

            _logger.LogWarning(
                "Tentative de {Geste} sur le compte super-administrateur {ParentId}, refusee.",
                geste, parentId);

            return StatusCode(403, new
            {
                message = "Le compte super-administrateur ne peut être ni modifié, ni supprimé, "
                        + "ni changé de rôle. Son adresse se règle dans la configuration du serveur."
            });
        }

        // ------------------------------------------------------------------
        // Échéances de référentiel
        //
        // Voir EcheanceReferentielWorker : il alerte tout seul, il ne peut
        // pas constater qu'une vérification a réellement eu lieu — seul un
        // humain le peut, via GetEcheancesReferentiel puis MarquerTraitee.
        // ------------------------------------------------------------------
        [HttpGet("echeances-referentiel")]
        [SwaggerResponse(200, "Les échéances de révision du référentiel.", typeof(IEnumerable<EcheanceReferentielDetail>))]
        public async Task<IActionResult> GetEcheancesReferentiel(
            [FromServices] IEcheanceReferentielRepository echeances, CancellationToken ct) =>
            Ok(await echeances.GetToutesAsync(ct));

        /// <summary>
        /// Le programme scolaire entier, classe par classe, avec le statut de
        /// chaque notion et les échéances officielles rangées sous leur
        /// matière — l'onglet « Programme scolaire » de l'administration.
        /// </summary>
        [HttpGet("programme-scolaire")]
        [SwaggerResponse(200, "Le programme, classe par classe.", typeof(ProgrammeScolaireAdmin))]
        public async Task<IActionResult> GetProgrammeScolaire(
            [FromServices] IProgrammeScolaireRepository programme, CancellationToken ct) =>
            Ok(await programme.GetProgrammeAsync(ct));

        /// <summary>
        /// La vérification des cartes d'examen : notions retenues par matière, et
        /// tout ce qui viderait une carte en silence. Voir `VerificationExamen`.
        /// </summary>
        [HttpGet("examens/verification")]
        [SwaggerResponse(200, "Les examens actifs, carte par carte.", typeof(IEnumerable<VerificationExamen>))]
        public async Task<IActionResult> VerifierExamens(
            [FromServices] IExamenRepository examens, CancellationToken ct) =>
            Ok(await examens.VerifierAsync(ct));

        [HttpPost("echeances-referentiel/{id:int}/traiter")]
        [SwaggerResponse(204, "Marquée traitée : le worker cesse d'alerter dessus.")]
        [SwaggerResponse(404, "Cette échéance n'existe pas.")]
        public async Task<IActionResult> TraiterEcheanceReferentiel(
            int id, [FromServices] IEcheanceReferentielRepository echeances, CancellationToken ct) =>
            await echeances.MarquerTraiteeAsync(id, DateTime.UtcNow, ct) ? NoContent() : NotFound();

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
        /// <summary>
        /// Le tunnel de conversion sur une fenêtre : combien sont venus,
        /// combien ont lancé un essai, combien sont allés plus loin.
        /// </summary>
        [HttpGet("stats/tunnel")]
        [SwaggerResponse(200, "Visiteurs, essais, conversions.", typeof(Tunnel))]
        public async Task<IActionResult> GetTunnel(
            [FromQuery] string granularite = "jour",
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null)
        {
            var (_, d, f) = Fenetre(granularite, debut, fin);
            return await Executer(() => _adminService.GetTunnelAsync(d, f));
        }

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
        /// <summary>
        /// La répartition du fichier clients, à cet instant.
        ///
        /// SANS FENÊTRE, contrairement au reste de l'onglet : « trois
        /// familles en Solo mensuel » est un état, pas un événement daté.
        /// </summary>
        [HttpGet("parents/repartition")]
        [SwaggerResponse(200, "Répartition des comptes.", typeof(RepartitionParents))]
        public async Task<IActionResult> GetRepartitionParents() =>
            await Executer(() => _adminService.GetRepartitionParentsAsync(MailsCaches()));

        [HttpGet("parents")]
        [SwaggerResponse(200, "Comptes parents.", typeof(IEnumerable<ParentAdmin>))]
        public async Task<IActionResult> GetParents(
            [FromQuery] string? recherche = null,
            [FromQuery] string periode = "mois",
            [FromQuery] int decalage = 0)
        {
            var (debut, fin) = FenetreNommee(periode, decalage);

            return await Executer(async () =>
            {
                var parents = await _adminService.GetParentsAsync(recherche, debut, fin);

                // Le filtrage a lieu ICI, pas dans le navigateur : une ligne
                // qu'on masque à l'affichage pendant que l'API l'envoie reste
                // lisible dans les outils de développement, et ses
                // identifiants restent utilisables sur les autres routes.
                var caches = MailsCaches();

                if (caches.Count > 0)
                {
                    parents = parents.Where(p => p.Mail is null
                                                 || !caches.Contains(p.Mail, StringComparer.OrdinalIgnoreCase));
                }

                // LE DRAPEAU EST POSÉ ICI, PAS EN BASE. La configuration est
                // la seule à savoir quelle adresse porte le rôle ; la
                // recopier dans une colonne la rendrait modifiable, et donc
                // fausse dès le premier changement d'adresse.
                foreach (var parent in parents)
                {
                    parent.EstSuperAdministrateur = _comptesProteges.Protege(parent.Mail);
                }

                return parents;
            });
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
        /// Ce que le produit a rapporté sur la période — la même fenêtre que
        /// `cout`, pour que l'écran puisse soustraire l'un de l'autre.
        ///
        /// AU MOIS ET À L'ANNÉE SEULEMENT. Au jour ou à la semaine, la réponse
        /// dit « non disponible » plutôt qu'un chiffre : un abonnement ne se
        /// découpe pas en jours, et un revenu inventé rassurerait à tort.
        /// </summary>
        [HttpGet("revenu")]
        [SwaggerResponse(200, "Revenu de la période.", typeof(RevenuPeriode))]
        public async Task<IActionResult> GetRevenu(
            [FromQuery] string periode = "mois",
            [FromQuery] int decalage = 0)
        {
            var (debut, fin) = FenetreNommee(periode, decalage);
            var calculable = periode?.ToLowerInvariant() is "mois" or "annee" or "année";

            if (!calculable)
            {
                return Ok(new RevenuPeriode { Debut = debut, Fin = fin, Disponible = false });
            }

            return await Executer(() => _adminService.GetRevenuAsync(debut, fin));
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
        /// L'état d'Anthropic et d'OpenAI, tel que la dernière vérification
        /// l'a constaté — sans rappeler personne.
        ///
        /// CE N'EST PAS LE SOLDE. Aucune des deux API ne le donne avec une clé
        /// ordinaire ; on sait seulement si un appel payant passe, et sinon
        /// pourquoi. Le solde se lit sur leurs sites, dont l'écran donne le lien.
        /// </summary>
        /// <summary>
        /// La grille tarifaire des fournisseurs d'IA : le prix officiel relevé,
        /// le prix appliqué dans nos calculs, et le statut qui les compare.
        /// </summary>
        [HttpGet("tarifs")]
        [SwaggerResponse(200, "La grille tarifaire.", typeof(IEnumerable<LigneTarif>))]
        public async Task<IActionResult> GetTarifs() =>
            await Executer(() => _adminService.GetTarifsAsync());

        /// <summary>Le corps de la modification d'un prix officiel.</summary>
        public sealed class RequeteTarif
        {
            public decimal? PrixEntree { get; set; }

            public decimal? PrixSortie { get; set; }

            public decimal? PrixMinute { get; set; }
        }

        /// <summary>
        /// Corrige un prix à la main, en secours de la veille automatique : il est
        /// appliqué aussitôt à nos calculs, et la date de mise à jour change.
        /// </summary>
        [HttpPut("tarifs/{id:int}")]
        [SwaggerResponse(200, "La ligne mise à jour.", typeof(LigneTarif))]
        [SwaggerResponse(400, "Un prix négatif.")]
        [SwaggerResponse(404, "Ligne introuvable.")]
        public async Task<IActionResult> ModifierTarif(int id, [FromBody] RequeteTarif requete)
        {
            if (requete is null) return BadRequest(new { message = "Aucun prix reçu." });

            if (requete.PrixEntree < 0 || requete.PrixSortie < 0 || requete.PrixMinute < 0)
            {
                return BadRequest(new { message = "Un prix ne peut pas être négatif." });
            }

            var ligne = await _adminService.ModifierTarifAsync(
                id, requete.PrixEntree, requete.PrixSortie, requete.PrixMinute);

            return ligne is null ? NotFound() : Ok(ligne);
        }

        [HttpGet("fournisseurs")]
        [SwaggerResponse(200, "L'état des fournisseurs d'IA.", typeof(IEnumerable<SchoolWebApp.Api.Services.Fournisseurs.EtatFournisseur>))]
        public IActionResult GetFournisseurs(
            [FromServices] SchoolWebApp.Api.Services.Fournisseurs.EtatFournisseurs etats) =>
            Ok(etats.Tous());

        /// <summary>
        /// Vérifie tout de suite, sans attendre le worker — le bouton qu'on
        /// presse juste après avoir rechargé un compte.
        /// </summary>
        [HttpPost("fournisseurs/verifier")]
        [SwaggerResponse(200, "L'état des fournisseurs d'IA, à l'instant.", typeof(IEnumerable<SchoolWebApp.Api.Services.Fournisseurs.EtatFournisseur>))]
        public async Task<IActionResult> VerifierFournisseurs(
            [FromServices] SchoolWebApp.Api.Services.Fournisseurs.SurveillanceFournisseurs surveillance,
            CancellationToken ct) =>
            Ok(await surveillance.VerifierAsync(ct));

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
        /// <summary>
        /// Accorde ou retire le droit d'administrer à un compte parent.
        ///
        /// RÉSERVÉE AU SUPER-ADMINISTRATEUR. Un administrateur qui pourrait
        /// promouvoir se donnerait un successeur, puis pourrait être retiré
        /// sans perdre la main : le droit de distribuer les droits est le seul
        /// qui ne se distribue pas.
        /// </summary>
        [HttpPut("parents/{id:int}/administrateur")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Droit mis à jour.")]
        [SwaggerResponse(404, "Parent inexistant.")]
        public async Task<IActionResult> DefinirAdministrateur(
            int id, [FromBody] DefinirAdministrateurRequest requete)
        {
            var refus = await RefuserSiCompteProtegeAsync(id, "changement de rôle");
            if (refus is not null) return refus;

            try
            {
                var fait = await _adminService.DefinirAdministrateurAsync(id, requete?.Actif ?? false);
                return fait ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la mise a jour du droit d administration du parent {ParentId}.", id);

                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <param name="Actif">Vrai pour accorder, faux pour retirer.</param>
        public record DefinirAdministrateurRequest(bool Actif);

        /// <summary>
        /// Les sections du tableau de bord ouvertes à un administrateur, et la
        /// liste complète de celles qui existent — c'est elle qui dessine les
        /// cases à cocher.
        ///
        /// RENDRE LES DEUX DANS LE MÊME APPEL évite que l'écran tienne sa propre
        /// copie de la liste : une section ajoutée au produit apparaît dans la
        /// fenêtre sans qu'on y touche, décochée pour tout le monde.
        /// </summary>
        [HttpGet("parents/{id:int}/onglets")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(200, "Sections ouvertes, et sections possibles.")]
        [SwaggerResponse(404, "Parent inexistant.")]
        public async Task<IActionResult> LireOnglets(int id)
        {
            var accordes = await _adminService.GetOngletsAdminAsync(id);
            if (accordes is null) return NotFound();

            return Ok(new { accordes, toutes = Domain.Models.OngletsAdmin.Toutes });
        }

        /// <summary>
        /// Remplace les sections ouvertes à un administrateur — voulu par Camara
        /// le 17/09/2026.
        ///
        /// RÉSERVÉE AU SUPER-ADMINISTRATEUR, pour la même raison que le droit
        /// d'administrer lui-même : un administrateur qui pourrait s'ouvrir une
        /// section de plus n'aurait aucune limite, et la délégation ne voudrait
        /// plus rien dire.
        /// </summary>
        [HttpPut("parents/{id:int}/onglets")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Sections mises à jour.")]
        [SwaggerResponse(404, "Parent inexistant.")]
        public async Task<IActionResult> DefinirOnglets(
            int id, [FromBody] DefinirOngletsRequest requete)
        {
            var refus = await RefuserSiCompteProtegeAsync(id, "changement de droits");
            if (refus is not null) return refus;

            try
            {
                var fait = await _adminService.DefinirOngletsAdminAsync(id, requete?.Onglets);
                if (!fait) return NotFound();

                // TRACÉ EN AVERTISSEMENT, comme les autres changements de pouvoir :
                // ce sont les lignes qu'on relit quand on se demande qui a eu accès
                // à quoi, et quand.
                _logger.LogWarning(
                    "Droits du parent {ParentId} redefinis : {Onglets}.",
                    id,
                    Domain.Models.OngletsAdmin.Ecrire(requete?.Onglets) is { Length: > 0 } l
                        ? l
                        : "AUCUN");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la mise a jour des droits du parent {ParentId}.", id);

                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <param name="Onglets">Les clés cochées. Vide ou absent = aucune section.</param>
        public record DefinirOngletsRequest(string[]? Onglets);

        /// <summary>
        /// Les sections que la personne connectée a le droit de voir.
        ///
        /// POURQUOI UN APPEL ET NON UN CLAIM DANS LE JETON. Un claim se fige à la
        /// connexion : le super-administrateur cocherait une section, et
        /// l'intéressé ne la verrait qu'après s'être déconnecté et reconnecté —
        /// sans savoir pourquoi, ni que c'est ce qu'il faut faire.
        ///
        /// LE SUPER-ADMINISTRATEUR REÇOIT TOUT. Ses droits ne sont écrits nulle
        /// part : il est le seul à pouvoir se les rendre, donc les lui retirer
        /// n'aurait aucun sens — et fermerait la maison sur une fausse manœuvre.
        /// </summary>
        [HttpGet("mes-onglets")]
        [SwaggerResponse(200, "Les sections ouvertes à la personne connectée.")]
        public async Task<IActionResult> MesOnglets(
            [FromServices] Api.Utils.ICurrentUserAccessor utilisateur)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return Ok(new { onglets = Domain.Models.OngletsAdmin.Toutes });
            }

            var onglets = await _adminService.GetOngletsAdminParMailAsync(utilisateur?.Mail);

            return Ok(new { onglets });
        }

        /// <summary>
        /// Autorise ou interdit à un parent d'enregistrer un enfant de plus —
        /// voulu par Camara le 17/09/2026.
        ///
        /// ADMINISTRATEUR ET NON SUPER-ADMINISTRATEUR, contrairement au droit
        /// d'administration juste au-dessus : celui-ci donne les clés du site,
        /// celui-là règle un compte client. Les deux ne se protègent pas de la
        /// même manière.
        ///
        /// PAS DE GARDE SUR LES COMPTES PROTÉGÉS : retirer à un administrateur
        /// le droit d'ajouter un enfant ne lui ôte aucun pouvoir sur le site, et
        /// il peut se le rendre lui-même. Le verrou n'aurait rien verrouillé.
        /// </summary>
        [HttpPut("parents/{id:int}/ajout-enfant")]
        [SwaggerResponse(204, "Droit mis à jour.")]
        [SwaggerResponse(404, "Parent inexistant.")]
        public async Task<IActionResult> DefinirAjoutEnfant(
            int id, [FromBody] DefinirAjoutEnfantRequest requete)
        {
            try
            {
                var fait = await _adminService.DefinirAjoutEnfantAsync(id, requete?.Autorise ?? true);
                return fait ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la mise a jour du droit d ajout d enfant du parent {ParentId}.", id);

                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <param name="Autorise">Vrai pour autoriser, faux pour interdire.</param>
        public record DefinirAjoutEnfantRequest(bool Autorise);

        /// <summary>
        /// LA FICHE D'UN PARENT CRÉÉ PAR L'ADMINISTRATION — Camara, le
        /// 16/09/2026. Le navigateur enchaîne, comme pour la suppression mais
        /// dans l'autre sens : l'identité d'abord (serveur d'identité), la
        /// fiche ensuite (ici), le rôle enfin si demandé.
        ///
        /// Sans cette route, la fiche n'apparaîtrait qu'à la première
        /// connexion du parent (`GetOrCreateAsync`) : l'administrateur
        /// venait de créer un compte qu'il ne voyait pas dans sa liste.
        /// </summary>
        [HttpPost("parents")]
        [SwaggerResponse(201, "Fiche créée.")]
        [SwaggerResponse(409, "Un parent existe déjà avec cette adresse ou cette identité.")]
        public async Task<IActionResult> CreerParent(
            [FromBody] CreerParentRequest requete, [FromServices] IParentService parents)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var mail = requete.Mail!.Trim();

            try
            {
                if (await parents.GetParentByMailAsync(mail) is not null
                    || await parents.GetParentByIdentityUserIdAsync(requete.IdentityUserId!) is not null)
                {
                    return Conflict(new { message = "Un parent existe déjà avec cette adresse." });
                }

                var cree = await parents.AddParentAsync(new Parent
                {
                    IdentityUserId = requete.IdentityUserId!,
                    Mail = mail,
                    Prenom = requete.Prenom?.Trim(),
                    Nom = requete.Nom?.Trim(),
                    DateCreation = DateTime.UtcNow,
                });

                _logger.LogWarning("Fiche parent {ParentId} creee par un administrateur.", cree.Id);

                return StatusCode(201, new { id = cree.Id, mail = cree.Mail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la creation d'une fiche parent par l'administration.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("eleves/{id:int}/fiche")]
        [SwaggerResponse(200, "Fiche de l'élève.", typeof(FicheEleve))]
        [SwaggerResponse(404, "Élève inexistant.")]
        public async Task<IActionResult> GetFicheEleve(int id, [FromQuery] int? niveau = null)
        {
            try
            {
                var fiche = await _adminService.GetFicheEleveAsync(id, niveau);
                return fiche is null ? NotFound() : Ok(fiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la fiche de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Le calendrier d'un élève, pour l'administration — vacances,
        /// séances par matière, évaluations passées et à venir.
        ///
        /// MÊME CONTENU QUE `ElevesController.GetCalendrier`, MAIS SANS LA
        /// PROPRIÉTÉ À VÉRIFIER. Celle-ci passe par
        /// `IChatContexteResolver.ResoudreEleveAsync`, qui rend null si
        /// l'élève n'appartient pas au parent du jeton — une garde qui n'a
        /// pas de sens ici : l'administration doit justement pouvoir ouvrir
        /// le calendrier de n'importe quel enfant. `IEleveService.GetEleveByIdAsync`
        /// est la version SANS cette vérification, réservée aux lectures déjà
        /// protégées en amont par `EstAdmin` — voir son commentaire.
        /// </summary>
        [HttpGet("eleves/{id:int}/calendrier")]
        [SwaggerResponse(200, "Le calendrier.", typeof(CalendrierEleveViewModel))]
        [SwaggerResponse(404, "Élève inexistant.")]
        public async Task<IActionResult> GetCalendrierEleve(
            int id,
            [FromQuery] int annee,
            [FromQuery] int mois,
            [FromServices] IEleveService eleveService,
            [FromServices] IReferentielService referentiel,
            [FromServices] IRapportRepository rapports,
            [FromServices] IEvaluationRepository evaluations,
            CancellationToken ct)
        {
            if (mois < 1 || mois > 12) return BadRequest();

            try
            {
                var eleve = await eleveService.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var debutMois = new DateTime(annee, mois, 1, 0, 0, 0, DateTimeKind.Utc);
                var finMoisExclusif = debutMois.AddMonths(1);

                var vacances = await referentiel.GetPeriodesVacancesAsync(eleve.Zone, debutMois, finMoisExclusif);
                var prochaineVacances = await referentiel.GetProchainePeriodeVacancesAsync(eleve.Zone, DateTime.UtcNow.Date);

                var seances = await rapports.GetEntreAsync(id, debutMois, finMoisExclusif, ct);
                var evaluationsDuMois = await evaluations.GetEntreAsync(id, debutMois, finMoisExclusif, ct);

                return Ok(new CalendrierEleveViewModel
                {
                    Zone = eleve.Zone,
                    Vacances = vacances.Select(v => new PeriodeVacancesViewModel
                    {
                        Libelle = v.Libelle,
                        DateDebut = v.DateDebut,
                        DateFin = v.DateFin,
                    }),
                    ProchaineVacances = prochaineVacances is null ? null : new PeriodeVacancesViewModel
                    {
                        Libelle = prochaineVacances.Libelle,
                        DateDebut = prochaineVacances.DateDebut,
                        DateFin = prochaineVacances.DateFin,
                    },
                    Seances = seances.Select(s => new SeanceJourViewModel
                    {
                        Date = s.DateCreation,
                        MatiereId = s.MatiereId,
                        MatiereLibelle = s.MatiereLibelle,
                        ProfCouleur = s.ProfCouleur,
                        DureeChoisieMinutes = s.DureeChoisieMinutes,
                    }),
                    Evaluations = evaluationsDuMois.Select(e => new EvaluationJourViewModel
                    {
                        Date = e.DateCreation,
                        MatiereId = e.MatiereId,
                        MatiereLibelle = e.MatiereLibelle,
                        Note = e.Note,
                    }),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du calendrier de l'eleve {EleveId}.", id);
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
        /// Le détail d'un compte rendu de séance.
        /// </summary>
        /// <remarks>
        /// POURQUOI UN DOUBLON DE LA ROUTE PARENT.
        ///
        /// La fiche est le MÊME composant des deux côtés. Ses listes étaient
        /// déjà paramétrées — l'administration y injecte ses propres routes —
        /// mais l'ouverture du détail restait figée sur `/eleves/...`, dont la
        /// première ligne vérifie que l'enfant appartient au parent du jeton.
        ///
        /// Un administrateur consultant une autre famille échouait donc à cette
        /// garde : la liste des séances s'affichait, et le bouton « Voir le
        /// rapport » renvoyait 404. Le symptôme désignait mal sa cause — on
        /// cherchait un rapport manquant, c'était un contrôle d'accès.
        ///
        /// La garde n'est pas relâchée pour autant : elle est simplement la
        /// bonne ici — `[Authorize]` d'administrateur au lieu de l'appartenance
        /// au parent. Le filtre sur l'élève reste dans la requête.
        /// </remarks>
        [HttpGet("eleves/{id:int}/rapports/{rapportId:int}")]
        [SwaggerResponse(200, "Le rapport.", typeof(RapportEleve))]
        [SwaggerResponse(404, "Rapport inexistant, ou n'appartenant pas à cet élève.")]
        /// <remarks>
        /// ÉCRITE À LA MAIN PLUTÔT QU'AVEC `Executer`, qui renvoie toujours 200
        /// — même sur un résultat nul. Un rapport introuvable arriverait alors
        /// au front comme un corps vide : le clic ne ferait rien, sans erreur ni
        /// message. Un 404 franc est plus honnête, et c'est déjà ce que fait la
        /// route parent.
        /// </remarks>
        public async Task<IActionResult> GetRapportEleve(
            int id, int rapportId, [FromServices] IRapportRepository rapports)
        {
            try
            {
                var rapport = await rapports.GetDetailAsync(rapportId, id);
                return rapport is null ? NotFound() : Ok(rapport);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Erreur lors du chargement du rapport {RapportId} de l'eleve {EleveId}.",
                    rapportId, id);

                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// La copie d'une évaluation. Même raison que ci-dessus : le bouton
        /// « Voir la copie » souffrait exactement du même défaut.
        /// </summary>
        [HttpGet("eleves/{id:int}/evaluations/{evaluationId:int}/copie")]
        [SwaggerResponse(200, "La copie.", typeof(EvaluationEleve))]
        [SwaggerResponse(404, "Évaluation inexistante, ou n'appartenant pas à cet élève.")]
        public async Task<IActionResult> GetCopieEleve(
            int id, int evaluationId, [FromServices] IEvaluationRepository evaluations)
        {
            try
            {
                var copie = await evaluations.GetCopieAsync(evaluationId, id);
                return copie is null ? NotFound() : Ok(copie);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Erreur lors du chargement de la copie {EvaluationId} de l'eleve {EleveId}.",
                    evaluationId, id);

                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Une dictée corrigée. Même raison que les deux routes ci-dessus : le
        /// bouton « Voir la dictée » de la fiche souffrirait exactement du
        /// même défaut sans elle — un administrateur qui consulte une autre
        /// famille se heurterait à la garde d'appartenance de la route
        /// parent.
        /// </summary>
        [HttpGet("eleves/{id:int}/dictees/{dicteeId:int}")]
        [SwaggerResponse(200, "La dictée.", typeof(DicteeEleve))]
        [SwaggerResponse(404, "Dictée inexistante, ou n'appartenant pas à cet élève.")]
        public async Task<IActionResult> GetDicteeEleve(
            int id, int dicteeId, [FromServices] IDicteeRepository dictees)
        {
            try
            {
                var dictee = await dictees.GetDetailAsync(dicteeId, id);
                return dictee is null ? NotFound() : Ok(dictee);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Erreur lors du chargement de la dictee {DicteeId} de l'eleve {EleveId}.",
                    dicteeId, id);

                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

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

            var refus = await RefuserSiCompteProtegeAsync(id, "modification");
            if (refus is not null) return refus;

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
            [FromServices] SchoolWebApp.Domain.Repositories.IBannissementRepository bannis,
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

            // La réponse part par la boîte du support, pas par le service d'envoi
            // filtré : le bannissement se vérifie donc ici, dès que l'adresse de
            // l'expéditeur est connue — dans la composition.
            var expediteurBanni = false;

            // LA COMPOSITION EST PASSÉE AU SERVICE, PAS FAITE AVANT.
            //
            // Elle a besoin du message d'origine ; le lire ici obligeait à
            // ouvrir une connexion IMAP de plus, et la deuxième réponse
            // consécutive butait sur la limite de l'hébergeur.
            var envoye = await messagerie.RepondreAsync((uint)identifiant, async origine =>
            {
                // UNE ADRESSE BANNIE NE REÇOIT PLUS RIEN, réponse du support
                // comprise. Lever ici interrompt l'envoi avant qu'il parte.
                if (await bannis.EstBanniAsync(origine.DeAdresse, ct))
                {
                    expediteurBanni = true;
                    throw new InvalidOperationException("Adresse bannie : reponse non envoyee.");
                }

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

            if (expediteurBanni)
            {
                return BadRequest(new { message = "Cette adresse est bannie : elle ne reçoit plus aucun courriel." });
            }

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
                ["mentionPied"] = diffusion.ComposerMentionPied(),
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
        /// Écrit à UN parent, dans la même mise en page que la diffusion.
        ///
        /// MÊME COMPOSITION, UN SEUL DESTINATAIRE.
        /// `IDiffusionService.ComposerCorps` et le gabarit « diffusion » sont
        /// réutilisés tels quels : ce parent voit la même mise en page qu'une
        /// diffusion générale, avec ses propres images et pièces jointes.
        /// `DiffuserAsync` avec une liste d'un seul destinataire fait
        /// exactement ce qu'il faut — pas de second chemin d'envoi à
        /// entretenir.
        ///
        /// SYNCHRONE, CONTRAIREMENT À LA DIFFUSION. Un seul message se compose
        /// et s'envoie en une poignée de secondes, largement sous le délai qui
        /// ferait expirer la requête. Le suivi d'avancement de la diffusion en
        /// masse n'a pas de sens pour un message à un seul destinataire.
        /// </summary>
        [HttpPost("mails/parent")]
        [RequestSizeLimit(TailleMaxDiffusion)]
        [SwaggerResponse(200, "Message envoyé.")]
        [SwaggerResponse(400, "Adresse, objet ou message manquant ou invalide.")]
        [SwaggerResponse(502, "L'envoi a échoué.")]
        public async Task<IActionResult> EnvoyerMailParent(
            [FromForm] string destinataire,
            [FromForm] string sujet,
            [FromForm] string titre,
            [FromForm] string texte,
            [FromServices] IDiffusionService diffusion,
            [FromServices] SchoolWebApp.Domain.Emails.IServiceEmail email,
            [FromServices] SchoolWebApp.Domain.Repositories.IBannissementRepository bannis,
            [FromForm] List<IFormFile>? images = null,
            [FromForm] List<IFormFile>? documents = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sujet) || string.IsNullOrWhiteSpace(texte))
            {
                return BadRequest(new { message = "L'objet et le message sont obligatoires." });
            }

            var adresse = destinataire?.Trim() ?? string.Empty;

            try
            {
                _ = new System.Net.Mail.MailAddress(adresse);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "L'adresse du parent n'est pas valide." });
            }

            // Une adresse bannie ne reçoit plus rien : on le dit, au lieu de
            // laisser le service d'envoi l'écarter et répondre « échec ».
            if (await bannis.EstBanniAsync(adresse, ct))
            {
                return BadRequest(new { message = "Cette adresse est bannie : elle ne reçoit plus aucun courriel." });
            }

            var lues = await LirePiecesAsync(images, "diffusion");
            var jointes = await LirePiecesAsync(documents, null);

            var valeurs = new Dictionary<string, string>
            {
                ["titre"] = sujet,
                ["titreMessage"] = titre ?? sujet,
                ["corpsMessage"] = diffusion.ComposerCorps(texte, lues.Count),
                ["blocPiecesJointes"] =
                    diffusion.ComposerPiecesJointes(jointes.Select(d => d.NomFichier).ToList()),
                ["mentionPied"] = diffusion.ComposerMentionPied(),
            };

            var resultat = await email.DiffuserAsync(
                new[] { adresse }, sujet, "diffusion", valeurs, lues, jointes, ct: ct);

            if (resultat.Envoyes == 0)
            {
                return StatusCode(502, new { message = "Le message n'a pas pu être envoyé." });
            }

            _logger.LogInformation(
                "MAIL PARENT ENVOYE a {Destinataire} : « {Sujet} », {Images} image(s), {Docs} document(s).",
                adresse, sujet, lues.Count, jointes.Count);

            return Ok(new { message = "Message envoyé." });
        }

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
                    id, model.Prenom, model.Nom, model.Age, model.NiveauScolaireId, model.Sexe,
                    model.AcademieId, model.Lv2Espagnol, model.Specialites);

                return eleve is null ? NotFound() : Ok(eleve);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Toutes les périodes de vacances, toutes zones et années — pour
        /// l'onglet « Périodes scolaires ». `CalendrierScolaireSyncWorker`
        /// les tient à jour tout seul chaque jour ; cet écran sert à
        /// corriger une date à la main quand la source officielle se trompe
        /// (voir `CalendrierScolaireApiService`), ou à ajouter une zone
        /// qu'elle ne couvre pas encore.
        /// </summary>
        [HttpGet("periodes-vacances")]
        [SwaggerResponse(200, "Toutes les periodes.", typeof(IEnumerable<Domain.Models.PeriodeVacances>))]
        public async Task<IActionResult> GetPeriodesVacances(
            [FromServices] IReferentielService referentiel) =>
            Ok(await referentiel.GetToutesLesPeriodesVacancesAsync());

        [HttpPost("periodes-vacances")]
        [SwaggerResponse(200, "La periode creee.", typeof(Domain.Models.PeriodeVacances))]
        [SwaggerResponse(400, "Requete invalide.")]
        public async Task<IActionResult> CreerPeriodeVacances(
            [FromBody] PeriodeVacancesRequest model, [FromServices] IReferentielService referentiel)
        {
            var faute = ValiderPeriodeVacances(model);
            if (faute is not null) return BadRequest(new { message = faute });

            try
            {
                var periode = await referentiel.AjouterPeriodeVacancesAsync(
                    model.Zone!.Trim(), model.AnneeScolaire!.Trim(), model.Libelle!.Trim(),
                    model.DateDebut!.Value, model.DateFin!.Value);

                return Ok(periode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la creation d'une periode de vacances.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpPut("periodes-vacances/{id:int}")]
        [SwaggerResponse(200, "La periode modifiee.", typeof(Domain.Models.PeriodeVacances))]
        [SwaggerResponse(400, "Requete invalide.")]
        [SwaggerResponse(404, "Periode inexistante.")]
        public async Task<IActionResult> ModifierPeriodeVacances(
            int id, [FromBody] PeriodeVacancesRequest model, [FromServices] IReferentielService referentiel)
        {
            var faute = ValiderPeriodeVacances(model);
            if (faute is not null) return BadRequest(new { message = faute });

            try
            {
                var periode = await referentiel.ModifierPeriodeVacancesAsync(
                    id, model.Zone!.Trim(), model.AnneeScolaire!.Trim(), model.Libelle!.Trim(),
                    model.DateDebut!.Value, model.DateFin!.Value);

                return periode is null ? NotFound() : Ok(periode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de la periode de vacances {Id}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("periodes-vacances/{id:int}")]
        [SwaggerResponse(204, "Periode supprimee.")]
        [SwaggerResponse(404, "Periode inexistante.")]
        public async Task<IActionResult> SupprimerPeriodeVacances(
            int id, [FromServices] IReferentielService referentiel) =>
            await referentiel.SupprimerPeriodeVacancesAsync(id) ? NoContent() : NotFound();

        /// <summary>
        /// Une fin avant un début n'est pas rattrapable par la base : c'est
        /// exactement la coquille relevée sur la source officielle
        /// (Guadeloupe, Noël 2026-2027) que ce garde-fou empêche de ressaisir
        /// à la main par erreur.
        /// </summary>
        private static string? ValiderPeriodeVacances(PeriodeVacancesRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Zone)) return "La zone est obligatoire.";
            if (string.IsNullOrWhiteSpace(model.AnneeScolaire)) return "L'annee scolaire est obligatoire.";
            if (string.IsNullOrWhiteSpace(model.Libelle)) return "Le libelle est obligatoire.";
            if (model.DateDebut is null || model.DateFin is null) return "Les deux dates sont obligatoires.";
            if (model.DateFin < model.DateDebut) return "La date de fin ne peut pas précéder la date de début.";

            return null;
        }

        // -----------------------------------------------------------------
        // Événements en direct — pour ne plus sonder le serveur en boucle
        // -----------------------------------------------------------------

        /// <summary>
        /// Un flux tenu ouvert : chaque « signalement » ou « visite » publié
        /// via <see cref="IEvenementsAdminHub"/> arrive ici à la seconde,
        /// tant que l'onglet d'administration reste ouvert.
        /// </summary>
        /// <summary>
        /// Le silence maximal toléré sur le flux avant d'envoyer un signe de vie.
        ///
        /// Un onglet d'administration peut rester ouvert des heures sans qu'il
        /// se passe quoi que ce soit — c'est même le cas normal. Or une
        /// connexion muette est coupée par les intermédiaires bien avant :
        /// nginx abandonne au bout d'une minute d'inactivité. Sans ce
        /// battement, le flux tomberait donc en boucle toutes les minutes en
        /// production, et l'écran ne devrait ses mises à jour qu'à la
        /// reconnexion suivante.
        /// </summary>
        private static readonly TimeSpan IntervalleBattement = TimeSpan.FromSeconds(25);

        [HttpGet("evenements")]
        [SwaggerResponse(200, "Flux SSE des événements d'administration.")]
        public async Task Evenements(
            [FromServices] IEvenementsAdminHub hub, CancellationToken ct)
        {
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            HttpContext.Features
                .Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()
                ?.DisableBuffering();

            async Task EcrireAsync(string charge)
            {
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(charge), ct);
                await Response.Body.FlushAsync(ct);
            }

            try
            {
                // LES EN-TÊTES PARTENT À LA CONNEXION, PAS AU PREMIER ÉVÉNEMENT.
                //
                // Les poser ne les envoie pas : ASP.NET attend la première
                // écriture pour ça. Et cette première écriture peut n'arriver
                // que dans trois heures, puisqu'elle dépend d'un signalement
                // qu'un parent n'a peut-être pas encore fait. D'ici là le
                // navigateur restait suspendu sur une requête sans réponse,
                // incapable de distinguer « connecté et en attente » de
                // « ignoré » — et le navigateur n'avait donc rien à refermer
                // proprement en quittant l'écran. Une ligne de commentaire SSE
                // (« : … »), que le navigateur lit et jette, suffit à trancher.
                await EcrireAsync(": connecté\n\n");

                await using var evenements = hub.SAbonnerAsync(ct).GetAsyncEnumerator(ct);

                // L'événement suivant n'est demandé QU'UNE FOIS, et attendu
                // d'un tour à l'autre : le redemander à chaque battement
                // ouvrirait une deuxième lecture sur la même file.
                var suivant = evenements.MoveNextAsync().AsTask();

                while (true)
                {
                    // Sans jeton d'annulation sur l'attente : un battement
                    // annulé laisserait derrière lui une tâche en échec que
                    // personne ne regarde. Il expire seul, la sortie se joue
                    // sur `suivant`.
                    var battement = Task.Delay(IntervalleBattement);

                    if (await Task.WhenAny(suivant, battement) == battement)
                    {
                        await EcrireAsync(": battement\n\n");
                        continue;
                    }

                    if (!await suivant) break;

                    var json = JsonSerializer.Serialize(new { type = evenements.Current });
                    await EcrireAsync($"data: {json}\n\n");

                    suivant = evenements.MoveNextAsync().AsTask();
                }
            }
            catch (OperationCanceledException)
            {
                // L'administrateur a fermé l'onglet — rien à signaler.
            }
        }

        // -----------------------------------------------------------------
        // Signalements — bouton « Signaler »
        // -----------------------------------------------------------------

        private static readonly HashSet<string> SignalementCategoriesConnues =
            new(StringComparer.OrdinalIgnoreCase) { "PROFESSEUR", "TECHNIQUE", "SUGGESTION", "AUTRE" };

        private static readonly HashSet<string> SignalementEtatsConnus =
            new(StringComparer.OrdinalIgnoreCase) { "nouveau", "en_cours", "traite" };

        [HttpGet("signalements")]
        [SwaggerResponse(200, "Tous les signalements.", typeof(IEnumerable<Signalement>))]
        public async Task<IActionResult> GetSignalements(
            [FromServices] ISignalementService signalements, CancellationToken ct) =>
            Ok(await signalements.GetTousAsync(ct));

        [HttpPost("signalements")]
        [SwaggerResponse(200, "Le signalement cree.", typeof(Signalement))]
        [SwaggerResponse(400, "Requete invalide.")]
        public async Task<IActionResult> CreerSignalement(
            [FromBody] SignalementAdminRequest model,
            [FromServices] ISignalementService signalements,
            [FromServices] IParentService parentService,
            CancellationToken ct)
        {
            var faute = ValiderSignalement(model, exigerParentMail: true);
            if (faute is not null) return BadRequest(new { message = faute });

            var parent = await parentService.GetParentByMailAsync(model.ParentMail!.Trim());
            if (parent is null)
            {
                return BadRequest(new { message = "Aucun parent ne correspond à cette adresse." });
            }

            try
            {
                var signalement = await signalements.CreerAsync(
                    parent.Id, null, model.Categorie!.Trim().ToUpperInvariant(),
                    model.Description!.Trim(), ct);

                // L'état par défaut est "nouveau" : une saisie initiale dans
                // un autre état n'est pas un CHANGEMENT vécu par le parent,
                // donc pas d'e-mail ici — contrairement à la modification
                // ci-dessous.
                var etatDemande = model.Etat?.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(etatDemande) && etatDemande != "nouveau")
                {
                    signalement = await signalements.ModifierAsync(
                        signalement.Id, signalement.Categorie!, signalement.Description!,
                        etatDemande, ct) ?? signalement;
                }

                return StatusCode(201, signalement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la creation manuelle d'un signalement.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpPut("signalements/{id:int}")]
        [SwaggerResponse(200, "Le signalement modifie.", typeof(Signalement))]
        [SwaggerResponse(400, "Requete invalide.")]
        [SwaggerResponse(404, "Signalement inexistant.")]
        public async Task<IActionResult> ModifierSignalement(
            int id, [FromBody] SignalementAdminRequest model,
            [FromServices] ISignalementService signalements,
            [FromServices] Domain.Emails.IServiceEmail email,
            CancellationToken ct)
        {
            var faute = ValiderSignalement(model, exigerParentMail: false);
            if (faute is not null) return BadRequest(new { message = faute });

            var avant = await signalements.GetByIdAsync(id, ct);
            if (avant is null) return NotFound();

            var categorie = model.Categorie!.Trim().ToUpperInvariant();
            var etat = model.Etat!.Trim().ToLowerInvariant();

            try
            {
                var apres = await signalements.ModifierAsync(
                    id, categorie, model.Description!.Trim(), etat, ct);

                if (apres is null) return NotFound();

                // UN MAIL SEULEMENT SI L'ÉTAT A VRAIMENT CHANGÉ.
                //
                // Corriger une faute de frappe dans la description ne doit
                // pas relancer un message au parent — seule une vraie
                // progression du traitement le justifie.
                if (etat != avant.Etat && !string.IsNullOrWhiteSpace(apres.ParentMail))
                {
                    await EnvoyerMailEtatSignalementAsync(email, apres, etat, ct);
                }

                return Ok(apres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du signalement {Id}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("signalements/{id:int}")]
        [SwaggerResponse(204, "Signalement supprime.")]
        [SwaggerResponse(404, "Signalement inexistant.")]
        public async Task<IActionResult> SupprimerSignalement(
            int id, [FromServices] ISignalementService signalements, CancellationToken ct) =>
            await signalements.SupprimerAsync(id, ct) ? NoContent() : NotFound();

        private static readonly Dictionary<string, string> LibellesCategorieSignalement =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["PROFESSEUR"] = "Problème avec un professeur",
                ["TECHNIQUE"] = "Problème technique",
                ["SUGGESTION"] = "Suggestion",
                ["AUTRE"] = "Autre",
            };

        /// <summary>
        /// Le mail « en cours » ou « traité », selon le nouvel état. Le
        /// second varie en plus selon la catégorie : un problème technique ou
        /// un souci avec un professeur mérite qu'on demande au parent de
        /// retester, une suggestion ou un « autre » non.
        /// </summary>
        private static async Task EnvoyerMailEtatSignalementAsync(
            Domain.Emails.IServiceEmail email, Signalement signalement, string etat, CancellationToken ct)
        {
            var theme = LibellesCategorieSignalement.GetValueOrDefault(
                signalement.Categorie ?? "", signalement.Categorie ?? "");

            var valeurs = new Dictionary<string, string>
            {
                ["theme"] = theme,
                ["description"] = signalement.Description ?? "",
            };

            if (etat == "en_cours")
            {
                await email.EnvoyerAsync(
                    signalement.ParentMail!, "Votre signalement est en cours de traitement",
                    "signalement-en-cours", valeurs, ct);
            }
            else if (etat == "traite")
            {
                var relance = string.Equals(signalement.Categorie, "PROFESSEUR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(signalement.Categorie, "TECHNIQUE", StringComparison.OrdinalIgnoreCase);

                valeurs["consigneSuivi"] = relance
                    ? """
                      <p style="margin:0 0 20px 0; font-size:15px; line-height:1.7; color:#16233a;">
                        N'hésitez pas à retester dès maintenant. Si le problème persiste,
                        répondez directement à ce message pour nous le signaler à nouveau.
                      </p>
                      """
                    : "";

                await email.EnvoyerAsync(
                    signalement.ParentMail!, "Votre signalement a été traité",
                    "signalement-traite", valeurs, ct);
            }
        }

        private static string? ValiderSignalement(SignalementAdminRequest model, bool exigerParentMail)
        {
            if (exigerParentMail && string.IsNullOrWhiteSpace(model.ParentMail))
            {
                return "L'adresse du parent est obligatoire.";
            }

            if (string.IsNullOrWhiteSpace(model.Categorie)
                || !SignalementCategoriesConnues.Contains(model.Categorie.Trim()))
            {
                return "La catégorie est obligatoire et doit être connue.";
            }

            if (string.IsNullOrWhiteSpace(model.Description)) return "La description est obligatoire.";

            if (string.IsNullOrWhiteSpace(model.Etat)
                || !SignalementEtatsConnus.Contains(model.Etat.Trim()))
            {
                return "L'état est obligatoire et doit être connu.";
            }

            return null;
        }

        /// <summary>
        /// Efface un compte parent, ses élèves et ses conversations.
        ///
        /// LES PRÉLÈVEMENTS SONT COUPÉS AVANT, ET C'EST TOUT LE POINT.
        ///
        /// Cette route ne retirait que la ligne en base. L'abonnement, lui,
        /// continuait de courir chez Stripe — et une fois le parent effacé,
        /// plus rien dans l'application ne permettait de le retrouver : ni
        /// l'écran des abonnements, qui lit la base, ni le tableau de bord.
        /// Il ne restait que le relevé bancaire du parent pour le signaler.
        ///
        /// La suppression demandée par le titulaire lui-même coupait déjà
        /// (`ProfilController`). Le même compte supprimé depuis
        /// l'administration ne coupait pas : deux chemins vers le même
        /// effacement, un seul des deux sûr. C'est le genre d'écart qu'on ne
        /// rattrape pas par la vigilance.
        ///
        /// L'ORDRE EST CELUI DE `ProfilController`, DÉLIBÉRÉMENT. Couper
        /// d'abord : si Stripe répond mal, la ligne existe encore et le
        /// compte reste réparable. Effacer d'abord laisserait un abonnement
        /// orphelin que plus rien ne rattache à personne.
        /// </summary>
        [HttpDelete("parents/{id:int}")]
        [SwaggerResponse(204, "Compte supprimé, avec ses élèves et conversations.")]
        [SwaggerResponse(404, "Compte inexistant.")]
        public async Task<IActionResult> SupprimerParent(
            int id,
            [FromServices] ICaisseStripeService caisse,
            CancellationToken ct)
        {
            var refus = await RefuserSiCompteProtegeAsync(id, "suppression");
            if (refus is not null) return refus;

            try
            {
                await caisse.CouperLesPrelevementsAsync(id, ct);

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

        // ------------------------------------------------------------------
        // Bannissement
        //
        // LA LISTE PORTE DES ADRESSES, PAS DES COMPTES, et c'est tout
        // l'intérêt. Quelqu'un qu'on met dehors supprime souvent son compte
        // dans la foulée, puis se réinscrit le lendemain avec la même
        // adresse. Un drapeau posé sur `Parent` disparaîtrait avec lui —
        // précisément au moment où il devient utile.
        //
        // LE REFUS TOMBE AILLEURS. Ces routes tiennent la liste ; c'est le
        // SERVEUR D'IDENTITÉ qui refuse la connexion et l'inscription, en la
        // lisant directement par sa connexion à la base métier. La
        // séparation est celle des responsabilités : l'API administre, le
        // serveur d'identité authentifie.
        // ------------------------------------------------------------------
        [HttpGet("bannis")]
        [SwaggerResponse(200, "Les adresses bannies.")]
        public async Task<IActionResult> Bannis(
            [FromServices] IBannissementRepository bannissements, CancellationToken ct) =>
            Ok(await bannissements.GetTousAsync(ct));

        /// <summary>
        /// Bannit un parent depuis sa fiche. Son compte n'est PAS supprimé.
        ///
        /// DEUX GESTES DISTINCTS, ET IL FAUT QUE ÇA LE RESTE. Bannir ferme la
        /// porte ; supprimer efface la personne. On peut vouloir l'un sans
        /// l'autre — mettre dehors sans détruire l'historique d'un litige en
        /// cours, par exemple. Les fondre aurait rendu le bannissement
        /// irréversible dans les faits.
        /// </summary>
        [HttpPost("parents/{id:int}/bannir")]
        [SwaggerResponse(200, "Le parent est banni.")]
        [SwaggerResponse(403, "Le compte super-administrateur ne se bannit pas.")]
        [SwaggerResponse(404, "Compte inexistant.")]
        public async Task<IActionResult> BannirParent(
            int id,
            [FromBody] BannirRequest? requete,
            [FromServices] IBannissementRepository bannissements,
            [FromServices] IVerrouBannissement verrou,
            CancellationToken ct)
        {
            // LA MÊME GARDE QUE LA SUPPRESSION. Se bannir soi-même fermerait
            // l'administration à clé, de l'intérieur, sans aucun moyen de
            // revenir : la liste ne se lève que depuis un écran auquel on
            // n'aurait plus accès.
            var refus = await RefuserSiCompteProtegeAsync(id, "bannissement");
            if (refus is not null) return refus;

            var mail = await _adminService.MailDuParentAsync(id);

            if (string.IsNullOrWhiteSpace(mail)) return NotFound();

            await bannissements.BannirAsync(mail, requete?.Motif, MailAppelant(), ct);

            // COUPE NET : sans cet oubli, la session en cours du banni
            // survivrait jusqu'a l expiration de l instantane. C est ce qui
            // rend le bannissement immediat plutot que differe.
            verrou.Oublier();

            _logger.LogWarning(
                "Parent {ParentId} banni par un administrateur. Motif : {Motif}.",
                id, string.IsNullOrWhiteSpace(requete?.Motif) ? "(aucun)" : requete!.Motif);

            return Ok(new { mail });
        }

        /// <summary>
        /// Ajoute une adresse à la main, sans passer par un compte.
        ///
        /// C'EST LE SEUL CHEMIN QUAND LE COMPTE N'EXISTE PLUS, ou n'a jamais
        /// existé : on peut fermer la porte à quelqu'un avant qu'il entre.
        /// </summary>
        [HttpPost("bannis")]
        [SwaggerResponse(204, "Adresse bannie.")]
        [SwaggerResponse(400, "Adresse absente ou protégée.")]
        public async Task<IActionResult> AjouterBanni(
            [FromBody] BannirRequest requete,
            [FromServices] IBannissementRepository bannissements,
            [FromServices] IVerrouBannissement verrou,
            CancellationToken ct)
        {
            var mail = (requete?.Mail ?? string.Empty).Trim();

            if (mail.Length == 0 || !mail.Contains('@'))
            {
                return BadRequest(new { message = "Adresse électronique invalide." });
            }

            // MÊME GARDE QUE PAR L'IDENTIFIANT. Sans elle, la protection du
            // super-administrateur se contournerait en tapant son adresse
            // dans le champ libre — et le verrou ne serait plus un verrou.
            if (_comptesProteges.Protege(mail))
            {
                _logger.LogWarning("Tentative de bannissement du super-administrateur, refusee.");

                return BadRequest(new
                {
                    message = "Le compte super-administrateur ne peut pas être banni.",
                });
            }

            await bannissements.BannirAsync(mail, requete?.Motif, MailAppelant(), ct);
            verrou.Oublier();

            _logger.LogWarning("Adresse bannie a la main par un administrateur.");

            return NoContent();
        }

        /// <summary>
        /// Lève un bannissement. La ligne est effacée, pas datée.
        /// </summary>
        [HttpDelete("bannis")]
        [SwaggerResponse(204, "Bannissement levé.")]
        [SwaggerResponse(404, "Cette adresse n'était pas bannie.")]
        public async Task<IActionResult> LeverBanni(
            [FromQuery] string mail,
            [FromServices] IBannissementRepository bannissements,
            [FromServices] IVerrouBannissement verrou,
            CancellationToken ct)
        {
            var leve = await bannissements.LeverAsync(mail, ct);

            // AUSSI IMPORTANT QUE POUR LE BANNISSEMENT, et dans l autre sens :
            // sans cet oubli, quelqu un a qui on vient de rouvrir la porte
            // resterait refuse une demi-minute, reessaierait, echouerait.
            verrou.Oublier();

            if (leve) _logger.LogWarning("Bannissement leve par un administrateur.");

            return leve ? NoContent() : NotFound();
        }

        public class BannirRequest
        {
            /// <summary>Requise pour un ajout à la main, ignorée depuis une fiche.</summary>
            public string? Mail { get; set; }

            public string? Motif { get; set; }
        }

        /// <summary>
        /// L'adresse de l'administrateur qui agit, pour la tracer.
        ///
        /// Nulle si le jeton ne la porte pas : une trace absente vaut mieux
        /// qu'un bannissement refusé parce qu'on n'a pas su qui le posait.
        /// </summary>
        private string? MailAppelant() =>
            User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name;

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
