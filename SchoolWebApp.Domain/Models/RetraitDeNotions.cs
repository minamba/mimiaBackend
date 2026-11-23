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

    /// <summary>
    /// LE RETRAIT D'UNE NOTION SE FAIT SUR UN OUI, JAMAIS SUR UNE INTERPRÉTATION — signalement #67 v5 (verdict d'Omar sur
    /// f33d9e2, avis du coordinateur). Après quatre versions, chaque assouplissement de l'analyse du texte libre de
    /// l'élève (négations, verbes, morceaux) rouvrait un trou : « retire le temps de demi-réaction mais garde la vitesse
    /// volumique » emportait la notion entière. Désormais le serveur ne retire une notion QUE si :
    /// 1. le DERNIER message du professeur pose la question fermée, avec le libellé EXACT de la notion
    ///    (<see cref="QuestionFermee"/> : « Je retire toute la notion « X » de ton programme ? ») ;
    /// 2. le message de l'élève qui suit est un OUI CLAIR (<see cref="OuiClair"/>).
    /// Tout le reste : rien n'est retiré, et le professeur le dit (le serveur pose lui-même la question fermée quand le
    /// professeur a voulu retirer sans elle). Les règles de refus de <see cref="OuiClair"/> ne peuvent qu'EMPÊCHER un
    /// retrait : aucune phrase de l'élève, à elle seule, ne supprime rien.
    /// </summary>
    public static partial class ConfirmationDeRetrait
    {
        /// <summary>La question fermée, au mot près : c'est elle, et elle seule, qu'un oui confirme.</summary>
        public static string QuestionFermee(string libelle) => $"Je retire toute la notion « {libelle.Trim()} » de ton programme ?";

        /// <summary>
        /// Les libellés dont <paramref name="messageDuProfesseur"/> demande le retrait par la question fermée (dans
        /// l'ordre, sans doublon). Espaces fines ou insécables autour des guillemets acceptées ; rien d'autre ne compte
        /// (« tu veux que je la retire ? » n'est pas la question fermée).
        /// </summary>
        public static IReadOnlyList<string> NotionsDemandees(string? messageDuProfesseur)
        {
            if (string.IsNullOrWhiteSpace(messageDuProfesseur)) return [];

            return Question().Matches(messageDuProfesseur)
                .Select(m => m.Groups["x"].Value.Trim())
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Les libellés qu'un oui CONFIRME dans <paramref name="messageDuProfesseur"/> — #67 v6 (verdict d'Omar, point 4).
        /// Une question fermée ne compte que si :
        /// - elle COMMENCE une phrase (début du message, ou après « . », « ! », « ? », « … » ou un saut de ligne ; gras
        ///   et espaces ignorés) : citée dans une phrase (« je te demanderais : Je retire… ? », « « Je retire… ? » ») ou
        ///   entre guillemets, ce n'est pas une question posée ;
        /// - elle est la DERNIÈRE question du message : aucun « ? » après elle. Plusieurs questions fermées qui se suivent
        ///   à la fin comptent toutes ; une autre question après (« Ou tu préfères qu'on la révise ? ») : rien.
        /// </summary>
        public static IReadOnlyList<string> NotionsConfirmables(string? messageDuProfesseur)
        {
            if (string.IsNullOrWhiteSpace(messageDuProfesseur)) return [];

            var texte = messageDuProfesseur;
            var posees = Question().Matches(texte).Where(m => EnTeteDePhrase(texte, m.Index) && !Citee(texte, m.Index + m.Length))
                .ToList();
            if (posees.Count == 0) return [];

            // La suite de questions fermées qui FINIT le message : chacune se termine au « ? » qui précède la suivante.
            var retenues = new List<string>();
            var attendu = texte.LastIndexOf('?');
            for (var i = posees.Count - 1; i >= 0 && attendu >= 0; i--)
            {
                var m = posees[i];
                if (m.Index + m.Length - 1 != attendu) break;
                retenues.Insert(0, m.Groups["x"].Value.Trim());
                attendu = m.Index == 0 ? -1 : texte.LastIndexOf('?', m.Index - 1);
            }

            return retenues.Where(x => x.Length > 0).Distinct().ToList();
        }

        private static bool EnTeteDePhrase(string texte, int debut)
        {
            var i = debut - 1;
            while (i >= 0 && (char.IsWhiteSpace(texte[i]) && texte[i] != '\n' || texte[i] is '*' or '_' or ' ' or ' '))
            {
                i--;
            }

            return i < 0 || texte[i] is '\n' or '.' or '!' or '?' or '…';
        }

        private static bool Citee(string texte, int fin)
        {
            var i = fin;
            while (i < texte.Length && (texte[i] is ' ' or ' ' or ' ' or '*' or '_')) i++;
            return i < texte.Length && texte[i] is '»' or '"' or '”' or '\'' or '’';
        }

        /// <summary>
        /// LA DÉCISION DU TOUR — #67 v6 (verdict d'Omar, point 5 : le harnais teste la vraie décision). Ce que le serveur
        /// fait du message de l'élève, à partir du DERNIER message du professeur (<paramref name="dernierProfesseur"/>,
        /// null si le dernier message de l'historique n'est pas le sien), du message d'avant
        /// (<paramref name="avantDernierProfesseur"/>, pour nommer la notion seulement) et du programme relu en base.
        /// Seules les notions de <see cref="DecisionDeRetrait.AConfirmer"/> sont passées au dépôt (au mot près).
        /// </summary>
        public static DecisionDeRetrait Decider(
            string? dernierProfesseur, string? avantDernierProfesseur, string? messageEleve, IReadOnlyList<string> programme)
        {
            var confirmables = NotionsConfirmables(dernierProfesseur);
            var cles = programme.Select(LibelleNotion.CleSouple).ToHashSet();

            // UN LIBELLÉ TRONQUÉ COUVERT PAR LA QUESTION EXACTE (#67 v6, point 9 : « Identifier un couple acide-base de
            // Brønsted » suivi de la question au libellé exact ajoutée par le serveur) : seul le libellé exact part au
            // dépôt, le tronqué ne laisse pas de « Introuvable » au journal.
            var exactes = confirmables.Where(c => cles.Contains(LibelleNotion.CleSouple(c))).ToList();
            var aPasser = confirmables
                .Where(c => cles.Contains(LibelleNotion.CleSouple(c)) || !Viser(c, programme).Any(v => exactes.Contains(v)))
                .ToList();

            var nonValables = NotionsDemandees(dernierProfesseur).Concat(NotionsDemandees(avantDernierProfesseur))
                .Where(n => !confirmables.Contains(n)).Distinct().ToList();

            if (confirmables.Count > 0)
            {
                var oui = OuiClair(messageEleve, confirmables, programme);
                return new DecisionDeRetrait(oui ? aPasser : [], confirmables, nonValables, oui, false, []);
            }

            // PAS DE QUESTION FERMÉE VALABLE : rien ne part. S'il demande pourtant un retrait (verbe de retrait non nié,
            // ou un oui à un message qui parlait de retrait), le serveur le dit et nomme la notion s'il n'y en a qu'une.
            var veut = DemandeUnRetrait(messageEleve)
                || (Acquiescement().IsMatch($" {LibelleNotion.CleSouple(messageEleve)} ")
                    && (nonValables.Count > 0 || QuestionDeRetrait(dernierProfesseur)));
            if (!veut) return new DecisionDeRetrait([], [], nonValables, false, false, []);

            var evoquees = NotionsNommees(messageEleve, programme);
            if (evoquees.Count == 0) evoquees = NotionsVisees(dernierProfesseur, programme);
            if (evoquees.Count == 0) evoquees = NotionsVisees(avantDernierProfesseur, programme);

            return new DecisionDeRetrait([], [], nonValables, false, true, evoquees, DemandeUnRetrait(messageEleve));
        }

        /// <summary>
        /// L'élève demande-t-il lui-même un retrait : un verbe de retrait (« retire », « enlève », « supprime », « vire »)
        /// qui n'est pas nié (« ne la retire pas »).
        /// </summary>
        public static bool DemandeUnRetrait(string? message)
        {
            var cle = $" {LibelleNotion.CleSouple(message)} ";
            return VerbeDeRetrait().IsMatch(cle) && !RetraitNie().IsMatch(cle);
        }

        /// <summary>
        /// Les notions du programme que <paramref name="texte"/> NOMME par un mot qui n'appartient qu'à elle (« les
        /// titrages », « les espèces amphotères ») ; vide si aucune, ou si le texte n'en nomme aucune seule.
        /// </summary>
        public static IReadOnlyList<string> NotionsNommees(string? texte, IReadOnlyList<string> programme)
        {
            if (string.IsNullOrWhiteSpace(texte) || programme.Count == 0) return [];

            var parNotion = programme.Select(n => (Notion: n, Mots: Vocabulaire([n]))).ToList();
            var dits = LibelleNotion.CleSouple(texte).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(m => m.Length >= 4).Select(Singulier).ToHashSet();

            return parNotion
                .Where(p => p.Mots.Any(m => dits.Contains(m) && parNotion.Count(q => q.Mots.Contains(m)) == 1))
                .Select(p => p.Notion)
                .ToList();
        }

        /// <summary>
        /// Les notions du programme qu'un message du professeur vise pour un RETRAIT (pour la question exacte
        /// seulement) : libellés entre guillemets après « retire / retirer / enlève » (question fermée tronquée ou
        /// reformulée : « je retire toute la notion « Identifier un couple acide-base de Brønsted » aussi ? »), puis les
        /// notions nommées dans une phrase qui parle de retrait. Le tableau ([ARDOISE]) et la liste ajoutée par le serveur
        /// (« Pour être exact… ») ne comptent pas.
        /// </summary>
        public static IReadOnlyList<string> NotionsVisees(string? messageDuProfesseur, IReadOnlyList<string> programme)
        {
            var texte = SansArdoise(messageDuProfesseur);
            if (texte.Length == 0 || programme.Count == 0) return [];

            var visees = new List<string>();
            foreach (Match m in CitationApresRetrait().Matches(texte))
            {
                visees.AddRange(Viser(m.Groups["x"].Value, programme));
            }

            if (visees.Count == 0)
            {
                foreach (var phrase in Phrases().Split(texte).Where(p => ParleDeRetrait(p)))
                {
                    visees.AddRange(NotionsNommees(phrase, programme));
                }
            }

            return visees.Distinct().ToList();
        }

        /// <summary>
        /// Les lignes du programme que vise un libellé : au mot près ; sinon celles qui ont tous ses vrais mots (un
        /// morceau, un libellé tronqué) ; sinon celles contenues dans ce qu'il a écrit (un libellé allongé). Trois au plus.
        /// </summary>
        public static IReadOnlyList<string> Viser(string? demande, IReadOnlyList<string> programme)
        {
            var cle = LibelleNotion.CleSouple(demande);
            if (cle.Length == 0) return [];

            var exactes = programme.Where(p => LibelleNotion.CleSouple(p) == cle).ToList();
            if (exactes.Count > 0) return exactes;

            var mots = cle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(m => m.Length >= 4).Select(Singulier).ToHashSet();
            var avecTousSesMots = mots.Count == 0 ? [] : programme.Where(p => mots.IsSubsetOf(Vocabulaire([p]))).ToList();
            if (avecTousSesMots.Count > 0) return avecTousSesMots.Take(3).ToList();

            return programme
                .Where(p => LibelleNotion.CleSouple(p) is { Length: > 0 } c && $" {cle} ".Contains($" {c} ", StringComparison.Ordinal))
                .Take(3)
                .ToList();
        }

        /// <summary>
        /// Le texte d'un message sans ses tableaux ([ARDOISE]…[/ARDOISE]) ni les paragraphes de liste ajoutés par le
        /// serveur (« Pour être exact… », « Le tableau n'est pas à jour… ») : ils énumèrent le programme, ils ne visent
        /// aucune notion.
        /// </summary>
        public static string SansArdoise(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return "";
            var sans = Ardoise().Replace(texte, "\n");
            return string.Join("\n", sans.Split('\n').Where(l => !ListeDuServeur().IsMatch(l))).Trim();
        }

        /// <summary>Une phrase du message (hors tableau) qui finit par « ? » et parle de retrait (« Tu veux que je la retire ? »).</summary>
        private static bool QuestionDeRetrait(string? texte) =>
            Phrases().Split(SansArdoise(texte)).Any(p => p.TrimEnd().EndsWith('?') && ParleDeRetrait(p));

        private static bool ParleDeRetrait(string? texte) =>
            !string.IsNullOrWhiteSpace(texte) && VerbeDeRetrait().IsMatch($" {LibelleNotion.CleSouple(texte)} ");

        [GeneratedRegex(@" (retir\w*|enlev\w*|supprim\w*|vire|virer|vires|virez) ")]
        private static partial Regex VerbeDeRetrait();

        [GeneratedRegex(@" ((ne|n) ((la|le|les|l|lui|en|me|te) ){0,2}(retir|enlev|supprim|vir)\w* (pas|jamais|rien|plus)|ne pas ((la|le|les|l) )?(retir|enlev|supprim|vir)\w*|pas (la|le|les|l) (retir|enlev|supprim|vir)\w*) ")]
        private static partial Regex RetraitNie();

        [GeneratedRegex(@"(retir|enlev|supprim)\w*[^«»\r\n?]{0,60}«[\s  ]*(?<x>[^«»\r\n]+?)[\s  ]*»", RegexOptions.IgnoreCase)]
        private static partial Regex CitationApresRetrait();

        [GeneratedRegex(@"(?<=[.!?…\n])")]
        private static partial Regex Phrases();

        [GeneratedRegex(@"\[ARDOISE\][\s\S]*?(\[/ARDOISE\]|$)", RegexOptions.IgnoreCase)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"^\s*(Pour être exact|Le tableau n'est pas à jour)", RegexOptions.IgnoreCase)]
        private static partial Regex ListeDuServeur();

        /// <summary>
        /// Un OUI CLAIR : le message COMMENCE par un acquiescement (« oui », « ouais », « ok », « d'accord », « vas-y »,
        /// « exactement », « bien sûr », « tout à fait », « je confirme ») et ne contient AUCUNE réserve : « non »,
        /// « mais », « sauf », « seulement », « juste », « uniquement », « garde / conserve / laisse », « plutôt »,
        /// « attends », « finalement », « enfin », « peut-être », « je crois / je pense », « pas » devant un article ou
        /// « toute / en entier / besoin / la peine / encore », « ne … que », « pour la / celle… », « la première… », un
        /// verbe de retrait nié (« ne la retire pas »), « évite », « inutile », « autre », ou un point d'interrogation
        /// (il pose une question). S'il reprend des mots d'une notion demandée (<paramref name="notionsDemandees"/>), il
        /// doit dire « toute la notion » ou « en entier » (« oui, la vitesse volumique » peut vouloir dire « seulement
        /// elle »). Une justification reste permise (« oui, on ne l'a pas vue »). Plus de 25 mots : pas clair.
        /// </summary>
        /// <param name="autresNotions">Le reste du programme : un oui qui nomme une AUTRE notion (« oui, la vitesse
        /// volumique » à la question sur la 864) n'est pas clair.</param>
        public static bool OuiClair(
            string? message, IReadOnlyList<string>? notionsDemandees = null, IReadOnlyList<string>? autresNotions = null)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Contains('?')) return false;

            var cle = LibelleNotion.CleSouple(message);
            var mots = cle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (mots.Length == 0 || mots.Length > 25) return false;
            if (!Acquiescement().IsMatch($" {cle} ") || Reserve().IsMatch($" {cle} ")) return false;

            var demandes = Vocabulaire(notionsDemandees);
            var autres = Vocabulaire(autresNotions);
            autres.ExceptWith(demandes);

            var dits = mots.Where(m => m.Length >= 4).Select(Singulier).ToList();
            if (dits.Any(autres.Contains)) return false;

            return !dits.Any(demandes.Contains) || Entiere().IsMatch($" {cle} ");
        }

        private static HashSet<string> Vocabulaire(IReadOnlyList<string>? notions) =>
            (notions ?? [])
                .SelectMany(n => LibelleNotion.CleSouple(n).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(m => m.Length >= 4)
                .Select(Singulier)
                .ToHashSet();

        private static string Singulier(string m) => m.Length > 4 && (m.EndsWith('s') || m.EndsWith('x')) ? m[..^1] : m;

        // « d'accord » devient « accord » dans la clé (l'amorce « d » est ôtée par CleSouple).
        [GeneratedRegex(@"^ (oui|ouai|ouais|ouaip|ok|okay|oki|yes|yep|accord|d accord|exactement|exact|absolument|parfait|carrement|vas y|vasy|go|bien sur|tout a fait|je confirme|confirme|c est ca|c est bon) ")]
        private static partial Regex Acquiescement();

        [GeneratedRegex(@" (non|nan|mais|sauf|seulement|juste|uniquement|garde\w*|conserve\w*|laisse\w*|plutot|attend\w*|finalement|enfin|peut etre|je crois|je pense|hesite\w*|evite\w*|inutile|autre|autres"
            + @"|premier\w*|deuxieme|second\w*|troisieme|dernier\w*|pour (la|le|l|les|celle|celui|cette|ce|cet|ces)"
            + @"|pas (la|le|les|l|un|une|toute?|tous|toutes|en|entier\w*|completement|totalement|cette|ce|ces|cet|celle|celui|du|des|de|d|besoin|la peine|encore|maintenant|tout de suite|ca|cela)"
            + @"|ne pas|que (la|le|les|l|cette|ce|ces|cet|celle|celui|du|des|un|une)"
            // #67 v6 (point 4) : « oui jsp », « oui je sais pas », « d'accord, on la révise alors », « oui, on l'a vue en
            // classe » (il l'a VUE : elle reste), « je la connais ».
            + @"|jsp|jsais|chais|sais pas|sait pas|sais plus|sais rien|ne sais|revis\w*|alors"
            + @"|on l a (vu|vue|vus|vues|fait|faite|faites|etudie\w*|deja)|(je|on) (la|le|l) (connai\w*|maitris\w*|sai\w*)"
            + @"|(ne|n) ((la|le|les|l|lui|en|me|te) ){0,2}(retir|enlev|supprim|ot|vir)\w* (pas|jamais|rien|plus)) ")]
        private static partial Regex Reserve();

        [GeneratedRegex(@" (toute? (la|cette) notion|(la|cette) notion (entiere|en entier|entierement|completement)|en entier|entiere\w*|tout entiere|completement|totalement) ")]
        private static partial Regex Entiere();

        [GeneratedRegex(@"Je retire toute la notion[\s  ]*«[\s  ]*(?<x>[^«»\r\n]+?)[\s  ]*»[\s  ]*de ton programme[\s  ]*\?", RegexOptions.IgnoreCase)]
        private static partial Regex Question();
    }

    /// <summary>
    /// La décision du tour sur un retrait (#67 v6) : voir <see cref="ConfirmationDeRetrait.Decider"/>.
    /// </summary>
    /// <param name="AConfirmer">Les libellés passés au dépôt (oui clair à une question fermée valable) ; vide sinon.</param>
    /// <param name="Posees">Les questions fermées VALABLES du dernier message du professeur (dernière question, en tête de
    /// phrase, non citées).</param>
    /// <param name="NonValables">Les questions fermées présentes mais qui ne comptent pas (pas la dernière question,
    /// citées, ou dans l'avant-dernier message).</param>
    /// <param name="OuiClair">L'élève a répondu par un oui clair aux questions valables.</param>
    /// <param name="DemandeSansQuestion">Il demande un retrait (ou dit oui à un message qui en parlait) sans question
    /// fermée valable : RIEN n'est retiré, le serveur le dit et pose la question exacte si la notion est unique.</param>
    /// <param name="Evoquees">Les notions du programme qu'il semble viser (pour la question exacte seulement).</param>
    /// <param name="DemandeExplicite">Son message contient lui-même un verbe de retrait non nié (« retire-la »).</param>
    public sealed record DecisionDeRetrait(
        IReadOnlyList<string> AConfirmer, IReadOnlyList<string> Posees, IReadOnlyList<string> NonValables,
        bool OuiClair, bool DemandeSansQuestion, IReadOnlyList<string> Evoquees, bool DemandeExplicite = false)
    {
        public static readonly DecisionDeRetrait Rien = new([], [], [], false, false, []);
    }

    /// <summary>Ce qu'un retrait a fait (ou ferait, en simulation) au programme d'un contrôle.</summary>
    public sealed record BilanRetrait(IReadOnlyList<NotionRetiree> Retirees, IReadOnlyList<RetraitRefuse> Refus)
    {
        public static readonly BilanRetrait Vide = new([], []);

        /// <summary>Les refus que le professeur doit expliquer à l'élève (tout sauf « introuvable »).</summary>
        public IEnumerable<RetraitRefuse> RefusADire => Refus.Where(r => r.Motif != MotifRefusRetrait.Introuvable);
    }
}
