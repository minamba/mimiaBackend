namespace SchoolWebApp.Domain.Models
{
    public class NiveauScolaire
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? Cycle { get; set; }

        public int Ordre { get; set; }
    }
}
