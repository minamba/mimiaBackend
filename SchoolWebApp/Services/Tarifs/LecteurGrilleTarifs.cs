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
    }
}
