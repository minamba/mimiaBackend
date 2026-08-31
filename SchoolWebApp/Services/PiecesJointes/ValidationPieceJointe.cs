using UglyToad.PdfPig;

namespace SchoolWebApp.Api.Services.PiecesJointes
{
    /// <summary>Le verdict d'une validation, et le motif s'il est négatif.</summary>
    /// <param name="Accepte">Le fichier peut être envoyé au professeur.</param>
    /// <param name="Motif">
    /// Message destiné à l'ÉLÈVE, pas au journal. Il doit dire quoi faire —
    /// « prends une photo » — et non énoncer une contrainte technique.
    /// </param>
    /// <param name="NombrePages">Zéro pour une image.</param>
    public record VerdictPieceJointe(bool Accepte, string? Motif = null, int NombrePages = 0);

    /// <summary>
    /// Ce qu'un élève a le droit d'envoyer, et pourquoi ces limites-là.
    ///
    /// TOUT SE JOUE À L'ENTRÉE
    /// -----------------------
    /// Un document accepté entre dans l'historique, et l'historique est
    /// renvoyé au modèle À CHAQUE TOUR tant qu'il reste dans la fenêtre. Un
    /// manuel de deux cents pages accepté une fois ne coûte pas une fois : il
    /// coûte pendant toute la séance. C'est pour ça que le contrôle est ici,
    /// avant l'enregistrement, et pas au moment de l'envoi au modèle.
    ///
    /// LE TYPE N'EST PAS DÉDUIT DU NOM DU FICHIER
    /// -----------------------------------------
    /// Une extension se renomme. On lit les premiers octets — la signature du
    /// format — et on n'accepte que ce qu'on a réellement reconnu. Un fichier
    /// nommé « exercice.png » qui est en réalité une archive est refusé.
    /// </summary>
    public static class ValidationPieceJointe
    {
        /// <summary>
        /// Exactement les formats que le modèle sait lire. En accepter d'autres
        /// ne ferait que déplacer l'échec : l'élève verrait son envoi réussir,
        /// puis le professeur répondrait qu'il ne voit rien.
        /// </summary>
        public const string TypeJpeg = "image/jpeg";
        public const string TypePng = "image/png";
        public const string TypeGif = "image/gif";
        public const string TypeWebp = "image/webp";
        public const string TypePdf = "application/pdf";

        /// <summary>
        /// Cinq mégaoctets. Une photo de feuille prise au téléphone en fait
        /// deux à trois ; au-delà, c'est une photo inutilement pleine
        /// définition, qui ne rendra pas l'énoncé plus lisible pour autant.
        /// </summary>
        public const int TailleMax = 5 * 1024 * 1024;

        /// <summary>
        /// Dix pages. C'est un chapitre ou une feuille d'exercices, ce que
        /// l'élève travaille réellement dans une séance. Au-delà, c'est un
        /// manuel entier, et le professeur n'en lira jamais la fin.
        /// </summary>
        public const int PagesMax = 10;

        public static VerdictPieceJointe Valider(byte[] donnees, string? typeAnnonce)
        {
            if (donnees.Length == 0)
            {
                return new VerdictPieceJointe(false, "Le fichier est vide.");
            }

            if (donnees.Length > TailleMax)
            {
                return new VerdictPieceJointe(false,
                    "Ce fichier est trop lourd. Essaie une photo un peu moins grande.");
            }

            var typeReel = ReconnaitreType(donnees);

            if (typeReel is null)
            {
                return new VerdictPieceJointe(false,
                    "Je ne sais lire que les photos et les PDF. Prends une photo de ta feuille.");
            }

            // Le type annoncé par le navigateur n'est qu'un indice : c'est la
            // signature qui fait foi. On refuse quand même le désaccord, parce
            // qu'il n'a aucune raison légitime de se produire.
            if (!string.IsNullOrWhiteSpace(typeAnnonce)
                && !string.Equals(typeAnnonce, typeReel, StringComparison.OrdinalIgnoreCase))
            {
                return new VerdictPieceJointe(false, "Ce fichier ne correspond pas à son format.");
            }

            if (typeReel != TypePdf) return new VerdictPieceJointe(true, null, 0);

            return CompterPages(donnees);
        }

        /// <summary>Le type réel, lu dans les premiers octets. Null si inconnu.</summary>
        public static string? ReconnaitreType(byte[] o)
        {
            if (Commence(o, 0x25, 0x50, 0x44, 0x46)) return TypePdf;           // %PDF
            if (Commence(o, 0xFF, 0xD8, 0xFF)) return TypeJpeg;
            if (Commence(o, 0x89, 0x50, 0x4E, 0x47)) return TypePng;           // .PNG
            if (Commence(o, 0x47, 0x49, 0x46, 0x38)) return TypeGif;           // GIF8

            // WebP : « RIFF » puis quatre octets de taille, puis « WEBP ».
            if (o.Length >= 12 && Commence(o, 0x52, 0x49, 0x46, 0x46)
                && o[8] == 0x57 && o[9] == 0x45 && o[10] == 0x42 && o[11] == 0x50)
            {
                return TypeWebp;
            }

            return null;
        }

        private static bool Commence(byte[] o, params byte[] signature)
        {
            if (o.Length < signature.Length) return false;

            for (var i = 0; i < signature.Length; i++)
            {
                if (o[i] != signature[i]) return false;
            }

            return true;
        }

        private static VerdictPieceJointe CompterPages(byte[] donnees)
        {
            try
            {
                using var document = PdfDocument.Open(donnees);
                var pages = document.NumberOfPages;

                if (pages > PagesMax)
                {
                    return new VerdictPieceJointe(false,
                        $"Ce PDF fait {pages} pages, c'est trop pour une séance. "
                        + $"Envoie-moi seulement les pages qui t'intéressent ({PagesMax} au maximum).");
                }

                return new VerdictPieceJointe(true, null, pages);
            }
            catch (Exception)
            {
                // PDF protégé par mot de passe, tronqué, ou malformé. On ne
                // distingue pas les cas : l'élève n'a rien à en faire, et le
                // conseil est le même.
                return new VerdictPieceJointe(false,
                    "Je n'arrive pas à ouvrir ce PDF. Essaie d'en prendre une photo.");
            }
        }
    }
}
