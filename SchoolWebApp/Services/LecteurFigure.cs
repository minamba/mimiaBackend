using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LE GARDE-FOU DE LA FIGURE — Camara, le 11/11/2026 : « mets le garde-fou directement pour éviter que le professeur
    /// ne respecte pas bien la syntaxe ».
    ///
    /// Le professeur de maths trace ses figures par un bloc [FIGURE] (une instruction par ligne), que l'ÉCRAN lit et
    /// dessine — voir <c>figureGeo.js</c>. Deux filets ici, sans second lecteur côté serveur (deux lecteurs finiraient
    /// par ne plus dire la même chose) :
    ///
    /// 1. CE QUE L'ÉCRAN N'A PAS SU DESSINER revient au professeur au tour suivant, comme un fait : l'écran le relève
    ///    dans son dernier message et l'envoie avec celui de l'élève. On ne garde QUE des lignes qui figurent mot pour
    ///    mot dans un bloc [FIGURE] de ce message : le champ vient du navigateur, il ne doit pas pouvoir souffler autre
    ///    chose au professeur.
    ///
    /// 2. LA SYNTAXE, RAPPELÉE À CHAQUE TOUR DE GÉOMÉTRIE en maths : une règle lue une fois en haut du prompt ne tient
    ///    pas (voir « une règle de prompt ne vaut pas un fait »).
    /// </summary>
    public static partial class LecteurFigure
    {
        private const int LignesMax = 8;
        private const int LongueurMax = 160;

        /// <summary>Les lignes signalées par l'écran qui sont VRAIMENT dans un [FIGURE] du dernier message du professeur.</summary>
        public static IReadOnlyList<string> Verifiees(IEnumerable<string>? signalees, string? dernierMessageProf)
        {
            if (signalees is null || string.IsNullOrEmpty(dernierMessageProf)) return Array.Empty<string>();

            var blocs = Blocs().Matches(dernierMessageProf).Select(m => m.Groups[1].Value).ToList();
            if (blocs.Count == 0) return Array.Empty<string>();

            return signalees
                .Select(l => l?.Trim() ?? string.Empty)
                .Where(l => l.Length is > 0 and <= LongueurMax)
                .Where(l => blocs.Any(b => b.Contains(l, StringComparison.Ordinal)))
                .Distinct()
                .Take(LignesMax)
                .ToList();
        }

        [GeneratedRegex(@"\[FIGURE\]([\s\S]*?)(?:\[/FIGURE\]|$)")]
        private static partial Regex Blocs();

        /// <summary>Ce cours est-il de maths ?</summary>
        public static bool EnMaths(string? agentSlug) =>
            agentSlug is not null && agentSlug.Contains("math", StringComparison.OrdinalIgnoreCase);

        [GeneratedRegex(
            @"perpendicul|parall[eè]l|\bdroites?\b|demi-droite|\bsegments?\b|triangle|\bcercles?\b|\bfigure|construi|\btrac|sym[ée]tri|\bmilieu|angle droit|[ée]querre|\bcompas\b|quadrilat|rectangle|\bcarr[ée]s?\b|losange|parall[ée]logramme|m[ée]diatrice|bissectrice|thal[eè]s|pythagore",
            RegexOptions.IgnoreCase)]
        private static partial Regex MotsDeGeometrie();

        /// <summary>
        /// Un tour de géométrie : l'élève en parle, ou le dernier message du professeur portait une figure.
        /// </summary>
        public static bool ParleDeGeometrie(string? messageEleve, string? dernierMessageProf) =>
            (dernierMessageProf?.Contains("[FIGURE]", StringComparison.Ordinal) ?? false)
            || (!string.IsNullOrWhiteSpace(messageEleve) && MotsDeGeometrie().IsMatch(messageEleve));

        /// <summary>La syntaxe, en une ligne : ce que l'écran sait tracer.</summary>
        public const string Syntaxe =
            "point A | point A (2 ; 3) | point E sur (d) | segment [AB] | segment [AB] de 5 cm | droite (AB) | droite (AB) : (d) | "
            + "demi-droite [AB) | perpendiculaire à (AB) passant par C : (d) | parallèle à (AB) passant par C : (d') | "
            + "intersection de (d) et (AB) : H | milieu de [AB] : I | cercle de centre O de rayon 3 cm | cercle de centre O passant par A | "
            + "symétrique de M par rapport à (d) : M' | symétrique de M par rapport à O : N | triangle ABC [rectangle en A | isocèle en A | équilatéral] | "
            + "triangle ABC de côtés AB = 5, BC = 4, AC = 3 | rectangle ABCD de longueur 5 et de largeur 3 | carré ABCD de côté 4 | polygone ABCDE";

        /// <summary>Le rappel d'un tour de géométrie.</summary>
        public const string Rappel =
            "[FIGURE DE GÉOMÉTRIE : si tu montres une figure, c'est dans [ARDOISE][FIGURE] … [/FIGURE][/ARDOISE], UNE instruction par ligne, "
            + "SEULEMENT sous ces formes : " + Syntaxe + ". Jamais de figure en caractères (/, \\, -), jamais de texte libre dans le bloc. "
            + "Si l'exercice demande à l'élève de CONSTRUIRE, c'est lui qui construit sur son cahier, puis tu demandes la photo.]";

        /// <summary>Le fait du tour quand l'écran n'a pas tout dessiné.</summary>
        public static string Fait(IReadOnlyList<string> lignes) =>
            "[TA FIGURE N'EST PAS ENTIÈREMENT DESSINÉE : l'écran n'a pas compris "
            + (lignes.Count == 1 ? "cette ligne" : "ces lignes")
            + " de ton dernier bloc [FIGURE] — "
            + string.Join(" ; ", lignes.Select(l => $"« {l} »"))
            + ". L'élève voit donc une figure incomplète. Dans ta réponse, récris TOUT le bloc [ARDOISE][FIGURE] … [/FIGURE][/ARDOISE] "
            + "avec uniquement ces formes : " + Syntaxe + ". Ce qui ne s'exprime pas ainsi, dis-le en mots dans ta bulle. "
            + "N'en parle pas à l'élève : corrige, c'est tout.]";
    }
}
