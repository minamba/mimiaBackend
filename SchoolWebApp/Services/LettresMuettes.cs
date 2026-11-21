using System.Text;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// UNE LETTRE MUETTE NE S'ENTEND PAS, ET LE PROFESSEUR NE DOIT JAMAIS DIRE LE CONTRAIRE — signalement #58 d'Adam,
    /// le 21/11/2026, dictée de mots au CP : « chat » dicté, « cha » écrit, et la correction « il manque une petite
    /// lettre à la fin de « chat ». Tu l'entends, ce son à la fin, « cha-t » ? ». On apprenait à un enfant de six ans
    /// à entendre un [t] qui n'existe pas : une fausse règle d'encodage, sur une notion de base.
    ///
    /// LE CODE TRANCHE, PAS LE MODÈLE (même leçon que les verdicts de calcul et les indices des nombres) : laissé
    /// libre, il explique une lettre oubliée par l'oreille, parce que c'est l'explication de toutes les autres lettres
    /// oubliées. Ici l'application compare chaque mot dicté à ce que l'enfant a écrit, et quand la seule différence
    /// est une fin MUETTE — oubliée, en trop ou remplacée —, le professeur reçoit le fait au tour même, avec la façon
    /// juste d'en parler : elle ne s'entend pas ; on la retient, ou un mot de la même famille la fait entendre.
    ///
    /// PRUDENT PAR CONSTRUCTION : un mot n'est retenu que si la différence est UNIQUEMENT une fin muette. Le moindre
    /// doute (consonne finale qui se prononce — « bus », « sept », « ours » —, « e » qui fait sonner la consonne
    /// d'avant — « petit » pour « petite ») et le mot sort de la liste : pas de fait vaut mieux qu'un fait faux.
    ///
    /// Français seulement : c'est l'orthographe du français qui a ces lettres-là.
    /// </summary>
    public static class LettresMuettes
    {
        public enum Sorte
        {
            /// <summary>« cha » pour « chat » : la fin muette est oubliée.</summary>
            Oubliee,

            /// <summary>« chats » pour « chat » (un seul) : une lettre muette en trop.</summary>
            EnTrop,

            /// <summary>« chas » pour « chat » : une fin muette, mais pas la bonne.</summary>
            Remplacee,
        }

        /// <param name="Dicte">Le mot dicté, tel que la comparaison le lit (minuscules, sans ponctuation).</param>
        /// <param name="Copie">Ce qu'il a écrit à sa place.</param>
        /// <param name="Lettres">Les lettres muettes en cause : oubliées, en trop, ou celles du mot dicté (remplacées).</param>
        /// <param name="Ecrites">Pour <see cref="Sorte.Remplacee"/> : la fin muette qu'il a écrite à la place.</param>
        public sealed record Ecart(string Dicte, string Copie, Sorte Sorte, string Lettres, string Ecrites);

        private const string ConsonnesMuettes = "tdsxzpg";
        private const string Voyelles = "aeiouyàâäéèêëîïôöùûüœ";

        /// <summary>Les mots dont la consonne finale SE PRONONCE : jamais muette, jamais dans le fait.</summary>
        private static readonly HashSet<string> FinPrononcee = new(StringComparer.Ordinal)
        {
            // s
            "bus", "autobus", "ours", "os", "fils", "mars", "tennis", "cactus", "hélas", "maïs", "jadis", "vis", "sens",
            "bis", "iris", "lotus", "virus", "atlas", "as", "terminus", "rébus", "prospectus", "humus", "blocus", "campus",
            "bonus", "sinus", "oasis", "express", "plus", "tous", "lis", "albatros", "rhinocéros", "ananas", "biceps",
            "chips", "jeans", "papyrus", "hibiscus", "eucalyptus", "pancréas", "myosotis", "tournevis", "métis", "gratis",
            "ibis", "kermès", "palmarès", "cosinus", "minus", "chorus", "pénis", "en-tête",
            // s qui se prononce, retour d'Omar sur #58 (« cassis » et les mots du même type)
            "cassis", "mœurs", "moeurs", "aloès", "florès", "ès", "pastis", "volubilis", "lapis", "pubis", "abribus",
            "minibus", "omnibus", "airbus", "trolleybus", "bibus", "calvados", "pathos", "cosmos", "tétanos", "thermos",
            "albinos", "vasistas", "fœtus", "foetus", "cubitus", "humérus", "utérus", "processus", "consensus",
            "détritus", "rictus", "hiatus", "motus", "lapsus", "mordicus", "habitus", "tonus", "stimulus", "focus",
            "crocus", "lupus", "anus", "sanctus", "corpus", "opus", "angélus", "cumulus", "nimbus", "fongus", "gus",
            "plexus", "phallus", "rasibus", "syllabus", "tumulus", "vénus", "rhésus", "sus", "mas", "lys",
            // t
            "huit", "sept", "net", "brut", "but", "chut", "zut", "dot", "mat", "ouest", "est", "août", "yaourt", "rut",
            "fat", "kit", "basket", "internet", "test", "toast", "short", "foot", "rapt", "scout", "volt", "watt", "flirt",
            "kilt", "bit", "granit", "transit", "déficit", "accessit", "aconit", "coït", "occiput", "sprint", "match",
            "tarot", "mazout", "ketchup", "huit", "contact", "exact", "direct", "correct", "intact", "compact", "tact",
            "strict", "district", "verdict", "infect", "suspect", "abject", "concept", "abrupt", "transept", "rapt",
            "script", "cobalt", "lest", "zest", "trust", "compost", "twist", "ballast", "kart", "fiat", "spot", "ut",
            // d
            "sud", "bled", "lad", "raid", "stand", "caïd", "celluloïd", "week-end", "plaid", "fjord", "lord", "oued",
            // x
            "dix", "six", "index", "lynx", "larynx", "thorax", "silex", "phénix", "fax", "box", "lux", "relax", "tex-mex",
            "coccyx", "sphinx", "borax", "anthrax", "pharynx", "mox", "ox", "latex", "télex", "codex", "vortex", "cortex",
            "duplex", "triplex", "onyx", "apex", "pyrex", "rex", "ex", "inox", "intox", "détox", "botox",
            // z
            "gaz", "fez", "quiz", "jazz", "blitz", "kibboutz", "hertz", "quartz", "ersatz", "luz", "merguez", "showbiz",
            // p
            "stop", "cap", "cep", "slip", "top", "hop", "hip", "clip", "rap", "jeep", "scalp", "handicap", "gp", "flip",
            "flop", "pop", "hip-hop", "zip", "ship", "group", "laptop", "cheap", "croup", "julep",
            // g
            "zigzag", "grog", "gag", "gang", "gong", "erg", "iceberg", "joug", "whig", "bug", "tag", "hamburger", "gag",
            "smog", "blog", "boomerang", "ginseng", "ping", "pong", "bang", "slang",
        };

        /// <summary>
        /// LE « -ENT » N'EST PAS TOUJOURS UN VERBE — retour d'Omar sur #58 (21/11/2026) : la règle « mot de cinq
        /// lettres ou plus en -ent, écrit sans le nt » faisait de « content » écrit « conte » un verbe au pluriel
        /// (« la fin -ent des verbes avec ils ou elles »), alors que le nt de « content » n'est pas muet : on y entend
        /// [ɑ̃]. Même faute pour « parent », « moment », « souvent ». Ces noms, adjectifs et adverbes ne passent
        /// jamais par la règle des verbes ; leur « t » final, lui, reste muet (« conten » pour « content » : la lettre
        /// « t » oubliée, c'est vrai).
        /// </summary>
        private static readonly HashSet<string> NonVerbesEnEnt = new(StringComparer.Ordinal)
        {
            // demandés par Omar
            "content", "parent", "moment", "souvent", "accident", "argent", "client", "serpent", "talent", "patient",
            "prudent", "dent", "vent", "lent",
            // noms
            "agent", "sergent", "régent", "accent", "adolescent", "torrent", "continent", "instrument", "monument",
            "argument", "document", "aliment", "ciment", "piment", "compliment", "sentiment", "régiment", "segment",
            "fragment", "pigment", "testament", "tempérament", "condiment", "sédiment", "détriment", "rudiment",
            "firmament", "filament", "médicament", "parlement", "serment", "tourment", "orient", "occident",
            "incident", "trident", "quotient", "récipient", "ingrédient", "inconvénient", "coefficient", "gradient",
            "arpent", "avent", "onguent", "tournevent", "paravent", "auvent", "contrevent", "engoulevent", "évent",
            "dissident", "chiendent", "pourcent",
            // adjectifs
            "absent", "présent", "récent", "décent", "indécent", "innocent", "urgent", "intelligent", "diligent",
            "indulgent", "évident", "imprudent", "impudent", "insolent", "indolent", "opulent", "succulent", "turbulent",
            "transparent", "apparent", "permanent", "éminent", "imminent", "pertinent", "impertinent", "fréquent",
            "conséquent", "éloquent", "subséquent", "latent", "compétent", "incompétent", "impatient", "conscient",
            "inconscient", "efficient", "clément", "véhément", "strident", "ardent", "fervent", "décadent",
            "omnipotent", "impotent", "magnificent", "malcontent", "mécontent", "cohérent", "incohérent",
            "inhérent", "fluorescent", "phosphorescent", "incandescent", "confident", "excédent", "obédient",
            "afférent", "évident",
            // adverbes
            "comment",
        };

        /// <summary>
        /// LES HOMOGRAPHES — même retour d'Omar : « président » est un nom (le président, t muet, on entend [ɑ̃]) et
        /// un verbe (ils président, nt muet) ; « couvent », « résident », « violent », « excellent », « affluent »,
        /// « convient » aussi. Sans le sens de la phrase, le code ne peut pas savoir lequel l'enfant devait écrire :
        /// il ne dit RIEN sur ces mots, le professeur corrige seul.
        /// </summary>
        private static readonly HashSet<string> HomographesEnEnt = new(StringComparer.Ordinal)
        {
            // demandés par Omar
            "président", "couvent", "résident", "violent", "excellent", "affluent", "convient",
            // même cas : un nom ou un adjectif, et la 3e personne du pluriel d'un verbe (ou du singulier, « il dément »)
            "expédient", "négligent", "différent", "émergent", "influent", "adhérent", "coïncident", "divergent",
            "convergent", "équivalent", "précédent", "somnolent", "confluent", "détergent", "effluent", "ferment",
            "dément", "résurgent", "pressent",
        };

        /// <summary>
        /// Les terminaisons qui ne sont pas celles d'un verbe conjugué avec « ils » ou « elles » : « -ement »,
        /// « -ément » (vêtement, élément, lentement), « -amment », « -emment » (savamment, prudemment). Et
        /// « -vient », « -tient » (il vient, il tient, il convient) : un verbe au singulier, dont le « n » s'entend.
        /// </summary>
        private static bool PasUnVerbeEnEnt(string mot) =>
            NonVerbesEnEnt.Contains(mot)
            || mot.EndsWith("ement", StringComparison.Ordinal)
            || mot.EndsWith("ément", StringComparison.Ordinal)
            || mot.EndsWith("amment", StringComparison.Ordinal)
            || mot.EndsWith("emment", StringComparison.Ordinal)
            || mot.EndsWith("vient", StringComparison.Ordinal)
            || mot.EndsWith("tient", StringComparison.Ordinal);

        /// <summary>
        /// Un mot de la même famille où la lettre muette S'ENTEND, avec son propre son — seulement ceux dont on est
        /// sûr. Pas de « loup » → « louve » (on entend un v, pas un p), ni « rang » → « ranger » (un j, pas un g) :
        /// sans mot sûr, le fait n'en donne pas.
        /// </summary>
        private static readonly Dictionary<string, string> Famille = new(StringComparer.Ordinal)
        {
            ["chat"] = "chaton", ["rat"] = "raton", ["petit"] = "petite", ["grand"] = "grande", ["gros"] = "grosse",
            ["lait"] = "laitier", ["dent"] = "dentiste", ["bord"] = "border", ["vert"] = "verte",
            ["gris"] = "grise", ["sport"] = "sportif", ["tard"] = "tarder", ["retard"] = "retarder", ["chaud"] = "chaude",
            ["froid"] = "froide", ["long"] = "longue", ["dos"] = "dossier", ["camp"] = "camper", ["galop"] = "galoper",
            ["bras"] = "brassard", ["gras"] = "grasse", ["bas"] = "basse", ["haut"] = "haute", ["fort"] = "forte",
            ["court"] = "courte", ["plat"] = "plate", ["fruit"] = "fruitier", ["toit"] = "toiture", ["pot"] = "poterie",
            ["sabot"] = "saboter", ["chocolat"] = "chocolaté", ["blond"] = "blonde", ["rond"] = "ronde", ["lourd"] = "lourde",
            ["profond"] = "profonde", ["bavard"] = "bavarde", ["content"] = "contente", ["méchant"] = "méchante",
            ["éléphant"] = "éléphanteau", ["chant"] = "chanter", ["saut"] = "sauter", ["bond"] = "bondir",
            ["tapis"] = "tapisser", ["drap"] = "draper", ["climat"] = "climatiseur", ["lit"] = "literie",
            ["trot"] = "trotter", ["front"] = "frontal", ["goût"] = "goûter", ["regard"] = "regarder", ["port"] = "porter",
            ["accord"] = "accorder", ["marchand"] = "marchande", ["cuit"] = "cuite", ["nuit"] = "nuitée",
            ["sang"] = "sanguin", ["vent"] = "venteux",
            ["gourmand"] = "gourmande", ["mont"] = "montagne", ["muet"] = "muette", ["sot"] = "sotte",
            ["idiot"] = "idiote",
        };

        private static bool EstVoyelle(char c) => Voyelles.Contains(c);

        /// <summary>
        /// La fin MUETTE d'un mot écrit en minuscules : la ou les consonnes finales qui ne se prononcent pas (« t » de
        /// « chat », « ps » de « temps »), le « e » ou le « es » final, ou « nt » d'un verbe en « -ent ». Vide si on
        /// ne peut pas l'affirmer.
        /// </summary>
        public static string FinMuette(string mot)
        {
            if (mot.Length < 2 || FinPrononcee.Contains(mot) || mot.Any(c => !char.IsLetter(c))) return string.Empty;

            // « -es » et « -e » : « les », « mes », « que » se prononcent — trois lettres au moins, et pas « que ».
            if (mot.Length >= 4 && mot.EndsWith("es", StringComparison.Ordinal)) return "es";
            if (mot.Length >= 3 && mot[^1] == 'e' && mot != "que") return "e";

            // Une consonne finale après un « c » (« contact », « respect ») se prononce souvent : on s'abstient.
            var debut = mot.Length;
            while (debut > 0 && ConsonnesMuettes.Contains(mot[debut - 1])) debut -= 1;
            if (debut == mot.Length || debut == 0 || mot[debut - 1] == 'c') return string.Empty;

            // « -ing » des mots anglais (« parking ») : le g s'entend un peu ; on s'abstient.
            if (mot.EndsWith("ing", StringComparison.Ordinal)) return string.Empty;

            return mot[debut..];
        }

        /// <summary>Le mot sans sa fin muette.</summary>
        private static string Radical(string mot) => mot[..^FinMuette(mot).Length];

        /// <summary>
        /// Un « e » muet ne l'est vraiment que derrière une voyelle (« amie », « joie », « rue ») : derrière une
        /// consonne, il la fait sonner (« petite », « porte »), et « petit » pour « petite » s'entend.
        /// </summary>
        private static bool EVraimentMuet(string lettres, string avant) =>
            !lettres.StartsWith('e') || (avant.Length > 0 && EstVoyelle(avant[^1]));

        /// <summary>L'écart de ce mot, s'il ne tient QU'À une fin muette ; null sinon.</summary>
        public static Ecart? Lire(string dicte, string copie)
        {
            if (dicte.Length == 0 || copie.Length == 0 || dicte == copie) return null;

            // Nom ou verbe ? Sans la phrase, on ne sait pas : rien (« président », « couvent »).
            if (HomographesEnEnt.Contains(dicte)) return null;

            // « -ent » d'un verbe au pluriel : « mangent » écrit « mange ». Jamais pour « content », « moment »… :
            // leur nt ne se tait pas ; ils continuent plus bas comme tout autre mot (seul leur « t » est muet).
            if (dicte.Length >= 5 && dicte.EndsWith("ent", StringComparison.Ordinal) && copie == dicte[..^2]
                && !PasUnVerbeEnEnt(dicte))
            {
                return new Ecart(dicte, copie, Sorte.Oubliee, "nt", string.Empty);
            }

            var finDicte = FinMuette(dicte);
            var finCopie = FinMuette(copie);

            // Oubliée : « cha » pour « chat », « pomme » pour « pommes ».
            if (dicte.StartsWith(copie, StringComparison.Ordinal))
            {
                var manque = dicte[copie.Length..];
                return finDicte.EndsWith(manque, StringComparison.Ordinal) && EVraimentMuet(manque, copie)
                    ? new Ecart(dicte, copie, Sorte.Oubliee, manque, string.Empty)
                    : null;
            }

            // En trop : « chats » pour un seul « chat ».
            if (copie.StartsWith(dicte, StringComparison.Ordinal))
            {
                var trop = copie[dicte.Length..];
                return finCopie.EndsWith(trop, StringComparison.Ordinal) && EVraimentMuet(trop, dicte)
                    && !FinPrononcee.Contains(copie)
                    ? new Ecart(dicte, copie, Sorte.EnTrop, trop, string.Empty)
                    : null;
            }

            // Remplacée : « chas » ou « chad » pour « chat », « li » + « e » pour « lit ».
            var radical = Radical(dicte);
            if (finDicte.Length > 0 && finCopie.Length > 0 && radical == Radical(copie)
                && EVraimentMuet(finDicte, radical) && EVraimentMuet(finCopie, radical))
            {
                return new Ecart(dicte, copie, Sorte.Remplacee, finDicte, finCopie);
            }

            return null;
        }

        /// <summary>Les écarts de lettres muettes de toute la copie, mot à mot, sans doublon.</summary>
        public static IReadOnlyList<Ecart> Ecarts(string? dicte, string? copie) =>
            EcartsDictee.MotsMalEcrits(dicte, copie)
                .Select(p => Lire(p.Dicte, p.Copie))
                .OfType<Ecart>()
                .DistinctBy(e => (e.Dicte, e.Copie))
                .ToList();

        /// <summary>
        /// Le fait du tour pour une correction de dictée en FRANÇAIS, ou null s'il n'y a aucune lettre muette en cause.
        /// Sans crochet fermant avant la fin : il ne s'affiche jamais.
        /// </summary>
        public static string? Fait(string? dicte, string? copie)
        {
            var ecarts = Ecarts(dicte, copie);
            if (ecarts.Count == 0) return null;

            var lignes = new StringBuilder();
            foreach (var e in ecarts)
            {
                lignes.Append("\n- ").Append(Ligne(e));
            }

            return "[Vérifié par l'application — LETTRES MUETTES dans sa copie. Une lettre muette ne se prononce pas : "
                + "on ne l'entend JAMAIS, même en écoutant très bien. Ce n'est donc pas son oreille qui s'est trompée."
                + lignes
                + "\nPour ces mots-là, ne dis JAMAIS qu'il l'entend ou qu'il aurait dû l'entendre, ne lui demande pas "
                + "d'écouter la fin du mot ni s'il l'entend, et ne découpe pas le mot pour faire sonner cette lettre : ce "
                + "serait lui apprendre une fausse prononciation. DIS-LUI toi-même qu'elle ne s'entend pas, qu'elle se retient en regardant "
                + "bien le mot, ou qu'elle se trouve grâce à un mot de la même famille où on l'entend, quand il y en a un.]";
        }

        private static string Lettres(string lettres) =>
            lettres.Length == 1 ? $"la lettre « {lettres} »" : $"les lettres « {lettres} »";

        private static string Ligne(Ecart e)
        {
            var aide = e.Sorte == Sorte.EnTrop ? null : Aide(e.Dicte, e.Lettres);

            var constat = e.Sorte switch
            {
                Sorte.Oubliee =>
                    $"« {e.Dicte} » : il a écrit « {e.Copie} ». Il manque {Lettres(e.Lettres)} de la fin, qui "
                    + (e.Lettres.Length == 1 ? "est MUETTE : elle ne s'entend pas" : "sont MUETTES : elles ne s'entendent pas")
                    + $" quand on dit « {e.Dicte} ».",
                Sorte.EnTrop =>
                    $"« {e.Dicte} » : il a écrit « {e.Copie} ». {Majuscule(Lettres(e.Lettres))} en trop "
                    + (e.Lettres.Length == 1 ? "est muette" : "sont muettes")
                    + " : il ne l'a pas entendue, il l'a ajoutée. Explique pourquoi ce mot-là ne la prend pas.",
                _ =>
                    $"« {e.Dicte} » : il a écrit « {e.Copie} ». Il a mis une fin muette (« {e.Ecrites} »), mais ce n'est "
                    + $"pas la bonne : ce mot finit par « {e.Lettres} », muet{(e.Lettres.Length == 1 ? string.Empty : "s")} aussi. "
                    + "Aucune des deux ne s'entend.",
            };

            return aide is null ? constat : $"{constat} {aide}";
        }

        private static string Majuscule(string texte) => char.ToUpperInvariant(texte[0]) + texte[1..];

        private static string? Aide(string mot, string lettres)
        {
            if (lettres is "nt") return "C'est la fin « -ent » des verbes avec « ils » ou « elles », qui ne s'entend pas.";
            if (lettres.StartsWith('e')) return "Le « e » de la fin ne s'entend pas : il se retient.";

            if (Famille.TryGetValue(mot, out var parent) && parent != mot)
            {
                return $"Un mot de la même famille la fait entendre : « {parent} ».";
            }

            if (lettres is "s" or "x")
            {
                return $"Si le mot parle de plusieurs choses, « {lettres} » est la marque du pluriel ; sinon, il fait partie du mot et se retient.";
            }

            return null;
        }
    }
}
