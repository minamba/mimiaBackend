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

        public static VerdictPieceJointe Valider(
            byte[] donnees, string? typeAnnonce, int pagesMax = PagesMax, int tailleMax = TailleMax)
        {
            if (donnees.Length == 0)
            {
                return new VerdictPieceJointe(false, "Le fichier est vide.");
            }

            if (donnees.Length > tailleMax)
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
            //
            // SAUF QUAND L'ANNONCE NE DIT RIEN (25/11/2026) : un PDF reçu par WhatsApp ou par mail arrive souvent
            // annoncé « application/octet-stream » (« un fichier quelconque »), ou sous un ancien nom (« application/
            // x-pdf », « image/jpg »). Ce n'est pas un désaccord : la signature tranche seule. Une maman était bloquée
            // sur le bloc physique-chimie d'un brevet blanc.
            var annonce = NormaliserTypeAnnonce(typeAnnonce);
            if (annonce is not null && !string.Equals(annonce, typeReel, StringComparison.OrdinalIgnoreCase))
            {
                return new VerdictPieceJointe(false, "Ce fichier ne correspond pas à son format.");
            }

            if (typeReel != TypePdf) return new VerdictPieceJointe(true, null, 0);

            return CompterPages(donnees, pagesMax);
        }

        /// <summary>
        /// Les annonces qui ne disent rien du format : on les ignore, la signature décidera.
        /// </summary>
        private static readonly HashSet<string> AnnoncesSansValeur = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/octet-stream", "binary/octet-stream", "application/binary", "application/unknown",
            "application/x-download", "application/download", "application/force-download",
        };

        /// <summary>Les anciens noms d'un format, ramenés au nom que la signature donne.</summary>
        private static readonly Dictionary<string, string> AliasDeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["application/x-pdf"] = TypePdf,
            ["application/acrobat"] = TypePdf,
            ["applications/vnd.pdf"] = TypePdf,
            ["text/pdf"] = TypePdf,
            ["text/x-pdf"] = TypePdf,
            ["image/jpg"] = TypeJpeg,
            ["image/pjpeg"] = TypeJpeg,
            ["image/x-png"] = TypePng,
        };

        /// <summary>
        /// Le type annoncé, ramené à ce qui compare : null s'il ne dit rien (vide, « fichier quelconque »), son nom
        /// courant s'il porte un ancien nom. Les paramètres (« ; charset=… ») sont retirés.
        /// </summary>
        public static string? NormaliserTypeAnnonce(string? typeAnnonce)
        {
            if (string.IsNullOrWhiteSpace(typeAnnonce)) return null;

            var type = typeAnnonce.Split(';')[0].Trim();
            if (type.Length == 0 || AnnoncesSansValeur.Contains(type)) return null;

            return AliasDeTypes.TryGetValue(type, out var courant) ? courant : type;
        }

        /// <summary>
        /// Jusqu'où chercher « %PDF » : la norme le permet dans le premier kilo-octet, et les lecteurs l'acceptent.
        /// </summary>
        public const int FenetreEnTetePdf = 1024;

        /// <summary>
        /// La position de « %PDF » dans le premier kilo-octet : 0 pour un PDF ordinaire, plus si un logiciel a mis
        /// quelques octets devant (espaces, marque d'encodage, en-tête d'un scanner), -1 si ce n'est pas un PDF.
        /// </summary>
        public static int DebutPdf(byte[] o)
        {
            var limite = Math.Min(o.Length, FenetreEnTetePdf) - 4;
            for (var i = 0; i <= limite; i++)
            {
                if (o[i] == 0x25 && o[i + 1] == 0x50 && o[i + 2] == 0x44 && o[i + 3] == 0x46) return i;
            }

            return -1;
        }

        /// <summary>
        /// Un PDF dont « %PDF » arrive après quelques octets, rendu propre : les octets d'avant sont retirés, pour que
        /// tout ce qui suit (signature, lecture, modèle) voie un PDF ordinaire. Tout autre fichier est rendu tel quel.
        /// </summary>
        public static byte[] RognerAvantPdf(byte[] o)
        {
            // Jamais un fichier déjà reconnu (une photo dont les métadonnées contiendraient « %PDF »), ni un document
            // Office (une archive « PK ») : seuls les fichiers sans signature connue sont examinés.
            if (ReconnaitreType(o) is not null || (o.Length >= 2 && o[0] == 0x50 && o[1] == 0x4B)) return o;

            var debut = DebutPdf(o);
            return debut > 0 ? o[debut..] : o;
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

        private static VerdictPieceJointe CompterPages(byte[] donnees, int pagesMax)
        {
            try
            {
                using var document = PdfDocument.Open(donnees);
                var pages = document.NumberOfPages;

                if (pages > pagesMax)
                {
                    // Le plafond par défaut est celui d'une séance ; un autre
                    // (l'import d'un cours pour ses fiches de révision) a sa phrase.
                    return new VerdictPieceJointe(false,
                        pagesMax == PagesMax
                            ? $"Ce PDF fait {pages} pages, c'est trop pour une séance. "
                                + $"Envoie-moi seulement les pages qui t'intéressent ({pagesMax} au maximum)."
                            : $"Ce PDF fait {pages} pages, c'est trop pour un cours. "
                                + $"Garde seulement les pages du chapitre ({pagesMax} au maximum).");
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
