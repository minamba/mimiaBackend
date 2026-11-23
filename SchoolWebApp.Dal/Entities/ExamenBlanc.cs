namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN EXAMEN BLANC DE L'ÉLÈVE — Camara, le 23/11/2026. Une date (et une heure facultative) ; ses matières sont
    /// dans <see cref="ExamenBlancMatiere"/>. Voir <c>SchoolWebApp.Domain.Models.ExamensBlancs</c>.
    /// </summary>
    public class ExamenBlanc
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>BREVET, BAC ou BAC_PRO : fixé à la création d'après la classe.</summary>
        public string Type { get; set; } = null!;

        public DateTime DateExamen { get; set; }

        public TimeSpan? HeureExamen { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual ICollection<ExamenBlancMatiere> Matieres { get; set; } = new List<ExamenBlancMatiere>();
    }

    /// <summary>Une matière d'un examen blanc. Une seule fois par examen blanc (index unique).</summary>
    public class ExamenBlancMatiere
    {
        public int Id { get; set; }

        public int ExamenBlancId { get; set; }

        public int MatiereId { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// ÉTAPE 2 (23/11/2026) — la dernière fois que les documents ont changé (ajout, retrait). Les notions sont à
        /// refaire dès que cette date dépasse <see cref="NotionsLe"/>.
        /// </summary>
        public DateTime? DocumentsModifiesLe { get; set; }

        /// <summary>Quand le professeur a tiré les notions (les dossiers) des documents ; null tant que jamais.</summary>
        public DateTime? NotionsLe { get; set; }

        /// <summary>
        /// ÉTAPE 3 (23/11/2026) — LE CONTRÔLE CACHÉ de cette matière : la préparation (séance, programme, pourcentage par
        /// notion, verdict du professeur) est celle d'un contrôle, sans en réécrire la mécanique. Il n'apparaît dans
        /// aucune liste de contrôles. Un entier sans clé étrangère : deux chemins de suppression en cascade depuis
        /// l'élève (par l'examen blanc et par le contrôle) seraient refusés par SQL Server ; le dépôt tient le lien.
        /// </summary>
        public int? ControleId { get; set; }

        /// <summary>Quand l'élève a dit d'accord au programme proposé au tableau ; null tant que non.</summary>
        public DateTime? ProgrammeValideLe { get; set; }

        public virtual ExamenBlanc? ExamenBlanc { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual ICollection<ExamenBlancDocument> Documents { get; set; } = new List<ExamenBlancDocument>();

        public virtual ICollection<ExamenBlancNotion> Notions { get; set; } = new List<ExamenBlancNotion>();
    }

    /// <summary>
    /// UN DOSSIER : une notion tirée des documents d'une matière d'examen blanc (étape 2, 23/11/2026), rattachée au
    /// référentiel de la classe quand elle y est (sinon en texte libre). Ses documents sont une liste d'identifiants
    /// (<see cref="DocumentIds"/>, « 12,15 ») : un document peut être dans plusieurs dossiers. Une table de liaison
    /// aurait fait deux chemins de suppression en cascade vers elle, que SQL Server refuse.
    /// </summary>
    public class ExamenBlancNotion
    {
        public int Id { get; set; }

        public int ExamenBlancMatiereId { get; set; }

        public int? CompetenceId { get; set; }

        public string Libelle { get; set; } = null!;

        public int Ordre { get; set; }

        public string DocumentIds { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }

        public virtual ExamenBlancMatiere? ExamenBlancMatiere { get; set; }

        public virtual Competence? Competence { get; set; }
    }

    /// <summary>
    /// Un document d'une matière d'examen blanc. AUCUN OCTET EN BASE : le fichier est sur le disque
    /// (<c>IStockageExamensBlancs</c>), effacé deux mois après l'examen blanc ; la ligne reste, marquée.
    /// </summary>
    public class ExamenBlancDocument
    {
        public int Id { get; set; }

        public int ExamenBlancMatiereId { get; set; }

        public string NomFichier { get; set; } = null!;

        public string TypeMime { get; set; } = null!;

        public long Taille { get; set; }

        public int NombrePages { get; set; }

        public string? Chemin { get; set; }

        public DateTime? FichierEffaceLe { get; set; }

        public string? Transcription { get; set; }

        /// <summary>Lectures ratées (étape 2) : à la troisième, le document est marqué illisible et on passe.</summary>
        public int Tentatives { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual ExamenBlancMatiere? ExamenBlancMatiere { get; set; }
    }
}
