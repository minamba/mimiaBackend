using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Le journal des appels de fond.
    ///
    /// IL OUVRE SA PROPRE PORTÉE, ET C'EST CE QUI LE REND UTILISABLE PARTOUT.
    /// Les quatre appelants n'ont pas la même forme : deux sont des services de
    /// fond qui vivent aussi longtemps que l'application, deux sont des
    /// services à portée de requête. Un journal à portée de requête aurait
    /// obligé les premiers à faire descendre une portée jusqu'au fond de leurs
    /// méthodes privées — donc à changer une dizaine de signatures pour écrire
    /// quatre entiers.
    ///
    /// Enregistré en singleton, il crée une portée par écriture. Une écriture
    /// par appel au modèle, soit quelques-unes par minute au pire : le coût est
    /// sans commune mesure avec ce qu'il mesure.
    /// </summary>
    public class JournalClaudeRepository : IJournalClaudeRepository
    {
        private readonly IServiceScopeFactory _portees;
        private readonly ILogger<JournalClaudeRepository> _logger;

        public JournalClaudeRepository(
            IServiceScopeFactory portees,
            ILogger<JournalClaudeRepository> logger)
        {
            _portees = portees ?? throw new ArgumentNullException(nameof(portees));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EnregistrerAsync(
            string origine,
            string? modele,
            long entree,
            long sortie,
            long cacheLecture,
            long cacheEcriture,
            string? reference = null,
            CancellationToken ct = default)
        {
            try
            {
                using var portee = _portees.CreateScope();
                var db = portee.ServiceProvider
                    .GetRequiredService<SchoolWebAppDatabaseContext>();

                db.AppelsClaude.Add(new AppelClaude
                {
                    Origine = origine,
                    Modele = modele,
                    TokensEntree = (int)entree,
                    TokensSortie = (int)sortie,
                    TokensCacheLecture = (int)cacheLecture,
                    TokensCacheEcriture = (int)cacheEcriture,
                    Reference = reference,
                    DateCreation = DateTime.UtcNow,
                });

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // UNE MESURE MANQUÉE NE DOIT JAMAIS COÛTER LE TRAVAIL MESURÉ.
                //
                // Ce journal sert à comprendre une facture ; l'appel qu'il note
                // est déjà passé et déjà payé. Laisser remonter une exception
                // ferait échouer une description de planche parce qu'on n'a pas
                // pu écrire combien elle a coûté — on perdrait la chose ET sa
                // mesure. On trace, et on continue.
                _logger.LogError(ex,
                    "Consommation NON ENREGISTREE pour {Origine} : l'appel a bien eu lieu, "
                    + "mais il manquera au total.", origine);
            }
        }
    }
}
