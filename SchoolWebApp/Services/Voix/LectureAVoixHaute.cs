using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// L'EXERCICE DE LECTURE À VOIX HAUTE — Camara, le 06/11/2026, sur le signalement de Hilda (CP) : « Adrien
    /// propose un texte de 3 phrases. La première est proposée en lecture autonome, mais pour les suivantes l'enfant
    /// entend la phrase avant de devoir la lire. Cela fausse l'exercice : l'enfant ne lit plus, il répète. »
    /// Du CP au CM2, en français.
    ///
    /// LE TEXTE À LIRE VA AU TABLEAU, qui n'est jamais prononcé, et JAMAIS dans la bulle, qui l'est. Le professeur
    /// ouvre l'exercice avec la balise <see cref="Balise"/> ; à chaque tour ensuite, il reçoit le texte au tableau et
    /// la règle (voir <see cref="Rappel"/>) : une règle lue plus haut ne vaut pas un fait au tour même. Le navigateur,
    /// lui, retire de la voix toute phrase du texte que l'élève n'a pas encore lue (`lib/lectureAVoixHaute.js`) :
    /// même si le modèle se trompe, la réponse n'est jamais soufflée.
    ///
    /// APRÈS L'ESSAI DE L'ÉLÈVE, le professeur peut relire la phrase qu'il vient de lire, pour le corriger : voulu
    /// par Camara le même jour.
    /// </summary>
    public static partial class LectureAVoixHaute
    {
        /// <summary>Écrite seule par le professeur au début de l'exercice ; ni affichée, ni dite.</summary>
        public const string Balise = "[EXERCICE_LECTURE]";

        /// <summary>
        /// Devant ce que l'élève a lu au micro pendant l'exercice (06/11/2026) : sa bulle n'affiche que « Lu à voix
        /// haute », comme un son jugé ; la transcription suit, pour le professeur.
        /// </summary>
        public const string MarqueurLu = "[LU_A_VOIX_HAUTE]";

        /// <summary>Au-delà de ces messages sans balise ni consigne de lecture, l'exercice est fini.</summary>
        public const int MessagesSuivis = 12;

        /// <summary>« Lis la phrase », « lis la deuxième ligne », « relis le texte » : le professeur fait lire.</summary>
        // L'ANGLAIS AUSSI (06/11/2026) : Marine dit « lis la phrase » comme « read the sentence ».
        [GeneratedRegex(@"\b(?:(?:re)?lis\b(?:-la|-le|-les)?[^.!?\n]{0,40}\b(?:phrases?|texte|lignes?|histoire|paragraphe|voix haute)|(?:re)?read\b[^.!?\n]{0,40}\b(?:sentences?|text|lines?|story|aloud|out loud))\b", RegexOptions.IgnoreCase)]
        private static partial Regex FaitLire();

        [GeneratedRegex(@"\[ARDOISE\]([\s\S]*?)\[/ARDOISE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Tableau();

        /// <summary>Seulement à l'élémentaire, en français et en anglais (Camara, 06/11/2026 : « pareil pour l'anglais »).</summary>
        public static bool Concerne(string? niveauCycle, string? matiereCode) =>
            string.Equals(niveauCycle, "Primaire", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(matiereCode, "FRANCAIS", StringComparison.OrdinalIgnoreCase) || EnAnglais(matiereCode));

        public static bool EnAnglais(string? matiereCode) =>
            string.Equals(matiereCode, "ANGLAIS", StringComparison.OrdinalIgnoreCase);

        /// <summary>L'exercice est-il en cours ? Les messages du professeur, du plus ancien au plus récent.</summary>
        public static bool EnCours(IEnumerable<string?> messagesDuProfesseur)
        {
            // LA LECTURE EN IMAGES N'EST PAS CET EXERCICE — production, 07/11/2026 : « lis à voix haute la syllabe en
            // couleur » (LectureDImage) le réveillait, et le professeur « réécrivait la syllabe au tableau » à chaque
            // tour. Le dernier exercice ouvert l'emporte (même règle que lib/lectureAVoixHaute.js).
            foreach (var m in messagesDuProfesseur.TakeLast(MessagesSuivis).Reverse())
            {
                if (m is null) continue;
                if (LectureDImage.Affiche(m) is not null || m.Contains(LectureDImage.BaliseChoix[..^1], StringComparison.OrdinalIgnoreCase)) return false;
                if (m.Contains(Balise, StringComparison.OrdinalIgnoreCase) || FaitLire().IsMatch(TexteHorsTableau(m))) return true;
            }

            return false;
        }

        /// <summary>Le texte à lire : le dernier tableau écrit par le professeur, ou null.</summary>
        public static string? TexteAuTableau(IEnumerable<string?> messagesDuProfesseur)
        {
            foreach (var message in messagesDuProfesseur.Reverse())
            {
                if (message is null) continue;
                var blocs = Tableau().Matches(message);
                for (var i = blocs.Count - 1; i >= 0; i--)
                {
                    var texte = blocs[i].Groups[1].Value.Trim();
                    if (texte.Length > 0) return texte;
                }
            }

            return null;
        }

        private static string TexteHorsTableau(string message) => Tableau().Replace(message, " ");

        /// <summary>LA RÈGLE, À CHAQUE TOUR DE L'EXERCICE, avec le texte au tableau s'il y en a un.</summary>
        public static string Rappel(string? texteAuTableau, bool anglais = false) =>
            "[Exercice de lecture à voix haute en cours" + (anglais ? ", en anglais" : string.Empty) + ". "
            + (string.IsNullOrWhiteSpace(texteAuTableau)
                ? "Écris d'abord le texte à lire au tableau, dans un bloc [ARDOISE], une phrase par ligne. "
                : $"Le texte à lire est au tableau : « {Compacter(texteAuTableau)} ». ")
            + "L'élève LIT, il ne répète pas : n'écris JAMAIS une phrase de ce texte dans ton message hors du tableau, "
            + "ne la cite pas, ne la mets pas entre guillemets" + (anglais ? " ni dans une balise [EN]" : string.Empty)
            + ", ne la prononce pas avant qu'il l'ait lue. "
            + "Fais lire UNE phrase à la fois : « Lis la première phrase au tableau », « Lis la phrase suivante ». "
            + "Ce qu'il a lu au micro t'arrive après « " + MarqueurLu + " » (l'élève, lui, voit seulement « Lu à voix haute ») ; "
            + "le micro corrige souvent les petites erreurs : juge surtout les mots "
            + "sautés, ajoutés ou remplacés. Après SON essai seulement, tu peux relire la phrase qu'il vient de lire pour "
            + "lui montrer la bonne lecture ; jamais la suivante. Un nouveau texte va au tableau dans un nouveau bloc [ARDOISE].]";

        private static string Compacter(string texte) =>
            Regex.Replace(texte, @"\s*\n\s*", " / ").Trim();
    }
}
