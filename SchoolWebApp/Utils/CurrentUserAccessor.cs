using System.Security.Claims;

namespace SchoolWebApp.Api.Utils
{
    /// <summary>
    /// Lit l'identité du porteur du JWT. Source de vérité unique pour savoir
    /// « qui appelle » — aucun contrôleur ne doit fouiller les claims lui-même.
    /// </summary>
    public interface ICurrentUserAccessor
    {
        /// <summary>Claim `sub` : identifiant de l'utilisateur côté serveur d'identité.</summary>
        string? IdentityUserId { get; }

        string? Mail { get; }

        string? Prenom { get; }

        string? Nom { get; }

        /// <summary>
        /// L'enfant qui appelle, si c'est une session enfant. Null pour un parent.
        ///
        /// C'est le seul point par lequel le reste du code apprend qu'il a
        /// affaire à un enfant plutôt qu'à son parent — les deux portent le
        /// même claim « sub », volontairement.
        /// </summary>
        int? EleveId { get; }

        /// <summary>
        /// Quand ce parent a coché les conditions générales à son inscription : une autre base, donc ça voyage par le jeton
        /// (08/11/2026). Null s'il est arrivé autrement, par Google ou avant la case.
        /// </summary>
        DateTime? ConditionsCocheesLe { get; }
    }

    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor
                ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        // OpenIddict émet le claim OIDC "sub". ASP.NET le remappe parfois vers
        // ClaimTypes.NameIdentifier selon le handler : on teste les deux.
        public string? IdentityUserId =>
            Premier("sub", ClaimTypes.NameIdentifier);

        public string? Mail =>
            Premier("email", ClaimTypes.Email);

        public string? Prenom =>
            Premier("given_name", ClaimTypes.GivenName);

        public string? Nom =>
            Premier("family_name", ClaimTypes.Surname);

        public int? EleveId =>
            int.TryParse(Premier(Auth.AuthentificationEleve.ClaimEleve), out var id) ? id : null;

        public DateTime? ConditionsCocheesLe =>
            DateTime.TryParse(Premier("cgv_cochees_le"), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var le) ? le : null;

        private string? Premier(params string[] types)
        {
            if (User is null) return null;

            foreach (var type in types)
            {
                var valeur = User.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(valeur)) return valeur;
            }

            return null;
        }
    }
}
