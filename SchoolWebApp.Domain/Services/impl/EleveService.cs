using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class EleveService : IEleveService
    {
        private readonly IEleveRepository _eleveRepository;

        public EleveService(IEleveRepository eleveRepository)
        {
            _eleveRepository = eleveRepository ?? throw new ArgumentNullException(nameof(eleveRepository));
        }

        public Task<IEnumerable<Eleve>> GetElevesAsync() =>
            _eleveRepository.GetElevesAsync();

        public Task<IEnumerable<Eleve>> GetElevesByParentAsync(int parentId) =>
            _eleveRepository.GetElevesByParentAsync(parentId);

        public Task<Eleve?> GetEleveByIdAsync(int id) =>
            _eleveRepository.GetEleveByIdAsync(id);

        public async Task<Eleve?> GetEleveForParentAsync(int eleveId, int parentId)
        {
            var eleve = await _eleveRepository.GetEleveByIdAsync(eleveId);

            // Un élève qui n'appartient pas au demandeur est traité comme inexistant :
            // renvoyer 403 plutôt que 404 confirmerait au passage que l'id existe.
            if (eleve is null || eleve.ParentId != parentId) return null;

            // Un profil retiré l'est PARTOUT, pas seulement dans les listes.
            // C'est ce point qui compte : sans lui, un lien mis en favori ou un
            // onglet resté ouvert permettrait de lancer un cours au nom d'un
            // enfant qui ne prend plus de place dans la formule — donc de
            // travailler hors quota.
            return eleve.ArchiveLe is null ? eleve : null;
        }

        public Task<Eleve> AddEleveAsync(Eleve model) =>
            _eleveRepository.AddEleveAsync(model);

        public Task<Eleve?> UpdateEleveAsync(Eleve model) =>
            _eleveRepository.UpdateEleveAsync(model);

        public Task<IEnumerable<Eleve>> GetArchivesByParentAsync(int parentId) =>
            _eleveRepository.GetArchivesByParentAsync(parentId);

        public Task<bool> ArchiverEleveAsync(int id) =>
            _eleveRepository.ArchiverEleveAsync(id);

        public Task<bool> RestaurerEleveAsync(int id) =>
            _eleveRepository.RestaurerEleveAsync(id);

        public Task<bool> AnonymiserEleveAsync(int id) =>
            _eleveRepository.AnonymiserEleveAsync(id);

        public Task<string?> AttribuerCodeAsync(int eleveId, CancellationToken ct = default) =>
            _eleveRepository.AttribuerCodeAsync(eleveId, ct);

        /// <summary>
        /// La normalisation est faite ICI, à l'entrée du domaine.
        ///
        /// L'enfant tape « kut-49r », « KUT 49R » ou « kut49r ». Laisser chaque
        /// appelant s'en charger garantirait qu'un jour l'un d'eux l'oublie, et
        /// refuserait un code pourtant juste — sans que personne comprenne
        /// pourquoi.
        /// </summary>
        public Task<Eleve?> GetParCodeAsync(string code, CancellationToken ct = default) =>
            _eleveRepository.GetParCodeAsync(CodeAccesEleve.Normaliser(code), ct);

        public Task<bool> SuspendreAccesAsync(
            int eleveId, bool suspendu, CancellationToken ct = default) =>
            _eleveRepository.SuspendreAccesAsync(eleveId, suspendu, ct);
    }
}
