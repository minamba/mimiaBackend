using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Le verdict du professeur sur la préparation d'une épreuve d'examen.</summary>
    public record ExamenPretDeclare(string Epreuve, string Verdict, string? Observation);

    /// <summary>
    /// Extrait le bloc [EXAMEN_PRET] — le pendant de [CONTROLE_PRET] pour une
    /// épreuve d'examen : à la fin d'une séance ouverte par le bouton de
    /// l'épreuve, le professeur dit si l'élève est prêt, et pourquoi.
    ///
    /// LA MÊME GRAMMAIRE DE VERDICTS que les contrôles, lue par la même
    /// fonction : deux analyseurs finiraient par accepter des choses
    /// différentes.
    ///
    /// L'épreuve se désigne par son CODE (DNB_2027_MATHS), qui vient du
    /// contexte de séance ; un code qui n'est pas celui de la séance est ignoré
    /// par l'appelant.
    /// </summary>
    public static partial class LecteurExamenPret
    {
        [GeneratedRegex(@"\[EXAMEN_PRET\](?<corps>.*?)\[/EXAMEN_PRET\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>Null s'il n'y a pas de bloc, pas de code d'épreuve, ou pas de verdict reconnu.</summary>
        public static ExamenPretDeclare? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            var epreuve = LecteurBloc.Valeur(champs, "epreuve", "code");
            if (string.IsNullOrWhiteSpace(epreuve)) return null;

            var verdict = LecteurControlePret.Traduire(LecteurBloc.Valeur(champs, "pret", "verdict", "statut"));
            if (verdict is null) return null;

            // L'OBSERVATION GARDE SES PARAGRAPHES : lue par `ChampLong`, et non
            // par `Champs`, qui les recollait en un seul bloc (voir LecteurBloc).
            var observation = LecteurBloc.ChampLong(
                bloc.Groups["corps"].Value,
                ["observation", "remarque"],
                ["epreuve", "code", "pret", "verdict", "statut", "observation", "remarque"]);

            return new ExamenPretDeclare(epreuve.Trim(), verdict, observation);
        }
    }
}
