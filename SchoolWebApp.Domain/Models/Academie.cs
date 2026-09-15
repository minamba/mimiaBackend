namespace SchoolWebApp.Domain.Models
{
    public class Academie
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        /// <summary>A, B, C, ou le calendrier propre à la Corse et à chaque académie d'outre-mer.</summary>
        public string? Zone { get; set; }
    }
}
