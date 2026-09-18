using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le compteur d'une matière : combien de conversations, et combien n'ont
    /// jamais été ouvertes.
    /// </summary>
    public record CompteurExpressionsOrales(int Total, int Nouveautes);

    /// <summary>
    /// Une conversation retrouvée dans les messages bruts, que le professeur
    /// n'a jamais archivée lui-même.
    /// </summary>
    public record ExpressionOraleReconstituee(
        string Langue, IReadOnlyList<TourExpressionOrale> Echange, DateTime DateExercice);

    /// <summary>
    /// Les conversations d'expression orale archivées.
    ///
    /// BEAUCOUP PLUS COURT QUE <see cref="IComprehensionOraleRepository"/>, et
    /// c'est normal : pas d'audio à ranger, à servir ni à purger, puisqu'on
    /// n'en garde aucun. Il ne reste que ce qu'on écrit et ce qu'on relit.
    /// </summary>
    public interface IExpressionOraleRepository
    {
        /// <summary>
        /// Archive une conversation. Toujours une nouvelle ligne : elle se
        /// conclut dans la séance, il n'y a rien à rapprocher plus tard.
        ///
        /// Null si la conversation n'appartient pas à cet élève.
        /// </summary>
        Task<ExpressionOraleEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string titre,
            string langue,
            IReadOnlyList<TourExpressionOrale> echange,
            string? remarque,
            DateTime? dateExercice = null,
            CancellationToken ct = default);

        /// <summary>
        /// Les conversations d'un élève dans une matière, la plus récente
        /// d'abord — SANS leur échange.
        ///
        /// Une liste de trente conversations ferait sinon transiter trente
        /// échanges complets pour afficher trente titres. Le nombre de tours,
        /// lui, part avec : il se compte en base et il dit à l'élève si la
        /// discussion était longue avant qu'il l'ouvre.
        /// </summary>
        Task<IEnumerable<ExpressionOraleEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>Une conversation complète, avec son échange.</summary>
        Task<ExpressionOraleEleve?> GetDetailAsync(
            int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// L'élève vient d'ouvrir la conversation : la pastille s'éteint.
        ///
        /// Renvoie false si elle n'est pas la sienne — la garde d'accès est la
        /// même que pour la lecture.
        /// </summary>
        Task<bool> MarquerVueAsync(int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// LA PREMIÈRE PHRASE QUE LE PROFESSEUR A DITE dans la conversation en
        /// cours, retrouvée dans les messages bruts. Null si introuvable.
        ///
        /// SERT DE CONTRE-VÉRIFICATION QUAND IL ARCHIVE LUI-MÊME — Camara, le
        /// 18/09/2026 : « ça n'enregistre jamais la première phrase que le
        /// professeur me dit, ça commence avec moi alors que c'est la prof qui a
        /// commencé à parler ».
        ///
        /// MÊME REMÈDE QUE `MeilleureReponseAsync` POUR LA COMPRÉHENSION ORALE,
        /// et pour la même raison : un modèle ne recopie pas fidèlement, et une
        /// consigne ne suffit pas à l'y forcer. Ce qui manque ici est dans les
        /// messages — il n'y a rien à deviner, seulement à relire.
        ///
        /// Sans elle, l'échange relu commence par la RÉPONSE de l'enfant à une
        /// question qui a disparu : on ne comprend plus ce qui se passait.
        /// </summary>
        Task<string?> PremiereRepliqueAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Les conversations de cette séance que le professeur n'a PAS archivées
        /// lui-même, reconstituées depuis les messages bruts — voulu par Camara le
        /// 18/09/2026 : « je veux que tu mettes le filet directement ».
        ///
        /// MÊME RAISON QUE POUR LA COMPRÉHENSION ORALE, et la leçon est déjà
        /// payée là-bas : le bloc d'archivage dépend du bon vouloir du modèle, et
        /// il l'oublie. Trois exercices d'écoute ont disparu ainsi le 10/09/2026,
        /// alors que la séance avait bien son compte rendu.
        ///
        /// CE QUI REND LA RECONSTITUTION POSSIBLE ICI : la balise [CONVERSATION]
        /// borne le début, et les répliques du professeur sont déjà entre balises
        /// de langue — il n'y a rien à deviner, seulement à relire. Ce qui manque,
        /// c'est le TITRE : on le compose à partir de la date, et le professeur
        /// qui pose son bloc en écrit un vrai.
        ///
        /// Ne rend que ce qui MANQUE : une conversation déjà archivée par le
        /// professeur n'est jamais doublée.
        /// </summary>
        Task<IEnumerable<ExpressionOraleReconstituee>> GetNonArchiveesAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Combien de conversations par matière, et combien jamais ouvertes.
        /// Sert au compteur sur les cartes de matière sans charger les lignes.
        /// </summary>
        Task<IReadOnlyDictionary<int, CompteurExpressionsOrales>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default);
    }
}
