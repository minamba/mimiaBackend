using System.Globalization;
using System.Text;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Orientation
{
    /// <summary>
    /// CE QUE LA CONSEILLÈRE SAIT DE L'ÉLÈVE ET DES LYCÉES AUTOUR DE LUI — à
    /// chaque séance, dans le bloc propre à l'élève (mis en cache, relu au
    /// dixième du prix).
    ///
    /// DES FAITS, PAS UNE CONSIGNE. Le cadre national et la manière de
    /// conseiller sont dans <c>PromptsOrientation</c> ; ici, seulement ce qui
    /// change d'un élève à l'autre : sa classe, son établissement, l'offre réelle
    /// de son lycée et de ceux qui l'entourent, avec la date de la source.
    ///
    /// UN BUDGET : le lycée de l'élève en entier, puis au plus
    /// <see cref="NombreProches"/> lycées dans <see cref="RayonKm"/> km, chacun
    /// résumé. ~2 000 à 3 000 jetons dans une zone dense, beaucoup moins à la
    /// campagne. Mesuré sur l'export du 11/10/2026 : un lycée tient en ~100
    /// jetons en médiane.
    /// </summary>
    public static class ContexteOrientation
    {
        public const int NombreProches = 10;
        public const double RayonKm = 25;

        /// <summary>Au-delà, une liste de formations est coupée (les très gros lycées).</summary>
        private const int FormationsMax = 1_800;

        public static async Task<string> ConstruireAsync(
            Eleve eleve, IOrientationRepository offres, CancellationToken ct)
        {
            var texte = new StringBuilder();
            texte.AppendLine("# Ce que tu sais de l'élève et des lycées autour de lui");
            texte.AppendLine();
            texte.AppendLine($"Classe : {eleve.NiveauLibelle ?? "inconnue"}.");

            var choix = VoiesScolaires.SansChoixPro(eleve.Specialites);
            if (choix.Count > 0)
            {
                texte.AppendLine($"Spécialités ou langues cochées par sa famille : {string.Join(", ", choix)}.");
            }

            // Sa filière professionnelle, en toutes lettres (15/10/2026).
            if (VoiesScolaires.DecrireFilierePro(eleve.Specialites) is { } filiere)
            {
                texte.AppendLine($"Filière : {filiere}.");
            }

            if (string.IsNullOrWhiteSpace(eleve.EtablissementUai))
            {
                texte.AppendLine();
                texte.AppendLine("""
                    Son établissement N'EST PAS RENSEIGNÉ. Tu ne connais donc ni son
                    lycée ni les lycées proches de chez lui. Tu peux conseiller sur le
                    cadre national, mais pour parler d'un lycée précis, propose-lui de
                    demander à son parent d'indiquer son collège ou son lycée dans son
                    profil (Mes enfants › modifier le profil). N'invente jamais l'offre
                    d'un lycée.
                    """);
                return texte.ToString();
            }

            texte.AppendLine($"Établissement : {eleve.EtablissementLibelle} ({eleve.EtablissementCommune}), code {eleve.EtablissementUai}.");

            var sien = await offres.GetParUaiAsync(eleve.EtablissementUai, ct);
            var proches = eleve.EtablissementLatitude is { } lat && eleve.EtablissementLongitude is { } lon
                ? await offres.ProchesAsync(lat, lon, NombreProches + 1, RayonKm, ct)
                : [];

            if (sien is null && proches.Count == 0)
            {
                texte.AppendLine();
                texte.AppendLine("""
                    L'offre des lycées n'est pas encore disponible (import pas encore
                    fait, ou aucun lycée connu à moins de 25 km). Conseille sur le cadre
                    national, et renvoie vers onisep.fr et son professeur principal pour
                    l'offre des lycées. N'invente jamais l'offre d'un lycée.
                    """);
                return texte.ToString();
            }

            var dateSource = (sien ?? proches[0].Lycee).MisAJourLe;
            texte.AppendLine();
            texte.AppendLine($"""
                SOURCES : Onisep (onisep.fr) et Annuaire de l'éducation, lues le
                {dateSource.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("fr-FR"))}.
                Ce sont les données officielles, mais un lycée peut ouvrir ou fermer une
                option d'une année sur l'autre : pour une décision, l'élève vérifie
                sur le site du lycée ou aux portes ouvertes.
                """);

            if (sien is not null)
            {
                texte.AppendLine();
                texte.AppendLine("## Son lycée");
                Decrire(texte, sien, null, complet: true);
            }

            var autres = proches.Where(p => sien is null || p.Lycee.Uai != sien.Uai).Take(NombreProches).ToList();
            if (autres.Count > 0)
            {
                texte.AppendLine();
                texte.AppendLine(sien is null
                    ? $"## Les lycées les plus proches de son établissement (moins de {RayonKm} km)"
                    : $"## Les autres lycées proches (moins de {RayonKm} km)");

                foreach (var proche in autres) Decrire(texte, proche.Lycee, proche.DistanceKm, complet: false);
            }

            return texte.ToString();
        }

        private static void Decrire(StringBuilder texte, OffreLycee l, double? distance, bool complet)
        {
            var titre = new StringBuilder($"- **{l.Libelle}**, {l.Commune}");
            if (distance is { } km) titre.Append(CultureInfo.InvariantCulture, $" ({km:0.#} km)");
            if (!string.IsNullOrWhiteSpace(l.Statut)) titre.Append($", {l.Statut}");
            texte.AppendLine(titre.ToString());

            var voies = new List<string>();
            if (l.VoieGenerale) voies.Add("générale");
            if (l.VoieTechnologique) voies.Add("technologique");
            if (l.VoieProfessionnelle) voies.Add("professionnelle");
            if (voies.Count > 0) texte.AppendLine($"  Voies : {string.Join(", ", voies)}.");

            var plus = new List<string>();
            if (l.SectionEuropeenne) plus.Add("section européenne");
            if (l.SectionInternationale) plus.Add("section internationale");
            if (l.SectionSportive) plus.Add("section sportive");
            if (l.SectionArts) plus.Add("section artistique");
            if (l.PostBac) plus.Add("formations après le bac");
            if (l.Apprentissage) plus.Add("apprentissage");
            if (plus.Count > 0) texte.AppendLine($"  Aussi : {string.Join(", ", plus)}.");

            if (!string.IsNullOrWhiteSpace(l.Specialites))
            {
                texte.AppendLine($"  Spécialités de 1re générale : {l.Specialites}.");
            }

            if (complet && !string.IsNullOrWhiteSpace(l.Options2de))
            {
                texte.AppendLine($"  Options et langues de 2de : {l.Options2de}.");
            }

            if (!string.IsNullOrWhiteSpace(l.Formations))
            {
                var formations = l.Formations.Replace("\n", " ; ");
                if (!complet && formations.Length > FormationsMax) formations = formations[..FormationsMax] + " (…)";
                texte.AppendLine($"  Formations : {formations}.");
            }

            if (complet && !string.IsNullOrWhiteSpace(l.SiteWeb)) texte.AppendLine($"  Site : {l.SiteWeb}");
        }
    }
}
