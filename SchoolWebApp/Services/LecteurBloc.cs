using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Analyse des blocs `clé: valeur` que le professeur écrit dans ses
    /// messages — évaluation, rapport de séance.
    ///
    /// Mis en commun plutôt que recopié : la tolérance de lecture est la partie
    /// délicate — clés accentuées, deux-points dans une valeur, ligne de
    /// continuation, « 11/20 » au lieu de « 11 ». Deux copies de ces règles
    /// finiraient par diverger, et un des deux blocs cesserait d'être lu sans
    /// que rien ne le signale.
    /// </summary>
    public static partial class LecteurBloc
    {
        /// <summary>
        /// Capture le premier nombre d'un champ de note. Couvre « 11 »,
        /// « 11/20 », « 11,5 » et « 11.5 » — les quatre formes observées.
        /// </summary>
        [GeneratedRegex(@"(\d{1,2})(?:[.,](\d+))?")]
        private static partial Regex Nombre();

        /// <summary>Format imposé côté prompt : AAAA-MM-JJ.</summary>
        [GeneratedRegex(@"(\d{4})-(\d{1,2})-(\d{1,2})")]
        private static partial Regex DateIso();

        /// <summary>Tolérance JJ/MM/AAAA, au cas où le modèle dérive du format imposé.</summary>
        [GeneratedRegex(@"(\d{1,2})/(\d{1,2})/(\d{4})")]
        private static partial Regex DateFrancaise();

        [GeneratedRegex(@"(\d{1,2})[h:](\d{2})?")]
        private static partial Regex HeureRegex();

        /// <summary>
        /// Découpe un corps de bloc en couples clé/valeur.
        ///
        /// Une ligne sans deux-points est rattachée à la clé précédente : le
        /// modèle passe volontiers à la ligne au milieu d'une remarque longue.
        /// </summary>
        public static Dictionary<string, string> Champs(string corps)
        {
            var champs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? courante = null;

            foreach (var ligne in corps.Split('\n'))
            {
                var propre = ligne.Trim();
                if (propre.Length == 0) continue;

                var separateur = propre.IndexOf(':');

                // Le deux-points doit venir tôt : « Bravo : c'est acquis » est
                // la suite d'une remarque, pas une clé nommée « Bravo ».
                if (separateur > 0 && separateur <= 20)
                {
                    courante = Normaliser(propre[..separateur]);
                    champs[courante] = propre[(separateur + 1)..].Trim();
                    continue;
                }

                if (courante is not null)
                {
                    champs[courante] = (champs[courante] + " " + propre).Trim();
                }
            }

            return champs;
        }

        /// <summary>
        /// Un champ LONG, paragraphes compris — l'observation d'un verdict.
        ///
        /// POURQUOI PAS `Champs`. Pour un champ court, une ligne sans
        /// deux-points se colle à la précédente et les lignes vides sont
        /// ignorées : la justification du professeur arrivait en un seul bloc
        /// (relevé par Camara le 14/09/2026). Pire, un paragraphe ouvert par
        /// « Fonctions : » y devenait une clé « fonctions », et sortait du texte.
        ///
        /// Ici, seules les clés DU BLOC ouvrent ou ferment le champ ; tout le
        /// reste — deux-points compris — en fait partie, et une ligne vide
        /// sépare deux paragraphes, rendus par « \n\n ». `Champs` ne change
        /// pas : les autres blocs gardent exactement leur lecture.
        /// </summary>
        /// <param name="corps">Le corps du bloc, balises retirées.</param>
        /// <param name="cles">Les noms acceptés pour ce champ.</param>
        /// <param name="clesDuBloc">Toutes les clés du bloc, celles du champ comprises.</param>
        public static string? ChampLong(string corps, string[] cles, string[] clesDuBloc)
        {
            var paragraphes = new List<string>();
            var courant = new StringBuilder();
            var dedans = false;

            void Clore()
            {
                if (courant.Length == 0) return;
                paragraphes.Add(courant.ToString());
                courant.Clear();
            }

            foreach (var brute in corps.Split('\n'))
            {
                var ligne = brute.Trim();
                var separateur = ligne.IndexOf(':');
                var cle = separateur > 0 && separateur <= 20 ? Normaliser(ligne[..separateur]) : null;

                if (cle is not null && clesDuBloc.Contains(cle, StringComparer.OrdinalIgnoreCase))
                {
                    // Une AUTRE clé du bloc après le champ le termine : le
                    // professeur a pu écrire l'observation avant le verdict.
                    if (dedans) break;
                    if (!cles.Contains(cle, StringComparer.OrdinalIgnoreCase)) continue;

                    dedans = true;
                    ligne = ligne[(separateur + 1)..].Trim();
                }

                if (!dedans) continue;

                if (ligne.Length == 0)
                {
                    Clore();
                    continue;
                }

                if (courant.Length > 0) courant.Append(' ');
                courant.Append(ligne);
            }

            Clore();

            return paragraphes.Count == 0 ? null : string.Join("\n\n", paragraphes);
        }

        /// <summary>« À revoir », « a-revoir » et « a_revoir » sont la même clé.</summary>
        public static string Normaliser(string cle)
        {
            var sansAccent = string.Concat(cle.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

            return sansAccent.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        }

        /// <summary>Valeur d'un champ, ou null s'il est absent ou vide.</summary>
        public static string? Valeur(Dictionary<string, string> champs, params string[] cles)
        {
            foreach (var cle in cles)
            {
                if (champs.TryGetValue(cle, out var valeur) && !string.IsNullOrWhiteSpace(valeur))
                {
                    return valeur.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Note sur 20 lue dans un champ, bornée. Null quand le champ est
        /// absent, vide, ou ne contient aucun chiffre — ce dernier cas couvre
        /// le « non évalué » qu'écrit le professeur quand il n'a rien à noter.
        /// </summary>
        public static double? Note(Dictionary<string, string> champs, params string[] cles)
        {
            var brut = Valeur(champs, cles);
            if (brut is null) return null;

            var nombre = Nombre().Match(brut);
            if (!nombre.Success) return null;

            var entier = double.Parse(nombre.Groups[1].Value, CultureInfo.InvariantCulture);
            var decimales = nombre.Groups[2].Success
                ? double.Parse("0." + nombre.Groups[2].Value, CultureInfo.InvariantCulture)
                : 0;

            return Math.Clamp(entier + decimales, 0, 20);
        }

        /// <summary>
        /// Date lue dans un champ, au format ISO imposé (AAAA-MM-JJ), avec
        /// une tolérance JJ/MM/AAAA en secours. Null quand le champ est
        /// absent, vide, ou ne contient aucune date reconnaissable — ou
        /// quand les nombres trouvés ne forment pas une date valide (31/02).
        /// </summary>
        public static DateTime? Date(Dictionary<string, string> champs, params string[] cles)
        {
            var brut = Valeur(champs, cles);
            if (brut is null) return null;

            var iso = DateIso().Match(brut);
            if (iso.Success && EssayerDate(iso.Groups[1].Value, iso.Groups[2].Value, iso.Groups[3].Value, out var dateIso))
            {
                return dateIso;
            }

            var francaise = DateFrancaise().Match(brut);
            if (francaise.Success
                && EssayerDate(francaise.Groups[3].Value, francaise.Groups[2].Value, francaise.Groups[1].Value, out var dateFr))
            {
                return dateFr;
            }

            return null;
        }

        private static bool EssayerDate(string annee, string mois, string jour, out DateTime date)
        {
            date = default;

            if (!int.TryParse(annee, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a)
                || !int.TryParse(mois, NumberStyles.Integer, CultureInfo.InvariantCulture, out var m)
                || !int.TryParse(jour, NumberStyles.Integer, CultureInfo.InvariantCulture, out var j))
            {
                return false;
            }

            try
            {
                date = new DateTime(a, m, j);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Heure lue dans un champ (« 14:00 », « 14h », « 14h30 »). Null
        /// quand le champ est absent, vide, ou hors bornes (24h+, 60min+).
        /// </summary>
        public static TimeSpan? Heure(Dictionary<string, string> champs, params string[] cles)
        {
            var brut = Valeur(champs, cles);
            if (brut is null) return null;

            var match = HeureRegex().Match(brut);
            if (!match.Success) return null;

            var heure = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var minute = match.Groups[2].Success
                ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
                : 0;

            if (heure is < 0 or > 23 || minute is < 0 or > 59) return null;

            return new TimeSpan(heure, minute, 0);
        }

        /// <summary>Retire un bloc et ses balises d'un message.</summary>
        public static string Retirer(string? message, Regex bloc, params string[] marqueurs)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;

            var texte = bloc.Replace(message, string.Empty);

            foreach (var marqueur in marqueurs)
            {
                texte = texte.Replace(marqueur, string.Empty);
            }

            return texte.Trim();
        }
    }
}
