using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Assemble le programme scolaire pour l'écran d'administration : TOUTES
    /// les classes, TOUTES les notions — retirées comprises, puisque c'est
    /// justement ce qu'on vient voir — et chaque échéance rangée sous la
    /// matière et la classe que ses codes désignent.
    /// </summary>
    public class ProgrammeScolaireRepository : IProgrammeScolaireRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ProgrammeScolaireRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ProgrammeScolaireAdmin> GetProgrammeAsync(CancellationToken ct = default)
        {
            var niveaux = await _context.NiveauxScolaires
                .AsNoTracking()
                .OrderBy(n => n.Ordre).ThenBy(n => n.Libelle)
                .Select(n => new { n.Id, n.Code, n.Libelle, n.Ordre })
                .ToListAsync(ct);

            var matieres = await _context.Matieres
                .AsNoTracking()
                .Where(m => m.Active)
                .OrderBy(m => m.Libelle)
                .Select(m => new { m.Id, m.Code, m.Libelle, m.ProfCouleur })
                .ToListAsync(ct);

            var notions = await _context.Competences
                .AsNoTracking()
                .OrderBy(c => c.Domaine).ThenBy(c => c.Ordre)
                .Select(c => new
                {
                    c.Id, c.Code, c.Domaine, c.Libelle, c.Ordre, c.Actif,
                    c.DateDebutValidite, c.DateFinValidite, c.DateCreation, c.DateModification,
                    c.MatiereId, c.NiveauScolaireId,
                })
                .ToListAsync(ct);

            var echeances = await _context.EcheancesReferentiel
                .AsNoTracking()
                .OrderBy(e => e.DateEcheance)
                .ToListAsync(ct);

            var parClasseEtMatiere = notions
                .GroupBy(c => (c.NiveauScolaireId, c.MatiereId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var maintenant = DateTime.UtcNow;

            var programme = new ProgrammeScolaireAdmin
            {
                AnneeScolaire = AnneeScolaire.Courante(maintenant),
                Titre = AnneeScolaire.LibelleProgrammes(maintenant),
                EcheancesEnRetard = echeances.Count(e =>
                    !e.Sentinelle && e.DateConnue && e.TraiteeLe == null && e.DateEcheance < maintenant),
                Sentinelles = echeances.Where(e => e.Sentinelle).Select(Detail).ToList(),
            };

            foreach (var niveau in niveaux)
            {
                var classe = new ClasseProgramme
                {
                    Code = niveau.Code ?? "", Libelle = niveau.Libelle ?? "", Ordre = niveau.Ordre,
                };

                foreach (var matiere in matieres)
                {
                    parClasseEtMatiere.TryGetValue((niveau.Id, matiere.Id), out var lignes);

                    // L'échéance se range par CODES, jamais par ressemblance
                    // de libellé : « Français et mathématiques » ne se
                    // rapproche pas tout seul de « FRANCAIS » et « MATHS ».
                    var concernees = echeances
                        .Where(e => !e.Sentinelle
                            && Contient(e.MatieresCodes, matiere.Code)
                            && Contient(e.NiveauxCodes, niveau.Code))
                        .Select(Detail)
                        .ToList();

                    // Une matière sans notion ET sans échéance pour cette
                    // classe n'a rien à montrer : elle n'encombre pas.
                    if ((lignes is null || lignes.Count == 0) && concernees.Count == 0) continue;

                    classe.Matieres.Add(new MatiereProgrammeAdmin
                    {
                        MatiereId = matiere.Id,
                        Code = matiere.Code ?? "",
                        Libelle = matiere.Libelle ?? "",
                        Couleur = matiere.ProfCouleur,
                        Echeances = concernees,
                        Notions = (lignes ?? new()).Select(c => new NotionProgramme
                        {
                            Id = c.Id,
                            Code = c.Code,
                            Domaine = c.Domaine,
                            Libelle = c.Libelle,
                            Ordre = c.Ordre,
                            Actif = c.Actif,
                            DateDebutValidite = c.DateDebutValidite,
                            DateFinValidite = c.DateFinValidite,
                            DateCreation = c.DateCreation,
                            DateModification = c.DateModification,
                        }).ToList(),
                    });
                }

                programme.Classes.Add(classe);
            }

            return programme;
        }

        private static bool Contient(string? codes, string? code) =>
            !string.IsNullOrWhiteSpace(codes) && !string.IsNullOrWhiteSpace(code)
            && codes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(code, StringComparer.OrdinalIgnoreCase);

        private static EcheanceReferentielDetail Detail(EcheanceReferentiel e) => new(
            e.Id, e.MatiereLibelle, e.NiveauxConcernes, e.DateEcheance, e.DateConnue, e.Sentinelle,
            e.TexteOfficiel, e.Notes, e.Url, e.DernierStatutVeille, e.DernierePageVerifieeLe,
            e.DerniereAlerteLe, e.TraiteeLe);
    }
}
