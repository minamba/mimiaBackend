namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Ce que le professeur a constaté sur une compétence pendant une séance.
    /// Trois valeurs et pas une note : un élève qui hésite puis trouve n'est ni
    /// en échec ni en réussite, et cette nuance est ce qui rend le suivi utile.
    /// </summary>
    public enum ResultatObservation
    {
        Echoue = 0,
        Hesitant = 1,
        Reussi = 2,
    }

    /// <summary>Une observation isolée, rattachée à une compétence du référentiel.</summary>
    public record ObservationCompetence(string Code, ResultatObservation Resultat, string? Indice = null);

    /// <summary>Compétence proposée à l'agent observateur comme rattachement possible.</summary>
    public class CompetenceCandidate
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? Domaine { get; set; }

        public string? NiveauLibelle { get; set; }

        /// <summary>
        /// Code de la classe à laquelle la compétence est rattachée
        /// (TERMINALE, TERMINALE_PRO...). Le rang ne suffit pas à distinguer
        /// les voies : elles le partagent.
        /// </summary>
        public string? NiveauCode { get; set; }

        public int NiveauOrdre { get; set; }
    }

    /// <summary>Un message de la séance, réduit à ce dont l'observateur a besoin.</summary>
    public record MessageObserve(string Role, string Contenu, DateTime Date);

    /// <summary>
    /// Une séance terminée dont les échanges n'ont pas encore été analysés.
    /// </summary>
    public class SeanceAObserver
    {
        public int ConversationId { get; set; }

        public int EleveId { get; set; }

        public string? ElevePrenom { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? NiveauLibelle { get; set; }

        /// <summary>Rang du niveau (1 = CP … 12 = Terminale) : sert à borner les candidats.</summary>
        public int NiveauOrdre { get; set; }

        /// <summary>
        /// Code de la classe de l'élève. Le rang borne les candidats ; le code
        /// dit de quelle VOIE ils doivent venir, puisque trois classes se
        /// partagent le même rang au lycée.
        /// </summary>
        public string? NiveauCode { get; set; }

        /// <summary>
        /// D'où venait l'élève — voir <see cref="ModesSeance"/>. Le filet de
        /// conclusion ne juge une préparation QUE sur une séance ouverte par le
        /// bouton de ce contrôle ou de cette épreuve.
        /// </summary>
        public string? ModeSeance { get; set; }

        public int? ModeControleId { get; set; }

        public string? ModeEpreuveCode { get; set; }

        /// <summary>Date du dernier message analysé, à repousser après traitement.</summary>
        public DateTime Jusqua { get; set; }

        public List<MessageObserve> Messages { get; set; } = [];

        /// <summary>
        /// Les exercices notés archivés pendant cette séance : évaluations,
        /// dictées, compréhensions orales.
        ///
        /// SÉPARÉS DES MESSAGES, ET POUR UNE RAISON DE FOND : l archive est la
        /// source de vérité, le message ne l est pas. Relevé le 10/09/2026 en
        /// base : quinze compréhensions orales archivées pour cinq blocs
        /// présents dans les messages, deux dictées pour aucun bloc. Le
        /// rattrapage reconstitue ce que le professeur a oublié de poser, et
        /// c est très bien — mais lire les messages pour retrouver les
        /// exercices notés en aurait raté les deux tiers.
        /// </summary>
        public List<ExerciceObserve> Exercices { get; set; } = [];
    }

    /// <summary>
    /// Un exercice noté tel qu il a été archivé : ce qui était attendu, ce que
    /// l élève a produit, et le verdict quand il y en a un.
    ///
    /// Une seule forme pour les trois genres, parce que l observateur en fait
    /// la même chose : les rattacher à des compétences. Ce qui change d un
    /// genre à l autre, c est quels champs sont remplis.
    /// </summary>
    public class ExerciceObserve
    {
        /// <summary>« évaluation », « dictée » ou « compréhension orale ».</summary>
        public string Genre { get; set; } = "";

        public string? Titre { get; set; }

        /// <summary>Sur 20. Seule l évaluation en porte une.</summary>
        public double? Note { get; set; }

        /// <summary>Le texte dicté, ou le passage entendu. Vide pour une évaluation.</summary>
        public string? Attendu { get; set; }

        /// <summary>La copie de l élève, ou ce qu il a dit du passage.</summary>
        public string? Production { get; set; }

        /// <summary>Ce qui reste à revoir, ou ce que le professeur a retenu.</summary>
        public string? Bilan { get; set; }

        /// <summary>Code de langue d une compréhension orale. Null ailleurs.</summary>
        public string? Langue { get; set; }

        public DateTime Date { get; set; }

        /// <summary>Le détail question par question d une évaluation.</summary>
        public List<QuestionEvaluation> Questions { get; set; } = [];
    }
}
