namespace SchoolWebApp.Domain.Models
{
    public class Parent
    {
        public int Id { get; set; }

        public string IdentityUserId { get; set; } = string.Empty;

        public string? Prenom { get; set; }

        public string? Nom { get; set; }

        public string? Mail { get; set; }

        /// <summary>
        /// Le client correspondant chez Stripe (`cus_…`), null tant qu'il n'a
        /// jamais atteint la caisse.
        /// </summary>
        public string? StripeClientId { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
