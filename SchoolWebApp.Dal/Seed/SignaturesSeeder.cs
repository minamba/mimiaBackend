using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// LE MODÈLE DE SIGNATURE DE BASE (17/10/2026) : « Bien à vous, », le nom et
    /// le rôle de l'expéditeur, son adresse, et la ligne du site. Semé une fois,
    /// s'il n'existe aucun modèle ; l'administration le règle ensuite.
    /// </summary>
    public static class SignaturesSeeder
    {
        /// <summary>La phrase du modèle de base, sous la ligne d'icônes (image de Camara, 17/10/2026).</summary>
        private const string Slogan = "Le prof particulier d’une nouvelle ère";

        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            // LE MODÈLE DE BASE SEMÉ AVANT LE 17/10/2026, JAMAIS RETOUCHÉ, prend la
            // nouvelle mise en page de Camara : sans « Bien à vous », le nom en
            // marine, la phrase en italique. Un modèle déjà modifié n'est pas touché.
            var ancien = await context.SignaturesMail.FirstOrDefaultAsync(s =>
                s.Nom == "Signature Mimia"
                && s.Formule == "Bien à vous,"
                && s.LigneLibre == "Mimia — le professeur particulier de vos enfants"
                && s.Couleur == "#d63f1c", ct);
            if (ancien is not null)
            {
                ancien.Formule = null;
                ancien.LigneLibre = Slogan;
                ancien.Couleur = "#16233a";
                await context.SaveChangesAsync(ct);
            }

            if (await context.SignaturesMail.AnyAsync(ct)) return;

            context.SignaturesMail.Add(new SignatureMail
            {
                Nom = "Signature Mimia",
                Formule = null,
                LigneLibre = Slogan,
                AfficherRole = true,
                AfficherAdresse = true,
                AfficherSite = true,
                Couleur = "#16233a",
                ParDefaut = true,
                DateCreation = DateTime.UtcNow,
            });

            await context.SaveChangesAsync(ct);
        }
    }
}
