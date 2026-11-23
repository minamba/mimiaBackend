namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// LES DOCUMENTS DES EXAMENS BLANCS, SUR LE DISQUE (23/11/2026) — même disque, même marque et mêmes gestes que
    /// les cours de contrôle (<see cref="IStockageCoursControles"/>), dans le sous-dossier <c>examens-blancs/</c>.
    /// Chemin : <c>eleveId/examenBlancId/guid.ext</c>. Effacés deux mois après l'examen blanc
    /// (<c>PurgeExamensBlancsWorker</c>).
    /// </summary>
    public interface IStockageExamensBlancs
    {
        bool Pret { get; }

        Task<string> EcrireAsync(int eleveId, int examenBlancId, byte[] donnees, string nom, CancellationToken ct = default);

        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        void Supprimer(string? cheminRelatif);

        void SupprimerDossier(int eleveId, int examenBlancId);

        /// <summary>Les dossiers de plus d'un jour : la purge efface ceux dont l'examen blanc n'existe plus.</summary>
        IReadOnlyList<(int EleveId, int ControleId)> DossiersAnciens();
    }
}
