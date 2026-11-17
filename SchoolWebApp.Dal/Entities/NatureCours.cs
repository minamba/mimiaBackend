using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une ligne du module des cours importés qui appartient aux FICHES ou aux PODCASTS — 15/11/2026, voir
    /// `NaturesCours`. Sur les six tables du module : cours, épisodes et fiches, décomptes, crédits, achats, packs.
    /// </summary>
    public interface IAvecNatureCours
    {
        string Nature { get; set; }
    }

    public partial class CoursImporte : IAvecNatureCours
    {
        public string Nature { get; set; } = null!;
    }

    public partial class FicheDeCours : IAvecNatureCours
    {
        public string Nature { get; set; } = null!;
    }

    public partial class FicheConsommee : IAvecNatureCours
    {
        public string Nature { get; set; } = null!;
    }

    public partial class CreditFichesFamille : IAvecNatureCours
    {
        public string Nature { get; set; } = null!;
    }

    public partial class AchatFiches : IAvecNatureCours
    {
        public string Nature { get; set; } = null!;
    }

    public partial class OffreFiches : IAvecNatureCours
    {
        public string Nature { get; set; } = null!;
    }

    /// <summary>
    /// LES FICHES ET LES PODCASTS DANS LES MÊMES TABLES, SANS JAMAIS SE VOIR — 15/11/2026.
    ///
    /// Les podcasts reprennent tout le chemin des fiches (dépôt, quota, crédits, tâche de fond). Plutôt que de dupliquer
    /// huit cents lignes de requêtes, chaque table du module porte sa nature, et un FILTRE DE LA BASE ne laisse passer que
    /// celle de la portée : `NatureCours`, « fiches » par défaut. Le code des fiches, inchangé, ne voit donc jamais un
    /// podcast ; le contrôleur et la tâche de fond des podcasts posent « podcasts » en entrant, et le même code ne voit
    /// plus que des podcasts. Une ligne AJOUTÉE prend la nature de la portée si elle n'en a pas (voir `SaveChanges`).
    ///
    /// PIÈGE : une requête qui doit tout voir (suppression d'un élève ou d'un compte, chiffres de l'administration) le
    /// dit avec `IgnoreQueryFilters()`.
    /// </summary>
    public partial class SchoolWebAppDatabaseContext
    {
        /// <summary>La nature que voit cette portée : `NaturesCours.Fiches` ou `NaturesCours.Podcasts`.</summary>
        public string NatureCours { get; set; } = NaturesCours.Fiches;

        private static void ConfigurerNatureCours(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CoursImporte>(e => NatureDe(e.Property(x => x.Nature)));
            modelBuilder.Entity<FicheDeCours>(e => NatureDe(e.Property(x => x.Nature)));
            modelBuilder.Entity<FicheConsommee>(e => NatureDe(e.Property(x => x.Nature)));
            modelBuilder.Entity<CreditFichesFamille>(e =>
            {
                // DANS LA CLÉ : sans `ValueGeneratedNever`, EF fabrique un identifiant aléatoire pour une clé texte à
                // valeur par défaut (vu au banc du 15/11/2026 : « 62b5f2ec-… » tronqué). La nature vient de la portée.
                NatureDe(e.Property(x => x.Nature));
                e.Property(x => x.Nature).ValueGeneratedNever();
            });
            modelBuilder.Entity<AchatFiches>(e => NatureDe(e.Property(x => x.Nature)));
            modelBuilder.Entity<OffreFiches>(e => NatureDe(e.Property(x => x.Nature)));
        }

        private static void NatureDe(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<string> propriete) =>
            propriete.IsRequired().HasMaxLength(16).HasDefaultValue(NaturesCours.Fiches).HasColumnName("nature");

        /// <summary>
        /// Le filtre lit la propriété du contexte qui EXÉCUTE la requête, pas celle du contexte qui a construit le
        /// modèle : EF la paramètre à chaque requête.
        /// </summary>
        private void FiltrerNatureCours(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CoursImporte>().HasQueryFilter(c => c.Nature == NatureCours);
            modelBuilder.Entity<FicheDeCours>().HasQueryFilter(f => f.Nature == NatureCours);
            modelBuilder.Entity<FicheConsommee>().HasQueryFilter(f => f.Nature == NatureCours);
            modelBuilder.Entity<CreditFichesFamille>().HasQueryFilter(c => c.Nature == NatureCours);
            modelBuilder.Entity<AchatFiches>().HasQueryFilter(a => a.Nature == NatureCours);
            modelBuilder.Entity<OffreFiches>().HasQueryFilter(o => o.Nature == NatureCours);
        }

        /// <summary>Une ligne ajoutée sans nature prend celle de la portée.</summary>
        private void PoserNatureCours()
        {
            foreach (var entree in ChangeTracker.Entries<IAvecNatureCours>())
            {
                if (entree.State == EntityState.Added && string.IsNullOrEmpty(entree.Entity.Nature))
                {
                    entree.Entity.Nature = NatureCours;
                }
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            PoserNatureCours();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            PoserNatureCours();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
