namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Ce qu'une pièce jointe d'idée sait faire à l'écran — voulu par Camara le
    /// 17/09/2026 : « si c'est une image, on l'affiche, si c'est un autre
    /// document, il sera juste mis en pj téléchargeable », et un audio « qu'on
    /// va pouvoir lire directement dans l'aperçu ».
    ///
    /// DÉDUIT DU TYPE MIME, PAS STOCKÉ EN COLONNE.
    /// ------------------------------------------
    /// Le genre n'est pas une décision qu'on prend au dépôt, c'est une lecture
    /// du fichier : un `audio/mpeg` est un audio, aujourd'hui et dans dix ans.
    /// Une colonne aurait dû être remplie, migrée, et surtout maintenue en
    /// accord avec le type — trois occasions de diverger pour une information
    /// qui se relit en une ligne.
    /// </summary>
    public static class GenrePiece
    {
        /// <summary>Montrée dans le texte, à l'endroit de son `[image:N]`.</summary>
        public const string Image = "image";

        /// <summary>Écoutée sur place, dans un lecteur.</summary>
        public const string Audio = "audio";

        /// <summary>Tout le reste : téléchargé, jamais ouvert dans la page.</summary>
        public const string Document = "document";

        public static string Deduire(string? typeMime)
        {
            var type = typeMime?.Trim().ToLowerInvariant() ?? string.Empty;

            // LE SVG EST UNE IMAGE QUI EXÉCUTE DU SCRIPT. Il n'entre pas dans la
            // liste blanche du contrôleur, mais si une ligne ancienne en portait
            // un, le classer « document » suffit à ce qu'il soit téléchargé au
            // lieu d'être affiché dans la page.
            if (type == "image/svg+xml") return Document;

            if (type.StartsWith("image/", System.StringComparison.Ordinal)) return Image;
            if (type.StartsWith("audio/", System.StringComparison.Ordinal)) return Audio;

            return Document;
        }
    }
}
