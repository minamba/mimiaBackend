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
                    JoursValidite = 7,
                    Ordre = 0,
                    Active = true,
                    EstEssai = true,
                },
                new Offre
                {
                    Code = "SOLO",
                    Libelle = "Solo",
                    Accroche = "Un enfant, un professeur par matière.",
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
                    Ordre = 1,
                    Active = true,
                    EstEssai = false,
                },
                new Offre
                {
                    Code = "DUO",
                    Libelle = "Duo",
                    Accroche = "Deux enfants, un pot d'heures à se partager.",
                    PrixMensuelCentimes = 6990,
                    PrixAnnuelCentimes = 76890, // onze mois payés, douze utilisés
                    NombreEnfantsMax = 2,
                    MinutesPotMensuel = 16 * Heure,

                    // Dix heures sur seize : le plafond garantit six heures au
                    // second enfant. Sans lui, l'aîné vide le pot avant que le
                    // cadet ait ouvert un cours.
                    MinutesPlafondEnfant = 10 * Heure,
                    JoursValidite = 0,
                    Ordre = 2,
                    Active = true,
                    EstEssai = false,
                },
                new Offre
                {
                    Code = "FAMILLE",
                    Libelle = "Famille",
                    Accroche = "Jusqu'à quatre enfants, chacun à son rythme.",
                    PrixMensuelCentimes = 9900,
                    PrixAnnuelCentimes = 108900, // onze mois payés, douze utilisés
                    NombreEnfantsMax = 4,

                    // Vingt-quatre heures et non vingt-six : c'est la formule
                    // qui a le moins de marge d'erreur — elle est déjà à 44 %
                    // du prix en coût d'IA. Deux heures de moins tiennent la
                    // règle des 50 % même si l'optimisation du cache ne rend
                    // que les deux tiers de ce qu'on en attend.
                    MinutesPotMensuel = 24 * Heure,
                    MinutesPlafondEnfant = 10 * Heure,
                    JoursValidite = 0,
                    Ordre = 3,
                    Active = true,
                    EstEssai = false,
                },
            };

            var codes = await context.Offres.Select(o => o.Code).ToListAsync(ct);
            var manquantes = offres.Where(o => !codes.Contains(o.Code)).ToList();

            if (manquantes.Count == 0) return;

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
