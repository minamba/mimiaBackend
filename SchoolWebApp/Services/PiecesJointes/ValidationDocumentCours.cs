namespace SchoolWebApp.Api.Services.PiecesJointes
{
    /// <summary>Le verdict sur un document de cours de contrôle : accepté avec son type réel, ou refusé avec son motif.</summary>
    public record VerdictDocumentCours(bool Accepte, string? Motif = null, string? TypeMime = null, int NombrePages = 0);

    /// <summary>
    /// CE QU'UN ÉLÈVE PEUT IMPORTER COMME COURS DE CONTRÔLE — Camara, le 04/11/2026 : « une
    /// image, un PDF, Word, Excel, PowerPoint ». Les images et les PDF passent par les règles
    /// des pièces jointes (5 Mo, 10 pages, type lu dans les octets) ; les documents Office
    /// récents par leur archive (10 Mo). Les anciens formats Office sont refusés avec le conseil.
    /// </summary>
    public static class ValidationDocumentCours
    {
        /// <summary>Le plus gros fichier accepté, tous formats confondus : celui des documents Office.</summary>
        public const int TailleMax = ExtracteurTexteOffice.TailleMax;

        public static VerdictDocumentCours Valider(byte[] donnees, string? typeAnnonce)
        {
            if (donnees.Length == 0) return new VerdictDocumentCours(false, "Le fichier est vide.");

            var office = ExtracteurTexteOffice.ReconnaitreType(donnees);

            if (office is not null)
            {
                if (donnees.Length > ExtracteurTexteOffice.TailleMax)
                {
                    return new VerdictDocumentCours(false, "Ce document est trop lourd (10 Mo au plus). Enlève les images les plus grosses, ou enregistre-le en PDF.");
                }

                return new VerdictDocumentCours(true, null, office, ExtracteurTexteOffice.CompterPages(donnees, office));
            }

            if (ExtracteurTexteOffice.EstAncienFormatOffice(donnees))
            {
                return new VerdictDocumentCours(false,
                    "Ce fichier est dans un ancien format (Word, Excel ou PowerPoint 97-2003). "
                    + "Enregistre-le en .docx, .xlsx ou .pptx, ou en PDF, puis réessaie.");
            }

            // Une archive qui n'est pas un document Office (zip quelconque) : refusée comme inconnue.
            var verdict = ValidationPieceJointe.Valider(donnees, typeAnnonce);
            if (!verdict.Accepte)
            {
                var motif = verdict.Motif == "Je ne sais lire que les photos et les PDF. Prends une photo de ta feuille."
                    ? "Je ne sais lire que les photos, les PDF et les documents Word, Excel ou PowerPoint."
                    : verdict.Motif;
                return new VerdictDocumentCours(false, motif);
            }

            return new VerdictDocumentCours(true, null, ValidationPieceJointe.ReconnaitreType(donnees), verdict.NombrePages);
        }
    }
}
