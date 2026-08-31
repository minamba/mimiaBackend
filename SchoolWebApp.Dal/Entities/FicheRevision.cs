namespace SchoolWebApp.Dal.Entities
{
    /// <summary>États successifs d'une fiche.</summary>
    public static class EtatFiche
    {
        /// <summary>Notion travaillée, pas encore vérifiée par une évaluation.</summary>
        public const string EnCours = "en_cours";

        /// <summary>Notion évaluée et réussie. La fiche est consolidée.</summary>
        public const string Acquise = "acquise";

        public static string Normaliser(string? valeur) =>
            valeur?.Trim().ToLowerInvariant() switch
            {
                "acquise" or "acquis" or "maitrisee" or "maîtrisée" => Acquise,
                _ => EnCours,
            };
    }

    /// <summary>
    /// Une fiche de révision, rédigée par le professeur sur une notion.
    ///
    /// Une fiche par NOTION et non par séance : trois séances par semaine sur
    /// une année scolaire feraient quatre-vingt-dix documents, et personne ne
    /// révise dans un tas. Revenir sur une notion réécrit sa fiche.
    ///
    /// Rattachée à l'ÉLÈVE et non au référentiel : la fiche reprend les mots
    /// employés pendant SA séance, les exemples qu'il a travaillés, les pièges
    /// sur lesquels il a buté. Une fiche générique servirait à tout le monde
    /// et ne rappellerait rien à personne.
    /// </summary>
    public partial class FicheRevision
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        /// <summary>Séance dont elle est issue. Sert à retrouver le contexte.</summary>
        public int? ConversationId { get; set; }

        /// <summary>Le titre : la notion maîtrisée.</summary>
        public string? Notion { get; set; }

        /// <summary>
        /// Domaine du référentiel — « Géométrie », « Nombres et calculs ».
        /// Sert à regrouper les fiches quand il y en a beaucoup.
        /// </summary>
        public string? Domaine { get; set; }

        /// <summary>
        /// Le corps de la fiche, en texte simple avec des titres et des puces.
        /// Pas de HTML : c'est produit par un modèle, et l'afficher tel quel
        /// ouvrirait une injection dans la page de l'enfant.
        /// </summary>
        public string? Contenu { get; set; }

        /// <summary>
        /// « en_cours » ou « acquise ». Une fiche sur notion fragile porte un
        /// risque : l'enfant se dit « j'ai la fiche, c'est bon » et arrête de
        /// chercher. L'état dit explicitement le contraire.
        /// </summary>
        public string? Etat { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime DateMiseAJour { get; set; }

        /// <summary>
        /// Dernière ouverture par l'élève. Null tant qu'il ne l'a jamais lue.
        ///
        /// C'est la COMPARAISON avec <see cref="DateMiseAJour"/> qui décide de
        /// la pastille, pas un booléen : le professeur réécrit la fiche sans
        /// avoir à penser à rabaisser un drapeau, et une fiche relue après mise
        /// à jour redevient silencieuse d'elle-même.
        /// </summary>
        public DateTime? DateConsultation { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }
    }
}
