namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Les courriels automatiques connus du planificateur. Le code, et non le
    /// nom affiché, est ce qui les relie au code qui décide à qui ils partent.
    /// </summary>
    public static class CodeModeleMail
    {
        public const string Bilans = "BILANS";
        public const string Relance = "RELANCE";
        public const string Rappels = "RAPPELS";
        public const string FinEssai = "FIN_ESSAI";
        public const string AvisEssai = "AVIS_ESSAI";
        public const string AvisGeneral = "AVIS_GENERAL";

        /// <summary>
        /// Ceux que le planificateur générique envoie lui-même. Les bilans ont
        /// leur propre envoi, étalé sur plusieurs jours.
        /// </summary>
        public static bool EnvoiGenerique(string? code) => code is FinEssai or AvisEssai or AvisGeneral;

        /// <summary>
        /// Relié à du code qui décide à qui il part. Ceux-là ne se suppriment
        /// pas : le planificateur et l'envoi des bilans les cherchent par leur
        /// code, et les supprimer arrêterait un envoi sans rien dire.
        /// </summary>
        public static bool EstRelieAUnEnvoi(string? code) =>
            code is Bilans or FinEssai or AvisEssai or AvisGeneral;

        /// <summary>
        /// Un courriel automatique SANS règle d'envoi : Relance, Rappels — Camara,
        /// le 15/09/2026, « ajoute juste le menu » — et tous ceux créés depuis
        /// l'écran. Modifiable, supprimable, mais impossible à programmer tant
        /// que personne n'a écrit à qui il part.
        /// </summary>
        public static bool ReglesADefinir(string? code) => !EstRelieAUnEnvoi(code);

        /// <summary>
        /// La catégorie de désabonnement d'un courriel, ou nulle s'il fait
        /// partie du service (bilans, fin d'essai) et part à tous.
        /// </summary>
        public static string? Categorie(string? code) => code switch
        {
            AvisEssai or AvisGeneral => CategorieDesabonnement.Avis,
            Relance => CategorieDesabonnement.Relance,
            Rappels => CategorieDesabonnement.Rappels,
            _ => null,
        };
    }

    /// <summary>Les catégories dont un parent peut se désabonner.</summary>
    public static class CategorieDesabonnement
    {
        public const string Avis = "avis";
        public const string Relance = "relance";
        public const string Rappels = "rappels";
        public const string Diffusion = "diffusion";

        public static bool EstConnue(string? categorie) => categorie is Avis or Relance or Rappels or Diffusion;

        /// <summary>Ce que la page de désabonnement dit en toutes lettres.</summary>
        public static string Libelle(string categorie) => categorie switch
        {
            Avis => "les demandes d’avis",
            Relance => "les relances",
            Rappels => "les rappels",
            Diffusion => "les annonces de Mimia",
            _ => "ces messages",
        };
    }

    public static class StatutEnvoiAutomatique
    {
        /// <summary>Écrit avant d'envoyer : la place est prise, même si l'envoi échoue.</summary>
        public const string Reserve = "Reserve";
        public const string Envoye = "Envoye";
        public const string Echec = "Echec";
    }

    /// <summary>Un parent à qui un courriel automatique doit partir.</summary>
    public record DestinataireAutomatique(
        int ParentId,
        string Mail,
        string? Prenom,
        int? AbonnementId = null,
        DateTime? DateFinEssai = null);
}
