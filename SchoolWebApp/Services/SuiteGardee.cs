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

        /// <summary>L'abandon dit : « on y reviendra », « plus tard », « autre chose », « un autre exercice », « on s'arrête »…</summary>
        [GeneratedRegex(
            @"\bon\s+y\s+reviendra\b|\bon\s+(?:va\s+)?(?:la|le)\s+(?:garde[rz]?|reprendra|laisse[rz]?)\b|\bplus\s+tard\b|\bautre\s+chose\b"
            + @"|\b(?:un|une)\s+autre\s+(?:petit\s+|petite\s+)?(?:exercice|jeu|question|calcul|activité|suite)\b|\bon\s+passe\s+à\b"
            + @"|\b(?:on|qu['’]on)\s+s['’]\s*arr[êe]te\b|\bon\s+fait\s+une\s+pause\b|\bde\s+côté\b|\bon\s+laisse\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Abandon();

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

            public string Terminer()
            {
                var texte = _tampon.ToString();
                _tampon.Clear();
                if (string.IsNullOrWhiteSpace(texte) || !QuitteLaQuestion(messagesAvant, texte, valeur)) return texte;
                Remplacement = MessageDuCode(rang, enonce);
                return Remplacement;
            }
        }
    }
}
