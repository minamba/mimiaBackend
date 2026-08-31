using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Les interrupteurs du produit.
    ///
    /// Deux routes et deux publics : l'administration les change, tout le monde
    /// les lit. La lecture est ANONYME à dessein — la barre de navigation et la
    /// page de connexion en ont besoin avant qu'un compte existe, et c'est
    /// précisément le cas d'usage du mode test.
    ///
    /// Rien ici n'est un secret : savoir que le service est en accès privé
    /// n'ouvre aucune porte, c'est le serveur d'identité qui refuse les
    /// inscriptions.
    /// </summary>
    [ApiController]
    [Route("reglages")]
    public class ReglagesController : ControllerBase
    {
        /// <summary>
        /// Accès privé : ni tarifs, ni inscription, ni connexion Google, ni
        /// réinitialisation de mot de passe.
        /// </summary>
        public const string ModeTest = "MODE_TEST";

        /// <summary>
        /// Le compte de démonstration accepte-t-il les connexions ?
        ///
        /// Éteint, il n'est PAS supprimé : le serveur d'identité refuse la
        /// connexion exactement comme si l'adresse n'existait pas. C'est ce qui
        /// permet de le rallumer pour une démonstration sans recréer ni son
        /// historique, ni ses fiches, ni ses évaluations.
        /// </summary>
        public const string CompteTest = "COMPTE_TEST_ACTIF";

        /// <summary>
        /// L'essai gratuit est-il ouvert aux nouveaux venus ?
        ///
        /// Fermé, le site continue de fonctionner normalement : seule la porte
        /// d'entrée gratuite disparaît. « Commencer gratuitement » devient
        /// « S'inscrire maintenant », et plus aucun chemin n'ouvre d'essai —
        /// ni le bouton d'accueil, ni la page des tarifs.
        ///
        /// Sert à couper la robinet quand l'essai coûte trop cher, ou pendant
        /// une campagne où on ne veut vendre que des abonnements. Un réglage
        /// plutôt qu'un déploiement : la décision se prend en une seconde et
        /// se défait aussi vite.
        /// </summary>
        public const string EssaisOuverts = "ESSAIS_OUVERTS";

        /// <summary>
        /// Les tâches de fond qui appellent le modèle tournent-elles ?
        ///
        /// LE COUPE-CIRCUIT DE LA FACTURE. Décrire une planche et en
        /// cartographier les repères coûtent DEUX lectures d'image chacune, à
        /// 8 000 jetons de plafond. Transcrire un document, observer une séance,
        /// rédiger un bilan s'ajoutent. Ces travaux sont utiles mais jamais
        /// urgents : ils peuvent tous attendre demain.
        ///
        /// EN BASE ET NON EN CONFIGURATION, et c'est tout l'intérêt. Un réglage
        /// de fichier demande un redéploiement — dix minutes pendant lesquelles
        /// la facture continue de courir. Celui-ci s'éteint d'un clic dans
        /// l'administration, et les workers le relisent à chaque passage : au
        /// pire deux minutes avant que ça prenne effet.
        ///
        /// Le dialogue n'est PAS concerné : couper la parole aux professeurs
        /// pour économiser reviendrait à fermer le magasin pour ne plus payer
        /// l'électricité.
        /// </summary>
        public const string TachesDeFond = "TACHES_DE_FOND_ACTIVES";

        /// <summary>
        /// Le site est-il en maintenance ?
        ///
        /// CE QUE ÇA FAIT, ET SURTOUT CE QUE ÇA NE FAIT PAS
        /// -----------------------------------------------
        /// C'est un rideau, pas un verrou. Le site affiche une page d'attente
        /// au lieu de ses écrans ; l'API, elle, continue de répondre
        /// normalement. C'est indispensable : sans ça, l'administrateur qui
        /// vient de tirer le rideau ne pourrait plus rien faire derrière — ni
        /// le lever, ni travailler.
        ///
        /// Ce réglage sert donc à ce qu'un visiteur voie une page soignée
        /// plutôt que des erreurs pendant qu'on travaille. Pour VRAIMENT
        /// fermer le service, on arrête l'API : c'est un autre geste, et il
        /// n'a pas à se confondre avec celui-ci.
        ///
        /// L'administrateur passe au travers du rideau — voir le verrou côté
        /// navigateur. Le lien de connexion reste ouvert sur la page
        /// d'attente, sans quoi un administrateur déconnecté resterait dehors.
        /// </summary>
        public const string Maintenance = "MAINTENANCE_ACTIVE";

        /// <summary>
        /// Les seules clés acceptées. Sans cette liste, la route deviendrait un
        /// magasin de clés arbitraires que personne ne lit et que rien ne nettoie.
        /// </summary>
        private static readonly string[] ClesConnues =
            [ModeTest, CompteTest, EssaisOuverts, TachesDeFond, Maintenance];

        private readonly IReglageRepository _reglages;
        private readonly ILogger<ReglagesController> _logger;

        public ReglagesController(
            IReglageRepository reglages, ILogger<ReglagesController> logger)
        {
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Ce que le navigateur a besoin de savoir, sans être connecté.</summary>
        [HttpGet("publics")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Les drapeaux publics.")]
        public async Task<IActionResult> Publics(CancellationToken ct)
        {
            try
            {
                return Ok(new
                {
                    modeTest = await _reglages.EstActifAsync(ModeTest, false, ct),

                    // Ouverts par défaut : un administrateur qui n'a jamais
                    // touché ce réglage s'attend à ce que l'essai fonctionne.
                    essaisOuverts = await _reglages.EstActifAsync(EssaisOuverts, true, ct),

                    // ÉTEINT PAR DÉFAUT, ET C'EST LE SEUL DÉFAUT ACCEPTABLE.
                    // Un réglage absent ou illisible qui afficherait la page
                    // d'attente fermerait le site sur une panne de lecture.
                    maintenance = await _reglages.EstActifAsync(Maintenance, false, ct),
                });
            }
            catch (Exception ex)
            {
                // Un réglage illisible ne doit pas empêcher le site de
                // s'afficher : on rend le mode éteint, c'est-à-dire le produit
                // ouvert, et on journalise.
                _logger.LogError(ex, "Lecture des reglages publics impossible.");
                return Ok(new { modeTest = false, essaisOuverts = true, maintenance = false });
            }
        }

        [HttpGet]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Tous les réglages.")]
        public async Task<IActionResult> Tous(CancellationToken ct) =>
            Ok(new
            {
                modeTest = await _reglages.EstActifAsync(ModeTest, false, ct),

                // Actif par défaut : le compte existe pour servir, et un
                // administrateur qui ne l'a jamais touché s'attend à ce qu'il
                // fonctionne.
                compteTest = await _reglages.EstActifAsync(CompteTest, true, ct),

                essaisOuverts = await _reglages.EstActifAsync(EssaisOuverts, true, ct),

                // Actives par défaut : une base neuve ne doit pas démarrer avec
                // le produit à moitié éteint.
                tachesDeFond = await _reglages.EstActifAsync(TachesDeFond, true, ct),

                maintenance = await _reglages.EstActifAsync(Maintenance, false, ct),
            });

        [HttpPut("{cle}")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(204, "Réglage enregistré.")]
        [SwaggerResponse(400, "Clé inconnue.")]
        public async Task<IActionResult> Definir(
            string cle, [FromBody] DefinirReglageRequest requete, CancellationToken ct)
        {
            var connue = ClesConnues.FirstOrDefault(
                k => string.Equals(cle, k, StringComparison.OrdinalIgnoreCase));

            if (connue is null) return BadRequest(new { message = "Réglage inconnu." });

            await _reglages.DefinirAsync(connue, requete.Actif, ct);

            _logger.LogWarning(
                "Reglage {Cle} {Etat} par un administrateur.",
                connue, requete.Actif ? "ACTIVE" : "DESACTIVE");

            return NoContent();
        }

        /// <summary>
        /// Recopie le catalogue de la base vers Stripe : produits et tarifs.
        ///
        /// Réservé à l'administration et déclenché à la main, jamais au
        /// démarrage : créer des tarifs chez un prestataire de paiement est
        /// une écriture, pas une lecture. Elle doit être voulue.
        ///
        /// Idempotent — relancer ne crée pas de doublons.
        /// </summary>
        [HttpPost("stripe/catalogue")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Le catalogue synchronisé.")]
        [SwaggerResponse(503, "Stripe n'est pas configuré.")]
        public async Task<IActionResult> SynchroniserCatalogue(
            [FromServices] ICatalogueStripeService catalogue, CancellationToken ct)
        {
            try
            {
                return Ok(new { lignes = await catalogue.SynchroniserAsync(ct) });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la synchronisation du catalogue Stripe.");
                return StatusCode(500, new { message = "La synchronisation a échoué. Voir les logs." });
            }
        }

        public class DefinirReglageRequest
        {
            public bool Actif { get; set; }
        }
    }
}
