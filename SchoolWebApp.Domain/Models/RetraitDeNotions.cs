namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Pourquoi une demande de retrait (champ `retirer:` de [CONTROLE_NOTIONS], signalement #67) n'a rien supprimé.
    /// </summary>
    public enum MotifRefusRetrait
    {
        /// <summary>Aucune ligne du programme ne répond : rien à dire à l'élève (déjà retirée, ou mal recopiée).</summary>
        Introuvable,

        /// <summary>
        /// La demande ne nomme qu'UNE PARTIE d'une notion du programme (« espèce amphotère » dans « Identifier un couple
        /// acide-base de Brønsted, une espèce amphotère… »), ou une ligne trop courte pour être sûre (« Les fractions »
        /// dans « comparer les fractions décimales ») : une notion se garde ou se retire ENTIÈRE.
        /// </summary>
        Partiel,

        /// <summary>Le retrait laisserait le programme du contrôle VIDE : il n'est pas appliqué.</summary>
        Plancher,

        /// <summary>
        /// Le même bloc pose ET retire la notion (`notions:` et `retirer:`) : on ne sait pas ce que l'élève veut, la
        /// notion RESTE.
        /// </summary>
        Conflit,
    }

    /// <summary>Une demande de retrait refusée, et la ligne entière du programme qu'elle visait (s'il y en a une).</summary>
    public sealed record RetraitRefuse(string Demande, MotifRefusRetrait Motif, string? Notion = null);

    /// <summary>
    /// Une ligne du programme supprimée par un retrait, avec tout ce qu'il faut pour la RESTAURER à l'identique
    /// (journal restaurable, verdict d'Omar sur le #67).
    /// </summary>
    public sealed record NotionRetiree(
        int Id, int ControleId, int? CompetenceId, string Libelle, string Source,
        DateTime? TravailleeLe, string? EtatProfesseur, DateTime? EtatProfesseurLe, DateTime DateCreation);

    /// <summary>Ce qu'un retrait a fait (ou ferait, en simulation) au programme d'un contrôle.</summary>
    public sealed record BilanRetrait(IReadOnlyList<NotionRetiree> Retirees, IReadOnlyList<RetraitRefuse> Refus)
    {
        public static readonly BilanRetrait Vide = new([], []);

        /// <summary>Les refus que le professeur doit expliquer à l'élève (tout sauf « introuvable »).</summary>
        public IEnumerable<RetraitRefuse> RefusADire => Refus.Where(r => r.Motif != MotifRefusRetrait.Introuvable);
    }
}
