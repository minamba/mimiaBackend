namespace SchoolWebApp.Domain.Models
{
    public class Matiere
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? AgentSlug { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        /// <summary>Ce que la matiÃ¨re promet Ã  l'enfant, en une phrase.</summary>
        public string? Promesse { get; set; }

        public int Ordre { get; set; }

        /// <summary>Bornes incluses de l'Ordre du niveau où la matière existe.</summary>
        public int NiveauOrdreMin { get; set; } = 1;

        /// <inheritdoc cref="NiveauOrdreMin"/>
        public int NiveauOrdreMax { get; set; } = 12;

        public bool Active { get; set; }
    }
}
