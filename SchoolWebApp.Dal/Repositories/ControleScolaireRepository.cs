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
                    n.EtatProfesseur,
                    n.EtatProfesseurLe,
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
                var duControle = lignes.Where(l => l.ControleId == controle.Id).ToList();

                // UN DOUBLON NE COMPTE JAMAIS DANS LA BARRE — voir
                // LigneDeLaNotion, qui les empêche de naître. Ceux d'avant le
                // 13/10/2026 (ou un libellé du référentiel corrigé après coup)
                // sont écartés ici : une ligne en texte libre qui dit la même
                // chose qu'une ligne rattachée n'apporte que son plancher.
                var rattachees = duControle
                    .Where(l => l.CompetenceId.HasValue)
                    .Select(l => LibelleNotion.CleSouple(l.Libelle))
                    .ToHashSet();

                var affichees = duControle
                    .Where(l => l.CompetenceId.HasValue || !rattachees.Contains(LibelleNotion.CleSouple(l.Libelle)))
                    .ToList();

                // QUAND LA LIGNE `notions:` MANQUE, L'OBSERVATION LA REMPLACE —
                // voir ObservationPret. Verdicts d'avant le 13/10/2026, ou
                // ligne oubliée : l'état de chaque notion est lu dans le
                // paragraphe que le professeur lui consacre, daté du verdict.
                var deduits = controle.PretLe is null
                    ? new Dictionary<int, string>()
                    : EtatsDeduits(controle.PretObservation, affichees.Select(l => (l.Id, l.Libelle)).ToList());

                foreach (var ligne in affichees)
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

                    // Les mêmes sources que le verrou du moteur de maîtrise
                    // (voir MaitriseRepository.EstUneMesure) : c'est la même
                    // distinction, lue au même endroit.
                    var valideeParMesure = mesuree?.Source is "Evaluation" or "Controle";

                    var etat = EtatAffiche(etatMesure, ligne.TravailleeLe);
                    var pourcent = PourcentNotion(mesuree?.Score, ligne.TravailleeLe);

                    // LE PROFESSEUR L'EMPORTE, sauf devant une séance
                    // postérieure — Camara, le 13/10/2026. Son état n'est pas
                    // un score : la fiche de l'élève garde la mesure. Mais la
                    // barre de CE contrôle dit ce qu'il a dit, tant qu'aucune
                    // séance plus récente ne l'a contredit (voir
                    // PretControle.MemeSeance). Une copie notée garde le
                    // dessus : elle ne descend jamais sur son mot.
                    // L'état écrit dans la ligne `notions:` d'abord ; celui lu
                    // dans l'observation s'il est le seul, ou s'il vient d'un
                    // verdict plus récent que l'état écrit.
                    var jugement = ligne.EtatProfesseur;
                    var jugeLe = ligne.EtatProfesseurLe;

                    if (deduits.TryGetValue(ligne.Id, out var lu)
                        && (jugement is null || controle.PretLe > jugeLe))
                    {
                        jugement = lu;
                        jugeLe = controle.PretLe;
                    }

                    if (jugement is not null
                        && jugeLe is DateTime le
                        && PretControle.ProfesseurPrevaut(le, mesuree?.DerniereEvaluation))
                    {
                        var pourcentProfesseur = PretControle.PourcentEtat(jugement);

                        if (!valideeParMesure || pourcentProfesseur >= pourcent)
                        {
                            pourcent = pourcentProfesseur;
                            etat = PretControle.EtatAfficheDe(jugement);
                        }
                    }

                    notions.Add(new NotionControle
                    {
                        Id = ligne.Id,
                        CompetenceId = ligne.CompetenceId,
                        Libelle = ligne.Libelle,
                        Etat = etat,
                        EtatMesure = etatMesure,
                        Pourcent = pourcent,
                        TravailleeLe = ligne.TravailleeLe,
                        Resultat = ligne.Resultat,
                        EtatProfesseur = jugement,
                        EtatProfesseurLe = jugement is null ? null : jugeLe,
                        ValideeParMesure = valideeParMesure,
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
                StyleCorrection = c.StyleCorrection,
                StyleCorrectionLe = c.StyleCorrectionLe,
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
                .Include(n => n.Competence)
                .Where(n => n.ControleId == controleId)
                .ToListAsync(ct);

            var maintenant = DateTime.UtcNow;
            var touchees = 0;

            foreach (var notion in notions)
            {
                var libelle = Tronquer(notion.Libelle, 200);
                if (libelle is null) continue;

                var deja = LigneDeLaNotion(existantes, notion);

                if (deja is not null)
                {
                    Rattacher(deja, notion, libelle);
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

        public async Task<int> AllegerControlesPassesAsync(
            DateTime maintenant, int lot, CancellationToken ct = default)
        {
            // UN MOIS APRÈS LA DATE, DEUX SI LE BILAN N'EST PAS FAIT : une copie
            // rendue trois ou cinq semaines plus tard doit encore pouvoir être
            // analysée avec ses notions. « Fait » = bilan posé, ou relances
            // épuisées (le contrôle ne remonte alors plus jamais).
            var unMois = maintenant.Date.AddMonths(-1);
            var deuxMois = maintenant.Date.AddMonths(-2);

            var ids = await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => (c.DateControle < deuxMois
                        || (c.DateControle < unMois
                            && (c.BilanLe != null || c.RelancesBilan >= ControleScolaire.RelancesMaximum)))
                    && (c.PretObservation != null
                        || c.StyleCorrection != null
                        || _context.ControlesDocuments.Any(d => d.ControleId == c.Id)
                        || _context.ControlesNotions.Any(n => n.ControleId == c.Id)))
                .OrderBy(c => c.DateControle)
                .Select(c => c.Id)
                .Take(lot)
                .ToListAsync(ct);

            if (ids.Count == 0) return 0;

            // Le score de maîtrise de chaque notion vit dans `MaitriseEleve` :
            // le détail par notion du contrôle peut partir sans qu'aucun score
            // ne bouge.
            await _context.ControlesDocuments.Where(d => ids.Contains(d.ControleId)).ExecuteDeleteAsync(ct);
            await _context.ControlesNotions.Where(n => ids.Contains(n.ControleId)).ExecuteDeleteAsync(ct);
            await _context.ControlesScolaires
                .Where(c => ids.Contains(c.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.PretObservation, (string?)null)
                    .SetProperty(c => c.StyleCorrection, (string?)null), ct);

            return ids.Count;
        }

        public async Task<int> SupprimerControlesAnneesPasseesAsync(
            DateTime maintenant, int lot, CancellationToken ct = default)
        {
            var debutAnnee = new DateTime(AnneeScolaire.Debut(maintenant), 8, 1);

            var aSupprimer = await _context.ControlesScolaires
                .AsNoTracking()
                .Where(c => c.DateControle < debutAnnee)
                .OrderBy(c => c.DateControle)
                .Select(c => new { c.Id, c.EleveId })
                .Take(lot)
                .ToListAsync(ct);

            var supprimes = 0;
            foreach (var c in aSupprimer)
            {
                // Le même chemin que la suppression à la main : cascade des
                // notions et documents, et blocs retirés des messages.
                if (await SupprimerAsync(c.EleveId, c.Id, ct)) supprimes++;
            }

            return supprimes;
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
            IReadOnlyList<EtatNotionDeclare>? notions = null,
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

            if (notions is { Count: > 0 })
            {
                await PoserEtatsDuProfesseurAsync(controleId, notions, maintenant, ct);
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// L'état que le professeur donne à chaque notion avec son verdict —
        /// Camara, le 13/10/2026. Rapproché par libellé : le nom du
        /// référentiel ou le texte libre, à l'accent et à la casse près ; à
        /// défaut, l'un contenu dans l'autre, si une seule notion répond. Ce
        /// qui ne se rapproche pas n'écrit rien : poser « tenue » sur la
        /// mauvaise notion serait pire que de laisser la barre.
        /// </summary>
        private async Task PoserEtatsDuProfesseurAsync(
            int controleId, IReadOnlyList<EtatNotionDeclare> notions, DateTime maintenant, CancellationToken ct)
        {
            var lignes = await _context.ControlesNotions
                .Include(n => n.Competence)
                .Where(n => n.ControleId == controleId)
                .ToListAsync(ct);

            if (lignes.Count == 0) return;

            foreach (var notion in notions)
            {
                var etat = PretControle.EtatNotionValide(notion.Etat);
                if (etat is null) continue;

                var cle = LibelleNotion.CleSouple(notion.Libelle);
                if (cle.Length == 0) continue;

                var ligne = lignes.FirstOrDefault(
                    n => LibelleNotion.CleSouple(n.Competence?.Libelle ?? n.Libelle) == cle);

                if (ligne is null)
                {
                    var proches = lignes
                        .Where(n =>
                        {
                            var sienne = LibelleNotion.CleSouple(n.Competence?.Libelle ?? n.Libelle);
                            return sienne.Contains(cle) || cle.Contains(sienne);
                        })
                        .ToList();

                    if (proches.Count == 1) ligne = proches[0];
                }

                if (ligne is null) continue;

                ligne.EtatProfesseur = etat;
                ligne.EtatProfesseurLe = maintenant;
            }
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
                .Include(n => n.Competence)
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

                var ligne = LigneDeLaNotion(lignes, notion);

                if (ligne is not null)
                {
                    Rattacher(ligne, notion, libelle);
                }
                else
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
            int eleveId, int controleId, int matiereId, DateTime depuis, CancellationToken ct = default)
        {
            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            // UNE COPIE ENCORE EN COURS NE REPART PAS DE ZÉRO — relevé par
            // Camara le 26/09/2026 : l'énoncé reçu, le professeur réclamait la
            // copie en réécrivant le bloc, la question « séparés ? » revenait
            // et l'énoncé était oublié. Trois fois de suite.
            //
            // « En cours » = une réponse ou une pièce récente, et pas tout
            // reçu. Une copie COMPLÈTE redemandée, elle, repart bien de zéro :
            // c'est « on la regarde encore une fois ».
            var complete = controle.CopiePieceJointeId != null
                && (controle.CopieSeparee == false || controle.EnoncePieceJointeId != null);

            if (controle.CopieSeparee != null && controle.CopieDemandeeLe >= depuis
                && controle.CopieAnalyseeLe == null && !complete)
            {
                return true;
            }

            // UNE NOUVELLE DEMANDE REPART DE ZÉRO : la question sera reposée,
            // et rien de ce qui a été reçu la dernière fois ne compte. Sans ça,
            // une copie déjà regardée hier ferait sauter la question et
            // afficherait « tout est reçu » avant le moindre envoi.
            controle.CopieSeparee = null;
            controle.CopieDemandeeLe = null;
            controle.EnoncePieceJointeId = null;
            controle.CopiePieceJointeId = null;
            controle.CopieAnalyseeLe = null;

            // Le choix « je me corrige seul / corrige-moi » vaut pour UNE
            // correction : une nouvelle demande le repose.
            controle.StyleCorrection = null;
            controle.StyleCorrectionLe = null;

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

        public async Task<bool> PoserStyleCorrectionAsync(
            int eleveId, int controleId, int matiereId, string style, DateTime maintenant,
            CancellationToken ct = default)
        {
            var normalise = StylesCorrection.Normaliser(style);
            if (normalise is null) return false;

            var controle = await _context.ControlesScolaires
                .Where(c => c.Id == controleId && c.EleveId == eleveId && c.MatiereId == matiereId)
                .FirstOrDefaultAsync(ct);

            if (controle is null) return false;

            controle.StyleCorrection = normalise;
            controle.StyleCorrectionLe = maintenant;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DocumentControle?> AjouterDocumentAsync(
            int eleveId, int controleId, DocumentControle document, CancellationToken ct = default)
        {
            var sien = await _context.ControlesScolaires
                .AnyAsync(c => c.Id == controleId && c.EleveId == eleveId, ct);

            if (!sien) return null;

            var deja = await _context.ControlesDocuments.CountAsync(d => d.ControleId == controleId, ct);
            if (deja >= DocumentControle.Maximum) return null;

            var ligne = new ControleDocument
            {
                ControleId = controleId,
                NomFichier = document.NomFichier,
                TypeMime = document.TypeMime,
                Taille = document.Taille,
                NombrePages = document.NombrePages,
                Donnees = document.Donnees,
                DateCreation = document.DateCreation,
            };

            _context.ControlesDocuments.Add(ligne);
            await _context.SaveChangesAsync(ct);

            document.Id = ligne.Id;
            document.ControleId = controleId;
            return document;
        }

        public async Task<IReadOnlyList<DocumentControle>> GetDocumentsAsync(
            int eleveId, int controleId, CancellationToken ct = default)
        {
            // Les octets ne sont JAMAIS lus ici : cette liste s'affiche, elle
            // ne transcrit rien.
            return await _context.ControlesDocuments
                .AsNoTracking()
                .Where(d => d.ControleId == controleId && d.Controle!.EleveId == eleveId)
                .OrderBy(d => d.DateCreation)
                .Select(d => new DocumentControle
                {
                    Id = d.Id,
                    ControleId = d.ControleId,
                    NomFichier = d.NomFichier,
                    TypeMime = d.TypeMime,
                    Taille = d.Taille,
                    NombrePages = d.NombrePages,
                    Transcription = d.Transcription == null ? null : string.Empty,
                    DateCreation = d.DateCreation,
                })
                .ToListAsync(ct);
        }

        public async Task<bool> SupprimerDocumentAsync(
            int eleveId, int controleId, int documentId, CancellationToken ct = default)
        {
            var ligne = await _context.ControlesDocuments
                .Where(d => d.Id == documentId && d.ControleId == controleId && d.Controle!.EleveId == eleveId)
                .FirstOrDefaultAsync(ct);

            if (ligne is null) return false;

            _context.ControlesDocuments.Remove(ligne);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<DocumentControle>> GetDocumentsATranscrireAsync(
            int taille, CancellationToken ct = default)
        {
            return await _context.ControlesDocuments
                .AsNoTracking()
                .Where(d => d.Transcription == null && d.Donnees != null)
                .OrderBy(d => d.DateCreation)
                .Take(taille)
                .Select(d => new DocumentControle
                {
                    Id = d.Id,
                    ControleId = d.ControleId,
                    NomFichier = d.NomFichier,
                    TypeMime = d.TypeMime,
                    Taille = d.Taille,
                    NombrePages = d.NombrePages,
                    Donnees = d.Donnees,
                    DateCreation = d.DateCreation,
                })
                .ToListAsync(ct);
        }

        public async Task EnregistrerTranscriptionDocumentAsync(
            int documentId, string transcription, DateTime maintenant, CancellationToken ct = default)
        {
            var ligne = await _context.ControlesDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);

            // Supprimé pendant la lecture : rien à écrire.
            if (ligne is null) return;

            ligne.Transcription = transcription;
            ligne.Donnees = null;
            ligne.DonneesEffaceesLe = maintenant;

            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> SignalerEchecLectureDocumentAsync(
            int documentId, DateTime maintenant, CancellationToken ct = default)
        {
            // En SQL, d'un bloc : la séance et la tâche de fond peuvent échouer
            // sur le même document au même moment.
            await _context.ControlesDocuments
                .Where(d => d.Id == documentId && d.Transcription == null)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.Tentatives, d => d.Tentatives + 1), ct);

            var abandonnes = await _context.ControlesDocuments
                .Where(d => d.Id == documentId && d.Transcription == null && d.Tentatives >= DocumentControle.LecturesMax)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Transcription, DocumentControle.MentionAbandon)
                    .SetProperty(d => d.Donnees, (byte[]?)null)
                    .SetProperty(d => d.DonneesEffaceesLe, maintenant), ct);

            return abandonnes > 0;
        }

        public async Task<IReadOnlyList<DocumentControle>> GetCoursPourSeanceAsync(
            int controleId, CancellationToken ct = default)
        {
            return await _context.ControlesDocuments
                .AsNoTracking()
                .Where(d => d.ControleId == controleId)
                .OrderBy(d => d.DateCreation)
                .Select(d => new DocumentControle
                {
                    Id = d.Id,
                    ControleId = d.ControleId,
                    NomFichier = d.NomFichier,
                    TypeMime = d.TypeMime,
                    Taille = d.Taille,
                    NombrePages = d.NombrePages,
                    // Les octets ne voyagent que pour un document pas encore lu.
                    Donnees = d.Transcription == null ? d.Donnees : null,
                    Transcription = d.Transcription,
                    DateCreation = d.DateCreation,
                })
                .ToListAsync(ct);
        }

        /// <summary>
        /// L'état de chaque ligne tel que l'observation du professeur le dit,
        /// par identifiant de ligne. Même rapprochement que la ligne `notions:` :
        /// à la plume près, sinon l'un contenu dans l'autre si une seule ligne
        /// répond.
        /// </summary>
        private static Dictionary<int, string> EtatsDeduits(string? observation, List<(int Id, string Libelle)> lignes)
        {
            var deduits = new Dictionary<int, string>();

            foreach (var lu in ObservationPret.Etats(observation))
            {
                if (TrouverParLibelle(lignes, lu.Libelle) is int id) deduits.TryAdd(id, lu.Etat);
            }

            return deduits;
        }

        private static int? TrouverParLibelle(List<(int Id, string Libelle)> lignes, string libelle)
        {
            var cle = LibelleNotion.CleSouple(libelle);
            if (cle.Length == 0) return null;

            var exacte = lignes.Where(l => LibelleNotion.CleSouple(l.Libelle) == cle).ToList();
            if (exacte.Count > 0) return exacte[0].Id;

            var proches = lignes
                .Where(l =>
                {
                    var sienne = LibelleNotion.CleSouple(l.Libelle);
                    return sienne.Length > 0 && (sienne.Contains(cle) || cle.Contains(sienne));
                })
                .ToList();

            return proches.Count == 1 ? proches[0].Id : null;
        }

        /// <summary>
        /// LA MÊME NOTION NE PREND JAMAIS DEUX LIGNES — relevé en prod le
        /// 13/10/2026 (Syan, 3e) : « l’Univers » au référentiel, « l'Univers »
        /// sous la plume du professeur, et deux lignes pour une seule notion —
        /// l'une mesurée, l'autre coincée à 25 % qui tirait la moyenne vers le
        /// bas. Le seul endroit où l'on cherche une ligne existante, pour les
        /// deux chemins qui en créent (le programme et le résultat d'une copie).
        ///
        /// Par la compétence d'abord. Puis par le libellé, à la plume près
        /// (accents, casse, apostrophes, « de … ») : une ligne en texte libre
        /// répond toujours ; une ligne déjà rattachée seulement si la notion
        /// cherchée n'a pas de compétence — deux compétences de deux classes
        /// peuvent porter le même nom, et ce ne sont pas les mêmes.
        /// </summary>
        private static ControleNotion? LigneDeLaNotion(List<ControleNotion> lignes, NotionDeclaree notion)
        {
            if (notion.CompetenceId is int id && lignes.FirstOrDefault(n => n.CompetenceId == id) is { } rattachee)
            {
                return rattachee;
            }

            var cle = LibelleNotion.CleSouple(notion.Libelle);
            if (cle.Length == 0) return null;

            return lignes.FirstOrDefault(n =>
                (n.CompetenceId is null || notion.CompetenceId is null)
                && LibelleNotion.CleSouple(n.Competence?.Libelle ?? n.Libelle) == cle);
        }

        /// <summary>
        /// Une ligne en texte libre retrouvée par son libellé, alors que la
        /// notion arrive rattachée : elle prend la compétence et le libellé du
        /// référentiel. C'est ce qui la rend MESURABLE — sans compétence, aucune
        /// observation ne l'atteint jamais.
        /// </summary>
        private static void Rattacher(ControleNotion ligne, NotionDeclaree notion, string libelle)
        {
            if (ligne.CompetenceId is not null || notion.CompetenceId is not int id) return;

            ligne.CompetenceId = id;
            ligne.Libelle = libelle;
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
