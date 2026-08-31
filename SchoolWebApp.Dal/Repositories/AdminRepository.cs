using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        /// <summary>
        /// Origine des découpages temporels. Un lundi, pour que les semaines
        /// commencent bien le lundi et non le dimanche comme le ferait
        /// DATEDIFF(week) de SQL Server.
        /// </summary>
        private static readonly DateTime Origine = new(2001, 1, 1);

        private readonly SchoolWebAppDatabaseContext _context;

        public AdminRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ------------------------------------------------------------------
        // Résumé
        // ------------------------------------------------------------------
        public async Task<ResumeAdmin> GetResumeAsync()
        {
            var maintenant = DateTime.UtcNow;
            var debutJour = maintenant.Date;
            var trenteJours = maintenant.AddDays(-30);

            var requetes = _context.Messages.AsNoTracking().Where(m => m.Role == "assistant");

            return new ResumeAdmin
            {
                NombreParents = await _context.Parents.CountAsync(),
                NombreEleves = await _context.Eleves.CountAsync(),
                NombreConversations = await _context.Conversations.CountAsync(),
                NombreRequetes = await requetes.CountAsync(),
                RequetesAujourdhui = await requetes.CountAsync(m => m.DateCreation >= debutJour),
                RequetesTrenteJours = await requetes.CountAsync(m => m.DateCreation >= trenteJours),
                TokensEntree = await requetes.SumAsync(m => (long)m.TokensEntree),
                TokensSortie = await requetes.SumAsync(m => (long)m.TokensSortie),
                TokensCacheLecture = await requetes.SumAsync(m => (long)m.TokensCacheLecture)
            };
        }

        // ------------------------------------------------------------------
        // Séries temporelles
        // ------------------------------------------------------------------
        public async Task<IEnumerable<PointSerie>> GetSerieRequetesAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId)
        {
            var requete = _context.Messages
                .AsNoTracking()
                .Where(m => m.Role == "assistant")
                .Where(m => m.DateCreation >= debut && m.DateCreation < fin);

            if (eleveId.HasValue)
            {
                requete = requete.Where(m => m.Conversation!.EleveId == eleveId.Value);
            }

            var seaux = await Grouper(requete.Select(m => m.DateCreation), granularite);
            return Completer(seaux, granularite, debut, fin, cumulDepart: null);
        }

        public async Task<IEnumerable<PointSerie>> GetSerieParentsAsync(
            Granularite granularite, DateTime debut, DateTime fin)
        {
            var source = _context.Parents.AsNoTracking();

            // Le cumul doit démarrer au nombre de comptes déjà existants avant
            // la fenêtre, sinon la courbe repartirait de zéro à chaque filtre.
            var avant = await source.CountAsync(p => p.DateCreation < debut);

            var seaux = await Grouper(
                source.Where(p => p.DateCreation >= debut && p.DateCreation < fin)
                      .Select(p => p.DateCreation),
                granularite);

            return Completer(seaux, granularite, debut, fin, avant);
        }

        public async Task<IEnumerable<PointSerie>> GetSerieElevesAsync(
            Granularite granularite, DateTime debut, DateTime fin)
        {
            var source = _context.Eleves.AsNoTracking();
            var avant = await source.CountAsync(e => e.DateCreation < debut);

            var seaux = await Grouper(
                source.Where(e => e.DateCreation >= debut && e.DateCreation < fin)
                      .Select(e => e.DateCreation),
                granularite);

            return Completer(seaux, granularite, debut, fin, avant);
        }

        /// <summary>
        /// Les VISITEURS UNIQUES par période.
        ///
        /// POURQUOI CETTE SÉRIE NE PASSE PAS PAR `Grouper`
        /// ---------------------------------------------
        /// Toutes les autres comptent des LIGNES : une inscription, un
        /// message, une résiliation. Ici on compte des PERSONNES, et la même
        /// personne laisse plusieurs lignes dans la même période — elle
        /// revient, elle ouvre un second onglet, elle lit deux pages à une
        /// heure d'intervalle.
        ///
        /// On dédoublonne donc À L'INTÉRIEUR de chaque seau, et le seau est
        /// calculé en SQL comme ailleurs : c'est la seule forme que SQL Server
        /// sait grouper sans ramener toutes les lignes en mémoire.
        ///
        /// LE TOTAL D'UNE PÉRIODE N'EST PAS LA SOMME DE SES PARTIES, et c'est
        /// normal : quelqu'un qui vient lundi et jeudi compte une fois dans la
        /// semaine et deux fois dans le détail par jour. Le cumul renvoyé ici
        /// est donc une somme de visites-jours, pas un nombre de personnes —
        /// l'affichage ne s'en sert pas.
        /// </summary>
        public async Task<IEnumerable<PointSerie>> GetSerieVisitesAsync(
            Granularite granularite, DateTime debut, DateTime fin)
        {
            var visites = _context.VisitesSite
                .AsNoTracking()
                .Where(v => v.Horodatage >= debut && v.Horodatage < fin);

            IQueryable<IGrouping<int, VisiteSite>> groupes = granularite switch
            {
                Granularite.Heure =>
                    visites.GroupBy(v => EF.Functions.DateDiffHour(Origine, v.Horodatage)),

                Granularite.Jour =>
                    visites.GroupBy(v => EF.Functions.DateDiffDay(Origine, v.Horodatage)),

                Granularite.Semaine =>
                    visites.GroupBy(v => EF.Functions.DateDiffDay(Origine, v.Horodatage) / 7),

                Granularite.Mois =>
                    visites.GroupBy(v => EF.Functions.DateDiffMonth(Origine, v.Horodatage)),

                _ =>
                    visites.GroupBy(v => EF.Functions.DateDiffYear(Origine, v.Horodatage)),
            };

            var seaux = await groupes
                .Select(g => new { Seau = g.Key, Nombre = g.Select(v => v.Visiteur).Distinct().Count() })
                .ToDictionaryAsync(x => x.Seau, x => x.Nombre);

            return Completer(seaux, granularite, debut, fin, cumulDepart: null);
        }

        /// <summary>
        /// Combien de personnes distinctes sur TOUTE la fenêtre.
        ///
        /// Ce chiffre ne se déduit pas de la série : additionner les seaux
        /// compterait deux fois celui qui est venu deux jours de suite. C'est
        /// une requête à part, et c'est elle que le bandeau affiche.
        /// </summary>
        public Task<int> CompterVisiteursAsync(DateTime debut, DateTime fin) =>
            _context.VisitesSite
                .AsNoTracking()
                .Where(v => v.Horodatage >= debut && v.Horodatage < fin)
                .Select(v => v.Visiteur)
                .Distinct()
                .CountAsync();

        /// <summary>
        /// Note une venue, sauf si ce visiteur s'est déjà signalé dans l'heure.
        ///
        /// LA RETENUE EST ICI ET PAS SEULEMENT DANS LE NAVIGATEUR. Celui-ci se
        /// tait déjà une heure après chaque signalement, mais il suffit d'un
        /// stockage local vidé — ou d'un appel rejoué à la main — pour que la
        /// table enfle. Le comptage étant dédoublonné de toute façon, ces
        /// lignes-là n'apporteraient rien qu'un coût de stockage.
        /// </summary>
        public async Task EnregistrerVisiteAsync(string visiteur, DateTime quand)
        {
            var depuis = quand.AddHours(-1);

            var dejaVu = await _context.VisitesSite
                .AsNoTracking()
                .AnyAsync(v => v.Visiteur == visiteur && v.Horodatage >= depuis);

            if (dejaVu) return;

            _context.VisitesSite.Add(new VisiteSite { Visiteur = visiteur, Horodatage = quand });
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Regroupe côté SQL. On calcule un index de seau entier — nombre de
        /// jours, semaines, mois ou années depuis l'origine — parce que c'est
        /// la seule forme que SQL Server sait grouper sans ramener toutes les
        /// lignes en mémoire.
        /// </summary>
        private static async Task<Dictionary<int, int>> Grouper(
            IQueryable<DateTime> dates, Granularite granularite)
        {
            IQueryable<IGrouping<int, DateTime>> groupes = granularite switch
            {
                Granularite.Heure =>
                    dates.GroupBy(d => EF.Functions.DateDiffHour(Origine, d)),

                Granularite.Jour =>
                    dates.GroupBy(d => EF.Functions.DateDiffDay(Origine, d)),

                // Division entière sur le nombre de jours : DATEDIFF(week) de
                // SQL Server compte les dimanches, ce qui décalerait les libellés.
                Granularite.Semaine =>
                    dates.GroupBy(d => EF.Functions.DateDiffDay(Origine, d) / 7),

                Granularite.Mois =>
                    dates.GroupBy(d => EF.Functions.DateDiffMonth(Origine, d)),

                _ =>
                    dates.GroupBy(d => EF.Functions.DateDiffYear(Origine, d)),
            };

            return await groupes
                .Select(g => new { Seau = g.Key, Nombre = g.Count() })
                .ToDictionaryAsync(x => x.Seau, x => x.Nombre);
        }

        /// <summary>
        /// Reconstruit la série complète : les périodes sans aucune donnée
        /// doivent apparaître à zéro, sinon le graphique ment en reliant deux
        /// points distants comme s'il ne s'était rien passé entre les deux.
        /// </summary>
        private static List<PointSerie> Completer(
            Dictionary<int, int> seaux,
            Granularite granularite,
            DateTime debut,
            DateTime fin,
            int? cumulDepart)
        {
            var points = new List<PointSerie>();
            var cumul = cumulDepart ?? 0;

            var seau = IndexSeau(debut, granularite);
            var seauFin = IndexSeau(fin.AddTicks(-1), granularite);

            // Garde-fou : une plage absurde (dix ans en granularité jour)
            // produirait des milliers de points illisibles et lourds.
            const int MaxPoints = 800;

            while (seau <= seauFin && points.Count < MaxPoints)
            {
                var valeur = seaux.TryGetValue(seau, out var n) ? n : 0;
                cumul += valeur;

                points.Add(new PointSerie
                {
                    Periode = DateDuSeau(seau, granularite),
                    Valeur = valeur,
                    Cumul = cumul
                });

                seau++;
            }

            return points;
        }

        /// <summary>
        /// L'état du parc d'abonnements période par période.
        ///
        /// LE STOCK NE SE CALCULE PAS COMME UN FLUX
        /// ---------------------------------------
        /// Les autres séries comptent des événements datés : une inscription
        /// tombe dans une période et une seule. Un abonnement actif, non — il
        /// compte dans TOUTES les périodes qu'il traverse. On ne peut donc pas
        /// le grouper ; il faut, pour chaque période, compter les abonnements
        /// qui l'enjambent.
        ///
        /// ON NE SE SERT PAS DE `Statut` POUR L'HISTORIQUE
        /// ----------------------------------------------
        /// `Statut` dit où en est l'abonnement AUJOURD'HUI. S'en servir pour
        /// reconstituer le passé donnerait un parc qui n'a jamais existé : un
        /// abonnement résilié hier apparaîtrait comme inactif l'an dernier, et
        /// la courbe s'effondrerait rétroactivement à chaque résiliation. Ce
        /// sont les DATES qui portent la vérité historique.
        /// </summary>
        public async Task<IEnumerable<PointAbonnements>> GetSerieAbonnementsAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId)
        {
            var source = _context.Abonnements.AsNoTracking();

            // Le filtre « élève » se traduit en filtre PARENT : un abonnement
            // appartient à une famille, pas à un enfant. Filtrer sur Emma
            // montre donc le contrat de ses parents — ce qui est bien ce qu'on
            // veut voir quand on enquête sur un compte.
            if (eleveId is int id)
            {
                var parentId = await _context.Eleves
                    .AsNoTracking()
                    .Where(e => e.Id == id)
                    .Select(e => (int?)e.ParentId)
                    .FirstOrDefaultAsync();

                if (parentId is null) return Array.Empty<PointAbonnements>();

                source = source.Where(a => a.ParentId == parentId);
            }

            // Les trois flux se groupent comme n'importe quel événement daté.
            var nouveaux = await Grouper(
                source.Where(a => a.DateDebut >= debut && a.DateDebut < fin)
                      .Select(a => a.DateDebut),
                granularite);

            var demandes = await Grouper(
                source.Where(a => a.ResiliationDemandeeLe != null
                                  && a.ResiliationDemandeeLe >= debut
                                  && a.ResiliationDemandeeLe < fin)
                      .Select(a => a.ResiliationDemandeeLe!.Value),
                granularite);

            var arrets = await Grouper(
                source.Where(a => a.DateFin != null
                                  && a.DateFin >= debut
                                  && a.DateFin < fin)
                      .Select(a => a.DateFin!.Value),
                granularite);

            // Pour le stock, on ramène les bornes des abonnements qui touchent
            // la fenêtre — pas tous, seulement ceux qui la chevauchent.
            var bornes = await source
                .Where(a => a.DateDebut < fin && (a.DateFin == null || a.DateFin >= debut))
                .Select(a => new { a.DateDebut, a.DateFin })
                .ToListAsync();

            var points = new List<PointAbonnements>();
            var seau = IndexSeau(debut, granularite);
            var seauFin = IndexSeau(fin.AddTicks(-1), granularite);

            // Même garde-fou que les autres séries : une plage absurde
            // produirait des milliers de points illisibles.
            const int MaxPoints = 800;

            while (seau <= seauFin && points.Count < MaxPoints)
            {
                // Les seaux sont séquentiels : le suivant commence exactement
                // là où celui-ci finit. C'est cet instant qu'on interroge —
                // « combien d'abonnements en cours à la fin de la période ».
                var finPeriode = DateDuSeau(seau + 1, granularite);

                points.Add(new PointAbonnements
                {
                    Periode = DateDuSeau(seau, granularite),
                    Actifs = bornes.Count(b => b.DateDebut < finPeriode
                                               && (b.DateFin == null || b.DateFin >= finPeriode)),
                    Nouveaux = nouveaux.TryGetValue(seau, out var n) ? n : 0,
                    Demandes = demandes.TryGetValue(seau, out var d) ? d : 0,
                    Arrets = arrets.TryGetValue(seau, out var a) ? a : 0,
                });

                seau++;
            }

            return points;
        }

        private static int IndexSeau(DateTime date, Granularite granularite) => granularite switch
        {
            Granularite.Heure => (int)(date - Origine).TotalHours,
            Granularite.Jour => (int)(date.Date - Origine).TotalDays,
            Granularite.Semaine => (int)(date.Date - Origine).TotalDays / 7,
            Granularite.Mois => (date.Year - Origine.Year) * 12 + date.Month - Origine.Month,
            _ => date.Year - Origine.Year
        };

        private static DateTime DateDuSeau(int seau, Granularite granularite) => granularite switch
        {
            Granularite.Heure => Origine.AddHours(seau),
            Granularite.Jour => Origine.AddDays(seau),
            Granularite.Semaine => Origine.AddDays(seau * 7),
            Granularite.Mois => Origine.AddMonths(seau),
            _ => Origine.AddYears(seau)
        };

        /// <summary>
        /// Ce que le produit a brûlé entre deux dates, toutes familles
        /// confondues.
        ///
        /// LE MÊME CALCUL QUE LA COLONNE DU TABLEAU, sur une autre découpe :
        /// mêmes tarifs, mêmes multiplicateurs de cache, même déduction pour
        /// la voix. Deux formules qui divergeraient donneraient deux vérités,
        /// et c'est celle qu'on ne regarde pas qui aurait raison le jour où
        /// ça compte.
        /// </summary>
        /// <summary>
        /// La place occupée par la base. Trois lectures, aucune écriture.
        /// </summary>
        public async Task<EtatBase> GetEtatBaseAsync()
        {
            // `FILEPROPERTY(name, 'SpaceUsed')` ET NON `size`. LA DIFFÉRENCE
            // EST TOUT LE SUJET DE CET ÉCRAN.
            //
            // `size` est la taille ALLOUÉE du fichier. Supprimer des lignes
            // libère la place à l'intérieur sans rendre un octet au disque :
            // après la purge de dix mille documents, `size` ne bouge pas d'un
            // mégaoctet. Une jauge bâtie dessus serait restée à 90 % alors que
            // la base venait de se vider — et aurait déclenché une panique, ou
            // pire, un `SHRINKFILE` de nuit sur la production.
            //
            // C'est aussi le chiffre qui décrit vraiment le mur : on ne
            // s'arrête pas quand le FICHIER atteint dix gigaoctets, on s'arrête
            // quand la place OCCUPÉE ne peut plus grandir dedans.
            //
            // Le journal garde `size` : `FILEPROPERTY` ne s'applique pas à lui,
            // et de toute façon il n'entre pas dans le plafond.
            const string sqlFichiers = @"
SELECT
    CAST(ISNULL(SUM(CASE WHEN type_desc = 'ROWS'
                         THEN CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) END), 0)
         * 8 / 1024 AS int) AS DonneesMo,
    CAST(ISNULL(SUM(CASE WHEN type_desc = 'LOG'
                         THEN CAST(size AS bigint) END), 0)
         * 8 / 1024 AS int) AS JournalMo
FROM sys.database_files;";

            var fichiers = (await _context.Database
                .SqlQueryRaw<TailleFichiers>(sqlFichiers)
                .ToListAsync())
                .FirstOrDefault() ?? new TailleFichiers();

            // `AS Value` n'est pas cosmétique : `SqlQueryRaw<T>` sur un type
            // scalaire cherche une colonne portant EXACTEMENT ce nom, et lève
            // une exception dont le message parle de mapping, pas de nom de
            // colonne.
            var edition = (await _context.Database
                .SqlQueryRaw<string>("SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128)) AS Value")
                .ToListAsync())
                .FirstOrDefault() ?? string.Empty;

            // Les octets encore présents, donc ceux que la rétention commande.
            // `donnees_effacees_le IS NULL` est la même condition que la purge :
            // une ligne allégée garde sa taille d'origine en colonne, et la
            // compter ferait afficher de la place qui n'existe plus.
            var documents = await _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.DonneesEffaceesLe == null)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Nombre = g.Count(),
                    Octets = (long?)g.Sum(p => (long)p.Taille) ?? 0L,
                })
                .FirstOrDefaultAsync();

            return new EtatBase
            {
                Edition = edition,
                DonneesMo = fichiers.DonneesMo,
                JournalMo = fichiers.JournalMo,

                // DIX GIGAOCTETS, ET SEULEMENT SI C'EST ÉCRIT « EXPRESS ».
                // Standard et Azure SQL n'ont pas ce plafond : en déduire un
                // ferait afficher une jauge inventée après une migration.
                PlafondMo = edition.Contains("Express", StringComparison.OrdinalIgnoreCase)
                    ? 10 * 1024
                    : null,

                DocumentsNombre = documents?.Nombre ?? 0,
                DocumentsMo = (int)((documents?.Octets ?? 0L) / (1024 * 1024)),
            };
        }

        private sealed class TailleFichiers
        {
            public int DonneesMo { get; set; }

            public int JournalMo { get; set; }
        }

        public async Task<CoutPeriode> GetCoutAsync(DateTime debut, DateTime fin)
        {
            var seaux = await _context.Messages
                .AsNoTracking()
                .Where(m => m.TokensEntree > 0
                            && m.DateCreation >= debut && m.DateCreation < fin)
                .GroupBy(m => new { m.Modele, m.DateCreation.Year, m.DateCreation.Month })
                .Select(g => new
                {
                    g.Key.Modele,
                    g.Key.Year,
                    g.Key.Month,
                    Tours = g.Count(),
                    Entree = g.Sum(m => (long)m.TokensEntree),
                    Sortie = g.Sum(m => (long)m.TokensSortie),
                    CacheLu = g.Sum(m => (long)m.TokensCacheLecture),
                    CacheEcrit = g.Sum(m => (long)m.TokensCacheEcriture),
                })
                .ToListAsync();

            var dialogue = 0m;
            var tours = 0;

            foreach (var s in seaux)
            {
                var (pe, ps) = Tarif(s.Modele, new DateTime(s.Year, s.Month, 1));

                dialogue += (s.Entree * pe
                             + s.CacheLu * pe * 0.1m
                             + s.CacheEcrit * pe * 1.25m
                             + s.Sortie * ps) / 1_000_000m;

                tours += s.Tours;
            }

            var voix = VoixDollars(tours);

            // LES TÂCHES DE FOND, QUI DÉPENSAIENT EN SILENCE.
            //
            // Décrire une planche, en cartographier les repères, observer une
            // séance, transcrire un document : quatre postes qui appellent le
            // modèle sans appartenir à aucune conversation. Ils n'étaient nulle
            // part dans ce total, et une facture qui montait sans que le
            // tableau ne bouge n'avait aucune explication visible.
            var fond = await _context.AppelsClaude
                .AsNoTracking()
                .Where(a => a.DateCreation >= debut && a.DateCreation < fin)
                .GroupBy(a => new { a.Origine, a.Modele, a.DateCreation.Year, a.DateCreation.Month })
                .Select(g => new
                {
                    g.Key.Origine,
                    g.Key.Modele,
                    g.Key.Year,
                    g.Key.Month,
                    Appels = g.Count(),
                    Entree = g.Sum(a => (long)a.TokensEntree),
                    Sortie = g.Sum(a => (long)a.TokensSortie),
                    CacheLu = g.Sum(a => (long)a.TokensCacheLecture),
                    CacheEcrit = g.Sum(a => (long)a.TokensCacheEcriture),
                })
                .ToListAsync();

            var postes = new List<PosteCout>();

            foreach (var groupe in fond.GroupBy(f => f.Origine))
            {
                var cout = 0m;
                var appels = 0;

                foreach (var f in groupe)
                {
                    var (pe, ps) = Tarif(f.Modele, new DateTime(f.Year, f.Month, 1));

                    cout += (f.Entree * pe
                             + f.CacheLu * pe * 0.1m
                             + f.CacheEcrit * pe * 1.25m
                             + f.Sortie * ps) / 1_000_000m;

                    appels += f.Appels;
                }

                postes.Add(new PosteCout
                {
                    Origine = groupe.Key,
                    Appels = appels,
                    Dollars = Math.Round(cout, 4),
                });
            }

            var tachesDeFond = postes.Sum(p => p.Dollars);

            return new CoutPeriode
            {
                Debut = debut,
                Fin = fin,
                Tours = tours,
                DialogueDollars = Math.Round(dialogue, 4),
                VoixDollars = Math.Round(voix, 4),
                TachesDeFondDollars = Math.Round(tachesDeFond, 4),
                Postes = postes.OrderByDescending(p => p.Dollars).ToList(),
                TotalDollars = Math.Round(dialogue + voix + tachesDeFond, 4),
                MinutesTravaillees = await MinutesTravailleesAsync(debut, fin),
            };
        }

        // ------------------------------------------------------------------
        // Tableaux
        // ------------------------------------------------------------------
        /// <summary>
        /// Le tableau des familles, SUR UNE FENÊTRE DE TEMPS.
        ///
        /// Trois colonnes suivent la fenêtre : le temps de cours, ce qu'il a
        /// coûté, et la part du forfait qu'il représente. Le reste — formule,
        /// nombre d'enfants, date d'inscription — n'a pas de sens périodique et
        /// reste tel quel.
        ///
        /// LE POT, LUI, RESTE MENSUEL. C'est celui de la formule, il ne se
        /// découpe pas : sur une fenêtre d'un jour, le pourcentage se lit donc
        /// « ce jour a mangé tant du mois », ce qui est exactement la question
        /// qu'on se pose en regardant un jour.
        /// </summary>
        public async Task<IEnumerable<ParentAdmin>> GetParentsAsync(
            string? recherche, DateTime debut, DateTime fin)
        {
            var requete = _context.Parents.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                var terme = recherche.Trim();
                requete = requete.Where(p =>
                    (p.Mail != null && p.Mail.Contains(terme)) ||
                    (p.Nom != null && p.Nom.Contains(terme)) ||
                    (p.Prenom != null && p.Prenom.Contains(terme)));
            }

            var lignes = await requete
                .OrderByDescending(p => p.DateCreation)
                .Select(p => new ParentAdmin
                {
                    Id = p.Id,
                    Prenom = p.Prenom,
                    Nom = p.Nom,
                    Mail = p.Mail,
                    DateCreation = p.DateCreation,
                    NombreEleves = p.Eleves.Count,
                    NombreRequetes = p.Eleves
                        .SelectMany(e => e.Conversations)
                        .SelectMany(c => c.Messages)
                        .Count(m => m.Role == "assistant"),
                    DerniereActivite = p.Eleves
                        .Where(e => e.DerniereActivite != null)
                        .Max(e => e.DerniereActivite),

                    // LA CONSOMMATION DE LA PÉRIODE EN COURS.
                    //
                    // On prend l'abonnement ACTIF, et le plus récent s'il y en
                    // a plusieurs — un parent qui change de formule laisse
                    // derrière lui des lignes résiliées qui fausseraient tout.
                    //
                    // Les consommations sont filtrées sur `PeriodeDebut` : la
                    // table garde l'historique des périodes précédentes, et les
                    // sommer toutes montrerait un pot consommé à 300 %.
                    //
                    // Les recharges s'ajoutent au pot, sur la même période. Les
                    // oublier ferait apparaître à plus de 100 % une famille qui
                    // a simplement acheté des heures — et donc payé pour.
                    Formule = p.Abonnements
                        .Where(a => a.Statut == "Actif")
                        .OrderByDescending(a => a.PeriodeDebut)
                        .Select(a => a.Offre!.Code)
                        .FirstOrDefault(),

                    // L'annuel ramené au mois : les coûts se lisent au mois, et
                    // les comparer à un prix annuel donnerait une marge douze
                    // fois trop belle.
                    PrixMensuelCentimes = p.Abonnements
                        .Where(a => a.Statut == "Actif")
                        .OrderByDescending(a => a.PeriodeDebut)
                        .Select(a => a.Periodicite == "Annuel"
                            ? (int?)(a.Offre!.PrixAnnuelCentimes / 12)
                            : a.Offre!.PrixMensuelCentimes)
                        .FirstOrDefault(),

                    // LA MÊME SOURCE ET LA MÊME FENÊTRE QUE L'ÉCRAN DU PARENT.
                    //
                    // Ce chiffre venait d'un calcul SQL sur les écarts entre
                    // messages, borné par le FILTRE DE PÉRIODE de l'écran —
                    // pendant que le dénominateur, lui, venait de la période
                    // d'ABONNEMENT. Le rapport « 3 min / 14 h » mélangeait donc
                    // deux fenêtres, et l'administration affichait 3 minutes là
                    // où le parent en voyait 5.
                    //
                    // Deux chiffres pour la même chose, c'est celui qu'on ne
                    // regarde pas qui a raison le jour où ça compte. On prend
                    // celui du parent : c'est lui qui décompte réellement son
                    // forfait, et c'est lui qu'il verra si jamais il conteste.
                    MinutesConsommees = p.Abonnements
                        .Where(a => a.Statut == "Actif")
                        .OrderByDescending(a => a.PeriodeDebut)
                        .Select(a => (int?)(a.Consommations
                            .Where(c => c.PeriodeDebut == a.PeriodeDebut)
                            .Sum(c => c.SecondesConsommees) / 60))
                        .FirstOrDefault(),

                    MinutesPot = p.Abonnements
                        .Where(a => a.Statut == "Actif")
                        .OrderByDescending(a => a.PeriodeDebut)
                        .Select(a => (int?)(
                            a.Offre!.MinutesPotMensuel
                            + a.Recharges
                                .Where(r => r.PeriodeDebut == a.PeriodeDebut

                                            // LES HEURES REMBOURSÉES NE COMPTENT PAS.
                                            //
                                            // La même règle que dans
                                            // `AbonnementRepository.MinutesRechargeAsync`, et
                                            // c'est bien le problème : elle est écrite DEUX
                                            // FOIS. Corrigée là-bas et oubliée ici, elle a fait
                                            // afficher 18 h en administration pour un parent qui
                                            // en voyait 14 — l'écran de l'exploitant comptait des
                                            // heures rendues.
                                            //
                                            // Si un jour cette condition change, elle doit
                                            // changer aux deux endroits.
                                            && r.DateRemboursement == null)
                                .Sum(r => r.Minutes)))
                        .FirstOrDefault(),

                })
                .ToListAsync();

            await RenseignerLesCoutsAsync(lignes, debut, fin);

            return lignes;
        }

        /// <summary>
        /// Le tarif d'un modèle, par million de jetons d'entrée et de sortie.
        ///
        /// EN DUR, ET ASSUMÉ. Une table de tarifs en base serait un écran de
        /// plus à tenir à jour, pour une grille qui change deux fois par an et
        /// qu'il faut de toute façon relire à la main quand elle bouge.
        ///
        /// Sonnet 5 a un tarif de lancement jusqu'au 31 août 2026. On applique
        /// celui qui était en vigueur À LA DATE DU MESSAGE : mettre le tarif
        /// plein sur tout l'historique gonflerait le passé de moitié, et c'est
        /// précisément ce passé qu'on compare aux abonnements encaissés.
        /// </summary>
        private static (decimal Entree, decimal Sortie) Tarif(string? modele, DateTime quand)
        {
            if (modele is not null && modele.Contains("opus", StringComparison.OrdinalIgnoreCase))
            {
                return (5m, 25m);
            }

            return quand < new DateTime(2026, 9, 1) ? (2m, 10m) : (3m, 15m);
        }

        /// <summary>
        /// Le temps de cours d'une période, recalculé depuis les messages.
        ///
        /// POURQUOI DU SQL BRUT. La règle a besoin de l'écart entre un tour et
        /// le PRÉCÉDENT de la même conversation, ce qu'on obtient avec `LAG` —
        /// une fonction de fenêtrage qu'EF ne sait pas traduire. L'alternative,
        /// ramener tous les messages en mémoire pour les parcourir, tient
        /// aujourd'hui et ne tiendra pas à dix mille élèves.
        ///
        /// LES BORNES SONT ÉLARGIES DE TROIS MINUTES vers le passé. Le premier
        /// tour d'une période a son précédent AVANT elle : sans ce débordement,
        /// il serait compté au forfait de vingt secondes alors qu'il prolonge
        /// une séance en cours. Trois minutes suffisent — au-delà, la règle
        /// applique le forfait de toute façon.
        ///
        /// Les valeurs sont celles de `ChatViewModelBuilder` : vingt secondes
        /// de plancher, trois minutes de plafond. Elles sont écrites ici en
        /// dur, et c'est le défaut de cette approche — si elles changent
        /// là-bas, ce chiffre dérive en silence.
        /// </summary>
        private async Task<int> MinutesTravailleesAsync(DateTime debut, DateTime fin)
        {
            const string sql = @"
WITH avec_ecart AS (
    SELECT  m.role, m.date_creation,
            DATEDIFF(second,
                     LAG(m.date_creation) OVER (PARTITION BY m.conversation_id
                                                ORDER BY m.id),
                     m.date_creation) AS ecart
    FROM    Message m
    WHERE   m.date_creation >= DATEADD(minute, -3, {0}) AND m.date_creation < {1}
)
-- `AS Value` n'est pas cosmétique : `SqlQueryRaw<T>` sur un type scalaire
-- cherche une colonne portant EXACTEMENT ce nom, et lève une exception
-- sinon. Le message parle de mapping, pas de nom de colonne, et on cherche
-- longtemps ailleurs.
SELECT ISNULL(SUM(CASE WHEN ecart IS NULL OR ecart > 180 THEN 20
                       WHEN ecart < 20 THEN 20
                       ELSE ecart END), 0) AS Value
FROM   avec_ecart
WHERE  role = 'assistant' AND date_creation >= {0}";

            // `ToListAsync` ET NON `FirstAsync`, et ce n'est pas un détail de
            // style. Une requête qui commence par `WITH` est jugée « non
            // composable » par EF : `FirstAsync` voudrait l'envelopper dans un
            // `SELECT TOP 1 … FROM (…)`, ce que SQL Server refuse sur une CTE.
            // L'exception parle de composition, pas de CTE, et on cherche
            // ailleurs — c'est ce qui a produit le 500.
            //
            // `ToListAsync` se contente de matérialiser ce que la requête rend,
            // sans rien ajouter autour.
            var lignes = await _context.Database
                .SqlQueryRaw<int>(sql, debut, fin)
                .ToListAsync();

            return lignes.Count == 0 ? 0 : lignes[0] / 60;
        }

        /// <summary>
        /// Le coût de la synthèse vocale, déduit du nombre de tours de parole.
        ///
        /// RIEN NE LA MESURE, ET IL FAUT LE DIRE. Le dialogue enregistre ses
        /// jetons ; la voix, non — le service de synthèse se facture à la
        /// minute prononcée, que nous ne relevons nulle part.
        ///
        /// La déduction vient de la mesure faite sur une séance réelle de
        /// 45 minutes : environ 45 tours de parole, dont 18 minutes prononcées
        /// par le professeur. Soit quatre dixièmes de minute par tour, à
        /// 0,015 $ la minute.
        ///
        /// Le tour est une meilleure base que la minute de séance : une
        /// famille peut laisser un cours ouvert sans parler, et les minutes
        /// consommées gonfleraient alors une voix qui n'a rien dit.
        /// </summary>
        private static decimal VoixDollars(int tours) => tours * 0.4m * 0.015m;

        /// <summary>
        /// Additionne le coût réel de chaque famille, d'après les jetons.
        ///
        /// UNE SEULE REQUÊTE POUR TOUT LE TABLEAU, et non une par ligne : la
        /// version naïve interrogerait la base autant de fois qu'il y a de
        /// parents, et l'écran ralentirait à mesure que le produit marche —
        /// exactement le mauvais sens.
        ///
        /// Agrégé par (parent, modèle, mois) : assez fin pour que le tarif
        /// s'applique par période, assez grossier pour ne pas ramener des
        /// dizaines de milliers de lignes en mémoire.
        /// </summary>
        private async Task RenseignerLesCoutsAsync(
            List<ParentAdmin> lignes, DateTime debut, DateTime fin)
        {
            if (lignes.Count == 0) return;

            var ids = lignes.Select(l => l.Id).ToList();

            var seaux = await _context.Messages
                .AsNoTracking()
                // `> 0` et non `!= null` : les colonnes ne sont pas nullables.
                // Un tour sans jetons est un message d'élève ou un tour
                // antérieur à la télémétrie — dans les deux cas, zéro à
                // additionner et une ligne de moins à ramener.
                .Where(m => m.TokensEntree > 0
                            && m.DateCreation >= debut && m.DateCreation < fin
                            && ids.Contains(m.Conversation!.Eleve!.ParentId))
                .GroupBy(m => new
                {
                    ParentId = m.Conversation!.Eleve!.ParentId,
                    m.Modele,
                    m.DateCreation.Year,
                    m.DateCreation.Month,
                })
                .Select(g => new
                {
                    g.Key.ParentId,
                    g.Key.Modele,
                    g.Key.Year,
                    g.Key.Month,
                    Tours = g.Count(),
                    Entree = g.Sum(m => (long)m.TokensEntree),
                    Sortie = g.Sum(m => (long)m.TokensSortie),
                    CacheLu = g.Sum(m => (long)m.TokensCacheLecture),
                    CacheEcrit = g.Sum(m => (long)m.TokensCacheEcriture),
                })
                .ToListAsync();

            var dialogue = new Dictionary<int, decimal>();

            foreach (var s in seaux)
            {
                var (entree, sortie) = Tarif(s.Modele, new DateTime(s.Year, s.Month, 1));

                // Lecture de cache à 0,1x, écriture à 1,25x : ce sont les
                // multiplicateurs d'Anthropic, pas des approximations.
                var cout =
                    (s.Entree * entree
                     + s.CacheLu * entree * 0.1m
                     + s.CacheEcrit * entree * 1.25m
                     + s.Sortie * sortie) / 1_000_000m;

                dialogue[s.ParentId] = dialogue.GetValueOrDefault(s.ParentId) + cout;
            }

            // Les tours de la FENÊTRE, et non ceux de toute la vie du compte :
            // la voix se déduit d'eux, et `NombreRequetes` compte depuis
            // l'inscription. Les mélanger aurait donné une voix de plusieurs
            // mois posée sur un dialogue d'une journée.
            var tours = seaux
                .GroupBy(s => s.ParentId)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Tours));

            foreach (var ligne in lignes)
            {
                var parle = dialogue.GetValueOrDefault(ligne.Id);

                ligne.CoutDialogueDollars = Math.Round(parle, 4);
                ligne.CoutDollars = Math.Round(
                    parle + VoixDollars(tours.GetValueOrDefault(ligne.Id)), 4);

                // `MinutesConsommees` n'est PLUS écrasé ici : il vient de la
                // projection, de la table de consommation, sur la période
                // d'abonnement. Le recalculer sur la fenêtre de l'écran, comme
                // c'était fait, remettait les deux chiffres en désaccord.
                //
                // Le coût, lui, reste bien celui de la FENÊTRE : c'est une
                // dépense, elle se lit sur la période qu'on regarde.
            }
        }

        public async Task<IEnumerable<EleveAdmin>> GetElevesAsync(int? parentId, string? recherche)
        {
            var requete = _context.Eleves.AsNoTracking().AsQueryable();

            if (parentId.HasValue)
            {
                requete = requete.Where(e => e.ParentId == parentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                var terme = recherche.Trim();

                // Un terme entièrement numérique ne peut désigner qu'un âge, et
                // on ne cherche que l'âge. Le passer aussi aux champs texte
                // ferait remonter tous les comptes dont l'adresse contient ce
                // chiffre — « prof1785191549@… » répond à « 7 », « 5 », « 1 »…
                // et la recherche par âge deviendrait inutilisable.
                if (int.TryParse(terme, out var age))
                {
                    requete = requete.Where(e => e.Age == age);
                }
                else
                {
                    // Un seul champ balaie prénom, nom, classe et compte parent :
                    // l'admin cherche « Emma », « 6e » ou « dupont » sans avoir à
                    // choisir dans quelle colonne il cherche.
                    requete = requete.Where(e =>
                        (e.Prenom != null && e.Prenom.Contains(terme)) ||
                        (e.Nom != null && e.Nom.Contains(terme)) ||
                        (e.NiveauScolaire!.Libelle != null && e.NiveauScolaire.Libelle.Contains(terme)) ||
                        (e.NiveauScolaire!.Code != null && e.NiveauScolaire.Code.Contains(terme)) ||
                        (e.Parent!.Nom != null && e.Parent.Nom.Contains(terme)) ||
                        (e.Parent!.Prenom != null && e.Parent.Prenom.Contains(terme)) ||
                        (e.Parent!.Mail != null && e.Parent.Mail.Contains(terme)));
                }
            }

            return await requete
                // Du plus récent au plus ancien, comme les comptes parents : ce
                // qu'on vient de créer est ce qu'on vient chercher.
                .OrderByDescending(e => e.DateCreation)
                .Select(e => new EleveAdmin
                {
                    Id = e.Id,
                    ParentId = e.ParentId,
                    Prenom = e.Prenom,
                    Nom = e.Nom,
                    Age = e.Age,
                    Sexe = e.Sexe,
                    NiveauLibelle = e.NiveauScolaire!.Libelle,
                    ParentMail = e.Parent!.Mail,
                    DateCreation = e.DateCreation,
                    DerniereActivite = e.DerniereActivite,
                    NombreRequetes = e.Conversations
                        .SelectMany(c => c.Messages)
                        .Count(m => m.Role == "assistant")
                })
                .ToListAsync();
        }

        /// <summary>
        /// Fiche complète d'un élève.
        ///
        /// Cinq requêtes plutôt qu'une seule : les agrégats par matière, par
        /// compétence et la liste des séances ont des cardinalités
        /// incompatibles. Les fusionner produirait un produit cartésien où le
        /// nombre de requêtes serait multiplié par le nombre de compétences.
        /// </summary>
        public async Task<FicheEleve?> GetFicheEleveAsync(int eleveId)
        {
            var fiche = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => new FicheEleve
                {
                    Id = e.Id,
                    Prenom = e.Prenom,
                    Nom = e.Nom,
                    Age = e.Age,
                    Sexe = e.Sexe,
                    NiveauCode = e.NiveauScolaire!.Code,
                    NiveauLibelle = e.NiveauScolaire.Libelle,
                    NiveauCycle = e.NiveauScolaire.Cycle,
                    ParentId = e.ParentId,
                    ParentMail = e.Parent!.Mail,
                    ParentNomComplet = (e.Parent.Prenom ?? "") + " " + (e.Parent.Nom ?? ""),
                    DateCreation = e.DateCreation,
                    DerniereActivite = e.DerniereActivite
                })
                .FirstOrDefaultAsync();

            if (fiche is null) return null;

            // --- activité par matière ---------------------------------------
            var activite = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId)
                .GroupBy(c => c.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    NombreCours = g.Count(),
                    NombreRequetes = g.SelectMany(c => c.Messages).Count(m => m.Role == "assistant"),
                    DernierCours = g.Max(c => c.DateDernierMessage ?? c.DateCreation)
                })
                .ToListAsync();

            // --- maîtrise par matière ---------------------------------------
            var maitrise = await _context.MaitrisesEleves
                .AsNoTracking()
                .Where(m => m.EleveId == eleveId)
                .GroupBy(m => m.Competence!.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    Evaluees = g.Count(),
                    Moyenne = g.Average(x => x.Score)
                })
                .ToListAsync();

            // --- une ligne par matière active, activité ou non ---------------
            var matieres = await _context.Matieres
                .AsNoTracking()
                .Where(m => m.Active)
                .OrderBy(m => m.Ordre)
                .Select(m => new StatMatiereEleve
                {
                    MatiereId = m.Id,
                    MatiereCode = m.Code,
                    MatiereLibelle = m.Libelle,
                    ProfPrenom = m.ProfPrenom,
                    ProfAvatar = m.ProfAvatar,
                    ProfCouleur = m.ProfCouleur
                })
                .ToListAsync();

            foreach (var matiere in matieres)
            {
                var a = activite.FirstOrDefault(x => x.MatiereId == matiere.MatiereId);
                if (a is not null)
                {
                    matiere.NombreCours = a.NombreCours;
                    matiere.NombreRequetes = a.NombreRequetes;
                    matiere.DernierCours = a.DernierCours;
                }

                var m = maitrise.FirstOrDefault(x => x.MatiereId == matiere.MatiereId);
                if (m is not null)
                {
                    matiere.CompetencesEvaluees = m.Evaluees;
                    matiere.MaitriseMoyenne = m.Moyenne;
                }
            }

            fiche.Matieres = matieres;
            fiche.NombreCours = activite.Sum(a => a.NombreCours);
            fiche.NombreRequetes = activite.Sum(a => a.NombreRequetes);
            fiche.CompetencesEvaluees = maitrise.Sum(m => m.Evaluees);

            // --- derniers cours ---------------------------------------------
            var seances = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId)
                .OrderByDescending(c => c.DateDernierMessage ?? c.DateCreation)
                .Take(10)
                .Select(c => new SeanceEleve
                {
                    ConversationId = c.Id,
                    MatiereId = c.MatiereId,
                    MatiereLibelle = c.Matiere!.Libelle,
                    ProfPrenom = c.Matiere.ProfPrenom,
                    ProfAvatar = c.Matiere.ProfAvatar,
                    Titre = c.Titre,
                    DateCreation = c.DateCreation,
                    DateDernierMessage = c.DateDernierMessage,
                    NombreMessages = c.Messages.Count
                })
                .ToListAsync();

            fiche.DernieresSeances = seances;
            fiche.DernierCours = seances.FirstOrDefault();

            // --- compétences -------------------------------------------------
            // Chargées d'un bloc puis réparties en mémoire : leur nombre est
            // borné par la taille du graphe pour un élève, deux requêtes
            // triées coûteraient plus qu'elles ne rapportent.
            var competences = await _context.MaitrisesEleves
                .AsNoTracking()
                .Where(x => x.EleveId == eleveId)
                .Select(x => new CompetenceEleve
                {
                    CompetenceId = x.CompetenceId,
                    Code = x.Competence!.Code,
                    Libelle = x.Competence.Libelle,
                    Domaine = x.Competence.Domaine,
                    MatiereLibelle = x.Competence.Matiere!.Libelle,
                    NiveauLibelle = x.Competence.NiveauScolaire!.Libelle,
                    Score = x.Score,
                    Confiance = x.Confiance,
                    NombreObservations = x.NombreObservations,
                    DerniereEvaluation = x.DerniereEvaluation
                })
                .ToListAsync();

            fiche.Lacunes = competences
                .Where(c => c.Score < SeuilAcquis)
                .OrderBy(c => c.Score)
                .Take(10)
                .ToList();

            fiche.Acquises = competences
                .Where(c => c.Score >= SeuilAcquis)
                .OrderByDescending(c => c.Score)
                .Take(10)
                .ToList();

            return fiche;
        }

        /// <summary>Au-dessus, la compétence est considérée acquise.</summary>
        private const double SeuilAcquis = 0.75;

        // ------------------------------------------------------------------
        // Modification / suppression
        // ------------------------------------------------------------------
        public async Task<ParentAdmin?> ModifierParentAsync(int id, string? prenom, string? nom, string? mail)
        {
            var entite = await _context.Parents.FirstOrDefaultAsync(p => p.Id == id);
            if (entite is null) return null;

            if (prenom is not null) entite.Prenom = prenom;
            if (nom is not null) entite.Nom = nom;
            if (mail is not null) entite.Mail = mail;

            await _context.SaveChangesAsync();

            // Le mois courant : cette ligne sert à rafraîchir l'affichage après
            // une modification d'identité, et l'écran rechargera de toute façon
            // le tableau avec sa propre fenêtre. Inutile de la faire remonter
            // jusqu'ici pour un prénom corrigé.
            var mois = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            return (await GetParentsAsync(null, mois, mois.AddMonths(1)))
                .FirstOrDefault(p => p.Id == id);
        }

        public async Task<EleveAdmin?> ModifierEleveAsync(
            int id, string? prenom, string? nom, int? age, int? niveauScolaireId, Sexe? sexe)
        {
            var entite = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == id);
            if (entite is null) return null;

            if (prenom is not null) entite.Prenom = prenom;
            if (nom is not null) entite.Nom = nom;
            if (age is > 0) entite.Age = age.Value;
            if (niveauScolaireId is > 0) entite.NiveauScolaireId = niveauScolaireId.Value;
            if (sexe is not null and not Sexe.NonPrecise) entite.Sexe = sexe.Value;

            await _context.SaveChangesAsync();

            return (await GetElevesAsync(null, null)).FirstOrDefault(e => e.Id == id);
        }

        public async Task<bool> SupprimerParentAsync(int id)
        {
            var entite = await _context.Parents.FirstOrDefaultAsync(p => p.Id == id);
            if (entite is null) return false;

            // Les élèves, conversations et messages partent en cascade (configuré
            // dans le DbContext). Le compte reste en revanche présent côté serveur
            // d'identité : les deux bases sont indépendantes.
            _context.Parents.Remove(entite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SupprimerEleveAsync(int id)
        {
            var entite = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == id);
            if (entite is null) return false;

            _context.Eleves.Remove(entite);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
