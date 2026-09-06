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

        /// <summary>Date du dernier message analysé, à repousser après traitement.</summary>
        public DateTime Jusqua { get; set; }

        public List<MessageObserve> Messages { get; set; } = [];
    }
}
