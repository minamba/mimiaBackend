using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une fiche de révision telle que le professeur l'a rédigée.</summary>
    public record FicheDeclaree(string Notion, string? Domaine, string Contenu, string? Etat);

    /// <summary>
    /// Extrait le bloc [FICHE] d'un message du professeur.
    ///
    /// Lecture différente des autres blocs : l'en-tête suit bien la forme
    /// `clé: valeur`, mais le CORPS est du texte libre sur plusieurs lignes —
    /// des titres, des puces, des exemples. Le passer dans l'analyseur clé/valeur
    /// le collerait tout entier sur la dernière clé rencontrée. On coupe donc à
    /// la ligne `contenu:` et on prend la suite telle quelle.
    /// </summary>
    public static partial class LecteurFiche
    {
        [GeneratedRegex(@"\[FICHE\](?<corps>.*?)\[/FICHE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>Repère la ligne qui ouvre le corps, quelle qu'en soit la casse.</summary>
        [GeneratedRegex(@"^\s*contenu\s*:\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex Separateur();

        /// <summary>
        /// Toutes les fiches d'un message.
        ///
        /// Le message de conclusion en porte une par notion travaillée, donc
        /// parfois deux ou trois. N'en lire qu'une perdrait silencieusement les
        /// autres.
        /// </summary>
        public static IReadOnlyList<FicheDeclaree> LireToutes(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return [];

            var fiches = new List<FicheDeclaree>();

            foreach (Match bloc in Bloc().Matches(message))
            {
                var fiche = LireBloc(bloc.Groups["corps"].Value);
                if (fiche is not null) fiches.Add(fiche);
            }

            return fiches;
        }

        /// <summary>La première fiche du message, ou null.</summary>
        public static FicheDeclaree? Lire(string? message) =>
            LireToutes(message).FirstOrDefault();

        private static FicheDeclaree? LireBloc(string corps)
        {
            var coupure = Separateur().Match(corps);

            // Sans séparateur, on ne sait pas où finit l'en-tête et où commence
            // la fiche. Mieux vaut ne rien enregistrer qu'une fiche dont le
            // titre serait noyé dans le contenu.
            if (!coupure.Success) return null;

            var champs = LecteurBloc.Champs(corps[..coupure.Index]);
            var notion = LecteurBloc.Valeur(champs, "notion", "titre");
            var contenu = corps[(coupure.Index + coupure.Length)..].Trim();

            if (string.IsNullOrWhiteSpace(notion) || string.IsNullOrWhiteSpace(contenu))
            {
                return null;
            }

            return new FicheDeclaree(
                notion,
                LecteurBloc.Valeur(champs, "domaine"),
                contenu,
                LecteurBloc.Valeur(champs, "etat", "état", "statut"));
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
