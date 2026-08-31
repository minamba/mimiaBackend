namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// Le vocabulaire attendu par le transcripteur, selon la matière.
    ///
    /// POURQUOI CE FICHIER EXISTE
    /// --------------------------
    /// Un élève a dit « pluriel » ; il est arrivé « la pupille féminin ». Le
    /// transcripteur arbitre entre des sons proches d'après ce qu'il juge
    /// probable, et hors contexte « pupille » est un mot courant quand
    /// « pluriel » ne l'est pas. Rien ne lui disait qu'il écoutait un cours.
    ///
    /// POURQUOI PAR MATIÈRE ET NON UNE LISTE UNIQUE
    /// --------------------------------------------
    /// Une liste unique a été essayée : elle mélangeait « hypoténuse » et
    /// « photosynthèse » à « participe passé ». Ce champ PONDÈRE — plus il
    /// contient de termes hors sujet, moins il pèse sur ceux qui comptent. Les
    /// mots de SVT n'ont rien à faire dans un cours d'anglais, et ils y
    /// diluent l'effet qu'on cherche.
    ///
    /// COURT, ET SANS PHRASES. Ce champ oriente le vocabulaire, il ne donne
    /// pas de consigne : une instruction rédigée y serait comprise comme du
    /// texte à transcrire et pourrait ressortir dans la réponse de l'élève.
    /// D'où des listes de termes, et rien d'autre.
    /// </summary>
    public static class VocabulaireTranscription
    {
        /// <summary>
        /// Ce qu'on entend dans TOUTES les matières : le langage de la classe
        /// lui-même. Il tient en une ligne pour laisser la place au reste.
        /// </summary>
        private const string Commun =
            "consigne, exercice, énoncé, exemple, question, réponse, "
            + "je n'ai pas compris, tu peux répéter";

        private static readonly IReadOnlyDictionary<string, string> ParMatiere =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FRANCAIS"] =
                    "singulier, pluriel, masculin, féminin, accord, sujet, verbe, "
                    + "participe passé, auxiliaire, conjugaison, orthographe, dictée, "
                    + "homophone, terminaison, adjectif, adverbe, complément, "
                    + "proposition, subordonnée, imparfait, passé simple, subjonctif, "

                    // LES LETTRES ÉPELÉES, PARCE QU'ELLES SE PERDENT.
                    //
                    // « é-e », épelé pour donner un accord au féminin, est
                    // ressorti « et eux » : les deux se prononcent pareil, et
                    // hors contexte « et eux » est de très loin le plus
                    // courant. Nommer ici les terminaisons de l'accord déplace
                    // ce pari du bon côté — c'est tout ce que ce champ sait
                    // faire, et c'est déjà ça.
                    //
                    // Ça ne rend pas l'épellation fiable pour autant : la
                    // consigne du professeur lui dit de faire ÉCRIRE une
                    // terminaison plutôt que de la faire dire.
                    + "é, ée, és, ées, e accent aigu, e accent grave, e muet, "
                    + "s final, épeler, lettre, terminaison en ée, "

                    // LE VOCABULAIRE DE L'ANALYSE, ET LA PHRASE QUI REVIENT.
                    //
                    // « COD » est ressorti « CUD » : le sigle n'existe pas hors
                    // de l'école, et rien ne disait qu'on y était. « est avant »
                    // est ressorti « étardant » — la liaison donne /ɛ.ta.vɑ̃/,
                    // et « étardant » n'est pas un mot, mais « est avant » ne
                    // l'était pas davantage pour un modèle qui ne sait pas de
                    // quoi on parle.
                    //
                    // La phrase entière est nommée, pas seulement ses mots :
                    // c'est elle qui revient à chaque tour de cette leçon, et
                    // c'est sur elle que le pari doit porter.
                    + "COD, COI, complément d'objet direct, complément d'objet "
                    + "indirect, placé avant le verbe, placé après le verbe, "
                    + "auxiliaire avoir, auxiliaire être",

                ["ANGLAIS"] =
                    "prétérit, present perfect, verbe irrégulier, pluriel, "
                    + "prononciation, traduction, vocabulaire, temps, auxiliaire, "
                    + "adjectif, préposition, comparatif, superlatif",

                ["MATHS"] =
                    "fraction, numérateur, dénominateur, équation, inéquation, "
                    + "multiplication, division, soustraction, addition, périmètre, "
                    + "aire, volume, hypoténuse, théorème, Pythagore, Thalès, "
                    + "proportionnalité, pourcentage, décimal, entier, puissance, "
                    + "racine carrée, abscisse, ordonnée, parallèle, perpendiculaire",

                ["PHYSIQUE_CHIMIE"] =
                    "molécule, atome, électron, proton, neutron, ion, masse, volume, "
                    + "densité, tension, intensité, résistance, circuit, ampoule, "
                    + "réaction, réactif, produit, solution, soluté, solvant, "
                    + "distillation, ébullition, condensation, énergie, force, vitesse",

                ["SVT"] =
                    "cellule, noyau, membrane, mitochondrie, chloroplaste, "
                    + "photosynthèse, respiration, digestion, nutriment, sang, "
                    + "artère, veine, alvéole, chromosome, gène, ADN, mitose, méiose, "
                    + "fécondation, espèce, évolution, sélection naturelle, séisme, "
                    + "plaque tectonique, subduction, érosion, sédiment",

                ["SCIENCES"] =
                    "matière, état, solide, liquide, gaz, mélange, volume, masse, "
                    + "circuit, énergie, vivant, végétal, animal, croissance, "
                    + "alimentation, hygiène, planète, Terre, Soleil, Lune, saison",

                ["HISTOIRE_GEO"] =
                    "siècle, révolution, république, monarchie, empire, guerre, "
                    + "traité, colonisation, industrialisation, démocratie, "
                    + "territoire, population, densité, métropole, littoral, "
                    + "climat, urbanisation, mondialisation, latitude, longitude",

                ["PHILOSOPHIE"] =
                    "conscience, inconscient, liberté, devoir, bonheur, justice, "
                    + "vérité, raison, nature, culture, technique, travail, langage, "
                    + "État, problématique, dissertation, thèse, argument, objection, "
                    + "concept, notion",
            };

        /// <summary>
        /// Les matières où l'élève PARLE UNE AUTRE LANGUE que le français.
        ///
        /// POURQUOI ELLES CHANGENT LA DONNE
        /// --------------------------------
        /// La langue est imposée au transcripteur, et c'est une bonne chose :
        /// sans elle, « deux » revenait en chinois et « bravo » en arabe. Un
        /// élève français parle français, il n'y a rien à deviner.
        ///
        /// Sauf en cours de langue. Là il parle les DEUX : il pose ses
        /// questions en français et produit ses phrases dans la langue
        /// étudiée — souvent dans le même tour. « J'ai juste dit my parents
        /// work » est ressorti « I juste dit my parents work ».
        ///
        /// Aucun code ISO ne dit « français OU anglais ». On lâche donc la
        /// contrainte pour ces matières et on laisse la détection faire son
        /// travail, segment par segment.
        ///
        /// LE CHOIX EST DÉSÉQUILIBRÉ, ET C'EST VOULU. Imposer le français
        /// abîme la phrase anglaise — celle qu'on ÉVALUE. La détection, elle,
        /// ne se trompe que sur des mots isolés, et ce qu'elle produit alors
        /// est écarté plus loin par le filtre d'alphabet. On préfère abîmer le
        /// bavardage que la réponse.
        /// </summary>
        private static readonly HashSet<string> MatieresBilingues =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "ANGLAIS", "ESPAGNOL", "ALLEMAND", "ITALIEN", "CHINOIS",
            };

        /// <summary>
        /// La langue à imposer, ou null pour laisser le transcripteur décider.
        /// </summary>
        public static string? Langue(string? matiereCode) =>
            matiereCode is not null && MatieresBilingues.Contains(matiereCode) ? null : "fr";

        /// <summary>
        /// Les mots à souffler pour cette matière.
        ///
        /// Une matière inconnue — ou une session ouverte sans contexte — reçoit
        /// le seul socle commun. C'est le bon défaut : mieux vaut une
        /// pondération faible que celle d'une autre matière.
        /// </summary>
        public static string Pour(string? matiereCode)
        {
            var socle = "Cours particulier, France. " + Commun;

            return matiereCode is not null && ParMatiere.TryGetValue(matiereCode, out var termes)
                ? socle + ", " + termes
                : socle;
        }
    }
}
