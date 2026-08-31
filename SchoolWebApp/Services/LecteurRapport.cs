using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Le compte rendu tel que le professeur l'a déclaré en concluant.</summary>
    public record RapportDeclare(
        string? Travaille,
        double? NoteComprehension,
        double? NoteRevision,
        string? Remarque,
        string? ARevoir);

    /// <summary>
    /// Extrait le bloc [RAPPORT] d'un message du professeur.
    ///
    /// Plus tolérant que le lecteur d'évaluation : là où une évaluation SANS
    /// note n'a aucun sens, un rapport sans note de révision est un cas normal
    /// — c'est la séance qui n'a porté que sur du neuf. Un rapport n'est rejeté
    /// que s'il ne contient rien d'exploitable du tout.
    /// </summary>
    public static partial class LecteurRapport
    {
        [GeneratedRegex(@"\[RAPPORT\](?<corps>.*?)\[/RAPPORT\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        public static RapportDeclare? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            var rapport = new RapportDeclare(
                LecteurBloc.Valeur(champs, "travaille", "travaille_", "vu"),
                LecteurBloc.Note(champs, "comprehension"),
                // « non évalué », « pas de révision », « — » : aucun chiffre,
                // donc null. C'est exactement le cas qu'on veut distinguer d'un
                // zéro, et la lecture du nombre s'en charge toute seule.
                LecteurBloc.Note(champs, "revision"),
                LecteurBloc.Valeur(champs, "remarque"),
                LecteurBloc.Valeur(champs, "a_revoir", "arevoir"));

            // Un bloc vide de bout en bout ne vaut pas la peine d'être stocké :
            // il produirait une carte sans contenu dans la fiche.
            var vide = rapport.Travaille is null
                       && rapport.NoteComprehension is null
                       && rapport.NoteRevision is null
                       && rapport.Remarque is null;

            return vide ? null : rapport;
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
