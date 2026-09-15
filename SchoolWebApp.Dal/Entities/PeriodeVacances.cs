namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une période de vacances scolaires, pour une zone et une année
    /// scolaire données.
    ///
    /// Rattachée à une ZONE (texte), jamais à une académie précise : neuf
    /// zones suffisent à couvrir les trente académies (voir
    /// <see cref="Academie.Zone"/>), pas la peine de dupliquer les dates
    /// académie par académie.
    ///
    /// `AnneeScolaire` en texte plutôt qu'une colonne « année en cours » :
    /// chaque rentrée ajoute un nouveau lot de lignes, les précédentes
    /// restent pour l'historique — jamais de mise à jour en place.
    /// </summary>
    public partial class PeriodeVacances
    {
        public int Id { get; set; }

        public string Zone { get; set; } = null!;

        /// <summary>« 2026-2027 ».</summary>
        public string AnneeScolaire { get; set; } = null!;

        /// <summary>Toussaint, Noël, Hiver, Printemps, Été...</summary>
        public string Libelle { get; set; } = null!;

        public DateTime DateDebut { get; set; }

        /// <summary>Inclusive : le dernier jour de vacances, pas le lendemain.</summary>
        public DateTime DateFin { get; set; }
    }
}
