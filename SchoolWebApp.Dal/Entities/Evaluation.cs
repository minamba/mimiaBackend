namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une évaluation notée, passée en fin de notion.
    ///
    /// Distincte de <see cref="MaitriseEleve"/> et volontairement : la maîtrise
    /// est une estimation continue que l'observateur révise en silence après
    /// chaque séance, l'évaluation est un événement daté que l'enfant a vécu et
    /// dont le parent garde une trace. On ne peut pas déduire l'une de l'autre.
    /// </summary>
    public partial class Evaluation
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        /// <summary>Séance pendant laquelle l'évaluation a été passée.</summary>
        public int ConversationId { get; set; }

        /// <summary>Ce sur quoi a porté l'évaluation, dit avec les mots du professeur.</summary>
        public string? Notion { get; set; }

        /// <summary>Note sur 20. Décimale : un 13,5 est une note, pas un arrondi manquant.</summary>
        public double Note { get; set; }

        /// <summary>Le mot du professeur sur cette évaluation, lisible par l'élève et par le parent.</summary>
        public string? Remarque { get; set; }

        /// <summary>Ce qu'il reste à reprendre. Vide quand tout est acquis.</summary>
        public string? ARevoir { get; set; }

        /// <summary>
        /// Le détail du contrôle en JSON : les questions, ce que l'élève a
        /// répondu, et le verdict de chacune.
        ///
        /// En JSON plutôt qu'en table fille : ce détail se lit toujours en
        /// entier, avec son évaluation, et jamais requêté question par
        /// question. Une table de plus n'apporterait qu'une jointure.
        /// </summary>
        public string? Detail { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// LA NOTE EST TOMBÉE, MAIS LA CORRECTION ORALE N'A PAS EU LE TEMPS.
        ///
        /// Voulu par Camara le 13/09/2026 : « le professeur est intelligent :
        /// en fonction du temps qu'il reste, il sait s'ils ont le temps de
        /// corriger. La note, il la donne, c'est rapide ; la correction, s'ils
        /// n'ont pas le temps, ils la feront au cours prochain. »
        ///
        /// RIEN N'EST PERDU POUR AUTANT. La copie écrite — la bonne réponse et
        /// le pourquoi de chaque erreur — est enregistrée dans
        /// <see cref="Detail"/> au moment même de la note. Ce qui attend, c'est
        /// le moment où le professeur reprend les erreurs AVEC l'enfant.
        ///
        /// Vrai tant que cette reprise n'a pas eu lieu : le cours suivant dans
        /// la matière s'ouvre alors par elle.
        /// </summary>
        public bool CorrectionReportee { get; set; }

        /// <summary>
        /// Combien de fois on a proposé de reprendre cette correction.
        ///
        /// LE FILET QUI GARANTIT QU'ON N'INSISTE PAS — même règle, et même
        /// raison, que <see cref="ControleScolaire.RelancesBilan"/>. On propose,
        /// on n'impose pas ; mais une consigne s'oublie, et une correction
        /// jamais close se reproposerait à chaque séance, indéfiniment.
        /// </summary>
        public int RelancesCorrection { get; set; }

        /// <summary>
        /// Deux cours, pas plus. Au-delà, la copie n'est plus fraîche : l'enfant
        /// ne se souvient plus de ce qu'il avait tenté, et on n'apprend plus
        /// rien à la reprendre.
        /// </summary>
        public const int RelancesCorrectionMaximum = 2;

        /// <summary>
        /// LA CLASSE DE L'ÉLÈVE LE JOUR DE LA NOTE, ET NON SA CLASSE ACTUELLE.
        ///
        /// Sans elle, une note prise en 4e remonte dans la fiche d'un élève de
        /// 3e comme si elle était de cette année. Le suivi mélangeait ainsi
        /// toute la scolarité en une seule courbe, alors qu'on suit le niveau
        /// d'une classe.
        ///
        /// NULLABLE, ET DÉFINITIVEMENT. Les notes antérieures à cette colonne
        /// n'ont pas de classe et n'en auront jamais : rien n'enregistrait le
        /// niveau au moment de l'évaluation, et le changement de classe est une
        /// simple mise à jour de colonne qui ne laisse aucune trace datée. On ne
        /// peut donc rien reconstituer. Ces notes se rangent dans « Toute la
        /// scolarité » plutôt que d'être attribuées au hasard à une année.
        /// </summary>
        public int? NiveauScolaireId { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }
    }
}
