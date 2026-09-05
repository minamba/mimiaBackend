using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Le verrou qui coupe une session EN COURS.
    ///
    /// POURQUOI IL EXISTE EN PLUS DU REFUS À LA CONNEXION
    /// --------------------------------------------------
    /// Le serveur d'identité ferme les portes : plus de connexion, plus
    /// d'inscription. Mais quelqu'un qui était DÉJÀ entré garde son jeton
    /// jusqu'à son expiration — il continuerait de travailler pendant une
    /// heure après avoir été mis dehors. Ce verrou-ci le refuse à la requête
    /// suivante, quelle qu'elle soit.
    ///
    /// DEUX IDENTIFIANTS, ET IL EN FAUT DEUX
    /// -------------------------------------
    /// Le jeton d'un parent porte son adresse. Celui d'un enfant porte le
    /// « sub » de son parent et son propre `eleve_id`, mais AUCUNE adresse —
    /// c'est délibéré, l'enfant n'a pas à en avoir une. Vérifier la seule
    /// adresse aurait donc laissé les enfants d'un parent banni travailler
    /// comme si de rien n'était.
    ///
    /// UN INSTANTANÉ EN MÉMOIRE, PAS UNE REQUÊTE PAR APPEL
    /// ---------------------------------------------------
    /// Ce verrou est traversé par CHAQUE requête authentifiée, y compris les
    /// battements du chat. Une lecture SQL à chaque fois coûterait plus cher
    /// que tout ce qu'elle protège. La liste, elle, tient en quelques adresses.
    ///
    /// L'INSTANTANÉ EST OUBLIÉ À CHAQUE ÉCRITURE, et c'est ce qui rend le
    /// « coupe net » exact : bannir depuis l'administration passe par le même
    /// processus, donc l'oubli est immédiat et la requête suivante du banni
    /// est déjà refusée. La durée de vie n'est qu'un filet — elle ne sert que
    /// le jour où l'API tournera en plusieurs exemplaires, où l'oubli d'une
    /// instance n'atteint pas les autres.
    /// </summary>
    public interface IVerrouBannissement
    {
        /// <summary>
        /// Cet appelant est-il banni ? `mail` pour un parent, `identite` pour
        /// un enfant — on passe les deux, l'un des deux suffit.
        /// </summary>
        Task<bool> EstBloqueAsync(
            string? mail, string? identite, CancellationToken ct = default);

        /// <summary>
        /// Jette l'instantané. Appelé après chaque bannissement ou levée.
        /// </summary>
        void Oublier();
    }

    public class VerrouBannissement : IVerrouBannissement
    {
        private const string Cle = "bannissement.instantane";

        /// <summary>
        /// Le filet, et rien de plus. L'oubli explicite fait le travail dans
        /// une API à une seule instance ; cette durée ne borne que le cas où
        /// il y en aura plusieurs.
        /// </summary>
        private static readonly TimeSpan Fraicheur = TimeSpan.FromSeconds(30);

        private readonly SchoolWebAppDatabaseContext _context;
        private readonly IMemoryCache _cache;

        public VerrouBannissement(SchoolWebAppDatabaseContext context, IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<bool> EstBloqueAsync(
            string? mail, string? identite, CancellationToken ct = default)
        {
            var propre = (mail ?? string.Empty).Trim().ToLowerInvariant();

            if (propre.Length == 0 && string.IsNullOrWhiteSpace(identite)) return false;

            var instantane = await LireAsync(ct);

            // RIEN DE BANNI : le cas de très loin le plus fréquent, et il sort
            // avant toute comparaison.
            if (instantane.Mails.Count == 0) return false;

            if (propre.Length > 0 && instantane.Mails.Contains(propre)) return true;

            return !string.IsNullOrWhiteSpace(identite)
                && instantane.Identites.Contains(identite);
        }

        public void Oublier() => _cache.Remove(Cle);

        private async Task<Instantane> LireAsync(CancellationToken ct)
        {
            if (_cache.TryGetValue(Cle, out Instantane? garde) && garde is not null) return garde;

            var mails = await _context.MailsBannis
                .AsNoTracking()
                .Select(b => b.Mail)
                .ToListAsync(ct);

            var identites = new List<string>();

            if (mails.Count > 0)
            {
                // LE PONT ENTRE LA LISTE ET LES SESSIONS D'ENFANTS. La liste ne
                // connaît que des adresses ; le jeton d'un enfant ne connaît
                // que le « sub » de son parent. On fait la jointure ICI, une
                // fois par instantané, plutôt qu'à chaque requête.
                identites = await _context.Parents
                    .AsNoTracking()
                    .Where(p => p.Mail != null
                                && p.IdentityUserId != ""
                                && mails.Contains(p.Mail.ToLower()))
                    .Select(p => p.IdentityUserId)
                    .ToListAsync(ct);
            }

            var neuf = new Instantane(
                mails.ToHashSet(StringComparer.OrdinalIgnoreCase),
                identites.ToHashSet(StringComparer.Ordinal));

            _cache.Set(Cle, neuf, Fraicheur);

            return neuf;
        }

        private sealed record Instantane(HashSet<string> Mails, HashSet<string> Identites);
    }
}
