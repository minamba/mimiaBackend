namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LA VÉRIFICATION DES CARTES D'EXAMEN — voulue par Camara le 14/09/2026 :
    /// une cinquantaine de cartes, impossibles à ouvrir une par une.
    ///
    /// Une carte ne plante jamais : une partie retenue mal orthographiée la VIDE,
    /// une partie exclue mal orthographiée ne retire RIEN, une matière fermée la
    /// fait disparaître. Tout cela en silence. Ce relevé le dit, avec les mêmes
    /// règles que celles qui construisent la carte de l'élève.
    /// </summary>
    public class VerificationExamen
    {
        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public int Session { get; set; }

        /// <summary>Ce qui cloche au niveau de l'examen lui-même : une classe inconnue.</summary>
        public List<string> Problemes { get; set; } = [];

        public List<VerificationEpreuve> Epreuves { get; set; } = [];

        /// <summary>Le nombre total de problèmes, examen et épreuves compris.</summary>
        public int NombreProblemes => Problemes.Count + Epreuves.Sum(e => e.Problemes.Count);
    }

    public class VerificationEpreuve
    {
        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        public List<string> Problemes { get; set; } = [];

        public List<VerificationMatiere> Matieres { get; set; } = [];
    }

    public class VerificationMatiere
    {
        public string Code { get; set; } = null!;

        public string? Libelle { get; set; }

        public bool Active { get; set; }

        /// <summary>Les notions actives de la matière aux niveaux de l'épreuve, avant les parties retenues ou exclues.</summary>
        public int NotionsDuProgramme { get; set; }

        /// <summary>Celles qui restent sur la carte : c'est ce que l'élève voit.</summary>
        public int NotionsRetenues { get; set; }
    }
}
