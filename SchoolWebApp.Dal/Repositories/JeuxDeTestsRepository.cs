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

            // LE TEST LE PLUS RÉCENT D'ABORD — Camara, le 04/11/2026. Dans un test, les lignes
            // gardent l'ordre où on les a écrites : « compétence 1 » reste avant « compétence 2 ».
            var tests = jeu.Tests
                .OrderByDescending(t => t.Id)
                .Select(t => new DomainTest
                {
                    Id = t.Id,
                    JeuId = t.JeuId,
                    Titre = t.Titre,
                    MatiereCode = t.MatiereCode,
                    Matiere = t.Matiere,
                    Lignes = t.Lignes.OrderBy(l => l.Id).Select(VersDomaine).ToList(),
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
            int jeuId, int ligneId, string competence, string statut, DateTime? dateEffectuee, string? commentaire,
            CancellationToken ct = default)
        {
            var ligne = await LigneDuJeuAsync(jeuId, ligneId, ct);
            if (ligne is null) return null;

            ligne.Competence = competence;
            ligne.Statut = statut;
            ligne.DateEffectuee = dateEffectuee?.Date;
            ligne.Commentaire = commentaire;

            await _context.SaveChangesAsync(ct);
            return await GetAsync(jeuId, ct);
        }

        public async Task<DomainJeu?> ChangerStatutAsync(int jeuId, int ligneId, string statut, CancellationToken ct = default)
        {
            var ligne = await LigneDuJeuAsync(jeuId, ligneId, ct);
            if (ligne is null) return null;

            ligne.Statut = statut;

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
