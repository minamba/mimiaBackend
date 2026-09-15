namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un contrôle à venir à l'école, posé soit par le parent ou l'enfant
    /// depuis le calendrier, soit par le professeur pendant une séance de
    /// chat quand l'élève l'annonce.
    ///
    /// UNE VRAIE DATE, CONTRAIREMENT À <see cref="EvaluationPrevue"/>. Celle-ci
    /// n'a pas de jour précis — un rendez-vous « à la prochaine fois » — donc
    /// elle vit dans une liste à part et se purge après 45 jours pour ne pas
    /// l'encombrer indéfiniment. Un contrôle scolaire, lui, a une date connue :
    /// il s'épingle sur le bon jour du calendrier, et reste affiché même
    /// après, sans jamais être purgé.
    /// </summary>
    public partial class ControleScolaire
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>
        /// Résolue depuis la conversation quand posé en séance, validée
        /// contre le référentiel matière de l'élève quand posé depuis le
        /// calendrier — jamais depuis un texte libre.
        /// </summary>
        public int MatiereId { get; set; }

        /// <summary>Null si posé depuis le calendrier, hors séance.</summary>
        public int? ConversationId { get; set; }

        /// <summary>
        /// LA CLASSE DE L'ÉLÈVE LE JOUR DE LA SAISIE, ET NON SA CLASSE
        /// ACTUELLE. Même raison que sur <see cref="Dictee.NiveauScolaireId"/> :
        /// nullable, et définitivement.
        /// </summary>
        public int? NiveauScolaireId { get; set; }

        /// <summary>ELEVE, PARENT ou PROFESSEUR — qui a posé le contrôle.</summary>
        public string PosePar { get; set; } = null!;

        public string? Sujet { get; set; }

        public DateTime DateControle { get; set; }

        /// <summary>
        /// Facultative : un enfant qui sait qu'il a contrôle vendredi mais ne
        /// se souvient plus du créneau ne doit pas être bloqué à la saisie.
        /// </summary>
        public TimeSpan? HeureControle { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Dernière modification par le parent ou l'enfant. Null tant qu'il n'y en a pas eu.</summary>
        public DateTime? DateModification { get; set; }

        /// <summary>
        /// La dernière séance ouverte POUR préparer ce contrôle — la
        /// « dernière séance liée » de sa fiche.
        ///
        /// Une date et un compte plutôt qu'une table de journal : aucun écran
        /// n'affiche autre chose que ces deux valeurs. Le jour où l'historique
        /// des préparations sera demandé, la table se créera, alimentée par le
        /// même point de code.
        /// </summary>
        public DateTime? DernierePreparationLe { get; set; }

        /// <inheritdoc cref="DernierePreparationLe"/>
        public int NombrePreparations { get; set; }

        /// <summary>
        /// EST-IL PRÊT ? LE PROFESSEUR TRANCHE, PAS LE POURCENTAGE.
        ///
        /// `PAS_PRET`, `BIENTOT` ou `PRET`. Null tant qu'il ne s'est pas
        /// prononcé.
        ///
        /// POURQUOI CE N'EST PAS UN SEUIL SUR LA BARRE. Voulu par Camara le
        /// 13/09/2026 : « c'est pas parce qu'on est pas à 100 % partout que
        /// l'élève n'est pas prêt ». Un enfant à 76 % sur deux notions solides
        /// peut être prêt ; un autre à 90 % peut ne pas l'être parce que la
        /// seule notion qui lui manque est celle qui tombera. Le pourcentage
        /// mesure ce qui est acquis, il ne sait pas ce que le contrôle
        /// demandera — le professeur, si.
        ///
        /// Un seuil aurait aussi l'inconvénient d'être une promesse : à 80 %
        /// l'enfant lirait « prêt » et arrêterait de réviser, sans que personne
        /// n'ait jamais regardé SES notions à lui.
        /// </summary>
        public string? PretVerdict { get; set; }

        /// <summary>
        /// Ce que le professeur en dit à l'élève, en une ou deux phrases —
        /// « Tu tiens les deux rapports, il te reste à poser le calcul avant de
        /// conclure ». C'est ce qui rend le statut utile : une bulle verte sans
        /// raison n'apprend rien, et une bulle rouge sans raison décourage.
        /// </summary>
        public string? PretObservation { get; set; }

        /// <summary>
        /// Quand il s'est prononcé.
        ///
        /// LE VERDICT NE PÉRIME PAS, MAIS IL SE DATE. Décidé avec Camara :
        /// rien ne s'efface tout seul — un enfant qui verrait son « prêt »
        /// disparaître sans avoir rien fait de mal ne comprendrait pas. La
        /// fiche affiche donc « Nora te trouvait prêt le 12 septembre », et le
        /// professeur le réévalue à la préparation suivante.
        /// </summary>
        public DateTime? PretLe { get; set; }

        /// <summary>
        /// La note obtenue, sur 20. Null tant qu'elle n'est pas connue — et
        /// elle peut ne jamais l'être.
        ///
        /// LUE, JAMAIS CALCULÉE. Contrairement à la note d'une évaluation
        /// Mimia (recalculée depuis les verdicts par question, parce que le
        /// modèle se trompait en l'agrégeant), celle-ci est écrite par le
        /// professeur de l'école sur la copie : le modèle la recopie, il ne la
        /// juge pas. On la borne quand même à 0-20.
        /// </summary>
        public double? Note { get; set; }

        /// <summary>
        /// Comment ça s'est passé, dans les mots de l'élève rapportés par le
        /// professeur. C'est souvent tout ce qu'on saura : beaucoup d'enfants
        /// n'ont pas leur note avant plusieurs semaines.
        /// </summary>
        public string? Ressenti { get; set; }

        /// <summary>
        /// Quand le professeur a fait le point après le contrôle. C'EST LUI
        /// LE STATUT : tant qu'il est nul, un contrôle passé attend son
        /// débriefing et le professeur le demandera à la prochaine séance.
        /// Une fois posé, on ne repose plus la question.
        /// </summary>
        public DateTime? BilanLe { get; set; }

        /// <summary>
        /// Combien de fois on a demandé à l'élève comment ça s'était passé.
        ///
        /// LE FILET QUI GARANTIT QU'ON N'INSISTE PAS. La consigne dit au
        /// professeur de clore le contrôle dès que l'élève ne veut pas en
        /// parler — mais une consigne s'oublie, et un contrôle jamais clos
        /// ferait reposer la même question à CHAQUE séance, indéfiniment.
        /// Au-delà de <see cref="ControleScolaire.RelancesMaximum"/>, on
        /// arrête de demander : le refus de l'enfant n'a pas besoin d'être
        /// enregistré pour être respecté.
        /// </summary>
        public int RelancesBilan { get; set; }

        /// <summary>
        /// Deux séances, pas plus. La première fois, l'élève n'a souvent pas
        /// encore sa copie ; la seconde suffit à le savoir. Au-delà, on
        /// n'apprendra plus rien et on ne fait qu'insister.
        /// </summary>
        public const int RelancesMaximum = 2;

        /// <summary>
        /// COMBIEN DE TEMPS ON SUPPOSE QU'UN CONTRÔLE DURE, quand son heure est
        /// connue. Une heure : c'est la durée d'un cours au collège et au lycée.
        ///
        /// Relevé par Camara le 13/09/2026 : contrôle de mathématiques à 18 h,
        /// et la professeure demande à 15 h « alors, c'est passé comment ? ».
        /// L'heure était pourtant saisie. Désormais un contrôle dont l'heure
        /// est connue reste « à venir » jusqu'à son heure, et son bilan ne se
        /// demande qu'une fois cette durée écoulée — jamais pendant.
        ///
        /// Sans heure, rien ne change : on ne sait pas quand il a lieu dans la
        /// journée, donc le bilan attend le lendemain.
        /// </summary>
        public static readonly TimeSpan DureeSupposee = TimeSpan.FromHours(1);

        /// <summary>
        /// L'élève a-t-il dit que l'énoncé et sa copie sont sur deux feuilles
        /// séparées ? Null tant qu'on ne lui a pas demandé.
        ///
        /// Voulu par Camara le 13/09/2026. C'est ce qui décide de ce que le
        /// professeur doit recevoir AVANT d'analyser : l'énoncé ET la copie
        /// s'ils sont séparés, la copie seule s'ils ne le sont pas.
        /// </summary>
        public bool? CopieSeparee { get; set; }

        /// <summary>
        /// Quand l'élève a répondu à la question, ou envoyé une pièce. Borne
        /// le rappel au professeur : une copie jamais envoyée ne le poursuit
        /// pas pendant des semaines.
        /// </summary>
        public DateTime? CopieDemandeeLe { get; set; }

        /// <summary>
        /// La pièce jointe reçue comme ÉNONCÉ. Sans clé étrangère, et c'est
        /// voulu : les octets d'une pièce sont purgés au bout de quelques
        /// jours, et sa ligne part avec sa conversation — le contrôle, lui,
        /// reste. Ce qui compte ici, c'est « l'a-t-on reçu », pas d'y revenir.
        /// </summary>
        public int? EnoncePieceJointeId { get; set; }

        /// <summary>La pièce jointe reçue comme COPIE de l'élève. Même raison, pas de clé étrangère.</summary>
        public int? CopiePieceJointeId { get; set; }

        /// <summary>
        /// Quand le professeur a rendu son analyse de la copie — le
        /// [CONTROLE_RESULTAT] écrit une fois TOUTES les pièces reçues. Éteint
        /// le rappel à chaque tour.
        /// </summary>
        public DateTime? CopieAnalyseeLe { get; set; }

        public virtual ICollection<ControleNotion> Notions { get; set; } = new List<ControleNotion>();

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }
    }
}
