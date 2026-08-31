using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SchoolWebApp.Api.Auth
{
    /// <summary>
    /// Marque une action ou un contrôleur comme accessible à un ENFANT.
    ///
    /// Sans cet attribut, une session enfant est refusée. C'est le sens de la
    /// règle : on autorise explicitement, on n'interdit jamais.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AutoriseEleveAttribute : Attribute
    {
    }

    /// <summary>
    /// Confine une session enfant à ce qui la concerne.
    ///
    /// DEUX RÈGLES, ET ELLES SE COMPLÈTENT
    /// ----------------------------------
    /// 1. TOUT EST FERMÉ SAUF CE QUI PORTE `[AutoriseEleve]`. Une liste noire
    ///    aurait laissé passer la prochaine route ajoutée — et ce serait
    ///    précisément celle qu'on aurait oublié de fermer. Ici, une route
    ///    nouvelle est interdite aux enfants tant que personne ne l'a ouverte
    ///    en connaissance de cause.
    ///
    /// 2. UN ENFANT N'EST QUE LUI-MÊME. Toute route portant un `eleveId` doit
    ///    porter le SIEN. Sans cette seconde règle, la première suffirait à
    ///    ouvrir les conversations d'un enfant… à son frère, puisque les deux
    ///    partagent le compte du parent et donc les contrôles d'appartenance.
    ///
    /// POURQUOI UN FILTRE GLOBAL ET NON UNE POLITIQUE
    /// ---------------------------------------------
    /// Une politique d'autorisation ne voit pas les paramètres de route. Or la
    /// seconde règle porte entièrement sur un paramètre de route.
    /// </summary>
    public class RestrictionEleveFilter : IAuthorizationFilter
    {
        /// <summary>Les noms sous lesquels l'identifiant d'un élève circule.</summary>
        private static readonly string[] NomsDeRoute = { "eleveId", "id" };

        public void OnAuthorization(AuthorizationFilterContext contexte)
        {
            var claim = contexte.HttpContext.User
                .FindFirst(AuthentificationEleve.ClaimEleve)?.Value;

            // Pas une session enfant : rien à restreindre ici.
            if (claim is null) return;

            if (!int.TryParse(claim, out var eleveId))
            {
                contexte.Result = new ForbidResult();
                return;
            }

            var autorise = contexte.ActionDescriptor.EndpointMetadata
                .OfType<AutoriseEleveAttribute>()
                .Any();

            if (!autorise)
            {
                contexte.Result = new ObjectResult(new
                {
                    message = "Cet espace est réservé à tes parents.",
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };

                return;
            }

            // `id` n'est un identifiant d'élève que sur les contrôleurs qui
            // parlent d'élèves. Ailleurs — une fiche, une conversation — il
            // désigne autre chose, et le comparer n'aurait aucun sens.
            var controleur = contexte.RouteData.Values["controller"] as string;

            foreach (var nom in NomsDeRoute)
            {
                if (nom == "id" && !string.Equals(controleur, "Eleves", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!contexte.RouteData.Values.TryGetValue(nom, out var brut)) continue;
                if (!int.TryParse(brut?.ToString(), out var demande)) continue;

                if (demande != eleveId)
                {
                    contexte.Result = new ObjectResult(new
                    {
                        message = "Ce n'est pas ton espace.",
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                    };

                    return;
                }
            }
        }
    }
}
