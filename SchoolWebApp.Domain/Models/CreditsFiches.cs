namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES CRÉDITS DE FICHES DE LA FAMILLE — Camara, le 15/10/2026.
    ///
    /// Achetés par le parent (5,99 € les 10, 9,99 € les 20), ils appartiennent
    /// à la FAMILLE — n'importe lequel de ses enfants s'en sert — et
    /// n'expirent jamais. Ils ne servent qu'une fois les vingt fiches du mois
    /// de l'enfant épuisées.
    ///
    /// Ils ne pèsent rien en base : un solde par famille, une ligne par achat.
    /// </summary>
    public static class CreditsFiches
    {
        /// <summary>La valeur de l'étiquette `type` d'une session Stripe qui vend des fiches.</summary>
        public const string TypeStripe = "fiches";

        /// <summary>Le code d'un mouvement fait à la main par l'administration (16/10/2026).</summary>
        public const string CodeAjustement = "AJUSTEMENT";
    }

    /// <summary>
    /// Une ligne de l'historique des crédits d'une famille, pour l'écran
    /// d'ajustement de l'administration : un achat (prix), un crédit offert
    /// (compte exempté), ou un ajustement (motif, crédits négatifs pour un
    /// retrait).
    /// </summary>
    public sealed record LigneCreditsFiches(
        DateTime Date, string Code, int Credits, int PrixCentimes, string? Motif, DateTime? DateRemboursement);

    /// <summary>Un pack de fiches tel que le catalogue le vend.</summary>
    public class OffreFichesTarif
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Libelle { get; set; }
        public int Fiches { get; set; }
        public int PrixCentimes { get; set; }
        public string? StripePrixId { get; set; }
    }

    /// <summary>
    /// Un enfant, ses cours décomptés ce mois-ci et ses cours encore en route,
    /// pour l'encart du forfait du parent.
    /// </summary>
    public sealed record EnfantFiches(int Id, string? Prenom, int Utilisees, int EnCours);

    /// <summary>Comment une fiche générée a été payée.</summary>
    public enum ImputationFiche
    {
        /// <summary>Sur les vingt du mois de l'enfant.</summary>
        Mois,

        /// <summary>Sur les crédits de la famille.</summary>
        Credit,

        /// <summary>
        /// Ni l'un ni l'autre : deux enfants ont pris le dernier crédit au même
        /// instant. La fiche est faite, on ne la jette pas ; elle est notée.
        /// </summary>
        Depassement,
    }
}
