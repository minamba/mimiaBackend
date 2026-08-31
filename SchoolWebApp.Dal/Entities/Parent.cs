namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Compte racine côté métier. Miroir local de l'utilisateur ASP.NET Identity
    /// qui vit dans SCHOOL_Identity_Database : on ne duplique pas les credentials,
    /// seulement le lien (IdentityUserId) et ce dont l'API a besoin.
    /// </summary>
    public partial class Parent
    {
        public int Id { get; set; }

        /// <summary>
        /// Clé de l'utilisateur dans le serveur d'identité (claim `sub` du JWT).
        /// Non-nullable : un parent sans identité est une donnée corrompue.
        /// </summary>
        public string IdentityUserId { get; set; } = string.Empty;

        public string? Prenom { get; set; }

        public string? Nom { get; set; }

        public string? Mail { get; set; }

        /// <summary>
        /// Le client correspondant chez Stripe (`cus_…`).
        ///
        /// POURQUOI SUR LE PARENT ET NON SUR L'ABONNEMENT
        /// ---------------------------------------------
        /// Un parent résilie, puis revient six mois plus tard : c'est un
        /// nouvel abonnement, mais le même client — mêmes factures, même
        /// carte enregistrée, même historique. Rattacher le client à
        /// l'abonnement en créerait un deuxième chez Stripe, et le parent
        /// perdrait ses moyens de paiement en même temps que ses factures.
        ///
        /// C'est aussi ce qui rend les webhooks exploitables : un événement
        /// Stripe ne connaît pas nos identifiants, il ne cite que le client
        /// et l'abonnement. Sans cette colonne, un paiement qui arrive ne
        /// peut être rattaché à personne.
        /// </summary>
        public string? StripeClientId { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual ICollection<Eleve> Eleves { get; set; } = new List<Eleve>();

        public virtual ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();
    }
}
