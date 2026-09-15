namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Les académies françaises, leur zone de vacances scolaires, et le
    /// calendrier de chaque zone.
    ///
    /// PROVENANCE — <see cref="Academies"/> vérifié le 9 septembre 2026.
    /// Le découpage académie → zone (A, B, C, ou calendrier propre) est fixé
    /// par le ministère et stable depuis plusieurs années — source :
    /// education.gouv.fr, page « Calendrier scolaire ». Vingt-cinq académies
    /// de métropole, plus la Corse et les cinq académies d'outre-mer
    /// (Guadeloupe, Guyane, Martinique, Mayotte, La Réunion), chacune de ces
    /// six-là suivant SON PROPRE calendrier, distinct des zones A/B/C.
    ///
    /// <see cref="Periodes"/> EST VOLONTAIREMENT VIDE — ET LE RESTE.
    /// ----------------------------------------------------------
    /// Contrairement au découpage par zone, qui ne change pas d'une année sur
    /// l'autre, les DATES de chaque période de vacances sont publiées chaque
    /// année par arrêté et ne se devinent pas. Une provenance qu'on ne peut
    /// pas vérifier vaut moins que pas de provenance du tout — même principe
    /// que sur <see cref="ReferentielMaths"/>.
    ///
    /// LA DIFFÉRENCE AVEC LES PROGRAMMES SCOLAIRES : ICI, LA SOURCE OFFICIELLE
    /// EST INTERROGEABLE DIRECTEMENT. Le ministère publie ces mêmes dates en
    /// open data (data.education.gouv.fr, jeu de données
    /// « fr-en-calendrier-scolaire »), et `CalendrierScolaireSyncWorker` les
    /// relit chaque jour pour tenir la table `PeriodeVacances` à jour tout
    /// seul — voir `SchoolWebApp/Workers/CalendrierScolaireSyncWorker.cs`.
    /// Recopier ces mêmes dates ici les ferait vivre en double, avec le
    /// risque qu'elles divergent le jour où l'une des deux copies est
    /// oubliée. `Periodes` reste donc vide : ce n'est pas une case à remplir
    /// avant la mise en production, c'est une table remplacée par un worker.
    ///
    /// Elle reste dans le code pour une seule raison : si la source officielle
    /// change un jour de format ou d'adresse et que la synchronisation
    /// automatique s'arrête, ce tableau est l'endroit prévu pour reprendre la
    /// main à la main, le temps de corriger le worker — voir la forme
    /// attendue dans le commentaire au-dessus de <see cref="Periodes"/>.
    /// </summary>
    public static class ReferentielVacancesScolaires
    {
        public static (string Code, string Libelle, string Zone)[] Academies =>
            new[]
            {
                // --- Métropole, zone A ---
                ("BESANCON",         "Besançon",         "A"),
                ("BORDEAUX",         "Bordeaux",         "A"),
                ("CLERMONT_FERRAND", "Clermont-Ferrand", "A"),
                ("DIJON",            "Dijon",            "A"),
                ("GRENOBLE",         "Grenoble",         "A"),
                ("LIMOGES",          "Limoges",          "A"),
                ("LYON",             "Lyon",             "A"),
                ("POITIERS",         "Poitiers",         "A"),

                // --- Métropole, zone B ---
                ("AIX_MARSEILLE", "Aix-Marseille", "B"),
                ("AMIENS",        "Amiens",        "B"),
                ("LILLE",         "Lille",         "B"),
                ("NANCY_METZ",    "Nancy-Metz",    "B"),
                ("NANTES",        "Nantes",        "B"),
                ("NICE",          "Nice",          "B"),
                ("NORMANDIE",     "Normandie",     "B"),
                ("ORLEANS_TOURS", "Orléans-Tours", "B"),
                ("REIMS",         "Reims",         "B"),
                ("RENNES",        "Rennes",        "B"),
                ("STRASBOURG",    "Strasbourg",    "B"),

                // --- Métropole, zone C ---
                ("CRETEIL",    "Créteil",    "C"),
                ("MONTPELLIER","Montpellier","C"),
                ("PARIS",      "Paris",      "C"),
                ("TOULOUSE",   "Toulouse",   "C"),
                ("VERSAILLES", "Versailles", "C"),

                // --- Calendrier propre ---
                ("CORSE",      "Corse",      "CORSE"),
                ("GUADELOUPE", "Guadeloupe", "GUADELOUPE"),
                ("GUYANE",     "Guyane",     "GUYANE"),
                ("MARTINIQUE", "Martinique", "MARTINIQUE"),
                ("MAYOTTE",    "Mayotte",    "MAYOTTE"),
                ("REUNION",    "La Réunion", "REUNION"),
            };

        /// <summary>
        /// Volontairement vide — voir le commentaire de la classe. Forme
        /// attendue une fois rempli :
        ///
        /// <code>
        /// ("A", "2026-2027", "Toussaint", new DateTime(2026, 10, 17), new DateTime(2026, 11, 2)),
        /// </code>
        /// </summary>
        public static (string Zone, string AnneeScolaire, string Libelle, DateTime Debut, DateTime Fin)[] Periodes =>
            System.Array.Empty<(string, string, string, DateTime, DateTime)>();
    }
}
