using System.Text.Json;

namespace SchoolWebApp.Domain.Services
{
    /// <summary>Une étiquette de la planche, et l'endroit qu'elle désigne.</summary>
    public record Repere(string Mot, double X, double Y);

    /// <summary>
    /// Ce qui répond à « l'élève a cliqué là, qu'est-ce que c'est ? ».
    ///
    /// POURQUOI LE SERVEUR TRANCHE PLUTÔT QUE LE PROFESSEUR
    /// ---------------------------------------------------
    /// La question était posée au modèle : voici l'image, voici la marque, dis
    /// ce qu'il y a dessous. Six versions de la marque ont été essayées — cercle
    /// fin, cercle épais, cercle cerclé de noir, agrandissement de la zone,
    /// épingle — et il a nommé un voisin à chaque fois, parfois très loin :
    /// l'anus annoncé « prostate » alors que l'épingle était au bon endroit.
    ///
    /// Ce n'était pas un défaut d'image. Un modèle de vision lit très bien du
    /// texte et situe très mal un point dans l'espace. On n'améliore pas ça avec
    /// une plus belle figure — on arrête de le lui demander.
    ///
    /// Ici, c'est une distance entre deux points. Le même clic donne la même
    /// réponse, sur toutes les planches, dans toutes les matières, indéfiniment.
    /// </summary>
    public static class CarteReperes
    {
        /// <summary>
        /// Au-delà de cette distance, on ne nomme rien.
        ///
        /// Un clic sur une marge blanche ou entre deux organes ne désigne pas
        /// l'étiquette la moins lointaine : il ne désigne rien. Annoncer
        /// « prostate » pour un clic à l'autre bout de la planche parce que
        /// c'est la moins éloignée serait exactement l'erreur qu'on corrige.
        ///
        /// 0,14 en fraction de la diagonale : environ un septième de la figure,
        /// mesuré large parce qu'une étiquette désigne une ZONE, pas un point.
        /// </summary>
        private const double PortéeMax = 0.14;

        /// <summary>
        /// LA CASSE NE COMPTE PAS, ET C'EST INDISPENSABLE.
        ///
        /// La consigne demande `{"mot":…,"x":…,"y":…}` en minuscules, et c'est
        /// bien ce que le modèle rend. `System.Text.Json` est sensible à la
        /// casse par défaut : rien ne se liait à `Mot`, `X`, `Y`, chaque carte
        /// se relisait VIDE, et le worker la jetait en silence — puis la
        /// redemandait deux minutes plus tard, indéfiniment.
        ///
        /// Aucune erreur nulle part : ni exception, ni journal, ni colonne
        /// remplie. Simplement zéro planche cartographiée, sans explication.
        /// </summary>
        private static readonly JsonSerializerOptions Souple = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// L'étiquette la plus proche du point, ou null si le clic ne tombe près
        /// d'aucune.
        /// </summary>
        public static Repere? PlusProche(string? json, double x, double y)
        {
            // Pas de tri : une planche porte trente étiquettes au plus, et on ne
            // garde que la meilleure. Un parcours suffit.
            Repere? meilleur = null;
            var meilleure = double.MaxValue;

            foreach (var repere in Lire(json))
            {
                var dx = repere.X - x;
                var dy = repere.Y - y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < meilleure)
                {
                    meilleure = distance;
                    meilleur = repere;
                }
            }

            return meilleure <= PortéeMax ? meilleur : null;
        }

        /// <summary>
        /// Les repères d'une planche, ou rien si le JSON est absent ou illisible.
        ///
        /// UNE CARTE ILLISIBLE N'EST PAS UNE PANNE. Elle vient d'un modèle, donc
        /// elle peut être malformée ; dans ce cas le professeur retombe sur
        /// l'ancien comportement — il regarde l'image et se débrouille. Dégradé,
        /// jamais cassé, et surtout jamais une exception au milieu d'un cours.
        /// </summary>
        public static IReadOnlyList<Repere> Lire(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<Repere>();

            try
            {
                var brut = JsonSerializer.Deserialize<List<RepereJson>>(json, Souple);
                if (brut is null) return Array.Empty<Repere>();

                return brut
                    .Where(r => !string.IsNullOrWhiteSpace(r.Mot))
                    // Hors cadre : le modèle a rendu autre chose que des
                    // fractions, ou s'est trompé de repère. On écarte plutôt que
                    // de border — une valeur aberrante bornée reste aberrante,
                    // et elle deviendrait la « plus proche » d'un bord.
                    .Where(r => r.X is >= 0 and <= 1 && r.Y is >= 0 and <= 1)
                    .Select(r => new Repere(r.Mot!.Trim(), r.X, r.Y))
                    .ToList();
            }
            catch (JsonException)
            {
                return Array.Empty<Repere>();
            }
        }

        private sealed class RepereJson
        {
            public string? Mot { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }
    }
}
