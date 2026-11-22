using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using DomainJeu = SchoolWebApp.Domain.Models.JeuDeTest;
using DomainLigne = SchoolWebApp.Domain.Models.LigneDeTest;
using DomainTest = SchoolWebApp.Domain.Models.TestDuJeu;
using EntiteJeu = SchoolWebApp.Dal.Entities.JeuDeTest;
using EntiteLigne = SchoolWebApp.Dal.Entities.LigneDeTest;
using EntiteTest = SchoolWebApp.Dal.Entities.TestDuJeu;

namespace SchoolWebApp.Dal.Repositories
{
    public class JeuxDeTestsRepository : IJeuxDeTestsRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public JeuxDeTestsRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IReadOnlyList<DomainJeu>> ListerAsync(CancellationToken ct = default)
        {
            var lus = await _context.JeuxDeTests
                .AsNoTracking()
                .OrderByDescending(j => j.DateCreation)
                .ThenByDescending(j => j.Id)
                .Select(j => new
                {
                    j.Id,
                    j.Titre,
                    j.Section,
                    j.NiveauCode,
                    j.Classe,
                    j.Auteur,
                    j.DateCreation,
                    Titres = j.Tests.OrderByDescending(t => t.Id).Select(t => t.Titre).ToList(),
                    Statuts = j.Tests.SelectMany(t => t.Lignes).Select(l => l.Statut).ToList(),
                })
                .ToListAsync(ct);

            return lus.Select(j => new DomainJeu
            {
                Id = j.Id,
                Titre = j.Titre,
                Section = j.Section,
                NiveauCode = j.NiveauCode,
                Classe = j.Classe,
                Auteur = j.Auteur,
                DateCreation = j.DateCreation,
                Titres = j.Titres,
                NombreLignes = j.Statuts.Count,
                NombreOk = j.Statuts.Count(s => s == StatutsDeTest.Ok),
                Statut = StatutsDeTest.DuJeu(j.Statuts),
            }).ToList();
        }

        public async Task<DomainJeu?> GetAsync(int id, CancellationToken ct = default)
        {
            var jeu = await _context.JeuxDeTests
                .AsNoTracking()
                .AsSplitQuery()
                .Include(j => j.Tests)
                .ThenInclude(t => t.Lignes)
                .FirstOrDefaultAsync(j => j.Id == id, ct);

            if (jeu is null) return null;

            // LE TEST LE PLUS RÉCENT D'ABORD — Camara, le 04/11/2026. Ses lignes : voir DansLOrdre.
            var tests = jeu.Tests
                .OrderByDescending(t => t.Id)
                .Select(t => new DomainTest
                {
                    Id = t.Id,
                    JeuId = t.JeuId,
                    Titre = t.Titre,
                    MatiereCode = t.MatiereCode,
                    Matiere = t.Matiere,
                    Lignes = DansLOrdre(t.Lignes).Select(VersDomaine).ToList(),
                })
                .ToList();

            var statuts = tests.SelectMany(t => t.Lignes).Select(l => l.Statut).ToList();

            return new DomainJeu
            {
                Id = jeu.Id,
                Titre = jeu.Titre,
                Section = jeu.Section,
                NiveauCode = jeu.NiveauCode,
                Classe = jeu.Classe,
                Auteur = jeu.Auteur,
                DateCreation = jeu.DateCreation,
                Titres = tests.Select(t => t.Titre).ToList(),
                NombreLignes = statuts.Count,
                NombreOk = statuts.Count(s => s == StatutsDeTest.Ok),
                Statut = StatutsDeTest.DuJeu(statuts),
                Tests = tests,
            };
        }

        /// <summary>
        /// L'ORDRE DES LIGNES D'UN TEST — Camara, le 08/11/2026 : la plus récente en haut, et une ligne déplacée garde sa place.
        ///
        /// Les lignes jamais déplacées d'abord, de la plus récente à la plus ancienne : une ligne neuve arrive donc toujours
        /// en haut, même après un déplacement. Créées d'un coup (même date), elles gardent l'ordre où on les a écrites :
        /// « compétence 1 » reste avant « compétence 2 ». Puis les lignes placées, dans leur ordre.
        /// </summary>
        private static IEnumerable<EntiteLigne> DansLOrdre(IEnumerable<EntiteLigne> lignes) => lignes
            .OrderBy(l => l.Position is null ? 0 : 1)
            .ThenBy(l => l.Position)
            .ThenByDescending(l => l.DateCreation)
            .ThenBy(l => l.Id);

        public async Task<DomainJeu> CreerAsync(
            string titre, string section, string niveauCode, string classe, string auteur, CancellationToken ct = default)
        {
            var jeu = new EntiteJeu
            {
                Titre = titre,
                Section = section,
                NiveauCode = niveauCode,
                Classe = classe,
                Auteur = auteur,
                DateCreation = DateTime.UtcNow,
            };

            _context.JeuxDeTests.Add(jeu);
            await _context.SaveChangesAsync(ct);

            return (await GetAsync(jeu.Id, ct))!;
        }

        public async Task<DomainJeu?> ModifierAsync(
            int id, string titre, string section, string niveauCode, string classe, CancellationToken ct = default)
        {
            var jeu = await _context.JeuxDeTests.FirstOrDefaultAsync(j => j.Id == id, ct);
            if (jeu is null) return null;

            jeu.Titre = titre;
            jeu.Section = section;
            jeu.NiveauCode = niveauCode;
            jeu.Classe = classe;

            await _context.SaveChangesAsync(ct);
            return await GetAsync(id, ct);
        }

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            var jeu = await _context.JeuxDeTests.FirstOrDefaultAsync(j => j.Id == id, ct);
            if (jeu is null) return false;

            // Ses tests et leurs lignes partent avec lui : la cascade est en base.
            _context.JeuxDeTests.Remove(jeu);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<string>> CategoriesAsync(CancellationToken ct = default)
        {
            var lues = await _context.TestsDesJeux
                .AsNoTracking()
                .Where(t => t.MatiereCode == StatutsDeTest.CodeCategorie)
                .Select(t => t.Matiere)
                .Distinct()
                .ToListAsync(ct);

            // Le tri et le dédoublonnage à la casse près se font ici : la collation de la base n'est pas celle du français.
            return lues
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .GroupBy(c => c.Trim(), StringComparer.CurrentCultureIgnoreCase)
                .Select(g => g.Key)
                .OrderBy(c => c, StringComparer.Create(new System.Globalization.CultureInfo("fr-FR"), ignoreCase: true))
                .ToList();
        }

        public async Task<DomainJeu?> AjouterTestAsync(
            int jeuId, string titre, string matiereCode, string matiere, IReadOnlyList<string> competences,
            CancellationToken ct = default)
        {
            if (!await _context.JeuxDeTests.AnyAsync(j => j.Id == jeuId, ct)) return null;

            var maintenant = DateTime.UtcNow;

            _context.TestsDesJeux.Add(new EntiteTest
            {
                JeuId = jeuId,
                Titre = titre,
                MatiereCode = matiereCode,
                Matiere = matiere,
                Lignes = competences
                    .Select(c => new EntiteLigne { Competence = c, Statut = StatutsDeTest.ATester, DateCreation = maintenant })
                    .ToList(),
            });

            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> ModifierTestAsync(
            int jeuId, int testId, string titre, string matiereCode, string matiere, CancellationToken ct = default)
        {
            var test = await _context.TestsDesJeux.FirstOrDefaultAsync(t => t.Id == testId && t.JeuId == jeuId, ct);
            if (test is null) return null;

            test.Titre = titre;
            test.MatiereCode = matiereCode;
            test.Matiere = matiere;

            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> SupprimerTestAsync(int jeuId, int testId, CancellationToken ct = default)
        {
            var test = await _context.TestsDesJeux.FirstOrDefaultAsync(t => t.Id == testId && t.JeuId == jeuId, ct);
            if (test is null) return null;

            _context.TestsDesJeux.Remove(test);
            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> AjouterLigneAsync(int jeuId, int testId, string competence, CancellationToken ct = default)
        {
            if (!await _context.TestsDesJeux.AnyAsync(t => t.Id == testId && t.JeuId == jeuId, ct)) return null;

            _context.LignesDeTest.Add(new EntiteLigne
            {
                TestId = testId,
                Competence = competence,
                Statut = StatutsDeTest.ATester,
                DateCreation = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> ModifierLigneAsync(
            int jeuId, int ligneId, string competence, string statut, string? commentaire,
            DateTime maintenant, CancellationToken ct = default)
        {
            var ligne = await LigneDuJeuAsync(jeuId, ligneId, ct);
            if (ligne is null) return null;

            // TOUTE MODIFICATION DATE LA LIGNE — Camara, le 08/11/2026 : plus de date au calendrier, le jour et l'heure se
            // posent seuls dès qu'un élément change. Réenregistrer sans rien changer ne touche pas à la date.
            if (ligne.Competence != competence || ligne.Statut != statut || ligne.Commentaire != commentaire)
            {
                ligne.DateEffectuee = maintenant;
            }

            ligne.Competence = competence;
            ligne.Statut = statut;
            ligne.Commentaire = commentaire;

            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> ChangerStatutAsync(
            int jeuId, int ligneId, string statut, DateTime maintenant, CancellationToken ct = default)
        {
            var ligne = await LigneDuJeuAsync(jeuId, ligneId, ct);
            if (ligne is null) return null;

            // LE TESTEUR CHANGE LE STATUT : « Effectué le » prend le jour ET l'heure (Camara, 06/11/2026), quel que soit le
            // nouveau statut, « À tester » compris (08/11/2026 : « à chaque fois qu'un élément est modifié »).
            if (ligne.Statut != statut) ligne.DateEffectuee = maintenant;
            ligne.Statut = statut;

            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> DeplacerLigneAsync(int jeuId, int ligneId, int position, CancellationToken ct = default)
        {
            var ligne = await LigneDuJeuAsync(jeuId, ligneId, ct);
            if (ligne is null) return null;

            // TOUT LE TEST EST NUMÉROTÉ, PAS LA SEULE LIGNE DÉPLACÉE : sinon une ligne encore sans place repasserait devant
            // elle à la prochaine lecture. La date « Effectué le » ne bouge pas : déplacer n'est pas modifier le test.
            var lignes = DansLOrdre(await _context.LignesDeTest.Where(l => l.TestId == ligne.TestId).ToListAsync(ct)).ToList();
            lignes.Remove(ligne);
            lignes.Insert(Math.Clamp(position, 0, lignes.Count), ligne);
            for (var i = 0; i < lignes.Count; i++) lignes[i].Position = i;

            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> SupprimerLigneAsync(int jeuId, int ligneId, CancellationToken ct = default)
        {
            var ligne = await LigneDuJeuAsync(jeuId, ligneId, ct);
            if (ligne is null) return null;

            _context.LignesDeTest.Remove(ligne);
            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        /// <summary>Une ligne n'est atteinte que par SON jeu : l'adresse dit les deux, on vérifie les deux.</summary>
        private Task<EntiteLigne?> LigneDuJeuAsync(int jeuId, int ligneId, CancellationToken ct) =>
            _context.LignesDeTest.FirstOrDefaultAsync(l => l.Id == ligneId && l.Test!.JeuId == jeuId, ct);

        private static DomainLigne VersDomaine(EntiteLigne l) => new()
        {
            Id = l.Id,
            TestId = l.TestId,
            Competence = l.Competence,
            Statut = l.Statut,
            DateCreation = l.DateCreation,
            DateEffectuee = l.DateEffectuee,
            Commentaire = l.Commentaire,
        };

        public async Task<int?> PoserLigneDeSignalementAsync(
            int jeuId, int testId, string competence, string commentaire, CancellationToken ct = default)
        {
            if (!await _context.TestsDesJeux.AnyAsync(t => t.Id == testId && t.JeuId == jeuId, ct)) return null;

            var ligne = await _context.LignesDeTest
                .Where(l => l.TestId == testId && l.Competence == "" && l.Commentaire == null && l.Statut == StatutsDeTest.ATester)
                .OrderBy(l => l.Id)
                .FirstOrDefaultAsync(ct);

            if (ligne is null)
            {
                ligne = new EntiteLigne { TestId = testId };
                _context.LignesDeTest.Add(ligne);
            }

            ligne.Competence = competence;
            ligne.Commentaire = commentaire;
            ligne.Statut = StatutsDeTest.Ko;
            ligne.DateCreation = DateTime.UtcNow;
            ligne.DateEffectuee = null;
            ligne.Position = null; // C'est une ligne nouvelle : elle arrive en haut.

            await _context.SaveChangesAsync(ct);
            return ligne.Id;
        }

        public async Task<IReadOnlyList<LigneDeSignalement>> LignesDeSignalementAsync(
            IEnumerable<int> ids, CancellationToken ct = default)
        {
            var liste = ids.Distinct().ToList();
            if (liste.Count == 0) return [];

            return await _context.LignesDeTest
                .AsNoTracking()
                .Where(l => liste.Contains(l.Id))
                .Select(l => new LigneDeSignalement
                {
                    LigneId = l.Id,
                    JeuId = l.Test!.JeuId,
                    JeuTitre = l.Test.Jeu!.Titre,
                    Section = l.Test.Jeu.Section,
                    Classe = l.Test.Jeu.Classe,
                    TestId = l.TestId,
                    TestTitre = l.Test.Titre,
                    Matiere = l.Test.Matiere,
                    Competence = l.Competence,
                    Commentaire = l.Commentaire,
                    Statut = l.Statut,
                })
                .ToListAsync(ct);
        }

        public async Task<bool> MettreAJourLigneDeSignalementAsync(
            int ligneId, string competence, string commentaire, CancellationToken ct = default)
        {
            var ligne = await _context.LignesDeTest.FirstOrDefaultAsync(l => l.Id == ligneId, ct);
            if (ligne is null) return false;

            ligne.Competence = competence;
            ligne.Commentaire = commentaire;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RemettreATesterAsync(int ligneId, CancellationToken ct = default)
        {
            var ligne = await _context.LignesDeTest.FirstOrDefaultAsync(l => l.Id == ligneId, ct);
            if (ligne is null) return false;

            ligne.Statut = StatutsDeTest.ATester;
            ligne.DateEffectuee = null;
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
