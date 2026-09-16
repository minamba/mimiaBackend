using Microsoft.Extensions.Logging;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// Le service d'envoi, avec la liste des adresses bannies devant.
    ///
    /// UNE ENVELOPPE PLUTÔT QU'UNE MODIFICATION DE `ServiceEmail` : l'envoi, la
    /// session, la reconnexion et le coupe-circuit restent tels quels, et le
    /// contrôle se tient en un seul endroit, qui voit passer TOUS les
    /// courriels — envoi simple, diffusion, session de bilans.
    ///
    /// UNE ADRESSE BANNIE N'EST NI ENVOYÉE NI COMPTÉE EN ÉCHEC : elle est
    /// écartée avant. Une diffusion à trois cents parents dont deux bannis
    /// annonce deux cent quatre-vingt-dix-huit envois, sans deux « échecs »
    /// qui feraient chercher une panne qui n'existe pas.
    /// </summary>
    public class ServiceEmailFiltre : IServiceEmail
    {
        private readonly IServiceEmail _service;
        private readonly IFiltreEnvoi _filtre;
        private readonly ILogger<ServiceEmailFiltre> _logger;

        public ServiceEmailFiltre(IServiceEmail service, IFiltreEnvoi filtre, ILogger<ServiceEmailFiltre> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _filtre = filtre ?? throw new ArgumentNullException(nameof(filtre));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Disponible => _service.Disponible;

        public async Task<bool> EnvoyerAsync(
            string destinataire,
            string sujet,
            string gabarit,
            IDictionary<string, string> valeurs,
            CancellationToken ct = default)
        {
            if (await BloqueAsync(destinataire, sujet, ct)) return false;

            return await _service.EnvoyerAsync(destinataire, sujet, gabarit, valeurs, ct);
        }

        public async Task<ResultatDiffusion> DiffuserAsync(
            IReadOnlyList<string> destinataires,
            string sujet,
            string gabarit,
            IDictionary<string, string> valeurs,
            IReadOnlyList<PieceMail>? imagesIntegrees = null,
            IReadOnlyList<PieceMail>? piecesJointes = null,
            IProgress<int>? avancement = null,
            CancellationToken ct = default)
        {
            var autorises = new List<string>(destinataires.Count);

            foreach (var destinataire in destinataires)
            {
                if (!await BloqueAsync(destinataire, sujet, ct)) autorises.Add(destinataire);
            }

            return await _service.DiffuserAsync(
                autorises, sujet, gabarit, valeurs, imagesIntegrees, piecesJointes, avancement, ct);
        }

        public Task<string> ComposerAsync(string sujet, string gabarit, IDictionary<string, string> valeurs) =>
            _service.ComposerAsync(sujet, gabarit, valeurs);

        public Task<string> RendreAsync(string sujet, string gabarit, IDictionary<string, string> valeurs) =>
            _service.RendreAsync(sujet, gabarit, valeurs);

        public ISessionEnvoi OuvrirSession() => new SessionFiltree(_service.OuvrirSession(), this);

        private async Task<bool> BloqueAsync(string? destinataire, string sujet, CancellationToken ct)
        {
            if (!await _filtre.EstBloqueAsync(destinataire, ct)) return false;

            // L'adresse n'est pas écrite dans le journal : elle est bannie, et le
            // journal n'a pas à devenir la liste de ceux qu'on a écartés.
            _logger.LogInformation("Courriel « {Sujet} » non envoye : adresse bannie.", sujet);
            return true;
        }

        /// <summary>La session d'envoi, avec le même contrôle à chaque message.</summary>
        private sealed class SessionFiltree : ISessionEnvoi
        {
            private readonly ISessionEnvoi _session;
            private readonly ServiceEmailFiltre _parent;

            public SessionFiltree(ISessionEnvoi session, ServiceEmailFiltre parent)
            {
                _session = session;
                _parent = parent;
            }

            public int Envoyes => _session.Envoyes;

            public int Echecs => _session.Echecs;

            public async Task<bool> EnvoyerAsync(
                string destinataire,
                string sujet,
                string gabarit,
                IDictionary<string, string> valeurs,
                CancellationToken ct = default)
            {
                if (await _parent.BloqueAsync(destinataire, sujet, ct)) return false;

                return await _session.EnvoyerAsync(destinataire, sujet, gabarit, valeurs, ct);
            }

            public async Task<bool> EnvoyerAsync(
                string destinataire,
                string sujet,
                string gabarit,
                IDictionary<string, string> valeurs,
                IReadOnlyList<PieceMail>? images,
                IReadOnlyList<PieceMail>? documents,
                IReadOnlyDictionary<string, string>? enTetes,
                CancellationToken ct = default)
            {
                if (await _parent.BloqueAsync(destinataire, sujet, ct)) return false;

                return await _session.EnvoyerAsync(
                    destinataire, sujet, gabarit, valeurs, images, documents, enTetes, ct);
            }

            public ValueTask DisposeAsync() => _session.DisposeAsync();
        }
    }
}
