namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une dictée, telle qu'elle s'affiche à l'élève.</summary>
    public class DicteeEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Titre { get; set; }

        public string? TexteDicte { get; set; }

        public string? Copie { get; set; }

        public string? Remarque { get; set; }

        /// <summary>« en_attente » ou « corrigee ».</summary>
        /// <summary>
        /// La séance où la dictée a eu lieu.
        ///
        /// SERT À REGROUPER LA LISTE PAR COURS, comme « Mes compréhensions
        /// orales ». Sans lui, les dictées défilaient à plat : rien ne disait
        /// que trois d'entre elles venaient du même après-midi de travail.
        /// </summary>
        public int ConversationId { get; set; }

        public string? Etat { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime DateMiseAJour { get; set; }

        public DateTime? DateConsultation { get; set; }

        /// <summary>Sa copie est archivée, mais la correction n'a pas encore eu lieu.</summary>
        public bool EnAttente => Etat == "en_attente";

        /// <summary>L'élève ne l'a jamais ouverte.</summary>
        public bool JamaisLue => DateConsultation is null;

        /// <summary>
        /// Le professeur l'a corrigée ou mise à jour depuis la dernière
        /// lecture. Distinct de <see cref="JamaisLue"/> : « à consulter » et
        /// « mise à jour » ne disent pas la même chose à l'élève.
        /// </summary>
        public bool MiseAJourNonLue =>
            DateConsultation is not null && DateConsultation < DateMiseAJour;

        // ------------------------------------------- l'élève, pour l'en-tête
        public string? ElevePrenom { get; set; }

        public string? EleveNom { get; set; }

        public string? EleveNiveau { get; set; }
    }
}
