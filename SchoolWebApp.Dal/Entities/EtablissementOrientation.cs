namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// L'OFFRE D'UN LYCÉE, LUE DANS LES SOURCES OFFICIELLES — pour la conseillère
    /// d'orientation (Camara, le 11/10/2026 : « en fonction du lycée, les
    /// parcours et les choix peuvent être différents »).
    ///
    /// UNE LIGNE PAR LYCÉE (code UAI), réécrite par l'import hebdomadaire
    /// (<c>ImportOrientationService</c>) depuis :
    ///   - l'Onisep (opendata.onisep.fr, licence ODbL) : formations du lycée,
    ///     spécialités de 1re générale, enseignements optionnels et langues de 2de ;
    ///   - l'Annuaire de l'éducation (data.education.gouv.fr, Licence Ouverte) :
    ///     voies, sections européenne, internationale, sportive, post-bac.
    ///
    /// LES LISTES SONT DU TEXTE, PAS DES TABLES : elles ne servent qu'à être lues
    /// par la conseillère, telles que la source les écrit. Une table par
    /// spécialité ou par formation n'apporterait rien qu'une requête de plus.
    /// </summary>
    public class EtablissementOrientation
    {
        /// <summary>Le code UAI (ex. 0772296C) : la clé commune à toutes les sources.</summary>
        public string Uai { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        /// <summary>« public », « privé sous contrat », « privé hors contrat ».</summary>
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

        /// <summary>Les spécialités de 1re générale, séparées par « / » (Onisep).</summary>
        public string? Specialites { get; set; }

        /// <summary>Les enseignements optionnels et langues de 2de GT, séparés par « / » (Onisep).</summary>
        public string? Options2de { get; set; }

        /// <summary>Les formations du lycée, une par ligne (bac général, bacs techno, bacs pro, CAP…).</summary>
        public string? Formations { get; set; }

        public DateTime MisAJourLe { get; set; }
    }
}
