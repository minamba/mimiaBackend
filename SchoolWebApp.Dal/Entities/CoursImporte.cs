namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN COURS IMPORTÉ PAR L'ÉLÈVE POUR EN FAIRE DES FICHES DE RÉVISION —
    /// Camara, le 14/10/2026. Voir `FichesRevisionEleve` (Domain).
    ///
    /// SA VIE : brouillon (l'enfant dépose ses pages) → à lire (il a validé ;
    /// la tâche de fond lit le cours et y trouve les notions) → notions
    /// (l'enfant choisit ce qu'il veut réviser) → à générer → prêt. Ou échec,
    /// avec une remarque écrite pour l'enfant.
    ///
    /// LE COURS N'EST PAS GARDÉ (Camara, 15/10/2026) : ses pages sont
    /// supprimées dès qu'il est prêt ou en échec, et au plus tard
    /// `HeuresChoixDesNotions` après sa lecture si l'enfant n'a pas choisi.
    /// Il ne reste que son titre et ses notions, pour ranger les fiches.
    /// </summary>
    public partial class CoursImporte
    {
        public int Id { get; set; }

        /// <summary>L'élève propriétaire — null pour un cours de parent (espace parent, 16/10/2026).</summary>
        public int? EleveId { get; set; }

        /// <summary>Le parent propriétaire — exactement l'un des deux (contrainte en base).</summary>
        public int? ParentId { get; set; }

        /// <summary>La matière d'un cours d'élève — null pour un cours de parent.</summary>
        public int? MatiereId { get; set; }

        /// <summary>La catégorie, créée par le parent, d'un cours de parent (null : « sans catégorie »).</summary>
        public int? CategorieId { get; set; }

        /// <summary>Le titre du cours, lu sur ses pages. Null tant qu'il n'est pas lu.</summary>
        public string? Titre { get; set; }

        /// <summary>brouillon, a_lire, notions, a_generer, pret, echec — voir `FichesRevisionEleve`.</summary>
        public string Etat { get; set; } = null!;

        /// <summary>Ce qui ne va pas, écrit pour l'enfant (photo floue, page coupée…).</summary>
        public string? Remarque { get; set; }

        /// <summary>Les notions trouvées, en JSON : [{ titre, resume }].</summary>
        public string? Notions { get; set; }

        /// <summary>Le type de fiche de la dernière demande.</summary>
        public string Type { get; set; } = null!;

        /// <summary>Les notions demandées (rangs, à partir de 1), en JSON. Vide pour un type qui couvre tout le cours.</summary>
        public string? NotionsDemandees { get; set; }

        /// <summary>Quand la dernière demande de fiches a été faite : les fiches créées depuis lui répondent.</summary>
        public DateTime? DemandeLe { get; set; }

        /// <summary>Les passages de génération ratés pour la demande en cours : au-delà de trois, on s'arrête.</summary>
        public int Tentatives { get; set; }

        /// <summary>Quand l'enfant a lancé la lecture : c'est ce qui compte dans la limite du mois.</summary>
        public DateTime? EnvoyeLe { get; set; }

        /// <summary>
        /// Ce que le cours compte dans le quota : 1, ou 2 au-delà de dix pages
        /// (16/10/2026). Fixé à l'envoi, quand les pages sont encore là : elles
        /// partent avant le décompte.
        /// </summary>
        public int Poids { get; set; } = 1;

        /// <summary>
        /// COMPLÉTER UN COURS — Camara, le 16/10/2026. Renseigné, ce cours est un
        /// COMPLÉMENT de celui-ci : de nouvelles pages du même chapitre. Il vit sa
        /// vie d'import (brouillon, lecture, notions, rédaction, quota), mais il
        /// n'a pas de carte à lui : ses fiches vont dans le cours complété.
        /// </summary>
        public int? ComplementDe { get; set; }

        /// <summary>Quand les pages ont été supprimées : à la fin du traitement.</summary>
        public DateTime? PagesEffaceesLe { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime DateModification { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Parent? Parent { get; set; }

        public virtual CategorieFichesParent? Categorie { get; set; }

        public virtual ICollection<CoursImportePage> Pages { get; set; } = new List<CoursImportePage>();

        public virtual ICollection<FicheDeCours> Fiches { get; set; } = new List<FicheDeCours>();
    }

    /// <summary>
    /// Une pièce du cours : une photo, une image ou un PDF. Les octets sont en
    /// base, comme les pièces jointes du chat — voir `PieceJointe`.
    /// </summary>
    public partial class CoursImportePage
    {
        public int Id { get; set; }

        public int CoursId { get; set; }

        /// <summary>L'ordre de dépôt : c'est l'ordre des pages du cours.</summary>
        public int Ordre { get; set; }

        public string? NomFichier { get; set; }

        public string TypeMime { get; set; } = null!;

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        /// <summary>Les octets de la page ; la ligne entière part avec le cours fini.</summary>
        public byte[]? Donnees { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual CoursImporte? Cours { get; set; }
    }

    /// <summary>
    /// UNE FICHE DE RÉVISION GÉNÉRÉE DEPUIS LE COURS DE L'ÉLÈVE. Distincte de
    /// `FicheRevisionEleve`, les fiches que rédigent les professeurs en séance :
    /// deux origines, deux tables, deux espaces.
    ///
    /// Le contenu est une suite de BRIQUES en JSON, vérifiée avant d'être
    /// écrite (`ContenuFiche`) — jamais du HTML.
    /// </summary>
    public partial class FicheDeCours
    {
        public int Id { get; set; }

        public int CoursId { get; set; }

        /// <summary>Recopié du cours : la liste par élève ne passe pas par les cours.</summary>
        public int? EleveId { get; set; }

        /// <summary>Le parent propriétaire (espace parent, 16/10/2026), recopié du cours.</summary>
        public int? ParentId { get; set; }

        public int? MatiereId { get; set; }

        public string Type { get; set; } = null!;

        public string Titre { get; set; } = null!;

        /// <summary>Le rang de la notion (à partir de 1), null pour une fiche du cours entier.</summary>
        public int? NotionRang { get; set; }

        public int? NotionTotal { get; set; }

        public string Contenu { get; set; } = null!;

        /// <summary>
        /// Les flash cards et le quiz de la fiche (16/10/2026), en JSON : écrits au
        /// premier « Me tester », gardés ensuite. Null tant que l'élève ne s'est
        /// pas testé.
        /// </summary>
        public string? Quiz { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual CoursImporte? Cours { get; set; }
    }
}
