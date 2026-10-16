using Anthropic;
using SchoolWebApp.Domain.Repositories;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Transcrit en texte les documents envoyés par les élèves, puis laisse la
    /// purge effacer les octets.
    ///
    /// POURQUOI ON GARDE LE TEXTE ET PAS L'IMAGE
    /// ----------------------------------------
    /// Une photo de feuille pèse trois mégaoctets, sa transcription deux
    /// kilo-octets — mille cinq cents fois moins. Et au bout de quelques jours,
    /// le professeur n'a plus besoin de VOIR la feuille : il a besoin de savoir
    /// sur quel exercice on avait travaillé. Le texte suffit à ça, l'image ne
    /// sert plus à rien — elle est déjà sortie de la fenêtre d'historique et
    /// n'est plus jamais envoyée au modèle.
    ///
    /// Côté vie privée, c'est la même conclusion par un autre chemin : garder
    /// l'écriture manuscrite d'un enfant et son nom en haut de la copie pendant
    /// douze mois n'a aucune justification une fois la séance passée.
    ///
    /// POURQUOI EN TÂCHE DE FOND ET NON À L'ENVOI
    /// -----------------------------------------
    /// Transcrire prend quelques secondes. Les faire attendre à l'élève, la
    /// photo choisie et le doigt sur « Envoyer », serait absurde : la
    /// transcription ne sert à rien pendant la séance — c'est l'image elle-même
    /// qui part au professeur. Elle ne sert qu'après.
    ///
    /// EN TÂCHE DE FOND, MAIS PLUS AU CHRONOMÈTRE
    /// -----------------------------------------
    /// Il regardait la base toutes les dix minutes. Il attend maintenant
    /// <see cref="ReveilDocuments"/> : le dépôt d'un document le réveille, il
    /// vide sa file, il se rendort. Aucun tour à vide, et une phrase suffit à
    /// dire ce qu'il dépense — rien tant que personne n'envoie rien.
    /// </summary>
    public class TranscriptionDocumentsWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly Services.PiecesJointes.TranscripteurDocuments _transcripteur;
        private readonly ReveilDocuments _reveil;
        private readonly ILogger<TranscriptionDocumentsWorker> _logger;

        /// <summary>
        /// Documents traités par tour. Chacun est un appel au modèle : en
        /// enchaîner cent d'affilée ferait un pic de dépense sans raison.
        /// </summary>
        private const int TailleLot = 10;

        public TranscriptionDocumentsWorker(
            IServiceScopeFactory scopes,
            Services.PiecesJointes.TranscripteurDocuments transcripteur,
            ReveilDocuments reveil,
            ILogger<TranscriptionDocumentsWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _transcripteur = transcripteur ?? throw new ArgumentNullException(nameof(transcripteur));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // AUCUN PASSAGE AU DÉMARRAGE, ET AUCUNE RONDE.
                //
                // Un tour au lancement reprendrait tout l'arriéré à chaque
                // redéploiement — exactement ce qui a vidé le crédit sur les
                // planches. L'arriéré, lui, repartira au prochain dépôt : il
                // n'est pressé par rien, la purge refusant d'effacer les octets
                // d'un document non transcrit.
                try { await _reveil.AttendreAsync(ct); }
                catch (OperationCanceledException) { return; }

                try
                {
                    // ON VIDE LA FILE AVANT DE SE RENDORMIR.
                    //
                    // Les lots font dix documents. Une classe qui en dépose
                    // trente en traiterait dix, et les vingt autres
                    // attendraient un dépôt SUIVANT — qui peut ne pas venir
                    // avant la fin de la semaine.
                    //
                    // La boucle s'arrête sur ce qui a été MARQUÉ, pas sur ce
                    // qui a été lu : un document qui échoue rend zéro, donc
                    // elle se termine. C'est cette distinction-là qui manquait
                    // sur les planches, et qui a fait la boucle chaude.
                    for (var tour = 0; tour < 100; tour++)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Le coupe-circuit, relu à CHAQUE tour de vidage : c'est
                        // pendant le vidage que l'administrateur l'éteint, parce
                        // qu'il voit la facture monter.
                        if (!await TachesDeFondActivesAsync(ct))
                        {
                            _logger.LogWarning(
                                "Transcription INTERROMPUE au tour {Tour} : taches de fond "
                                + "eteintes par l'administration.", tour);
                            break;
                        }

                        // Les pièces jointes, puis le cours importé avec un
                        // contrôle : la même consigne, deux files.
                        if (await TranscrireAsync(ct) + await TranscrireCoursAsync(ct) == 0) break;

                        if (tour == 99)
                        {
                            _logger.LogError(
                                "Vidage des documents ARRETE au plafond de 100 tours : "
                                + "quelque chose n'avance pas. Verifier la file a la main.");
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec du tour de transcription des documents.");
                }
            }
        }

        private async Task<bool> TachesDeFondActivesAsync(CancellationToken ct)
        {
            using var portee = _scopes.CreateScope();
            var reglages = portee.ServiceProvider.GetRequiredService<IReglageRepository>();

            return await reglages.EstActifAsync(
                Controllers.ReglagesController.TachesDeFond, true, ct);
        }

        /// <summary>
        /// Un lot. Rend le nombre de documents réellement MARQUÉS — pas lus.
        ///
        /// LA DISTINCTION EST TOUT LE SUJET. Rendre le nombre de documents
        /// rapportés par la base ferait tourner la boucle de vidage à plein
        /// régime sur une file qui n'avance pas : chaque tour relirait les
        /// mêmes dix documents, échouerait sur les mêmes, et se déclarerait
        /// productif. C'est exactement le bug qui a brûlé le crédit sur les
        /// planches.
        /// </summary>
        private async Task<int> TranscrireAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var conversations = scope.ServiceProvider.GetRequiredService<IConversationService>();

            var aFaire = (await conversations.GetATranscrireAsync(TailleLot, ct)).ToList();
            if (aFaire.Count == 0) return 0;

            var marques = 0;

            foreach (var piece in aFaire)
            {
                if (ct.IsCancellationRequested) return marques;
                if (piece.Donnees is not { Length: > 0 }) continue;

                try
                {
                    var texte = await _transcripteur.TranscrireAsync(
                        piece.Donnees, piece.TypeMime, piece.NomFichier, "transcription-document", ct);

                    // UN ÉCHEC EST ÉCRIT, IL N'EST PLUS LAISSÉ EN ATTENTE.
                    //
                    // Le raisonnement d'origine se défendait : une transcription
                    // vide autoriserait la purge à effacer les octets, et le
                    // document serait perdu sans trace. On préférait donc le
                    // laisser revenir au tour suivant.
                    //
                    // Sauf qu'un document illisible le reste. Chaque tour
                    // renvoie la même image au modèle pour la même réponse
                    // vide, toutes les dix minutes, indéfiniment. C'est ce
                    // mécanisme — sur le worker des planches, où il était
                    // identique — qui a fait relire un tableau périodique sept
                    // mille fois en dix jours.
                    //
                    // On écrit donc un constat plutôt que rien. Le document
                    // n'est pas « perdu sans trace » : la trace, c'est cette
                    // ligne, et elle dit ce qui s'est passé. Un avertissement
                    // permet de reprendre le document à la main tant que ses
                    // octets sont encore là.
                    if (string.IsNullOrWhiteSpace(texte))
                    {
                        _logger.LogWarning(
                            "Piece jointe {Id} : transcription VIDE. Document probablement trop "
                            + "dense pour le plafond de jetons. Marquee pour ne pas boucler ; "
                            + "a reprendre a la main avant la purge des octets.", piece.Id);

                        await conversations.EnregistrerTranscriptionAsync(
                            piece.Id, "(document illisible : transcription automatique vide)", ct);

                        // COMPTÉ COMME MARQUÉ, et c'est voulu : le document est
                        // sorti de la file pour de bon. Ne pas le compter
                        // arrêterait le vidage sur le premier document
                        // illisible, laissant les suivants en plan.
                        marques++;
                        continue;
                    }

                    await conversations.EnregistrerTranscriptionAsync(piece.Id, texte.Trim(), ct);
                    marques++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return marques;
                }
                catch (Exception ex)
                {
                    // Un document illisible ne doit pas bloquer les suivants.
                    _logger.LogWarning(ex,
                        "Transcription impossible pour le document {PieceId}.", piece.Id);
                }
            }

            _logger.LogInformation(
                "{Marques}/{Lus} document(s) transcrit(s).", marques, aFaire.Count);

            return marques;
        }

        /// <summary>
        /// LE COURS IMPORTÉ AVEC UN CONTRÔLE — 26/09/2026. Même consigne que les
        /// pièces jointes (voir TranscripteurDocuments) ; les octets partent dès
        /// que le texte est écrit.
        ///
        /// UN DOCUMENT ILLISIBLE EST MARQUÉ, pour la même raison qu'au-dessus :
        /// le laisser dans la file le ferait relire à chaque réveil.
        /// </summary>
        private async Task<int> TranscrireCoursAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var controles = scope.ServiceProvider.GetRequiredService<IControleScolaireRepository>();

            var aFaire = await controles.GetDocumentsATranscrireAsync(TailleLot, ct);
            if (aFaire.Count == 0) return 0;

            var marques = 0;

            foreach (var document in aFaire)
            {
                if (ct.IsCancellationRequested) return marques;
                if (document.Donnees is not { Length: > 0 }) continue;

                try
                {
                    var texte = await _transcripteur.TranscrireAsync(
                        document.Donnees, document.TypeMime, document.NomFichier,
                        "transcription-cours-controle", ct);

                    if (string.IsNullOrWhiteSpace(texte))
                    {
                        _logger.LogWarning(
                            "Cours du controle {ControleId}, document {Id} : transcription VIDE.",
                            document.ControleId, document.Id);
                        texte = "(document illisible : transcription automatique vide)";
                    }

                    await controles.EnregistrerTranscriptionDocumentAsync(
                        document.Id, texte.Trim(), DateTime.UtcNow, ct);
                    marques++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return marques;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Transcription impossible pour le cours {DocumentId}.", document.Id);

                    // Compté : au troisième échec, le document est abandonné et
                    // ses octets partent — il ne reste pas en base pour toujours.
                    if (await controles.SignalerEchecLectureDocumentAsync(document.Id, DateTime.UtcNow, ct))
                    {
                        _logger.LogWarning(
                            "Cours du controle {ControleId}, document {Id} : ABANDONNE apres trois lectures en echec.",
                            document.ControleId, document.Id);
                        marques++;
                    }
                }
            }

            _logger.LogInformation(
                "{Marques}/{Lus} page(s) de cours transcrite(s).", marques, aFaire.Count);

            return marques;
        }
    }
}
