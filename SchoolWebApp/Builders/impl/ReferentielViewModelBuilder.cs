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

                    var (titre, presentation) = Vitrine(siennes);

                    return new ProfesseurViewModel
                    {
                        Prenom = principale.ProfPrenom,
                        Avatar = principale.ProfAvatar,
                        Couleur = principale.ProfCouleur,
                        Code = principale.Code,
                        Matieres = siennes.Select(m => NomCourt(m.Code, m.Libelle)).ToList(),
                        Titre = titre,
                        Presentation = presentation,
                        Disciplines = siennes.Select(m => new DisciplineViewModel
                        {
                            Libelle = m.Libelle,
                            Promesse = m.Promesse,
                            Niveaux = Niveaux(m.NiveauOrdreMin, m.NiveauOrdreMax),
                            Voie = Voie(m.Code, m.NiveauOrdreMin),
                        }).ToList(),
                    };
                })
                // L'équipe suit l'ordre des matières, pas l'ordre du
                // regroupement : sans ce tri, `GroupBy` rend les professeurs
                // dans un ordre qui n'est garanti nulle part.
                .OrderBy(p => matieres.First(m => m.Code == p.Code).Ordre)
                .ToList();
        }

        /// <summary>
        /// LA CARTE ET LA FICHE D'UN PROFESSEUR — Camara, le 15/09/2026 : « sur la
        /// landing page, uniquement la matière », le détail des disciplines
        /// passant dans une fiche qui s'ouvre au clic. Le titre est celui que
        /// Camara a choisi pour chacun ; la présentation est à la première
        /// personne, sans diplôme ni promesse que le produit ne tiendrait pas.
        ///
        /// CHERCHÉ DANS TOUTES SES MATIÈRES, PAS SEULEMENT LA PREMIÈRE : si l'ordre
        /// de la grille change en base, Karim pourrait ouvrir sur « Management »
        /// au lieu de « Sciences de gestion ». La première matière reconnue, dans
        /// l'ordre, donne la carte. Un professeur absent de la table garde le
        /// libellé de sa matière principale, sans présentation : un ajout ne
        /// casse rien.
        /// </summary>
        private static (string Titre, string? Presentation) Vitrine(List<Matiere> siennes)
        {
            foreach (var matiere in siennes)
            {
                (string, string)? trouve = matiere.Code?.ToUpperInvariant() switch
                {
                    "MATHS" => ("Mathématiques", "Les maths ne sont pas une question de don : on part de ce que tu sais déjà, et on avance une marche à la fois, jusqu'à ce que ça devienne clair."),
                    "FRANCAIS" => ("Français", "Lire, écrire, trouver ses mots : je t'accompagne de la première dictée jusqu'au bac de français, à ton rythme."),
                    "HISTOIRE_GEO" => ("Histoire-Géographie", "Le passé et le monde se comprennent mieux quand on les raconte : on relie les dates, les cartes et les grandes idées."),
                    "ANGLAIS" => ("Anglais", "Une langue s'apprend en la parlant : on écoute, on ose, et on se trompe sans avoir peur. C'est comme ça qu'on progresse."),
                    "SCIENCES" or "PHYSIQUE_CHIMIE" => ("Sciences · Physique-Chimie", "J'aime quand on comprend pourquoi ça marche : on observe, on mesure, et on explique ensemble ce qui se passe."),
                    "ESPAGNOL" => ("Espagnol", "¡Hola! On apprend l'espagnol en le parlant, et on découvre au passage les cultures du monde hispanique."),
                    "SVT" => ("SVT", "Le vivant, le corps humain, la Terre : on part de ce qu'on observe pour comprendre comment tout cela fonctionne."),
                    "PHILOSOPHIE" => ("Philosophie", "Penser par soi-même, ça s'apprend : on pose le problème, on argumente, et la dissertation devient beaucoup moins intimidante."),
                    "SCIENCES_GESTION" or "MANAGEMENT" or "DROIT_ECONOMIE" => ("Management / Droit et économie", "L'économie, le droit et la gestion sont partout autour de toi : on part d'exemples concrets pour comprendre les décisions."),
                    // « STSS » ne disait rien à une famille : Camara voulait un nom
                    // plus parlant, et court.
                    "SANITAIRE_SOCIAL" => ("Santé et social", "La santé, le bien-être et la vie sociale : on relie tes cours aux situations bien réelles qu'ils décrivent."),
                    // La NSI a son propre professeur depuis le 15/09/2026 : elle
                    // n'apparaît plus dans la fiche de Nora.
                    "NSI" => ("Informatique", "Programmer, c'est apprendre à donner des instructions claires : on écrit du code, on le teste, on se trompe, et on comprend peu à peu comment marchent les outils numériques de tous les jours."),
                    "EPPCS" => ("Sport","Le sport se vit et se pense : on relie la pratique, le corps et la place du sport dans la société."),
                    "ARTS_PLASTIQUES" => ("Arts", "Chaque œuvre a quelque chose à dire : on apprend à la regarder, à l'écouter et à en parler avec tes propres mots."),
                    _ => null,
                };

                if (trouve is { } vitrine) return vitrine;
            }

            return (siennes[0].Libelle ?? string.Empty, null);
        }

        private static readonly string[] Classes =
            ["CP", "CE1", "CE2", "CM1", "CM2", "6e", "5e", "4e", "3e", "2de", "1re", "Terminale"];

        /// <summary>
        /// Les classes d'une discipline, en clair : « Du CP à la Terminale »,
        /// « De la 5e à la Terminale », « 1re et Terminale », « Terminale ».
        /// Le rang 1 est le CP, le rang 12 la terminale — ceux du référentiel.
        /// </summary>
        private static string? Niveaux(int min, int max)
        {
            if (min < 1 || max > Classes.Length || min > max) return null;

            string Nom(int rang) => Classes[rang - 1];

            if (min == max) return Nom(min);
            if (min == 11 && max == 12) return "1re et Terminale";

            // Les classes du primaire se disent au masculin (« le CP »), celles du
            // secondaire au féminin (« la 5e », « la Terminale »).
            var depuis = min <= 5 ? $"Du {Nom(min)}" : $"De la {Nom(min)}";
            var jusqua = max <= 5 ? $"au {Nom(max)}" : $"à la {Nom(max)}";
            return $"{depuis} {jusqua}";
        }

        /// <summary>
        /// La voie ou la série d'une discipline réservée. Les mathématiques, la
        /// physique-chimie et la SVT sont AUSSI des spécialités générales : la
        /// mention n'est donc posée qu'aux disciplines qui n'existent qu'au
        /// lycée, sans quoi « Mathématiques, du CP à la Terminale » se lirait
        /// comme une spécialité.
        /// </summary>
        private static string? Voie(string? code, int niveauMin)
        {
            var normalise = code?.ToUpperInvariant();

            switch (normalise)
            {
                case "SCIENCES_GESTION" or "MANAGEMENT" or "DROIT_ECONOMIE": return "Série STMG";
                case "SANITAIRE_SOCIAL" or "BIOLOGIE_HUMAINE": return "Série ST2S";
                case "BIOTECHNOLOGIES" or "SPCL": return "Série STL";
                case "ESPAGNOL": return "En LV2";
            }

            var specialite = niveauMin >= 11 && VoiesScolaires.SpecialitesGenerales
                .Any(s => string.Equals(s.Code, normalise, StringComparison.OrdinalIgnoreCase));

            return specialite ? "Spécialité, voie générale" : null;
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
