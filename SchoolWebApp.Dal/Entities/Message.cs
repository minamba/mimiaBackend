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

        /// <summary>Tokens écrits dans le cache de prompt (facturés ~1,25x).</summary>
        public int TokensCacheEcriture { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Conversation? Conversation { get; set; }
    }
}
