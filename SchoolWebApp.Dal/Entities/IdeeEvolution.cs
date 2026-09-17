namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une idée d'évolution pour Mimia — voulue par Camara le 17/09/2026.
    ///
    /// UN CARNET, PAS UN OUTIL DE TICKETS. Ce qu'on note ici, ce sont des idées
    /// à l'état brut : une phrase, parfois une capture d'écran, parfois rien
    /// qu'un titre griffonné entre deux séances. Le suivi vient après, et il
    /// tient dans une seule colonne — le statut. Pas d'assignation, pas de
    /// sprint, pas d'estimation : Camara est seul à s'en servir, et un formulaire
    /// qui demande dix champs pour noter une idée fait qu'on ne la note pas.
    ///
    /// LA DESCRIPTION EST DU TEXTE, PAS DU HTML. Elle porte les mêmes balises
    /// que les courriels de diffusion — `**gras**`, `_italique_`, `[texte](lien)`
    /// et `[image:N]` — et c'est l'affichage qui les transforme. Stocker du HTML
    /// aurait signifié l'assainir à chaque lecture contre l'injection de script,
    /// pour un gain nul : ces quatre formes suffisent à décrire une idée.
    /// </summary>
    public partial class IdeeEvolution
    {
        public int Id { get; set; }

        /// <summary>Ce qu'on retient de l'idée en une ligne.</summary>
        public string Titre { get; set; } = null!;

        /// <summary>« Basse », « Moyenne » ou « Haute ». Voir `UrgenceIdee`.</summary>
        public string Urgence { get; set; } = null!;

        /// <summary>Le texte balisé. Peut être vide : un titre suffit à noter une idée.</summary>
        public string? Description { get; set; }

        /// <summary>Où en est l'idée. Voir `StatutIdee`. « Nouveau » à la création.</summary>
        public string Statut { get; set; } = null!;

        /// <summary>
        /// QUI L'A NOTÉE. Le prénom et le nom sont RECOPIÉS au moment de la
        /// création, pas rattachés au compte par une clé : une idée est datée et
        /// signée comme une note de carnet. Si le compte disparaît ou change de
        /// nom, la signature de l'idée ne bouge pas — c'est bien celui-là qui
        /// l'avait eue, ce jour-là.
        /// </summary>
        public string? AuteurPrenom { get; set; }

        public string? AuteurNom { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        public virtual ICollection<PieceIdee> Pieces { get; set; } = new List<PieceIdee>();
    }

    /// <summary>
    /// Une image jointe à une idée, appelée dans la description par `[image:N]`
    /// où N est son rang.
    ///
    /// LES OCTETS SONT DANS LA TABLE, comme pour les bandeaux promo et les
    /// modèles de courriel : le disque du conteneur ne survit pas à un
    /// redémarrage, la base est sauvegardée.
    /// </summary>
    public partial class PieceIdee
    {
        public int Id { get; set; }

        public int IdeeEvolutionId { get; set; }

        /// <summary>
        /// Son numéro dans la description, à partir de 1 et sans trou : c'est
        /// lui que `[image:N]` désigne. Une suppression renumérote les suivantes
        /// ET réécrit les marqueurs du texte, sinon `[image:3]` pointerait sur
        /// une image qui n'existe plus.
        /// </summary>
        public int Rang { get; set; }

        public string NomFichier { get; set; } = null!;

        public string TypeMime { get; set; } = null!;

        public int Taille { get; set; }

        public byte[] Donnees { get; set; } = null!;

        public DateTime DateCreation { get; set; }

        public virtual IdeeEvolution? IdeeEvolution { get; set; }
    }

    /// <summary>
    /// Les trois niveaux d'urgence. Écrits en toutes lettres en base plutôt
    /// qu'en nombres : une ligne lue directement en SQL doit se comprendre sans
    /// aller chercher la table de correspondance dans le code.
    /// </summary>
    public static class UrgenceIdee
    {
        public const string Basse = "Basse";
        public const string Moyenne = "Moyenne";
        public const string Haute = "Haute";

        public static readonly string[] Toutes = [Basse, Moyenne, Haute];

        public static bool EstConnue(string? valeur) =>
            valeur is not null && Toutes.Contains(valeur, StringComparer.Ordinal);
    }

    /// <summary>
    /// Le cycle de vie d'une idée, de la trouvaille à la mise en production.
    ///
    /// LES PARTICIPES S'ACCORDENT AU FÉMININ — « une idée » : nouvelle, validée,
    /// implémentée. Camara, le 17/09/2026. Changer ces chaînes CHANGE CE QUI EST
    /// EN BASE : voir la migration 20261011000000_StatutsIdeeFeminin.
    ///
    /// L'ORDRE EST CELUI DU PARCOURS, et il sert à trier la liste : une idée en
    /// test est plus avancée qu'une idée validée. « Abandonnée » est en dernier
    /// parce qu'elle sort du parcours — ce n'est pas une étape, c'est une fin.
    /// </summary>
    public static class StatutIdee
    {
        public const string Nouveau = "Nouvelle";
        public const string Analyse = "En cours d'analyse";
        public const string Valide = "Validée";
        public const string Implementation = "En cours d'implémentation";
        public const string Implemente = "Implémentée";
        public const string Test = "En test";
        public const string Production = "En production";
        public const string Abandonnee = "Abandonnée";

        public static readonly string[] Tous =
            [Nouveau, Analyse, Valide, Implementation, Implemente, Test, Production, Abandonnee];

        public static bool EstConnu(string? valeur) =>
            valeur is not null && Tous.Contains(valeur, StringComparer.Ordinal);
    }
}
