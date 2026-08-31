namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une planche importée qui remplace le dessin d'un professeur.
    ///
    /// POURQUOI EN BASE ET NON DANS LE PAQUET DU FRONT
    /// ----------------------------------------------
    /// Les premières planches vivaient dans `public/schemas/`. En ajouter une
    /// exigeait donc de reconstruire et redéployer l'application entière —
    /// tenable pour sept fichiers, absurde pour les cinq autres matières, où
    /// il en faudra une centaine. Ici, une planche s'ajoute depuis
    /// l'administration ou par un appel d'API, sans toucher au code.
    ///
    /// LA CLÉ EST CELLE DU CATALOGUE, ET LE CATALOGUE RESTE CÔTÉ FRONT
    /// -------------------------------------------------------------
    /// Le serveur ne sait pas quelles figures existent : il ne fait que garder
    /// celles qu'on lui donne, indexées par la clé que le professeur écrit
    /// (`svt-respiratoire`). C'est le front qui connaît la liste, et c'est
    /// suffisant — l'administration et le tableau sont dans la même
    /// application. Dupliquer le catalogue en C# n'apporterait qu'une source
    /// de désynchronisation de plus.
    /// </summary>
    public partial class PlancheSchema
    {
        public int Id { get; set; }

        /// <summary>La clé du catalogue : svt-respiratoire, pc-circuit-serie…</summary>
        public string Cle { get; set; } = string.Empty;

        /// <summary>
        /// Le code de la matière, tel qu'en base : SVT, PHYSIQUE_CHIMIE…
        ///
        /// Redondant avec le préfixe de la clé, et c'est voulu : l'écran
        /// d'administration groupe par matière, et déduire la matière d'un
        /// préfixe de chaîne casserait le jour où une clé changera de forme.
        /// </summary>
        public string MatiereCode { get; set; } = string.Empty;

        public string? NomFichier { get; set; }

        /// <summary>image/svg+xml, image/png, image/jpeg…</summary>
        public string? TypeMime { get; set; }

        public int Taille { get; set; }

        public byte[] Donnees { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Les trois mentions qu'imposent CC BY et CC BY-SA. Elles sont
        /// affichées sous la planche, dans le tableau — c'est le seul endroit
        /// où elles figurent, donc le seul endroit où la condition de licence
        /// est remplie ou non.
        /// </summary>
        public string? Auteur { get; set; }

        public string? Source { get; set; }

        public string? Licence { get; set; }

        /// <summary>
        /// La planche est de nous : produite ou générée en interne, elle n'a
        /// aucun auteur tiers à créditer.
        ///
        /// POURQUOI UN DRAPEAU PLUTÔT QUE TROIS CHAMPS VIDES
        /// ------------------------------------------------
        /// Un crédit absent est ambigu, et l'ambiguïté a des conséquences ici :
        /// <c>DescriptionPlanchesWorker</c> ramasse en fond les planches sans
        /// crédit et va leur en chercher un. Une illustration maison laissée
        /// vide se verrait donc attribuer un auteur Wikimedia — une fausse
        /// attribution, affichée sous les yeux d'un enfant, et écrite par nous.
        ///
        /// Ce drapeau dit la différence entre « personne n'a encore rempli » et
        /// « il n'y a rien à remplir ». Le worker saute les secondes.
        /// </summary>
        public bool Maison { get; set; }

        /// <summary>
        /// Les étiquettes de la planche AVEC LEUR POSITION, en JSON :
        /// <c>[{"mot":"Prostate","x":0.52,"y":0.36}, …]</c>, en fractions.
        ///
        /// POURQUOI ON NE DEMANDE PLUS AU PROFESSEUR DE LIRE
        /// ------------------------------------------------
        /// Quand l'élève clique sur la figure, il fallait que le professeur
        /// regarde l'image et dise ce qu'il y a sous la marque. Six versions de
        /// cette marque ont été essayées — cercle, cercle épais, cercle cerclé
        /// de noir, agrandissement de la zone, épingle — et à chaque fois il a
        /// nommé un organe voisin, parfois lointain : l'anus annoncé « prostate »
        /// alors que l'épingle était au bon endroit.
        ///
        /// Ce n'est pas un défaut d'image. Un modèle de vision lit très bien du
        /// texte et situe très mal un point dans l'espace. On ne corrige pas ça
        /// avec une plus belle figure.
        ///
        /// Ces coordonnées, extraites UNE FOIS à l'import, déplacent le calcul
        /// là où il est exact : le serveur compare le clic aux positions et
        /// donne le mot au professeur. Même réponse à chaque fois, sur toutes
        /// les planches, dans toutes les matières.
        ///
        /// Nulle tant que l'extraction n'a pas eu lieu — le professeur retombe
        /// alors sur l'ancien comportement, dégradé mais pas cassé.
        /// </summary>
        public string? Reperes { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Ce que la planche montre RÉELLEMENT : la liste de ses légendes.
        ///
        /// POURQUOI C'EST INDISPENSABLE
        /// ---------------------------
        /// Le professeur n'a jamais vu la planche. Il écrit une clé, et le
        /// tableau affiche ce qui a été importé. Sans cette liste, il interroge
        /// à l'aveugle : il demande « après les bronches, l'air va où ? » sur
        /// une figure qui s'arrête aux bronches, et il ignore l'encart
        /// alvéolaire qui aurait fait tout l'intérêt de la séance.
        ///
        /// Extraite automatiquement à l'import par un modèle de vision, et
        /// injectée dans son catalogue.
        /// </summary>
        public string? Contenu { get; set; }

        public DateTime? DateModification { get; set; }
    }
}
