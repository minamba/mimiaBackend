using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Les interrupteurs du produit.
    ///
    /// Deux routes et deux publics : l'administration les change, tout le monde
    /// les lit. La lecture est ANONYME à dessein — la barre de navigation et la
    /// page de connexion en ont besoin avant qu'un compte existe, et c'est
    /// précisément le cas d'usage du mode test.
    ///
    /// Rien ici n'est un secret : savoir que le service est en accès privé
    /// n'ouvre aucune porte, c'est le serveur d'identité qui refuse les
    /// inscriptions.
    /// </summary>
    [ApiController]
    [Route("reglages")]
    public class ReglagesController : ControllerBase
    {
        /// <summary>
        /// Accès privé : ni tarifs, ni inscription, ni connexion Google, ni
        /// réinitialisation de mot de passe.
        /// </summary>
        public const string ModeTest = "MODE_TEST";

        /// <summary>
        /// Le compte de démonstration accepte-t-il les connexions ?
        ///
        /// Éteint, il n'est PAS supprimé : le serveur d'identité refuse la
        /// connexion exactement comme si l'adresse n'existait pas. C'est ce qui
        /// permet de le rallumer pour une démonstration sans recréer ni son
        /// historique, ni ses fiches, ni ses évaluations.
        /// </summary>
        public const string CompteTest = "COMPTE_TEST_ACTIF";

        /// <summary>
        /// L'essai gratuit est-il ouvert aux nouveaux venus ?
        ///
        /// Fermé, le site continue de fonctionner normalement : seule la porte
        /// d'entrée gratuite disparaît. « Commencer gratuitement » devient
        /// « S'inscrire maintenant », et plus aucun chemin n'ouvre d'essai —
        /// ni le bouton d'accueil, ni la page des tarifs.
        ///
        /// Sert à couper la robinet quand l'essai coûte trop cher, ou pendant
        /// une campagne où on ne veut vendre que des abonnements. Un réglage
        /// plutôt qu'un déploiement : la décision se prend en une seconde et
        /// se défait aussi vite.
        /// </summary>
        public const string EssaisOuverts = "ESSAIS_OUVERTS";

        /// <summary>
        /// Les tâches de fond qui appellent le modèle tournent-elles ?
        ///
        /// LE COUPE-CIRCUIT DE LA FACTURE. Décrire une planche et en
        /// cartographier les repères coûtent DEUX lectures d'image chacune, à
        /// 8 000 jetons de plafond. Transcrire un document, observer une séance,
        /// rédiger un bilan s'ajoutent. Ces travaux sont utiles mais jamais
        /// urgents : ils peuvent tous attendre demain.
        ///
        /// EN BASE ET NON EN CONFIGURATION, et c'est tout l'intérêt. Un réglage
        /// de fichier demande un redéploiement — dix minutes pendant lesquelles
        /// la facture continue de courir. Celui-ci s'éteint d'un clic dans
        /// l'administration, et les workers le relisent à chaque passage : au
        /// pire deux minutes avant que ça prenne effet.
        ///
        /// Le dialogue n'est PAS concerné : couper la parole aux professeurs
        /// pour économiser reviendrait à fermer le magasin pour ne plus payer
        /// l'électricité.
        /// </summary>
        public const string TachesDeFond = "TACHES_DE_FOND_ACTIVES";

        /// <summary>
        /// Le site est-il en maintenance ?
        ///
        /// CE QUE ÇA FAIT, ET SURTOUT CE QUE ÇA NE FAIT PAS
        /// -----------------------------------------------
        /// C'est un rideau, pas un verrou. Le site affiche une page d'attente
        /// au lieu de ses écrans ; l'API, elle, continue de répondre
        /// normalement. C'est indispensable : sans ça, l'administrateur qui
        /// vient de tirer le rideau ne pourrait plus rien faire derrière — ni
        /// le lever, ni travailler.
        ///
        /// Ce réglage sert donc à ce qu'un visiteur voie une page soignée
        /// plutôt que des erreurs pendant qu'on travaille. Pour VRAIMENT
        /// fermer le service, on arrête l'API : c'est un autre geste, et il
        /// n'a pas à se confondre avec celui-ci.
        ///
        /// L'administrateur passe au travers du rideau — voir le verrou côté
        /// navigateur. Le lien de connexion reste ouvert sur la page
        /// d'attente, sans quoi un administrateur déconnecté resterait dehors.
        /// </summary>
        public const string Maintenance = "MAINTENANCE_ACTIVE";

        /// <summary>
        /// La voix de secours : `tts-1` au lieu de `gpt-4o-mini-tts`.
        ///
        /// POURQUOI CE MODE EXISTE
        /// -----------------------
        /// Le 03/09/2026, `gpt-4o-mini-tts` s'est mis à produire des clics
        /// audibles au milieu des mots — mesuré à 248 sauts francs par
        /// seconde sur la voix de Nora, contre 8 pour `tts-1` avec nova.
        /// Le défaut vient du modèle, pas du code : le même texte demandé
        /// directement au fournisseur, sans passer par l'application, sort
        /// déjà abîmé.
        ///
        /// Une panne de ce genre ne se corrige pas de notre côté et peut
        /// revenir sans prévenir. Elle ne doit pas coûter une journée de
        /// cours : ce drapeau bascule la synthèse entière en une seconde,
        /// sans redéploiement.
        ///
        /// CE QUE LE SECOURS COÛTE. `tts-1` ignore le paramètre
        /// `instructions` : la prosodie est plus plate et le ton ne
        /// s'adapte plus à l'âge. La lenteur de dictée, elle, est
        /// préservée — `tts-1` accepte `speed`, ce que le modèle principal
        /// refuse.
        /// </summary>
        public const string VoixDeSecours = "VOIX_DE_SECOURS";

        /// <summary>
        /// Le style « Blue Sky » : le fond bleu de marque, les cartes en verre
        /// dépoli et les accents corail, sur TOUT le site.
        ///
        /// VOULU PAR CAMARA LE 15/09/2026, pour faire tester ce style à des
        /// personnes et pouvoir revenir à l'ancien quand il le veut. D'où un
        /// réglage et non un déploiement : le choix se fait et se défait d'un
        /// clic, pour tous les visiteurs à la fois.
        ///
        /// Lu par les DEUX serveurs : l'application (route publique ci-dessous)
        /// et le serveur d'identité, qui habille la page de connexion.
        ///
        /// ÉTEINT PAR DÉFAUT : le style d'origine reste la référence tant que
        /// rien n'a été décidé, et une lecture qui échoue ne change pas l'allure
        /// du site.
        /// </summary>
        public const string BlueSky = "BLUE_SKY";

        /// <summary>
        /// LE MODE DÉVELOPPEUR : le professeur exécute tout, sans discuter.
        ///
        /// VOULU PAR CAMARA LE 16/09/2026, pour ses essais : relancer un
        /// exercice à l'infini sans que le professeur oppose « on n'a pas fini
        /// le précédent ». La demande d'origine était que la phrase « je suis
        /// le développeur » suffise, dans le chat. Refusé, et c'est la seule
        /// chose qui compte ici : une phrase en clair se répète, et le premier
        /// élève qui l'apprend obtient les réponses toutes faites et des
        /// évaluations fausses. Un interrupteur d'administration, lui, est
        /// hors de portée d'un élève.
        ///
        /// ÉTEINT PAR DÉFAUT, et à rallumer sciemment : allumé, il vaut pour
        /// TOUTES les séances en cours, celles des vrais élèves comprises.
        /// C'est un outil d'essai, pas un réglage de confort.
        /// </summary>
        public const string ModeDeveloppeur = "MODE_DEVELOPPEUR";

        /// <summary>
        /// Le bandeau d'information affiché en haut du site.
        ///
        /// DEUX CLÉS ET NON UNE. Le texte survit à son extinction : on
        /// prépare le message de la maintenance de dimanche le vendredi, on
        /// l'allume le jour venu, on l'éteint le soir — sans le réécrire. Une
        /// clé unique où « vide » vaudrait « éteint » obligerait à retaper le
        /// message à chaque fois, et deux versions d'un même avis finiraient
        /// par se contredire.
        ///
        /// CE N'EST PAS LA MAINTENANCE. Le rideau remplace le site ; le
        /// bandeau le surmonte. On annonce avec le bandeau ce que le rideau
        /// fera plus tard — et pendant la panne, un bandeau prévient sans
        /// fermer, ce qui est souvent la bonne mesure.
        /// </summary>
        public const string BandeauMessage = "BANDEAU_MESSAGE";

        /// <summary>Le bandeau est-il affiché ?</summary>
        public const string BandeauActif = "BANDEAU_ACTIF";

        /// <summary>
        /// La longueur au-delà de laquelle un bandeau cesse d'être lu.
        ///
        /// Ce n'est pas une limite technique : la colonne en accepte bien
        /// davantage. C'est une limite d'usage. Un bandeau se lit debout, en
        /// passant, et doit tenir sur deux lignes au téléphone ; au-delà il
        /// devient un paragraphe que personne ne lit, et l'information
        /// urgente se perd dans son propre bavardage.
        /// </summary>
        public const int LongueurBandeauMax = 300;

        /// <summary>
        /// L'offre de lancement : trois heures offertes le premier mois
        /// sur la formule Solo.
        ///
        /// TROIS CLÉS POUR UNE OFFRE. Le drapeau décide, le texte habille,
        /// la date borne. Les séparer permet de préparer l'habillage et
        /// l'échéance à froid, puis d'allumer d'un seul geste — et surtout
        /// d'ÉTEINDRE d'un seul geste, sans avoir à effacer ce qu'on
        /// remettra à la prochaine campagne.
        ///
        /// CE QUI SE PASSE VRAIMENT EST DANS `OffreLancementService`, pas
        /// ici : la page des tarifs et le webhook de paiement doivent lire
        /// la MÊME règle. Un parent qui paie en voyant « 3 h offertes »
        /// doit les recevoir, à la seconde près.
        /// </summary>
        public const string OffreLancement = "OFFRE_LANCEMENT";

        /// <summary>Ce qui s'écrit entre parenthèses à côté de « Solo ».</summary>
        public const string OffreLancementTexte = "OFFRE_LANCEMENT_TEXTE";

        /// <summary>
        /// L'échéance, en ISO 8601 avec son fuseau.
        ///
        /// EN TEXTE ET NON EN DATE, parce que la table des réglages stocke
        /// des chaînes — c'est ce qui lui évite une migration à chaque
        /// nouveau réglage. Le service la relit en `RoundtripKind`, seule
        /// façon de ne pas perdre le fuseau en chemin.
        /// </summary>
        public const string OffreLancementFin = "OFFRE_LANCEMENT_FIN";

        /// <summary>
        /// Le compte à rebours s'affiche-t-il sur la page d'accueil ?
        ///
        /// SÉPARÉ DE L'OFFRE, parce que ce sont deux décisions. L'offre est
        /// un engagement commercial — elle change la carte Solo et crédite
        /// des heures. Le compte à rebours est une VITRINE : il presse le
        /// visiteur, et on peut vouloir de l'un sans l'autre.
        ///
        /// Le cas courant : les premiers jours d'une campagne, l'urgence ne
        /// veut rien dire — il reste trois semaines. On allume l'offre
        /// tout de suite et le décompte la dernière semaine, quand il a
        /// enfin quelque chose à dire.
        ///
        /// ALLUMÉ PAR DÉFAUT : c'était le comportement avant que ce réglage
        /// existe, et un réglage neuf ne doit rien éteindre au premier
        /// démarrage qui suit son déploiement.
        /// </summary>
        public const string OffreLancementBandeau = "OFFRE_LANCEMENT_BANDEAU";

        /// <summary>
        /// Combien de MINUTES sont offertes.
        ///
        /// UN NOMBRE, ET SURTOUT PAS UN CODE DE PACK.
        /// -----------------------------------------
        /// La première version pointait un pack du catalogue des heures
        /// supplémentaires, pour pouvoir annoncer « valeur 14,90 € ». Deux
        /// défauts, et le second est rédhibitoire :
        ///
        ///   1. Ce prix n'est affiché NULLE PART sur le site. L'argument ne
        ///      tenait que dans l'écran de réglage.
        ///
        ///   2. Le catalogue sert à VENDRE : ses packs sont poussés chez
        ///      Stripe. Offrir cinq heures aurait donc exigé de créer un
        ///      produit Stripe à cinq heures — un objet marchand pour un
        ///      cadeau qui ne se vend pas — et l'offre serait tombée le jour
        ///      où ce pack aurait été désactivé.
        ///
        /// Un cadeau est un nombre de minutes. Rien de plus.
        ///
        /// EN MINUTES ET NON EN HEURES : la table des consommations, les
        /// forfaits et les recharges comptent tous en minutes. Introduire une
        /// seconde unité ici obligerait à convertir quelque part, et c'est
        /// toujours là que les arrondis se perdent.
        /// </summary>
        public const string OffreLancementMinutes = "OFFRE_LANCEMENT_MINUTES";

        /// <summary>
        /// Les formules concernées, séparées par des virgules : « SOLO »,
        /// « SOLO,DUO »…
        ///
        /// PLUSIEURS ET NON UNE SEULE, parce qu'une campagne porte souvent
        /// sur plus d'une formule — et qu'un réglage à choix unique aurait
        /// obligé à en refaire un le jour où on veut les deux.
        ///
        /// DES CODES ET NON DES IDENTIFIANTS. Un identifiant numérique
        /// changerait d'une base à l'autre : le réglage exporté depuis le
        /// développement désignerait une autre formule en production. Le
        /// code, lui, est le même partout — c'est déjà lui qui voyage dans
        /// les étiquettes Stripe.
        /// </summary>
        public const string OffreLancementFormules = "OFFRE_LANCEMENT_FORMULES";

        /// <summary>
        /// LE COUPE-CIRCUIT DU TEMPS RÉEL — voulu par Camara le 16/09/2026,
        /// avant le lancement : « si le site explose, je ne veux pas que cette
        /// histoire de SSE ait un impact sur les performances ».
        ///
        /// Éteint, plus aucun navigateur n'ouvre de connexion longue et les
        /// écoutes en cours sont fermées immédiatement. Le produit ne perd
        /// rien d'essentiel : les réglages se propagent alors en une minute au
        /// lieu d'être instantanés, comme avant que ce flux existe.
        ///
        /// ALLUMÉ PAR DÉFAUT, sans quoi la fonction serait éteinte chez tout
        /// le monde tant que personne n'a posé le réglage. Mais ÉTEINT quand la
        /// lecture échoue : une panne de base ne doit pas ouvrir des connexions
        /// qui durent des heures.
        /// </summary>
        public const string FluxSse = "FLUX_SSE";

        /// <summary>
        /// LES JEUX, CYCLE PAR CYCLE — voulu par Camara le 20/09/2026.
        ///
        /// TROIS INTERRUPTEURS ET NON UN, parce que les jeux n'arriveront pas
        /// tous en même temps : ceux du primaire — très visuels, à manipuler —
        /// ne ressemblent pas à ceux du lycée, et rien n'oblige à attendre les
        /// seconds pour livrer les premiers. Un drapeau unique aurait forcé à
        /// tout ouvrir ou tout fermer.
        ///
        /// ÉTEINTS PAR DÉFAUT, LES TROIS : tant qu'aucun jeu n'existe, la porte
        /// reste fermée. Un réglage absent ou illisible ne doit pas montrer aux
        /// enfants une page vide.
        ///
        /// Ils décident de l'AFFICHAGE DU BOUTON, rien d'autre. La page reste
        /// joignable par son adresse — cacher un bouton n'est pas une garde de
        /// sécurité, et il n'y a rien à protéger ici.
        /// </summary>
        public const string JeuxPrimaire = "JEUX_PRIMAIRE";

        /// <summary>Voir <see cref="JeuxPrimaire"/>.</summary>
        public const string JeuxCollege = "JEUX_COLLEGE";

        /// <summary>Voir <see cref="JeuxPrimaire"/>.</summary>
        public const string JeuxLycee = "JEUX_LYCEE";

        /// <summary>Le texte du badge ne doit pas déborder de la carte.</summary>
        private const int LongueurTexteLancementMax = 40;

        /// <summary>
        /// Les seules clés acceptées. Sans cette liste, la route deviendrait un
        /// magasin de clés arbitraires que personne ne lit et que rien ne nettoie.
        /// </summary>
        private static readonly string[] ClesConnues =
            [ModeTest, CompteTest, EssaisOuverts, TachesDeFond, Maintenance, VoixDeSecours,
             OffreLancement, OffreLancementBandeau, BlueSky, ModeDeveloppeur, FluxSse,
             JeuxPrimaire, JeuxCollege, JeuxLycee];

        private readonly IReglageRepository _reglages;
        private readonly ILogger<ReglagesController> _logger;

        public ReglagesController(
            IReglageRepository reglages, ILogger<ReglagesController> logger)
        {
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// LE FLUX DES CHANGEMENTS DE RÉGLAGE — Camara, le 16/09/2026 : « quand
        /// j'active Blue Sky, ça ne change pas sur tous les ordinateurs et
        /// mobiles de manière instantanée ».
        ///
        /// Le navigateur ouvre cette écoute une fois (`EventSource`), et reçoit
        /// le NOM de la clé changée — jamais sa valeur. Il rappelle ensuite
        /// `reglages/publics`, qui reste seule à décider de ce qu'un visiteur a
        /// le droit de savoir : un flux anonyme ne doit rien révéler de plus.
        ///
        /// ANONYME, comme `publics` et pour la même raison : le style habille
        /// le site avant toute connexion, page d'attente comprise.
        ///
        /// UN BATTEMENT TOUTES LES 25 SECONDES. Sans trafic, un proxy referme
        /// une connexion inactive au bout d'une minute environ ; le commentaire
        /// SSE (une ligne qui commence par `:`) la tient ouverte sans rien
        /// annoncer. `X-Accel-Buffering: no` interdit à nginx de mettre le flux
        /// en tampon — sans lui, l'annonce arriverait quand la connexion se
        /// ferme, c'est-à-dire jamais.
        /// </summary>
        [HttpGet("flux")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Flux SSE des changements de réglage.")]
        [SwaggerResponse(503, "Trop d'écoutes ouvertes : relecture périodique.")]
        public async Task Flux(
            [FromServices] IDiffusionReglages diffusion, CancellationToken ct)
        {
            // L'ABONNEMENT D'ABORD, LES EN-TÊTES ENSUITE. Une fois le
            // `Content-Type` posé et le corps entamé, il n'est plus possible de
            // répondre autre chose qu'un flux.
            var (id, lecteur) = diffusion.Abonner();

            // REFUSÉ AU-DELÀ DU PLAFOND, ET REFUSÉ FRANCHEMENT. Accepter la
            // connexion pour la refermer aussitôt serait pire que de la refuser :
            // le navigateur y verrait une écoute réussie puis coupée, et
            // reviendrait toutes les trois secondes — le garde-fou nourrirait
            // alors la saturation qu'il est censé contenir. Un statut d'erreur
            // arrête net la reconnexion automatique d'`EventSource` ; le
            // navigateur retombe sur sa relecture périodique, et le service
            // reste rendu.
            if (id == Guid.Empty)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                Response.Headers.RetryAfter = "60";
                return;
            }

            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers["X-Accel-Buffering"] = "no";

            HttpContext.Features
                .Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()
                ?.DisableBuffering();

            try
            {
                // `retry` : le délai de reconnexion que le navigateur respecte
                // après une coupure. Trois secondes, pour qu'un changement
                // manqué pendant une micro-coupure soit rattrapé vite.
                await EcrireSseAsync("retry: 3000\n\n", ct);

                while (!ct.IsCancellationRequested)
                {
                    using var tour = CancellationTokenSource.CreateLinkedTokenSource(ct);

                    var annonce = lecteur.WaitToReadAsync(tour.Token).AsTask();
                    var battement = Task.Delay(BattementSse, tour.Token);
                    var premier = await Task.WhenAny(annonce, battement);

                    // L'autre attente est annulée tout de suite : sans ça, un
                    // minuteur de 25 s resterait en vol à chaque annonce.
                    tour.Cancel();

                    if (premier == battement)
                    {
                        await EcrireSseAsync(": battement\n\n", ct);
                        continue;
                    }

                    if (!await annonce) break;

                    while (lecteur.TryRead(out var cle))
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(cle);
                        await EcrireSseAsync($"data: {{\"cle\":{json}}}\n\n", ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // L'onglet s'est fermé, ou le serveur s'arrête : rien à signaler.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Flux des reglages interrompu.");
            }
            finally
            {
                diffusion.Desabonner(id);
            }
        }

        /// <summary>
        /// Vingt-cinq secondes : sous la minute d'inactivité au bout de laquelle
        /// un proxy referme une connexion, et assez rare pour ne rien coûter —
        /// une douzaine d'octets par navigateur et par demi-minute.
        /// </summary>
        private static readonly TimeSpan BattementSse = TimeSpan.FromSeconds(25);

        private async Task EcrireSseAsync(string texte, CancellationToken ct)
        {
            var octets = System.Text.Encoding.UTF8.GetBytes(texte);

            await Response.Body.WriteAsync(octets, ct);
            await Response.Body.FlushAsync(ct);
        }

        /// <summary>Ce que le navigateur a besoin de savoir, sans être connecté.</summary>
        [HttpGet("publics")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Les drapeaux publics.")]
        public async Task<IActionResult> Publics(
            [FromServices] IOffreLancementService lancement, CancellationToken ct)
        {
            try
            {
                return Ok(new
                {
                    modeTest = await _reglages.EstActifAsync(ModeTest, false, ct),

                    // Ouverts par défaut : un administrateur qui n'a jamais
                    // touché ce réglage s'attend à ce que l'essai fonctionne.
                    essaisOuverts = await _reglages.EstActifAsync(EssaisOuverts, true, ct),

                    // ÉTEINT PAR DÉFAUT, ET C'EST LE SEUL DÉFAUT ACCEPTABLE.
                    // Un réglage absent ou illisible qui afficherait la page
                    // d'attente fermerait le site sur une panne de lecture.
                    maintenance = await _reglages.EstActifAsync(Maintenance, false, ct),

                    // Le style du site : public, puisqu'il habille aussi les
                    // pages qu'on voit avant d'avoir un compte.
                    blueSky = await _reglages.EstActifAsync(BlueSky, false, ct),

                    // LE COUPE-CIRCUIT DU TEMPS RÉEL. Public parce que c'est le
                    // navigateur qui ouvre la connexion longue : c'est donc à lui
                    // qu'il faut dire de s'en abstenir. Allumé par défaut — une
                    // API antérieure à ce drapeau ne l'enverrait pas, et le lire
                    // comme éteint priverait tout le monde du temps réel.
                    fluxSse = await _reglages.EstActifAsync(FluxSse, true, ct),

                    // LES JEUX, CYCLE PAR CYCLE. Publics parce que c'est
                    // l'écran de l'enfant qui décide d'afficher sa porte ou
                    // non, et éteints par défaut : aucun jeu n'existe encore.
                    jeuxPrimaire = await _reglages.EstActifAsync(JeuxPrimaire, false, ct),
                    jeuxCollege = await _reglages.EstActifAsync(JeuxCollege, false, ct),
                    jeuxLycee = await _reglages.EstActifAsync(JeuxLycee, false, ct),

                    // LE BANDEAU EST PUBLIC, et il doit l'être : il annonce
                    // une maintenance ou une panne, c'est-à-dire précisément
                    // ce qu'un visiteur non connecté a besoin de savoir avant
                    // d'essayer de s'inscrire.
                    //
                    // Le texte ne part que si le bandeau est allumé. Sinon un
                    // brouillon préparé pour dimanche serait lisible dans la
                    // réponse dès aujourd'hui, par n'importe qui.
                    bandeau = await BandeauAsync(ct),

                    // PUBLIQUE : c'est une offre commerciale, elle
                    // s'adresse d'abord à ceux qui n'ont pas de compte.
                    //
                    // `Active` est l'état EFFECTIF — interrupteur allumé ET
                    // échéance non dépassée. Le navigateur ne recalcule pas
                    // la règle : il la reçoit. Deux horloges qui décident
                    // séparément d'une même promotion finissent toujours par
                    // se contredire, et le visiteur voit la contradiction.
                    offreLancement = await lancement.LireAsync(ct),
                });
            }
            catch (Exception ex)
            {
                // Un réglage illisible ne doit pas empêcher le site de
                // s'afficher : on rend le mode éteint, c'est-à-dire le produit
                // ouvert, et on journalise.
                _logger.LogError(ex, "Lecture des reglages publics impossible.");
                return Ok(new
                {
                    modeTest = false,
                    essaisOuverts = true,
                    maintenance = false,

                    // Le style d'origine : une panne de lecture ne change pas
                    // l'allure du site.
                    blueSky = false,

                    // ÉTEINT SUR UNE PANNE DE LECTURE, à l'inverse du défaut
                    // normal : quand la base ne répond pas, la dernière chose à
                    // faire est d'ouvrir des connexions qui durent des heures.
                    // Les navigateurs relisent par minute, ce qui suffit
                    // largement le temps que la panne passe.
                    fluxSse = false,

                    // Pas de bandeau quand on ne sait pas lire : afficher un
                    // avertissement sur une panne de lecture inquiéterait
                    // sans rien apprendre.
                    bandeau = (string?)null,

                    // Ni promotion : promettre un cadeau qu'on n'est pas sûr
                    // de pouvoir tenir est pire que de ne rien promettre.
                    offreLancement =
                        new OffreLancement(false, string.Empty, null, false, 0, [], string.Empty),
                });
            }
        }

        [HttpGet]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Tous les réglages.")]
        public async Task<IActionResult> Tous(
            [FromServices] IAbonnementRepository formules, CancellationToken ct) =>
            Ok(new
            {
                modeTest = await _reglages.EstActifAsync(ModeTest, false, ct),

                // Actif par défaut : le compte existe pour servir, et un
                // administrateur qui ne l'a jamais touché s'attend à ce qu'il
                // fonctionne.
                compteTest = await _reglages.EstActifAsync(CompteTest, true, ct),

                essaisOuverts = await _reglages.EstActifAsync(EssaisOuverts, true, ct),

                // Actives par défaut : une base neuve ne doit pas démarrer avec
                // le produit à moitié éteint.
                tachesDeFond = await _reglages.EstActifAsync(TachesDeFond, true, ct),

                maintenance = await _reglages.EstActifAsync(Maintenance, false, ct),

                // Éteint par défaut : le modèle principal reste le
                // meilleur des deux quand il fonctionne. Le secours est un
                // secours, pas un réglage de confort.
                voixDeSecours = await _reglages.EstActifAsync(VoixDeSecours, false, ct),

                blueSky = await _reglages.EstActifAsync(BlueSky, false, ct),

                // Réservé à l'administration : il ne figure PAS dans la route
                // publique. Un visiteur n'a pas à savoir dans quel état sont
                // les professeurs, et un élève encore moins.
                modeDeveloppeur = await _reglages.EstActifAsync(ModeDeveloppeur, false, ct),

                // Allumé par défaut, comme sur la route publique : l'écran doit
                // montrer « activé » tant que personne n'a posé le réglage,
                // puisque c'est bien l'état du produit.
                fluxSse = await _reglages.EstActifAsync(FluxSse, true, ct),

                // Les trois portes des jeux, éteintes par défaut.
                jeuxPrimaire = await _reglages.EstActifAsync(JeuxPrimaire, false, ct),
                jeuxCollege = await _reglages.EstActifAsync(JeuxCollege, false, ct),
                jeuxLycee = await _reglages.EstActifAsync(JeuxLycee, false, ct),

                // Ici le texte part TOUJOURS, allumé ou non : sans quoi on ne
                // pourrait ni préparer un message à l'avance, ni relire celui
                // qu'on vient d'éteindre.
                bandeauMessage = await _reglages.LireAsync(BandeauMessage, ct) ?? string.Empty,
                bandeauActif = await _reglages.EstActifAsync(BandeauActif, false, ct),

                // LES VALEURS BRUTES, et non l'état effectif : l'écran doit
                // pouvoir montrer un interrupteur allumé sur une échéance
                // dépassée. C'est même le cas qu'il faut le plus signaler —
                // l'administrateur croit son offre en cours, elle est morte
                // depuis mardi.
                offreLancement = await _reglages.EstActifAsync(OffreLancement, false, ct),
                offreLancementTexte =
                    await _reglages.LireAsync(OffreLancementTexte, ct) ?? string.Empty,
                offreLancementFin =
                    await _reglages.LireAsync(OffreLancementFin, ct) ?? string.Empty,

                offreLancementBandeau =
                    await _reglages.EstActifAsync(OffreLancementBandeau, true, ct),

                offreLancementMinutes =
                    await _reglages.LireAsync(OffreLancementMinutes, ct) ?? string.Empty,

                offreLancementFormules =
                    await _reglages.LireAsync(OffreLancementFormules, ct) ?? string.Empty,

                // LE CATALOGUE PART AVEC : l'écran coche des formules réelles
                // plutôt que de faire taper des codes qui n'existent peut-être
                // pas. Les essais en sont exclus — on ne fait pas de
                // promotion sur ce qui est déjà gratuit.
                formulesDisponibles = (await formules.GetOffresAsync(ct))
                    .Where(o => !o.EstEssai)
                    .Select(o => new { o.Code, o.Libelle }),
            });

        /// <summary>
        /// Le bandeau : son texte et son affichage, en une seule écriture.
        ///
        /// UNE ROUTE À PART, et non la route générique des drapeaux : celle-ci
        /// écrit une CHAÎNE, quand l'autre n'accepte qu'un booléen. Les tordre
        /// pour partager un chemin aurait donné une route qui prend « soit
        /// l'un soit l'autre », c'est-à-dire une route dont on ne peut plus
        /// lire la signature.
        ///
        /// LES DEUX ENSEMBLE, parce que le geste est un : on écrit un message
        /// et on décide s'il s'affiche. Les séparer en deux appels ouvrirait
        /// une fenêtre — courte, mais réelle — où l'ancien message serait
        /// affiché comme si on venait de le confirmer.
        ///
        /// MÊME BARRE QUE LES MODES. Un bandeau s'adresse à tous les visiteurs,
        /// y compris à ceux qui ne se sont jamais connectés ; annoncer une
        /// panne qui n'existe pas fait autant de dégâts qu'une panne.
        /// </summary>
        [HttpPut("bandeau")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Bandeau enregistré.")]
        [SwaggerResponse(400, "Message trop long, ou allumé sans texte.")]
        public async Task<IActionResult> DefinirBandeau(
            [FromBody] DefinirBandeauRequest requete, CancellationToken ct)
        {
            var message = (requete?.Message ?? string.Empty).Trim();
            var actif = requete?.Actif ?? false;

            if (message.Length > LongueurBandeauMax)
            {
                return BadRequest(new
                {
                    message =
                        $"Le message ne peut pas dépasser {LongueurBandeauMax} caractères.",
                });
            }

            // UN BANDEAU VIDE ALLUMÉ EST UN BANDEAU CASSÉ : une bande de
            // couleur sans texte en haut du site, que le visiteur lit comme
            // un défaut d'affichage. On refuse ici plutôt que de laisser le
            // navigateur deviner quoi en faire.
            if (actif && message.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Écrivez un message avant d'afficher le bandeau.",
                });
            }

            await _reglages.EcrireAsync(BandeauMessage, message, ct);
            await _reglages.DefinirAsync(BandeauActif, actif, ct);

            _logger.LogWarning(
                "Bandeau d information {Etat} ({Longueur} caracteres).",
                actif ? "AFFICHE" : "MASQUE", message.Length);

            return NoContent();
        }

        /// <summary>
        /// Le bandeau tel qu'un visiteur doit le voir : le texte s'il est
        /// allumé, rien du tout sinon.
        /// </summary>
        private async Task<string?> BandeauAsync(CancellationToken ct)
        {
            if (!await _reglages.EstActifAsync(BandeauActif, false, ct)) return null;

            var message = await _reglages.LireAsync(BandeauMessage, ct);

            return string.IsNullOrWhiteSpace(message) ? null : message;
        }

        /// <summary>
        /// L'habillage de l'offre de lancement : son texte et son échéance.
        ///
        /// SÉPARÉE DE L'INTERRUPTEUR, qui reste sur la route des drapeaux.
        /// Régler une campagne et la lancer sont deux gestes, et ce sont
        /// deux gestes qu'on fait à des moments différents : on prépare le
        /// vendredi, on allume le lundi.
        ///
        /// LES DEUX ENSEMBLE parce qu'ils forment une même annonce. Écrire
        /// le texte puis l'échéance en deux appels laisserait, entre les
        /// deux, une promotion dont le compte à rebours pointe encore la
        /// campagne précédente.
        /// </summary>
        [HttpPut("lancement")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Offre enregistrée.")]
        [SwaggerResponse(400, "Texte trop long, ou date illisible.")]
        public async Task<IActionResult> DefinirLancement(
            [FromBody] DefinirLancementRequest requete, CancellationToken ct)
        {
            var texte = (requete?.Texte ?? string.Empty).Trim();
            var fin = (requete?.Fin ?? string.Empty).Trim();
            // UN CHAMP ABSENT NE VAUT PAS ZÉRO, ET ÇA A COÛTÉ UNE OFFRE ÉTEINTE
            // EN SILENCE LE 05/09/2026.
            //
            // Un navigateur resté sur une version antérieure de la page
            // n'envoyait pas encore ces deux champs. Lus en  et ,
            // ils ont écrit zéro heure et aucune formule : l'offre s'est
            // éteinte toute seule, sans erreur, et l'écran continuait
            // d'afficher ses valeurs par défaut comme si tout allait bien.
            //
            // ABSENT VEUT DONC DIRE « NE TOUCHE PAS », et il n'y a que le type
            // nullable pour le distinguer d'un choix. Une liste VIDE, elle,
            // reste une décision : l'administrateur a décoché toutes les
            // formules.
            //
            // Même règle que pour les visuels d'un bandeau promo, où une image
            // absente ne remplace pas celle en place.
            if (requete?.Minutes is int m)
            {
                // Borné : le plafond empêche une faute de frappe d'offrir mille
                // heures à chaque souscription.
                await _reglages.EcrireAsync(
                    OffreLancementMinutes, Math.Clamp(m, 0, 100 * 60).ToString(), ct);
            }

            if (requete?.Formules is not null)
            {
                // NORMALISÉES ET DÉDOUBLONNÉES ICI, une fois. Le service qui les
                // relit compare des codes en majuscules ; laisser passer
                // « solo » ferait une offre réglée qui ne s'applique à rien.
                var liste = string.Join(",", requete.Formules
                    .Select(f => (f ?? string.Empty).Trim().ToUpperInvariant())
                    .Where(f => f.Length > 0)
                    .Distinct());

                await _reglages.EcrireAsync(OffreLancementFormules, liste, ct);
            }

            if (texte.Length > LongueurTexteLancementMax)
            {
                return BadRequest(new
                {
                    message = $"Le texte ne peut pas dépasser {LongueurTexteLancementMax} "
                            + "caractères, sinon il déborde de la carte.",
                });
            }

            // UNE DATE ILLISIBLE EST REFUSÉE, PAS IGNORÉE. Enregistrée en
            // silence, elle se lirait comme « pas d'échéance » : la
            // promotion tournerait sans fin, et le compte à rebours
            // disparaîtrait sans que personne comprenne pourquoi.
            if (fin.Length > 0 && !DateTime.TryParse(
                    fin, null, System.Globalization.DateTimeStyles.RoundtripKind, out _))
            {
                return BadRequest(new { message = "La date de fin est illisible." });
            }

            await _reglages.EcrireAsync(OffreLancementTexte, texte, ct);
            await _reglages.EcrireAsync(OffreLancementFin, fin, ct);


            _logger.LogWarning(
                "Offre de lancement reglee : « {Texte} », fin {Fin}, {Minutes} min, {Formules}.",
                texte,
                fin.Length == 0 ? "sans terme" : fin,
                requete?.Minutes?.ToString() ?? "(inchange)",
                requete?.Formules is null ? "(inchange)" : string.Join(",", requete.Formules));

            return NoContent();
        }

        public class DefinirLancementRequest
        {
            /// <summary>Ce qui s'écrit entre parenthèses. Vide = valeur par défaut.</summary>
            public string? Texte { get; set; }

            /// <summary>ISO 8601 avec fuseau. Vide = offre sans terme.</summary>
            public string? Fin { get; set; }

            /// <summary>Les minutes offertes. Zéro = aucun cadeau.</summary>
            public int? Minutes { get; set; }

            /// <summary>Les codes des formules concernées. Vide = aucune.</summary>
            public string[]? Formules { get; set; }
        }

        // LES MODES SONT RÉSERVÉS AU SUPER-ADMINISTRATEUR. Le mode test et
        // la maintenance ferment ou ouvrent le site pour tout le monde : ce
        // n'est pas une décision qu'on délègue avec l'accès au tableau de bord.
        [HttpPut("{cle}")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Réglage enregistré.")]
        [SwaggerResponse(400, "Clé inconnue.")]
        public async Task<IActionResult> Definir(
            string cle, [FromBody] DefinirReglageRequest requete,
            [FromServices] IDiffusionReglages diffusion, CancellationToken ct)
        {
            var connue = ClesConnues.FirstOrDefault(
                k => string.Equals(cle, k, StringComparison.OrdinalIgnoreCase));

            if (connue is null) return BadRequest(new { message = "Réglage inconnu." });

            await _reglages.DefinirAsync(connue, requete.Actif, ct);

            // LE COUPE-CIRCUIT AGIT MAINTENANT, pas au prochain redémarrage.
            //
            // APRÈS l'écriture, et l'ordre a du sens : l'enregistrement annonce
            // déjà la clé aux navigateurs à l'écoute, qui relisent et se
            // débranchent d'eux-mêmes ; la fermeture ci-dessous ramasse ce qui
            // resterait. Ils sont donc prévenus, puis coupés — jamais coupés
            // sans savoir pourquoi.
            if (connue == FluxSse) diffusion.DefinirActif(requete.Actif);

            _logger.LogWarning(
                "Reglage {Cle} {Etat} par un administrateur.",
                connue, requete.Actif ? "ACTIVE" : "DESACTIVE");

            return NoContent();
        }

        /// <summary>
        /// Recopie le catalogue de la base vers Stripe : produits et tarifs.
        ///
        /// Réservé à l'administration et déclenché à la main, jamais au
        /// démarrage : créer des tarifs chez un prestataire de paiement est
        /// une écriture, pas une lecture. Elle doit être voulue.
        ///
        /// Idempotent — relancer ne crée pas de doublons.
        /// </summary>
        [HttpPost("stripe/catalogue")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(200, "Le catalogue synchronisé.")]
        [SwaggerResponse(503, "Stripe n'est pas configuré.")]
        public async Task<IActionResult> SynchroniserCatalogue(
            [FromServices] ICatalogueStripeService catalogue, CancellationToken ct)
        {
            try
            {
                return Ok(new { lignes = await catalogue.SynchroniserAsync(ct) });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la synchronisation du catalogue Stripe.");
                return StatusCode(500, new { message = "La synchronisation a échoué. Voir les logs." });
            }
        }

        public class DefinirReglageRequest
        {
            public bool Actif { get; set; }
        }

        public class DefinirBandeauRequest
        {
            /// <summary>Le texte affiché. Vide et éteint efface le bandeau.</summary>
            public string? Message { get; set; }

            /// <summary>Le bandeau doit-il apparaître en haut du site ?</summary>
            public bool Actif { get; set; }
        }
    }
}
