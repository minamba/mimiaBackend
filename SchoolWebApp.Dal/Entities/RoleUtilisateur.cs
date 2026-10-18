namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN RÔLE DE COMPTE PARENT — voulu par Camara le 17/10/2026 : « je veux
    /// pouvoir créer des rôles et les attribuer aux utilisateurs ».
    ///
    /// LE RÔLE PORTE LES DROITS : ses onglets d'administration (les clés de
    /// `OngletsAdmin`) sont ceux qu'ouvre le rôle à qui le porte. Un rôle sans
    /// onglet n'ouvre pas l'administration — sauf « Administrateur », qui
    /// l'ouvre toujours, même vide (« tout décoché » à la nomination).
    ///
    /// TROIS RÔLES SYSTÈME (`Code`), semés au démarrage : Utilisateur (le rôle
    /// par défaut, sans droit), Administrateur, Super-admin. Ce dernier n'est
    /// qu'AFFICHÉ : le super-administrateur vient de la configuration du
    /// serveur, jamais de la base, et ce rôle ne s'attribue pas.
    ///
    /// Pas de rôle pour les enfants : c'est une notion des seuls comptes parents.
    /// </summary>
    public partial class RoleUtilisateur
    {
        public int Id { get; set; }

        public string Nom { get; set; } = null!;

        /// <summary>« utilisateur », « administrateur », « super_admin » ; null pour un rôle créé.</summary>
        public string? Code { get; set; }

        /// <summary>Les onglets ouverts, séparés par des virgules (comme `Parent.OngletsAdmin`).</summary>
        public string? Onglets { get; set; }

        public int Ordre { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
