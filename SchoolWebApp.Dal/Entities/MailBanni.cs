namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une adresse à qui le service est fermé.
    ///
    /// UNE LISTE D'ADRESSES, PAS UN DRAPEAU SUR LE COMPTE
    /// --------------------------------------------------
    /// Un indicateur « banni » posé sur `Parent` disparaîtrait avec le compte —
    /// et c'est précisément à ce moment-là qu'il devient utile. Quelqu'un qu'on
    /// bannit supprime souvent son compte dans la foulée, puis se réinscrit le
    /// lendemain avec la même adresse. La liste, elle, survit à l'effacement :
    /// elle est le seul endroit d'où le refus peut encore parler.
    ///
    /// Même raisonnement que `EssaiConsomme`, qui garde l'empreinte d'une
    /// adresse pour empêcher de reprendre l'essai en se réinscrivant.
    ///
    /// EN CLAIR, ET NON EN EMPREINTE
    /// -----------------------------
    /// `EssaiConsomme` hache l'adresse : personne n'a jamais besoin de la
    /// relire, seulement de la reconnaître. Ici c'est l'inverse — une liste de
    /// bannis qu'on ne peut pas LIRE est une liste qu'on ne peut pas lever. Il
    /// faut voir « untel@exemple.fr » pour décider de le retirer six mois plus
    /// tard, quand le compte a disparu depuis longtemps et qu'il ne reste que
    /// cette ligne.
    ///
    /// C'est une donnée personnelle conservée après effacement du compte, au
    /// titre de la prévention des abus. Elle est limitée au strict nécessaire —
    /// une adresse, une date, un motif — et se retire d'un clic.
    ///
    /// LES DEUX BASES LA LISENT
    /// ------------------------
    /// L'API la gère ; le serveur d'identité la CONSULTE, par la même connexion
    /// qu'il utilise déjà pour les interrupteurs de l'onglet Modes. C'est là
    /// que le refus doit tomber : l'inscription et la connexion lui
    /// appartiennent, pas à l'API.
    /// </summary>
    public partial class MailBanni
    {
        public int Id { get; set; }

        /// <summary>
        /// L'adresse, NORMALISÉE : minuscules, sans espaces autour.
        ///
        /// Normalisée à l'écriture et non à la lecture, pour que l'index unique
        /// veuille dire quelque chose. Sans ça, « Jean@X.fr » et « jean@x.fr »
        /// feraient deux lignes, et lever le bannissement sur l'une laisserait
        /// l'autre en place.
        /// </summary>
        public string Mail { get; set; } = string.Empty;

        /// <summary>
        /// Pourquoi. Facultatif, et pourtant ce qui compte le plus longtemps.
        ///
        /// Une adresse seule, retrouvée dans six mois, ne dit pas si elle a été
        /// bannie pour une fraude au paiement ou par erreur de manipulation.
        /// C'est le motif qui rend la décision révisable.
        /// </summary>
        public string? Motif { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Qui a banni. Utile le jour où l'on est plusieurs à administrer, et
        /// dès aujourd'hui pour distinguer un bannissement d'une importation.
        /// </summary>
        public string? BanniPar { get; set; }
    }
}
