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

        /// <summary>
        /// La dernière venue du PARENT sur le site.
        ///
        /// À ne pas confondre avec DerniereActivite, qui vit sur l'élève :
        /// celle-là dit quand l'ENFANT a travaillé. Les deux ensemble racontent
        /// des choses opposées — un enfant assidu dont le parent n'ouvre plus
        /// rien depuis deux mois est un désabonnement qui se prépare, et rien
        /// ne le montrait.
        ///
        /// Nulle tant que le parent n'est pas revenu depuis l'ajout de la
        /// colonne : on n'invente pas un passé qu'on n'a pas mesuré.
        /// </summary>
        public DateTime? DerniereConnexion { get; set; }

        /// <summary>
        /// Ce parent a-t-il reçu le droit d administrer ?
        ///
        /// Accordé et retiré par le SUPER-administrateur seul. Celui-ci n est
        /// pas ici : il vient de la configuration, et reste donc hors de portée
        /// de l interface — une fausse manœuvre ne peut pas fermer la maison.
        /// </summary>
        public bool EstAdministrateur { get; set; }

        public virtual ICollection<Eleve> Eleves { get; set; } = new List<Eleve>();

        public virtual ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();
    }
}
