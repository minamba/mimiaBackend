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
            ["ines"] = "sage"      // SVT — posée, explicative
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
            ["ines"] = "shimmer"    // 175 — contre 212
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
        private readonly ILogger<SyntheseVocaleService> _logger;

        public SyntheseVocaleService(
            HttpClient http,
            IOptions<OptionsVoix> options,
            IReglageRepository reglages,
            ILogger<SyntheseVocaleService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
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
            string texte, string? avatar, int age, bool dictee = false, CancellationToken ct = default)
        {
            using var reponse = await AppelerAsync(texte, avatar, age, dictee, ct);
            return await reponse.Content.ReadAsByteArrayAsync(ct);
        }

        public async Task CopierAudioAsync(
            string texte, string? avatar, int age, Stream destination,
            bool dictee = false, CancellationToken ct = default)
        {
            using var reponse = await AppelerAsync(texte, avatar, age, dictee, ct);
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
            string texte, string? avatar, int age, bool dictee, CancellationToken ct)
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

            // DEUX MODÈLES, DEUX FAÇONS DE RALENTIR UNE DICTÉE.
            //
            // Le principal refuse `speed` : on le lui demande par consigne.
            // Le secours ignore les consignes mais accepte `speed`. Chacun
            // reçoit donc ce qu’il sait lire, et l’autre champ est absent
            // plutôt que nul — un `instructions: null` sur `tts-1` passe,
            // mais autant n’envoyer que ce qui a un sens.
            object corps = secours
                ? new
                {
                    model = ModeleSecours,
                    input = propre,
                    voice = ChoisirVoix(avatar, true),
                    speed = dictee ? VitesseDictee : 1.0,
                    response_format = "pcm",
                }
                : new
            {
                model = _options.Modele,
                input = propre,
                voice = ChoisirVoix(avatar, false),
                instructions = Jeu(age, dictee),
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
            """;

        private static string Jeu(int age, bool dictee) =>
            dictee ? Dictee : Naturel + "\n\n" + Registre(age);

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
            exagérer au point de déformer.

            Dis le texte par groupes de souffle — quelques mots qui vont
            ensemble — et marque un VRAI silence après chacun, le temps que la
            main de l'élève rattrape la voix. Le silence doit être franc,
            plusieurs secondes, pas une respiration.

            Marque un silence plus long encore à chaque point.

            Ton neutre et posé, sans emphase ni intonation expressive : tu ne
            joues pas le texte, tu le donnes à écrire. Aucune hésitation, aucun
            commentaire, aucune familiarité — seulement le texte.
            """;

        private static string Registre(int age) => age switch
        {
            <= 8 => """
                Tu es un professeur particulier qui parle à un enfant de sept ans.
                Débit lent et très articulé, ton chaleureux et rassurant, beaucoup
                de douceur. Tu souris en parlant. Marque de vraies pauses entre les
                idées, comme si tu laissais à l'enfant le temps de réfléchir.
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
