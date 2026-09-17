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
        ///
        /// PLANCHER, ET NON PLUS VALEUR UNIQUE, depuis le 17/09/2026. Voir
        /// <see cref="Portée"/> : sur une carte de France à treize régions, cette
        /// valeur-là refusait des clics tombés en plein milieu de la bonne.
        /// </summary>
        private const double PortéeMin = 0.14;

        /// <summary>
        /// Au-delà, on nommerait n'importe quoi. Une planche à deux étiquettes
        /// aurait sinon une portée qui couvre toute la figure, et le moindre clic
        /// dans une marge recevrait un nom.
        /// </summary>
        private const double PortéeMaxAbsolue = 0.35;

        /// <summary>
        /// LA PORTÉE DÉPEND DE LA DENSITÉ DES ÉTIQUETTES, ET IL LE FAUT.
        ///
        /// 0,14 était réglé sur les planches d'anatomie : trente mots serrés sur
        /// une coupe, où une étiquette désigne un organe de quelques pour cent de
        /// large. Sur une carte de France à treize régions, les mots sont espacés
        /// de près de 0,18 et une région couvre le quart de l'image — mesuré le
        /// 17/09/2026 sur la carte des régions : un clic sur le Pays basque tombe
        /// à 0,20 de « Nouvelle-Aquitaine », un clic vers La Rochelle à 0,16.
        /// Deux clics parfaitement justes, tous les deux sans réponse.
        ///
        /// LA FIGURE PORTE ELLE-MÊME SON ÉCHELLE : l'espacement entre étiquettes
        /// voisines dit la taille des zones. On prend la MÉDIANE des distances au
        /// plus proche voisin — pas la moyenne, qu'une étiquette isolée dans un
        /// coin ferait tripler — et on en garde une fois trois quarts.
        ///
        /// Sur une coupe d'anatomie, la médiane tourne autour de 0,07 : une fois
        /// trois quarts font 0,12, le plancher reprend la main à 0,14 et RIEN NE
        /// CHANGE sur les cinquante planches déjà en base. Sur la carte de France,
        /// elle donne 0,24 (mesuré), et le clic sur le Pays basque reçoit son nom.
        /// </summary>
        private static double Portée(IReadOnlyList<Repere> reperes)
        {
            if (reperes.Count < 3) return PortéeMin;

            var voisins = new List<double>(reperes.Count);

            foreach (var a in reperes)
            {
                var plusProche = double.MaxValue;

                foreach (var b in reperes)
                {
                    if (ReferenceEquals(a, b)) continue;

                    var d = Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
                    if (d < plusProche) plusProche = d;
                }

                // Deux étiquettes au même endroit — ça arrive quand un mot est
                // relevé deux fois — ne disent rien de l'échelle de la figure.
                if (plusProche > 0 && plusProche < double.MaxValue) voisins.Add(plusProche);
            }

            if (voisins.Count == 0) return PortéeMin;

            voisins.Sort();
            var mediane = voisins[voisins.Count / 2];

            return Math.Clamp(mediane * 1.75, PortéeMin, PortéeMaxAbsolue);
        }

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
            var reperes = Lire(json);
            var portee = Portée(reperes);

            // Pas de tri : une planche porte trente étiquettes au plus, et on ne
            // garde que la meilleure. Un parcours suffit.
            Repere? meilleur = null;
            var meilleure = double.MaxValue;

            foreach (var repere in reperes)
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

            return meilleure <= portee ? meilleur : null;
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
