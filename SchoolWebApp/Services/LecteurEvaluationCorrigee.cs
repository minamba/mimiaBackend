using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Extrait le bloc [EVALUATION_CORRIGEE] : la correction d'une évaluation
    /// remise au cours suivant vient d'avoir lieu — ou l'élève n'a pas voulu la
    /// faire. Dans les deux cas, on n'en reparle plus.
    ///
    /// UN SEUL BLOC POUR LES DEUX ISSUES, et c'est voulu : « on n'impose rien »
    /// est la règle du produit. Un enfant qui refuse de revenir sur sa copie ne
    /// doit pas se la voir reproposer au cours d'après, et le professeur n'a pas
    /// à distinguer « faite » de « refusée » pour la clore — l'une comme
    /// l'autre met fin à l'attente.
    ///
    /// LE NUMÉRO NE S'INVENTE PAS : il est donné par le serveur dans le
    /// contexte de la séance. Un bloc sans numéro lisible est ignoré.
    /// </summary>
    public static partial class LecteurEvaluationCorrigee
    {
        [GeneratedRegex(@"\[EVALUATION_CORRIGEE\]\s*(?<numero>\d+)\s*\[/EVALUATION_CORRIGEE\]",
            RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>Le numéro de l'évaluation close, ou null s'il n'y a pas de bloc lisible.</summary>
        public static int? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            return int.TryParse(bloc.Groups["numero"].Value, out var numero) ? numero : null;
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
