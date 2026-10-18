namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN BROUILLON DE LA MESSAGERIE (17/10/2026) — Camara : « tant qu'on ne l'a
    /// pas envoyé, qu'il soit sauvegardé en tant que brouillon ». Enregistré au fil
    /// de la frappe : en base plutôt que dans le dossier « Drafts » de LWS, qui
    /// coûterait une connexion IMAP toutes les deux secondes (l'hébergeur borne
    /// les connexions par compte).
    ///
    /// À SON AUTEUR, même sur la boîte commune du support : chacun ses brouillons.
    /// </summary>
    public partial class BrouillonMail
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        /// <summary>« moi » (sa boîte LWS) ou « support » (la boîte commune).</summary>
        public string Boite { get; set; } = null!;

        /// <summary>
        /// L'identifiant IMAP du message auquel on répond, dans la boîte de
        /// réception ; nul pour un message écrit depuis « Écrire à un parent ».
        /// </summary>
        public long? MessageOrigine { get; set; }

        public string? Destinataire { get; set; }

        public string? Sujet { get; set; }

        /// <summary>Le titre affiché dans le message (« Écrire à un parent ») ; vide = l'objet.</summary>
        public string? Titre { get; set; }

        public string Texte { get; set; } = null!;

        public DateTime DateModification { get; set; }

        public virtual Parent? Parent { get; set; }

        /// <summary>Ses images et documents (18/10/2026), rangés sur le disque.</summary>
        public virtual ICollection<PieceBrouillonMail> Pieces { get; set; } = new List<PieceBrouillonMail>();
    }
}
