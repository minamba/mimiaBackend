using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une évaluation proposée par le professeur, reportée à la prochaine fois.</summary>
    public record EvaluationPrevueDeclaree(string? Notion);

    /// <summary>
    /// Extrait le bloc [EVALUATION_PREVUE] d'un message du professeur.
    ///
    /// Aucun champ obligatoire : `notion` manquant vaut mieux qu'un bloc
    /// rejeté — l'élève aura quand même une ligne « évaluation à venir »
    /// dans son calendrier, même sans savoir dire sur quoi précisément.
    /// </summary>
    public static partial class LecteurEvaluationPrevue
    {
        [GeneratedRegex(@"\[EVALUATION_PREVUE\](?<corps>.*?)\[/EVALUATION_PREVUE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>Null s'il n'y a pas de bloc.</summary>
        public static EvaluationPrevueDeclaree? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            return new EvaluationPrevueDeclaree(LecteurBloc.Valeur(champs, "notion"));
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
