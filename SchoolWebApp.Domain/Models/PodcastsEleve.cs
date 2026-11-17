namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES DEUX USAGES D'UN COURS IMPORTÉ — 15/11/2026. Un cours importé devient des FICHES de révision, ou un PODCAST.
    /// Les deux partagent tout le chemin (import, lecture des notions, tâche de fond, quota, crédits) mais jamais leurs
    /// données : chaque table du module porte sa nature, et un filtre de la base ne laisse voir que celle du moment
    /// (`SchoolWebAppDatabaseContext.NatureCours`). Une page des fiches ne voit pas un podcast, et l'inverse.
    /// </summary>
    /// <summary>
    /// UN PROFESSEUR QUE LE PARENT PEUT CHOISIR pour lire son podcast (15/11/2026) : son code d'avatar (sa voix), son
    /// prénom, et les matières qu'il enseigne aux élèves (pour que le parent le reconnaisse).
    /// </summary>
    public sealed record ProfesseurPodcast(string Avatar, string Prenom, IReadOnlyList<string> Matieres);

    public static class NaturesCours
    {
        public const string Fiches = "fiches";

        public const string Podcasts = "podcasts";

        public static bool Connue(string? nature) => nature is Fiches or Podcasts;
    }

    /// <summary>
    /// LES PODCASTS AUDIO DE L'ÉLÈVE — Camara, le 15/11/2026 : « un enfant choisit une matière, importe son cours ou
    /// choisit un sujet oralement, comme dans les fiches de révision, mais derrière on lui génère un audio avec la voix
    /// du professeur de la matière concernée ».
    ///
    /// LE MÊME CHEMIN QUE LES FICHES (`FichesRevisionEleve`) : brouillon, pages ou texte, lecture des notions, choix des
    /// notions, rédaction — sauf qu'ici chaque notion devient UN ÉPISODE : un texte écrit pour être écouté, lu par le
    /// professeur de la matière, rangé en audio. Un compteur À PART des fiches (décision de Camara, le même jour).
    /// </summary>
    public static class PodcastsEleve
    {
        /// <summary>Le module coûte à chaque épisode : éteint tant que Camara ne l'allume pas.</summary>
        public const string Reglage = "MODULE_PODCASTS";

        public const bool ActifParDefaut = false;

        /// <summary>
        /// LE BOUTON « TÉLÉCHARGER EN MP3 » — Camara, le 15/11/2026 : « un bouton pour télécharger l'audio en MP3, que je
        /// pourrais afficher ou pas à partir de l'admin ». Éteint par défaut. Le MP3 est fabriqué dans le navigateur à
        /// partir de l'épisode : le serveur n'a rien de plus à faire.
        /// </summary>
        public const string ReglageTelechargement = "PODCASTS_TELECHARGEMENT";

        public const bool TelechargementParDefaut = false;

        // ------------------------------------------------------------------ le résumé de livre

        /// <summary>
        /// LE RÉSUMÉ DE LIVRE — Camara, le 15/11/2026 : un troisième choix à la création d'un podcast, « l'enfant écrit
        /// ou dicte le nom du livre ». Rangé comme un texte libre (`text/plain`, donc tout le chemin du libre s'applique :
        /// un seul crédit, pas de pages) MARQUÉ « livre » : la lecture et la rédaction prennent alors leurs propres
        /// consignes (les parties du livre, et rien d'inventé sur un livre mal connu). Aucune colonne de plus.
        /// </summary>
        public const string TypeLivre = "text/plain; mimia=livre";

        public const string NomLivre = "Mon livre";

        public const int TitreLivreMax = 200;

        public const int AuteurLivreMax = 120;

        public static bool EstUnLivre(string? typeMime) =>
            typeMime is not null && typeMime.Contains("mimia=livre", StringComparison.OrdinalIgnoreCase);

        /// <summary>Ce que le modèle lit : le titre, et l'auteur si l'enfant le connaît.</summary>
        public static string TexteLivre(string titre, string? auteur) =>
            string.IsNullOrWhiteSpace(auteur)
                ? $"Le livre dont je veux le résumé : {titre.Trim()}"
                : $"Le livre dont je veux le résumé : {titre.Trim()}\nSon auteur : {auteur.Trim()}";

        /// <summary>Le seul « type » d'un cours de podcast : chaque notion devient un épisode.</summary>
        public const string Type = "podcast";

        /// <summary>DIX PAR MOIS ET PAR ENFANT — Camara, le 15/11/2026, compteur à part des fiches.</summary>
        public const int CoursParMois = 10;

        /// <summary>QUARANTE PAGES PAR COURS — Camara, le 15/11/2026 (vingt pour les fiches).</summary>
        public const int PagesMax = 40;

        public const int PiecesMax = 40;

        /// <summary>Au-delà, un cours de quarante pages ferait trente épisodes : on garde les grandes notions.</summary>
        public const int NotionsMax = 12;

        /// <summary>Les plafonds d'un import de podcast. Une famille administratrice garde ceux des fiches (100 pages).</summary>
        public static readonly FichesRevisionEleve.LimitesImport LimitesStandard = new(
            false, PagesMax, PiecesMax, FichesRevisionEleve.TailleFichierMax, null, NotionsMax);

        public static FichesRevisionEleve.LimitesImport Limites(bool sansLimite) =>
            sansLimite ? FichesRevisionEleve.LimitesAdministrateur : LimitesStandard;

        /// <summary>
        /// UN COURS DE PLUS DE DIX PAGES COMPTE POUR DEUX — Camara, le 15/11/2026, « le même système que les fiches ».
        /// La même règle (`FichesRevisionEleve.Poids`), quarante pages restant permises.
        /// </summary>
        public static int Poids(int pages) => FichesRevisionEleve.Poids(pages);

        /// <summary>
        /// LE COÛT DES PODCASTS À PART — Camara, le 15/11/2026 : « je veux une ligne podcasts avec le montant, même si
        /// c'est lié à Gemini ; c'est différent, ça ne vient pas de la discussion avec les élèves ». Toutes les origines
        /// des podcasts au journal des appels commencent par ce préfixe : la lecture du cours, la rédaction des
        /// épisodes, et la voix (`OrigineVoix`, qui n'est PLUS notée `voix`).
        /// </summary>
        public const string PrefixeOrigine = "podcasts-";

        public const string OrigineVoix = "podcasts-voix";

        public const string MessageEssaiUtilise =
            "Il faut être abonné pour continuer à créer des podcasts.";

        /// <summary>
        /// LES FORMULES SANS PODCASTS — Camara, le 15/11/2026 : « cette fonctionnalité n'est pas disponible pour les
        /// personnes ayant l'abonnement Starter », et « le bouton est visible mais l'utilisateur est bloqué avec un
        /// message ». La liste vit ICI : le serveur refuse (`ImportCoursService.PorteDuForfaitAsync`, achat de packs),
        /// la route `acces` et l'espace parent le disent à l'écran, qui garde la carte et bloque au clic.
        /// L'ESSAI AUSSI (Camara, le 15/11/2026 : « il faut minimum l'abonnement Solo ») : il n'a pas de code de
        /// formule à mettre ici, c'est `AccesForfaitService.PodcastsOuvertsAsync` qui le lit.
        /// </summary>
        public static readonly IReadOnlySet<string> FormulesSansPodcasts =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "STARTER" };

        /// <summary>La formule de ce code donne-t-elle les podcasts ? Sans formule connue : oui, d'autres gardes jugent.</summary>
        public static bool InclusDans(string? codeOffre) =>
            codeOffre is null || !FormulesSansPodcasts.Contains(codeOffre);

        public const string MessageNonInclusEnfant =
            "Il faut au minimum l'abonnement Solo pour accéder aux podcasts. Demande à tes parents.";

        public const string MessageNonInclusParent =
            "Il faut au minimum l'abonnement Solo pour accéder aux podcasts. Choisissez la formule Solo ou une formule supérieure.";

        // ------------------------------------------------------------------ l'épisode

        /// <summary>Un épisode vise cette durée : trois minutes au primaire, cinq au collège, six au lycée.</summary>
        public static int MinutesVisees(int rang) => rang switch
        {
            <= 0 => 5,
            <= 5 => 3,
            <= 9 => 5,
            _ => 6,
        };

        /// <summary>Environ 150 mots à la minute à l'oral, un peu moins pour un enfant de primaire.</summary>
        public static int MotsVises(int rang) => MinutesVisees(rang) * (rang is > 0 and <= 5 ? 130 : 150);

        /// <summary>Le format de l'audio rangé : Ogg Opus, une minute ≈ 240 Ko.</summary>
        public const string TypeAudio = "audio/ogg";

        public const string ExtensionAudio = ".opus";
    }
}
