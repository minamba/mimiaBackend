namespace SchoolWebApp.Api.ViewModels
{
    public class NiveauScolaireViewModel
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? Cycle { get; set; }

        public int Ordre { get; set; }

        /// <summary>
        /// Le groupe sous lequel ranger la classe dans une liste de choix :
        /// « Collège », « Série STMG »… Voir `VoiesScolaires.Groupe`.
        /// </summary>
        public string? Groupe { get; set; }

        /// <summary>
        /// Faux pour les classes de regroupement (« Terminale STMG (tronc
        /// commun) ») : elles portent des compétences, pas des élèves.
        /// </summary>
        public bool Selectionnable { get; set; } = true;

        /// <summary>
        /// Une LV2 existe dans cette classe : le formulaire y propose la case
        /// « espagnol en LV2 ». Voir `VoiesScolaires.AUneLv2`.
        /// </summary>
        public bool Lv2Possible { get; set; }

        /// <summary>
        /// Combien de spécialités le parent coche dans cette classe : 3 en
        /// première générale, 2 en terminale, 0 ailleurs.
        /// </summary>
        public int NombreSpecialites { get; set; }

        /// <summary>Les spécialités proposées — seulement celles qui ont un professeur ouvert.</summary>
        public List<SpecialiteViewModel> SpecialitesPossibles { get; set; } = [];
    }

    public class SpecialiteViewModel
    {
        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        /// <summary>Le nom officiel en toutes lettres, sous un sigle (« Langues, littératures et cultures étrangères »).</summary>
        public string? Precision { get; set; }
    }

    public class AcademieViewModel
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        /// <summary>A, B, C, ou le calendrier propre à la Corse et à chaque académie d'outre-mer.</summary>
        public string? Zone { get; set; }
    }

    /// <summary>Le calendrier d'un élève : ses vacances, ses séances, ses évaluations à venir.</summary>
    public class CalendrierEleveViewModel
    {
        /// <summary>Null si l'académie de l'enfant n'a pas été renseignée.</summary>
        public string? Zone { get; set; }

        public IEnumerable<PeriodeVacancesViewModel> Vacances { get; set; } = [];

        /// <summary>
        /// La période en cours ou, à défaut, la toute prochaine — jamais
        /// bornée au mois affiché, contrairement à <see cref="Vacances"/> :
        /// c'est elle qui alimente le décompte affiché sous le bouton
        /// « Retour », qui doit rester juste quel que soit le mois consulté.
        /// Null si l'académie n'est pas renseignée ou si aucune période à
        /// venir n'est connue.
        /// </summary>
        public PeriodeVacancesViewModel? ProchaineVacances { get; set; }

        public IEnumerable<SeanceJourViewModel> Seances { get; set; } = [];

        /// <summary>Les évaluations déjà passées dans le mois affiché.</summary>
        public IEnumerable<EvaluationJourViewModel> Evaluations { get; set; } = [];

        // PLUS D'« ÉVALUATIONS PRÉVUES » ICI — retirées du calendrier par
        // Camara le 15/09/2026 : sans date, un report que l'élève repousse
        // restait affiché indéfiniment. Elles sont toujours enregistrées en
        // séance (`EvaluationPrevueRepository`), simplement plus montrées.

        /// <summary>
        /// Les contrôles à venir du mois affiché. Un contrôle a une vraie
        /// date : il s'épingle sur son jour, comme <see cref="Evaluations"/>.
        /// </summary>
        public IEnumerable<ControleViewModel> Controles { get; set; } = [];
    }

    public class PeriodeVacancesViewModel
    {
        public string? Libelle { get; set; }

        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }
    }

    public class SeanceJourViewModel
    {
        public DateTime Date { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfCouleur { get; set; }

        /// <summary>
        /// La durée choisie par l'élève pour cette séance (15/25/35/45 min).
        /// Null pour toute séance enregistrée avant l'existence de ce champ.
        /// </summary>
        public int? DureeChoisieMinutes { get; set; }
    }

    public class EvaluationJourViewModel
    {
        public DateTime Date { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public double Note { get; set; }
    }

    public class ControleViewModel
    {
        /// <summary>Sans lui, la fenêtre du calendrier ne peut pas ouvrir la fiche.</summary>
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Sujet { get; set; }

        public DateTime DateControle { get; set; }

        public TimeSpan? HeureControle { get; set; }
    }

    /// <summary>
    /// Un contrôle tel que la section « Mes contrôles » et sa fiche le
    /// montrent : la date, l'échéance, et où en est sa préparation.
    /// </summary>
    public class ControleEleveViewModel : ControleViewModel
    {
        /// <summary>
        /// Calculé SERVEUR, en heure de Paris. Le navigateur ne recalcule
        /// jamais un délai : deux horloges donneraient deux réponses le soir
        /// du réveillon, et c'est le serveur qui a raison.
        /// </summary>
        public int JoursRestants { get; set; }

        public DateTime? DernierePreparationLe { get; set; }

        public int NombrePreparations { get; set; }

        /// <summary>La note obtenue, sur 20. Null tant qu'elle n'est pas connue.</summary>
        public double? Note { get; set; }

        public string? Ressenti { get; set; }

        /// <summary>
        /// Null sur un contrôle passé = le point n'a pas encore été fait, et
        /// le professeur le demandera à la prochaine séance.
        /// </summary>
        public DateTime? BilanLe { get; set; }

        /// <summary>
        /// Vrai quand plus personne ne redemandera comment ça s'est passé :
        /// soit le point a été fait, soit on a déjà posé la question autant de
        /// fois qu'on se l'autorise. L'écran ne doit pas laisser croire à une
        /// attente qui n'aura pas lieu.
        /// </summary>
        public bool BilanClos { get; set; }

        public PreparationViewModel? Preparation { get; set; }
    }

    public class PreparationViewModel
    {
        /// <summary>
        /// L'avancement global, toujours renseigné — 0 compris. C'est la
        /// moyenne des notions, donc la barre du haut et celles du détail ne
        /// peuvent pas se contredire.
        /// </summary>
        public int Pourcent { get; set; }

        public bool PerimetreConnu { get; set; }

        public int Total { get; set; }

        public int Acquises { get; set; }

        /// <summary>
        /// « pas-commence » | « pas-pret » | « bientot » | « pret ».
        ///
        /// TOUJOURS RENSEIGNÉ, ET CALCULÉ AU SERVEUR. L'écran choisit une
        /// couleur et un libellé, jamais la règle : deux écrans la
        /// recomposeraient un jour différemment.
        /// </summary>
        public string? PretStatut { get; set; }

        /// <summary>Ce que le professeur en dit. Null tant qu'il n'a pas tranché.</summary>
        public string? PretObservation { get; set; }

        /// <summary>Quand il a tranché — le verdict ne périme pas, mais il se date.</summary>
        public DateTime? PretLe { get; set; }

        public IEnumerable<NotionControleViewModel> Notions { get; set; } = [];
    }

    public class NotionControleViewModel
    {
        public int Id { get; set; }

        public string? Libelle { get; set; }

        /// <summary>"acquise" | "a-confirmer" | "en-cours" | "fragile" | "a-decouvrir"</summary>
        public string? Etat { get; set; }

        public DateTime? TravailleeLe { get; set; }

        /// <summary>"reussie" | "ratee" | null — ce que la copie corrigée a montré.</summary>
        public string? Resultat { get; set; }

        /// <summary>Où il en est sur cette notion, de 0 à 100.</summary>
        public int Pourcent { get; set; }

        /// <summary>
        /// Validée par une copie notée ou un résultat de contrôle — une mesure,
        /// pas une impression. Une impression ne peut plus la faire baisser.
        /// </summary>
        public bool ValideeParMesure { get; set; }
    }

    public class MatiereViewModel
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? AgentSlug { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        public int Ordre { get; set; }

        /// <summary>
        /// Bornes incluses de l'ordre du niveau où la matière existe. Le front
        /// s'en sert pour ne proposer à l'élève que ce qui figure réellement à
        /// son emploi du temps — un CM1 ne doit pas voir « Physique-Chimie ».
        /// </summary>
        public int NiveauOrdreMin { get; set; }

        /// <inheritdoc cref="NiveauOrdreMin"/>
        public int NiveauOrdreMax { get; set; }

        public bool Active { get; set; }

        /// <summary>
        /// Ce que la matière promet à l enfant, en une phrase.
        ///
        /// « Calculs, problèmes et géométrie ». Un nom de matière seul ne dit
        /// rien à un enfant de neuf ans ; cette phrase lui dit ce qu il va
        /// FAIRE. Elle vient de la base et non du front : une matière se décrit
        /// à un seul endroit.
        /// </summary>
        public string? Promesse { get; set; }

        /// <summary>
        /// Les classes où cette matière ne figure pas, malgré un rang d année
        /// compris dans ses bornes.
        ///
        /// Les bornes ne suffisent plus depuis que trois classes partagent le
        /// même rang : la terminale professionnelle a du français et pas de
        /// philosophie, la terminale générale l inverse. Voir
        /// `VoiesScolaires`.
        /// </summary>
        public string[] NiveauCodesExclus { get; set; } = [];
    }

    /// <summary>
    /// Un professeur, tel que la page d accueil le présente.
    ///
    /// POURQUOI CE MODÈLE EXISTE PLUTÔT QUE DE RÉUTILISER LES MATIÈRES
    /// ---------------------------------------------------------------
    /// L équipe n est pas la liste des matières : un professeur peut en tenir
    /// deux. Yann enseigne les sciences puis la physique-chimie, et il ne faut
    /// pas l afficher deux fois sous deux visages identiques.
    ///
    /// Le regroupement est fait par le SERVEUR parce qu il découle des données
    /// — deux matières portant le même visage sont le même professeur. Laissé
    /// au front, il aurait fallu le refaire à chaque endroit qui montre
    /// l équipe.
    /// </summary>
    public class ProfesseurViewModel
    {
        public string? Prenom { get; set; }

        /// <summary>Le nom du visage à dessiner : nora, adrien, camille...</summary>
        public string? Avatar { get; set; }

        public string? Couleur { get; set; }

        /// <summary>
        /// Ses matières, dans l ordre d affichage. Le front décide du
        /// séparateur : c est de la présentation, elle ne se décide pas ici.
        /// </summary>
        public List<string> Matieres { get; set; } = [];

        /// <summary>
        /// Le code de sa matière principale — la première de la liste. Sert au
        /// motif dessiné en fond de sa carte.
        /// </summary>
        public string? Code { get; set; }
    }
}
