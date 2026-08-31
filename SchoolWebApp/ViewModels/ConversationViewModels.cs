namespace SchoolWebApp.Api.ViewModels
{
    public class ConversationViewModel
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereCode { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Titre { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateDernierMessage { get; set; }
    }

    public class MessageViewModel
    {
        public int Id { get; set; }

        public string? Role { get; set; }

        public string? Contenu { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Le document joint à ce message, s'il y en a un. Métadonnées
        /// seulement : les octets se récupèrent par leur propre route, pour
        /// que la liste des messages ne transporte pas plusieurs mégaoctets
        /// que le navigateur n'affichera peut-être jamais.
        /// </summary>
        public PieceJointeViewModel? PieceJointe { get; set; }

        // Les compteurs de tokens ne sont pas exposés : ils servent au suivi
        // de coût côté opérateur, pas à l'élève.
    }

    /// <summary>Ce que le navigateur sait d'un document, sans son contenu.</summary>
    public class PieceJointeViewModel
    {
        public int Id { get; set; }

        public string? NomFichier { get; set; }

        public string? TypeMime { get; set; }

        public int Taille { get; set; }

        /// <summary>Zéro pour une image.</summary>
        public int NombrePages { get; set; }

        /// <summary>
        /// Les octets sont-ils encore là ? Au-delà de quelques jours ils sont
        /// effacés et seule la transcription subsiste — le navigateur affiche
        /// alors une mention plutôt qu'une image cassée.
        /// </summary>
        public bool Consultable { get; set; }

        /// <summary>
        /// Une image s'affiche en vignette ; un PDF se montre par son nom et
        /// son nombre de pages. Calculé ici plutôt que déduit du type MIME
        /// dans le navigateur : la règle appartient au serveur, qui est aussi
        /// celui qui décide des formats acceptés.
        /// </summary>
        public bool EstImage { get; set; }
    }
}
