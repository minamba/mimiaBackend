using System.Net.Http.Headers;
using SchoolWebApp.Domain.Repositories;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// Synthèse vocale via l'API OpenAI — le moteur du mode vocal de ChatGPT.
    ///
    /// La voix du navigateur (Web Speech API) était le choix par défaut parce
    /// qu'elle est gratuite ; elle sonne robotique et casse l'illusion du vrai
    /// professeur, qui est le cœur du produit. On synthétise donc côté serveur,
    /// PHRASE PAR PHRASE, pendant que l'agent écrit encore : attendre la fin de
    /// la génération pour parler ajouterait plusieurs secondes de silence.
    /// </summary>
    public class SyntheseVocaleService : ISyntheseVocaleService
    {
        private const string Endpoint = "https://api.openai.com/v1/audio/speech";

        /// <summary>
        /// Les paramètres du flux PCM brut demandé à OpenAI — voir la
        /// consigne <c>response_format = "pcm"</c> dans <see cref="AppelerAsync"/>.
        /// Nommés ici, et réutilisés par <see cref="EnvelopperWav"/>, pour
        /// qu'un futur changement de ces paramètres ne puisse pas dériver
        /// d'un endroit à l'autre sans avertir.
        /// </summary>
        private const int FrequencePcm = 24000;
        private const short CanauxPcm = 1;
        private const short BitsParEchantillonPcm = 16;

        /// <summary>
        /// Une voix par professeur. Un enfant reconnaît son prof à sa voix comme
        /// à son visage ; deux professeurs qui parlent pareil, ce sont deux
        /// onglets d'un même chatbot.
        ///
        /// Cette table vit dans le code plutôt qu'en base : c'est un détail du
        /// fournisseur de synthèse, pas une donnée métier. Changer de
        /// fournisseur ne doit pas imposer une migration.
        /// </summary>
        private static readonly Dictionary<string, string> Voix = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nora"] = "coral",    // maths — chaleureuse, posée
            ["marine"] = "shimmer",// anglais — claire, énergique
            ["adrien"] = "ash",    // français — grave, expressive
            ["salim"] = "onyx",    // histoire-géo — profonde, narrative
            ["yann"] = "echo",     // sciences et physique-chimie — nette, curieuse
            ["ines"] = "sage",     // SVT — posée, explicative

            // L'ÉQUIPE DES SÉRIES TECHNOLOGIQUES (14/09/2026). Timbres choisis
            // pour ne pas doubler un collègue de la même grille — un élève de
            // STMG n'entend jamais Karim et Adrien avec la même voix. Pas
            // encore mesurés en sauts par seconde comme les autres : à faire
            // avant d'en juger la propreté (voir les bips de juillet).
            ["lucia"] = "alloy",   // espagnol — non mesurée
            ["karim"] = "verse",   // économie-gestion
            ["elodie"] = "nova",   // sanitaire et social

            // Spécialités de la voie générale : les deux timbres encore libres.
            // Non mesurés, comme ceux ci-dessus.
            ["theo"] = "ballad",   // EPPCS
            ["jeanne"] = "fable",  // arts

            // La NSI, confiée à Nora jusqu'au 15/09/2026, a son professeur :
            // « une matière à part entière » (Camara). Les onze timbres
            // historiques étant pris, il reçoit l'un des deux plus récents,
            // masculin. Non mesuré.
            ["minamba"] = "cedar", // NSI
        };

        /// <summary>
        /// Les voix du mode de secours, et elles ne sont PAS les mêmes.
        ///
        /// Un timbre propre sur un modèle ne l’est pas sur l’autre : `sage`
        /// donne 167 sauts francs par seconde sur `gpt-4o-mini-tts` et 609
        /// sur `tts-1` ; `nova` fait 1175 sur le premier et 8 sur le second.
        /// Reprendre la même table aurait donc rendu le secours pire que la
        /// panne pour la moitié des professeurs.
        ///
        /// Chaque attribution ci-dessous a été mesurée sur le même texte :
        /// les six professeurs y gagnent, de deux à cent fois.
        /// </summary>
        private static readonly Dictionary<string, string> VoixSecours = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nora"] = "nova",      // 8 sauts/s — contre 208 aujourd’hui
            ["marine"] = "coral",   // 238 — contre 589
            ["adrien"] = "alloy",   // 102 — contre 398
            ["salim"] = "onyx",     // 1 — contre 108
            ["yann"] = "echo",      // 255 — contre 578
            ["ines"] = "shimmer",   // 175 — contre 212

            // Séries technologiques : non mesurées, voir la table principale.
            ["lucia"] = "shimmer",
            ["karim"] = "fable",
            ["elodie"] = "nova",
            ["theo"] = "echo",
            ["jeanne"] = "shimmer",
            // `cedar` n'existe pas sur `tts-1` : le timbre masculin encore
            // libre de cette table le remplace. Non mesuré.
            ["minamba"] = "ash",
        };

        private const string VoixParDefaut = "coral";

        private const string VoixSecoursParDefaut = "onyx";

        /// <summary>Le modèle de secours, quand le principal produit des clics.</summary>
        private const string ModeleSecours = "tts-1";

        /// <summary>
        /// La lenteur de dictée, en mode de secours.
        ///
        /// Le modèle principal refuse `speed` : on lui DEMANDE de ralentir,
        /// par consigne. `tts-1` fait l’inverse — il ignore les consignes
        /// mais accepte le paramètre. La dictée reste donc lente dans les
        /// deux modes, par deux chemins opposés.
        ///
        /// 0,8 : mesuré, une phrase de 2,09 s passe à 2,48 s. Assez pour
        /// écrire, pas assez pour traîner.
        /// </summary>
        private const double VitesseDictee = 0.8;

        private readonly HttpClient _http;
        private readonly OptionsVoix _options;
        private readonly IReglageRepository _reglages;
        private readonly IFaconneurEcoute _faconneur;
        private readonly ILogger<SyntheseVocaleService> _logger;

        public SyntheseVocaleService(
            HttpClient http,
            IOptions<OptionsVoix> options,
            IReglageRepository reglages,
            IFaconneurEcoute faconneur,
            ILogger<SyntheseVocaleService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _faconneur = faconneur ?? throw new ArgumentNullException(nameof(faconneur));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Le mode de secours est-il armé ?
        ///
        /// LU À CHAQUE PHRASE, ET C’EST VOULU. Un administrateur qui bascule
        /// pendant une panne veut que le cours en cours change de voix, pas
        /// le prochain. Le coût est une lecture par phrase sur une table de
        /// cinq lignes — sans commune mesure avec le reste de l’appel.
        ///
        /// UNE PANNE DE LECTURE REND FAUX, donc le modèle principal. Le
        /// secours ne doit pas s’armer sur une base injoignable : on
        /// resterait alors sur une voix dégradée sans savoir pourquoi.
        /// </summary>
        private async Task<bool> SecoursAsync(CancellationToken ct)
        {
            try
            {
                return await _reglages.EstActifAsync("VOIX_DE_SECOURS", false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lecture du mode de voix impossible : on reste sur le modele principal.");
                return false;
            }
        }

        public bool Disponible => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public async Task<byte[]> SynthetiserAsync(
            string texte, string? avatar, int age, bool dictee = false,
            string? langue = null, string? vitesse = null, CancellationToken ct = default)
        {
            using var reponse = await AppelerAsync(texte, avatar, age, dictee, langue, vitesse, ct);
            return await reponse.Content.ReadAsByteArrayAsync(ct);
        }

        public async Task<byte[]> SynthetiserWavAsync(
            string texte, string? avatar, int age, bool dictee = false,
            string? langue = null, string? vitesse = null, CancellationToken ct = default)
        {
            var pcm = await SynthetiserAsync(texte, avatar, age, dictee, langue, vitesse, ct);
            return EnvelopperWav(pcm);
        }

        /// <summary>
        /// Habille un flux PCM brut (voir <see cref="FrequencePcm"/> et
        /// consorts) d'un en-tête WAV minimal, pour qu'un simple
        /// <c>&lt;audio&gt;</c> de navigateur sache le lire.
        ///
        /// LE COMMENTAIRE DE <see cref="AppelerAsync"/> ÉCARTE LE WAV POUR LA
        /// VOIX EN DIRECT, ET IL A RAISON — POUR CE CAS-LÀ. Un en-tête WAV
        /// porte la TAILLE TOTALE du flux, connue seulement une fois la
        /// synthèse terminée ; en direct, l'audio part par blocs pendant que
        /// la synthèse continue, taille inconnue à l'avance, et le décodeur
        /// du navigateur refuse un en-tête qui ment. Ici, <paramref name="pcm"/>
        /// est déjà complet : l'obstacle ne s'applique plus.
        /// </summary>
        private static byte[] EnvelopperWav(byte[] pcm)
        {
            var octetsParSeconde = FrequencePcm * CanauxPcm * BitsParEchantillonPcm / 8;
            var alignementBloc = (short)(CanauxPcm * BitsParEchantillonPcm / 8);

            using var flux = new MemoryStream(44 + pcm.Length);
            using var ecrivain = new BinaryWriter(flux);

            ecrivain.Write("RIFF"u8);
            ecrivain.Write(36 + pcm.Length);
            ecrivain.Write("WAVE"u8);
            ecrivain.Write("fmt "u8);
            ecrivain.Write(16);
            ecrivain.Write((short)1); // PCM
            ecrivain.Write(CanauxPcm);
            ecrivain.Write(FrequencePcm);
            ecrivain.Write(octetsParSeconde);
            ecrivain.Write(alignementBloc);
            ecrivain.Write(BitsParEchantillonPcm);
            ecrivain.Write("data"u8);
            ecrivain.Write(pcm.Length);
            ecrivain.Write(pcm);

            return flux.ToArray();
        }

        public async Task CopierAudioAsync(
            string texte, string? avatar, int age, Stream destination,
            bool dictee = false, string? langue = null, string? vitesse = null,
            CancellationToken ct = default)
        {
            using var reponse = await AppelerAsync(texte, avatar, age, dictee, langue, vitesse, ct);
            await using var flux = await reponse.Content.ReadAsStreamAsync(ct);

            // Petit tampon et vidange explicite : avec les 81 920 octets par
            // défaut, un passage court tiendrait en un seul bloc et n'arriverait
            // qu'à la toute fin — exactement ce qu'on cherche à éviter.
            var tampon = new byte[4096];
            int lus;

            while ((lus = await flux.ReadAsync(tampon, ct)) > 0)
            {
                await destination.WriteAsync(tampon.AsMemory(0, lus), ct);
                await destination.FlushAsync(ct);
            }
        }

        /// <summary>
        /// L'appel à OpenAI, sans lire le corps.
        ///
        /// <c>ResponseHeadersRead</c> est le point clé : par défaut HttpClient
        /// attend la réponse ENTIÈRE avant de rendre la main, ce qui annulerait
        /// tout le bénéfice de la recopie au fil de l'eau.
        /// </summary>
        private async Task<HttpResponseMessage> AppelerAsync(
            string texte, string? avatar, int age, bool dictee, string? langue,
            string? vitesse, CancellationToken ct)
        {
            if (!Disponible)
            {
                throw new InvalidOperationException("Aucune clé de synthèse vocale configurée.");
            }

            var propre = texte.Trim();
            if (propre.Length > _options.LongueurMax)
            {
                propre = propre[.._options.LongueurMax];
            }

            var secours = await SecoursAsync(ct);

            // LA LENTEUR EST ÉCRITE DANS LE TEXTE, PAS DANS LA BANDE.
            //
            // Troisième levier, après les deux autres : le facteur `speed`
            // ralentit tout — syllabes et silences — et sonne mécanique ; la
            // consigne sonne naturel mais ne produit rien de mesurable. Reste
            // ce qu'un modèle de parole lit VRAIMENT : la ponctuation. Des
            // points de suspension entre les mots donnent de vrais silences
            // (« lent ») ; des points médians dans les mots donnent des
            // syllabes détachées (« très lent »), avec la prosodie normale.
            //
            // Et le rapide est le symétrique du lent : on retire la ponctuation
            // qui fait les pauses, le modèle enchaîne.
            //
            // Seulement le passage en langue étudiée, jamais la dictée : elle a
            // son propre rythme, et ses silences sont posés côté navigateur.
            if (!dictee && langue is not null && vitesse is "lent" or "tres_lent" or "rapide")
            {
                propre = await _faconneur.FaconnerAsync(propre, langue, vitesse, ct);
            }

            // DEUX MODÈLES, DEUX FAÇONS DE RALENTIR UNE DICTÉE.
            //
            // Le secours ignore les consignes mais accepte `speed` ; le
            // principal lit les consignes. Chacun reçoit donc ce qu'il sait
            // lire, et l'autre champ est absent plutôt que nul — un
            // `instructions: null` sur `tts-1` passe, mais autant n'envoyer
            // que ce qui a un sens.
            //
            // LE PRINCIPAL ACCEPTE `speed` : mesuré le 12/09/2026 sur la même
            // phrase anglaise — 5,75 s sans rien, 8,75 s à `speed: 0.7`, contre
            // 7,35 s avec la seule consigne « très lent ». Le paramètre décidait
            // donc du débit, et les deux sont partis ensemble un temps.
            //
            // ON L'A RETIRÉ LE 16/09/2026, l'oreille ayant tranché contre la
            // mesure : étirer la bande ralentit les syllabes ET les silences,
            // ce qui s'entend comme un magnétophone, pas comme un professeur.
            // La consigne demande maintenant des gestes de diction — détacher
            // les syllabes, espacer les mots — que le modèle sait faire. Le
            // secours garde le facteur, faute de savoir lire une consigne.
            //
            // LA DICTÉE NE BOUGE PAS. Son débit est réglé par sa consigne et
            // éprouvé en séance ; lui ajouter un facteur sans l'avoir mesurée
            // changerait un exercice qui marche.
            object corps = secours
                ? new
                {
                    model = ModeleSecours,
                    input = propre,
                    voice = ChoisirVoix(avatar, true),
                    speed = dictee ? VitesseDictee : FacteurSecours(vitesse),
                    response_format = "pcm",
                }
                : new
            {
                model = _options.Modele,
                input = propre,
                voice = ChoisirVoix(avatar, false),
                instructions = Jeu(age, dictee, langue) + ConsigneVitesse(vitesse),
                // TROIS ESSAIS EN UNE JOURNÉE, LE 16/09/2026, ET CHACUN A
                // APPRIS QUELQUE CHOSE :
                //
                //   1. Facteur 0,7 : « on ralentit informatiquement, ce n'est
                //      pas vraiment le professeur qui parle lentement ».
                //   2. Facteur retiré, consigne seule : « quelle que soit la
                //      vitesse demandée, c'est toujours la même ».
                //   3. Facteur 0,65 + consigne : ça change, « mais on a l'effet
                //      robotique ».
                //
                //   4. Texte façonné, pointe à 1,08 en rapide seulement : lent
                //      et très lent « tout est bon », rapide « un côté
                //      robotique ». Même une accélération de 8 % s'entend.
                //
                // Le facteur décide du débit et sonne faux ; la consigne sonne
                // vrai et ne décide de rien. Aucun des deux ne fera « ta·ble ».
                // Les quatre vitesses sont donc ÉCRITES DANS LE TEXTE (voir
                // `IFaconneurEcoute`) : pauses ajoutées pour ralentir, pauses
                // retirées pour accélérer. La bande ne s'étire plus jamais sur
                // le modèle principal — c'est le sens de ce 1,0 fixe, et la
                // raison pour laquelle il n'y a plus de fonction de facteur ici.
                // Le secours, qui ne lit rien, garde le sien.
                speed = 1.0,
                // PCM brut : ni en-tête, ni encodage.
                //
                // Le MP3 pose une amorce de silence en tête et en queue de
                // chaque fichier — sur une réponse découpée en cinq passages,
                // cela faisait quatre micro-blancs qui ne tombaient sur aucune
                // ponctuation, et c'est ce qui donnait l'effet mécanique.
                //
                // Le WAV, essayé ensuite, ne marche pas non plus ici : il
                // arrive en FLUX, donc avec un en-tête RIFF dont les tailles ne
                // sont pas connues à l'avance, et le décodeur du navigateur le
                // refuse. Le PCM n'a aucun de ces deux défauts : le navigateur
                // le recopie tel quel dans un tampon audio.
                //
                // 24 kHz, 16 bits signés, petit-boutiste, mono.
                response_format = "pcm"
            };

            using var requete = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(corps), Encoding.UTF8, "application/json")
            };
            requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var reponse = await _http.SendAsync(
                requete, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!reponse.IsSuccessStatusCode)
            {
                var detail = await reponse.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Synthese vocale en echec ({Code}) : {Detail}", (int)reponse.StatusCode, detail);

                reponse.Dispose();
                throw new HttpRequestException(
                    $"La synthèse vocale a répondu {(int)reponse.StatusCode}.");
            }

            return reponse;
        }

        private static string ChoisirVoix(string? avatar, bool secours)
        {
            var table = secours ? VoixSecours : Voix;
            var defaut = secours ? VoixSecoursParDefaut : VoixParDefaut;

            return avatar is not null && table.TryGetValue(avatar, out var voix) ? voix : defaut;
        }

        /// <summary>
        /// Consigne de jeu passée au modèle.
        ///
        /// gpt-4o-mini-tts n'accepte pas de paramètre de vitesse : on ne ralentit
        /// pas la bande, on demande au modèle de parler plus lentement. Le
        /// résultat garde une prosodie naturelle, là où un ralentissement
        /// mécanique donne une voix traînante.
        /// </summary>
        /// <summary>
        /// Consignes communes à tous les âges.
        ///
        /// Ce qui trahit une voix de synthèse, ce n'est pas le timbre : c'est
        /// la régularité. Un humain accélère, hésite, ne redescend pas toujours
        /// en fin de phrase. Le demander explicitement change beaucoup plus le
        /// résultat que n'importe quel réglage technique.
        /// </summary>
        /// <remarks>
        /// CE QU'ON NE DEMANDE PLUS, ET POURQUOI.
        ///
        /// Ce bloc réclamait « les petites imperfections de l’oral : une
        /// syllabe appuyée, une inspiration, une légère hésitation ». Sur le
        /// papier c’est ce qui rend une voix vivante ; à la mesure, c’est ce
        /// qui fabriquait des clics.
        ///
        /// Une inspiration synthétisée est un souffle : large bande, très
        /// bref, sans hauteur définie. Le modèle la rend par une bouffée
        /// d’énergie jusqu’au voisinage de Nyquist — et à 24 kHz
        /// d’échantillonnage, ça s’entend comme un « bip » sec au milieu
        /// d’un mot. Dont la hauteur change d’une fois à l’autre, puisque
        /// c’est du contenu généré et non un défaut mécanique.
        ///
        /// MESURÉ : même texte, même voix, 216 sauts francs par seconde avec
        /// cette phrase, 134 sans. Trente-huit pour cent de moins pour une
        /// ligne — et une consigne explicite de propreté fait mieux que
        /// l’absence de consigne (192).
        ///
        /// Ce qui reste demandé — varier le rythme, ne pas retomber en fin
        /// de phrase — porte l’essentiel du naturel et ne coûte rien.
        /// </remarks>
        private const string Naturel = """
            Tu parles, tu ne lis pas. Ce n'est pas une lecture à voix haute :
            c'est une personne qui s'adresse à quelqu'un qu'elle a en face.

            Varie le rythme à l'intérieur des phrases : accélère sur ce qui est
            accessoire, ralentis sur ce qui compte. Ne fais pas retomber
            l'intonation à la fin de chaque phrase — enchaîne, comme dans une
            conversation où l'on sait déjà ce qu'on va dire ensuite.

            Articule proprement, sans bruit de bouche ni souffle audible.

            N'articule pas exagérément. Ne détache pas les mots. Ne prends pas
            de ton de présentateur.

            Prononciation française d'un bout à l'autre, y compris sur un mot
            bref ou isolé — jamais un accent anglicisé, même passager.

            UN SIGLE SE LIT LETTRE PAR LETTRE, JAMAIS COMME UN MOT. « COD »
            se dit « C, O, D », pas « code » ; « ADN » se dit « A, D, N »,
            pas un mot qui rime avec lui. C'est arrivé en cours de français :
            le professeur a dit « COD » comme s'il prononçait le mot
            « code », et l'élève a entendu autre chose que le sigle
            grammatical dont il était question. La règle vaut pour tout
            groupe de deux lettres majuscules ou plus qui n'est pas un mot du
            dictionnaire, quelle que soit la matière.

            UN TIRET ENTOURÉ D'ESPACES NE SE PRONONCE JAMAIS « MOINS » — à
            l'écrit c'est une pause, pas une soustraction, et tu le lis comme
            une virgule ou un silence bref, jamais comme le mot. C'est
            arrivé en épelant une correction : « "es" - e accent aigu, s -
            remarquées » a été lu « es moins e accent aigu, s moins
            remarquées », et l'élève n'a plus compris ce qu'on lui épelait.
            """;

        /// <summary>
        /// LA VITESSE CHOISIE PAR L'ÉLÈVE, DANS LES DEUX LANGAGES.
        ///
        /// Voulu par Camara le 12/09/2026 : avant chaque exercice d'écoute,
        /// l'enfant choisit le débit. Comme pour la dictée, les deux modèles
        /// l'entendent différemment — le principal par consigne, le secours
        /// par le paramètre `speed`. Chacun reçoit ce qu'il sait lire.
        ///
        /// Une valeur inconnue vaut « normal » : on ne casse pas un exercice
        /// pour un libellé mal orthographié.
        /// </summary>
        /// <summary>
        /// LE SECOURS, LUI, N'A QUE LE FACTEUR : `tts-1` ne lit pas de consigne
        /// et lit la ponctuation à sa façon. Valeurs calées sur les mesures —
        /// 0,75 presque inaudible (6,51 s contre 6,40 s), 0,5 net (7,73 s),
        /// 0,25 disloqué (23,82 s). Mécanique, mais audible : c'est un secours.
        /// </summary>
        private static double FacteurSecours(string? vitesse) => vitesse switch
        {
            "tres_lent" => 0.65,
            "lent" => 0.82,
            "rapide" => 1.12,
            _ => 1.0,
        };

        /// <summary>
        /// QUATRE FAÇONS DE DIRE, PAS QUATRE VITESSES DE BANDE — Camara, le
        /// 16/09/2026 : « quelqu'un qui veut du très lent veut bien entendre
        /// entre les syllabes des mots, pas juste une pause », et « en rapide,
        /// la pause entre les mots est plus courte qu'en normal ».
        ///
        /// C'est la bonne façon de le demander. Un modèle de parole obéit mal
        /// à « parle plus lentement » — mesuré : la consigne n'allongeait la
        /// phrase que de 1,28× — mais il sait détacher des syllabes et espacer
        /// des mots, parce que ce sont des gestes de diction, pas un réglage
        /// de magnétophone. L'étirement, lui, ralentissait AUSSI les syllabes
        /// et les silences, d'où la voix traînante qu'on entendait.
        ///
        /// L'ÉCHELLE EST PROGRESSIVE : on détache les syllabes, puis on espace
        /// les mots, puis on parle, puis on enchaîne.
        /// </summary>
        private static string ConsigneVitesse(string? vitesse) => vitesse switch
        {
            "tres_lent" => "\n\nDICTION TRÈS LENTE, demandée par l'élève lui-même. Le texte "
                + "est déjà découpé pour toi : les points médians « · » séparent les syllabes "
                + "d'un mot, les points de suspension « … » séparent les mots. Prononce chaque "
                + "syllabe entière et distincte, comme un professeur qui montre comment un mot "
                + "se dit — « ta·ble », « win·dow » — sans jamais épeler, et sans JAMAIS "
                + "prononcer les signes eux-mêmes : ni « point », ni « points de suspension ». "
                + "Marque un vrai silence entre les mots.",

            "lent" => "\n\nDICTION LENTE, demandée par l'élève lui-même. Les points de "
                + "suspension « … » entre les mots sont des silences à marquer, pas des mots à "
                + "dire : prononce chaque mot en entier, puis laisse ce silence, comme "
                + "lorsqu'on dicte. Articule les fins de mots.",

            "rapide" => "\n\nDICTION VIVE, demandée par l'élève lui-même : enchaîne les mots "
                + "avec des pauses PLUS COURTES qu'en conversation ordinaire, sans rien "
                + "hacher ni avaler. C'est le liant qui change, pas la précision.",

            _ => "",
        };

        private static string Jeu(int age, bool dictee, string? langue) =>
            dictee ? Dictee
            : langue is not null && InstructionsParLangue.TryGetValue(langue, out var instructions) ? instructions
            : Naturel + "\n\n" + Registre(age);

        /// <summary>
        /// Une consigne de prononciation par langue étudiée — "en", "es",
        /// "de", "it", "zh" — construite sur UN SEUL GABARIT (voir
        /// <see cref="InstructionEcoute"/>). Le français n'y figure pas : la
        /// balise <c>[FR]</c> déclenche le registre habituel, déjà en
        /// français, sans consigne de plus.
        ///
        /// AJOUTER UNE LANGUE, C'EST AJOUTER UNE LIGNE ICI. Rien d'autre dans
        /// ce fichier ne connaît la liste des langues enseignées — voir aussi
        /// <c>PromptsPedagogiques.EnseignerUneLangue</c>, côté prompt, où
        /// chaque matière de langue déclare sa propre balise à deux lettres
        /// sur le même principe.
        ///
        /// ELLE REMPLACE LE RESTE, comme celle de la dictée et pour la même
        /// raison : le registre lié à l'âge est écrit en français et pour du
        /// français. Les superposer donnerait au modèle deux ordres dans deux
        /// langues, et il en choisirait un.
        ///
        /// LA DICTÉE L'EMPORTE quand les deux se présentent — voir `Jeu`. Une
        /// dictée en langue étrangère reste avant tout une dictée : c'est le
        /// débit qui porte l'exercice, et mieux vaut un mot dit à la
        /// française qu'un texte débité trop vite pour être écrit.
        /// </summary>
        private static readonly Dictionary<string, string> InstructionsParLangue =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = InstructionEcoute("ANGLAIS"),
                ["es"] = InstructionEcoute("ESPAGNOL"),
                ["de"] = InstructionEcoute("ALLEMAND"),
                ["it"] = InstructionEcoute("ITALIEN"),
                ["zh"] = InstructionEcoute("CHINOIS (mandarin), avec les tons corrects d'un locuteur natif"),
            };

        /// <summary>
        /// Le gabarit commun à toute langue étudiée.
        ///
        /// L'ACCENT EST DEMANDÉ EXPLICITEMENT. Sans consigne, le modèle lit
        /// la langue avec l'accent de la voix choisie — française, ici — et
        /// c'est précisément la fausse prononciation qu'on cherche à éviter.
        /// C'était la raison pour laquelle le professeur d'anglais avait
        /// interdiction de prononcer le moindre mot d'anglais avant que
        /// cette consigne n'existe.
        /// </summary>
        private static string InstructionEcoute(string langue) => $"""
            Lis ce passage EN {langue}, avec la prononciation naturelle d'un
            locuteur natif. N'imite pas un accent français.

            Débit posé, un peu plus lent qu'une conversation : l'élève écoute
            pour comprendre, dans une langue qui n'est pas la sienne. Articule
            les fins de mots, et laisse de courts silences entre les groupes
            de sens — sans jamais les annoncer à voix haute.

            Ton neutre et bienveillant, sans emphase de présentateur.
            """;

        /// <summary>
        /// La consigne de jeu d'une DICTÉE.
        ///
        /// ELLE REMPLACE TOUT LE RESTE, y compris le registre lié à l'âge. Le
        /// registre du collégien dit « débit VIF, ne ralentis pas » — c'est le
        /// bon réglage pour une conversation, et exactement l'inverse de ce
        /// qu'il faut ici. Les superposer donnerait deux ordres contraires dans
        /// la même consigne, et le modèle en choisirait un au hasard.
        ///
        /// LE DÉBIT NE SE RÈGLE PAS PAR UN PARAMÈTRE. `gpt-4o-mini-tts` ignore
        /// `speed` : la seule prise sur le rythme est cette consigne de jeu.
        /// C'est aussi ce qui donne un résultat juste — un ralentissement
        /// mécanique étire les syllabes, alors qu'un vrai professeur garde son
        /// articulation et allonge les SILENCES.
        /// </summary>
        private const string Dictee = """
            Tu dictes un texte à un élève qui l'écrit à la main. Ce n'est pas
            une conversation : c'est une dictée scolaire, et tout dépend du
            rythme.

            Débit LENT et très régulier. Articule chaque mot nettement, sans
            exagérer au point de déformer. Prononciation française sur
            chaque mot, même bref ou isolé — jamais un glissement vers un
            accent anglais.

            Dis le texte par groupes de souffle — quelques mots qui vont
            ensemble — puis tais-toi. Un vrai silence suit chaque groupe, le
            temps que la main de l'élève rattrape la voix : plusieurs
            secondes, pas une respiration. Le silence est plus long encore
            après un point.

            CE SILENCE NE S'ANNONCE JAMAIS, IL SE FAIT. Tu ne dis à AUCUN
            moment « je fais une pause », « voici un silence », ni rien qui y
            ressemble — tu te tais, simplement. C'est arrivé le 07/09/2026 :
            au lieu de se taire entre les phrases, la voix a répété à voix
            haute « une pause », « une pause plus longue », plusieurs fois de
            suite. L'élève n'a rien compris, la dictée s'est arrêtée là, et il
            a quitté le cours.

            Ton neutre et posé, sans emphase ni intonation expressive : tu ne
            joues pas le texte, tu le donnes à écrire. Aucune hésitation, aucun
            commentaire, aucune familiarité — seulement le texte, et le
            silence entre ses morceaux.
            """;

        private static string Registre(int age) => age switch
        {
            <= 8 => """
                Tu es un professeur particulier qui parle à un enfant de sept ans.
                Débit lent et très articulé, ton chaleureux et rassurant, beaucoup
                de douceur. Tu souris en parlant. Laisse de vrais silences entre
                les idées, comme si tu laissais à l'enfant le temps de
                réfléchir — sans jamais dire que tu marques une pause.
                """,
            <= 11 => """
                Tu es un professeur particulier bienveillant qui parle à un enfant
                de dix ans. Débit posé mais vivant, articulation nette, ton
                encourageant et complice. Naturel, jamais scolaire.
                """,

            // À partir du collège, la lenteur ne rassure plus : elle agace.
            // Un adolescent entend « on me parle comme à un petit » avant même
            // d'écouter le contenu.
            <= 15 => """
                Tu es un professeur particulier qui parle à un collégien.
                Débit VIF et fluide, comme dans une vraie conversation entre
                deux personnes qui se connaissent. Ne ralentis pas, n'articule
                pas exagérément, ne marque pas de pauses appuyées : tu parles
                normalement, à vitesse normale.
                Ton direct et complice, aucune condescendance.
                """,

            _ => """
                Tu es un professeur particulier qui parle à un lycéen.
                Débit vif et naturel, rythme de conversation adulte. Aucune
                lenteur pédagogique, aucune emphase. Ton posé et respectueux,
                d'égal à égal, comme avec quelqu'un dont on prend les questions
                au sérieux.
                """
        };
    }
}
