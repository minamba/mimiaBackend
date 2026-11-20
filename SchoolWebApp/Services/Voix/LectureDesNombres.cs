using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LIRE LES NOMBRES AFFICHÉS AU TABLEAU — Camara, le 12/11/2026 : « tu peux tout construire sur la même base que
    /// l'exercice des syllabes et des mots ». Du CP à la 3e, en maths : le CODE choisit un nombre de la classe de l'enfant
    /// (et de la classe d'avant, pour réviser), l'affiche au tableau par une balise [LECTURE_IMAGE:nombre]cible
    /// [/LECTURE_IMAGE], écrit la consigne, et JUGE ce que l'enfant a lu. Le professeur ne dit jamais le nombre à lire.
    ///
    /// LE JUGEMENT SE FAIT SUR LE TEXTE, PAS SUR LE SON : le micro écrit « soixante-treize » ou « 73 », « trois virgule
    /// vingt-cinq », « trois quarts », « dix puissance moins trois », « racine carrée de deux »… Chaque lecture est ramenée
    /// à une écriture canonique (« 73 », « 3,25 », « 3/4 », « 10^-3 », « rac2 ») et comparée à la cible. Rien ne passe
    /// par le relais du micro ni par le juge du son : le français du primaire n'est pas touché.
    ///
    /// LE CATALOGUE EST CELUI DU PDF « Mimia - Nombres a illustrer (CP-Terminale) » : la cible donne aussi le nom de
    /// l'image (`nombresALire.js`, côté écran). Le lycée n'y est pas : ce n'est plus un exercice de lecture.
    /// </summary>
    public static partial class LectureDesNombres
    {
        public const string Genre = "nombre";

        public sealed record Element(string Cible, int Rang);

        /// <summary>Ce que le code montre à ce tour : l'élément, s'il est redemandé ou nouveau, et s'il vient d'abandonner.</summary>
        public sealed record Tour(Element Element, bool Redite, bool Premier, bool Abandon, bool Demande = false);

        /// <summary>Le verdict du message de l'enfant : ce qu'il devait lire, s'il est juste, et ce qu'il a dit.</summary>
        public sealed record Verdict(Element Montre, bool Juste, string Dit);

        // ------------------------------------------------------------------ le catalogue (PDF du 12/11/2026)

        private static readonly (int Rang, string[] Cibles)[] Catalogue =
        [
            (1, Enumerable.Range(0, 101).Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray()),
            (2, ["101", "108", "110", "115", "120", "170", "180", "190", "199", "200", "208", "250", "271", "300", "345", "391", "400",
                 "470", "491", "500", "512", "600", "666", "700", "709", "780", "800", "871", "880", "900", "960", "999", "1000"]),
            (3, ["1001", "1010", "1100", "1200", "1500", "1999", "2000", "2008", "2026", "2470", "3000", "3333", "4091", "4500", "5000",
                 "5780", "6300", "7071", "7999", "8000", "8180", "9000", "9090", "9999", "10000"]),
            (4, ["10500", "12345", "20000", "45080", "70071", "99999", "100000", "101010", "250000", "480300", "600006", "999999",
                 "1000000", "0,5", "0,25", "1,2", "2,5", "3,75", "7,08", "10,05", "12,4", "1/2", "1/3", "1/4", "3/4", "2/3", "5/10"]),
            (5, ["1200000", "3450000", "10000000", "25600080", "100000000", "999999999", "1000000000", "2500000000", "0,001",
                 "0,125", "3,141", "12,007", "45,6", "100,01", "7/10", "25/100", "3/5", "5/8", "7/4", "3/1000"]),
            (6, ["12000000000", "304050607", "0,75", "0,09", "104,05", "3,5", "2026,26", "1/5", "2/7", "11/6", "9/4", "13/100"]),
            (7, ["-1", "-5", "-12", "+4", "-100", "-0,5", "-3,7", "-2026", "5/3", "-2/5", "-1/2", "7/9"]),
            (8, ["10^2", "10^3", "10^6", "10^-1", "10^-3", "2^3", "3^2", "5^2", "(-2)^2", "-2^2", "2,5x10^4", "3x10^-2", "1,5x10^8",
                 "6,02x10^23"]),
            (9, ["rac9", "rac16", "rac25", "rac2", "rac50", "3rac2", "-rac3", "10^-6"]),
        ];

        public static IReadOnlyList<Element> Tous { get; } =
            Catalogue.SelectMany(c => c.Cibles.Select(cible => new Element(cible, c.Rang))).ToList();

        private static readonly Dictionary<string, int> Rangs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CP"] = 1, ["CE1"] = 2, ["CE2"] = 3, ["CM1"] = 4, ["CM2"] = 5,
            ["SIXIEME"] = 6, ["6E"] = 6, ["CINQUIEME"] = 7, ["5E"] = 7, ["QUATRIEME"] = 8, ["4E"] = 8,
            ["TROISIEME"] = 9, ["TROISIEME_PREPA"] = 9, ["3E"] = 9,
        };

        /// <summary>Le rang de la classe (CP = 1 … 3e = 9) ; 0 hors de l'exercice (lycée, inconnu).</summary>
        public static int Rang(string? niveau) => niveau is not null && Rangs.TryGetValue(niveau.Trim(), out var r) ? r : 0;

        /// <summary>L'exercice existe pour ce cours : les maths, du CP à la 3e.</summary>
        public static bool Concerne(string? agentSlug, string? niveau) =>
            agentSlug is not null && agentSlug.Contains("math", StringComparison.OrdinalIgnoreCase) && Rang(niveau) > 0;

        /// <summary>Ce qu'il peut avoir à lire : sa classe, et celle d'avant pour réviser.</summary>
        public static IReadOnlyList<Element> Possibles(string? niveau)
        {
            var rang = Rang(niveau);
            return Tous.Where(e => e.Rang == rang || e.Rang == rang - 1).ToList();
        }

        // ------------------------------------------------------------------ la balise

        [GeneratedRegex(@"\[LECTURE_IMAGE:nombre\]([^\[\]]+)\[/LECTURE_IMAGE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Marqueur();

        public static string Balise(Element e) => $"[LECTURE_IMAGE:{Genre}]{e.Cible}[/LECTURE_IMAGE]";

        /// <summary>Le dernier nombre montré dans ce message, ou null.</summary>
        public static Element? Affiche(string? contenu)
        {
            if (string.IsNullOrEmpty(contenu)) return null;
            var trouve = Marqueur().Matches(contenu).LastOrDefault();
            if (trouve is null) return null;
            var cible = trouve.Groups[1].Value.Trim();
            return Tous.FirstOrDefault(e => e.Cible == cible) ?? new Element(cible, 0);
        }

        // ------------------------------------------------------------------ entrer, rester, sortir

        // « Je veux lire des nombres », « on lit des chiffres », « la lecture des nombres ».
        [GeneratedRegex(@"\b(?:lire|lis|lisons|lit|lecture)\b[^.?!\n]{0,30}\b(?:nombres?|chiffres?)\b|\b(?:nombres?|chiffres?)\b[^.?!\n]{0,20}\b(?:à|a)\s+lire\b", RegexOptions.IgnoreCase)]
        private static partial Regex DemandeDeLecture();

        // La proposition du professeur : « Tu veux qu'on lise des nombres ? »
        [GeneratedRegex(@"\b(?:lire|lise|lisions|lecture)\b[^?\n]{0,30}\bnombres?\b[^?\n]*\?", RegexOptions.IgnoreCase)]
        private static partial Regex Proposition();

        [GeneratedRegex(@"^\s*(?:oui|ouais|ouai|d'accord|dac|ok|okay|vas-y|vas y|allez|go|on y va|carr[ée]ment|volontiers|avec plaisir|je veux bien|bien s[uû]r)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Accepte();

        // Il veut arrêter ou changer : l'exercice s'arrête là.
        [GeneratedRegex(@"\b(?:arr[êe]te|arr[êe]ter|stop|autre\s+(?:chose|exercice)|on\s+change|change(?:r)?\s+d.exercice|plus\s+de\s+nombres|j.en\s+ai\s+marre|c.est\s+bon\s+les\s+nombres|on\s+fait\s+autre)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Arret();

        private static bool EstTechnique(string? contenu) =>
            string.IsNullOrWhiteSpace(contenu) || Regex.IsMatch(contenu, @"^\s*\[(?:FICHE|RAPPORT|FIN_SEANCE|EVALUATION)\]", RegexOptions.IgnoreCase);

        /// <summary>Le dernier message du professeur qui parle à l'élève (ni rapport, ni fiche).</summary>
        private static string? DernierDuProfesseur(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents) =>
            recents.LastOrDefault(m => m.Role == "assistant" && !EstTechnique(m.Contenu)).Contenu;

        /// <summary>
        /// L'élève est-il dans l'exercice à ce tour ? Il le demande, demande d'afficher un nombre de sa classe (le
        /// <paramref name="niveau"/> sert alors), accepte la proposition, ou y était déjà.
        /// </summary>
        public static bool EnCours(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents, string? messageDeLEleve,
            string? niveau = null)
        {
            var message = messageDeLEleve ?? string.Empty;
            if (DemandeDeLecture().IsMatch(message)) return true;
            // « Propose-moi un nouvel exercice » (14/11/2026) : la même règle que partout, voir ChangementDExercice.
            if (Arret().IsMatch(message) || ChangementDExercice.Demande(message)) return false;

            // IL DEMANDE D'AFFICHER UN NOMBRE (commentaire de Min's sur le signalement #50, 20/11/2026 : « quand l'élève
            // demande d'afficher un chiffre, Nora devra afficher l'image correspondante, comme pour les exercices de
            // comptage où elle affiche les images »). « Affiche le nombre 23 », « écris-moi 7 au tableau », même hors de
            // la lecture des nombres : le code montre l'IMAGE du nombre (nb-23), au lieu de laisser le professeur l'écrire
            // à la craie. Seulement un nombre du catalogue de sa classe ou d'une classe en dessous (il a une image).
            if (NombreDemande(message, niveau) is not null) return true;

            var dernier = DernierDuProfesseur(recents);
            if (dernier is null) return false;
            if (Affiche(dernier) is not null) return true;

            // Il dit oui à « Tu veux qu'on lise des nombres ? » — une réponse courte, pas une phrase.
            return Proposition().IsMatch(dernier) && Accepte().IsMatch(message) && message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 8;
        }

        /// <summary>Le nombre qu'il avait à lire à ce tour (celui du dernier message du professeur), ou null.</summary>
        public static Element? DernierMontre(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents) =>
            Affiche(DernierDuProfesseur(recents));

        /// <summary>
        /// LE VERDICT DE SON MESSAGE : null s'il n'y a pas de nombre à lire, ou si ce qu'il dit n'est pas une lecture
        /// (« je sais pas », une question) — l'image reste, l'essai n'est pas compté.
        /// </summary>
        public static Verdict? Juger(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents, string? messageDeLEleve)
        {
            var montre = DernierMontre(recents);
            if (montre is null || string.IsNullOrWhiteSpace(messageDeLEleve) || DemandeDeLecture().IsMatch(messageDeLEleve)) return null;
            // « Écris le nombre 23 » n'est pas une lecture du nombre affiché : c'est une demande (signalement #50).
            if (CanoniqueDemande(messageDeLEleve) is not null) return null;
            var lu = Canonique(messageDeLEleve);
            if (lu is null) return null;
            return new Verdict(montre, lu == Normaliser(montre.Cible), messageDeLEleve.Trim());
        }

        /// <summary>Ce que garde le message de l'enfant : « J'ai lu le nombre soixante-treize. »</summary>
        public static string TexteDeLEleve(Verdict v) =>
            // Déjà écrit par le relais du micro (EcouteController) : on ne l'enveloppe pas une seconde fois.
            VerdictDeLecture().IsMatch(v.Dit) ? v.Dit.Trim() : $"J'ai lu le nombre {v.Dit.Trim().TrimEnd('.', '!', ' ')}.";

        [GeneratedRegex(@"^\s*j.ai lu le nombre\b", RegexOptions.IgnoreCase)]
        private static partial Regex VerdictDeLecture();

        // ------------------------------------------------------------------ le nombre qu'il demande (signalement #50)

        // « Écris le nombre 23 au tableau », « montre-moi 48 », « affiche le chiffre sept », « mets-moi un autre nombre : 12 »,
        // « je veux que tu affiches 23 ».
        // Le verbe, puis seulement des petits mots de consigne, puis le nombre. « un » / « une » ne sont des mots de
        // consigne que devant « nombre », « chiffre », « autre » ou « nouveau » (« écris un nombre » ne demande pas « 1 »).
        // « Je propose 23 », « je mets 23 », « j'écris 23 », « je te donne 23 » : c'est SA réponse, pas une demande
        // (sinon une réponse à un calcul ouvrirait la lecture des nombres, et une lecture serait prise pour une demande).
        [GeneratedRegex(@"(?<!\p{L})(?<!\bje\s+(?:te\s+|t['’]\s*)?|\bj['’]\s*)(?:[ée]cri(?:s|re|vez|ves?)|affiche[rsz]?|montre[rsz]?|mets|mettre|mettes?|donne[rz]?|propose[rz]?)(?!\p{L})"
            + @"(?:[-\s,:'’]+(?:moi|nous|le|la|l|ce|cet|au|du|tableau|nombre|chiffre|autre|nouveau|s|il|te|pla[iî]t|stp|maintenant|alors|plut[oô]t|(?:un|une)(?=[-\s]+(?:nombre|chiffre|autre|nouveau)))(?!\p{L}))*"
            + @"[-\s,:'’«""]+(?<suite>[^.!?;\n]{1,60})", RegexOptions.IgnoreCase)]
        private static partial Regex Demande();

        [GeneratedRegex(@"^(?:au|aux|sur|a|et|s'il|s’il|stp|svp|pour|que|qu|je|j|tableau|en|dans|ici|maintenant|merci|alors|puis|stp|please)\b", RegexOptions.IgnoreCase)]
        private static partial Regex MotApresLeNombre();

        /// <summary>Le nombre qu'il demande d'afficher, en écriture canonique (« 23 »), ou null.</summary>
        public static string? CanoniqueDemande(string? message)
        {
            if (string.IsNullOrWhiteSpace(message) || VerdictDeLecture().IsMatch(message)) return null;
            foreach (Match m in Demande().Matches(message))
            {
                // Seulement le nombre qui suit, mot à mot : « 23 au tableau et je te le lis » → « 23 ».
                var mots = new List<string>();
                string? apres = null;
                foreach (var brut in m.Groups["suite"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var mot = brut.Trim('«', '»', '"', '“', '”', ',');
                    var simple = SansAccents(mot.ToLowerInvariant());
                    var estNombre = Regex.IsMatch(mot, @"^\(?[+-]?\d[\d,./^x()−-]*$")
                        || simple.Split('-', StringSplitOptions.RemoveEmptyEntries).All(p => EstMotNombre(p) || p is "et" or "zero" or "virgule");
                    if (!estNombre || simple.Length == 0) { apres = simple; break; }
                    mots.Add(mot);
                }
                while (mots.Count > 0 && SansAccents(mots[^1].ToLowerInvariant()) is "et" or "virgule") mots.RemoveAt(mots.Count - 1);
                if (mots.Count == 0) continue;
                // « Donne-moi deux exemples », « montre-moi un indice » : un nom suit, ce n'est pas un nombre à afficher.
                if (apres is not null && !MotApresLeNombre().IsMatch(apres)) continue;
                if (Canonique(string.Join(' ', mots)) is { } lu) return lu;
            }
            return null;
        }

        /// <summary>Le nombre demandé, s'il est au catalogue de sa classe ou d'une classe en dessous ; null sinon.</summary>
        public static Element? NombreDemande(string? message, string? niveau)
        {
            var rang = Rang(niveau);
            if (rang == 0 || CanoniqueDemande(message) is not { } lu) return null;
            return Tous.FirstOrDefault(e => e.Rang >= 1 && e.Rang <= rang && Normaliser(e.Cible) == lu);
        }

        // ------------------------------------------------------------------ ce que le code montre

        private const int EssaisMax = 3;

        /// <summary>
        /// LE NOMBRE DE CE TOUR. Le même tant qu'il ne l'a pas lu juste ; un autre après une bonne lecture, après trois
        /// lectures fausses de suite (on ne donne jamais la réponse, on passe), ou au début de l'exercice.
        /// </summary>
        public static Tour? AMontrer(string? niveau, IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents, Verdict? verdict, string graine,
            string? messageDeLEleve = null)
        {
            var possibles = Possibles(niveau);
            if (possibles.Count == 0) return null;

            var precedent = DernierMontre(recents);

            // IL DEMANDE UN NOMBRE PRÉCIS (20/11/2026, signalement #50 : « Écris le nombre 23 au tableau, et je te le lis »,
            // le code en montrait un autre, et « vingt-trois » était refusé). Hors d'une lecture en cours de jugement, le
            // nombre demandé est celui du tour, s'il est au catalogue de sa classe ou d'une classe en dessous.
            if (verdict is null && NombreDemande(messageDeLEleve, niveau) is { } demande)
            {
                return new Tour(demande, Redite: precedent?.Cible == demande.Cible, Premier: precedent is null, Abandon: false, Demande: true);
            }

            // Les lectures fausses de suite sur ce nombre, celle de ce tour comprise.
            var abandon = false;
            if (precedent is not null && verdict is { Juste: false })
            {
                var faux = 1;
                for (var i = recents.Count - 1; i >= 0; i--)
                {
                    var m = recents[i];
                    if (m.Role == "assistant" && !EstTechnique(m.Contenu))
                    {
                        if (Affiche(m.Contenu)?.Cible != precedent.Cible) break;
                        continue;
                    }
                    if (m.Role == "user" && VerdictDeLecture().IsMatch(m.Contenu ?? string.Empty))
                    {
                        if (m.SonJuste == false) faux++;
                        else break;
                    }
                }
                abandon = faux >= EssaisMax;
            }

            if (precedent is not null && verdict is not { Juste: true } && !abandon)
            {
                return new Tour(precedent, Redite: true, Premier: false, Abandon: false);
            }

            var dejaVus = recents.Select(m => Affiche(m.Contenu)?.Cible).Where(c => c is not null).ToHashSet();
            var neufs = possibles.Where(e => !dejaVus.Contains(e.Cible)).ToList();
            var pool = neufs.Count > 0 ? neufs : possibles.Where(e => e.Cible != precedent?.Cible).ToList();
            if (pool.Count == 0) pool = possibles.ToList();

            var choisi = pool[Indice(graine, pool.Count)];
            return new Tour(choisi, Redite: false, Premier: precedent is null, Abandon: abandon);
        }

        // ------------------------------------------------------------------ le message du professeur

        [GeneratedRegex(@"\[ARDOISE\][\s\S]*?\[/ARDOISE\]|\[TABLEAU_EFFACE\]|\[LECTURE_IMAGE:[^\]]*\][^\[]*\[/LECTURE_IMAGE\]|\[FIGURE\][\s\S]*?\[/FIGURE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Tableau();

        [GeneratedRegex(@"\[(FR|EN)\]\s*([^\[\]]+?)\s*\[/\1\]", RegexOptions.IgnoreCase)]
        private static partial Regex Voix();

        [GeneratedRegex(@"[^.!?\n]*\?+")]
        private static partial Regex Question();

        // Sa propre consigne (« Lis ce nombre à voix haute ») : celle du code la remplace.
        [GeneratedRegex(@"[^.!?\n]*\b(?:lis|lis-le|lire|relis|relis-le|relire)\b[^.!?\n]*\b(?:voix\s+haute|au\s+tableau|le\s+tableau|ce\s+nombre|le\s+nombre)\b[^.!?\n]*[.!]*", RegexOptions.IgnoreCase)]
        private static partial Regex ConsigneDuProfesseur();

        // « Écoute bien le nombre et dis-le-moi » (signalement #50) : il ne l'entend pas, il le LIT. La phrase s'en va,
        // la consigne du code (« Regarde bien le nombre… ») dit la bonne chose.
        [GeneratedRegex(@"[^.!?\n]*(?<!\p{L})(?:[ée]coute(?:s|z|r)?|entend(?:s|re)?)(?!\p{L})[^.!?\n]*[.!]*", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseDEcoute();

        [GeneratedRegex(@"\n{3,}")]
        private static partial Regex LignesVides();

        /// <summary>
        /// LE MESSAGE DU PROFESSEUR, REMIS EN ORDRE : sa phrase (bravo, encouragement), la consigne du code, la balise. Ce
        /// qu'il aurait écrit au tableau, ses questions et sa propre consigne s'en vont. Un au revoir reste tel quel.
        /// </summary>
        public static string Reecrire(string texte, Tour tour)
        {
            if (LectureDImage.ConclutLaSeance(texte)) return texte;

            var propre = Tableau().Replace(texte ?? string.Empty, " ");
            propre = Voix().Replace(propre, "$2");
            propre = Question().Replace(propre, string.Empty);
            propre = ConsigneDuProfesseur().Replace(propre, string.Empty);
            propre = PhraseDEcoute().Replace(propre, string.Empty);

            // Le nombre à lire lui-même, s'il l'a cité (il ne le connaît pas, mais une redite peut le reprendre) : la ligne part.
            var affiche = Affichage(tour.Element.Cible);
            if (tour.Redite)
            {
                propre = string.Join("\n", propre.Split('\n').Where(l => !l.Contains(affiche, StringComparison.Ordinal)
                    && !l.Contains(tour.Element.Cible, StringComparison.Ordinal)));
            }

            var lignes = propre.Split('\n').Select(l => Regex.Replace(l, @"[ \t]{2,}", " ").Trim());
            var corps = LignesVides().Replace(string.Join("\n", lignes), "\n\n").Trim();

            var morceaux = new List<string>();
            if (corps.Length > 0) morceaux.Add(corps);
            morceaux.Add(Consigne(tour));
            morceaux.Add(Balise(tour.Element));
            return string.Join("\n\n", morceaux);
        }

        /// <summary>
        /// La consigne du code. SES DÉBUTS SONT RECONNUS PAR L'ÉCRAN (`CONSIGNE_DE_LECTURE` dans lectureDImage.js) pour
        /// faire attendre la nouvelle image jusqu'à la voix : les deux se modifient ensemble.
        /// </summary>
        public static string Consigne(Tour tour) =>
            tour.Redite ? "Regarde bien le nombre, et essaie encore de le lire à voix haute."
            : tour.Premier || tour.Demande ? "Regarde le tableau : lis ce nombre à voix haute."
            : "Voici un autre nombre : lis-le à voix haute.";

        /// <summary>Le fait de l'exercice, à chaque tour.</summary>
        public const string FaitType =
            "[Fait pour ce tour : l'élève fait l'exercice de LECTURE DES NOMBRES. C'est le CODE qui choisit le nombre, l'affiche au "
            + "tableau et écrit la consigne à la fin de ton message. Toi, tu écris seulement une ou deux phrases courtes (bravo, "
            + "encouragement, un indice de méthode), SANS lui redemander de lire : la consigne est déjà écrite par le code. Tu "
            + "n'écris et ne prononces JAMAIS le nombre à lire, et tu n'écris rien au tableau (ni [ARDOISE], ni [FIGURE]). Tu ne "
            + "changes d'exercice que s'il le demande lui-même. Il LIT le nombre au tableau, il ne l'entend pas : ne dis jamais "
            + "« écoute » ni « tu as entendu », dis « regarde ».]";

        /// <summary>
        /// Le fait d'un nombre qu'il a demandé lui-même (signalement #50) : sans lui, le professeur répondait « je ne choisis
        /// pas moi-même le nombre, c'est l'exercice qui s'en charge » alors que le tableau montrait justement le sien.
        /// </summary>
        public const string FaitDemande =
            "[Fait pour ce tour : il a demandé lui-même ce nombre, et le code l'affiche au tableau. Dis-lui en une phrase "
            + "que c'est parti. Ne dis pas que tu ne choisis pas le nombre, et ne le prononce pas : c'est lui qui le lit.]";

        /// <summary>Le fait du verdict.</summary>
        public static string FaitVerdict(Verdict v, Tour? tour)
        {
            const string fin = " Ne parle jamais du micro ni de cet outil à l'élève.]";
            var cible = Affichage(v.Montre.Cible);
            var lu = Canonique(v.Dit) is { } c ? Affichage(c) : null;

            if (v.Juste)
            {
                return $"[Fait pour ce tour : il a bien lu le nombre {cible}. Son écran affiche déjà « Bonne lecture » : félicite-le en une "
                    + "phrase. Le code affiche le nombre suivant." + fin;
            }

            if (tour is { Abandon: true })
            {
                return $"[Fait pour ce tour : il devait lire {cible} et a dit « {v.Dit} »{(lu is null ? string.Empty : $" (soit {lu})")}, pour la "
                    + "troisième fois sans y arriver. Le code passe à un autre nombre. Dis-lui en une phrase que ce n'est pas grave et qu'on "
                    + "y reviendra. NE DONNE PAS la réponse et ne la prononce pas." + fin;
            }

            return $"[Fait pour ce tour : il devait lire {cible} et a dit « {v.Dit} »{(lu is null ? string.Empty : $" (soit {lu})")}. Son écran "
                + "affiche « On réessaie ». Encourage-le en une phrase et, si tu vois l'erreur (soixante-dix et quatre-vingts, un zéro "
                + "oublié, la virgule, le signe moins, le dénominateur), donne un indice de MÉTHODE. NE DONNE JAMAIS la réponse et ne la "
                + "prononce pas : le code lui remet le même nombre." + fin;
        }

        /// <summary>La cible telle qu'on l'écrit pour le professeur : « 12 345 », « 3/4 », « 10^-3 », « 3√2 ».</summary>
        public static string Affichage(string cible) =>
            Regex.Replace(cible.Replace("rac", "√").Replace("x10^", " × 10^"), @"\d{4,}", m =>
                long.Parse(m.Value, CultureInfo.InvariantCulture).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' '));

        // ------------------------------------------------------------------ lire ce qu'il a dit

        /// <summary>L'écriture canonique d'une cible : « +4 » vaut « 4 », « 3,50 » vaut « 3,5 ».</summary>
        public static string Normaliser(string cible)
        {
            var c = cible.Trim();
            if (Regex.IsMatch(c, @"^[+-]?\d+(,\d+)?$")) return Nombre(c.TrimStart('+'));
            var sci = Regex.Match(c, @"^(\d+(?:,\d+)?)x10\^(-?\d+)$");
            if (sci.Success) return $"{Nombre(sci.Groups[1].Value)}x10^{sci.Groups[2].Value}";
            return c;
        }

        private static string Nombre(string texte)
        {
            var negatif = texte.StartsWith('-');
            var valeur = decimal.Parse(texte.TrimStart('-').Replace(',', '.'), CultureInfo.InvariantCulture);
            var ecrit = valeur.ToString("0.#############", CultureInfo.InvariantCulture).Replace('.', ',');
            return negatif && valeur != 0 ? "-" + ecrit : ecrit;
        }

        private enum Sorte { Nombre, Zero, Moins, Plus, Virgule, Sur, Puissance, Carre, Cube, Fois, Racine, Tout, Parenthese, Denominateur }

        private sealed record Jeton(Sorte Sorte, string Valeur = "");

        private static readonly Dictionary<string, long> Unites = new()
        {
            ["zero"] = 0, ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6, ["sept"] = 7,
            ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11, ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14,
            ["quinze"] = 15, ["seize"] = 16,
        };

        private static readonly Dictionary<string, long> Dizaines = new()
        {
            ["vingt"] = 20, ["vingts"] = 20, ["trente"] = 30, ["quarante"] = 40, ["cinquante"] = 50, ["soixante"] = 60,
        };

        private static readonly Dictionary<string, long> Echelles = new()
        {
            ["cent"] = 100, ["cents"] = 100, ["mille"] = 1_000, ["million"] = 1_000_000, ["millions"] = 1_000_000,
            ["milliard"] = 1_000_000_000, ["milliards"] = 1_000_000_000,
        };

        private static bool EstMotNombre(string m) => Unites.ContainsKey(m) || Dizaines.ContainsKey(m) || Echelles.ContainsKey(m);

        /// <summary>« quatre cent quatre-vingts » → 480 ; null si ce n'est pas une suite de mots de nombre.</summary>
        private static long? Entier(IReadOnlyList<string> mots)
        {
            if (mots.Count == 0) return null;
            long total = 0, groupe = 0, petit = 0;
            foreach (var m in mots)
            {
                if (m == "et") continue;
                if (Unites.TryGetValue(m, out var u)) petit += u;
                else if (m is "vingt" or "vingts") petit = petit is > 1 and < 10 ? petit * 20 : petit + 20;
                else if (Dizaines.TryGetValue(m, out var d)) petit += d;
                else if (m is "cent" or "cents") { groupe += (petit == 0 ? 1 : petit) * 100; petit = 0; }
                else if (Echelles.TryGetValue(m, out var e))
                {
                    var facteur = groupe + petit;
                    total += (facteur == 0 ? 1 : facteur) * e;
                    groupe = 0;
                    petit = 0;
                }
                else return null;
            }
            return total + groupe + petit;
        }

        /// <summary>« huitièmes » → 8, « demi » → 2 ; null sinon.</summary>
        private static long? Denominateur(string mot)
        {
            switch (mot)
            {
                case "demi" or "demis": return 2;
                case "tiers": return 3;
                case "quart" or "quarts": return 4;
            }
            var m = Regex.Match(mot, @"^(.+?)iemes?$");
            if (!m.Success) return null;
            var base_ = m.Groups[1].Value;
            if (base_ == "un") return null; // « unième » seul n'est pas un dénominateur
            if (base_.EndsWith("qu")) base_ = base_[..^1];
            if (base_ == "neuv") base_ = "neuf";
            foreach (var essai in new[] { base_, base_ + "e" })
            {
                if (Entier(essai.Split('-', StringSplitOptions.RemoveEmptyEntries)) is long n && n > 1) return n;
            }
            return null;
        }

        private static string SansAccents(string texte)
        {
            var decompose = texte.Normalize(NormalizationForm.FormD);
            return new string(decompose.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
                .Normalize(NormalizationForm.FormC);
        }

        private static readonly Dictionary<char, char> Exposants = new()
        {
            ['⁰'] = '0', ['¹'] = '1', ['²'] = '2', ['³'] = '3', ['⁴'] = '4', ['⁵'] = '5', ['⁶'] = '6', ['⁷'] = '7', ['⁸'] = '8', ['⁹'] = '9', ['⁻'] = '-',
        };

        private static List<Jeton> Jetons(string dit)
        {
            var t = dit.ToLowerInvariant()
                .Replace('−', '-').Replace('–', '-').Replace('×', 'x').Replace('*', 'x').Replace('·', 'x')
                .Replace('’', '\'').Replace("√", " racine ");

            // Les exposants écrits en petit (10³, 10⁻²) deviennent « ^3 », « ^-2 ».
            t = Regex.Replace(t, "[⁰¹²³⁴⁵⁶⁷⁸⁹⁻]+", m => "^" + new string(m.Value.Select(c => Exposants[c]).ToArray()));
            t = SansAccents(t);

            // « 1 200 000 » : les espaces entre les groupes de trois chiffres tombent.
            t = Regex.Replace(t, @"(?<=\d)[   ](?=\d{3}(?!\d))", string.Empty);
            // « soixante-treize » : le trait d'union entre deux mots les sépare.
            t = Regex.Replace(t, @"(?<=[a-z])-(?=[a-z])", " ");

            var bruts = Regex.Matches(t, @"\d+(?:[.,]\d+)?|[a-z]+|[\^/()+-]").Select(m => m.Value).ToList();
            var jetons = new List<Jeton>();
            var mots = new List<string>();

            void Vider()
            {
                if (mots.Count == 0) return;
                // « et » en fin de suite n'appartient pas au nombre.
                while (mots.Count > 0 && mots[^1] == "et") mots.RemoveAt(mots.Count - 1);
                if (mots.Count > 0 && Entier(mots) is long n) jetons.Add(new Jeton(Sorte.Nombre, n.ToString(CultureInfo.InvariantCulture)));
                mots.Clear();
            }

            for (var i = 0; i < bruts.Count; i++)
            {
                var b = bruts[i];
                if (b == "zero")
                {
                    Vider();
                    jetons.Add(new Jeton(Sorte.Zero, "0"));
                    continue;
                }
                if (EstMotNombre(b) || (b == "et" && mots.Count > 0))
                {
                    // « deux cinq » (des chiffres dits un à un, après la virgule surtout) : deux unités de suite ne font
                    // pas un nombre, la seconde en commence un autre. « dix-sept », « vingt-cinq » restent entiers.
                    static bool EstUnite(string m) => Unites.TryGetValue(m, out var v) && v is >= 1 and <= 16 and not 10;
                    if (EstUnite(b) && mots.Count > 0 && EstUnite(mots[^1])) Vider();
                    mots.Add(b);
                    continue;
                }
                Vider();

                if (char.IsDigit(b[0])) { jetons.Add(new Jeton(Sorte.Nombre, b.Replace('.', ','))); continue; }
                if (Denominateur(b) is long den) { jetons.Add(new Jeton(Sorte.Denominateur, den.ToString(CultureInfo.InvariantCulture))); continue; }

                var sorte = b switch
                {
                    "moins" or "-" => Sorte.Moins,
                    "plus" or "+" => Sorte.Plus,
                    "virgule" => Sorte.Virgule,
                    "sur" or "/" => Sorte.Sur,
                    "puissance" or "exposant" or "^" => Sorte.Puissance,
                    "carre" => Sorte.Carre,
                    "cube" => Sorte.Cube,
                    "fois" or "x" => Sorte.Fois,
                    "racine" => Sorte.Racine,
                    "tout" => Sorte.Tout,
                    "(" => Sorte.Parenthese,
                    _ => (Sorte?)null,
                };
                if (sorte is { } s) jetons.Add(new Jeton(s));
            }
            Vider();
            return jetons;
        }

        /// <summary>
        /// CE QU'IL A LU, EN ÉCRITURE CANONIQUE : « 73 », « 3,25 », « -5 », « 3/4 », « 10^-3 », « (-2)^2 », « 2,5x10^4 »,
        /// « 3rac2 » ; null si ce n'est pas une lecture de nombre (« je sais pas »).
        /// </summary>
        public static string? Canonique(string? dit)
        {
            if (string.IsNullOrWhiteSpace(dit)) return null;
            var j = Jetons(dit);
            if (j.Count == 0) return null;

            var i = 0;
            Jeton? Voir(int k = 0) => i + k < j.Count ? j[i + k] : null;
            bool Est(Sorte s, int k = 0) => Voir(k)?.Sorte == s;

            var parenthese = false;
            if (Est(Sorte.Parenthese)) { parenthese = true; i++; }
            var moins = false;
            if (Est(Sorte.Moins)) { moins = true; i++; }
            else if (Est(Sorte.Plus)) i++;

            // « racine carrée de deux » (le coefficient vient plus bas : « trois racine de deux »)
            if (Est(Sorte.Racine) && Est(Sorte.Nombre, 1))
            {
                return Fin(i + 2, j) ? $"{(moins ? "-" : string.Empty)}rac{j[i + 1].Valeur}" : null;
            }

            // Le premier nombre : un entier, ou un décimal (« trois virgule zéro huit »).
            if (!(Est(Sorte.Nombre) || Est(Sorte.Zero))) return null;
            var nombre = Voir()!.Valeur;
            i++;
            if (Est(Sorte.Virgule))
            {
                i++;
                var decimales = new StringBuilder();
                while (Est(Sorte.Nombre) || Est(Sorte.Zero)) { decimales.Append(Voir()!.Valeur); i++; }
                if (decimales.Length == 0) return null;
                nombre = $"{nombre},{decimales}";
            }
            nombre = Nombre(nombre);
            var signe = moins ? "-" : string.Empty;

            // Rien après : un nombre.
            if (i >= j.Count) return moins && nombre != "0" ? "-" + nombre : nombre;

            // « trois quarts », « cinq huitièmes »
            if (Est(Sorte.Denominateur) && Fin(i + 1, j)) return $"{signe}{nombre}/{Voir()!.Valeur}";

            // « trois sur quatre »
            if (Est(Sorte.Sur) && Est(Sorte.Nombre, 1) && Fin(i + 2, j)) return $"{signe}{nombre}/{j[i + 1].Valeur}";

            // « trois racine (carrée) de deux »
            if (Est(Sorte.Racine) && Est(Sorte.Nombre, 1) && Fin(i + 2, j)) return $"{signe}{nombre}rac{j[i + 1].Valeur}";

            // « deux virgule cinq fois dix puissance quatre »
            if (Est(Sorte.Fois) && Voir(1) is { Sorte: Sorte.Nombre, Valeur: "10" } && Est(Sorte.Puissance, 2))
            {
                i += 3;
                var exposant = Exposant(ref i, j);
                return exposant is not null && Fin(i, j) ? $"{signe}{nombre}x10^{exposant}" : null;
            }

            // Une puissance : « dix puissance moins trois », « trois au carré », « moins deux, le tout au carré ».
            var tout = parenthese;
            if (Est(Sorte.Tout)) { tout = true; i++; }
            string? exp = null;
            if (Est(Sorte.Carre)) { exp = "2"; i++; }
            else if (Est(Sorte.Cube)) { exp = "3"; i++; }
            else if (Est(Sorte.Puissance)) { i++; exp = Exposant(ref i, j); }
            if (Est(Sorte.Tout)) { tout = true; i++; if (Est(Sorte.Carre)) { exp = "2"; i++; } else if (Est(Sorte.Cube)) { exp = "3"; i++; } }
            if (exp is null || !Fin(i, j)) return null;

            return tout && moins ? $"(-{nombre})^{exp}" : $"{signe}{nombre}^{exp}";
        }

        private static string? Exposant(ref int i, List<Jeton> j)
        {
            var moins = false;
            if (i < j.Count && j[i].Sorte == Sorte.Moins) { moins = true; i++; }
            if (i >= j.Count || j[i].Sorte is not (Sorte.Nombre or Sorte.Zero)) return null;
            var valeur = j[i].Valeur;
            i++;
            return (moins ? "-" : string.Empty) + valeur;
        }

        /// <summary>Plus rien d'utile après : la lecture est entière.</summary>
        private static bool Fin(int i, List<Jeton> j) => i >= j.Count;

        private static int Indice(string graine, int taille)
        {
            var somme = 0;
            foreach (var c in graine) somme = unchecked(somme * 31 + c);
            return (int)((uint)somme % (uint)taille);
        }
    }

    /// <summary>
    /// LE FILTRE DE LA LECTURE DES NOMBRES : la réponse du professeur est retenue en entier, puis remise en ordre par
    /// <see cref="LectureDesNombres.Reecrire"/> (sa phrase, la consigne du code, la balise). Même principe que
    /// FiltreMotsVerifies pour les syllabes, sans liste ni micro.
    /// </summary>
    public sealed class FiltreLectureNombres(LectureDesNombres.Tour tour)
    {
        private string _tampon = string.Empty;

        public string Passer(string fragment)
        {
            _tampon += fragment;
            return string.Empty;
        }

        public string Terminer()
        {
            var texte = LectureDesNombres.Reecrire(_tampon, tour);
            _tampon = string.Empty;
            return texte;
        }

        public string Corriger(string texte) => LectureDesNombres.Reecrire(texte, tour);
    }
}
