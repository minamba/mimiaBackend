using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les examens blancs de l'élève (23/11/2026). Toute lecture et toute écriture passe par l'élève : un examen
    /// blanc, une matière ou un document qui n'est pas à lui n'existe pas (null, faux, liste vide).
    /// </summary>
    public interface IExamenBlancRepository
    {
        /// <summary>Tous ses examens blancs, du plus proche au plus lointain, avec matières et documents.</summary>
        Task<IReadOnlyList<ExamenBlanc>> GetListeAsync(int eleveId, CancellationToken ct = default);

        Task<ExamenBlanc?> GetAsync(int eleveId, int examenBlancId, CancellationToken ct = default);

        /// <summary>
        /// Ajoute une matière pour cette date. UNE MÊME DATE, UN SEUL EXAMEN BLANC : s'il en a déjà un ce jour-là, la
        /// matière le rejoint (l'heure donnée complète une heure absente, sans en écraser une) ; sinon il est créé.
        /// Rend l'examen blanc et la ligne de la matière ; la matière déjà présente est rendue telle quelle.
        /// </summary>
        Task<(int ExamenBlancId, int MatiereLigneId)> AjouterMatiereAsync(
            int eleveId, string type, DateTime dateExamen, TimeSpan? heureExamen, int matiereId,
            DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Le document, si la matière est bien à l'élève et qu'il lui reste de la place
        /// (<see cref="ExamensBlancs.DocumentsMax"/>) ; null sinon.
        /// </summary>
        Task<DocumentExamenBlanc?> AjouterDocumentAsync(
            int eleveId, int examenBlancId, int matiereLigneId, DocumentExamenBlanc document, CancellationToken ct = default);

        Task<DocumentExamenBlanc?> GetDocumentAsync(int eleveId, int examenBlancId, int documentId, CancellationToken ct = default);

        /// <summary>Retire la ligne et rend le document retiré (son fichier est à effacer), ou null.</summary>
        Task<DocumentExamenBlanc?> SupprimerDocumentAsync(int eleveId, int examenBlancId, int documentId, CancellationToken ct = default);

        /// <summary>
        /// Retire une matière et ses documents ; l'examen blanc part avec sa dernière matière. Rend les chemins des
        /// fichiers à effacer, ou null si la matière n'est pas à l'élève.
        /// </summary>
        Task<RestesExamenBlanc?> RetirerMatiereAsync(int eleveId, int examenBlancId, int matiereLigneId, CancellationToken ct = default);

        /// <summary>
        /// Change la date et l'heure d'un examen blanc (25/11/2026). Rend null s'il n'est pas à l'élève, ou
        /// <see cref="ModificationExamenBlanc.DateDejaPrise"/> si un AUTRE examen blanc de l'élève tombe déjà ce
        /// jour-là (ils ne se fusionnent pas en silence) ; sinon les contrôles cachés de ses matières, à déplacer.
        /// </summary>
        Task<ModificationExamenBlanc?> ModifierDateAsync(
            int eleveId, int examenBlancId, DateTime dateExamen, TimeSpan? heureExamen, DateTime maintenant,
            CancellationToken ct = default);

        /// <summary>Supprime l'examen blanc entier ; rend les chemins des fichiers à effacer, ou null.</summary>
        Task<RestesExamenBlanc?> SupprimerAsync(int eleveId, int examenBlancId, CancellationToken ct = default);

        /// <summary>
        /// Les documents dont le fichier est encore sur le disque alors que l'examen blanc date de plus de deux mois
        /// (<see cref="ExamensBlancs.EffacementLe"/>), avec l'élève et l'examen blanc pour retrouver le dossier.
        /// </summary>
        Task<IReadOnlyList<(int EleveId, int ExamenBlancId, DocumentExamenBlanc Document)>> GetFichiersAPurgerAsync(
            DateTime maintenant, int maximum, CancellationToken ct = default);

        Task MarquerFichierEffaceAsync(int documentId, DateTime maintenant, CancellationToken ct = default);

        /// <summary>Parmi ces identifiants, ceux des examens blancs qui existent encore.</summary>
        Task<IReadOnlySet<int>> ExistantsAsync(IEnumerable<int> examenBlancIds, CancellationToken ct = default);

        // ------------------------------------------------------------------ étape 2 : la lecture et les dossiers

        /// <summary>Les photos et PDF pas encore lus, encore sur le disque, sous le plafond de lectures ratées.</summary>
        Task<IReadOnlyList<DocumentALire>> GetDocumentsALireAsync(int maximum, CancellationToken ct = default);

        Task EnregistrerLectureAsync(int documentId, string texte, CancellationToken ct = default);

        /// <summary>Une lecture ratée de plus ; à la troisième, le document est marqué illisible. Rend vrai alors.</summary>
        Task<bool> SignalerEchecLectureAsync(int documentId, string mentionAbandon, CancellationToken ct = default);

        /// <summary>
        /// Les matières dont les dossiers sont à refaire : documents tous lus, changés depuis le dernier rangement, et
        /// plus aucun envoi depuis <paramref name="calmeDepuis"/>. Une matière sans document perd ses dossiers ici
        /// même (rien à ranger, rien à rendre).
        /// </summary>
        Task<IReadOnlyList<MatiereANotionner>> GetMatieresANotionnerAsync(
            DateTime calmeDepuis, int maximum, CancellationToken ct = default);

        /// <summary>Remplace les dossiers de la matière et la marque rangée.</summary>
        Task PoserNotionsAsync(int ligneId, IReadOnlyList<NotionProposee> notions, DateTime maintenant, CancellationToken ct = default);

        // ------------------------------------------------------------------ étape 3 : la séance et le programme

        /// <summary>Rattache le contrôle caché à sa matière d'examen blanc.</summary>
        Task LierControleAsync(int ligneId, int controleId, CancellationToken ct = default);

        /// <summary>Léger : le nombre de dossiers et l'accord sur le programme, ou null (un contrôle ordinaire).</summary>
        Task<(int Notions, DateTime? ProgrammeValideLe)?> GetEtatParControleAsync(int controleId, CancellationToken ct = default);

        /// <summary>L'examen blanc dont ce contrôle est le contrôle caché, ou null (un contrôle ordinaire).</summary>
        Task<ExamenBlancPourSeance?> GetParControleAsync(int controleId, CancellationToken ct = default);

        /// <summary>L'élève a dit d'accord au programme : la date est posée (la première fois seulement).</summary>
        Task<bool> MarquerProgrammeValideAsync(int controleId, DateTime maintenant, CancellationToken ct = default);
    }
}
