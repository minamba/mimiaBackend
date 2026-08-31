using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le compteur d'une matière : combien de fiches, et combien réclament
    /// l'attention de l'élève.
    /// </summary>
    public record CompteurFiches(int Total, int Nouveautes);

    public interface IFicheRepository
    {
        /// <summary>
        /// Enregistre une fiche, ou remplace celle qui existe déjà pour la même
        /// notion. Réviser deux fois une notion doit enrichir la fiche, pas en
        /// empiler une seconde que l'enfant devrait comparer à la première.
        ///
        /// La date de consultation n'est jamais touchée ici : c'est la mise à
        /// jour du contenu qui rend la fiche « non lue » à nouveau, par simple
        /// comparaison des deux dates.
        ///
        /// Null si la conversation n'appartient pas à cet élève.
        /// </summary>
        Task<FicheRevisionEleve?> EnregistrerAsync(
            int eleveId,
            int conversationId,
            string notion,
            string? domaine,
            string contenu,
            string? etat = null,
            CancellationToken ct = default);

        /// <summary>Les fiches d'un élève dans une matière, la plus récente d'abord.</summary>
        Task<IEnumerable<FicheRevisionEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>
        /// Les seuls titres des fiches déjà écrites, pour le contexte de l'agent.
        ///
        /// Le professeur écrivait ses fiches sans savoir lesquelles existaient
        /// déjà : il réinventait un intitulé à chaque séance, et une notion
        /// reprise donnait une seconde fiche au lieu d'enrichir la première.
        /// Deux colonnes suffisent — il ne s'agit pas de lui relire ce qu'il a
        /// écrit, seulement de lui rappeler ce qu'il a couvert.
        /// </summary>
        Task<IReadOnlyList<string>> GetTitresAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>Une fiche complète, avec l'identité de l'élève, pour l'imprimer.</summary>
        Task<FicheRevisionEleve?> GetDetailAsync(
            int ficheId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// L'élève vient d'ouvrir la fiche : la pastille s'éteint.
        ///
        /// Renvoie false si la fiche n'est pas la sienne — la garde d'accès est
        /// la même que pour la lecture.
        /// </summary>
        Task<bool> MarquerVueAsync(int ficheId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Combien de fiches par matière, et combien de non-lues. Sert à
        /// afficher le compteur sur les cartes de matière sans charger toutes
        /// les fiches.
        /// </summary>
        Task<IReadOnlyDictionary<int, CompteurFiches>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default);
    }
}
