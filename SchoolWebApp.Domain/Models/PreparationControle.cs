namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Où en est la préparation d'un contrôle : le périmètre de notions, leur
    /// état, et l'avancement qui en découle.
    ///
    /// RIEN N'EST STOCKÉ ICI, TOUT EST CALCULÉ. L'état de chaque notion vient
    /// du moteur de maîtrise (mis à jour après chaque séance par
    /// l'observateur), les seuils de <c>SeuilsMaitrise</c>. Stocker un
    /// pourcentage l'aurait figé au dernier calcul, et il aurait fallu penser
    /// à le rafraîchir à chaque observation.
    /// </summary>
    public class PreparationControle
    {
        /// <summary>
        /// L'avancement global, de 0 à 100 — LA MOYENNE DES NOTIONS, jamais
        /// une pondération à part.
        ///
        /// Toujours renseigné, y compris à zéro : voulu par Camara le
        /// 13/09/2026. La barre est toujours là, même avant la première
        /// séance — un enfant qui voit une jauge vide sait qu'il lui reste
        /// tout à faire, là où l'absence de jauge ne disait rien du tout.
        /// `PerimetreConnu` dit à part si le programme est connu.
        /// </summary>
        public int Pourcent { get; set; }

        public bool PerimetreConnu { get; set; }

        public int Total { get; set; }

        public int Acquises { get; set; }

        public DateTime? DernierePreparationLe { get; set; }

        /// <summary>
        /// EST-IL PRÊT ? "pas-commence" | "pas-pret" | "bientot" | "pret", ou
        /// NULL — et null veut dire qu'il n'y a aucune pastille à afficher.
        ///
        /// C'EST LE PROFESSEUR QUI GÈRE, règle posée par Camara le
        /// 13/09/2026. Trois des quatre valeurs sont son verdict ;
        /// "pas-commence" est le seul que l'application déduise, parce que
        /// c'est un fait mesuré et non un jugement. Entre les deux — l'élève
        /// travaille, personne ne l'a encore jugé — la valeur est null et
        /// l'écran se tait.
        ///
        /// UN SEUL CHAMP et non « verdict + a-commencé » : l'écran ne doit
        /// jamais recomposer la règle pour choisir sa couleur, deux écrans la
        /// recomposeraient un jour différemment.
        /// </summary>
        public string? PretStatut { get; set; }

        /// <summary>
        /// LA JUSTIFICATION DU STATUT, ET ELLE VIENT DU PROFESSEUR — jamais du
        /// code.
        ///
        /// Null tant qu'il ne s'est pas prononcé, et c'est alors qu'il n'y a
        /// AUCUNE pastille : voir <see cref="PretStatut"/>.
        ///
        /// Une version de ce champ a été doublée, le 13/09/2026, d'un constat
        /// composé par le serveur à partir des notions — « il te reste à
        /// consolider X (54 %) ». Retiré le jour même sur la règle de Camara :
        /// « on ne devine pas, c'est le professeur qui gère ». Le constat
        /// paraissait inoffensif parce qu'il n'énonçait que des faits, mais il
        /// répondait à une question — pourquoi ce statut — dont la réponse
        /// n'appartient qu'à celui qui a posé le statut.
        /// </summary>
        public string? PretObservation { get; set; }

        /// <summary>
        /// Quand il s'est prononcé. Le verdict ne périme pas, mais la fiche le
        /// date : « Nora te trouvait prêt le 12 septembre ».
        /// </summary>
        public DateTime? PretLe { get; set; }

        public List<NotionControle> Notions { get; set; } = [];
    }

    /// <summary>Une notion au programme d'un contrôle, avec son état du moment.</summary>
    public class NotionControle
    {
        public int Id { get; set; }

        /// <summary>Null pour une notion hors référentiel — voir `ControleNotion`.</summary>
        public int? CompetenceId { get; set; }

        /// <summary>
        /// VALIDÉE PAR UNE MESURE — une copie notée ou le résultat d'un contrôle
        /// —, et non par une impression de conversation.
        ///
        /// Voulu par Camara le 13/09/2026 : une notion qu'un élève a prouvée sur
        /// copie pendant la préparation d'un contrôle ne se ré-évalue pas, sauf
        /// s'il le demande — et il est alors prévenu qu'une note moins bonne
        /// fera baisser sa progression. Le professeur doit donc savoir
        /// lesquelles sont dans ce cas : c'est ce que dit ce drapeau.
        /// </summary>
        public bool ValideeParMesure { get; set; }

        public string Libelle { get; set; } = null!;

        /// <summary>
        /// Le domaine du référentiel (« Nombres et calculs »). Renseigné pour les
        /// épreuves d'examen, où les notions se lisent groupées — elles sont
        /// trop nombreuses pour une liste à plat. Null pour un contrôle.
        /// </summary>
        public string? Domaine { get; set; }

        /// <summary>
        /// L'état MONTRÉ À L'ENFANT : "acquise" | "a-confirmer" | "en-cours" |
        /// "fragile" | "a-decouvrir". Une notion travaillée en préparation ne
        /// descend jamais en dessous de « en cours » — c'est l'effort rendu
        /// visible.
        /// </summary>
        public string Etat { get; set; } = null!;

        /// <summary>
        /// L'état MESURÉ, sans l'encouragement — celui que le professeur doit
        /// voir.
        ///
        /// LES DEUX NE S'ADRESSENT PAS À LA MÊME PERSONNE. Remonter une notion
        /// travaillée à « en cours » évite à l'enfant une barre qui ne bouge
        /// jamais ; servir la même chose au professeur lui ferait croire
        /// qu'une notion est consolidée alors qu'elle est encore fragile, et
        /// il passerait à autre chose la veille du contrôle.
        /// </summary>
        public string EtatMesure { get; set; } = null!;

        /// <summary>Quand elle a été travaillée en préparation. Null si jamais.</summary>
        public DateTime? TravailleeLe { get; set; }

        /// <summary>
        /// Ce que la copie corrigée a montré : "reussie", "ratee", ou null
        /// tant que le contrôle n'a pas été repris.
        /// </summary>
        public string? Resultat { get; set; }

        /// <summary>
        /// Où il en est SUR CETTE NOTION, de 0 à 100 — la mesure du moteur de
        /// maîtrise, ramenée sur cent.
        ///
        /// C'est ce qui donne à la fiche sa raison d'être : une barre par
        /// notion montre à l'enfant ce qui est tenu et ce qui reste, là où un
        /// pourcentage global ne dit pas PAR QUOI continuer. Le global n'est
        /// d'ailleurs rien d'autre que la moyenne de ces valeurs — les deux ne
        /// peuvent donc pas se contredire à l'écran.
        /// </summary>
        public int Pourcent { get; set; }
    }
}
