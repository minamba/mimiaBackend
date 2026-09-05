namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un visuel promotionnel affiché en haut de la page d'accueil.
    ///
    /// UNE BIBLIOTHÈQUE, PAS UN RÉGLAGE
    /// --------------------------------
    /// Le bandeau d'information est un réglage : une phrase, allumée ou
    /// éteinte. Celui-ci est un OBJET dont on garde une collection — la
    /// rentrée, Noël, le parrainage — parce qu'une promotion revient. La
    /// préparer une fois et la rallumer l'année suivante vaut mieux que de
    /// refaire le visuel à chaque saison.
    ///
    /// UN SEUL AFFICHÉ À LA FOIS, et c'est une règle du dépôt, pas de
    /// l'écran : allumer un bandeau éteint les autres dans la même écriture.
    /// Deux bandes d'images superposées repousseraient le contenu de la page
    /// hors de l'écran, et rien ne dirait laquelle est la bonne.
    ///
    /// LES OCTETS SONT EN BASE, comme pour les planches
    /// -----------------------------------------------
    /// Un fichier posé sur le disque du serveur ne survit pas au conteneur :
    /// chaque `docker restart` repartirait avec un bandeau cassé. La base est
    /// sauvegardée, elle ; c'est le seul endroit où une image ajoutée à 23 h
    /// sera encore là le lendemain.
    ///
    /// ELLE COÛTE UNE DISCIPLINE EN RETOUR : jamais charger `Image*` dans une
    /// liste. La bibliothèque de l'administration et la lecture publique ne
    /// ramènent que les métadonnées ; les octets ne sortent que par la route
    /// qui les sert, un fichier à la fois. Sans ça, afficher six lignes de
    /// tableau tirerait vingt mégaoctets de la base.
    /// </summary>
    public partial class BandeauPromo
    {
        public int Id { get; set; }

        /// <summary>
        /// Le nom interne — « Rentrée 2026 ». Il n'est JAMAIS affiché aux
        /// visiteurs : il sert à s'y retrouver dans la bibliothèque, six mois
        /// plus tard, entre quatre visuels qui se ressemblent.
        /// </summary>
        public string Titre { get; set; } = string.Empty;

        /// <summary>
        /// Ce que lit un lecteur d'écran, et ce qui s'affiche si l'image ne
        /// charge pas.
        ///
        /// OBLIGATOIRE, ET LA ROUTE LE REFUSE VIDE. Toute la promotion est
        /// DANS l'image : le prix, la date, la remise. Sans ce texte, un
        /// visiteur aveugle et un visiteur sur un réseau coupé n'ont
        /// rigoureusement rien — pas une offre dégradée, rien du tout.
        /// </summary>
        public string TexteAlternatif { get; set; } = string.Empty;

        /// <summary>
        /// Où le clic emmène. Nul quand le bandeau ne fait qu'annoncer.
        ///
        /// Une adresse interne (« /tarifs ») ou externe. Un bandeau sans lien
        /// n'est pas cliquable du tout — plutôt qu'un cadre qui réagit au
        /// survol et ne mène nulle part.
        /// </summary>
        public string? Lien { get; set; }

        /// <summary>
        /// Affiché sur le site ? Un seul bandeau peut l'être à la fois.
        /// </summary>
        public bool Actif { get; set; }

        /// <summary>
        /// Le visuel s'étend-il d'un bord à l'autre de l'écran ?
        ///
        /// Faux : une bulle arrondie, à la largeur de la colonne du site,
        /// qui parle la même langue que les cartes autour. Vrai : une bande
        /// pleine, qui frappe plus fort mais qui n'appartient plus tout à
        /// fait à la page.
        ///
        /// PAR BANDEAU ET NON PAR SITE, parce que le bon choix dépend du
        /// visuel : une affiche léchée gagne à couvrir la largeur, un
        /// message sobre gagne à rester dans la colonne. C'est en le voyant
        /// qu'on tranche, pas avant.
        /// </summary>
        public bool PleineLargeur { get; set; }

        // ------------------------------------------------------ les visuels

        /// <summary>
        /// Le média large, pour un écran d'ordinateur : une image ou une
        /// vidéo.
        ///
        /// LE NOM DIT « IMAGE » ET GARDE UN SENS. Les octets d'une vidéo
        /// sont des octets ; c'est `TypeMimeLarge` qui dit ce qu'ils sont,
        /// et lui seul. Renommer la colonne aurait coûté une migration de
        /// données pour un gain de vocabulaire — et laissé, le temps du
        /// déploiement, une base dont le code ne connaît pas le schéma.
        /// </summary>
        public byte[] ImageLarge { get; set; } = Array.Empty<byte>();

        public string TypeMimeLarge { get; set; } = string.Empty;

        public int TailleLarge { get; set; }

        /// <summary>
        /// La version téléphone, plus haute que large. Facultative.
        ///
        /// POURQUOI DEUX IMAGES ET NON UNE MISE À L'ÉCHELLE. Un visuel de
        /// 1920 × 320 ramené à la largeur d'un téléphone fait 65 pixels de
        /// haut : le prix devient illisible, et la promotion ne sert plus à
        /// rien là où se fait la moitié du trafic. Aucune règle de style ne
        /// rattrape ça — il faut un autre cadrage, donc un autre fichier.
        ///
        /// Absente, l'image large est utilisée partout. C'est un moindre mal
        /// assumé : mieux vaut une promotion petite qu'une page vide.
        /// </summary>
        public byte[]? ImageMobile { get; set; }

        public string? TypeMimeMobile { get; set; }

        public int TailleMobile { get; set; }

        // -------------------------------------------------------- les dates

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Sert d'étiquette de cache : la route des images en fait un `ETag`.
        /// Remplacer un visuel change cette date, donc l'étiquette, donc le
        /// navigateur redemande — sans quoi l'ancien visuel resterait affiché
        /// jusqu'à ce que le cache expire.
        /// </summary>
        public DateTime? DateModification { get; set; }
    }
}
