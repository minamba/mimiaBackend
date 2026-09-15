namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une période de vacances scolaires, pour une zone donnée.</summary>
    public class PeriodeVacances
    {
        public int Id { get; set; }

        public string? Zone { get; set; }

        public string? AnneeScolaire { get; set; }

        public string? Libelle { get; set; }

        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }
    }
}
