namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une conversation d'expression orale archivée : l'élève a PARLÉ dans la
    /// langue étudiée, le professeur lui a répondu dans cette langue, et on
    /// garde l'échange.
    ///
    /// CE N'EST PAS UNE COMPRÉHENSION ORALE — voulu par Camara le 18/09/2026,
    /// et les deux vivent côte à côte sans se confondre :
    ///
    ///   - <see cref="ComprehensionOrale"/> : le professeur LIT un passage,
    ///     l'élève ÉCOUTE et répond à des questions dessus. Ce qu'on archive,
    ///     c'est le passage, sa réponse, et ce qu'il en a compris. L'audio est
    ///     resynthétisé pour être réécouté.
    ///   - Ici : les deux PARLENT, en alternance, dans la langue du cours. Ce
    ///     qu'on archive, c'est la CONVERSATION — qui a dit quoi, dans
    ///     l'ordre.
    ///
    /// AUCUN AUDIO, ET C'EST DEMANDÉ : « ça sera aussi archivé comme la
    /// compréhension orale sans besoin d'avoir les audio. On aura juste la
    /// discussion entre l'élève et le professeur d'affichée comme dans une
    /// messagerie classique. » Un échange de dix tours regénéré en synthèse
    /// vocale coûterait dix fois ce que coûte une compréhension orale, pour
    /// une conversation qu'on relit plutôt qu'on ne réécoute.
    ///
    /// RÉSERVÉ AUX MATIÈRES DE LANGUE — français, anglais, espagnol, et celles
    /// qui viendront. Parler anglais en cours de mathématiques n'a pas de
    /// sens ; le filtre est le même que pour les dictées, côté écran comme
    /// dans la consigne du professeur.
    /// </summary>
    public partial class ExpressionOrale
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        /// <summary>Séance pendant laquelle la conversation a eu lieu.</summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// De quoi on a parlé, en quelques mots — « Commander au restaurant »,
        /// « Raconter ses vacances ».
        ///
        /// OBLIGATOIRE ICI, contrairement à la compréhension orale. Camara :
        /// « il y aura un titre ». Une compréhension orale sans titre se
        /// reconnaît aux premiers mots de son passage ; une conversation, non
        /// — « Hello! How are you? » ne distingue rien de rien.
        /// </summary>
        public string Titre { get; set; } = null!;

        /// <summary>Code de la langue parlée : en, fr, es, de, it, zh.</summary>
        public string Langue { get; set; } = null!;

        /// <summary>
        /// La conversation entière, en JSON :
        /// <c>[{"qui":"eleve","texte":"Hello!"},{"qui":"professeur","texte":"Hi Bilal!"}]</c>
        ///
        /// POURQUOI UNE COLONNE ET NON UNE TABLE DE TOURS
        /// ---------------------------------------------
        /// Cet échange s'écrit d'un bloc à l'archivage et se lit d'un bloc à
        /// l'affichage. On ne cherche jamais « les tours où l'élève a employé
        /// le prétérit », on ne compte rien, on ne trie rien : une table
        /// aurait ajouté une entité, une clé étrangère et un `Include` à
        /// chaque lecture pour ranger ce qui ne se consulte que d'une pièce.
        ///
        /// C'est le même raisonnement que <c>PlancheSchema.Reperes</c>, et la
        /// même limite : si un jour on veut interroger l'intérieur, il faudra
        /// une table.
        /// </summary>
        public string Echange { get; set; } = null!;

        /// <summary>
        /// Ce que le professeur retient de l'échange, lisible par l'élève et
        /// par le parent. Facultatif.
        /// </summary>
        public string? Remarque { get; set; }

        /// <summary>
        /// La classe de l'élève au moment de la conversation.
        ///
        /// FIGÉE, comme pour les autres archives : une conversation de 6e ne
        /// devient pas une conversation de 5e parce que l'année a tourné.
        /// </summary>
        public int? NiveauScolaireId { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Quand l'élève l'a ouverte. Null tant qu'il ne l'a pas fait — c'est
        /// ce qui alimente la pastille « à consulter » sur sa carte de matière.
        /// </summary>
        public DateTime? DateConsultation { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }
    }
}
