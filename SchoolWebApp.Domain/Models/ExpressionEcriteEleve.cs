namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES CINQ GENRES DE REPRISE, ET CE SONT CEUX DE L'EXAMEN.
    ///
    /// Quatre catégories de faute plus une de réussite, parce que la consigne
    /// du professeur impose de commencer par ce qui est réussi : une archive
    /// qui ne garderait que les fautes ferait de chaque relecture un rappel
    /// d'échec.
    ///
    /// EN TOUTES LETTRES ET EN MINUSCULES, jamais un entier : une ligne lue
    /// directement en base doit se comprendre sans aller chercher la table de
    /// correspondance dans le code — même règle que pour les statuts d'idée,
    /// les urgences et les variantes de planche.
    /// </summary>
    public static class GenreReprise
    {
        /// <summary>Ce qui marche. Le professeur commence toujours par là.</summary>
        public const string Reussi = "reussi";

        public const string Orthographe = "orthographe";
        public const string Grammaire = "grammaire";
        public const string Vocabulaire = "vocabulaire";

        /// <summary>L'ordre des mots, la phrase qui ne tient pas, le texte qui part partout.</summary>
        public const string Construction = "construction";

        public static readonly IReadOnlyList<string> Tous =
            [Reussi, Orthographe, Grammaire, Vocabulaire, Construction];

        /// <summary>
        /// Ramène ce qu'a écrit le modèle à l'un des cinq genres.
        ///
        /// UN GENRE INCONNU DEVIENT `construction` PLUTÔT QUE DE FAIRE TOMBER
        /// LA LIGNE. Le modèle écrit « syntaxe », « conjugaison », « style » —
        /// on ne va pas perdre la correction d'un enfant parce qu'il a choisi
        /// un synonyme. `construction` est le genre le plus général des quatre,
        /// donc le moins faux quand on ne sait pas.
        /// </summary>
        public static string Normaliser(string? genre)
        {
            var propre = (genre ?? string.Empty).Trim().ToLowerInvariant();

            return propre switch
            {
                "reussi" or "réussi" or "reussite" or "réussite" or "bien" => Reussi,
                "orthographe" or "ortho" => Orthographe,
                "grammaire" or "conjugaison" or "accord" => Grammaire,
                "vocabulaire" or "lexique" or "mot" => Vocabulaire,
                _ => Construction,
            };
        }
    }

    /// <summary>
    /// Une chose relevée par le professeur : son genre, et ce qu'il en dit.
    ///
    /// LE TEXTE PORTE LA FORME JUSTE, PAS LE CONSTAT DE LA FAUTE. La consigne
    /// est explicite là-dessus : « Tu as écrit *I have 12 years*. On dit *I am
    /// 12 years old* — l'âge se dit avec *to be*. » L'élève doit repartir avec
    /// la bonne phrase.
    /// </summary>
    public record RepriseEcrite(string Genre, string Texte);

    /// <summary>
    /// Un texte écrit par l'élève et sa correction, tels qu'il les relit.
    ///
    /// CE QU'ON ARCHIVE, C'EST LA PAIRE — et c'est la raison d'être de cet
    /// écran. Le texte seul ne vaut rien à relire, la correction seule encore
    /// moins : ce qui apprend, c'est de voir ce qu'on a écrit à côté de ce
    /// qu'il fallait écrire. Même leçon que la copie d'évaluation que Camara a
    /// fait rendre à l'élève.
    ///
    /// LE TROISIÈME DE LA FAMILLE : <see cref="ComprehensionOraleEleve"/> garde
    /// ce qu'il a ENTENDU, <see cref="ExpressionOraleEleve"/> ce qu'il a DIT,
    /// celui-ci ce qu'il a ÉCRIT. C'est le seul où son orthographe se voit.
    /// </summary>
    public class ExpressionEcriteEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        /// <summary>
        /// Le prénom du professeur et sa couleur, pour la pastille de la
        /// correction. Celui de l'élève n'est pas ici : l'écran l'a déjà.
        /// </summary>
        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        /// <summary>De quoi il a parlé — « Raconter son week-end ». Toujours renseigné.</summary>
        public string Titre { get; set; } = string.Empty;

        /// <summary>Code de la langue écrite : fr, en, es, de, it, zh.</summary>
        public string Langue { get; set; } = string.Empty;

        /// <summary>
        /// Ce que le professeur a demandé, mot pour mot.
        ///
        /// ELLE PART MÊME DANS LA LISTE, contrairement au texte : c'est elle qui
        /// dit à l'élève ce qu'il retrouvera en ouvrant, et elle tient en une
        /// ligne.
        /// </summary>
        public string Consigne { get; set; } = string.Empty;

        /// <summary>
        /// Le texte de l'élève, TEL QU'IL L'A ÉCRIT — fautes comprises.
        ///
        /// VIDE DANS LA LISTE, REMPLI DANS LE DÉTAIL, comme l'échange d'une
        /// conversation : trente textes ne transitent pas pour afficher trente
        /// titres.
        ///
        /// NULL VEUT DIRE « PAS ENCORE RECOPIÉ » : il a écrit sur son cahier, sa
        /// photo est là, et le professeur n'a pas eu le temps de la transcrire.
        /// Voir <see cref="APhoto"/>.
        /// </summary>
        public string? Texte { get; set; }

        /// <summary>
        /// Sa photo de cahier est-elle encore consultable ?
        ///
        /// LE BOOLÉEN ET NON LES OCTETS : une liste de trente textes ne fait pas
        /// voyager trente photos de trois mégaoctets. L'image se demande par sa
        /// propre route, quand l'écran en a besoin.
        /// </summary>
        public bool APhoto { get; set; }

        /// <summary>
        /// Vrai quand le texte a été recopié. Faux tant qu'il n'y a que la photo.
        ///
        /// L'ÉCRAN EN A BESOIN POUR NE PAS MENTIR : une archive sans texte n'est
        /// pas une archive vide, c'est un travail qui attend d'être repris.
        /// </summary>
        public bool Transcrit { get; set; }

        /// <summary>Ce que le professeur a relevé. Vide dans la liste, lui aussi.</summary>
        public List<RepriseEcrite> Corrections { get; set; } = [];

        /// <summary>
        /// Combien de reprises, hors réussites, pour l'afficher dans la liste
        /// sans charger la correction.
        ///
        /// SANS LES RÉUSSITES, ET C'EST VOULU : « 6 points » sur une carte, dont
        /// trois compliments, se lirait comme six fautes. On compte ce qui est à
        /// retravailler.
        /// </summary>
        public int NombreReprises { get; set; }

        /// <summary>Le nombre de mots du texte. Dit l'effort avant l'ouverture.</summary>
        public int NombreMots { get; set; }

        public string? Remarque { get; set; }

        public int? NiveauScolaireId { get; set; }

        public string? NiveauLibelle { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateConsultation { get; set; }
    }
}
