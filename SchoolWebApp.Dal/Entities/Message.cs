namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un tour de conversation. Les compteurs de tokens sont stockés ici :
    /// c'est la seule façon de suivre le coût réel par élève et de détecter
    /// les utilisateurs déficitaires avant qu'ils ne pèsent sur la marge.
    /// </summary>
    public partial class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        /// <summary>user | assistant</summary>
        public string? Role { get; set; }

        public string? Contenu { get; set; }

        /// <summary>Modèle utilisé pour ce tour (claude-opus-5, claude-haiku-4-5...).</summary>
        public string? Modele { get; set; }

        public int TokensEntree { get; set; }

        public int TokensSortie { get; set; }

        /// <summary>Tokens servis depuis le cache de prompt (facturés ~0,1x).</summary>
        public int TokensCacheLecture { get; set; }

        /// <summary>Tokens écrits dans le cache de prompt : 1,25x pour cinq minutes, 2x pour une heure.</summary>
        public int TokensCacheEcriture { get; set; }

        /// <summary>
        /// La part de <see cref="TokensCacheEcriture"/> écrite pour UNE HEURE
        /// (facturée 2x) ; le reste l'a été pour cinq minutes (1,25x). NULL
        /// pour les tours antérieurs au 19/09/2026 : détail inconnu.
        /// </summary>
        public int? TokensCacheEcriture1h { get; set; }

        /// <summary>
        /// Le modèle qui a prêté sa voix à cette réponse (`gpt-4o-mini-tts`,
        /// `gemini-3.8-flash-lite-tts`, `tts-1`). NULL avant le 28/09/2026 :
        /// OpenAI. Sert au coût de la synthèse vocale, par fournisseur.
        /// </summary>
        public string? ModeleVoix { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Conversation? Conversation { get; set; }
    }
}
