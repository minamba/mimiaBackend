using SchoolWebApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// La grille tarifaire.
    ///
    /// Idempotent comme le référentiel : chaque offre est identifiée par son
    /// Code et n'est insérée que si elle est absente. Une offre EXISTANTE n'est
    /// jamais modifiée par le seed — sinon un changement de prix en base serait
    /// écrasé au démarrage suivant, et une baisse de quota s'appliquerait
    /// rétroactivement aux abonnés en cours.
    ///
    /// Les quotas sont dimensionnés sur une MESURE et non sur une hypothèse :
    /// 48,9 minutes de cours réelles ont coûté 0,0452 $/minute — dont 87 % pour
    /// Claude, 13 % pour la synthèse vocale, et une part négligeable pour la
    /// transcription. La grille vise à ne jamais dépasser 50 % du prix TTC en
    /// coût d'IA, et suppose l'optimisation du cache de conversation faite
    /// (~0,0330 $/min). Sans elle, FAMILLE passe au-dessus du seuil.
    /// </summary>
    public static class OffresSeeder
    {
        private const int Heure = 60;

        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            await SeedOffresAsync(context, ct);
            await SeedRechargesAsync(context, ct);
            await SeedFichesAsync(context, ct);
        }

        /// <summary>
        /// LES PACKS DE CRÉDITS DE FICHES — Camara, le 15/10/2026 : 10 crédits
        /// pour 5,99 €, 20 pour 9,99 €. 1 crédit = 1 COURS IMPORTÉ, quel que soit
        /// le nombre de fiches qu'il donne (16/10/2026) ; un cours coûte
        /// ≈ 0,05 à 0,15 $ à lire et rédiger. La colonne s'appelle encore
        /// `fiches` : elle compte des crédits.
        ///
        /// Même règle que les recharges : on n'ajoute que ce qui manque. SEULE
        /// EXCEPTION, le libellé du 15/10 (« 10 fiches de révision ») : il
        /// annonçait des fiches là où l'on vend des cours, on le remplace.
        /// </summary>
        private static async Task SeedFichesAsync(SchoolWebAppDatabaseContext context, CancellationToken ct)
        {
            var packs = new[]
            {
                new OffreFiches { Code = "FICHES10", Libelle = "10 crédits de fiches de révision", Fiches = 10, PrixCentimes = 599, Ordre = 1, Active = true, Nature = NaturesCours.Fiches },
                new OffreFiches { Code = "FICHES20", Libelle = "20 crédits de fiches de révision", Fiches = 20, PrixCentimes = 999, Ordre = 2, Active = true, Nature = NaturesCours.Fiches },

                // LES PACKS DE PODCASTS — Camara, le 15/11/2026 : « 5 pour 7,90 et 10 pour 12,90 ». Un crédit = un cours
                // transformé en podcast (deux au-delà de dix pages), compteur à part des fiches.
                new OffreFiches { Code = "PODCASTS5", Libelle = "5 crédits de podcasts", Fiches = 5, PrixCentimes = 790, Ordre = 1, Active = true, Nature = NaturesCours.Podcasts },
                new OffreFiches { Code = "PODCASTS10", Libelle = "10 crédits de podcasts", Fiches = 10, PrixCentimes = 1290, Ordre = 2, Active = true, Nature = NaturesCours.Podcasts },
            };

            foreach (var pack in packs)
            {
                var ancien = $"{pack.Fiches} fiches de révision";
                await context.OffresFiches
                    .Where(o => o.Code == pack.Code && o.Libelle == ancien)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Libelle, pack.Libelle), ct);
            }

            // TOUTES LES NATURES (15/11/2026) : le filtre de la base ne montrerait que les fiches, et les packs de
            // podcasts seraient réinsérés à chaque démarrage — refusés par l'index unique sur le code.
            var codes = await context.OffresFiches.IgnoreQueryFilters().Select(o => o.Code).ToListAsync(ct);
            var manquants = packs.Where(o => !codes.Contains(o.Code)).ToList();

            if (manquants.Count == 0) return;

            context.OffresFiches.AddRange(manquants);
            await context.SaveChangesAsync(ct);
        }

        private static async Task SeedOffresAsync(SchoolWebAppDatabaseContext context, CancellationToken ct)
        {
            var offres = new[]
            {
                new Offre
                {
                    Code = "ESSAI",
                    Libelle = "Essai",
                    Accroche = "Une demi-heure de cours pour voir si le courant passe.",
                    PrixMensuelCentimes = 0,
                    PrixAnnuelCentimes = 0,
                    NombreEnfantsMax = 1,

                    // Trente minutes et non soixante : l'essai est offert à
                    // tout le monde, converti ou non. Il coûte donc son plein
                    // prix en coût d'acquisition, et une demi-heure suffit
                    // largement à juger d'un professeur.
                    MinutesPotMensuel = 30,
                    MinutesPlafondEnfant = 30,

                    // SANS DÉLAI — Camara, le 18/10/2026 : « il faut supprimer
                    // le délai, sinon je risque de faire fuir les utilisateurs
                    // s'ils n'ont pas eu le temps de tester ». Les trente
                    // minutes restent valables jusqu'à ce qu'elles soient
                    // utilisées. (Sept jours avant ; les bases existantes sont
                    // passées à 0 par la migration EssaiSansDelai.) L'essai ne
                    // se reconduit pas pour autant : voir `RafraichirAsync`.
                    JoursValidite = 0,
                    Ordre = 0,
                    Active = true,
                    EstEssai = true,
                },
                // STARTER — Camara, le 14/11/2026 : « les mêmes avantages que Solo, mais à 19,90 € pour 4 h 30 de
                // cours ». Un enfant, mêmes fiches, mêmes jeux, même mois de pause : tout se déduit des colonnes, rien
                // n'est écrit en dur pour Solo. 4,42 €/h, le prix horaire de Solo (4,43 €) : c'est une demi-formule,
                // pas une remise. Les recharges (4,69 et 4,97 €/h) restent plus chères à l'heure, la règle tient.
                // Annuel : onze mois payés, comme les autres (choix de Camara le 14/11/2026).
                // 4 H ET NON PLUS 4 H 30 (Camara, le 14/11/2026, migration HeuresDesFormules) : 4,98 €/h.
                new Offre
                {
                    Code = "STARTER",
                    Libelle = "Starter",
                    Accroche = "Pour des révisions et des besoins ponctuels.",
                    PrixMensuelCentimes = 1990,
                    PrixAnnuelCentimes = 21890,
                    NombreEnfantsMax = 1,
                    MinutesPotMensuel = 4 * Heure,
                    MinutesPlafondEnfant = 4 * Heure,
                    JoursValidite = 0,
                    Ordre = 1,
                    Active = true,
                    EstEssai = false,
                },
                new Offre
                {
                    Code = "SOLO",
                    Libelle = "Solo",
                    Accroche = "Pour un accompagnement scolaire régulier.",
                    PrixMensuelCentimes = 3990,

                    // Onze mois payés, douze utilisés — et non dix. Sur un
                    // logiciel, deux mois offerts ne coûtent presque rien : le
                    // coût marginal y est nul. Ici il représente 44 % du prix,
                    // et 16,7 % de remise emportait plus du tiers de la marge.
                    // FAMILLE passait même au-dessus du seuil des 50 %.
                    PrixAnnuelCentimes = 43890,
                    NombreEnfantsMax = 1,
                    MinutesPotMensuel = 9 * Heure,
                    MinutesPlafondEnfant = 9 * Heure,
                    JoursValidite = 0,
                    Ordre = 2,
                    Active = true,
                    EstEssai = false,
                },
                new Offre
                {
                    Code = "DUO",
                    Libelle = "Duo",
                    Accroche = "Pour accompagner deux enfants avec un seul abonnement.",
                    PrixMensuelCentimes = 6990,
                    PrixAnnuelCentimes = 76890, // onze mois payés, douze utilisés
                    NombreEnfantsMax = 2,
                    // 18 H ET UN PLAFOND DE 12 (Camara, le 14/11/2026, migration
                    // HeuresDesFormules ; avant : 16 h et 10 h).
                    MinutesPotMensuel = 18 * Heure,

                    // Douze heures sur dix-huit : le plafond garantit six heures
                    // au second enfant. Sans lui, l'aîné vide le pot avant que le
                    // cadet ait ouvert un cours.
                    MinutesPlafondEnfant = 12 * Heure,
                    JoursValidite = 0,
                    Ordre = 3,
                    Active = true,
                    EstEssai = false,
                },
                new Offre
                {
                    Code = "FAMILLE",
                    Libelle = "Famille",
                    Accroche = "Pour accompagner jusqu'à quatre enfants avec un seul abonnement.",
                    PrixMensuelCentimes = 9900,
                    PrixAnnuelCentimes = 108900, // onze mois payés, douze utilisés
                    NombreEnfantsMax = 4,

                    // 28 H ET UN PLAFOND DE 12 (Camara, le 14/11/2026, migration
                    // HeuresDesFormules ; avant : 24 h et 10 h). 32 h demandées,
                    // ramenées à 28 : à pleine consommation, 32 h coûtaient
                    // ~92 % du prix en IA (3,3 $/h mesurés le 03/10), 28 h
                    // ~81 %. C'est la formule qui a le moins de marge d'erreur.
                    MinutesPotMensuel = 28 * Heure,
                    MinutesPlafondEnfant = 12 * Heure,
                    JoursValidite = 0,
                    Ordre = 4,
                    Active = true,
                    EstEssai = false,
                },
            };

            // LES SOUS-TITRES DU 14/11/2026 (Camara). Seule exception à « une offre existante n'est jamais modifiée » :
            // un texte de présentation, ni prix ni quota. Remplacé seulement si l'ANCIEN texte est encore là.
            var anciennesAccroches = new Dictionary<string, string>
            {
                ["STARTER"] = "Un enfant, quelques heures par mois pour reprendre pied.",
                ["SOLO"] = "Un enfant, un professeur par matière.",
                ["DUO"] = "Deux enfants, un pot d'heures à se partager.",
                ["FAMILLE"] = "Jusqu'à quatre enfants, chacun à son rythme.",
            };
            foreach (var (code, ancienne) in anciennesAccroches)
            {
                var nouvelle = offres.First(o => o.Code == code).Accroche;
                await context.Offres
                    .Where(o => o.Code == code && o.Accroche == ancienne)
                    .ExecuteUpdateAsync(x => x.SetProperty(o => o.Accroche, nouvelle), ct);
            }

            var codes = await context.Offres.Select(o => o.Code).ToListAsync(ct);
            var manquantes = offres.Where(o => !codes.Contains(o.Code)).ToList();

            if (manquantes.Count == 0) return;

            // STARTER PASSE EN TÊTE (14/11/2026), une seule fois : à son arrivée dans une base qui a déjà la grille, les
            // formules payantes reculent d'un rang. Seul l'ordre d'affichage bouge — jamais un prix ni un quota.
            if (manquantes.Any(o => o.Code == "STARTER") && codes.Count > 0)
            {
                await context.Offres
                    .Where(o => !o.EstEssai && o.Ordre >= 1)
                    .ExecuteUpdateAsync(x => x.SetProperty(o => o.Ordre, o => o.Ordre + 1), ct);
            }

            context.Offres.AddRange(manquantes);
            await context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Les recharges. Volontairement PLUS CHÈRES à l'heure que la formule
        /// la plus chère (4,97 € et 4,69 € contre 4,43 € sur SOLO) : sinon un
        /// gros consommateur prendrait la formule la moins chère et vivrait de
        /// recharges.
        ///
        /// Ce rapport se vérifie à CHAQUE changement de grille. Aux anciens
        /// tarifs, les packs étaient tombés sous le prix horaire de SOLO et de
        /// DUO — la règle était inversée sans que rien ne le signale.
        /// </summary>
        private static async Task SeedRechargesAsync(SchoolWebAppDatabaseContext context, CancellationToken ct)
        {
            var recharges = new[]
            {
                new OffreRecharge
                {
                    Code = "PACK3H",
                    Libelle = "3 heures de plus",
                    Minutes = 3 * Heure,
                    PrixCentimes = 1490,
                    Ordre = 1,
                    Active = true,
                },
                new OffreRecharge
                {
                    Code = "PACK10H",
                    Libelle = "10 heures de plus",
                    Minutes = 10 * Heure,
                    PrixCentimes = 4690,
                    Ordre = 2,
                    Active = true,
                },
            };

            var codes = await context.OffresRecharge.Select(o => o.Code).ToListAsync(ct);
            var manquantes = recharges.Where(o => !codes.Contains(o.Code)).ToList();

            if (manquantes.Count == 0) return;

            context.OffresRecharge.AddRange(manquantes);
            await context.SaveChangesAsync(ct);
        }
    }
}
