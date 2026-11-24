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
                + "c'est SON CLIC qui retire, et le serveur te le dira au message suivant. Ne propose un retrait que s'il "
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
            if (clics.Count == 0) return "";

            var profs = messagesProfesseur.ToList();
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

            if (annonceFausse || reduit)
            {
                var debut = annonceFausse && !clicRetire ? "Pour être exact, je n'ai rien retiré : " : "Pour être exact, ";
                ajouts.Add((absentes.Count > 0 ? "Le tableau n'est pas à jour. " : "") + debut
                    + $"ton programme contient toujours {(programme.Count == 1 ? "cette notion" : $"ces {programme.Count} notions")} : "
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

        [GeneratedRegex(@" ((ai|a|as|avons|ont|est|sont|a ete|ont ete) ((bien|aussi|deja|egalement) )?(retire|enleve|supprime)\w*|c est ((bien|deja) )?(retire|enleve|supprime|fait|note)\w*) ")]
        private static partial Regex RetraitPasse();

        [GeneratedRegex(@"^ (retiree?s?|enlevee?s?|supprimee?s?) ")]
        private static partial Regex ParticipeEnTete();

        [GeneratedRegex(@" c est (fait|note)(?! (pour|bravo|super|felicitation\w*|tres bien|bien joue|dans ton agenda))( |$)")]
        private static partial Regex CestFaitSeul();
    }
}
