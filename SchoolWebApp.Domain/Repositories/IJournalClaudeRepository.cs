namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le journal de ce que consomment les tâches de fond.
    ///
    /// Le dialogue porte ses jetons sur chaque message ; les workers, eux,
    /// n'appartiennent à aucune conversation et n'avaient nulle part où les
    /// écrire. D'où ce journal : sans lui, une facture qui monte ne désigne
    /// personne, et on cherche dans les journaux d'application ce qu'une
    /// somme aurait dit en une ligne.
    /// </summary>
    public interface IJournalClaudeRepository
    {
        /// <summary>
        /// Note un appel. N'échoue jamais : une mesure perdue ne doit pas
        /// emporter le travail qu'elle mesure.
        /// </summary>
        /// <param name="origine">
        /// `description-planche`, `reperes-planche`, `transcription-document`,
        /// `observation-competences`, `bilan`.
        /// </param>
        /// <param name="reference">
        /// La clé de la planche ou l'identifiant de la séance : de quoi
        /// retrouver le coupable quand un poste s'emballe.
        /// </param>
        Task EnregistrerAsync(
            string origine,
            string? modele,
            long entree,
            long sortie,
            long cacheLecture,
            long cacheEcriture,
            string? reference = null,
            CancellationToken ct = default);
    }
}
