namespace SchoolWebApp.Domain.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        /// <summary>user | assistant</summary>
        public string? Role { get; set; }

        public string? Contenu { get; set; }

        public string? Modele { get; set; }

        public int TokensEntree { get; set; }

        public int TokensSortie { get; set; }

        public int TokensCacheLecture { get; set; }

        public int TokensCacheEcriture { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
