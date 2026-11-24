using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Builders.impl
{
    /// <summary>
    /// LE RETRAIT D'UNE NOTION PAR LE BOUTON — signalement #67 v7 (décision du coordinateur, dans le cadre validé par
    /// Min's : « question fermée + oui, éventuellement par un bouton »). Six versions d'analyse du TEXTE de l'élève ont
    /// chacune laissé passer des cas ; désormais :
    ///
    ///   1. le professeur PROPOSE un retrait par la balise <c>[RETRAIT_PROPOSE] id=&lt;id&gt;</c> (ou le libellé exact
    ///      entre guillemets) ; le serveur la résout sur le programme relu en base et, seulement si elle désigne une
    ///      notion retirable (programme d'au moins deux notions), ajoute à la réponse la balise du serveur
    ///      <c>[RETRAIT_BOUTON]controle=C|id=N|libelle=X[/RETRAIT_BOUTON]</c> : c'est elle, et elle seule, qui fait
    ///      afficher les boutons « Oui, retirer « X » » / « Non, la garder » ;
    ///   2. seul le clic sur « Oui » appelle la route dédiée (<c>POST eleves/{id}/controles/{c}/notions/{n}/retrait</c>),
    ///      qui retire CETTE ligne et rien d'autre ;
    ///   3. AUCUN texte tapé ne retire jamais rien ; le serveur ne propose JAMAIS de lui-même un retrait ;
    ///   4. sur une annonce douteuse (« c'est retiré », tableau réduit…), le serveur n'ajoute QUE la phrase vraie et le
    ///      programme relu en base, sans question ;
    ///   5. après un clic, le message suivant du professeur reçoit le fait « RETIRÉ : X » ou « GARDÉE : X ».
    /// </summary>
    public partial class ChatViewModelBuilder
    {
        /// <summary>Une ligne du programme d'un contrôle : son identifiant (ControleNotion.Id) et son libellé affiché.</summary>
        public sealed record NotionDuProgramme(int Id, string Libelle);

        internal const string OuvreBoutonRetrait = "[RETRAIT_BOUTON]";
        internal const string FermeBoutonRetrait = "[/RETRAIT_BOUTON]";

        /// <summary>La balise écrite par le PROFESSEUR : <c>[RETRAIT_PROPOSE] id=123</c> ou <c>[RETRAIT_PROPOSE] « libellé »</c>.</summary>
        [GeneratedRegex(@"\[RETRAIT_PROPOSE\][ \t  ]*(?:id[ \t]*[=:][ \t]*(?<id>\d+)|«[ \t  ]*(?<lib>[^«»\r\n\]]+?)[ \t  ]*»)?", RegexOptions.IgnoreCase)]
        private static partial Regex BaliseRetraitPropose();

        /// <summary>La balise du SERVEUR, ajoutée à la réponse (et seule lue par l'écran pour montrer les boutons).</summary>
        [GeneratedRegex(@"\[RETRAIT_BOUTON\]controle=(?<c>\d+)\|id=(?<id>\d+)\|libelle=(?<lib>[^\r\n]*?)\[/RETRAIT_BOUTON\]")]
        private static partial Regex BaliseBoutonRetrait();

        /// <summary>
        /// Le marqueur joint par l'écran au message de l'élève APRÈS son clic : <c>[RETRAIT_CLIC:RETIREE id=N]</c> (le
        /// « Oui » a été accepté par la route) ou <c>[RETRAIT_CLIC:GARDEE id=N]</c>. Il ne retire RIEN : le serveur le
        /// relit contre la base (une notion dite retirée mais toujours là est dite toujours là).
        /// </summary>
        [GeneratedRegex(@"\[RETRAIT_CLIC:(?<choix>RETIREE|GARDEE) id=(?<id>\d+)\]")]
        private static partial Regex MarqueurClicRetrait();

        /// <summary>Les clics joints au message de l'élève (choix, identifiant de la ligne).</summary>
        internal static IReadOnlyList<(bool Retiree, int Id)> ClicsDeRetrait(string? message) =>
            string.IsNullOrEmpty(message) ? []
                : MarqueurClicRetrait().Matches(message)
                    .Select(m => (m.Groups["choix"].Value == "RETIREE", int.Parse(m.Groups["id"].Value)))
                    .ToList();

        /// <summary>
        /// Le libellé qu'un bouton du serveur a montré pour la ligne <paramref name="id"/> dans les messages du
        /// professeur (le plus récent d'abord) : c'est lui qu'on redit après un clic, la ligne n'étant plus en base.
        /// </summary>
        internal static string? LibelleDuBouton(IEnumerable<string?> messagesProfesseur, int id)
        {
            foreach (var texte in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(texte)) continue;
                foreach (Match m in BaliseBoutonRetrait().Matches(texte))
                {
                    if (m.Groups["id"].Value == id.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    {
                        return m.Groups["lib"].Value.Trim();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// LE PROGRAMME AVEC SES IDENTIFIANTS ET LA RÈGLE DU BOUTON, à chaque tour d'une préparation (#67 v7). Une notion
        /// seule ne se retire pas (plancher) : pas d'identifiant à proposer, le professeur le sait.
        /// </summary>
        internal static string ProgrammeEtBoutonDeRetrait(IReadOnlyList<NotionDuProgramme> programme)
        {
            if (programme.Count == 0) return "";

            if (programme.Count == 1)
            {
                return "\n\n[RETRAIT D'UNE NOTION : son programme n'a qu'UNE notion (« " + programme[0].Libelle + " ») : "
                    + "elle ne peut pas être retirée (un programme n'est jamais vide). N'écris pas [RETRAIT_PROPOSE] ; s'il "
                    + "veut la changer, demande-lui ce qu'il y aura à la place.]";
            }

            return "\n\n[RETRAIT D'UNE NOTION — SEUL LE BOUTON RETIRE : tu ne retires jamais une notion toi-même, et rien de "
                + "ce qu'il écrit (« oui », « retire-la », « ok ») ne retire quoi que ce soit. S'il veut retirer une notion de "
                + "son programme (ou te dit qu'elle ne sera pas à son contrôle), écris À LA FIN de ton message la balise "
                + "[RETRAIT_PROPOSE] id=<identifiant> de CETTE notion (une seule par message) : l'écran lui montrera deux "
                + "boutons, « Oui, retirer » et « Non, la garder ». Dis-lui simplement de cliquer s'il veut la retirer. "
                + "N'écris jamais « je la retire », « c'est retiré », « c'est fait » ni « c'est noté » pour un retrait : "
                + "c'est SON CLIC qui retire, et le serveur te le dira au message suivant. Tant qu'il n'a pas cliqué, la "
                + "notion est TOUJOURS dans son programme : n'écris pas « on retire… », « on enlève… », « il ne reste (donc) que… » ni un "
                + "programme sans elle ; écris « si tu veux la retirer, clique sur « Oui, retirer » ». Ne propose un retrait que s'il "
                + "l'a demandé. Une notion se garde ou se retire entière. SON PROGRAMME, RELU EN BASE (identifiants) : "
                + string.Join(" ; ", programme.Select(n => $"id={n.Id} « {n.Libelle} »")) + ".]";
        }

        /// <summary>
        /// LE FAIT DU CLIC, DONNÉ AVANT LA RÉPONSE (#67 v7, point 6) : « RETIRÉ : X » si la ligne cliquée n'est plus en
        /// base, « GARDÉE : X » s'il a cliqué « Non ». Un marqueur « retirée » dont la ligne est toujours là (tapé à la
        /// main, ou route en échec) est dit « PAS retirée ». Vide sans clic.
        /// </summary>
        internal static string FaitDuClicDeRetrait(
            string? messageEleve, IReadOnlyList<NotionDuProgramme> programme, IEnumerable<string?> messagesProfesseur)
        {
            var clics = ClicsDeRetrait(messageEleve);
            var profs = messagesProfesseur.ToList();
            if (clics.Count == 0) return FaitSansClic(messageEleve, programme, profs);
            var phrases = new List<string>();
            foreach (var (retiree, id) in clics.Distinct())
            {
                var enBase = programme.FirstOrDefault(n => n.Id == id);
                var libelle = enBase?.Libelle ?? LibelleDuBouton(profs, id) ?? "la notion proposée";

                if (!retiree)
                {
                    phrases.Add($"GARDÉE : « {libelle} » — il a cliqué « Non, la garder » : elle RESTE dans son programme. "
                        + "Ne lui reproposes pas de la retirer ; continue avec lui");
                }
                else if (enBase is null)
                {
                    phrases.Add($"RETIRÉ : « {libelle} » — il a cliqué « Oui » et le serveur l'a retirée de son programme. "
                        + "Dis-le-lui en une phrase et, si tu montres son programme, réécris-le sans elle");
                }
                else
                {
                    phrases.Add($"« {libelle} » n'a PAS été retirée : elle est TOUJOURS dans son programme. Ne dis pas "
                        + "qu'elle est retirée");
                }
            }

            var liste = programme.Count > 0
                ? " SON PROGRAMME, RELU EN BASE APRÈS SON CLIC : " + string.Join(" ; ", programme.Select(n => $"« {n.Libelle} »"))
                  + ". Tout tableau de son programme reprend EXACTEMENT ces notions."
                : "";

            return "\n\n[RETRAIT PAR LE BOUTON : " + string.Join(" ; ", phrases) + "." + liste + "]";
        }

        /// <summary>
        /// IL A ÉCRIT AU LIEU DE CLIQUER (#67 v7, banc T-1 sur a596a65 : « Oui, retire-la. » tapé sous les boutons → « Merci,
        /// c'est noté pour ton clic. »). Le dernier message du professeur portait un bouton dont la notion est toujours en
        /// base, et l'élève répond « oui / ok / d'accord… » ou parle de retrait, SANS clic : le professeur l'apprend AVANT
        /// de répondre (rien n'est retiré, seul le bouton retire). Aucun retrait, aucun bouton ajouté par le serveur.
        /// </summary>
        private static string FaitSansClic(
            string? messageEleve, IReadOnlyList<NotionDuProgramme> programme, IReadOnlyList<string?> profs)
        {
            if (string.IsNullOrWhiteSpace(messageEleve)) return "";
            var dernier = profs.LastOrDefault(p => !string.IsNullOrWhiteSpace(p));
            if (dernier is null) return "";

            var bouton = BaliseBoutonRetrait().Matches(dernier).LastOrDefault();
            if (bouton is null || !int.TryParse(bouton.Groups["id"].Value, out var id)) return "";
            var notion = programme.FirstOrDefault(n => n.Id == id);
            if (notion is null) return "";

            if (!OuiTape().IsMatch(messageEleve) && !MorceauDeNotion.ParleDeRetrait(messageEleve)) return "";

            return "\n\n[RETRAIT PAR LE BOUTON : il a ÉCRIT sa réponse, il n'a PAS cliqué : RIEN n'a été retiré, « "
                + notion.Libelle + " » est TOUJOURS dans son programme. Ne dis ni « c'est noté », ni « c'est fait », ni « ton "
                + "clic » : dis-lui en une phrase que seul le bouton « Oui, retirer » retire une notion et, s'il veut toujours la "
                + "retirer, termine ton message par [RETRAIT_PROPOSE] id=" + notion.Id + ".]";
        }

        [GeneratedRegex(@"^\s*(oui|ouais|ouai|ok|okay|d['’ ]?accord|vas[- ]y|yes|carr[ée]ment|bien s[ûu]r|exactement|voil[àa])\b", RegexOptions.IgnoreCase)]
        private static partial Regex OuiTape();

        /// <summary>
        /// CE QUE LE SERVEUR AJOUTE À LA RÉPONSE DU PROFESSEUR (#67 v7) — dans le même tour, affiché et gardé :
        ///   - la balise du bouton, si la réponse propose (balise) une notion RETIRABLE du programme relu ;
        ///   - la phrase vraie et le programme relu, si la réponse annonce un retrait qui n'a pas eu lieu ou un programme
        ///     réduit — SANS AUCUNE question (le serveur ne propose jamais de lui-même un retrait).
        /// Null s'il n'y a rien à ajouter.
        /// </summary>
        internal static string? AjoutDuRetraitParBouton(
            string? texte, IReadOnlyList<NotionDuProgramme> programme, int controleId, string? messageEleve)
        {
            if (string.IsNullOrWhiteSpace(texte) || programme.Count == 0) return null;

            var libelles = programme.Select(n => n.Libelle).ToList();
            var ajouts = new List<string>();

            // LA PHRASE VRAIE. Les notions retirées par un clic à ce tour ne sont plus en base : l'annonce de leur retrait
            // est VRAIE. Une annonce ne compte donc comme fausse que si elle nomme une notion toujours en base, ou, sans
            // clic à ce tour, si elle ne nomme rien (« C'est retiré ! »).
            var clicRetire = ClicsDeRetrait(messageEleve).Any(c => c.Retiree && programme.All(n => n.Id != c.Id));
            var nommees = AnnonceesRetirees(texte, libelles);
            var annonceFausse = nommees.Count > 0 || (!clicRetire && AnnonceUnRetraitFait(texte, messageEleve));
            var absentes = AbsentesDuTableau(texte, libelles);
            var reduit = absentes.Count > 0 || AffirmeUnProgrammeReduit(texte, programme.Count);

            // #67 v7 (banc bouton sur c82abb5, R-1) : « Pas de souci, on retire les titrages — il ne reste donc que la
            // cinétique et les couples acide-base. » écrit AVANT le clic, bouton juste en dessous : programme réduit
            // affirmé, rien corrigé. « Il ne reste (donc) que » + au moins une notion nommée mais pas toutes ne compte que
            // quand un retrait est en jeu à ce tour (balise dans la réponse, ou l'élève parle de retrait) et sans clic
            // accepté (après le clic, c'est vrai) : « il ne reste que 5 minutes » ou un rappel ordinaire ne déclenchent rien.
            var retraitEnJeu = BaliseRetraitPropose().IsMatch(texte)
                || MorceauDeNotion.ParleDeRetrait(MarqueurClicRetrait().Replace(messageEleve ?? "", " "));
            if (!reduit && !clicRetire && retraitEnJeu
                && ResteQueSansToutes(texte, libelles, NotionProposee(texte, programme)?.Libelle)) reduit = true;

            if (annonceFausse || reduit)
            {
                var debut = annonceFausse && !clicRetire ? "Pour être exact, je n'ai rien retiré : " : "Pour être exact, ";
                ajouts.Add((absentes.Count > 0 ? "Le tableau n'est pas à jour. " : "") + debut
                    + $"ton programme contient {(clicRetire ? "maintenant" : "toujours")} {(programme.Count == 1 ? "cette notion" : $"ces {programme.Count} notions")} : "
                    + string.Join(" ; ", programme.Select(n => $"« {n.Libelle} »")) + ".");
            }

            // LE BOUTON. Une seule proposition par message : la première balise qui désigne une notion du programme.
            var balises = BaliseRetraitPropose().Matches(texte);
            if (balises.Count > 0)
            {
                NotionDuProgramme? visee = null;
                foreach (Match m in balises)
                {
                    visee = Resoudre(m, programme);
                    if (visee is not null) break;
                }

                if (programme.Count < 2)
                {
                    ajouts.Add("Ton programme n'a qu'une notion : elle ne peut pas être retirée.");
                }
                else if (visee is null)
                {
                    ajouts.Add("Je n'ai pas pu afficher le bouton de retrait : dis-moi quelle notion de ton programme tu "
                        + "veux retirer.");
                }
                else
                {
                    ajouts.Add($"{OuvreBoutonRetrait}controle={controleId}|id={visee.Id}|libelle={visee.Libelle}{FermeBoutonRetrait}");
                }
            }

            return ajouts.Count == 0 ? null : "\n\n" + string.Join("\n\n", ajouts);
        }

        /// <summary>La notion que désigne une balise du professeur : identifiant du programme, ou libellé au mot près (unique).</summary>
        private static NotionDuProgramme? Resoudre(Match balise, IReadOnlyList<NotionDuProgramme> programme)
        {
            if (balise.Groups["id"].Success
                && int.TryParse(balise.Groups["id"].Value, out var id))
            {
                return programme.FirstOrDefault(n => n.Id == id);
            }

            if (balise.Groups["lib"].Success)
            {
                var cle = LibelleNotion.CleSouple(balise.Groups["lib"].Value);
                var trouvees = programme.Where(n => LibelleNotion.CleSouple(n.Libelle) == cle).ToList();
                return trouvees.Count == 1 ? trouvees[0] : null;
            }

            return null;
        }

        /// <summary>
        /// La réponse annonce-t-elle un retrait FAIT ? <see cref="AnnonceUnRetrait"/> (formes fortes, verdict d'Omar v6
        /// point 6 compris), plus une phrase qui COMMENCE par « Retirée ! » / « Enlevée ». « C'est fait » / « c'est
        /// noté » seuls ne comptent que si l'élève vient de parler de retrait (sinon « C'est noté pour vendredi »).
        /// </summary>
        internal static bool AnnonceUnRetraitFait(string? texte, string? messageEleve)
        {
            if (string.IsNullOrWhiteSpace(texte)) return false;

            var sans = ConfirmationDeRetrait.SansArdoise(BaliseRetraitPropose().Replace(texte, " "));

            // #67 v7 (banc, tirage B-1) : une QUESTION qui propose (« Tu veux que je les retire toutes les deux ? ») n'annonce
            // rien — la phrase vraie « je n'ai rien retiré » y était ajoutée à tort. Une question ne compte que si elle
            // dit un retrait PASSÉ (« Je l'ai retirée, on continue ? », « C'est retiré, ça te va ? »).
            sans = string.Concat(FinDePhrase().Split(sans)
                .Where(p => !p.TrimEnd().EndsWith('?') || RetraitPasse().IsMatch($" {LibelleNotion.CleSouple(p)} ")));
            if (AnnonceUnRetrait(sans)) return true;

            var phrases = FinDePhrase().Split(sans).Select(p => $" {LibelleNotion.CleSouple(p)} ").ToList();
            if (phrases.Any(p => ParticipeEnTete().IsMatch(p))) return true;

            return MorceauDeNotion.ParleDeRetrait(MarqueurClicRetrait().Replace(messageEleve ?? "", " "))
                && phrases.Any(p => CestFaitSeul().IsMatch(p));
        }

        /// <summary>
        /// Une phrase qui AFFIRME le programme déjà réduit (#67 v7, banc sur c82abb5 ; resserrée en v8, faux positifs
        /// F2/F3/F5 d'Omar) — hors tableau, les trois conditions ensemble :
        ///   1. « il ne (te / nous) reste (donc / alors / plus / maintenant…) que » au PRÉSENT : « il ne restera / resterait
        ///      que » décrit ce que le clic ferait, c'est vrai ;
        ///   2. pas dans une proposition en « si » (« Si tu la retires, il ne reste que… ») ;
        ///   3. ce qui suit « que » est le programme MOINS la notion proposée : soit « deux notions » (le nombre = programme
        ///      moins une), soit une énumération d'autant d'éléments (coupée aux virgules et aux « et »), dont au moins un
        ///      nomme une notion du programme, jamais deux la même, et aucun la notion proposée (balise de la réponse).
        ///      « La cinétique » ne nomme aucun libellé (le mot n'y est pas) : on compte donc les éléments au lieu de
        ///      comparer des ensembles. « il ne te reste plus que le temps de demi-réaction à revoir » (un seul élément sur
        ///      trois notions) n'est pas un programme réduit.
        /// </summary>
        internal static bool ResteQueSansToutes(string? texte, IReadOnlyList<string> libelles, string? libellePropose)
        {
            var sans = ConfirmationDeRetrait.SansArdoise(texte);
            if (sans.Length == 0 || libelles.Count < 2) return false;
            return FinDePhrase().Split(sans).Any(p =>
            {
                var m = ResteQue().Match(p);
                return m.Success && !DansUnSi(p, m.Index)
                    && ProgrammeMoinsUne(p[(m.Index + m.Length)..], libelles, libellePropose);
            });
        }

        /// <summary>Ce qui suit « il ne reste que » : le programme moins une notion (condition 3 ci-dessus) ?</summary>
        private static bool ProgrammeMoinsUne(string suite, IReadOnlyList<string> libelles, string? libellePropose)
        {
            var attendu = libelles.Count - 1;
            var nombre = NombreDeNotions().Match(suite);
            if (nombre.Success) return NombreEcrit(nombre.Groups["n"].Value) == attendu;

            var fin = suite.IndexOfAny(['.', '!', '?', ';', ':', '\n', '—', '–', '(']);
            var elements = Enumeration().Split(fin >= 0 ? suite[..fin] : suite)
                .Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (elements.Count != attendu) return false;

            var nommees = elements.SelectMany(e => ConfirmationDeRetrait.NotionsNommees(e, libelles)).ToList();
            return nommees.Count > 0
                && nommees.Distinct().Count() == nommees.Count
                && (libellePropose is null || !nommees.Contains(libellePropose));
        }

        private static int NombreEcrit(string n) => n.ToLowerInvariant() switch
        {
            "un" or "une" => 1, "deux" => 2, "trois" => 3, "quatre" => 4, "cinq" => 5, "six" => 6,
            _ => int.TryParse(n, out var v) ? v : -1,
        };

        /// <summary>
        /// « il ne reste que » est-il la principale d'une proposition en « si » ? Ce qui précède, dans la même proposition
        /// (coupée aux « : ; — » et aux virgules, guillemets mis à part), commence par « si / s'il » — directement avant
        /// (« Et si tu la retires il ne reste que ») ou dans le morceau juste avant la virgule (« Si tu cliques sur « Oui,
        /// retirer », il ne te reste que »). « Oui, si je comprends bien, on retire les titrages : il ne reste que » et
        /// « Même si c'est dommage, on retire…, il ne reste que » restent des affirmations.
        /// </summary>
        private static bool DansUnSi(string phrase, int position)
        {
            var avant = Guillemets().Replace(phrase[..position], "«»");
            var coupe = avant.LastIndexOfAny([':', ';', '—', '–']);
            if (coupe >= 0) avant = avant[(coupe + 1)..];
            var morceaux = avant.Split(',');
            var dernier = $" {LibelleNotion.CleSouple(morceaux[^1])} ";
            if (SiEnTete().IsMatch(dernier)) return true;
            return morceaux.Length > 1 && LiaisonSeule().IsMatch(dernier)
                && SiEnTete().IsMatch($" {LibelleNotion.CleSouple(morceaux[^2])} ");
        }

        [GeneratedRegex(@"\bil\s+ne\s+(?:te\s+|nous\s+)?reste\s+(?:(?:donc|alors|plus|maintenant|ensuite|d[ée]sormais)\s+)*qu(?:e\s+|['’]\s*)", RegexOptions.IgnoreCase)]
        private static partial Regex ResteQue();

        [GeneratedRegex(@"^\s*(?<n>une|un|deux|trois|quatre|cinq|six|\d+)\s+(?:seules?\s+)?notions?\b", RegexOptions.IgnoreCase)]
        private static partial Regex NombreDeNotions();

        [GeneratedRegex(@"\s*,\s*|\s+(?:et|ainsi que|puis)\s+", RegexOptions.IgnoreCase)]
        private static partial Regex Enumeration();

        [GeneratedRegex(@"«[^»]*»")]
        private static partial Regex Guillemets();

        [GeneratedRegex(@"^ ((et|mais|alors|donc|ok|oui|bon|d accord|accord) )*(si|s il|s ils) ")]
        private static partial Regex SiEnTete();

        [GeneratedRegex(@"^ *((et|alors|donc|eh bien|bien) +)*$")]
        private static partial Regex LiaisonSeule();

        /// <summary>La notion que propose la réponse : la première balise [RETRAIT_PROPOSE] qui désigne une ligne du programme.</summary>
        private static NotionDuProgramme? NotionProposee(string texte, IReadOnlyList<NotionDuProgramme> programme) =>
            BaliseRetraitPropose().Matches(texte).Select(m => Resoudre(m, programme)).FirstOrDefault(n => n is not null);

        [GeneratedRegex(@" ((ai|a|as|avons|ont|est|sont|a ete|ont ete) ((bien|aussi|deja|egalement) )?(retire|enleve|supprime)\w*|c est ((bien|deja) )?(retire|enleve|supprime|fait|note)\w*) ")]
        private static partial Regex RetraitPasse();

        [GeneratedRegex(@"^ (retiree?s?|enlevee?s?|supprimee?s?) ")]
        private static partial Regex ParticipeEnTete();

        [GeneratedRegex(@" c est (fait|note)(?! (pour|bravo|super|felicitation\w*|tres bien|bien joue|dans ton agenda))( |$)")]
        private static partial Regex CestFaitSeul();
    }
}
