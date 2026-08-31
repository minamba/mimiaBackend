namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les interrupteurs du produit, lus et actionnés depuis l'administration.
    ///
    /// Volontairement minuscule : ce sont des drapeaux d'exploitation, pas des
    /// données métier. Le jour où il y en aura vingt, l'interface changera ;
    /// ce contrat, lui, n'aura pas de raison de bouger.
    /// </summary>
    public interface IReglageRepository
    {
        /// <summary>Tous les réglages posés, sous la forme clé → valeur.</summary>
        Task<IReadOnlyDictionary<string, string>> GetTousAsync(CancellationToken ct = default);

        /// <summary>
        /// La valeur d'un drapeau, ou <paramref name="parDefaut"/> s'il n'a
        /// jamais été posé.
        /// </summary>
        Task<bool> EstActifAsync(string cle, bool parDefaut = false, CancellationToken ct = default);

        /// <summary>Pose ou remplace un drapeau.</summary>
        Task DefinirAsync(string cle, bool actif, CancellationToken ct = default);

        /// <summary>
        /// La valeur brute d'une clé, ou null si elle n'a jamais été posée.
        ///
        /// Cette table sert d'abord aux drapeaux, mais elle stocke des CHAÎNES.
        /// Ces deux méthodes ouvrent ce qui existait déjà : de quoi retenir une
        /// date entre deux démarrages sans créer une table pour une ligne.
        /// </summary>
        Task<string?> LireAsync(string cle, CancellationToken ct = default);

        /// <summary>Écrit une valeur brute.</summary>
        Task EcrireAsync(string cle, string valeur, CancellationToken ct = default);
    }
}
