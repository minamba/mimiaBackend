using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le compteur d'une matière : combien de textes, et combien jamais
    /// ouverts.
    /// </summary>
    public record CompteurExpressionsEcrites(int Total, int Nouveautes);

    /// <summary>
    /// Un texte retrouvé dans les messages bruts, que le professeur n'a jamais
    /// archivé lui-même.
    ///
    /// SANS SA CORRECTION, ET C'EST ASSUMÉ. Voir
    /// <see cref="IExpressionEcriteRepository.GetNonArchiveesAsync"/> : les
    /// reprises du professeur sont de la prose française mêlée à sa
    /// pédagogie, on ne les redécoupe pas en genres sans inventer. Le texte de
    /// l'enfant, lui, est là mot pour mot — et c'est la moitié qui ne se
    /// retrouve nulle part ailleurs.
    /// </summary>
    public record ExpressionEcriteReconstituee(
        string Langue,
        string Consigne,
        string? Texte,
        DateTime DateExercice,
        byte[]? Photo = null,
        string? PhotoTypeMime = null);

    /// <summary>
    /// Un texte dont la photo attend encore d'être recopiée.
    ///
    /// Porté au professeur À L'ARRIVÉE DE L'ÉLÈVE, avec l'image : le texte
    /// peut dater de plusieurs séances et ne plus figurer dans la fenêtre
    /// d'historique qu'il a sous les yeux. Même mécanique que la dictée en
    /// attente de correction.
    /// </summary>
    public record TexteATranscrire(
        int Id, string Titre, string Consigne, DateTime DateCreation,
        byte[] Photo, string TypeMime);

    /// <summary>
    /// Les textes d'expression écrite archivés.
    ///
    /// LE TROISIÈME DE LA FAMILLE, après <see cref="IComprehensionOraleRepository"/>
    /// et <see cref="IExpressionOraleRepository"/>. Aucun audio ici non plus :
    /// il ne reste que ce qu'on écrit et ce qu'on relit.
    /// </summary>
    public interface IExpressionEcriteRepository
    {
        /// <summary>
        /// Archive un texte et sa correction. Toujours une nouvelle ligne : le
        /// texte se corrige dans la séance où il s'écrit, il n'y a rien à
        /// rapprocher plus tard.
        ///
        /// Null si la conversation n'appartient pas à cet élève.
        /// </summary>
        Task<ExpressionEcriteEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string titre,
            string langue,
            string consigne,
            string? texte,
            IReadOnlyList<RepriseEcrite> corrections,
            string? remarque,
            DateTime? dateExercice = null,
            byte[]? photo = null,
            string? photoTypeMime = null,
            CancellationToken ct = default);

        /// <summary>
        /// Le professeur recopie enfin une copie restée en photo, et la corrige.
        ///
        /// VOULU PAR CAMARA LE 18/09/2026 : « le prof pourra quand même refaire
        /// la transcription si elle a pas été faite. » Sans cela, une ligne
        /// créée au départ de l'élève resterait muette pour toujours — elle
        /// aurait gardé la photo, mais rien de relisible.
        ///
        /// LE TITRE ET LA CONSIGNE NE BOUGENT PAS : ils étaient justes le jour
        /// de l'exercice, et les rouvrir laisserait le modèle réécrire un
        /// énoncé qu'il ne fait que relire.
        ///
        /// Null si la ligne n'existe pas, n'est pas la sienne, ou a déjà son
        /// texte — on ne réécrit jamais une transcription par-dessus une autre.
        /// </summary>
        Task<ExpressionEcriteEleve?> CompleterAsync(
            int id,
            int eleveId,
            string texte,
            IReadOnlyList<RepriseEcrite> corrections,
            string? remarque,
            CancellationToken ct = default);

        /// <summary>
        /// Le texte le plus récent dont la photo attend d'être recopiée, dans
        /// cette matière. Null s'il n'y en a aucun.
        ///
        /// LE PLUS RÉCENT, PAS LE PLUS ANCIEN — même correction que sur la
        /// dictée en attente, relevée par Camara le 11/09/2026 : la dernière
        /// chose qu'il a faite est celle dont il se souvient.
        /// </summary>
        Task<TexteATranscrire?> ATranscrireAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>La photo d'un texte, pour la servir à l'élève. Null si purgée.</summary>
        Task<(byte[] Donnees, string TypeMime)?> GetPhotoAsync(
            int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Efface les octets des photos DÉJÀ TRANSCRITES et plus vieilles que
        /// <paramref name="anciennete"/>. Rend combien, et quel poids.
        ///
        /// « DÉJÀ TRANSCRITES » N'EST PAS UNE PRÉCAUTION DE PLUS, C'EST LA
        /// CONDITION — la règle est recopiée mot pour mot de la purge des pièces
        /// jointes, parce qu'elle y a été payée : effacer les octets d'un
        /// document dont le texte n'a pas encore été extrait le perdrait
        /// définitivement, et sans bruit.
        ///
        /// L'AUTRE MOITIÉ COMPTE AUTANT : passé ce délai, conserver l'écriture
        /// manuscrite d'un enfant et son nom en haut de la copie n'a plus aucune
        /// justification. La ligne, elle, reste — c'est le texte qu'on relit,
        /// pas la photo.
        /// </summary>
        Task<(int Photos, long Octets)> PurgerPhotosAsync(
            TimeSpan anciennete, int limite, CancellationToken ct = default);

        /// <summary>
        /// Les textes d'un élève dans une matière, le plus récent d'abord —
        /// SANS le texte ni la correction.
        ///
        /// La consigne, elle, part : elle tient en une ligne et c'est elle qui
        /// dit ce qu'on retrouvera en ouvrant.
        /// </summary>
        Task<IEnumerable<ExpressionEcriteEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>Un texte complet, avec sa correction.</summary>
        Task<ExpressionEcriteEleve?> GetDetailAsync(
            int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// L'élève vient d'ouvrir le texte : la pastille s'éteint. False s'il
        /// n'est pas le sien.
        /// </summary>
        Task<bool> MarquerVueAsync(int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// LE TEXTE QUE L'ÉLÈVE A RÉELLEMENT TAPÉ, retrouvé dans les messages
        /// bruts. Null s'il a écrit sur son cahier, ou si rien n'est trouvé.
        ///
        /// CONTRE-VÉRIFICATION DU CHAMP LE PLUS FRAGILE DE L'ARCHIVE, et c'est
        /// la même leçon que `PremiereRepliqueAsync` pour l'expression orale :
        /// une consigne de prompt ne vaut pas un fait. On demande au professeur
        /// de recopier le texte « mot pour mot, fautes comprises » — or un
        /// modèle de langue CORRIGE en recopiant, c'est ce qu'il sait faire de
        /// mieux. Un texte nettoyé de ses fautes ne montrerait aucun chemin
        /// parcouru, et la correction d'à côté parlerait de fautes devenues
        /// invisibles.
        ///
        /// Quand le texte est tapé au clavier, il n'y a rien à deviner : le
        /// message de l'enfant est en base. On le préfère alors à la
        /// transcription du modèle.
        /// </summary>
        Task<string?> TexteTapeAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Les textes de cette séance que le professeur n'a PAS archivés
        /// lui-même, reconstitués depuis les messages bruts.
        ///
        /// MÊME FILET QUE POUR LES DEUX AUTRES EXERCICES, et la leçon est déjà
        /// payée : le bloc d'archivage dépend du bon vouloir du modèle, et il
        /// l'oublie. Trois exercices d'écoute ont disparu ainsi le 10/09/2026.
        ///
        /// SEULEMENT LES TEXTES TAPÉS AU CLAVIER. Sur cahier, le texte n'existe
        /// que dans une photo : il n'y a rien à relire, et on n'invente pas.
        /// C'est une limite du filet, pas un oubli — le professeur qui pose son
        /// bloc reste la voie normale.
        ///
        /// SANS CORRECTION : ses reprises sont mêlées à sa pédagogie en français
        /// courant, les redécouper en genres reviendrait à deviner. On sauve la
        /// moitié qui ne se retrouve nulle part ailleurs.
        ///
        /// Ne rend que ce qui MANQUE : un texte déjà archivé n'est jamais doublé.
        /// </summary>
        Task<IEnumerable<ExpressionEcriteReconstituee>> GetNonArchiveesAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Combien de textes par matière, et combien jamais ouverts. Sert au
        /// compteur des cartes de matière sans charger les lignes.
        /// </summary>
        Task<IReadOnlyDictionary<int, CompteurExpressionsEcrites>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default);
    }
}
