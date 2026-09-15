namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UNE DATE À LAQUELLE LE RÉFÉRENTIEL DOIT ÊTRE REVÉRIFIÉ CONTRE UN TEXTE
    /// OFFICIEL — un programme qui bascule, ou un texte encore incertain qu'il
    /// faut surveiller.
    ///
    /// POURQUOI CETTE TABLE EXISTE, ET CE QU'ELLE NE FAIT PAS
    /// --------------------------------------------------------
    /// Voulu par Camara le 13/09/2026 : « il faut un worker qui garantit que
    /// les référentiels sont à jour tous les ans ». Ce que ce worker PEUT
    /// garantir : que personne n'oublie une échéance. Ce qu'il ne PEUT PAS
    /// faire : aller chercher tout seul le nouveau texte et réécrire le
    /// référentiel. Il n'existe aucune source officielle structurée des
    /// programmes — le Bulletin officiel est un texte de loi en PDF, pas un
    /// flux de « compétences » exploitable par une machine. Le convertir en
    /// lignes de référentiel demande un jugement humain : c'est exactement ce
    /// que documente `ReferentielSeeder`, où TROIS passes de vérification ont
    /// chacune trouvé une vraie erreur — jusqu'à un script qui n'avait lu que
    /// la première annexe d'un arrêté qui en portait trois. Un worker n'aurait
    /// pas fait mieux qu'un lecteur pressé ; il aurait juste fait croire que
    /// c'était vérifié.
    ///
    /// Cette table retrace donc le tableau qui vivait en commentaire dans
    /// `ReferentielSeeder` — matière, niveaux concernés, date de bascule,
    /// texte officiel — sous une forme qu'un worker peut réellement
    /// interroger, pour prévenir plutôt que pour deviner.
    /// </summary>
    public partial class EcheanceReferentiel
    {
        public int Id { get; set; }

        /// <summary>
        /// La matière ou le groupe de matières concerné, tel qu'il figurait
        /// dans le tableau de vérification — « Français et maths »,
        /// « Langues vivantes », « Lycée général et technologique ».
        ///
        /// TEXTE LIBRE, PAS UNE CLÉ ÉTRANGÈRE VERS Matiere — et c'est voulu :
        /// certaines lignes concernent plusieurs matières à la fois, et
        /// d'autres (les langues vivantes autres que l'anglais) n'ont pas
        /// encore de ligne dans `Matiere` au moment où cette échéance est
        /// enregistrée. Forcer une clé aurait obligé à créer ces matières en
        /// avance, ou à éclater une ligne du texte officiel en plusieurs
        /// lignes qui n'existent pas dans le texte lui-même.
        /// </summary>
        public string MatiereLibelle { get; set; } = null!;

        /// <summary>Les niveaux concernés, tels que le texte officiel les regroupe — « 4e », « CE1, CE2, CM2 ».</summary>
        public string NiveauxConcernes { get; set; } = null!;

        /// <summary>
        /// La date à laquelle agir.
        ///
        /// DEUX SENS SELON <see cref="DateConnue"/> : si le texte officiel est
        /// déjà publié, c'est sa date d'entrée en vigueur. S'il ne l'est pas
        /// encore — un texte « en consultation nationale » —, c'est une date
        /// de rappel provisoire : le prochain moment où il faut revérifier
        /// s'il a été publié, pas une échéance réelle.
        /// </summary>
        public DateTime DateEcheance { get; set; }

        /// <summary>
        /// Faux si <see cref="DateEcheance"/> est une simple date de rappel —
        /// le texte définitif n'existe pas encore. Change le message envoyé :
        /// « le programme bascule » contre « vérifie si le texte est sorti ».
        /// </summary>
        public bool DateConnue { get; set; } = true;

        /// <summary>Le Bulletin officiel qui porte le nouveau texte, quand il est déjà identifié.</summary>
        public string? TexteOfficiel { get; set; }

        /// <summary>
        /// L'adresse de la page officielle du texte (education.gouv.fr/bo/…),
        /// QUAND ELLE A ÉTÉ TROUVÉE ET VÉRIFIÉE — jamais devinée. Une adresse
        /// fausse serait pire qu'aucune : elle rendrait un 404 silencieux, ou
        /// pointerait vers le mauvais texte sans que personne ne le remarque.
        ///
        /// C'est elle que <see cref="EcheanceReferentielWorker"/> va
        /// interroger : il ne peut PAS transformer cette page en compétences
        /// — voir la table elle-même pour pourquoi — mais il PEUT constater
        /// mécaniquement qu'elle existe, et qu'elle a changé depuis la
        /// dernière fois. C'est exactement la partie automatisable de la
        /// veille, et rien de plus.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Une empreinte du contenu de <see cref="Url"/>, relevée au dernier
        /// relevé RÉUSSI. Sert à détecter un CHANGEMENT sans jamais
        /// interpréter ce qui a changé — cette part-là reste humaine.
        /// N'est PAS effacée par un relevé en échec : elle reste la référence
        /// du prochain relevé qui réussira.
        /// </summary>
        public string? DernierHashPage { get; set; }

        /// <summary>
        /// « changee » | « inchangee » | « injoignable » | null (jamais
        /// relevée, ou pas d'adresse connue). NULL AUSSI après le tout
        /// PREMIER relevé réussi : il n'y a alors rien à comparer, et
        /// annoncer « inchangée » mentirait sur ce qui a été constaté.
        /// </summary>
        public string? DernierStatutVeille { get; set; }

        /// <summary>
        /// Quand le worker a tenté de relever la page pour la dernière fois —
        /// que la tentative ait réussi ou non. C'est la date qui répond à
        /// « quand a-t-on vérifié ? », pas seulement « quand a-t-on réussi ? ».
        /// </summary>
        public DateTime? DernierePageVerifieeLe { get; set; }

        /// <summary>
        /// UNE SENTINELLE N'A PAS D'ÉCHÉANCE : elle surveille une page en
        /// permanence, et n'alerte que lorsque cette page change. La
        /// page-carrefour des programmes du ministère en est une — c'est le
        /// seul filet contre ce que le worker ne connaît pas encore : un
        /// texte publié pour une matière ou un niveau qu'aucune ligne
        /// d'échéance ne cite. « Marquer traitée » remet son statut à zéro
        /// sans jamais la clore : une sentinelle qu'on clôt n'en est plus une.
        /// </summary>
        public bool Sentinelle { get; set; }

        /// <summary>
        /// Les codes de `Matiere` que cette échéance concerne, séparés par
        /// « ; » (ex. « FRANCAIS;MATHS »). C'est par eux que l'écran
        /// d'administration range l'échéance sous la bonne matière de la
        /// bonne classe — <see cref="MatiereLibelle"/> reste le texte lisible,
        /// libre, et n'est jamais rapproché par ressemblance.
        /// </summary>
        public string? MatieresCodes { get; set; }

        /// <summary>Les codes de `NiveauScolaire` concernés, séparés par « ; » (ex. « QUATRIEME »).</summary>
        public string? NiveauxCodes { get; set; }

        /// <summary>Le contexte libre — ce qui a motivé la ligne, ce qu'il faudra vérifier en premier.</summary>
        public string? Notes { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Quand la dernière alerte a été envoyée. Sert au worker à espacer
        /// ses rappels — voir <see cref="EcheanceReferentielWorker"/> — plutôt
        /// que d'écrire à Camara chaque jour sur la même échéance.
        /// </summary>
        public DateTime? DerniereAlerteLe { get; set; }

        /// <summary>
        /// Posée à la main une fois le référentiel revérifié contre le texte
        /// officiel et corrigé si besoin. C'EST LE SEUL GESTE QUI ARRÊTE LES
        /// ALERTES — rien d'automatique ne peut constater qu'une vérification
        /// a réellement eu lieu, pour la même raison que rien d'automatique
        /// ne peut la faire à la place d'un humain.
        /// </summary>
        public DateTime? TraiteeLe { get; set; }
    }
}
