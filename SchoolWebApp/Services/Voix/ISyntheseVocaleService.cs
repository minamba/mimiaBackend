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
        Task<byte[]> SynthetiserAsync(
            string texte, string? avatar, int age, bool dictee = false, CancellationToken ct = default);

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
            bool dictee = false, CancellationToken ct = default);
    }
}
