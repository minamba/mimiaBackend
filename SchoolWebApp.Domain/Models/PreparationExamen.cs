namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LA PRÉPARATION D'UN ÉLÈVE À SON EXAMEN — la section « Préparation au
    /// brevet » de sa page d'accueil.
    ///
    /// Voulu par Camara le 14/09/2026 : une barre qui est la moyenne des
    /// épreuves, puis une carte par épreuve, puis, en ouvrant la carte, toutes
    /// les notions à maîtriser avec leur barre, et le statut et la justification
    /// du professeur — « le même principe que les contrôles ».
    ///
    /// RIEN N'EST STOCKÉ, TOUT EST CALCULÉ : les pourcentages viennent du moteur
    /// de maîtrise au moment de la lecture, comme pour un contrôle. Une notion
    /// travaillée en cours normal fait donc monter la barre du brevet : c'est la
    /// même compétence.
    /// </summary>
    public class PreparationExamenEleve
    {
        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public string TitreSection { get; set; } = null!;

        public int Session { get; set; }

        /// <summary>La moyenne des épreuves — jamais une pondération à part.</summary>
        public int Pourcent { get; set; }

        /// <summary>
        /// Des épreuves de l'examen dépendent des spécialités, et la famille ne
        /// les a pas TOUTES cochées : l'écran le dit, plutôt que de laisser
        /// croire que le bac se réduit à ce qu'il affiche.
        ///
        /// À MOITIÉ RENSEIGNÉ COMPTE AUTANT QUE PAS DU TOUT — Camara, le
        /// 20/09/2026 : « c'est pas normal que dans sa préparation au bac
        /// général il y ait que la philosophie et sa spécialité, où sont les
        /// autres matières ? » Une seule des deux spécialités de terminale
        /// était cochée ; l'alerte ne se déclenchait qu'à zéro, l'écran se
        /// taisait, et le bac paraissait amputé sans dire pourquoi.
        /// </summary>
        public bool SpecialitesARenseigner { get; set; }

        /// <summary>
        /// Combien de spécialités manquent à l'appel. Porté jusqu'à l'écran
        /// pour que le message distingue « aucune n'est cochée » de « il en
        /// manque une » : les deux n'appellent pas la même phrase, et le
        /// second cas est le plus déroutant des deux.
        /// </summary>
        public int SpecialitesManquantes { get; set; }

        /// <summary>
        /// CE QUI COMPTE AU BAC SANS ÉPREUVE FINALE, dit à l'élève — l'espagnol en
        /// LV2 notamment. Voulu par Camara le 14/09/2026 : un élève de terminale
        /// qui a l'espagnol en LV2 se demande s'il a « le bac d'espagnol ». Il ne
        /// l'a pas : la LVB compte en contrôle continu (arrêtés du 16/07/2018
        /// consolidés, général et technologique). Seule la spécialité LLCER a une
        /// épreuve terminale.
        /// </summary>
        public List<string> NotesControleContinu { get; set; } = [];

        public List<EpreuvePreparation> Epreuves { get; set; } = [];
    }

    /// <summary>Une épreuve, avec la préparation de chacune de ses matières.</summary>
    public class EpreuvePreparation
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public string? Description { get; set; }

        public string? Remarque { get; set; }

        public int Ordre { get; set; }

        public string ExamenCode { get; set; } = null!;

        public string ExamenLibelle { get; set; } = null!;

        public int Session { get; set; }

        public string[] NiveauxProgramme { get; set; } = [];

        /// <summary>La moyenne de ses matières.</summary>
        public int Pourcent { get; set; }

        public List<MatiereEpreuvePreparation> Matieres { get; set; } = [];
    }

    /// <summary>
    /// Une matière d'une épreuve : ses notions, sa barre, et le verdict de SON
    /// professeur. Réutilise la forme des contrôles — même écran, même règle de
    /// statut, mêmes seuils.
    /// </summary>
    public class MatiereEpreuvePreparation
    {
        public int MatiereId { get; set; }

        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        public int NombrePreparations { get; set; }

        public DateTime? DernierePreparationLe { get; set; }

        /// <summary>
        /// Une séance de préparation ouverte, OU une notion du programme déjà
        /// mesurée — en cours normal compris. C'est le fait qui borne le verdict,
        /// voir `PretControle.VerdictPlafonne`.
        /// </summary>
        public bool RevisionCommencee { get; set; }

        public PreparationControle Preparation { get; set; } = new();
    }

    /// <summary>Ce qu'il faut savoir d'une épreuve pour ouvrir ou conduire une séance, sans les notions.</summary>
    public class EpreuveExamenInfo
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public string ExamenCode { get; set; } = null!;

        public string ExamenLibelle { get; set; } = null!;

        public int Session { get; set; }

        public string[] Matieres { get; set; } = [];

        public string[] NiveauxProgramme { get; set; } = [];

        public string? Description { get; set; }

        public string? Remarque { get; set; }
    }
}
