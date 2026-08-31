using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Auth
{
    /// <summary>
    /// La seconde voie d'entrée : un enfant, avec son jeton de session.
    ///
    /// L'ENFANT PORTE L'IDENTITÉ DE SON PARENT, PLUS LA SIENNE
    /// ------------------------------------------------------
    /// Tous les contrôles d'appartenance de l'API partent du claim `sub` du
    /// parent : « cet élève est-il à moi ? », « cet abonnement est-il le
    /// mien ? ». Donner à l'enfant une identité entièrement nouvelle aurait
    /// obligé à réécrire chacun de ces contrôles — et un seul oublié, c'est un
    /// enfant qui lit les données d'une autre famille.
    ///
    /// On lui donne donc le `sub` du parent, ce qui fait fonctionner tout
    /// l'existant sans y toucher, ET un claim `eleve_id` qui dit lequel des
    /// enfants il est. C'est ce second claim qui le confine, par le filtre
    /// <see cref="RestrictionEleveFilter"/>.
    ///
    /// L'ACCÈS EST REVÉRIFIÉ À CHAQUE APPEL
    /// -----------------------------------
    /// Pas seulement à l'ouverture de session. Le parent qui suspend son
    /// enfant, ou qui régénère son code, doit voir l'appareil se fermer tout
    /// de suite — pas à l'expiration d'un jeton. C'est le prix d'une session
    /// en base plutôt que d'un jeton signé, et c'est ce prix qu'on a choisi de
    /// payer.
    /// </summary>
    public class AuthentificationEleve : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string Schema = "SessionEleve";

        /// <summary>Le claim qui dit quel enfant appelle.</summary>
        public const string ClaimEleve = "eleve_id";

        /// <summary>Le préfixe attendu : `Authorization: Eleve <jeton>`.</summary>
        private const string Prefixe = "Eleve ";

        private readonly ISessionEleveRepository _sessions;
        private readonly IEleveService _eleves;
        private readonly IParentService _parents;

        public AuthentificationEleve(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISessionEleveRepository sessions,
            IEleveService eleves,
            IParentService parents)
            : base(options, logger, encoder)
        {
            _sessions = sessions;
            _eleves = eleves;
            _parents = parents;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var entete = Request.Headers.Authorization.ToString();

            // Pas notre schéma : on passe la main sans se prononcer. C'est ce
            // qui laisse le jeton du parent être traité par OpenIddict.
            if (!entete.StartsWith(Prefixe, StringComparison.Ordinal))
            {
                return AuthenticateResult.NoResult();
            }

            var jeton = entete[Prefixe.Length..].Trim();
            if (jeton.Length == 0) return AuthenticateResult.NoResult();

            var session = await _sessions.TrouverAsync(Hacher(jeton), Context.RequestAborted);
            if (session is null) return AuthenticateResult.Fail("Session inconnue.");

            // L'ENFANT PEUT AVOIR ÉTÉ SUSPENDU DEPUIS. On revérifie.
            var eleve = await _eleves.GetEleveByIdAsync(session.EleveId);
            if (eleve is null || !eleve.AccesOuvert)
            {
                // La session ne sert plus à rien : on la ferme au passage,
                // plutôt que de la laisser être refusée mille fois.
                await _sessions.FermerToutesAsync(session.EleveId, Context.RequestAborted);
                return AuthenticateResult.Fail("Accès suspendu.");
            }

            var parent = await _parents.GetParentByIdAsync(session.ParentId);
            if (parent is null) return AuthenticateResult.Fail("Compte introuvable.");

            var identite = new ClaimsIdentity(
                new[]
                {
                    // Le `sub` du PARENT : tous les contrôles d'appartenance
                    // existants continuent de fonctionner tels quels.
                    new Claim("sub", parent.IdentityUserId),
                    new Claim(ClaimEleve, session.EleveId.ToString()),
                    new Claim(ClaimTypes.Name, session.PrenomEleve ?? "Élève"),
                },
                Schema);

            // ATTENDU, ET IL LE FAUT.
            //
            // Première version : lancé sans attendre, pour ne pas retarder la
            // réponse d'une milliseconde. Sauf que le dépôt partage le
            // `DbContext` de la requête, et qu'un contexte ne supporte pas deux
            // opérations en vol. Résultat : « The connection does not support
            // MultipleActiveResultSets », et un 500 sur la première page que
            // l'enfant ouvrait.
            //
            // Une milliseconde n'a jamais valu ça.
            await _sessions.ToucherAsync(session.Id, Context.RequestAborted);

            return AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identite), Schema));
        }

        /// <summary>
        /// Le jeton n'est jamais stocké en clair : on ne compare que des
        /// empreintes. Pas de sel — le jeton fait déjà trente-deux octets
        /// aléatoires, il n'y a pas de table à construire contre ça.
        /// </summary>
        public static string Hacher(string jeton) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jeton)));

        /// <summary>Un jeton de session : trente-deux octets tirés au sort.</summary>
        public static string GenererJeton() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
    }
}
