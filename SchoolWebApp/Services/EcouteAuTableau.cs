using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// PENDANT UNE COMPRÉHENSION ORALE, LE TEXTE LU NE S'ÉCRIT PAS AU TABLEAU —
    /// Camara, le 04/10/2026 : « sur un exercice de compréhension orale, la prof
    /// ne doit pas écrire le texte au tableau, sinon l'enfant ne s'entraîne pas
    /// correctement ».
    ///
    /// CE QUI S'EST PASSÉ. Marine lit l'histoire de Tom ; l'élève dit « je
    /// t'entends plus » ; elle répond « je te réécris le texte au tableau pour
    /// qu'on reparte bien », et pose le passage ENTIER dans un bloc [ARDOISE],
    /// avant de le relire. L'exercice d'écoute est devenu un exercice de
    /// lecture. Deux tours plus loin, elle confirme : « le texte est au
    /// tableau ».
    ///
    /// LA RÈGLE EXISTAIT, EN TOUTES LETTRES, dans la consigne de l'exercice :
    /// « tant que l'élève n'a pas répondu à ta question, le tableau ne montre
    /// RIEN du texte lu, quoi qu'il demande et quoi qu'il insiste ». Elle n'a
    /// pas tenu devant un élève qui dit ne plus entendre. Une garantie d'usage
    /// ne se demande pas, elle s'impose : c'est le serveur qui retire le bloc.
    ///
    /// CE QUI EST « OUVERT ». Un passage lu entre balises de langue
    /// ([EN]…[/EN], [ES], [DE], [IT], [ZH], [FR]) ouvre une écoute ; le bloc
    /// [COMPREHENSION_ORALE] — l'archive, écrite une fois que l'élève a rendu
    /// ce qu'il a compris — ou [COMPREHENSION_SUPPRIMEE] la referme. Une séance
    /// qui s'arrête sans archive laisse l'écoute ouverte : c'est voulu, elle
    /// reprend au cours suivant.
    ///
    /// CE QUI EST RETIRÉ. Un bloc [ARDOISE] d'au moins huit mots dont six sur
    /// dix sont des mots d'un passage ouvert. Une liste de vocabulaire avec
    /// ses traductions n'y ressemble pas et passe.
    ///
    /// DANS LE MESSAGE D'ARCHIVE, LE TEXTE EST PERMIS : l'exercice est rendu,
    /// le montrer sert la correction.
    /// </summary>
    public static partial class EcouteAuTableau
    {
        private const string Ouverture = "[ARDOISE]";
        private const string Fermeture = "[/ARDOISE]";
        private const int MotsMinimum = 8;
        private const double PartCommune = 0.6;

        [GeneratedRegex(@"\[(?<l>EN|FR|ES|DE|IT|ZH|AR)\](?<passage>[\s\S]*?)\[/\k<l>\]")]
        private static partial Regex Passage();

        [GeneratedRegex(@"\[ARDOISE\](?<corps>[\s\S]*?)\[/ARDOISE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"\[COMPREHENSION_(?:ORALE|SUPPRIMEE)\]", RegexOptions.IgnoreCase)]
        private static partial Regex Cloture();

        [GeneratedRegex(@"[^\p{L}\p{N}]+")]
        private static partial Regex PasUneLettre();

        /// <summary>Le fait joint au tour tant qu'une écoute est ouverte.</summary>
        public const string Rappel =
            "[COMPRÉHENSION ORALE OUVERTE, pas encore archivée. Tant que tu n'as pas écrit le bloc "
            + "[COMPREHENSION_ORALE], le texte lu ne s'écrit PAS au tableau — l'application retire "
            + "tout bloc [ARDOISE] qui le reproduit, et le tableau ne le montre donc pas : ne dis "
            + "jamais qu'il y est. S'il dit qu'il n'a pas entendu, relis le passage entre ses "
            + "balises, et rien d'autre.]";

        private static HashSet<string> Mots(string? texte) =>
            MotsDansLOrdre(texte).ToHashSet(StringComparer.Ordinal);

        private static List<string> MotsDansLOrdre(string? texte)
        {
            var sans = new string((texte ?? string.Empty)
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray())
                .ToLowerInvariant();

            return PasUneLettre().Split(sans).Where(m => m.Length >= 2).ToList();
        }

        /// <summary>Les passages lus dans un message, tels quels.</summary>
        public static IEnumerable<string> PassagesDe(string? texte) =>
            string.IsNullOrEmpty(texte)
                ? []
                : Passage().Matches(texte).Select(m => m.Groups["passage"].Value).Where(p => !string.IsNullOrWhiteSpace(p));

        /// <summary>Ce tableau reproduit-il le texte d'une écoute ouverte ?</summary>
        public static bool Reproduit(string? ardoise, IEnumerable<string> passages)
        {
            var mots = MotsDansLOrdre(ardoise);
            if (mots.Count < MotsMinimum) return false;

            return passages
                .Select(Mots)
                .Any(lu => mots.Count(lu.Contains) >= mots.Count * PartCommune);
        }

        /// <summary>
        /// Avance d'un message du professeur : le rend sans les tableaux qui
        /// reproduisent une écoute ouverte, et met <paramref name="ouvertes"/> à
        /// jour — les passages qu'il lit s'y ajoutent, une archive les referme.
        /// Sert à l'historique relu comme au message qu'on enregistre.
        /// </summary>
        public static string Avancer(List<string> ouvertes, string? contenu)
        {
            if (string.IsNullOrEmpty(contenu)) return contenu ?? string.Empty;

            var cloture = Cloture().Matches(contenu).LastOrDefault();

            if (cloture is not null)
            {
                // L'exercice est rendu DANS ce message : ses tableaux sont
                // permis. Ne restent ouverts que les passages lus APRÈS.
                ouvertes.Clear();
                ouvertes.AddRange(PassagesDe(contenu[cloture.Index..]));
                return contenu;
            }

            var ici = PassagesDe(contenu).ToList();
            var candidats = ouvertes.Concat(ici).ToList();

            var assaini = candidats.Count == 0
                ? contenu
                : Ardoise().Replace(contenu, m => Reproduit(m.Groups["corps"].Value, candidats) ? string.Empty : m.Value);

            ouvertes.AddRange(ici);
            return assaini;
        }

        /// <summary>
        /// LE MÊME RETRAIT, DANS LE FLUX — l'élève ne doit jamais voir le texte,
        /// pas même une seconde. Le texte passe tel quel jusqu'à l'ouverture d'un
        /// bloc [ARDOISE] ; le bloc est retenu jusqu'à sa fermeture, puis rendu
        /// ou retiré. N'est créé que lorsqu'une écoute est ouverte : en dehors,
        /// rien n'est retenu ni retardé.
        ///
        /// Un tableau retiré n'est pas perdu si le message se révèle être
        /// l'archive de l'exercice : il est alors rendu à la fin.
        /// </summary>
        public sealed class Filtre(IReadOnlyList<string> ouvertes)
        {
            private readonly StringBuilder _attente = new();
            private readonly StringBuilder _toutVu = new();
            private readonly List<string> _retires = [];
            private bool _dansArdoise;

            public string Passer(string? fragment)
            {
                if (string.IsNullOrEmpty(fragment)) return string.Empty;

                _attente.Append(fragment);
                _toutVu.Append(fragment);

                var sortie = new StringBuilder();

                for (;;)
                {
                    var texte = _attente.ToString();

                    if (_dansArdoise)
                    {
                        var fin = texte.IndexOf(Fermeture, StringComparison.OrdinalIgnoreCase);
                        if (fin < 0) break; // le bloc n'est pas fini : on attend

                        var bloc = texte[..(fin + Fermeture.Length)];
                        var corps = bloc[Ouverture.Length..^Fermeture.Length];

                        if (Reproduit(corps, ouvertes.Concat(PassagesDe(_toutVu.ToString())))) _retires.Add(bloc);
                        else sortie.Append(bloc);

                        _attente.Remove(0, bloc.Length);
                        _dansArdoise = false;
                        continue;
                    }

                    var debut = texte.IndexOf(Ouverture, StringComparison.OrdinalIgnoreCase);

                    if (debut >= 0)
                    {
                        sortie.Append(texte, 0, debut);
                        _attente.Remove(0, debut);
                        _dansArdoise = true;
                        continue;
                    }

                    // Pas de bloc : tout part, sauf une fin qui pourrait être le
                    // début de « [ARDOISE] » coupé entre deux paquets.
                    var garde = DebutDeBalise(texte);
                    sortie.Append(texte, 0, texte.Length - garde);
                    _attente.Remove(0, texte.Length - garde);
                    break;
                }

                return sortie.ToString();
            }

            /// <summary>Ce qui restait en attente, à la fin du flux.</summary>
            public string Terminer()
            {
                var reste = _attente.ToString();
                _attente.Clear();

                // Un bloc jamais refermé : jugé sur ce qu'on en a.
                if (_dansArdoise && Reproduit(reste, ouvertes.Concat(PassagesDe(_toutVu.ToString()))))
                {
                    _retires.Add(reste);
                    reste = string.Empty;
                }

                // Le message était l'archive de l'exercice : le texte est permis.
                if (_retires.Count > 0 && Cloture().IsMatch(_toutVu.ToString()))
                {
                    reste += "\n" + string.Join("\n", _retires);
                }

                return reste;
            }

            /// <summary>Longueur de la fin de <paramref name="texte"/> qui commence « [ARDOISE] ».</summary>
            private static int DebutDeBalise(string texte)
            {
                for (var n = Math.Min(Ouverture.Length - 1, texte.Length); n > 0; n--)
                {
                    if (Ouverture.StartsWith(texte[^n..], StringComparison.OrdinalIgnoreCase)) return n;
                }

                return 0;
            }
        }
    }
}
