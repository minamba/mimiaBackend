namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN EXAMEN NATIONAL, VERSIONNÉ PAR SESSION — le brevet 2027, le bac 2027.
    ///
    /// Voulu par Camara le 14/09/2026, sur le plan « Brevet et baccalauréats » :
    /// jamais un simple « Brevet » ou « Bac », parce que les épreuves et les
    /// programmes changent d'une session à l'autre. Le brevet 2027 ne porte
    /// déjà plus sur le même périmètre que celui de 2026 (arrêté du 10 avril
    /// 2025, article 7).
    ///
    /// ÉCRIT PAR LE SEMEUR, JAMAIS PAR UNE MAIN : les épreuves se vérifient
    /// contre les textes officiels avant d'entrer ici — voir `ExamenSeeder`.
    /// </summary>
    public partial class Examen
    {
        public int Id { get; set; }

        /// <summary>« DNB_2027 ». Stable : c'est la clé du semeur.</summary>
        public string Code { get; set; } = null!;

        /// <summary>« Brevet ».</summary>
        public string Libelle { get; set; } = null!;

        /// <summary>Le titre de la section sur la page d'accueil : « Préparation au brevet ».</summary>
        public string TitreSection { get; set; } = null!;

        /// <summary>L'année de la session : 2027 pour les épreuves de juin 2027.</summary>
        public int Session { get; set; }

        /// <summary>
        /// Les codes de `NiveauScolaire` des élèves qui le passent, séparés par
        /// « ; ». La 3e prépa-métiers n'y est pas : elle relève d'une autre série.
        /// </summary>
        public string NiveauxCodes { get; set; } = null!;

        /// <summary>Les textes officiels d'où viennent les épreuves, et quand ils ont été lus.</summary>
        public string? Source { get; set; }

        public bool Actif { get; set; } = true;

        public DateTime DateCreation { get; set; }

        public virtual ICollection<EpreuveExamen> Epreuves { get; set; } = new List<EpreuveExamen>();
    }

    /// <summary>
    /// UNE ÉPREUVE D'UN EXAMEN — une carte sur la page d'accueil.
    ///
    /// SON PÉRIMÈTRE N'EST PAS RECOPIÉ : c'est une règle — telles matières, tels
    /// niveaux de programme —, appliquée aux compétences actives du référentiel
    /// au moment où on la lit. Une notion retirée du programme disparaît donc
    /// aussi de la carte, sans que personne n'y touche.
    /// </summary>
    public partial class EpreuveExamen
    {
        public int Id { get; set; }

        public int ExamenId { get; set; }

        /// <summary>« DNB_2027_MATHS ». Stable, et c'est lui qui voyage dans l'adresse du chat.</summary>
        public string Code { get; set; } = null!;

        /// <summary>« Brevet de mathématiques ».</summary>
        public string Libelle { get; set; } = null!;

        /// <summary>
        /// Les codes de `Matiere` évaluées, séparés par « ; ». Plusieurs pour les
        /// sciences : physique-chimie et SVT se préparent chacune avec leur
        /// professeur, pour une seule épreuve.
        /// </summary>
        public string MatieresCodes { get; set; } = null!;

        /// <summary>
        /// Les niveaux dont le programme tombe à l'épreuve, séparés par « ; ».
        /// « TROISIEME » seul, ou « CINQUIEME;QUATRIEME;TROISIEME » quand le
        /// texte officiel renvoie au cycle 4.
        /// </summary>
        public string NiveauxProgrammeCodes { get; set; } = null!;

        /// <summary>Ce que l'épreuve demande : durée, coefficient, parties.</summary>
        public string? Description { get; set; }

        /// <summary>Ce qu'il faut savoir en plus — une discipline pas encore couverte, par exemple.</summary>
        public string? Remarque { get; set; }

        public int Ordre { get; set; }

        /// <summary>
        /// Code d'une spécialité (`VoiesScolaires.SpecialitesGenerales`) : l'épreuve
        /// ne concerne que l'élève qui l'a cochée. Null : tous les élèves de l'examen.
        /// </summary>
        public string? SpecialiteRequise { get; set; }

        /// <summary>
        /// L'inverse : l'épreuve ne concerne que l'élève dont la famille a coché
        /// ses spécialités SANS celle-ci — les mathématiques anticipées de
        /// l'élève qui n'a pas pris la spécialité maths.
        /// </summary>
        public string? SpecialiteExclue { get; set; }

        /// <summary>
        /// Préfixes de `Competence.Domaine`, séparés par « ; » : seules ces
        /// notions tombent. Null : toutes celles des matières et niveaux.
        /// </summary>
        public string? DomainesInclus { get; set; }

        /// <summary>Préfixes de `Competence.Domaine` écartés du périmètre, séparés par « ; ».</summary>
        public string? DomainesExclus { get; set; }

        public virtual Examen? Examen { get; set; }
    }

    /// <summary>
    /// OÙ EN EST UN ÉLÈVE SUR UNE ÉPREUVE, DANS UNE MATIÈRE — le pendant du
    /// contrôle, pour les examens : séances de préparation, verdict et
    /// justification du professeur.
    ///
    /// PAR MATIÈRE, ET NON PAR ÉPREUVE : l'épreuve de sciences se prépare avec
    /// le professeur de physique-chimie ET celui de SVT, et chacun ne peut juger
    /// que ce qu'il a vu.
    ///
    /// LES POURCENTAGES NE SONT PAS ICI : ils se calculent à la lecture, depuis
    /// le moteur de maîtrise — comme pour les contrôles.
    /// </summary>
    public partial class PreparationEpreuve
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int EpreuveId { get; set; }

        public int MatiereId { get; set; }

        public int NombrePreparations { get; set; }

        public DateTime? DernierePreparationLe { get; set; }

        /// <summary>PRET, BIENTOT ou PAS_PRET — les constantes de `PretControle`.</summary>
        public string? PretVerdict { get; set; }

        public string? PretObservation { get; set; }

        public DateTime? PretLe { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual EpreuveExamen? Epreuve { get; set; }

        public virtual Matiere? Matiere { get; set; }
    }
}
