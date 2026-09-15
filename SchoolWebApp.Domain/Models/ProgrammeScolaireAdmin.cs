namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Le programme scolaire tel que l'administration le lit : classe par
    /// classe, matière par matière, notion par notion — avec, pour chacune,
    /// ce qui lui est arrivé et quand, et l'échéance officielle qui la
    /// concerne. Voulu par Camara le 13/09/2026 : « tout le programme séparé
    /// en classe, comme dans le référentiel, avec le statut de chaque notion
    /// et la date d'échéance ».
    /// </summary>
    public class ProgrammeScolaireAdmin
    {
        /// <summary>« 2026-2027 » — calculée, jamais écrite en dur. Voir <see cref="AnneeScolaire"/>.</summary>
        public string AnneeScolaire { get; set; } = null!;

        /// <summary>« Programmes officiels 2026-2027 » — le titre de l'écran.</summary>
        public string Titre { get; set; } = null!;

        /// <summary>Les échéances dépassées non traitées : ce qui devrait déjà avoir été vérifié.</summary>
        public int EcheancesEnRetard { get; set; }

        public List<ClasseProgramme> Classes { get; set; } = new();

        /// <summary>Les surveillances permanentes, sans classe — la page-carrefour du ministère.</summary>
        public List<EcheanceReferentielDetail> Sentinelles { get; set; } = new();
    }

    public class ClasseProgramme
    {
        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public int Ordre { get; set; }

        public List<MatiereProgrammeAdmin> Matieres { get; set; } = new();
    }

    public class MatiereProgrammeAdmin
    {
        public int MatiereId { get; set; }

        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public string? Couleur { get; set; }

        public List<NotionProgramme> Notions { get; set; } = new();

        /// <summary>Les échéances officielles rangées sous cette matière pour cette classe.</summary>
        public List<EcheanceReferentielDetail> Echeances { get; set; } = new();
    }

    /// <summary>Une notion du référentiel, avec son histoire — jamais son score.</summary>
    public class NotionProgramme
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Domaine { get; set; }

        public string? Libelle { get; set; }

        public int Ordre { get; set; }

        /// <summary>Faux = retirée du programme, conservée pour les élèves qui l'ont travaillée.</summary>
        public bool Actif { get; set; }

        public DateTime? DateDebutValidite { get; set; }

        public DateTime? DateFinValidite { get; set; }

        public DateTime? DateCreation { get; set; }

        public DateTime? DateModification { get; set; }
    }
}
