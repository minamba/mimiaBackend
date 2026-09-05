using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class RapportRepository : IRapportRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public RapportRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<RapportEleve?> EnregistrerAsync(
            int eleveId,
            int conversationId,
            string? travaille,
            double? noteComprehension,
            double? noteRevision,
            string? remarque,
            string? aRevoir,
            CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte du modèle :
            // c'est la seule façon d'être sûr que le rapport atterrit dans la
            // bonne matière, quoi qu'il ait écrit.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            // Un compte rendu par SÉANCE, pas par conversation.
            //
            // La base imposait l'unicité sur la conversation, et comme un élève
            // garde le même fil pendant des semaines, chaque cours écrasait le
            // compte rendu du précédent : cinq séances dans la journée ne
            // laissaient qu'une ligne. Le parent perdait tout l'historique.
            //
            // Il reste à écarter le vrai doublon, celui d'UNE séance conclue
            // deux fois — un au revoir, puis l'échéance du minuteur derrière.
            // Faute d'identifiant de séance en base, on le reconnaît au temps :
            // ces deux conclusions-là se suivent de quelques secondes, alors
            // que deux séances sont forcément séparées par le cours lui-même,
            // six minutes au minimum. La fenêtre est donc large pour le
            // doublon et hors d'atteinte pour une relance.
            var seuil = DateTime.UtcNow.AddMinutes(-2);

            var rapport = await _context.RapportsSeance
                .Where(r => r.ConversationId == conversationId && r.DateCreation >= seuil)
                .OrderByDescending(r => r.DateCreation)
                .FirstOrDefaultAsync(ct);

            if (rapport is null)
            {
                rapport = new RapportSeance
                {
                    EleveId = eleveId,
                    MatiereId = conversation.MatiereId,
                    ConversationId = conversation.Id,
                };

                _context.RapportsSeance.Add(rapport);
            }

            rapport.Travaille = Tronquer(travaille, 1000);
            rapport.NoteComprehension = Borner(noteComprehension);
            rapport.NoteRevision = Borner(noteRevision);
            rapport.Remarque = Tronquer(remarque, 2000);
            rapport.ARevoir = Tronquer(aRevoir, 1000);
            rapport.DateCreation = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await _context.RapportsSeance
                .AsNoTracking()
                .Where(r => r.Id == rapport.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<IEnumerable<RapportEleve>> GetParEleveAsync(
            int eleveId, int limite, CancellationToken ct = default) =>
            await _context.RapportsSeance
                .AsNoTracking()
                .Where(r => r.EleveId == eleveId)
                .OrderByDescending(r => r.DateCreation)
                .Take(limite)
                .Select(Projection)
                .ToListAsync(ct);

        /// <summary>
        /// Une tranche de l'historique, prise à l'endroit où la précédente
        /// s'est arrêtée.
        ///
        /// On demande UNE LIGNE DE PLUS que la taille voulue : c'est ce qui dit
        /// s'il reste quelque chose derrière, sans une seconde requête. Elle est
        /// jetée juste après, elle n'a servi qu'à répondre à cette question.
        /// </summary>
        public async Task<PageHistorique<RapportEleve>> GetHistoriqueAsync(
            int eleveId,
            string? curseur,
            int taille,
            int? matiereId = null,
            bool duPlusAncien = false,
            int? niveauScolaireId = null,
            CancellationToken ct = default)
        {
            var lesSiens = _context.RapportsSeance
                .AsNoTracking()
                .Where(r => r.EleveId == eleveId);

            // La matière est filtrée EN BASE, pas sur la tranche déjà chargée :
            // filtrer dix lignes sur deux cents afficherait « aucune séance de
            // maths » à un élève qui en a trente, simplement parce qu'aucune ne
            // figure dans les dix dernières.
            if (matiereId is int matiere)
                lesSiens = lesSiens.Where(r => r.MatiereId == matiere);

            // L'ANNÉE PASSE PAR LA DATE, PAS PAR LA CONVERSATION.
            //
            // Une première version héritait le niveau de la conversation. C'était
            // faux : le fil d'une matière vit d'une année sur l'autre, et une
            // séance de seconde tenue dans un fil ouvert en troisième aurait été
            // comptée en troisième. Une séance, elle, est datée.
            var (debutAnnee, finAnnee) = await BornesAnnee.ResoudreAsync(
                _context, eleveId, niveauScolaireId, ct);

            if (debutAnnee is DateTime d) lesSiens = lesSiens.Where(r => r.DateCreation >= d);
            if (finAnnee is DateTime f) lesSiens = lesSiens.Where(r => r.DateCreation < f);

            // Compté sur l'historique ENTIER (filtre compris) : « 10 sur 214 »
            // doit suivre le filtre pour rester vrai.
            var total = await lesSiens.CountAsync(ct);

            var depuis = lesSiens;

            // Le sens de la comparaison suit celui du tri : « la suite », c'est
            // ce qui vient APRÈS la dernière ligne servie dans l'ordre affiché.
            if (CurseurHistorique.Lire(curseur) is { } reprise)
                depuis = duPlusAncien
                    ? depuis.Where(r =>
                        r.DateCreation > reprise.Date
                        || (r.DateCreation == reprise.Date && r.Id > reprise.Id))
                    : depuis.Where(r =>
                        r.DateCreation < reprise.Date
                        || (r.DateCreation == reprise.Date && r.Id < reprise.Id));

            var triees = duPlusAncien
                ? depuis.OrderBy(r => r.DateCreation).ThenBy(r => r.Id)
                : depuis.OrderByDescending(r => r.DateCreation).ThenByDescending(r => r.Id);

            var lignes = await triees
                .Take(taille + 1)
                .Select(Projection)
                .ToListAsync(ct);

            var ilEnReste = lignes.Count > taille;
            if (ilEnReste) lignes.RemoveAt(taille);

            return new PageHistorique<RapportEleve>
            {
                Elements = lignes,
                Total = total,
                Suite = ilEnReste && lignes.Count > 0
                    ? CurseurHistorique.Ecrire(lignes[^1].DateCreation, lignes[^1].Id)
                    : null
            };
        }

        public async Task<IEnumerable<RapportEleve>> GetEntreAsync(
            int eleveId, DateTime debut, DateTime fin, CancellationToken ct = default) =>
            await _context.RapportsSeance
                .AsNoTracking()
                .Where(r => r.EleveId == eleveId
                            && r.DateCreation >= debut
                            && r.DateCreation < fin)
                .OrderBy(r => r.DateCreation)
                .Select(Projection)
                .ToListAsync(ct);

        public async Task<RapportEleve?> GetDetailAsync(
            int rapportId, int eleveId, CancellationToken ct = default) =>
            // Le filtre sur l'élève est la garde d'accès : sans lui, connaître
            // un identifiant suffirait à lire le rapport de n'importe qui.
            await _context.RapportsSeance
                .AsNoTracking()
                .Where(r => r.Id == rapportId && r.EleveId == eleveId)
                .Select(r => new RapportEleve
                {
                    Id = r.Id,
                    MatiereId = r.MatiereId,
                    MatiereLibelle = r.Matiere!.Libelle,
                    ProfPrenom = r.Matiere.ProfPrenom,
                    ProfAvatar = r.Matiere.ProfAvatar,
                    ProfCouleur = r.Matiere.ProfCouleur,
                    Travaille = r.Travaille,
                    NoteComprehension = r.NoteComprehension,
                    NoteRevision = r.NoteRevision,
                    Remarque = r.Remarque,
                    ARevoir = r.ARevoir,
                    DateCreation = r.DateCreation,
                    ElevePrenom = r.Eleve!.Prenom,
                    EleveNom = r.Eleve.Nom,
                    EleveNiveau = r.Eleve.NiveauScolaire!.Libelle,
                })
                .FirstOrDefaultAsync(ct);

        /// <summary>
        /// Projection commune aux listes. Déclarée une fois : trois requêtes
        /// divergentes finiraient par ne plus renvoyer les mêmes champs.
        /// </summary>
        private static readonly Expression<Func<RapportSeance, RapportEleve>> Projection =
            r => new RapportEleve
            {
                Id = r.Id,
                MatiereId = r.MatiereId,
                MatiereLibelle = r.Matiere!.Libelle,
                ProfPrenom = r.Matiere.ProfPrenom,
                ProfAvatar = r.Matiere.ProfAvatar,
                ProfCouleur = r.Matiere.ProfCouleur,
                Travaille = r.Travaille,
                NoteComprehension = r.NoteComprehension,
                NoteRevision = r.NoteRevision,
                Remarque = r.Remarque,
                ARevoir = r.ARevoir,
                DateCreation = r.DateCreation,
            };

        private static double? Borner(double? note) =>
            note is null ? null : Math.Clamp(note.Value, 0, 20);

        /// <summary>
        /// Le texte vient d'un modèle : rien ne garantit qu'il respecte la
        /// longueur de colonne. On tronque plutôt que de laisser SQL Server
        /// rejeter l'insertion et perdre le compte rendu.
        /// </summary>
        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }
    }
}
