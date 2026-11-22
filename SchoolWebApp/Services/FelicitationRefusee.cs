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
        /// <summary>
        /// LA FÉLICITATION FERME SA PROPOSITION — contrôle d'Omar sur le lot groupé v2 (19abbfd, 22/11/2026, point 1) : la 1re
        /// forme s'arrêtait sur un simple blanc, et coupait une phrase qui dépendait de la félicitation (« …, et c'est très
        /// bien de connaître son sens ! » → « …français de connaître son sens ! » ; « Écoute bien, c'est super facile à
        /// dire. » → « Écoute bien facile à dire. » ; « En anglais, c'est top pour s'entraîner… » → « En anglais. »). Une
        /// félicitation ne part plus que si elle FERME la proposition : une ponctuation ou la fin de la phrase juste après
        /// (« de ta part » et le prénom de l'élève compris). Suivie d'un complément (« de connaître… », « facile… », « pour
        /// s'entraîner… »), la phrase reste entière. Seul « bravo pour … » garde son complément retirable.
        /// </summary>
        private const string Ferme = @"(?:\s*(?:!+|,)(?=\s|$)|(?=\s*(?:[.!?…,;:)—–]|$)))";

        /// <summary>« , c'est très bien ! », « et c'est super, », « — ça c'est top », « , bravo ! », « Bravo ! », « Très bien ! ».</summary>
        [GeneratedRegex(
            // « tu en as compris le sens, c'est déjà bien ! » (bout en bout QE1 sur c71d07a, conv. de test 1070) : « déjà bien »
            // part ; « c'est bien le mot français » (un constat) reste.
            @"(?:\s*,\s*|\s*[—–]\s*|\s+)?(?:\bet\s+)?(?:\bça\s+|\bca\s+)?\bc['’]est\s+(?:(?:vraiment|trop|déjà|deja|quand\s+même|tout\s+de\s+même)\s+)?(?:très\s+bien|bien\s+joué|super|top|génial|genial|parfait|formidable|excellent|chouette|bravo|(?:déjà|deja)\s+bien)\b(?:\s+de\s+ta\s+part)?(?-i:\s+\p{Lu}\p{Ll}+)?" + Ferme
            // « Bon, et bravo pour l'effort de répéter ! » (bout en bout QR2, conv. de test 949) : « et » devant, « pour … » derrière.
            + @"|(?:\s*,\s*|(?<=^|[.!?…\n])\s*)(?:\bet\s+)?\b(?:bravo|très\s+bien|super|génial|parfait|bien\s+joué)\b(?:(?<=bravo)\s+pour\s+[^!.?,;:\n\[]{1,40}?|(?-i:\s+\p{Lu}\p{Ll}+))?(?:\s*\.(?!\.)(?=\s|$)|" + Ferme + ")",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Felicitation();

        /// <summary>Une phrase : jusqu'à la fin d'une suite de ponctuations finales (ou de sauts de ligne), blancs suivants compris.</summary>
        [GeneratedRegex(@"[^.!?…\n]*(?:[.!?…\n]+[^\S\n]*|$)")]
        private static partial Regex Phrases();

        /// <summary>Le texte entier, phrase par phrase (même découpage que <see cref="Filtre"/>).</summary>
        public static string Retirer(string? texte, out int retraits) => ParPhrase(texte, Felicitation(), out retraits);

        /// <summary>Le motif retiré phrase par phrase, avec le même découpage que le filtre du flux.</summary>
        internal static string ParPhrase(string? texte, Regex motif, out int retraits, bool debut = true, Options? options = null)
        {
            retraits = 0;
            if (string.IsNullOrEmpty(texte)) return texte ?? string.Empty;
            var sortie = new StringBuilder();
            foreach (Match m in Phrases().Matches(texte))
            {
                if (m.Length == 0) continue;
                sortie.Append(RetirerDeLaPhrase(m.Value, motif, ref retraits, options));
            }
            // La 1re phrase du message partie (« C'est sa troisième erreur sur cette question.⏎… », conv. de test 1015) : ses
            // sauts de ligne ne restent pas en tête du message. Le flux fait pareil tant qu'il n'a rien envoyé (debut).
            var resultat = sortie.ToString();
            return debut && retraits > 0 && !char.IsWhiteSpace(texte[0]) ? resultat.TrimStart() : resultat;
        }

        public static string Retirer(string? texte) => Retirer(texte, out _);

        /// <summary>
        /// Réglages d'un filtre par phrase. <c>Garder</c> : un groupe nommé du motif dont la PREMIÈRE occurrence du message
        /// reste (« C'est ton dernier essai » dit deux fois au 3e essai : la 1re reste, la 2e part — contrôle d'Omar sur le lot
        /// groupé v2, point 5). <c>SurBalise</c> : un motif qui peut aussi partir d'une phrase qui porte une balise, hors de
        /// la balise, sans autre retouche que les blancs et la majuscule (« Écoute une dernière fois le son anglais :
        /// [EN]Blue[/EN]. », point 4 ; « [/ARDOISE] C'est ton dernier essai : qu'est-ce qui vient… ? », point 5, série 8).
        /// L'état (occurrences déjà vues) vit avec l'objet : un par message.
        /// </summary>
        public sealed class Options
        {
            public string? Garder { get; init; }

            public Regex? SurBalise { get; init; }

            internal int Vues;
        }

        /// <summary>La place d'un retrait dans une phrase à balise, le temps de remettre la majuscule.</summary>
        private const char Trou = '\u0001';

        private static string RetirerDeLaPhrase(string phrase, Regex motif, ref int retraits, Options? options = null)
        {
            // Les balises (voix, tableau) ne sont jamais touchées : la phrase qui en porte une part telle quelle, sauf le motif
            // « sur balise », retiré seulement HORS des balises.
            if (phrase.Contains('['))
            {
                if (options?.SurBalise is not { } surBalise)
                {
                    // Une mention à garder dite dans une phrase à balise compte quand même comme la première.
                    if (options?.Garder is { } g) options.Vues += motif.Matches(phrase).Count(m => m.Groups[g].Success);
                    return phrase;
                }
                var k = 0;
                var r = surBalise.Replace(phrase, m =>
                {
                    // Dans une balise (« [EN », « [FR]…[/FR] », « [ARDOISE]78, 79[/ARDOISE] ») : jamais.
                    var avant = phrase[..m.Index];
                    if (avant.LastIndexOf('[') > avant.LastIndexOf(']')) return m.Value;
                    if (Regex.IsMatch(phrase[(m.Index + m.Length)..], @"^[^\[]*\[/")) return m.Value;
                    if (options.Garder is { } garder && m.Groups[garder].Success && options.Vues++ == 0) return m.Value;
                    k++;
                    return Trou.ToString();
                });
                if (k == 0) return phrase;
                retraits += k;
                // En tête de phrase (ou juste après une balise fermée), la suite reprend la majuscule.
                r = Regex.Replace(r, @"(?<=(?:^|[.!?…\]])[^\S\n]*)\u0001[^\S\n]*(\p{Ll})", m => " " + m.Groups[1].Value.ToUpperInvariant());
                r = r.Replace(Trou.ToString(), " ");
                r = Regex.Replace(r, @"[^\S\n]{2,}", " ");
                r = Regex.Replace(r, @"(?<=\S)[^\S\n]+(?=[,.](?!\.))", string.Empty);
                return Regex.Replace(r, @"^[^\S\n]+", string.Empty);
            }
            // La fin d'origine (« !», « … », sauts de ligne), remise telle quelle derrière ce qui reste.
            var fin = Regex.Match(phrase, @"(?<p>[^\S\n]*[.!?…]+)?(?<b>\s*)$");
            var ponctuation = fin.Groups["p"].Value;
            var blancs = fin.Groups["b"].Value;
            var corps = phrase[..fin.Index];

            var n = 0;
            // « …le son au début :⏎⏎[EN]Blue[/EN] » (bout en bout QR2 sur 5c685bd) : les deux-points qui annoncent la ligne
            // suivante restent, s'ils ne faisaient pas partie de ce qui part.
            var origine = corps.TrimEnd();
            var finDuDernier = 0;
            corps = motif.Replace(corps, m =>
            {
                if (options?.Garder is { } garder && m.Groups[garder].Success && options.Vues++ == 0) return m.Value;
                n++;
                finDuDernier = m.Index + m.Length;
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
            if (origine.EndsWith(':') && finDuDernier < origine.Length && !corps.EndsWith(':')) corps += " :";
            if (char.IsLower(corps[0])) corps = char.ToUpperInvariant(corps[0]) + corps[1..];
            if (Regex.IsMatch(corps, @"[.!?…]$")) ponctuation = string.Empty;
            return corps + ponctuation + blancs;
        }

        /// <summary>Le filtre du flux : retient la phrase en cours, rend chaque phrase finie, nettoyée.</summary>
        public sealed class Filtre(Regex? motif = null, Options? options = null)
        {
            private readonly Regex _motif = motif ?? Felicitation();
            private readonly Options? _options = options;
            private readonly StringBuilder _tampon = new();
            private int _retraits;

            public int Retraits => _retraits;

            /// <summary>Rien n'est encore parti : une phrase retirée en tête ne laisse pas ses sauts de ligne.</summary>
            private bool _rienEnvoye = true;

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
                var sortie = ParPhrase(fini, _motif, out var r, _rienEnvoye, _options);
                _retraits += r;
                if (sortie.Length > 0) _rienEnvoye = false;
                return sortie;
            }

            public string Terminer()
            {
                var reste = _tampon.ToString();
                _tampon.Clear();
                var sortie = ParPhrase(reste, _motif, out var r, _rienEnvoye, _options);
                _retraits += r;
                if (sortie.Length > 0) _rienEnvoye = false;
                return sortie;
            }

            private static bool EstFinale(char c) => c is '.' or '!' or '?' or '…' or '\n';
        }
    }
}
