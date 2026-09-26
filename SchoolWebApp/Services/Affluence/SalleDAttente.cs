namespace SchoolWebApp.Api.Services.Affluence
{
    /// <summary>
    /// Ce qu'un visiteur reçoit quand il demande à entrer.
    /// </summary>
    /// <param name="Admis">Peut-il entrer maintenant&nbsp;?</param>
    /// <param name="Jeton">
    /// Son billet. Il le garde et le renvoie à chaque appel : c'est lui qui
    /// prouve qu'il a fait la queue.
    /// </param>
    /// <param name="Rang">Sa place dans la file, 1 étant le prochain servi.</param>
    /// <param name="Devant">Combien de personnes le précèdent.</param>
    /// <param name="AttenteSecondes">
    /// L'attente estimée, ou <c>null</c> quand on ne sait pas encore.
    ///
    /// NULL PLUTÔT QU'UN CHIFFRE INVENTÉ. Tant que personne n'est sorti, il
    /// n'existe aucun débit à partir duquel estimer quoi que ce soit.
    /// Annoncer « environ 2 minutes » sans le savoir, c'est promettre — et
    /// une promesse ratée coûte plus cher que l'absence d'estimation.
    /// </param>
    /// <param name="RappelDans">
    /// Dans combien de secondes revenir demander. Le serveur fixe le rythme,
    /// pas le navigateur : c'est ce qui l'empêche d'être piétiné par ceux-là
    /// mêmes qu'il fait patienter.
    /// </param>
    public sealed record Billet(
        bool Admis,
        Guid Jeton,
        int Rang,
        int Devant,
        int? AttenteSecondes,
        int RappelDans);

    /// <summary>L'état de la salle, pour l'administration.</summary>
    public sealed record EtatSalle(int Places, int Occupees, int EnAttente, int? DebitParMinute);

    public interface ISalleDAttente
    {
        /// <summary>
        /// Demande à entrer, ou renouvelle une place déjà obtenue.
        ///
        /// <paramref name="jetonConnu"/> vaut <see cref="Guid.Empty"/> pour un
        /// visiteur qui se présente sans billet.
        /// </summary>
        Billet Demander(Guid jetonConnu, int placesMax);

        /// <summary>
        /// Ce jeton est-il admis&nbsp;? Rafraîchit au passage son horodatage.
        /// Appelé par le garde, à chaque requête : doit rester très bon marché.
        /// </summary>
        bool EstAdmis(Guid jeton);

        /// <summary>Rend une place (fermeture de l'onglet annoncée).</summary>
        void Liberer(Guid jeton);

        /// <summary>Vide la salle. Appelé quand on éteint le réglage.</summary>
        void Ouvrir();

        EtatSalle Etat(int placesMax);
    }

    /// <summary>
    /// LA SALLE D'ATTENTE — le rang affiché, et le plafond qui le rend utile.
    ///
    /// VOULUE PAR CAMARA LE 25/09/2026 : « mettre les personnes dans une file
    /// d'attente si le serveur ne supporte pas ». Le besoin derrière la
    /// demande n'est pas d'afficher un rang, c'est qu'un afflux ne fasse pas
    /// tomber le service pour tout le monde. L'affichage est la partie
    /// visible ; le plafond d'admission est la partie qui travaille.
    ///
    /// CE QU'ELLE COMPTE : des VISITEURS admis, pas des requêtes en cours. Un
    /// parent qui lit une page tranquillement occupe une place, parce qu'il
    /// reviendra. Limiter les requêtes simultanées protégerait le processeur
    /// une seconde et rendrait le site inutilisable la suivante ; limiter les
    /// visiteurs donne à ceux qui sont entrés une séance entière qui
    /// fonctionne. Mieux vaut trois cents parents servis et cent qui
    /// patientent, que quatre cents parents devant un site qui ne répond plus.
    ///
    /// EN MÉMOIRE, ET C'EST ASSUMÉ. L'API tourne en un seul exemplaire — ses
    /// seize workers de fond l'imposent déjà, sans quoi chaque bilan
    /// hebdomadaire partirait en double. Un état partagé (Redis, base) serait
    /// une dépendance de plus pour résoudre un problème qu'on n'a pas. Le
    /// jour où l'API se dédoublera, cette classe sera à reprendre — et elle ne
    /// sera pas la première.
    ///
    /// CONSÉQUENCE À CONNAÎTRE : un redémarrage vide la salle. Tout le monde
    /// rentre d'un coup. C'est le bon comportement — au redémarrage, le
    /// serveur est au repos — mais il faut le savoir avant de le constater.
    ///
    /// UN VERROU PLUTÔT QUE DES STRUCTURES SANS VERROU. Le rang d'un visiteur
    /// n'a de sens que rapporté à une photographie cohérente de la file ;
    /// calculé sur des dictionnaires qui bougent, il sauterait dans tous les
    /// sens sous les yeux du parent. Le verrou est tenu quelques microsecondes
    /// et n'est pris qu'une fois par appel.
    /// </summary>
    public sealed class SalleDAttente : ISalleDAttente
    {
        /// <summary>
        /// Sans signe de vie depuis ce délai, une place est rendue.
        ///
        /// Le site donne signe toutes les 30 secondes. Trois battements
        /// manqués valent départ : en-dessous, un téléphone qui passe sous un
        /// tunnel perdrait sa place et repartirait au bout de la file.
        /// </summary>
        private static readonly TimeSpan SilenceAdmis = TimeSpan.FromSeconds(95);

        /// <summary>
        /// Sans nouvelles depuis ce délai, on quitte la file.
        ///
        /// BIEN PLUS COURT, et pour une raison précise : celui qui ferme
        /// l'onglet en attendant ne revient pas, et chaque fantôme laissé dans
        /// la file allonge le rang annoncé à tous ceux qui sont derrière.
        /// Une file qui ment est pire qu'une file.
        /// </summary>
        private static readonly TimeSpan SilenceAttente = TimeSpan.FromSeconds(30);

        /// <summary>Le balayage ne se refait pas à chaque appel : il coûte un parcours.</summary>
        private static readonly TimeSpan PasDeBalayage = TimeSpan.FromSeconds(2);

        /// <summary>Fenêtre sur laquelle se mesure le débit de sortie.</summary>
        private static readonly TimeSpan FenetreDebit = TimeSpan.FromMinutes(2);

        private readonly object _verrou = new();

        /// <summary>Jeton admis → dernier signe de vie.</summary>
        private readonly Dictionary<Guid, DateTime> _admis = [];

        /// <summary>Jeton en attente → dernier signe de vie.</summary>
        private readonly Dictionary<Guid, DateTime> _attente = [];

        /// <summary>Jeton en attente → son numéro d'ordre. C'est lui qui fait la file.</summary>
        private readonly Dictionary<Guid, long> _numeroDe = [];

        /// <summary>Les numéros en attente, triés. Le plus petit est le prochain servi.</summary>
        private readonly SortedSet<long> _file = [];

        /// <summary>Les numéros vers leur jeton, pour servir la tête de file.</summary>
        private readonly Dictionary<long, Guid> _jetonDe = [];

        /// <summary>Horodatage des dernières admissions, pour estimer l'attente.</summary>
        private readonly Queue<DateTime> _admissions = new();

        private long _prochainNumero;
        private DateTime _dernierBalayage = DateTime.MinValue;

        public Billet Demander(Guid jetonConnu, int placesMax)
        {
            if (placesMax < 1) placesMax = 1;

            lock (_verrou)
            {
                var maintenant = DateTime.UtcNow;
                Balayer(maintenant);

                // DÉJÀ DEDANS : on renouvelle, on ne refait pas la queue. Ce cas
                // est le plus fréquent de tous — c'est le battement régulier
                // d'un parent en pleine séance.
                if (_admis.ContainsKey(jetonConnu))
                {
                    _admis[jetonConnu] = maintenant;
                    return new Billet(true, jetonConnu, 0, 0, null, 30);
                }

                var jeton = jetonConnu == Guid.Empty ? Guid.NewGuid() : jetonConnu;

                // DÉJÀ DANS LA FILE : on garde son numéro. Un visiteur qui
                // rafraîchit sa page ne doit pas repartir en queue — c'est
                // exactement ce qui pousse les gens à marteler F5.
                if (!_numeroDe.TryGetValue(jeton, out var numero))
                {
                    numero = ++_prochainNumero;
                    _numeroDe[jeton] = numero;
                    _file.Add(numero);
                    _jetonDe[numero] = jeton;
                }

                _attente[jeton] = maintenant;

                // ON N'ADMET QUE DANS L'ORDRE. Sans cette règle, celui qui
                // interroge le plus souvent passe devant celui qui attend
                // patiemment : la file deviendrait une loterie qui récompense
                // le rafraîchissement compulsif.
                var libres = placesMax - _admis.Count;
                if (libres > 0)
                {
                    foreach (var premier in _file.Take(libres).ToList())
                    {
                        var servi = _jetonDe[premier];
                        RetirerDeLaFile(servi);
                        _admis[servi] = maintenant;
                        _admissions.Enqueue(maintenant);
                    }

                    if (_admis.ContainsKey(jeton))
                        return new Billet(true, jeton, 0, 0, null, 30);
                }

                var devant = _file.GetViewBetween(long.MinValue, numero).Count - 1;
                return new Billet(
                    false, jeton, devant + 1, devant, Estimer(devant, maintenant),
                    Rythme(devant));
            }
        }

        public bool EstAdmis(Guid jeton)
        {
            if (jeton == Guid.Empty) return false;

            lock (_verrou)
            {
                if (!_admis.ContainsKey(jeton)) return false;
                _admis[jeton] = DateTime.UtcNow;
                return true;
            }
        }

        public void Liberer(Guid jeton)
        {
            if (jeton == Guid.Empty) return;

            lock (_verrou)
            {
                _admis.Remove(jeton);
                RetirerDeLaFile(jeton);
                _attente.Remove(jeton);
            }
        }

        public void Ouvrir()
        {
            lock (_verrou)
            {
                _admis.Clear();
                _attente.Clear();
                _numeroDe.Clear();
                _file.Clear();
                _jetonDe.Clear();
                _admissions.Clear();
            }
        }

        public EtatSalle Etat(int placesMax)
        {
            lock (_verrou)
            {
                var maintenant = DateTime.UtcNow;
                Balayer(maintenant);
                return new EtatSalle(
                    placesMax, _admis.Count, _file.Count, Debit(maintenant));
            }
        }

        /// <summary>
        /// Retire les silencieux. Appelé sous verrou, jamais seul.
        /// </summary>
        private void Balayer(DateTime maintenant)
        {
            if (maintenant - _dernierBalayage < PasDeBalayage) return;
            _dernierBalayage = maintenant;

            foreach (var parti in _admis
                         .Where(p => maintenant - p.Value > SilenceAdmis)
                         .Select(p => p.Key).ToList())
            {
                _admis.Remove(parti);
            }

            foreach (var parti in _attente
                         .Where(p => maintenant - p.Value > SilenceAttente)
                         .Select(p => p.Key).ToList())
            {
                _attente.Remove(parti);
                RetirerDeLaFile(parti);
            }

            while (_admissions.Count > 0 && maintenant - _admissions.Peek() > FenetreDebit)
                _admissions.Dequeue();
        }

        private void RetirerDeLaFile(Guid jeton)
        {
            if (!_numeroDe.TryGetValue(jeton, out var numero)) return;
            _numeroDe.Remove(jeton);
            _file.Remove(numero);
            _jetonDe.Remove(numero);
        }

        /// <summary>Combien de places se libèrent par minute, ou null si on l'ignore.</summary>
        private int? Debit(DateTime maintenant)
        {
            if (_admissions.Count < 3) return null;
            var minutes = FenetreDebit.TotalMinutes;
            var parMinute = (int)Math.Round(_admissions.Count / minutes);
            return parMinute < 1 ? null : parMinute;
        }

        private int? Estimer(int devant, DateTime maintenant)
        {
            var debit = Debit(maintenant);
            if (debit is null) return null;

            // ARRONDI AU-DESSUS, toujours. Une attente annoncée trop courte se
            // paie en agacement ; annoncée trop longue, elle se paie en bonne
            // surprise.
            var secondes = (int)Math.Ceiling((devant + 1) * 60.0 / debit.Value);
            return Math.Clamp(secondes, 10, 3600);
        }

        /// <summary>
        /// À quel rythme revenir demander.
        ///
        /// ESPACÉ QUAND LA FILE EST LONGUE, et c'est tout le sujet : mille
        /// personnes qui redemandent toutes les trois secondes, ce sont
        /// trois cents requêtes par seconde sur un serveur qu'on essaie
        /// justement de soulager. Le premier de la file, lui, doit être
        /// prévenu vite : sa place l'attend.
        /// </summary>
        private static int Rythme(int devant) => devant switch
        {
            < 20 => 3,
            < 100 => 6,
            < 500 => 12,
            _ => 20,
        };
    }
}
