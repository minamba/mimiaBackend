using System.Text.Json;
using SchoolWebApp.Api.Services.Affluence;

namespace SchoolWebApp.Api.Middleware
{
    /// <summary>
    /// Le garde de la salle d'attente : sans billet admis, l'API ne répond pas.
    ///
    /// SANS CE GARDE, LA SALLE D'ATTENTE N'EST QU'UN DÉCOR. Une file qui ne
    /// vit que dans le navigateur se contourne en rechargeant la page, en
    /// ouvrant une fenêtre privée, ou en appelant l'API directement. Et ce
    /// n'est même pas une question de malveillance : le jour d'un afflux, ce
    /// sont les gens pressés et bien intentionnés qui contournent, et ce sont
    /// eux qui font tomber le serveur pour tous les autres.
    ///
    /// LES PAGES, ELLES, PASSENT TOUJOURS. Ce garde ne s'applique qu'aux
    /// routes de contrôleur — jamais aux fichiers du site ni au repli SPA.
    /// Il faut bien que le visiteur puisse charger la page qui lui annonce
    /// qu'il est en file d'attente ; la lui refuser le laisserait devant un
    /// navigateur vide, ce qui est la pire façon de faire patienter
    /// quelqu'un. Ces fichiers sont servis depuis le disque et mis en cache :
    /// ils ne coûtent presque rien, ce ne sont pas eux qu'on protège.
    ///
    /// L'ADMINISTRATEUR ENTRE TOUJOURS, comme pour le rideau de maintenance et
    /// pour la même raison : celui qui a allumé la salle doit pouvoir
    /// l'éteindre. Un garde-fou dont on devient soi-même prisonnier n'en est
    /// plus un.
    ///
    /// UN 503 ET NON UN 403. Le 503 dit « reviens plus tard », c'est
    /// exactement le cas, et il se distingue des refus de droits. Le corps
    /// porte le billet complet : le site affiche le rang tout de suite, sans
    /// un second aller-retour au moment précis où le serveur est chargé.
    /// </summary>
    public sealed class GardeAffluenceMiddleware
    {
        public const string Code = "AFFLUENCE";

        /// <summary>
        /// L'en-tête qui porte le billet.
        ///
        /// UN EN-TÊTE ET NON UN COOKIE : le billet n'est pas une identité, il
        /// ne doit pas voyager tout seul sur des requêtes que le site n'a pas
        /// voulues, ni survivre à la fermeture de l'onglet.
        /// </summary>
        public const string EnTete = "X-Billet";

        private readonly RequestDelegate _suivant;

        public GardeAffluenceMiddleware(RequestDelegate suivant)
        {
            _suivant = suivant ?? throw new ArgumentNullException(nameof(suivant));
        }

        public async Task InvokeAsync(
            HttpContext contexte,
            ReglageAffluence reglage,
            ISalleDAttente salle)
        {
            await reglage.RafraichirAsync(contexte.RequestAborted);

            if (!reglage.Active)
            {
                await _suivant(contexte);
                return;
            }

            // SEULEMENT LES ROUTES DE CONTRÔLEUR. Les fichiers du site et le
            // repli SPA n'ont pas d'endpoint de contrôleur : ils passent.
            var endpoint = contexte.GetEndpoint();
            if (endpoint?.Metadata
                    .GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                is null)
            {
                await _suivant(contexte);
                return;
            }

            if (endpoint.Metadata.GetMetadata<HorsSalleDAttenteAttribute>() is not null)
            {
                await _suivant(contexte);
                return;
            }

            if (EstAdministrateur(contexte.User))
            {
                await _suivant(contexte);
                return;
            }

            // L'EN-TÊTE, OU LE PARAMÈTRE `billet` POUR LE MICRO : un WebSocket ne
            // peut pas porter d'en-tête choisi (06/11/2026).
            var billet = contexte.Request.Headers[EnTete].ToString();
            if (string.IsNullOrEmpty(billet)) billet = contexte.Request.Query["billet"].ToString();
            var aUnBillet = Guid.TryParse(billet, out var jeton) && jeton != Guid.Empty;

            if (aUnBillet && salle.EstAdmis(jeton))
            {
                await _suivant(contexte);
                return;
            }

            // SANS BILLET, ON REFUSE SANS EN CRÉER — Camara, le 06/11/2026 : « comment je peux avoir 10 personnes
            // en file d'attente alors que je suis tout seul ? ». Au chargement, la page lance une dizaine d'appels
            // à la fois, tous sans billet : chacun en recevait un NEUF, et un seul visiteur prenait dix rangs (les
            // neuf orphelins expiraient ensuite en trente secondes, d'où un rang qui descend tout seul). Le billet
            // ne se délivre plus qu'au guichet (`affluence/billet`), que le site appelle une seule fois.
            if (!aUnBillet)
            {
                await RefuserAsync(contexte, null);
                return;
            }

            // ON DÉLIVRE UN BILLET PLUTÔT QUE DE RENVOYER UN REFUS SEC. Le
            // visiteur refoulé ici est justement celui qui doit prendre son
            // rang ; le lui donner dans la même réponse lui épargne un appel
            // de plus, et nous une requête de plus.
            await RefuserAsync(contexte, salle.Demander(jeton, reglage.Places));
        }

        /// <summary>Le 503, avec le billet complet quand il y en a un ; sans billet, le site va le chercher au guichet.</summary>
        private static async Task RefuserAsync(HttpContext contexte, Billet? place)
        {
            contexte.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            contexte.Response.Headers.RetryAfter = (place?.RappelDans ?? 1).ToString();
            contexte.Response.ContentType = "application/json; charset=utf-8";

            await contexte.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                code = Code,
                message = "Beaucoup de monde en ce moment. Votre place est gardée.",
                billet = place?.Jeton,
                rang = place?.Rang,
                devant = place?.Devant,
                attenteSecondes = place?.AttenteSecondes,
                rappelDans = place?.RappelDans ?? 1,
            }));
        }

        /// <summary>
        /// LES DEUX FORMES, comme la politique « EstAdmin » de
        /// <c>Program.cs</c>. Selon la façon dont le jeton a été émis, le rôle
        /// arrive en revendication « role » ou en rôle d'identité ; n'en
        /// tester qu'une enfermerait l'administrateur dehors une fois sur
        /// deux, et c'est le genre de panne qu'on ne découvre qu'un jour
        /// d'affluence.
        /// </summary>
        internal static bool EstAdministrateur(System.Security.Claims.ClaimsPrincipal? qui)
            => qui is not null
               && (qui.HasClaim("role", "Admin") || qui.IsInRole("Admin")
                   || qui.HasClaim("role", "SuperAdmin") || qui.IsInRole("SuperAdmin"));
    }
}
