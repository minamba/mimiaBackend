using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Jeux
{
    /// <summary>
    /// La balise par laquelle le professeur propose un jeu : <c>[JEU]CE1/course-des-tables[/JEU]</c>.
    ///
    /// UNE PAIRE EN MAJUSCULES, comme [FICHE] ou [RAPPORT], et non « [JEU:cle] » :
    /// les archives d'un départ anticipé ne conservent que les blocs de la
    /// forme <c>[NOM]…[/NOM]</c> (voir <c>BlocsArchive</c> dans le constructeur
    /// de vue), et une balise à deux-points y serait perdue.
    ///
    /// Le contenu est l'identifiant « classe/cle » du catalogue, recopié tel
    /// quel depuis la liste que la consigne fournit. C'est le catalogue qui
    /// valide : une balise qui n'y correspond pas est retirée du message.
    /// </summary>
    public static partial class LecteurJeu
    {
        [GeneratedRegex(@"\[JEU\]\s*(?<id>[A-Za-z0-9_]+/[a-z0-9-]+)\s*\[/JEU\]", RegexOptions.IgnoreCase)]
        private static partial Regex Balise();

        /// <summary>Tout ce qui ressemble à la balise, bien formé ou non — pour nettoyer.</summary>
        [GeneratedRegex(@"\[JEU\].*?\[/JEU\]", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>
        /// L'identifiant proposé, ou <c>null</c>. LA DERNIÈRE occurrence l'emporte,
        /// comme pour la vitesse d'écoute : un modèle qui se reprend laisse
        /// l'élève sur ce qu'il a annoncé en dernier.
        /// </summary>
        public static string? Lire(string? message)
        {
            if (string.IsNullOrEmpty(message)) return null;

            var trouvees = Balise().Matches(message);
            return trouvees.Count > 0 ? trouvees[^1].Groups["id"].Value.Trim() : null;
        }

        /// <summary>Le message sans sa balise (ni ses variantes mal formées).</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
