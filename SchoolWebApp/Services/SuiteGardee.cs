using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// PENDANT UNE SUITE NON TROUVÉE, ON NE CHANGE PAS D'EXERCICE — contrôle d'Omar sur le lot groupé v3 (89f010e, 22/11/2026,
    /// point 6). Au bout en bout #53 ×10, 2 séries sur 10 abandonnaient la question à la 3e erreur malgré le fait du code
    /// (<see cref="CalculAuTableau.FaitRangDeLErreur"/>) : « C'est pas grave, Adam, on y reviendra. Là, je te propose un autre
    /// petit exercice : tu veux compter des images… ? » (conv. de test 1173), ou « On passe à autre chose ! Qu'est-ce qui vient
    /// juste avant 10 ? [ARDOISE]9, 10[/ARDOISE] » (conv. de test 1174 : une nouvelle question, sa réponse au tableau). Ce
    /// n'est pas un filtre : le brut et le gardé étaient identiques. Le défaut existait avant la v3 (1 série sur 3 au lot v1,
    /// jusqu'à 2 sur 10 en v2).
    ///
    /// Aux 1re, 2e et 3e erreurs comptées par le code sur une SUITE, la réponse du professeur est retenue en entier (pas de
    /// coupure possible) puis lue : si elle donne la réponse (voir <see cref="ReponseNonDonnee.LaDonne"/>), abandonne la question (« on y reviendra », « autre chose », « un autre exercice »,
    /// « on s'arrête »…) ou en pose une autre (le juge, relisant le message, y lit une autre question : « avant 10 », « 3 + 4 »), elle est
    /// remplacée par un message du code, en entier : jamais de phrase coupée, la question du code à la fin, et aucune ardoise
    /// d'une autre question. Sinon elle part telle quelle. Générique : tous les professeurs, toute suite jugée par le code.
    /// La 4e erreur n'est pas concernée (la règle des essais dit alors de passer à la suite).
    /// </summary>
    public static partial class SuiteGardee
    {
        /// <summary>Les rangs d'erreur où la question ne se quitte pas.</summary>
        public static bool AGarder(int? rang) => rang is >= 1 and <= 3;

        /// <summary>L'abandon dit : « on y reviendra », « plus tard », « on passe à autre chose », « un autre exercice », « on s'arrête »…</summary>
        // CONTRÔLE D'OMAR SUR LE LOT GROUPÉ v4 (7c4803a, point 5) : le motif prenait de bonnes relances pour un abandon — « Après
        // 29, on passe à la dizaine suivante », « On laisse tes doigts parler », « Ce n'est pas autre chose que 80 plus un petit
        // pas », « Mets le 80 de côté un instant », « on fait une pause et on recompte », « comme une autre suite que tu
        // connais ». Resserré (tâche 0000o) : « on passe à » seulement suivi de autre chose / un autre / une autre / la suite ;
        // « autre chose » seulement après passer / faire ; « on laisse » et « de côté » seuls ne comptent plus (« on la laisse »,
        // « on laisse cette question » restent) ; « une autre suite » sauf « que tu connais » ; « on fait une pause » sauf
        // « … et on recompte ».
        //
        // CONTRÔLE D'OMAR SUR LA v5 (12cf7e0, points 1 et 5, émulateur ab5.py) : resserré trop loin, le motif ne voyait plus
        // des abandons francs — « On passe à la question suivante. », « On passe au nombre suivant », « On met cette question de
        // côté », « On verra ça une autre fois », « On reprendra ça demain », « On change d'exercice », « un nouvel exercice » —
        // et prenait encore de bonnes relances : « comme un autre calcul que tu connais », « on fait une pause, on respire, et
        // on recompte », « on le garde en tête », « on laisse ce 80 sur l'ardoise », « on laisse ça, et on recompte ». Ajouté
        // (v6, tâche 0000q) : « à la (question) suivante » / « au (nombre) suivant » (pas « la dizaine suivante ») ; « mettre
        // … de côté » sauf « un instant » derrière ; « une autre fois / plus tard / demain / la prochaine fois » après
        // verra / reprendra / essaiera… ; « on reprendra ça », « on reviendra » ; « on change d'exercice / de jeu » ; « un
        // nouvel exercice » ; « on arrête là » ; « laissons ». Exceptions : « … que tu connais » pour tout « un autre
        // exercice / jeu / calcul / activité / question / suite » ; pause avec incise avant « et (puis) on re… » ; « on le
        // garde en tête » ; « on laisse ce/ça » suivi de « sur l'ardoise », « au tableau » ou « et on re… » ; « Plus tard,
        // quand tu seras grand ». Mesure : 0 changement sur les 260 réponses réelles des séries #53 (v2 à v5).
        [GeneratedRegex(
            @"\bon\s+y\s+reviendra\b"
            + @"|\bon\s+(?:va\s+)?(?:la|le)\s+(?:garde[rz]?|reprendra|laisse[rz]?)\b(?!\s+(?:bien\s+)?(?:en\s+t[êe]te|sur\s+l['’]\s*ardoise|au\s+tableau))"
            + @"|\bplus\s+tard\b(?!\s*,?\s*quand\b)"
            + @"|\bon\s+(?:va\s+)?pass(?:e|er)\s+à\s+(?:autre\s+chose|un\s+autre|une\s+autre|la\s+suite)\b|\bpassons\s+à\s+(?:autre\s+chose|un\s+autre|une\s+autre|la\s+suite)\b"
            + @"|\b(?:on\s+(?:va\s+)?pass(?:e|er)|passons)\s+(?:à\s+la\s+(?:(?:question|activité|leçon|partie|série|consigne)\s+)?suivante|au\s+(?:(?:nombre|calcul|exercice|jeu|mot|problème|défi)\s+)?suivant)\b"
            // « On garde ce nombre pour une autre fois. On continue autre chose maintenant. » (corpus réel v2, 3e essai) : « pour
            // une autre fois » et « continuer autre chose » sont des abandons (« essaie une autre fois » n'en est pas un).
            + @"|\b(?:on\s+(?:va\s+)?(?:fait|faire|continue[r]?)|faisons|continuons)\s+(?:avec\s+)?autre\s+chose\b(?!\s+qu)|\bpour\s+une\s+autre\s+fois\b"
            + @"|\b(?:un|une)\s+autre\s+(?:petit\s+|petite\s+)?(?:exercice|jeu|question|calcul|activité|suite)\b(?!\s+qu)"
            + @"|\b(?:un|une)\s+nouv(?:el|eau|elle)\s+(?:petit\s+|petite\s+)?(?:exercice|jeu|question|calcul|activité|suite)\b(?!\s+qu)"
            + @"|\b(?:on\s+(?:va\s+)?laisse[rz]?|laissons)\s+(?:cette|ce|cet|celle-ci|celle-là|celui-ci|celui-là|ça|tomber)\b"
            + @"(?![^.!?\n]{0,40}?(?:\bsur\s+l['’]\s*ardoise|\bau\s+tableau|\bet\s+(?:puis\s+)?on\s+re))"
            + @"|\b(?:on\s+(?:va\s+)?(?:(?:la|le|les)\s+)?met(?:tre)?|mets|mettons)\s+(?:(?:cette|ce|cet|ça|la|le|les|l['’])\s*(?:[\w-]+\s+)?)?de\s+c[ôo]té\b"
            + @"(?!\s+(?:un\s+(?:instant|moment|petit\s+instant)|une\s+seconde|deux\s+secondes))"
            + @"|\b(?:on|qu['’]on)\s+s['’]\s*arr[êe]te\b"
            + @"|\bon\s+(?:va\s+)?arr[êe]te[rz]?\s+(?:là|ici|pour\s+(?:aujourd|cette|ce|l))|\barr[êe]tons(?:-nous)?\s+(?:là|ici)\b"
            // « On en reste là pour cette question, Adam, ce n'est pas grave. » (bout en bout v6 sur 1760617, série 4, 3e erreur).
            + @"|\bon\s+(?:va\s+)?en\s+rest(?:e|er|era)\s+là\b|\brestons-en\s+là\b"
            + @"|\bon\s+fait\s+une\s+pause\b(?![^.!?\n]{0,40}?\bet\s+(?:puis\s+)?on\s+re)"
            + @"|\bon\s+reviendra\b|\bon\s+reprendra\s+(?:ça|cela|ce|cette)\b"
            + @"|\b(?:verra|reverra|reprendra|reprend|refera|essaiera|réessaiera|continuera|retentera|recommencera)\b[^.!?\n]{0,40}?\b(?:une\s+autre\s+fois|plus\s+tard|demain|la\s+prochaine\s+fois)\b"
            + @"|\bla\s+prochaine\s+fois\b(?!\s+qu)"
            + @"|\b(?:on\s+(?:va\s+)?change[rz]?|changeons)\s+(?:d['’]\s*(?:exercice|activité)|de\s+(?:jeu|calcul|question|sujet|leçon))\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Abandon();

        /// <summary>L'abandon lu par le code (harnais).</summary>
        public static bool Abandonne(string? texte) => texte is not null && Abandon().IsMatch(texte);

        /// <summary>Le message quitte-t-il la question ? <paramref name="messagesAvant"/> : les messages du professeur AVANT
        /// celui-ci (même lecture que le juge), <paramref name="valeur"/> : le nombre attendu.</summary>
        public static bool QuitteLaQuestion(IReadOnlyList<string?> messagesAvant, string texte, decimal valeur)
        {
            if (Abandon().IsMatch(texte)) return true;
            // LA RÉPONSE DONNÉE — bout en bout final v4 (a77bcd4, série 9, conv. de test 1201, 3e erreur) : « 78... 79... 80... et
            // après, ça fait quatre-vingt-UN. Dis-moi juste ce dernier mot. » Le filtre « réponse jamais donnée » retirait bien
            // le nombre, mais il restait « Dis-moi juste ce dernier mot. » : une phrase sans objet, et pas de question. Le
            // message était construit autour de la réponse : il est remplacé en entier (le filtre reste derrière, par sûreté).
            if (ReponseNonDonnee.LaDonne(texte, valeur)) return true;
            // « Le nombre juste après, c'est 8 dizaines et 1 : essaie de le dire. » (= 81, bout en bout lot v3, conv. de test
            // 1147) : la réponse dite autrement. Lue jusqu'ici par le retrait de phrase d'AnalogieDeSuite, qui n'est plus branché
            // (tâche 0000o) : le message entier est remplacé.
            if (AnalogieDeSuite.DonneEnDizaines(texte, valeur)) return true;
            var nombre = valeur.ToString(CultureInfo.InvariantCulture);
            var lu = CalculAuTableau.Juger(messagesAvant.Append(texte).TakeLast(CalculAuTableau.MessagesRemontes), nombre);
            // Une autre question lue (« Qu'est-ce qui vient juste avant 10 ? », « combien font 3 + 4 ? ») : elle remplace la nôtre.
            // Une relance que le juge ne sait pas lire (« …quatre-vingts, et ensuite ? ») reste : c'est souvent une bonne
            // relance (3 sur 240 réponses du corpus #53), la remplacer serait plus lourd que le défaut (point ouvert, compte rendu).
            return lu is not null && !string.Equals(lu.Attendu, nombre, StringComparison.Ordinal);
        }

        /// <summary>Le message du code, en entier. À la 3e erreur, le dernier essai est annoncé une fois.</summary>
        public static string MessageDuCode(int rang, string enonce) =>
            (rang >= 3
                ? "Ce n'est pas encore ça, et ce n'est pas grave. On reste sur cette question : c'est ton dernier essai. "
                : "Ce n'est pas encore ça. On reste sur cette question, prends ton temps. ")
            + AnalogieDeSuite.QuestionDuCode(enonce);

        /// <summary>Le filtre du flux : tout est retenu jusqu'à la fin, puis rendu tel quel ou remplacé.</summary>
        public sealed class Filtre(IReadOnlyList<string?> messagesAvant, string enonce, decimal valeur, int rang)
        {
            private readonly StringBuilder _tampon = new();

            /// <summary>Le message du code, s'il a remplacé celui du professeur (le texte enregistré prend le même).</summary>
            public string? Remplacement { get; private set; }

            public string Passer(string? fragment)
            {
                _tampon.Append(fragment);
                return string.Empty;
            }

            /// <summary>La question du code ajoutée à la fin d'une réponse gardée qui n'en posait pas.</summary>
            public bool QuestionAjoutee { get; private set; }

            public string Terminer()
            {
                var texte = _tampon.ToString();
                _tampon.Clear();
                if (string.IsNullOrWhiteSpace(texte)) return texte;
                if (QuitteLaQuestion(messagesAvant, texte, valeur))
                {
                    Remplacement = MessageDuCode(rang, enonce);
                    return Remplacement;
                }
                var avec = AvecLaQuestion(texte, enonce);
                QuestionAjoutee = avec.Length != texte.Length;
                return avec;
            }
        }

        /// <summary>
        /// JAMAIS UNE RELANCE SANS QUESTION — tâche 0000o (22/11/2026, décision du coordinateur) : aux 1re-3e erreurs d'une suite,
        /// une réponse gardée qui ne pose aucune question (« Regarde bien l'ardoise et recompte doucement. ») reçoit la question
        /// du code à la fin, sur sa propre ligne. Rien n'est retiré ni réécrit : un ajout seulement, que le juge relit au tour
        /// suivant. Le texte enregistré reçoit le même ajout (même méthode).
        /// </summary>
        // « Dis-moi ce qui vient juste après. » (bout en bout #53 sur dc8c86b, séries 8 et 10) : une demande sans « ? » est déjà une
        // question pour l'élève ; la question du code s'ajoutait derrière (« … juste après.  78, 79, 80… ? »), en double.
        public static string AvecLaQuestion(string texte, string enonce) =>
            texte.Contains('?') || string.IsNullOrWhiteSpace(enonce) || Demande().IsMatch(texte)
                ? texte
                : texte.TrimEnd() + "\n\n" + AnalogieDeSuite.QuestionDuCode(enonce);

        /// <summary>Une demande qui attend sa réponse : « dis-moi », « donne-moi », « écris-moi », « à toi de trouver / de dire »,
        /// « dis-le », « trouve … », « essaie de trouver », « compte … et dis ».</summary>
        // CONTRÔLE D'OMAR SUR LA v5 (point 4) : « Dis-le avec moi, et trouve ce qui vient juste après. » recevait la question du
        // code derrière (« ⏎⏎ 78, 79, 80, … ? »), en double. « trouve » seulement à l'impératif, en tête de proposition (après
        // une ponctuation, « et », « puis », « alors », « maintenant ») : « je sais que tu trouves ça dur. » n'est pas une demande.
        [GeneratedRegex(@"\b(?:dis|donne|écris|ecris|montre)[- ]moi\b|\bà\s+toi\s+de\s+(?:trouver|dire|me\s+dire)\b"
            + @"|\bdis[- ]le\b|\bessaie\s+de\s+(?:trouver|(?:me\s+)?(?:le\s+)?dire|compter)\b"
            + @"|(?:^|[.!:;,…]\s*|\b(?:et|puis|alors|maintenant)\s+)trouve(?:[- ]moi)?\s+(?:ce|le|la|quel|quelle|combien|qu|l['’])"
            + @"|\bcompte\b[^.!?\n]{0,60}?\bet\s+(?:dis|donne)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
        private static partial Regex Demande();
    }
}
