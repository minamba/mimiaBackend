using System.Collections.Concurrent;
using System.Threading.Channels;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>Ce qu'on sait d'un son, et la demande de le tester quand on ne sait rien.</summary>
    public interface IFiabiliteDesSons
    {
        /// <summary>Vrai ou faux si ce son a été testé ; null s'il ne l'a pas été, ou si la base ne répond pas.</summary>
        Task<bool?> LireAsync(string mot, CancellationToken ct, string langue = LanguesDesSons.Francais);

        /// <summary>L'interrupteur de l'admin ; faux si la base ne répond pas.</summary>
        Task<bool> ActifAsync(CancellationToken ct);

        /// <summary>Les sons fiables de cette langue ; vide si la base ne répond pas.</summary>
        Task<IReadOnlyList<string>> ListeAsync(string langue, CancellationToken ct);

        /// <summary>Le fait tester en arrière-plan, une seule fois : l'enfant n'attend jamais ce test.</summary>
        void Demander(string mot);
    }

    public sealed class FiabiliteDesSons : IFiabiliteDesSons
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<FiabiliteDesSons> _logger;
        private readonly ConcurrentDictionary<string, byte> _enCours = new(StringComparer.Ordinal);

        private readonly Channel<string> _file = Channel.CreateBounded<string>(
            new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

        public FiabiliteDesSons(IServiceScopeFactory scopes, ILogger<FiabiliteDesSons> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ChannelReader<string> Demandes => _file.Reader;

        public async Task<bool?> LireAsync(string mot, CancellationToken ct, string langue = LanguesDesSons.Francais)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<IMotsVerifiesRepository>().FiableAsync(mot, ct, langue);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Fiabilite d'un son illisible : {Cause}.", ex.GetType().Name);
                return null;
            }
        }

        public async Task<bool> ActifAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<IReglageRepository>()
                    .EstActifAsync(RegleVerificationSon.Reglage, RegleVerificationSon.ActifParDefaut, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Interrupteur des sons illisible : {Cause}.", ex.GetType().Name);
                return false;
            }
        }

        public async Task<IReadOnlyList<string>> ListeAsync(string langue, CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var fiables = await scope.ServiceProvider.GetRequiredService<IMotsVerifiesRepository>().FiablesAsync(langue, ct);
                return langue == LanguesDesSons.Francais ? fiables.Where(m => !BanqueDeSons.EstRefuse(m)).ToList() : fiables;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Liste des sons illisible : {Cause}.", ex.GetType().Name);
                return [];
            }
        }

        public void Demander(string mot)
        {
            if (_enCours.TryAdd(mot, 0) && !_file.Writer.TryWrite(mot)) _enCours.TryRemove(mot, out _);
        }

        /// <summary>Le test est fini ou raté : une prochaine apparition pourra le redemander.</summary>
        public void Terminer(string mot) => _enCours.TryRemove(mot, out _);
    }
}
