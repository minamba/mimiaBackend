namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Profil enfant rattaché à un compte parent. Ce n'est pas un compte :
    /// l'enfant n'a pas de credentials propres, il est sélectionné après
    /// la connexion du parent.
    /// </summary>
    public partial class Eleve
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        /// <summary>Pilote le contenu : programme, exercices, compétences attendues.</summary>
        public int NiveauScolaireId { get; set; }

        /// <summary>
        /// Quand l enfant a regardé sa carte de progression pour la dernière
        /// fois. Nulle tant qu il ne l a jamais ouverte.
        ///
        /// C EST LA FRONTIÈRE QUI REND LA RÉCOMPENSE POSSIBLE. La maîtrise est
        /// recalculée par un observateur qui tourne APRÈS la séance : on ne
        /// peut donc rien célébrer pendant le cours. Cette date dit ce qui a
        /// été acquis depuis la dernière visite — c est-à-dire ce qui mérite
        /// d être annoncé.
        /// </summary>
        public DateTime? ProgressionVueLe { get; set; }

        public string? Prenom { get; set; }

        /// <summary>Administratif : sert à distinguer deux homonymes côté admin.</summary>
        public string? Nom { get; set; }

        /// <summary>
        /// Pilote le ton et l'interface (vocabulaire, longueur des réponses).
        /// Volontairement indépendant du niveau scolaire.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Pilote uniquement les accords grammaticaux du professeur. Stocké en
        /// entier : 0 = non précisé, ce qui laisse les profils antérieurs valides.
        /// </summary>
        public Domain.Models.Sexe Sexe { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DerniereActivite { get; set; }

        /// <summary>
        /// Quand le parent a retiré ce profil, ou null s'il est actif.
        ///
        /// Le profil disparaît des listes et LIBÈRE SA PLACE dans la formule,
        /// mais garde tout son historique — conversations, évaluations, fiches,
        /// maîtrise des compétences. C'est ce que veut le parent qui bascule
        /// d'un enfant à l'autre, et c'est réversible.
        ///
        /// La ligne n'est jamais retirée de la base : les consommations
        /// pointent dessus, et les effacer rendrait au pot les heures déjà
        /// utilisées.
        /// </summary>
        public DateTime? ArchiveLe { get; set; }

        /// <summary>
        /// Quand les données personnelles ont été effacées, ou null.
        ///
        /// L'autre geste, celui du droit à l'effacement : l'identité est
        /// vidée et l'historique pédagogique supprimé, définitivement. La ligne
        /// subsiste sans rien de personnel, uniquement pour ancrer le registre
        /// de consommation — de la comptabilité d'abonnement, pas une donnée de
        /// l'enfant.
        ///
        /// Un profil anonymisé ne se restaure pas : il n'y a plus rien à
        /// restaurer.
        /// </summary>
        public DateTime? AnonymiseLe { get; set; }

        /// <summary>
        /// Le code que l'enfant tape sur mimia.fr pour entrer dans ses cours.
        ///
        /// STOCKÉ EN CLAIR, ET C'EST UNE DÉCISION
        /// -------------------------------------
        /// Le parent doit pouvoir le RELIRE : un enfant l'oublie, le perd, ou
        /// se déconnecte. Un hachage ne se relit pas — c'est son intérêt — donc
        /// il faudrait en régénérer un à chaque oubli, et redonner le nouveau à
        /// l'enfant. Ingérable pour un CE1.
        ///
        /// Ce qui rend ce choix proportionné : ce code n'ouvre QUE les cours de
        /// cet enfant. Pas la facturation, pas ses frères et sœurs, pas le
        /// compte du parent. Une fuite de base donnerait accès aux leçons d'un
        /// enfant, pas au foyer.
        ///
        /// En contrepartie, la limitation des tentatives compte davantage : on
        /// ne peut pas s'appuyer sur la lenteur d'un hachage pour freiner qui
        /// essaierait des codes au hasard.
        /// </summary>
        public string? CodeAcces { get; set; }

        /// <summary>
        /// Quand le parent a coupé l'accès de cet enfant. Null s'il est ouvert.
        ///
        /// Une DATE et non un booléen : savoir DEPUIS QUAND l'accès est coupé
        /// répond à la seule question qu'on se pose ensuite — « c'est moi qui
        /// ai fait ça la semaine dernière, ou ça vient de bugger ? ».
        /// </summary>
        public DateTime? AccesSuspenduLe { get; set; }

        public virtual Parent? Parent { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }

        public virtual ICollection<MaitriseEleve> Maitrises { get; set; } = new List<MaitriseEleve>();

        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

        public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

        public virtual ICollection<RapportSeance> Rapports { get; set; } = new List<RapportSeance>();

        public virtual ICollection<FicheRevision> Fiches { get; set; } = new List<FicheRevision>();

        public virtual ICollection<ConsommationEleve> Consommations { get; set; } = new List<ConsommationEleve>();
    }
}
