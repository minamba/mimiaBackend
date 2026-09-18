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
        public virtual DbSet<Academie> Academies { get; set; }
        public virtual DbSet<PeriodeVacances> PeriodesVacances { get; set; }
        public virtual DbSet<Parent> Parents { get; set; }
        public virtual DbSet<Eleve> Eleves { get; set; }
        public virtual DbSet<Matiere> Matieres { get; set; }
        public virtual DbSet<Competence> Competences { get; set; }
        public virtual DbSet<CompetencePrerequis> CompetencesPrerequis { get; set; }
        public virtual DbSet<EcheanceReferentiel> EcheancesReferentiel { get; set; }

        /// <summary>Les examens nationaux par session, leurs épreuves, et la préparation de chaque élève.</summary>
        public virtual DbSet<Examen> Examens { get; set; }
        public virtual DbSet<EpreuveExamen> EpreuvesExamens { get; set; }
        public virtual DbSet<PreparationEpreuve> PreparationsEpreuves { get; set; }

        public virtual DbSet<MaitriseEleve> MaitrisesEleves { get; set; }
        public virtual DbSet<Conversation> Conversations { get; set; }
        public virtual DbSet<Message> Messages { get; set; }

        public virtual DbSet<PieceJointe> PiecesJointes { get; set; }
        public virtual DbSet<PlancheSchema> PlanchesSchemas { get; set; }

        public virtual DbSet<SessionEleve> SessionsEleves { get; set; }
        public virtual DbSet<Evaluation> Evaluations { get; set; }
        public virtual DbSet<RapportSeance> RapportsSeance { get; set; }
        public virtual DbSet<FicheRevision> FichesRevision { get; set; }
        public virtual DbSet<Dictee> Dictees { get; set; }
        public virtual DbSet<ComprehensionOrale> ComprehensionsOrales { get; set; }

        public virtual DbSet<ExpressionOrale> ExpressionsOrales { get; set; }
        public virtual DbSet<EvaluationPrevue> EvaluationsPrevues { get; set; }
        public virtual DbSet<ControleScolaire> ControlesScolaires { get; set; }
        public virtual DbSet<ControleNotion> ControlesNotions { get; set; }
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

        // Les modèles de courriel : diffusions réutilisables et courriels
        // automatiques, avec leurs images et documents.
        public virtual DbSet<ModeleMail> ModelesMail { get; set; }
        public virtual DbSet<PieceModeleMail> PiecesModelesMail { get; set; }
        public virtual DbSet<IdeeEvolution> IdeesEvolution { get; set; }
        public virtual DbSet<PieceIdee> PiecesIdees { get; set; }

        // Le journal des courriels automatiques, et qui ne veut plus quoi.
        public virtual DbSet<EnvoiAutomatique> EnvoisAutomatiques { get; set; }
        public virtual DbSet<DesabonnementMail> DesabonnementsMail { get; set; }

        public virtual DbSet<MailBanni> MailsBannis { get; set; }

        /// <summary>Les annees scolaires passees chez nous, en intervalles.</summary>
        public virtual DbSet<HistoriqueClasseEleve> HistoriquesClasse { get; set; }

        /// <summary>
        /// Une VUE, pas une table : la règle du forfait vit en SQL, où il
        /// n'en existe qu'un exemplaire. Voir `ForfaitAbonnement`.
        /// </summary>
        public virtual DbSet<ForfaitAbonnement> ForfaitsAbonnement { get; set; }

        public virtual DbSet<MesureVoix> MesuresVoix { get; set; }

        /// <summary>Les problèmes et suggestions déposés depuis le bouton « Signaler ».</summary>
        public virtual DbSet<Signalement> Signalements { get; set; }

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
            // Academie
            // ---------------------------------------------------------------
            modelBuilder.Entity<Academie>(entity =>
            {
                entity.ToTable("Academie");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(30).HasColumnName("code");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(100).HasColumnName("libelle");
                entity.Property(e => e.Zone).IsRequired().HasMaxLength(20).HasColumnName("zone");

                entity.HasIndex(e => e.Code).IsUnique();
            });

            // ---------------------------------------------------------------
            // PeriodeVacances
            // ---------------------------------------------------------------
            modelBuilder.Entity<PeriodeVacances>(entity =>
            {
                entity.ToTable("PeriodeVacances");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Zone).IsRequired().HasMaxLength(20).HasColumnName("zone");
                entity.Property(e => e.AnneeScolaire).IsRequired().HasMaxLength(10).HasColumnName("annee_scolaire");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(50).HasColumnName("libelle");
                entity.Property(e => e.DateDebut).HasColumnName("date_debut");
                entity.Property(e => e.DateFin).HasColumnName("date_fin");

                entity.HasIndex(e => new { e.Zone, e.AnneeScolaire });
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

                // Pas de valeur par défaut : la colonne est nullable, et NULL se lit
                // comme « aucune section ouverte ». Voir `OngletsAdmin`.
                entity.Property(e => e.OngletsAdmin)
                      .HasMaxLength(500)
                      .HasColumnName("onglets_admin");

                // Vrai par défaut : le droit d'ajouter un enfant est l'état normal,
                // on le RETIRE à un compte précis. Même précaution que ci-dessus —
                // la valeur par défaut doit figurer dans le modèle ET la migration.
                entity.Property(e => e.PeutAjouterEnfant)
                      .HasDefaultValue(true)
                      .HasColumnName("peut_ajouter_enfant");

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
                entity.Property(e => e.AcademieId).HasColumnName("academie_id");
                entity.Property(e => e.ProgressionVueLe).HasColumnName("progression_vue_le");
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
                entity.Property(e => e.Lv2Espagnol).HasColumnName("lv2_espagnol").HasDefaultValue(false);
                entity.Property(e => e.Specialites).HasMaxLength(200).HasColumnName("specialites");

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

                // Restrict, jamais Cascade : une académie est une donnée de
                // référence, elle ne doit jamais entraîner la suppression
                // d'un enfant.
                entity.HasOne(e => e.Academie)
                      .WithMany(a => a.Eleves)
                      .HasForeignKey(e => e.AcademieId)
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
                entity.Property(e => e.DateDebutValidite).HasColumnName("date_debut_validite");
                entity.Property(e => e.Actif).HasColumnName("actif").HasDefaultValue(true);
                entity.Property(e => e.DateFinValidite).HasColumnName("date_fin_validite");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");

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
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");
                // NoAction : un niveau scolaire est une donnee de reference,
                // il ne se supprime pas. La contrainte interdit un identifiant
                // fantaisiste, elle ne propage rien.
                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);

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
                entity.Property(e => e.DureeChoisieMinutes).HasColumnName("duree_choisie_minutes");
                entity.Property(e => e.ModeSeance).HasMaxLength(20).HasColumnName("mode_seance");
                entity.Property(e => e.ModeControleId).HasColumnName("mode_controle_id");
                entity.Property(e => e.ModeEpreuveCode).HasMaxLength(60).HasColumnName("mode_epreuve_code");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");
                // NoAction : un niveau scolaire est une donnee de reference,
                // il ne se supprime pas. La contrainte interdit un identifiant
                // fantaisiste, elle ne propage rien.
                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);

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

                // LA FILE D OBSERVATION SE LISAIT EN BALAYANT TOUTE LA TABLE.
                //
                // Le worker cherche les seances inactives dont le marqueur
                // d observation est en retard, les plus anciennes d abord.
                // Aucun index ne portait DateDernierMessage en tete : le plan
                // etait un balayage complet SUIVI D UN TRI, toutes les dix
                // minutes. A treize conversations c est gratuit ; a cinquante
                // mille, c est un tri de cinquante mille lignes pour en garder
                // vingt.
                //
                // Avec cet index, la lecture part de la plus ancienne et
                // s arrete des qu elle en a vingt.
                //
                // DateDerniereObservation est EMBARQUEE : le filtre compare
                // les deux dates, et sans elle chaque ligne PARCOURUE — pas
                // seulement chaque ligne retenue — rouvrirait la table.
                // Mesure sur treize conversations : 2 lectures en balayage,
                // 24 avec l index nu, parce que la lecture commence par les
                // plus anciennes, qui sont justement les deja observees.
                entity.HasIndex(e => e.DateDernierMessage)
                      .IncludeProperties(e => e.DateDerniereObservation);
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

                // `legende` par défaut : les lignes déjà en base SONT les légendées.
                entity.Property(e => e.Variante)
                    .HasMaxLength(20)
                    .IsRequired()
                    .HasDefaultValue("legende")
                    .HasColumnName("variante");

                entity.Property(e => e.Niveau).HasMaxLength(20).HasColumnName("niveau");
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

                // Une planche par clé ET PAR VARIANTE : réimporter remplace, ça
                // n'empile pas. Sans cette contrainte, deux versions
                // coexisteraient et celle qui s'afficherait dépendrait de
                // l'ordre de lecture.
                //
                // La variante est entrée dans l'index le 17/09/2026, pour que la
                // muette puisse tenir à côté de sa légendée sous la même clé.
                entity.HasIndex(e => new { e.Cle, e.Variante }).IsUnique();
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
                entity.Property(e => e.CorrectionReportee).HasColumnName("correction_reportee");
                entity.Property(e => e.RelancesCorrection).HasColumnName("relances_correction");

                // Pas de HasMaxLength : nvarchar(max). Un contrôle de six
                // questions avec les réponses de l'élève dépasse 4000 caractères.
                entity.Property(e => e.Detail).HasColumnName("detail");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Evaluations)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // NoAction, et pas seulement pour éviter une cascade de plus :
                // un niveau scolaire ne se supprime pas, il est de référence.
                // La contrainte sert à empêcher un identifiant fantaisiste, pas
                // à propager une suppression qui n'arrivera jamais.
                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);

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
                entity.Property(e => e.DureeChoisieMinutes).HasColumnName("duree_choisie_minutes");
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
            // Dictee
            // ---------------------------------------------------------------
            modelBuilder.Entity<Dictee>(entity =>
            {
                entity.ToTable("Dictee");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Titre).HasMaxLength(300).HasColumnName("titre");

                // nvarchar(max) : une dictée de plusieurs phrases, et sa copie,
                // dépassent largement 4000 caractères à elles deux.
                entity.Property(e => e.TexteDicte).IsRequired().HasColumnName("texte_dicte");
                entity.Property(e => e.Copie).IsRequired().HasColumnName("copie");

                entity.Property(e => e.Remarque).HasMaxLength(2000).HasColumnName("remarque");
                entity.Property(e => e.Etat).HasMaxLength(20).HasColumnName("etat");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateMiseAJour).HasColumnName("date_mise_a_jour");
                entity.Property(e => e.DateConsultation).HasColumnName("date_consultation");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Dictees)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // NoAction, comme sur Evaluation : un niveau scolaire est de
                // référence, la contrainte empêche un identifiant fantaisiste
                // sans jamais propager de suppression.
                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Restrict, pas Cascade : trois chemins de suppression
                // convergeraient sinon vers Dictee (élève, matière,
                // conversation), et SQL Server refuse les cascades multiples.
                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.Dictees)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                // NoAction : une conversation purgée au bout d'un an ne doit
                // pas emporter la dictée. Le parent garde sa trace même quand
                // le détail des échanges a disparu.
                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.Dictees)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.DateCreation });
                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.DateMiseAJour });
            });

            // ---------------------------------------------------------------
            // ComprehensionOrale
            // ---------------------------------------------------------------
            modelBuilder.Entity<ComprehensionOrale>(entity =>
            {
                entity.ToTable("ComprehensionOrale");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Titre).HasMaxLength(300).HasColumnName("titre");
                entity.Property(e => e.Langue).IsRequired().HasMaxLength(10).HasColumnName("langue");

                // nvarchar(max) : un passage de compréhension orale dépasse
                // vite 4000 caractères une fois la compréhension et la
                // remarque ajoutées.
                entity.Property(e => e.Passage).IsRequired().HasColumnName("passage");
                entity.Property(e => e.ReponseEleve).IsRequired().HasColumnName("reponse_eleve");
                entity.Property(e => e.Comprehension).IsRequired().HasColumnName("comprehension");

                entity.Property(e => e.Remarque).HasMaxLength(2000).HasColumnName("remarque");
                entity.Property(e => e.AudioDonnees).HasColumnName("audio_donnees");
                entity.Property(e => e.AudioChemin).HasMaxLength(200).HasColumnName("audio_chemin");
                entity.Property(e => e.AudioEffaceLe).HasColumnName("audio_efface_le");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateConsultation).HasColumnName("date_consultation");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.ComprehensionsOrales)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // NoAction, comme sur Dictee : un niveau scolaire est de
                // référence, la contrainte empêche un identifiant fantaisiste
                // sans jamais propager de suppression.
                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Restrict, pas Cascade : trois chemins de suppression
                // convergeraient sinon vers ComprehensionOrale (élève,
                // matière, conversation), et SQL Server refuse les cascades
                // multiples.
                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.ComprehensionsOrales)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                // NoAction : une conversation purgée au bout d'un an ne doit
                // pas emporter l'archive. Le parent garde sa trace même
                // quand le détail des échanges a disparu.
                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.ComprehensionsOrales)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.DateCreation });
            });

            // ---------------------------------------------------------------
            // ExpressionOrale
            // ---------------------------------------------------------------
            modelBuilder.Entity<ExpressionOrale>(entity =>
            {
                entity.ToTable("ExpressionOrale");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");

                // OBLIGATOIRE, contrairement au titre d'une compréhension orale :
                // celle-ci se reconnaît aux premiers mots de son passage, une
                // conversation non.
                entity.Property(e => e.Titre)
                      .IsRequired().HasMaxLength(300).HasColumnName("titre");

                entity.Property(e => e.Langue)
                      .IsRequired().HasMaxLength(10).HasColumnName("langue");

                // nvarchar(max) : une conversation de quinze tours dépasse
                // largement 4000 caractères une fois en JSON.
                entity.Property(e => e.Echange).IsRequired().HasColumnName("echange");

                entity.Property(e => e.Remarque).HasMaxLength(2000).HasColumnName("remarque");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateConsultation).HasColumnName("date_consultation");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");

                // SANS COLLECTION INVERSE, contrairement à la compréhension orale.
                // Trois entités auraient gagné une propriété de plus pour une
                // archive qu'on ne lit jamais depuis elles — on part toujours de
                // l'élève ET de la matière, jamais de la conversation.
                entity.HasOne(e => e.Eleve)
                      .WithMany()
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // NoAction : un niveau scolaire est de référence, la contrainte
                // empêche un identifiant fantaisiste sans propager de suppression.
                entity.HasOne<NiveauScolaire>()
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Restrict, pas Cascade : trois chemins de suppression
                // convergeraient sinon ici (élève, matière, conversation), et SQL
                // Server refuse les cascades multiples.
                entity.HasOne(e => e.Matiere)
                      .WithMany()
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                // NoAction : une conversation purgée au bout d'un an ne doit pas
                // emporter l'archive.
                entity.HasOne(e => e.Conversation)
                      .WithMany()
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.DateCreation });
            });

            // ---------------------------------------------------------------
            // EvaluationPrevue
            // ---------------------------------------------------------------
            modelBuilder.Entity<EvaluationPrevue>(entity =>
            {
                entity.ToTable("EvaluationPrevue");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Notion).HasMaxLength(300).HasColumnName("notion");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.ConsommeeLe).HasColumnName("consommee_le");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.EvaluationsPrevues)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict, même raison que sur Dictee : trois chemins de
                // suppression convergeraient sinon vers cette table.
                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.EvaluationsPrevues)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.EvaluationsPrevues)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.ConsommeeLe });
            });

            // ---------------------------------------------------------------
            // ControleScolaire
            // ---------------------------------------------------------------
            modelBuilder.Entity<ControleScolaire>(entity =>
            {
                entity.ToTable("ControleScolaire");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");
                entity.Property(e => e.PosePar).IsRequired().HasMaxLength(20).HasColumnName("pose_par");
                entity.Property(e => e.Sujet).HasMaxLength(300).HasColumnName("sujet");
                entity.Property(e => e.DateControle).HasColumnName("date_controle");
                entity.Property(e => e.HeureControle).HasColumnName("heure_controle");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");
                entity.Property(e => e.DernierePreparationLe).HasColumnName("derniere_preparation_le");
                entity.Property(e => e.NombrePreparations).HasColumnName("nombre_preparations");
                entity.Property(e => e.Note).HasColumnName("note");
                entity.Property(e => e.Ressenti).HasMaxLength(500).HasColumnName("ressenti");
                entity.Property(e => e.BilanLe).HasColumnName("bilan_le");
                entity.Property(e => e.RelancesBilan).HasColumnName("relances_bilan");
                entity.Property(e => e.CopieSeparee).HasColumnName("copie_separee");
                entity.Property(e => e.CopieDemandeeLe).HasColumnName("copie_demandee_le");
                entity.Property(e => e.EnoncePieceJointeId).HasColumnName("enonce_piece_jointe_id");
                entity.Property(e => e.CopiePieceJointeId).HasColumnName("copie_piece_jointe_id");
                entity.Property(e => e.CopieAnalyseeLe).HasColumnName("copie_analysee_le");
                entity.Property(e => e.PretVerdict).HasMaxLength(20).HasColumnName("pret_verdict");
                entity.Property(e => e.PretObservation).HasColumnName("pret_observation");
                entity.Property(e => e.PretLe).HasColumnName("pret_le");

                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.ControlesScolaires)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict, même raison que sur ComprehensionOrale : trois
                // chemins de suppression convergeraient sinon vers cette
                // table (élève, matière, conversation).
                entity.HasOne(e => e.Matiere)
                      .WithMany(m => m.ControlesScolaires)
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.Restrict);

                // NoAction, nullable : un contrôle posé depuis le calendrier
                // n'a pas de conversation, et une conversation purgée au
                // bout d'un an ne doit pas emporter le contrôle.
                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.ControlesScolaires)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Sert la requête « prochain contrôle non dépassé pour cet
                // élève et cette matière », appelée à chaque arrivée en
                // séance.
                entity.HasIndex(e => new { e.EleveId, e.MatiereId, e.DateControle });
            });

            // ---------------------------------------------------------------
            // ControleNotion — le périmètre d'un contrôle
            // ---------------------------------------------------------------
            modelBuilder.Entity<ControleNotion>(entity =>
            {
                entity.ToTable("ControleNotion");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ControleId).HasColumnName("controle_id");
                entity.Property(e => e.CompetenceId).HasColumnName("competence_id");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200).HasColumnName("libelle");
                entity.Property(e => e.TravailleeLe).HasColumnName("travaillee_le");
                entity.Property(e => e.Source).IsRequired().HasMaxLength(20).HasColumnName("source");
                entity.Property(e => e.Resultat).HasMaxLength(20).HasColumnName("resultat");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                // Cascade, et c'est le seul chemin qui arrive ici : le
                // périmètre n'a aucune vie hors de son contrôle.
                entity.HasOne(e => e.Controle)
                      .WithMany(c => c.Notions)
                      .HasForeignKey(e => e.ControleId)
                      .OnDelete(DeleteBehavior.Cascade);

                // NoAction : une compétence du référentiel n'est jamais
                // supprimée, la contrainte interdit seulement un identifiant
                // fantaisiste.
                entity.HasOne(e => e.Competence)
                      .WithMany()
                      .HasForeignKey(e => e.CompetenceId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.ControleId);

                // Une notion ne compte qu'une fois dans le pourcentage, même
                // si le professeur la redéclare d'une séance à l'autre.
                // Filtré : les lignes à libellé libre se dédoublonnent en C#.
                entity.HasIndex(e => new { e.ControleId, e.CompetenceId })
                      .IsUnique()
                      .HasFilter("[competence_id] IS NOT NULL");
            });

            // ---------------------------------------------------------------
            // Signalement — bouton « Signaler »
            // ---------------------------------------------------------------
            modelBuilder.Entity<Signalement>(entity =>
            {
                entity.ToTable("Signalement");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.Categorie).HasMaxLength(30).HasColumnName("categorie");
                entity.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
                entity.Property(e => e.Etat).HasMaxLength(20).IsRequired()
                      .HasDefaultValue("nouveau").HasColumnName("etat");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateMiseAJour).HasColumnName("date_mise_a_jour");

                entity.HasOne(e => e.Parent)
                      .WithMany(p => p.Signalements)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict, et non Cascade : Parent -> Eleve cascade déjà vers
                // cette table via ParentId. Un second chemin en cascade
                // (Parent -> Eleve -> Signalement) ferait deux chemins de
                // suppression convergents, que SQL Server refuse.
                entity.HasOne(e => e.Eleve)
                      .WithMany(el => el.Signalements)
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Etat);
                entity.HasIndex(e => e.ParentId);
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

                entity.Property(e => e.EleveId).HasColumnName("eleve_id");

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

                // Restrict, et non Cascade : Parent -> Eleve cascade déjà vers
                // cette table via ParentId. Un second chemin en cascade
                // (Parent -> Eleve -> AvisClient) ferait deux chemins de
                // suppression convergents, que SQL Server refuse — même
                // recette que Signalement.EleveId.
                entity.HasOne(e => e.Eleve)
                      .WithMany()
                      .HasForeignKey(e => e.EleveId)
                      .HasConstraintName("FK_AvisClient_Eleve")
                      .OnDelete(DeleteBehavior.Restrict);
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

            // ---------------------------------------------------------------
            // HistoriqueClasseEleve
            // ---------------------------------------------------------------
            modelBuilder.Entity<HistoriqueClasseEleve>(entity =>
            {
                entity.ToTable("HistoriqueClasseEleve");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.NiveauScolaireId).HasColumnName("niveau_scolaire_id");
                entity.Property(e => e.Debut).HasColumnName("debut");
                entity.Property(e => e.Fin).HasColumnName("fin");

                // L'ELEVE ET SA DATE : c'est la seule question posee a cette
                // table — « dans quelle classe etait-il le jour ou ce message a
                // ete ecrit ? ». Elle est posee pour chaque bloc de la fiche.
                entity.HasIndex(e => new { e.EleveId, e.Debut });

                entity.HasOne(e => e.Eleve)
                      .WithMany()
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.NiveauScolaire)
                      .WithMany()
                      .HasForeignKey(e => e.NiveauScolaireId)
                      .OnDelete(DeleteBehavior.NoAction);
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

            // ---------------------------------------------------------------
            // Modèles de courriel — diffusions enregistrées et automatiques
            // ---------------------------------------------------------------
            modelBuilder.Entity<ModeleMail>(entity =>
            {
                entity.ToTable("ModeleMail");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nature).IsRequired().HasMaxLength(20).HasColumnName("nature");
                entity.Property(e => e.Code).HasMaxLength(40).HasColumnName("code");
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(120).HasColumnName("nom");
                entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
                entity.Property(e => e.Sujet).IsRequired().HasMaxLength(150).HasColumnName("sujet");
                entity.Property(e => e.Titre).IsRequired().HasMaxLength(150).HasColumnName("titre");
                entity.Property(e => e.Texte).IsRequired().HasColumnName("texte");

                entity.Property(e => e.Frequence).IsRequired().HasMaxLength(10).HasColumnName("frequence");
                entity.Property(e => e.HeureEnvoi).HasColumnName("heure_envoi");
                entity.Property(e => e.JourSemaine).HasColumnName("jour_semaine");
                entity.Property(e => e.JourMois).HasColumnName("jour_mois");
                entity.Property(e => e.Actif).HasDefaultValue(false).HasColumnName("actif");
                entity.Property(e => e.DerniereOccurrence).HasColumnName("derniere_occurrence");
                entity.Property(e => e.DernierEnvoiLe).HasColumnName("dernier_envoi_le");
                entity.Property(e => e.DernierResultat).HasMaxLength(400).HasColumnName("dernier_resultat");

                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");

                // Unique mais filtré : les courriels automatiques ont un code
                // qui ne se répète pas, les diffusions n'en ont aucun.
                entity.HasIndex(e => e.Code).IsUnique().HasFilter("[code] IS NOT NULL");

                entity.HasIndex(e => e.Nature);
            });

            modelBuilder.Entity<PieceModeleMail>(entity =>
            {
                entity.ToTable("PieceModeleMail");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ModeleMailId).HasColumnName("modele_mail_id");
                entity.Property(e => e.Genre).IsRequired().HasMaxLength(10).HasColumnName("genre");
                entity.Property(e => e.Rang).HasColumnName("rang");
                entity.Property(e => e.NomFichier).IsRequired().HasMaxLength(255).HasColumnName("nom_fichier");
                entity.Property(e => e.TypeMime).IsRequired().HasMaxLength(100).HasColumnName("type_mime");
                entity.Property(e => e.Taille).HasColumnName("taille");
                entity.Property(e => e.Donnees).IsRequired().HasColumnName("donnees");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                // Cascade : une image n'a aucune vie hors de son modèle.
                entity.HasOne(e => e.ModeleMail)
                      .WithMany(m => m.Pieces)
                      .HasForeignKey(e => e.ModeleMailId)
                      .OnDelete(DeleteBehavior.Cascade);

                // L'ordre de lecture : les pièces d'un modèle, par genre puis
                // rang. Commence par la clé étrangère, qui s'en sert aussi.
                entity.HasIndex(e => new { e.ModeleMailId, e.Genre, e.Rang });
            });

            // ---------------------------------------------------------------
            // Les idées d'évolution — le carnet de Camara
            // ---------------------------------------------------------------
            modelBuilder.Entity<IdeeEvolution>(entity =>
            {
                entity.ToTable("IdeeEvolution");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Titre).IsRequired().HasMaxLength(150).HasColumnName("titre");
                entity.Property(e => e.Urgence).IsRequired().HasMaxLength(10).HasColumnName("urgence");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Statut).IsRequired().HasMaxLength(30).HasColumnName("statut");
                entity.Property(e => e.AuteurPrenom).HasMaxLength(80).HasColumnName("auteur_prenom");
                entity.Property(e => e.AuteurNom).HasMaxLength(80).HasColumnName("auteur_nom");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateModification).HasColumnName("date_modification");

                // La liste se lit par statut : c'est le filtre du tableau.
                entity.HasIndex(e => e.Statut);
            });

            modelBuilder.Entity<PieceIdee>(entity =>
            {
                entity.ToTable("PieceIdee");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdeeEvolutionId).HasColumnName("idee_evolution_id");
                entity.Property(e => e.Rang).HasColumnName("rang");
                entity.Property(e => e.NomFichier).IsRequired().HasMaxLength(255).HasColumnName("nom_fichier");
                entity.Property(e => e.TypeMime).IsRequired().HasMaxLength(100).HasColumnName("type_mime");
                entity.Property(e => e.Taille).HasColumnName("taille");
                entity.Property(e => e.Donnees).IsRequired().HasColumnName("donnees");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                // Cascade : une image n'a aucune vie hors de son idée.
                entity.HasOne(e => e.IdeeEvolution)
                      .WithMany(i => i.Pieces)
                      .HasForeignKey(e => e.IdeeEvolutionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.IdeeEvolutionId, e.Rang });
            });

            // ---------------------------------------------------------------
            // Courriels automatiques — journal des envois, désabonnements
            // ---------------------------------------------------------------
            modelBuilder.Entity<EnvoiAutomatique>(entity =>
            {
                entity.ToTable("EnvoiAutomatique");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CodeModele).IsRequired().HasMaxLength(40).HasColumnName("code_modele");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.Cle).HasMaxLength(80).HasColumnName("cle");
                entity.Property(e => e.Occurrence).HasColumnName("occurrence");
                entity.Property(e => e.DateEnvoi).HasColumnName("date_envoi");
                entity.Property(e => e.Statut).IsRequired().HasMaxLength(10).HasColumnName("statut");

                // Cascade : le journal d'un compte supprimé ne lui survit pas.
                entity.HasOne<Parent>()
                      .WithMany()
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // LA GARDE CONTRE LE DOUBLON : une seule fin d'essai par
                // abonnement, une seule demande d'avis d'essai par parent.
                entity.HasIndex(e => new { e.CodeModele, e.Cle })
                      .IsUnique()
                      .HasFilter("[cle] IS NOT NULL");

                // La fenêtre de trois mois de la demande d'avis générale.
                entity.HasIndex(e => new { e.ParentId, e.CodeModele, e.DateEnvoi });
            });

            modelBuilder.Entity<DesabonnementMail>(entity =>
            {
                entity.ToTable("DesabonnementMail");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.Categorie).IsRequired().HasMaxLength(20).HasColumnName("categorie");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.HasOne<Parent>()
                      .WithMany()
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ParentId, e.Categorie }).IsUnique();
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

            // ---------------------------------------------------------------
            // EcheanceReferentiel
            // ---------------------------------------------------------------
            modelBuilder.Entity<EcheanceReferentiel>(entity =>
            {
                entity.ToTable("EcheanceReferentiel");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MatiereLibelle).IsRequired().HasMaxLength(200).HasColumnName("matiere_libelle");
                entity.Property(e => e.NiveauxConcernes).IsRequired().HasMaxLength(200).HasColumnName("niveaux_concernes");
                entity.Property(e => e.DateEcheance).HasColumnName("date_echeance");
                entity.Property(e => e.DateConnue).HasColumnName("date_connue");
                entity.Property(e => e.TexteOfficiel).HasMaxLength(300).HasColumnName("texte_officiel");
                entity.Property(e => e.Url).HasMaxLength(500).HasColumnName("url");
                entity.Property(e => e.DernierHashPage).HasMaxLength(100).HasColumnName("dernier_hash_page");
                entity.Property(e => e.DernierStatutVeille).HasMaxLength(20).HasColumnName("dernier_statut_veille");
                entity.Property(e => e.Sentinelle).HasColumnName("sentinelle").HasDefaultValue(false);
                entity.Property(e => e.MatieresCodes).HasMaxLength(200).HasColumnName("matieres_codes");
                entity.Property(e => e.NiveauxCodes).HasMaxLength(300).HasColumnName("niveaux_codes");
                entity.Property(e => e.DernierePageVerifieeLe).HasColumnName("derniere_page_verifiee_le");
                entity.Property(e => e.Notes).HasMaxLength(1000).HasColumnName("notes");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DerniereAlerteLe).HasColumnName("derniere_alerte_le");
                entity.Property(e => e.TraiteeLe).HasColumnName("traitee_le");

                // Sert le balayage du worker : « en attente, la plus proche
                // d'abord ». Voir la migration EcheancesReferentiel.
                entity.HasIndex(e => new { e.TraiteeLe, e.DateEcheance })
                      .HasDatabaseName("IX_EcheanceReferentiel_traitee_le_date_echeance");
            });

            // ---------------------------------------------------------------
            // Examen, EpreuveExamen, PreparationEpreuve — préparation aux examens
            // ---------------------------------------------------------------
            modelBuilder.Entity<Examen>(entity =>
            {
                entity.ToTable("Examen");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(40).HasColumnName("code");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(100).HasColumnName("libelle");
                entity.Property(e => e.TitreSection).IsRequired().HasMaxLength(120).HasColumnName("titre_section");
                entity.Property(e => e.Session).HasColumnName("session");
                entity.Property(e => e.NiveauxCodes).IsRequired().HasMaxLength(200).HasColumnName("niveaux_codes");
                entity.Property(e => e.Source).HasMaxLength(1000).HasColumnName("source");
                entity.Property(e => e.Actif).HasColumnName("actif").HasDefaultValue(true);
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<EpreuveExamen>(entity =>
            {
                entity.ToTable("EpreuveExamen");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ExamenId).HasColumnName("examen_id");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(60).HasColumnName("code");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120).HasColumnName("libelle");
                entity.Property(e => e.MatieresCodes).IsRequired().HasMaxLength(200).HasColumnName("matieres_codes");
                entity.Property(e => e.NiveauxProgrammeCodes).IsRequired().HasMaxLength(200).HasColumnName("niveaux_programme_codes");
                entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
                entity.Property(e => e.Remarque).HasMaxLength(500).HasColumnName("remarque");
                entity.Property(e => e.Ordre).HasColumnName("ordre");
                entity.Property(e => e.SpecialiteRequise).HasMaxLength(40).HasColumnName("specialite_requise");
                entity.Property(e => e.SpecialiteExclue).HasMaxLength(40).HasColumnName("specialite_exclue");
                entity.Property(e => e.DomainesInclus).HasMaxLength(200).HasColumnName("domaines_inclus");
                entity.Property(e => e.DomainesExclus).HasMaxLength(200).HasColumnName("domaines_exclus");

                entity.HasOne(e => e.Examen)
                      .WithMany(x => x.Epreuves)
                      .HasForeignKey(e => e.ExamenId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.ExamenId);
            });

            modelBuilder.Entity<PreparationEpreuve>(entity =>
            {
                entity.ToTable("PreparationEpreuve");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EleveId).HasColumnName("eleve_id");
                entity.Property(e => e.EpreuveId).HasColumnName("epreuve_id");
                entity.Property(e => e.MatiereId).HasColumnName("matiere_id");
                entity.Property(e => e.NombrePreparations).HasColumnName("nombre_preparations").HasDefaultValue(0);
                entity.Property(e => e.DernierePreparationLe).HasColumnName("derniere_preparation_le");
                entity.Property(e => e.PretVerdict).HasMaxLength(20).HasColumnName("pret_verdict");
                entity.Property(e => e.PretObservation).HasColumnName("pret_observation");
                entity.Property(e => e.PretLe).HasColumnName("pret_le");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");

                entity.HasOne(e => e.Eleve)
                      .WithMany()
                      .HasForeignKey(e => e.EleveId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Epreuve)
                      .WithMany()
                      .HasForeignKey(e => e.EpreuveId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Matiere)
                      .WithMany()
                      .HasForeignKey(e => e.MatiereId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.EleveId, e.EpreuveId, e.MatiereId }).IsUnique();
                entity.HasIndex(e => e.EpreuveId);
                entity.HasIndex(e => e.MatiereId);
            });
        }
    }
}
