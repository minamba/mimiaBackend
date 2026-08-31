using System.ComponentModel.DataAnnotations;

namespace SchoolWebApp.IdentityServer.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'adresse email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Rester connecté")]
        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Fournisseurs externes réellement enregistrés. Vide si les identifiants
        /// Google ne sont pas configurés — auquel cas le bouton ne doit pas
        /// s'afficher : le cliquer provoquerait une erreur 500.
        /// </summary>
        public IList<string> FournisseursExternes { get; set; } = new List<string>();
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Nom")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [StringLength(100, MinimumLength = 10,
            ErrorMessage = "Le mot de passe doit contenir au moins 10 caractères.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare(nameof(Password), ErrorMessage = "Les deux mots de passe ne correspondent pas.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Le compte est celui d'un adulte responsable : obligatoire pour le RGPD
        /// (consentement parental pour les profils enfants de moins de 15 ans).
        /// </summary>
        [Display(Name = "Je certifie être le parent ou le responsable légal des élèves que j'inscrirai")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Cette confirmation est obligatoire.")]
        public bool ConfirmeResponsabiliteParentale { get; set; }

        public string? ReturnUrl { get; set; }

        public IList<string> FournisseursExternes { get; set; } = new List<string>();
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "L'adresse email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [Display(Name = "Adresse email")]
        public string? Email { get; set; }

        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Vrai après traitement, que le compte existe ou non : la vue affiche
        /// la même confirmation dans les deux cas.
        /// </summary>
        public bool Envoye { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string? Jeton { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [StringLength(100, MinimumLength = 10,
            ErrorMessage = "Le mot de passe doit contenir au moins 10 caractères.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare(nameof(Password), ErrorMessage = "Les deux mots de passe ne correspondent pas.")]
        public string? ConfirmPassword { get; set; }

        public string? ReturnUrl { get; set; }

        public bool Reussi { get; set; }
    }

    /// <summary>
    /// L'écran « vérifiez votre boîte », après une inscription ou une tentative
    /// de connexion sur un compte pas encore confirmé.
    /// </summary>
    public class ConfirmationViewModel
    {
        /// <summary>
        /// L'adresse où le lien vient de partir. Affichée pour que le parent
        /// repère tout de suite une faute de frappe — c'est le seul moment où
        /// il peut encore la corriger sans écrire au support.
        /// </summary>
        public string? Mail { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
