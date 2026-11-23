using SchoolWebApp.Domain.Models;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Ce qu'une séance de préparation apprend sur un contrôle.</summary>
    /// <param name="Retirees">
    /// Les notions que l'élève a fait RETIRER du programme (champ `retirer:`, signalement #67) : « on ne les a pas
    /// vues », « mon prof a dit que ce ne serait pas au bac blanc ». Jamais dans <paramref name="Notions"/> : une notion
    /// écrite dans les deux champs n'est PAS retirée (conflit, voir <paramref name="Conflits"/>).
    /// </param>
    /// <param name="Conflits">
    /// Les notions écrites À LA FOIS dans `notions:` et dans `retirer:` (verdict d'Omar, #67 v2) : on ne sait pas ce
    /// que l'élève veut, la notion RESTE dans <paramref name="Notions"/> et le conflit part au journal.
    /// </param>
    public record ControleNotionsDeclarees(
        int Controle, string? Sujet, IReadOnlyList<string> Notions, IReadOnlyList<string>? Retirees = null,
        IReadOnlyList<string>? Conflits = null)
    {
        /// <summary>Jamais null.</summary>
        public IReadOnlyList<string> NotionsRetirees => Retirees ?? [];

        /// <summary>Jamais null.</summary>
        public IReadOnlyList<string> NotionsEnConflit => Conflits ?? [];
    }

    /// <summary>
    /// Extrait le bloc [CONTROLE_NOTIONS] : le professeur complète le
    /// programme d'un contrôle DÉJÀ enregistré, au fil des séances.
    ///
    /// POURQUOI UN SECOND BLOC PLUTÔT QUE DE RÉÉCRIRE LE PREMIER : un contrôle
    /// posé depuis le calendrier par un parent n'a qu'un sujet en une ligne
    /// (« les fractions ») — personne ne sait encore ce qu'il y a dedans. Le
    /// périmètre se découvre en travaillant, séance après séance, et chaque
    /// bloc l'enrichit sans jamais toucher à la date ni au sujet.
    ///
    /// LE NUMÉRO NE S'INVENTE PAS : il est donné par le serveur dans le
    /// contexte de la séance. Un bloc sans numéro lisible est ignoré — écrire
    /// dans le programme d'un contrôle au hasard serait pire que ne rien
    /// écrire.
    /// </summary>
    public static partial class LecteurControleNotions
    {
        [GeneratedRegex(@"\[CONTROLE_NOTIONS\](?<corps>.*?)\[/CONTROLE_NOTIONS\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        [GeneratedRegex(@"\d+")]
        private static partial Regex Numero();

        /// <summary>
        /// Null s'il n'y a pas de bloc, pas de numéro lisible, ou rien à en
        /// tirer — ni sujet, ni notion.
        /// </summary>
        public static ControleNotionsDeclarees? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            var brutControle = LecteurBloc.Valeur(champs, "controle", "controle_id", "numero");
            if (brutControle is null) return null;

            var numero = Numero().Match(brutControle);
            if (!numero.Success || !int.TryParse(numero.Value, out var controle)) return null;

            var sujet = LecteurBloc.Valeur(champs, "sujet");

            // RETIRER UNE NOTION DU PROGRAMME — signalement #67 (23/11/2026) : « enlève les espèces amphotères, on ne
            // les a pas vues », puis « mon prof a dit que les titrages ne seront pas au bac blanc ». Le professeur
            // répondait « je les retire », mais le bloc ne savait qu'AJOUTER : le programme gardait les trois notions et
            // la préparation plafonnait à 20 %. Le champ `retirer:` (mêmes points-virgules que `notions:`) dit ce qui
            // sort du programme.
            var retirees = LecteurControleProgramme.DecouperNotions(
                LecteurBloc.Valeur(champs, "retirer", "retirees", "retire", "a_retirer", "notions_retirees", "hors_programme"));

            var notions = LecteurControleProgramme.DecouperNotions(
                LecteurBloc.Valeur(champs, "notions", "notion"));

            // UNE NOTION ÉCRITE À LA FOIS DANS `notions:` ET DANS `retirer:` (il a recopié le tableau entier, et la
            // notion refusée avec) : CONFLIT, on ne supprime RIEN — verdict d'Omar, #67 v2. Sur un bac blanc de test,
            // le prof a recopié « Identifier un couple acide-base… une espèce amphotère… » dans les deux champs alors
            // que l'élève ne retirait que les amphotères : la notion entière serait partie. Elle reste, le conflit va au
            // journal, et le professeur le dit à l'élève au tour suivant.
            var posees = notions.Select(LibelleNotion.CleSouple).Where(c => c.Length > 0).ToHashSet();
            var conflits = retirees.Where(r => posees.Contains(LibelleNotion.CleSouple(r))).ToList();
            retirees = retirees.Where(r => !posees.Contains(LibelleNotion.CleSouple(r))).ToList();

            // Un bloc qui n'apporte NI sujet NI notion NI retrait ne dit rien :
            // mieux vaut l'ignorer que d'écrire une ligne vide.
            return sujet is null && notions.Count == 0 && retirees.Count == 0
                ? null
                : new ControleNotionsDeclarees(controle, sujet, notions, retirees, conflits);
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
