using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Affluence
{
    /// <summary>
    /// Les deux réglages de la salle d'attente, gardés au chaud.
    ///
    /// POURQUOI UNE MÉMOIRE ET NON UNE LECTURE. Le garde consulte ces valeurs
    /// à CHAQUE requête. Une lecture en base par requête ajouterait un
    /// aller-retour à la base sur le chemin de toutes les pages — sur le
    /// serveur qu'on essaie de soulager, et précisément aux heures où il est
    /// chargé. Le remède deviendrait le mal.
    ///
    /// DIX SECONDES DE RETARD ASSUMÉES. Quand Camara allume la salle depuis
    /// l'administration, elle prend effet dans les dix secondes. Pour un
    /// garde-fou de capacité, c'est sans conséquence : il n'existe pas
    /// d'afflux qui se joue à dix secondes près. Le prix serait tout autre à
    /// l'envers — d'où l'extinction immédiate ci-dessous.
    ///
    /// ÉTEINTE PAR DÉFAUT, et une lecture qui échoue laisse le site ouvert.
    /// Une base momentanément injoignable ne doit pas mettre tous les parents
    /// en file d'attente : le mode dégradé d'un garde-fou, c'est de laisser
    /// passer.
    /// </summary>
    public sealed class ReglageAffluence
    {
        /// <summary>La salle d'attente est-elle en service&nbsp;?</summary>
        public const string CleActive = "AFFLUENCE_ACTIVE";

        /// <summary>Combien de visiteurs à la fois.</summary>
        public const string ClePlaces = "AFFLUENCE_PLACES";

        /// <summary>
        /// Le nombre de places tant que rien n'a été réglé.
        ///
        /// CHOISI PRUDEMMENT HAUT, et à corriger par le test de charge — qui
        /// n'a jamais été fait. Un plafond trop bas fait patienter des gens
        /// que la machine aurait servis sans peine ; un plafond trop haut ne
        /// protège de rien. Tant que la mesure manque, le second défaut est
        /// le moins grave : il laisse le site dans l'état où il est
        /// aujourd'hui, au lieu de brider ce qui fonctionne.
        /// </summary>
        public const int PlacesParDefaut = 300;

        private static readonly TimeSpan Fraicheur = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _portees;
        private readonly ILogger<ReglageAffluence> _logger;
        private readonly SemaphoreSlim _relecture = new(1, 1);

        private volatile bool _active;
        private volatile int _places = PlacesParDefaut;
        private DateTime _lu = DateTime.MinValue;

        public ReglageAffluence(IServiceScopeFactory portees, ILogger<ReglageAffluence> logger)
        {
            _portees = portees ?? throw new ArgumentNullException(nameof(portees));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Active => _active;

        public int Places => _places;

        /// <summary>
        /// Éteint la salle sans attendre les dix secondes.
        ///
        /// L'ASYMÉTRIE EST VOULUE. Allumer peut attendre ; éteindre, non.
        /// Quand Camara coupe la salle, c'est en général parce qu'elle
        /// retient des gens à tort — dix secondes de plus, ce sont dix
        /// secondes pendant lesquelles il regarde son geste rester sans
        /// effet, et recommence.
        /// </summary>
        public void Oublier() => _lu = DateTime.MinValue;

        public async Task RafraichirAsync(CancellationToken ct = default)
        {
            if (DateTime.UtcNow - _lu < Fraicheur) return;

            // UNE SEULE RELECTURE À LA FOIS. À l'expiration, mille requêtes
            // arrivent ensemble et voudraient toutes relire : ce serait mille
            // lectures en base pour une seule valeur. Les autres repartent
            // avec la valeur précédente, vieille de dix secondes.
            if (!await _relecture.WaitAsync(0, ct)) return;

            try
            {
                if (DateTime.UtcNow - _lu < Fraicheur) return;

                using var portee = _portees.CreateScope();
                var reglages = portee.ServiceProvider.GetRequiredService<IReglageRepository>();

                _active = await reglages.EstActifAsync(CleActive, false, ct);

                var brut = await reglages.LireAsync(ClePlaces, ct);
                _places = int.TryParse(brut, out var places) && places > 0
                    ? places
                    : PlacesParDefaut;

                _lu = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                // ON LAISSE PASSER. Voir l'en-tête : un garde-fou aveugle
                // ouvre, il ne ferme pas.
                _active = false;
                _lu = DateTime.UtcNow;
                _logger.LogWarning(
                    ex, "Lecture des reglages d'affluence impossible : salle laissee ouverte.");
            }
            finally
            {
                _relecture.Release();
            }
        }
    }
}
