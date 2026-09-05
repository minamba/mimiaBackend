using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les bandeaux promotionnels de la page d'accueil.
    ///
    /// TOUTES LES LECTURES DE LISTE SONT SANS OCTETS. Seul
    /// <see cref="GetImageAsync"/> ramène un fichier, et un seul à la fois.
    /// </summary>
    public interface IPromoRepository
    {
        /// <summary>
        /// Le bandeau affiché, ou null s'il n'y en a aucun.
        ///
        /// LA LECTURE LA PLUS CHAUDE DU DÉPÔT : elle part à chaque affichage
        /// de la page d'accueil, y compris pour les visiteurs anonymes.
        /// </summary>
        Task<Promo?> GetActifAsync(CancellationToken ct = default);

        /// <summary>Toute la bibliothèque, la plus récente d'abord.</summary>
        Task<IEnumerable<Promo>> GetTousAsync(CancellationToken ct = default);

        Task<Promo?> GetAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Les octets d'un visuel.
        /// </summary>
        /// <param name="mobile">
        /// Vrai pour la version téléphone. Si elle n'existe pas, l'image large
        /// est rendue à la place — l'appelant n'a pas à connaître la règle de
        /// repli, et une promo sans version mobile doit rester visible.
        /// </param>
        Task<ImagePromo?> GetImageAsync(int id, bool mobile, CancellationToken ct = default);

        /// <summary>Crée un bandeau. Il est ÉTEINT à la création.</summary>
        /// <remarks>
        /// Éteint, et ce n'est pas une commodité : un visuel qui s'afficherait
        /// dès le téléversement mettrait en ligne un brouillon, ou une image
        /// dont on voulait d'abord vérifier le cadrage. On l'allume ensuite,
        /// après l'avoir vu dans la bibliothèque.
        /// </remarks>
        Task<Promo> CreerAsync(
            string titre,
            string texteAlternatif,
            string? lien,
            byte[] imageLarge,
            string typeMimeLarge,
            byte[]? imageMobile,
            string? typeMimeMobile,
            bool pleineLargeur,
            CancellationToken ct = default);

        /// <summary>
        /// Modifie ce qui est fourni, laisse le reste tel quel.
        /// </summary>
        /// <remarks>
        /// LES IMAGES NULLES NE VIDENT RIEN : elles veulent dire « ne touche
        /// pas à celle-ci ». Corriger une faute dans le texte alternatif ne
        /// doit pas obliger à retéléverser deux fichiers.
        /// </remarks>
        Task<Promo?> ModifierAsync(
            int id,
            string titre,
            string texteAlternatif,
            string? lien,
            byte[]? imageLarge,
            string? typeMimeLarge,
            byte[]? imageMobile,
            string? typeMimeMobile,
            bool pleineLargeur,
            CancellationToken ct = default);

        /// <summary>
        /// Affiche ce bandeau, ou le retire.
        /// </summary>
        /// <remarks>
        /// EN L'AFFICHANT, ÉTEINT TOUS LES AUTRES — dans la même écriture, pas
        /// en deux appels. La règle « un seul à la fois » tient ici, au seul
        /// endroit qui la garantisse quel que soit l'appelant. Un écran qui
        /// enverrait deux requêtes laisserait une fenêtre où deux bandeaux
        /// sont allumés, et un plantage entre les deux la rendrait permanente.
        /// </remarks>
        Task<bool> DefinirAffichageAsync(int id, bool actif, CancellationToken ct = default);

        /// <summary>Supprime un bandeau et ses visuels. Définitif.</summary>
        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);
    }
}
