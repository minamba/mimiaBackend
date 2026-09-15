namespace SchoolWebApp.Api.Services.Voix
{
    public interface ISyntheseVocaleService
    {
        /// <summary>Vrai si une clé est configurée : sinon le front reste sur la voix du navigateur.</summary>
        bool Disponible { get; }

        /// <summary>
        /// Produit l'audio d'une phrase, dans la voix du professeur indiqué.
        /// </summary>
        /// <param name="texte">Une phrase, pas un paragraphe : on synthétise au fil du flux.</param>
        /// <param name="avatar">Identifiant du professeur (nora, adrien, salim, chloe, yann).</param>
        /// <param name="age">Âge de l'élève : pilote le débit et le ton.</param>
        /// <param name="dictee">
        /// Vrai quand le passage est DICTÉ. Le débit n'est alors plus celui
        /// d'une conversation : lent, régulier, avec de vrais silences entre
        /// les groupes de souffle. Cette consigne REMPLACE le registre lié à
        /// l'âge — celui du collégien dit « ne ralentis pas », ce qui est
        /// exactement l'inverse de ce qu'il faut ici.
        /// </param>
        /// <param name="langue">
        /// Code de la langue dans laquelle CE passage se prononce — "en",
        /// "es", "de", "it", "zh" — ou null pour le registre habituel.
        ///
        /// UN PROFESSEUR DE LANGUE EXPLIQUE EN FRANÇAIS ET FAIT PRATIQUER
        /// DANS LA LANGUE ÉTUDIÉE : seuls les passages qu'il borne lui-même
        /// (une balise par langue — voir <c>PromptsPedagogiques.EnseignerUneLangue</c>)
        /// portent ce code, phrase par phrase. Sans lui, la synthèse lit tout
        /// avec le registre habituel, et un mot en langue étrangère y devient
        /// une fausse prononciation apprise.
        ///
        /// UN CODE INCONNU N'EST PAS UNE ERREUR : il retombe sur le registre
        /// habituel plutôt que d'échouer. C'est ce qui permet d'ajouter une
        /// langue côté prompt sans toucher ce service tant que sa consigne de
        /// prononciation n'a pas encore été écrite.
        /// </param>
        /// <param name="vitesse">
        /// La vitesse choisie par l'élève avant un exercice d'écoute :
        /// "tres_lent", "lent", "normal", "rapide" — null ailleurs.
        ///
        /// Voulu par Camara le 12/09/2026. Elle ne concerne que les passages
        /// dans la langue étudiée : ralentir une explication déjà comprise
        /// n'aide personne et allonge la séance. Une valeur inconnue vaut
        /// "normal".
        /// </param>
        Task<byte[]> SynthetiserAsync(
            string texte, string? avatar, int age, bool dictee = false,
            string? langue = null, string? vitesse = null, CancellationToken ct = default);

        /// <summary>
        /// Même chose, mais le résultat est un fichier WAV complet — jouable
        /// tel quel par un <c>&lt;audio&gt;</c> de navigateur, contrairement
        /// au flux PCM brut de <see cref="SynthetiserAsync"/>.
        ///
        /// RÉSERVÉ À L'ARCHIVAGE, PAS AU DIRECT : l'en-tête WAV a besoin de
        /// connaître la taille totale avant de l'écrire, ce qui suppose
        /// d'attendre la synthèse complète — exactement l'attente que la
        /// voix en direct cherche à éviter phrase par phrase.
        /// </summary>
        Task<byte[]> SynthetiserWavAsync(
            string texte, string? avatar, int age, bool dictee = false,
            string? langue = null, string? vitesse = null, CancellationToken ct = default);

        /// <summary>
        /// Même chose, mais recopiée vers <paramref name="destination"/> au fil
        /// de l'arrivée, sans jamais tenir l'audio complet en mémoire.
        ///
        /// C'est la variante à utiliser pour répondre à un client : la version
        /// qui rend un tableau d'octets attend qu'OpenAI ait fini de produire
        /// tout le passage avant d'envoyer le premier octet, et cette attente
        /// s'ajoutait telle quelle au silence entendu par l'élève.
        /// </summary>
        Task CopierAudioAsync(
            string texte, string? avatar, int age, Stream destination,
            bool dictee = false, string? langue = null, string? vitesse = null,
            CancellationToken ct = default);
    }
}
