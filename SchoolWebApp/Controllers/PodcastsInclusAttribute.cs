using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// PAS DE PODCASTS AVEC STARTER — Camara, le 15/11/2026 : « cette fonctionnalité n'est pas disponible pour les
    /// personnes ayant l'abonnement Starter ». Posé sur les deux contrôleurs des podcasts (élève et espace parent) :
    /// toute route répond 403 avec la phrase, SAUF la liste, qui renvoie `nonInclus` pour que la page dise pourquoi.
    /// Une adresse tapée, un lien gardé ou une famille qui repasse à Starter ne lisent donc plus rien.
    /// La règle elle-même : <c>AccesForfaitService.PodcastsOuvertsAsync</c> (administrateurs, exemptés et essai passent).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PodcastsInclusAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext contexte, ActionExecutionDelegate suite)
        {
            var action = contexte.ActionDescriptor.RouteValues.TryGetValue("action", out var a) ? a : null;
            if (string.Equals(action, "Lister", StringComparison.Ordinal))
            {
                await suite();
                return;
            }

            var services = contexte.HttpContext.RequestServices;
            var acces = services.GetRequiredService<AccesForfaitService>();
            var ct = contexte.HttpContext.RequestAborted;

            bool inclus;
            string message;
            if (contexte.RouteData.Values.TryGetValue("eleveId", out var brut) && int.TryParse(brut?.ToString(), out var eleveId))
            {
                inclus = await acces.PodcastsPourEleveAsync(eleveId, ct);
                message = PodcastsEleve.MessageNonInclusEnfant;
            }
            else
            {
                // L'espace parent : le parent connecté (une session d'enfant n'y a de toute façon aucun accès).
                var utilisateur = services.GetRequiredService<ICurrentUserAccessor>();
                if (string.IsNullOrWhiteSpace(utilisateur.IdentityUserId) || utilisateur.EleveId is not null)
                {
                    await suite();
                    return;
                }

                var parent = await services.GetRequiredService<IParentService>().GetOrCreateAsync(
                    utilisateur.IdentityUserId, utilisateur.Mail, utilisateur.Prenom, utilisateur.Nom, noterLaVenue: false);
                inclus = await acces.PodcastsOuvertsAsync(parent.Id, ct);
                message = PodcastsEleve.MessageNonInclusParent;
            }

            if (inclus)
            {
                await suite();
                return;
            }

            contexte.Result = new ObjectResult(new { message, nonInclus = true }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
