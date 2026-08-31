using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>Ce que le navigateur envoie en arrivant sur le site.</summary>
    /// <param name="Visiteur">
    /// L'identifiant qu'il s'est tiré au sort et rangé dans son stockage
    /// local. Rien d'autre ne circule : ni page, ni référent, ni adresse.
    /// </param>
    public record VenueSurLeSite(string Visiteur);

    /// <summary>
    /// Le compteur de visites du site public.
    ///
    /// POURQUOI UNE ROUTE OUVERTE À TOUS
    /// ---------------------------------
    /// C'est tout l'objet de la mesure : compter ceux qui ne sont PAS encore
    /// inscrits. Une route protégée ne verrait que les familles déjà clientes,
    /// c'est-à-dire précisément celles qu'on sait déjà compter.
    ///
    /// CE QU'ON ACCEPTE D'ELLE, ET RIEN DE PLUS
    /// ----------------------------------------
    /// Un identifiant au format attendu, et c'est tout. Pas de date — l'heure
    /// est celle du serveur, sinon n'importe qui écrirait dans le passé et
    /// fausserait une courbe qu'on lit pour décider. Pas de page, pas de
    /// référent : ce sont des données qu'on n'a pas demandées et qu'il
    /// faudrait alors déclarer.
    ///
    /// Le dépôt ignore un visiteur déjà vu dans l'heure. Un appel rejoué en
    /// boucle ne fait donc pas grossir la table.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("visites")]
    public class VisitesController : ControllerBase
    {
        /// <summary>
        /// Longueur exacte d'un identifiant tel que le navigateur le produit.
        ///
        /// La forme est vérifiée pour une seule raison : la colonne fait
        /// soixante-quatre caractères, et une chaîne plus longue ferait
        /// échouer l'insertion en base plutôt qu'ici, avec une trace d'erreur
        /// pour quelque chose qui n'est qu'un appel mal formé.
        /// </summary>
        private const int LongueurIdentifiant = 36;

        private readonly IAdminService _adminService;
        private readonly ILogger<VisitesController> _logger;

        public VisitesController(IAdminService adminService, ILogger<VisitesController> logger)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Note une venue. Répond toujours, et sans contenu.</summary>
        [HttpPost]
        public async Task<IActionResult> Noter([FromBody] VenueSurLeSite venue)
        {
            var visiteur = venue?.Visiteur?.Trim();

            if (string.IsNullOrEmpty(visiteur) || visiteur.Length != LongueurIdentifiant)
            {
                return NoContent();
            }

            try
            {
                await _adminService.EnregistrerVisiteAsync(visiteur);
            }
            catch (Exception ex)
            {
                // UNE VISITE PERDUE NE VAUT PAS UNE ERREUR CHEZ LE VISITEUR.
                //
                // Cet appel part d'une page d'accueil que personne n'a demandé
                // à instrumenter. S'il échoue, on le note pour nous et le
                // visiteur ne voit rien : un compteur n'a pas à faire clignoter
                // la console de quelqu'un qui découvre le site.
                _logger.LogWarning(ex, "Visite non enregistrée.");
            }

            return NoContent();
        }
    }
}
