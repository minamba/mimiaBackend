using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Services
{
    public interface IAlerteQuotaService
    {
        /// <summary>
        /// Prévient le parent de cet élève qu'il approche de sa limite.
        /// Retourne faux si l'envoi n'a pas abouti.
        /// </summary>
        Task<bool> PrevenirAsync(int eleveId, CancellationToken ct = default);
    }

    public class AlerteQuotaService : IAlerteQuotaService
    {
        private static readonly CultureInfo Fr = new("fr-FR");

        private readonly IAbonnementRepository _abonnements;
        private readonly IEleveService _eleves;
        private readonly IParentService _parents;
        private readonly IServiceEmail _email;
        private readonly OptionsEmail _optionsEmail;
        private readonly ILogger<AlerteQuotaService> _logger;

        public AlerteQuotaService(
            IAbonnementRepository abonnements,
            IEleveService eleves,
            IParentService parents,
            IServiceEmail email,
            IOptions<OptionsEmail> optionsEmail,
            ILogger<AlerteQuotaService> logger)
        {
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _eleves = eleves ?? throw new ArgumentNullException(nameof(eleves));
            _parents = parents ?? throw new ArgumentNullException(nameof(parents));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _optionsEmail = optionsEmail?.Value ?? throw new ArgumentNullException(nameof(optionsEmail));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> PrevenirAsync(int eleveId, CancellationToken ct = default)
        {
            var eleve = await _eleves.GetEleveByIdAsync(eleveId);
            if (eleve is null) return false;

            var etat = await _abonnements.GetEtatQuotaAsync(eleve.ParentId, ct);
            if (etat is null) return false;

            var parent = await _parents.GetParentByIdAsync(eleve.ParentId);
            if (string.IsNullOrWhiteSpace(parent?.Mail)) return false;

            var site = (_optionsEmail.UrlSite ?? "https://mimia.fr").TrimEnd('/');

            var valeurs = new Dictionary<string, string>
            {
                ["apercu"] = $"Il reste {Heures(etat.MinutesRestantes)} sur votre forfait ce mois-ci.",
                ["heuresRestantes"] = Heures(etat.MinutesRestantes),
                ["offre"] = Echapper(etat.OffreLibelle),
                // À l'heure de Paris : une période qui se termine à 01 h 00 est
                // stockée la veille en UTC, et le mail annoncerait le mauvais jour.
                ["renouvellement"] = HeureFrance.Locale(etat.PeriodeFin).ToString("d MMMM", Fr),
                ["partConsommee"] = $"{Math.Round(etat.PartConsommee * 100)} %",
                ["blocEnfants"] = BlocEnfants(etat),
                ["lien"] = $"{site}/profil",
            };

            var ok = await _email.EnvoyerAsync(
                parent.Mail!,
                $"Il reste {Heures(etat.MinutesRestantes)} sur votre forfait Mimia",
                "quota",
                valeurs,
                ct);

            if (!ok)
            {
                _logger.LogWarning(
                    "L'alerte de quota n'a pas pu etre envoyee au parent {ParentId}.", eleve.ParentId);
            }

            return ok;
        }

        /// <summary>
        /// Une ligne par enfant. Ceux qui n'ont rien fait apparaissent aussi :
        /// c'est une information utile pour le parent, et leur absence donnerait
        /// l'impression d'un relevé incomplet.
        /// </summary>
        private static string BlocEnfants(EtatQuota etat)
        {
            if (etat.Enfants.Count == 0) return "Aucun enfant enregistré.";

            var lignes = new StringBuilder();

            foreach (var enfant in etat.Enfants.OrderByDescending(e => e.MinutesConsommees))
            {
                var consomme = enfant.MinutesConsommees == 0
                    ? "pas de séance ce mois-ci"
                    : $"{Heures(enfant.MinutesConsommees)} de cours";

                lignes.Append(
                    $"<strong style=\"color:#16233a;\">{Echapper(enfant.Prenom)}</strong> — {consomme}<br />");
            }

            return lignes.ToString();
        }

        /// <summary>
        /// Des heures, jamais des minutes ni des tokens : « il reste 4 h 30 » se
        /// comprend d'un coup d'œil, « il reste 270 minutes » demande un calcul.
        /// </summary>
        private static string Heures(int minutes)
        {
            if (minutes <= 0) return "0 h";

            var h = minutes / 60;
            var m = minutes % 60;

            if (h == 0) return $"{m} min";
            return m == 0 ? $"{h} h" : $"{h} h {m:00}";
        }

        private static string Echapper(string? texte) => WebUtility.HtmlEncode(texte ?? string.Empty);
    }
}
