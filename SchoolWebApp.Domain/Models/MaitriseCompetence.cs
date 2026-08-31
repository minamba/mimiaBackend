namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Vue aplatie d'une maîtrise, prête à être injectée dans le contexte de l'agent.
    /// </summary>
    public class MaitriseCompetence
    {
        public int CompetenceId { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? Domaine { get; set; }

        /// <summary>
        /// La matière de la compétence.
        ///
        /// Absente tant que le référentiel ne couvrait que les mathématiques :
        /// tout venait de la même matière, la préciser n'aurait rien dit. Avec
        /// sept référentiels, une ligne « Le vivant et son évolution » posée à
        /// côté d'une ligne « Nombres et calculs » n'est plus lisible sans elle.
        /// </summary>
        public string? MatiereCode { get; set; }

        public string? MatiereLibelle { get; set; }

        /// <summary>Niveau auquel la compétence appartient — révèle les lacunes de primaire.</summary>
        public string? NiveauCode { get; set; }

        public string? NiveauLibelle { get; set; }

        public double Score { get; set; }

        public double Confiance { get; set; }

        public DateTime DerniereEvaluation { get; set; }
    }
}
