namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// L'OFFRE D'UN LYCÉE, telle que l'import l'a lue dans les sources officielles
    /// (Onisep, Annuaire de l'éducation) — voir <c>EtablissementOrientation</c>
    /// côté base, et <c>ImportOrientationService</c> pour la lecture.
    /// </summary>
    public class OffreLycee
    {
        public string Uai { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public string? Statut { get; set; }

        public string? Commune { get; set; }

        public string? CodePostal { get; set; }

        public string? Departement { get; set; }

        public string? Academie { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? SiteWeb { get; set; }

        public bool VoieGenerale { get; set; }

        public bool VoieTechnologique { get; set; }

        public bool VoieProfessionnelle { get; set; }

        public bool SectionEuropeenne { get; set; }

        public bool SectionInternationale { get; set; }

        public bool SectionSportive { get; set; }

        public bool SectionArts { get; set; }

        public bool PostBac { get; set; }

        public bool Apprentissage { get; set; }

        /// <summary>Les spécialités de 1re générale, séparées par « / ».</summary>
        public string? Specialites { get; set; }

        /// <summary>Les enseignements optionnels et langues de 2de GT, séparés par « / ».</summary>
        public string? Options2de { get; set; }

        /// <summary>Les formations du lycée, une par ligne.</summary>
        public string? Formations { get; set; }

        public DateTime MisAJourLe { get; set; }
    }

    /// <summary>Un lycée et sa distance à l'élève, en kilomètres.</summary>
    public record LyceeProche(OffreLycee Lycee, double DistanceKm);

    /// <summary>Un passage de l'import, pour le journal et Admin › Modes.</summary>
    public class PassageImportOrientation
    {
        public DateTime Date { get; set; }

        public bool Reussi { get; set; }

        public string? Message { get; set; }

        public int Lycees { get; set; }

        /// <summary>La date de mise à jour annoncée par chaque source (jeu → date ISO).</summary>
        public Dictionary<string, string> Sources { get; set; } = new();

        public int DureeSecondes { get; set; }
    }
}
