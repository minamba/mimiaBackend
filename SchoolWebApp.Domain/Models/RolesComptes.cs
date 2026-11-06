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

    /// <summary>
    /// LES COURRIELS QU'UN RÔLE REÇOIT — Camara, le 06/11/2026 : « dans les droits, j'affecte les mails à un type
    /// d'utilisateur et tous ces utilisateurs recevront le mail ». Chaque compte qui porte un rôle coché reçoit le
    /// courriel. Stockés comme les onglets : des codes séparés par des virgules (<c>RoleUtilisateur.Courriels</c>).
    ///
    /// L'ADRESSE DE CAMARA À PART : minamba.camara@mimia.fr est une boîte, pas un compte parent ; un interrupteur
    /// de l'onglet « Rôles » (<see cref="ReglageCopieMinamba"/>) décide si elle reçoit les courriels de signalement.
    /// </summary>
    public static class CourrielsDeRole
    {
        /// <summary>Un signalement admin passé à « Traité » : « Le bug « … » vient d'être corrigé - Prêt à être testé ».</summary>
        public const string TestPret = "test_pret";

        public static readonly IReadOnlyList<CourrielConnu> Toutes =
        [
            new(TestPret, "Signalement admin traité : prêt à être testé"),
        ];

        /// <summary>L'interrupteur de minamba.camara@mimia.fr (table des réglages), éteint par défaut.</summary>
        public const string ReglageCopieMinamba = "COURRIEL_SIGNALEMENTS_MINAMBA";

        public const string AdresseMinamba = "minamba.camara@mimia.fr";

        /// <summary>Les codes connus, dans l'ordre du catalogue ; un code inconnu est ignoré.</summary>
        public static string[] Lire(string? valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return [];
            var lus = valeur.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return Toutes.Select(c => c.Code).Where(c => lus.Contains(c, StringComparer.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>La valeur à stocker : null quand rien n'est coché.</summary>
        public static string? Ecrire(IEnumerable<string>? codes)
        {
            var valeur = string.Join(",", Lire(string.Join(",", codes ?? [])));
            return valeur.Length == 0 ? null : valeur;
        }
    }

    /// <summary>Un courriel qu'un rôle peut recevoir, tel que l'onglet « Rôles » le propose.</summary>
    public record CourrielConnu(string Code, string Libelle);

    /// <summary>Un rôle, tel que l'onglet « Rôles » l'affiche.</summary>
    public record RoleCompte(
        int Id,
        string Nom,
        string? Code,
        string[] Onglets,
        int NombreComptes,
        bool OuvreAdministration,
        string[] Courriels);

    /// <summary>Le résultat d'une écriture de rôle : le rôle, ou la raison du refus.</summary>
    public record ResultatRole(RoleCompte? Role, string? Erreur, bool Introuvable = false);
}
