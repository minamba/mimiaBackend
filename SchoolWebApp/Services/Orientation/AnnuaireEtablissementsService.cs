using System.Globalization;
using System.Text.Json;

namespace SchoolWebApp.Api.Services.Orientation
{
    /// <summary>Un collège ou un lycée de l'Annuaire de l'éducation.</summary>
    public record EtablissementTrouve(
        string Uai, string Nom, string Type, string? Commune, string? CodePostal,
        string? Statut, double? Latitude, double? Longitude);

    /// <summary>
    /// CHERCHER L'ÉTABLISSEMENT DE L'ÉLÈVE — le parent tape un nom ou une ville,
    /// on interroge l'Annuaire de l'éducation (data.education.gouv.fr, Licence
    /// Ouverte, sans clé). Collèges et lycées seulement : la conseillère ne sert
    /// qu'à partir de la 3e.
    ///
    /// PAR LE SERVEUR, PAS PAR LE NAVIGATEUR : une seule adresse à surveiller si
    /// la source change, et le navigateur du parent ne parle qu'à Mimia.
    /// </summary>
    public interface IAnnuaireEtablissementsService
    {
        Task<IReadOnlyList<EtablissementTrouve>> ChercherAsync(string texte, CancellationToken ct = default);

        Task<EtablissementTrouve?> GetParUaiAsync(string uai, CancellationToken ct = default);
    }

    public class AnnuaireEtablissementsService : IAnnuaireEtablissementsService
    {
        private const string Base =
            "https://data.education.gouv.fr/api/explore/v2.1/catalog/datasets/fr-en-annuaire-education/records";

        private const string Champs =
            "identifiant_de_l_etablissement,nom_etablissement,type_etablissement,nom_commune,code_postal,"
            + "statut_public_prive,latitude,longitude";

        private readonly HttpClient _http;

        public AnnuaireEtablissementsService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<IReadOnlyList<EtablissementTrouve>> ChercherAsync(string texte, CancellationToken ct = default)
        {
            var mots = Nettoyer(texte);
            if (mots.Length < 3) return [];

            // Chaque mot doit se trouver dans le nom OU la commune : « mare carrée
            // moissy » trouve le lycée de la Mare Carrée à Moissy-Cramayel.
            var conditions = mots
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(5)
                .Select(m => $"(search(nom_etablissement, \"{m}\") or search(nom_commune, \"{m}\") or code_postal like \"{m}*\")");

            var ou = string.Join(" and ", conditions)
                + " and (type_etablissement=\"Collège\" or type_etablissement=\"Lycée\") and etat=\"OUVERT\"";

            var url = $"{Base}?select={Champs}&where={Uri.EscapeDataString(ou)}&limit=15";
            return await LireAsync(url, ct);
        }

        public async Task<EtablissementTrouve?> GetParUaiAsync(string uai, CancellationToken ct = default)
        {
            var code = Nettoyer(uai).Replace(" ", string.Empty).ToUpperInvariant();
            if (code.Length is < 7 or > 10) return null;

            var url = $"{Base}?select={Champs}&where={Uri.EscapeDataString($"identifiant_de_l_etablissement=\"{code}\"")}&limit=1";
            return (await LireAsync(url, ct)).FirstOrDefault();
        }

        private async Task<IReadOnlyList<EtablissementTrouve>> LireAsync(string url, CancellationToken ct)
        {
            using var reponse = await _http.GetAsync(url, ct);
            reponse.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct));

            if (!doc.RootElement.TryGetProperty("results", out var resultats)) return [];

            return resultats.EnumerateArray()
                .Select(r => new EtablissementTrouve(
                    Texte(r, "identifiant_de_l_etablissement") ?? string.Empty,
                    Texte(r, "nom_etablissement") ?? string.Empty,
                    Texte(r, "type_etablissement") ?? string.Empty,
                    Texte(r, "nom_commune"),
                    Texte(r, "code_postal"),
                    Texte(r, "statut_public_prive"),
                    Nombre(r, "latitude"),
                    Nombre(r, "longitude")))
                .Where(e => e.Uai.Length > 0)
                .ToList();
        }

        /// <summary>
        /// Seulement des lettres, des chiffres et des espaces : le texte du parent
        /// entre dans une requête de la source, et rien ne doit pouvoir la
        /// détourner (guillemets, opérateurs).
        /// </summary>
        public static string Nettoyer(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return string.Empty;

            var garde = new string(texte
                .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
                .ToArray());

            return string.Join(' ', garde.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        private static string? Texte(JsonElement e, string cle) =>
            e.TryGetProperty(cle, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : e.TryGetProperty(cle, out var n) && n.ValueKind == JsonValueKind.Number ? n.GetRawText() : null;

        private static double? Nombre(JsonElement e, string cle)
        {
            if (!e.TryGetProperty(cle, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
            if (v.ValueKind == JsonValueKind.String
                && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s)) return s;
            return null;
        }
    }
}
