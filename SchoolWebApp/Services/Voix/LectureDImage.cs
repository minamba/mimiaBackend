using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LIRE LA SYLLABE OU LE MOT DE L'IMAGE — Camara, le 07/11/2026 : « le professeur affiche une image avec une syllabe et
    /// lui demande de dire à voix haute la syllabe ou le mot ; le professeur ne dit pas la syllabe ou le mot », puis
    /// « c'est vraiment faire l'inverse de l'exercice des syllabes et des mots », « tout est à l'oral ».
    ///
    /// L'ÉCOUTE À L'ENVERS. À l'écoute, la voix dit « jo » et l'enfant dit ce qu'il a entendu ; ici le tableau montre « jo »
    /// (en couleur dans « joker ») et l'enfant le lit. LE JUGEMENT EST CELUI DE L'ÉCOUTE : le micro écrit ce qu'il a dit,
    /// comparé à la cible, puis au son si les écritures diffèrent (« beau » vaut « bo ») — voir
    /// VerificationSonService.VerifierLectureAsync. Aucun modèle audio, donc aucune liste de sons refusés.
    ///
    /// C'EST LE CODE QUI CHOISIT ET QUI MONTRE, PAS LE PROFESSEUR (une règle de prompt ne vaut pas un fait) : la syllabe ou
    /// le mot est tiré ici, la consigne est écrite ici, et la balise [LECTURE_IMAGE:genre]cible[/LECTURE_IMAGE] finit son
    /// message. L'écran la lit pour afficher l'image et ne l'affiche ni ne la dit ; le relais du micro la lit pour juger.
    /// Le professeur, lui, ne prononce jamais la cible : ses balises de voix d'un mot sont retirées de ce tour.
    ///
    /// LES IMAGES :
    ///  - syllabes : les 314 de `ImageAuTableau.SyllabesIllustrees`, la syllabe en couleur dans un mot écrit ; au CP celles
    ///    du CP (197), dès le CE1 toutes ;
    ///  - mots français : les 164 mots de CP de `ImageAuTableau.MotsIllustres`. Leur dessin n'a pas le mot écrit : l'écran
    ///    montre le MOT en grand, et le dessin une fois lu (sinon l'enfant nommerait le dessin sans lire) ;
    ///  - anglais : les mots des images de `SonsAnglaisIllustres`, de sa classe et en dessous. Toujours le MOT : un son seul
    ///    (sh, igh) ne s'écrit pas au micro.
    /// </summary>
    public static partial class LectureDImage
    {
        /// <summary>Les deux exercices, tels que `RegleVerificationSon.TypeDEcoute` les rend.</summary>
        public const string Syllabe = "lecture-syllabe";
        public const string Mot = "lecture-mot";

        /// <summary>La carte du choix de l'exercice : l'écran la montre sous la question du professeur.</summary>
        public const string BaliseChoix = "[CHOIX_EXERCICE_SONS]";

        public const string GenreSyllabe = "syllabe";
        public const string GenreMot = "mot";
        public const string GenreAnglais = "anglais";

        public sealed record Element(string Genre, string Cible)
        {
            public bool EstSyllabe => Genre == GenreSyllabe;

            public bool EnAnglais => Genre == GenreAnglais;

            /// <summary>Le mot de l'image d'une syllabe (« joker » pour « jo ») ; null pour un mot.</summary>
            public string? MotDeLImage => EstSyllabe && ImageAuTableau.SyllabesIllustrees.TryGetValue(Cible, out var mot) ? mot : null;
        }

        /// <summary>Ce que le code montre à ce tour : l'élément, et s'il est redemandé ou nouveau.</summary>
        public sealed record Tour(Element Element, bool Redite, bool Premier, string? MotLu = null);

        [GeneratedRegex(@"\[LECTURE_IMAGE:(syllabe|mot|anglais)\]([^\[\]]+)\[/LECTURE_IMAGE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Marqueur();

        // « Je veux lire des syllabes », « la lecture de mots » : un déterminant pluriel, pour que « je sais pas lire ce mot »
        // ne change pas d'exercice.
        [GeneratedRegex(@"\b(?:lire|lecture)\s+(?:des|les|de)\s+(syllabes|mots)\b", RegexOptions.IgnoreCase)]
        private static partial Regex ChoixDeLecture();

        // Ce qu'il dicterait par habitude de l'écoute : une balise de voix d'UN mot, et ses phrases d'écoute.
        [GeneratedRegex(@"\[(FR|EN)\]\s*([^\s\[\]]+)\s*\[/\1\]", RegexOptions.IgnoreCase)]
        private static partial Regex VoixDUnMot();

        [GeneratedRegex(@"\[ARDOISE\][\s\S]*?\[/ARDOISE\]|\[TABLEAU_EFFACE\]|\[LECTURE_IMAGE:[^\]]*\][^\[]*\[/LECTURE_IMAGE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Tableau();

        [GeneratedRegex(@"[^.!?\n]*\b(?:entendu|entends|écoute\s+bien|ecoute\s+bien|voici\s+(?:la|le|une|un)\s+(?:prochaine?|premi[eè]re?|nouvelle?|nouveau|autre)?\s*(?:syllabe|mot)|je\s+te\s+l[ae]\s+redis)\b[^.!?\n]*[.!?]*", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseDEcoute();

        [GeneratedRegex(@"[^.!?\n]*\?+")]
        private static partial Regex Question();

        // « Regarde bien la syllabe en couleur, et lis-la encore une fois à voix haute. », « Lis ce mot au tableau. » : une
        // phrase qui fait lire (lis, lire, relis, essaie de lire) ET parle de la voix haute, de la couleur ou du tableau.
        [GeneratedRegex(@"[^.!?\n]*\b(?:lis|lis-la|lis-le|lire|relis|relis-la|relis-le|relire)\b[^.!?\n]*\b(?:voix\s+haute|en\s+couleur|au\s+tableau|le\s+tableau|cette\s+syllabe|ce\s+mot)\b[^.!?\n]*[.!]*", RegexOptions.IgnoreCase)]
        private static partial Regex ConsigneDuProfesseur();

        // « Pas grave, on réessaie ! Regarde bien le mot. » puis la consigne du code « Regarde bien le mot, et essaie encore
        // de le lire à voix haute. » : deux fois la même chose (contrôle d'Omar, signalement #49, capture 1-clavier-faux).
        // Une phrase qui ne fait que regarder le mot, la syllabe, l'image ou le tableau part : la consigne est celle du code.
        [GeneratedRegex(@"(?<![\p{L}])regarde\s+(?:bien\s+)?(?:le|la|ce|cette|l'|l’)\s*(?:mot|syllabe|image|tableau)(?:\s+(?:en\s+couleur|anglais|au\s+tableau))?\s*(?:[.!]+|(?=\r?\n|$))", RegexOptions.IgnoreCase)]
        private static partial Regex RegardeSeulement();

        [GeneratedRegex(@"\n{3,}")]
        private static partial Regex LignesVides();

        /// <summary>Les syllabes du CE1 (correspondance.csv de Camara) : au CP, seules les autres se montrent.</summary>
        private static readonly HashSet<string> SyllabesDuCe1 = new(StringComparer.Ordinal)
        {
            "bla", "blé", "blo", "blou", "bra", "bri", "bro", "brou", "bru", "cla", "clé", "cli", "clo", "clou", "cra", "cré", "cri", "cro",
            "crou", "cru", "dra", "dru", "fla", "flo", "flou", "fra", "fré", "fri", "fro", "gla", "gli", "glo", "glou", "glu", "gra", "gri",
            "gro", "grou", "gru", "pla", "pli", "plo", "plou", "plu", "pra", "pré", "pri", "pro", "prou", "tra", "tré", "tri", "tro", "trou",
            "vri", "sca", "sco", "spa", "spo", "sta", "sto", "bac", "bal", "bar", "ber", "bol", "bor", "car", "cil", "col", "cor", "dar", "dir",
            "dor", "fer", "fil", "for", "jar", "lac", "lar", "lir", "mal", "mar", "mer", "mil", "mor", "mur", "pal", "par", "per", "pir", "por",
            "sac", "sar", "ser", "sol", "sor", "sur", "tar", "ter", "tir", "tor", "tur", "ver", "vir", "ail", "eil", "euil", "gne", "gno",
            "gnon", "gui", "ouil", "qua", "qui", "ya", "yo",
        };

        private static readonly string[] Classes = ["CP", "CE1", "CE2", "CM1", "CM2"];

        private static int Rang(string? niveau) => Array.IndexOf(Classes, niveau?.Trim().ToUpperInvariant()) + 1;

        /// <summary>Le dernier élément montré dans ce message, ou null.</summary>
        public static Element? Affiche(string? contenu)
        {
            if (string.IsNullOrEmpty(contenu)) return null;
            var trouve = Marqueur().Matches(contenu).LastOrDefault();
            return trouve is null ? null : new Element(trouve.Groups[1].Value.ToLowerInvariant(), trouve.Groups[2].Value.Trim());
        }

        [GeneratedRegex(@"^\s*j.ai lu (la syllabe|le mot)\b", RegexOptions.IgnoreCase)]
        private static partial Regex VerdictDeLecture();

        /// <summary>
        /// L'exercice d'un message qui en relève : l'image montrée par le professeur, ou le verdict écrit pour l'enfant
        /// (« J'ai lu la syllabe ja. »), qui dit « syllabe » sans vouloir repasser à l'écoute. Null sinon.
        /// </summary>
        public static string? TypeDuMessage(string? contenu)
        {
            if (Affiche(contenu) is { } e) return e.EstSyllabe ? Syllabe : Mot;
            var verdict = VerdictDeLecture().Match(contenu ?? string.Empty);
            return !verdict.Success ? null : verdict.Groups[1].Value.EndsWith("syllabe", StringComparison.OrdinalIgnoreCase) ? Syllabe : Mot;
        }

        /// <summary>L'exercice que l'enfant demande dans ce message (« je veux lire des syllabes ») ; null sinon.</summary>
        public static string? ChoixDeLEnfant(string? message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            var choix = ChoixDeLecture().Matches(message).LastOrDefault();
            return choix is null ? null : choix.Groups[1].Value.StartsWith("syllabe", StringComparison.OrdinalIgnoreCase) ? Syllabe : Mot;
        }

        public static bool EstLecture(string? type) => type is Syllabe or Mot;

        [GeneratedRegex(@"\b[ée]cout\w*\s+(?:des|les)\s+(?:syllabes|mots|sons)\b", RegexOptions.IgnoreCase)]
        private static partial Regex EcouteDemandee();

        /// <summary>L'enfant demande un exercice : ce n'est pas un essai sur l'image qu'il a sous les yeux.</summary>
        public static bool ChangeDExercice(string? message) =>
            ChoixDeLEnfant(message) is not null || DemandeSansPrecision(message) || (message is not null && EcouteDemandee().IsMatch(message));

        // « Je veux faire un exercice sur les mots », « on fait des syllabes » : une demande, sur des syllabes, des mots ou
        // des sons, en fin de phrase (« les mots invariables » est une autre leçon).
        [GeneratedRegex(@"\b(?:exercices?|faire|fait|travailler|travaille|veux|voudrais|aimerais|jouer|on\s+fait)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Demande();

        [GeneratedRegex(@"\b(?:des|les|de|sur\s+les)\s+(syllabes|mots|sons)\s*(?:[.!?,]|s.il\s+te\s+pla[iî]t|stp|svp|$)", RegexOptions.IgnoreCase)]
        private static partial Regex SonsEnFinDePhrase();

        [GeneratedRegex(@"\b(?:lire|lis|lecture|[ée]cout\w*|entend\w*|[ée]crire|dict[ée]e)\b", RegexOptions.IgnoreCase)]
        private static partial Regex VerbeDeLExercice();

        [GeneratedRegex(@"\[CHOIX_EXERCICE_SONS(?::[a-z]+)?\]", RegexOptions.IgnoreCase)]
        private static partial Regex BaliseDeChoix();

        /// <summary>
        /// IL DEMANDE UN EXERCICE DE SONS SANS DIRE LEQUEL — Camara, le 07/11/2026 : « j'ai demandé un exercice sur les
        /// syllabes, il m'a affiché la carte ; sur les mots, elle ne s'affiche pas ». La carte dépendait du professeur ; elle
        /// est posée par le code (voir FiltreMotsVerifies).
        /// </summary>
        public static bool DemandeSansPrecision(string? message) => SujetDeLaDemande(message) is not null;

        /// <summary>
        /// CE QU'IL A DEMANDÉ : « syllabes », « mots » ou « sons » ; null si ce n'est pas une demande sans précision.
        /// Camara, le 07/11/2026 : « quand je demande un exercice sur les mots, il doit proposer uniquement écouter des mots et
        /// lire des mots, rien sur les syllabes », et l'inverse. La carte ne montre que les exercices de ce sujet.
        /// </summary>
        public static string? SujetDeLaDemande(string? message)
        {
            var dit = (message ?? string.Empty).Split('\n')[0].Trim();
            if (dit.Length is 0 or > 120 || !Demande().IsMatch(dit) || VerbeDeLExercice().IsMatch(dit)) return null;
            var sujet = SonsEnFinDePhrase().Matches(dit).LastOrDefault();
            return sujet?.Groups[1].Value.ToLowerInvariant();
        }

        /// <summary>La balise de la carte, limitée au sujet demandé : « [CHOIX_EXERCICE_SONS:mots] ».</summary>
        public static string BaliseChoixDe(string? sujet) =>
            sujet is null ? BaliseChoix : $"[CHOIX_EXERCICE_SONS:{sujet}]";

        // Une phrase qui annonce déjà un exercice (« on écoute des mots ! ») : rien n'est choisi, elle ne se garde pas.
        [GeneratedRegex(@"\b(?:[ée]cout\w*|entend\w*|lire|lis|lit|lisons|lecture|dict\w*)\b", RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceUnExercice();

        /// <summary>Le message du professeur quand la carte est posée : sa phrase d'accueil, sans rien commencer.</summary>
        public static string PoserLeChoix(string texte, string? sujet = null)
        {
            var propre = Tableau().Replace(texte ?? string.Empty, " ");
            propre = BaliseDeChoix().Replace(propre, " ");
            propre = VoixDUnMot().Replace(propre, " ");
            propre = PhraseDEcoute().Replace(propre, string.Empty);
            propre = Question().Replace(propre, string.Empty);
            var lignes = propre.Split('\n').Select(l => Regex.Replace(l, @"[ \t]{2,}", " ").Trim());
            var corps = LignesVides().Replace(string.Join("\n", lignes), "\n\n").Trim();

            // Sa première phrase suffit : la suite annonçait un exercice qu'il n'a pas encore choisi. Et si elle-même en
            // annonce un (capture de Camara : « D'accord Zakariya, on écoute des mots ! »), un simple « D'accord ! ».
            var premiere = Regex.Match(corps, @"^[^.!\n]*[.!]").Value.Trim();
            if (premiere.Length == 0 || AnnonceUnExercice().IsMatch(premiere)) premiere = "D'accord !";
            return premiere + "\n\nChoisis ton exercice.\n" + BaliseChoixDe(sujet);
        }

        public static string FaitChoixAPoser(string? sujet) =>
            "[Fait pour ce tour : il demande un exercice sur les " + (sujet ?? "sons") + " sans dire lequel. Son écran lui montre la carte "
            + "des exercices (écouter ou lire). Réponds en UNE phrase courte et chaleureuse, sans dire s'il va écouter ou lire, et ne "
            + "commence AUCUN exercice : ne dicte rien, ne montre rien, ne pose aucune question. Son choix t'arrivera dans son prochain message.]";

        public static string Balise(Element e) => $"[LECTURE_IMAGE:{e.Genre}]{e.Cible}[/LECTURE_IMAGE]";

        /// <summary>Ce qu'il peut avoir à lire, pour cet exercice et cette classe.</summary>
        public static IReadOnlyList<Element> Possibles(string type, bool anglais, string? niveau)
        {
            var rang = Math.Max(1, Rang(niveau));

            // EN ANGLAIS, TOUJOURS LE MOT : un son seul ne s'écrit pas au micro.
            if (anglais)
            {
                // LES IMAGES DES MOTS (07/11/2026) : 242 mots, l'image porte le mot et sa traduction (« apple / Pomme »).
                var mots = MotsAnglaisIllustres.JusquA(Classes[rang - 1]);
                if (mots.Count > 0) return mots.Select(m => new Element(GenreAnglais, m.Mot)).ToList();

                return SonsAnglaisIllustres.Tous.Where(s => Rang(s.Niveau) <= rang)
                    .Select(s => s.Mot == "June" ? s.Mot : s.Mot.ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(m => new Element(GenreAnglais, m))
                    .ToList();
            }

            if (type == Syllabe)
            {
                // UNE SYLLABE QUI EST TOUT LE MOT N'EN EST PAS UNE POUR L'ENFANT — Camara, le 07/11/2026, capture à l'appui :
                // « je suis sur un exercice de syllabes et il m'a mis un mot » (« chien », tout en couleur). 40 images sur 314
                // (eau, chou, pain, clé, sac, mur…) restent à l'écoute, jamais ici : la couleur est toujours un morceau du mot.
                return ImageAuTableau.SyllabesIllustrees
                    .Where(p => !MemeCible(p.Key, p.Value))
                    .Select(p => p.Key)
                    .Where(s => rang > 1 || !SyllabesDuCe1.Contains(s))
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .Select(s => new Element(GenreSyllabe, s))
                    .ToList();
            }

            return MotsFrancais.Value.Select(m => new Element(GenreMot, m)).ToList();
        }

        /// <summary>
        /// LES 164 MOTS ILLUSTRÉS, ÉCRITS COMME IL FAUT LES LIRE : le nom du fichier n'a pas d'accent (« ane », « fee »), la
        /// banque des mots du CP, si. Un mot absent de la banque garde le nom du fichier.
        /// </summary>
        private static readonly Lazy<IReadOnlyList<string>> MotsFrancais = new(() =>
        {
            var parNom = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var mot in BanqueDeSons.MotsDeLaBanque)
            {
                var nom = ImageAuTableau.NomDeFichier(mot);
                if (ImageAuTableau.MotsIllustres.Contains(nom) && !parNom.ContainsKey(nom)) parNom[nom] = mot;
            }

            return ImageAuTableau.MotsIllustres.OrderBy(n => n, StringComparer.Ordinal)
                .Select(n => parNom.GetValueOrDefault(n, n))
                .ToList();
        });

        /// <summary>
        /// CE QUE LE CODE MONTRE À CE TOUR. La même image tant qu'il ne l'a pas lue juste (un « je sais pas », un micro qui
        /// n'a rien compris, une lecture fausse) ; une autre après une bonne lecture, après trois essais ratés
        /// (EssaisRates), ou quand il vient de choisir l'exercice. `recents` : les messages, du plus ancien au plus récent,
        /// sans celui de l'enfant à ce tour.
        /// </summary>
        public static Tour? AMontrer(string type, bool anglais, string? niveau, IReadOnlyList<string?> recents, bool? sonJuste, string? abandonne, string? messageDeLEleve, string graine)
        {
            var possibles = Possibles(type, anglais, niveau);
            if (possibles.Count == 0) return null;

            var genre = anglais ? GenreAnglais : type == Syllabe ? GenreSyllabe : GenreMot;
            var montres = recents.Select(Affiche).Where(e => e is not null).Select(e => e!).ToList();
            var precedent = montres.Count > 0 ? montres[^1] : null;
            var vientDeChoisir = ChoixDeLEnfant(messageDeLEleve) is not null;

            // Une image qui n'est plus permise (« chien » montré avant qu'on retire les mots entiers) ne revient pas.
            var toujoursPermis = precedent is not null && possibles.Any(e => MemeCible(e.Cible, precedent.Cible));

            if (precedent is not null && toujoursPermis && precedent.Genre == genre && sonJuste != true && !vientDeChoisir
                && (abandonne is null || !MemeCible(abandonne, precedent.Cible)))
            {
                return new Tour(precedent, Redite: true, Premier: false);
            }

            var dejaVus = montres.Select(e => e.Cible).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (precedent is not null) dejaVus.Add(precedent.Cible);
            var neufs = possibles.Where(e => !dejaVus.Contains(e.Cible)).ToList();
            var pool = neufs.Count > 0 ? neufs : possibles.Where(e => precedent is null || !MemeCible(e.Cible, precedent.Cible)).ToList();
            if (pool.Count == 0) pool = possibles.ToList();

            var choisi = pool[Indice(graine, pool.Count)];
            var premier = precedent is null || precedent.Genre != genre || vientDeChoisir;

            // LE MOT DE L'IMAGE QU'IL VIENT DE BIEN LIRE : le seul « comme dans… » permis à ce tour (voir ImageAuTableau.CommeDans).
            return new Tour(choisi, Redite: false, Premier: premier, MotLu: sonJuste == true ? precedent?.MotDeLImage : null);
        }

        /// <summary>
        /// LE MESSAGE DU PROFESSEUR, REMIS EN ORDRE : sa phrase (bravo, encouragement), puis la consigne du code, puis la
        /// balise. Ce qu'il aurait dicté ou écrit par habitude de l'écoute s'en va : la cible ne se dit jamais.
        /// </summary>
        public static string Reecrire(string texte, Tour tour)
        {
            // UN MESSAGE QUI CONCLUT LA SÉANCE NE REÇOIT NI CONSIGNE NI IMAGE — Camara, le 11/11/2026 : l'au revoir de
            // Nora suivi de « essaie encore de la lire à voix haute ». Le professeur a dit au revoir : on n'y ajoute rien.
            if (ConclutLaSeance(texte)) return texte;

            var propre = Tableau().Replace(texte ?? string.Empty, " ");
            propre = BaliseDeChoix().Replace(propre, " ");
            propre = VoixDUnMot().Replace(propre, " ");
            propre = PhraseDEcoute().Replace(propre, string.Empty);

            // SES QUESTIONS PARTENT AUSSI — capture de Camara, le 07/11/2026 : « Tu veux lire une autre syllabe ? » juste
            // avant « Voici une autre image » : l'image est déjà là, la seule demande est celle du code.
            propre = Question().Replace(propre, string.Empty);

            // SA CONSIGNE DE LECTURE PART AUSSI — capture de Camara, le 07/11/2026 : « Regarde bien la syllabe en couleur, et
            // lis-la encore une fois à voix haute », puis celle du code : deux fois la même chose. La consigne est celle du code.
            propre = ConsigneDuProfesseur().Replace(propre, string.Empty);
            propre = RegardeSeulement().Replace(propre, string.Empty);

            // La cible elle-même, s'il l'a citée (« Lis "jo" ») : la ligne entière part. Citée seulement : « a », « ou », « on »
            // sont aussi des mots de ses phrases.
            propre = string.Join("\n", propre.Split('\n').Select(ligne => ContientLaCible(ligne, tour.Element.Cible) ? string.Empty : ligne));

            var lignes = propre.Split('\n').Select(l => Regex.Replace(l, @"[ \t]{2,}", " ").Trim());
            var corps = LignesVides().Replace(string.Join("\n", lignes), "\n\n").Trim();

            corps = ImageAuTableau.CommeDans(ImageAuTableau.SansSyllabeCitee(corps), tour.MotLu);

            var morceaux = new List<string>();
            if (corps.Length > 0) morceaux.Add(corps);
            morceaux.Add(Consigne(tour));
            morceaux.Add(Balise(tour.Element));
            return string.Join("\n\n", morceaux);
        }

        /// <summary>Le message du professeur termine-t-il la séance ? Ses balises de fin le disent.</summary>
        public static bool ConclutLaSeance(string? texte) =>
            !string.IsNullOrEmpty(texte)
            && (texte.Contains("[FIN_SEANCE]", StringComparison.Ordinal) || texte.Contains("[RAPPORT]", StringComparison.Ordinal));

        public static string Consigne(Tour tour)
        {
            var e = tour.Element;
            if (e.EstSyllabe)
            {
                return tour.Redite ? "Regarde bien la syllabe en couleur, et essaie encore de la lire à voix haute."
                    : tour.Premier ? "Regarde le tableau : lis à voix haute la syllabe en couleur."
                    : "Voici une autre image : lis à voix haute la syllabe en couleur.";
            }

            var anglais = e.EnAnglais ? " anglais" : string.Empty;
            return tour.Redite ? $"Regarde bien le mot{anglais}, et essaie encore de le lire à voix haute."
                : tour.Premier ? $"Regarde le tableau : lis ce mot{anglais} à voix haute."
                : $"Voici un autre mot{anglais} : lis-le à voix haute.";
        }

        /// <summary>Le fait de l'exercice, à chaque tour : ce que le professeur fait, et surtout ce qu'il ne fait pas.</summary>
        public static string FaitType(string type, bool anglais)
        {
            var quoi = anglais ? "LIRE à voix haute des mots ANGLAIS en image"
                : type == Syllabe ? "LIRE à voix haute la syllabe en couleur d'une image" : "LIRE à voix haute des mots";
            return $"[Fait pour ce tour, qui l'emporte sur les consignes d'écoute : l'élève a choisi de {quoi}. C'est le CODE qui choisit ce qu'il lit, "
                + "l'affiche au tableau et écrit la consigne à la fin de ton message. Toi, tu écris seulement une ou deux phrases courtes, en français "
                + "(bravo, encouragement), SANS lui redemander de lire : la consigne est déjà écrite par le code. "
                + "Tu ne dis, n'écris et ne prononces JAMAIS de syllabe ni de mot à lire : aucune balise de voix, rien au "
                + "tableau (ni [ARDOISE], ni [TABLEAU_EFFACE]), et pas de « qu'as-tu entendu ». Tu ne changes d'exercice que s'il le demande lui-même.]";
        }

        /// <summary>
        /// Le fait du verdict, pour le tour suivant du professeur. `auClavier` : il a ÉCRIT sa lecture au lieu de la dire
        /// (signalement #49, 20/11/2026) — même verdict, mais ni « le micro a entendu » ni « le micro peut s'être trompé ».
        /// </summary>
        public static string FaitVerdict(VerdictSon verdict, Element e, bool auClavier = false)
        {
            var quoi = e.EstSyllabe ? "la syllabe" : "le mot";
            const string fin = " Ne parle jamais de cet outil ni du micro à l'élève.]";

            if (auClavier && !verdict.SansJugement && !verdict.Juste && !(e.EnAnglais && EstLaTraduction(e.Cible, verdict.Entendu)))
            {
                return $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : il devait lire {quoi} « {e.Cible} », et il a écrit "
                    + $"« {verdict.Entendu} » : ce n'est pas ça. Son écran affiche « On réessaie ». Ne le félicite pas et ne dis pas « bravo » : "
                    + "encourage-le en une phrase, SANS lui donner la réponse et sans la prononcer, et SANS consigne : n'écris ni « regarde bien », "
                    + "ni « lis », ni « essaie encore de le lire » — le code remet la même image et écrit lui-même la consigne à la fin de ton message." + fin;
            }

            if (verdict.SansJugement)
            {
                return $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : il a lu le mot entier « {verdict.Entendu} » au lieu de la seule "
                    + "syllabe en couleur. Ce n'est pas une erreur : dis-lui gentiment, en une phrase, de ne lire que la partie en couleur. Ne lui dis "
                    + "pas laquelle : le code lui remet la même image." + fin;
            }

            // LE MOT EN FRANÇAIS AU LIEU DE L'ANGLAIS — Camara, le 07/11/2026 : c'est bien le mot, mais en français ; elle
            // attend l'anglais, et c'est une erreur (l'écran dit « On réessaie », l'essai compte).
            if (!verdict.Juste && e.EnAnglais && EstLaTraduction(e.Cible, verdict.Entendu))
            {
                return $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : il devait lire le mot anglais « {e.Cible} », et il a dit "
                    + $"« {verdict.Entendu} », le même mot en FRANÇAIS. Son écran affiche « On réessaie ». Dis-lui que c'est bien le mot en "
                    + "français, bravo, mais que tu attends qu'il le lise en ANGLAIS, comme il est écrit au tableau. Ne lui donne pas le mot "
                    + "anglais et ne le prononce pas : le code lui remet la même image." + fin;
            }

            if (verdict.Juste)
            {
                var image = e.MotDeLImage is { } mot
                    ? $" Tu peux ajouter « Comme dans {mot} ! ». N'écris JAMAIS la syllabe seule dans ta phrase : ta voix la lirait mal (« sin » est devenu « sine »)."
                    : string.Empty;
                return $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : il a bien lu {quoi} « {e.Cible} ». Son écran affiche déjà "
                    + $"« Bonne lecture » en vert : félicite-le en une phrase.{image} Le code affiche la suite." + fin;
            }

            return $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : il devait lire {quoi} « {e.Cible} », et le micro a entendu "
                + $"« {verdict.Entendu} ». Son écran affiche « On réessaie ». Encourage-le en une phrase, SANS lui donner la réponse et sans la "
                + "prononcer : le code lui remet la même image. Le micro peut aussi s'être trompé : ne lui dis pas qu'il a mal lu." + fin;
        }

        /// <summary>Ce qu'il a dit est-il la traduction française du mot anglais (« bateau » pour ship) ?</summary>
        public static bool EstLaTraduction(string motAnglais, string? dit)
        {
            if (string.IsNullOrWhiteSpace(dit)) return false;

            // La traduction écrite sur l'image (table de Camara), et les synonymes d'enfant de la table des sons.
            var francais = new List<string>();
            if (MotsAnglaisIllustres.Trouve(motAnglais) is { } illustre) francais.Add(illustre.Francais);
            if (SonsAnglaisIllustres.Traductions.TryGetValue(motAnglais, out var synonymes)) francais.AddRange(synonymes);

            // Un mot qui s'écrit pareil dans les deux langues (« lion », « pizza ») n'est pas une erreur de langue.
            francais.RemoveAll(f => string.Equals(f, motAnglais, StringComparison.OrdinalIgnoreCase));
            if (francais.Count == 0) return false;
            var propre = Regex.Replace(SansAccents(dit.ToLowerInvariant().Replace("œ", "oe")), @"[^a-z]", string.Empty);
            return francais.Any(f => Regex.Replace(SansAccents(f.ToLowerInvariant().Replace("œ", "oe")), @"[^a-z]", string.Empty) == propre);
        }

        private const string DebutDuMotEntier = "J'ai lu la syllabe en disant le mot entier";

        /// <summary>Il a lu le mot entier de l'image : pas une erreur, mais un essai (voir EssaisRates).</summary>
        public static bool EstMotEntier(string? contenu) =>
            contenu is not null && contenu.TrimStart().StartsWith(DebutDuMotEntier, StringComparison.Ordinal);

        /// <summary>
        /// L'IMAGE QU'IL LIT EN TAPANT SA RÉPONSE — signalement #49 (Adam, 20/11/2026) : ce qu'il écrit au clavier devant
        /// l'image du tableau est une lecture, jugée comme celle du micro. Rend l'élément montré par le dernier message du
        /// professeur, ou null quand ce message n'est pas à juger : pas d'image, un contrôle noté ouvert, un message marqué
        /// par l'écran (« [EXERCICE CHOISI : …] »), un verdict déjà écrit, une demande (changer d'exercice, au revoir) ou
        /// une phrase (plus de deux mots : « je sais pas lire », une question).
        /// `historique` : les messages avant le sien, du plus ancien au plus récent.
        /// </summary>
        public static Element? ATaperA(IReadOnlyList<(string? Role, string? Contenu)> historique, string? message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Contains('[') || message.Contains('\n')) return null;
            if (TypeDuMessage(message) is not null || ChangeDExercice(message)) return null;
            if (ChangementDExercice.Demande(message) || LecteurAuRevoir.DitAuRevoir(message)) return null;
            if (RegleVerificationSon.MotsDUneReponseCourte(message) is not { } mots || EstUneDemande(mots)) return null;

            var duProfesseur = historique.Where(m => m.Role == "assistant").Select(m => m.Contenu).ToList();
            if (LecteurEvaluation.EstOuvert(duProfesseur)) return null;

            var dernier = duProfesseur.LastOrDefault(c => !string.IsNullOrWhiteSpace(c) && !RegleVerificationSon.EstUneCloture(c)
                && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase));
            return Affiche(dernier);
        }

        // Mots (déjà normalisés : lettres seules, minuscules) d'une demande ou d'une réaction, pas d'une lecture.
        private static readonly HashSet<string> MotsDeDemande = new(StringComparer.Ordinal)
        {
            "quoi", "aide", "aider", "aidez", "aidemoi", "sais", "sait", "pourquoi", "comment", "répète", "repete", "répéter", "repeter",
            "pardon", "attends", "attend", "stop", "pause", "encore", "help", "ok", "okay", "oké", "daccord", "merci", "compris", "comprends",
            "jsp", "chais", "chaispas", "chepa", "saispas", "cestquoi", "euh", "hein", "bof", "oui", "non", "veux",
        };

        // Seuls, ils ne sonnent comme aucune image du CP : jamais une lecture. Les autres mots de demande, seuls, restent
        // une tentative (« quoi » pour « coi », « non » pour « non ») — le service n'en retient que ce qui sonne comme la cible.
        private static readonly HashSet<string> JamaisUneLecture = new(StringComparer.Ordinal)
        {
            "ok", "okay", "oké", "daccord", "jsp", "chais", "chaispas", "chepa", "saispas", "cestquoi", "aidemoi", "euh", "hein", "bof",
            "pourquoi", "help", "merci",
        };

        /// <summary>
        /// UNE DEMANDE, PAS UNE LECTURE — contrôle d'Omar (20/11/2026, signalement #49) : « c'est quoi ? », « aide moi », « sais
        /// pas », « ok » passaient pour une lecture (deux mots au plus) et comptaient comme une lecture FAUSSE. Au clavier il n'y a
        /// pas de micro qui déforme : deux mots dont un mot de demande, ou un mot qui ne se lit jamais seul, partent au professeur.
        /// </summary>
        public static bool EstUneDemande(IReadOnlyList<string> mots) =>
            mots.Count == 1 ? JamaisUneLecture.Contains(mots[0]) : mots.Any(MotsDeDemande.Contains);

        /// <summary>Ce que garde le message de l'enfant : ce qu'il a lu, pour le professeur et le rapport.</summary>
        public static string TexteDeLEleve(Element e, VerdictSon verdict) =>
            // « J'ai lu la syllabe » d'abord : c'est ce début qui dit l'exercice (TypeDuMessage), pas « le mot ».
            verdict.SansJugement ? $"{DebutDuMotEntier} {verdict.Entendu}."
            : e.EstSyllabe ? $"J'ai lu la syllabe {verdict.Entendu}." : $"J'ai lu le mot {verdict.Entendu}.";

        private static bool ContientLaCible(string ligne, string cible) =>
            Regex.IsMatch(SansAccents(ligne.ToLowerInvariant()), @"[«""“]\s*" + Regex.Escape(SansAccents(cible.ToLowerInvariant())) + @"\s*[»""”]");

        private static bool MemeCible(string a, string b) =>
            string.Equals(SansAccents(a.Trim().ToLowerInvariant()), SansAccents(b.Trim().ToLowerInvariant()), StringComparison.Ordinal);

        private static string SansAccents(string texte)
        {
            var decompose = texte.Normalize(NormalizationForm.FormD);
            return new string(decompose.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).Normalize(NormalizationForm.FormC);
        }

        private static int Indice(string graine, int taille)
        {
            var somme = 0;
            foreach (var c in graine) somme = unchecked(somme * 31 + c);
            return (int)((uint)somme % (uint)taille);
        }
    }
}
