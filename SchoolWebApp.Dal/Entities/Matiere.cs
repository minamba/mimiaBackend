namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une matière = un agent conversationnel spécialisé.
    /// Le noyau pédagogique est commun ; c'est AgentSlug qui sélectionne
    /// la couche de spécialité et les outils (SymPy, correcteur, etc.).
    /// </summary>
    public partial class Matiere
    {
        public int Id { get; set; }

        /// <summary>MATHS, FRANCAIS, HISTOIRE_GEO, ANGLAIS, SCIENCES...</summary>
        public string? Code { get; set; }

        public string? Libelle { get; set; }

        /// <summary>Identifiant de la couche de prompt et des outils de l'agent.</summary>
        public string? AgentSlug { get; set; }

        /// <summary>
        /// Prénom du professeur de la matière. Un enfant ne travaille pas avec
        /// « l'agent maths » : il travaille avec Nora. L'agent se présente avec
        /// ce prénom et s'y tient d'une séance à l'autre.
        /// </summary>
        public string? ProfPrenom { get; set; }

        /// <summary>Clé de l'avatar dessiné côté front (nora, adrien, salim…).</summary>
        public string? ProfAvatar { get; set; }

        /// <summary>Couleur d'accent du professeur, en hexadécimal.</summary>
        public string? ProfCouleur { get; set; }

        /// <summary>
        /// Ce que la matière promet à l'enfant, en une phrase.
        ///
        /// « Calculs, problèmes et géométrie ». Un nom de matière seul ne dit
        /// rien à un enfant de neuf ans ; cette phrase lui dit ce qu'il va
        /// FAIRE.
        ///
        /// EN BASE ET NON DANS LE FRONT, malgré son air de texte de vente. Elle
        /// y était, et le défaut s'est produit exactement comme prévu : une
        /// matière ajoutée partout ailleurs — semeur, avatars, page d'accueil —
        /// arrivait dans la grille avec un professeur, un motif, une couleur, et
        /// une phrase VIDE. Rien ne plantait, rien ne le signalait.
        ///
        /// Une matière se décrit désormais à un seul endroit.
        /// </summary>
        public string? Promesse { get; set; }

        /// <summary>Ordre d'affichage dans la grille de matières.</summary>
        public int Ordre { get; set; }

        /// <summary>
        /// Bornes de l'Ordre du niveau scolaire où cette matière existe.
        ///
        /// POURQUOI UN SIMPLE « Active » NE SUFFIT PAS
        /// -------------------------------------------
        /// Toutes les matières ne se pratiquent pas de la même façon selon le
        /// niveau, et certaines n'existent tout simplement pas. Les sciences en
        /// sont l'exemple le plus net : « Sciences et technologie » est UNE
        /// matière jusqu'à la 6e, puis elle se scinde en SVT et physique-chimie
        /// dès la 5e — deux professeurs, deux notes au bulletin. Un élève de
        /// CM1 à qui on proposerait « Physique-Chimie » ne reconnaîtrait rien de
        /// son école ; un élève de 4e à qui on proposerait « Sciences » non plus.
        ///
        /// Le besoin dépasse les sciences : la philosophie n'existe qu'en
        /// terminale, la technologie qu'au collège, la SNT qu'en seconde. Un
        /// booléen global ne sait dire que « ouverte » ou « fermée », jamais
        /// « ouverte à partir de la 5e ».
        ///
        /// Bornes INCLUSES. On borne sur l'Ordre et non sur le code du niveau :
        /// c'est ce qui permet d'écrire « de la 5e à la terminale » sans lister
        /// les huit niveaux, et de continuer à fonctionner si un niveau
        /// s'intercale un jour.
        /// </summary>
        public int NiveauOrdreMin { get; set; } = 1;

        /// <inheritdoc cref="NiveauOrdreMin"/>
        public int NiveauOrdreMax { get; set; } = 12;

        public bool Active { get; set; }

        public virtual ICollection<Competence> Competences { get; set; } = new List<Competence>();

        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

        public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

        public virtual ICollection<RapportSeance> Rapports { get; set; } = new List<RapportSeance>();

        public virtual ICollection<FicheRevision> Fiches { get; set; } = new List<FicheRevision>();
    }
}
