using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Lit les planches importées et en extrait la liste des légendes.
    ///
    /// POURQUOI CE WORKER EXISTE
    /// -------------------------
    /// Le professeur n'a jamais vu la planche qu'il affiche. Il écrit une clé,
    /// et le tableau montre ce qui a été importé. Sans description, il
    /// interroge à l'aveugle : il demande « après les bronches, l'air va où ? »
    /// sur une figure qui s'arrête aux bronches, et il ignore l'encart
    /// alvéolaire qui aurait fait tout l'intérêt de la séance.
    ///
    /// Ce qu'on extrait n'est donc PAS un résumé, mais l'inventaire exact de ce
    /// qui est écrit sur la figure. C'est cette liste qui borne ses questions.
    ///
    /// POURQUOI EN TÂCHE DE FOND
    /// -------------------------
    /// L'import doit rester instantané : l'administrateur en enchaîne dix, et
    /// l'agent qui viendra ensuite en enverra cinquante. La description n'est
    /// utile qu'au prochain cours, pas à la seconde qui suit.
    /// </summary>
    public class DescriptionPlanchesWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ReveilPlanches _reveil;
        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IConfiguration _configuration;
        private readonly IJournalClaudeRepository _journal;
        private readonly ILogger<DescriptionPlanchesWorker> _logger;

        private const int TailleLot = 5;

        /// <summary>
        /// Le plafond de la DESCRIPTION.
        ///
        /// Mille deux cents suffisaient tant que les planches étaient des
        /// schémas d'anatomie à six étiquettes. Un tableau périodique en porte
        /// cent dix-huit : le modèle réfléchit, dépasse le plafond, et la
        /// réponse est coupée AVANT le texte. Aucune erreur, aucune exception,
        /// une réponse valide et vide.
        ///
        /// C'est le même piège que celui documenté sur `MaxTokensCarte`, et il
        /// avait été corrigé là sans l'être ici. Quatre mille laissent de la
        /// place à la réflexion et à une liste longue, sans autoriser un pavé :
        /// on ne paie que ce qui est réellement produit.
        /// </summary>
        private const int MaxTokens = 4000;

        /// <summary>
        /// Le plafond de la CARTE, très au-dessus de celui de la description.
        ///
        /// Mille deux cents suffisent à lister des légendes. Placer chacune sur
        /// l'image demande en revanche un vrai travail de lecture, et le modèle
        /// réfléchit avant de répondre : mesuré sur une figure de six
        /// étiquettes, 3 730 jetons de sortie. Sous ce plafond, la réponse est
        /// coupée AVANT le texte — il ne reste que la réflexion, et le bloc de
        /// texte est vide.
        ///
        /// Le symptôme était trompeur : aucune erreur, aucune exception, une
        /// réponse parfaitement valide et vide. Dix-sept planches sur trente-deux
        /// ont été marquées « sans carte » pour cette seule raison.
        ///
        /// Huit mille laisse de la marge aux planches denses. On ne paie que ce
        /// qui est réellement produit.
        /// </summary>
        private const int MaxTokensCarte = 8000;

        public DescriptionPlanchesWorker(
            IServiceScopeFactory scopes,
            ReveilPlanches reveil,
            AnthropicClient client,
            IOptions<OptionsClaude> options,
            IConfiguration configuration,
            IJournalClaudeRepository journal,
            ILogger<DescriptionPlanchesWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // ON ATTEND LA SONNERIE AVANT TOUT TRAVAIL, Y COMPRIS AU DÉMARRAGE.
                //
                // Il y avait ici un passage « au cas où », une minute après le
                // lancement. Il se défendait — rattraper ce qui aurait été
                // importé pendant un déploiement — mais il faisait de chaque
                // REDÉMARRAGE une dépense non demandée. Au premier lancement
                // après correction, il a traité trente planches en quarante
                // secondes : du travail légitime, mais que personne n'avait
                // demandé à cet instant.
                //
                // La règle est maintenant sans exception : ce worker ne dépense
                // que sur un import. Un arriéré éventuel part au prochain, donc
                // quand l'administrateur l'a décidé.
                try { await _reveil.AttendreAsync(ct); }
                catch (OperationCanceledException) { return; }

                try
                {
                    {
                        // ON VIDE LA FILE AVANT DE SE RENDORMIR.
                        //
                        // Les lots font cinq planches. Sans cette boucle, un
                        // import de vingt en traiterait cinq et les quinze
                        // autres attendraient l'import SUIVANT — qui pourrait
                        // ne jamais venir.
                        //
                        // Elle s'arrête sur ce qui a été MARQUÉ, pas sur ce qui
                        // a été lu — une planche qui échoue rend `false`, donc
                        // zéro, donc la boucle se termine. C'est cette
                        // distinction qui l'empêche de tourner à plein régime
                        // sur une figure impossible.
                        //
                        // Le plafond de tours est une ceinture par-dessus la
                        // bretelle : si un chemin oublié rendait un compte
                        // faussement positif, on s'arrêterait quand même. Cent
                        // tours, c'est cinq cents planches — bien au-delà de
                        // tout import réel.
                        for (var tour = 0; tour < 100; tour++)
                        {
                            ct.ThrowIfCancellationRequested();

                            // L'INTERRUPTEUR EST RELU À CHAQUE TOUR DE VIDAGE,
                            // ET NON UNE FOIS AVANT LA BOUCLE.
                            //
                            // Relu une seule fois, il ne servait à rien au
                            // moment où il sert : c'est PENDANT le vidage que
                            // l'administrateur le coupe, parce qu'il voit la
                            // facture monter. La boucle continuait alors
                            // jusqu'au bout de l'arriéré, sourde.
                            if (!await TachesDeFondActivesAsync(ct))
                            {
                                _logger.LogWarning(
                                    "Vidage des planches INTERROMPU au tour {Tour} : "
                                    + "taches de fond eteintes par l'administration.", tour);
                                break;
                            }

                            var faites = await DecrireAsync(ct)
                                         + await ReperersAsync(ct)
                                         + await CrediterAsync(ct);

                            if (faites == 0) break;

                            if (tour == 99)
                            {
                                _logger.LogError(
                                    "Vidage des planches ARRETE au plafond de 100 tours : "
                                    + "quelque chose n'avance pas. Verifier la file a la main.");
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec du tour de description des planches.");
                }
            }
        }

        /// <summary>
        /// Le coupe-circuit, relu en base à chaque tour.
        ///
        /// PAR DÉFAUT ACTIF : une base neuve, ou un réglage jamais posé, ne doit
        /// pas éteindre le produit en silence. On ne coupe que sur décision
        /// explicite.
        ///
        /// Une lecture SQL toutes les deux minutes, contre des lectures d'image
        /// à 8 000 jetons : le rapport de coût se passe de commentaire.
        /// </summary>
        private async Task<bool> TachesDeFondActivesAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var reglages = scope.ServiceProvider.GetRequiredService<IReglageRepository>();

            return await reglages.EstActifAsync(
                Controllers.ReglagesController.TachesDeFond, parDefaut: true, ct);
        }

        private async Task<int> DecrireAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var planches = scope.ServiceProvider.GetRequiredService<IPlancheRepository>();

            var aFaire = (await planches.GetADecrireAsync(TailleLot, ct)).ToList();
            if (aFaire.Count == 0) return 0;

            // ON COMPTE CE QUI A ETE MARQUE, PAS CE QUI A ETE LU.
            //
            // La difference decide de l arret de la boucle de vidage. Rendre le
            // nombre de lignes LUES la ferait tourner sans fin sur une planche
            // qui echoue toujours : relue, jamais marquee, recomptee — et cette
            // fois sans les deux minutes de la ronde pour borner les degats.
            var marquees = 0;

            foreach (var planche in aFaire)
            {
                if (ct.IsCancellationRequested) return marquees;
                if (await DecrireUneAsync(planche, planches, ct)) marquees++;
            }

            // Résolu depuis le scope du tour : ce service est *scoped*, et le
            // worker est un singleton — le prendre au constructeur retiendrait
            // une instance pour la vie du processus.
            var bibliothequeLot = scope.ServiceProvider
                .GetRequiredService<Services.IBibliothequePlanchesService>();

            foreach (var matiere in aFaire.Select(p => p.MatiereCode).Distinct())
            {
                bibliothequeLot.Oublier(matiere);
            }

            _logger.LogInformation(
                "{Marquees}/{Lues} planche(s) decrite(s).", marquees, aFaire.Count);

            return marquees;
        }

        /// <summary>
        /// Décrit UNE planche, et rend la main quand c'est écrit en base.
        ///
        /// Extrait de la boucle pour que l'import puisse l'appeler directement :
        /// un administrateur qui remplace une figure ne doit pas attendre le
        /// prochain tour du worker pour que le professeur sache la lire.
        /// </summary>
        private async Task<bool> DecrireUneAsync(
            Domain.Models.Planche planche, IPlancheRepository planches, CancellationToken ct)
        {
            {
                if (planche.Donnees is not { Length: > 0 }) return true;   // rien a decrire, mais rien a reprendre non plus

                // Un SVG n'est pas une image pour le modèle de vision, et n'a
                // pas besoin de l'être : ses légendes sont du texte, en clair,
                // dans le fichier. Les lire est plus fiable, et gratuit.
                if (EstUnSvg(planche.TypeMime))
                {
                    // Une planche muette est marquée comme telle plutôt que
                    // laissée vide : sans ça, le worker la rechargerait — ses
                    // octets compris — toutes les deux minutes, à jamais.
                    var legendes = LecteurLegendesSvg.Lire(planche.Donnees);

                    // UNE ENVELOPPE N'EST PAS UN DESSIN.
                    //
                    // Beaucoup de « SVG » ne sont qu'un cadre autour d'une image
                    // matricielle — tous les exports d'illustration, tous les
                    // générateurs. Leurs légendes sont dans les pixels, et le
                    // fichier ne contient pas une seule balise `text`.
                    //
                    // Sans ce détour, on les déclarait « sans aucun nom écrit »
                    // et le professeur le soutenait à un élève qui avait une
                    // planche entièrement légendée sous les yeux. C'est arrivé.
                    //
                    // On sort donc l'image de son enveloppe, et la suite se
                    // déroule exactement comme pour un PNG. Le type de la LIGNE
                    // n'est pas touché : c'est bien un SVG qu'on continue de
                    // servir au navigateur, seule sa DESCRIPTION passe par
                    // l'image qu'il contient.
                    var embarquee = string.IsNullOrWhiteSpace(legendes)
                        ? ImageEmbarqueeSvg.Extraire(planche.Donnees)
                        : null;

                    if (embarquee is { } image)
                    {
                        planche.Donnees = image.Donnees;
                        planche.TypeMime = image.TypeMime;

                        _logger.LogInformation(
                            "Planche {Cle} : SVG sans texte enveloppant une image {Type}, "
                            + "description par la vision.", planche.Cle, image.TypeMime);
                    }
                    else
                    {
                        // UN SVG SANS TEXTE N'EST PAS UN DOCUMENT SANS INTÉRÊT.
                        //
                        // L'appareil reproducteur féminin en vue 3D et le schéma
                        // des cinq sens n'ont aucun mot dessus — ce sont des
                        // figures à légender, exactement l'exercice recherché.
                        // Les marquer « aucune légende lisible » les excluait de
                        // la consigne du professeur, qui n'apprenait donc jamais
                        // leur existence.
                        //
                        // Ici il n'y a vraiment rien à décrire : ni texte, ni
                        // image embarquée. On dit ce qu'on sait, et le
                        // professeur s'en sert comme d'une carte muette.
                        await planches.EnregistrerContenuAsync(
                            planche.Id,
                            string.IsNullOrWhiteSpace(legendes)
                                ? "SANS AUCUN NOM ÉCRIT : figure sans aucune étiquette, "
                                  + "à faire légender de mémoire."
                                : await QualifierAsync(legendes, planche.MatiereCode, ct),
                            ct);

                        return true;
                    }
                }

                try
                {
                    var contenu = await DemanderAsync(planche, ct);

                    // UNE RÉPONSE VIDE MARQUE QUAND MÊME LA PLANCHE.
                    //
                    // « On laisse la ligne en attente pour le tour suivant » :
                    // c'est ce que faisait cette méthode, et ça a coûté cher.
                    // Le tableau périodique de la production est resté DIX
                    // JOURS dans la file, relu toutes les deux minutes, chaque
                    // fois facturé, chaque fois rendu vide.
                    //
                    // Vide ne veut pas dire « à réessayer » mais « cette figure
                    // dépasse ce que l'appel peut rendre » — 118 éléments à
                    // lister ne tiennent pas sous le plafond de jetons. Le
                    // prochain tour donnera exactement le même résultat, et le
                    // suivant aussi.
                    //
                    // On écrit donc le constat. La planche sort de la file, le
                    // professeur la traite comme une figure sans légende — ce
                    // qu'elle est, faute de mieux — et un avertissement dit à
                    // l'administrateur de la reprendre à la main.
                    if (string.IsNullOrWhiteSpace(contenu))
                    {
                        _logger.LogWarning(
                            "Planche {Cle} : description VIDE. La figure est probablement trop "
                            + "dense pour le plafond de jetons. Marquee sans legende pour ne pas "
                            + "boucler ; a decrire a la main.", planche.Cle);

                        await planches.EnregistrerContenuAsync(
                            planche.Id, "aucune légende lisible (description automatique vide)", ct);

                        return true;
                    }

                    await planches.EnregistrerContenuAsync(
                        planche.Id, await QualifierAsync(contenu.Trim(), planche.MatiereCode, ct), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Description impossible pour la planche {Cle}.", planche.Cle);
                }
            }

            // On arrive ici apres une exception : rien n a ete marque, et la
            // planche reste en file. Dire `false` arrete la boucle de vidage —
            // sinon elle relirait la meme planche a plein regime.
            return false;

        }

        /// <summary>
        /// La consigne qui produit la carte des étiquettes.
        ///
        /// ON DEMANDE LA POSITION DE LA CHOSE, PAS CELLE DU MOT. Sur ces
        /// planches, les légendes sont rejetées dans les marges et reliées à
        /// l'organe par un trait de rappel : la position du texte « Prostate »
        /// n'est pas celle de la prostate, elle en est souvent à l'autre bout.
        /// C'est le point d'arrivée du trait qui nous intéresse — c'est là que
        /// l'élève clique.
        /// </summary>
        private const string ConsigneReperes = """
            Tu établis la carte des repères d'un schéma scolaire.

            Pour CHAQUE étiquette de la figure, donne la position de CE QU'ELLE
            DÉSIGNE — pas la position du texte.

            Sur ce genre de planche, le mot est écrit dans la marge et un trait
            de rappel le relie à l'organe. Suis ce trait : la position à donner
            est celle de son EXTRÉMITÉ, sur le dessin, là où le trait s'arrête.
            Quand il n'y a pas de trait, donne le centre de la forme désignée.

            Réponds UNIQUEMENT par un tableau JSON, sans texte autour, sans
            balise de code :

            [{"mot":"Prostate","x":0.52,"y":0.36},{"mot":"Anus","x":0.63,"y":0.52}]

            - `x` et `y` sont des FRACTIONS de la largeur et de la hauteur de
              l'image entière, entre 0 et 1. 0,0 est le coin en haut à gauche.
            - Recopie le mot tel qu'il est écrit, sans le traduire ni l'abréger.
            - Inclus les étiquettes des encarts et agrandissements : l'élève
              peut cliquer dessus aussi.
            - N'invente aucune étiquette. Si la figure n'en porte aucune,
              réponds `[]`.

            La précision compte plus que le nombre : une position approximative
            fera désigner le mauvais organe à un enfant. Dans le doute sur une
            étiquette, ne la mets pas.
            """;

        /// <summary>
        /// Extrait la carte des étiquettes des planches qui n'en ont pas encore.
        ///
        /// Un lot par tour, comme la description : c'est un appel à un modèle de
        /// vision par planche, une seule fois dans la vie de la figure.
        /// </summary>
        private async Task<int> ReperersAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var planches = scope.ServiceProvider.GetRequiredService<IPlancheRepository>();

            var aFaire = (await planches.GetSansReperesAsync(TailleLot, ct)).ToList();
            if (aFaire.Count == 0) return 0;

            var marquees = 0;

            foreach (var planche in aFaire)
            {
                if (ct.IsCancellationRequested) return marquees;
                if (await ReperUneAsync(planche, planches, ct)) marquees++;
            }

            return marquees;
        }

        /// <summary>
        /// Cartographie UNE planche, et rend la main quand c'est écrit.
        ///
        /// Extraite de la boucle pour que l'import puisse l'appeler : la file
        /// est triée par ancienneté, donc une planche qu'on vient de déposer y
        /// arrive DERNIÈRE. Elle serait décrite tout de suite et cartographiée
        /// une demi-heure plus tard — et entre les deux, le professeur nommerait
        /// au hasard sur la figure qu'on vient justement de mettre en ligne.
        /// </summary>
        private async Task<bool> ReperUneAsync(
            Domain.Models.Planche planche, IPlancheRepository planches, CancellationToken ct)
        {
            {
                if (planche.Donnees is not { Length: > 0 }) return true;   // rien a cartographier, rien a reprendre

                // UN SEUL ENDROIT DÉCIDE DE CE QU'UN MODÈLE DE VISION PEUT LIRE.
                //
                // Cette conversion se faisait ici avec sa propre règle, qui ne
                // couvrait que les SVG enveloppant une image. Les SVG
                // VECTORIELS — dix-huit planches, toutes légendées — n'étaient
                // jamais cartographiées, et rien ne le disait.
                var lisible = RasteriseurSvg.PourLaVision(planche.Donnees, planche.TypeMime);

                if (lisible is not { } vue)
                {
                    // Marquée vide pour la même raison : un format qu'on ne sait
                    // pas rendre ne se mettra pas à se rendre au tour suivant, et
                    // il bloquerait la file éternellement.
                    await planches.EnregistrerReperesAsync(planche.Id, "[]", ct);

                    _logger.LogWarning(
                        "Planche {Cle} : format illisible pour la vision ({Type}), "
                        + "marquee vide.", planche.Cle, planche.TypeMime);
                    return true;
                }

                try
                {
                    var carte = await DemanderReperesAsync(vue.Donnees, vue.TypeMime, ct);

                    // UN ÉCHEC MUET EST PIRE QU'UNE ERREUR.
                    //
                    // Cette branche ne journalisait rien : une carte illisible
                    // était jetée, redemandée deux minutes plus tard, jetée à
                    // nouveau — sans exception, sans journal, sans colonne
                    // remplie. On a cherché ailleurs pendant que la boucle
                    // tournait à vide sous nos yeux.
                    if (carte is null)
                    {
                        // UNE PLANCHE EN ÉCHEC NE DOIT PAS BLOQUER LA FILE.
                        //
                        // La file est triée par ancienneté et prend les cinq
                        // premières. Laisser une planche sans carte la remet en
                        // tête au tour suivant, et au suivant : deux planches
                        // récalcitrantes ont suffi à figer les cinquante-six
                        // autres. Trois cartes en huit minutes, puis plus rien,
                        // et la boucle tournait pourtant.
                        //
                        // On inscrit donc une carte VIDE. Elle sort de la file,
                        // le professeur retombe sur la lecture de l'image pour
                        // celle-là — dégradé, pas cassé — et le contrôle visuel
                        // la signalera comme « sans carte exploitable ».
                        await planches.EnregistrerReperesAsync(planche.Id, "[]", ct);

                        _logger.LogWarning(
                            "Planche {Cle} : aucune carte exploitable, marquee vide "
                            + "pour ne pas bloquer les suivantes.", planche.Cle);
                        return true;
                    }

                    await planches.EnregistrerReperesAsync(planche.Id, carte, ct);

                    _logger.LogInformation(
                        "Planche {Cle} : {Nombre} repere(s) cartographie(s).",
                        planche.Cle,
                        Domain.Services.CarteReperes.Lire(carte).Count);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Reperage impossible pour la planche {Cle}.", planche.Cle);
                }
            }

            // Apres une exception : rien de marque, donc `false` pour que la
            // boucle de vidage s arrete au lieu de reessayer sans repit.
            return false;
        }

        /// <summary>
        /// L'appel de vision qui rend la carte, ou null s'il n'a rien donné
        /// d'exploitable.
        /// </summary>
        private async Task<string?> DemanderReperesAsync(
            byte[] octets, string? typeMime, CancellationToken ct)
        {
            var reponse = await _client.Messages.Create(
                new MessageCreateParams
                {
                    Model = _options.ModeleDialogue,
                    MaxTokens = MaxTokensCarte,
                    Messages = new List<MessageParam>
                    {
                        new()
                        {
                            Role = Role.User,
                            Content = new List<ContentBlockParam>
                            {
                                new ImageBlockParam
                                {
                                    Source = new Base64ImageSource
                                    {
                                        Data = Convert.ToBase64String(octets),
                                        MediaType = typeMime switch
                                        {
                                            "image/png" => MediaType.ImagePng,
                                            "image/gif" => MediaType.ImageGif,
                                            "image/webp" => MediaType.ImageWebP,
                                            _ => MediaType.ImageJpeg,
                                        },
                                    },
                                },
                                new TextBlockParam { Text = ConsigneReperes },
                            },
                        },
                    },
                },
                cancellationToken: ct);

            // Ce que cet appel a coûté. Sans cette ligne, le poste dépense en
            // silence : seul le dialogue portait ses jetons.
            await _journal.EnregistrerAsync(
                "reperes-planche", reponse.Model,
                reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                reponse.Usage.CacheReadInputTokens ?? 0,
                reponse.Usage.CacheCreationInputTokens ?? 0,
                ct: ct);

            // `TryPickText` ET NON `OfType<TextBlock>()`.
            //
            // `Content` est une liste de blocs en UNION, pas de `TextBlock`.
            // `OfType<TextBlock>()` compile — la méthode accepte n'importe quel
            // type — et ne rend jamais rien. La réponse revenait donc vide, la
            // carte nulle, et la planche était reprise deux minutes plus tard
            // pour le même résultat. Aucune erreur, nulle part.
            //
            // C'est exactement la forme employée par la description, quinze
            // lignes plus haut. Je ne l'ai pas recopiée, je l'ai réécrite.
            var texte = string.Concat(
                reponse.Content
                    .Where(b => b.TryPickText(out _))
                    .Select(b => { b.TryPickText(out var t); return t!.Text; }))
                .Trim();

            // Le modèle enrobe parfois son JSON d'une clôture de code malgré la
            // consigne. On ne le lui reproche pas, on découpe.
            var debut = texte.IndexOf('[');
            var fin = texte.LastIndexOf(']');
            if (debut < 0 || fin <= debut) return null;

            var json = texte[debut..(fin + 1)];

            // On ne stocke que ce qui se relit : une carte illisible enregistrée
            // ne serait découverte qu'en plein cours, et jamais réessayée.
            return Domain.Services.CarteReperes.Lire(json).Count > 0 ? json : null;
        }

        /// <summary>
        /// Décrit une planche À L'INSTANT, pour l'import.
        ///
        /// POURQUOI L'IMPORT ATTEND PLUTÔT QUE DE DÉLÉGUER AU WORKER
        /// --------------------------------------------------------
        /// Tant que la description n'est pas écrite, le professeur peut afficher
        /// la figure sans savoir la lire — et en SVT, où les clés sont écrites
        /// en dur dans sa consigne, il l'affiche effectivement. Il reste alors
        /// une fenêtre où l'élève voit une planche légendée et s'entend dire
        /// qu'elle ne l'est pas.
        ///
        /// Réveiller le worker raccourcissait cette fenêtre ; l'attendre la
        /// SUPPRIME. C'est la seule version dont on puisse dire « ça marche »
        /// sans ajouter « au bout de deux minutes ».
        ///
        /// Le coût est porté par le bon acteur : quelques secondes sur un clic
        /// d'administration, jamais sur une séance.
        ///
        /// Rien n'est propagé en cas d'échec : la planche est enregistrée quoi
        /// qu'il arrive, et le worker repassera. Un modèle de vision
        /// indisponible ne doit pas empêcher d'importer une figure.
        /// </summary>
        public async Task DecrireMaintenantAsync(string cle, CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var planches = scope.ServiceProvider.GetRequiredService<IPlancheRepository>();

                // Par la clé, et non par la file : `GetADecrireAsync` ramènerait
                // un lot entier AVEC ses octets pour en retrouver une seule.
                var planche = await planches.GetAsync(cle, ct);

                // Disparue entre-temps, ou déjà décrite — l'import remet le
                // contenu à null, donc une description présente ici signifie
                // qu'un tour de worker est passé avant nous. Rien à refaire.
                if (planche is null || !string.IsNullOrWhiteSpace(planche.Contenu)) return;

                await DecrireUneAsync(planche, planches, ct);

                // LA CARTE AUSSI, ET TOUT DE SUITE.
                //
                // La file de cartographie est triée par ancienneté : une planche
                // qu'on vient de déposer y arrive DERNIÈRE. Décrite dans la
                // seconde, cartographiée une demi-heure plus tard — et entre les
                // deux, le professeur nomme au hasard sur la figure qu'on vient
                // justement de mettre en ligne.
                //
                // La planche est rechargée : `DecrireUneAsync` a pu remplacer
                // ses octets en mémoire par l'image extraite d'une enveloppe
                // SVG, et le repérage doit repartir de l'original.
                var fraiche = await planches.GetAsync(cle, ct);
                if (fraiche is not null) await ReperUneAsync(fraiche, planches, ct);

                scope.ServiceProvider
                    .GetRequiredService<Services.IBibliothequePlanchesService>()
                    .Oublier(planche.MatiereCode);

                _logger.LogInformation("Planche {Cle} decrite et cartographiee a l'import.", cle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Description immediate impossible pour la planche {Cle} — "
                    + "le worker reprendra.", cle);
            }
        }

        /// <summary>
        /// Re-juge la langue des planches déjà écartées, sans relire aucune
        /// image.
        ///
        /// POURQUOI CETTE PASSE EXISTE
        /// ---------------------------
        /// La règle de langue a changé deux fois, et chaque fois elle a laissé
        /// derrière elle des planches marquées à tort — treize en anglais, où
        /// la langue enseignée est évidemment attendue, puis plusieurs en
        /// histoire-géo et en physique-chimie, condamnées par « Sputnik »,
        /// « perestroïka », « Erlenmeyer » ou « Liebig ». Toutes entièrement
        /// françaises.
        ///
        /// Sans cette passe, la seule façon de les récupérer serait de les
        /// réimporter — donc de repayer une lecture d'image par planche, pour
        /// retrouver des légendes déjà en base et parfaitement justes. Ici on
        /// ne refait QUE le jugement de langue : un appel de texte de quelques
        /// jetons.
        ///
        /// IDEMPOTENTE. Une planche re-jugée étrangère garde sa marque, et
        /// aucune planche saine n'est touchée : on ne lit que celles qui
        /// portent déjà le préfixe.
        /// </summary>
        /// <returns>Le nombre de planches rendues au professeur.</returns>
        public async Task<(int Examinees, int Liberees)> RequalifierAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var planches = scope.ServiceProvider.GetRequiredService<IPlancheRepository>();

            var marquees = (await planches.GetMarqueesEtrangeresAsync(ct)).ToList();
            var liberees = 0;

            foreach (var planche in marquees)
            {
                ct.ThrowIfCancellationRequested();

                // On retire le préfixe pour retrouver les légendes telles
                // qu'elles avaient été relevées, puis on les soumet à la règle
                // du jour.
                var legendes = planche.Contenu![LangueEtrangere.Length..].TrimStart(' ', '—', ' ');

                if (await EstAcceptableAsync(legendes, planche.MatiereCode, ct))
                {
                    await planches.EnregistrerContenuAsync(planche.Id, legendes, ct);
                    liberees++;

                    _logger.LogInformation(
                        "Planche {Cle} ({Matiere}) rendue au professeur.",
                        planche.Cle, planche.MatiereCode);
                }
            }

            if (liberees > 0)
            {
                // Le bloc de consigne est gardé cinq minutes : sans cet oubli,
                // le professeur continuerait d'ignorer ces planches pendant
                // tout ce temps.
                var bibliotheque = scope.ServiceProvider
                    .GetRequiredService<IBibliothequePlanchesService>();

                foreach (var matiere in marquees.Select(p => p.MatiereCode).Distinct())
                {
                    bibliotheque.Oublier(matiere);
                }
            }

            _logger.LogWarning(
                "Requalification : {Liberees} planche(s) rendue(s) sur {Examinees} examinee(s).",
                liberees, marquees.Count);

            return (marquees.Count, liberees);
        }

        /// <summary>
        /// Le relevé, marqué s'il n'est pas en français.
        ///
        /// Le marqueur est écrit DANS le contenu, et non dans une colonne à
        /// part : c'est ce texte que lisent l'administration et le professeur,
        /// donc c'est là qu'il doit se voir. Une colonne de plus aurait exigé
        /// une migration, et surtout deux endroits à interroger là où un seul
        /// suffit.
        ///
        /// « Aucune légende lisible » n'est pas soumis au contrôle : il n'y a
        /// rien à juger, et l'appel serait payé pour rien.
        /// </summary>
        private async Task<string> QualifierAsync(
            string legendes, string? matiereCode, CancellationToken ct)
        {
            if (legendes.StartsWith(AucuneLegende, StringComparison.OrdinalIgnoreCase))
            {
                return legendes;
            }

            // NOTRE PROPRE PROSE N'EST PAS À JUGER.
            //
            // Une description de planche muette est rédigée par nous, en
            // français, à partir de notre consigne. La soumettre au contrôle a
            // fait écarter le système solaire — « SANS AUCUN NOM ÉCRIT : trois
            // cercles concentriques… » déclaré langue étrangère.
            if (legendes.StartsWith(SansNomEcrit, StringComparison.OrdinalIgnoreCase))
            {
                return legendes;
            }

            // RIEN À JUGER S'IL N'Y A PAS DE MOTS.
            //
            // « +, −, A » sur un circuit, « I = I1 + I2 » sur une dérivation,
            // une planche numérotée « 119, 118, 63 » : ce ne sont pas des
            // langues. Deux mots d'au moins trois lettres, c'est le minimum
            // pour qu'une question de langue ait un sens.
            var mots = System.Text.RegularExpressions.Regex
                .Matches(legendes, @"\p{L}{3,}")
                .Count;

            if (mots < 2) return legendes;

            return await EstAcceptableAsync(legendes, matiereCode, ct)
                ? legendes
                : $"{LangueEtrangere} — {legendes}";
        }

        /// <summary>
        /// Retrouve l'origine des planches déposées à la main.
        ///
        /// L'agent, lui, relève auteur et licence au moment où il choisit la
        /// planche. Celles qu'on dépose depuis l'administration arrivent nues :
        /// l'administrateur a téléchargé une image depuis Commons, l'a glissée
        /// ici, et rien ne dit plus d'où elle vient. Or CC BY impose de nommer
        /// l'auteur partout où l'œuvre est montrée.
        ///
        /// L'empreinte du fichier suffit à l'identifier avec certitude. Vérifié
        /// sur la coupe respiratoire : bloc de métadonnées interne VIDE, et
        /// l'empreinte a rendu « Berrucomons, CC BY-SA 3.0 ».
        ///
        /// UNE SOURCE EST ÉCRITE MÊME EN CAS D'ÉCHEC, et c'est indispensable :
        /// sans elle, la planche repasserait à chaque tour, et on interrogerait
        /// Commons indéfiniment sur un fichier qu'il ne connaît pas.
        /// </summary>
        private async Task<int> CrediterAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var planches = scope.ServiceProvider.GetRequiredService<IPlancheRepository>();
            var commons = scope.ServiceProvider.GetRequiredService<IOrigineCommonsService>();

            var nues = (await planches.GetSansCreditAsync(TailleLot, ct)).ToList();
            if (nues.Count == 0) return 0;

            // LE NOMBRE DE PLANCHES MARQUÉES, JAMAIS CELUI DES PLANCHES LUES.
            //
            // C'est le défaut qui a fait tourner la boucle de vidage toute une
            // nuit. Une planche sans octets est sautée par le `continue`
            // ci-dessous sans jamais quitter la file — mais elle était comptée
            // dans le total rendu. Le vidage croyait donc avoir avancé à chaque
            // tour, et repartait indéfiniment sur la même.
            //
            // Le même défaut existait sur les deux autres files ; je l'ai
            // corrigé là-bas et oublié ici, ce qui a suffi.
            var marquees = 0;

            foreach (var planche in nues)
            {
                if (ct.IsCancellationRequested) return marquees;

                // Sautée ET non marquée : elle ne compte pas comme un progrès,
                // sinon la boucle ne s'arrête jamais.
                if (planche.Donnees is not { Length: > 0 }) continue;

                var origine = await commons.ChercherAsync(planche.Donnees, ct);

                if (origine?.Licence is { Length: > 0 })
                {
                    await planches.EnregistrerCreditAsync(
                        planche.Id, origine.Auteur, origine.Source, origine.Licence, ct);

                    _logger.LogInformation(
                        "Origine trouvee pour {Cle} : {Auteur}, {Licence}.",
                        planche.Cle, origine.Auteur, origine.Licence);
                }
                else
                {
                    await planches.EnregistrerCreditAsync(
                        planche.Id, null, OrigineInconnue, null, ct);

                    _logger.LogInformation(
                        "Origine introuvable pour {Cle} : le credit reste a saisir.", planche.Cle);
                }

                // Les deux branches écrivent un crédit : la planche quitte la
                // file dans les deux cas. C'est ça, et seulement ça, qu'on
                // compte comme un progrès.
                marquees++;
            }

            if (marquees < nues.Count)
            {
                _logger.LogWarning(
                    "Credits : {Marquees}/{Lues} planche(s) traitee(s). Les autres n'ont pas "
                    + "d'octets en base et resteront dans la file.", marquees, nues.Count);
            }

            return marquees;
        }

        /// <summary>
        /// Posé dans la source quand Commons ne reconnaît pas le fichier — il a
        /// été recadré, ré-enregistré, ou vient d'ailleurs. Marque la recherche
        /// comme faite ; le crédit reste à saisir à la main.
        /// </summary>
        public const string OrigineInconnue = "origine non retrouvée";

        private static bool EstUnSvg(string? typeMime) =>
            string.Equals(typeMime, "image/svg+xml", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Marque une planche dont on a bien tenté la lecture, sans rien
        /// trouver. Elle ne repassera pas ; le bloc du prompt l'ignore.
        /// </summary>
        public const string AucuneLegende = "aucune légende lisible";

        /// <summary>
        /// Préfixe posé sur une planche dont les légendes ne sont pas en
        /// français. Elle reste en base — l'administrateur doit la voir pour la
        /// remplacer — mais elle n'entre plus dans la consigne du professeur.
        /// </summary>
        public const string LangueEtrangere = "LANGUE ÉTRANGÈRE";

        /// <summary>
        /// Les matières où une langue étrangère est ATTENDUE sur la planche.
        ///
        /// LE CONTRÔLE DE LANGUE NE PEUT PAS ÊTRE LE MÊME PARTOUT.
        /// -------------------------------------------------------
        /// Il a été écrit pour la SVT et l'histoire-géo, où un mot anglais sur
        /// une figure signale une planche importée par erreur. Appliqué tel
        /// quel à l'anglais, il condamne exactement ce qu'on cherche : une
        /// planche des prépositions de lieu porte « The ball is in the box »
        /// en face de « La balle est dans la boîte », et c'est précisément
        /// l'intérêt de la figure. Les treize planches d'anglais ont toutes été
        /// marquées « le professeur ne l'affichera pas ».
        ///
        /// La règle devient donc : le français est toujours accepté, et la
        /// langue de la matière aussi. Une planche norvégienne importée sous
        /// « Anglais » reste écartée — l'erreur d'import, elle, existe encore.
        ///
        /// À COMPLÉTER quand une langue s'ajoute au parcours. Une matière
        /// absente de cette table est traitée comme une matière non
        /// linguistique : seul le français y est admis, ce qui est le bon
        /// défaut pour la SVT comme pour une langue qu'on aurait oublié
        /// d'inscrire ici — le symptôme se voit tout de suite, à l'import.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> LanguesEnseignees =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ANGLAIS"] = "anglais",
                ["ESPAGNOL"] = "espagnol",
                ["ALLEMAND"] = "allemand",
                ["ITALIEN"] = "italien",
                ["CHINOIS"] = "chinois",
            };

        /// <summary>
        /// En-tête des descriptions que NOUS rédigeons, pour les figures qui ne
        /// portent aucun mot. Elles sont françaises par construction.
        /// </summary>
        public const string SansNomEcrit = "SANS AUCUN NOM ÉCRIT";

        /// <summary>
        /// LA LANGUE SE JUGE SUR CE QUI EST ÉCRIT, PAS SUR LE NOM DU FICHIER.
        ///
        /// L'agent filtrait sur le titre et la description de Commons. C'est
        /// insuffisant, et le premier tour complet l'a montré : l'appareil
        /// reproducteur masculin est arrivé légendé en chinois, la cellule
        /// végétale en norvégien, la chaîne alimentaire en anglais. Aucun de ces
        /// fichiers ne porte de variante française — le titre, lui, ne disait
        /// rien de concluant.
        ///
        /// Ici, on tient la vérité : les mots relevés SUR la planche. Un appel
        /// de texte de quelques jetons suffit à trancher, et il tombe une seule
        /// fois par planche.
        ///
        /// EN CAS DE DOUTE, ON GARDE. Une planche française écartée à tort
        /// disparaîtrait sans laisser de trace ; une planche étrangère qui passe
        /// se voit au premier cours.
        /// </summary>
        private async Task<bool> EstAcceptableAsync(
            string legendes, string? matiereCode, CancellationToken ct)
        {
            // LA LANGUE DE LA MATIÈRE EST ATTENDUE, PAS TOLÉRÉE.
            //
            // Sur une planche d'anglais, « The ball is in the box » en face de
            // « La balle est dans la boîte » n'est pas un défaut d'import :
            // c'est l'objet même de la figure. Le mélange des deux langues est
            // la forme normale d'un support de langue vivante.
            var langue = matiereCode is not null
                && LanguesEnseignees.TryGetValue(matiereCode, out var enseignee)
                    ? enseignee
                    : null;

            // La phrase qui suit est vide pour les autres matières : on ne
            // souffle aucune langue au modèle quand aucune n'est attendue.
            // LA LISTE DES LANGUES ACCEPTÉES, écrite une fois et reprise deux
            // fois dans la question — au moment de demander, et au moment de
            // dire quand refuser. Les deux DOIVENT dire la même chose : une
            // question qui accepte l'anglais et une consigne de refus qui ne le
            // mentionne pas laisserait le modèle arbitrer seul.
            var accepte = langue is null ? "le français" : $"le français ou l'{langue}";

            try
            {
                var reponse = await _client.Messages.Create(
                    new MessageCreateParams
                    {
                        Model = _options.ModeleTranscription,
                        MaxTokens = 8,
                        Messages = new List<MessageParam>
                        {
                            new()
                            {
                                Role = Role.User,
                                // ON JUGE LA LANGUE DOMINANTE, PAS LA PRÉSENCE
                                // D'UN MOT ÉTRANGER.
                                //
                                // Trois formulations, et voici pourquoi c'est
                                // la troisième :
                                //
                                // 1. « Est-ce du français ? » — écartait les
                                //    planches sans mots. Un circuit légendé
                                //    « +, −, A » n'est aucune langue.
                                //
                                // 2. « Y a-t-il des mots étrangers ? » — corrige
                                //    ce cas, mais condamne l'histoire-géo. Une
                                //    planche sur la guerre froide porte
                                //    Sputnik, Apollo, perestroïka, glasnost,
                                //    Plan Marshall, BELGIQUE, ALLEMAGNE : que
                                //    des mots français ou francisés, et le
                                //    modèle répond OUI. Deux planches
                                //    entièrement françaises ont été refusées.
                                //
                                // 3. Celle-ci : la langue DOMINANTE est-elle le
                                //    français ? Un texte de deux cents mots
                                //    français avec trois emprunts russes est du
                                //    français, et un locuteur le dirait sans
                                //    hésiter. Le cas des planches sans mots
                                //    reste écarté en amont — on exige au moins
                                //    deux mots de trois lettres avant même de
                                //    poser la question.
                                // UNE QUESTION FERMÉE, ET UNE SEULE FORME POUR
                                // TOUTES LES MATIÈRES.
                                //
                                // La version d'avant en posait une à trois
                                // branches pour les langues vivantes — « en
                                // français, en anglais, ou dans un mélange des
                                // deux ? » — à laquelle on ne peut répondre ni
                                // OUI ni NON. Mesuré sur un jeu de sept cas
                                // étiquetés : elle refusait la planche des
                                // prépositions anglaises, celle-là même qu'on
                                // cherchait à sauver. La forme fermée passe les
                                // sept.
                                Content =
                                    "Voici les mots relevés sur un schéma scolaire :\n\n"
                                    + legendes
                                    + $"\n\nQUESTION : l'essentiel de ce texte est-il écrit "
                                    + $"en {accepte} ?\n\n"
                                    + "Les noms propres, noms de lieux, sigles, symboles, chiffres, "
                                    + "unités, termes latins et mots empruntés passés dans l'usage "
                                    + "(spoutnik, perestroïka, Erlenmeyer, leader) ne comptent PAS "
                                    + "comme une autre langue : ils s'emploient tels quels en "
                                    + "français.\n\n"
                                    + $"Réponds NON uniquement si la MAJORITÉ du texte est dans "
                                    + $"une langue qui n'est ni {accepte}.\n\n"
                                    + "Réponds par OUI ou NON, un seul mot.",
                            },
                        },
                    },
                    cancellationToken: ct);

                // Ce que cet appel a coûté. Sans cette ligne, le poste dépense en
                // silence : seul le dialogue portait ses jetons.
                await _journal.EnregistrerAsync(
                    "reperes-planche", reponse.Model,
                    reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                    reponse.Usage.CacheReadInputTokens ?? 0,
                    reponse.Usage.CacheCreationInputTokens ?? 0,
                    ct: ct);

                var texte = string.Join(" ", reponse.Content
                    .Where(b => b.TryPickText(out _))
                    .Select(b => { b.TryPickText(out var t); return t!.Text; }));

                // LA QUESTION PORTE DE NOUVEAU SUR LE FRANÇAIS : C'EST NON QUI
                // CONDAMNE.
                //
                // Et en cas de réponse illisible — ni OUI ni NON — on GARDE.
                // Une planche française écartée à tort disparaît sans laisser
                // de trace ; une planche étrangère qui passe se voit au premier
                // cours, et se retire d'un clic.
                return !texte.TrimStart().StartsWith("NON", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Controle de langue impossible : la planche est gardée.");
                return true;
            }
        }

        private async Task<string?> DemanderAsync(
            Domain.Models.Planche planche, CancellationToken ct)
        {
            var blocs = new List<ContentBlockParam>
            {
                new ImageBlockParam
                {
                    Source = new Base64ImageSource
                    {
                        Data = Convert.ToBase64String(planche.Donnees!),
                        MediaType = planche.TypeMime switch
                        {
                            "image/png" => MediaType.ImagePng,
                            "image/gif" => MediaType.ImageGif,
                            "image/webp" => MediaType.ImageWebP,
                            _ => MediaType.ImageJpeg,
                        },
                    },
                },
                new TextBlockParam { Text = Consigne },
            };

            var reponse = await _client.Messages.Create(
                new MessageCreateParams
                {
                    // LE MODÈLE DE DIALOGUE, PAS CELUI DE TRANSCRIPTION.
                    //
                    // Transcrire un énoncé, c'est recopier ce qui est écrit ;
                    // Haiku y suffit et il y en a des milliers. Lire une carte,
                    // c'est reconnaître des formes — et le petit modèle s'y est
                    // trompé : sur un fond de carte des régions, il a annoncé
                    // « encarts montrant la Corse, la Bretagne, l'Alsace » là où
                    // les encarts sont les territoires d'outre-mer.
                    //
                    // Une erreur ici est PERMANENTE et INVISIBLE : elle est
                    // écrite une fois, relue à chaque cours, et le professeur n'a
                    // aucun moyen de la démentir puisqu'il ne voit pas l'image.
                    //
                    // Le surcoût est nul à l'échelle : un appel par planche, une
                    // fois pour toutes, cent cinquante planches au total.
                    Model = _options.ModeleDialogue,
                    MaxTokens = MaxTokens,
                    Messages = new List<MessageParam>
                    {
                        new() { Role = Role.User, Content = blocs },
                    },
                },
                cancellationToken: ct);

            // Ce que cet appel a coûté. Sans cette ligne, le poste dépense en
            // silence : seul le dialogue portait ses jetons.
            await _journal.EnregistrerAsync(
                "description-planche", reponse.Model,
                reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                reponse.Usage.CacheReadInputTokens ?? 0,
                reponse.Usage.CacheCreationInputTokens ?? 0,
                ct: ct);

            var morceaux = reponse.Content
                .Where(b => b.TryPickText(out _))
                .Select(b => { b.TryPickText(out var t); return t!.Text; });

            return string.Join("\n", morceaux);
        }

        /// <summary>
        /// La consigne d'extraction.
        ///
        /// Elle demande un INVENTAIRE, pas une explication : ce texte servira à
        /// borner les questions du professeur, donc tout ce qui n'y figure pas
        /// sera traité comme absent de la figure — et tout ce qui y figure à
        /// tort le fera interroger sur du vide.
        /// </summary>
        private const string Consigne = """
            Tu prépares la fiche technique d'un document scolaire, pour un
            professeur qui va l'afficher SANS L'AVOIR VU.

            CAS 1 — le document porte des mots écrits.

            Liste-les EXACTEMENT, dans l'ordre où on les lit, séparés par des
            virgules. Rien d'autre.

            - Recopie-les tels quels, sans les traduire ni les corriger.
            - S'il y a un encart ou un agrandissement, signale-le et liste ses
              mots aussi : « encart : alvéoles, artère pulmonaire ».
            - N'invente RIEN. Un élément non écrit sur l'image n'existe pas pour
              cette liste, même si tu sais qu'il devrait y être.
            - Ne recopie PAS les crédits collés dans l'image — nom d'un
              organisme, d'un auteur, d'une banque d'images, filigrane. « National
              Cancer Institute » n'est pas un organe, et le professeur l'a
              annoncé à l'élève comme un élément du schéma.

            CAS 2 — le document ne porte AUCUN mot, ou presque.

            C'est fréquent et ce n'est pas un défaut : une carte muette, une
            silhouette à légender, un fond de carte sont faits pour ça, et c'est
            souvent l'exercice recherché.

            Commence alors par « SANS AUCUN NOM ÉCRIT : » puis dis en une phrase
            ce que le document représente et ce qu'un élève peut y DISTINGUER à
            l'œil. Exemple :

            SANS AUCUN NOM ÉCRIT : carte du relief de la France métropolitaine
            et de la Corse, reliefs en teintes, limites des départements,
            tracé des fleuves, échelle en kilomètres.

            Tu peux nommer ce que la FORME rend reconnaissable SANS LE MOINDRE
            DOUTE — un massif, un littoral, un fleuve — sans prétendre que le nom
            est écrit. Le professeur en a besoin : c'est ce qui lui dit s'il peut
            demander « montre-moi les Alpes » plutôt que « lis le nom de ce
            massif ».

            MAIS UN NOM INVENTÉ EST PIRE QUE PAS DE NOM. Le professeur te croira
            sur parole — il n'a pas vu l'image — et enverra l'élève chercher
            quelque chose qui n'y est pas. Dans le doute, décris la forme sans la
            nommer : « un massif au sud-est », « quatre encarts en bas à gauche ».

            LES ENCARTS SONT LE PIÈGE PRINCIPAL. Compte-les, dis où ils sont, et
            ne nomme leur contenu QUE si tu le reconnais avec certitude. Sur une
            carte de France, des encarts détachés montrent presque toujours les
            territoires d'outre-mer — jamais des régions métropolitaines, qui
            sont déjà sur la carte principale. La Corse est dessinée à sa place,
            au sud-est, ce n'est pas un encart.

            Dans les deux cas : pas d'introduction, pas de conclusion, pas de
            commentaire sur la qualité du document.

            Si ce n'est pas un document scolaire du tout — une photographie, un
            portrait, un logo — réponds exactement :
            aucune légende lisible
            """;
    }
}
