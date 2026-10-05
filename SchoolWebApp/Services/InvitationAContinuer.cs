using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// « VAS-Y » N'EST PAS UNE RÉPONSE — relevé par Camara le 03/10/2026, en
    /// cours de maths : « elle m'a dit que le résultat était bon alors que
    /// j'avais donné aucune réponse ».
    ///
    /// CE QUI S'EST PASSÉ. Nora pose un dernier exercice — un nombre divisé par
    /// 8, quotient 6, reste 5 — et l'écrit au tableau. L'élève répond « Vas-y. »
    /// Elle enchaîne : « Oui, c'est exactement ça : 53 », puis félicite — « tu as
    /// tout retrouvé tout seul aujourd'hui ». Le 53 est juste ; il n'a jamais été
    /// donné par l'élève. Un exercice a été posé, résolu et validé sans lui.
    ///
    /// C'est le symétrique exact de <see cref="ReponseChiffree"/>, qui dit « un
    /// résultat vient d'arriver, vérifie-le ». Celui-ci dit l'inverse, et il est
    /// aussi nécessaire : rien dans le message ne distingue, pour le modèle, un
    /// élève qui donne sa réponse d'un élève qui attend l'exercice. Le serveur,
    /// lui, voit le message en entier.
    ///
    /// LA BORNE EST ÉTROITE, ET C'EST VOULU. Seule une INVITATION pure est
    /// reconnue — « vas-y », « c'est parti », « envoie », éventuellement précédée
    /// d'un « oui » ou d'un « ok ». Un simple « oui » ne suffit PAS : à une
    /// question fermée, « oui » est une vraie réponse, et la valider est juste.
    /// </summary>
    public static partial class InvitationAContinuer
    {
        /// <summary>
        /// Les tours de parole qui ne disent rien d'autre que « continue ».
        /// Le mot d'invitation est obligatoire ; ce qui l'entoure — « oui »,
        /// « ok », « s'il te plaît » — est facultatif.
        /// </summary>
        private const string Mots =
            "vas y|vasy|va y|allez y|allez|allons y|go|c est parti|cest parti|continue|"
            + "on continue|continuons|envoie|balance|lance|dis moi|dis|je t ecoute|"
            + "je suis prete?|prete?|j attends|je t attends|a toi|"
            + "oui|ouais|ok|okay|d accord|daccord|super|parfait|tres bien|bien sur|alors|"
            + "ben|bah|euh|stp|s il te plait|merci|maintenant|donc";

        /// <summary>Le message n'est fait QUE de ces mots-là, et de rien d'autre.</summary>
        [GeneratedRegex(
            @"^(?:" + Mots + @")(?:[\s,!.]+(?:" + Mots + @"))*[\s,!.…]*$",
            RegexOptions.IgnoreCase)]
        private static partial Regex RienDAutre();

        /// <summary>
        /// Et au moins un de ces mots-là INVITE. « Oui » seul ne suffit pas :
        /// à une question fermée, c'est une vraie réponse, et la valider est juste.
        /// </summary>
        [GeneratedRegex(
            @"(?<!\p{L})(?:vas y|vasy|va y|allez y|allez|allons y|go|c est parti|cest parti|"
            + @"continue|on continue|continuons|envoie|balance|lance|dis moi|dis|je t ecoute|"
            + @"je suis prete?|prete?|j attends|je t attends|a toi)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex Invite();

        /// <summary>
        /// Vrai quand le message de l'élève ne fait qu'inviter à continuer.
        ///
        /// Les faits entre crochets que l'application accroche au message sont
        /// écartés d'abord : ils ne viennent pas de l'élève.
        /// </summary>
        public static bool Est(string? messageEleve)
        {
            if (string.IsNullOrWhiteSpace(messageEleve)) return false;

            var dit = Regex.Replace(messageEleve, @"\[[^\]]*\]", " ");

            // Apostrophes et traits d'union deviennent des espaces — « vas-y »,
            // « vas y » et « Vas-y ! » sont le même tour de parole —, les accents
            // tombent, et il ne doit rien rester d'autre.
            dit = new string(dit.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());
            dit = Regex.Replace(dit, @"['’\-–—]", " ");
            dit = Regex.Replace(dit, @"\s+", " ").Trim();

            // Une phrase un peu longue porte forcément autre chose : on ne juge
            // que les tours de parole courts, ceux où il n'y a rien d'autre.
            return dit.Length <= 40 && RienDAutre().IsMatch(dit) && Invite().IsMatch(dit);
        }

        /// <summary>
        /// Court, et non mis en cache : il ne part que sur ces tours-là.
        /// </summary>
        public const string Rappel =
            "[CE MESSAGE N'EST PAS UNE RÉPONSE : l'élève te dit seulement de continuer. "
            + "Il n'a donné AUCUN résultat — ne valide rien, ne dis ni « c'est exactement ça » "
            + "ni « bravo ». Donne l'exercice (ou reprends ton explication) et attends sa "
            + "réponse. Ne résous pas l'exercice à sa place.]";
    }
}
