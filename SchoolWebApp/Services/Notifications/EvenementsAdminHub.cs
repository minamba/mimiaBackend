using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace SchoolWebApp.Api.Services.Notifications
{
    public interface IEvenementsAdminHub
    {
        /// <summary>
        /// Le flux d'événements de CETTE connexion — un canal par onglet
        /// d'administration ouvert. Se termine quand `ct` s'annule (l'onglet
        /// se ferme) ou que le serveur s'arrête.
        /// </summary>
        IAsyncEnumerable<string> SAbonnerAsync(CancellationToken ct);

        /// <summary>Prévient tous les onglets d'administration actuellement connectés.</summary>
        void Publier(string type);
    }

    /// <summary>
    /// Fait remonter en direct, aux onglets d'administration ouverts, qu'un
    /// signalement ou une visite vient d'arriver — sans qu'ils aient à
    /// interroger le serveur toutes les N secondes pour le savoir.
    ///
    /// POURQUOI PAS UN SIMPLE SONDAGE PÉRIODIQUE
    /// ------------------------------------------
    /// Une première version relançait l'appel toutes les 30 secondes. Deux
    /// défauts : l'écran restait faux jusqu'à une demi-minute après un vrai
    /// événement, et l'appel partait même quand rien ne s'était produit.
    /// Ici, c'est l'inverse — rien ne part tant qu'il ne se passe rien, et
    /// l'écran se met à jour à la seconde où quelque chose arrive.
    ///
    /// UN CANAL PAR ONGLET OUVERT, PAS UN SEUL PARTAGÉ. Un administrateur qui
    /// garde deux onglets ouverts doit voir les deux se mettre à jour ;
    /// fermer l'un ne doit pas couper l'autre. `Channel<string>` sert de
    /// boîte aux lettres à chaque abonné, exactement comme `FileObservation`
    /// et `FileConclusion` le font pour un seul lecteur — ici il y en a
    /// plusieurs, d'où la liste.
    /// </summary>
    public class EvenementsAdminHub : IEvenementsAdminHub
    {
        private readonly List<Channel<string>> _abonnes = new();
        private readonly object _verrou = new();

        public async IAsyncEnumerable<string> SAbonnerAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            var canal = Channel.CreateUnbounded<string>();

            lock (_verrou) { _abonnes.Add(canal); }

            try
            {
                await foreach (var evenement in canal.Reader.ReadAllAsync(ct))
                {
                    yield return evenement;
                }
            }
            finally
            {
                // Retiré même si le flux s'arrête par une exception (déconnexion
                // brutale) : un abonné oublié recevrait des événements dans le
                // vide indéfiniment, une fuite qui grossirait à chaque onglet
                // ouvert puis fermé.
                lock (_verrou) { _abonnes.Remove(canal); }
            }
        }

        public void Publier(string type)
        {
            lock (_verrou)
            {
                foreach (var canal in _abonnes)
                {
                    // Non bloquant et sans échec possible : le canal est
                    // illimité, `TryWrite` ne fait qu'écarter un abonné déjà
                    // parti dont le retrait n'a pas encore eu lieu.
                    canal.Writer.TryWrite(type);
                }
            }
        }
    }
}
