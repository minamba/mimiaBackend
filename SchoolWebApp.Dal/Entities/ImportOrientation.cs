namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// LE JOURNAL DES IMPORTS DE L'OFFRE DES LYCÉES — une ligne par passage de la
    /// tâche hebdomadaire, réussi ou non. C'est ce que montre Admin › Modes :
    /// date du dernier passage, nombre de lycées, erreur éventuelle.
    ///
    /// <see cref="Sources"/> garde la date de mise à jour annoncée par chaque
    /// source : au passage suivant, une source qui n'a pas bougé n'est pas
    /// retéléchargée.
    /// </summary>
    public class ImportOrientation
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        /// <summary>Vrai si la base a été réécrite, ou s'il n'y avait rien de neuf.</summary>
        public bool Reussi { get; set; }

        /// <summary>Ce qui s'est passé, en une phrase lisible par l'administrateur.</summary>
        public string? Message { get; set; }

        /// <summary>Le nombre de lycées en base après le passage.</summary>
        public int Lycees { get; set; }

        /// <summary>Les dates de mise à jour des sources, en JSON (jeu → date).</summary>
        public string? Sources { get; set; }

        /// <summary>La durée du passage, en secondes.</summary>
        public int DureeSecondes { get; set; }
    }
}
