using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class ControleScolaireRepository : IControleScolaireRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ControleScolaireRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ControleScolaireEleve?> EnregistrerDepuisConversationAsync(
            int eleveId, int conversationId, int? matiereId, string? sujet,
            DateTime dateControle, TimeSpan? heureControle, CancellationToken ct = default)
        {
            // La conversation reste la source de vérité par défaut, et le
            // repli quand rien n'a été déclaré. `matiereId` n'arrive jamais du
            // texte du modèle : l'appelant l'a déjà rapproché de la liste
            // fermée des matières de l'élève (`ResolveurMatiereDeclaree`).
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync(ct);

            var entite = new ControleScolaire
            {
                EleveId = eleveId,
                MatiereId = matiereId ?? conversation.MatiereId,
                ConversationId = conversation.Id,
                NiveauScolaireId = niveauId == 0 ? null : niveauId,
                PosePar = "PROFESSEUR",
                Sujet = Tronquer(sujet, 300),
                DateControle = dateControle.Date,
                HeureControle = heureControle,
                DateCreation = DateTime.UtcNow,
            };

            _context.ControlesScolaires.Add(entite);
            await _context.SaveChangesAsync(ct);

            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.Id == entite.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<ControleScolaireEleve?> EnregistrerDepuisCalendrierAsync(
            int eleveId, int matiereId, string posePar, string? sujet,
            DateTime dateControle, TimeSpan? heureControle, CancellationToken ct = default)
        {
            var eleve = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => new { e.Id, e.NiveauScolaireId })
                .FirstOrDefaultAsync(ct);

            if (eleve is null) return null;

            var entite = new ControleScolaire
            {
                EleveId = eleveId,
                MatiereId = matiereId,
                ConversationId = null,
                NiveauScolaireId = eleve.NiveauScolaireId == 0 ? null : eleve.NiveauScolaireId,
                PosePar = posePar,
                Sujet = Tronquer(sujet, 300),
                DateControle = dateControle.Date,
                HeureControle = heureControle,
                DateCreation = DateTime.UtcNow,
            };

            _context.ControlesScolaires.Add(entite);
            await _context.SaveChangesAsync(ct);

            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.Id == entite.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<IEnumerable<ControleScolaireEleve>> GetEntreAsync(
            int eleveId, DateTime debut, DateTime finExclusive, CancellationToken ct = default)
        {
            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId
                    && c.DateControle >= debut
                    && c.DateControle < finExclusive)
                .OrderBy(c => c.DateControle)
                .Select(Projection)
                .ToListAsync(ct);
        }

        public async Task<ControleScolaireEleve?> GetProchainAsync(
            int eleveId, int matiereId, DateTime maintenant, CancellationToken ct = default)
        {
            // `maintenant` EST L'HEURE DE PARIS — c'est elle qui dit si 18 h
            // est passé. Un contrôle d'aujourd'hui dont l'heure est connue
            // cesse d'être « à venir » dès qu'il a commencé ; sans heure, il le
            // reste toute la journée, puisqu'on ne sait pas quand il a lieu.
            // Voir ControleScolaire.DureeSupposee.
            var jour = maintenant.Date;
            var demain = jour.AddDays(1);
            var heure = maintenant.TimeOfDay;

            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId
                    && c.MatiereId == matiereId
                    && (c.DateControle >= demain
                        || (c.DateControle >= jour
                            && (c.HeureControle == null || c.HeureControle > heure))))
                .OrderBy(c => c.DateControle)
                .ThenBy(c => c.HeureControle)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<ControleScolaireEleve?> GetAsync(
            int eleveId, int controleId, CancellationToken ct = default)
        {
            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.Id == controleId && c.EleveId == eleveId)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<ControleScolaireEleve>> GetAVenirAsync(
            int eleveId, DateTime maintenant, int limite, CancellationToken ct = default)
        {
            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId && c.DateControle >= maintenant.Date)
                .OrderBy(c => c.DateControle)
                .Take(limite)
                .Select(Projection)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ControleScolaireEleve>> GetPassesAsync(
            int eleveId, DateTime maintenant, int limite, CancellationToken ct = default)
        {
            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId && c.DateControle < maintenant.Date)
                .OrderByDescending(c => c.DateControle)
                .Take(limite)
                .Select(Projection)
                .ToListAsync(ct);
        }

        public async Task<PreparationControle?> GetPreparationAsync(
            int eleveId, int controleId, CancellationToken ct = default)
        {
            var preparations = await GetPreparationsAsync(eleveId, [controleId], ct);
            return preparations.TryGetValue(controleId, out var preparation) ? preparation : null;
        }

        public async Task<IReadOnlyDictionary<int, PreparationControle>> GetPreparationsAsync(
            int eleveId, IEnumerable<int> controleIds, CancellationToken ct = default)
        {
            var ids = controleIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, PreparationControle>();

            // LE FILTRE SUR L'ÉLÈVE EST DANS LA REQUÊTE, jamais après : un
            // identifiant de contrôle venu de l'URL ne doit pas pouvoir
            // ramener le périmètre de l'enfant d'un autre compte.
            var controles = await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => ids.Contains(c.Id) && c.EleveId == eleveId)
                .Select(c => new
                {
                    c.Id,
                    c.DernierePreparationLe,
                    c.NombrePreparations,
                    c.PretVerdict,
                    c.PretObservation,
                    c.PretLe,
                })
                .ToListAsync(ct);

            if (controles.Count == 0) return new Dictionary<int, PreparationControle>();

            var idsAutorises = controles.Select(c => c.Id).ToList();

            var lignes = await _context.ControlesNotions
                .AsNoTracking()
                .Where(n => idsAutorises.Contains(n.ControleId))
                .Select(n => new
                {
                    n.Id,
                    n.ControleId,
                    n.CompetenceId,
                    n.TravailleeLe,
                    n.Resultat,
                    // Le libellé du référentiel l'emporte : il peut avoir été
                    // corrigé depuis que la notion a été déclarée.
                    Libelle = n.Competence != null ? n.Competence.Libelle! : n.Libelle,
                })
                .ToListAsync(ct);

            var competenceIds = lignes
                .Where(l => l.CompetenceId.HasValue)
                .Select(l => l.CompetenceId!.Value)
                .Distinct()
                .ToList();

            // Une seule requête de maîtrises pour tous les contrôles demandés.
            var maitrises = competenceIds.Count == 0
                ? []
                : await _context.MaitrisesEleves
                    .AsNoTracking()
                    .Where(m => m.EleveId == eleveId && competenceIds.Contains(m.CompetenceId))
                    .Select(m => new { m.CompetenceId, m.Score, m.NombreObservations, m.DerniereEvaluation, m.Source })
                    .ToListAsync(ct);

            var parCompetence = maitrises.ToDictionary(m => m.CompetenceId);
            var maintenant = DateTime.UtcNow;

            var resultat = new Dictionary<int, PreparationControle>();

            foreach (var controle in controles)
            {
                var notions = new List<NotionControle>();

                foreach (var ligne in lignes.Where(l => l.ControleId == controle.Id))
                {
                    // Pas de compétence rattachée, ou aucune observation : rien
                    // n'a été mesuré. C'est l'existence de la ligne de maîtrise
                    // qui tranche, comme dans `GetProgressionAsync`.
                    var mesuree = ligne.CompetenceId.HasValue
                        && parCompetence.TryGetValue(ligne.CompetenceId.Value, out var m)
                            ? m
                            : null;

                    var etatMesure = mesuree is null
                        ? "a-decouvrir"
                        : SeuilsMaitrise.Etat(
                            mesuree.Score, mesuree.NombreObservations, mesuree.DerniereEvaluation, maintenant);

                    notions.Add(new NotionControle
                    {
                        Id = ligne.Id,
                        CompetenceId = ligne.CompetenceId,
                        Libelle = ligne.Libelle,
                        Etat = EtatAffiche(etatMesure, ligne.TravailleeLe),
                        EtatMesure = etatMesure,
                        Pourcent = PourcentNotion(mesuree?.Score, ligne.TravailleeLe),
                        TravailleeLe = ligne.TravailleeLe,
                        Resultat = ligne.Resultat,

                        // Les mêmes sources que le verrou du moteur de
                        // maîtrise (voir MaitriseRepository.EstUneMesure) :
                        // c'est la même distinction, lue au même endroit.
                        ValideeParMesure = mesuree?.Source is "Evaluation" or "Controle",
                    });
                }

                resultat[controle.Id] = Assembler(
                    notions,
                    controle.DernierePreparationLe,
                    controle.NombrePreparations,
                    controle.PretVerdict,
                    controle.PretObservation,
                    controle.PretLe);
            }

            return resultat;
        }

        /// <summary>
        /// L'EFFORT SE VOIT, MAIS NE SE DÉCRÈTE PAS ACQUIS.
        ///
        /// Une notion travaillée en préparation ne peut pas rester présentée
        /// comme « à découvrir » ou « fragile » : l'enfant y a passé du temps,
        /// et une barre qui ne bouge jamais après une vraie séance de révision
        /// est une barre qu'il cesse de regarder. Elle ne monte jamais jusqu'à
        /// « acquise » pour autant — ce verdict-là n'appartient qu'au moteur de
        /// maîtrise, et le score, lui, n'est jamais touché par ce code.
        /// </summary>
        private static string EtatAffiche(string etatMesure, DateTime? travailleeLe) =>
            travailleeLe is not null && Rang(etatMesure) < Rang("en-cours")
                ? "en-cours"
                : etatMesure;

        private static int Rang(string etat) => etat switch
        {
            "acquise" => 3,
            "a-confirmer" => 2,
            "en-cours" => 2,
            "fragile" => 1,
            _ => 0,
        };

        /// <summary>
        /// Le plancher d'une notion travaillée en préparation.
        ///
        /// Même raison que l'état « en cours » forcé : une séance passée
        /// dessus doit se voir sur la barre, sinon l'enfant a l'impression de
        /// n'avoir rien fait. C'est un plancher, pas une addition — une
        /// notion déjà mesurée plus haut ne redescend jamais ici.
        /// </summary>
        private const int PlancherTravaillee = 25;

        /// <summary>
        /// Où en est UNE notion, de 0 à 100 : le score du moteur de maîtrise,
        /// ramené sur cent.
        ///
        /// LE SCORE BRUT PLUTÔT QU'UN PALIER PAR ÉTAT. Quatre valeurs
        /// possibles (0 / 25 / 50 / 100) auraient donné quatre barres
        /// identiques d'une notion à l'autre, et rien vu bouger entre deux
        /// séances. Ici la barre avance vraiment au rythme des observations.
        /// </summary>
        private static int PourcentNotion(double? score, DateTime? travailleeLe)
        {
            var mesure = score is double valeur ? (int)Math.Round(valeur * 100) : 0;

            return travailleeLe is not null && mesure < PlancherTravaillee
                ? PlancherTravaillee
                : Math.Clamp(mesure, 0, 100);
        }

        private static PreparationControle Assembler(
            List<NotionControle> notions,
            DateTime? dernierePreparationLe,
            int nombrePreparations,
            string? pretVerdict,
            string? pretObservation,
            DateTime? pretLe)
        {
            // « A-T-IL COMMENCÉ À RÉVISER ? » EST UN FAIT, PAS UN JUGEMENT.
            //
            // Deux preuves, et l'une OU l'autre suffit : avoir ouvert une
            // séance pour préparer ce contrôle, ou avoir travaillé l'une de ses
            // notions — ce qui arrive aussi en cours ordinaire, sans que
            // personne n'ait cliqué « préparer ce contrôle ». Ne retenir que la
            // première afficherait « pas encore commencé » à un enfant qui a
            // travaillé la moitié du programme.
            var revisionCommencee = nombrePreparations > 0
                || notions.Any(n => n.TravailleeLe is not null);

            var statut = PretControle.Statut(pretVerdict, revisionCommencee, perimetreConnu: notions.Count > 0);
            // PÉRIMÈTRE INCONNU : LA BARRE EXISTE QUAND MÊME, À ZÉRO.
            //
            // Voulu par Camara le 13/09/2026, contre le choix précédent de ne
            // rien afficher du tout. Une jauge vide dit « il te reste tout à
            // faire », là où l'absence de jauge ne disait rien et laissait
            // croire que la mécanique ne marchait pas. `PerimetreConnu` reste
            // là pour que l'écran puisse ajouter, à côté, que le programme
            // n'est pas encore connu.
            if (notions.Count == 0)
            {
                return new PreparationControle
                {
                    Pourcent = 0,
                    PerimetreConnu = false,
                    Total = 0,
                    Acquises = 0,
                    DernierePreparationLe = dernierePreparationLe,
                    Notions = notions,
                    PretStatut = statut,
                    PretObservation = pretObservation,
                    PretLe = pretLe,
                };
            }

            // LA MOYENNE DES BARRES, PAS UNE PONDÉRATION À PART. La fiche
            // montre une barre par notion ; si le global se calculait
            // autrement, on lirait « 30 % » au-dessus de quatre notions à
            // 50 %, et personne ne saurait laquelle croire.
            return new PreparationControle
            {
                Pourcent = (int)Math.Round(notions.Average(n => (double)n.Pourcent)),
                PerimetreConnu = true,
                Total = notions.Count,
                Acquises = notions.Count(n => n.Etat == "acquise"),
                DernierePreparationLe = dernierePreparationLe,
                Notions = notions,
                PretStatut = statut,
                PretObservation = pretObservation,
                PretLe = pretLe,
            };
        }

        private static readonly System.Linq.Expressions.Expression<Func<ControleScolaire, ControleScolaireEleve>> Projection =
            c => new ControleScolaireEleve
            {
                Id = c.Id,
                MatiereId = c.MatiereId,
                MatiereLibelle = c.Matiere!.Libelle,
                ProfCouleur = c.Matiere.ProfCouleur,
                Sujet = c.Sujet,
                DateControle = c.DateControle,
                HeureControle = c.HeureControle,
                DateCreation = c.DateCreation,
                DernierePreparationLe = c.DernierePreparationLe,
                NombrePreparations = c.NombrePreparations,
                Note = c.Note,
                Ressenti = c.Ressenti,
                BilanLe = c.BilanLe,
                BilanClos = c.BilanLe != null || c.RelancesBilan >= ControleScolaire.RelancesMaximum,
                CopieSeparee = c.CopieSeparee,
                CopieDemandeeLe = c.CopieDemandeeLe,
                EnonceRecu = c.EnoncePieceJointeId != null,
                CopieRecue = c.CopiePieceJointeId != null,
                CopieAnalyseeLe = c.CopieAnalyseeLe,
            };

        public async Task<int> AjouterNotionsAsync(
            int eleveId, int controleId, int matiereId,
            IEnumerable<NotionDeclaree> notions, bool travaillee,
            CancellationToken ct = default)
        {
            // Le contrôle doit être à cet élève ET dans cette matière : poser
            // le périmètre d'une préparation est un acte pédagogique, un
            // professeur ne le fait que dans sa propre matière.
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return 0;

            var existantes = await _context.ControlesNotions
                .Where(n => n.ControleId == controleId)
                .ToListAsync(ct);

            var maintenant = DateTime.UtcNow;
            var touchees = 0;

            foreach (var notion in notions)
            {
                var libelle = Tronquer(notion.Libelle, 200);
                if (libelle is null) continue;

                // Rapprochement par compétence quand elle est connue, par
                // libellé normalisé sinon — une contrainte unique sur du texte
                // libre serait ingérable (casse, accents, ponctuation).
                var deja = notion.CompetenceId.HasValue
                    ? existantes.FirstOrDefault(n => n.CompetenceId == notion.CompetenceId)
                    : existantes.FirstOrDefault(n => n.CompetenceId == null && Cle(n.Libelle) == Cle(libelle));

                if (deja is not null)
                {
                    if (travaillee) deja.TravailleeLe = maintenant;
                    touchees++;
                    continue;
                }

                var ligne = new ControleNotion
                {
                    ControleId = controleId,
                    CompetenceId = notion.CompetenceId,
                    Libelle = libelle,
                    TravailleeLe = travaillee ? maintenant : null,
                    Source = travaillee ? "PREPARATION" : "DECLAREE",
                    DateCreation = maintenant,
                };

                _context.ControlesNotions.Add(ligne);
                existantes.Add(ligne);
                touchees++;
            }

            if (touchees > 0) await _context.SaveChangesAsync(ct);

            return touchees;
        }

        public async Task<bool> PreciserSujetAsync(
            int eleveId, int controleId, int matiereId, string sujet,
            CancellationToken ct = default)
        {
            var propre = Tronquer(sujet, 300);
            if (propre is null) return false;

            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            // On écrase sans état d'âme : ce que le professeur a obtenu de
            // l'élève en lui posant la question vaut mieux que ce qui avait
            // été tapé à la va-vite dans un formulaire.
            controle.Sujet = propre;
            controle.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> MarquerNotionsTravailleesAsync(
            int eleveId, int matiereId, IEnumerable<string> codesCompetences,
            DateTime maintenant, CancellationToken ct = default)
        {
            var codes = codesCompetences.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            if (codes.Count == 0) return 0;

            // Les contrôles encore à venir seulement : repasser sur un contrôle
            // déjà passé ne préparerait plus rien.
            var lignes = await _context.ControlesNotions
                .Where(n => n.Competence != null
                    && n.Competence.Code != null
                    && codes.Contains(n.Competence.Code)
                    && n.Controle!.EleveId == eleveId
                    && n.Controle.MatiereId == matiereId
                    && n.Controle.DateControle >= maintenant.Date)
                .ToListAsync(ct);

            if (lignes.Count == 0) return 0;

            foreach (var ligne in lignes) ligne.TravailleeLe = maintenant;

            await _context.SaveChangesAsync(ct);
            return lignes.Count;
        }

        public async Task<bool> MarquerPreparationAsync(
            int eleveId, int controleId, DateTime maintenant, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            controle.DernierePreparationLe = maintenant;
            controle.NombrePreparations += 1;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ControleScolaireEleve?> ModifierAsync(
            int eleveId, int controleId, string? sujet,
            DateTime dateControle, TimeSpan? heureControle, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return null;

            var nouveauSujet = Tronquer(sujet, 300);
            var sujetChange = !string.Equals(controle.Sujet, nouveauSujet, StringComparison.Ordinal);

            // LA MATIÈRE NE BOUGE PAS : elle n'est même plus un paramètre de
            // cette méthode. Un appel qui en enverrait une autre — un client
            // modifié, une requête forgée — ne peut rien déplacer.
            if (sujetChange)
            {
                // CHANGER DE SUJET NE JETTE QUE CE QUI N'A PAS SERVI.
                //
                // « Thalès » devient « Pythagore » : les notions déduites de
                // l'ancien sujet n'ont plus lieu d'être, et les laisser
                // fausserait la barre. Mais celles que l'élève a RÉELLEMENT
                // travaillées, ou que sa copie corrigée a jugées, sont des
                // faits — on n'efface pas du travail parce qu'un libellé a
                // changé. Elles restent, le reste part, et le programme se
                // redéduit du nouveau sujet.
                var jamaisServies = await _context.ControlesNotions
                    .Where(n => n.ControleId == controleId
                        && n.TravailleeLe == null
                        && n.Resultat == null)
                    .ToListAsync(ct);

                _context.ControlesNotions.RemoveRange(jamaisServies);
            }

            controle.Sujet = nouveauSujet;
            controle.DateControle = dateControle.Date;
            controle.HeureControle = heureControle;
            controle.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.Id == controleId)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<bool> SupprimerAsync(
            int eleveId, int controleId, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            // Suppression dure, contrairement aux dictées et aux évaluations :
            // un contrôle n'est pas une trace pédagogique, c'est une ligne
            // d'agenda — et une ligne d'agenda qu'on efface doit disparaître.
            // Le périmètre part en cascade (FK vérifiée en base).
            _context.ControlesScolaires.Remove(controle);

            await EffacerBlocsAsync(eleveId, controleId, ct);

            await _context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        /// Retire des messages déjà écrits les blocs qui désignent ce contrôle.
        ///
        /// SANS ÇA, LE CONTRÔLE SURVIT DANS LA MÉMOIRE DU PROFESSEUR. Les
        /// blocs [CONTROLE_NOTIONS] et [CONTROLE_RESULTAT] restent dans les
        /// messages persistés, qui sont relus à chaque tour : le professeur y
        /// lirait « controle: 42 » et pourrait écrire dans le programme d'un
        /// contrôle qui n'existe plus — ou pire, en reparler à l'élève qui l'a
        /// justement effacé.
        ///
        /// Ne touche QUE les blocs techniques, jamais la prose. Une phrase
        /// comme « on prépare ton contrôle de vendredi » reste dans
        /// l'historique : la réécrire reviendrait à falsifier ce que le
        /// professeur a dit, et le remède serait pire que le mal. Elle sort
        /// d'elle-même de la fenêtre d'historique au fil des séances.
        /// </summary>
        private async Task EffacerBlocsAsync(int eleveId, int controleId, CancellationToken ct)
        {
            // On ne balaie pas toute la base : seuls les messages qui portent
            // le numéro peuvent le citer.
            var marqueur = $"controle: {controleId}";

            var messages = await _context.Messages
                .Where(m => m.Conversation!.EleveId == eleveId && m.Contenu != null
                    && m.Contenu.Contains(marqueur))
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                var nettoye = RetirerBlocsDuControle(message.Contenu!, controleId);
                if (nettoye != message.Contenu) message.Contenu = nettoye;
            }
        }

        /// <summary>
        /// Retire les blocs [CONTROLE_NOTIONS] et [CONTROLE_RESULTAT] dont le
        /// champ `controle:` porte ce numéro. Les autres blocs, y compris ceux
        /// d'un AUTRE contrôle dans le même message, sont laissés intacts.
        /// </summary>
        internal static string RetirerBlocsDuControle(string contenu, int controleId)
        {
            foreach (var nom in new[] { "CONTROLE_NOTIONS", "CONTROLE_RESULTAT" })
            {
                var motif = new Regex(
                    $@"\[{nom}\](?:(?!\[/{nom}\]).)*?controle\s*:\s*{controleId}\b(?:(?!\[/{nom}\]).)*\[/{nom}\]",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);

                contenu = motif.Replace(contenu, string.Empty);
            }

            return contenu;
        }

        public async Task<ControleScolaireEleve?> GetADebrieferAsync(
            int eleveId, int matiereId, DateTime maintenant, CancellationToken ct = default)
        {
            // Le plus récent des contrôles passés non débriefés : c'est celui
            // dont l'élève se souvient, et celui dont la note vient d'arriver.
            //
            // LE PLAFOND DE RELANCES EST DANS LA REQUÊTE, pas dans une
            // consigne : au-delà, le contrôle ne remonte plus, quoi que fasse
            // le modèle. Un enfant qui n'a pas voulu en parler deux fois n'a
            // pas à se justifier une troisième.
            var requete = _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId
                    && c.MatiereId == matiereId
                    && c.BilanLe == null
                    && c.RelancesBilan < ControleScolaire.RelancesMaximum);

            return await Passes(requete, maintenant)
                .OrderByDescending(c => c.DateControle)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<ControleScolaireEleve?> GetDernierPasseAsync(
            int eleveId, int matiereId, DateTime maintenant, int joursMaximum,
            CancellationToken ct = default)
        {
            var depuis = maintenant.Date.AddDays(-joursMaximum);

            var requete = _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId
                    && c.MatiereId == matiereId
                    && c.DateControle >= depuis);

            return await Passes(requete, maintenant)
                .OrderByDescending(c => c.DateControle)
                .ThenByDescending(c => c.HeureControle)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>
        /// PASSÉ = LA VEILLE OU AVANT… OU AUJOURD'HUI, SI SON HEURE EST CONNUE
        /// ET QU'IL EST TERMINÉ. Une seule définition, pour le bilan et pour la
        /// copie. Relevé par Camara le 13/09/2026 : « si l'heure est indiquée,
        /// le professeur ne doit pas me poser la question tant que l'heure
        /// n'est pas passée ». On attend même la fin supposée — un contrôle de
        /// 18 h n'est pas fini à 18 h 01.
        ///
        /// Avant une heure du matin, la soustraction serait négative, et une
        /// colonne `time` ne sait pas comparer à une durée négative : la
        /// branche « aujourd'hui » ne peut alors rien trouver, on s'en passe.
        /// </summary>
        private static IQueryable<ControleScolaire> Passes(IQueryable<ControleScolaire> requete, DateTime maintenant)
        {
            var jour = maintenant.Date;
            var demain = jour.AddDays(1);

            if (maintenant.TimeOfDay < ControleScolaire.DureeSupposee)
            {
                return requete.Where(c => c.DateControle < jour);
            }

            var finAuPlusTard = maintenant.TimeOfDay - ControleScolaire.DureeSupposee;

            return requete.Where(c => c.DateControle < jour
                || (c.DateControle < demain
                    && c.HeureControle != null
                    && c.HeureControle <= finAuPlusTard));
        }

        public async Task<bool> MarquerRelanceBilanAsync(
            int eleveId, int controleId, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            controle.RelancesBilan += 1;
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<ControleScolaireEleve?> GetPreparationSansVerdictAsync(
            int eleveId, int matiereId, DateTime debut, DateTime fin,
            CancellationToken ct = default)
        {
            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId
                    && c.MatiereId == matiereId

                    // Un contrôle déjà passé n'a plus de préparation à
                    // conclure : ce qui l'attend, c'est son bilan.
                    && c.DateControle >= fin.Date

                    // AUCUNE CONDITION DE PRÉPARATION, ET C'EST VOULU — décidé
                    // avec Camara le 13/09/2026 : « le professeur sait de lui-
                    // même si ce qu'il fait avec l'élève concerne un contrôle ».
                    //
                    // La requête exigeait une trace mécanique — le bouton
                    // « Préparer ce contrôle » cliqué pendant la séance, ou une
                    // notion marquée travaillée. Or l'élève entre le plus
                    // souvent par son cours ordinaire, et la marque des notions
                    // est posée APRÈS ce filet, par l'observateur. Une séance
                    // qui révisait la réciproque de Thalès, au programme du
                    // contrôle du soir, passait donc pour « sans rapport ».
                    //
                    // Le juge est le professeur, à qui le filet soumet la
                    // transcription ET le programme : c'est lui qui répond
                    // « préparation » ou « rien à voir ». Le prix : un appel
                    // par séance dans une matière où un contrôle approche —
                    // c'est le cas où l'on veut justement ne rien rater.

                    // Laissé sans conclusion. Un verdict rendu pendant la
                    // séance suffit : le professeur a fait son travail, on ne
                    // repasse pas derrière lui.
                    && (c.PretLe == null || c.PretLe < debut))

                // Le plus proche d'abord : c'est celui qui presse.
                .OrderBy(c => c.DateControle)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> PoserVerdictPretAsync(
            int eleveId, int controleId, int matiereId,
            string verdict, string? observation, DateTime maintenant,
            CancellationToken ct = default)
        {
            var valide = PretControle.VerdictValide(verdict);
            if (valide is null) return false;

            // LA MATIÈRE EST DANS LA REQUÊTE, comme pour le résultat : juger
            // de la préparation est un acte pédagogique, et un professeur de
            // français n'a pas à déclarer un enfant prêt pour son contrôle de
            // maths. Le cloisonnement est strict ici, contrairement à la simple
            // prise de date.
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId
                    && c.EleveId == eleveId
                    && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            // LE PLAFOND, À L'ÉCRITURE — voir PretControle.VerdictPlafonne.
            // Un « bientôt prêt » posé sans programme ni préparation est
            // enregistré « pas prêt » : sinon il resterait en base, et
            // ressurgirait dès la première notion travaillée.
            var travaillees = await _context.ControlesNotions
                .Where(n => n.ControleId == controleId)
                .Select(n => n.TravailleeLe)
                .ToListAsync(ct);

            var revisionCommencee = controle.NombrePreparations > 0 || travaillees.Any(t => t != null);

            // LE DERNIER VERDICT REMPLACE LE PRÉCÉDENT, il ne s'y ajoute pas :
            // ce qui compte est où l'élève en est AUJOURD'HUI. La date suit,
            // pour que la fiche puisse dire quand ça a été jugé.
            controle.PretVerdict = PretControle.VerdictPlafonne(
                valide, revisionCommencee, perimetreConnu: travaillees.Count > 0);
            // 1 000 et non plus 400 : la justification passe désormais en revue
            // TOUTES les notions du programme et les difficultés liées. À 400,
            // elle était coupée net, en plein mot, sur la dernière notion —
            // souvent celle qui explique le verdict. Voir la migration
            // `ObservationPretAllongee`.
            // AUCUNE LIMITE depuis le 14/09/2026 (migration `ObservationPretParagraphes`)
            // — Camara : « si le prof fait un jour une énorme justification, il ne
            // doit pas être bloqué ». Tronquer coupait en plein mot.
            controle.PretObservation = string.IsNullOrWhiteSpace(observation) ? null : observation.Trim();
            controle.PretLe = maintenant;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ResultatEnregistre?> EnregistrerResultatAsync(
            int eleveId, int controleId, int matiereId,
            double? note, string? ressenti,
            IEnumerable<NotionDeclaree> reussies, IEnumerable<NotionDeclaree> ratees,
            DateTime maintenant, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return null;

            // La note ne s'efface jamais par un second passage : un contrôle
            // débriefé à l'oral puis repris en photo trois semaines plus tard
            // apporte la note à la seconde visite, et ne doit pas perdre ce
            // qu'il avait déjà.
            if (note is double valeur) controle.Note = Math.Clamp(valeur, 0, 20);
            if (Tronquer(ressenti, 500) is string texte) controle.Ressenti = texte;

            controle.BilanLe = maintenant;

            var lignes = await _context.ControlesNotions
                .Where(n => n.ControleId == controleId)
                .ToListAsync(ct);

            var codesReussis = await AppliquerResultatAsync(
                controleId, lignes, reussies, "reussie", maintenant, ct);

            var codesRates = await AppliquerResultatAsync(
                controleId, lignes, ratees, "ratee", maintenant, ct);

            await _context.SaveChangesAsync(ct);

            return new ResultatEnregistre(codesReussis, codesRates);
        }

        /// <summary>
        /// Marque des notions du périmètre, et crée celles que le contrôle a
        /// révélées — une notion ratée dont personne n'avait parlé avant est
        /// justement celle qu'il faut retenir.
        /// </summary>
        private async Task<List<string>> AppliquerResultatAsync(
            int controleId, List<ControleNotion> lignes, IEnumerable<NotionDeclaree> notions,
            string resultat, DateTime maintenant, CancellationToken ct)
        {
            var codes = new List<string>();

            foreach (var notion in notions)
            {
                var libelle = Tronquer(notion.Libelle, 200);
                if (libelle is null) continue;

                var ligne = notion.CompetenceId.HasValue
                    ? lignes.FirstOrDefault(n => n.CompetenceId == notion.CompetenceId)
                    : lignes.FirstOrDefault(n => n.CompetenceId == null && Cle(n.Libelle) == Cle(libelle));

                if (ligne is null)
                {
                    ligne = new ControleNotion
                    {
                        ControleId = controleId,
                        CompetenceId = notion.CompetenceId,
                        Libelle = libelle,
                        Source = "RESULTAT",
                        DateCreation = maintenant,
                    };

                    _context.ControlesNotions.Add(ligne);
                    lignes.Add(ligne);
                }

                ligne.Resultat = resultat;

                if (notion.CompetenceId.HasValue)
                {
                    var code = await _context.Competences
                        .AsNoTracking()
                        .Where(c => c.Id == notion.CompetenceId.Value)
                        .Select(c => c.Code)
                        .FirstOrDefaultAsync(ct);

                    if (!string.IsNullOrWhiteSpace(code)) codes.Add(code);
                }
            }

            return codes;
        }

        public async Task<bool> OuvrirCopieAsync(
            int eleveId, int controleId, int matiereId, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            // UNE NOUVELLE DEMANDE REPART DE ZÉRO : la question sera reposée,
            // et rien de ce qui a été reçu la dernière fois ne compte. Sans ça,
            // une copie déjà regardée hier ferait sauter la question et
            // afficherait « tout est reçu » avant le moindre envoi.
            controle.CopieSeparee = null;
            controle.CopieDemandeeLe = null;
            controle.EnoncePieceJointeId = null;
            controle.CopiePieceJointeId = null;
            controle.CopieAnalyseeLe = null;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> PoserChoixCopieAsync(
            int eleveId, int controleId, int matiereId, bool separee, DateTime maintenant,
            CancellationToken ct = default)
        {
            // Cloisonné sur la matière : regarder une copie avec l'élève est
            // un acte pédagogique, comme poser son résultat.
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            controle.CopieSeparee = separee;
            controle.CopieDemandeeLe = maintenant;
            controle.CopieAnalyseeLe = null;

            // Tout sur la même feuille : il n'y a pas d'énoncé à attendre à
            // part. Un énoncé reçu avant qu'il change d'avis ne compte plus.
            if (!separee) controle.EnoncePieceJointeId = null;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RecevoirPieceCopieAsync(
            int eleveId, int controleId, int matiereId, bool estEnonce, int pieceJointeId,
            DateTime maintenant, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            if (estEnonce)
            {
                controle.EnoncePieceJointeId = pieceJointeId;

                // Un énoncé envoyé à part PROUVE que les feuilles sont
                // séparées, même si la question n'a pas été répondue.
                controle.CopieSeparee = true;
            }
            else
            {
                controle.CopiePieceJointeId = pieceJointeId;
            }

            controle.CopieDemandeeLe = maintenant;

            // Une pièce qui arrive rouvre l'analyse : c'est une nouvelle
            // version de ce qu'il y a à regarder.
            controle.CopieAnalyseeLe = null;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ControleScolaireEleve?> GetCopieEnCoursAsync(
            int eleveId, int matiereId, DateTime depuis, CancellationToken ct = default)
        {
            return await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId
                    && c.MatiereId == matiereId
                    && c.CopieSeparee != null
                    && c.CopieAnalyseeLe == null
                    && c.CopieDemandeeLe >= depuis)
                .OrderByDescending(c => c.CopieDemandeeLe)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> MarquerCopieAnalyseeAsync(
            int eleveId, int controleId, DateTime maintenant, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId)
                .FirstOrDefaultAsync(ct);

            if (controle is null || controle.CopieSeparee is null) return false;

            // LA MÊME RÈGLE QUE `ControleScolaireEleve.CopieComplete` : une
            // analyse ne se clôt pas sur des pièces incomplètes. Un
            // [CONTROLE_RESULTAT] écrit au récit, avant la copie, ne doit pas
            // éteindre le rappel qui attend encore la feuille.
            var complete = controle.CopiePieceJointeId != null
                && (controle.CopieSeparee == false || controle.EnoncePieceJointeId != null);

            if (!complete) return false;

            controle.CopieAnalyseeLe = maintenant;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }

        /// <summary>
        /// La clé de dédoublonnage d'un libellé libre : sans accent, sans
        /// casse, sans ponctuation de bord. « Les fractions » et « fractions »
        /// restent deux notions distinctes — on ne devine pas.
        /// </summary>
        private static string Cle(string libelle)
        {
            var sansAccent = string.Concat(libelle.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

            return sansAccent.Trim().ToLowerInvariant();
        }
    }
}
