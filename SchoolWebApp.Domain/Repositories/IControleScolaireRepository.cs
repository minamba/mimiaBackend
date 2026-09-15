using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IControleScolaireRepository
    {
        /// <summary>
        /// Posé par le professeur en séance. Null si la conversation
        /// n'appartient pas à cet élève.
        /// </summary>
        /// <param name="matiereId">
        /// La matière DÉJÀ RÉSOLUE par l'appelant contre la liste fermée des
        /// matières de l'élève (voir `ResolveurMatiereDeclaree`) — jamais un
        /// identifiant venu du texte du modèle. Null = celle de la
        /// conversation, qui reste le cas majoritaire.
        /// </param>
        Task<ControleScolaireEleve?> EnregistrerDepuisConversationAsync(
            int eleveId, int conversationId, int? matiereId, string? sujet,
            DateTime dateControle, TimeSpan? heureControle, CancellationToken ct = default);

        /// <summary>
        /// Posé depuis le calendrier par le parent ou l'enfant. `matiereId`
        /// est fourni par l'appelant, déjà validé contre le référentiel de
        /// l'élève avant l'appel — jamais du texte libre.
        /// </summary>
        Task<ControleScolaireEleve?> EnregistrerDepuisCalendrierAsync(
            int eleveId, int matiereId, string posePar, string? sujet,
            DateTime dateControle, TimeSpan? heureControle, CancellationToken ct = default);

        /// <summary>
        /// Les contrôles du mois affiché, bornée comme les évaluations —
        /// contrairement aux évaluations prévues, un contrôle a une vraie
        /// date, donc un vrai jour dans la grille du mois consulté.
        /// </summary>
        Task<IEnumerable<ControleScolaireEleve>> GetEntreAsync(
            int eleveId, DateTime debut, DateTime finExclusive, CancellationToken ct = default);

        /// <summary>
        /// Le plus proche contrôle NON DÉPASSÉ pour cet élève et cette
        /// matière — alimente le rappel en séance. Null s'il n'y en a pas.
        /// </summary>
        Task<ControleScolaireEleve?> GetProchainAsync(
            int eleveId, int matiereId, DateTime maintenant, CancellationToken ct = default);

        /// <summary>Un contrôle précis de cet élève. Null s'il ne lui appartient pas.</summary>
        Task<ControleScolaireEleve?> GetAsync(
            int eleveId, int controleId, CancellationToken ct = default);

        /// <summary>Les contrôles encore à venir, du plus proche au plus lointain.</summary>
        Task<IEnumerable<ControleScolaireEleve>> GetAVenirAsync(
            int eleveId, DateTime maintenant, int limite, CancellationToken ct = default);

        /// <summary>Les contrôles dépassés, du plus récent au plus ancien.</summary>
        Task<IEnumerable<ControleScolaireEleve>> GetPassesAsync(
            int eleveId, DateTime maintenant, int limite, CancellationToken ct = default);

        /// <summary>
        /// Où en est la préparation d'un contrôle. Null s'il n'appartient pas
        /// à cet élève.
        /// </summary>
        Task<PreparationControle?> GetPreparationAsync(
            int eleveId, int controleId, CancellationToken ct = default);

        /// <summary>
        /// La même chose pour plusieurs contrôles d'un coup.
        ///
        /// EXISTE POUR ÉVITER UN N+1 SUR L'ÉCRAN LE PLUS CONSULTÉ DU PRODUIT :
        /// la section « Mes contrôles » de l'accueil en affiche trois, la page
        /// dédiée davantage. Une requête par contrôle serait payée à chaque
        /// ouverture de l'application.
        /// </summary>
        Task<IReadOnlyDictionary<int, PreparationControle>> GetPreparationsAsync(
            int eleveId, IEnumerable<int> controleIds, CancellationToken ct = default);

        /// <summary>
        /// Ajoute des notions au périmètre d'un contrôle, sans jamais créer de
        /// doublon. `travaillee` pose la date du jour sur les lignes citées —
        /// le professeur ne les déclare qu'après les avoir travaillées.
        /// Renvoie le nombre de lignes créées ou mises à jour.
        /// </summary>
        Task<int> AjouterNotionsAsync(
            int eleveId, int controleId, int matiereId,
            IEnumerable<NotionDeclaree> notions, bool travaillee,
            CancellationToken ct = default);

        /// <summary>
        /// Remplace le sujet d'un contrôle par ce que le professeur en a
        /// appris en séance.
        ///
        /// UN CONTRÔLE MAL DÉCLARÉ SE COMPLÈTE, IL NE SE RECRÉE PAS. Beaucoup
        /// arrivent avec « contrôle de maths » et rien d'autre : c'est en
        /// parlant avec l'élève que le professeur découvre qu'il s'agit de
        /// Thalès. Faux si le contrôle n'est pas à cet élève dans cette
        /// matière.
        /// </summary>
        Task<bool> PreciserSujetAsync(
            int eleveId, int controleId, int matiereId, string sujet,
            CancellationToken ct = default);

        /// <summary>
        /// Pose « travaillée » sur les notions de tous les contrôles NON
        /// DÉPASSÉS de cet élève dans cette matière dont la compétence figure
        /// dans la liste de codes.
        ///
        /// Appelée par l'observateur après chaque séance : travailler une
        /// notion au programme d'un contrôle, c'est préparer ce contrôle,
        /// que le professeur ait pensé à le déclarer ou non.
        /// </summary>
        Task<int> MarquerNotionsTravailleesAsync(
            int eleveId, int matiereId, IEnumerable<string> codesCompetences,
            DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// L'élève ouvre une séance POUR préparer ce contrôle : pose la date
        /// et incrémente le compteur. Faux si le contrôle n'est pas à lui.
        /// </summary>
        Task<bool> MarquerPreparationAsync(
            int eleveId, int controleId, DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Modifie la date, l'heure et le sujet d'un contrôle.
        ///
        /// LA MATIÈRE N'EN FAIT PAS PARTIE, ET C'EST DÉLIBÉRÉ. En changer
        /// après coup viderait le programme, invaliderait la préparation déjà
        /// faite, et laisserait le professeur de l'ancienne matière avec des
        /// séances rattachées à un contrôle qui n'est plus le sien. S'être
        /// trompé de matière se répare en supprimant, pas en déplaçant.
        /// </summary>
        Task<ControleScolaireEleve?> ModifierAsync(
            int eleveId, int controleId, string? sujet,
            DateTime dateControle, TimeSpan? heureControle, CancellationToken ct = default);

        /// <summary>Supprime un contrôle et son périmètre. Faux s'il n'est pas à cet élève.</summary>
        Task<bool> SupprimerAsync(int eleveId, int controleId, CancellationToken ct = default);

        /// <summary>
        /// Le contrôle PASSÉ dont le professeur n'a pas encore fait le point
        /// avec l'élève, dans cette matière. Null s'il n'y en a pas.
        ///
        /// C'est ce qui déclenche « alors, comment ça s'est passé ? » à la
        /// séance suivante — et ce qui fait que la question ne se repose plus
        /// une fois qu'on y a répondu.
        /// </summary>
        Task<ControleScolaireEleve?> GetADebrieferAsync(
            int eleveId, int matiereId, DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Compte une demande de plus faite à l'élève sur ce contrôle.
        ///
        /// <summary>
        /// LE CONTRÔLE À VENIR DE CETTE MATIÈRE QUE CETTE SÉANCE A LAISSÉ SANS
        /// VERDICT. Null s'il n'y en a pas — le cas ordinaire.
        ///
        /// Existe pour le filet de fin de séance : une préparation ne doit pas
        /// se refermer sans que l'élève sache où il en est, même quand il a
        /// fermé l'onglet au milieu.
        ///
        /// AUCUNE CONDITION DE PRÉPARATION ICI — décidé avec Camara le
        /// 13/09/2026. Savoir si la séance a préparé ce contrôle est un
        /// jugement, pas une trace mécanique : c'est le professeur qui le
        /// rend, transcription et programme sous les yeux. Cette requête ne
        /// fait que lui présenter le contrôle ; il répond « préparation » ou
        /// « rien à voir ».
        ///
        /// « SANS VERDICT » se mesure sur la fenêtre de la séance : un verdict posé
        /// AVANT cette séance ne vaut pas pour elle — c'est justement ce qu'on
        /// veut faire évoluer.
        /// </summary>
        Task<ControleScolaireEleve?> GetPreparationSansVerdictAsync(
            int eleveId, int matiereId, DateTime debut, DateTime fin,
            CancellationToken ct = default);

        /// <summary>
        /// Le professeur tranche : l'élève est-il prêt pour ce contrôle ?
        ///
        /// `verdict` vaut PRET, BIENTOT ou PAS_PRET — les constantes de
        /// <see cref="PretControle"/>, jamais une chaîne libre. Un verdict
        /// inconnu ne fait rien et rend faux.
        ///
        /// Le dernier verdict REMPLACE le précédent : ce qui compte est où
        /// l'élève en est aujourd'hui, pas l'historique de ses progrès.
        /// Cloisonné sur la matière, comme le résultat : juger d'une
        /// préparation est un acte pédagogique.
        /// </summary>
        Task<bool> PoserVerdictPretAsync(
            int eleveId, int controleId, int matiereId,
            string verdict, string? observation, DateTime maintenant,
            CancellationToken ct = default);

        /// <summary>
        /// C'est ce compteur, et non la bonne volonté du modèle, qui garantit
        /// qu'on cesse de demander : passé le plafond, `GetADebrieferAsync` ne
        /// rend plus ce contrôle.
        /// </summary>
        Task<bool> MarquerRelanceBilanAsync(
            int eleveId, int controleId, CancellationToken ct = default);

        /// <summary>
        /// Enregistre ce que le contrôle a donné : la note si elle est connue,
        /// le ressenti de l'élève, et ce que la copie corrigée a montré notion
        /// par notion.
        ///
        /// Pose `BilanLe` : le point est fait, on ne le redemandera plus.
        /// Renvoie les codes de compétence réussis et ratés, pour que
        /// l'appelant les applique au moteur de maîtrise — le dépôt ne touche
        /// pas aux maîtrises lui-même.
        /// </summary>
        Task<ResultatEnregistre?> EnregistrerResultatAsync(
            int eleveId, int controleId, int matiereId,
            double? note, string? ressenti,
            IEnumerable<NotionDeclaree> reussies, IEnumerable<NotionDeclaree> ratees,
            DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// L'élève a répondu à « L'énoncé et ta copie sont-ils séparés ? ».
        /// Faux si le contrôle n'est pas à lui, dans cette matière.
        /// </summary>
        Task<bool> PoserChoixCopieAsync(
            int eleveId, int controleId, int matiereId, bool separee, DateTime maintenant,
            CancellationToken ct = default);

        /// <summary>Une pièce est arrivée : l'énoncé, ou sa copie.</summary>
        Task<bool> RecevoirPieceCopieAsync(
            int eleveId, int controleId, int matiereId, bool estEnonce, int pieceJointeId,
            DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// La copie de contrôle en cours d'échange dans cette matière — question
        /// répondue, analyse pas encore rendue, activité depuis `depuis`. Null
        /// s'il n'y en a pas, le cas ordinaire.
        /// </summary>
        Task<ControleScolaireEleve?> GetCopieEnCoursAsync(
            int eleveId, int matiereId, DateTime depuis, CancellationToken ct = default);

        /// <summary>
        /// Clôt l'analyse de la copie — SEULEMENT si toutes les pièces sont là.
        /// Faux sinon, et rien n'est écrit.
        /// </summary>
        Task<bool> MarquerCopieAnalyseeAsync(
            int eleveId, int controleId, DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Le dernier contrôle PASSÉ de cette matière, depuis au plus
        /// `joursMaximum` jours — clos ou non. C'est celui dont l'élève peut
        /// avoir la copie sous la main. `maintenant` est l'heure de Paris.
        /// </summary>
        Task<ControleScolaireEleve?> GetDernierPasseAsync(
            int eleveId, int matiereId, DateTime maintenant, int joursMaximum,
            CancellationToken ct = default);

        /// <summary>
        /// Une nouvelle demande de copie commence : l'état de la précédente
        /// est effacé, la question sera reposée. Faux si le contrôle n'est pas
        /// à cet élève, dans cette matière.
        /// </summary>
        Task<bool> OuvrirCopieAsync(
            int eleveId, int controleId, int matiereId, CancellationToken ct = default);
    }
}
