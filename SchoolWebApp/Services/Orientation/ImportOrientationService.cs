using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Orientation
{
    /// <summary>
    /// L'IMPORT DE L'OFFRE DES LYCÉES — Camara, le 11/10/2026 : « une tâche
    /// programmée qui ira vérifier les changements et mettra à jour les
    /// connaissances de la conseillère d'orientation ».
    ///
    /// LES SOURCES, TOUTES OFFICIELLES ET OUVERTES
    /// -------------------------------------------
    ///   - Onisep (opendata.onisep.fr, licence ODbL), trois jeux nationaux :
    ///       605340ddc19a9  Idéo — actions de formation initiale, univers lycée
    ///                      (bac général, bacs techno et leurs spécialités, bacs
    ///                      pro, CAP, familles de métiers de 2de pro…) ;
    ///       60113f395cce6  Idéo — enseignements de spécialité de 1re générale ;
    ///       60113c3d5fee0  Idéo — enseignements optionnels et langues de 2de GT.
    ///     LES FICHIERS FIXES, PAS L'API : <c>api.opendata.onisep.fr/downloads/
    ///     &lt;jeu&gt;/&lt;jeu&gt;.json</c>, ceux que référence data.gouv.fr. L'API de
    ///     l'Onisep est limitée à CINQ requêtes par jour sans compte (mesuré le
    ///     11/10/2026 : « Daily-Request-Limit: 5 », puis 429) ; les fichiers
    ///     fixes ne comptent pas dans ce quota.
    ///   - Annuaire de l'éducation (data.education.gouv.fr, Licence Ouverte) :
    ///     voies, sections européenne, internationale, sportive, arts, post-bac.
    ///
    /// CE QUI EST NEUF SE VOIT AVANT DE TÉLÉCHARGER
    /// --------------------------------------------
    /// Chaque fichier Onisep annonce sa date de mise à jour dans l'en-tête
    /// <c>Last-Modified</c>, lu par une requête HEAD, sans rien télécharger.
    /// Si aucune n'a bougé depuis le dernier import réussi, et que celui-ci a
    /// moins de trente jours, on s'arrête là. Les trente jours rafraîchissent
    /// les sections de l'Annuaire, qui change sans prévenir.
    ///
    /// LES GARDE-FOUS
    /// --------------
    ///   - Un téléchargement raté, un fichier vide : rien n'est écrit, l'ancienne
    ///     offre reste.
    ///   - UN CHANGEMENT DE FORMAT ARRÊTE TOUT. L'Onisep a déjà changé la
    ///     structure de ses jeux (avertissement du 20/01/2026). Si une colonne
    ///     attendue manque, on n'écrit rien et le journal le dit.
    ///   - Moins de 3 000 lycées lus (il y en a ~5 600) : on n'écrit rien.
    ///   - L'écriture est une transaction : tout ou rien.
    ///
    /// AUCUN MODÈLE N'EST APPELÉ : téléchargement et écriture en base, zéro jeton.
    /// </summary>
    public interface IImportOrientationService
    {
        /// <summary>Un import tourne-t-il en ce moment ?</summary>
        bool EnCours { get; }

        /// <summary>
        /// Vérifie les sources, et réécrit l'offre si elles ont changé (ou si
        /// <paramref name="forcer"/>). Le passage est inscrit au journal.
        /// </summary>
        Task<PassageImportOrientation> ImporterAsync(bool forcer, CancellationToken ct = default);
    }

    public class ImportOrientationService : IImportOrientationService
    {
        public const string JeuFormations = "605340ddc19a9";
        public const string JeuSpecialites = "60113f395cce6";
        public const string JeuOptions2de = "60113c3d5fee0";

        /// <summary>Le fichier fixe d'un jeu Onisep, hors quota de l'API.</summary>
        private const string FichierOnisep = "https://api.opendata.onisep.fr/downloads/{0}/{0}.json";

        private const string ExportAnnuaire =
            "https://data.education.gouv.fr/api/explore/v2.1/catalog/datasets/fr-en-annuaire-education/exports/json"
            + "?where=type_etablissement%3D%22Lyc%C3%A9e%22%20and%20etat%3D%22OUVERT%22"
            + "&select=identifiant_de_l_etablissement,nom_etablissement,statut_public_prive,nom_commune,code_postal,"
            + "libelle_departement,libelle_academie,latitude,longitude,web,voie_generale,voie_technologique,"
            + "voie_professionnelle,section_europeenne,section_internationale,section_sport,section_arts,"
            + "section_cinema,section_theatre,post_bac,apprentissage";

        /// <summary>Moins de lycées que ça : la lecture est suspecte, on n'écrit rien.</summary>
        public const int MinimumLycees = 3000;

        private static readonly TimeSpan RafraichissementMinimum = TimeSpan.FromDays(30);

        /// <summary>Un seul import à la fois, que ce soit la tâche ou le bouton de l'administration.</summary>
        private static readonly SemaphoreSlim Verrou = new(1, 1);

        // Les colonnes sans lesquelles un jeu est illisible. Si l'une manque, la
        // source a changé de format : on arrête.
        private static readonly string[] ColonnesFormations =
            ["formation_for_libelle", "for_type", "ens_code_uai", "lieu_denseignement_ens_libelle"];

        private static readonly string[] ColonnesSpecialites =
            ["enseignements_de_specialite_de_classe_de_1ere_generale", "uai_lieu_de_cours"];

        private static readonly string[] ColonnesOptions =
            ["enseignements_optionnels_et_langues_de_classe_de_2nde_gt", "uai_lieu_de_cours"];

        private readonly HttpClient _http;
        private readonly IOrientationRepository _depot;
        private readonly ILogger<ImportOrientationService> _logger;

        public ImportOrientationService(
            HttpClient http, IOrientationRepository depot, ILogger<ImportOrientationService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool EnCours => Verrou.CurrentCount == 0;

        public async Task<PassageImportOrientation> ImporterAsync(bool forcer, CancellationToken ct = default)
        {
            if (!await Verrou.WaitAsync(0, ct))
            {
                return new PassageImportOrientation
                {
                    Date = DateTime.UtcNow,
                    Reussi = false,
                    Message = "Un import est déjà en cours.",
                    Lycees = await _depot.CompterAsync(ct),
                };
            }

            var chrono = Stopwatch.StartNew();
            var passage = new PassageImportOrientation { Date = DateTime.UtcNow };

            try
            {
                await ImporterSousVerrouAsync(passage, forcer, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // L'ANCIENNE OFFRE RESTE : rien n'a été écrit, ou la transaction a
                // été annulée. La conseillère continue avec ce qu'elle avait.
                _logger.LogError(ex, "Import de l'offre des lycees en echec.");
                passage.Reussi = false;
                passage.Message = $"Échec : {ex.Message} L'offre précédente est conservée.";
            }
            finally
            {
                passage.DureeSecondes = (int)chrono.Elapsed.TotalSeconds;
                try
                {
                    if (passage.Lycees == 0) passage.Lycees = await _depot.CompterAsync(CancellationToken.None);
                    await _depot.NoterPassageAsync(passage, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible d'inscrire le passage de l'import au journal.");
                }

                Verrou.Release();
            }

            return passage;
        }

        private async Task ImporterSousVerrouAsync(PassageImportOrientation passage, bool forcer, CancellationToken ct)
        {
            // 1. Les dates annoncées par les sources.
            foreach (var jeu in new[] { JeuFormations, JeuSpecialites, JeuOptions2de })
            {
                passage.Sources[jeu] = await DateDuJeuAsync(jeu, ct) ?? "inconnue";
            }

            // 2. Y a-t-il du nouveau ?
            var dernierReussi = (await _depot.DerniersPassagesAsync(20, ct))
                .FirstOrDefault(p => p.Reussi && p.Lycees > 0);

            var dejaEnBase = await _depot.CompterAsync(ct);
            var sourcesInchangees = dernierReussi is not null
                && passage.Sources.All(s => dernierReussi.Sources.TryGetValue(s.Key, out var v) && v == s.Value);
            var recent = dernierReussi is not null && DateTime.UtcNow - dernierReussi.Date < RafraichissementMinimum;

            if (!forcer && dejaEnBase > 0 && sourcesInchangees && recent)
            {
                passage.Reussi = true;
                passage.Lycees = dejaEnBase;
                passage.Message = "Rien de neuf dans les sources : l'offre en base est à jour.";
                return;
            }

            // 3. Les téléchargements.
            var formations = await ExporterOnisepAsync(JeuFormations, ColonnesFormations, ct);
            var specialites = await ExporterOnisepAsync(JeuSpecialites, ColonnesSpecialites, ct);
            var options = await ExporterOnisepAsync(JeuOptions2de, ColonnesOptions, ct);
            var annuaire = await LireAnnuaireAsync(ct);

            // 4. La fusion, puis le contrôle de vraisemblance.
            var lycees = Fusionner(formations, specialites, options, annuaire, DateTime.UtcNow);

            if (lycees.Count < MinimumLycees)
            {
                passage.Reussi = false;
                passage.Lycees = dejaEnBase;
                passage.Message = $"Seulement {lycees.Count} lycées lus (au moins {MinimumLycees} attendus) : "
                    + "rien n'a été écrit, l'offre précédente est conservée.";
                return;
            }

            // 5. L'écriture, tout ou rien.
            passage.Lycees = await _depot.RemplacerToutAsync(lycees, ct);
            passage.Reussi = true;
            passage.Message = $"{passage.Lycees} lycées mis à jour depuis l'Onisep et l'Annuaire de l'éducation.";

            _logger.LogInformation("Offre des lycees importee : {Lycees} lycees.", passage.Lycees);
        }

        /// <summary>La date de mise à jour du fichier d'un jeu Onisep (en-tête Last-Modified).</summary>
        private async Task<string?> DateDuJeuAsync(string jeu, CancellationToken ct)
        {
            using var demande = new HttpRequestMessage(HttpMethod.Head, string.Format(CultureInfo.InvariantCulture, FichierOnisep, jeu));
            using var reponse = await _http.SendAsync(demande, ct);
            reponse.EnsureSuccessStatusCode();
            return reponse.Content.Headers.LastModified?.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)
                ?? reponse.Headers.ETag?.Tag;
        }

        /// <summary>
        /// Le fichier complet d'un jeu Onisep. Les colonnes attendues sont
        /// vérifiées sur la première ligne : une colonne absente veut dire que la
        /// source a changé de format, et l'import s'arrête sans rien écrire.
        /// </summary>
        private async Task<List<Dictionary<string, JsonElement>>> ExporterOnisepAsync(
            string jeu, string[] colonnes, CancellationToken ct)
        {
            var adresse = string.Format(CultureInfo.InvariantCulture, FichierOnisep, jeu);
            using var fichier = await _http.GetAsync(adresse, HttpCompletionOption.ResponseHeadersRead, ct);
            fichier.EnsureSuccessStatusCode();
            await using var flux = await fichier.Content.ReadAsStreamAsync(ct);

            var lignes = await JsonSerializer.DeserializeAsync<List<Dictionary<string, JsonElement>>>(flux, cancellationToken: ct)
                ?? [];

            if (lignes.Count == 0)
            {
                throw new InvalidOperationException($"Le fichier Onisep du jeu {jeu} est vide.");
            }

            var manquantes = colonnes.Where(c => !lignes[0].ContainsKey(c)).ToList();
            if (manquantes.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Le jeu Onisep {jeu} a changé de format (colonnes absentes : {string.Join(", ", manquantes)}).");
            }

            return lignes;
        }


        private async Task<Dictionary<string, Dictionary<string, JsonElement>>> LireAnnuaireAsync(CancellationToken ct)
        {
            using var reponse = await _http.GetAsync(ExportAnnuaire, HttpCompletionOption.ResponseHeadersRead, ct);
            reponse.EnsureSuccessStatusCode();
            await using var flux = await reponse.Content.ReadAsStreamAsync(ct);

            var lignes = await JsonSerializer.DeserializeAsync<List<Dictionary<string, JsonElement>>>(flux, cancellationToken: ct)
                ?? [];

            if (lignes.Count == 0 || !lignes[0].ContainsKey("identifiant_de_l_etablissement"))
            {
                throw new InvalidOperationException("L'Annuaire de l'éducation a changé de format ou est vide.");
            }

            var parUai = new Dictionary<string, Dictionary<string, JsonElement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var ligne in lignes)
            {
                var uai = Texte(ligne, "identifiant_de_l_etablissement");
                if (uai is not null) parUai[uai] = ligne;
            }

            return parUai;
        }

        /// <summary>
        /// UNE LIGNE PAR LYCÉE : les formations de l'Onisep forment la base ; les
        /// spécialités, les options et l'Annuaire s'y ajoutent par code UAI. Un
        /// lycée que l'Onisep ne connaît pas n'est pas gardé : sans formation, il
        /// n'aurait rien à dire à la conseillère.
        /// </summary>
        public static List<OffreLycee> Fusionner(
            IEnumerable<Dictionary<string, JsonElement>> formations,
            IEnumerable<Dictionary<string, JsonElement>> specialites,
            IEnumerable<Dictionary<string, JsonElement>> options,
            IReadOnlyDictionary<string, Dictionary<string, JsonElement>> annuaire,
            DateTime maintenant)
        {
            var lycees = new Dictionary<string, OffreLycee>(StringComparer.OrdinalIgnoreCase);
            var listes = new Dictionary<string, List<(int Rang, string Libelle)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in formations)
            {
                var uai = Texte(f, "ens_code_uai")?.ToUpperInvariant();
                if (uai is null) continue;

                if (!lycees.TryGetValue(uai, out var lycee))
                {
                    lycee = new OffreLycee
                    {
                        Uai = uai,
                        Libelle = Texte(f, "lieu_denseignement_ens_libelle") ?? uai,
                        Statut = Texte(f, "ens_statut"),
                        Commune = Texte(f, "ens_commune"),
                        CodePostal = Texte(f, "ens_code_postal"),
                        Departement = Texte(f, "ens_departement"),
                        Academie = Texte(f, "ens_academie"),
                        Latitude = Nombre(f, "ens_latitude"),
                        Longitude = Nombre(f, "ens_longitude"),
                        SiteWeb = Texte(f, "ens_site_web"),
                        MisAJourLe = maintenant,
                    };
                    lycees[uai] = lycee;
                    listes[uai] = [];
                }

                var type = Texte(f, "for_type") ?? string.Empty;
                var libelle = Texte(f, "formation_for_libelle");
                if (libelle is null || !EstUtile(type, libelle)) continue;

                listes[uai].Add((RangFormation(type, libelle), Abreger(libelle)));
            }

            foreach (var (uai, liste) in listes)
            {
                lycees[uai].Formations = string.Join("\n", liste
                    .DistinctBy(l => l.Libelle, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(l => l.Rang)
                    .ThenBy(l => l.Libelle, StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), true))
                    .Select(l => l.Libelle));
            }

            Rattacher(specialites, "uai_lieu_de_cours", "enseignements_de_specialite_de_classe_de_1ere_generale",
                lycees, (l, v) => l.Specialites = v);
            Rattacher(options, "uai_lieu_de_cours", "enseignements_optionnels_et_langues_de_classe_de_2nde_gt",
                lycees, (l, v) => l.Options2de = v);

            foreach (var lycee in lycees.Values)
            {
                if (!annuaire.TryGetValue(lycee.Uai, out var a)) continue;

                lycee.VoieGenerale = Drapeau(a, "voie_generale");
                lycee.VoieTechnologique = Drapeau(a, "voie_technologique");
                lycee.VoieProfessionnelle = Drapeau(a, "voie_professionnelle");
                lycee.SectionEuropeenne = Drapeau(a, "section_europeenne");
                lycee.SectionInternationale = Drapeau(a, "section_internationale");
                lycee.SectionSportive = Drapeau(a, "section_sport");
                lycee.SectionArts = Drapeau(a, "section_arts") || Drapeau(a, "section_cinema") || Drapeau(a, "section_theatre");
                lycee.PostBac = Drapeau(a, "post_bac");
                lycee.Apprentissage = Drapeau(a, "apprentissage");
                lycee.SiteWeb ??= Texte(a, "web");
                lycee.Latitude ??= Nombre(a, "latitude");
                lycee.Longitude ??= Nombre(a, "longitude");
            }

            return lycees.Values.OrderBy(l => l.Uai, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Une même UAI peut porter plusieurs lignes (plusieurs actions de
        /// formation) : on réunit leurs listes, sans doublon.
        /// </summary>
        private static void Rattacher(
            IEnumerable<Dictionary<string, JsonElement>> lignes, string colonneUai, string colonneListe,
            Dictionary<string, OffreLycee> lycees, Action<OffreLycee, string> poser)
        {
            var parUai = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var ligne in lignes)
            {
                var uai = Texte(ligne, colonneUai)?.ToUpperInvariant();
                var liste = Texte(ligne, colonneListe);
                if (uai is null || liste is null || !lycees.ContainsKey(uai)) continue;

                if (!parUai.TryGetValue(uai, out var elements)) parUai[uai] = elements = [];
                elements.AddRange(liste.Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            foreach (var (uai, elements) in parUai)
            {
                poser(lycees[uai], string.Join(" / ", elements.Distinct(StringComparer.OrdinalIgnoreCase)));
            }
        }

        /// <summary>
        /// LES CLASSES QUI NE DISENT RIEN DE PLUS : « classe de 1re générale »,
        /// « classe de 2de générale et technologique », « classe de 1re STMG »
        /// répètent le bac qui suit. On garde la 2de professionnelle (elle nomme
        /// la famille de métiers) et la 2de spécifique (une entrée à part).
        /// </summary>
        private static bool EstUtile(string type, string libelle)
        {
            if (!type.StartsWith("classe de ", StringComparison.OrdinalIgnoreCase)) return true;

            return type.Contains("2de professionnelle", StringComparison.OrdinalIgnoreCase)
                || type.Contains("2de spécifique", StringComparison.OrdinalIgnoreCase)
                || libelle.Contains("mise à niveau", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>L'ordre de lecture : voie générale, technologique, professionnelle, puis le reste.</summary>
        private static int RangFormation(string type, string libelle)
        {
            if (type.Contains("baccalauréat général", StringComparison.OrdinalIgnoreCase)) return 0;
            if (type.Contains("international ou européen", StringComparison.OrdinalIgnoreCase)
                || type.Contains("binational", StringComparison.OrdinalIgnoreCase)) return 1;
            if (type.Contains("baccalauréat technologique", StringComparison.OrdinalIgnoreCase)) return 2;
            if (type.Contains("2de professionnelle", StringComparison.OrdinalIgnoreCase)) return 3;
            if (type.Contains("baccalauréat professionnel", StringComparison.OrdinalIgnoreCase)) return 4;
            if (type.StartsWith("CAP", StringComparison.OrdinalIgnoreCase)) return 5;
            if (libelle.Contains("métiers d'art", StringComparison.OrdinalIgnoreCase)) return 6;
            return 7;
        }

        /// <summary>
        /// Les libellés de l'Onisep, raccourcis sans rien perdre :
        /// « bac techno STMG sciences et technologies du management et de la
        /// gestion enseignement spécifique mercatique (marketing) » devient
        /// « bac techno STMG — mercatique (marketing) ». Le sigle de la série
        /// suffit à la conseillère, qui connaît les séries.
        /// </summary>
        public static string Abreger(string libelle)
        {
            const string Specifique = " enseignement spécifique ";
            var texte = libelle.Trim();

            if (texte.StartsWith("bac techno ", StringComparison.OrdinalIgnoreCase))
            {
                var mots = texte.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var serie = mots.Length > 2 ? mots[2] : string.Empty;
                var i = texte.IndexOf(Specifique, StringComparison.OrdinalIgnoreCase);
                return i >= 0
                    ? $"bac techno {serie} — {texte[(i + Specifique.Length)..]}"
                    : $"bac techno {serie}";
            }

            return texte;
        }

        private static string? Texte(IReadOnlyDictionary<string, JsonElement> ligne, string cle)
        {
            if (!ligne.TryGetValue(cle, out var v)) return null;

            var texte = v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                _ => null,
            };

            return string.IsNullOrWhiteSpace(texte) ? null : texte.Trim();
        }

        private static double? Nombre(IReadOnlyDictionary<string, JsonElement> ligne, string cle)
        {
            if (!ligne.TryGetValue(cle, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
            if (v.ValueKind == JsonValueKind.String
                && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s)) return s;
            return null;
        }

        private static bool Drapeau(IReadOnlyDictionary<string, JsonElement> ligne, string cle) =>
            Texte(ligne, cle) is "1" or "true" or "True" or "oui" or "OUI";
    }
}
