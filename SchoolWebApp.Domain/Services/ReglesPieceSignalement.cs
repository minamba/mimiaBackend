namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// CE QU'ON ACCEPTE EN PIÈCE JOINTE D'UN SIGNALEMENT — Camara, le 03/11/2026 : « des
    /// images png, jpeg, et même un document autre ».
    ///
    /// UNE LISTE DE CE QUI EST PERMIS, ET NON DE CE QUI EST INTERDIT : ces fichiers
    /// viennent de comptes parents et enfants, et un administrateur les ouvre. Pas de
    /// SVG ni de HTML (ils exécutent du script dans le navigateur), pas d'exécutable.
    /// Le type servi vient d'ici, jamais de ce que le navigateur a déclaré, et une image
    /// doit en être une : ses premiers octets sont vérifiés.
    /// </summary>
    public static class ReglesPieceSignalement
    {
        public const int TailleMax = 10 * 1024 * 1024;

        /// <summary>Le corps de la requête : le fichier et l'enveloppe du formulaire.</summary>
        public const int TailleRequeteMax = TailleMax + (64 * 1024);

        public const int MaxParSignalement = 10;

        private static readonly Dictionary<string, string> Images = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
        };

        private static readonly Dictionary<string, string> Documents = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".odt"] = "application/vnd.oasis.opendocument.text",
            [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
            [".odp"] = "application/vnd.oasis.opendocument.presentation",
            [".zip"] = "application/zip",
        };

        /// <summary>Le nom sans chemin ni caractère de contrôle, jamais vide.</summary>
        public static string NomPropre(string? nom)
        {
            var seul = (nom ?? string.Empty).Replace('\\', '/');
            seul = seul[(seul.LastIndexOf('/') + 1)..];
            seul = new string(seul.Where(c => !char.IsControl(c)).ToArray()).Trim();

            if (seul.Length == 0) return "piece-jointe";
            if (seul.Length <= 200) return seul;

            var extension = Path.GetExtension(seul);
            return seul[..(200 - extension.Length)] + extension;
        }

        /// <summary>Null si le fichier est accepté, sinon la raison à afficher.</summary>
        public static string? Refus(string nom, byte[] octets, out string typeMime, out bool estImage)
        {
            typeMime = string.Empty;
            estImage = false;

            if (octets.Length == 0) return "Ce fichier est vide.";
            if (octets.Length > TailleMax) return "Ce fichier dépasse 10 Mo.";

            var extension = Path.GetExtension(nom);

            if (Images.TryGetValue(extension, out var image))
            {
                if (!EstVraimentUneImage(image, octets)) return "Ce fichier n'est pas une image valide.";

                typeMime = image;
                estImage = true;
                return null;
            }

            if (Documents.TryGetValue(extension, out var document))
            {
                typeMime = document;
                return null;
            }

            return "Ce type de fichier n'est pas accepté : images (PNG, JPEG, WebP, GIF), PDF, Word, Excel, PowerPoint, OpenDocument, texte, CSV ou ZIP.";
        }

        private static bool EstVraimentUneImage(string type, byte[] o) => type switch
        {
            "image/png" => Commence(o, 0x89, 0x50, 0x4E, 0x47),
            "image/jpeg" => Commence(o, 0xFF, 0xD8, 0xFF),
            "image/gif" => Commence(o, 0x47, 0x49, 0x46, 0x38),
            "image/webp" => o.Length > 12 && Commence(o, 0x52, 0x49, 0x46, 0x46)
                && o[8] == 0x57 && o[9] == 0x45 && o[10] == 0x42 && o[11] == 0x50,
            _ => false,
        };

        private static bool Commence(byte[] octets, params byte[] debut) =>
            octets.Length >= debut.Length && octets.AsSpan(0, debut.Length).SequenceEqual(debut);
    }
}
