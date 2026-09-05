namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une adresse bannie, telle que l'administration la lit.</summary>
    public class MailBanniVue
    {
        public int Id { get; set; }

        public string Mail { get; set; } = string.Empty;

        public string? Motif { get; set; }

        public DateTime DateCreation { get; set; }

        public string? BanniPar { get; set; }

        /// <summary>
        /// Le compte existe-t-il encore ?
        ///
        /// C'EST CE QUI REND LA LISTE LISIBLE. Une adresse dont le compte a
        /// disparu ne se retrouve nulle part ailleurs : c'est le seul endroit
        /// où elle existe encore, et le seul depuis lequel on peut lever le
        /// bannissement. Une adresse dont le compte tourne toujours, elle,
        /// désigne quelqu'un qu'on vient de mettre dehors sans l'effacer — et
        /// les deux situations n'appellent pas les mêmes gestes.
        /// </summary>
        public bool CompteExiste { get; set; }
    }
}
