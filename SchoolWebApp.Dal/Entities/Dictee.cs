namespace SchoolWebApp.Dal.Entities
{
    public static class EtatDictee
    {
        /// <summary>Sa copie est arrivée, mais la correction n'a pas encore eu lieu.</summary>
        public const string EnAttente = "en_attente";

        /// <summary>La correction a été faite, l'observation du professeur est la vraie.</summary>
        public const string Corrigee = "corrigee";

        public static string Normaliser(string? valeur) =>
            valeur?.Trim().ToLowerInvariant() switch
            {
                "en_attente" or "attente" or "non_corrigee" or "pas_corrigee" or "en_cours" => EnAttente,
                _ => Corrigee,
            };
    }

    /// <summary>
    /// Une dictée archivée, avec le texte dicté et la copie de l'élève.
    ///
    /// UNE LIGNE PEUT ÊTRE MISE À JOUR, comme <see cref="FicheRevision"/> —
    /// et pas seulement créée une fois comme <see cref="Evaluation"/>. Une
    /// dictée dont la copie est reçue mais pas encore corrigée s'archive
    /// quand même, avec <see cref="EtatDictee.EnAttente"/> ; la correction,
    /// venue peut-être une séance plus tard, RÉÉCRIT LA MÊME LIGNE au lieu
    /// d'en ouvrir une seconde. Voir <c>DicteeRepository.AjouterAsync</c>
    /// pour le rapprochement — sur le texte dicté, pas sur un identifiant
    /// que le modèle ne connaît pas.
    /// </summary>
    public partial class Dictee
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        /// <summary>Séance pendant laquelle la dictée a été archivée pour la première fois.</summary>
        public int ConversationId { get; set; }

        /// <summary>Ce que travaillait la dictée, en quelques mots. Facultatif.</summary>
        public string? Titre { get; set; }

        /// <summary>Le texte tel que le professeur l'a dicté. Sert aussi de clé de rapprochement.</summary>
        public string TexteDicte { get; set; } = null!;

        /// <summary>Ce que l'élève a écrit — tapé, ou lu sur sa photo.</summary>
        public string Copie { get; set; } = null!;

        /// <summary>Le mot du professeur, lisible par l'élève et par le parent.</summary>
        public string? Remarque { get; set; }

        /// <summary>« en_attente » ou « corrigee ». Voir <see cref="EtatDictee"/>.</summary>
        public string? Etat { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// La dernière fois que cette ligne a été écrite — la première
        /// archive, ou la correction qui l'a mise à jour ensuite. Distincte
        /// de <see cref="DateCreation"/> pour la même raison que sur
        /// <see cref="FicheRevision.DateMiseAJour"/> : c'est elle qui dit si
        /// la fiche mérite un badge « à consulter » après une relecture.
        /// </summary>
        public DateTime DateMiseAJour { get; set; }

        /// <summary>
        /// L'élève ne l'a jamais ouverte tant que cette date est nulle — même
        /// principe que <see cref="FicheRevision.DateConsultation"/>.
        /// </summary>
        public DateTime? DateConsultation { get; set; }

        /// <summary>
        /// LA CLASSE DE L'ÉLÈVE LE JOUR DE LA DICTÉE, ET NON SA CLASSE ACTUELLE.
        /// Même raison que sur <see cref="Evaluation.NiveauScolaireId"/> : nullable,
        /// et définitivement — rien ne permet de reconstituer une classe passée.
        /// </summary>
        public int? NiveauScolaireId { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }
    }
}
