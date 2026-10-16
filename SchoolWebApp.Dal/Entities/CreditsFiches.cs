namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UNE FICHE DE RÉVISION GÉNÉRÉE, TELLE QUE LE QUOTA LA COMPTE —
    /// Camara, le 15/10/2026.
    ///
    /// Un registre à part, et non le compte des fiches existantes : une fiche
    /// supprimée, ou un cours effacé, ne doit pas rendre de place dans les
    /// vingt du mois. Sans aucun contenu : un élève, une date, une imputation.
    /// </summary>
    public partial class FicheConsommee
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>La fiche produite ; elle peut avoir été supprimée depuis, pas de clé.</summary>
        public int? FicheId { get; set; }

        /// <summary>« mois », « credit » ou « depassement » (voir `ImputationFiche`).</summary>
        public string Imputation { get; set; } = null!;

        public DateTime DateCreation { get; set; }

        public virtual Eleve? Eleve { get; set; }
    }

    /// <summary>Le solde de crédits de fiches d'une famille. Il n'expire jamais.</summary>
    public partial class CreditFichesFamille
    {
        public int ParentId { get; set; }

        public int Solde { get; set; }

        public DateTime DateModification { get; set; }

        public virtual Parent? Parent { get; set; }
    }

    /// <summary>
    /// Un mouvement de crédits de fiches d'une famille : un pack acheté (ou
    /// crédité à un compte exempté), ou un ajustement à la main (code
    /// AJUSTEMENT, `Fiches` négatif pour un retrait).
    /// </summary>
    public partial class AchatFiches
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public string Code { get; set; } = null!;

        public int Fiches { get; set; }

        public int PrixCentimes { get; set; }

        public DateTime DateAchat { get; set; }

        /// <summary>Unique quand renseignée : c'est la base qui refuse le double crédit.</summary>
        public string? StripeSessionId { get; set; }

        public string? StripePaiementId { get; set; }

        public DateTime? DateRemboursement { get; set; }

        /// <summary>
        /// Pourquoi, pour un ajustement fait à la main par l'administration
        /// (code AJUSTEMENT, 16/10/2026). Null pour un achat.
        /// </summary>
        public string? Motif { get; set; }

        public virtual Parent? Parent { get; set; }
    }

    /// <summary>Un pack de fiches au catalogue. Paiement unique.</summary>
    public partial class OffreFiches
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string? Libelle { get; set; }

        public int Fiches { get; set; }

        public string? StripePrixId { get; set; }

        public int PrixCentimes { get; set; }

        public int Ordre { get; set; }

        public bool Active { get; set; }
    }
}
