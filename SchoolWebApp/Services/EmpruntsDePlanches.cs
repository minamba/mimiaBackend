namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LES PLANCHES QU'UNE MATIÈRE EMPRUNTE À UNE AUTRE — décidé avec Camara le
    /// 14/09/2026, pour les spécialités des séries technologiques.
    ///
    /// POURQUOI UN EMPRUNT ET NON UNE COPIE
    /// ------------------------------------
    /// Une planche est rangée sous UNE matière, et la bibliothèque du prompt ne
    /// sert que celles de la matière du professeur. La règle suivie jusqu'ici
    /// était de déclarer une clé par matière : « svt-effet-de-serre n'est jamais
    /// servi à un professeur de physique-chimie ». Pour les séries
    /// technologiques, ç'aurait été importer une seconde fois l'appareil
    /// respiratoire pour la biologie de ST2S, le titrage pour les sciences
    /// physiques de STL — les mêmes images, à tenir à jour deux fois.
    ///
    /// L'emprunt est une liste FERMÉE, clé par clé : la biologie humaine prend
    /// le cœur et l'immunité à la SVT, pas la subduction ni l'érosion. Une
    /// planche empruntée n'est annoncée que si elle est réellement en base,
    /// exactement comme les autres.
    ///
    /// Les figures SVT DESSINÉES, qui s'affichent sans import, sont annoncées
    /// dans la couche de consignes des deux matières de biologie : voir
    /// `PromptsSeriesTechnologiques`.
    /// </summary>
    public static class EmpruntsDePlanches
    {
        public static readonly IReadOnlyDictionary<string, string[]> ParMatiere =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // ST2S — biologie et physiopathologie humaines : le normal avant
                // le pathologique, appareil par appareil, puis l'immunité, la
                // reproduction et les gènes.
                ["BIOLOGIE_HUMAINE"] =
                [
                    "svt-respiratoire", "svt-alveole", "svt-digestif", "svt-circulation",
                    "svt-arc-reflexe", "svt-synapse", "svt-reproducteur-feminin",
                    "svt-reproducteur-masculin", "svt-fecondation", "svt-communication-hormonale",
                    "svt-phagocytose", "svt-immunite-adaptative", "svt-vaccination", "svt-vih",
                    "svt-glycemie", "svt-cellule-animale", "svt-chromosome-adn-gene",
                    "svt-transcription-traduction", "svt-cancer",
                ],

                // STL — biochimie, biologie et biotechnologies : la cellule, l'ADN,
                // la division, le métabolisme, l'immunité, les enzymes.
                ["BIOTECHNOLOGIES"] =
                [
                    "svt-cellule-animale", "svt-cellule-vegetale", "svt-adn-double-helice",
                    "svt-chromosome-adn-gene", "svt-replication", "svt-transcription-traduction",
                    "svt-mitose", "svt-meiose", "svt-brassage-meiose", "svt-crossing-over",
                    "svt-metabolisme", "svt-enzyme", "svt-digestif", "svt-reproducteur-feminin",
                    "svt-reproducteur-masculin", "svt-fecondation", "svt-glycemie",
                    "svt-phagocytose", "svt-immunite-adaptative", "svt-vih",
                ],

                // STL — sciences physiques et chimiques en laboratoire : montages,
                // dosages, synthèse, ondes, optique, mesure.
                ["SPCL"] =
                [
                    "pc-echelle-ph", "pc-titrage", "pc-chauffage-reflux", "pc-synthese-organique",
                    "pc-distillation", "pc-filtration", "pc-chromatographie", "pc-tests-ions",
                    "pc-schema-lewis", "pc-formules-organiques", "pc-tableau-periodique",
                    "pc-spectre-electromagnetique", "pc-diffraction-interferences", "pc-effet-doppler",
                    "pc-lunette-astronomique", "pc-lentille-convergente", "pc-refraction",
                    "pc-capteur", "pc-pression-fluide",
                ],

            };

        /// <summary>Cette planche est-elle servie au professeur de cette matière ?</summary>
        public static bool EstServie(string? cle, string? matierePlanche, string matiereProfesseur) =>
            string.Equals(matierePlanche, matiereProfesseur, StringComparison.OrdinalIgnoreCase)
            || (cle is not null
                && ParMatiere.TryGetValue(matiereProfesseur, out var empruntees)
                && empruntees.Contains(cle, StringComparer.OrdinalIgnoreCase));
    }
}
