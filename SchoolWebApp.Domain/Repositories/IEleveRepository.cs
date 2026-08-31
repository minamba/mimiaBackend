using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IEleveRepository
    {
        Task<IEnumerable<Eleve>> GetElevesAsync();

        /// <summary>Les profils enfants d'un parent donné.</summary>
        Task<IEnumerable<Eleve>> GetElevesByParentAsync(int parentId);

        Task<Eleve?> GetEleveByIdAsync(int id);

        Task<Eleve> AddEleveAsync(Eleve model);

        Task<Eleve?> UpdateEleveAsync(Eleve model);

        Task<IEnumerable<Eleve>> GetArchivesByParentAsync(int parentId);

        /// <summary>Retire le profil des listes et du quota, sans rien effacer.</summary>
        Task<bool> ArchiverEleveAsync(int id);

        /// <summary>Remet un profil retiré parmi les enfants actifs.</summary>
        Task<bool> RestaurerEleveAsync(int id);

        /// <summary>Efface l'identité et tout l'historique. Irréversible.</summary>
        Task<bool> AnonymiserEleveAsync(int id);

        /// <summary>
        /// Attribue un code d'accès neuf à un enfant et le rend.
        ///
        /// Reprend le tirage si la base refuse pour cause de doublon : deux
        /// cent quarante-quatre millions de codes ne rendent pas la collision
        /// impossible, seulement rare — et « rare » veut dire « un jour ».
        /// </summary>
        Task<string?> AttribuerCodeAsync(int eleveId, CancellationToken ct = default);

        /// <summary>
        /// L'enfant qui porte ce code, ou null. Le code est attendu NORMALISÉ —
        /// sans tiret, en capitales.
        /// </summary>
        Task<Eleve?> GetParCodeAsync(string code, CancellationToken ct = default);

        /// <summary>Coupe ou rouvre l'accès d'un enfant.</summary>
        Task<bool> SuspendreAccesAsync(int eleveId, bool suspendu, CancellationToken ct = default);
    }
}
