using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LA RÉPONSE N'EST PAS DONNÉE PAR LE PROFESSEUR — tâche 1941 (défaut générique vu par Omar sur master, calcul au
    /// tableau). Règle de Camara et de Min's : on ne donne JAMAIS la réponse à l'enfant, il la trouve lui-même. Le fait
    /// FAUX du juge le dit, la règle des essais aussi ; pourtant, sur 40 réponses de master au scénario « 7 + 5 »,
    /// environ 10 la donnaient : « 7 + 5, ça fait 12 : je te le dis cette fois », « on arrive à 12 », « 8, 9, 10, 11,
    /// 12 », ou « 7 + 5 = 12 » réécrit au tableau.
    ///
    /// Le code connaît la réponse attendue (<see cref="CalculAuTableau.ReponseCherchee"/>). Tant que l'élève ne l'a pas
    /// trouvée, ce filtre la retire de ce que le professeur envoie — écran, voix (faite par le front à partir du même
    /// flux) et texte gardé en base, donc historique relu :
    ///   - une suite qui compte jusqu'à elle s'arrête juste avant : « 8, 9, 10, 11, 12 » → « 8, 9, 10, 11… » (l'aide
    ///     reste, l'enfant dit le dernier nombre) ;
    ///   - au tableau, l'égalité garde son trou : « 7 + 5 = 12 » → « 7 + 5 = ? » ;
    ///   - hors du tableau, la PHRASE qui l'énonce part en entier : « Alors 7 + 5, ça fait 12. », « On arrive à 12. »,
    ///     « La réponse, c'est 12. », « 10 + 2 = 12. ».
    /// Les blocs qui ne s'adressent pas à l'enfant ([RAPPORT], [FICHE]…) ne sont pas touchés. Si plus rien ne reste à
    /// dire, une phrase neutre prend la place : l'enfant n'est jamais face à une bulle vide.
    ///
    /// COMMENT ON FILTRE UN TEXTE QUI ARRIVE PAR MORCEAUX : une phrase part quand elle est finie (« . », « ! », « ? »
    /// suivis d'un blanc, ou un saut de ligne), un bloc quand il est fermé. Des points de suspension suivis d'un nombre
    /// ne finissent rien : « 8... 9... 10... 11... 12 » est une seule suite. Chaque morceau est traité seul, et la
    /// découpe ne dépend que de ce qui l'entoure de près : le flux donne exactement le texte entier filtré
    /// (<see cref="Retirer(string, decimal, out int)"/>), qui est ce qu'on garde en base. Le filtre n'existe que s'il y
    /// a une réponse à protéger : ailleurs, pas le moindre retard.
    ///
    /// Portée générique : commun à tous les professeurs, ne dépend d'aucune matière ni d'aucun prompt.
    /// </summary>
    public sealed partial class ReponseNonDonnee(decimal attendue)
    {
        /// <summary>Ce qui remplace une réponse dont il ne resterait rien (pas une lettre hors des blocs).</summary>
        public const string PhraseDeRepli = "On cherche encore ensemble : regarde bien le tableau, et essaie encore !";

        /// <summary>Au-delà, on n'attend plus la fin de la phrase : mieux vaut un texte qui part (filtré quand même).</summary>
        private const int AttenteMaximum = 1200;

        private string _tampon = string.Empty;
        private bool _lettreEnvoyee;
        private bool _vide = true;

        /// <summary>Combien de phrases retirées ou de suites coupées dans ce flux.</summary>
        public int Retraits { get; private set; }

        public string Passer(string? fragment)
        {
            if (string.IsNullOrEmpty(fragment)) return string.Empty;
            _tampon += fragment;

            var coupe = DerniereCoupure(_tampon, fin: false);
            if (coupe <= 0)
            {
                // Un bloc ouvert (tableau, rapport…) s'attend jusqu'à sa fermeture : on ne le coupe jamais.
                if (_tampon.Length < AttenteMaximum || OuvertureDeBloc().Match(_tampon) is { Success: true, Index: 0 }) return string.Empty;
                coupe = _tampon.Length;
            }

            var tete = _tampon[..coupe];
            _tampon = _tampon[coupe..];
            return Sortir(tete);
        }

        public string Terminer()
        {
            var reste = _tampon;
            _tampon = string.Empty;
            var sortie = Sortir(reste);
            if (sortie.Length > 0) _finBlanche = char.IsWhiteSpace(sortie[^1]);
            return sortie + Complement(Retraits, _lettreEnvoyee, _invitationEnvoyee, _vide, _finBlanche);
        }

        private bool _finBlanche = true;

        private bool _invitationEnvoyee;

        private string Sortir(string texte)
        {
            if (texte.Length == 0) return string.Empty;
            var sortie = Filtrer(texte, attendue, out var retraits);
            Retraits += retraits;
            if (ALettreHorsBlocs(sortie)) _lettreEnvoyee = true;
            if (Invite(sortie)) _invitationEnvoyee = true;
            if (sortie.Trim().Length > 0) _vide = false;
            if (sortie.Length > 0) _finBlanche = char.IsWhiteSpace(sortie[^1]);
            return sortie;
        }

        /// <summary>Le texte entier filtré : ce que l'enfant a vu et entendu, à garder en base.</summary>
        public static string Retirer(string? texte, decimal attendue, out int retraits)
        {
            retraits = 0;
            if (string.IsNullOrEmpty(texte)) return texte ?? string.Empty;

            var sortie = Filtrer(texte, attendue, out retraits);
            return sortie + Complement(retraits, ALettreHorsBlocs(sortie), Invite(sortie), sortie.Trim().Length == 0,
                sortie.Length == 0 || char.IsWhiteSpace(sortie[^1]));
        }

        /// <summary>Ce qui relance l'enfant quand le retrait a emporté la question (« C'est pas grave ! » tout seul).</summary>
        public const string Relance = "À toi de trouver : essaie encore !";

        /// <summary>
        /// Après un retrait seulement : plus une lettre à dire → la phrase neutre ; plus de question ni d'invitation (ni de
        /// passage à autre chose) → la relance. Sinon rien.
        /// </summary>
        private static string Complement(int retraits, bool lettre, bool invitation, bool vide, bool finBlanche)
        {
            if (retraits == 0) return string.Empty;
            var espace = vide || finBlanche ? "" : " ";
            if (!lettre) return espace + PhraseDeRepli;
            return invitation ? string.Empty : espace + Relance;
        }

        // Une question, une invitation à chercher, ou un passage à autre chose (« On passe à un autre calcul ») : l'enfant
        // sait quoi faire.
        [GeneratedRegex(@"\?|(?<!\p{L})(?:essaie|essayer|réessaie|à\s+toi|vas-y|dis-moi|tu\s+me\s+dis|recompte|compte|redis|redire|répète|continue|passe|passer|autre|suivant|suivante|on\s+(?:va|fait|cherche)|regarde|montre)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex Invitation();

        private static bool Invite(string texte) =>
            Invitation().IsMatch(Regex.Replace(texte, @"\[(?<tag>[A-Z_]{2,})\][\s\S]*?(?:\[/\k<tag>\]|$)", " "));

        /// <summary>Le texte énonce-t-il la réponse (phrase ou suite) ? Pour les preuves et les journaux.</summary>
        public static bool LaDonne(string? texte, decimal attendue)
        {
            if (string.IsNullOrEmpty(texte)) return false;
            Filtrer(texte, attendue, out var retraits);
            return retraits > 0;
        }

        // ───────────── La découpe ─────────────

        /// <summary>Les blocs appariés de l'application : ils partent entiers, une fois fermés.</summary>
        [GeneratedRegex(@"\[(?<tag>ARDOISE|FIGURE|RAPPORT|RAPPORT_ORIENTATION|FICHE|PROJET|JEU|EVALUATION|EVALUATION_PREVUE|EVALUATION_CORRIGEE|DICTEE|DICTEE_AU_TABLEAU|DICTEE_SUPPRIMEE|DICTEE_CORRIGEE|COPIE_CONTROLE|EXPRESSION_ECRITE|EXPRESSION_ORALE|COMPREHENSION_SUPPRIMEE|A_ECOUTER|LECTURE_IMAGE|EXAMEN_PRET|ENONCE_EXERCICE|CONTROLE_RESULTAT|CONTROLE_PRET|CONTROLE_PROGRAMME|CONTROLE_NOTIONS|NOM)\]")]
        private static partial Regex OuvertureDeBloc();

        /// <summary>Un nombre en lettres : des mots-nombres liés par des traits d'union, ou « vingt et un ».</summary>
        private const string MotNombre = @"(?:zéro|zero|un|une|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize|vingts?|trente|quarante|cinquante|soixante|cents?|mille)";

        [GeneratedRegex(@"(?<![\p{L}\d])(?:(?<chiffres>\d{1,9}(?:,\d{1,6})?)(?![\d\p{L}])|(?<lettres>" + MotNombre + @"(?:(?:-|-et-|\s+et\s+(?=(?:un|une|onze)(?!\p{L})))" + MotNombre + @")*)(?![\p{L}\d]))",
            RegexOptions.IgnoreCase)]
        private static partial Regex Nombre();

        /// <summary>
        /// La dernière position où l'on peut couper sans rien changer au résultat : la fin d'une phrase finie, ou la
        /// fin d'un bloc fermé ; 0 si aucune. <paramref name="fin"/> : tout le texte est là.
        /// </summary>
        private static int DerniereCoupure(string texte, bool fin)
        {
            var derniere = 0;
            foreach (var (_, finSegment, final) in Segments(texte, fin))
            {
                if (!final) break;
                derniere = finSegment;
            }
            return derniere;
        }

        /// <summary>
        /// Les morceaux du texte : (début, fin, sûr). Un bloc apparié de son ouverture à sa fermeture ; ailleurs, des
        /// phrases, blancs de fin compris. Le dernier morceau n'est « sûr » que si ce qui le suit est connu.
        /// </summary>
        private static IEnumerable<(int Debut, int Fin, bool Sur)> Segments(string texte, bool fin)
        {
            var i = 0;
            while (i < texte.Length)
            {
                // Un bloc apparié qui commence ici : jusqu'à sa fermeture.
                var bloc = OuvertureDeBloc().Match(texte, i);
                var finPhrase = FinDePhrase(texte, i, fin, out var phraseSure);

                if (bloc.Success && bloc.Index < finPhrase)
                {
                    if (bloc.Index > i)
                    {
                        // Le texte avant le bloc est un morceau à lui seul (il finit au bloc, qui est un sûr séparateur).
                        yield return (i, bloc.Index, true);
                        i = bloc.Index;
                        continue;
                    }

                    var fermeture = "[/" + bloc.Groups["tag"].Value + "]";
                    var f = texte.IndexOf(fermeture, bloc.Index + bloc.Length, StringComparison.Ordinal);
                    if (f < 0)
                    {
                        yield return (i, texte.Length, fin);
                        yield break;
                    }
                    var apres = f + fermeture.Length;
                    yield return (i, apres, true);
                    i = apres;
                    continue;
                }

                // Un crochet ouvert à la toute fin du texte peut être le début d'un bloc : on attend.
                if (!fin && !phraseSure)
                {
                    yield return (i, texte.Length, false);
                    yield break;
                }

                if (!fin && phraseSure && Regex.IsMatch(texte[i..finPhrase], @"\[[A-Z_/]*$"))
                {
                    yield return (i, texte.Length, false);
                    yield break;
                }

                yield return (i, finPhrase, phraseSure);
                if (!phraseSure) yield break;
                i = finPhrase;
            }
        }

        /// <summary>
        /// La fin de la phrase qui commence en <paramref name="debut"/>, blancs suivants compris. <paramref name="sure"/>
        /// est faux quand on ne peut pas encore le savoir (texte qui s'arrête trop tôt).
        /// </summary>
        private static int FinDePhrase(string texte, int debut, bool fin, out bool sure)
        {
            for (var j = debut; j < texte.Length; j++)
            {
                var c = texte[j];
                if (c == '[' && j > debut && OuvertureDeBloc().Match(texte, j) is { Success: true } m && m.Index == j)
                {
                    sure = true;
                    return j;
                }

                if (c == '\n')
                {
                    var k = j;
                    while (k < texte.Length && char.IsWhiteSpace(texte[k])) k++;
                    sure = k < texte.Length || fin;
                    return k;
                }

                if (c is not ('.' or '!' or '?' or '…')) continue;

                // La ponctuation entière (« !! », « ?! », « ... »).
                var p = j;
                while (p < texte.Length && texte[p] is '.' or '!' or '?' or '…') p++;
                if (p >= texte.Length)
                {
                    sure = fin;
                    return texte.Length;
                }
                if (!char.IsWhiteSpace(texte[p]))
                {
                    // « 2.5 », « …12 » : pas une fin de phrase.
                    j = p - 1;
                    continue;
                }

                var suite = p;
                while (suite < texte.Length && char.IsWhiteSpace(texte[suite])) suite++;
                if (suite >= texte.Length)
                {
                    sure = fin;
                    return texte.Length;
                }

                // Des points de suspension suivis d'un nombre ne finissent pas la phrase : « 8... 9... 10 ».
                var ponctuation = texte[j..p];
                if (ponctuation.Contains('…') || ponctuation.Contains(".."))
                {
                    // « onze... et douze », « 10... puis 11 » : le petit mot de liaison ne coupe pas non plus.
                    var regard = suite;
                    var liaison = Regex.Match(texte[suite..], @"^(?:et|puis|encore)\s+", RegexOptions.IgnoreCase);
                    if (liaison.Success && suite + liaison.Length < texte.Length) regard += liaison.Length;
                    else if (liaison.Success && !fin)
                    {
                        sure = false;
                        return texte.Length;
                    }
                    var mot = Regex.Match(texte[regard..], @"^[«""(]*[\p{L}\d-]+");
                    if (!mot.Success)
                    {
                        sure = true;
                        return suite;
                    }
                    if (regard + mot.Length >= texte.Length && !fin)
                    {
                        // Le mot suivant n'est peut-être pas fini.
                        sure = false;
                        return texte.Length;
                    }
                    var motSeul = mot.Value.TrimStart('«', '"', '(');
                    if (Nombre().Match(motSeul) is { Success: true } n && n.Index == 0 && n.Length == motSeul.Length)
                    {
                        j = regard - 1;
                        continue;
                    }
                }

                sure = true;
                return suite;
            }

            sure = fin;
            return texte.Length;
        }

        // ───────────── Le filtre ─────────────

        private static string Filtrer(string texte, decimal attendue, out int retraits)
        {
            retraits = 0;
            var sortie = new StringBuilder();
            foreach (var (debut, finSegment, _) in Segments(texte, fin: true))
            {
                var morceau = texte[debut..finSegment];
                var bloc = OuvertureDeBloc().Match(morceau);
                if (bloc.Success && bloc.Index == 0)
                {
                    sortie.Append(bloc.Groups["tag"].Value == "ARDOISE" ? Tableau(morceau, attendue, ref retraits) : morceau);
                    continue;
                }
                sortie.Append(Phrase(morceau, attendue, ref retraits));
            }
            return sortie.ToString();
        }

        private static bool ALettreHorsBlocs(string texte)
        {
            var sansBlocs = Regex.Replace(texte, @"\[(?<tag>[A-Z_]{2,})\][\s\S]*?(?:\[/\k<tag>\]|$)", " ");
            sansBlocs = Regex.Replace(sansBlocs, @"\[[A-Z_/]+\]", " ");
            return Regex.IsMatch(sansBlocs, @"\p{L}");
        }

        /// <summary>Une phrase hors bloc : sa suite coupée avant la réponse, puis retirée si elle l'énonce encore.</summary>
        private static string Phrase(string phrase, decimal attendue, ref int retraits)
        {
            // Une phrase qui énonce la réponse part en entière, suite comprise (« Après 10, ça fait 11 ! » ne devient pas
            // « Après 10… ») ; sinon, seule sa suite s'arrête avant la réponse.
            var coupee = phrase;
            if (!Enonce(phrase, attendue).Any())
            {
                var retraitsAvant = retraits;
                coupee = SansFinDeSuite(phrase, attendue, ref retraits);
                if (!Enonce(coupee, attendue).Any()) return coupee;
                retraits = retraitsAvant;
            }

            retraits++;

            // La phrase part, ses blancs de fin aussi — sauf un saut de ligne, qui sépare encore ce qui l'entoure. Ses blancs
            // de tête restent : ils la séparaient d'un bloc (« [/ARDOISE] 7 + 5, ça fait 12. On refait ? »).
            var tete = coupee[..(coupee.Length - coupee.TrimStart().Length)];
            var blancs = coupee[coupee.TrimEnd().Length..];
            if (tete.Length == coupee.Length) return coupee;
            return tete + (blancs.Contains('\n') ? blancs : string.Empty);
        }

        /// <summary>Au tableau : l'égalité garde son trou, la suite s'arrête avant, une réponse énoncée devient « ? ».</summary>
        private static string Tableau(string bloc, decimal attendue, ref int retraits)
        {
            var coupe = SansFinDeSuite(bloc, attendue, ref retraits);
            var trouves = Enonce(coupe, attendue).OrderByDescending(m => m.Index).ToList();
            foreach (var (index, longueur) in trouves)
            {
                coupe = coupe[..index] + "?" + coupe[(index + longueur)..];
                retraits++;
            }

            // Ce qui reste écrit de la réponse au tableau, seul (« 70… : soixante et onze ») : un « ? ». Pas un terme de calcul
            // (« 10 - 5 = ? » attend 5 : ce 5-là est la question), ni un nombre d'une suite qui continue après lui (« 75, 76,
            // 77 » : l'exercice suivant).
            var lus = Nombres(coupe);
            bool Lie(int a, int b) => a >= 0 && b < lus.Count
                && SeparateurDeSuite().IsMatch(coupe[(lus[a].Index + lus[a].Longueur)..lus[b].Index]);
            var seuls = lus.Select((n, i) => (n, i))
                .Where(x => x.n.Valeur == attendue && !TermeDeCalcul(coupe, x.n) && !Lie(x.i - 1, x.i) && !Lie(x.i, x.i + 1))
                .Select(x => x.n)
                .OrderByDescending(n => n.Index)
                .ToList();
            foreach (var n in seuls)
            {
                coupe = coupe[..n.Index] + "?" + coupe[(n.Index + n.Longueur)..];
                retraits++;
            }
            return coupe;
        }

        private sealed record Lu(int Index, int Longueur, decimal Valeur);

        private static List<Lu> Nombres(string texte)
        {
            var lus = new List<Lu>();
            foreach (Match m in Nombre().Matches(texte))
            {
                decimal? valeur = null;
                if (m.Groups["chiffres"].Success)
                {
                    if (decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d)) valeur = d;
                }
                else
                {
                    // « un » ou « une » seuls : un article (même exclusion que NombresEnLettres).
                    var lu = NombresEnLettres.Lire(m.Value);
                    if (lu.Count == 1) valeur = lu[0].Valeur;
                }
                if (valeur is not null) lus.Add(new Lu(m.Index, m.Length, valeur.Value));
            }
            return lus;
        }

        // Ce qui sépare deux nombres d'une suite qu'on compte : virgules, points de suspension, flèches, « puis », « et »,
        // guillemets, et une petite parenthèse (« 8 (un doigt), 9 (deux doigts) »).
        // « 7, puis 1 de plus ça fait 8, encore un ça fait 9, encore un 10 » : les mots du comptage aussi.
        [GeneratedRegex(@"^(?:\s|,|;|\.{2,}|…|→|->|=>|–|—|-(?=\s)|«|»|""|\bpuis\b|\bet\b|\bencore\b|\bune?\b|\bde\s+plus\b|\b(?:ça|ca)\s+fait\b|\([^()\n]{0,40}\))+$", RegexOptions.IgnoreCase)]
        private static partial Regex SeparateurDeSuite();

        // Deux nombres seulement ne font une suite que s'ils se suivent (« 11, 12 », « onze et douze ») ou si le lien dit
        // qu'on compte (« 10, 20 », « 10 → 12 ») : « entre 8 et 12 » n'en est pas une.
        [GeneratedRegex(@"[,…→]|\.{2,}|->|\bpuis\b", RegexOptions.IgnoreCase)]
        private static partial Regex LienDeComptage();

        // Après le dernier nombre : ses guillemets, sa petite parenthèse, sa ponctuation de suspension ou d'exclamation.
        // Et la suite qui continuait sans nombre au tableau : « 74, 75, ... » → « 74… », pas « 74…, ... ».
        [GeneratedRegex(@"^(?:\s*(?:""|»))?(?:\s*\([^()\n]{0,40}\))?\s*(?:""|»)?\s*(?:,\s*(?:\.{2,}|…)|\.{2,}|…|!+|\.(?!\d))?")]
        private static partial Regex FinDuDernier();

        // Ce qui reste collé à l'avant-dernier nombre : son guillemet fermant, sa petite parenthèse (« "11" (4e) »).
        [GeneratedRegex(@"^(?:\s*(?:""|»))?(?:\s*\([^()\n]{0,40}\))?")]
        private static partial Regex GardeDeLAvantDernier();

        private static bool TermeDeCalcul(string texte, Lu n) =>
            // Un opérateur entre deux nombres seulement : « pas : 11, 12 » et « 12 - essaie » ne calculent rien.
            Regex.IsMatch(texte[..n.Index], @"\d\s*(?:[+\-−–×*÷=]|(?<!\p{L})(?:x|plus|moins|fois)(?!\p{L}))\s*$", RegexOptions.IgnoreCase)
            || Regex.IsMatch(texte[(n.Index + n.Longueur)..], @"^\s*(?:[+\-−–×*÷=]|(?:x|plus|moins|fois)(?!\p{L}))\s*\d", RegexOptions.IgnoreCase)
            // Le « : » n'est une division que dans un calcul posé, « 12 : 3 = ? » ou « 12 : 3 ? » : « à partir de 10 : 11, 12 »
            // (conv. 722) et « compte après 10 : onze, douze » ne divisent rien.
            || (char.IsDigit(texte[n.Index])
                && (Regex.IsMatch(texte[..n.Index], @"\d\s*[:÷]\s*$") && Regex.IsMatch(texte[(n.Index + n.Longueur)..], @"^\s*(?:=|\?)")
                    || Regex.IsMatch(texte[(n.Index + n.Longueur)..], @"^\s*[:÷]\s*\d+(?:,\d+)?\s*(?:=|\?)")));

        /// <summary>Dans une parenthèse : « 8 (un doigt), 9 (deux doigts) » — le « deux » ne compte pas dans la suite.</summary>
        private static bool DansUneParenthese(string texte, int index)
        {
            var profondeur = 0;
            for (var i = 0; i < index; i++)
            {
                if (texte[i] == '(') profondeur++;
                else if (texte[i] == ')' && profondeur > 0) profondeur--;
            }
            return profondeur > 0;
        }

        /// <summary>
        /// « 8, 9, 10, 11, 12 » → « 8, 9, 10, 11… » quand la suite (au moins deux nombres d'un pas régulier) finit sur la
        /// réponse. Le reste du texte ne bouge pas.
        /// </summary>
        private static string SansFinDeSuite(string texte, decimal attendue, ref int retraits)
        {
            // Ni un nombre entre parenthèses, ni un terme de calcul (« 7 + 5, ça fait 12 » n'est pas la suite 5, 12 : c'est
            // une phrase qui énonce la réponse, et elle part en entier).
            var nombres = Nombres(texte)
                .Where(n => !DansUneParenthese(texte, n.Index) && !TermeDeCalcul(texte, n))
                .ToList();
            var coupes = new List<(int Debut, int Fin)>();

            var debutSuite = 0;
            for (var k = 1; k <= nombres.Count; k++)
            {
                var lie = k < nombres.Count
                    && nombres[k].Valeur != nombres[k - 1].Valeur
                    && SeparateurDeSuite().IsMatch(texte[(nombres[k - 1].Index + nombres[k - 1].Longueur)..nombres[k].Index]);
                var memePas = k - debutSuite < 2
                    || (k < nombres.Count && nombres[k].Valeur - nombres[k - 1].Valeur == nombres[k - 1].Valeur - nombres[k - 2].Valeur);

                if (lie && memePas) continue;

                // La suite [debutSuite, k-1] est finie : finit-elle sur la réponse ?
                var longueur = k - debutSuite;
                var dernier = nombres[k - 1];
                if (longueur >= 2 && dernier.Valeur == attendue)
                {
                    var avant = nombres[k - 2];
                    var pas = Math.Abs(dernier.Valeur - avant.Valeur);
                    var lien = texte[(avant.Index + avant.Longueur)..dernier.Index];
                    // Des CHOIX ne sont pas un comptage : « Ça fait 11, 12 ou 13 ? » (bout en bout, conv. 740) reste entier.
                    var choix = Regex.IsMatch(texte[(dernier.Index + dernier.Longueur)..], @"^\s*,?\s*ou\s+(?:[«""]\s*)?(?:\d|" + MotNombre + @"(?!\p{L}))", RegexOptions.IgnoreCase);
                    if (!choix && (longueur >= 3 ? pas <= 100 : pas == 1 || (pas <= 10 && LienDeComptage().IsMatch(lien))))
                    {
                        var finAvant = avant.Index + avant.Longueur;
                        var apresDernier = dernier.Index + dernier.Longueur;
                        var queue = FinDuDernier().Match(texte[apresDernier..]);
                        coupes.Add((finAvant, apresDernier + queue.Length));
                    }
                }

                // Le pas change mais le lien tient : le dernier nombre commence la suite suivante (« 5, 7, 8, 9 »).
                debutSuite = lie ? k - 1 : k;
            }

            if (coupes.Count == 0) return texte;

            var sortie = texte;
            foreach (var (debut, fin) in coupes.OrderByDescending(c => c.Debut))
            {
                // Une parenthèse juste après l'avant-dernier nombre (« 11 (quatre doigts) ») reste avec lui.
                var garde = GardeDeLAvantDernier().Match(sortie[debut..fin]);
                var debutCoupe = debut + garde.Length;
                var suite = sortie[fin..];
                var liaison = suite.Length > 0 && !char.IsWhiteSpace(suite[0]) && suite[0] is not (',' or ')') ? " " : "";
                sortie = sortie[..debutCoupe] + "…" + liaison + suite;
                retraits++;
            }
            return sortie;
        }

        // L'énoncé de la réponse R (le nombre) : ce qui le précède, puis R, puis rien qui en fasse autre chose.
        private const string AvantR = @"(?:"
            // « = 12 »
            + @"=\s*"
            // « ça fait 12 », « 7 + 5 font 12 », « ça donne bien 12 », « 7 + 5 égale 12 »
            + @"|(?<!\p{L})(?:fait|font|donne|donnent|vaut|valent|égale|égalent|fera|feront|faisait|faisaient|donnera|donnerait|ferait)\s+(?:(?:bien|donc|alors|exactement|juste|tout\s+juste|pile)\s+)?"
            // « 6 et 4, c'est un couple qui fait tout rond, 10 ! » (corpus de test)
            + @"|(?<!\p{L})(?:fait|font|donne|donnent)\s+(?:tout\s+rond|une\s+dizaine|une\s+dizaine\s+pile)\s*,?\s*(?:[:=]\s*)?"
            // « on arrive à 12 », « tu tombes sur 12 », « on s'arrête à 12 »
            + @"|(?<!\p{L})(?:arrive|arrives|arrivons|arrivez|arriver|arrivé|arrivée|tombe|tombes|tombons|tomber|arrête|arrêtes|arrêtons|arrêter|finit|finis|termine|termines)\s+(?:donc\s+|bien\s+|alors\s+)?(?:à|a|sur|jusqu['’]à)\s+"
            // « la réponse est 12 », « le résultat, c'est 12 », « la bonne réponse : 12 », « le total est de 12 »
            + @"|(?<!\p{L})(?:réponse|résultat|total|solution|somme)\s*(?:,|:)?\s*(?:c['’]est|est|était|sera|serait|:|=)?\s*(?:(?:bien|donc|alors|exactement|de)\s+)?"
            // « c'est 12 », « c'est bien 12 », « c'est encore 12 », « c'était 75 », « ce sera 12 »
            + @"|(?<!\p{L})(?:c['’]est|c['’]était|ce\s+sera(?:it)?)\s+(?:(?:bien|donc|alors|encore|juste|exactement|tout\s+simplement)\s+)?"
            // La suite des nombres (corpus de test, #60 sur master) : « après 74, on dit 75 », « il y a 75 », « on a 75 »,
            // « après 74 vient 75 », « on obtient 12 », « tu trouves 12 »
            + @"|(?<!\p{L})(?:on\s+(?:dit|disait|a|avait|obtient|trouve|tombe\s+sur)|il\s+y\s+a(?:vait)?|vient|viennent|tu\s+(?:obtiens|trouves))\s+(?:(?:bien|donc|alors|juste|ensuite)\s+)?"
            // « Dis-le avec moi : soixante-quatorze », « répète après moi : 75 », « tu peux me redire 75 ? »
            + @"|(?<!\p{L})(?:avec\s+moi|après\s+moi|(?:re)?dire|redis|répète|répéter)\s*:\s*(?:[«""]\s*)?"
            + @"|(?<!\p{L})redire\s+(?:ce\s+nombre\s*,?\s*|le\s+nombre\s+)?(?:[«""]\s*)?"
            // « il y en a 12 en tout » : voir AvecEnTout.
            + @")";

        /// <summary>« 12 » n'est pas la réponse s'il est suivi d'un rang, d'un pourcentage ou d'un autre calcul.</summary>
        private const string ApresR = @"(?!\s*(?:dizaines?|unités?|centaines?|%|[+\-−–×x*:÷]\s*\d|ans\b|euros?\b))";

        [GeneratedRegex(@"(?<!\p{L})(?:il\s+y\s+(?:en\s+)?a|on\s+(?:en\s+)?a|tu\s+(?:en\s+)?as|j['’]en\s+ai|ça\s+fait|ca\s+fait)\s+$", RegexOptions.IgnoreCase)]
        private static partial Regex AvantEnTout();

        [GeneratedRegex(@"^\s*(?:\p{L}+\s+)?en\s+tout", RegexOptions.IgnoreCase)]
        private static partial Regex EnToutApres();

        // « 12, c'est la réponse », « 12 est la bonne réponse », « 12 ! C'est ça le résultat »
        [GeneratedRegex(@"^\s*,?\s*(?:c['’]est|est|sera)\s+(?:bien\s+)?(?:la\s+(?:bonne\s+)?réponse|le\s+(?:bon\s+)?résultat|le\s+total|ça\s+la\s+réponse|ça\s+le\s+résultat)", RegexOptions.IgnoreCase)]
        private static partial Regex RAnnonce();

        /// <summary>Les nombres R du texte qui énoncent la réponse : (position, longueur).</summary>
        [GeneratedRegex(AvantR + "$", RegexOptions.IgnoreCase)]
        private static partial Regex AvantLaReponse();

        [GeneratedRegex("^" + ApresR, RegexOptions.IgnoreCase)]
        private static partial Regex ApresLaReponse();

        private static IEnumerable<(int Index, int Longueur)> Enonce(string texte, decimal attendue)
        {
            var avant = AvantLaReponse();
            var apres = ApresLaReponse();

            foreach (var n in Nombres(texte))
            {
                if (n.Valeur != attendue) continue;

                var gauche = texte[..n.Index];
                var droite = texte[(n.Index + n.Longueur)..];
                if (!apres.IsMatch(droite)) continue;

                // À gauche, on ne regarde que la phrase en cours (le texte après la dernière ponctuation forte).
                var debutPhrase = Math.Max(Math.Max(gauche.LastIndexOf('.'), gauche.LastIndexOf('!')), Math.Max(gauche.LastIndexOf('?'), gauche.LastIndexOf('\n')));
                var proche = gauche[(debutPhrase + 1)..];

                if (avant.IsMatch(proche)
                    || (AvantEnTout().IsMatch(proche) && EnToutApres().IsMatch(droite))
                    || RAnnonce().IsMatch(droite))
                {
                    yield return (n.Index, n.Longueur);
                }
            }
        }
    }
}
