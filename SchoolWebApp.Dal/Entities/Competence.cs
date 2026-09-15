namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Nœud du graphe de compétences — l'actif central du produit.
    /// C'est lui qui permet, quand un 4e bloque sur les équations, de remonter
    /// jusqu'à la distributivité (5e) ou aux fractions (6e).
    /// </summary>
    public partial class Competence
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public int NiveauScolaireId { get; set; }

        /// <summary>Code stable, ex : MATH_6E_FRAC_SIMPLIFIER.</summary>
        public string? Code { get; set; }

        /// <summary>Référence au programme officiel (Éduscol) quand elle existe.</summary>
        public string? CodeEduscol { get; set; }

        /// <summary>Domaine au sein de la matière : "Nombres et calculs", "Géométrie"...</summary>
        public string? Domaine { get; set; }

        public string? Libelle { get; set; }

        public string? Description { get; set; }

        public int Ordre { get; set; }

        /// <summary>
        /// Depuis quand cette compétence précise est en vigueur dans le texte
        /// officiel — pas la date d'entrée en vigueur du PROGRAMME entier
        /// (voir <see cref="EcheanceReferentiel"/> pour ça), la date propre à
        /// CETTE ligne.
        ///
        /// NULLABLE, ET NON PEUPLÉE AUJOURD'HUI — délibérément. Les fichiers
        /// de seed (`SchoolWebApp.Dal/Seed/Referentiels`) documentent une date
        /// d'entrée en vigueur par COUPLE (matière, niveau), en commentaire —
        /// c'est ce que retrace `EcheanceReferentiel`. Ils ne portent pas,
        /// compétence par compétence, la date propre à chaque ligne : la
        /// retrouver demanderait de reconfronter chacune des quatre cents et
        /// quelques lignes à son texte officiel, notion par notion — le même
        /// travail de vérification qui a déjà pris trois passes et trouvé une
        /// erreur à chaque fois. On ne invente pas cette date : elle reste
        /// nulle tant qu'elle n'a pas été vérifiée pour de vrai.
        /// </summary>
        public DateTime? DateDebutValidite { get; set; }

        /// <summary>
        /// FAUX = LA NOTION A QUITTÉ LE PROGRAMME. Elle n'est plus proposée à
        /// personne — ni au professeur qui cherche quoi travailler, ni au
        /// programme complet d'une préparation de contrôle — mais SA LIGNE
        /// RESTE, et avec elle toutes les maîtrises que des élèves y ont
        /// gagnées.
        ///
        /// Décidé par Camara le 13/09/2026, en revenant sur sa consigne du 5
        /// septembre (« on n'enlève rien, on ajoute seulement ») : « une
        /// notion valable à N-1 mais plus à N, il faut l'enlever ; par contre
        /// pour les élèves de N-1 qui ont travaillé dessus, il ne faut pas
        /// supprimer les traces. La notion restera en tant qu'obsolète. Seuls
        /// les nouveaux élèves, qui ont la nouvelle réforme, ne l'auront pas. »
        ///
        /// C'est le semeur qui pose ce drapeau, jamais une main : une
        /// compétence dont le code a disparu du fichier de sa matière devient
        /// obsolète au démarrage suivant, et redevient active si le code
        /// revient. Supprimer la ligne est de toute façon impossible dès
        /// qu'un élève l'a travaillée — la clé vers `MaitriseEleve` est en
        /// `Restrict`, et c'est voulu.
        /// </summary>
        public bool Actif { get; set; } = true;

        /// <summary>Quand la notion a quitté le programme — posée avec <see cref="Actif"/> = faux.</summary>
        public DateTime? DateFinValidite { get; set; }

        /// <summary>
        /// Quand le semeur a créé cette ligne. NULLE pour tout ce qui existait
        /// avant le 13/09/2026 : on ne date pas rétroactivement ce qu'on n'a
        /// pas vu naître. Sert à l'écran d'administration pour dire
        /// « ajoutée le … » — ce qui a changé dans le programme, et quand.
        /// </summary>
        public DateTime? DateCreation { get; set; }

        /// <summary>
        /// La dernière fois que le semeur a changé quelque chose à cette ligne
        /// (libellé, domaine, ordre, niveau) — pas la dernière fois qu'il l'a
        /// relue sans rien y toucher.
        /// </summary>
        public DateTime? DateModification { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }

        /// <summary>Compétences qu'il faut maîtriser avant celle-ci.</summary>
        public virtual ICollection<CompetencePrerequis> Prerequis { get; set; } = new List<CompetencePrerequis>();

        /// <summary>Compétences qui dépendent de celle-ci.</summary>
        public virtual ICollection<CompetencePrerequis> Successeurs { get; set; } = new List<CompetencePrerequis>();

        public virtual ICollection<MaitriseEleve> Maitrises { get; set; } = new List<MaitriseEleve>();
    }
}
