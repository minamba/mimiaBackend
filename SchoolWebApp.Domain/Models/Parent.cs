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

        /// <summary>
        /// Le droit d'administrer, accordé par le super-administrateur.
        ///
        /// PORTÉ JUSQU'AU DOMAINE DEPUIS LE 16/09/2026, pour la facturation :
        /// « toutes les personnes qui ont le rôle d'administrateur ne se font
        /// pas facturer les heures et les forfaits » (Camara). La caisse lit
        /// ce drapeau en base, et non le jeton : un droit accordé vaut à
        /// l'instant, sans attendre que le parent se reconnecte.
        /// </summary>
        public bool EstAdministrateur { get; set; }

        /// <summary>
        /// Les sections du tableau de bord qui lui sont ouvertes.
        ///
        /// Vide pour un administrateur qu'on vient de nommer : le
        /// super-administrateur coche ce qu'il veut lui ouvrir.
        /// </summary>
        public string[] OngletsAdmin { get; set; } = [];

        /// <summary>Ce parent peut-il enregistrer un enfant de plus ?</summary>
        public bool PeutAjouterEnfant { get; set; } = true;
    }
}
