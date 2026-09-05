using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Repositories
{
    public class AbonnementRepository : IAbonnementRepository
    {
        /// <summary>
        /// Durée d'une pause. Un mois : la pause remplace la résiliation d'un
        /// mois creux, pas un abonnement à la carte.
        /// </summary>
        /// <summary>
        /// Disjoncteur d'emballement : jetons par élève et par jour.
        ///
        /// CINQUANTE MILLIONS, soit plus du double de ce qu'un enfant peut
        /// atteindre physiquement. Le calcul : dix heures — son plafond
        /// mensuel entier consommé en une journée — à 2,29 millions de jetons
        /// l'heure mesurés en usage réel, font vingt-trois millions. Une séance
        /// de quarante-cinq minutes dense en explications en pèse 1,7.
        ///
        /// Il ne se déclenchera donc JAMAIS sur un usage réel, et c'est voulu.
        /// Ce n'est pas un quota — les heures jouent ce rôle — mais un
        /// disjoncteur pour l'imprévu : une boucle de reprise, un bug, un cas
        /// qu'on n'a pas anticipé. Un seuil serré couperait un élève en train
        /// de travailler, ce qui coûterait plus cher que ce qu'il économise.
        /// </summary>
        private const long PlafondJetonsParJour = 50_000_000;

        /// <summary>
        /// Le drapeau « les essais sont ouverts ». Recopié ici plutôt que
        /// référencé depuis l'API : la couche de données ne dépend pas des
        /// contrôleurs, et une constante partagée dans les deux sens créerait
        /// une dépendance circulaire pour une chaîne de vingt caractères.
        /// </summary>
        private const string CleEssaisOuverts = "ESSAIS_OUVERTS";

        private static readonly TimeSpan DureePause = TimeSpan.FromDays(30);

        /// <summary>Délai avant qu'un nouveau droit de pause se rouvre.</summary>
        private static readonly TimeSpan DelaiEntrePauses = TimeSpan.FromDays(365);

        private readonly SchoolWebAppDatabaseContext _context;

        /// <summary>
        /// Le sel des empreintes d'adresses. Vient de la configuration, jamais
        /// de la base : une empreinte non salée se retrouve au dictionnaire, et
        /// la table dirait alors qui est inscrit chez nous.
        ///
        /// Facultatif pour que les utilitaires et les tests puissent construire
        /// le dépôt sans configuration. En son absence, la garde fonctionne
        /// toujours — elle est simplement moins résistante à une fuite.
        /// </summary>
        private readonly string? _sel;

        public AbonnementRepository(SchoolWebAppDatabaseContext context, string? selEmpreinte = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sel = selEmpreinte;
        }

        // ------------------------------------------------------------ catalogue
        public async Task<IEnumerable<OffreTarif>> GetOffresAsync(CancellationToken ct = default) =>
            await _context.Offres
                .AsNoTracking()
                .Where(o => o.Active)
                .OrderBy(o => o.Ordre)
                .Select(o => new OffreTarif
                {
                    Id = o.Id,
                    Code = o.Code,
                    Libelle = o.Libelle,
                    Accroche = o.Accroche,
                    PrixMensuelCentimes = o.PrixMensuelCentimes,
                    PrixAnnuelCentimes = o.PrixAnnuelCentimes,
                    NombreEnfantsMax = o.NombreEnfantsMax,
                    MinutesPotMensuel = o.MinutesPotMensuel,
                    MinutesPlafondEnfant = o.MinutesPlafondEnfant,
                    EstEssai = o.EstEssai,
                    StripePrixMensuelId = o.StripePrixMensuelId,
                    StripePrixAnnuelId = o.StripePrixAnnuelId,
                    JoursValidite = o.JoursValidite,
                })
                .ToListAsync(ct);

        public async Task<IEnumerable<OffreRechargeTarif>> GetOffresRechargeAsync(
            CancellationToken ct = default) =>
            await _context.OffresRecharge
                .AsNoTracking()
                .Where(o => o.Active)
                .OrderBy(o => o.Ordre)
                .Select(o => new OffreRechargeTarif
                {
                    Id = o.Id,
                    Code = o.Code,
                    Libelle = o.Libelle,
                    Minutes = o.Minutes,
                    PrixCentimes = o.PrixCentimes,
                    StripePrixId = o.StripePrixId,
                })
                .ToListAsync(ct);

        // ----------------------------------------------------------- abonnement
        public async Task<EtatQuota?> GetEtatQuotaAsync(int parentId, CancellationToken ct = default)
        {
            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            await RafraichirAsync(abonnement, ct);

            // Le rafraîchissement vient peut-être de résilier un essai expiré.
            // Rendre son état ferait afficher au parent un forfait actif dont
            // les heures ne fonctionnent plus : on rend null, et l'interface
            // propose les formules — ce qui est exactement l'étape suivante.
            if (abonnement.Statut == StatutAbonnement.Resilie) return null;

            return await ComposerAsync(abonnement, ct);
        }

        public async Task EnregistrerPrixStripeAsync(
            string codeOffre, string prixMensuelId, string prixAnnuelId, CancellationToken ct = default)
        {
            var offre = await _context.Offres.FirstOrDefaultAsync(o => o.Code == codeOffre, ct);
            if (offre is null) return;

            offre.StripePrixMensuelId = prixMensuelId;
            offre.StripePrixAnnuelId = prixAnnuelId;

            await _context.SaveChangesAsync(ct);
        }

        public async Task EnregistrerPrixRechargeStripeAsync(
            string codeRecharge, string prixId, CancellationToken ct = default)
        {
            var recharge = await _context.OffresRecharge.FirstOrDefaultAsync(o => o.Code == codeRecharge, ct);
            if (recharge is null) return;

            recharge.StripePrixId = prixId;

            await _context.SaveChangesAsync(ct);
        }

        public async Task<long> JetonsDuJourAsync(int eleveId, CancellationToken ct = default)
        {
            // Depuis minuit UTC. Une fenêtre glissante de vingt-quatre heures
            // serait plus juste, mais l'élève ne saurait jamais quand il repart
            // à zéro ; une journée civile se comprend sans explication.
            var debut = DateTime.UtcNow.Date;

            return await _context.Messages
                .AsNoTracking()
                .Where(m => m.Conversation!.EleveId == eleveId
                            && m.Role == "assistant"
                            && m.DateCreation >= debut)
                .SumAsync(m => (long)m.TokensEntree + m.TokensSortie
                               + m.TokensCacheLecture + m.TokensCacheEcriture, ct);
        }

        public async Task<EtatQuota?> SouscrireAsync(
            int parentId,
            string codeOffre,
            string periodicite,
            string? stripeAbonnementId = null,
            CancellationToken ct = default)
        {
            var offre = await _context.Offres
                .FirstOrDefaultAsync(o => o.Code == codeOffre && o.Active, ct);

            if (offre is null) return null;

            // L'ESSAI NE SE REPREND PAS, ET LA GARDE EST ICI.
            //
            // Elle vivait dans `OuvrirEssaiAsync`, que la page des tarifs ne
            // traverse pas : elle appelle cette méthode-ci directement. Un
            // parent dont l'essai était épuisé n'avait donc qu'à revenir sur
            // les tarifs et le reprendre — trente minutes de plus, autant de
            // fois qu'il voulait, sans rien supprimer.
            //
            // Placée dans la méthode que TOUS les chemins traversent, la règle
            // ne peut plus être contournée par un chemin qu'on aurait oublié.
            if (offre.EstEssai)
            {
                // LES ESSAIS SONT-ILS SEULEMENT OUVERTS ?
                //
                // Vérifié ICI et pas seulement dans l'écran : quand
                // l'administrateur ferme les essais, le bouton d'accueil change
                // de texte, mais la route reste appelable — par un onglet resté
                // ouvert sur l'ancienne page, ou directement. Une porte qu'on
                // ferme à l'affichage n'est pas fermée.
                var ouverts = await _context.Reglages
                    .AsNoTracking()
                    .Where(r => r.Cle == CleEssaisOuverts)
                    .Select(r => r.Valeur)
                    .FirstOrDefaultAsync(ct);

                // Absent = ouvert : c'est l'état d'un service qui n'a jamais
                // touché ce réglage.
                if (string.Equals(ouverts, "false", StringComparison.OrdinalIgnoreCase)) return null;

                var dejaAbonne = await _context.Abonnements
                    .AsNoTracking()
                    .AnyAsync(a => a.ParentId == parentId, ct);

                if (dejaAbonne) return await GetEtatQuotaAsync(parentId, ct);

                // ET L'ADRESSE, QUI SE SOUVIENT PLUS LOIN QUE LE COMPTE.
                //
                // Le test ci-dessus ne voit que l'historique du compte COURANT.
                // Supprimer son compte et se réinscrire le remet à zéro — et
                // rend l'essai, indéfiniment. L'empreinte de l'adresse, elle,
                // survit à l'effacement : c'est la seule chose qu'on garde, et
                // elle ne sert qu'à répondre à cette question-là.
                var mail = await _context.Parents
                    .AsNoTracking()
                    .Where(p => p.Id == parentId)
                    .Select(p => p.Mail)
                    .FirstOrDefaultAsync(ct);

                var empreinte = EmpreinteMail.Calculer(mail, _sel);

                if (empreinte.Length > 0)
                {
                    var dejaEssaye = await _context.EssaisConsommes
                        .AsNoTracking()
                        .AnyAsync(e => e.MailHache == empreinte, ct);

                    // Rien à rendre : ce compte n'a aucun abonnement, et il
                    // n'aura pas d'essai. Null fait répondre « offre
                    // indisponible » à l'appelant, ce qui est la vérité.
                    if (dejaEssaye) return null;

                    _context.EssaisConsommes.Add(new EssaiConsomme
                    {
                        MailHache = empreinte,
                        DateCreation = DateTime.UtcNow,
                    });
                }
            }

            // Un seul abonnement actif par compte : on clôt l'ancien plutôt que
            // de le laisser traîner, sinon `Courant` deviendrait ambigu.
            var anciens = await _context.Abonnements
                .Where(a => a.ParentId == parentId && a.Statut != StatutAbonnement.Resilie)
                .ToListAsync(ct);

            foreach (var ancien in anciens)
            {
                ancien.Statut = StatutAbonnement.Resilie;
                ancien.DateFin = DateTime.UtcNow;
            }

            var debut = DateTime.UtcNow;

            var abonnement = new Abonnement
            {
                ParentId = parentId,
                OffreId = offre.Id,
                DateDebut = debut,
                DateFin = offre.JoursValidite > 0 ? debut.AddDays(offre.JoursValidite) : null,
                Statut = StatutAbonnement.Actif,
                Periodicite = Domain.Models.PeriodiciteAbonnement.EstValide(periodicite)
                    ? periodicite
                    : Domain.Models.PeriodiciteAbonnement.Mensuel,
                PeriodeDebut = debut,
                PeriodeFin = FinPeriode(debut, offre),
                StripeAbonnementId = stripeAbonnementId,
                DateCreation = debut,
            };

            _context.Abonnements.Add(abonnement);
            await _context.SaveChangesAsync(ct);

            abonnement.Offre = offre;
            return await ComposerAsync(abonnement, ct);
        }

        public async Task<EtatQuota?> OuvrirEssaiAsync(
            int parentId, CancellationToken ct = default)
        {
            // A-T-IL DÉJÀ EU UN ABONNEMENT, N'IMPORTE LEQUEL ?
            //
            // La question porte sur l'HISTORIQUE et non sur l'abonnement en
            // cours. Regarder seulement le courant rendrait l'essai reprenable
            // à volonté : il suffirait de résilier pour repartir avec trente
            // minutes offertes, autant de fois qu'on veut.
            var dejaAbonne = await _context.Abonnements
                .AsNoTracking()
                .AnyAsync(a => a.ParentId == parentId, ct);

            if (dejaAbonne) return await GetEtatQuotaAsync(parentId, ct);

            var essai = await _context.Offres
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.EstEssai && o.Active, ct);

            if (essai is null) return null;

            return await SouscrireAsync(
                parentId, essai.Code!, Domain.Models.PeriodiciteAbonnement.Mensuel, null, ct);
        }

        public async Task<IReadOnlyList<string>> AbonnementsStripeAsync(
            int parentId, CancellationToken ct = default) =>
            await _context.Abonnements
                .AsNoTracking()
                .Where(a => a.ParentId == parentId && a.StripeAbonnementId != null)
                .Select(a => a.StripeAbonnementId!)
                .ToListAsync(ct);

        public async Task<string?> AbonnementStripeCourantAsync(
            int parentId, CancellationToken ct = default) =>
            await Courant(parentId)
                .AsNoTracking()
                .Select(a => a.StripeAbonnementId)
                .FirstOrDefaultAsync(ct);

        public async Task<EtatQuota?> ChangerDeFormuleAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default)
        {
            var offre = await _context.Offres
                .FirstOrDefaultAsync(o => o.Code == codeOffre && o.Active, ct);

            if (offre is null) return null;

            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            abonnement.OffreId = offre.Id;
            abonnement.Offre = offre;

            abonnement.Periodicite = Domain.Models.PeriodiciteAbonnement.EstValide(periodicite)
                ? periodicite
                : Domain.Models.PeriodiciteAbonnement.Mensuel;

            // Le pot change de taille : l'alerte des 80 % doit pouvoir se
            // redéclencher, sinon un parent qui passe de Solo à Famille ne
            // serait plus jamais prévenu de sa consommation.
            abonnement.AlerteQuotaEnvoyee = false;

            // Une résiliation en attente tombe : demander une autre formule,
            // c'est vouloir continuer. La laisser courir couperait l'accès à
            // la fin du mois, sur l'abonnement qu'on vient de choisir.
            abonnement.ResiliationDemandeeLe = null;

            // Un essai porte une date de fin ; les formules payantes se
            // reconduisent. La garder ferait expirer l'abonnement payé au
            // septième jour.
            abonnement.DateFin = offre.JoursValidite > 0
                ? abonnement.DateDebut.AddDays(offre.JoursValidite)
                : null;

            await _context.SaveChangesAsync(ct);
            return await ComposerAsync(abonnement, ct);
        }

        public async Task<EtatQuota?> PlanifierChangementAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default)
        {
            var offre = await _context.Offres
                .FirstOrDefaultAsync(o => o.Code == codeOffre && o.Active, ct);

            if (offre is null) return null;

            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            abonnement.OffrePrevueId = offre.Id;
            abonnement.PeriodicitePrevue = Domain.Models.PeriodiciteAbonnement.EstValide(periodicite)
                ? periodicite
                : Domain.Models.PeriodiciteAbonnement.Mensuel;

            // La date d'effet est celle de la fin de période CONNUE AUJOURD'HUI.
            // Elle sert à l'écrire au parent ; c'est le passage de période, et
            // non cette date, qui déclenche réellement la bascule.
            abonnement.ChangementPrevuLe = abonnement.PeriodeFin;

            await _context.SaveChangesAsync(ct);
            return await ComposerAsync(abonnement, ct);
        }

        public async Task<EtatQuota?> AnnulerChangementPrevuAsync(
            int parentId, CancellationToken ct = default)
        {
            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            abonnement.OffrePrevueId = null;
            abonnement.PeriodicitePrevue = null;
            abonnement.ChangementPrevuLe = null;

            await _context.SaveChangesAsync(ct);
            return await ComposerAsync(abonnement, ct);
        }

        public async Task<bool> ReporterPeriodeAsync(
            string stripeAbonnementId,
            DateTime periodeDebut,
            DateTime periodeFin,
            CancellationToken ct = default)
        {
            // L'alerte des 80 % repart à faux avec la période : sans ça le
            // parent ne serait prévenu qu'une seule fois dans la vie de son
            // abonnement, et jamais aux mois suivants.
            var touchees = await _context.Abonnements
                .Where(a => a.StripeAbonnementId == stripeAbonnementId)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(a => a.PeriodeDebut, periodeDebut)
                    .SetProperty(a => a.PeriodeFin, periodeFin)
                    .SetProperty(a => a.AlerteQuotaEnvoyee, false)

                    // Une facture payée solde l'impayé, quel qu'il soit : c'est
                    // le seul événement qui prouve que l'argent est arrivé.
                    // Sans cette remise à zéro, un parent qui régularise
                    // resterait bloqué pour toujours.
                    .SetProperty(a => a.ImpayeDepuis, (DateTime?)null)
                    .SetProperty(a => a.Statut, StatutAbonnement.Actif), ct);

            return touchees > 0;
        }

        public async Task<bool> MarquerImpayeAsync(
            string stripeAbonnementId, CancellationToken ct = default)
        {
            // `ImpayeDepuis == null` dans le filtre : on garde la date du
            // PREMIER refus. Stripe relance trois ou quatre fois, et chaque
            // relance repousserait l'échéance si on écrasait la date — au bout
            // de deux semaines on ne saurait plus depuis quand ça dure.
            var touchees = await _context.Abonnements
                .Where(a => a.StripeAbonnementId == stripeAbonnementId && a.ImpayeDepuis == null)
                .ExecuteUpdateAsync(m => m.SetProperty(a => a.ImpayeDepuis, DateTime.UtcNow), ct);

            return touchees > 0;
        }

        public async Task<bool> CloreParStripeAsync(
            string stripeAbonnementId, CancellationToken ct = default)
        {
            var touchees = await _context.Abonnements
                .Where(a => a.StripeAbonnementId == stripeAbonnementId
                            && a.Statut != StatutAbonnement.Resilie)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(a => a.Statut, StatutAbonnement.Resilie)
                    .SetProperty(a => a.DateFin, DateTime.UtcNow), ct);

            return touchees > 0;
        }

        // -------------------------------------------------------- vérification
        public async Task<VerdictQuota> VerifierAsync(int eleveId, CancellationToken ct = default)
        {
            var parentId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => (int?)e.ParentId)
                .FirstOrDefaultAsync(ct);

            if (parentId is null) return VerdictQuota.Refus(MotifRefus.SansAbonnement);

            // Le disjoncteur, en premier et avant toute autre considération.
            //
            // Vérifié ICI, dans la vérification d'avant-tour : c'est le seul
            // endroit qui s'exécute avant que le professeur ouvre la bouche.
            // Le placer ailleurs — au fil de la génération, par exemple —
            // couperait une explication ou une justification en plein milieu,
            // ce qui est exactement ce qu'on refuse.
            if (await JetonsDuJourAsync(eleveId, ct) >= PlafondJetonsParJour)
            {
                return VerdictQuota.Refus(MotifRefus.PlafondJetons);
            }

            var abonnement = await Courant(parentId.Value).FirstOrDefaultAsync(ct);
            if (abonnement is null) return VerdictQuota.Refus(MotifRefus.SansAbonnement);

            await RafraichirAsync(abonnement, ct);

            if (abonnement.Statut == StatutAbonnement.EnPause)
            {
                return VerdictQuota.Refus(MotifRefus.EnPause);
            }

            if (abonnement.Statut == StatutAbonnement.Resilie)
            {
                return VerdictQuota.Refus(MotifRefus.SansAbonnement);
            }

            // IMPAYÉ ET PÉRIODE ÉCHUE : l'accès s'arrête ici.
            //
            // Les deux conditions ensemble, jamais l'une seule. Un impayé dont
            // la période court encore ne change rien : elle est payée, et
            // Stripe est peut-être en train de représenter la carte avec
            // succès. C'est le franchissement de l'échéance sans règlement qui
            // ferme la porte.
            //
            // Un motif à lui : « aucun abonnement » enverrait le parent
            // souscrire une formule qu'il a déjà, alors qu'il n'a qu'une carte
            // à mettre à jour.
            if (abonnement.ImpayeDepuis is not null && abonnement.PeriodeFin <= DateTime.UtcNow)
            {
                return VerdictQuota.Refus(MotifRefus.Impaye);
            }

            var offre = abonnement.Offre!;

            // Le nombre d'enfants couverts, AVANT les minutes : un quatrième
            // enfant sur une formule Solo n'a pas sa place, même si le pot est
            // plein. Les places se prennent à l'usage, dans l'ordre d'arrivée
            // sur la période — le premier à travailler garde la sienne jusqu'au
            // renouvellement.
            if (!await PlaceDisponibleAsync(abonnement, eleveId, offre.NombreEnfantsMax, ct))
            {
                return VerdictQuota.Refus(MotifRefus.TropDEnfants);
            }

            var (consommeesPot, consommeesEnfant) = await ConsommationAsync(abonnement, eleveId, ct);

            // LES DEUX NOMBRES VIENNENT DE LA VUE, pas de calculs faits ici.
            //
            // `vw_ForfaitAbonnement` sait quelles recharges comptent, et
            // ajoute déjà leur total au forfait comme au plafond. Refaire
            // l'addition en C# rouvrirait la porte par laquelle cette règle
            // a divergé deux fois entre cet écran et celui de
            // l'administration.
            var forfait = await ForfaitAsync(abonnement, offre, ct);

            var alloueesPot = forfait.MinutesAllouees;

            // LES RECHARGES LÈVENT AUSSI LE PLAFOND INDIVIDUEL.
            //
            // Elles ne le levaient pas, et sur Solo c'était une vente à vide.
            // Le pot vaut 9 h, le plafond aussi, et un seul enfant est admis :
            // les trois heures achetées entraient dans le pot puis butaient
            // sur le plafond, refusées avec le motif `PlafondEnfant` — pendant
            // que l'écran du parent affichait bien 12 h allouées. Il pouvait
            // donc payer 14,90 € pour des heures inatteignables.
            //
            // Le plafond existe pour protéger les FRÈRES ET SŒURS d'un aîné
            // qui viderait le pot. Une recharge, elle, est ajoutée exprès pour
            // débloquer quelqu'un : la refuser à celui qu'elle vise vide le
            // geste de son sens. Sur Duo et Famille, elle ne dérègle rien —
            // le pot reste le vrai frein, et il est commun.
            var plafondEnfant = forfait.MinutesPlafondEnfant;

            // Le pot d'abord : c'est lui qui borne le coût. Le plafond ensuite,
            // qui protège la fratrie plutôt que la facture — d'où deux motifs
            // de refus distincts, que les écrans expliquent différemment.
            if (consommeesPot >= alloueesPot) return VerdictQuota.Refus(MotifRefus.PotEpuise);

            if (consommeesEnfant >= plafondEnfant)
            {
                return VerdictQuota.Refus(MotifRefus.PlafondEnfant);
            }

            var restantes = Math.Min(
                alloueesPot - consommeesPot,
                plafondEnfant - consommeesEnfant);

            return VerdictQuota.Ok(restantes);
        }

        public async Task<CapaciteEnfants> CapaciteAsync(int parentId, CancellationToken ct = default)
        {
            // Les profils retirés ne prennent plus de place : c'est ce qui rend
            // le retrait utile. Une famille en formule Solo dont l'aîné arrête
            // doit pouvoir inscrire la cadette sans changer d'abonnement.
            var actuels = await _context.Eleves
                .CountAsync(e => e.ParentId == parentId && e.ArchiveLe == null, ct);

            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is not null) await RafraichirAsync(abonnement, ct);

            // Une place sans abonnement : de quoi créer le premier profil et
            // lancer l'essai. Zéro bloquerait l'inscription sur elle-même.
            var maximum = abonnement is null || abonnement.Statut == StatutAbonnement.Resilie
                ? 1
                : abonnement.Offre!.NombreEnfantsMax;

            return new CapaciteEnfants(actuels, maximum, abonnement?.Offre?.Libelle);
        }

        // ------------------------------------------------------------ décompte
        public async Task<bool> ImputerAsync(
            int eleveId, int secondes, double seuilAlerte, CancellationToken ct = default)
        {
            if (secondes <= 0) return false;

            var parentId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => (int?)e.ParentId)
                .FirstOrDefaultAsync(ct);

            if (parentId is null) return false;

            var abonnement = await Courant(parentId.Value).FirstOrDefaultAsync(ct);
            if (abonnement is null) return false;

            await RafraichirAsync(abonnement, ct);

            var ligne = await _context.ConsommationsEleves
                .FirstOrDefaultAsync(
                    c => c.AbonnementId == abonnement.Id
                         && c.EleveId == eleveId
                         && c.PeriodeDebut == abonnement.PeriodeDebut,
                    ct);

            if (ligne is null)
            {
                ligne = new ConsommationEleve
                {
                    AbonnementId = abonnement.Id,
                    EleveId = eleveId,
                    PeriodeDebut = abonnement.PeriodeDebut,
                    SecondesConsommees = 0,
                };

                _context.ConsommationsEleves.Add(ligne);
            }

            ligne.SecondesConsommees += secondes;
            ligne.DerniereActivite = DateTime.UtcNow;

            // L'alerte se décide ici, dans la même transaction que le décompte :
            // la calculer plus tard laisserait passer le franchissement entre
            // deux requêtes concurrentes.
            var alerte = false;

            if (!abonnement.AlerteQuotaEnvoyee)
            {
                var allouees = abonnement.Offre!.MinutesPotMensuel
                               + await MinutesRechargeAsync(abonnement, ct);

                var totalSecondes = await _context.ConsommationsEleves
                    .Where(c => c.AbonnementId == abonnement.Id
                                && c.PeriodeDebut == abonnement.PeriodeDebut
                                && c.Id != ligne.Id)
                    .SumAsync(c => c.SecondesConsommees, ct);

                var consommees = (totalSecondes + ligne.SecondesConsommees) / 60;

                if (allouees > 0 && consommees >= allouees * seuilAlerte)
                {
                    abonnement.AlerteQuotaEnvoyee = true;
                    alerte = true;
                }
            }

            await _context.SaveChangesAsync(ct);
            return alerte;
        }

        // --------------------------------------------------------------- pause
        public async Task<EtatQuota?> MettreEnPauseAsync(int parentId, CancellationToken ct = default)
        {
            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            await RafraichirAsync(abonnement, ct);

            if (abonnement.Offre!.EstEssai) return null;
            if (abonnement.Statut != StatutAbonnement.Actif) return null;
            if (!PauseDisponible(abonnement)) return null;

            var maintenant = DateTime.UtcNow;

            abonnement.Statut = StatutAbonnement.EnPause;
            abonnement.DernierePause = maintenant;
            abonnement.PauseJusquau = maintenant.Add(DureePause);

            // La période est repoussée d'autant : le parent ne perd pas le
            // quota du mois qu'il met en pause, il le décale.
            abonnement.PeriodeFin = abonnement.PeriodeFin.Add(DureePause);

            await _context.SaveChangesAsync(ct);
            return await ComposerAsync(abonnement, ct);
        }

        public async Task<EtatQuota?> ReprendreAsync(int parentId, CancellationToken ct = default)
        {
            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            if (abonnement.Statut == StatutAbonnement.EnPause)
            {
                abonnement.Statut = StatutAbonnement.Actif;
                abonnement.PauseJusquau = null;
                await _context.SaveChangesAsync(ct);
            }

            await RafraichirAsync(abonnement, ct);
            return await ComposerAsync(abonnement, ct);
        }

        // --------------------------------------------------------- résiliation
        /// <summary>
        /// Arrête la reconduction tacite. NE COUPE RIEN SUR-LE-CHAMP.
        ///
        /// Le parent a payé sa période, il la garde jusqu'au bout : la
        /// résiliation prend effet à `PeriodeFin`. Couper immédiatement
        /// reviendrait à confisquer des heures achetées, et transformerait un
        /// départ calme en litige.
        ///
        /// Idempotent : redemander ne déplace pas la date déjà enregistrée.
        /// </summary>
        public async Task<EtatQuota?> ResilierAsync(int parentId, CancellationToken ct = default)
        {
            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            await RafraichirAsync(abonnement, ct);

            // Déjà clos : rien à résilier, et surtout rien à laisser croire.
            if (abonnement.Statut == StatutAbonnement.Resilie) return null;

            if (abonnement.ResiliationDemandeeLe is null)
            {
                abonnement.ResiliationDemandeeLe = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            return await ComposerAsync(abonnement, ct);
        }

        /// <summary>
        /// Revient sur une résiliation, tant que la période court encore.
        ///
        /// Indispensable, et pas par gentillesse : sans ce chemin, un parent
        /// qui se ravise devrait résilier puis re-souscrire, donc repayer une
        /// période qu'il a déjà réglée.
        /// </summary>
        public async Task<EtatQuota?> AnnulerResiliationAsync(int parentId, CancellationToken ct = default)
        {
            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            // Trop tard : la période est passée et le statut a basculé. On ne
            // ressuscite pas un abonnement clos, on en souscrit un nouveau.
            if (abonnement.Statut == StatutAbonnement.Resilie) return null;

            if (abonnement.ResiliationDemandeeLe is not null)
            {
                abonnement.ResiliationDemandeeLe = null;
                await _context.SaveChangesAsync(ct);
            }

            await RafraichirAsync(abonnement, ct);
            return await ComposerAsync(abonnement, ct);
        }

        // ------------------------------------------------------------ recharge
        public Task<EtatQuota?> RechargerAsync(
            int parentId, string codeRecharge, CancellationToken ct = default) =>
            CrediterPackAsync(parentId, codeRecharge, null, null, null, ct);

        public async Task<EtatQuota?> RechargerApresPaiementAsync(
            int parentId, string codeRecharge, string sessionStripeId,
            string? paiementStripeId, CancellationToken ct = default)
        {
            // Une session vide ne prouve aucun paiement : sans identifiant, il
            // n'y a plus rien pour empêcher le double crédit, et créditer
            // quand même reviendrait à faire confiance à l'appelant.
            if (string.IsNullOrWhiteSpace(sessionStripeId)) return null;

            // DEUX BARRIÈRES, ET C'EST VOULU.
            //
            // Ce test attrape le cas courant — Stripe rejoue un événement
            // qu'il a déjà envoyé — et évite d'écrire pour rien. Mais deux
            // webhooks arrivés EN MÊME TEMPS le passeraient tous les deux : à
            // cet instant aucune ligne n'existe encore. C'est l'index unique
            // qui tranche alors, et l'exception qu'il lève est rattrapée plus
            // bas. Le test est un confort, la contrainte est la garantie.
            var deja = await _context.Recharges
                .AsNoTracking()
                .AnyAsync(r => r.StripeSessionId == sessionStripeId, ct);

            if (deja) return null;

            try
            {
                return await CrediterPackAsync(
                    parentId, codeRecharge, sessionStripeId, paiementStripeId, null, ct);
            }
            catch (DbUpdateException)
            {
                // L'index unique a refusé : un autre webhook a crédité entre
                // notre test et notre écriture. Le parent a bien ses heures —
                // rendre null dit « rien à ajouter », ce qui est exact.
                return null;
            }
        }

        /// <summary>
        /// Offre un pack : les mêmes heures, au prix de zéro.
        ///
        /// LE PRIX EST LA SEULE DIFFÉRENCE AVEC UN PACK ACHETÉ, et elle
        /// compte double. Enregistré au tarif du catalogue, un cadeau
        /// gonflerait le chiffre d'affaires lu en administration — l'écran
        /// même qui sert à décider des prix — et afficherait au parent une
        /// ligne à 14,90 € qu'il n'a jamais payée. Le motif, lui, dit dans
        /// l'historique d'où viennent ces heures.
        ///
        /// L'UNICITÉ PASSE PAR LA MÊME COLONNE que les paiements, et le
        /// même index unique filtré la garantit. C'est ce qui permet
        /// d'appeler cette méthode depuis un webhook, que Stripe réémet
        /// tant qu'il n'a pas reçu de 200.
        /// </summary>
        public async Task<EtatQuota?> OffrirMinutesAsync(
            int parentId, int minutes, string cleUnicite, string motif,
            CancellationToken ct = default)
        {
            if (minutes <= 0) return null;
            if (string.IsNullOrWhiteSpace(cleUnicite)) return null;
            if (string.IsNullOrWhiteSpace(motif)) return null;

            // Même garde en deux temps que pour un pack payé : ce test
            // attrape le cas courant, la contrainte tranche les courses.
            var deja = await _context.Recharges
                .AsNoTracking()
                .AnyAsync(r => r.StripeSessionId == cleUnicite, ct);

            if (deja) return null;

            try
            {
                return await CrediterMinutesAsync(
                    parentId, minutes, cleUnicite, motif.Trim(), ct);
            }
            catch (DbUpdateException)
            {
                return null;
            }
        }

        /// <summary>
        /// Écrit les heures offertes. Le jumeau de `CrediterPackAsync`, sans
        /// catalogue ni prix.
        ///
        /// LES MÊMES GARDES QU'UN PACK PAYÉ, et il le faut : un cadeau posé
        /// sur un essai expirerait avec lui, et un cadeau sans abonnement
        /// n'aurait rien où se rattacher.
        /// </summary>
        private async Task<EtatQuota?> CrediterMinutesAsync(
            int parentId, int minutes, string cleUnicite, string motif, CancellationToken ct)
        {
            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            // ON N'OFFRE PAS D'HEURES SUR UN ESSAI. Elles expireraient avec
            // lui — sept jours — et il n'y a pas d'abonnement derrière pour
            // en profiter ensuite.
            if (abonnement.Offre!.EstEssai) return null;

            await RafraichirAsync(abonnement, ct);

            _context.Recharges.Add(new Recharge
            {
                AbonnementId = abonnement.Id,
                PeriodeDebut = abonnement.PeriodeDebut,
                Minutes = minutes,

                // ZÉRO EURO : rien n'a été encaissé. Le porter à un prix
                // fausserait le chiffre d'affaires lu en administration, et
                // montrerait au parent une ligne qu'il n'a jamais payée.
                PrixCentimes = 0,
                Motif = motif,

                DateAchat = DateTime.UtcNow,
                StripeSessionId = cleUnicite,
            });

            abonnement.AlerteQuotaEnvoyee = false;

            await _context.SaveChangesAsync(ct);

            return await ComposerAsync(abonnement, ct);
        }

        /// <param name="motif">
        /// Nul quand le pack est ACHETÉ ; renseigné quand il est offert. Ce
        /// paramètre décide aussi du prix enregistré — voir plus bas.
        /// </param>
        private async Task<EtatQuota?> CrediterPackAsync(
            int parentId, string codeRecharge, string? sessionStripeId,
            string? paiementStripeId, string? motif, CancellationToken ct)
        {
            var pack = await _context.OffresRecharge
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Code == codeRecharge && o.Active, ct);

            if (pack is null) return null;

            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            // ON NE VEND PAS D'HEURES SUR UN ESSAI.
            //
            // Une recharge est valable jusqu'à la fin de la période en cours.
            // Sur l'essai, c'est sept jours, et il n'y a pas d'abonnement
            // derrière pour en profiter ensuite : le parent paierait quinze
            // euros d'heures qui expirent avec son essai.
            //
            // Refusé ICI et pas seulement dans l'écran : le bouton y est
            // masqué, mais un bouton caché n'est pas une règle. La route reste
            // appelable — par un onglet resté ouvert sur l'ancienne version,
            // par curiosité, ou par n'importe quel client. Ce qui protège
            // l'argent d'un parent ne se met pas dans le navigateur.
            if (abonnement.Offre!.EstEssai) return null;

            await RafraichirAsync(abonnement, ct);

            _context.Recharges.Add(new Recharge
            {
                AbonnementId = abonnement.Id,
                PeriodeDebut = abonnement.PeriodeDebut,
                Minutes = pack.Minutes,

                // UN PACK OFFERT VAUT ZÉRO EURO. Le porter au prix du
                // catalogue fausserait le chiffre d'affaires lu en
                // administration, et montrerait au parent une ligne à
                // 14,90 € qu'il n'a jamais payée. Même raisonnement que
                // pour `AjusterHeuresAsync`, qui traite les gestes
                // commerciaux saisis à la main.
                PrixCentimes = motif is null ? pack.PrixCentimes : 0,
                Motif = motif,

                DateAchat = DateTime.UtcNow,

                // Nul quand la recharge est offerte : compte exempté, geste
                // commercial. L'index unique est filtré pour l'accepter.
                StripeSessionId = sessionStripeId,
                StripePaiementId = paiementStripeId,
            });

            // Le pot repart au-dessus du seuil : l'alerte doit pouvoir se
            // redéclencher si le parent reconsomme tout.
            abonnement.AlerteQuotaEnvoyee = false;

            await _context.SaveChangesAsync(ct);
            return await ComposerAsync(abonnement, ct);
        }

        public async Task<IEnumerable<LigneHeures>> HistoriqueHeuresAsync(
            int parentId, CancellationToken ct = default)
        {
            // TOUS LES ABONNEMENTS DU PARENT, pas seulement le courant : un
            // parent qui a résilié puis repris garde son historique, et c'est
            // précisément dans ces cas-là qu'il conteste.
            return await _context.Recharges
                .AsNoTracking()
                .Where(r => r.Abonnement!.ParentId == parentId)
                .OrderByDescending(r => r.DateAchat)
                .Select(r => new LigneHeures
                {
                    Date = r.DateAchat,
                    Minutes = r.Minutes,
                    PrixCentimes = r.PrixCentimes,
                    Motif = r.Motif,
                    StripePaiementId = r.StripePaiementId,
                    DateRemboursement = r.DateRemboursement,
                    PeriodeDebut = r.PeriodeDebut,
                })
                .ToListAsync(ct);
        }

        public async Task<EtatQuota?> AjusterHeuresAsync(
            int parentId, int minutes, string motif, CancellationToken ct = default)
        {
            if (minutes == 0) return null;
            if (string.IsNullOrWhiteSpace(motif)) return null;

            var abonnement = await Courant(parentId).FirstOrDefaultAsync(ct);
            if (abonnement is null) return null;

            await RafraichirAsync(abonnement, ct);

            // L'AJUSTEMENT NE TOUCHE QUE LES HEURES SUPPLÉMENTAIRES, JAMAIS LE
            // FORFAIT.
            //
            // Le forfait vient de la formule souscrite : le rogner ferait
            // livrer moins que ce qui est facturé, et le parent n'aurait aucun
            // moyen de le voir. On ne peut donc retirer que ce qui a été
            // ajouté — achats et gestes commerciaux — et pas en dessous de
            // zéro.
            var supplement = await MinutesRechargeAsync(abonnement, ct);

            if (supplement + minutes < 0)
            {
                return null;
            }

            _context.Recharges.Add(new Recharge
            {
                AbonnementId = abonnement.Id,
                PeriodeDebut = abonnement.PeriodeDebut,
                Minutes = minutes,

                // Zéro : rien n'a été encaissé. Le porter au prix d'un pack
                // fausserait le chiffre d'affaires lu dans l'administration.
                PrixCentimes = 0,
                DateAchat = DateTime.UtcNow,
                Motif = motif.Trim(),
            });

            // Le pot repart peut-être au-dessus du seuil : l'alerte doit
            // pouvoir se redéclencher.
            if (minutes > 0) abonnement.AlerteQuotaEnvoyee = false;

            await _context.SaveChangesAsync(ct);
            return await ComposerAsync(abonnement, ct);
        }

        public async Task<bool> RembourserRechargeAsync(
            string paiementStripeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(paiementStripeId)) return false;

            var recharge = await _context.Recharges
                .FirstOrDefaultAsync(r => r.StripePaiementId == paiementStripeId, ct);

            // Paiement inconnu : ce n'était pas une recharge. Un remboursement
            // d'abonnement passe par ici aussi, et il n'y a rien à reprendre —
            // c'est Stripe qui gère la période, pas nous.
            if (recharge is null) return false;

            // DÉJÀ REPRIS. Stripe émet un événement par remboursement, et
            // rejoue ceux dont il n'a pas eu de 200 : sans ce test, un
            // remboursement partiel suivi d'un autre retirerait deux fois les
            // mêmes heures.
            if (recharge.DateRemboursement is not null) return false;

            recharge.DateRemboursement = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return true;
        }

        // ------------------------------------------------------------- interne
        private IQueryable<Abonnement> Courant(int parentId) =>
            _context.Abonnements
                .Include(a => a.Offre)
                .Include(a => a.OffrePrevue)
                .Where(a => a.ParentId == parentId && a.Statut != StatutAbonnement.Resilie)
                .OrderByDescending(a => a.DateCreation);

        /// <summary>
        /// Remet l'abonnement à l'heure : sortie de pause échue, renouvellement
        /// de période, expiration de l'essai.
        ///
        /// Fait à la lecture plutôt que par une tâche planifiée : un cron qui ne
        /// tourne pas laisserait un parent bloqué sur un quota épuisé alors que
        /// sa période est passée, et c'est le genre de panne qu'on découvre par
        /// une réclamation.
        /// </summary>
        private async Task RafraichirAsync(Abonnement abonnement, CancellationToken ct)
        {
            var maintenant = DateTime.UtcNow;
            var modifie = false;

            if (abonnement.Statut == StatutAbonnement.EnPause
                && abonnement.PauseJusquau is { } fin
                && fin <= maintenant)
            {
                abonnement.Statut = StatutAbonnement.Actif;
                abonnement.PauseJusquau = null;
                modifie = true;
            }

            // L'essai expiré se résilie de lui-même. Pas de quota qui repart :
            // c'est tout l'intérêt d'un essai.
            if (abonnement.DateFin is { } echeance && echeance <= maintenant)
            {
                if (abonnement.Statut != StatutAbonnement.Resilie)
                {
                    abonnement.Statut = StatutAbonnement.Resilie;
                    modifie = true;
                }
            }
            // Résiliation demandée et période échue : c'est ici que la
            // résiliation prend effet. Ce test PASSE AVANT la reconduction —
            // dans l'ordre inverse, la période serait renouvelée d'abord et le
            // parent repartirait pour un mois qu'il ne veut pas.
            else if (abonnement.ResiliationDemandeeLe is not null
                     && abonnement.PeriodeFin <= maintenant)
            {
                if (abonnement.Statut != StatutAbonnement.Resilie)
                {
                    abonnement.Statut = StatutAbonnement.Resilie;
                    abonnement.DateFin = abonnement.PeriodeFin;
                    modifie = true;
                }
            }
            // UNE PÉRIODE NON PAYÉE NE SE RENOUVELLE PAS.
            //
            // Sans ce test, l'échéance rechargeait le pot d'heures quoi qu'il
            // arrive : un prélèvement refusé laissait le compte pleinement
            // utilisable, mois après mois. Stripe finit par clore l'abonnement
            // au bout de ses relances — mais si ce webhook-là se perd, jamais.
            //
            // Placé APRÈS la résiliation et AVANT la reconduction : un parent
            // qui a résilié doit voir sa résiliation prendre effet, même s'il
            // traîne un impayé.
            else if (abonnement.ImpayeDepuis is not null
                     && abonnement.PeriodeFin <= maintenant)
            {
                // Rien à faire, et c'est l'intention : la période reste échue,
                // le pot ne se recharge pas. L'accès s'arrête de lui-même,
                // sans avoir à changer le statut — l'abonnement n'est pas
                // résilié, il attend son règlement.
            }
            else if (abonnement.Statut == StatutAbonnement.Actif
                     && abonnement.PeriodeFin <= maintenant)
            {
                // LA DESCENTE DE GAMME PREND EFFET ICI, ET NULLE PART AILLEURS.
                //
                // Avant de recalculer la période : c'est la nouvelle formule
                // qui doit en fixer la durée. Dans l'ordre inverse, un passage
                // au mensuel garderait une période d'un an.
                if (abonnement.OffrePrevueId is int prevue)
                {
                    var nouvelle = await _context.Offres.FirstOrDefaultAsync(o => o.Id == prevue, ct);

                    if (nouvelle is not null)
                    {
                        abonnement.OffreId = nouvelle.Id;
                        abonnement.Offre = nouvelle;

                        if (Domain.Models.PeriodiciteAbonnement.EstValide(abonnement.PeriodicitePrevue))
                        {
                            abonnement.Periodicite = abonnement.PeriodicitePrevue!;
                        }
                    }

                    abonnement.OffrePrevueId = null;
                    abonnement.PeriodicitePrevue = null;
                    abonnement.ChangementPrevuLe = null;
                }

                // Boucle et non simple addition : un compte inactif trois mois
                // doit repartir sur la période courante, pas rattraper les
                // trois périodes manquées une par une à chaque lecture.
                var offre = abonnement.Offre!;

                while (abonnement.PeriodeFin <= maintenant)
                {
                    abonnement.PeriodeDebut = abonnement.PeriodeFin;
                    abonnement.PeriodeFin = FinPeriode(abonnement.PeriodeDebut, offre);
                }

                abonnement.AlerteQuotaEnvoyee = false;
                modifie = true;
            }

            if (modifie) await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Fin de période. AddMonths et non 30 jours : un abonnement souscrit le
        /// 31 janvier doit se renouveler le 28 février, pas le 2 mars.
        /// </summary>
        private static DateTime FinPeriode(DateTime debut, Offre offre) =>
            offre.JoursValidite > 0 ? debut.AddDays(offre.JoursValidite) : debut.AddMonths(1);

        private static bool PauseDisponible(Abonnement abonnement) =>
            abonnement.DernierePause is null
            || DateTime.UtcNow - abonnement.DernierePause.Value >= DelaiEntrePauses;

        /// <summary>
        /// Cet enfant a-t-il une place sur la formule ?
        ///
        /// Les places se prennent à l'usage et non à l'inscription : un parent
        /// de quatre enfants qui prend Solo choisit lui-même qui travaille, sans
        /// avoir à désigner un enfant dans un écran de réglages. Celui qui a
        /// commencé garde sa place jusqu'au renouvellement — sinon deux enfants
        /// se la voleraient à tour de rôle et aucun ne pourrait finir.
        /// </summary>
        private async Task<bool> PlaceDisponibleAsync(
            Abonnement abonnement, int eleveId, int maximum, CancellationToken ct)
        {
            var occupants = await _context.ConsommationsEleves
                .AsNoTracking()
                .Where(c => c.AbonnementId == abonnement.Id
                            && c.PeriodeDebut == abonnement.PeriodeDebut
                            && c.SecondesConsommees > 0)
                .Select(c => c.EleveId)
                .ToListAsync(ct);

            // Déjà en train de travailler ce mois-ci : sa place est acquise.
            if (occupants.Contains(eleveId)) return true;

            return occupants.Count < maximum;
        }

        /// <summary>
        /// Les minutes rechargées valables sur la période en cours.
        ///
        /// L'ÉGALITÉ DE DATE NE SUFFIT PAS, ET ÇA A COÛTÉ TROIS HEURES EN
        /// PRODUCTION LE 04/09/2026.
        /// ------------------------------------------------------------------
        /// Une recharge portait la `PeriodeDebut` de l'abonnement au moment
        /// de l'achat, et le rattachement se faisait par égalité stricte.
        /// Sauf que cette date est RÉÉCRITE quelques instants plus tard :
        /// Stripe envoie `invoice.paid` juste après `checkout.session.completed`,
        /// et `ReporterPeriodeAsync` remplace la période par celle de la
        /// facture — un horodatage à la seconde entière, donc voisin mais
        /// jamais identique au nôtre.
        ///
        /// L'égalité tombait, la somme rendait zéro, et les heures créditées
        /// disparaissaient de la vue SANS disparaître de la base. Le parent
        /// voyait 9 h là où la ligne existait bel et bien.
        ///
        /// CE N'ÉTAIT PAS PROPRE À L'OFFRE DE LANCEMENT : n'importe quel
        /// pack acheté dans la minute qui suit une souscription se perdait de
        /// la même façon. On vendait 14,90 € des heures qui devenaient
        /// invisibles.
        ///
        /// LA DATE D'ACHAT EST LE VRAI CRITÈRE. Une recharge appartient à la
        /// période PENDANT LAQUELLE elle a été achetée. `PeriodeDebut` était
        /// une tentative d'enregistrer ce fait ; la fenêtre le mesure
        /// directement, et rien ne peut la redater sous nos pieds.
        ///
        /// L'ANCIENNE RÈGLE EST GARDÉE EN PREMIÈRE BRANCHE, et ce n'est pas
        /// de la timidité : elle est juste, elle couvre tout ce qui
        /// fonctionne aujourd'hui, et la seconde branche ne fait que
        /// rattraper ce qu'elle rate. Ensemble elles ne peuvent pas ramener
        /// une recharge d'une AUTRE période — celles du mois passé ont une
        /// date d'achat antérieure au début de la période en cours, et un
        /// horodatage qui ne correspond plus.
        ///
        /// AUCUNE MIGRATION DE DONNÉES : les recharges déjà orphelines
        /// redeviennent visibles au premier redémarrage.
        /// </summary>
        /// <summary>
        /// Ce que ce compte a le droit de consommer, lu dans la vue.
        ///
        /// UN REPLI EN DUR SI LA VUE NE RÉPOND PAS. Elle ne peut manquer
        /// qu'entre le déploiement du code et l'application de la migration
        /// — quelques secondes au démarrage. Pendant ce laps, mieux vaut
        /// rendre le forfait de base qu'échouer : le parent voit ses heures
        /// de formule, sans ses recharges, plutôt qu'une erreur.
        /// </summary>
        private async Task<ForfaitAbonnement> ForfaitAsync(
            Abonnement abonnement, Offre offre, CancellationToken ct)
        {
            var id = abonnement.Id;

            var lu = await _context.ForfaitsAbonnement
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.AbonnementId == id, ct);

            return lu ?? new ForfaitAbonnement
            {
                AbonnementId = id,
                MinutesForfait = offre.MinutesPotMensuel,
                MinutesRecharge = 0,
                MinutesAllouees = offre.MinutesPotMensuel,
                MinutesPlafondEnfant = offre.MinutesPlafondEnfant,
            };
        }

        private Task<int> MinutesRechargeAsync(Abonnement abonnement, CancellationToken ct)
        {
            var id = abonnement.Id;

            // ZÉRO SI LA VUE NE RÉPOND PAS : voir `ForfaitAsync`. Cette
            // somme sert à borner un ajustement, jamais à autoriser une
            // consommation — un zéro y est prudent, pas dangereux.
            return _context.ForfaitsAbonnement
                .AsNoTracking()
                .Where(f => f.AbonnementId == id)
                .Select(f => f.MinutesRecharge)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>Minutes consommées : par la fratrie, et par cet enfant.</summary>
        private async Task<(int Pot, int Enfant)> ConsommationAsync(
            Abonnement abonnement, int eleveId, CancellationToken ct)
        {
            var lignes = await _context.ConsommationsEleves
                .AsNoTracking()
                .Where(c => c.AbonnementId == abonnement.Id
                            && c.PeriodeDebut == abonnement.PeriodeDebut)
                .Select(c => new { c.EleveId, c.SecondesConsommees })
                .ToListAsync(ct);

            var pot = lignes.Sum(l => l.SecondesConsommees) / 60;
            var enfant = lignes.Where(l => l.EleveId == eleveId).Sum(l => l.SecondesConsommees) / 60;

            return (pot, enfant);
        }

        private async Task<EtatQuota> ComposerAsync(Abonnement abonnement, CancellationToken ct)
        {
            var offre = abonnement.Offre
                        ?? await _context.Offres.FirstAsync(o => o.Id == abonnement.OffreId, ct);

            var recharge = await MinutesRechargeAsync(abonnement, ct);

            var lignes = await _context.ConsommationsEleves
                .AsNoTracking()
                .Where(c => c.AbonnementId == abonnement.Id
                            && c.PeriodeDebut == abonnement.PeriodeDebut)
                .Select(c => new { c.EleveId, Prenom = c.Eleve!.Prenom, c.SecondesConsommees })
                .ToListAsync(ct);

            // Tous les enfants du compte, y compris ceux qui n'ont rien consommé :
            // un enfant absent de la liste se lit comme un enfant supprimé.
            var enfants = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.ParentId == abonnement.ParentId)
                .OrderBy(e => e.Prenom)
                .Select(e => new { e.Id, e.Prenom })
                .ToListAsync(ct);

            return new EtatQuota
            {
                AbonnementId = abonnement.Id,
                OffreCode = offre.Code,
                OffreLibelle = offre.Libelle,
                EstEssai = offre.EstEssai,
                Periodicite = abonnement.Periodicite,
                Statut = abonnement.Statut,
                PeriodeDebut = abonnement.PeriodeDebut,
                PeriodeFin = abonnement.PeriodeFin,
                NombreEnfantsMax = offre.NombreEnfantsMax,
                MinutesAllouees = offre.MinutesPotMensuel + recharge,
                MinutesRecharge = recharge,
                MinutesConsommees = lignes.Sum(l => l.SecondesConsommees) / 60,

                // LE PLAFOND ANNONCÉ SUIT LES RECHARGES, comme le verdict.
                // C'est ici que le décalage se voyait : l'écran promettait
                // 12 h allouées et 9 h de plafond, et le moteur refusait à 9 h.
                // Deux chiffres qui décrivent la même règle doivent sortir du
                // même calcul, sans quoi celui qui affiche et celui qui décide
                // finissent par se contredire.
                MinutesPlafondEnfant = offre.MinutesPlafondEnfant + recharge,
                Impaye = abonnement.ImpayeDepuis is not null,
                ImpayeDepuis = abonnement.ImpayeDepuis,
                OffrePrevueCode = abonnement.OffrePrevue?.Code,
                OffrePrevueLibelle = abonnement.OffrePrevue?.Libelle,
                PeriodicitePrevue = abonnement.PeriodicitePrevue,
                ChangementPrevuLe = abonnement.ChangementPrevuLe,
                EnPause = abonnement.Statut == StatutAbonnement.EnPause,
                PauseJusquau = abonnement.PauseJusquau,
                PauseDisponible = !offre.EstEssai && PauseDisponible(abonnement),
                ResiliationDemandee = abonnement.ResiliationDemandeeLe is not null,
                // La fin réelle est celle de la période en cours — pas la date
                // de la demande. L'écran doit pouvoir écrire « vous gardez tout
                // jusqu'au 6 septembre » sans avoir à la calculer.
                FinPrevue = abonnement.ResiliationDemandeeLe is null ? null : abonnement.PeriodeFin,
                Enfants = enfants
                    .Select(e => new QuotaEnfant
                    {
                        EleveId = e.Id,
                        Prenom = e.Prenom,
                        MinutesConsommees =
                            lignes.Where(l => l.EleveId == e.Id).Sum(l => l.SecondesConsommees) / 60,
                        MinutesPlafond = offre.MinutesPlafondEnfant + recharge,
                    })
                    .ToList(),
            };
        }
    }
}
