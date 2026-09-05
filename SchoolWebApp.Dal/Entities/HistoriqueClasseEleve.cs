namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Les années scolaires qu'un élève a passées chez nous, en intervalles.
    ///
    /// POURQUOI DES INTERVALLES PLUTÔT QU'UNE COLONNE PARTOUT
    /// -----------------------------------------------------
    /// La fiche d'un élève se lit par année : on suit le niveau d'une classe,
    /// pas une scolarité empilée. Il faut donc savoir, pour chaque trace de
    /// travail, dans quelle classe l'enfant était ce jour-là.
    ///
    /// La solution évidente — une colonne `niveau_scolaire_id` sur chaque
    /// table — casse sur les MESSAGES. Il y en a plus de mille par matière et
    /// par élève, et les estampiller imposerait une lecture de plus en base à
    /// chaque message du chat, sur le chemin le plus chaud du produit. Ici, un
    /// message se rattache à son année par sa date, sans rien coûter à
    /// l'écriture.
    ///
    /// CE QUI NE DOIT SURTOUT PAS ÊTRE DÉCOUPÉ : LA CONVERSATION
    /// --------------------------------------------------------
    /// Le fil d'une matière vit d'une année sur l'autre, et c'est voulu. C'est
    /// ce qui permet au professeur de dire à un élève de seconde « cette
    /// notion, tu l'as vue en troisième, elle est redevenue fragile ». Il
    /// connaît tout le parcours ; seule la FICHE se lit année par année.
    ///
    /// Une première tentative estampillait la conversation à sa création : elle
    /// aurait attribué à la troisième tout le travail de seconde d'un élève qui
    /// n'avait pas rouvert son fil. L'erreur ne se serait vue qu'à la rentrée
    /// suivante.
    /// </summary>
    public class HistoriqueClasseEleve
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int NiveauScolaireId { get; set; }

        public DateTime Debut { get; set; }

        /// <summary>
        /// NULL tant que l'élève est dans cette classe. Une seule ligne ouverte
        /// à la fois : c'est elle qui dit sa classe d'aujourd'hui, et le
        /// changement de classe la ferme avant d'en ouvrir une autre.
        /// </summary>
        public DateTime? Fin { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }
    }
}
