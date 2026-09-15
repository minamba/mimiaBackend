using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Ce que le contrôle a donné, une fois passé.</summary>
    public record ControleResultatDeclare(
        int Controle,
        double? Note,
        string? Ressenti,
        IReadOnlyList<string> Reussies,
        IReadOnlyList<string> Ratees);

    /// <summary>
    /// Extrait le bloc [CONTROLE_RESULTAT] : le professeur fait le point après
    /// le contrôle.
    ///
    /// UN SEUL BLOC POUR DEUX MOMENTS. L'élève revient et raconte comment ça
    /// s'est passé (« c'était plus dur que prévu ») ; deux semaines plus tard,
    /// il rapporte sa copie corrigée et la photographie. Ce sont deux
    /// occasions de remplir le même objet, jamais deux objets : ce qui est
    /// connu se pose, le reste attend. Un bloc par étape aurait obligé à
    /// décider laquelle fait foi quand les deux existent.
    ///
    /// LA NOTE EST LUE, PAS JUGÉE. Contrairement à celle d'une évaluation
    /// Mimia — recalculée depuis les verdicts, parce que le modèle se trompait
    /// en l'agrégeant — celle-ci est écrite sur la copie par le professeur de
    /// l'école. Le modèle la recopie. On la borne quand même : un « 145/20 »
    /// lu de travers sur une photo floue ne doit pas entrer en base.
    /// </summary>
    public static partial class LecteurControleResultat
    {
        [GeneratedRegex(@"\[CONTROLE_RESULTAT\](?<corps>.*?)\[/CONTROLE_RESULTAT\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        [GeneratedRegex(@"\d+")]
        private static partial Regex Numero();

        /// <summary>Null s'il n'y a pas de bloc, ou pas de numéro de contrôle lisible.</summary>
        public static ControleResultatDeclare? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            var brutControle = LecteurBloc.Valeur(champs, "controle", "controle_id", "numero");
            if (brutControle is null) return null;

            var numero = Numero().Match(brutControle);
            if (!numero.Success || !int.TryParse(numero.Value, out var controle)) return null;

            return new ControleResultatDeclare(
                controle,
                // `Note` borne déjà à 0-20 et tolère « 14/20 », « 14,5 ».
                LecteurBloc.Note(champs, "note"),
                LecteurBloc.Valeur(champs, "ressenti", "comment"),
                LecteurControleProgramme.DecouperNotions(LecteurBloc.Valeur(champs, "reussies", "reussie")),
                LecteurControleProgramme.DecouperNotions(LecteurBloc.Valeur(champs, "ratees", "ratee", "a_revoir")));
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
