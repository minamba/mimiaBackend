using AutoMapper;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Api.ViewModels;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Services;
using DomainEleve = SchoolWebApp.Domain.Models.Eleve;

namespace SchoolWebApp.Api.Builders.impl
{
    public class EleveViewModelBuilder : IEleveViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IEleveService _eleveService;
        private readonly IParentService _parentService;
        private readonly IReferentielService _referentielService;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly ILogger<EleveViewModelBuilder> _logger;

        public EleveViewModelBuilder(
            IMapper mapper,
            IEleveService eleveService,
            IParentService parentService,
            IReferentielService referentielService,
            ICurrentUserAccessor currentUser,
            ILogger<EleveViewModelBuilder> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _eleveService = eleveService ?? throw new ArgumentNullException(nameof(eleveService));
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
            _referentielService = referentielService ?? throw new ArgumentNullException(nameof(referentielService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EleveViewModel>> GetElevesAsync()
        {
            var parent = await ResoudreParentAsync();
            var eleves = await _eleveService.GetElevesByParentAsync(parent.Id);
            return _mapper.Map<IEnumerable<EleveViewModel>>(eleves);
        }

        /// <summary>
        /// Les matières de cet élève, telles que le SERVEUR les décide.
        ///
        /// POURQUOI CETTE ROUTE EXISTE PLUTÔT QU'UN FILTRE DANS LA GRILLE
        /// -------------------------------------------------------------
        /// La grille recevait toutes les matières et écartait elle-même celles
        /// qui ne concernaient pas l'élève. La règle vivait donc deux fois — en
        /// C# et en JavaScript — et la copie JavaScript avait un défaut : quand
        /// le niveau de l'élève n'était pas encore connu, elle n'écartait plus
        /// RIEN. Un enfant de sixième s'est vu proposer la philosophie.
        ///
        /// Ici la question est posée au serveur, qui connaît l'élève et la
        /// règle. Le front affiche ce qu'on lui donne. Il n'y a plus de seconde
        /// copie à maintenir, ni de moment où elle s'ouvre en grand.
        ///
        /// LES MATIÈRES « À VENIR » SONT INCLUSES, avec leur drapeau `Active` à
        /// faux. Elles concernent bien la classe de l'élève ; c'est leur
        /// ouverture qui n'est pas faite. La grille les montre en « Bientôt »,
        /// et ce n'est pas la même information que « pas pour toi ».
        /// </summary>
        public async Task<IEnumerable<MatiereViewModel>?> GetMatieresDeLEleveAsync(int id)
        {
            var parent = await ResoudreParentAsync();
            var eleve = await _eleveService.GetEleveForParentAsync(id, parent.Id);
            if (eleve is null) return null;

            var niveaux = await _referentielService.GetNiveauxScolairesAsync();
            var niveau = niveaux.FirstOrDefault(n => n.Id == eleve.NiveauScolaireId);

            if (niveau is null)
            {
                // Un élève sans niveau lisible ne reçoit RIEN plutôt que TOUT.
                // C'est exactement le choix inverse de celui que faisait la
                // grille, et c'est le seul défendable : on ne sait pas ce qui
                // le concerne, donc on ne lui propose pas d'ouvrir un cours au
                // hasard.
                _logger.LogWarning(
                    "Eleve {EleveId} sans niveau lisible ({NiveauId}) : aucune matiere proposee.",
                    eleve.Id, eleve.NiveauScolaireId);

                return [];
            }

            var matieres = await _referentielService.GetMatieresAsync(activesSeulement: false);

            var siennes = matieres
                .Where(m => VoiesScolaires.EstAuProgramme(m, niveau))
                .ToList();

            return _mapper.Map<IEnumerable<MatiereViewModel>>(siennes);
        }

        public async Task<EleveViewModel?> GetEleveByIdAsync(int id)
        {
            var parent = await ResoudreParentAsync();
            var eleve = await _eleveService.GetEleveForParentAsync(id, parent.Id);
            return eleve is null ? null : _mapper.Map<EleveViewModel>(eleve);
        }

        public async Task<EleveViewModel?> AddEleveAsync(EleveRequest model)
        {
            var niveau = await _referentielService.GetNiveauScolaireByIdAsync(model.NiveauScolaireId);
            if (niveau is null)
            {
                _logger.LogWarning("Niveau scolaire {NiveauId} inexistant.", model.NiveauScolaireId);
                return null;
            }

            var parent = await ResoudreParentAsync();

            var eleve = _mapper.Map<DomainEleve>(model);
            eleve.Id = 0;
            eleve.ParentId = parent.Id;      // jamais depuis la requête
            eleve.DateCreation = DateTime.UtcNow;

            var cree = await _eleveService.AddEleveAsync(eleve);

            _logger.LogInformation("Profil eleve {EleveId} cree pour le parent {ParentId}.", cree.Id, parent.Id);

            return _mapper.Map<EleveViewModel>(cree);
        }

        public async Task<EleveViewModel?> UpdateEleveAsync(EleveRequest model)
        {
            var parent = await ResoudreParentAsync();

            // Vérifie la propriété AVANT toute écriture.
            var existant = await _eleveService.GetEleveForParentAsync(model.Id, parent.Id);
            if (existant is null) return null;

            if (model.NiveauScolaireId > 0)
            {
                var niveau = await _referentielService.GetNiveauScolaireByIdAsync(model.NiveauScolaireId);
                if (niveau is null) return null;
            }

            var eleve = _mapper.Map<DomainEleve>(model);
            eleve.ParentId = parent.Id;

            var maj = await _eleveService.UpdateEleveAsync(eleve);
            return maj is null ? null : _mapper.Map<EleveViewModel>(maj);
        }

        public async Task<IEnumerable<EleveViewModel>> GetArchivesAsync()
        {
            var parent = await ResoudreParentAsync();
            var eleves = await _eleveService.GetArchivesByParentAsync(parent.Id);
            return _mapper.Map<IEnumerable<EleveViewModel>>(eleves);
        }

        public async Task<bool> ArchiverEleveAsync(int id) =>
            await AppartientAuParentAsync(id) && await _eleveService.ArchiverEleveAsync(id);

        public async Task<bool> RestaurerEleveAsync(int id)
        {
            // `GetEleveForParentAsync` ne rend que les profils ACTIFS : un
            // profil retiré n'y figure plus, et la garde d'accès habituelle
            // refuserait donc systématiquement sa restauration.
            var parent = await ResoudreParentAsync();

            var archives = await _eleveService.GetArchivesByParentAsync(parent.Id);
            if (!archives.Any(e => e.Id == id)) return false;

            return await _eleveService.RestaurerEleveAsync(id);
        }

        public async Task<bool> AnonymiserEleveAsync(int id)
        {
            var parent = await ResoudreParentAsync();

            // Effaçable qu'il soit actif ou déjà retiré : un parent qui a mis
            // un profil de côté doit pouvoir aller au bout de sa démarche.
            var actif = await _eleveService.GetEleveForParentAsync(id, parent.Id);
            var archives = await _eleveService.GetArchivesByParentAsync(parent.Id);

            if (actif is null && !archives.Any(e => e.Id == id)) return false;

            _logger.LogWarning(
                "Effacement des donnees de l'eleve {EleveId} demande par le parent {ParentId}.",
                id, parent.Id);

            return await _eleveService.AnonymiserEleveAsync(id);
        }

        private async Task<bool> AppartientAuParentAsync(int id)
        {
            var parent = await ResoudreParentAsync();
            return await _eleveService.GetEleveForParentAsync(id, parent.Id) is not null;
        }

        /// <summary>
        /// Résout le parent métier depuis le JWT, en le créant au premier appel.
        /// </summary>
        private async Task<Domain.Models.Parent> ResoudreParentAsync()
        {
            var identityUserId = _currentUser.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                throw new UnauthorizedAccessException("Le jeton ne contient pas de claim `sub`.");
            }

            return await _parentService.GetOrCreateAsync(
                identityUserId,
                _currentUser.Mail,
                _currentUser.Prenom,
                _currentUser.Nom,

                // SEULEMENT SI CE N EST PAS L ENFANT. Son jeton porte le « sub »
                // du parent : sans ce test, chaque clic d un enfant ferait
                // passer son parent pour présent.
                noterLaVenue: _currentUser.EleveId is null);
        }
    }
}
