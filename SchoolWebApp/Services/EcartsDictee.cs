using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Un écart numéroté, tel que le tableau le montre à l'élève.</summary>
    /// <param name="Numero">Le badge : 1, 2, 3… dans l'ordre du texte.</param>
    /// <param name="Dicte">Ce qui était dicté à cet endroit — vide pour un mot en trop.</param>
    /// <param name="Copie">Ce que l'élève a écrit — vide pour un mot oublié.</param>
    public record EcartDictee(int Numero, string Dicte, string Copie);

    /// <summary>
    /// LES ÉCARTS NUMÉROTÉS DU TABLEAU, RECALCULÉS CÔTÉ SERVEUR — Camara, le
    /// 03/10/2026, en correction de dictée d'anglais.
    ///
    /// Le tableau numérote les erreurs (①, ②, ③…) et le professeur ne voyait
    /// pas ces numéros : il recomptait de tête, par « lignes ». Il a annoncé
    /// trois erreurs sur la première ligne, s'est dédit deux fois, a inventé un
    /// mot — « end qui manque dans weekend », dans un texte sans « weekend » —,
    /// puis, l'élève ayant vu « trois fautes » sur la phrase suivante (③ ④ ⑤),
    /// est revenu à la première. Il ne peut pas parler des numéros qu'on ne lui
    /// donne pas.
    ///
    /// UNE SEULE RÈGLE, ÉCRITE DEUX FOIS — ET À LA LETTRE. C'est la copie fidèle
    /// de `src/lib/storage/diffDictee.js` (`cleMot`, `decouper`, `aligner`,
    /// `numeroter`) : mêmes coûts, même ordre de remontée, mêmes égalités. Un
    /// calcul « équivalent » donnerait un n° 4 au professeur pendant que
    /// l'élève voit un n° 5 — pire que pas de numéro du tout. SI L'UN CHANGE,
    /// L'AUTRE DOIT SUIVRE ; les cas de `ecartsDicteeServeur.test.js` fixent
    /// la sortie attendue des deux côtés.
    ///
    /// Seule la partie qui NUMÉROTE est portée : surligner, relier et poser
    /// les badges sont de l'affichage.
    /// </summary>
    public static partial class EcartsDictee
    {
        /// <summary>
        /// Chinois et japonais : pas d'espaces entre les mots — chaque caractère
        /// est une unité. Mêmes plages que `MORCEAUX` côté front.
        /// </summary>
        [GeneratedRegex(
            @"(\s+)|([぀-ヿ㐀-䶿一-鿿豈-﫿])"
            + @"|([^\s぀-ヿ㐀-䶿一-鿿豈-﫿]+)")]
        private static partial Regex Morceaux();

        [GeneratedRegex(@"^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$")]
        private static partial Regex BordsNonLettres();

        private const double CoutRemplacementLointain = 2.5;

        /// <summary>La forme d'un mot qu'on compare : ni casse, ni ponctuation autour.</summary>
        public static string CleMot(string? mot)
        {
            var cle = (mot ?? string.Empty)
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Replace('’', '\'').Replace('‘', '\'').Replace('`', '\'').Replace('´', '\'')
                .Replace("œ", "oe")
                .Replace("æ", "ae");

            return BordsNonLettres().Replace(cle, string.Empty);
        }

        /// <summary>Les mots d'un texte, dans l'ordre : leur forme écrite et leur clé.</summary>
        private static List<(string Texte, string Cle)> Mots(string? texte)
        {
            var mots = new List<(string, string)>();

            foreach (Match trouve in Morceaux().Matches((texte ?? string.Empty).Normalize(NormalizationForm.FormC)))
            {
                if (trouve.Groups[1].Success) continue;

                var cle = CleMot(trouve.Value);
                if (cle.Length > 0) mots.Add((trouve.Value, cle));
            }

            return mots;
        }

        /// <summary>Distance d'édition entre deux mots, en unités UTF-16 comme en JavaScript.</summary>
        private static int Distance(string a, string b)
        {
            var ligne = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) ligne[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                var diagonale = ligne[0];
                ligne[0] = i;

                for (var j = 1; j <= b.Length; j++)
                {
                    var dessus = ligne[j];
                    ligne[j] = Math.Min(
                        Math.Min(ligne[j] + 1, ligne[j - 1] + 1),
                        diagonale + (a[i - 1] == b[j - 1] ? 0 : 1));
                    diagonale = dessus;
                }
            }

            return ligne[b.Length];
        }

        private static bool Proches(string a, string b) =>
            Distance(a, b) <= Math.Max(1, Math.Max(a.Length, b.Length) / 2);

        private enum TypeOp { Egal, Faute, Oubli, Ajout }

        private sealed class Operation
        {
            public TypeOp Type { get; init; }
            public int A { get; init; } = -1;
            public int B { get; init; } = -1;
            public int Erreur { get; set; }
        }

        private static List<Operation> Aligner(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            var n = a.Count;
            var m = b.Count;

            double Remplacement(int i, int j) =>
                a[i] == b[j] ? 0 : (Proches(a[i], b[j]) ? 1 : CoutRemplacementLointain);

            var cout = new double[n + 1, m + 1];
            for (var i = 0; i <= n; i++) cout[i, 0] = i;
            for (var j = 0; j <= m; j++) cout[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    cout[i, j] = Math.Min(
                        cout[i - 1, j - 1] + Remplacement(i - 1, j - 1),
                        Math.Min(cout[i - 1, j] + 1, cout[i, j - 1] + 1));
                }
            }

            var operations = new List<Operation>();
            var x = n;
            var y = m;

            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0)
                {
                    var prix = Remplacement(x - 1, y - 1);

                    // L'égalité stricte, comme en JavaScript : les coûts sont des
                    // sommes de 0, 1 et 2,5, exactes en virgule flottante.
                    if (prix < CoutRemplacementLointain && cout[x, y] == cout[x - 1, y - 1] + prix)
                    {
                        operations.Add(new Operation { Type = prix == 0 ? TypeOp.Egal : TypeOp.Faute, A = x - 1, B = y - 1 });
                        x--;
                        y--;
                        continue;
                    }
                }

                if (x > 0 && cout[x, y] == cout[x - 1, y] + 1)
                {
                    operations.Add(new Operation { Type = TypeOp.Oubli, A = x - 1 });
                    x--;
                    continue;
                }

                operations.Add(new Operation { Type = TypeOp.Ajout, B = y - 1 });
                y--;
            }

            operations.Reverse();
            return operations;
        }

        private static void Numeroter(List<Operation> operations)
        {
            var numero = 0;
            (int Numero, bool Faute, int Trous)? region = null;

            foreach (var op in operations)
            {
                if (op.Type == TypeOp.Egal)
                {
                    region = null;
                    continue;
                }

                var rejoint = region is { } r && (op.Type == TypeOp.Faute
                    ? !r.Faute && r.Trous <= 1
                    : (r.Faute ? r.Trous == 0 : true));

                if (!rejoint)
                {
                    numero++;
                    region = (numero, false, 0);
                }

                var courante = region!.Value;
                region = op.Type == TypeOp.Faute
                    ? (courante.Numero, true, courante.Trous)
                    : (courante.Numero, courante.Faute, courante.Trous + 1);

                op.Erreur = courante.Numero;
            }
        }

        /// <summary>
        /// Les écarts numérotés, ou null quand la copie ne ressemble pas assez au
        /// texte pour que le tableau les numérote — même seuil que le front :
        /// au moins 30 % des mots dictés retrouvés.
        /// </summary>
        public static IReadOnlyList<EcartDictee>? Calculer(string? dicte, string? copie)
        {
            var motsDicte = Mots(dicte);
            var motsCopie = Mots(copie);

            if (motsDicte.Count == 0 || motsCopie.Count == 0) return null;

            var operations = Aligner(
                motsDicte.Select(m => m.Cle).ToList(),
                motsCopie.Select(m => m.Cle).ToList());

            var communs = operations.Count(op => op.Type == TypeOp.Egal);
            if (communs < Math.Max(1, motsDicte.Count * 0.3)) return null;

            Numeroter(operations);

            return operations
                .Where(op => op.Erreur > 0)
                .GroupBy(op => op.Erreur)
                .OrderBy(g => g.Key)
                .Select(g => new EcartDictee(
                    g.Key,
                    string.Join(' ', g.Where(op => op.A >= 0).Select(op => motsDicte[op.A].Texte)),
                    string.Join(' ', g.Where(op => op.B >= 0).Select(op => motsCopie[op.B].Texte))))
                .ToList();
        }

        /// <summary>
        /// Le fait joint à chaque tour pendant la correction : la liste que
        /// l'élève a sous les yeux, et la façon d'en parler.
        /// </summary>
        public static string? Marqueur(string? dicte, string? copie)
        {
            var ecarts = Calculer(dicte, copie);
            if (ecarts is null || ecarts.Count == 0) return null;

            var lignes = ecarts.Select(e =>
                $"n°{e.Numero} — dictée {(e.Dicte.Length > 0 ? $"« {e.Dicte} »" : ": rien")}"
                + $" / sa copie {(e.Copie.Length > 0 ? $"« {e.Copie} »" : ": rien (mot oublié)")}"
                + (e.Dicte.Length == 0 ? " (mot en trop)" : string.Empty));

            return "[LES ÉCARTS NUMÉROTÉS AU TABLEAU — exactement ce que l'élève voit, "
                + "calculé par l'application :\n"
                + string.Join("\n", lignes) + "\n"
                + $"Il y en a {ecarts.Count}, pas un de plus : ne recompte jamais. Désigne-les par "
                + "leur NUMÉRO (« l'erreur n°3 »), jamais par « la première ligne » ou « la "
                + "deuxième ligne » : le tableau revient à la ligne où il veut, et l'élève ne voit "
                + "pas les mêmes lignes que toi. La ponctuation et les majuscules ne sont pas "
                + "numérotées. L'élève parle à voix haute et un mot étranger peut arriver déformé "
                + "(« end » pour « and ») : rapporte ce qu'il dit à cette liste, et n'invente "
                + "jamais un mot qui n'est pas dans les deux textes.]";
        }
    }
}
