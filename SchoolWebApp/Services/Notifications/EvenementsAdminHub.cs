using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace SchoolWebApp.Api.Services.Notifications
{
    public interface IEvenementsAdminHub
    {
        /// <summary>
        /// Le flux d'événements de CETTE connexion — un canal par onglet
        /// d'administration ouvert. Se termine quand `ct` s'annule (l'onglet
        /// se ferme) ou que le serveur s'arrête. Chaque élément est la charge
        /// JSON de l'événement, prête à écrire après « data: ».
        ///
        /// `parentId` : le compte de l'administrateur connecté, quand on le
        /// connaît — c'est ce qui permet de ne prévenir que LUI d'un courriel
        /// arrivé dans SA boîte (19/10/2026).
        /// </summary>
        IAsyncEnumerable<string> SAbonnerAsync(int? parentId, CancellationToken ct);

        /// <summary>Prévient tous les onglets d'administration actuellement connectés.</summary>
        void Publier(string type);

        /// <summary>
        /// Prévient les seuls onglets ouverts par ce compte. `charge` est
        /// sérialisée telle quelle ; elle porte son propre champ `type`.
        /// </summary>
        void PublierA(int parentId, object charge);

        /// <summary>Les comptes qui ont au moins un onglet d'administration ouvert.</summary>
        IReadOnlyCollection<int> ParentsConnectes();
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
    ///
    /// CHAQUE ABONNÉ SAIT À QUI IL EST (19/10/2026) : les courriels arrivés
    /// dans une boîte d'administrateur ne regardent que lui. Les signalements
    /// et les visites, eux, partent toujours à tout le monde.
    /// </summary>
    public class EvenementsAdminHub : IEvenementsAdminHub
    {
        private sealed record Abonne(Channel<string> Canal, int? ParentId);

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly List<Abonne> _abonnes = new();
        private readonly object _verrou = new();

        public async IAsyncEnumerable<string> SAbonnerAsync(
            int? parentId, [EnumeratorCancellation] CancellationToken ct)
        {
            var abonne = new Abonne(Channel.CreateUnbounded<string>(), parentId);

            lock (_verrou) { _abonnes.Add(abonne); }

            try
            {
                await foreach (var evenement in abonne.Canal.Reader.ReadAllAsync(ct))
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
                lock (_verrou) { _abonnes.Remove(abonne); }
            }
        }

        public void Publier(string type) => Ecrire(null, JsonSerializer.Serialize(new { type }, Json));

        public void PublierA(int parentId, object charge) => Ecrire(parentId, JsonSerializer.Serialize(charge, Json));

        public IReadOnlyCollection<int> ParentsConnectes()
        {
            lock (_verrou)
            {
                return _abonnes.Where(a => a.ParentId is not null).Select(a => a.ParentId!.Value).ToHashSet();
            }
        }

        private void Ecrire(int? seulementA, string json)
        {
            lock (_verrou)
            {
                foreach (var abonne in _abonnes)
                {
                    if (seulementA is int id && abonne.ParentId != id) continue;

                    // Non bloquant et sans échec possible : le canal est
                    // illimité, `TryWrite` ne fait qu'écarter un abonné déjà
                    // parti dont le retrait n'a pas encore eu lieu.
                    abonne.Canal.Writer.TryWrite(json);
                }
            }
        }
    }
}
