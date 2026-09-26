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

        /// <summary>
        /// Le fuseau dans lequel se lisent les chiffres.
        ///
        /// POURQUOI CE FUSEAU EXISTE
        /// -------------------------
        /// Tout est horodaté en UTC — c'est la bonne décision, et elle ne
        /// change pas. Mais les fenêtres arrivent du navigateur en heure
        /// LOCALE : le tableau de bord demande « le 2 septembre », pas
        /// « du 1er à 22 h au 2 à 22 h ». Les deux étaient comparées telles
        /// quelles, et l'écart se voyait à l'œil nu : un abonnement souscrit
        /// à 16 h 08 apparaissait dans la barre de 14 h.
        ///
        /// Deux heures en été, une en hiver, et le décalage se déplaçait
        /// avec la saison — de quoi rendre suspect chaque chiffre du
        /// tableau de bord sans qu'aucun ne soit franchement faux.
        /// </summary>
        private static readonly TimeZoneInfo Fuseau = TrouverLeFuseau();

        /// <summary>
        /// Le fuseau de Paris, quel que soit le système d'exploitation.
        ///
        /// L'identifiant IANA marche sur Linux et, depuis .NET 6, sur
        /// Windows aussi. Le nom Windows reste en second recours pour une
        /// machine sans ICU. En dernier ressort UTC : des heures décalées
        /// valent mieux qu'un tableau de bord qui refuse de s'ouvrir.
        /// </summary>
        private static TimeZoneInfo TrouverLeFuseau()
        {
            foreach (var identifiant in new[] { "Europe/Paris", "Romance Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(identifiant);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return TimeZoneInfo.Utc;
        }

        /// <summary>
        /// Traduit une fenêtre locale en fenêtre UTC, et rend le décalage.
        ///
        /// Le décalage sert deux fois : ici pour borner la requête, et dans
        /// `Grouper` pour ramener chaque horodatage à l'heure locale AVANT
        /// de le ranger dans un seau. Sans ce second usage, les bornes
        /// seraient justes et les barres toujours décalées.
        ///
        /// UN SEUL DÉCALAGE POUR TOUTE LA FENÊTRE, celui de son début. Une
        /// fenêtre qui enjambe un changement d’heure — la dernière semaine
        /// d'octobre, ou une vue à l'année — a donc une heure de biais sur
        /// sa seconde moitié. Le calculer ligne par ligne coûterait un appel
        /// de fuseau par enregistrement, pour une erreur qui ne déplace
        /// jamais un point de plus d'un cran et jamais un seau mensuel.
        /// </summary>
        private static (DateTime Debut, DateTime Fin, int Decalage) EnUtc(DateTime debut, DateTime fin)
        {
            var decalage = Fuseau.GetUtcOffset(
                DateTime.SpecifyKind(debut, DateTimeKind.Unspecified));

            var heures = (int)decalage.TotalHours;

            return (debut.AddHours(-heures), fin.AddHours(-heures), heures);
        }

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
            var (debutUtc, finUtc, decalage) = EnUtc(debut, fin);

            var requete = _context.Messages
                .AsNoTracking()
                .Where(m => m.Role == "assistant")
                .Where(m => m.DateCreation >= debutUtc && m.DateCreation < finUtc);

            if (eleveId.HasValue)
            {
                requete = requete.Where(m => m.Conversation!.EleveId == eleveId.Value);
            }

            var seaux = await Grouper(requete.Select(m => m.DateCreation), granularite, decalage);
            return Completer(seaux, granularite, debut, fin, cumulDepart: null);
        }

        public async Task<IEnumerable<PointSerie>> GetSerieParentsAsync(
            Granularite granularite, DateTime debut, DateTime fin)
        {
            var (debutUtc, finUtc, decalage) = EnUtc(debut, fin);
            var source = _context.Parents.AsNoTracking();

            // Le cumul doit démarrer au nombre de comptes déjà existants avant
            // la fenêtre, sinon la courbe repartirait de zéro à chaque filtre.
            var avant = await source.CountAsync(p => p.DateCreation < debutUtc);

            var seaux = await Grouper(
                source.Where(p => p.DateCreation >= debutUtc && p.DateCreation < finUtc)
                      .Select(p => p.DateCreation),
                granularite, decalage);

            return Completer(seaux, granularite, debut, fin, avant);
        }

        public async Task<IEnumerable<PointSerie>> GetSerieElevesAsync(
            Granularite granularite, DateTime debut, DateTime fin)
        {
            var (debutUtc, finUtc, decalage) = EnUtc(debut, fin);
            var source = _context.Eleves.AsNoTracking();
            var avant = await source.CountAsync(e => e.DateCreation < debutUtc);

            var seaux = await Grouper(
                source.Where(e => e.DateCreation >= debutUtc && e.DateCreation < finUtc)
                      .Select(e => e.DateCreation),
                granularite, decalage);

            return Completer(seaux, granularite, debut, fin, avant);
        }

        /// <summary>
        /// Accorde ou retire le droit d'administrer à un parent.
        ///
        /// LE JETON NE CHANGE PAS TOUT DE SUITE, et il faut le savoir : le rôle
        /// est lu à l'émission du jeton, pas à chaque requête. Un parent promu
        /// pendant qu'il est connecté n'obtiendra l'accès qu'à sa prochaine
        /// connexion — et un administrateur déchu gardera le sien jusque-là.
        ///
        /// C'est le prix d'un jeton signé, et il est assumé : vérifier le rôle
        /// en base à chaque appel coûterait une lecture par requête pour un
        /// droit qui change deux fois par an.
        /// </summary>
        public async Task<bool> DefinirAjoutEnfantAsync(int parentId, bool autorise)
        {
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Id == parentId);
            if (parent is null) return false;

            parent.PeutAjouterEnfant = autorise;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DefinirAdministrateurAsync(int parentId, bool actif)
        {
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Id == parentId);
            if (parent is null) return false;

            parent.EstAdministrateur = actif;

            // RETIRER LE RÔLE EFFACE LES DROITS, et il le faut. Sans cette ligne,
            // un compte rétrogradé puis repromu six mois plus tard retrouverait
            // exactement les sections qu'on lui avait ouvertes — sans que
            // personne ne les ait redécidées. Le super-administrateur croirait
            // avoir nommé un administrateur vierge.
            //
            // C'est aussi ce qui rend la règle de Camara vraie à la lettre : « de
            // base quand le super admin passe un utilisateur en admin, tout est
            // décoché ».
            if (!actif) parent.OngletsAdmin = null;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string[]?> GetOngletsAdminAsync(int parentId)
        {
            var parent = await _context.Parents
                .AsNoTracking()
                .Where(p => p.Id == parentId)
                .Select(p => new { p.OngletsAdmin })
                .FirstOrDefaultAsync();

            return parent is null ? null : OngletsAdmin.Lire(parent.OngletsAdmin);
        }

        public async Task<bool> DefinirOngletsAdminAsync(int parentId, IEnumerable<string>? onglets)
        {
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Id == parentId);
            if (parent is null) return false;

            // VIDE S'ÉCRIT NULL. Une chaîne vide et NULL diraient la même chose à
            // la lecture, mais une colonne qu'on relit à l'œil en base est plus
            // claire avec une seule façon de dire « rien ».
            var valeur = OngletsAdmin.Ecrire(onglets);
            parent.OngletsAdmin = string.IsNullOrEmpty(valeur) ? null : valeur;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string[]> GetOngletsAdminParMailAsync(string? mail)
        {
            if (string.IsNullOrWhiteSpace(mail)) return [];

            var parent = await _context.Parents
                .AsNoTracking()
                .Where(p => p.Mail == mail && p.EstAdministrateur)
                .Select(p => new { p.OngletsAdmin })
                .FirstOrDefaultAsync();

            return parent is null ? [] : OngletsAdmin.Lire(parent.OngletsAdmin);
        }

        /// <summary>
        /// Le tunnel de conversion, sur une fenêtre.
        ///
        /// POURQUOI LA CONVERSION N'EST PAS BORNÉE PAR LA FENÊTRE
        /// -----------------------------------------------------
        /// Un essai lancé lundi peut se transformer en abonnement des semaines
        /// plus tard. Compter les conversions DANS la fenêtre donnerait un taux
        /// nul sur la semaine en cours — faute de recul — et amputé sur les
        /// anciennes, faute de compter ce qui a suivi.
        ///
        /// On date donc l'ESSAI dans la fenêtre, et on regarde ce qu'il est
        /// devenu, quand que ce soit. Le taux d'une période récente monte
        /// encore pendant des semaines, et c'est la vérité du tunnel.
        ///
        /// « APRÈS » COMPTE DEPUIS LE DÉBUT DE L'ESSAI, pas depuis la fin de la
        /// fenêtre : un parent qui prend une offre payante le lendemain de son
        /// essai est converti, même si les deux tombent le même jour.
        /// </summary>
        public async Task<Tunnel> GetTunnelAsync(DateTime debut, DateTime fin)
        {
            // Les essais lancés dans la fenêtre, avec leur date : c'est elle qui
            // borne la recherche de conversion, parent par parent.
            var (debutUtc, finUtc, _) = EnUtc(debut, fin);

            var essais = await _context.Abonnements
                .AsNoTracking()
                .Where(a => a.Offre!.EstEssai && a.DateDebut >= debutUtc && a.DateDebut < finUtc)
                .GroupBy(a => a.ParentId)
                .Select(g => new { ParentId = g.Key, Premier = g.Min(a => a.DateDebut) })
                .ToListAsync();

            if (essais.Count == 0)
            {
                return new Tunnel
                {
                    Visiteurs = await CompterVisiteursAsync(debut, fin),
                    Essais = 0,
                    Convertis = 0,
                };
            }

            // UNE SEULE REQUÊTE POUR TOUTES LES CONVERSIONS. Interroger la base
            // une fois par parent tiendrait tant qu'ils sont dix ; à mille, le
            // tableau de bord mettrait une minute à s'afficher.
            var parents = essais.Select(e => e.ParentId).ToList();

            // TOUTES LES DATES, PAS LA PREMIÈRE.
            //
            // Prendre le minimum condamnait tout parent ayant déjà payé AVANT
            // son essai : sa première offre payante étant antérieure, la
            // comparaison échouait même s il en reprenait une le lendemain.
            // Relevé sur les données réelles — essai le 4 août à 00h38, offre
            // payante le même jour à 12h47, comptée zéro.
            //
            // Ce qu on cherche n est pas « quand a-t-il payé la première fois »
            // mais « a-t-il payé APRÈS son essai ».
            var payants = await _context.Abonnements
                .AsNoTracking()
                .Where(a => !a.Offre!.EstEssai && parents.Contains(a.ParentId))
                .Select(a => new { a.ParentId, a.DateDebut })
                .ToListAsync();

            var convertis = essais.Count(e =>
                payants.Any(p => p.ParentId == e.ParentId && p.DateDebut >= e.Premier));

            return new Tunnel
            {
                Visiteurs = await CompterVisiteursAsync(debut, fin),
                Essais = essais.Count,
                Convertis = convertis,
            };
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
            var (debutUtc, finUtc, decalage) = EnUtc(debut, fin);

            var visites = _context.VisitesSite
                .AsNoTracking()
                .Where(v => v.Horodatage >= debutUtc && v.Horodatage < finUtc);

            IQueryable<IGrouping<int, VisiteSite>> groupes = granularite switch
            {
                Granularite.Heure =>
                    visites.GroupBy(v => EF.Functions.DateDiffHour(Origine, v.Horodatage.AddHours(decalage))),

                Granularite.Jour =>
                    visites.GroupBy(v => EF.Functions.DateDiffDay(Origine, v.Horodatage.AddHours(decalage))),

                Granularite.Semaine =>
                    visites.GroupBy(v => EF.Functions.DateDiffDay(Origine, v.Horodatage.AddHours(decalage)) / 7),

                Granularite.Mois =>
                    visites.GroupBy(v => EF.Functions.DateDiffMonth(Origine, v.Horodatage.AddHours(decalage))),

                _ =>
                    visites.GroupBy(v => EF.Functions.DateDiffYear(Origine, v.Horodatage.AddHours(decalage))),
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
            CompterVisiteursUtcAsync(EnUtc(debut, fin));

        private Task<int> CompterVisiteursUtcAsync((DateTime Debut, DateTime Fin, int _) fenetre) =>
            _context.VisitesSite
                .AsNoTracking()
                .Where(v => v.Horodatage >= fenetre.Debut && v.Horodatage < fenetre.Fin)
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
        /// <summary>Vrai si une ligne a vraiment été écrite — faux si ce visiteur était déjà connu.</summary>
        public async Task<bool> EnregistrerVisiteAsync(string visiteur, DateTime quand)
        {
            var depuis = quand.AddHours(-1);

            var dejaVu = await _context.VisitesSite
                .AsNoTracking()
                .AnyAsync(v => v.Visiteur == visiteur && v.Horodatage >= depuis);

            if (dejaVu) return false;

            _context.VisitesSite.Add(new VisiteSite { Visiteur = visiteur, Horodatage = quand });
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Regroupe côté SQL. On calcule un index de seau entier — nombre de
        /// jours, semaines, mois ou années depuis l'origine — parce que c'est
        /// la seule forme que SQL Server sait grouper sans ramener toutes les
        /// lignes en mémoire.
        /// </summary>
        private static async Task<Dictionary<int, int>> Grouper(
            IQueryable<DateTime> dates, Granularite granularite, int decalage)
        {
            // RAMENÉ À L'HEURE LOCALE AVANT DE RANGER, et en SQL : décaler
            // le seau plutôt que la valeur ne marcherait que pour les
            // heures — DATEDIFF(day) ignore la partie horaire de son
            // origine, et les jours ne bougeraient pas d'un pouce.
            var locales = decalage == 0 ? dates : dates.Select(d => d.AddHours(decalage));

            IQueryable<IGrouping<int, DateTime>> groupes = granularite switch
            {
                Granularite.Heure =>
                    locales.GroupBy(d => EF.Functions.DateDiffHour(Origine, d)),

                Granularite.Jour =>
                    locales.GroupBy(d => EF.Functions.DateDiffDay(Origine, d)),

                // Division entière sur le nombre de jours : DATEDIFF(week) de
                // SQL Server compte les dimanches, ce qui décalerait les libellés.
                Granularite.Semaine =>
                    locales.GroupBy(d => EF.Functions.DateDiffDay(Origine, d) / 7),

                Granularite.Mois =>
                    locales.GroupBy(d => EF.Functions.DateDiffMonth(Origine, d)),

                _ =>
                    locales.GroupBy(d => EF.Functions.DateDiffYear(Origine, d)),
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
            var (debutUtc, finUtc, decalage) = EnUtc(debut, fin);
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

            // LES ESSAIS SONT COMPTÉS À PART DES ABONNEMENTS PAYANTS.
            //
            // Un essai crée une ligne d'abonnement comme une offre payante :
            // même table, même mécanique. Les compter ensemble annonçait un
            // abonnement là où personne n'avait rien payé.
            var nouveaux = await Grouper(
                source.Where(a => a.DateDebut >= debutUtc && a.DateDebut < finUtc
                                  && !a.Offre!.EstEssai)
                      .Select(a => a.DateDebut),
                granularite, decalage);

            var essais = await Grouper(
                source.Where(a => a.DateDebut >= debutUtc && a.DateDebut < finUtc
                                  && a.Offre!.EstEssai)
                      .Select(a => a.DateDebut),
                granularite, decalage);

            var demandes = await Grouper(
                source.Where(a => a.ResiliationDemandeeLe != null
                                  && a.ResiliationDemandeeLe >= debutUtc
                                  && a.ResiliationDemandeeLe < finUtc)
                      .Select(a => a.ResiliationDemandeeLe!.Value),
                granularite, decalage);

            var arrets = await Grouper(
                source.Where(a => a.DateFin != null
                                  && a.DateFin >= debutUtc
                                  && a.DateFin < finUtc)
                      .Select(a => a.DateFin!.Value),
                granularite, decalage);

            // Pour le stock, on ramène les bornes des abonnements qui touchent
            // la fenêtre — pas tous, seulement ceux qui la chevauchent.
            var bornesUtc = await source
                .Where(a => a.DateDebut < finUtc && (a.DateFin == null || a.DateFin >= debutUtc))
                .Select(a => new { a.DateDebut, a.DateFin })
                .ToListAsync();

            // RAMENÉES À L'HEURE LOCALE, parce qu'elles sont comparées plus
            // bas à des bornes de seau, lesquelles sont locales. Deux
            // référentiels dans la même comparaison feraient apparaître ou
            // disparaître un abonnement selon l'heure de la journée.
            var bornes = bornesUtc
                .Select(b => new
                {
                    DateDebut = b.DateDebut.AddHours(decalage),
                    DateFin = b.DateFin?.AddHours(decalage),
                })
                .ToList();

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
                    Essais = essais.TryGetValue(seau, out var e) ? e : 0,
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

        /// <summary>
        /// Ce que le produit rapporte sur la période — voir <see cref="RevenuPeriode"/>
        /// pour les règles : échéances pour les mensuels, douzièmes pour les
        /// annuels, achats entiers pour les packs.
        ///
        /// EN MÉMOIRE, ET C'EST VOULU : une ligne par abonnement payant, soit
        /// quelques centaines au plus. Écrire les échéances d'un mensuel en SQL
        /// demanderait une table de nombres et une requête que personne ne
        /// relirait ; ici, c'est une boucle.
        /// </summary>
        public async Task<RevenuPeriode> GetRevenuAsync(DateTime debut, DateTime fin)
        {
            var (debutUtc, finUtc, _) = EnUtc(debut, fin);
            var maintenant = DateTime.UtcNow;

            var abonnements = await _context.Abonnements
                .AsNoTracking()
                .Where(a => !a.Offre!.EstEssai
                            && a.DateDebut < finUtc
                            && (a.DateFin == null || a.DateFin > debutUtc))
                .Select(a => new
                {
                    a.Id,
                    a.DateDebut,
                    a.DateFin,
                    a.Periodicite,
                    a.DernierePause,
                    a.PauseJusquau,
                    a.ImpayeDepuis,
                    a.Offre!.Code,
                    a.Offre.Libelle,
                    a.Offre.PrixMensuelCentimes,
                    a.Offre.PrixAnnuelCentimes,
                })
                .ToListAsync();

            var lignes = new Dictionary<(string?, string), (LigneRevenu Ligne, HashSet<int> Familles)>();

            void Compter(string? code, string? libelle, string periodicite, int prixCentimes,
                         int abonnementId, decimal euros)
            {
                if (!lignes.TryGetValue((code, periodicite), out var l))
                {
                    l = (new LigneRevenu
                    {
                        Code = code,
                        Libelle = libelle,
                        Periodicite = periodicite,
                        PrixCentimes = prixCentimes,
                    }, new HashSet<int>());
                    lignes[(code, periodicite)] = l;
                }

                l.Ligne.Nombre++;
                l.Ligne.Euros += euros;
                l.Familles.Add(abonnementId);
            }

            // ------------------------------------------ les mensuels : leurs échéances
            foreach (var a in abonnements.Where(a => a.Periodicite == PeriodiciteAbonnement.Mensuel))
            {
                // Rien après la fin de l'abonnement, ni dans le futur : une
                // échéance qui n'est pas encore tombée n'a rien rapporté.
                var borne = new[] { finUtc, a.DateFin ?? DateTime.MaxValue, maintenant }.Min();

                // Borné à cent ans d'échéances : une date aberrante ne doit pas
                // faire tourner la boucle sans fin.
                for (var k = 0; k < 1200; k++)
                {
                    var echeance = a.DateDebut.AddMonths(k);
                    if (echeance >= borne) break;
                    if (echeance < debutUtc) continue;

                    // Une échéance tombée pendant une pause n'est pas prélevée.
                    if (a.DernierePause is { } pause && a.PauseJusquau is { } reprise
                        && echeance >= pause && echeance < reprise) continue;

                    // Ni pendant un impayé : on ne compte que ce qui est encaissé.
                    if (a.ImpayeDepuis is { } impaye && echeance >= impaye) continue;

                    Compter(a.Code, a.Libelle, PeriodiciteAbonnement.Mensuel,
                            a.PrixMensuelCentimes, a.Id, a.PrixMensuelCentimes / 100m);
                }
            }

            // ---------------------------------- les annuels : un douzième par mois
            var annuels = abonnements.Where(a => a.Periodicite == PeriodiciteAbonnement.Annuel).ToList();

            for (var mois = new DateTime(debut.Year, debut.Month, 1); mois < fin; mois = mois.AddMonths(1))
            {
                var (moisDebutUtc, moisFinUtc, _) = EnUtc(mois, mois.AddMonths(1));

                // Les mois à venir de l'année en cours ne comptent pas encore.
                if (moisDebutUtc > maintenant) break;

                foreach (var a in annuels)
                {
                    var couvert = a.DateDebut < moisFinUtc
                                  && (a.DateFin == null || a.DateFin > moisDebutUtc);
                    var impaye = a.ImpayeDepuis is { } depuis && depuis <= moisDebutUtc;

                    if (couvert && !impaye)
                    {
                        Compter(a.Code, a.Libelle, PeriodiciteAbonnement.Annuel,
                                a.PrixAnnuelCentimes, a.Id, a.PrixAnnuelCentimes / 1200m);
                    }
                }
            }

            // -------------------------------------- les packs : le mois de l'achat
            // `PrixCentimes > 0` : un ajustement manuel d'heures n'est pas une vente.
            var packs = await _context.Recharges
                .AsNoTracking()
                .Where(r => r.DateAchat >= debutUtc && r.DateAchat < finUtc
                            && r.DateRemboursement == null && r.PrixCentimes > 0)
                .Select(r => r.PrixCentimes)
                .ToListAsync();

            var detail = lignes.Values
                .Select(l =>
                {
                    l.Ligne.Abonnements = l.Familles.Count;
                    l.Ligne.Euros = Math.Round(l.Ligne.Euros, 2);
                    return l.Ligne;
                })
                .OrderByDescending(l => l.Euros)
                .ToList();

            var mensuels = detail.Where(l => l.Periodicite == PeriodiciteAbonnement.Mensuel).ToList();
            var annuelsLignes = detail.Where(l => l.Periodicite == PeriodiciteAbonnement.Annuel).ToList();

            var revenu = new RevenuPeriode
            {
                Debut = debut,
                Fin = fin,
                Disponible = true,
                Lignes = detail,
                Mensualites = mensuels.Sum(l => l.Nombre),
                MensuelsEuros = mensuels.Sum(l => l.Euros),
                AnnuelsAbonnements = annuels.Count(a => lignes.Values.Any(l => l.Familles.Contains(a.Id))),
                AnnuelsMois = annuelsLignes.Sum(l => l.Nombre),
                AnnuelsEuros = annuelsLignes.Sum(l => l.Euros),
                Packs = packs.Count,
                PacksEuros = packs.Sum() / 100m,
            };

            revenu.TotalEuros = revenu.MensuelsEuros + revenu.AnnuelsEuros + revenu.PacksEuros;

            return revenu;
        }

        public async Task<CoutPeriode> GetCoutAsync(DateTime debut, DateTime fin)
        {
            var (debutUtc, finUtc, _) = EnUtc(debut, fin);
            var grille = await GrilleAppliqueeAsync();

            var seaux = await _context.Messages
                .AsNoTracking()
                .Where(m => m.TokensEntree > 0
                            && m.DateCreation >= debutUtc && m.DateCreation < finUtc)
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
                    CacheEcritDetaille = g.Sum(m => m.TokensCacheEcriture1h != null ? (long)m.TokensCacheEcriture : 0L),
                    CacheEcrit1h = g.Sum(m => (long)(m.TokensCacheEcriture1h ?? 0)),
                })
                .ToListAsync();

            var dialogue = 0m;
            var tours = 0;

            foreach (var s in seaux)
            {
                var (pe, ps) = Tarif(grille, s.Modele);

                // LES MÊMES CONSTANTES QUE LE COÛT PAR PARENT, et c'est une
                // correction du 19/09/2026 : ce total comptait encore
                // l'écriture à 1,25x quand la colonne des parents était
                // passée à 1,75x. Seul parent actif le 17/09, Camara lisait
                // 8,29 $ ici et 10,48 $ sur sa ligne, pour les mêmes 105 tours.
                //
                // Les tâches de fond, plus bas, gardent 1,25x : elles
                // n'utilisent pas le cache d'une heure.
                dialogue += (s.Entree * pe
                             + s.CacheLu * pe * LectureCache
                             + EcritureEquivalente(s.CacheEcrit, s.CacheEcritDetaille, s.CacheEcrit1h) * pe
                             + s.Sortie * ps) / 1_000_000m;

                tours += s.Tours;
            }

            var voix = VoixDollars(tours, grille);

            // LES TÂCHES DE FOND, QUI DÉPENSAIENT EN SILENCE.
            //
            // Décrire une planche, en cartographier les repères, observer une
            // séance, transcrire un document : quatre postes qui appellent le
            // modèle sans appartenir à aucune conversation. Ils n'étaient nulle
            // part dans ce total, et une facture qui montait sans que le
            // tableau ne bouge n'avait aucune explication visible.
            var fond = await _context.AppelsClaude
                .AsNoTracking()
                .Where(a => a.DateCreation >= debutUtc && a.DateCreation < finUtc)
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
                    var (pe, ps) = Tarif(grille, f.Modele);

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
        /// <summary>
        /// Où en est le fichier clients, à cet instant.
        ///
        /// UN SEUL ABONNEMENT RETENU PAR PARENT, le plus récent. La table en
        /// garde plusieurs — un changement de formule laisse derrière lui la
        /// ligne résiliée — et les compter tous afficherait plus de clients
        /// que de comptes, ce qui ferait douter de l'écran entier.
        /// </summary>
        public async Task<RepartitionParents> GetRepartitionParentsAsync(
            IReadOnlyCollection<string> mailsExclus)
        {
            var parents = _context.Parents.AsNoTracking().AsQueryable();

            // LES COMPTES DE L'EXPLOITANT SORTENT DU COMPTE, quand l'appelant
            // n'a pas le droit de les voir. Les laisser dans le total pendant
            // que le tableau du dessous les masque afficherait un fichier
            // clients qui ne correspond à aucune liste — et ce sont les deux
            // mêmes chiffres, à deux endroits.
            if (mailsExclus.Count > 0)
            {
                parents = parents.Where(p => p.Mail == null || !mailsExclus.Contains(p.Mail));
            }

            var retenus = await parents.Select(p => p.Id).ToListAsync();
            var total = retenus.Count;

            var lignes = await _context.Abonnements
                .AsNoTracking()
                .Where(a => retenus.Contains(a.ParentId))
                .Select(a => new
                {
                    a.ParentId,
                    a.Statut,
                    a.Periodicite,
                    a.DateDebut,
                    Code = a.Offre!.Code,
                    a.Offre.Libelle,
                    a.Offre.EstEssai,
                    a.Offre.PrixMensuelCentimes,
                })
                .ToListAsync();

            var parAbonne = lignes
                .GroupBy(l => l.ParentId)
                .Select(g => new
                {
                    // Le courant si le parent en a un, sinon le dernier connu :
                    // c'est lui qui dit s'il est parti ou s'il n'est jamais venu.
                    Courant = g.Where(l => l.Statut != StatutAbonnement.Resilie)
                               .OrderByDescending(l => l.DateDebut)
                               .FirstOrDefault(),
                    APayeUnJour = g.Any(l => !l.EstEssai),
                })
                .ToList();

            var forfaits = parAbonne
                .Where(p => p.Courant is not null
                            && p.Courant.Statut == StatutAbonnement.Actif
                            && !p.Courant.EstEssai)
                .GroupBy(p => new { p.Courant!.Code, p.Courant.Libelle, p.Courant.PrixMensuelCentimes })
                .Select(g => new LigneForfait
                {
                    Code = g.Key.Code,
                    Libelle = g.Key.Libelle,
                    Mensuel = g.Count(p => !PeriodiciteAbonnement.EstAnnuel(p.Courant!.Periodicite)),
                    Annuel = g.Count(p => PeriodiciteAbonnement.EstAnnuel(p.Courant!.Periodicite)),
                })
                .OrderByDescending(f => f.Total)
                .ThenBy(f => f.Libelle)
                .ToList();

            return new RepartitionParents
            {
                Total = total,

                Essais = parAbonne.Count(p => p.Courant is not null
                                              && p.Courant.EstEssai
                                              && p.Courant.Statut == StatutAbonnement.Actif),

                EnPause = parAbonne.Count(p => p.Courant is not null
                                               && p.Courant.Statut == StatutAbonnement.EnPause),

                Resilies = parAbonne.Count(p => p.Courant is null && p.APayeUnJour),

                // Le reste : les comptes sans aucun abonnement en cours et qui
                // n'ont jamais rien payé. Déduit du total plutôt que compté,
                // pour que la somme des cases retombe TOUJOURS sur le total,
                // même le jour où un état nouveau apparaît dans la table.
                JamaisAbonnes = total
                                - forfaits.Sum(f => f.Total)
                                - parAbonne.Count(p => p.Courant is not null
                                                       && p.Courant.EstEssai
                                                       && p.Courant.Statut == StatutAbonnement.Actif)
                                - parAbonne.Count(p => p.Courant is not null
                                                       && p.Courant.Statut == StatutAbonnement.EnPause)
                                - parAbonne.Count(p => p.Courant is null && p.APayeUnJour),

                Forfaits = forfaits,
            };
        }

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
                    // La clé des actions d'identité — voir `ParentAdmin`.
                    IdentityUserId = p.IdentityUserId,
                    DateCreation = p.DateCreation,
                    NombreEleves = p.Eleves.Count,
                    NombreRequetes = p.Eleves
                        .SelectMany(e => e.Conversations)
                        .SelectMany(c => c.Messages)
                        .Count(m => m.Role == "assistant"),
                    DerniereActivite = p.Eleves
                        .Where(e => e.DerniereActivite != null)
                        .Max(e => e.DerniereActivite),
                    DerniereConnexion = p.DerniereConnexion,
                    EstAdministrateur = p.EstAdministrateur,
                    PeutAjouterEnfant = p.PeutAjouterEnfant,

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

                    // LE FORFAIT VIENT DE LA VUE, PAS D UN CALCUL REFAIT ICI.
                    //
                    // La regle — quelles recharges comptent — etait ecrite deux
                    // fois : la, et dans AbonnementRepository pour l ecran du
                    // parent. Elles ont diverge DEUX FOIS, chaque fois dans un
                    // sens different : 18 h contre 14, puis 9 h contre 12.
                    //
                    // Elle vit maintenant dans vw_ForfaitAbonnement, ou il n en
                    // existe qu un exemplaire. Ajouter une formule ou vendre un
                    // nouveau pack ne demande plus rien ici.
                    MinutesPot = p.Abonnements
                        .Where(a => a.Statut == "Actif")
                        .OrderByDescending(a => a.PeriodeDebut)
                        .Select(a => _context.ForfaitsAbonnement
                            .Where(f => f.AbonnementId == a.Id)
                            .Select(f => (int?)f.MinutesAllouees)
                            .FirstOrDefault())
                        .FirstOrDefault(),

                })
                .ToListAsync();

            await RenseignerLesCoutsAsync(lignes, debut, fin);

            return lignes;
        }

        /// <summary>La lecture de cache : un dixième du tarif d'entrée.</summary>
        private const decimal LectureCache = 0.1m;

        /// <summary>L'écriture en cache pour CINQ MINUTES : 1,25x le tarif d'entrée.</summary>
        private const decimal EcritureCinqMinutes = 1.25m;

        /// <summary>L'écriture en cache pour UNE HEURE : 2x le tarif d'entrée.</summary>
        private const decimal EcritureUneHeure = 2m;

        /// <summary>
        /// L'ESTIMATION, SEULEMENT POUR LES TOURS D'AVANT LE 19/09/2026.
        ///
        /// Jusque-là, `tokens_cache_ecriture` ne portait que le total, sans
        /// dire quelle part était écrite pour une heure (2x) et laquelle pour
        /// cinq minutes (1,25x). 1,75x supposait deux tiers d'une heure : le
        /// gros des écritures vient du prompt système, gardé une heure. Une
        /// supposition, jamais une mesure.
        ///
        /// Depuis la colonne `tokens_cache_ecriture_1h`, chaque nouveau tour
        /// est compté EXACTEMENT — voir <see cref="EcritureEquivalente"/>. Ce
        /// taux ne sert plus qu'aux anciens tours, faute de détail.
        /// </summary>
        private const decimal EcritureCache = 1.75m;

        /// <summary>
        /// Les jetons écrits en cache, convertis en jetons d'entrée au prix
        /// facturé : à multiplier par le tarif d'entrée du modèle.
        ///
        /// Voulu par Camara le 19/09/2026 : « j'ai besoin de chiffres
        /// exacts ». Les tours qui ont le détail sont comptés au tarif réel
        /// de chaque durée ; les autres gardent l'estimation de 1,75x.
        /// </summary>
        /// <param name="total">Tous les jetons écrits.</param>
        /// <param name="detailles">Ceux des tours qui ont le détail par durée.</param>
        /// <param name="uneHeure">Parmi ces derniers, ceux écrits pour une heure.</param>
        private static decimal EcritureEquivalente(long total, long detailles, long uneHeure) =>
            (detailles - uneHeure) * EcritureCinqMinutes
            + uneHeure * EcritureUneHeure
            + (total - detailles) * EcritureCache;

        /// <summary>
        /// Le tarif d'un modèle, par million de jetons d'entrée et de sortie.
        ///
        /// EN DUR, ET ASSUMÉ. Une table de tarifs en base serait un écran de
        /// plus à tenir à jour, pour une grille qui change deux fois par an et
        /// qu'il faut de toute façon relire à la main quand elle bouge.
        ///
        /// SONNET 5 : 2 $ / 10 $, SANS DATE — corrigé le 19/09/2026, facture à
        /// l'appui. On croyait que 2 $ / 10 $ était un tarif de lancement fini
        /// le 31 août, et on comptait 3 $ / 15 $ depuis : tous les coûts de
        /// dialogue étaient gonflés de moitié. La console Anthropic facture
        /// 9,61 $ pour le 18/09 (UTC) ; nos jetons du même jour donnent 7,17 à
        /// 9,89 $ à 2 $ / 10 $, et au moins 10,76 $ à 3 $ / 15 $ — impossible.
        ///
        /// DEPUIS LE MÊME JOUR, LE PRIX VIENT DE LA GRILLE EN BASE — le prix
        /// APPLIQUÉ de `TarifFournisseur`, modifiable dans « Anthropic / OpenAI ›
        /// Tarifs ». Les valeurs écrites ci-dessous ne servent plus qu'en secours.
        /// La console (Analytique › Coût) reste la seule source qui fait foi.
        ///
        /// LIMITE CONNUE : un seul prix pour tout l'historique. Le jour où un
        /// tarif change, les mois passés seront recalculés au nouveau prix.
        /// </summary>
        private static (decimal Entree, decimal Sortie) Tarif(
            IReadOnlyList<TarifFournisseur> grille, string? modele)
        {
            // LA GRILLE D'ABORD — le prix APPLIQUÉ, voir `TarifFournisseur`. Le
            // début d'identifiant le plus long gagne : « claude-haiku-4-5 » vaut
            // pour « claude-haiku-4-5-20251001 ».
            var ligne = grille
                .Where(t => t.PrixEntreeApplique is not null && t.PrixSortieApplique is not null
                            && modele is not null
                            && modele.StartsWith(t.Modele, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Modele.Length)
                .FirstOrDefault();

            if (ligne is not null) return (ligne.PrixEntreeApplique!.Value, ligne.PrixSortieApplique!.Value);

            // EN SECOURS, les tarifs au 19/09/2026 : une grille vide ou un
            // modèle neuf ne doivent pas afficher un coût nul.
            if (modele is not null && modele.Contains("opus", StringComparison.OrdinalIgnoreCase))
            {
                return (5m, 25m);
            }

            if (modele is not null && modele.Contains("haiku", StringComparison.OrdinalIgnoreCase))
            {
                return (1m, 5m);
            }

            return (2m, 10m);
        }

        // ------------------------------------------------------------------
        // La grille tarifaire, vue et modifiée depuis l'écran « Tarifs »
        // ------------------------------------------------------------------

        public async Task<IReadOnlyList<LigneTarif>> GetTarifsAsync() =>
            (await _context.TarifsFournisseurs
                .AsNoTracking()
                .OrderBy(t => t.Fournisseur)
                .ThenBy(t => t.Ordre)
                .ToListAsync())
            .Select(VersLigne)
            .ToList();

        public async Task<LigneTarif?> ModifierTarifAsync(
            int id, decimal? prixEntree, decimal? prixSortie, decimal? prixMinute)
        {
            var tarif = await _context.TarifsFournisseurs.FirstOrDefaultAsync(t => t.Id == id);
            if (tarif is null) return null;

            // EN SECOURS DE LA VEILLE (voir `VeilleTarifsWorker`) : une correction
            // à la main s'applique aussitôt à nos calculs, comme un prix lu.
            // Une ligne qui n'entre dans aucun calcul le reste.
            var compte = tarif.PrixEntreeApplique is not null || tarif.PrixSortieApplique is not null
                         || tarif.PrixMinuteApplique is not null;

            tarif.PrixEntree = prixEntree;
            tarif.PrixSortie = prixSortie;
            tarif.PrixMinute = prixMinute;

            if (compte)
            {
                tarif.PrixEntreeApplique = prixEntree;
                tarif.PrixSortieApplique = prixSortie;
                tarif.PrixMinuteApplique = prixMinute;
            }

            tarif.DateMiseAJour = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return VersLigne(tarif);
        }

        private static LigneTarif VersLigne(TarifFournisseur t) => new()
        {
            Id = t.Id,
            Fournisseur = t.Fournisseur,
            Modele = t.Modele,
            Usage = t.Usage,
            PrixEntree = t.PrixEntree,
            PrixSortie = t.PrixSortie,
            PrixMinute = t.PrixMinute,
            PrixEntreeApplique = t.PrixEntreeApplique,
            PrixSortieApplique = t.PrixSortieApplique,
            PrixMinuteApplique = t.PrixMinuteApplique,
            DateMiseAJour = t.DateMiseAJour,
            DerniereVerification = t.DerniereVerification,
        };

        /// <summary>
        /// La grille, lue une fois par calcul. Quelques lignes : rien à mettre
        /// en cache, et un prix appliqué depuis l'écran compte tout de suite.
        /// </summary>
        private async Task<IReadOnlyList<TarifFournisseur>> GrilleAppliqueeAsync() =>
            await _context.TarifsFournisseurs.AsNoTracking().ToListAsync();

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
        /// <summary>
        /// LA RÈGLE DU TEMPS DE COURS, en SQL : ce que vaut un tour du professeur.
        ///
        /// L’écart avec le message précédent de la même conversation, borné à
        /// vingt secondes au moins ; au-delà de trois minutes, le forfait de
        /// vingt secondes — une pause plus longue n’est pas du temps de cours.
        ///
        /// ÉCRITE UNE SEULE FOIS, pour le total du tableau de bord ET le temps
        /// par parent. Deux copies de la même règle auraient fini par donner
        /// deux chiffres qui ne se recoupent plus, sans que rien ne le signale.
        /// </summary>
        private const string SecondesDuTourSql =
            "CASE WHEN ecart IS NULL OR ecart > 180 THEN 20 "
            + "WHEN ecart < 20 THEN 20 ELSE ecart END";

        private async Task<int> MinutesTravailleesAsync(DateTime debut, DateTime fin)
        {
            var (debutUtc, finUtc, _) = EnUtc(debut, fin);

            const string sql = @"
WITH avec_ecart AS (
    SELECT  m.role, m.date_creation,
            -- LA MÊME MESURE QUE LE FORFAIT (voir ImputerQuotaAsync) : l'écart
            -- depuis le message qui PRÉCÈDE celui de l'élève, c'est-à-dire
            -- la réponse précédente du professeur. Le temps où l'enfant
            -- réfléchit et répond compte ; mesuré depuis son message, il ne
            -- restait que les trois secondes de réponse du professeur.
            DATEDIFF(second,
                     CASE WHEN LAG(m.role) OVER (PARTITION BY m.conversation_id ORDER BY m.id) = 'user'
                          THEN LAG(m.date_creation, 2) OVER (PARTITION BY m.conversation_id ORDER BY m.id)
                          ELSE LAG(m.date_creation) OVER (PARTITION BY m.conversation_id ORDER BY m.id)
                     END,
                     m.date_creation) AS ecart
    FROM    Message m
    WHERE   m.date_creation >= DATEADD(minute, -3, {0}) AND m.date_creation < {1}
)
-- `AS Value` n'est pas cosmétique : `SqlQueryRaw<T>` sur un type scalaire
-- cherche une colonne portant EXACTEMENT ce nom, et lève une exception
-- sinon. Le message parle de mapping, pas de nom de colonne, et on cherche
-- longtemps ailleurs.
SELECT ISNULL(SUM(" + SecondesDuTourSql + @"), 0) AS Value
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
                .SqlQueryRaw<int>(sql, debutUtc, finUtc)
                .ToListAsync();

            return lignes.Count == 0 ? 0 : lignes[0] / 60;
        }

        /// <summary>Une ligne de la requête du temps réel, par parent.</summary>
        public sealed class SecondesParParent
        {
            public int ParentId { get; set; }

            public int Secondes { get; set; }
        }

        /// <summary>
        /// LE TEMPS RÉELLEMENT PASSÉ EN COURS, PAR PARENT, sur la période —
        /// voulu par Camara le 19/09/2026 : « je sais pas les réelles minutes
        /// qu’il a consommées ». Voir <c>ParentAdmin.SecondesReellesPeriode</c>.
        ///
        /// LA MÊME REQUÊTE QUE LE TOTAL, GROUPÉE PAR PARENT, et pour les mêmes
        /// raisons : `LAG` n’existe pas pour EF, et ramener tous les messages
        /// en mémoire ne tiendrait pas. UNE requête pour tout le tableau, et non
        /// une par ligne — l’écran ne doit pas ralentir à mesure que le produit
        /// marche.
        ///
        /// Mêmes bornes aussi : trois minutes de débordement vers le passé, pour
        /// que le premier tour de la période retrouve son précédent.
        /// </summary>
        private async Task<Dictionary<int, int>> SecondesReellesParParentAsync(
            DateTime debutUtc, DateTime finUtc)
        {
            const string sql = @"
WITH avec_ecart AS (
    SELECT  e.parent_id, m.role, m.date_creation,
            -- LA MÊME MESURE QUE LE FORFAIT (voir ImputerQuotaAsync) : l'écart
            -- depuis le message qui PRÉCÈDE celui de l'élève, c'est-à-dire
            -- la réponse précédente du professeur. Le temps où l'enfant
            -- réfléchit et répond compte ; mesuré depuis son message, il ne
            -- restait que les trois secondes de réponse du professeur.
            DATEDIFF(second,
                     CASE WHEN LAG(m.role) OVER (PARTITION BY m.conversation_id ORDER BY m.id) = 'user'
                          THEN LAG(m.date_creation, 2) OVER (PARTITION BY m.conversation_id ORDER BY m.id)
                          ELSE LAG(m.date_creation) OVER (PARTITION BY m.conversation_id ORDER BY m.id)
                     END,
                     m.date_creation) AS ecart
    FROM    Message m
    JOIN    Conversation c ON c.id = m.conversation_id
    JOIN    Eleve e ON e.id = c.eleve_id
    WHERE   m.date_creation >= DATEADD(minute, -3, {0}) AND m.date_creation < {1}
)
SELECT  parent_id AS ParentId, ISNULL(SUM(" + SecondesDuTourSql + @"), 0) AS Secondes
FROM    avec_ecart
WHERE   role = 'assistant' AND date_creation >= {0}
GROUP BY parent_id";

            // `ToListAsync` et non `FirstAsync` : une requête qui commence par
            // `WITH` n’est pas composable pour EF — voir `MinutesTravailleesAsync`.
            var lignes = await _context.Database
                .SqlQueryRaw<SecondesParParent>(sql, debutUtc, finUtc)
                .ToListAsync();

            // EN SECONDES, SANS ARRONDI — « à la seconde près », a demandé
            // Camara. C’est l’écran qui met en forme.
            return lignes.ToDictionary(l => l.ParentId, l => l.Secondes);
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
        private static decimal VoixDollars(int tours, IReadOnlyList<TarifFournisseur> grille)
        {
            // Le prix APPLIQUÉ de la voix, 0,015 $ la minute en secours.
            var minute = grille
                .FirstOrDefault(t => t.Modele == "gpt-4o-mini-tts" && t.PrixMinuteApplique is not null)
                ?.PrixMinuteApplique ?? 0.015m;

            return tours * 0.4m * minute;
        }

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

            var (debutUtc, finUtc, _) = EnUtc(debut, fin);
            var grille = await GrilleAppliqueeAsync();

            var ids = lignes.Select(l => l.Id).ToList();

            var seaux = await _context.Messages
                .AsNoTracking()
                // `> 0` et non `!= null` : les colonnes ne sont pas nullables.
                // Un tour sans jetons est un message d'élève ou un tour
                // antérieur à la télémétrie — dans les deux cas, zéro à
                // additionner et une ligne de moins à ramener.
                .Where(m => m.TokensEntree > 0
                            && m.DateCreation >= debutUtc && m.DateCreation < finUtc
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
                    CacheEcritDetaille = g.Sum(m => m.TokensCacheEcriture1h != null ? (long)m.TokensCacheEcriture : 0L),
                    CacheEcrit1h = g.Sum(m => (long)(m.TokensCacheEcriture1h ?? 0)),
                })
                .ToListAsync();

            var dialogue = new Dictionary<int, decimal>();

            foreach (var s in seaux)
            {
                var (entree, sortie) = Tarif(grille, s.Modele);

                var cout =
                    (s.Entree * entree
                     + s.CacheLu * entree * LectureCache
                     + EcritureEquivalente(s.CacheEcrit, s.CacheEcritDetaille, s.CacheEcrit1h) * entree
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

            var secondesReelles = await SecondesReellesParParentAsync(debutUtc, finUtc);

            foreach (var ligne in lignes)
            {
                var parle = dialogue.GetValueOrDefault(ligne.Id);

                // Le temps réel de la MÊME fenêtre que le coût : les deux se
                // lisent côte à côte, ils doivent parler des mêmes dates.
                ligne.SecondesReellesPeriode = secondesReelles.GetValueOrDefault(ligne.Id);

                // Les échanges facturés de la même fenêtre : les tours qui
                // portent des jetons, ceux-là mêmes que le coût additionne.
                ligne.ToursPeriode = tours.GetValueOrDefault(ligne.Id);

                ligne.CoutDialogueDollars = Math.Round(parle, 4);
                ligne.CoutDollars = Math.Round(
                    parle + VoixDollars(tours.GetValueOrDefault(ligne.Id), grille), 4);

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
                    NiveauOrdre = e.NiveauScolaire.Ordre,
                    // La classe, la LVB et les spécialités : ce que le parent a
                    // toujours vu, et que l'administration ne recevait pas. La
                    // liste est relue en mémoire, dans la projection finale.
                    NiveauScolaireId = e.NiveauScolaireId,
                    Lv2Espagnol = e.Lv2Espagnol,
                    Specialites = VoiesScolaires.LireSpecialites(e.Specialites),
                    AcademieId = e.AcademieId,
                    AcademieLibelle = e.Academie!.Libelle,
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
        /// <summary>
        /// La fiche d'un élève, éventuellement réduite à UNE ANNÉE SCOLAIRE.
        /// </summary>
        /// <param name="niveauScolaireId">
        /// L'année à regarder, ou null pour toute la scolarité.
        ///
        /// LE FILTRE PORTE SUR TOUTE LA FICHE, pas seulement sur la
        /// progression : cours suivis, réponses du professeur, dernier cours,
        /// statistiques par matière, points fragiles et compétences acquises.
        /// Un suivi de niveau qui mélangerait les années ne dirait rien — et
        /// afficher « 44 compétences maîtrisées » en cumulant la 6e à la 3e
        /// laisserait croire à un niveau de 3e qui n'est pas mesuré.
        ///
        /// C'EST L'ANNÉE DU TRAVAIL, PAS LE NIVEAU DE LA NOTION. Un élève de 3e
        /// qui rattrape une notion de CM1 travaille en 3e : sa maîtrise porte
        /// l'année de l'observation, la compétence garde le niveau où elle
        /// s'apprend.
        /// </param>
        public async Task<FicheEleve?> GetFicheEleveAsync(int eleveId, int? niveauScolaireId = null)
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
                    NiveauOrdre = e.NiveauScolaire.Ordre,
                    NiveauCycle = e.NiveauScolaire.Cycle,
                    Lv2Espagnol = e.Lv2Espagnol,
                    Specialites = e.Specialites,
                    ParentId = e.ParentId,
                    ParentMail = e.Parent!.Mail,
                    ParentNomComplet = (e.Parent.Prenom ?? "") + " " + (e.Parent.Nom ?? ""),
                    DateCreation = e.DateCreation,
                    DerniereActivite = e.DerniereActivite
                })
                .FirstOrDefaultAsync();

            if (fiche is null) return null;

            // --- les années qu'il a passées chez nous ------------------------
            //
            // Elles viennent de l'HISTORIQUE DES CLASSES, pas du travail lui-même :
            // c'est la seule source qui dise où l'élève était, y compris sur une
            // année où il n'a rien fait. Une année vide reste une année, et son
            // onglet doit exister pour qu'on puisse constater le vide.
            var intervalles = await _context.HistoriquesClasse
                .AsNoTracking()
                .Where(h => h.EleveId == eleveId)
                .OrderBy(h => h.Debut)
                .Select(h => new { h.NiveauScolaireId, h.Debut, h.Fin })
                .ToListAsync();

            var annees = intervalles.Select(i => i.NiveauScolaireId).Distinct().ToList();

            // LES BORNES DE L'ANNÉE REGARDÉE.
            //
            // Un élève peut avoir occupé la même classe sur deux intervalles —
            // un redoublement, ou une correction de saisie suivie d'un retour en
            // arrière. On prend donc du premier début à la dernière fin, ce qui
            // couvre les deux d'un seul encadrement.
            //
            // Sans filtre, les bornes restent ouvertes et la fiche montre tout.
            var vises = niveauScolaireId is int cible
                ? intervalles.Where(i => i.NiveauScolaireId == cible).ToList()
                : [];

            DateTime? depuis = vises.Count > 0 ? vises.Min(i => i.Debut) : null;

            // Une fin nulle veut dire « toujours en cours » : l'année n'est pas
            // bornée à droite, et tout ce qui arrive aujourd'hui lui appartient.
            DateTime? jusqua = vises.Count > 0 && vises.All(i => i.Fin != null)
                ? vises.Max(i => i.Fin)
                : null;

            var eleveNiveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync();

            if (eleveNiveauId != 0 && !annees.Contains(eleveNiveauId)) annees.Add(eleveNiveauId);

            fiche.Classes = await _context.NiveauxScolaires
                .AsNoTracking()
                .Where(n => annees.Contains(n.Id))
                .OrderByDescending(n => n.Ordre)
                .Select(n => new ClasseFrequentee
                {
                    NiveauScolaireId = n.Id,
                    Libelle = n.Libelle,
                    Ordre = n.Ordre,
                    Courante = n.Id == eleveNiveauId,
                })
                .ToListAsync();

            // --- activité par matière ---------------------------------------
            //
            // UN COURS SUIVI = UNE SÉANCE, et non une conversation. Le fil d'une
            // matière traverse les années : le compter dans une seule serait
            // faux, et « 6 cours » — une par matière — ne changerait jamais
            // d'une année sur l'autre. Les séances, elles, sont datées et
            // parlent : « 49 cours suivis en 3e » dit quelque chose.
            var seancesParMatiere = await _context.RapportsSeance
                .AsNoTracking()
                .Where(r => r.EleveId == eleveId)
                .Where(r => depuis == null || r.DateCreation >= depuis)
                .Where(r => jusqua == null || r.DateCreation < jusqua)
                .GroupBy(r => r.MatiereId)
                .Select(g => new { MatiereId = g.Key, NombreCours = g.Count() })
                .ToListAsync();

            // Les messages du professeur, datés eux aussi. C'est ce qui évite
            // d'avoir à estampiller chaque message à l'écriture — plus de mille
            // par matière et par élève, sur le chemin le plus chaud du produit.
            var messagesParMatiere = await _context.Messages
                .AsNoTracking()
                .Where(m => m.Conversation!.EleveId == eleveId && m.Role == "assistant")
                .Where(m => depuis == null || m.DateCreation >= depuis)
                .Where(m => jusqua == null || m.DateCreation < jusqua)
                .GroupBy(m => m.Conversation!.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    NombreRequetes = g.Count(),
                    DernierCours = g.Max(x => x.DateCreation)
                })
                .ToListAsync();

            var activite = messagesParMatiere
                .Select(m => new
                {
                    m.MatiereId,
                    NombreCours = seancesParMatiere
                        .FirstOrDefault(s => s.MatiereId == m.MatiereId)?.NombreCours ?? 0,
                    m.NombreRequetes,
                    DernierCours = (DateTime?)m.DernierCours
                })
                .Concat(seancesParMatiere
                    .Where(s => !messagesParMatiere.Any(m => m.MatiereId == s.MatiereId))
                    .Select(s => new
                    {
                        s.MatiereId,
                        s.NombreCours,
                        NombreRequetes = 0,
                        DernierCours = (DateTime?)null
                    }))
                .ToList();

            // --- maîtrise par matière ---------------------------------------
            var maitrise = await _context.MaitrisesEleves
                .AsNoTracking()
                .Where(m => m.EleveId == eleveId)
                // La maîtrise est un ÉTAT, pas un événement : on la rattache à
                // l'année de sa dernière observation, qui est le moment où
                // l'élève y a effectivement travaillé.
                .Where(m => depuis == null || m.DerniereEvaluation >= depuis)
                .Where(m => jusqua == null || m.DerniereEvaluation < jusqua)
                .GroupBy(m => m.Competence!.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    Evaluees = g.Count(),

                    // LA MOYENNE NE COMPTE QUE CE QUI A ETE VU DEUX FOIS.
                    //
                    // Elle portait sur toutes les competences observees, y
                    // compris celles vues une seule fois : deux notions
                    // effleurees a 85 % affichaient « 85 % de maitrise en
                    // maths » a un parent, sur une matiere qui en compte",
                    // cinquante. Le chiffre etait vrai au sens arithmetique et
                    // faux au sens ou il etait lu.
                    Moyenne = g.Where(x => x.NombreObservations >= SeuilsMaitrise.ObservationsPourTrancher)
                        .Average(x => (double?)x.Score),

                    // Ce que la moyenne recouvre vraiment, pour pouvoir le dire.
                    Assises = g.Count(x => x.NombreObservations >= SeuilsMaitrise.ObservationsPourTrancher)
                })
                .ToListAsync();

            // --- une ligne par matière active, activité ou non ---------------
            // LES MATIÈRES DE SA CLASSE, PAS TOUTES CELLES QUI EXISTENT.
            //
            // Le filtre ne portait que sur `Active`, et la fiche d'un élève de
            // troisième affichait donc la philosophie — matière de terminale —
            // avec sa professeure et son « pas encore travaillée », comme s'il
            // avait simplement négligé de s'y mettre.
            //
            // C'est la MÊME règle que celle qui décide ce qu'un enfant peut
            // ouvrir : `VoiesScolaires.EstAuProgramme`, qui croise les bornes
            // de niveau de la matière ET les exclusions par voie — le français
            // n'existe pas en terminale, la philosophie pas en terminale pro.
            // La réécrire ici aurait fait deux vérités pour une seule question.
            //
            // Le filtrage se fait en mémoire, après la requête : la règle vit
            // dans le domaine et ne se traduit pas en SQL. Une dizaine de
            // matières, c'est sans conséquence.
            var niveau = await _context.NiveauxScolaires
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Code == fiche.NiveauCode);

            var toutes = await _context.Matieres
                .AsNoTracking()
                .Where(m => m.Active)
                .OrderBy(m => m.Ordre)
                .ToListAsync();

            var matieres = toutes
                .Where(m => Domain.Models.VoiesScolaires.EstAuProgramme(
                    new Domain.Models.Matiere
                    {
                        Code = m.Code,
                        NiveauOrdreMin = m.NiveauOrdreMin,
                        NiveauOrdreMax = m.NiveauOrdreMax,
                    },
                    niveau is null ? null : new Domain.Models.NiveauScolaire
                    {
                        Code = niveau.Code,
                        Ordre = niveau.Ordre,
                    },
                    fiche.Lv2Espagnol,
                    Domain.Models.VoiesScolaires.LireSpecialites(fiche.Specialites)))
                .Select(m => new StatMatiereEleve
                {
                    MatiereId = m.Id,
                    MatiereCode = m.Code,
                    MatiereLibelle = m.Libelle,
                    ProfPrenom = m.ProfPrenom,
                    ProfAvatar = m.ProfAvatar,
                    ProfCouleur = m.ProfCouleur
                })
                .ToList();

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
                    matiere.CompetencesAssises = m.Assises;
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
                // Les fils qui ont VÉCU pendant l'année, et non ceux qui y ont
                // été ouverts : une conversation de maths commencée en 3e et
                // poursuivie en 2de appartient aux deux.
                .Where(c => c.Messages.Any(m =>
                    (depuis == null || m.DateCreation >= depuis)
                    && (jusqua == null || m.DateCreation < jusqua)))
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
                .Where(x => depuis == null || x.DerniereEvaluation >= depuis)
                .Where(x => jusqua == null || x.DerniereEvaluation < jusqua)
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

            // RIEN DE CE QUE L ENFANT TRAVAILLE NE DOIT DISPARAITRE.
            //
            // Chaque liste s arretait a dix lignes, en silence. Avec douze
            // notions sous le seuil, les dix places partaient aux scores les
            // plus bas — et toute la comprehension orale, entre 40 et 65 %,
            // tombait juste sous la ligne de flottaison. Six competences
            // mesurees, a jour, jamais montrees au parent : il croyait
            // qu on ne travaillait pas l oral.
            //
            // Une fiche compte quelques dizaines de competences, pas des
            // milliers : il n y a rien a economiser ici, et le repli se decide
            // a l affichage, ou il se voit et se deplie.

            // UNE SEULE OBSERVATION NE TRANCHE RIEN.
            //
            // Une competence vue une fois affichait « 16 % » au parent comme
            // un fait etabli. Elle attend desormais une seconde observation
            // avant de rejoindre les fragiles ou les acquis — la confiance
            // etait calculee depuis toujours, et n etait lue nulle part.
            // UNE MESURE DE SEPTEMBRE N EST PAS UNE MESURE D HIER.
            //
            // Un score ne bouge qu a une observation : une notion montee a
            // 85 % en septembre s affichait encore 85 % en juin, presentee au
            // parent comme si elle venait d etre verifiee. Passe le delai de
            // peremption, une ACQUISE rejoint « a confirmer » — on ne dit pas
            // que l enfant a oublie, on dit qu on ne sait plus. Le score, lui,
            // n est jamais retouche : voir SeuilsMaitrise.JoursAvantPeremption.
            var maintenant = DateTime.UtcNow;

            var tranchees = competences
                .Where(c => c.NombreObservations >= SeuilsMaitrise.ObservationsPourTrancher)
                .Where(c => !SeuilsMaitrise.Perimee(c.DerniereEvaluation, maintenant)
                            || c.Score < SeuilsMaitrise.Acquis)
                .ToList();

            // Les acquises perimees : elles quittent leur colonne, mais ne
            // quittent pas la fiche.
            var aVerifier = competences
                .Where(c => c.NombreObservations >= SeuilsMaitrise.ObservationsPourTrancher)
                .Where(c => c.Score >= SeuilsMaitrise.Acquis
                            && SeuilsMaitrise.Perimee(c.DerniereEvaluation, maintenant))
                .ToList();

            fiche.Lacunes = tranchees
                .Where(c => c.Score < SeuilsMaitrise.Fragile)
                .OrderBy(c => c.Score)
                .ToList();

            // « En cours » : commencees, pas encore tenues. Elles n avaient",
            // leur place nulle part et disparaissaient de la fiche — or c est
            // exactement la que se trouve un eleve qui travaille.
            fiche.EnCours = tranchees
                .Where(c => c.Score >= SeuilsMaitrise.Fragile && c.Score < SeuilsMaitrise.Acquis)
                .OrderBy(c => c.Score)
                .ToList();

            fiche.Acquises = tranchees
                .Where(c => c.Score >= SeuilsMaitrise.Acquis)
                .OrderByDescending(c => c.Score)
                .ToList();

            fiche.TotalFragiles = tranchees.Count(c => c.Score < SeuilsMaitrise.Fragile);

            fiche.TotalEnCours = tranchees.Count(
                c => c.Score >= SeuilsMaitrise.Fragile && c.Score < SeuilsMaitrise.Acquis);

            fiche.TotalAcquises = tranchees.Count(c => c.Score >= SeuilsMaitrise.Acquis);

            // DEUX FAMILLES, UNE SEULE COLONNE, ET LA MEME PHRASE : « ON NE
            // SAIT PAS ENCORE ». Celle qu on n a vue qu une fois, et celle
            // qu on a bien vue mais il y a trop longtemps. Les plus anciennes
            // d abord : ce sont celles dont la verification presse.
            var jamaisTranchees = competences
                .Where(c => c.NombreObservations < SeuilsMaitrise.ObservationsPourTrancher)
                .ToList();

            fiche.TotalAConfirmer = jamaisTranchees.Count + aVerifier.Count;

            fiche.AConfirmer = aVerifier
                .OrderBy(c => c.DerniereEvaluation)
                .Concat(jamaisTranchees.OrderBy(c => c.Score))
                .ToList();

            return fiche;
        }

        // Les seuils vivent desormais dans SeuilsMaitrise, partages par tout le produit.

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
            int id, string? prenom, string? nom, int? age, int? niveauScolaireId, Sexe? sexe,
            int? academieId = null, bool? lv2Espagnol = null, IEnumerable<string>? specialites = null)
        {
            var entite = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == id);
            if (entite is null) return null;

            if (prenom is not null) entite.Prenom = prenom;
            if (nom is not null) entite.Nom = nom;
            if (age is > 0) entite.Age = age.Value;

            // AVANT d'écraser la classe, comme côté parent : une fois la colonne
            // remplacée, l'ancienne est perdue et l'intervalle ne peut plus être
            // fermé. Le helper ne fait rien si la classe est inchangée.
            if (niveauScolaireId is > 0)
            {
                await HistoriqueClasse.ChangerAsync(_context, entite.Id, niveauScolaireId.Value);
                entite.NiveauScolaireId = niveauScolaireId.Value;
            }

            if (sexe is not null and not Sexe.NonPrecise) entite.Sexe = sexe.Value;

            // Facultative comme côté parent : null veut dire « pas de
            // changement », jamais « à effacer » — même raison que
            // NiveauScolaireId juste au-dessus.
            if (academieId.HasValue) entite.AcademieId = academieId;

            // LA LVB ET LES SPÉCIALITÉS, COMME LE PARENT LES POSE — Camara, le
            // 16/09/2026 : l'administration doit pouvoir tout ce qu'un parent
            // peut. Null veut dire « pas de changement » : un ancien écran qui
            // n'envoie pas ces champs ne les efface pas.
            if (lv2Espagnol.HasValue) entite.Lv2Espagnol = lv2Espagnol.Value;

            if (specialites is not null)
            {
                // Filtrées sur la classe FINALE, celle qu'on vient peut-être de
                // changer : les trois spécialités de première ne passent pas
                // telles quelles en terminale, ni en voie technologique. Le
                // serveur ne croit jamais la liste sur parole — même règle que
                // `EleveViewModelBuilder` côté parent.
                var codeNiveau = await _context.NiveauxScolaires.AsNoTracking()
                    .Where(n => n.Id == entite.NiveauScolaireId)
                    .Select(n => n.Code)
                    .FirstOrDefaultAsync();

                entite.Specialites = VoiesScolaires.EcrireSpecialites(
                    VoiesScolaires.SpecialitesRetenues(codeNiveau, specialites));
            }

            await _context.SaveChangesAsync();

            return (await GetElevesAsync(null, null)).FirstOrDefault(e => e.Id == id);
        }

        public Task<string?> MailDuParentAsync(int id) =>
            _context.Parents.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => p.Mail)
                .FirstOrDefaultAsync();

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
