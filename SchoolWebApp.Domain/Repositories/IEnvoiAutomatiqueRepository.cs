using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Qui reçoit les courriels automatiques, ce qui est déjà parti, et qui
    /// s'est désabonné.
    ///
    /// CHAQUE REQUÊTE DE DESTINATAIRES ÉCARTE D'ELLE-MÊME les adresses bannies,
    /// les parents désabonnés de la catégorie et ceux qui ont déjà reçu le
    /// courriel : l'appelant n'a aucun filtre à se rappeler.
    /// </summary>
    public interface IEnvoiAutomatiqueRepository
    {
        /// <summary>
        /// Les essais qui se terminent dans la fenêtre [debut, fin), pas encore
        /// remplacés par une formule payante et pas encore prévenus.
        /// </summary>
        Task<IReadOnlyList<DestinataireAutomatique>> GetEssaisExpirantAsync(
            DateTime debutUtc, DateTime finUtc, DateTime maintenantUtc, int limite, CancellationToken ct = default);

        /// <summary>
        /// Les parents inscrits dans la fenêtre donnée, qui ont eu un essai,
        /// n'ont laissé aucun avis et n'ont jamais reçu cette demande.
        /// </summary>
        Task<IReadOnlyList<DestinataireAutomatique>> GetAvisEssaiAsync(
            DateTime inscritApresUtc, DateTime inscritAvantUtc, int limite, CancellationToken ct = default);

        /// <summary>
        /// Les parents abonnés à une formule payante, sans avis, à qui aucune
        /// demande d'avis n'est partie depuis <paramref name="depuisUtc"/>.
        /// </summary>
        Task<IReadOnlyList<DestinataireAutomatique>> GetAvisGeneralAsync(
            DateTime depuisUtc, int limite, CancellationToken ct = default);

        /// <summary>
        /// Les parents d'une diffusion de masse : ceux qui ont une adresse et ne
        /// se sont pas désabonnés des annonces.
        /// </summary>
        Task<IReadOnlyList<DestinataireAutomatique>> GetDestinatairesDiffusionAsync(CancellationToken ct = default);

        /// <summary>
        /// Réserve un envoi AVANT de le faire. Nul si la clé est déjà prise :
        /// ce courriel est déjà parti à ce parent.
        /// </summary>
        Task<int?> ReserverAsync(
            string codeModele, int parentId, string? cle, DateTime occurrenceUtc, CancellationToken ct = default);

        Task MarquerAsync(int id, string statut, CancellationToken ct = default);

        Task<bool> EstDesabonneAsync(int parentId, string categorie, CancellationToken ct = default);

        /// <summary>Idempotent : désabonner deux fois ne fait rien de plus.</summary>
        Task DesabonnerAsync(int parentId, string categorie, CancellationToken ct = default);

        Task ReabonnerAsync(int parentId, string categorie, CancellationToken ct = default);
    }
}
