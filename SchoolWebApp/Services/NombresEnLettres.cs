namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LES NOMBRES DITS EN LETTRES, RENDUS EN CHIFFRES POUR LE PROFESSEUR — Camara, le 11/11/2026, signalement
    /// du 09/11 : un exercice de comptage où l'enfant répond « Soixante et onze. », puis « Soixante-treize. », juste
    /// les deux fois ; le professeur commence par « Pas tout à fait ! Après soixante-douze, on dit soixante-treize »,
    /// puis se reprend : « Oh pardon, en fait tu as raison ». Il plaquait le verdict de ses messages précédents
    /// avant d'avoir comparé.
    ///
    /// La valeur du nombre est un fait que le code connaît : on la lui donne, juste à côté de la réponse, au lieu
    /// de le laisser décomposer « soixante-treize » au fil de sa phrase (voir <c>ReponseChiffree</c>).
    ///
    /// PORTAGE À LA LETTRE de <c>lireNombreEnLettres</c> (front, <c>src/lib/storage/mathsOrales.js</c>) : deux
    /// lectures différentes du même nombre feraient dire au professeur autre chose que ce qu'affiche l'écran.
    /// </summary>
    public static class NombresEnLettres
    {
        private static readonly Dictionary<string, int> Unites = new()
        {
            ["zero"] = 0, ["zéro"] = 0, ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4,
            ["cinq"] = 5, ["six"] = 6, ["sept"] = 7, ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11,
            ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14, ["quinze"] = 15, ["seize"] = 16,
        };

        private static readonly Dictionary<string, int> Dizaines = new()
        {
            ["vingt"] = 20, ["vingts"] = 20, ["trente"] = 30, ["quarante"] = 40, ["cinquante"] = 50, ["soixante"] = 60,
        };

        /// <summary>Un nombre lu dans le message : les mots tels qu'il les a dits, et leur valeur.</summary>
        public sealed record Lu(string Dit, long Valeur);

        private sealed record Mot(string Coeur, string Minuscule, string Fin);

        private static bool EstMotNombre(string m) =>
            Unites.ContainsKey(m) || Dizaines.ContainsKey(m) || m is "cent" or "cents" or "mille" or "et";

        /// <summary>
        /// Les nombres écrits EN LETTRES dans ce message, dans l'ordre. Un « un » ou un « une » seul est écarté :
        /// trop souvent un article (même exclusion que <c>ReponseChiffree</c>). Les chiffres n'y sont pas : le
        /// professeur les lit tels quels.
        /// </summary>
        public static IReadOnlyList<Lu> Lire(string? texte)
        {
            var lus = new List<Lu>();
            if (string.IsNullOrWhiteSpace(texte)) return lus;

            var mots = Decouper(texte);
            var i = 0;

            while (i < mots.Count)
            {
                var nombre = LireNombre(mots, i);
                if (nombre is null)
                {
                    i += 1;
                    continue;
                }

                var dit = string.Join(" ", mots.Skip(i).Take(nombre.Value.Fin - i).Select(m => m.Coeur));
                var seulArticle = nombre.Value.Fin - i == 1 && mots[i].Minuscule is "un" or "une";
                if (!seulArticle) lus.Add(new Lu(dit, nombre.Value.Valeur));

                i = nombre.Value.Fin;
            }

            return lus;
        }

        /// <summary>« « soixante-treize » = 73 », pour les nombres du message ; null s'il n'y en a aucun.</summary>
        public static string? Valeurs(string? texte, int maximum = 6)
        {
            var lus = Lire(texte).DistinctBy(l => l.Dit.ToLowerInvariant()).Take(maximum).ToList();
            return lus.Count == 0 ? null : string.Join(", ", lus.Select(l => $"« {l.Dit} » = {l.Valeur}"));
        }

        /// <summary>Comme le front : la ponctuation collée au mot est mise à part, et ferme le nombre.</summary>
        private static List<Mot> Decouper(string texte)
        {
            var mots = new List<Mot>();

            foreach (var brut in texte.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                var m = System.Text.RegularExpressions.Regex.Match(brut, @"^([«""(]*)(.*?)([.,;:!?»"")]*)$");
                var coeur = m.Groups[2].Value;
                mots.Add(new Mot(coeur, coeur.ToLowerInvariant(), m.Groups[3].Value));
            }

            return mots;
        }

        private static (long Valeur, int Fin)? LireNombre(List<Mot> mots, int i)
        {
            long total = 0;
            long courant = 0;
            var j = i;
            var lu = false;

            while (j < mots.Count)
            {
                // « dix-sept », « quatre-vingt-dix » : les traits d'union séparent des mots.
                var parties = mots[j].Minuscule.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parties.Length == 0 || !parties.All(EstMotNombre)) break;

                // « et » n'est un nombre que dans « vingt et un », « soixante et onze ».
                if (parties.Length == 1 && parties[0] == "et")
                {
                    var suivant = j + 1 < mots.Count ? mots[j + 1].Minuscule : null;
                    if (!lu || suivant is not ("un" or "une" or "onze")) break;
                    j += 1;
                    continue;
                }

                foreach (var p in parties)
                {
                    if (p == "et") continue;

                    if (Unites.TryGetValue(p, out var unite)) courant += unite;
                    else if (p is "vingt" or "vingts")
                    {
                        // « quatre-vingt » : vingt MULTIPLIE ce qui précède.
                        courant = courant > 0 && courant < 20 && courant % 10 != 0 ? courant * 20 : courant + 20;
                    }
                    else if (Dizaines.TryGetValue(p, out var dizaine)) courant += dizaine;
                    else if (p is "cent" or "cents") courant = (courant == 0 ? 1 : courant) * 100;
                    else if (p == "mille")
                    {
                        total += (courant == 0 ? 1 : courant) * 1000;
                        courant = 0;
                    }
                }

                lu = true;
                j += 1;

                // Une ponctuation après le mot referme le nombre (la virgule seule non, comme le front).
                if (mots[j - 1].Fin.Length > 0 && mots[j - 1].Fin != ",") break;
            }

            if (!lu) return null;
            return (total + courant, j);
        }
    }
}
