using AutoMapper;
using SchoolWebApp.Api.ViewModels;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Builders.impl
{
    public class ReferentielViewModelBuilder : IReferentielViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IReferentielService _referentielService;

        public ReferentielViewModelBuilder(IMapper mapper, IReferentielService referentielService)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _referentielService = referentielService ?? throw new ArgumentNullException(nameof(referentielService));
        }

        public async Task<IEnumerable<NiveauScolaireViewModel>> GetNiveauxScolairesAsync()
        {
            var niveaux = await _referentielService.GetNiveauxScolairesAsync();
            var vues = _mapper.Map<IEnumerable<NiveauScolaireViewModel>>(niveaux).ToList();

            // UNE SPÉCIALITÉ N'EST PROPOSÉE QUE SI SON PROFESSEUR EXISTE : une case
            // cochée sans matière ouverte derrière ne donnerait rien à l'élève.
            var ouvertes = (await _referentielService.GetMatieresAsync(activesSeulement: true))
                .Select(m => m.Code)
                .Where(c => c is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var proposees = VoiesScolaires.SpecialitesGenerales
                .Where(s => ouvertes.Contains(s.Code))
                .Select(s => new SpecialiteViewModel { Code = s.Code, Libelle = s.Libelle, Precision = s.Precision })
                .ToList();

            // Le regroupement et le choix possible viennent de la même table que
            // le programme de chaque classe : `VoiesScolaires`.
            foreach (var vue in vues)
            {
                vue.Groupe = VoiesScolaires.Groupe(vue.Code, vue.Cycle);
                vue.Selectionnable = VoiesScolaires.EstSelectionnable(vue.Code);
                vue.Lv2Possible = VoiesScolaires.AUneLv2(vue.Code);
                vue.NombreSpecialites = VoiesScolaires.NombreSpecialites(vue.Code);
                vue.SpecialitesPossibles = vue.NombreSpecialites > 0 ? proposees : [];
            }

            return vues;
        }

        public async Task<IEnumerable<AcademieViewModel>> GetAcademiesAsync()
        {
            var academies = await _referentielService.GetAcademiesAsync();
            return _mapper.Map<IEnumerable<AcademieViewModel>>(academies);
        }

        public async Task<IEnumerable<MatiereViewModel>> GetMatieresAsync(bool activesSeulement)
        {
            var matieres = await _referentielService.GetMatieresAsync(activesSeulement);
            var vues = _mapper.Map<IEnumerable<MatiereViewModel>>(matieres).ToList();

            // LA RÈGLE DES VOIES EST POSÉE ICI, PAS EN BASE.
            //
            // Elle décrit le programme national, pas la configuration de cette
            // installation : elle n a donc rien à faire dans une colonne qu un
            // administrateur pourrait changer. Le front s en sert pour ne pas
            // proposer la philosophie à un terminale professionnelle, ni le
            // français à un terminale générale.
            foreach (var vue in vues)
            {
                vue.NiveauCodesExclus = VoiesScolaires.NiveauxExclus(vue.Code);
            }

            return vues;
        }

        /// <summary>
        /// L'équipe pédagogique, telle que la page d'accueil la montre.
        ///
        /// REGROUPÉE PAR VISAGE, ET NON PAR MATIÈRE. Yann enseigne les sciences
        /// et technologie jusqu'à la 6e, puis la physique-chimie à partir de la
        /// 5e : ce sont deux matières et un seul professeur. Une liste de
        /// matières l'afficherait deux fois, avec le même visage et le même
        /// prénom — ce qui donne l'impression d'un défaut d'affichage.
        ///
        /// SEULES LES MATIÈRES OUVERTES. Une matière « à venir » n'a pas de
        /// professeur à présenter : promettre un visage sur la page d'accueil
        /// pour une matière qu'on ne peut pas encore prendre serait une
        /// annonce, pas une équipe.
        /// </summary>
        public async Task<IEnumerable<ProfesseurViewModel>> GetEquipeAsync()
        {
            var matieres = await _referentielService.GetMatieresAsync(activesSeulement: true);

            return matieres
                // Un professeur sans visage ne peut pas être dessiné : on
                // l'écarte plutôt que d'afficher une carte vide.
                .Where(m => !string.IsNullOrWhiteSpace(m.ProfAvatar))
                .GroupBy(m => m.ProfAvatar!, StringComparer.OrdinalIgnoreCase)
                .Select(groupe =>
                {
                    // Par l'ordre de la matière : c'est celui de la grille que
                    // l'élève voit, et la matière PRINCIPALE d'un professeur est
                    // celle qu'il enseigne en premier — pour Yann, les sciences,
                    // dont le motif parle des deux.
                    var siennes = groupe.OrderBy(m => m.Ordre).ToList();
                    var principale = siennes[0];

                    return new ProfesseurViewModel
                    {
                        Prenom = principale.ProfPrenom,
                        Avatar = principale.ProfAvatar,
                        Couleur = principale.ProfCouleur,
                        Code = principale.Code,
                        Matieres = siennes.Select(m => NomCourt(m.Code, m.Libelle)).ToList(),
                    };
                })
                // L'équipe suit l'ordre des matières, pas l'ordre du
                // regroupement : sans ce tri, `GroupBy` rend les professeurs
                // dans un ordre qui n'est garanti nulle part.
                .OrderBy(p => matieres.First(m => m.Code == p.Code).Ordre)
                .ToList();
        }

        /// <summary>
        /// LE NOM COURT D'UNE MATIÈRE, POUR LES CARTES DE L'ÉQUIPE — Camara, le
        /// 15/09/2026 : « enlève les descriptions, laisse juste le nom des
        /// matières, sinon c'est trop long ». Sous Adrien s'alignaient trois
        /// intitulés officiels complets (« Littérature, langues et cultures de
        /// l'Antiquité — latin »…) et la carte devenait un paragraphe.
        ///
        /// LES SIGLES QUE LES FAMILLES CONNAISSENT : c'est ainsi que les
        /// spécialités s'écrivent sur un bulletin et dans les conversations.
        ///
        /// POUR CES CARTES SEULEMENT : ailleurs — grille des matières, fiches,
        /// bilans — l'intitulé complet reste, parce qu'on y a la place et que
        /// l'élève y cherche sa matière exacte. Une matière absente de la table
        /// garde son libellé : un ajout ne casse rien, il reste simplement long.
        /// </summary>
        private static string NomCourt(string? code, string? libelle) => code?.ToUpperInvariant() switch
        {
            "SCIENCES" => "Sciences",
            "HGGSP" => "HGGSP",
            "HLP" => "HLP",
            "SES" => "SES",
            "NSI" => "NSI",
            "SI" => "SI",
            "EPPCS" => "EPPCS",
            "LLCER_ANGLAIS" => "LLCER anglais",
            "AMC" => "AMC",
            "LLCER_ESPAGNOL" => "LLCER espagnol",
            "LLCA_LATIN" => "LLCA latin",
            "LLCA_GREC" => "LLCA grec",
            "SCIENCES_GESTION" => "SGN",
            "SANITAIRE_SOCIAL" => "STSS",
            "BIOLOGIE_HUMAINE" => "Biologie humaine",
            "BIOTECHNOLOGIES" => "Biotechnologies",
            "SPCL" => "SPCL",
            _ => libelle ?? string.Empty,
        };
    }
}
