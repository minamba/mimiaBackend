using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class EvaluationPrevueRepository : IEvaluationPrevueRepository
    {
        /// <summary>
        /// Au-delà, une évaluation prévue sort de la liste « à venir » sans
        /// jamais être supprimée — même principe que partout ailleurs dans
        /// ce projet : rien n'est effacé, juste plus affiché. Choisie plutôt
        /// qu'un compte de séances, qui demanderait une vraie comptabilité
        /// pour un gain incertain.
        /// </summary>
        private static readonly TimeSpan FenetreEnAttente = TimeSpan.FromDays(45);

        private readonly SchoolWebAppDatabaseContext _context;

        public EvaluationPrevueRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<EvaluationPrevueEleve?> EnregistrerAsync(
            int eleveId, int conversationId, string? notion, CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte du
            // modèle — même garde que pour les dictées, les fiches et les
            // évaluations.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            var entite = new EvaluationPrevue
            {
                EleveId = eleveId,
                MatiereId = conversation.MatiereId,
                ConversationId = conversation.Id,
                Notion = Tronquer(notion, 300),
                DateCreation = DateTime.UtcNow,
            };

            _context.EvaluationsPrevues.Add(entite);
            await _context.SaveChangesAsync(ct);

            return await _context.EvaluationsPrevues
                .AsNoTracking()
                .Where(e => e.Id == entite.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<IEnumerable<EvaluationPrevueEleve>> GetEnAttenteAsync(
            int eleveId, CancellationToken ct = default)
        {
            var seuil = DateTime.UtcNow - FenetreEnAttente;

            return await _context.EvaluationsPrevues
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId
                    && e.ConsommeeLe == null
                    && e.DateCreation >= seuil)
                .OrderByDescending(e => e.DateCreation)
                .Select(Projection)
                .ToListAsync(ct);
        }

        public async Task MarquerConsommeesAsync(
            int eleveId, int matiereId, CancellationToken ct = default)
        {
            var ouvertes = await _context.EvaluationsPrevues
                .Where(e => e.EleveId == eleveId && e.MatiereId == matiereId && e.ConsommeeLe == null)
                .ToListAsync(ct);

            if (ouvertes.Count == 0) return;

            var maintenant = DateTime.UtcNow;
            foreach (var e in ouvertes) e.ConsommeeLe = maintenant;

            await _context.SaveChangesAsync(ct);
        }

        private static readonly System.Linq.Expressions.Expression<Func<EvaluationPrevue, EvaluationPrevueEleve>> Projection =
            e => new EvaluationPrevueEleve
            {
                Id = e.Id,
                MatiereId = e.MatiereId,
                MatiereLibelle = e.Matiere!.Libelle,
                ProfCouleur = e.Matiere.ProfCouleur,
                Notion = e.Notion,
                DateCreation = e.DateCreation,
            };

        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }
    }
}
