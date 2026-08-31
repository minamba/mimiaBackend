namespace SchoolWebApp.Api.Services.Paiement
{
    /// <summary>
    /// Les comptes qui ne passent JAMAIS par le paiement.
    ///
    /// Ce sont les comptes de l'exploitant : le sien et celui de démonstration.
    /// Ils souscrivent, changent de formule, consomment leurs heures et voient
    /// leur décompte comme n'importe quel parent — mais aucune carte n'est
    /// jamais demandée, aucun prélèvement n'est jamais déclenché.
    ///
    /// POURQUOI UNE LISTE ET NON « LES ADMINISTRATEURS »
    /// -------------------------------------------------
    /// Le compte de démonstration n'est pas administrateur, et il ne doit pas
    /// l'être : tout son intérêt est de voir le produit avec les yeux d'un
    /// parent ordinaire. Le rattacher au rôle d'administrateur pour lui offrir
    /// l'exemption lui donnerait au passage l'accès au tableau de bord, aux
    /// comptes des autres et aux interrupteurs du produit.
    ///
    /// POURQUOI MAINTENANT, AVANT MÊME QUE STRIPE EXISTE
    /// -------------------------------------------------
    /// Aujourd'hui rien n'est facturé, donc la règle est vraie par accident.
    /// Le jour où le tunnel de paiement arrive, elle doit être vraie par
    /// construction — sinon elle sera oubliée, et l'exploitant se paiera
    /// lui-même sur sa propre carte.
    /// </summary>
    public class ExemptionFacturation
    {
        public const string Section = "Facturation";

        /// <summary>Adresses exemptées, comparées sans tenir compte de la casse.</summary>
        public string[] ComptesExemptes { get; set; } = [];

        public bool EstExempte(string? mail) =>
            !string.IsNullOrWhiteSpace(mail)
            && ComptesExemptes.Any(c =>
                string.Equals(c?.Trim(), mail.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
