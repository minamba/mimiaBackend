using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les carnets de test de l'administration. Chaque écriture rend le jeu entier, relu :
    /// c'est lui qui porte le statut recalculé, et l'écran n'a rien à déduire. Null quand le
    /// jeu, ou ce qu'on vise dans ce jeu, n'existe pas.
    /// </summary>
    public interface IJeuxDeTestsRepository
    {
        /// <summary>Le plus récent d'abord, sans le détail des tests.</summary>
        Task<IReadOnlyList<JeuDeTest>> ListerAsync(CancellationToken ct = default);

        Task<JeuDeTest?> GetAsync(int id, CancellationToken ct = default);

        Task<JeuDeTest> CreerAsync(string titre, string section, string niveauCode, string classe, string auteur, CancellationToken ct = default);

        Task<JeuDeTest?> ModifierAsync(int id, string titre, string section, string niveauCode, string classe, CancellationToken ct = default);

        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// LES CATÉGORIES DÉJÀ EMPLOYÉES dans les blocs des tests généraux (22/11/2026), sans doublon, par ordre
        /// alphabétique : la liste les repropose.
        /// </summary>
        Task<IReadOnlyList<string>> CategoriesAsync(CancellationToken ct = default);

        /// <summary>Crée le test et une ligne par compétence, toutes « à tester ».</summary>
        Task<JeuDeTest?> AjouterTestAsync(
            int jeuId, string titre, string matiereCode, string matiere, IReadOnlyList<string> competences,
            CancellationToken ct = default);

        Task<JeuDeTest?> ModifierTestAsync(
            int jeuId, int testId, string titre, string matiereCode, string matiere, CancellationToken ct = default);

        Task<JeuDeTest?> SupprimerTestAsync(int jeuId, int testId, CancellationToken ct = default);

        Task<JeuDeTest?> AjouterLigneAsync(int jeuId, int testId, string competence, CancellationToken ct = default);

        /// <summary>
        /// `maintenant` (heure de France) devient « Effectué le » seulement si le statut change ; le texte seul ne date pas.
        /// </summary>
        Task<JeuDeTest?> ModifierLigneAsync(
            int jeuId, int ligneId, string competence, string statut, string? commentaire,
            DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Le choix dans la liste : le statut, et « Effectué le » à `maintenant` (heure de France) s'il a changé.
        /// </summary>
        Task<JeuDeTest?> ChangerStatutAsync(int jeuId, int ligneId, string statut, DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Place la ligne au rang `position` (0 = tout en haut) dans SON test, et mémorise l'ordre de tout le test. Un rang
        /// hors de la liste vaut le bout le plus proche.
        /// </summary>
        Task<JeuDeTest?> DeplacerLigneAsync(int jeuId, int ligneId, int position, CancellationToken ct = default);

        Task<JeuDeTest?> SupprimerLigneAsync(int jeuId, int ligneId, CancellationToken ct = default);

        /// <summary>
        /// LA LIGNE D'UN SIGNALEMENT D'ADMINISTRATEUR (05/11/2026) : compétence, commentaire, statut « KO ». La
        /// première ligne vide du bloc sert d'abord (celle du test créé exprès, avec une ligne) ; sinon une ligne
        /// s'ajoute. Null si le bloc n'est pas dans ce jeu.
        /// </summary>
        Task<int?> PoserLigneDeSignalementAsync(
            int jeuId, int testId, string competence, string commentaire, CancellationToken ct = default);

        /// <summary>Ces lignes, avec leur jeu et leur bloc ; celles qui n'existent plus manquent.</summary>
        Task<IReadOnlyList<LigneDeSignalement>> LignesDeSignalementAsync(IEnumerable<int> ids, CancellationToken ct = default);

        /// <summary>
        /// Le signalement admin modifié (06/11/2026) : sa ligne reprend sa compétence et sa description (commentaire).
        /// Le statut ne bouge pas. Faux si la ligne n'existe plus.
        /// </summary>
        Task<bool> MettreAJourLigneDeSignalementAsync(int ligneId, string competence, string commentaire, CancellationToken ct = default);

        /// <summary>« Traité » côté signalement : la ligne repasse « à tester ». Faux si elle n'existe plus.</summary>
        Task<bool> RemettreATesterAsync(int ligneId, CancellationToken ct = default);
    }
}
