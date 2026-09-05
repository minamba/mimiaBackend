using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using DomainEvaluation = SchoolWebApp.Domain.Models.EvaluationEleve;

namespace SchoolWebApp.Dal.Repositories
{
    public class EvaluationRepository : IEvaluationRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public EvaluationRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DomainEvaluation?> AjouterAsync(
            int eleveId,
            int conversationId,
            string? notion,
            double note,
            string? remarque,
            string? aRevoir,
            IEnumerable<QuestionEvaluation>? questions,
            CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte produit par le
            // modèle : c'est la seule façon d'être sûr que la note atterrit dans
            // la bonne matière, quoi qu'il ait écrit dans sa balise.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            // LA CLASSE EST LUE MAINTENANT, PAS À LA RELECTURE.
            //
            // Une note appartient à l'année où elle a été prise. Si on se
            // contentait de la classe actuelle de l'élève au moment d'afficher
            // la fiche, une note de 4e deviendrait une note de 3e le jour où le
            // parent change la classe — et le suivi de niveau, faux. Le
            // changement de classe est une simple mise à jour de colonne qui ne
            // laisse aucune trace datée : ce qui n'est pas capturé ici est perdu
            // pour toujours.
            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync(ct);

            var liste = questions?.ToList() ?? [];

            var evaluation = new Evaluation
            {
                EleveId = eleveId,
                MatiereId = conversation.MatiereId,
                ConversationId = conversation.Id,
                Notion = Tronquer(notion, 300),
                Note = Math.Clamp(note, 0, 20),
                Remarque = Tronquer(remarque, 2000),
                ARevoir = Tronquer(aRevoir, 1000),
                Detail = liste.Count == 0 ? null : JsonSerializer.Serialize(liste, JsonOptions),
                DateCreation = DateTime.UtcNow,

                // Zéro plutôt que null signifierait « niveau inconnu » avec un
                // identifiant qui n'existe pas : on garde null, qui a déjà ce
                // sens dans la colonne.
                NiveauScolaireId = niveauId == 0 ? null : niveauId
            };

            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync(ct);

            return await _context.Evaluations
                .AsNoTracking()
                .Where(e => e.Id == evaluation.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<IEnumerable<DomainEvaluation>> GetParEleveAsync(
            int eleveId, int limite, CancellationToken ct = default) =>
            await _context.Evaluations
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId)
                .OrderByDescending(e => e.DateCreation)
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
        public async Task<PageHistorique<DomainEvaluation>> GetHistoriqueAsync(
            int eleveId,
            string? curseur,
            int taille,
            int? matiereId = null,
            bool duPlusAncien = false,
            int? niveauScolaireId = null,
            CancellationToken ct = default)
        {
            var lesSiennes = _context.Evaluations
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId);

            // La matière est filtrée EN BASE, pas sur la tranche déjà chargée :
            // filtrer dix lignes sur deux cents ne montrerait que les
            // évaluations de maths qui se trouvent dans les dix dernières, et
            // afficherait « aucune » pour un élève qui en a trente.
            if (matiereId is int matiere)
                lesSiennes = lesSiennes.Where(e => e.MatiereId == matiere);

            // L'ANNÉE SCOLAIRE, encadrée par des DATES et non par une colonne :
            // c'est l'historique des classes qui dit quand l'élève y était.
            // Filtrée en base pour la même raison que la matière — le compte
            // affiché doit suivre le filtre, sans quoi « 10 sur 214 » parlerait
            // de toute la scolarité sous un onglet qui n'en montre qu'une année.
            var (debutAnnee, finAnnee) = await BornesAnnee.ResoudreAsync(
                _context, eleveId, niveauScolaireId, ct);

            if (debutAnnee is DateTime d) lesSiennes = lesSiennes.Where(e => e.DateCreation >= d);
            if (finAnnee is DateTime f) lesSiennes = lesSiennes.Where(e => e.DateCreation < f);

            // Compté sur l'historique ENTIER (filtre compris), pas sur la
            // tranche : c'est ce qui permet d'écrire « 10 sur 214 », et le
            // compte doit suivre le filtre pour rester vrai.
            var total = await lesSiennes.CountAsync(ct);

            var depuis = lesSiennes;

            // Le sens de la comparaison suit celui du tri : « la suite », c'est
            // ce qui vient APRÈS la dernière ligne servie dans l'ordre affiché.
            // Du plus ancien, la suite est ce qui est plus RÉCENT — garder le
            // `<` ferait repartir la deuxième tranche à l'envers, dans ce qui
            // avait déjà défilé.
            if (CurseurHistorique.Lire(curseur) is { } reprise)
                depuis = duPlusAncien
                    ? depuis.Where(e =>
                        e.DateCreation > reprise.Date
                        || (e.DateCreation == reprise.Date && e.Id > reprise.Id))
                    : depuis.Where(e =>
                        e.DateCreation < reprise.Date
                        || (e.DateCreation == reprise.Date && e.Id < reprise.Id));

            var triees = duPlusAncien
                ? depuis.OrderBy(e => e.DateCreation).ThenBy(e => e.Id)
                : depuis.OrderByDescending(e => e.DateCreation).ThenByDescending(e => e.Id);

            var lignes = await triees
                .Take(taille + 1)
                .Select(Projection)
                .ToListAsync(ct);

            var ilEnReste = lignes.Count > taille;
            if (ilEnReste) lignes.RemoveAt(taille);

            return new PageHistorique<DomainEvaluation>
            {
                Elements = lignes,
                Total = total,
                Suite = ilEnReste && lignes.Count > 0
                    ? CurseurHistorique.Ecrire(lignes[^1].DateCreation, lignes[^1].Id)
                    : null
            };
        }

        public async Task<IEnumerable<DomainEvaluation>> GetEntreAsync(
            int eleveId, DateTime debut, DateTime fin, CancellationToken ct = default) =>
            await _context.Evaluations
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId
                            && e.DateCreation >= debut
                            && e.DateCreation < fin)
                .OrderBy(e => e.DateCreation)
                .Select(Projection)
                .ToListAsync(ct);

        /// <summary>
        /// Deux requêtes plutôt qu'une jointure : les notes et les notions ont
        /// des cardinalités indépendantes, les joindre produirait un produit
        /// cartésien de l'une par l'autre.
        /// </summary>
        public async Task<IEnumerable<ProgressionMatiere>> GetProgressionAsync(
            int eleveId, int? niveauScolaireId = null, CancellationToken ct = default)
        {
            var bornes = await BornesAnnee.ResoudreAsync(_context, eleveId, niveauScolaireId, ct);

            var notes = await _context.Evaluations
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId)
                .Where(e => bornes.Depuis == null || e.DateCreation >= bornes.Depuis)
                .Where(e => bornes.Jusqua == null || e.DateCreation < bornes.Jusqua)
                .OrderBy(e => e.DateCreation)
                .Select(e => new
                {
                    e.MatiereId,
                    Point = new PointNote
                    {
                        Date = e.DateCreation,
                        Note = e.Note,
                        Notion = e.Notion,
                        NiveauLibelle = e.NiveauScolaire!.Libelle,
                        NiveauOrdre = e.NiveauScolaire!.Ordre
                    }
                })
                .ToListAsync(ct);

            var notions = await _context.MaitrisesEleves
                .AsNoTracking()
                .Where(m => m.EleveId == eleveId)
                // La maîtrise est un ÉTAT : on la rattache à l'année de sa
                // dernière observation, le moment où l'élève y a travaillé.
                .Where(m => bornes.Depuis == null || m.DerniereEvaluation >= bornes.Depuis)
                .Where(m => bornes.Jusqua == null || m.DerniereEvaluation < bornes.Jusqua)
                .OrderByDescending(m => m.Score)
                .Select(m => new
                {
                    MatiereId = m.Competence!.MatiereId,
                    Notion = new NotionMaitrisee
                    {
                        CompetenceId = m.CompetenceId,
                        Libelle = m.Competence.Libelle,
                        Domaine = m.Competence.Domaine,
                        NiveauLibelle = m.Competence.NiveauScolaire!.Libelle,
                        NiveauOrdre = m.Competence.NiveauScolaire!.Ordre,
                        Score = m.Score,
                        Confiance = m.Confiance,
                        NombreObservations = m.NombreObservations,
                        DerniereEvaluation = m.DerniereEvaluation
                    }
                })
                .ToListAsync(ct);

            // Une matière n'apparaît que si l'élève y a laissé quelque chose.
            // Un graphique vide pour l'anglais qui n'a jamais ouvert se lirait
            // comme un échec plutôt que comme une absence.
            var matieresConcernees = notes.Select(n => n.MatiereId)
                .Concat(notions.Select(n => n.MatiereId))
                .Distinct()
                .ToList();

            var matieres = await _context.Matieres
                .AsNoTracking()
                .Where(m => matieresConcernees.Contains(m.Id))
                .OrderBy(m => m.Ordre)
                .Select(m => new ProgressionMatiere
                {
                    MatiereId = m.Id,
                    MatiereLibelle = m.Libelle,
                    ProfPrenom = m.ProfPrenom,
                    ProfCouleur = m.ProfCouleur
                })
                .ToListAsync(ct);

            foreach (var matiere in matieres)
            {
                matiere.Notes = notes
                    .Where(n => n.MatiereId == matiere.MatiereId)
                    .Select(n => n.Point)
                    .ToList();

                matiere.Notions = notions
                    .Where(n => n.MatiereId == matiere.MatiereId)
                    .Select(n => n.Notion)
                    .ToList();
            }

            return matieres;
        }

        /// <summary>
        /// Projection commune. Déclarée une fois : trois requêtes divergentes
        /// finiraient par ne plus renvoyer les mêmes champs.
        /// </summary>
        private static readonly System.Linq.Expressions.Expression<Func<Evaluation, DomainEvaluation>> Projection =
            e => new DomainEvaluation
            {
                Id = e.Id,
                MatiereId = e.MatiereId,
                MatiereLibelle = e.Matiere!.Libelle,
                ProfPrenom = e.Matiere.ProfPrenom,
                ProfAvatar = e.Matiere.ProfAvatar,
                ProfCouleur = e.Matiere.ProfCouleur,
                Notion = e.Notion,
                Note = e.Note,
                Remarque = e.Remarque,
                ARevoir = e.ARevoir,
                DateCreation = e.DateCreation
            };

        /// <summary>
        /// Sérialisation du détail. Sans échappement systématique des accents :
        /// la colonne est en nvarchar, et un « é » à la place d'un « é »
        /// rendrait le contenu illisible pour qui regarde la base.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task<DomainEvaluation?> GetCopieAsync(
            int evaluationId, int eleveId, CancellationToken ct = default)
        {
            // Le filtre sur l'élève est la garde d'accès : sans lui, connaître
            // un identifiant suffirait à lire la copie de n'importe qui.
            var copie = await _context.Evaluations
                .AsNoTracking()
                .Where(e => e.Id == evaluationId && e.EleveId == eleveId)
                .Select(e => new
                {
                    Evaluation = new DomainEvaluation
                    {
                        Id = e.Id,
                        MatiereId = e.MatiereId,
                        MatiereLibelle = e.Matiere!.Libelle,
                        ProfPrenom = e.Matiere.ProfPrenom,
                        ProfAvatar = e.Matiere.ProfAvatar,
                        ProfCouleur = e.Matiere.ProfCouleur,
                        Notion = e.Notion,
                        Note = e.Note,
                        Remarque = e.Remarque,
                        ARevoir = e.ARevoir,
                        DateCreation = e.DateCreation,
                        ElevePrenom = e.Eleve!.Prenom,
                        EleveNom = e.Eleve.Nom,
                        EleveNiveau = e.Eleve.NiveauScolaire!.Libelle
                    },
                    e.Detail
                })
                .FirstOrDefaultAsync(ct);

            if (copie is null) return null;

            copie.Evaluation.Questions = Desserialiser(copie.Detail);
            return copie.Evaluation;
        }

        /// <summary>
        /// Un détail illisible ne fait pas échouer la lecture : la copie
        /// s'affiche alors sans le détail des questions, ce qui vaut mieux
        /// qu'une erreur devant un parent.
        /// </summary>
        private static List<QuestionEvaluation> Desserialiser(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];

            try
            {
                return JsonSerializer.Deserialize<List<QuestionEvaluation>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        /// <summary>
        /// Le texte vient d'un modèle : rien ne garantit qu'il respecte la
        /// longueur de colonne. On tronque plutôt que de laisser SQL Server
        /// rejeter l'insertion et perdre la note.
        /// </summary>
        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }
    }
}
