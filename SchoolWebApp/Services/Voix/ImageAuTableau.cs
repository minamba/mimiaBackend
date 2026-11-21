using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LE PROFESSEUR PEUT NOMMER L'IMAGE DU TABLEAU — Camara, le 07/11/2026 : « quand l'enfant donne la bonne réponse, on
    /// affiche l'image avec le mot illustré ou la syllabe mise dans un mot ; est-ce que le professeur peut lire le mot de
    /// l'image ? », puis « oui, pour toutes les classes, en français et en anglais ». Une syllabe sans image : rien, le
    /// professeur ne parle pas d'une image qui n'existe pas.
    ///
    /// L'IMAGE EST CHOISIE PAR LE NAVIGATEUR (`lib/imagesMots.js`, `lib/imagesSonsAnglais.js`) : cette classe REJOUE LA
    /// MÊME RÈGLE sur le même message, pour dire au professeur ce que l'enfant voit. Les deux se modifient ensemble.
    ///  - mot français trouvé, au CP, s'il est dans <see cref="MotsIllustres"/> : l'image montre le mot lui-même ;
    ///  - syllabe française trouvée, au CP et au CE1 : l'image montre un mot qui la contient (<see cref="SyllabesIllustrees"/>,
    ///    le correspondance.csv de Camara, 314 images) ;
    ///  - cours d'anglais, du CP au CM2 : le mot de l'image d'un son (<see cref="SonsAnglaisIllustres"/>).
    ///
    /// LE MOT DE L'IMAGE NE DEVIENT JAMAIS LE SON À JUGER : dans un tour d'écoute, seule la dernière balise de voix reste
    /// (`DicteeDeMots.GarderSeulementLaDerniereVoix`) et la question suivante est remise à la fin par le code. En anglais,
    /// la balise du mot de l'image est gardée pour que Marine le dise avec l'accent (<see cref="FiltreMotsVerifies"/>).
    /// </summary>
    public static partial class ImageAuTableau
    {
        public sealed record ImageMontree(string Mot, string? Syllabe, bool Anglais);

        [GeneratedRegex(@"^\s*j.ai entendu (le mot|la syllabe)\s+(.+?)\s*[.!?]?\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex VerdictEcoute();

        [GeneratedRegex(@"\b(?:voici le (?:premier|prochain) mot|je te le redis)\b", RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceDeMot();

        [GeneratedRegex(@"\b(?:voici la (?:première|premiere|prochaine) syllabe|je te la redis)\b", RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceDeSyllabe();

        [GeneratedRegex(@"\bsyllabe\b", RegexOptions.IgnoreCase)]
        private static partial Regex SyllabeDansLaQuestion();

        [GeneratedRegex(@"\[FR\]([\s\S]*?)\[/FR\]", RegexOptions.IgnoreCase)]
        private static partial Regex BaliseFr();

        [GeneratedRegex(@"\[EN\]([\s\S]*?)\[/EN\]", RegexOptions.IgnoreCase)]
        private static partial Regex BaliseEn();

        /// <summary>« Le son sh. », « le a de cat » : la question de l'exercice des sons (même motif que le navigateur).</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:le|du|ce)\s+son(?!\p{L})|(?<!\p{L})le\s+\S{1,4}\s+de\s+[a-z]+", RegexOptions.IgnoreCase)]
        private static partial Regex ExerciceDeSons();

        [GeneratedRegex(@"[«»""“”.!?…,;:]")]
        private static partial Regex Ponctuation();

        [GeneratedRegex(@"^\s*(?:\[COMPREHENSION_SUPPRIMEE\][\s\S]*?\[/COMPREHENSION_SUPPRIMEE\]\s*)?\[(?:FICHE|RAPPORT|FIN_SEANCE)\]", RegexOptions.IgnoreCase)]
        private static partial Regex MessageTechnique();

        /// <summary>
        /// Une fiche, un rapport, une fin de séance, écrits par le professeur APRÈS sa question (relevé le 06/11/2026,
        /// message 10020) : ce n'est pas la question à laquelle l'enfant répond.
        /// </summary>
        public static bool EstTechnique(string? contenu) => contenu is not null && MessageTechnique().IsMatch(contenu);

        private static readonly string[] Classes = ["CP", "CE1", "CE2", "CM1", "CM2"];

        /// <summary>
        /// L'image que l'élève voit au tableau après ce message, ou null. `question` : le dernier message du professeur
        /// avant la réponse.
        /// </summary>
        public static ImageMontree? Montree(string? contenu, bool? sonJuste, string? question, string? niveauCode, string? matiereCode)
        {
            if (sonJuste != true || string.IsNullOrWhiteSpace(contenu)) return null;

            if (string.Equals(matiereCode, "ANGLAIS", StringComparison.OrdinalIgnoreCase)) return EnAnglais(contenu, question, niveauCode);
            if (!string.Equals(matiereCode, "FRANCAIS", StringComparison.OrdinalIgnoreCase)) return null;

            var niveau = niveauCode?.Trim().ToUpperInvariant();
            var cp = niveau == "CP";
            var cpOuCe1 = cp || niveau == "CE1";
            question ??= string.Empty;
            var verdict = VerdictEcoute().Match(contenu);

            string mot;
            if (verdict.Success)
            {
                if (verdict.Groups[1].Value.Equals("la syllabe", StringComparison.OrdinalIgnoreCase))
                {
                    if (!cpOuCe1 || AnnonceDeMot().IsMatch(question)) return null;
                    return Syllabe((AnnonceDeSyllabe().IsMatch(question) ? DernierPassage(question, BaliseFr()) : null) ?? verdict.Groups[2].Value);
                }
                if (AnnonceDeSyllabe().IsMatch(question)) return null;
                mot = (AnnonceDeMot().IsMatch(question) ? DernierPassage(question, BaliseFr()) : null) ?? verdict.Groups[2].Value;
            }
            else
            {
                if (contenu.Trim().Contains(' ')) return null;
                var motIllustre = cp && !AnnonceDeSyllabe().IsMatch(question) && EstIllustre(Nettoyer(contenu));
                if (!motIllustre && (AnnonceDeSyllabe().IsMatch(question) || SyllabeDansLaQuestion().IsMatch(question)))
                {
                    return cpOuCe1 ? Syllabe(contenu) : null;
                }
                mot = contenu;
            }

            if (!cp) return null;
            var propre = Nettoyer(mot);
            return EstIllustre(propre) ? new ImageMontree(propre.ToLowerInvariant(), null, false) : null;
        }

        /// <summary>
        /// LA SYLLABE SEULE NE S'ÉCRIT PAS DANS SA PHRASE — Camara, capture du 07/11/2026 : « « Sin », comme dans singe ! » lu
        /// « sine » par la voix ; « il peut pas dire n'importe quoi ». Hors de sa balise de voix (qui joue la prise d'Adrien),
        /// une syllabe isolée se lit au hasard. Une citation courte devant « comme dans » s'en va : « Comme dans singe ! ».
        /// </summary>
        /// <param name="seulementCommeDans">En anglais (retest KO #57) : seule la citation devant « comme dans » part ; un mot
        /// français cité ailleurs (« un », la traduction) reste.</param>
        public static string SansSyllabeCitee(string texte, bool seulementCommeDans = false)
        {
            var sansCommeDans = SyllabeCitee().Replace(texte ?? string.Empty, m =>
            {
                var avant = m.Index == 0 ? string.Empty : texte![..m.Index].TrimEnd();
                var debutDePhrase = avant.Length == 0 || ".!?…\n:".Contains(avant[^1]);
                return debutDePhrase ? "Comme dans" : "comme dans";
            });
            if (seulementCommeDans) return sansCommeDans;

            // ET PARTOUT AILLEURS — capture de Camara, le 07/11/2026 : « Pas grave, « can » reviendra une autre fois », lu
            // « cane ». Une syllabe de la méthode citée seule devient « cette syllabe » ; un vrai mot cité reste.
            return Citation().Replace(sansCommeDans, m =>
            {
                var cite = m.Groups[1].Value;
                if (!PrononciationDesSyllabes.EstUneSyllabeDeLaMethode(cite)) return m.Value;
                var avant = m.Index == 0 ? string.Empty : sansCommeDans[..m.Index].TrimEnd();
                var debutDePhrase = avant.Length == 0 || ".!?…\n:".Contains(avant[^1]);
                return debutDePhrase ? "Cette syllabe" : "cette syllabe";
            });
        }

        [GeneratedRegex(@"[«""“]\s*(\p{L}{1,5})\s*[»""”]")]
        private static partial Regex Citation();

        [GeneratedRegex(@"[«""“]\s*\p{L}{1,5}\s*[»""”]\s*,?\s*comme\s+dans", RegexOptions.IgnoreCase)]
        private static partial Regex SyllabeCitee();

        /// <summary>
        /// « COMME DANS… » : LE MOT DE L'IMAGE, OU RIEN — Camara, le 08/11/2026 : « la syllabe c'était man, l'image un manteau,
        /// et le professeur a dit "Bravo, comme dans maman" ». Le fait lui donnait le bon mot, il en a pris un autre : une règle
        /// ne vaut pas un fait. Le mot donné remplace le sien ; sans mot (pas d'image, réponse fausse), la formule s'en va.
        /// </summary>
        public static string CommeDans(string texte, string? mot)
        {
            if (!string.IsNullOrWhiteSpace(mot))
            {
                return FormuleCommeDans().Replace(texte ?? string.Empty, m =>
                    $"{m.Groups["avant"].Value}{m.Groups["cest"].Value}{m.Groups["c"].Value}omme dans {mot}");
            }

            var sans = FormuleCommeDans().Replace(texte ?? string.Empty, string.Empty);
            sans = PonctuationOrpheline().Replace(sans, "$1");
            return DebutSansPhrase().Replace(sans, string.Empty).Trim();
        }

        [GeneratedRegex(@"(?<avant>[,;]?\s*)(?<cest>c['’]est\s+(?:bien\s+)?)?\b(?<c>[Cc])omme\s+dans\s[^.!?…\n]*?(?=\s*[.!?…\n]|$)")]
        private static partial Regex FormuleCommeDans();

        [GeneratedRegex(@"([.!?…])\s*[.!]+")]
        private static partial Regex PonctuationOrpheline();

        [GeneratedRegex(@"^\s*[.!]+\s*")]
        private static partial Regex DebutSansPhrase();

        /// <summary>Une bonne réponse sans image au tableau : pas de mot « comme dans… » inventé (capture de Camara, 07/11/2026).</summary>
        public const string FaitSansImage =
            "[Fait pour ce tour : aucune image n'est au tableau pour ce son. Félicite-le simplement, SANS donner de mot qui le contient "
            + "(pas de « comme dans… ») : un mot inventé peut ne pas commencer par ce son.]";

        /// <summary>CE QUE LE PROFESSEUR EN FAIT : la nommer après son bravo, sans en faire le son suivant.</summary>
        public static string Fait(ImageMontree image)
        {
            const string tableau = " Ne touche pas au tableau dans ce message (ni [ARDOISE], ni [TABLEAU_EFFACE]) : l'image y est.";
            if (image.Anglais)
            {
                return $"[Fait pour ce tour : son tableau montre maintenant l'image de « {image.Mot} ». Après ton bravo, tu peux la nommer "
                    + $"en une courte phrase, le mot dans ta balise [EN] (« Comme sur l'image : [EN]{image.Mot}[/EN] ! »), PUIS seulement le mot "
                    + "suivant, qui reste le dernier de ton message." + tableau + "]";
            }
            if (image.Syllabe is not null)
            {
                return $"[Fait pour ce tour : son tableau montre maintenant l'image de « {image.Mot} », un mot qui contient « {image.Syllabe} ». "
                    + $"Après ton bravo, tu peux le dire en une courte phrase (« Comme dans {image.Mot} ! »), le mot écrit SANS balise de voix, "
                    + "PUIS seulement la syllabe suivante. N'écris JAMAIS la syllabe seule dans ta phrase : ta voix la lirait mal." + tableau + "]";
            }
            return $"[Fait pour ce tour : son tableau montre maintenant l'image de « {image.Mot} », le mot qu'il vient de trouver. Après ton "
                + "bravo, tu peux nommer l'image en une courte phrase (« Oui, comme sur l'image ! »), le mot écrit SANS balise de voix, PUIS "
                + "seulement le mot suivant." + tableau + "]";
        }

        private static ImageMontree? EnAnglais(string contenu, string? question, string? niveauCode)
        {
            if (Array.IndexOf(Classes, niveauCode?.Trim().ToUpperInvariant()) < 0) return null;

            string mot;
            var verdict = VerdictEcoute().Match(contenu);
            if (verdict.Success)
            {
                var dit = DernierPassage(question ?? string.Empty, BaliseEn());
                mot = dit is not null && !dit.Contains(' ') ? dit : verdict.Groups[2].Value;
            }
            else
            {
                if (contenu.Trim().Contains(' ')) return null;
                mot = contenu;
            }

            var propre = new string(Nettoyer(mot).ToLowerInvariant().Where(c => c is >= 'a' and <= 'z').ToArray());
            var son = SonsAnglaisIllustres.Tous.FirstOrDefault(s => s.Mot.Equals(propre, StringComparison.OrdinalIgnoreCase));

            // L'IMAGE DU SON OU CELLE DU MOT (07/11/2026), comme `sonAnglaisTrouve` du navigateur : le son dans l'exercice
            // des sons (« le son sh »), le mot ailleurs s'il a une image.
            var motIllustre = MotsAnglaisIllustres.Trouve(propre);
            var exerciceDeSons = ExerciceDeSons().IsMatch(question ?? string.Empty);
            if (son is not null && (exerciceDeSons || motIllustre is null)) return new ImageMontree(son.Mot, null, true);
            return motIllustre is null ? null : new ImageMontree(motIllustre.Mot, null, true);
        }

        private static ImageMontree? Syllabe(string syllabe)
        {
            var propre = Nettoyer(syllabe).ToLowerInvariant().Normalize(NormalizationForm.FormC);
            return propre.Length > 0 && !propre.Contains(' ') && SyllabesIllustrees.TryGetValue(propre, out var mot)
                ? new ImageMontree(mot, propre, false)
                : null;
        }

        private static string? DernierPassage(string contenu, Regex balise)
        {
            var passages = balise.Matches(contenu).Select(m => m.Groups[1].Value.Trim()).Where(p => p.Length > 0).ToList();
            return passages.Count > 0 ? Nettoyer(passages[^1]) : null;
        }

        private static string Nettoyer(string texte) => Ponctuation().Replace(texte ?? string.Empty, string.Empty).Trim();

        /// <summary>« Âne » → « ane » ; « Sœur » → « soeur » : le nom du fichier (comme `nomDeFichier` du navigateur).</summary>
        public static string NomDeFichier(string mot)
        {
            var bas = (mot ?? string.Empty).ToLowerInvariant().Replace("œ", "oe").Replace("æ", "ae").Normalize(NormalizationForm.FormD);
            return new string(bas.Where(c => c is >= 'a' and <= 'z').ToArray());
        }

        private static bool EstIllustre(string mot) => NomDeFichier(mot) is { Length: > 0 } nom && MotsIllustres.Contains(nom);

        /// <summary>Les 164 mots de CP qui ont une image (`MOTS_ILLUSTRES` du navigateur).</summary>
        public static readonly HashSet<string> MotsIllustres = new(StringComparer.Ordinal)
        {
            "abeille", "air", "ami", "ane", "araignee", "arbre", "balle", "ballon", "banane", "bateau", "beau", "bebe", "blanc", "bleu",
            "bonbon", "bouche", "boule", "bras", "cafe", "cahier", "canard", "carotte", "chaise", "chapeau", "char", "chat", "chaud", "chaussure",
            "chemise", "cheval", "chevre", "chocolat", "ciel", "cinq", "citron", "classe", "clef", "cochon", "coeur", "cour", "crayon", "dent",
            "dix", "doigt", "dos", "ecole", "enfant", "ete", "etoile", "fee", "fenetre", "feuille", "fille", "fleur", "foret", "four",
            "frere", "fromage", "fusee", "gare", "gateau", "genou", "girafe", "gomme", "grand", "gros", "guitare", "herbe", "huit", "jambe",
            "jardin", "jaune", "jouet", "jour", "jupe", "jus", "lait", "lampe", "lapin", "lent", "ligne", "lit", "livre", "loup",
            "lourd", "lune", "maison", "maman", "manteau", "mere", "moins", "montagne", "moto", "mouton", "neige", "neuf", "nez", "niche",
            "nid", "noir", "nuage", "nuit", "oiseau", "orange", "oreille", "oui", "ours", "pantalon", "papa", "peau", "peigne", "pere",
            "petit", "peur", "pied", "plage", "pluie", "poisson", "pomme", "porte", "poule", "poupee", "quatre", "rat", "reine", "riz",
            "robe", "robot", "roche", "roue", "rouge", "route", "sable", "salade", "savon", "sel", "sept", "singe", "six", "soeur",
            "soleil", "soupe", "souris", "stylo", "sucre", "table", "tasse", "tete", "the", "tigre", "tomate", "tortue", "tour", "tulipe",
            "vache", "velo", "vent", "ventre", "verre", "vert", "vite", "vol", "zebre", "zero",
        };

        /// <summary>Les 314 syllabes qui ont une image, et le mot que l'image dessine (correspondance.csv de Camara).</summary>
        public static readonly Dictionary<string, string> SyllabesIllustrees = new(StringComparer.Ordinal)
        {
            ["a"] = "avion", ["ain"] = "train", ["am"] = "ampoule", ["an"] = "ange", ["é"] = "étoile", ["eau"] = "eau", ["eu"] = "euro", ["i"] = "igloo",
            ["im"] = "imprimante", ["in"] = "insecte", ["o"] = "olive", ["oi"] = "oiseau", ["oin"] = "poing", ["om"] = "ombre", ["on"] = "ongle", ["ou"] = "ours",
            ["u"] = "usine", ["un"] = "un", ["ba"] = "bateau", ["bi"] = "biberon", ["bo"] = "botte", ["bu"] = "bulle", ["ca"] = "canard", ["ce"] = "cerise",
            ["cha"] = "chat", ["chi"] = "chiffon", ["cho"] = "chocolat", ["chu"] = "parachute", ["ci"] = "citron", ["co"] = "cochon", ["cu"] = "culotte", ["da"] = "panda",
            ["di"] = "dinosaure", ["do"] = "domino", ["du"] = "dune", ["fa"] = "farine", ["fi"] = "filet", ["fo"] = "forêt", ["fu"] = "fusée", ["ga"] = "galette",
            ["ge"] = "genou", ["gi"] = "girafe", ["go"] = "gorille", ["ja"] = "jaguar", ["jo"] = "joker", ["ju"] = "jupe", ["la"] = "lapin", ["li"] = "livre",
            ["lo"] = "locomotive", ["lu"] = "lune", ["ma"] = "maman", ["mi"] = "miroir", ["mo"] = "moto", ["mu"] = "musique", ["na"] = "ananas", ["ni"] = "nid",
            ["no"] = "noël", ["nu"] = "numéro", ["pa"] = "papillon", ["pi"] = "pirate", ["po"] = "pomme", ["pu"] = "puce", ["ra"] = "radis", ["ri"] = "rideau",
            ["ro"] = "robe", ["ru"] = "ruche", ["sa"] = "salade", ["si"] = "sirène", ["so"] = "soleil", ["su"] = "sucre", ["ta"] = "table", ["ti"] = "tigre",
            ["to"] = "tomate", ["tu"] = "tulipe", ["va"] = "vache", ["vi"] = "visage", ["vo"] = "volcan", ["zi"] = "zigzag", ["zo"] = "zoo", ["bé"] = "bébé",
            ["be"] = "belette", ["ché"] = "marché", ["che"] = "cheval", ["dé"] = "dé", ["de"] = "demi", ["fé"] = "fée", ["fe"] = "fenêtre", ["gué"] = "guépard",
            ["gue"] = "guenon", ["je"] = "jeton", ["lé"] = "légume", ["le"] = "leçon", ["mé"] = "métro", ["me"] = "menu", ["né"] = "nénuphar", ["ne"] = "neveu",
            ["pé"] = "pédale", ["pe"] = "pelouse", ["que"] = "quenotte", ["ré"] = "réveil", ["re"] = "renard", ["sé"] = "séchoir", ["se"] = "semaine", ["té"] = "télé",
            ["te"] = "tenue", ["vé"] = "vélo", ["ve"] = "velours", ["zé"] = "zéro", ["bou"] = "bougie", ["chou"] = "chou", ["cou"] = "cou", ["dou"] = "douche",
            ["fou"] = "fourmi", ["gou"] = "gourde", ["jou"] = "jouet", ["lou"] = "loup", ["mou"] = "mouton", ["nou"] = "nouille", ["pou"] = "poule", ["rou"] = "roue",
            ["sou"] = "souris", ["tou"] = "toupie", ["ban"] = "banc", ["bon"] = "bonbon", ["can"] = "cantine", ["chan"] = "chanson", ["chon"] = "cochon", ["con"] = "concombre",
            ["dan"] = "danseuse", ["din"] = "dinde", ["don"] = "donjon", ["fan"] = "fantôme", ["fin"] = "fin", ["fon"] = "fontaine", ["gon"] = "dragon", ["jan"] = "janvier",
            ["jon"] = "jonquille", ["lan"] = "lanterne", ["lin"] = "câlin", ["lon"] = "ballon", ["man"] = "manteau", ["min"] = "chemin", ["mon"] = "montagne", ["non"] = "canon",
            ["pan"] = "pantalon", ["pin"] = "lapin", ["pon"] = "pont", ["ran"] = "orange", ["rin"] = "marin", ["ron"] = "rond", ["san"] = "sandale", ["sin"] = "singe",
            ["son"] = "poisson", ["tan"] = "tante", ["tin"] = "lutin", ["ton"] = "mouton", ["vin"] = "vingt", ["von"] = "savon", ["zon"] = "gazon", ["beu"] = "beurre",
            ["boi"] = "boisson", ["coi"] = "coiffeur", ["deu"] = "deux", ["doi"] = "doigt", ["feu"] = "feu", ["foi"] = "foire", ["gueu"] = "gueule", ["jeu"] = "jeu",
            ["joi"] = "joie", ["loi"] = "loir", ["meu"] = "meuble", ["moi"] = "moineau", ["neu"] = "neuf", ["noi"] = "noix", ["peu"] = "peur", ["poi"] = "poire",
            ["queu"] = "queue", ["reu"] = "heureux", ["roi"] = "roi", ["seu"] = "danseuse", ["soi"] = "arrosoir", ["teu"] = "chanteur", ["toi"] = "toit", ["veu"] = "cheveux",
            ["voi"] = "voiture", ["bain"] = "bain", ["chien"] = "chien", ["coin"] = "coin", ["faim"] = "faim", ["foin"] = "foin", ["lion"] = "lion", ["loin"] = "loin",
            ["main"] = "main", ["nain"] = "nain", ["pain"] = "pain", ["pie"] = "pie", ["rein"] = "rein", ["bla"] = "blason", ["blé"] = "blé", ["blo"] = "bloc",
            ["blou"] = "blouson", ["bra"] = "bras", ["bri"] = "brique", ["bro"] = "brosse", ["brou"] = "brouette", ["bru"] = "brume", ["cla"] = "clavier", ["clé"] = "clé",
            ["cli"] = "éclipse", ["clo"] = "cloche", ["clou"] = "clou", ["cra"] = "crabe", ["cré"] = "créneau", ["cri"] = "criquet", ["cro"] = "crocodile", ["crou"] = "écrou",
            ["cru"] = "cruche", ["dra"] = "drapeau", ["dru"] = "druide", ["fla"] = "flaque", ["flo"] = "flocon", ["flou"] = "flou", ["fra"] = "fragile", ["fré"] = "frégate",
            ["fri"] = "frite", ["fro"] = "fromage", ["gla"] = "glace", ["gli"] = "glisser", ["glo"] = "globe", ["glou"] = "glouton", ["glu"] = "glu", ["gra"] = "grappe",
            ["gri"] = "griffe", ["gro"] = "grotte", ["grou"] = "groupe", ["gru"] = "grue", ["pla"] = "plage", ["pli"] = "pli", ["plo"] = "explorateur", ["plou"] = "plouf",
            ["plu"] = "plume", ["pra"] = "praline", ["pré"] = "pré", ["pri"] = "prise", ["pro"] = "professeur", ["prou"] = "proue", ["tra"] = "tracteur", ["tré"] = "trésor",
            ["tri"] = "tricycle", ["tro"] = "trottinette", ["trou"] = "trou", ["vri"] = "avril", ["sca"] = "scarabée", ["sco"] = "scorpion", ["spa"] = "espace", ["spo"] = "sport",
            ["sta"] = "stade", ["sto"] = "stop", ["bac"] = "bac", ["bal"] = "balle", ["bar"] = "barbe", ["ber"] = "berceau", ["bol"] = "bol", ["bor"] = "borne",
            ["car"] = "carte", ["cil"] = "cil", ["col"] = "collier", ["cor"] = "corde", ["dar"] = "dard", ["dir"] = "dirigeable", ["dor"] = "dormir", ["fer"] = "fer",
            ["fil"] = "fil", ["for"] = "fort", ["jar"] = "jardin", ["lac"] = "lac", ["lar"] = "larme", ["lir"] = "lire", ["mal"] = "malle", ["mar"] = "marteau",
            ["mer"] = "mer", ["mil"] = "mille-pattes", ["mor"] = "morceau", ["mur"] = "mur", ["pal"] = "palme", ["par"] = "parc", ["per"] = "perle", ["pir"] = "vampire",
            ["por"] = "porte", ["sac"] = "sac", ["sar"] = "sardine", ["ser"] = "serpent", ["sol"] = "soldat", ["sor"] = "sorcière", ["sur"] = "surprise", ["tar"] = "tarte",
            ["ter"] = "terre", ["tir"] = "tirelire", ["tor"] = "tortue", ["tur"] = "turban", ["ver"] = "ver", ["vir"] = "virgule", ["ail"] = "ail", ["eil"] = "soleil",
            ["euil"] = "feuille", ["gne"] = "montagne", ["gno"] = "mignon", ["gnon"] = "oignon", ["gui"] = "guitare", ["ouil"] = "grenouille", ["qua"] = "quatre", ["qui"] = "quille",
            ["ya"] = "yaourt", ["yo"] = "yoyo",
        };
    }
}
