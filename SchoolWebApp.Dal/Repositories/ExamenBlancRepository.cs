using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using M = SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>Les examens blancs (23/11/2026) — voir <see cref="IExamenBlancRepository"/>.</summary>
    public class ExamenBlancRepository : IExamenBlancRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ExamenBlancRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IReadOnlyList<M.ExamenBlanc>> GetListeAsync(int eleveId, CancellationToken ct = default)
        {
            var examens = await Requete()
                .Where(e => e.EleveId == eleveId)
                .OrderBy(e => e.DateExamen)
                .ThenBy(e => e.Id)
                .ToListAsync(ct);

            return examens.Select(Projeter).ToList();
        }

        public async Task<M.ExamenBlanc?> GetAsync(int eleveId, int examenBlancId, CancellationToken ct = default)
        {
            var examen = await Requete()
                .FirstOrDefaultAsync(e => e.Id == examenBlancId && e.EleveId == eleveId, ct);

            return examen is null ? null : Projeter(examen);
        }

        public async Task<(int ExamenBlancId, int MatiereLigneId)> AjouterMatiereAsync(
            int eleveId, string type, DateTime dateExamen, TimeSpan? heureExamen, int matiereId,
            DateTime maintenant, CancellationToken ct = default)
        {
            var jour = dateExamen.Date;

            var examen = await _context.ExamensBlancs
                .Include(e => e.Matieres)
                .FirstOrDefaultAsync(e => e.EleveId == eleveId && e.DateExamen == jour, ct);

            if (examen is null)
            {
                examen = new ExamenBlanc
                {
                    EleveId = eleveId,
                    Type = type,
                    DateExamen = jour,
                    HeureExamen = heureExamen,
                    DateCreation = maintenant,
                };
                _context.ExamensBlancs.Add(examen);
            }
            else if (examen.HeureExamen is null && heureExamen is not null)
            {
                examen.HeureExamen = heureExamen;
                examen.DateModification = maintenant;
            }

            var ligne = examen.Matieres.FirstOrDefault(m => m.MatiereId == matiereId);

            if (ligne is null)
            {
                ligne = new ExamenBlancMatiere { MatiereId = matiereId, DateCreation = maintenant };
                examen.Matieres.Add(ligne);
            }

            await _context.SaveChangesAsync(ct);
            return (examen.Id, ligne.Id);
        }

        public async Task<M.DocumentExamenBlanc?> AjouterDocumentAsync(
            int eleveId, int examenBlancId, int matiereLigneId, M.DocumentExamenBlanc document, CancellationToken ct = default)
        {
            var ligne = await _context.ExamensBlancsMatieres
                .Where(m => m.Id == matiereLigneId
                    && m.ExamenBlancId == examenBlancId
                    && m.ExamenBlanc!.EleveId == eleveId)
                .Select(m => new { m.Id, Nombre = m.Documents.Count })
                .FirstOrDefaultAsync(ct);

            if (ligne is null || ligne.Nombre >= M.ExamensBlancs.DocumentsMax) return null;

            // Les dossiers sont à refaire : un document de plus.
            await _context.ExamensBlancsMatieres
                .Where(m => m.Id == ligne.Id)
                .ExecuteUpdateAsync(u => u.SetProperty(m => m.DocumentsModifiesLe, document.DateCreation), ct);

            var entite = new ExamenBlancDocument
            {
                ExamenBlancMatiereId = ligne.Id,
                NomFichier = document.NomFichier,
                TypeMime = document.TypeMime,
                Taille = document.Taille,
                NombrePages = document.NombrePages,
                Chemin = document.Chemin,
                Transcription = document.Transcription,
                DateCreation = document.DateCreation,
            };

            _context.ExamensBlancsDocuments.Add(entite);
            await _context.SaveChangesAsync(ct);

            return ProjeterDocument(entite);
        }

        public async Task<M.DocumentExamenBlanc?> GetDocumentAsync(int eleveId, int examenBlancId, int documentId, CancellationToken ct = default)
        {
            var document = await DocumentsDe(eleveId, examenBlancId)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId, ct);

            return document is null ? null : ProjeterDocument(document);
        }

        public async Task<M.DocumentExamenBlanc?> SupprimerDocumentAsync(int eleveId, int examenBlancId, int documentId, CancellationToken ct = default)
        {
            var document = await DocumentsDe(eleveId, examenBlancId)
                .FirstOrDefaultAsync(d => d.Id == documentId, ct);

            if (document is null) return null;

            _context.ExamensBlancsDocuments.Remove(document);
            await _context.SaveChangesAsync(ct);

            // Les dossiers sont à refaire : un document de moins (et il ne doit plus apparaître dans aucun).
            await _context.ExamensBlancsMatieres
                .Where(m => m.Id == document.ExamenBlancMatiereId)
                .ExecuteUpdateAsync(u => u.SetProperty(m => m.DocumentsModifiesLe, DateTime.UtcNow), ct);

            return ProjeterDocument(document);
        }

        public async Task<M.RestesExamenBlanc?> RetirerMatiereAsync(int eleveId, int examenBlancId, int matiereLigneId, CancellationToken ct = default)
        {
            var examen = await _context.ExamensBlancs
                .Include(e => e.Matieres).ThenInclude(m => m.Documents)
                .FirstOrDefaultAsync(e => e.Id == examenBlancId && e.EleveId == eleveId, ct);

            var ligne = examen?.Matieres.FirstOrDefault(m => m.Id == matiereLigneId);
            if (examen is null || ligne is null) return null;

            var chemins = ligne.Documents.Select(d => d.Chemin).OfType<string>().ToList();
            var controles = ligne.ControleId is int c ? new List<int> { c } : [];

            // L'EXAMEN BLANC PART AVEC SA DERNIÈRE MATIÈRE : une carte « Brevet blanc » sans matière ne dirait rien.
            if (examen.Matieres.Count == 1) _context.ExamensBlancs.Remove(examen);
            else _context.ExamensBlancsMatieres.Remove(ligne);

            await _context.SaveChangesAsync(ct);
            return new M.RestesExamenBlanc(chemins, controles);
        }

        public async Task<M.RestesExamenBlanc?> SupprimerAsync(int eleveId, int examenBlancId, CancellationToken ct = default)
        {
            var examen = await _context.ExamensBlancs
                .Include(e => e.Matieres).ThenInclude(m => m.Documents)
                .FirstOrDefaultAsync(e => e.Id == examenBlancId && e.EleveId == eleveId, ct);

            if (examen is null) return null;

            var chemins = examen.Matieres
                .SelectMany(m => m.Documents)
                .Select(d => d.Chemin)
                .OfType<string>()
                .ToList();
            var controles = examen.Matieres.Select(m => m.ControleId).OfType<int>().ToList();

            _context.ExamensBlancs.Remove(examen);
            await _context.SaveChangesAsync(ct);
            return new M.RestesExamenBlanc(chemins, controles);
        }

        public async Task<IReadOnlyList<(int EleveId, int ExamenBlancId, M.DocumentExamenBlanc Document)>> GetFichiersAPurgerAsync(
            DateTime maintenant, int maximum, CancellationToken ct = default)
        {
            // Deux mois après le JOUR de l'examen blanc : la borne se calcule ici, pas en SQL.
            var limite = maintenant.Date.AddMonths(-M.ExamensBlancs.MoisConservation);

            var lignes = await _context.ExamensBlancsDocuments
                .AsNoTracking()
                .Where(d => d.Chemin != null && d.ExamenBlancMatiere!.ExamenBlanc!.DateExamen <= limite)
                .OrderBy(d => d.Id)
                .Take(maximum)
                .Select(d => new
                {
                    Document = d,
                    d.ExamenBlancMatiere!.ExamenBlanc!.EleveId,
                    d.ExamenBlancMatiere.ExamenBlancId,
                })
                .ToListAsync(ct);

            return lignes.Select(l => (l.EleveId, l.ExamenBlancId, ProjeterDocument(l.Document))).ToList();
        }

        public async Task MarquerFichierEffaceAsync(int documentId, DateTime maintenant, CancellationToken ct = default)
        {
            var document = await _context.ExamensBlancsDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
            if (document is null) return;

            document.Chemin = null;
            document.FichierEffaceLe = maintenant;
            await _context.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlySet<int>> ExistantsAsync(IEnumerable<int> examenBlancIds, CancellationToken ct = default)
        {
            var ids = examenBlancIds.Distinct().ToList();
            if (ids.Count == 0) return new HashSet<int>();

            var existants = await _context.ExamensBlancs
                .AsNoTracking()
                .Where(e => ids.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync(ct);

            return existants.ToHashSet();
        }

        private IQueryable<ExamenBlanc> Requete() =>
            _context.ExamensBlancs
                .AsNoTracking()
                .Include(e => e.Matieres).ThenInclude(m => m.Matiere)
                .Include(e => e.Matieres).ThenInclude(m => m.Documents)
                .Include(e => e.Matieres).ThenInclude(m => m.Notions)
                .AsSplitQuery();

        private IQueryable<ExamenBlancDocument> DocumentsDe(int eleveId, int examenBlancId) =>
            _context.ExamensBlancsDocuments
                .Where(d => d.ExamenBlancMatiere!.ExamenBlancId == examenBlancId
                    && d.ExamenBlancMatiere.ExamenBlanc!.EleveId == eleveId);

        private static M.ExamenBlanc Projeter(ExamenBlanc e) => new()
        {
            Id = e.Id,
            EleveId = e.EleveId,
            Type = e.Type,
            DateExamen = e.DateExamen,
            HeureExamen = e.HeureExamen,
            DateCreation = e.DateCreation,
            Matieres = e.Matieres
                .OrderBy(m => m.DateCreation)
                .ThenBy(m => m.Id)
                .Select(m => new M.ExamenBlancMatiere
                {
                    Id = m.Id,
                    ExamenBlancId = m.ExamenBlancId,
                    MatiereId = m.MatiereId,
                    MatiereLibelle = m.Matiere?.Libelle,
                    ProfPrenom = m.Matiere?.ProfPrenom,
                    ProfAvatar = m.Matiere?.ProfAvatar,
                    ProfCouleur = m.Matiere?.ProfCouleur,
                    DateCreation = m.DateCreation,
                    DocumentsModifiesLe = m.DocumentsModifiesLe,
                    NotionsLe = m.NotionsLe,
                    ControleId = m.ControleId,
                    ProgrammeValideLe = m.ProgrammeValideLe,
                    Documents = m.Documents.OrderBy(d => d.Id).Select(ProjeterDocument).ToList(),
                    Notions = m.Notions
                        .OrderBy(n => n.Ordre)
                        .ThenBy(n => n.Id)
                        .Select(n => new M.NotionExamenBlanc
                        {
                            Id = n.Id,
                            CompetenceId = n.CompetenceId,
                            Libelle = n.Libelle,
                            DocumentIds = LireIds(n.DocumentIds),
                        })
                        .ToList(),
                })
                .ToList(),
        };

        private static M.DocumentExamenBlanc ProjeterDocument(ExamenBlancDocument d) => new()
        {
            Id = d.Id,
            ExamenBlancMatiereId = d.ExamenBlancMatiereId,
            NomFichier = d.NomFichier,
            TypeMime = d.TypeMime,
            // Long en base (jusqu'à 100 Mo, sans plafond propre) ; l'affichage n'en a pas besoin au-delà.
            Taille = (int)Math.Min(d.Taille, int.MaxValue),
            NombrePages = d.NombrePages,
            Chemin = d.Chemin,
            FichierEffaceLe = d.FichierEffaceLe,
            Transcription = d.Transcription,
            Tentatives = d.Tentatives,
            DateCreation = d.DateCreation,
        };

        private static List<int> LireIds(string? liste) =>
            (liste ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

        // ------------------------------------------------------------------ étape 2 : la lecture et les dossiers

        public async Task<IReadOnlyList<M.DocumentALire>> GetDocumentsALireAsync(int maximum, CancellationToken ct = default)
        {
            var lignes = await _context.ExamensBlancsDocuments
                .AsNoTracking()
                .Where(d => d.Transcription == null && d.Chemin != null && d.Tentatives < M.ExamensBlancs.LecturesMax)
                .OrderBy(d => d.Id)
                .Take(maximum)
                .Select(d => new
                {
                    d.Id,
                    d.ExamenBlancMatiere!.ExamenBlanc!.EleveId,
                    d.ExamenBlancMatiere.ExamenBlancId,
                    d.Chemin,
                    d.TypeMime,
                    d.NomFichier,
                })
                .ToListAsync(ct);

            return lignes
                .Select(l => new M.DocumentALire(l.Id, l.EleveId, l.ExamenBlancId, l.Chemin!, l.TypeMime, l.NomFichier))
                .ToList();
        }

        public async Task EnregistrerLectureAsync(int documentId, string texte, CancellationToken ct = default)
        {
            await _context.ExamensBlancsDocuments
                .Where(d => d.Id == documentId)
                .ExecuteUpdateAsync(u => u.SetProperty(d => d.Transcription, texte), ct);
        }

        public async Task<bool> SignalerEchecLectureAsync(int documentId, string mentionAbandon, CancellationToken ct = default)
        {
            var document = await _context.ExamensBlancsDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
            if (document is null) return false;

            document.Tentatives += 1;
            var abandon = document.Tentatives >= M.ExamensBlancs.LecturesMax;
            if (abandon) document.Transcription = mentionAbandon;

            await _context.SaveChangesAsync(ct);
            return abandon;
        }

        public async Task<IReadOnlyList<M.MatiereANotionner>> GetMatieresANotionnerAsync(
            DateTime calmeDepuis, int maximum, CancellationToken ct = default)
        {
            // PLUS AUCUN DOCUMENT : les dossiers partent, sans appel au modèle.
            var videes = await _context.ExamensBlancsMatieres
                .Where(m => !m.Documents.Any() && m.Notions.Any())
                .Select(m => m.Id)
                .ToListAsync(ct);

            foreach (var id in videes)
            {
                await _context.ExamensBlancsNotions.Where(n => n.ExamenBlancMatiereId == id).ExecuteDeleteAsync(ct);
                await _context.ExamensBlancsMatieres.Where(m => m.Id == id)
                    .ExecuteUpdateAsync(u => u.SetProperty(m => m.NotionsLe, DateTime.UtcNow), ct);
            }

            var lignes = await _context.ExamensBlancsMatieres
                .AsNoTracking()
                .Where(m => m.Documents.Any()
                    && m.Documents.All(d => d.Transcription != null || d.Chemin == null)
                    && (m.NotionsLe == null || (m.DocumentsModifiesLe != null && m.DocumentsModifiesLe > m.NotionsLe))
                    && (m.DocumentsModifiesLe == null || m.DocumentsModifiesLe <= calmeDepuis))
                .OrderBy(m => m.Id)
                .Take(maximum)
                .Select(m => new
                {
                    m.Id,
                    m.MatiereId,
                    MatiereLibelle = m.Matiere!.Libelle,
                    m.ExamenBlanc!.Eleve!.NiveauScolaireId,
                    Documents = m.Documents
                        .Where(d => d.Transcription != null)
                        .OrderBy(d => d.Id)
                        .Select(d => new { d.Id, d.NomFichier, d.Transcription })
                        .ToList(),
                })
                .ToListAsync(ct);

            return lignes
                .Select(l => new M.MatiereANotionner(
                    l.Id, l.MatiereId, l.MatiereLibelle,
                    l.NiveauScolaireId == 0 ? null : l.NiveauScolaireId,
                    l.Documents.Select(d => (d.Id, d.NomFichier, d.Transcription!)).ToList()))
                .ToList();
        }

        public async Task PoserNotionsAsync(
            int ligneId, IReadOnlyList<M.NotionProposee> notions, DateTime maintenant, CancellationToken ct = default)
        {
            var ligne = await _context.ExamensBlancsMatieres
                .Include(m => m.Notions)
                .Include(m => m.Documents)
                .FirstOrDefaultAsync(m => m.Id == ligneId, ct);

            if (ligne is null) return;

            // Seuls les documents de CETTE matière, encore là, entrent dans ses dossiers.
            var siens = ligne.Documents.Select(d => d.Id).ToHashSet();

            _context.ExamensBlancsNotions.RemoveRange(ligne.Notions);

            var ordre = 0;
            foreach (var notion in notions)
            {
                var ids = notion.DocumentIds.Where(siens.Contains).Distinct().ToList();
                var libelle = (notion.Libelle ?? string.Empty).Trim();
                if (ids.Count == 0 || libelle.Length == 0) continue;
                if (libelle.Length > 300) libelle = libelle[..300];

                _context.ExamensBlancsNotions.Add(new ExamenBlancNotion
                {
                    ExamenBlancMatiereId = ligne.Id,
                    CompetenceId = notion.CompetenceId,
                    Libelle = libelle,
                    Ordre = ordre++,
                    DocumentIds = string.Join(',', ids),
                    DateCreation = maintenant,
                });
            }

            ligne.NotionsLe = maintenant;
            await _context.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------ étape 3 : la séance et le programme

        public async Task LierControleAsync(int ligneId, int controleId, CancellationToken ct = default)
        {
            await _context.ExamensBlancsMatieres
                .Where(m => m.Id == ligneId)
                .ExecuteUpdateAsync(u => u.SetProperty(m => m.ControleId, controleId), ct);
        }

        public async Task<(int Notions, DateTime? ProgrammeValideLe)?> GetEtatParControleAsync(int controleId, CancellationToken ct = default)
        {
            var etat = await _context.ExamensBlancsMatieres
                .AsNoTracking()
                .Where(m => m.ControleId == controleId)
                .Select(m => new { Notions = m.Notions.Count, m.ProgrammeValideLe })
                .FirstOrDefaultAsync(ct);

            return etat is null ? null : (etat.Notions, etat.ProgrammeValideLe);
        }

        public async Task<M.ExamenBlancPourSeance?> GetParControleAsync(int controleId, CancellationToken ct = default)
        {
            var ligne = await _context.ExamensBlancsMatieres
                .AsNoTracking()
                .Include(m => m.ExamenBlanc)
                .Include(m => m.Matiere)
                .Include(m => m.Notions)
                .Include(m => m.Documents)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.ControleId == controleId, ct);

            if (ligne?.ExamenBlanc is null) return null;

            return new M.ExamenBlancPourSeance(
                ligne.ExamenBlancId, ligne.Id, ligne.ExamenBlanc.Type, ligne.ExamenBlanc.DateExamen,
                ligne.Matiere?.Libelle, ligne.ProgrammeValideLe,
                ligne.Notions
                    .OrderBy(n => n.Ordre)
                    .ThenBy(n => n.Id)
                    .Select(n => new M.NotionExamenBlanc
                    {
                        Id = n.Id,
                        CompetenceId = n.CompetenceId,
                        Libelle = n.Libelle,
                        DocumentIds = LireIds(n.DocumentIds),
                    })
                    .ToList(),
                ligne.Documents
                    .OrderBy(d => d.Id)
                    .Select(d => (d.Id, d.NomFichier, d.Transcription))
                    .ToList());
        }

        public async Task<bool> MarquerProgrammeValideAsync(int controleId, DateTime maintenant, CancellationToken ct = default)
        {
            var touchees = await _context.ExamensBlancsMatieres
                .Where(m => m.ControleId == controleId && m.ProgrammeValideLe == null)
                .ExecuteUpdateAsync(u => u.SetProperty(m => m.ProgrammeValideLe, maintenant), ct);

            return touchees > 0;
        }
    }
}
