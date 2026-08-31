namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un document envoyé par l'élève au professeur : la photo de son exercice,
    /// ou le PDF que son enseignant a distribué.
    ///
    /// POURQUOI LES OCTETS SONT EN BASE ET NON SUR LE DISQUE
    /// ----------------------------------------------------
    /// Un fichier sur disque, c'est un chemin à configurer différemment en dev
    /// et sur le VPS, une sauvegarde qui ne le contient pas, et surtout des
    /// orphelins : la ligne supprimée, le fichier reste. Ici la purge d'une
    /// conversation emporte ses documents dans la même transaction, et une
    /// restauration de sauvegarde restitue un état cohérent. Le prix à payer
    /// est la taille de la base — d'où le plafond strict à l'entrée.
    ///
    /// POURQUOI CE N'EST PAS UNE COLONNE DE Message
    /// -------------------------------------------
    /// L'élève joint son fichier AVANT d'écrire sa phrase : la pièce jointe
    /// existe donc pendant un moment sans message auquel se rattacher. D'où
    /// MessageId nullable — et la purge des pièces orphelines, pour celles
    /// qu'on a jointes puis abandonnées sans jamais envoyer.
    /// </summary>
    public partial class PieceJointe
    {
        public int Id { get; set; }

        /// <summary>
        /// La conversation à laquelle le document appartient. Renseignée dès
        /// l'envoi : c'est elle qui porte le contrôle d'accès, avant même
        /// qu'un message existe.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// Le message auquel la pièce a fini par être attachée. Null tant que
        /// l'élève n'a pas envoyé son tour.
        /// </summary>
        public int? MessageId { get; set; }

        /// <summary>Nom d'origine, montré à l'élève. Jamais utilisé comme chemin.</summary>
        public string? NomFichier { get; set; }

        /// <summary>
        /// image/png, image/jpeg, image/gif, image/webp ou application/pdf.
        /// La liste est exactement celle que le modèle sait lire : accepter
        /// autre chose ne servirait qu'à produire une erreur plus tard.
        /// </summary>
        public string? TypeMime { get; set; }

        public int Taille { get; set; }

        /// <summary>
        /// Nombre de pages, pour un PDF. Zéro pour une image. Sert au plafond
        /// d'entrée : une page coûte des milliers de jetons à chaque tour tant
        /// qu'elle reste dans la fenêtre d'historique.
        /// </summary>
        public int NombrePages { get; set; }

        /// <summary>
        /// Les octets du document. Vidés une fois la transcription faite et le
        /// délai de conservation passé — la ligne, elle, subsiste.
        /// </summary>
        public byte[] Donnees { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Ce que le document contient, en texte.
        ///
        /// POURQUOI ON GARDE LE TEXTE ET PAS L'IMAGE
        /// ----------------------------------------
        /// Une photo de feuille pèse trois mégaoctets, sa transcription deux
        /// kilo-octets. Mille cinq cents fois moins, pour l'essentiel de ce qui
        /// sert ensuite : dans trois mois, le professeur n'a plus besoin de
        /// VOIR la feuille, il a besoin de savoir sur quel exercice on avait
        /// travaillé.
        ///
        /// C'est aussi la bonne réponse côté vie privée : au-delà de quelques
        /// jours, conserver l'écriture manuscrite d'un enfant et son nom en
        /// haut de la copie n'a plus aucune justification.
        ///
        /// L'image, elle, reste indispensable PENDANT la séance : une figure de
        /// géométrie, un graphique à lire ou la copie de l'élève — où il a mis
        /// la virgule, ce qu'il a raturé — ne survivent pas à une transcription.
        /// D'où les deux, chacun sur sa durée.
        /// </summary>
        public string? Transcription { get; set; }

        /// <summary>
        /// Quand les octets ont été effacés. Null tant que le document est
        /// encore consultable.
        ///
        /// Une date plutôt qu'un booléen, et plutôt que de déduire l'état d'un
        /// tableau vide : on veut pouvoir répondre « effacé le 14 mars » à un
        /// parent qui exerce son droit à l'information.
        /// </summary>
        public DateTime? DonneesEffaceesLe { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Conversation? Conversation { get; set; }

        public virtual Message? Message { get; set; }
    }
}
