namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// LA DATE OFFICIELLE D'UNE ÉPREUVE — Camara, le 25/11/2026 : les dates des sessions officielles (brevet, bac…)
    /// sur chaque carte, mises à jour par la veille, et validées par lui. Une ligne VALIDEE au plus par épreuve (celle
    /// que voient les familles) et une ligne PROPOSEE au plus (ce que la veille a lu, en attente de sa validation).
    /// Voir <c>SchoolWebApp.Domain.Models.CalendrierExamens</c>.
    /// </summary>
    public class DateEpreuveOfficielle
    {
        public int Id { get; set; }

        /// <summary>Le code de l'épreuve (<c>EpreuveExamen.Code</c>), sans clé étrangère : l'épreuve est re-semée.</summary>
        public string EpreuveCode { get; set; } = null!;

        /// <summary>VALIDEE ou PROPOSEE.</summary>
        public string Etat { get; set; } = null!;

        /// <summary>JOUR, UN_DES_DEUX (le 16 OU le 17, selon la convocation) ou PERIODE (le grand oral).</summary>
        public string Nature { get; set; } = null!;

        public DateTime DateEpreuve { get; set; }

        /// <summary>Le second jour possible (UN_DES_DEUX) ou la fin de la période (PERIODE).</summary>
        public DateTime? DateFin { get; set; }

        public TimeSpan? HeureDebut { get; set; }

        public TimeSpan? HeureFin { get; set; }

        /// <summary>Le jour de la session de remplacement, s'il est connu.</summary>
        public DateTime? RemplacementLe { get; set; }

        public string SourceLibelle { get; set; } = null!;

        public string SourceUrl { get; set; } = null!;

        /// <summary>L'annexe qui donne les horaires, quand elle existe.</summary>
        public string? AnnexeUrl { get; set; }

        /// <summary>La phrase du texte officiel d'où vient la date : ce que Camara relit avant de valider.</summary>
        public string? Extrait { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? ProposeLe { get; set; }

        public DateTime? ValideLe { get; set; }
    }
}
