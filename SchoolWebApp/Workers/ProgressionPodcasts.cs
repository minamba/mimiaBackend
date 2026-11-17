using System.Collections.Concurrent;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// OÙ EN EST LA FABRICATION D'UN PODCAST — Camara, le 15/11/2026 : « une barre de progression qui reste fixe même si
    /// on change d'onglet, pour voir l'avancement de la génération ; à 100 %, une fenêtre dit que le podcast est prêt ».
    ///
    /// La base ne sait compter que les épisodes FINIS : avec un seul épisode, la barre sauterait de 0 à 100. La tâche de
    /// fond note donc ici, en mémoire, l'avancée de l'épisode en cours (son texte, puis chaque paragraphe dit par la
    /// voix). Une seule instance de l'API tourne : la mémoire suffit. Après un redémarrage, l'écran retombe sur le
    /// compte des épisodes finis, sans rien casser.
    /// </summary>
    public sealed class ProgressionPodcasts
    {
        /// <summary>Le texte d'un épisode compte pour un cinquième ; la voix pour presque tout le reste.</summary>
        public const double PartTexte = 0.2;

        public const double PartVoix = 0.75;

        private static readonly TimeSpan Peremption = TimeSpan.FromHours(3);

        private readonly ConcurrentDictionary<int, Avancee> _cours = new();

        private sealed class Avancee
        {
            public int Total;
            public int Faits;
            public double EnCours;
            public DateTime Maj;
        }

        public void Commencer(int coursId, int total, int faits) =>
            _cours[coursId] = new Avancee { Total = Math.Max(1, total), Faits = Math.Max(0, faits), Maj = DateTime.UtcNow };

        /// <summary>L'épisode en cours a fait cette part de son chemin (0 à 1). Jamais en arrière.</summary>
        public void Avancer(int coursId, double part)
        {
            if (!_cours.TryGetValue(coursId, out var a)) return;
            lock (a)
            {
                a.EnCours = Math.Clamp(Math.Max(a.EnCours, part), 0, 1);
                a.Maj = DateTime.UtcNow;
            }
        }

        /// <summary>Un épisode de plus, réussi ou non : la barre passe au suivant.</summary>
        public void EpisodeFini(int coursId)
        {
            if (!_cours.TryGetValue(coursId, out var a)) return;
            lock (a)
            {
                a.Faits = Math.Min(a.Total, a.Faits + 1);
                a.EnCours = 0;
                a.Maj = DateTime.UtcNow;
            }
        }

        public void Terminer(int coursId) => _cours.TryRemove(coursId, out _);

        /// <summary>Le pourcentage (0 à 99) tant que le cours se fabrique ; null si la tâche de fond ne le tient pas.</summary>
        public int? Pourcent(int coursId)
        {
            if (!_cours.TryGetValue(coursId, out var a)) return null;
            lock (a)
            {
                if (DateTime.UtcNow - a.Maj > Peremption)
                {
                    _cours.TryRemove(coursId, out _);
                    return null;
                }

                // 99 au plus : le 100 appartient à l'état « prêt », lu en base.
                return (int)Math.Clamp(Math.Floor((a.Faits + a.EnCours) / a.Total * 100), 0, 99);
            }
        }
    }
}
