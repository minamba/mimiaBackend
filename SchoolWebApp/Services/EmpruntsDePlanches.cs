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

                // STI2D — ingénierie (23/11/2026) : la chaîne d'énergie, les
                // circuits, le magnétisme, le mouvement et les ondes.
                // 25/11/2026 (Camara : « et aussi pour la matière de Lucas ») : les
                // 24 schémas des sciences de l'ingénieur, qui couvrent le programme
                // de STI2D (chaînes de puissance et d'information, SysML, liaisons,
                // électricité, asservissement, thermique, réseaux, IA), et le
                // réseau local de NSI. Aucune planche propre à l'ingénierie.
                ["INGENIERIE"] =
                [
                    "pc-chaine-energetique", "pc-circuit-serie", "pc-circuit-derivation",
                    "pc-circuit-rc", "pc-aimant-champ", "pc-mouvement-vecteurs",
                    "pc-spectre-electromagnetique", "pc-capteur",
                    "si-effort-flux-puissance", "si-chaine-puissance", "si-sysml-exigences-cas",
                    "si-graphe-liaisons", "si-trame-encapsulation", "si-asservissement-boucle",
                    "si-trois-realites-ecarts", "si-etats-transitions", "si-chaine-information",
                    "si-liaisons-normalisees", "si-bielle-manivelle", "si-diagramme-blocs-internes",
                    "si-lois-kirchhoff", "si-pont-en-h", "si-bilan-pertes-sankey", "si-champ-vitesses",
                    "si-isolement-statique", "si-cone-frottement", "si-charges-ouvrage",
                    "si-modulation-numerique", "si-architecture-objets-connectes", "si-reponses-modeles",
                    "si-intelligence-artificielle", "si-transferts-thermiques",
                    "nsi-reseau-local",
                ],

                // TECHNOLOGIE DU COLLÈGE (25/11/2026) : en attendant ses propres
                // schémas (préfixe « te- », catalogue du même jour), ce qui colle au
                // cycle 4 ailleurs. Les schémas de technologie de 6e (objet
                // technique, cycle de vie, levier, sources et conversion d'énergie,
                // réseau) : la 5e part de là. Électricité et énergie de la
                // physique-chimie. Les chaînes d'énergie et d'information et l'IA
                // des sciences de l'ingénieur ; réseau, octet et arborescence de NSI.
                ["TECHNOLOGIE"] =
                [
                    "sc-objet-technique", "sc-cycle-vie-objet", "sc-levier", "sc-sources-energie",
                    "sc-chaine-conversion-energie", "sc-symboles-electriques", "sc-reseau-informatique",
                    "pc-chaine-energetique", "pc-circuit-serie", "pc-circuit-derivation", "pc-capteur",
                    "pc-symboles-electriques", "pc-signal-information",
                    "si-chaine-information", "si-chaine-puissance", "si-intelligence-artificielle",
                    "nsi-reseau-local", "nsi-octet-bases", "nsi-arborescence-droits",
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
