using System.Text.Json;

namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LE PROJET D'ORIENTATION DE L'ÉLÈVE, tel que la conseillère l'a noté —
    /// Camara, le 11/10/2026 : « si l'enfant a discuté avec la conseillère, que
    /// des instructions soient envoyées aux professeurs sur ce qu'il doit
    /// renforcer pour atteindre ses objectifs de parcours ».
    ///
    /// UN SEUL PROJET, LE PLUS RÉCENT : un projet de 3e évolue, le nouveau
    /// remplace l'ancien. Daté, pour qu'un professeur sache s'il est frais.
    ///
    /// NOTÉ AVEC L'ACCORD DE L'ÉLÈVE (décision de Camara) : la conseillère lui
    /// demande s'il est d'accord pour qu'elle en parle à ses professeurs, et
    /// n'écrit rien sinon. Le parent le voit sur la fiche de son enfant.
    ///
    /// Chaque professeur ne reçoit que la ligne de SA matière.
    /// </summary>
    public class ProjetOrientation
    {
        /// <summary>Le but : « ingénieur en informatique », « infirmière »…</summary>
        public string Objectif { get; set; } = "";

        /// <summary>Ce qui compte pour y arriver, matière par matière.</summary>
        public List<LigneProjetOrientation> Matieres { get; set; } = [];

        public DateTime Le { get; set; }

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        /// <summary>Les lignes en JSON, pour la colonne de l'élève.</summary>
        public static string Ecrire(IEnumerable<LigneProjetOrientation> lignes) =>
            JsonSerializer.Serialize(lignes, Json);

        /// <summary>Le projet relu depuis les trois colonnes ; null s'il n'y en a pas, ou s'il est illisible.</summary>
        public static ProjetOrientation? Lire(string? objectif, string? matieres, DateTime? le)
        {
            if (string.IsNullOrWhiteSpace(objectif) || le is null) return null;

            List<LigneProjetOrientation> lignes;
            try
            {
                lignes = string.IsNullOrWhiteSpace(matieres)
                    ? []
                    : JsonSerializer.Deserialize<List<LigneProjetOrientation>>(matieres, Json) ?? [];
            }
            catch (JsonException)
            {
                lignes = [];
            }

            return new ProjetOrientation { Objectif = objectif, Matieres = lignes, Le = le.Value };
        }

        /// <summary>La ligne d'une matière, ou null : un professeur ne lit que la sienne.</summary>
        public LigneProjetOrientation? Pour(int matiereId) =>
            Matieres.FirstOrDefault(l => l.MatiereId == matiereId);
    }

    /// <summary>Une matière du projet, et ce qui y compte pour atteindre le but.</summary>
    public class LigneProjetOrientation
    {
        public int MatiereId { get; set; }

        /// <summary>Le nom de la matière au moment de la note, pour l'affichage.</summary>
        public string MatiereLibelle { get; set; } = "";

        /// <summary>« calcul littéral ; fonctions (fragiles) », « spécialité utile en 1re »…</summary>
        public string Texte { get; set; } = "";
    }
}
