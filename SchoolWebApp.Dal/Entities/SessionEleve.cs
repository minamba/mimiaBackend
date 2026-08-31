namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// La session d'un enfant sur un appareil.
    ///
    /// POURQUOI UNE TABLE ET NON UN JETON SIGNÉ
    /// ---------------------------------------
    /// Un JWT est autoportant : le serveur n'a rien à consulter pour le croire,
    /// et rien pour le refuser avant son expiration. Or on doit pouvoir couper
    /// un accès À LA SECONDE — le parent suspend l'enfant, ou régénère son
    /// code, et l'appareil doit se fermer. Une ligne en base se supprime ; un
    /// jeton signé, non.
    ///
    /// LE JETON EST HACHÉ, LUI
    /// ----------------------
    /// Contrairement au code d'accès, personne n'a jamais besoin de RELIRE un
    /// jeton de session : l'appareil le garde, le parent ne le voit pas. On
    /// peut donc le hacher, et une fuite de la base ne livrerait aucune session
    /// active.
    /// </summary>
    public partial class SessionEleve
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>Le jeton porté par l'appareil, haché en SHA-256.</summary>
        public string JetonHache { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Dernier appel reçu de cet appareil.
        ///
        /// Sert au parent : « Bilal travaille depuis un appareil, vu il y a
        /// deux minutes ». C'est ce qui rend la liste des sessions lisible au
        /// lieu d'être une suite d'identifiants.
        /// </summary>
        public DateTime DernierAcces { get; set; }

        /// <summary>
        /// Ce que le navigateur dit de lui-même, tronqué.
        ///
        /// Assez pour reconnaître « l'iPad » du « PC du salon » quand il faudra
        /// en révoquer un. Pas une identification, un repère.
        /// </summary>
        public string? Appareil { get; set; }

        public virtual Eleve? Eleve { get; set; }
    }
}
