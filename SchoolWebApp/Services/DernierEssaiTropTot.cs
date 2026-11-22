using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// « DERNIER ESSAI » ANNONCÉ TROP TÔT, RETIRÉ PAR LE CODE — contrôle d'Omar sur le lot groupé (22/11/2026, point 4). La
    /// règle des essais (<see cref="Prompts.PromptsPedagogiques.RegleDesTroisEssais"/>) annonce le dernier essai à la
    /// TROISIÈME erreur. Avec le rang compté par le code (<see cref="CalculAuTableau.RangDeLErreur"/>), le professeur
    /// l'annonçait encore dès la deuxième 1 fois sur 10 au bout en bout (« Ce n'est pas encore ça, Adam, et c'est ton dernier
    /// essai ! »), puis abandonnait la question à la troisième. Aux 1re et 2e erreurs comptées par le code, la mention part
    /// de sa phrase, à l'écran, dans la voix et dans l'historique (même découpage par phrase que <see cref="FelicitationRefusee"/>).
    /// Générique : tous les professeurs, tout calcul ou suite jugé par le code. À la 3e erreur et au-delà, la mention reste ;
    /// seul le compte des erreurs dit à l'élève part (voir <c>Compte</c>), à tous les rangs.
    /// </summary>
    public static partial class DernierEssaiTropTot
    {
        /// <summary>
        /// LE COMPTE DES ERREURS NE SE DIT PAS À L'ÉLÈVE — bout en bout ×10 du lot groupé sur 6d46e37 (conv. de test 1015) :
        /// le professeur a recopié le fait du code, « C'est sa troisième erreur sur cette question. », en tête de sa
        /// réponse. La règle des essais l'interdit (« tu ne lui dis pas combien de fois il s'est trompé ») : à tout tour où
        /// le code a compté, une phrase qui donne ce compte (« sa / ta troisième erreur », « ton deuxième essai ») part.
        /// « C'est ton dernier essai » n'est pas un compte : il reste (à la 3e erreur).
        /// </summary>
        // CONTRÔLE D'OMAR SUR LE LOT GROUPÉ v2 (19abbfd, point 2) : « Pour ton deuxième essai, prends ton temps. » devenait
        // « Pour prends ton temps. », « C'est ta première fois avec les fractions ? » devenait « Avec les fractions ? ». Le
        // compte ne part plus que sous la forme « c'est sa/ta Nième erreur/fois » (« c'est » obligatoire), et seulement s'il
        // ferme sa proposition (ponctuation ou fin de phrase juste après) : suivi d'un complément, rien ne part.
        private const string Compte =
            @"(?:\s*,\s*|\s*[—–]\s*|\s+)?(?:\bet\s+)?\bc['’](?:est|était)\s+(?:déjà\s+|deja\s+)?(?:sa|ta|son|ton)\s+"
            + @"(?:première|premiere|deuxième|deuxieme|seconde|second|troisième|troisieme|quatrième|quatrieme|cinquième|cinquieme|\d{1,2}\s*(?:e|ème|eme))\s+"
            // « C'est sa troisième fois, Adam » (bout en bout ×10 sur e952d87, conv. de test 1029) : « fois » aussi.
            + @"(?:erreur|faute|réponse\s+fausse|essai|tentative|fois)(?:\s+sur\s+(?:cette\s+question|ce\s+calcul|ce\s+nombre))?\b(?-i:\s+\p{Lu}\p{Ll}+)?" + Ferme;

        /// <summary>La proposition se ferme : « ! », « , », « : » consommés, ou une ponctuation / la fin de la phrase juste après.</summary>
        private const string Ferme = @"(?:\s*(?:!+|,|:)(?=\s|$)|(?=\s*(?:[.!?…,;:)—–]|$)))";

        /// <summary>Le rang d'erreur (compté par le code) où l'annonce est trop tôt.</summary>
        public static bool TropTot(int? rang) => rang is 1 or 2;

        /// <summary>
        /// Ce qui part à ce rang : la mention « dernier essai » (et « une dernière fois ») aux 1re et 2e erreurs, le compte à
        /// tous les rangs ; à partir de la 3e, la mention reste UNE fois.
        /// </summary>
        private static Regex Motif(int? rang) => TropTot(rang) ? MentionOuCompte() : CompteOuMentionGardee();

        [GeneratedRegex(Compte + "|" + MentionTexte + "|" + DerniereFoisTexte, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MentionOuCompte();

        /// <summary>
        /// UNE SEULE ANNONCE — contrôle d'Omar sur le lot groupé v2 (point 5) : au 3e essai, « C'est ton dernier essai. … C'est
        /// ton dernier essai : qu'est-ce qui vient… ? » (séries 8 et 9). Le fait dit de l'annoncer une fois : la 1re mention
        /// reste, les suivantes partent (groupe « garder », voir FelicitationRefusee.Options).
        /// </summary>
        [GeneratedRegex(Compte + "|(?<garder>" + MentionTexte + ")", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex CompteOuMentionGardee();

        /// <summary>
        /// « ÉCOUTE UNE DERNIÈRE FOIS » TROP TÔT — contrôle d'Omar sur le lot groupé v2 (point 4, QR2 final : au 1er essai après
        /// le retour, compteur à zéro). Aux rangs trop tôt, « une dernière fois » derrière écoute / essaie / répète / redis part
        /// (« Écoute une dernière fois le son anglais : » → « Écoute le son anglais : »), même dans une phrase qui porte la
        /// balise de voix (hors de la balise). « Comme la dernière fois » reste.
        /// </summary>
        private const string DerniereFoisTexte =
            @"(?<=\b(?:[ÉEée]coute|[ÉEée]coutons|essaie|essaye|essayons|r[ée]essaie|r[ée]p[èe]te|r[ée]p[ée]tons|redis|dis|r[ée]ponds)(?:-(?:le|la|les|moi|nous))?\b[^.!?…\n,;:\[\]]{0,40}?)\s+une\s+derni[eè]re\s+fois\b";

        /// <summary>
        /// Les mêmes retraits dans une phrase qui porte une balise (voix, ardoise), hors de la balise : « [/ARDOISE] C'est ton
        /// dernier essai : qu'est-ce qui vient… ? » (série 8, 2e mention) ; « Écoute une dernière fois le son anglais :
        /// [EN]Blue[/EN]. » (QR2).
        /// </summary>
        private static FelicitationRefusee.Options Options(int? rang) => TropTot(rang)
            ? new FelicitationRefusee.Options { SurBalise = MentionOuCompte() }
            : new FelicitationRefusee.Options { Garder = "garder", SurBalise = CompteOuMentionGardee() };

        /// <summary>
        /// « (Allez,) (c'est) ton dernier essai (pour celui-là) ! » — « pour celui-là » : conv. de test 990.
        /// CONTRÔLE D'OMAR SUR LE LOT GROUPÉ v2 (19abbfd, point 2) : « Lors de ton dernier essai, tu avais trouvé 12. » devenait
        /// « Lors de tu avais trouvé 12. », « Demain, c'est ta dernière chance de réviser… » devenait « Demain de réviser… »,
        /// « C'est presque ton dernier essai, concentre-toi. » devenait « C'est presque concentre-toi. ». Seule l'ANNONCE part :
        /// la mention est en tête de proposition (« c'est … », ou juste après une ponctuation, « et », « allez »), jamais
        /// après « de », « lors de », « pour », « avant », « presque », et elle ferme sa proposition (pas de « de réviser… »).
        /// </summary>
        private const string MentionTexte =
            @"(?:\s*,\s*|\s*[—–]\s*|\s+)?(?:\bet\s+)?(?:\b(?:allez|alors|bon|ok)\s*,?\s*)?"
            + @"(?:(?<!\b(?:de|d['’]|pour|avant|presque)\s+)\bc['’](?:est|était)\s+(?:\b(?:ton|ta|le|un|une)\s+)?"
            + @"|(?<=(?:^|[,;:—–.!?…(\]])\s*(?:\b(?:et|allez|alors|bon|ok)\b\s*,?\s*)*)(?:\b(?:ton|ta|le|un|une)\s+)?)"
            + @"\b(?:dernier\s+essai|derni[eè]re\s+(?:chance|tentative))\b"
            // « C'est ton dernier essai sur ce mot, tu es prêt ? » (accueil du retour, conv. de test 1055) : « sur ce mot » part avec.
            + @"(?:\s+(?:pour|sur)\s+(?:celui-ci|celui-là|celle-ci|celle-là|cette\s+question|ce\s+nombre|ce\s+calcul|cette\s+fois|aujourd['’]hui|ce\s+mot|cette\s+syllabe|cette\s+couleur|ce\s+son))?(?-i:\s+\p{Lu}\p{Ll}+)?"
            + Ferme;

        /// <summary>
        /// LE TOUR D'ARRIVÉE D'UN RETOUR — décision de Min's du 22/11/2026 (07h58) : au retour, le compte des essais repart de
        /// zéro (EssaisRates.FinDeSeance). Le prochain essai est le premier : « c'est ton dernier essai » ne se dit pas à
        /// l'accueil (bout en bout QR1 du lot groupé, conv. de test 1055). Générique : tout exercice, tout professeur.
        /// </summary>
        public static bool AuRetour(TypeAccueil type) =>
            type is TypeAccueil.Retour or TypeAccueil.NouvelleSeance or TypeAccueil.RetourDicteeInterrompue or TypeAccueil.RetourControleAbandonne;

        /// <summary>
        /// APRÈS UN RETOUR, PAS DE « DERNIER ESSAI » À LA PREMIÈRE RÉPONSE — lot groupé, bout en bout QR1 sur c2cac28 (conv. de
        /// test 1089) : l'accueil du retour n'avait pas redit le mot (« On repart sur l'écoute des couleurs ? »), « Bleu! »
        /// n'était donc pas jugé, et le professeur, comptant lui-même les deux erreurs d'AVANT le départ, a dit « C'est ton
        /// dernier essai » au premier essai après le retour. Le compte repart de zéro à la frontière de séance
        /// (<see cref="Voix.EssaisRates.FinDeSeance"/>) : sa première réponse depuis n'est jamais un dernier essai (au plus
        /// une erreur comptée ; l'annonce vient après la 2e en écoute/répétition, après la 3e en calcul, par le code). <paramref name="historique"/> : les messages AVANT celui du tour, dans l'ordre.
        /// </summary>
        public static bool TropTotApresLeRetour(IReadOnlyList<(string? Role, string? Contenu)> historique)
        {
            for (var i = historique.Count - 1; i >= 0; i--)
            {
                if (!Voix.EssaisRates.FinDeSeance(historique[i].Role, historique[i].Contenu)) continue;
                return !historique.Skip(i + 1).Any(m => m.Role == "user");
            }
            return false;
        }

        /// <summary>Un filtre à poser dès que le code a compté une erreur (rang non nul).</summary>
        public static bool AFiltrer(int? rang) => rang is not null;

        public static string Retirer(string? texte, int? rang, out int retraits) => FelicitationRefusee.ParPhrase(texte, Motif(rang), out retraits, options: Options(rang));

        public static string Retirer(string? texte, int? rang) => Retirer(texte, rang, out _);

        public static FelicitationRefusee.Filtre Filtre(int? rang) => new(Motif(rang), Options(rang));

        /// <summary>Rang 2 par défaut (mention et compte) : harnais et preuves.</summary>
        public static string Retirer(string? texte, out int retraits) => Retirer(texte, 2, out retraits);

        public static string Retirer(string? texte) => Retirer(texte, 2, out _);

        public static FelicitationRefusee.Filtre Filtre() => Filtre(2);
    }
}
