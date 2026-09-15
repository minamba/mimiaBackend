using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le compteur d'une matière : combien de dictées, et combien n'ont
    /// jamais été ouvertes.
    /// </summary>
    public record CompteurDictees(int Total, int Nouveautes);

    /// <summary>Les marqueurs de dictée posés par l'application, pas par le professeur.</summary>
    public static class MarqueursDictee
    {
        /// <summary>
        /// La dictée a été interrompue avant que la copie n'arrive : l'élève
        /// est parti pendant la lecture. Elle est annulée, rien n'est archivé.
        /// </summary>
        public const string Abandon = "[DICTEE_ABANDONNEE]";
    }

    /// <summary>Où en est la dernière dictée d'une conversation, vue du départ de l'élève.</summary>
    public enum InterruptionDictee
    {
        /// <summary>Rien d'interrompu : corrigée, copie reçue, ou pas de dictée du tout.</summary>
        Aucune,

        /// <summary>Interrompue avant la copie, et le marqueur n'est pas encore posé.</summary>
        ANoter,

        /// <summary>Le marqueur est posé, et l'élève n'a pas encore reparlé depuis.</summary>
        DejaNotee,
    }

    public interface IDicteeRepository
    {
        /// <summary>
        /// Archive une dictée, ou met à jour celle qui attendait sa
        /// correction — comme une fiche de révision, rapprochée cette fois
        /// sur le texte dicté plutôt que sur un titre de notion.
        ///
        /// Null si la conversation n'appartient pas à cet élève.
        /// </summary>
        Task<DicteeEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string? titre,
            string? etat,
            string texteDicte,
            string copie,
            string? remarque,
            CancellationToken ct = default);

        /// <summary>
        /// L'élève a quitté avant que le professeur n'ait pu archiver
        /// lui-même la dictée en cours. Reconstitue une ligne « en_attente »
        /// à partir du texte dicté et de la copie déjà reçue — rien si aucune
        /// copie n'est jamais arrivée, rien non plus si cette dictée est déjà
        /// archivée sous une forme ou une autre.
        /// </summary>
        Task<DicteeEleve?> ArchiverAbandonneeAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// La dernière dictée de cette conversation a-t-elle été interrompue
        /// AVANT QUE SA COPIE N'ARRIVE ? Voulu par Camara le 11/09/2026 : un
        /// élève qui part pendant la lecture voit la dictée annulée, rien
        /// n'est archivé, et le professeur le lui dit à son retour.
        /// </summary>
        Task<InterruptionDictee> EtatInterruptionAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Le professeur a-t-il de quoi archiver cette dictée ?
        ///
        /// Oui si la copie de la dernière dictée est vraiment arrivée — tapée
        /// et rendue au clavier, ou en photo. Oui encore s'il corrige une
        /// dictée restée en attente (même texte, à peu près). Non sinon : une
        /// dictée sans copie ne s'archive jamais, quoi que le modèle écrive.
        /// </summary>
        Task<bool> PeutArchiverAsync(
            int conversationId, int eleveId, string texteDicte, CancellationToken ct = default);

        /// <summary>
        /// L'élève ne veut plus de cette dictée : elle est SUPPRIMÉE, pour
        /// qu'il n'en reste aucune trace et que le professeur ne la ressorte
        /// jamais — voulu par Camara le 11/09/2026.
        ///
        /// Avec un numéro : cette dictée-là. Sans : la dernière dictée de la
        /// conversation. Seulement les dictées EN ATTENTE — une dictée corrigée
        /// est un travail fait, qu'on ne jette pas parce qu'on n'y revient pas.
        /// Renvoie le nombre de lignes supprimées.
        /// </summary>
        Task<int> SupprimerAsync(
            int conversationId, int eleveId, int? dicteeId, CancellationToken ct = default);

        /// <summary>
        /// Le professeur a corrigé la dernière dictée devant l'élève, mais
        /// sans l'archiver lui-même. On la passe « corrigée » — celle déjà
        /// archivée en attente, ou une nouvelle ligne avec le texte dicté et
        /// la vraie copie. Rien si la copie n'est jamais arrivée, si l'élève
        /// n'en voulait plus, ou si elle est déjà corrigée.
        /// </summary>
        Task<DicteeEleve?> ConstaterCorrectionAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>Les dictées d'un élève dans une matière, la plus récente d'abord.</summary>
        /// <summary>
        /// Le texte VRAIMENT dicté pendant la dictée en cours, relu dans les
        /// messages plutôt que recopié par le professeur.
        ///
        /// MÊME RAISON QUE POUR LA COMPRÉHENSION ORALE : en archivant, le
        /// modèle ne recopie que ce qu'il a sous les yeux. Relevé le
        /// 11/09/2026 — une dictée de plusieurs phrases archivée avec une
        /// seule, celle qu'il venait de relire. Le texte, lui, est dans les
        /// balises de dictée des messages, mot pour mot, puisque ce sont
        /// elles qui commandent la voix.
        ///
        /// Null si aucun passage n'a été dicté depuis la dernière correction.
        /// </summary>
        Task<string?> TexteDicteCompletAsync(
            int conversationId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// La copie que l'élève a vraiment rendue, relue dans ses messages.
        ///
        /// Symétrique du texte dicté. Le professeur ne recopie que ce qu'il a
        /// sous les yeux : le 11/09/2026, une copie de dix lignes archivée en
        /// une seule. Le texte retenu doit PARTAGER DES MOTS avec ce qui a été
        /// dicté — sans quoi une phrase de conversation passerait pour une
        /// copie, ce qui est déjà arrivé.
        ///
        /// Null si rien de crédible : on garde alors ce qu'a écrit le professeur.
        /// </summary>
        Task<string?> CopieCompleteAsync(
            int conversationId, int eleveId, string texteDicte, CancellationToken ct = default);

        Task<IEnumerable<DicteeEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>Une dictée complète, avec l'identité de l'élève, pour l'imprimer.</summary>
        Task<DicteeEleve?> GetDetailAsync(
            int dicteeId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// L'élève vient d'ouvrir la dictée : la pastille s'éteint.
        ///
        /// Renvoie false si la dictée n'est pas la sienne — la garde d'accès
        /// est la même que pour la lecture.
        /// </summary>
        Task<bool> MarquerVueAsync(int dicteeId, int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Combien de dictées par matière, et combien jamais ouvertes. Sert au
        /// compteur sur les cartes de matière sans charger toutes les dictées.
        /// </summary>
        Task<IReadOnlyDictionary<int, CompteurDictees>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default);
    }
}
