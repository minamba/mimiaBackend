namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES RÔLES DES COMPTES PARENTS — voulus par Camara le 17/10/2026. Un rôle
    /// porte des droits : ses onglets d'administration (clés de
    /// <see cref="OngletsAdmin"/>). Voir l'entité <c>RoleUtilisateur</c>.
    /// </summary>
    public static class RolesComptes
    {
        public const string Utilisateur = "utilisateur";
        public const string Administrateur = "administrateur";
        public const string SuperAdmin = "super_admin";

        /// <summary>Le nom d'un rôle : court, comme un intitulé de fonction.</summary>
        public const int NomMax = 60;

        /// <summary>
        /// Le rôle ouvre-t-il l'administration ? « Administrateur » toujours (même
        /// sans onglet : c'est l'état « tout décoché » de la nomination) ; un rôle
        /// créé, dès qu'il a au moins un onglet ; Utilisateur et Super-admin,
        /// jamais par ce chemin (le super-admin vient de la configuration).
        /// </summary>
        public static bool OuvreAdministration(string? code, string? onglets) => code switch
        {
            Administrateur => true,
            Utilisateur or SuperAdmin => false,
            _ => OngletsAdmin.Lire(onglets).Length > 0,
        };
    }

    /// <summary>Un rôle, tel que l'onglet « Rôles » l'affiche.</summary>
    public record RoleCompte(
        int Id,
        string Nom,
        string? Code,
        string[] Onglets,
        int NombreComptes,
        bool OuvreAdministration);

    /// <summary>Le résultat d'une écriture de rôle : le rôle, ou la raison du refus.</summary>
    public record ResultatRole(RoleCompte? Role, string? Erreur, bool Introuvable = false);
}
