using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les échéances de révision du référentiel — voir `EcheanceReferentiel`
    /// et `EcheanceReferentielWorker`, qui est leur seul écrivain régulier.
    /// </summary>
    public interface IEcheanceReferentielRepository
    {
        /// <summary>
        /// TOUT ce qui a une page à relever et n'est pas clos — sans
        /// considération de date. Le relevé est quotidien pour toutes les
        /// lignes, pas seulement celles dont l'échéance approche : l'écran
        /// d'administration montre un statut par ligne, et un statut vieux
        /// d'un an n'en est pas un.
        /// </summary>
        Task<IEnumerable<EcheanceASignaler>> GetAVeillerAsync(CancellationToken ct = default);

        /// <summary>
        /// Ce qui mérite un courriel maintenant : une échéance datée qui
        /// tombe dans la fenêtre, OU n'importe quelle ligne — sentinelle
        /// comprise — dont la page vient de changer. Dans les deux cas, pas
        /// plus d'un rappel par <paramref name="espacementRappel"/>.
        /// </summary>
        Task<IEnumerable<EcheanceASignaler>> GetASignalerAsync(
            DateTime maintenant, TimeSpan fenetre, TimeSpan espacementRappel, CancellationToken ct = default);

        /// <summary>Pose la date de dernière alerte, pour espacer les rappels.</summary>
        Task MarquerAlerteEnvoyeeAsync(int id, DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Enregistre un relevé RÉUSSI de la page officielle. `statut` vaut
        /// « changee » ou « inchangee » dès le second relevé ; null pour le
        /// tout premier, qui n'a rien à comparer. UN « CHANGEE » EST COLLANT :
        /// il reste tant qu'un humain n'a pas marqué la ligne traitée, même
        /// si les relevés suivants ne voient plus rien bouger — sinon le
        /// signal disparaîtrait de l'écran au bout d'un jour.
        /// </summary>
        Task EnregistrerReleveAsync(
            int id, string hash, string? statut, DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Enregistre un relevé EN ÉCHEC : la page n'a pas pu être lue. Ne
        /// touche PAS à l'empreinte déjà connue — elle reste la référence du
        /// prochain relevé qui réussira. Ne recouvre pas non plus un
        /// « changee » encore non traité.
        /// </summary>
        Task EnregistrerEchecReleveAsync(int id, DateTime maintenant, CancellationToken ct = default);

        /// <summary>Crée la ligne si elle n'existe pas déjà (clé naturelle : matière + niveaux + date).</summary>
        Task SeedSiAbsenteAsync(
            string matiereLibelle, string niveauxConcernes, DateTime dateEcheance,
            bool dateConnue, string? texteOfficiel, string? notes, string? url = null,
            bool sentinelle = false, string? matieresCodes = null, string? niveauxCodes = null,
            CancellationToken ct = default);

        /// <summary>
        /// Combien d'échéances datées sont DÉPASSÉES sans avoir été traitées
        /// — les sentinelles et les dates de rappel provisoires ne comptent
        /// pas. Zéro veut dire : rien de connu n'attend une vérification en
        /// retard. Ça ne veut pas dire « tout est juste » — voir le worker.
        /// </summary>
        Task<int> CompterEnRetardAsync(DateTime maintenant, CancellationToken ct = default);

        /// <summary>Toutes les lignes, traitées comprises — pour l'écran d'administration.</summary>
        Task<IEnumerable<EcheanceReferentielDetail>> GetToutesAsync(CancellationToken ct = default);

        /// <summary>
        /// Le geste humain qui arrête les rappels. Pour une échéance, clôt la
        /// ligne. Pour une sentinelle, remet seulement son statut à zéro : elle
        /// continue de veiller. Faux si la ligne n'existe pas.
        /// </summary>
        Task<bool> MarquerTraiteeAsync(int id, DateTime maintenant, CancellationToken ct = default);
    }
}
