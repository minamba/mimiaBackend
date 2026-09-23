using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IMaitriseRepository
    {
        /// <summary>
        /// Compétences les moins maîtrisées, tous niveaux confondus.
        /// Le tri par score croissant fait naturellement remonter les lacunes de
        /// primaire avant les difficultés du niveau courant — c'est exactement
        /// ce qu'un professeur particulier regarde en premier.
        /// </summary>
        Task<IEnumerable<MaitriseCompetence>> GetLacunesAsync(int eleveId, int? matiereId, double seuil, int limite);

        /// <summary>Compétences acquises, pour ne pas réexpliquer ce qui est su.</summary>
        Task<IEnumerable<MaitriseCompetence>> GetAcquisesAsync(int eleveId, int? matiereId, double seuil, int limite);

        /// <summary>
        /// Les compétences non tenues (score sous <paramref name="seuil"/>)
        /// d'une année STRICTEMENT antérieure à <paramref name="rangMax"/>,
        /// dans une matière, la plus fragile en tête. Ce sont celles qu'un jeu
        /// d'une classe d'avant peut consolider.
        /// </summary>
        Task<IEnumerable<MaitriseCompetence>> GetFragilesEnAmontAsync(
            int eleveId, int matiereId, int rangMax, double seuil, int limite);

        /// <summary>
        /// Notions dont l'échéance de révision espacée est passée, la plus
        /// fragile en tête. Sans cette lecture, `ProchaineRevision` était
        /// calculée à chaque observation et jamais consultée.
        /// </summary>
        Task<IEnumerable<MaitriseCompetence>> GetARevoirAsync(int eleveId, int? matiereId, int limite);

        /// <summary>
        /// Compétences auxquelles une séance peut être rattachée : le niveau de
        /// l'élève, et les niveaux voisins. C'est l'amont qui compte — un blocage
        /// en 6e vient souvent d'une notion de CM1.
        /// </summary>
        Task<IEnumerable<CompetenceCandidate>> GetCandidatesAsync(
            int matiereId, int niveauOrdre, int margeAmont, int margeAval,
            string? codeNiveau, CancellationToken ct = default);

        /// <summary>
        /// Les notions du programme d une classe précise, dans une matière.
        ///
        /// PAS DE MARGE NI DE RÈGLE DE VOIE, contrairement aux candidates de
        /// l observateur : l identifiant de niveau désigne déjà LA classe, pas
        /// un rang que trois classes de lycée se partagent. Il n y a donc rien
        /// à départager.
        /// </summary>
        Task<IEnumerable<CompetenceCandidate>> GetNotionsDuNiveauAsync(
            int matiereId, int niveauScolaireId, CancellationToken ct = default);

        /// <summary>
        /// Intègre des observations et met à jour la maîtrise. Les codes
        /// inconnus du référentiel sont ignorés. Retourne le nombre appliqué.
        /// </summary>
        Task<int> AppliquerObservationsAsync(
            int eleveId,
            IEnumerable<ObservationCompetence> observations,
            string source,
            CancellationToken ct = default);

        /// <summary>
        /// La carte des compétences de l enfant, pour son propre écran.
        /// </summary>
        /// <param name="marquerVue">
        /// Vrai quand l enfant OUVRE sa carte : la date de dernière visite est
        /// alors avancée, et les victoires listées dans cet appel ne
        /// reparaîtront plus au suivant.
        ///
        /// Faux quand c est le parent qui regarde : sa visite ne doit pas
        /// consommer la récompense de son enfant, qui ne la verrait jamais.
        /// </param>
        Task<Progression?> GetProgressionAsync(
            int eleveId, bool marquerVue, CancellationToken ct = default);
    }
}
