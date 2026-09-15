namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Un contrôle à venir, tel que le calendrier de l'élève et le rappel en
    /// séance le montrent.
    /// </summary>
    public class ControleScolaireEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Sujet { get; set; }

        public DateTime DateControle { get; set; }

        public TimeSpan? HeureControle { get; set; }

        /// <summary>
        /// Quand le contrôle a été posé — donc quand sa préparation a pu
        /// commencer.
        ///
        /// Sert à borner « ce qui a posé problème PENDANT la préparation » :
        /// une lacune observée des mois plus tôt n'a rien à faire dans la
        /// justification d'un contrôle posé la semaine dernière.
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>La dernière séance ouverte pour préparer ce contrôle. Null si aucune.</summary>
        public DateTime? DernierePreparationLe { get; set; }

        /// <summary>
        /// Combien de fois l'élève est venu le préparer — c'est lui qui décide
        /// du libellé du bouton : « Commencer à réviser » ou « Continuer la
        /// préparation ».
        /// </summary>
        public int NombrePreparations { get; set; }

        /// <summary>La note obtenue, sur 20. Null tant qu'elle n'est pas connue.</summary>
        public double? Note { get; set; }

        /// <summary>Comment ça s'est passé, rapporté par le professeur.</summary>
        public string? Ressenti { get; set; }

        /// <summary>Quand le point a été fait. Null = le professeur le demandera.</summary>
        public DateTime? BilanLe { get; set; }

        /// <summary>
        /// Vrai quand plus personne ne redemandera comment ça s'est passé :
        /// soit le point a été fait, soit la question a déjà été posée autant
        /// de fois qu'on se l'autorise.
        ///
        /// Calculé par le dépôt, qui seul connaît le plafond : l'écran a
        /// besoin de savoir s'il doit annoncer une attente, pas de recompter.
        /// </summary>
        public bool BilanClos { get; set; }

        /// <summary>Énoncé et copie sur deux feuilles ? Null tant que l'élève n'a pas répondu.</summary>
        public bool? CopieSeparee { get; set; }

        public DateTime? CopieDemandeeLe { get; set; }

        /// <summary>L'énoncé est-il arrivé ?</summary>
        public bool EnonceRecu { get; set; }

        /// <summary>La copie de l'élève est-elle arrivée ?</summary>
        public bool CopieRecue { get; set; }

        public DateTime? CopieAnalyseeLe { get; set; }

        /// <summary>
        /// Le professeur a-t-il TOUT ce qu'il lui faut pour analyser ? L'énoncé
        /// ET la copie s'ils sont séparés, la copie seule sinon. Calculé ici,
        /// une fois — le rappel au professeur et la clôture de l'analyse
        /// posent la même question et ne doivent jamais y répondre
        /// différemment.
        /// </summary>
        public bool CopieComplete => CopieRecue && (CopieSeparee == false || EnonceRecu);
    }

    /// <summary>
    /// Une notion déclarée par le professeur, déjà rapprochée du référentiel
    /// quand c'était possible.
    /// </summary>
    public record NotionDeclaree(int? CompetenceId, string Libelle);

    /// <summary>
    /// Ce qu'a produit l'enregistrement d'un résultat de contrôle : les codes
    /// de compétence à porter au moteur de maîtrise.
    ///
    /// LE DÉPÔT NE TOUCHE PAS AUX MAÎTRISES LUI-MÊME. Elles ont leur propre
    /// dépôt, leur propre calcul bayésien et leur propre source ; les écrire
    /// depuis ici créerait un second chemin d'écriture sur la donnée la plus
    /// sensible du produit. Il rend donc ce qu'il a constaté, et l'appelant
    /// l'applique là où il faut.
    /// </summary>
    public record ResultatEnregistre(
        IReadOnlyList<string> CodesReussis,
        IReadOnlyList<string> CodesRates);
}
