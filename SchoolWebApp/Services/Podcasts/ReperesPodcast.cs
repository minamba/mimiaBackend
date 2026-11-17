using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Podcasts
{
    /// <summary>
    /// Les repères d'un paragraphe dans le temps de l'épisode : son début, sa fin, et pour chacune de ses phrases le
    /// caractère où elle commence (`Coupes`) et l'instant où la voix la commence (`Temps`).
    /// </summary>
    public sealed record ReperesParagraphe(double Debut, double Fin, IReadOnlyList<int> Coupes, IReadOnlyList<double> Temps);

    /// <summary>
    /// LE TEXTE QUI SUIT LA VOIX — Camara, le 15/11/2026 : « je voudrais que le suivi soit vraiment synchronisé avec la
    /// parole du professeur, comme ça l'élève suit en même temps ».
    ///
    /// CE QU'ON SAIT VRAIMENT : chaque paragraphe est dit à part, puis les morceaux sont collés avec un silence entre
    /// deux. Le début et la fin de chaque paragraphe sont donc EXACTS (la longueur de son audio).
    ///
    /// CE QU'ON RETROUVE : dans un paragraphe, la voix marque une pause entre deux phrases. On prévoit le passage d'une
    /// phrase à la suivante au prorata des caractères (dans le temps de PAROLE, pauses retirées), puis on le CALE sur
    /// une pause de la voix. Sans pause à portée, la prévision reste. Aucun appel payant.
    /// </summary>
    public static class ReperesPodcast
    {
        /// <summary>Dix millisecondes d'audio.</summary>
        private const int Trame = AudioPodcast.Frequence / 100;

        /// <summary>En dessous, la trame est un silence (le souffle d'une voix de synthèse est bien plus bas qu'une syllabe).</summary>
        private const int SeuilSilence = 90;

        /// <summary>Une pause entre deux phrases dure au moins cela.</summary>
        private const double PauseMin = 0.18;

        /// <summary>Deux pauses séparées d'au plus trois trames (un clic) n'en font qu'une.</summary>
        private const int ClicMax = 3;

        /// <summary>Une pause de cette durée ne dit rien : c'est celle d'une virgule.</summary>
        private const double PauseDeVirgule = 0.2;

        /// <summary>La phrase s'éclaire un rien avant la voix : l'œil arrive avant l'oreille.</summary>
        private const double Avance = 0.08;

        // Une fin de phrase (avec son guillemet fermant), des espaces, puis le début d'une autre.
        private static readonly Regex Frontiere = new(
            @"(?<=[.!?…](?:\s?[»”""])?)\s+(?=[\p{Lu}«“""\d—–-])", RegexOptions.Compiled);

        /// <summary>Le caractère où commence chaque phrase du paragraphe (le premier est toujours 0).</summary>
        public static IReadOnlyList<int> Phrases(string paragraphe)
        {
            var coupes = new List<int> { 0 };
            foreach (Match m in Frontiere.Matches(paragraphe ?? string.Empty))
            {
                var debut = m.Index + m.Length;
                if (debut < paragraphe!.Length) coupes.Add(debut);
            }

            return coupes;
        }

        /// <summary>
        /// Les repères d'un paragraphe. `voix` : son audio (PCM 16 bits, 24 kHz) ; `debut` : l'instant où il commence
        /// dans l'épisode.
        /// </summary>
        public static ReperesParagraphe Paragraphe(string texte, ReadOnlySpan<short> voix, double debut)
        {
            var duree = voix.Length / (double)AudioPodcast.Frequence;
            var coupes = Phrases(texte);
            var temps = new double[coupes.Count];
            temps[0] = debut;
            if (coupes.Count == 1) return new ReperesParagraphe(Arrondir(debut), Arrondir(debut + duree), coupes, temps.Select(Arrondir).ToList());

            // 1. Les trames muettes, la voix (du premier au dernier son), et les pauses à l'intérieur.
            var trames = voix.Length / Trame;
            var muette = new bool[trames];
            for (var t = 0; t < trames; t += 1)
            {
                var max = 0;
                foreach (var e in voix.Slice(t * Trame, Trame)) max = Math.Max(max, Math.Abs((int)e));
                muette[t] = max < SeuilSilence;
            }

            var premiere = Array.IndexOf(muette, false);
            var derniere = Array.LastIndexOf(muette, false);
            if (premiere < 0) premiere = 0;
            if (derniere < premiere) derniere = Math.Max(premiere, trames - 1);
            var voixDebut = premiere / 100.0;
            var voixFin = (derniere + 1) / 100.0;

            // LE TEMPS DE PAROLE : `parle[t]` = le nombre de trames où la voix parle avant la trame t. Les pauses n'y
            // comptent pas — c'est là que le débit est régulier (mesuré : au prorata du temps réel, une phrase à deux
            // virgules décalait la suivante de plus d'une seconde).
            var parle = new int[trames + 1];
            for (var t = 0; t < trames; t += 1) parle[t + 1] = parle[t] + (t >= premiere && t <= derniere && !muette[t] ? 1 : 0);
            var total = parle[trames];

            // Les pauses de la voix. Un clic de une à trois trames entre deux vraies pauses ne les sépare pas (mesuré :
            // une pause de 1,1 s comptait pour deux petites, et une virgule voisine l'emportait). Seules deux pauses
            // entières se rejoignent : un court silence après une consonne d'attaque (« P-arce ») n'est pas une pause.
            var silences = new List<(int Debut, int Fin)>();
            var depuis = -1;
            for (var t = premiere; t <= derniere; t += 1)
            {
                if (muette[t] && depuis < 0) depuis = t;
                if (!muette[t] && depuis >= 0)
                {
                    if ((t - depuis) / 100.0 >= PauseMin) silences.Add((depuis, t));
                    depuis = -1;
                }
            }

            var pauses = new List<(int Debut, int Fin)>();
            foreach (var silence in silences)
            {
                if (pauses.Count > 0 && silence.Debut - pauses[^1].Fin <= ClicMax) pauses[^1] = (pauses[^1].Debut, silence.Fin);
                else pauses.Add(silence);
            }

            // 2. Le poids de chaque phrase : ses caractères (une phrase longue se dit plus longtemps).
            var poids = new double[coupes.Count];
            for (var k = 0; k < coupes.Count; k += 1)
            {
                var fin = k + 1 < coupes.Count ? coupes[k + 1] : texte.Length;
                poids[k] = Math.Max(1, fin - coupes[k]);
            }

            // 3. LES PASSAGES D'UNE PHRASE À LA SUIVANTE, CHOISIS ENSEMBLE. Chaque passage prend une pause de la voix, ou
            //    à défaut l'instant prévu au prorata. On cherche l'ensemble qui s'écarte le moins des durées attendues
            //    tout en prenant les pauses les plus longues (une fin de phrase dure plus qu'une virgule).
            //    Choisis un par un, un passage volait la longue pause du suivant (mesuré contre une transcription :
            //    « Prends quelques secondes pour y réfléchir. » s'éclairait trois secondes trop tard).
            var n = coupes.Count;
            var somme = poids.Sum();
            var totalS = total / 100.0;
            var attendu = poids.Select(x => totalS * x / somme).ToArray();

            var candidats = new List<(double Position, double Prime, int Pour, double Instant)>();
            foreach (var (d, f) in pauses)
            {
                candidats.Add((parle[d] / 100.0, ((f - d) / 100.0) - PauseDeVirgule, 0, (f / 100.0) - Avance));
            }

            var cumul = 0.0;
            for (var k = 1; k < n; k += 1)
            {
                // Le passage sans pause : l'instant où la voix a dit sa part.
                cumul += attendu[k - 1];
                var t = Array.BinarySearch(parle, (int)Math.Round(cumul * 100));
                if (t < 0) t = ~t;
                candidats.Add((cumul, 0, k, Math.Min(trames, t) / 100.0));
            }

            candidats.Sort((x, y) => x.Position.CompareTo(y.Position));

            var nc = candidats.Count;
            var cout = new double[n, nc];
            var dou = new int[n, nc];
            for (var k = 0; k < n; k += 1)
            {
                for (var c = 0; c < nc; c += 1) cout[k, c] = double.PositiveInfinity;
            }

            static double Ecart(double duree, double attendue) => (duree - attendue) * (duree - attendue) / Math.Max(0.3, attendue);

            for (var k = 1; k < n; k += 1)
            {
                for (var c = 0; c < nc; c += 1)
                {
                    var (position, prime, pour, _) = candidats[c];
                    if (pour != 0 && pour != k) continue;

                    if (k == 1)
                    {
                        cout[k, c] = Ecart(position, attendu[0]) - prime;
                        continue;
                    }

                    for (var avant = 0; avant < nc; avant += 1)
                    {
                        if (double.IsInfinity(cout[k - 1, avant])) continue;
                        var duree2 = position - candidats[avant].Position;
                        if (duree2 < 0.1) continue;
                        var valeur = cout[k - 1, avant] + Ecart(duree2, attendu[k - 1]) - prime;
                        if (valeur < cout[k, c]) { cout[k, c] = valeur; dou[k, c] = avant; }
                    }
                }
            }

            var fin2 = -1;
            var meilleur = double.PositiveInfinity;
            for (var c = 0; c < nc; c += 1)
            {
                if (double.IsInfinity(cout[n - 1, c])) continue;
                var valeur = cout[n - 1, c] + Ecart(totalS - candidats[c].Position, attendu[n - 1]);
                if (valeur < meilleur) { meilleur = valeur; fin2 = c; }
            }

            var choisis = new int[n];
            for (var k = n - 1; k >= 1 && fin2 >= 0; k -= 1)
            {
                choisis[k] = fin2;
                fin2 = dou[k, fin2];
            }

            var precedent = voixDebut;
            cumul = 0;
            for (var k = 1; k < n; k += 1)
            {
                cumul += attendu[k - 1];
                // Sans solution (des phrases trop courtes pour tenir entre deux pauses), la prévision.
                var instant = double.IsInfinity(meilleur)
                    ? candidats.First(c => c.Pour == k).Instant
                    : candidats[choisis[k]].Instant;
                instant = Math.Clamp(instant, precedent + 0.15, Math.Max(precedent + 0.15, voixFin));
                temps[k] = debut + instant;
                precedent = instant;
            }

            return new ReperesParagraphe(Arrondir(debut), Arrondir(debut + duree), coupes, temps.Select(Arrondir).ToList());
        }

        private static double Arrondir(double secondes) => Math.Round(secondes, 2);
    }
}
