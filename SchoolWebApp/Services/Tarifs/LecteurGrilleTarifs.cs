using System.Globalization;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Tarifs
{
    /// <summary>Les prix lus pour un modèle. Chaque champ peut manquer.</summary>
    public sealed record PrixLus(decimal? Entree, decimal? Sortie, decimal? Minute);

    /// <summary>
    /// LIT LES PRIX DANS LES PAGES DE TARIFS PUBLIQUES D'ANTHROPIC ET D'OPENAI —
    /// voulu par Camara le 19/09/2026 : « un worker qui va lire les tarifs pour
    /// que nos calculs soient toujours bons ».
    ///
    /// AUCUN DES DEUX NE PUBLIE SES TARIFS PAR UNE API. Mais tous deux servent
    /// leur page de tarifs en markdown (l'adresse suivie de « .md »), avec des
    /// tableaux simples : une ligne par modèle, une cellule par prix. On lit donc
    /// ces tableaux avec du code, sans modèle de langage — un tarif mal lu par
    /// une IA fausserait tous nos coûts sans que rien ne le signale.
    ///
    /// SI LA MISE EN PAGE CHANGE, LA LECTURE ÉCHOUE FRANCHEMENT : le modèle
    /// n'est pas trouvé, la veille le signale sur Telegram, et le dernier prix
    /// connu reste en place. Jamais de prix deviné.
    /// </summary>
    public static partial class LecteurGrilleTarifs
    {
        /// <summary>
        /// Jetons audio par minute de parole pour la voix d'OpenAI. Leur grille
        /// facture la voix au million de jetons audio (12 $) ; leur estimation
        /// publiée, 0,015 $ la minute, correspond à 1 250 jetons par minute.
        /// </summary>
        public const decimal JetonsAudioParMinute = 1250m;

        /// <summary>« $2 / MTok », « $0.0045 / minute », « $12.00 » → 2, 0.0045, 12.</summary>
        [GeneratedRegex(@"\$\s*([0-9]+(?:\.[0-9]+)?)")]
        private static partial Regex Montant();

        private static decimal? LireMontant(string? cellule)
        {
            if (string.IsNullOrWhiteSpace(cellule)) return null;
            var m = Montant().Match(cellule);
            return m.Success ? decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>Les cellules d'une ligne de tableau markdown, sans les bords.</summary>
        private static string[] Cellules(string ligne) =>
            ligne.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();

        /// <summary>
        /// « claude-sonnet-5 » → « Claude Sonnet 5 », « claude-haiku-4-5 » →
        /// « Claude Haiku 4.5 » : le nom que la page d'Anthropic affiche.
        /// </summary>
        public static string NomAnthropic(string modele)
        {
            var parties = modele.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parties.Length < 3) return modele;

            var famille = char.ToUpperInvariant(parties[1][0]) + parties[1][1..];
            return $"Claude {famille} {string.Join('.', parties[2..])}";
        }

        /// <summary>
        /// Anthropic : le PREMIER tableau de la page, celui dont l'en-tête porte
        /// « Base input tokens » et « Output tokens ». Les tableaux suivants
        /// (lots, mode rapide) portent les mêmes noms de modèles à d'autres prix :
        /// les lire ferait appliquer le tarif des lots à tout le monde.
        /// </summary>
        public static PrixLus? LireAnthropic(string markdown, string modele)
        {
            var nom = NomAnthropic(modele);
            var lignes = markdown.Split('\n');

            var entete = Array.FindIndex(lignes, l =>
                l.Contains("Base input tokens", StringComparison.OrdinalIgnoreCase)
                && l.Contains("Output tokens", StringComparison.OrdinalIgnoreCase));
            if (entete < 0) return null;

            var colonnes = Cellules(lignes[entete]);
            var iEntree = Array.FindIndex(colonnes, c => c.StartsWith("Base input", StringComparison.OrdinalIgnoreCase));
            var iSortie = Array.FindIndex(colonnes, c => c.StartsWith("Output", StringComparison.OrdinalIgnoreCase));
            if (iEntree < 0 || iSortie < 0) return null;

            for (var i = entete + 1; i < lignes.Length && lignes[i].TrimStart().StartsWith('|'); i++)
            {
                var cellules = Cellules(lignes[i]);
                if (cellules.Length <= Math.Max(iEntree, iSortie)) continue;

                // Le nom EXACT, pas un début : « Claude Opus 5 » ne doit pas
                // attraper une ligne « Claude Opus 5.1 » ajoutée un jour au-dessus.
                if (!string.Equals(cellules[0], nom, StringComparison.OrdinalIgnoreCase)) continue;

                var entree = LireMontant(cellules[iEntree]);
                var sortie = LireMontant(cellules[iSortie]);
                return entree is null || sortie is null ? null : new PrixLus(entree, sortie, null);
            }

            return null;
        }

        /// <summary>
        /// OpenAI, deux formes selon le modèle :
        ///   « | gpt-transcribe | Transcription | - | - | $0.0045 / minute | » → un prix à la minute ;
        ///   « | gpt-4o-mini-tts | Text | $0.60 | - | - | » et « | gpt-4o-mini-tts | Audio | - | - | $12.00 | »
        ///     → l'entrée texte, la sortie audio, et la minute qui s'en déduit.
        /// </summary>
        public static PrixLus? LireOpenAi(string markdown, string modele)
        {
            var lignes = markdown.Split('\n')
                .Where(l => l.TrimStart().StartsWith('|'))
                .Select(Cellules)
                .Where(c => c.Length >= 2 && string.Equals(c[0], modele, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (lignes.Count == 0) return null;

            // Un prix à la minute, écrit comme tel.
            foreach (var cellules in lignes)
            {
                var minute = cellules.FirstOrDefault(c => c.Contains("/ minute", StringComparison.OrdinalIgnoreCase));
                if (minute is not null && LireMontant(minute) is { } prix) return new PrixLus(null, null, prix);
            }

            // La voix : une ligne « Text » pour l'entrée, une ligne « Audio » pour la sortie.
            var texte = lignes.FirstOrDefault(c => string.Equals(c[1], "Text", StringComparison.OrdinalIgnoreCase));
            var audio = lignes.FirstOrDefault(c => string.Equals(c[1], "Audio", StringComparison.OrdinalIgnoreCase));

            var entree = texte is null ? null : texte.Skip(2).Select(LireMontant).FirstOrDefault(v => v is not null);
            var sortie = audio is null ? null : audio.Skip(2).Reverse().Select(LireMontant).FirstOrDefault(v => v is not null);

            if (sortie is null) return null;

            return new PrixLus(entree, sortie, Math.Round(sortie.Value * JetonsAudioParMinute / 1_000_000m, 5));
        }

        /// <summary>
        /// Les jetons audio que Google FACTURE par seconde de son : 39, mesurés le
        /// 28/09/2026 sur ses propres réponses (`usageMetadata`), quand sa page en
        /// annonce 25. On compte ce qui est facturé.
        /// </summary>
        public const decimal JetonsAudioParSecondeGemini = 39m;

        [GeneratedRegex(@"\$\s*([0-9]+(?:\.[0-9]+)?)\s*\((?:text|audio)\)\s*(through|starting)\s+([A-Z][a-z]+ [0-9]{1,2}, [0-9]{4})", RegexOptions.IgnoreCase)]
        private static partial Regex MontantDate();

        /// <summary>
        /// La page de Google (`ai.google.dev/gemini-api/docs/pricing.md.txt`) —
        /// une section par modèle, repérée par son identifiant écrit en code
        /// (`gemini-3.8-flash-lite-tts`), puis un tableau « ### Standard » :
        ///
        ///   | Input price  | Free of charge | $0.50 (text) through December 31, 2026. $1.00 (text) starting January 1, 2027. |
        ///   | Output price | Free of charge | $6.00 (audio) through December 31, 2026. $12.00 (audio) starting January 1, 2027. … |
        ///
        /// DEUX PRIX PAR CASE, DATÉS : on retient celui qui vaut le jour de la
        /// lecture (<paramref name="aujourdhui"/>). Le 1er janvier 2027, la veille
        /// basculera d'elle-même sur le second — c'est tout l'intérêt de la lire.
        /// La colonne payante est la dernière : la gratuite ne nous concerne pas.
        /// </summary>
        public static PrixLus? LireGemini(string markdown, string modele, DateTime aujourdhui)
        {
            var lignes = markdown.Split('\n');

            var debut = Array.FindIndex(lignes, l => l.Contains($"`{modele}`", StringComparison.OrdinalIgnoreCase));
            if (debut < 0) return null;

            // La section s'arrête au modèle suivant (« ## »).
            var fin = Array.FindIndex(lignes, debut + 1, l => l.StartsWith("## ", StringComparison.Ordinal));
            if (fin < 0) fin = lignes.Length;

            var standard = Array.FindIndex(lignes, debut, fin - debut,
                l => l.Trim().Equals("### Standard", StringComparison.OrdinalIgnoreCase));
            if (standard < 0) return null;

            decimal? Ligne(string intitule)
            {
                for (var i = standard + 1; i < fin; i++)
                {
                    if (lignes[i].StartsWith("### ", StringComparison.Ordinal)) break;
                    if (!lignes[i].TrimStart().StartsWith('|')) continue;

                    var cellules = Cellules(lignes[i]);
                    if (cellules.Length < 2 || !cellules[0].Equals(intitule, StringComparison.OrdinalIgnoreCase)) continue;

                    return PrixDuJour(cellules[^1], aujourdhui);
                }

                return null;
            }

            var entree = Ligne("Input price");
            var sortie = Ligne("Output price");
            if (sortie is null) return null;

            return new PrixLus(
                entree,
                sortie,
                Math.Round(sortie.Value * JetonsAudioParSecondeGemini * 60m / 1_000_000m, 5));
        }

        /// <summary>
        /// Le prix valable à cette date parmi « $X (…) through D. $Y (…) starting
        /// D2. », ou le seul montant de la case s'il n'est pas daté.
        /// </summary>
        private static decimal? PrixDuJour(string cellule, DateTime aujourdhui)
        {
            var jour = aujourdhui.Date;

            foreach (Match m in MontantDate().Matches(cellule))
            {
                if (!DateTime.TryParse(m.Groups[3].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var borne))
                {
                    continue;
                }

                var valable = m.Groups[2].Value.Equals("through", StringComparison.OrdinalIgnoreCase)
                    ? jour <= borne.Date
                    : jour >= borne.Date;

                if (valable) return decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            return MontantDate().IsMatch(cellule) ? null : LireMontant(cellule);
        }
    }
}
