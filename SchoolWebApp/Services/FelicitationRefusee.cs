using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// UNE TRADUCTION REFUSÉE NE SE FÉLICITE PAS, RETIRÉ PAR LE CODE — contrôle d'Omar sur le lot groupé (22/11/2026, point 5).
    /// Le fait (<see cref="RepetitionEnLangue.SansFelicitation"/>) ne suffisait pas : au bout en bout sur la branche, le
    /// professeur écrivait encore « …en français, c'est très bien ! », « …le sens du mot, c'est super ! », « …tu as compris
    /// le sens, c'est top ! » sous une bulle « On réessaie » (leçon de #29/#45/#46 : seul le code tient). Au tour où sa
    /// réponse est la traduction française du mot (écoute des mots au micro ou au clavier, répétition en langue), les
    /// FÉLICITATIONS partent de sa phrase ; « tu as (bien) compris le sens » reste (décision de Min's du 22/11, 00 h 04).
    ///
    /// Générique : tous les professeurs de langue. Phrase par phrase, avec le même découpage dans le flux et sur le texte
    /// enregistré (écran = historique). Ne touche ni « c'est bien le mot pour deux » (« bien » seul n'est pas retiré), ni une
    /// balise de voix, ni « ce n'est pas très bien ».
    /// </summary>
    public static partial class FelicitationRefusee
    {
        /// <summary>« , c'est très bien ! », « et c'est super, », « — ça c'est top », « , bravo ! », « Bravo ! », « Très bien ! ».</summary>
        [GeneratedRegex(
            @"(?:\s*,\s*|\s*[—–]\s*|\s+)?(?:\bet\s+)?(?:\bça\s+|\bca\s+)?\bc['’]est\s+(?:(?:vraiment|trop|déjà|deja|quand\s+même|tout\s+de\s+même)\s+)?(?:très\s+bien|bien\s+joué|super|top|génial|genial|parfait|formidable|excellent|chouette|bravo)\b(?:\s+de\s+ta\s+part|\s+pour\s+[^!.?,;:\n\[]{1,40})?\s*(?:[!]+|,)?(?=\s|$|[.!?…])"
            // « Bon, et bravo pour l'effort de répéter ! » (bout en bout QR2, conv. de test 949) : « et » devant, « pour … » derrière.
            + @"|(?:\s*,\s*|(?<=^|[.!?…\n])\s*)(?:\bet\s+)?\b(?:bravo|très\s+bien|super|génial|parfait|bien\s+joué)\b(?:\s+pour\s+[^!.?,;:\n\[]{1,40}|(?-i:\s+\p{Lu}\p{Ll}+))?\s*(?:!+|,|\.(?!\.))?(?=\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Felicitation();

        /// <summary>Une phrase : jusqu'à la fin d'une suite de ponctuations finales (ou de sauts de ligne), blancs suivants compris.</summary>
        [GeneratedRegex(@"[^.!?…\n]*(?:[.!?…\n]+[^\S\n]*|$)")]
        private static partial Regex Phrases();

        /// <summary>Le texte entier, phrase par phrase (même découpage que <see cref="Filtre"/>).</summary>
        public static string Retirer(string? texte, out int retraits) => ParPhrase(texte, Felicitation(), out retraits);

        /// <summary>Le motif retiré phrase par phrase, avec le même découpage que le filtre du flux.</summary>
        internal static string ParPhrase(string? texte, Regex motif, out int retraits)
        {
            retraits = 0;
            if (string.IsNullOrEmpty(texte)) return texte ?? string.Empty;
            var sortie = new StringBuilder();
            foreach (Match m in Phrases().Matches(texte))
            {
                if (m.Length == 0) continue;
                sortie.Append(RetirerDeLaPhrase(m.Value, motif, ref retraits));
            }
            return sortie.ToString();
        }

        public static string Retirer(string? texte) => Retirer(texte, out _);

        private static string RetirerDeLaPhrase(string phrase, Regex motif, ref int retraits)
        {
            // Les balises (voix, tableau) ne sont jamais touchées : la phrase qui en porte une part telle quelle.
            if (phrase.Contains('[')) return phrase;
            // La fin d'origine (« !», « … », sauts de ligne), remise telle quelle derrière ce qui reste.
            var fin = Regex.Match(phrase, @"(?<p>[^\S\n]*[.!?…]+)?(?<b>\s*)$");
            var ponctuation = fin.Groups["p"].Value;
            var blancs = fin.Groups["b"].Value;
            var corps = phrase[..fin.Index];

            var n = 0;
            corps = motif.Replace(corps, m =>
            {
                n++;
                var v = m.Value.Trim();
                // Au milieu, entre deux virgules (« …en français, et c'est très bien, tu as compris ») : une virgule reste.
                if ((v.StartsWith(',') || v.StartsWith('—') || v.StartsWith('–')) && v.EndsWith(',') && m.Index > 0) return ", ";
                // Entre deux mots, jamais rien : « ça vient, c'est ton dernier essai pour… » ne devient pas « ça vientpour… »
                // (bout en bout du lot groupé sur 7039668, conv. de test 990).
                var avant = m.Index > 0 ? corps[m.Index - 1] : ' ';
                var apres = m.Index + m.Length < corps.Length ? corps[m.Index + m.Length] : ' ';
                return !char.IsWhiteSpace(avant) && !char.IsWhiteSpace(apres) && m.Index > 0 ? " " : string.Empty;
            });
            if (n == 0) return phrase;
            retraits += n;

            corps = Regex.Replace(corps, @"\(\s*\)", string.Empty);
            corps = Regex.Replace(corps, @"[\s,;:—–-]+$", string.Empty);
            corps = Regex.Replace(corps, @"^[\s,;:—–-]+", string.Empty);
            corps = Regex.Replace(corps, @"[^\S\n]{2,}", " ");
            corps = Regex.Replace(corps, @"\s+,", ",");
            // Il ne reste qu'une interjection (« Bon », « Alors », « Ah »), ou le seul prénom de l'élève (« C'est ton dernier
            // essai, Adam. » → « Adam. », conv. de test 984) : elle ne dit plus rien, elle part avec.
            if (!corps.Any(char.IsLetterOrDigit)
                || Regex.IsMatch(corps, @"^(?:bon|alors|ah|oh|eh\s+bien|ok|okay|d['’]accord|allez)$", RegexOptions.IgnoreCase)
                || (Regex.IsMatch(corps, @"^\p{Lu}\p{Ll}+(?:-\p{Lu}\p{Ll}+)?$")
                    && !Regex.IsMatch(corps, @"^(?:non|oui|presque|attention|regarde|écoute|ecoute|courage|voilà|voila|exactement|pas|encore|essaie|réessaie|vas-y)$", RegexOptions.IgnoreCase)))
            {
                // Une phrase qui n'était que la félicitation (« Bravo ! ») part entière ; ses sauts de ligne restent.
                return blancs.Contains('\n') ? blancs[blancs.IndexOf('\n')..] : string.Empty;
            }
            if (char.IsLower(corps[0])) corps = char.ToUpperInvariant(corps[0]) + corps[1..];
            if (Regex.IsMatch(corps, @"[.!?…]$")) ponctuation = string.Empty;
            return corps + ponctuation + blancs;
        }

        /// <summary>Le filtre du flux : retient la phrase en cours, rend chaque phrase finie, nettoyée.</summary>
        public sealed class Filtre(Regex? motif = null)
        {
            private readonly Regex _motif = motif ?? Felicitation();
            private readonly StringBuilder _tampon = new();
            private int _retraits;

            public int Retraits => _retraits;

            public string Passer(string? fragment)
            {
                if (string.IsNullOrEmpty(fragment)) return string.Empty;
                _tampon.Append(fragment);
                var texte = _tampon.ToString();

                // Coupe après la dernière suite de ponctuations finales SUIVIE d'autre chose qu'un blanc : la phrase est finie.
                var coupe = -1;
                for (var i = texte.Length - 1; i > 0; i--)
                {
                    if (!char.IsWhiteSpace(texte[i]) && !EstFinale(texte[i]))
                    {
                        var j = i - 1;
                        while (j >= 0 && char.IsWhiteSpace(texte[j]) && texte[j] != '\n') j--;
                        if (j >= 0 && EstFinale(texte[j])) { coupe = j + 1; while (coupe < i && char.IsWhiteSpace(texte[coupe]) && texte[coupe] != '\n') coupe++; break; }
                    }
                }
                if (coupe <= 0) return string.Empty;

                var fini = texte[..coupe];
                _tampon.Remove(0, coupe);
                var r = 0;
                var sortie = ParPhrase(fini, _motif, out r);
                _retraits += r;
                return sortie;
            }

            public string Terminer()
            {
                var reste = _tampon.ToString();
                _tampon.Clear();
                var sortie = ParPhrase(reste, _motif, out var r);
                _retraits += r;
                return sortie;
            }

            private static bool EstFinale(char c) => c is '.' or '!' or '?' or '…' or '\n';
        }
    }
}
