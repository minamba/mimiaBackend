namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// LA BOÎTE MAIL D'UN ADMINISTRATEUR (17/10/2026) — Camara : « que les
    /// messages soient envoyés sous son mail LWS ». Une par compte : son adresse
    /// chez LWS et son mot de passe CHIFFRÉ (voir `ChiffrementBoites`), demandés
    /// à la première ouverture de l'onglet Mails.
    /// </summary>
    public partial class BoiteMailAdmin
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public string Adresse { get; set; } = null!;

        /// <summary>Nonce, étiquette et texte chiffré, en base64 — jamais le mot de passe en clair.</summary>
        public string MotDePasseChiffre { get; set; } = null!;

        public DateTime DateCreation { get; set; }

        public DateTime DateModification { get; set; }

        public virtual Parent? Parent { get; set; }
    }
}
