using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le compteur d'une matière : combien de compréhensions orales, et
    /// combien n'ont jamais été ouvertes.
    /// </summary>
    public record CompteurComprehensionsOrales(int Total, int Nouveautes);

    /// <summary>Les marqueurs posés par l'application autour d'un exercice d'écoute.</summary>
    public static class MarqueursComprehensionOrale
    {
        /// <summary>
        /// L'élève ne veut plus de cet exercice : il est SUPPRIMÉ de partout —
        /// voulu par Camara le 12/09/2026, comme pour la dictée. Ni fiche, ni
        /// rattrapage à la fin de la séance : une donnée refusée ne sert qu'à
        /// encombrer.
        /// </summary>
        public const string Suppression = "[COMPREHENSION_SUPPRIMEE]";

        public const string FinSuppression = "[/COMPREHENSION_SUPPRIMEE]";
    }

    /// <summary>
    /// Un exercice d'écoute retrouvé dans les messages bruts, que le
    /// professeur n'a jamais archivé lui-même.
    /// </summary>
    public record ComprehensionOraleReconstituee(
        string Langue, string Passage, string ReponseEleve, string Comprehension,
        DateTime DateExercice);

    public interface IComprehensionOraleRepository
    {
        /// <summary>
        /// Archive un exercice de compréhension orale. Toujours une nouvelle
        /// ligne — contrairement à la dictée, il n'y a rien à rapprocher :
        /// l'exercice se conclut en un seul échange.
        ///
        /// Null si la conversation n'appartient pas à cet élève.
        /// </summary>
        Task<ComprehensionOraleEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string? titre,
            string langue,
            string passage,
            string reponseEleve,
            string comprehension,
            string? remarque,
            byte[]? audioDonnees,
            DateTime? dateExercice = null,
            CancellationToken ct = default);

        /// <summary>
        /// L'élève ne veut plus de cet exercice d'écoute : il est supprimé,
        /// pour qu'il n'en reste aucune trace — voulu par Camara le
        /// 12/09/2026, même règle que la dictée abandonnée.
        ///
        /// Avec un numéro : cette fiche-là. Sans : le dernier exercice écouté
        /// dans la conversation, s'il a déjà été archivé. Dans les deux cas,
        /// le marqueur reste dans le message : c'est lui qui empêche le filet
        /// de fin de séance de le reconstituer.
        ///
        /// Renvoie le nombre de fiches supprimées.
        /// </summary>
        Task<int> SupprimerAsync(
            int conversationId, int eleveId, int? comprehensionOraleId,
            CancellationToken ct = default);

        /// <summary>
        /// Les exercices d'écoute de cette séance que le professeur n'a PAS
        /// archivés lui-même, reconstitués depuis les messages bruts.
        ///
        /// MÊME RAISON QUE POUR LA DICTÉE ABANDONNÉE : le bloc d'archivage
        /// dépend du bon vouloir du modèle, et il l'oublie. Trois exercices
        /// d'affilée ont ainsi disparu le 10/09/2026 alors que la séance,
        /// elle, avait bien son compte rendu. Le passage lu, lui, est dans
        /// le message — il n'y a rien à deviner, seulement à relire.
        ///
        /// Ne rend que ce qui MANQUE : un exercice déjà archivé par le
        /// professeur n'est jamais doublé.
        /// </summary>
        Task<IEnumerable<ComprehensionOraleReconstituee>> GetNonArchiveesAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Ce que l'élève a VRAIMENT dit sur ce passage, retrouvé dans les
        /// messages — son résumé le plus complet, hors demandes de réécoute.
        ///
        /// SERT DE CONTRE-VÉRIFICATION QUAND LE PROFESSEUR ARCHIVE LUI-MÊME.
        /// Il ne cite pas toujours l'élève en entier : le 10/09/2026, une
        /// explication longue en français a été réduite à sa dernière phrase,
        /// alors qu'en anglais — où c'est le rattrapage qui écrit — la fiche
        /// était complète. Une même règle pour toutes les langues, donc, et
        /// elle ne dépend plus de ce que le modèle a bien voulu recopier.
        ///
        /// Null si le passage est introuvable ou sans réponse exploitable.
        /// </summary>
        Task<string?> MeilleureReponseAsync(
            int conversationId, int eleveId, string passage, CancellationToken ct = default);

        /// <summary>Les compréhensions orales d'un élève dans une matière, la plus récente d'abord.</summary>
        Task<IEnumerable<ComprehensionOraleEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>Une compréhension orale complète, avec l'identité de l'élève.</summary>
        Task<ComprehensionOraleEleve?> GetDetailAsync(
            int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Les octets audio d'une compréhension orale, ou null si absents
        /// (synthèse échouée à l'archivage) ou si elle n'appartient pas à
        /// cet élève.
        /// </summary>
        Task<byte[]?> GetAudioAsync(int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// L'élève vient d'ouvrir la fiche : la pastille s'éteint.
        ///
        /// Renvoie false si la fiche n'est pas la sienne — la garde d'accès
        /// est la même que pour la lecture.
        /// </summary>
        Task<bool> MarquerVueAsync(int id, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Efface les audio passé une certaine ancienneté. Rend combien ont
        /// été effacés et combien d'octets ont été rendus.
        ///
        /// LA LIGNE SURVIT, SEUL LE SON PART — même mécanique que la purge des
        /// pièces jointes. Le passage, la réponse de l'élève et le retour du
        /// professeur restent consultables ; c'est le son, et lui seul, qui
        /// occupe la place. Il est d'ailleurs REGÉNÉRABLE : `Passage` est en
        /// base, la synthèse peut le redire un jour si on décide de l'offrir.
        /// </summary>
        Task<(int Effaces, long Octets)> PurgerAudiosAsync(
            TimeSpan anciennete, int limite, CancellationToken ct = default);

        /// <summary>
        /// Combien de compréhensions orales par matière, et combien jamais
        /// ouvertes. Sert au compteur sur les cartes de matière sans charger
        /// toutes les lignes.
        /// </summary>
        Task<IReadOnlyDictionary<int, CompteurComprehensionsOrales>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default);
    }
}
