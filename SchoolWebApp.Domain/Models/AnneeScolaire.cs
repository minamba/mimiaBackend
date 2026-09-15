namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// L'ANNÉE SCOLAIRE EN COURS — UNE SEULE FOIS, POUR TOUT LE PRODUIT.
    ///
    /// Voulu par Camara le 13/09/2026 : « ce titre doit se mettre à jour tous
    /// les ans, automatiquement ». Le libellé « Programmes officiels
    /// 2026-2027 » du site et de l'administration sort d'ici, jamais d'une
    /// chaîne écrite en dur qu'il faudrait penser à changer chaque rentrée.
    ///
    /// LA BASCULE SE FAIT AU 1er AOÛT — même règle que le worker du
    /// calendrier scolaire, qu'elle remplace ici. La rentrée tombe fin août
    /// ou début septembre selon l'année et l'académie ; dès août, les
    /// familles préparent l'année qui vient, et un site qui afficherait
    /// encore « 2025-2026 » le 25 août aurait l'air de dormir.
    /// </summary>
    public static class AnneeScolaire
    {
        private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

        /// <summary>L'année civile où commence l'année scolaire qui contient cette date (heure de Paris).</summary>
        public static int Debut(DateTime instantUtc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc), Paris);

            return local.Month >= 8 ? local.Year : local.Year - 1;
        }

        /// <summary>« 2026-2027 ».</summary>
        public static string Courante(DateTime instantUtc)
        {
            var debut = Debut(instantUtc);
            return $"{debut}-{debut + 1}";
        }

        /// <summary>« 2027-2028 » — celle d'après, pour ce qui se prépare à l'avance.</summary>
        public static string Suivante(DateTime instantUtc)
        {
            var debut = Debut(instantUtc) + 1;
            return $"{debut}-{debut + 1}";
        }

        /// <summary>« Programmes officiels 2026-2027 » — le badge du site et le titre de l'administration.</summary>
        public static string LibelleProgrammes(DateTime instantUtc) =>
            $"Programmes officiels {Courante(instantUtc)}";
    }
}
