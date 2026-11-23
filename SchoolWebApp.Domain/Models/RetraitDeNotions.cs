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
            var motsDuMessage = MotsDuMessage(message);
            var dit = motsDuMessage.Select(m => m.Cle).ToList();
            var notion = MotsPleins(LibelleNotion.CleSouple(libelle));
            if (dit.Count < 2 || notion.Count < 2) return null;

            // Il la nomme entière (il a recopié son libellé, ou tous ses vrais mots — le verbe d'amorce en moins).
            if (NommeeEntiere(dit, libelle)) return null;

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

            // #67 v4 (verdict d'Omar, point 5) : le morceau tel que l'ÉLÈVE l'a écrit (« les espèces amphotères », « temps
            // de demi-réaction »), pas la clé normalisée (« espece amphotere », « temp demi reaction ») : c'est ce que le
            // professeur lui recopie.
            return morceau is null ? null : MotsDOrigine(message!, motsDuMessage, morceau.Split(' '));
        }

        /// <summary>
        /// Le message nomme-t-il la notion ENTIÈRE : tous ses vrais mots, sauf le VERBE D'AMORCE (#67 v4, verdict d'Omar,
        /// point 2) ? « Retire le temps de demi-réaction et la vitesse volumique » nomme toute « Déterminer un temps de
        /// demi-réaction et une vitesse volumique » : personne ne dit « retire le déterminer… ».
        /// </summary>
        public static bool NommeeEntiere(string? message, string? libelle) =>
            NommeeEntiere(MotsDuMessage(message).Select(m => m.Cle).ToList(), libelle);

        private static bool NommeeEntiere(List<string> dit, string? libelle)
        {
            var cle = LibelleNotion.CleSouple(libelle);
            var notion = MotsPleins(cle);
            if (notion.Count == 0) return false;

            // Le verbe d'amorce : le PREMIER mot du libellé, s'il a la forme d'un infinitif (« identifier », « écrire »,
            // « connaître », « savoir ») et qu'il reste au moins deux vrais mots sans lui. « Les nombres décimaux »,
            // « Couple acide-base » n'ont pas de verbe : tous leurs mots comptent.
            var premier = cle.Split(' ')[0];
            var sansAmorce = notion.Count >= 3 && premier.Length >= 4 && premier == notion[0] && Infinitif().IsMatch(premier)
                ? notion.Skip(1).ToList()
                : notion;

            return sansAmorce.All(dit.Contains);
        }

        /// <summary>
        /// « Retire toute la notion », « enlève-la en entier » : l'élève veut la notion ENTIÈRE, même s'il la désigne par
        /// un morceau. Il faut un mot de retrait, un « entier » qui porte sur LA NOTION (#67 v4, point 3 : « enlève
        /// entièrement les espèces amphotères » reste un morceau), et AUCUNE forme qui restreint le RETRAIT (« garde-la en
        /// entier », « ne la retire pas en entier », « pas toute la notion », « seulement », « juste »). Une justification
        /// négative ne restreint rien (#67 v4, point 1) : « on ne l'a pas vue », « on n'a pas fait… », « ce ne sera pas
        /// au bac ».
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

        // « Entier » QUI PORTE SUR LA NOTION (#67 v4, point 3) : « toute la notion », « la notion en entier », « en entier
        // la notion », « retire-la en entier » (le pronom juste après le verbe). « Enlève entièrement les espèces
        // amphotères », « retire en entier les espèces amphotères » : l'adverbe porte sur le MORCEAU, ce n'en est pas un.
        [GeneratedRegex(@" (toute? (la|cette) notion"
            + @"|(la|cette) notion (toute |tout )?(entiere|en entier|entierement|completement|totalement)"
            + @"|(la|cette) notion toute"
            + @"|(entierement|en entier|completement|totalement) (la|cette) notion"
            + @"|(retir|enlev|supprim|ot|vir)\w* (la|le|les|l) (toute |tout )?(entiere|entier|en entier|entierement|completement|totalement)) ")]
        private static partial Regex Entiere();

        // CE QUI RESTREINT LE RETRAIT, et seulement lui (#67 v4, point 1) : la négation du VERBE de retrait (« ne la retire
        // pas », « ne pas l'enlever »), « pas toute / pas en entier / pas la notion », « seulement », « juste »,
        // « uniquement », « sauf », et « garde / conserve / laisse » quand il ne s'agit pas du reste du programme (« garde
        // le reste », « laisse tomber » ne gardent rien de cette notion). Le « pas » d'une JUSTIFICATION (« on ne l'a pas
        // vue », « on n'a pas fait… », « ne seront pas au bac ») n'est pas une restriction.
        [GeneratedRegex(@" ((ne|n) ((la|le|les|l|lui|me|te|en) ){0,2}(retir|enlev|supprim|ot|vir)\w* pas"
            + @"|ne pas ((la|le|les|l) )?(retir|enlev|supprim|oter|vir)\w*"
            + @"|pas (la|le|les|l) (retir|enlev|supprim|oter|vir)\w*"
            + @"|pas (toute?|tout entiere|en entier|entiere?|entierement|completement|totalement|(la|cette) notion)"
            + @"|seulement|juste|uniquement|sauf"
            + @"|(garde|conserve|laisse)\w*(?= )(?! ((tout )?(le |du )?reste|les autres|tomber) )) ")]
        private static partial Regex Restriction();

        /// <summary>Forme d'un infinitif (« identifier », « écrire », « connaître », « savoir ») : le verbe d'amorce.</summary>
        [GeneratedRegex(@"(er|ir|re)$")]
        private static partial Regex Infinitif();

        [GeneratedRegex(@"[\p{L}\p{N}]+")]
        private static partial Regex MotBrut();

        /// <summary>Les vrais mots (4 lettres et plus), dans l'ordre, sans leur « s » ou « x » final.</summary>
        private static List<string> MotsPleins(string cle) =>
            cle.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(m => m.Length >= 4)
                .Select(Singulier)
                .ToList();

        private static string Singulier(string m) => m.Length > 4 && (m.EndsWith('s') || m.EndsWith('x')) ? m[..^1] : m;

        /// <summary>
        /// Les vrais mots du message, dans l'ordre : leur clé (comme <see cref="MotsPleins"/>) ET leur place dans le texte
        /// d'origine, pour rendre le morceau tel que l'élève l'a écrit.
        /// </summary>
        private static List<(string Cle, int Debut, int Fin)> MotsDuMessage(string? message)
        {
            var mots = new List<(string, int, int)>();
            if (string.IsNullOrWhiteSpace(message)) return mots;

            foreach (Match brut in MotBrut().Matches(message))
            {
                foreach (var m in LibelleNotion.CleSouple(brut.Value).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (m.Length >= 4) mots.Add((Singulier(m), brut.Index, brut.Index + brut.Length));
                }
            }

            return mots;
        }

        /// <summary>
        /// Le passage du message qui porte les mots du morceau, mot pour mot dans l'ordre (petits mots et ponctuation
        /// d'origine compris : « temps de demi-réaction ») ; à défaut, les mots d'origine un par un.
        /// </summary>
        private static string MotsDOrigine(string message, List<(string Cle, int Debut, int Fin)> mots, string[] morceau)
        {
            for (var s = 0; s + morceau.Length <= mots.Count; s++)
            {
                if (Enumerable.Range(0, morceau.Length).All(k => mots[s + k].Cle == morceau[k]))
                {
                    var debut = mots[s].Debut;
                    var fin = mots[s + morceau.Length - 1].Fin;
                    return message[debut..fin];
                }
            }

            return string.Join(' ', morceau
                .Select(c => mots.FirstOrDefault(m => m.Cle == c))
                .Select(m => m.Fin > m.Debut ? message[m.Debut..m.Fin] : "")
                .Where(x => x.Length > 0)
                .Distinct());
        }

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
