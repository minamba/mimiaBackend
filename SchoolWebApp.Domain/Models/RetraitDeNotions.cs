using System.Text.RegularExpressions;

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

    /// <summary>
    /// L'ÉLÈVE N'A NOMMÉ QU'UN MORCEAU D'UNE NOTION — verdict d'Omar sur le #67 v2 (A 1/8) : « retire les espèces
    /// amphotères », et le professeur écrit lui-même dans `retirer:` le libellé COMPLET de « Identifier un couple
    /// acide-base de Brønsted, une espèce amphotère, et écrire la réaction associée ». Le libellé est exact, le serveur
    /// obéissait. Ce qui trahit le morceau, c'est le message de l'élève : il nomme deux vrais mots qui se suivent dans
    /// cette notion, et pas la notion entière.
    ///
    /// Sert au dépôt (le retrait de la ligne entière est refusé, motif <see cref="MotifRefusRetrait.Partiel"/>) et au
    /// rappel du tour (le professeur l'apprend AVANT de répondre).
    /// </summary>
    public static partial class MorceauDeNotion
    {
        /// <summary>
        /// Le morceau de <paramref name="libelle"/> que nomme <paramref name="message"/> (mots ramenés à leur clé,
        /// au singulier), ou null :
        /// - le message contient au moins DEUX VRAIS MOTS (4 lettres et plus) qui se suivent dans la notion (les petits
        ///   mots entre eux ne comptent pas : « espèce amphotère », « temps de demi-réaction ») ;
        /// - il ne contient pas TOUS les vrais mots de la notion (sinon il la nomme entière) ;
        /// - ces deux mots ne se suivent dans AUCUNE autre ligne du programme (sinon c'est un THÈME, « dosage par
        ///   titrage » dans deux lignes : l'élève retire tout le thème, c'est légitime) ;
        /// - il ne demande pas expressément la notion ENTIÈRE (<see cref="DemandeLaNotionEntiere"/>).
        /// </summary>
        public static string? Nomme(string? message, string? libelle, IEnumerable<string?> autresLignes)
        {
            var dit = MotsPleins(LibelleNotion.CleSouple(message));
            var notion = MotsPleins(LibelleNotion.CleSouple(libelle));
            if (dit.Count < 2 || notion.Count < 2) return null;

            // Il la nomme entière (il a recopié son libellé, ou tous ses vrais mots).
            if (notion.All(dit.Contains)) return null;

            if (DemandeLaNotionEntiere(message)) return null;

            var paires = Paires(dit);
            var autres = autresLignes
                .SelectMany(l => Paires(MotsPleins(LibelleNotion.CleSouple(l))))
                .ToHashSet();

            // Le plus long passage de la notion que le message reprend mot à mot (deux vrais mots au moins).
            string? morceau = null;
            for (var i = 0; i < notion.Count - 1; i++)
            {
                var j = i;
                while (j < notion.Count - 1 && paires.Contains((notion[j], notion[j + 1]))) j++;
                if (j == i) continue;

                var passage = notion.Skip(i).Take(j - i + 1).ToList();
                var propre = Enumerable.Range(0, passage.Count - 1).Any(k => !autres.Contains((passage[k], passage[k + 1])));
                if (propre && (morceau is null || passage.Count > morceau.Split(' ').Length))
                {
                    morceau = string.Join(' ', passage);
                }

                i = j - 1;
            }

            return morceau;
        }

        /// <summary>
        /// « Retire toute la notion », « enlève-la en entier » : l'élève veut la notion ENTIÈRE, même s'il la désigne par
        /// un morceau. Il faut un mot de retrait ET un mot d'entier, et AUCUN mot qui garde ou restreint (« garde-la en
        /// entier », « ne la retire pas en entier », « retire seulement les amphotères, pas la notion entière »).
        /// </summary>
        public static bool DemandeLaNotionEntiere(string? message)
        {
            var cle = $" {LibelleNotion.CleSouple(message)} ";
            return Retrait().IsMatch(cle) && Entiere().IsMatch(cle) && !Restriction().IsMatch(cle);
        }

        /// <summary>
        /// Le message parle-t-il de sortir quelque chose du programme (« retire », « enlève », « on ne l'a pas vu »,
        /// « ce ne sera pas au bac blanc ») ? Une question de cours qui nomme un morceau de notion n'en parle pas.
        /// </summary>
        public static bool ParleDeRetrait(string? message) =>
            IntentionDeRetrait().IsMatch($" {LibelleNotion.CleSouple(message)} ");

        [GeneratedRegex(@" (retir\w*|enlev\w*|supprim\w*|ote\w*|oter|vire\w*|hors programme|pas au programme|pas dans le programme|pas vu\w*|ne sera pas|ne seront pas|pas a l examen|pas au bac|pas au brevet) ")]
        private static partial Regex IntentionDeRetrait();

        [GeneratedRegex(@" (retire\w*|enleve\w*|supprime\w*|ote\w*|vire\w*) ")]
        private static partial Regex Retrait();

        [GeneratedRegex(@" (entiere|entier|entierement|toute la notion|la notion toute) ")]
        private static partial Regex Entiere();

        [GeneratedRegex(@" (garde\w*|conserve\w*|laisse\w*|pas|seulement|juste|uniquement|sauf) ")]
        private static partial Regex Restriction();

        /// <summary>Les vrais mots (4 lettres et plus), dans l'ordre, sans leur « s » ou « x » final.</summary>
        private static List<string> MotsPleins(string cle) =>
            cle.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(m => m.Length >= 4)
                .Select(m => m.Length > 4 && (m.EndsWith('s') || m.EndsWith('x')) ? m[..^1] : m)
                .ToList();

        private static HashSet<(string, string)> Paires(List<string> mots) =>
            Enumerable.Range(0, Math.Max(0, mots.Count - 1)).Select(i => (mots[i], mots[i + 1])).ToHashSet();
    }

    /// <summary>Ce qu'un retrait a fait (ou ferait, en simulation) au programme d'un contrôle.</summary>
    public sealed record BilanRetrait(IReadOnlyList<NotionRetiree> Retirees, IReadOnlyList<RetraitRefuse> Refus)
    {
        public static readonly BilanRetrait Vide = new([], []);

        /// <summary>Les refus que le professeur doit expliquer à l'élève (tout sauf « introuvable »).</summary>
        public IEnumerable<RetraitRefuse> RefusADire => Refus.Where(r => r.Motif != MotifRefusRetrait.Introuvable);
    }
}
