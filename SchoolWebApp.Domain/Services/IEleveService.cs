using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface IEleveService
    {
        Task<IEnumerable<Eleve>> GetElevesAsync();

        Task<IEnumerable<Eleve>> GetElevesByParentAsync(int parentId);

        Task<Eleve?> GetEleveByIdAsync(int id);

        /// <summary>
        /// Retourne l'élève uniquement s'il appartient au parent indiqué.
        /// Toute lecture déclenchée par une requête authentifiée doit passer par ici,
        /// jamais par GetEleveByIdAsync : sinon n'importe quel parent peut lire
        /// l'enfant d'un autre en changeant l'id dans l'URL.
        /// </summary>
        Task<Eleve?> GetEleveForParentAsync(int eleveId, int parentId);

        Task<Eleve> AddEleveAsync(Eleve model);

        Task<Eleve?> UpdateEleveAsync(Eleve model);

        Task<IEnumerable<Eleve>> GetArchivesByParentAsync(int parentId);

        Task<bool> ArchiverEleveAsync(int id);

        Task<bool> RestaurerEleveAsync(int id);

        Task<bool> AnonymiserEleveAsync(int id);

        /// <summary>Attribue un code d'accès neuf à un enfant et le rend.</summary>
        Task<string?> AttribuerCodeAsync(int eleveId, CancellationToken ct = default);

        /// <summary>L'enfant qui porte ce code — normalisé — ou null.</summary>
        Task<Models.Eleve?> GetParCodeAsync(string code, CancellationToken ct = default);

        /// <summary>Coupe ou rouvre l'accès d'un enfant.</summary>
        Task<bool> SuspendreAccesAsync(int eleveId, bool suspendu, CancellationToken ct = default);
    }
}
