namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un exercice de compréhension orale archivé : le passage lu par le
    /// professeur, ce que l'élève en a compris, et son retour.
    ///
    /// UNE SEULE ÉCRITURE, JAMAIS DE MISE À JOUR — contrairement à
    /// <see cref="Dictee"/>. Une dictée peut voir sa copie arriver puis sa
    /// correction plus tard, parfois une autre séance ; une compréhension
    /// orale se conclut en un seul échange (écoute, réponse, retour), donc
    /// une seule ligne, jamais réécrite.
    ///
    /// L'AUDIO EST RÉGÉNÉRÉ À L'ARCHIVAGE, PAS CAPTURÉ EN DIRECT. La lecture
    /// à voix haute part au fil de l'eau en plusieurs appels de synthèse
    /// (un par groupe de phrases, voir <c>SyntheseVocaleService</c>) — il
    /// n'existe jamais, côté serveur, UN SEUL appel correspondant exactement
    /// à tout le passage. Au moment de l'archivage, <see cref="Passage"/> est
    /// resynthétisé une seule fois, proprement, pour produire un fichier
    /// unique à réécouter.
    /// </summary>
    public partial class ComprehensionOrale
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        /// <summary>Séance pendant laquelle l'exercice a eu lieu.</summary>
        public int ConversationId { get; set; }

        /// <summary>Ce qui était travaillé, en quelques mots. Facultatif.</summary>
        public string? Titre { get; set; }

        /// <summary>Code de la langue du passage : en, fr, es, de, it, zh.</summary>
        public string Langue { get; set; } = null!;

        /// <summary>Le texte exact lu à voix haute. Sert aussi de script pour régénérer l'audio.</summary>
        public string Passage { get; set; } = null!;

        /// <summary>Ce que l'élève a répondu, mot pour mot ou presque — sa vraie réponse, pas un résumé.</summary>
        public string ReponseEleve { get; set; } = null!;

        /// <summary>Ce qu'il a réellement compris, dans les mots du professeur — son évaluation, pas une citation.</summary>
        public string Comprehension { get; set; } = null!;

        /// <summary>Le retour du professeur, lisible par l'élève et par le parent.</summary>
        public string? Remarque { get; set; }

        /// <summary>
        /// L'audio, quand il était encore rangé DANS la base.
        ///
        /// PLUS JAMAIS REMPLI DEPUIS LE 10/09/2026 : mesuré chez le premier
        /// élève, la table pesait 9,14 Mo pour quinze lignes — six cents
        /// kilo-octets par exercice. À mille élèves faisant deux
        /// compréhensions orales par semaine, c'est une cinquantaine de
        /// giga-octets d'audio par an dans une base qu'il faut sauvegarder,
        /// restaurer et migrer. L'audio vit désormais sur le disque, et la
        /// base n'en garde que le chemin.
        ///
        /// La colonne reste pour les lignes écrites avant ce changement : la
        /// lecture y retombe quand <see cref="AudioChemin"/> est nul, et la
        /// purge finit par les vider comme les autres.
        /// </summary>
        public byte[]? AudioDonnees { get; set; }

        /// <summary>
        /// Où l'audio est rangé sur le disque, relativement à la racine de
        /// stockage configurée. Null si la synthèse a échoué à l'archivage
        /// (la ligne texte s'archive quand même, silencieusement), ou si
        /// l'audio a depuis été purgé.
        ///
        /// RELATIF, JAMAIS ABSOLU : la racine change entre le poste de
        /// développement et le conteneur. Un chemin absolu en base rendrait
        /// toutes les archives illisibles au premier déplacement.
        /// </summary>
        public string? AudioChemin { get; set; }

        /// <summary>
        /// Quand l'audio a été effacé. Null tant qu'il est encore
        /// consultable — même mécanique que <see cref="PieceJointe.DonneesEffaceesLe"/>.
        /// </summary>
        public DateTime? AudioEffaceLe { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// L'élève ne l'a jamais ouverte tant que cette date est nulle — même
        /// principe que <see cref="Dictee.DateConsultation"/>.
        /// </summary>
        public DateTime? DateConsultation { get; set; }

        /// <summary>
        /// LA CLASSE DE L'ÉLÈVE LE JOUR DE L'EXERCICE, ET NON SA CLASSE
        /// ACTUELLE. Même raison que sur <see cref="Dictee.NiveauScolaireId"/> :
        /// nullable, et définitivement.
        /// </summary>
        public int? NiveauScolaireId { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }
    }
}
