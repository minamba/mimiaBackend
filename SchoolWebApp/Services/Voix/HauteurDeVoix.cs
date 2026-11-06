namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LA HAUTEUR D'UNE VOIX, POUR NE PAS GARDER UNE PRISE QUI CHANGE DE TIMBRE — Camara, le 06/11/2026 : « à chaque
    /// fois, le professeur de français dit "Qu'as-tu entendu ?" avec une voix de femme ».
    ///
    /// MESURÉ : trois prises Gemini de cette phrase, voix d'Adrien (Sadaltager) : 185 Hz, 120 Hz, 104 Hz. Sur une phrase
    /// de trois mots, la voix sort parfois aiguë, comme une voix de femme ; et la première prise propre d'une annonce
    /// fixe est gardée tant que l'API tourne (SyntheseVocaleService.AnnoncesGardees) : tombée aiguë, elle revenait à
    /// chaque tour. Une prise se juge donc aussi à sa hauteur avant d'être gardée.
    ///
    /// LA MESURE : fréquence fondamentale médiane, par autocorrélation, sur des trames de 1 024 échantillons (PCM 16 bits
    /// mono 24 kHz) assez fortes et assez périodiques pour être de la voix. Une voix d'homme parle vers 85-155 Hz, une
    /// voix de femme vers 165-255 Hz.
    /// </summary>
    public static class HauteurDeVoix
    {
        private const int Frequence = 24000;

        /// <summary>Au-dessus, une voix d'homme sonne comme une voix de femme.</summary>
        public const double PlafondVoixDHomme = 160;

        /// <summary>En dessous, une voix de femme sonne comme une voix d'homme.</summary>
        public const double PlancherVoixDeFemme = 140;

        /// <summary>Les professeurs à voix d'homme (voix Gemini de SyntheseGemini).</summary>
        private static readonly HashSet<string> VoixDHomme = new(StringComparer.OrdinalIgnoreCase)
        {
            "adrien", "salim", "yann", "karim", "theo", "minamba", "jonas", "youssef", "hugo",
        };

        public static bool EstUneVoixDHomme(string? avatar) => avatar is not null && VoixDHomme.Contains(avatar);

        /// <summary>
        /// La prise a-t-elle le timbre du professeur ? Vrai quand on ne peut pas mesurer (trop courte, pas de voix) :
        /// dans le doute, on ne jette rien.
        /// </summary>
        public static bool ConvientA(string? avatar, byte[] pcm)
        {
            if (avatar is null) return true;
            var mediane = Mediane(pcm);
            if (mediane is null) return true;

            return EstUneVoixDHomme(avatar) ? mediane <= PlafondVoixDHomme : mediane >= PlancherVoixDeFemme;
        }

        /// <summary>La fréquence fondamentale médiane, en hertz, ou null s'il y a trop peu de voix pour mesurer.</summary>
        public static double? Mediane(byte[] pcm)
        {
            const int taille = 1024;
            const int pas = 480;
            var n = pcm.Length / 2;
            if (n < taille * 2) return null;

            var s = new float[n];
            for (var i = 0; i < n; i++) s[i] = BitConverter.ToInt16(pcm, i * 2) / 32768f;

            var lagMin = Frequence / 400;
            var lagMax = Frequence / 70;
            var hauteurs = new List<double>();

            for (var d = 0; d + taille < n; d += pas)
            {
                double energie = 0;
                for (var i = 0; i < taille; i++) energie += s[d + i] * s[d + i];
                if (energie / taille < 0.0008) continue;

                double meilleure = 0;
                var lagMeilleur = 0;
                for (var lag = lagMin; lag <= lagMax; lag++)
                {
                    double c = 0;
                    for (var i = 0; i < taille - lag; i++) c += s[d + i] * s[d + i + lag];
                    if (c > meilleure) { meilleure = c; lagMeilleur = lag; }
                }

                if (lagMeilleur > 0 && meilleure / energie > 0.45) hauteurs.Add((double)Frequence / lagMeilleur);
            }

            if (hauteurs.Count < 5) return null;
            hauteurs.Sort();
            return hauteurs[hauteurs.Count / 2];
        }
    }
}
