using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Ce qu'une séance de préparation apprend sur un contrôle.</summary>
    public record ControleNotionsDeclarees(
        int Controle, string? Sujet, IReadOnlyList<string> Notions);

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

            var notions = LecteurControleProgramme.DecouperNotions(
                LecteurBloc.Valeur(champs, "notions", "notion"));

            // Un bloc qui n'apporte NI sujet NI notion ne dit rien : mieux
            // vaut l'ignorer que d'écrire une ligne vide.
            return sujet is null && notions.Count == 0
                ? null
                : new ControleNotionsDeclarees(controle, sujet, notions);
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
