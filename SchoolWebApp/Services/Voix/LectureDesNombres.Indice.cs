using System.Globalization;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// L'INDICE APRÈS UNE LECTURE FAUSSE — signalement #52 (Adam, 21/11/2026). Le professeur inventait lui-même sa règle sur
    /// les chiffres, et elle était fausse : 58 au tableau, « le premier chiffre est plus grand que cinq, donc ça commence par
    /// soixante » (deux fois) ; 53, « commence par un chiffre plus grand que cinq, donc ça commence par cinquante ». Un enfant
    /// de CP apprend la règle qu'on lui donne.
    ///
    /// Comme pour les verdicts (CalculAuTableau, DicteeDeNombres) : le CODE calcule l'indice exact et le donne au professeur
    /// dans le fait du verdict (<see cref="IndiceExact"/>), avec l'interdiction d'en inventer un autre. Et, en filet, une
    /// phrase du professeur qui affirme quelque chose de FAUX sur les chiffres du nombre au tableau (« le premier chiffre est
    /// plus grand que cinq » devant 58, « ça commence par soixante » devant 58, « le premier chiffre est un 6 » devant 58)
    /// est remplacée par l'indice exact (<see cref="SansIndiceFaux"/>). Une phrase VRAIE reste (« gros chiffre, plus grand
    /// que cinq » devant 78).
    ///
    /// L'indice ne donne jamais le nombre entier : il dit le premier chiffre et le mot par lequel le nombre commence (ou, si
    /// le début lu était déjà bon, il fait regarder la fin). Pour un nombre rond (50, 300), le mot du début serait la
    /// réponse : l'indice parle alors de dizaines (ou de centaines) et du zéro. Seulement pour les entiers de 10 à 999 :
    /// au-delà (et pour les décimaux, fractions, puissances…), pas d'indice calculé.
    /// </summary>
    public static partial class LectureDesNombres
    {
        private static readonly string[] MotsDesChiffres = ["zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf"];

        private static readonly string[] MotsDesDizaines = ["", "dix", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante", "quatre-vingt", "quatre-vingt"];

        private static readonly string[] DixASeize = ["dix", "onze", "douze", "treize", "quatorze", "quinze", "seize"];

        /// <summary>L'entier de 10 à 999 que montre la cible, sinon null.</summary>
        private static int? EntierDeDixAMille(string? cible) =>
            int.TryParse(cible, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is >= 10 and <= 999 ? n : null;

        /// <summary>
        /// L'INDICE EXACT pour le nombre <paramref name="cible"/>, quand il a lu <paramref name="lu"/> (écriture canonique,
        /// null si on ne sait pas). Null hors des entiers de 10 à 999, ou s'il a lu le bon nombre.
        /// </summary>
        public static string? IndiceExact(string? cible, string? lu)
        {
            if (EntierDeDixAMille(cible) is not int n) return null;
            int? l = int.TryParse(lu, NumberStyles.None, CultureInfo.InvariantCulture, out var x) ? x : null;
            if (l == n) return null;
            return n >= 100 ? IndiceCentaines(n, l) : IndiceDizaines(n, l);
        }

        private static string IndiceDizaines(int n, int? lu)
        {
            int d = n / 10, u = n % 10;

            // Le début qu'il a lu est le bon (même chiffre des dizaines) : c'est la fin qu'il faut regarder.
            if (lu is >= 10 and <= 99 && lu / 10 == d)
            {
                if (d is 7 or 9)
                    return $"Le début est bon. Regarde bien le dernier chiffre : c'est un {u}, et après « {MotsDesDizaines[d]} » on compte à partir de dix.";
                return u == 0
                    ? "Le début est bon. Regarde bien le dernier chiffre : c'est un 0, il n'y a pas d'unités en plus."
                    : $"Le début est bon. Regarde bien le dernier chiffre : c'est un {u}.";
            }

            return d switch
            {
                1 => "Le premier chiffre est un 1 : c'est une seule dizaine, on est avant vingt.",
                7 or 9 => $"Le premier chiffre est un {d} : en français, on dit « {MotsDesDizaines[d]} », puis un nombre de dix à dix-neuf.",
                _ when u == 0 => $"Le premier chiffre est un {d} : c'est {d} dizaines, et le 0 dit qu'il n'y a pas d'unités en plus.",
                _ => $"Le premier chiffre est un {d} : le nombre commence par « {MotsDesDizaines[d]} ».",
            };
        }

        private static string IndiceCentaines(int n, int? lu)
        {
            int c = n / 100, reste = n % 100;
            if (lu is >= 100 and <= 999 && lu / 100 == c)
            {
                return reste == 0
                    ? "Le début est bon. Regarde bien les deux derniers chiffres : ce sont deux 0, il n'y a rien après les centaines."
                    : "Le début est bon. Regarde bien les deux derniers chiffres : ils se disent ensemble, comme un nombre à deux chiffres.";
            }

            if (reste == 0)
                return $"Le premier chiffre est un {c} : c'est {c} centaine{(c > 1 ? "s" : string.Empty)}, et les deux 0 disent qu'il n'y a rien après.";
            return $"Le premier chiffre est un {c} : le nombre commence par « {(c == 1 ? "cent" : MotsDesChiffres[c] + " cent")} ».";
        }

        // ------------------------------------------------------------------ le filet : une affirmation fausse sur les chiffres

        // « le premier chiffre est plus grand que cinq », « un gros chiffre, plus grand que 5 », « le chiffre des unités est
        // plus petit que 3 » : une comparaison d'un CHIFFRE du nombre avec une valeur.
        [GeneratedRegex(@"\bchiffres?\b(?<avant>[^.!?\n]*?)\b(?<op>plus\s+(?:grands?|gros|petits?)\s+que|sup[ée]rieurs?\s+[àa]|inf[ée]rieurs?\s+[àa])\s+(?:le\s+)?(?<k>\d+|\p{L}+(?:-\p{L}+)*)",
            RegexOptions.IgnoreCase)]
        private static partial Regex ComparaisonDeChiffre();

        // « le premier chiffre est un 6 », « le chiffre des dizaines, c'est le cinq », « le dernier chiffre est 3 ».
        [GeneratedRegex(@"\b(?<pos>premier\s+chiffre|1er\s+chiffre|chiffre\s+de\s+gauche|dernier\s+chiffre|chiffre\s+de\s+droite|deuxi[eè]me\s+chiffre|second\s+chiffre|chiffre\s+des\s+(?:dizaines|unit[ée]s|centaines))\s*,?\s*(?:est|c['’]est)\s+(?:(?:un|une|le)\s+)?(?<k>\d+|\p{L}+)(?![\p{L}\d])",
            RegexOptions.IgnoreCase)]
        private static partial Regex ChiffreAffirme();

        // « ça commence par « soixante » », « il commence par cinquante », « ça commence par un 5 », « commence par le chiffre 5 ».
        [GeneratedRegex(@"\bcommencen?t?\s+par\s+[«""“'’\s]*(?:(?<art>un|une|le|la)\s+)?(?:(?<chiffre>chiffre)\s+)?[«""“'’\s]*(?<k>\d+|\p{L}+)(?![\p{L}\d])",
            RegexOptions.IgnoreCase)]
        private static partial Regex CommencePar();

        /// <summary>Le chiffre (0-9) ou la valeur dite par un mot ou des chiffres : « cinq », « 5 », « vingt » ; null sinon.</summary>
        private static long? Valeur(string mot)
        {
            var m = SansAccents(mot.ToLowerInvariant());
            if (Regex.IsMatch(m, @"^\d+$")) return long.TryParse(m, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : null;
            if (m == "zero") return 0;
            return Entier(m.Split('-', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>Le premier mot qu'on prononce pour ce nombre : 58 → « cinquante », 76 → « soixante », 85 → « quatre », 345 → « trois ».</summary>
        private static string PremierMot(int n) => n switch
        {
            >= 200 => SansAccents(MotsDesChiffres[n / 100]),
            >= 100 => "cent",
            >= 17 and <= 19 => "dix",
            >= 10 and <= 16 => DixASeize[n - 10],
            >= 80 => "quatre",
            _ => MotsDesDizaines[n / 10],
        };

        /// <summary>
        /// La phrase affirme-t-elle quelque chose de FAUX sur les chiffres de <paramref name="element"/> ? (seulement pour les
        /// entiers de 10 à 999 ; « on ne sait pas » = non, la phrase reste).
        /// </summary>
        public static bool AffirmeUnIndiceFaux(string phrase, Element element)
        {
            if (EntierDeDixAMille(element.Cible) is not int n || string.IsNullOrWhiteSpace(phrase)) return false;
            var chiffres = n.ToString(CultureInfo.InvariantCulture);
            int Premier() => chiffres[0] - '0';
            int Dernier() => chiffres[^1] - '0';

            foreach (Match m in ComparaisonDeChiffre().Matches(phrase))
            {
                // « plus grand que 50 » parle du nombre entier, pas d'un chiffre : on ne juge que les comparaisons à un chiffre.
                if (Valeur(m.Groups["k"].Value) is not long k || k > 9) continue;
                var duBout = Regex.IsMatch(m.Groups["avant"].Value + " " + phrase[..m.Index], @"\b(?:dernier|droite|unit[ée]s?)\b", RegexOptions.IgnoreCase)
                    && !Regex.IsMatch(m.Groups["avant"].Value, @"\b(?:premier|gauche|dizaines?|centaines?)\b", RegexOptions.IgnoreCase);
                var chiffre = duBout ? Dernier() : Premier();
                var plusGrand = !Regex.IsMatch(m.Groups["op"].Value, @"petit|inf", RegexOptions.IgnoreCase);
                if (plusGrand ? chiffre <= k : chiffre >= k) return true;
            }

            foreach (Match m in ChiffreAffirme().Matches(phrase))
            {
                if (Valeur(m.Groups["k"].Value) is not long k) continue;
                var pos = SansAccents(m.Groups["pos"].Value.ToLowerInvariant());
                int? vrai = pos switch
                {
                    _ when pos.Contains("dizaines") => n / 10 % 10,
                    _ when pos.Contains("unite") => n % 10,
                    _ when pos.Contains("centaines") => n >= 100 ? n / 100 : null,
                    _ when pos.Contains("dernier") || pos.Contains("droite") => Dernier(),
                    _ when pos.Contains("deuxieme") || pos.Contains("second") => chiffres[1] - '0',
                    _ => Premier(),
                };
                if (vrai is not null && k != vrai) return true;
            }

            foreach (Match m in CommencePar().Matches(phrase))
            {
                var mot = m.Groups["k"].Value;
                if (Valeur(mot) is not long k) continue;
                // « commence par un 5 », « commence par le chiffre 5 » : un chiffre. « commence par cinquante » : un mot.
                var unChiffre = m.Groups["chiffre"].Success || (m.Groups["art"].Success && k <= 9) || Regex.IsMatch(mot, @"^\d$");
                if (unChiffre)
                {
                    if (k != Premier()) return true;
                }
                else if (!Regex.IsMatch(mot, @"^\d+$"))
                {
                    var dit = SansAccents(mot.ToLowerInvariant());
                    if (dit == "vingts") dit = "vingt";
                    // « ça commence par cinq » devant 58 parle du chiffre 5 : vrai aussi.
                    if (dit != PremierMot(n) && !(k <= 9 && k == Premier())) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// LE FILET (signalement #52) : chaque phrase qui affirme quelque chose de faux sur les chiffres du nombre au tableau
        /// part ; la première est remplacée par l'indice exact (sauf s'il est déjà dans le texte). Un texte sans phrase fausse
        /// revient tel quel, au caractère près.
        /// </summary>
        public static string SansIndiceFaux(string texte, Element element)
        {
            if (EntierDeDixAMille(element.Cible) is null || string.IsNullOrEmpty(texte)) return texte ?? string.Empty;
            var indice = IndiceExact(element.Cible, null);
            var dejaLa = indice is not null && texte.Contains(indice, StringComparison.Ordinal);
            var remplace = false;
            return Phrase().Replace(texte, m =>
            {
                if (!AffirmeUnIndiceFaux(m.Value, element)) return m.Value;
                if (remplace || dejaLa || indice is null) return string.Empty;
                remplace = true;
                var debut = m.Value.Length - m.Value.TrimStart().Length;
                return m.Value[..debut] + indice;
            });
        }

        /// <summary>Ce que le fait du verdict ajoute quand le code connaît l'indice exact.</summary>
        private static string ConsigneDIndice(string indice) =>
            $"INDICE EXACT, VÉRIFIÉ : « {indice} » Si tu donnes un indice sur les chiffres, donne CELUI-LÀ (avec tes mots si tu veux, "
            + "sans rien changer aux chiffres ni aux mots de nombre). N'invente AUCUNE autre règle sur les chiffres : jamais « plus "
            + "grand que », « plus petit que », « gros chiffre », et ne dis pas par quel mot le nombre commence si ce n'est pas "
            + "celui de cet indice.";
    }
}
