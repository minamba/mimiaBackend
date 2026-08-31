using Microsoft.AspNetCore.Identity;

namespace SchoolWebApp.IdentityServer.Data
{
    /// <summary>
    /// Compte racine de la plateforme : un parent (ou un adulte responsable).
    /// Les profils enfants ne vivent pas ici — ils sont dans SCHOOL_Database,
    /// rattachés à cet utilisateur par IdentityUserId.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string? Prenom { get; set; }

        public string? Nom { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Renseigné quand le compte a été créé via un fournisseur externe (Google).
        /// Null pour une inscription par email/mot de passe.
        /// </summary>
        public string? FournisseurExterne { get; set; }
    }
}
