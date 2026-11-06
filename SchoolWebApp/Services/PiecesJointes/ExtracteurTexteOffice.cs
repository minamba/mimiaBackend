using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace SchoolWebApp.Api.Services.PiecesJointes
{
    /// <summary>
    /// LE TEXTE D'UN DOCUMENT WORD, EXCEL OU POWERPOINT, SANS MODÈLE — Camara, le 04/11/2026 :
    /// le cours d'un contrôle « peut être une image, un PDF, Word, Excel, PowerPoint ». Les
    /// formats récents (.docx, .xlsx, .pptx) sont des archives ZIP de XML : on en sort le texte
    /// ici, en quelques millisecondes, et c'est cette extraction qui tient lieu de transcription
    /// pour le professeur. Rien ne part chez un fournisseur pour ces fichiers.
    ///
    /// LES ANCIENS FORMATS (.doc, .xls, .ppt) SONT REFUSÉS : fichiers binaires fermés, qu'il
    /// faudrait une bibliothèque entière pour lire. L'élève les enregistre « au format récent ».
    /// </summary>
    public static class ExtracteurTexteOffice
    {
        public const string TypeWord = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        public const string TypeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public const string TypePowerPoint = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

        /// <summary>Dix mégaoctets : un cours avec ses images, pas un manuel.</summary>
        public const int TailleMax = 10 * 1024 * 1024;

        /// <summary>
        /// Au-delà, le texte est coupé : il part dans le contexte de chaque séance de
        /// préparation, et un cours de contrôle n'a pas cinquante pages.
        /// </summary>
        public const int CaracteresMax = 40_000;

        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
        private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        /// <summary>Le type réel d'une archive Office, lu dans ses entrées ; null si ce n'en est pas une.</summary>
        public static string? ReconnaitreType(byte[] octets)
        {
            // Une archive ZIP commence par « PK\003\004 » ; un .doc/.xls/.ppt par D0 CF 11 E0.
            if (octets.Length < 4 || octets[0] != 0x50 || octets[1] != 0x4B || octets[2] != 0x03 || octets[3] != 0x04) return null;

            try
            {
                using var archive = new ZipArchive(new MemoryStream(octets, writable: false), ZipArchiveMode.Read);
                if (archive.GetEntry("word/document.xml") is not null) return TypeWord;
                if (archive.GetEntry("xl/workbook.xml") is not null) return TypeExcel;
                if (archive.GetEntry("ppt/presentation.xml") is not null) return TypePowerPoint;
            }
            catch (InvalidDataException)
            {
            }

            return null;
        }

        /// <summary>Un ancien format Office (Word 97-2003…) : à refuser avec le bon conseil.</summary>
        public static bool EstAncienFormatOffice(byte[] octets) =>
            octets.Length >= 8 && octets[0] == 0xD0 && octets[1] == 0xCF && octets[2] == 0x11 && octets[3] == 0xE0;

        public static bool EstOffice(string? typeMime) => typeMime is TypeWord or TypeExcel or TypePowerPoint;

        /// <summary>Le texte du document, par paragraphes, lignes ou diapositives ; vide si rien de lisible.</summary>
        public static string Extraire(byte[] octets, string typeMime)
        {
            using var archive = new ZipArchive(new MemoryStream(octets, writable: false), ZipArchiveMode.Read);

            var texte = typeMime switch
            {
                TypeWord => Word(archive),
                TypeExcel => Excel(archive),
                TypePowerPoint => PowerPoint(archive),
                _ => string.Empty,
            };

            texte = texte.Trim();
            return texte.Length <= CaracteresMax ? texte : texte[..CaracteresMax] + "\n(suite coupée : document trop long)";
        }

        /// <summary>Le nombre de « pages » annoncé à l'écran : diapositives, feuilles, ou 0 pour Word.</summary>
        public static int CompterPages(byte[] octets, string typeMime)
        {
            try
            {
                using var archive = new ZipArchive(new MemoryStream(octets, writable: false), ZipArchiveMode.Read);
                return typeMime switch
                {
                    TypePowerPoint => Diapositives(archive).Count,
                    TypeExcel => Feuilles(archive).Count,
                    _ => 0,
                };
            }
            catch (InvalidDataException)
            {
                return 0;
            }
        }

        private static string Word(ZipArchive archive)
        {
            var document = Lire(archive, "word/document.xml");
            if (document is null) return string.Empty;

            var sortie = new StringBuilder();

            foreach (var paragraphe in document.Descendants(W + "p"))
            {
                var ligne = new StringBuilder();

                foreach (var noeud in paragraphe.Descendants())
                {
                    if (noeud.Name == W + "t") ligne.Append(noeud.Value);
                    else if (noeud.Name == W + "tab") ligne.Append('\t');
                    else if (noeud.Name == W + "br" || noeud.Name == W + "cr") ligne.Append('\n');
                }

                sortie.AppendLine(ligne.ToString());
            }

            return sortie.ToString();
        }

        private static string Excel(ZipArchive archive)
        {
            var partagees = Lire(archive, "xl/sharedStrings.xml")?
                .Descendants(S + "si")
                .Select(si => string.Concat(si.Descendants(S + "t").Select(t => t.Value)))
                .ToList() ?? new List<string>();

            var sortie = new StringBuilder();

            foreach (var (nom, entree) in Feuilles(archive))
            {
                var feuille = Lire(archive, entree);
                if (feuille is null) continue;

                sortie.AppendLine($"## {nom}");

                foreach (var rangee in feuille.Descendants(S + "row"))
                {
                    var cellules = rangee.Elements(S + "c").Select(c =>
                    {
                        var type = (string?)c.Attribute("t");
                        if (type == "s" && int.TryParse(c.Element(S + "v")?.Value, out var index) && index >= 0 && index < partagees.Count)
                        {
                            return partagees[index];
                        }

                        if (type == "inlineStr") return string.Concat(c.Descendants(S + "t").Select(t => t.Value));
                        return c.Element(S + "v")?.Value ?? string.Empty;
                    }).ToList();

                    if (cellules.Any(v => v.Length > 0)) sortie.AppendLine(string.Join('\t', cellules));
                }

                sortie.AppendLine();
            }

            return sortie.ToString();
        }

        private static string PowerPoint(ZipArchive archive)
        {
            var sortie = new StringBuilder();
            var numero = 0;

            foreach (var entree in Diapositives(archive))
            {
                numero += 1;
                var diapositive = Lire(archive, entree);
                if (diapositive is null) continue;

                sortie.AppendLine($"## Diapositive {numero}");

                foreach (var paragraphe in diapositive.Descendants(A + "p"))
                {
                    var ligne = string.Concat(paragraphe.Descendants(A + "t").Select(t => t.Value));
                    if (ligne.Trim().Length > 0) sortie.AppendLine(ligne);
                }

                sortie.AppendLine();
            }

            return sortie.ToString();
        }

        /// <summary>Les diapositives dans l'ordre de leur numéro (slide1, slide2, … slide10).</summary>
        private static List<string> Diapositives(ZipArchive archive) =>
            archive.Entries
                .Select(e => e.FullName)
                .Where(n => n.StartsWith("ppt/slides/slide", StringComparison.Ordinal) && n.EndsWith(".xml", StringComparison.Ordinal))
                .Select(n => (nom: n, numero: int.TryParse(n["ppt/slides/slide".Length..^4], out var k) ? k : int.MaxValue))
                .OrderBy(x => x.numero).ThenBy(x => x.nom, StringComparer.Ordinal)
                .Select(x => x.nom)
                .ToList();

        /// <summary>Les feuilles dans l'ordre du classeur, avec leur nom.</summary>
        private static List<(string Nom, string Entree)> Feuilles(ZipArchive archive)
        {
            var classeur = Lire(archive, "xl/workbook.xml");
            var relations = Lire(archive, "xl/_rels/workbook.xml.rels");
            if (classeur is null) return new List<(string, string)>();

            XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace rels = "http://schemas.openxmlformats.org/package/2006/relationships";

            var cibles = relations?.Descendants(rels + "Relationship")
                .ToDictionary(x => (string?)x.Attribute("Id") ?? string.Empty, x => (string?)x.Attribute("Target") ?? string.Empty)
                ?? new Dictionary<string, string>();

            var sortie = new List<(string, string)>();

            foreach (var feuille in classeur.Descendants(S + "sheet"))
            {
                var id = (string?)feuille.Attribute(r + "id") ?? string.Empty;
                if (!cibles.TryGetValue(id, out var cible)) continue;

                var entree = cible.StartsWith('/') ? cible[1..] : "xl/" + cible;
                sortie.Add(((string?)feuille.Attribute("name") ?? "Feuille", entree));
            }

            return sortie;
        }

        private static XDocument? Lire(ZipArchive archive, string nom)
        {
            var entree = archive.GetEntry(nom);
            if (entree is null) return null;

            try
            {
                using var flux = entree.Open();
                return XDocument.Load(flux, LoadOptions.None);
            }
            catch (Exception ex) when (ex is System.Xml.XmlException or InvalidDataException)
            {
                return null;
            }
        }
    }
}
