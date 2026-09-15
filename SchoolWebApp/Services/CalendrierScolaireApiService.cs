using System.Text.Json;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une période de vacances, telle que la source officielle la publie.</summary>
    public record PeriodeVacancesBrute(string Zone, string Libelle, DateTime Debut, DateTime Fin);

    /// <summary>
    /// Va chercher le calendrier scolaire officiel auprès de data.education.gouv.fr
    /// — le jeu de données « fr-en-calendrier-scolaire », publié par le
    /// ministère et mis à jour par lui, pas par nous.
    ///
    /// POURQUOI CETTE SOURCE ET PAS UNE SAISIE À LA MAIN.
    /// Les dates de vacances sont publiées chaque année par arrêté, et
    /// ressaisir les neuf zones à la main chaque été est le genre de tâche
    /// qu'on oublie de faire — voir <see cref="Seed.Referentiels.ReferentielVacancesScolaires"/>,
    /// resté délibérément vide pour cette raison précise. Le ministère
    /// publie déjà ces mêmes dates en open data ; autant les lire directement
    /// que les recopier.
    /// </summary>
    public interface ICalendrierScolaireApiService
    {
        /// <summary>
        /// Les périodes de vacances de l'année scolaire demandée (« 2026-2027 »),
        /// une ligne par zone et par période — déjà dédoublonnées : la source
        /// publie une ligne par ACADÉMIE, pas par zone, et plusieurs académies
        /// partagent les mêmes dates.
        /// </summary>
        Task<IReadOnlyList<PeriodeVacancesBrute>> RecupererAsync(
            string anneeScolaire, CancellationToken ct = default);
    }

    public class CalendrierScolaireApiService : ICalendrierScolaireApiService
    {
        private const string BaseUrl =
            "https://data.education.gouv.fr/api/explore/v2.1/catalog/datasets/fr-en-calendrier-scolaire/records";

        /// <summary>
        /// Le maximum accepté par l'API en une page. 2023-2027 tient large-
        /// ment dans deux pages ; boucler sur `offset` couvre les années où
        /// il y en aurait plus, sans plafond arbitraire de notre part.
        /// </summary>
        private const int TaillePage = 100;

        /// <summary>
        /// La correspondance entre le nom de zone de la source et notre code
        /// interne — voir <see cref="Dal.Entities.Academie.Zone"/>. Tout ce
        /// qui n'y figure pas (Polynésie, Nouvelle-Calédonie, Saint-Pierre-
        /// et-Miquelon, Wallis-et-Futuna) n'est pas une académie au sens du
        /// ministère de l'Éducation nationale — ce sont des collectivités
        /// avec leur propre système éducatif, hors de notre liste des 30
        /// académies — et n'a donc aucun élève Mimia à qui s'appliquer.
        /// </summary>
        private static readonly Dictionary<string, string> ZonesConnues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Zone A"] = "A",
            ["Zone B"] = "B",
            ["Zone C"] = "C",
            ["Corse"] = "CORSE",
            ["Guadeloupe"] = "GUADELOUPE",
            ["Guyane"] = "GUYANE",
            ["Martinique"] = "MARTINIQUE",
            ["Mayotte"] = "MAYOTTE",
            ["Réunion"] = "REUNION",
        };

        private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

        private readonly HttpClient _http;
        private readonly ILogger<CalendrierScolaireApiService> _logger;

        public CalendrierScolaireApiService(HttpClient http, ILogger<CalendrierScolaireApiService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<PeriodeVacancesBrute>> RecupererAsync(
            string anneeScolaire, CancellationToken ct = default)
        {
            // DÉDOUBLONNÉ SUR (zone, libellé, début, fin).
            //
            // La source publie une ligne par académie — huit lignes
            // identiques pour la zone A, une par académie qui la compose.
            // On n'en garde qu'une.
            var vues = new HashSet<(string Zone, string Libelle, DateTime Debut, DateTime Fin)>();
            var resultat = new List<PeriodeVacancesBrute>();

            var offset = 0;
            for (;;)
            {
                var adresse =
                    $"{BaseUrl}?where=annee_scolaire%3D%22{Uri.EscapeDataString(anneeScolaire)}%22"
                    + $"%20and%20population%3D%22-%22"
                    + $"&limit={TaillePage}&offset={offset}";

                using var reponse = await _http.GetAsync(adresse, ct);
                if (!reponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Calendrier scolaire officiel : reponse {Statut} pour {Annee}.",
                        reponse.StatusCode, anneeScolaire);
                    break;
                }

                using var flux = await reponse.Content.ReadAsStreamAsync(ct);
                using var document = await JsonDocument.ParseAsync(flux, cancellationToken: ct);

                if (!document.RootElement.TryGetProperty("results", out var lignes)) break;

                var recues = 0;
                foreach (var ligne in lignes.EnumerateArray())
                {
                    recues++;

                    var zoneSource = Texte(ligne, "zones");
                    if (zoneSource is null || !ZonesConnues.TryGetValue(zoneSource, out var zone)) continue;

                    var libelle = Texte(ligne, "description");
                    var debut = DateParisOuNull(Texte(ligne, "start_date"));
                    var fin = DateParisOuNull(Texte(ligne, "end_date"));

                    if (libelle is null || debut is null || fin is null) continue;

                    // GARDE-FOU CONTRE UNE COQUILLE DE LA SOURCE ELLE-MÊME.
                    //
                    // Relevé en le testant : la ligne « Vacances de Noël » de
                    // la Guadeloupe pour 2026-2027 porte une fin au
                    // 2026-01-04 au lieu du 2027-01-04 — une période qui finit
                    // avant d'avoir commencé. Ce n'est pas une erreur de
                    // notre lecture : les autres zones, lues par le même
                    // code, portent la bonne année. On ne corrige pas la
                    // source à sa place — on écarte juste la ligne
                    // manifestement fausse plutôt que de peindre une
                    // « période » à l'envers sur le calendrier d'un enfant.
                    if (fin.Value < debut.Value)
                    {
                        _logger.LogWarning(
                            "Calendrier scolaire officiel : periode ecartee (fin {Fin:yyyy-MM-dd} "
                            + "avant debut {Debut:yyyy-MM-dd}) — zone {Zone}, {Libelle}, {Annee}.",
                            fin.Value, debut.Value, zone, libelle, anneeScolaire);
                        continue;
                    }

                    var cle = (zone, libelle, debut.Value, fin.Value);
                    if (!vues.Add(cle)) continue;

                    resultat.Add(new PeriodeVacancesBrute(zone, libelle, debut.Value, fin.Value));
                }

                if (recues < TaillePage) break;
                offset += TaillePage;
            }

            return resultat;
        }

        private static string? Texte(JsonElement ligne, string champ) =>
            ligne.TryGetProperty(champ, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;

        /// <summary>
        /// La source publie l'instant en UTC ; converti à l'heure de Paris,
        /// c'est le jour calendaire qui compte pour une case du calendrier —
        /// jamais l'heure.
        /// </summary>
        private static DateTime? DateParisOuNull(string? isoUtc)
        {
            if (isoUtc is null || !DateTimeOffset.TryParse(isoUtc, out var instant)) return null;

            return TimeZoneInfo.ConvertTime(instant, Paris).Date;
        }
    }
}
