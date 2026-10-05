using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LA REDITE DE FLASH-LITE, COUPÉE AVANT D'ARRIVER DANS LE CASQUE — Camara,
    /// le 04/10/2026 : « que le professeur ne redise pas plusieurs fois ce qu'il
    /// a dit, c'est juste ça qui me dérange ».
    ///
    /// LE DÉFAUT. Flash-Lite relit parfois TOUT son texte une seconde fois dans
    /// la même réponse — l'audio dure exactement le double. Mesuré à 5-6 % sur
    /// un paragraphe ; bien plus en séance réelle (deux phrases sur deux pour
    /// Minamba le 04/10). Une consigne « lis-le une seule fois » n'y change
    /// rien. Le modèle Flash ne le fait pas, mais il coûte 1,5×, et Camara l'a
    /// refusé : « le prix est trop cher ». On garde Lite, et on se protège.
    ///
    /// POURQUOI LE TEXTE ET PAS LE SON. Comparer la fin du son à son début ne
    /// marche pas : deux lectures d'une même phrase se ressemblent autant que
    /// deux phrases différentes. Couper au-delà d'une durée attendue non plus :
    /// la vitesse de lecture varie du simple au double selon le texte. Ce qu'on
    /// SAIT, c'est le texte envoyé.
    ///
    /// COMMENT ÇA TIENT DANS LE TEMPS. Gemini fabrique le son cinq fois plus
    /// vite qu'il ne se joue. Le serveur transmet tout de suite ce qu'une
    /// lecture ne peut pas avoir fini — le texte à vingt-deux caractères par
    /// seconde, la vitesse la plus rapide mesurée — et RETIENT le reste, qui
    /// part en transcription mot par mot avec horodatage (Whisper).
    ///
    /// LA RÈGLE, REFAITE LE 04/10/2026 AU SOIR. La première cherchait les TROIS
    /// PREMIERS MOTS du texte dans la fin retenue. « Salut Mins ! On reprend… » :
    /// il suffit que la transcription écrive « Mince » pour que rien ne soit
    /// reconnu, et la redite passait — or une phrase sur deux commence par un
    /// prénom. On compare désormais TOUT le texte à TOUTE la fin retenue :
    ///
    ///   - une lecture normale laisse en retenue au plus la moitié de ses mots ;
    ///   - une redite y laisse la fin de la première lecture PLUS le texte
    ///     entier une seconde fois.
    ///
    /// Deux conditions, donc, qu'une lecture normale ne peut pas remplir : la
    /// fin retenue compte presque autant de mots que le texte, et on y retrouve
    /// DANS L'ORDRE au moins les trois quarts des mots du texte. Quelques mots
    /// mal transcrits — un prénom, un nombre, un mot anglais — n'y changent
    /// rien.
    ///
    /// OÙ COUPER : la seconde lecture occupe les derniers mots de la retenue,
    /// autant que le texte en compte. On affine sur ses premiers mots, puis on
    /// recule au début du silence qui précède.
    ///
    /// JAMAIS À L'AVEUGLE : transcription en panne ou critères non remplis, la
    /// fin part entière.
    ///
    /// CE QU'ELLE NE COUVRE PAS : les dictées et les écoutes lentes (le son
    /// retenu y est trop long pour arriver à temps) et les textes de moins de
    /// cinq mots.
    /// </summary>
    public sealed partial class GardeRedite
    {
        private const string EndpointTranscription = "https://api.openai.com/v1/audio/transcriptions";

        /// <summary>Whisper, pour ses horodatages par mot : c'est lui qui dit OÙ couper.</summary>
        private const string ModeleTranscription = "whisper-1";

        /// <summary>La lecture la plus rapide mesurée : ce qui est en deçà ne peut pas être une redite.</summary>
        private const double CaracteresParSecondeMaximum = 22d;

        private const int OctetsParSeconde = 24000 * 2;

        /// <summary>En dessous, il n'y a pas assez de mots pour distinguer une redite d'une fin de phrase.</summary>
        private const int MotsMinimum = 5;

        /// <summary>La retenue doit porter presque un texte entier en mots…</summary>
        private const double PartDeMotsRetenus = 0.9;

        /// <summary>… et les trois quarts des mots du texte, dans l'ordre.</summary>
        private const double PartRetrouvee = 0.75;

        private readonly HttpClient _http;
        private readonly string? _cle;
        private readonly ILogger _logger;

        /// <summary>
        /// Le son confié à la transcription, en centièmes de seconde. Zéro :
        /// la garde n'a pas eu à relire (passage non surveillé, ou fin trop
        /// courte). -1 : elle a RENONCÉ — plus le temps, ou transcription en
        /// panne — et la fin est partie sans contrôle.
        /// </summary>
        public int CentiemesRelus { get; private set; }

        /// <summary>Le son retiré parce qu'il redisait le texte, en centièmes de seconde.</summary>
        public int CentiemesRetires { get; private set; }

        public GardeRedite(HttpClient http, string? cleOpenAI, ILogger logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _cle = cleOpenAI;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// La garde a-t-elle un sens pour ce passage ?
        ///
        /// JAMAIS SUR UN PASSAGE D'ÉCOUTE NI SUR UNE DICTÉE — corrigé le
        /// 04/10/2026, après deux coupures en pleine compréhension orale :
        /// « j'ai une coupure, je l'entends plus du tout, et après ça reprend,
        /// ou des fois ça reprend pas ». La garde transmet d'avance ce qu'une
        /// lecture RAPIDE ne peut pas avoir fini, et retient le reste. Un
        /// passage d'écoute est lu lentement, exprès, et il est long : sur les
        /// 470 caractères de l'histoire de Tom, vingt secondes partaient et
        /// trente étaient retenues, le temps de les transcrire. L'élève
        /// entendait le début, puis rien. Seule la langue étudiée
        /// (<paramref name="langue"/> non nul) n'était pas exclue : seuls le
        /// lent et le très lent l'étaient.
        /// </summary>
        public bool Surveille(string texte, bool dictee, string? langue)
        {
            if (string.IsNullOrWhiteSpace(_cle) || dictee || langue is not null) return false;

            return MotsDe(texte).Count >= MotsMinimum;
        }

        /// <summary>Les octets qu'on transmet sans attendre : une lecture n'a pas pu finir avant.</summary>
        public static long OctetsTransmisDAvance(string texte) =>
            (long)(CaracteresPrononces(texte) / CaracteresParSecondeMaximum * OctetsParSeconde) & ~1L;

        /// <summary>
        /// CE QUI SE DIT, PAS CE QUI S'ÉCRIT — corrigé le 04/10/2026, au
        /// contrôle d'une séance de NSI. « regarde `file.popleft()` » : les
        /// accents graves, le point et les parenthèses comptaient dans la
        /// longueur sans jamais se prononcer. La garde croyait la phrase plus
        /// longue qu'à l'oreille, transmettait trop de son d'avance, et sur une
        /// redite le début de la seconde lecture serait parti avant la retenue.
        /// On ne compte que les lettres, les chiffres et les blancs.
        /// </summary>
        internal static int CaracteresPrononces(string texte) =>
            texte.Count(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c));

        /// <summary>
        /// La fin retenue, telle qu'il faut la transmettre : entière, ou coupée
        /// au début de la redite. Ne lève jamais.
        /// </summary>
        /// <param name="delaiMaximum">
        /// LE TEMPS QU'ON PEUT SE PERMETTRE : ce qui reste avant que l'élève
        /// n'arrive au bout du son déjà transmis. Passé ce délai, la fin part
        /// telle quelle — retenir plus longtemps, c'est couper la voix, et une
        /// redite vaut mieux qu'un silence.
        /// </param>
        /// <param name="secondesDejaTransmises">
        /// Le son parti avant la retenue. Sert à situer le MILIEU du son
        /// entier, là où une redite commence — voir <see cref="OuCouper"/>.
        /// </param>
        public async Task<ReadOnlyMemory<byte>> FiltrerAsync(
            string texte, byte[] fin, string? langue, TimeSpan delaiMaximum, CancellationToken ct,
            double secondesDejaTransmises = 0)
        {
            if (fin.Length < OctetsParSeconde) return fin; // moins d'une seconde : rien à relire

            if (delaiMaximum < TimeSpan.FromMilliseconds(500))
            {
                _logger.LogInformation("Garde de redite : plus le temps de relire, la fin part telle quelle.");
                CentiemesRelus = -1;
                return fin;
            }

            using var echeance = CancellationTokenSource.CreateLinkedTokenSource(ct);
            echeance.CancelAfter(delaiMaximum);

            try
            {
                var entendus = await TranscrireAsync(fin, langue, echeance.Token);
                if (entendus is null)
                {
                    CentiemesRelus = -1;
                    return fin;
                }

                CentiemesRelus = (int)(fin.Length * 100L / OctetsParSeconde);
                if (entendus.Count == 0) return fin;

                var attendus = MotsDe(texte);
                var coupe = DebutDeLaRedite(attendus, entendus);

                if (coupe is not int position)
                {
                    _logger.LogInformation(
                        "Garde de redite : rien a couper ({Entendus} mots retenus pour un texte de {Attendus}).",
                        entendus.Count, attendus.Count);
                    return fin;
                }

                var retenues = fin.Length / (double)OctetsParSeconde;
                var coupeSecondes = DebutDuSilenceAvant(
                    fin, OuCouper(entendus[position].Debut, secondesDejaTransmises, retenues));
                var coupeOctets = (long)(coupeSecondes * OctetsParSeconde) & ~1L;

                _logger.LogWarning(
                    "Redite Gemini coupee : le texte ({Attendus} mots) revient entier dans la fin retenue "
                    + "({Entendus} mots) a partir de {Seconde:F1} s ; {Retire:F1} s de son retirees sur {Total:F1}.",
                    attendus.Count, entendus.Count, entendus[position].Debut,
                    (fin.Length - coupeOctets) / (double)OctetsParSeconde, fin.Length / (double)OctetsParSeconde);

                CentiemesRetires = (int)((fin.Length - Math.Max(0, coupeOctets)) * 100L / OctetsParSeconde);

                return new ReadOnlyMemory<byte>(fin, 0, (int)Math.Max(0, coupeOctets));
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // C'est NOTRE échéance, pas l'élève qui a coupé : la
                // transcription a trop tardé. On ne fait pas attendre la voix.
                _logger.LogWarning(
                    "Garde de redite : transcription trop lente ({Delai:F1} s accordees), la fin part telle quelle.",
                    delaiMaximum.TotalSeconds);
                CentiemesRelus = -1;
                return fin;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Une garde qui ne sait pas ne coupe pas.
                _logger.LogWarning(ex, "Garde de redite indisponible : la fin part telle quelle.");
                CentiemesRelus = -1;
                return fin;
            }
        }

        /// <summary>
        /// L'INSTANT OÙ COUPER, DANS LA RETENUE — ajouté le 04/10/2026, au
        /// premier contrôle du journal. Sur « Vas-y, calcule 37 plus 26. »,
        /// 3,6 s fabriquées (le double) : la garde a retiré TOUTE la retenue,
        /// 2,57 s, et n'a laissé que la première seconde. Or ce qui est transmis
        /// d'avance est, par construction, moins qu'une lecture : couper au
        /// tout début de la retenue ampute la fin de la PREMIÈRE lecture.
        ///
        /// Les mots disent QU'IL Y A une redite ; ils situent mal son début sur
        /// une phrase courte, où un seul mot mal transcrit déplace tout. Le son,
        /// lui, donne un repère sûr : une redite dure le double, donc la seconde
        /// lecture commence au MILIEU du son entier. On garde l'instant donné
        /// par les mots s'il tombe près de ce milieu ; sinon, le milieu.
        /// </summary>
        internal static double OuCouper(double selonLesMots, double dejaTransmises, double retenues)
        {
            var moitie = (dejaTransmises + retenues) / 2;
            var milieu = Math.Max(0, moitie - dejaTransmises);

            return Math.Abs(selonLesMots - milieu) <= moitie * 0.25 ? selonLesMots : milieu;
        }

        public sealed record MotTranscrit(string Mot, double Debut);

        /// <summary>
        /// Le rang, dans les mots entendus, où commence la SECONDE lecture du
        /// texte — ou null s'il n'y a pas de redite. Pur, donc vérifiable sans
        /// réseau.
        /// </summary>
        public static int? DebutDeLaRedite(IReadOnlyList<string> attendus, IReadOnlyList<MotTranscrit> entendus)
        {
            if (attendus.Count < MotsMinimum) return null;
            if (entendus.Count < attendus.Count * PartDeMotsRetenus) return null;

            var mots = entendus.Select(m => m.Mot).ToList();
            if (DansLOrdre(attendus, mots) < attendus.Count * PartRetrouvee) return null;

            // La seconde lecture tient dans les derniers mots, autant que le
            // texte en compte ; on cherche son vrai début autour de ce rang.
            //
            // LES PREMIERS MOTS DU TEXTE, RETROUVÉS DANS L'ORDRE — pas à des
            // places fixes : la transcription avale volontiers un prénom, et
            // un décalage d'un mot ferait couper la fin de la PREMIÈRE lecture.
            // À égalité, on préfère un rang qui tombe SUR l'un des deux premiers
            // mots du texte, puis le plus proche de l'estimation.
            var estime = Math.Max(0, mots.Count - attendus.Count);
            var debut = attendus.Take(6).ToList();

            var meilleur = estime;
            var meilleurScore = -1;
            var meilleurSurUnMot = false;

            for (var k = Math.Max(0, estime - 4); k <= Math.Min(mots.Count - 1, estime + 4); k++)
            {
                var score = DansLOrdre(debut, mots.Skip(k).Take(debut.Count + 2).ToList());
                var surUnMot = Proches(attendus[0], mots[k])
                    || (attendus.Count > 1 && Proches(attendus[1], mots[k]));

                var mieux = score > meilleurScore
                    || (score == meilleurScore && surUnMot && !meilleurSurUnMot)
                    || (score == meilleurScore && surUnMot == meilleurSurUnMot
                        && Math.Abs(k - estime) < Math.Abs(meilleur - estime));

                if (mieux)
                {
                    meilleurScore = score;
                    meilleur = k;
                    meilleurSurUnMot = surUnMot;
                }
            }

            return meilleur;
        }

        /// <summary>
        /// Combien de mots de <paramref name="attendus"/> se retrouvent dans
        /// <paramref name="entendus"/>, dans le même ordre — la plus longue
        /// sous-suite commune, à une lettre près par mot.
        /// </summary>
        private static int DansLOrdre(IReadOnlyList<string> attendus, IReadOnlyList<string> entendus)
        {
            var precedente = new int[entendus.Count + 1];
            var courante = new int[entendus.Count + 1];

            for (var i = 1; i <= attendus.Count; i++)
            {
                for (var j = 1; j <= entendus.Count; j++)
                {
                    courante[j] = Proches(attendus[i - 1], entendus[j - 1])
                        ? precedente[j - 1] + 1
                        : Math.Max(precedente[j], courante[j - 1]);
                }

                (precedente, courante) = (courante, precedente);
            }

            return precedente[entendus.Count];
        }

        private async Task<List<MotTranscrit>?> TranscrireAsync(byte[] pcm, string? langue, CancellationToken ct)
        {
            using var corps = new MultipartFormDataContent();
            var fichier = new ByteArrayContent(EnvelopperWav(pcm));
            fichier.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            corps.Add(fichier, "file", "fin.wav");
            corps.Add(new StringContent(ModeleTranscription), "model");
            corps.Add(new StringContent("verbose_json"), "response_format");
            corps.Add(new StringContent("word"), "timestamp_granularities[]");
            corps.Add(new StringContent(string.IsNullOrWhiteSpace(langue) ? "fr" : langue), "language");

            using var requete = new HttpRequestMessage(HttpMethod.Post, EndpointTranscription) { Content = corps };
            requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cle);

            using var reponse = await _http.SendAsync(requete, ct);
            if (!reponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Garde de redite : transcription en echec ({Code}).", (int)reponse.StatusCode);
                return null;
            }

            using var json = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct));
            if (!json.RootElement.TryGetProperty("words", out var mots)) return null;

            return mots.EnumerateArray()
                .Select(m => new MotTranscrit(
                    Normaliser(m.GetProperty("word").GetString()),
                    m.GetProperty("start").GetDouble()))
                .Where(m => m.Mot.Length >= 2)
                .ToList();
        }

        private static bool Proches(string a, string b)
        {
            if (a == b) return true;
            if (Math.Abs(a.Length - b.Length) > 1) return false;
            return Distance(a, b) <= 1;
        }

        private static int Distance(string a, string b)
        {
            var ligne = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) ligne[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                var diagonale = ligne[0];
                ligne[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var dessus = ligne[j];
                    ligne[j] = Math.Min(Math.Min(ligne[j] + 1, ligne[j - 1] + 1), diagonale + (a[i - 1] == b[j - 1] ? 0 : 1));
                    diagonale = dessus;
                }
            }

            return ligne[b.Length];
        }

        /// <summary>
        /// Le début du silence qui précède l'instant donné, pour ne pas couper
        /// au milieu d'un mot ni laisser entendre la respiration d'avant la
        /// redite. À défaut de silence dans la seconde et demie qui précède,
        /// l'instant lui-même.
        /// </summary>
        private static double DebutDuSilenceAvant(byte[] pcm, double secondes)
        {
            const int trame = 24000 / 50; // 20 ms
            const double seuil = 300;     // bien sous la parole, bien au-dessus du souffle numérique

            var trames = pcm.Length / 2 / trame;
            var cible = Math.Min(trames - 1, (int)(secondes * 50));
            var plancher = Math.Max(0, cible - 75); // 1,5 s

            var finSilence = -1;
            for (var t = cible; t >= plancher; t--)
            {
                double energie = 0;
                for (var i = 0; i < trame; i++)
                {
                    var e = BitConverter.ToInt16(pcm, (t * trame + i) * 2);
                    energie += (double)e * e;
                }

                var silencieux = Math.Sqrt(energie / trame) < seuil;

                if (silencieux && finSilence < 0) finSilence = t;
                if (!silencieux && finSilence >= 0) return (t + 1) / 50d;
            }

            return finSilence >= 0 ? plancher / 50d : secondes;
        }

        [GeneratedRegex(@"[^\p{L}\p{N}]+")]
        private static partial Regex PasUneLettre();

        private static string Normaliser(string? mot)
        {
            var sans = new string((mot ?? string.Empty)
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            return PasUneLettre().Replace(sans.ToLowerInvariant(), string.Empty);
        }

        /// <summary>Les mots d'un texte, normalisés comme ceux de la transcription.</summary>
        public static List<string> MotsDe(string texte) =>
            texte.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(Normaliser)
                .Where(m => m.Length >= 2)
                .ToList();

        private static byte[] EnvelopperWav(byte[] pcm)
        {
            using var flux = new MemoryStream(44 + pcm.Length);
            using var ecrivain = new BinaryWriter(flux);
            ecrivain.Write("RIFF"u8); ecrivain.Write(36 + pcm.Length); ecrivain.Write("WAVE"u8);
            ecrivain.Write("fmt "u8); ecrivain.Write(16); ecrivain.Write((short)1); ecrivain.Write((short)1);
            ecrivain.Write(24000); ecrivain.Write(OctetsParSeconde); ecrivain.Write((short)2); ecrivain.Write((short)16);
            ecrivain.Write("data"u8); ecrivain.Write(pcm.Length); ecrivain.Write(pcm);
            return flux.ToArray();
        }
    }
}
