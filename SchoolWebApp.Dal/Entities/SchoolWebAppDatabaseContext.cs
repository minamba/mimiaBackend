using Microsoft.EntityFrameworkCore;

namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Contexte métier (SCHOOL_Database). Distinct de la base d'identité,
    /// qui vit dans SCHOOL_Identity_Database et appartient à l'IdentityServer.
    ///
    /// Toutes les contraintes passent par la Fluent API ci-dessous —
    /// aucune data annotation sur les entités.
    /// </summary>
    public partial class SchoolWebAppDatabaseContext : DbContext
    {
        public SchoolWebAppDatabaseContext(DbContextOptions<SchoolWebAppDatabaseContext> options)
            : base(options)
        {
        }

        public virtual DbSet<NiveauScolaire> NiveauxScolaires { get; set; }
        public virtual DbSet<Parent> Parents { get; set; }
        public virtual DbSet<Eleve> Eleves { get; set; }
        public virtual DbSet<Matiere> Matieres { get; set; }
        public virtual DbSet<Competence> Competences { get; set; }
        public virtual DbSet<CompetencePrerequis> CompetencesPrerequis { get; set; }
        public virtual DbSet<MaitriseEleve> MaitrisesEleves { get; set; }
        public virtual DbSet<Conversation> Conversations { get; set; }
        public virtual DbSet<Message> Messages { get; set; }

        public virtual DbSet<PieceJointe> PiecesJointes { get; set; }
        public virtual DbSet<PlancheSchema> PlanchesSchemas { get; set; }

        public virtual DbSet<SessionEleve> SessionsEleves { get; set; }
        public virtual DbSet<Evaluation> Evaluations { get; set; }
        public virtual DbSet<RapportSeance> RapportsSeance { get; set; }
        public virtual DbSet<FicheRevision> FichesRevision { get; set; }
        public virtual DbSet<Offre> Offres { get; set; }
        public virtual DbSet<OffreRecharge> OffresRecharge { get; set; }
        public virtual DbSet<Abonnement> Abonnements { get; set; }
        public virtual DbSet<ConsommationEleve> ConsommationsEleves { get; set; }
        public virtual DbSet<Recharge> Recharges { get; set; }
        public virtual DbSet<Reglage> Reglages { get; set; }
        public virtual DbSet<EssaiConsomme> EssaisConsommes { get; set; }

        public virtual DbSet<VisiteSite> VisitesSite { get; set; }

        /// <summary>Les avis laissés par les familles sur le produit.</summary>
        public virtual DbSet<AvisClient> AvisClients { get; set; }

        public virtual DbSet<BandeauPromo> BandeauxPromo { get; set; }

        public virtual DbSet<MailBanni> MailsBannis { get; set; }

        /// <summary>
        /// Une VUE, pas une table : la règle du forfait vit en SQL, où il
        /// n'en existe qu'un exemplaire. Voir `ForfaitAbonnement`.
        /// </summary>
        public virtual DbSet<ForfaitAbonnement> ForfaitsAbonnement { get; set; }

        public virtual DbSet<MesureVoix> MesuresVoix { get; set; }

        /// <summary>Les appels au modèle qui ne sont pas des tours de dialogue.</summary>
        public virtual DbSet<AppelClaude> AppelsClaude { get; set; }

        /// <summary>
        /// Toutes les dates de la base sont en UTC, et repartent qualifiées
        /// comme telles. Sans ça le navigateur — et les gabarits d'e-mail —
        /// les prendraient pour de l'heure de Paris et afficheraient deux
        /// heures de moins l'été.
        ///
        /// Posé ici en convention plutôt que colonne par colonne : une date
        /// ajoutée demain hériterait sinon du défaut, et le défaut est faux.
        /// </summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>().HaveConversion<ConvertisseurDateUtc>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<ConvertisseurDateUtcNullable>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---------------------------------------------------------------
            // NiveauScolaire
            // ---------------------------------------------------------------
            modelBuilder.Entity<NiveauScolaire>(entity =>
            {
                entity.ToTable("NiveauScolaire");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasMaxLength(20).HasColumnName("code");
                entity.Property(e => e.Libelle).HasMaxLength(100).HasColumnName("libelle");
                entity.Property(e => e.Cycle).HasMaxLength(20).HasColumnName("cycle");
                entity.Property(e => e.Ordre).HasColumnName("ordre");

                entity.HasIndex(e => e.Code).IsUnique();
            });

            // ---------------------------------------------------------------
            // Parent
            // ---------------------------------------------------------------
            modelBuilder.Entity<Parent>(entity =>
            {
                entity.ToTable("Parent");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdentityUserId).HasMaxLength(450).IsRequired().HasColumnName("identity_user_id");
                entity.Property(e => e.Prenom).HasMaxLength(100).HasColumnName("prenom");
                entity.Property(e => e.Nom).HasMaxLength(100).HasColumnName("nom");
                entity.Property(e => e.Mail).HasMaxLength(255).HasColumnName("mail");
                entity.Property(e => e.StripeClientId).HasMaxLength(255).HasColumnName("stripe_client_id");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DerniereConnexion).HasColumnName("derniere_connexion");
                // LA VALEUR PAR DÉFAUT EST DÉCLARÉE ICI AUSSI, pas seulement dans
                // la migration : sans elle, le modèle et l instantané divergent
                // et EF refuse de démarrer sur un « PendingModelChanges ».
                entity.Property(e => e.EstAdministrateur)
                      .HasDefaultValue(false)
                      .HasColumnName("est_administrateur");

                // Un utilisateur du serveur d'identité = un parent, jamais deux.
                entity.HasIndex(e => e.IdentityUserId).IsUnique();
                entity.HasIndex(e => e.Mail).IsUnique();

                // C'EST L'INDEX QUI REND LES WEBHOOKS EXPLOITABLES.
                //
                // Un événement Stripe ne cite que le client ; le retrouver
                // impose donc une lecture par cette colonne, à chaque
                // renouvellement de chaque abonné. Unique parce qu'un client
                // Stripe ne peut appartenir qu'à un parent : deux parents sur
                // le même client, ce serait la facture de l'un débitée pour
                // l'autre. Le filtre exclut les comptes sans client — la
                // majorité tant qu'ils n'ont pas payé.
                entity.HasIndex(e => e.StripeClientId)
                    .IsUnique()
                    .HasFilter("[stripe_client_id] IS NOT NULL");
            });

            // ---------------------------------------------------------------
            // Eleve
            // ---------------------------------------------------------------
            modelBuilder.Entity<Eleve>(entity =>
            {
                entity.ToTable("Eleve");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");
                entity.Property(e => e.Prenom).HasMaxLength(100).HasColumnName("prenom");
                entity.Property(e => e.Nom).HasMaxLength(100).HasColumnName("nom");
                entity.Property(e => e.Age).HasColumnName("age");
                entity.Property(e => e.Sexe).HasColumnName("sexe").HasDefaultValue(Domain.Models.Sexe.NonPrecise);
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DerniereActivite).HasColumnName("derniere_activite");
                entity.Property(e => e.ArchiveLe).HasColumnName("archive_le");
                entity.Property(e => e.AnonymiseLe).HasColumnName("anonymise_le");
                entity.Property(e => e.CodeAcces).HasMaxLength(16).HasColumnName("code_acces");
                entity.Property(e => e.AccesSuspenduLe).HasColumnName("acces_suspendu_le");

                // UN CODE NE DÉSIGNE QU'UN ENFANT, ET LA BASE LE GARANTIT.
                //
                // Sans cet index, deux enfants pourraient recevoir le même code
                // par collision du tirage, et le second à se connecter
                // atterrirait dans les cours du premier. Le filtre exclut les
                // codes absents : plusieurs enfants sans code, c'est normal.
                entity.HasIndex(e => e.CodeAcces)
                    .IsUnique()
                    .HasFilter("[code_acces] IS NOT NULL");

                entity.HasOne(e => e.Parent)
                      .WithMany(p => p.Eleves)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany(n => n.Eleves)
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ParentId);
            });

            // ---------------------------------------------------------------
            // Matiere
            // ---------------------------------------------------------------
            modelBuilder.Entity<Matiere>(entity =>
            {
                entity.ToTable("Matiere");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasMaxLength(50).HasColumnName("code");
                entity.Property(e => e.Libelle).HasMaxLength(100).HasColumnName("libelle");
                entity.Property(e => e.AgentSlug).HasMaxLength(50).HasColumnName("agent_slug");
                entity.Property(e => e.ProfPrenom).HasMaxLength(50).HasColumnName("prof_prenom");
                entity.Property(e => e.ProfAvatar).HasMaxLength(50).HasColumnName("prof_avatar");
                entity.Property(e => e.ProfCouleur).HasMaxLength(20).HasColumnName("prof_couleur");
                entity.Property(e => e.Ordre).HasColumnName("ordre");
                entity.Property(e => e.Promesse).HasMaxLength(200).HasColumnName("promesse");

                // Valeurs par défaut « toute la scolarité » : une matière déjà
                // en base avant l'ajout de ces colonnes reste visible partout,
                // ce qui est exactement le comportement qu'elle avait.
                entity.Property(e => e.NiveauOrdreMin)
                    .HasDefaultValue(1).HasColumnName("niveau_ordre_min");
                entity.Property(e => e.NiveauOrdreMax)
                    .HasDefaultValue(12).HasColumnName("niveau_ordre_max");

                entity.Property(e => e.Active).HasColumnName("active");

                entity.HasIndex(e => e.Code).IsUnique();
            });

            // ---------------------------------------------------------------
            // Competence
            // ---------------------------------------------------------------
            modelBuilder.Entity<Competence>(entity =>
            {
                entity.ToTable("Competence");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");
                entity.Property(e => e.Code).HasMaxLength(100).HasColumnName("code");
                entity.Property(e => e.CodeEduscol).HasMaxLength(100).HasColumnName("code_eduscol");
                entity.Property(e => e.Domaine).HasMaxLength(150).HasColumnName("domaine");
                entity.Property(e => e.Libelle).HasMaxLength(500).HasColumnName("libelle");
                entity.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
                entity.Property(e => e.Ordre).HasColumnName("ordre");

                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.Competences)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany(n => n.Competences)
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => new { e.MatiereId, e.NiveauScolaireId });
            });

            // ---------------------------------------------------------------
            // CompetencePrerequis — arête réflexive du graphe
            // ---------------------------------------------------------------
            modelBuilder.Entity<CompetencePrerequis>(entity =>
            {
                entity.ToTable("CompetencePrerequis");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CompetenceId).HasColumnName("competence_id");
                entity.Property(e => e.PrerequisId).HasColumnName("prerequis_id");
                entity.Property(e => e.Poids).HasColumnName("poids");

                entity.HasOne(e => e.Competence)
                      .WithMany(c => c.Prerequis)
                      .HasForeignKey(e => e.CompetenceId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict des deux côtés serait redondant, mais NoAction est
                // obligatoire ici : deux cascades vers la même table = cycle SQL Server.
                entity.HasOne(e => e.Prerequis)
                      .WithMany(c => c.Successeurs)
                      .HasForeignKey(e => e.PrerequisId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.CompetenceId, e.PrerequisId }).IsUnique();
            });

            // ---------------------------------------------------------------
            // MaitriseEleve
            // ---------------------------------------------------------------
            modelBuilder.Entity<MaitriseEleve>(entity =>
            {
                entity.ToTable("MaitriseEleve");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.CompetenceId).HasColumnName("competence_id");
                entity.Property(e => e.Score).HasColumnName("score");
                entity.Property(e => e.Confiance).HasColumnName("confiance");
                entity.Property(e => e.NombreObservations).HasColumnName("nombre_observations");
                entity.Property(e => e.DerniereEvaluation).HasColumnName("derniere_evaluation");
                entity.Property(e => e.ProchaineRevision).HasColumnName("prochaine_revision");
                entity.Property(e => e.Source).HasMaxLength(50).HasColumnName("source");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Maitrises)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Competence)
                      .WithMany(c => c.Maitrises)
                      .HasForeignKey(e => e.CompetenceId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Une seule ligne de maîtrise par couple élève/compétence.
                entity.HasIndex(e => new { e.EleveId, e.CompetenceId }).IsUnique();

                // Sert la requête des relances de révision espacée (cron n8n).
                entity.HasIndex(e => e.ProchaineRevision);
            });

            // ---------------------------------------------------------------
            // Conversation
            // ---------------------------------------------------------------
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("Conversation");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.Titre).HasMaxLength(300).HasColumnName("titre");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateDernierMessage).HasColumnName("date_dernier_message");
                entity.Property(e => e.DatePurge).HasColumnName("date_purge");
                entity.Property(e => e.DateDerniereObservation).HasColumnName("date_derniere_observation");
                entity.Property(e => e.DateSortie).HasColumnName("date_sortie");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Conversations)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.Conversations)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.EleveId, e.DateDernierMessage });
                entity.HasIndex(e => e.DatePurge);
            });

            // ---------------------------------------------------------------
            // Message
            // ---------------------------------------------------------------
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Message");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Role).HasMaxLength(20).HasColumnName("role");

                // Pas de HasMaxLength : nvarchar(max). Un échange pédagogique
                // avec un énoncé collé dépasse largement 4000 caractères.
                entity.Property(e => e.Contenu).HasColumnName("contenu");

                entity.Property(e => e.Modele).HasMaxLength(50).HasColumnName("modele");
                entity.Property(e => e.TokensEntree).HasColumnName("tokens_entree");
                entity.Property(e => e.TokensSortie).HasColumnName("tokens_sortie");
                entity.Property(e => e.TokensCacheLecture).HasColumnName("tokens_cache_lecture");
                entity.Property(e => e.TokensCacheEcriture).HasColumnName("tokens_cache_ecriture");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ConversationId, e.DateCreation });
            });

            // ---------------------------------------------------------------
            // PieceJointe
            // ---------------------------------------------------------------
            modelBuilder.Entity<PieceJointe>(entity =>
            {
                entity.ToTable("PieceJointe");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.MessageId).HasColumnName("message_id");
                entity.Property(e => e.NomFichier).HasMaxLength(255).HasColumnName("nom_fichier");
                entity.Property(e => e.TypeMime).HasMaxLength(60).HasColumnName("type_mime");
                entity.Property(e => e.Taille).HasColumnName("taille");
                entity.Property(e => e.NombrePages).HasColumnName("nombre_pages");
                entity.Property(e => e.Donnees).HasColumnName("donnees");

                // Pas de HasMaxLength : nvarchar(max). Une page d'énoncé
                // transcrite dépasse largement 4000 caractères.
                entity.Property(e => e.Transcription).HasColumnName("transcription");
                entity.Property(e => e.DonneesEffaceesLe).HasColumnName("donnees_effacees_le");

                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                // Les deux files d'attente des tâches de fond : ce qui reste à
                // transcrire, et ce dont les octets peuvent partir. Sans cet
                // index, chaque passage balaierait toute la table — y compris
                // les colonnes de blobs.
                entity.HasIndex(e => new { e.DonneesEffaceesLe, e.DateCreation });

                // La conversation supprimée emporte ses documents : c'est ce qui
                // garantit qu'une purge ne laisse rien derrière elle.
                entity.HasOne(e => e.Conversation)
                      .WithMany()
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // NoAction et non Cascade : les deux chemins de suppression
                // partiraient de Conversation, et SQL Server refuse les cycles
                // de cascade. C'est la cascade ci-dessus qui fait le ménage.
                entity.HasOne(e => e.Message)
                      .WithMany()
                      .HasForeignKey(e => e.MessageId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.MessageId);
                entity.HasIndex(e => new { e.ConversationId, e.DateCreation });
            });

            // ---------------------------------------------------------------
            // PlancheSchema
            // ---------------------------------------------------------------
            // ---------------------------------------------------------------
            // SessionEleve
            // ---------------------------------------------------------------
            modelBuilder.Entity<SessionEleve>(entity =>
            {
                entity.ToTable("SessionEleve");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.JetonHache).HasMaxLength(100).IsRequired()
                    .HasColumnName("jeton_hache");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DernierAcces).HasColumnName("dernier_acces");
                entity.Property(e => e.Appareil).HasMaxLength(200).HasColumnName("appareil");

                // C'est l'index par lequel passe CHAQUE appel d'un enfant.
                // Unique autant par correction que par vitesse.
                entity.HasIndex(e => e.JetonHache).IsUnique();

                // L'enfant supprimé emporte ses sessions : une session
                // orpheline serait un jeton qui ouvre sur rien, et qu'on
                // oublierait de nettoyer.
                entity.HasOne(e => e.Eleve)
                    .WithMany()
                    .HasForeignKey(e => e.EleveId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlancheSchema>(entity =>
            {
                entity.ToTable("PlancheSchema");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Cle).HasMaxLength(80).IsRequired().HasColumnName("cle");
                entity.Property(e => e.MatiereCode).HasMaxLength(50).IsRequired().HasColumnName("matiere_code");
                entity.Property(e => e.NomFichier).HasMaxLength(255).HasColumnName("nom_fichier");
                entity.Property(e => e.TypeMime).HasMaxLength(60).HasColumnName("type_mime");
                entity.Property(e => e.Taille).HasColumnName("taille");
                entity.Property(e => e.Donnees).HasColumnName("donnees");
                entity.Property(e => e.Auteur).HasMaxLength(255).HasColumnName("auteur");
                entity.Property(e => e.Source).HasMaxLength(500).HasColumnName("source");
                entity.Property(e => e.Licence).HasMaxLength(120).HasColumnName("licence");

                // Faux par défaut : les planches déjà en base viennent toutes
                // de Commons, et une valeur par défaut évite d'avoir à les
                // parcourir pour poser un drapeau qu'elles n'ont pas.
                entity.Property(e => e.Maison).HasDefaultValue(false).HasColumnName("maison");

                // JSON des étiquettes et de leurs positions. Sans longueur : une
                // planche dense en porte une trentaine, et tronquer en
                // supprimerait au hasard.
                entity.Property(e => e.Reperes).HasColumnName("reperes");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.Contenu).HasColumnName("contenu");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");

                // Une planche par clé : réimporter remplace, ça n'empile pas.
                // Sans cette contrainte, deux versions coexisteraient et celle
                // qui s'afficherait dépendrait de l'ordre de lecture.
                entity.HasIndex(e => e.Cle).IsUnique();
                entity.HasIndex(e => e.MatiereCode);
            });

            // ---------------------------------------------------------------
            // Evaluation
            // ---------------------------------------------------------------
            modelBuilder.Entity<Evaluation>(entity =>
            {
                entity.ToTable("Evaluation");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Notion).HasMaxLength(300).HasColumnName("notion");
                entity.Property(e => e.Note).HasColumnName("note");
                entity.Property(e => e.Remarque).HasMaxLength(2000).HasColumnName("remarque");
                entity.Property(e => e.ARevoir).HasMaxLength(1000).HasColumnName("a_revoir");

                // Pas de HasMaxLength : nvarchar(max). Un contrôle de six
                // questions avec les réponses de l'élève dépasse 4000 caractères.
                entity.Property(e => e.Detail).HasColumnName("detail");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Evaluations)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict et non Cascade : trois chemins de suppression
                // convergeraient sinon vers Evaluation (élève, matière,
                // conversation), et SQL Server refuse les cascades multiples.
                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.Evaluations)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                // NoAction : une conversation purgée au bout d'un an ne doit pas
                // emporter la note. Le parent garde sa trace même quand le
                // détail des échanges a disparu.
                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.Evaluations)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.EleveId, e.DateCreation });
            });

            // ---------------------------------------------------------------
            // RapportSeance
            // ---------------------------------------------------------------
            modelBuilder.Entity<RapportSeance>(entity =>
            {
                entity.ToTable("RapportSeance");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Travaille).HasMaxLength(1000).HasColumnName("travaille");
                entity.Property(e => e.NoteComprehension).HasColumnName("note_comprehension");
                entity.Property(e => e.NoteRevision).HasColumnName("note_revision");
                entity.Property(e => e.Remarque).HasMaxLength(2000).HasColumnName("remarque");
                entity.Property(e => e.ARevoir).HasMaxLength(1000).HasColumnName("a_revoir");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Rapports)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict et NoAction pour les mêmes raisons que sur Evaluation :
                // trois chemins de suppression convergeraient sinon vers cette
                // table, ce que SQL Server refuse. Et une conversation purgée au
                // bout d'un an ne doit pas emporter le compte rendu.
                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.Rapports)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.Rapports)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.NoAction);

                // PLUS d'unicité sur la conversation.
                //
                // L'intention était bonne — éviter deux comptes rendus pour un
                // même cours, le professeur pouvant conclure deux fois (un au
                // revoir, puis l'échéance du minuteur) — mais la clé était
                // fausse : une conversation N'EST PAS une séance. C'est le fil
                // continu d'un élève dans une matière, il vit des semaines et
                // porte des dizaines de cours. C'est même ce qui fait la
                // mémoire du professeur.
                //
                // Conséquence mesurée : cinq séances dans la même journée,
                // cinq comptes rendus écrits, et UNE SEULE ligne en base —
                // chaque nouveau rapport écrasait le précédent. La liste
                // « Ses séances », qui doit donner au parent une trace
                // continue, n'en montrait jamais qu'une.
                //
                // La protection contre la double conclusion vit maintenant
                // dans le dépôt, sur une fenêtre de temps : voir
                // RapportRepository.EnregistrerAsync.
                entity.HasIndex(e => e.ConversationId);

                entity.HasIndex(e => new { e.EleveId, e.DateCreation });
            });

            // ---------------------------------------------------------------
            // FicheRevision
            // ---------------------------------------------------------------
            modelBuilder.Entity<FicheRevision>(entity =>
            {
                entity.ToTable("FicheRevision");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Notion).HasMaxLength(300).IsRequired().HasColumnName("notion");
                entity.Property(e => e.Domaine).HasMaxLength(150).HasColumnName("domaine");

                // nvarchar(max) : une fiche complète avec ses exemples dépasse
                // largement 4000 caractères.
                entity.Property(e => e.Contenu).HasColumnName("contenu");

                entity.Property(e => e.Etat).HasMaxLength(20).HasColumnName("etat");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateMiseAJour).HasColumnName("date_mise_a_jour");
                entity.Property(e => e.DateConsultation).HasColumnName("date_consultation");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Fiches)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.Fiches)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Une seule fiche par notion et par élève : réviser deux fois la
                // même notion doit ENRICHIR la fiche, pas en empiler une
                // deuxième que l'enfant devrait comparer à la première.
                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.Notion }).IsUnique();

                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.DateMiseAJour });
            });

            // ---------------------------------------------------------------
            // Offre / OffreRecharge — le catalogue
            // ---------------------------------------------------------------
            modelBuilder.Entity<Offre>(entity =>
            {
                entity.ToTable("Offre");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasMaxLength(30).IsRequired().HasColumnName("code");
                entity.Property(e => e.Libelle).HasMaxLength(100).HasColumnName("libelle");
                entity.Property(e => e.Accroche).HasMaxLength(300).HasColumnName("accroche");
                entity.Property(e => e.PrixMensuelCentimes).HasColumnName("prix_mensuel_centimes");
                entity.Property(e => e.PrixAnnuelCentimes).HasColumnName("prix_annuel_centimes");
                entity.Property(e => e.NombreEnfantsMax).HasColumnName("nombre_enfants_max");
                entity.Property(e => e.MinutesPotMensuel).HasColumnName("minutes_pot_mensuel");
                entity.Property(e => e.MinutesPlafondEnfant).HasColumnName("minutes_plafond_enfant");
                entity.Property(e => e.JoursValidite).HasColumnName("jours_validite");
                entity.Property(e => e.Ordre).HasColumnName("ordre");
                entity.Property(e => e.Active).HasColumnName("active");
                entity.Property(e => e.EstEssai).HasColumnName("est_essai");
                entity.Property(e => e.StripePrixMensuelId).HasMaxLength(80).HasColumnName("stripe_prix_mensuel_id");
                entity.Property(e => e.StripePrixAnnuelId).HasMaxLength(80).HasColumnName("stripe_prix_annuel_id");

                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<OffreRecharge>(entity =>
            {
                entity.ToTable("OffreRecharge");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasMaxLength(30).IsRequired().HasColumnName("code");
                entity.Property(e => e.Libelle).HasMaxLength(100).HasColumnName("libelle");
                entity.Property(e => e.Minutes).HasColumnName("minutes");
                entity.Property(e => e.PrixCentimes).HasColumnName("prix_centimes");
                entity.Property(e => e.StripePrixId).HasMaxLength(80).HasColumnName("stripe_prix_id");
                entity.Property(e => e.Ordre).HasColumnName("ordre");
                entity.Property(e => e.Active).HasColumnName("active");

                entity.HasIndex(e => e.Code).IsUnique();
            });

            // ---------------------------------------------------------------
            // Abonnement
            // ---------------------------------------------------------------
            modelBuilder.Entity<Abonnement>(entity =>
            {
                entity.ToTable("Abonnement");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.OffreId).HasColumnName("offre_id");
                entity.Property(e => e.DateDebut).HasColumnName("date_debut");
                entity.Property(e => e.DateFin).HasColumnName("date_fin");
                entity.Property(e => e.Statut).HasMaxLength(20).IsRequired().HasColumnName("statut");
                entity.Property(e => e.Periodicite).HasMaxLength(20).IsRequired()
                      .HasDefaultValue(Domain.Models.PeriodiciteAbonnement.Mensuel).HasColumnName("periodicite");
                entity.Property(e => e.PeriodeDebut).HasColumnName("periode_debut");
                entity.Property(e => e.PeriodeFin).HasColumnName("periode_fin");
                entity.Property(e => e.DernierePause).HasColumnName("derniere_pause");
                entity.Property(e => e.PauseJusquau).HasColumnName("pause_jusquau");
                entity.Property(e => e.ResiliationDemandeeLe).HasColumnName("resiliation_demandee_le");
                entity.Property(e => e.AlerteQuotaEnvoyee).HasColumnName("alerte_quota_envoyee");
                entity.Property(e => e.StripeAbonnementId).HasMaxLength(255).HasColumnName("stripe_abonnement_id");
                entity.Property(e => e.ImpayeDepuis).HasColumnName("impaye_depuis");
                entity.Property(e => e.OffrePrevueId).HasColumnName("offre_prevue_id");
                entity.Property(e => e.PeriodicitePrevue).HasMaxLength(20).HasColumnName("periodicite_prevue");
                entity.Property(e => e.ChangementPrevuLe).HasColumnName("changement_prevu_le");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.HasOne(e => e.Parent)
                      .WithMany(p => p.Abonnements)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Offre)
                      .WithMany(o => o.Abonnements)
                      .HasForeignKey(e => e.OffreId)
                      .OnDelete(DeleteBehavior.Restrict);

                // La formule prévue pointe la même table, sans collection en
                // retour : personne n'a besoin de « tous les abonnements qui
                // basculeront vers Solo », et une seconde collection sur Offre
                // n'apporterait qu'une ambiguïté de navigation.
                entity.HasOne(e => e.OffrePrevue)
                      .WithMany()
                      .HasForeignKey(e => e.OffrePrevueId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Sert la recherche de l'abonnement courant d'un parent.
                entity.HasIndex(e => new { e.ParentId, e.Statut });

                // Sert le balayage de renouvellement des périodes échues.
                entity.HasIndex(e => e.PeriodeFin);

                // Par où entrent les webhooks : un renouvellement ou un impayé
                // ne cite que l'abonnement Stripe. Unique — deux abonnements
                // sur le même `sub_…` voudrait dire qu'un paiement crédite
                // deux comptes.
                entity.HasIndex(e => e.StripeAbonnementId)
                    .IsUnique()
                    .HasFilter("[stripe_abonnement_id] IS NOT NULL");
            });

            // ---------------------------------------------------------------
            // ConsommationEleve
            // ---------------------------------------------------------------
            modelBuilder.Entity<ConsommationEleve>(entity =>
            {
                entity.ToTable("ConsommationEleve");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AbonnementId).HasColumnName("abonnement_id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.PeriodeDebut).HasColumnName("periode_debut");
                entity.Property(e => e.SecondesConsommees).HasColumnName("secondes_consommees");
                entity.Property(e => e.DerniereActivite).HasColumnName("derniere_activite");

                entity.HasOne(e => e.Abonnement)
                      .WithMany(a => a.Consommations)
                      .HasForeignKey(e => e.AbonnementId)
                      .OnDelete(DeleteBehavior.Cascade);

                // NoAction : deux cascades convergeraient vers cette table
                // (parent → abonnement → consommation, et parent → élève →
                // consommation), ce que SQL Server refuse.
                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Consommations)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Une seule ligne par enfant et par période : c'est cet index
                // qui rend le décompte idempotent sous concurrence.
                entity.HasIndex(e => new { e.AbonnementId, e.EleveId, e.PeriodeDebut }).IsUnique();
            });

            // ---------------------------------------------------------------
            // Recharge
            // ---------------------------------------------------------------
            // ---------------------------------------------------------------
            // Reglage — les interrupteurs du produit
            // ---------------------------------------------------------------
            modelBuilder.Entity<Reglage>(entity =>
            {
                entity.ToTable("Reglage");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Cle).HasMaxLength(80).IsRequired().HasColumnName("cle");
                entity.Property(e => e.Valeur).HasMaxLength(400).IsRequired().HasColumnName("valeur");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");

                entity.HasIndex(e => e.Cle).IsUnique();
            });

            // ---------------------------------------------------------------
            // EssaiConsomme
            //
            // AUCUNE CLÉ ÉTRANGÈRE VERS Parent, ET C'EST TOUT L'INTÉRÊT.
            // Cette table doit survivre à la suppression du compte : la
            // rattacher au parent la ferait disparaître avec lui, et l'essai
            // redeviendrait reprenable par simple réinscription.
            // ---------------------------------------------------------------
            modelBuilder.Entity<EssaiConsomme>(entity =>
            {
                entity.ToTable("EssaiConsomme");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MailHache).HasMaxLength(64).IsRequired().HasColumnName("mail_hache");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                // Unique : c'est la base qui garantit qu'une adresse n'a qu'un
                // essai, y compris si deux inscriptions se croisent.
                entity.HasIndex(e => e.MailHache).IsUnique();
            });

            // ---------------------------------------------------- VisiteSite
            modelBuilder.Entity<AvisClient>(entity =>
            {
                entity.ToTable("AvisClient", t =>
                    t.HasCheckConstraint("CK_AvisClient_note", "[note] >= 1 AND [note] <= 5"));

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.Note).HasColumnName("note");
                entity.Property(e => e.Titre).HasMaxLength(120).HasColumnName("titre");
                entity.Property(e => e.Commentaire).HasMaxLength(2000).HasColumnName("commentaire");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");

                // LE DÉFAUT EST DÉCLARÉ ICI AUSSI, et pas seulement dans la
                // migration : un défaut présent en base et absent du modèle
                // fait diverger le snapshot, et le démarrage suivant lève
                // `PendingModelChangesWarning`.
                entity.Property(e => e.Publie).HasDefaultValue(false).HasColumnName("publie");

                // UN SEUL AVIS PAR FOYER, garanti par la base. Une règle
                // tenue uniquement par le code céderait au premier
                // double-clic sur « Envoyer ».
                entity.HasIndex(e => e.ParentId).IsUnique();

                // La page d'accueil ne lit que les avis publiés, du plus
                // récent au plus ancien : le seul chemin chaud de la table.
                entity.HasIndex(e => new { e.Publie, e.DateCreation });

                entity.HasOne(e => e.Parent)
                      .WithMany()
                      .HasForeignKey(e => e.ParentId)
                      .HasConstraintName("FK_AvisClient_Parent")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ForfaitAbonnement>(entity =>
            {
                // `ToView` ET NON `ToTable` : sans lui, EF générerait une
                // migration créant une table du même nom, qui écraserait la
                // vue au premier démarrage.
                entity.ToView("vw_ForfaitAbonnement");

                // SANS CLÉ, donc jamais suivie : chaque lecture repart de la
                // base. Un total mis en cache dans le suivi resterait celui
                // d'avant l'achat d'une recharge.
                entity.HasNoKey();

                entity.Property(e => e.AbonnementId).HasColumnName("abonnement_id");
                entity.Property(e => e.MinutesForfait).HasColumnName("minutes_forfait");
                entity.Property(e => e.MinutesRecharge).HasColumnName("minutes_recharge");
                entity.Property(e => e.MinutesAllouees).HasColumnName("minutes_allouees");
                entity.Property(e => e.MinutesPlafondEnfant)
                      .HasColumnName("minutes_plafond_enfant");
            });

            modelBuilder.Entity<MailBanni>(entity =>
            {
                entity.ToTable("MailBanni");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Mail)
                      .HasMaxLength(256).IsRequired().HasColumnName("mail");

                entity.Property(e => e.Motif)
                      .HasMaxLength(300).HasColumnName("motif");

                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.Property(e => e.BanniPar)
                      .HasMaxLength(256).HasColumnName("banni_par");

                // UNIQUE : la question est « cette adresse est-elle bannie ? »,
                // pas « combien de fois ». Deux lignes feraient un
                // bannissement qu'on lève à moitié.
                entity.HasIndex(e => e.Mail).IsUnique();
            });

            modelBuilder.Entity<BandeauPromo>(entity =>
            {
                entity.ToTable("BandeauPromo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Titre)
                      .HasMaxLength(120).IsRequired().HasColumnName("titre");

                entity.Property(e => e.TexteAlternatif)
                      .HasMaxLength(300).IsRequired().HasColumnName("texte_alternatif");

                entity.Property(e => e.Lien)
                      .HasMaxLength(500).HasColumnName("lien");

                entity.Property(e => e.Actif)
                      .HasDefaultValue(false).HasColumnName("actif");

                entity.Property(e => e.PleineLargeur)
                      .HasDefaultValue(false).HasColumnName("pleine_largeur");

                entity.Property(e => e.ImageLarge)
                      .IsRequired().HasColumnName("image_large");

                entity.Property(e => e.TypeMimeLarge)
                      .HasMaxLength(100).IsRequired().HasColumnName("type_mime_large");

                entity.Property(e => e.TailleLarge).HasColumnName("taille_large");

                entity.Property(e => e.ImageMobile).HasColumnName("image_mobile");

                entity.Property(e => e.TypeMimeMobile)
                      .HasMaxLength(100).HasColumnName("type_mime_mobile");

                entity.Property(e => e.TailleMobile).HasColumnName("taille_mobile");

                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");

                // La page d'accueil ne cherche QUE le bandeau affiché, à
                // chaque visite y compris anonyme. Un index sur un booléen
                // paraît maigre, mais la table est petite et très
                // déséquilibrée — au plus une ligne vraie — : c'est
                // exactement le cas où il sert.
                entity.HasIndex(e => e.Actif);
            });

            modelBuilder.Entity<VisiteSite>(entity =>
            {
                entity.ToTable("VisiteSite");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Visiteur).HasMaxLength(64).IsRequired().HasColumnName("visiteur");
                entity.Property(e => e.Horodatage).HasColumnName("horodatage");

                // L'INDEX PORTE SUR LA DATE, ET C'EST LE SEUL QUI SERVE.
                //
                // Toutes les lectures sont des tranches de temps — un jour, un
                // mois, une année — et elles dédoublonnent le visiteur À
                // L'INTÉRIEUR de la tranche. On filtre donc par date d'abord,
                // et le visiteur n'est lu qu'ensuite : l'ajouter à l'index
                // coûterait de l'écriture à chaque venue sans rien accélérer.
                entity.HasIndex(e => e.Horodatage);
            });

            // ---------------------------------------------------------------
            // MesureVoix
            //
            // AUCUNE CLÉ ÉTRANGÈRE, ET POUR LA RAISON INVERSE D'EssaiConsomme.
            // Celle-là devait survivre à la suppression d'un compte ; celle-ci
            // n'a simplement rien à voir avec personne. Elle ne porte ni élève,
            // ni conversation, ni matière — seulement un délai, un drapeau et
            // l'heure. Il n'y a donc rien à effacer quand un parent s'en va.
            // ---------------------------------------------------------------
            modelBuilder.Entity<MesureVoix>(entity =>
            {
                entity.ToTable("MesureVoix");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Seance).HasMaxLength(36).IsRequired().HasColumnName("seance");
                entity.Property(e => e.DelaiMs).HasColumnName("delai_ms");
                entity.Property(e => e.TranscriptionMs).HasColumnName("transcription_ms");
                entity.Property(e => e.AssemblageMs).HasColumnName("assemblage_ms");
                entity.Property(e => e.ReponseMs).HasColumnName("reponse_ms");
                entity.Property(e => e.Repli).HasColumnName("repli");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                // On interroge cette table par période — « les mesures de la
                // semaine du test » — jamais par identifiant.
                entity.HasIndex(e => e.DateCreation);
            });

            // ---------------------------------------------------------------
            // AppelClaude — ce que consomment les tâches de fond
            // ---------------------------------------------------------------
            modelBuilder.Entity<AppelClaude>(entity =>
            {
                entity.ToTable("AppelClaude");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Origine).HasMaxLength(40).IsRequired().HasColumnName("origine");
                entity.Property(e => e.Modele).HasMaxLength(50).HasColumnName("modele");
                entity.Property(e => e.TokensEntree).HasColumnName("tokens_entree");
                entity.Property(e => e.TokensSortie).HasColumnName("tokens_sortie");
                entity.Property(e => e.TokensCacheLecture).HasColumnName("tokens_cache_lecture");
                entity.Property(e => e.TokensCacheEcriture).HasColumnName("tokens_cache_ecriture");
                entity.Property(e => e.Reference).HasMaxLength(120).HasColumnName("reference");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                // Les deux seules lectures prévues : « combien sur la période »
                // et « lequel des postes ». L'index porte donc les deux, dans
                // cet ordre — on filtre toujours par date d'abord.
                entity.HasIndex(e => new { e.DateCreation, e.Origine });
            });

            modelBuilder.Entity<Recharge>(entity =>
            {
                entity.ToTable("Recharge");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AbonnementId).HasColumnName("abonnement_id");
                entity.Property(e => e.PeriodeDebut).HasColumnName("periode_debut");
                entity.Property(e => e.Minutes).HasColumnName("minutes");
                entity.Property(e => e.PrixCentimes).HasColumnName("prix_centimes");
                entity.Property(e => e.DateAchat).HasColumnName("date_achat");

                entity.Property(e => e.StripeSessionId)
                      .HasMaxLength(120)
                      .HasColumnName("stripe_session_id");

                // INDEX UNIQUE FILTRÉ. C'est la base, et non le code, qui
                // interdit le double crédit : deux webhooks rejoués en même
                // temps passeraient tous deux un test applicatif « existe
                // déjà ? » avant que l'un ait enregistré. La contrainte, elle,
                // ne se contourne pas.
                //
                // Filtré sur les non-nulles : les recharges offertes n'ont pas
                // de session, et un index unique ordinaire n'en tolérerait
                // qu'une seule dans toute la table.
                entity.HasIndex(e => e.StripeSessionId)
                      .IsUnique()
                      .HasFilter("[stripe_session_id] IS NOT NULL")
                      .HasDatabaseName("UX_Recharge_stripe_session");

                entity.Property(e => e.StripePaiementId)
                      .HasMaxLength(120)
                      .HasColumnName("stripe_paiement_id");

                entity.Property(e => e.DateRemboursement)
                      .HasColumnName("date_remboursement");

                entity.Property(e => e.Motif)
                      .HasMaxLength(200)
                      .HasColumnName("motif");

                // C'est par cette colonne qu'un remboursement retrouve les
                // heures à reprendre. Index simple, non unique : rien
                // n'interdit à Stripe de nous renvoyer deux fois le même
                // paiement, et c'est `date_remboursement` qui garantit qu'on
                // ne reprend qu'une fois.
                entity.HasIndex(e => e.StripePaiementId)
                      .HasFilter("[stripe_paiement_id] IS NOT NULL")
                      .HasDatabaseName("IX_Recharge_stripe_paiement");

                entity.HasOne(e => e.Abonnement)
                      .WithMany(a => a.Recharges)
                      .HasForeignKey(e => e.AbonnementId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.AbonnementId, e.PeriodeDebut });
            });
        }
    }
}
