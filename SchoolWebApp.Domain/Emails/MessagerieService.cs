using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// La boîte de support, lue en IMAP et répondue en SMTP.
    ///
    /// UNE CONNEXION PAR OPÉRATION, ET C'EST ASSUMÉ
    /// --------------------------------------------
    /// Garder une connexion IMAP ouverte entre deux requêtes HTTP obligerait à
    /// gérer sa mort — le serveur ferme au bout de quelques minutes
    /// d'inactivité — et à sérialiser les accès, un client IMAP n'étant pas
    /// utilisable depuis deux fils à la fois. Pour un écran consulté quelques
    /// fois par jour, reconnecter coûte une seconde et supprime toute cette
    /// mécanique.
    /// </summary>
    public class MessagerieService : IMessagerieService
    {
        /// <summary>
        /// Ce qu'on retire du HTML reçu avant de l'afficher.
        ///
        /// LES BALISES ENTIÈRES, CONTENU COMPRIS. Retirer `&lt;script&gt;` sans son
        /// contenu laisserait le code JavaScript en clair dans la page — où il
        /// ne s'exécuterait pas, mais s'afficherait, ce qui est presque aussi
        /// mauvais : on croit à un message corrompu.
        /// </summary>
        private static readonly Regex BalisesDangereuses = new(
            @"<(script|iframe|object|embed|form|link|meta)\b[^>]*>.*?</\1\s*>|<(script|iframe|object|embed|form|link|meta)\b[^>]*/?>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Les attributs d'événement : `onclick`, `onerror`, `onload`…
        ///
        /// C'est par `onerror` sur une image cassée qu'on exécute du script sans
        /// jamais écrire le mot `script`.
        /// </summary>
        private static readonly Regex AttributsEvenement = new(
            @"\son[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Les adresses `javascript:` dans les liens.</summary>
        private static readonly Regex ProtocoleScript = new(
            @"(href|src)\s*=\s*(""|')\s*javascript:[^""']*(""|')",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Les images en pièce liée (`cid:`) des messages cités.
        ///
        /// UN NAVIGATEUR NE SAIT PAS LES RÉSOUDRE. Quand un parent répond
        /// depuis Gmail, son client recopie notre courriel en citation — logo
        /// compris, sous forme de `cid:logoMimia` qui ne pointe vers rien hors
        /// du message d'origine. On voyait donc une icône d'image brisée au
        /// milieu de chaque réponse, ce qui ressemble à un défaut de notre
        /// écran alors que c'est le fonctionnement normal du format.
        ///
        /// La balise entière est retirée : un logo manquant dans une citation
        /// n'apporte rien, et laisser un cadre vide serait pire.
        /// </summary>
        private static readonly Regex ImageLiee = new(
            @"<img\b[^>]*\ssrc\s*=\s*(""|')\s*cid:[^""']*(""|')[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Les sources d'images distantes, neutralisées par défaut.</summary>
        private static readonly Regex SourceImage = new(
            @"<img\b([^>]*?)\ssrc\s*=\s*(""|')(https?://[^""']*)(""|')",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly OptionsMessagerie _options;
        private readonly ILogger<MessagerieService> _logger;

        public MessagerieService(
            IOptions<OptionsMessagerie> options, ILogger<MessagerieService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Disponible => _options.Configure;

        public IMessagerieService Pour(CompteMessagerie compte) =>
            new MessagerieService(Options.Create(_options.AvecCompte(compte)), _logger);

        public async Task<string?> VerifierAsync(CancellationToken ct = default)
        {
            if (!Disponible) return "La boite n'est pas configuree.";
            try
            {
                using var client = await ConnecterAsync(ct);
                await client.DisconnectAsync(true, CancellationToken.None);
                return null;
            }
            catch (MailKit.Security.AuthenticationException)
            {
                return "Adresse ou mot de passe refusé par la boîte mail.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Messagerie : verification de la boite {Adresse} impossible.", _options.Adresse);
                return $"La boîte mail ne répond pas : {ex.Message}";
            }
        }

        public async Task<ResultatBoite> ListerAsync(
            string? dossier = null, int page = 1, int taille = 10, CancellationToken ct = default)
        {
            if (!Disponible) return new ResultatBoite([], "La boite n'est pas configuree.");

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = await DossierAsync(client, dossier, ct);
                if (boite is null) return new ResultatBoite([], "Le dossier des messages envoyés est introuvable dans la boîte.");

                // Les non lus de TOUT le dossier, pas de la seule page : demandés
                // avant l'ouverture (STATUS ne se pose pas sur le dossier ouvert).
                var nonLus = 0;
                if (boite == client.Inbox)
                {
                    await boite.StatusAsync(StatusItems.Unread, ct);
                    nonLus = boite.Unread;
                }

                await boite.OpenAsync(FolderAccess.ReadOnly, ct);

                var total = boite.Count;
                if (total == 0) return new ResultatBoite([], Total: 0, NonLus: nonLus);

                // UNE PAGE, LES PLUS RÉCENTS D'ABORD (17/10/2026 : dix par page).
                // Les numéros IMAP suivent l'ordre d'arrivée : la page 1 est la
                // fin du dossier. Rapatrier toute la boîte à chaque ouverture
                // d'écran serait absurde.
                taille = Math.Clamp(taille, 1, 100);
                var fin = total - 1 - (Math.Max(1, page) - 1) * taille;
                if (fin < 0) return new ResultatBoite([], Total: total, NonLus: nonLus);
                var depuis = Math.Max(0, fin - taille + 1);

                var resumes = await boite.FetchAsync(
                    depuis,
                    fin,
                    MessageSummaryItems.UniqueId
                        | MessageSummaryItems.Envelope
                        | MessageSummaryItems.Flags
                        | MessageSummaryItems.BodyStructure
                        | MessageSummaryItems.PreviewText,
                    ct);

                var messages = resumes
                    .Select(r => new MessageRecu
                    {
                        Identifiant = r.UniqueId.Id,
                        DeNom = Expediteur(r.Envelope)?.Name,
                        DeAdresse = Expediteur(r.Envelope)?.Address,
                        RepondreA = AutreAdresse(r.Envelope?.ReplyTo?.Mailboxes?.FirstOrDefault(), Expediteur(r.Envelope)),
                        ANom = r.Envelope?.To?.Mailboxes?.FirstOrDefault()?.Name,
                        AAdresse = r.Envelope?.To?.Mailboxes?.FirstOrDefault()?.Address,
                        Sujet = r.Envelope?.Subject,
                        Date = (r.Envelope?.Date ?? DateTimeOffset.UtcNow).UtcDateTime,
                        Lu = r.Flags?.HasFlag(MessageFlags.Seen) ?? false,
                        Repondu = r.Flags?.HasFlag(MessageFlags.Answered) ?? false,
                        Extrait = Raccourcir(r.PreviewText, 160),
                        AvecPiecesJointes = r.Attachments?.Any() ?? false,
                    })
                    .OrderByDescending(m => m.Date)
                    .ToList();

                await client.DisconnectAsync(true, CancellationToken.None);
                return new ResultatBoite(messages, Total: total, NonLus: nonLus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messagerie : lecture de la boite impossible.");

                // LE MESSAGE DE L'EXCEPTION REMONTE JUSQU'A L'ECRAN.
                //
                // Il est technique, et c'est voulu : seul un administrateur
                // voit cet ecran, et « AuthenticationException » lui dit ou
                // chercher la ou « une erreur est survenue » l'enverrait lire
                // des journaux sur le serveur.
                return new ResultatBoite([], ex.Message);
            }
        }

        public async Task<MessageDetail?> LireAsync(
            uint identifiant, bool afficherImages = false, CancellationToken ct = default, string? dossier = null)
        {
            if (!Disponible) return null;

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = await DossierAsync(client, dossier, ct);
                if (boite is null) return null;

                // Lecture-écriture : ouvrir un message le marque lu, et c'est
                // ce qu'on attend d'une messagerie.
                await boite.OpenAsync(FolderAccess.ReadWrite, ct);

                var uid = new UniqueId(identifiant);
                var message = await boite.GetMessageAsync(uid, ct);

                if (message is null) return null;

                var html = message.HtmlBody;
                var bloquees = false;

                if (!string.IsNullOrWhiteSpace(html))
                {
                    // LES IMAGES DU MESSAGE LUI-MÊME, À LEUR PLACE (Camara, le
                    // 18/10/2026 : « les pièces que j'ai mises ne s'affichent pas »).
                    // Une image en pièce liée (cid:) que le message contient est
                    // incorporée ; seules celles qu'il ne contient pas — le logo
                    // d'un courriel cité — sont retirées par Nettoyer, comme avant.
                    html = IncorporerImagesLiees(html!, message);
                    html = Nettoyer(html);

                    if (!afficherImages)
                    {
                        var avant = html;
                        html = SourceImage.Replace(html, "<img$1 data-src-bloquee=$2$3$4");
                        bloquees = !ReferenceEquals(avant, html) && avant != html;
                    }
                }

                var detail = new MessageDetail
                {
                    Identifiant = identifiant,
                    DeNom = message.From.Mailboxes.FirstOrDefault()?.Name,
                    DeAdresse = message.From.Mailboxes.FirstOrDefault()?.Address,
                    RepondreA = AutreAdresse(message.ReplyTo.Mailboxes.FirstOrDefault(), message.From.Mailboxes.FirstOrDefault()),
                    ANom = message.To.Mailboxes.FirstOrDefault()?.Name,
                    AAdresse = message.To.Mailboxes.FirstOrDefault()?.Address,
                    Destinataires = message.To.Mailboxes.Select(m => new AdresseMail(m.Name, m.Address)).ToList(),
                    Copies = message.Cc.Mailboxes.Select(m => new AdresseMail(m.Name, m.Address)).ToList(),
                    Sujet = message.Subject,
                    Date = message.Date.UtcDateTime,
                    Lu = true,
                    Html = html,
                    Texte = message.TextBody,
                    ImagesBloquees = bloquees,
                    PiecesJointes = message.Attachments
                        .OfType<MimePart>()
                        .Select(p => new PieceJointeRecue
                        {
                            NomFichier = p.FileName ?? "document",
                            TypeMime = p.ContentType?.MimeType ?? "application/octet-stream",
                            Taille = TailleDe(p),
                        })
                        .ToList(),
                };

                detail.AvecPiecesJointes = detail.PiecesJointes.Count > 0;

                await boite.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
                await client.DisconnectAsync(true, CancellationToken.None);

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messagerie : lecture du message {Id} impossible.", identifiant);
                return null;
            }
        }

        public async Task<(byte[] Donnees, string TypeMime, string NomFichier)?> PieceJointeAsync(
            uint identifiant, string nomFichier, CancellationToken ct = default, string? dossier = null)
        {
            if (!Disponible) return null;

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = await DossierAsync(client, dossier, ct);
                if (boite is null) return null;
                await boite.OpenAsync(FolderAccess.ReadOnly, ct);

                var message = await boite.GetMessageAsync(new UniqueId(identifiant), ct);
                if (message is null) return null;

                var piece = message.Attachments
                    .OfType<MimePart>()
                    .FirstOrDefault(p => string.Equals(
                        p.FileName, nomFichier, StringComparison.OrdinalIgnoreCase));

                if (piece is null) return null;

                using var memoire = new MemoryStream();
                await piece.Content.DecodeToAsync(memoire, ct);

                await client.DisconnectAsync(true, CancellationToken.None);

                return (memoire.ToArray(),
                        piece.ContentType?.MimeType ?? "application/octet-stream",
                        piece.FileName ?? "document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Messagerie : piece jointe {Nom} du message {Id} illisible.", nomFichier, identifiant);
                return null;
            }
        }

        public async Task<ResultatEnvoi> RepondreAsync(
            uint identifiant,
            Func<MessageDetail, Task<string>> composer,
            CancellationToken ct = default,
            IReadOnlyList<string>? copies = null,
            IReadOnlyList<string>? copiesCachees = null)
        {
            if (!Disponible) return new ResultatEnvoi(false, Detail: "Boite non configuree.");

            try
            {
                MimeMessage origine;

                // UNE SEULE CONNEXION IMAP POUR TOUTE LA RÉPONSE.
                //
                // Le contrôleur lisait le message avant d'appeler cette méthode,
                // qui le relisait ensuite : trois connexions par réponse en
                // comptant le SMTP. Les hébergeurs mutualisés en bornent le
                // nombre par compte, et la deuxième réponse envoyée coup sur
                // coup échouait — sans que rien n'arrive au parent.
                using var imap = await ConnecterAsync(ct);
                var boite = imap.Inbox;
                await boite.OpenAsync(FolderAccess.ReadWrite, ct);

                var uid = new UniqueId(identifiant);
                origine = await boite.GetMessageAsync(uid, ct);

                if (origine is null)
                {
                    return new ResultatEnvoi(false, Detail: "Message d'origine introuvable.");
                }

                // Le drapeau « répondu » est posé AVANT l'envoi.
                //
                // Si l'envoi échoue on aura un message marqué à tort, ce qui
                // se corrige d'un clic. Dans l'autre ordre, un envoi réussi
                // suivi d'une erreur de drapeau laisserait croire qu'il
                // reste à répondre — et le parent recevrait deux réponses.
                await boite.AddFlagsAsync(uid, MessageFlags.Answered | MessageFlags.Seen, true, ct);

                // À QUI RÉPONDRE (18/10/2026) : « Répondre à » d'abord, comme tout
                // client de messagerie — le formulaire de contact l'y met, son
                // expéditeur étant no-reply@mimia.fr.
                var destinataire = origine.ReplyTo.Mailboxes.FirstOrDefault()
                                   ?? origine.From.Mailboxes.FirstOrDefault();

                if (destinataire is null)
                {
                    await boite.RemoveFlagsAsync(uid, MessageFlags.Answered, true, CancellationToken.None);
                    return new ResultatEnvoi(false, Detail: "Le message d'origine n'a pas d'expediteur lisible.");
                }

                // UNE ADRESSE QUI NE REÇOIT RIEN : on refuse, au lieu d'envoyer dans le vide.
                if (NeRecoitPas(destinataire.Address))
                {
                    await boite.RemoveFlagsAsync(uid, MessageFlags.Answered, true, CancellationToken.None);
                    return new ResultatEnvoi(false, Detail:
                        $"Ce message vient de {destinataire.Address}, une adresse qui ne reçoit pas de réponse. "
                        + "S'il vient du formulaire de contact, l'adresse de la personne est écrite dans le message : "
                        + "écrivez-lui avec « Écrire un nouveau message ».");
                }

                // Le corps est composé À PARTIR du message déjà en main : plus
                // aucune relecture, donc plus aucune connexion supplémentaire.
                var texte = await composer(new MessageDetail
                {
                    Identifiant = identifiant,
                    DeNom = destinataire.Name,
                    DeAdresse = destinataire.Address,
                    Sujet = origine.Subject,
                    Date = origine.Date.UtcDateTime,
                    Lu = true,
                    Texte = origine.TextBody,
                });

                var reponse = new MimeMessage();
                reponse.From.Add(new MailboxAddress(
                    _options.Nom ?? "Support Mimia", _options.Adresse!));
                reponse.To.Add(destinataire);

                // LES COPIES (19/10/2026) — « Répondre à tous » ou ajoutées à la
                // main. Jamais le destinataire principal, ni la boîte elle-même,
                // ni une adresse qui ne reçoit pas.
                bool EnCopie(string adresse) =>
                    !string.Equals(adresse, destinataire.Address, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(adresse, _options.Adresse, StringComparison.OrdinalIgnoreCase)
                    && !NeRecoitPas(adresse);
                foreach (var enCopie in (copies ?? []).Where(EnCopie)) reponse.Cc.Add(MailboxAddress.Parse(enCopie));
                foreach (var cachee in (copiesCachees ?? []).Where(EnCopie)) reponse.Bcc.Add(MailboxAddress.Parse(cachee));

                reponse.Subject = origine.Subject?.StartsWith("Re", StringComparison.OrdinalIgnoreCase) == true
                    ? origine.Subject
                    : $"Re : {origine.Subject}";

                // LES ENTÊTES QUI TIENNENT LE FIL.
                //
                // Sans elles, la réponse arrive détachée : le parent reçoit un
                // message isolé, sans le sien au-dessus, et ne sait pas de quoi
                // on lui parle. `References` accumule toute la chaîne — c'est
                // elle qui permet à un client de reconstituer une conversation
                // de dix échanges.
                if (!string.IsNullOrWhiteSpace(origine.MessageId))
                {
                    reponse.InReplyTo = origine.MessageId;

                    foreach (var reference in origine.References)
                    {
                        reponse.References.Add(reference);
                    }

                    reponse.References.Add(origine.MessageId);
                }

                var corps = new BodyBuilder
                {
                    HtmlBody = texte,
                    TextBody = VersTexte(texte),
                };

                // LA BANNIÈRE, EN PIÈCE LIÉE ET NON EN BASE64.
                //
                // Gmail et Outlook ignorent les images en `data:` — la bannière
                // n'apparaîtrait tout simplement pas chez la moitié des
                // parents, et l'entête du message resterait vide.
                await using (var flux = typeof(MessagerieService).Assembly
                    .GetManifestResourceStream("SchoolWebApp.Domain.Emails.Ressources.bann_mail.jpg"))
                {
                    if (flux is not null)
                    {
                        var image = corps.LinkedResources.Add("bann_mail.jpg", flux);
                        image.ContentId = "logoMimia";
                    }
                }

                // Les images de la signature, jointes de la même façon.
                ImagesSignature.Attacher(corps);

                reponse.Body = corps.ToMessageBody();

                using var smtp = new SmtpClient();

                var securite = _options.PortSmtp == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(_options.ServeurEnvoi, _options.PortSmtp, securite, ct);
                await smtp.AuthenticateAsync(_options.IdentifiantEnvoi, _options.SecretEnvoi, ct);

                // LE DRAPEAU EST RETIRÉ SI L'ENVOI ÉCHOUE.
                //
                // Il est posé AVANT pour ne pas répondre deux fois si le
                // marquage échoue après un envoi réussi. Mais l'envoi peut
                // échouer pour de bon — l'hébergeur bloque l'adresse vingt
                // minutes au-delà d'un certain débit — et le message resterait
                // alors marqué « répondu » sans que le parent ait rien reçu.
                // C'est le pire des deux mondes : on ne le rappellerait jamais.

                // LA RÉPONSE DU SERVEUR EST JOURNALISÉE, PAS SEULEMENT LE FAIT
                // D'AVOIR ENVOYÉ.
                //
                // `SendAsync` rend la réponse SMTP finale — souvent un
                // identifiant de file d'attente. C'est LUI qu'on cite à
                // l'hébergeur quand un message accepté n'arrive jamais : sans
                // cette ligne, on ne peut rien prouver et le support renvoie
                // chercher ailleurs.
                string accuse;

                try
                {
                    accuse = await smtp.SendAsync(reponse, ct);
                }
                catch
                {
                    try
                    {
                        await boite.RemoveFlagsAsync(
                            uid, MessageFlags.Answered, true, CancellationToken.None);
                    }
                    catch (Exception retrait)
                    {
                        _logger.LogWarning(retrait,
                            "Messagerie : drapeau « repondu » non retire apres un envoi echoue. "
                            + "Le message {Id} apparait repondu a tort.", identifiant);
                    }

                    throw;
                }

                await smtp.DisconnectAsync(true, CancellationToken.None);

                _logger.LogWarning(
                    "Messagerie : reponse envoyee a {Destinataire} depuis {Expediteur}. "
                    + "Reponse du serveur : {Accuse}",
                    destinataire.Address, _options.Adresse, accuse);

                // UNE COPIE DANS « ENVOYÉS », comme le ferait n'importe quel
                // client de messagerie.
                //
                // Sans elle, une réponse partie de l'application n'existe nulle
                // part : ni dans le webmail, ni dans la boîte. On ne peut ni
                // vérifier ce qu'on a écrit, ni prouver qu'on a répondu — c'est
                // exactement ce qui manquait pour comprendre où passaient les
                // messages.
                var copie = await DeposerDansEnvoyesAsync(imap, reponse, ct);

                return new ResultatEnvoi(
                    Envoye: true,
                    Destinataire: destinataire.Address,
                    Accuse: accuse,
                    CopieDeposee: copie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messagerie : reponse au message {Id} impossible.", identifiant);

                // LE MESSAGE DE L'EXCEPTION REMONTE À L'ÉCRAN.
                //
                // « La réponse n'a pas pu être envoyée » n'apprend rien à
                // personne. `SmtpCommandException` avec son code, ou
                // `AuthenticationException`, disent où chercher — et seul un
                // administrateur voit cet écran.
                return new ResultatEnvoi(false, Detail: $"{ex.GetType().Name} : {ex.Message}");
            }
        }

        public async Task MarquerLuAsync(uint identifiant, bool lu, CancellationToken ct = default, string? dossier = null)
        {
            if (!Disponible) return;

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = await DossierAsync(client, dossier, ct);
                if (boite is null) return;
                await boite.OpenAsync(FolderAccess.ReadWrite, ct);

                var uid = new UniqueId(identifiant);

                if (lu) await boite.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
                else await boite.RemoveFlagsAsync(uid, MessageFlags.Seen, true, ct);

                await client.DisconnectAsync(true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messagerie : marquage du message {Id} impossible.", identifiant);
            }
        }

        public async Task<CompteursBoite> CompterAsync(CancellationToken ct = default)
        {
            if (!Disponible) return new CompteursBoite(0, 0, 0, "La boite n'est pas configuree.");

            try
            {
                using var client = await ConnecterAsync(ct);

                // STATUS : les nombres seuls, sans ouvrir ni lire aucun message.
                await client.Inbox.StatusAsync(StatusItems.Count | StatusItems.Unread, ct);
                var envoyes = await DossierAsync(client, DossiersMessagerie.Envoyes, ct);
                if (envoyes is not null) await envoyes.StatusAsync(StatusItems.Count, ct);

                var compteurs = new CompteursBoite(client.Inbox.Count, client.Inbox.Unread, envoyes?.Count ?? 0);
                await client.DisconnectAsync(true, CancellationToken.None);
                return compteurs;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Messagerie : compteurs de la boite {Adresse} illisibles.", _options.Adresse);
                return new CompteursBoite(0, 0, 0, ex.Message);
            }
        }

        public async Task<int> SupprimerAsync(
            IReadOnlyCollection<uint> identifiants, string? dossier = null, CancellationToken ct = default)
        {
            if (!Disponible || identifiants.Count == 0) return 0;

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = await DossierAsync(client, dossier, ct);
                if (boite is null) return 0;

                await boite.OpenAsync(FolderAccess.ReadWrite, ct);
                var uids = identifiants.Distinct().Select(i => new UniqueId(i)).ToList();

                // À LA CORBEILLE, PAS À LA BROYEUSE : un clic de trop se rattrape
                // depuis le webmail. Effacé pour de bon seulement sans corbeille.
                var corbeille = await DossierSpecialAsync(client, SpecialFolder.Trash,
                    ["Trash", "INBOX.Trash", "Corbeille", "Deleted Items", "Deleted Messages"], ct);

                if (corbeille is not null && corbeille.FullName != boite.FullName)
                {
                    await boite.MoveToAsync(uids, corbeille, ct);
                }
                else
                {
                    await boite.AddFlagsAsync(uids, MessageFlags.Deleted, true, ct);
                    await boite.ExpungeAsync(uids, ct);
                }

                await client.DisconnectAsync(true, CancellationToken.None);

                _logger.LogWarning(
                    "Messagerie : {Nombre} message(s) supprime(s) du dossier {Dossier} de {Adresse}{Corbeille}.",
                    uids.Count, boite.FullName, _options.Adresse, corbeille is null ? " (efface, pas de corbeille)" : " (vers la corbeille)");
                return uids.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messagerie : suppression impossible dans {Dossier}.", dossier);
                throw;
            }
        }

        public async Task<bool> DeposerCopieAsync(
            string destinataire,
            string sujet,
            string html,
            IReadOnlyList<PieceMail>? imagesIntegrees = null,
            IReadOnlyList<PieceMail>? piecesJointes = null,
            CancellationToken ct = default,
            IReadOnlyList<string>? copies = null,
            IReadOnlyList<string>? copiesCachees = null)
        {
            if (!Disponible) return false;

            try
            {
                var copie = new MimeMessage();
                copie.From.Add(new MailboxAddress(_options.Nom ?? "Mimia", _options.Adresse!));
                copie.To.Add(MailboxAddress.Parse(destinataire));
                // Comme dans tout client : la copie d'« Envoyés » montre aussi
                // les copies et les copies cachées (19/10/2026).
                foreach (var c in copies ?? []) copie.Cc.Add(MailboxAddress.Parse(c));
                foreach (var c in copiesCachees ?? []) copie.Bcc.Add(MailboxAddress.Parse(c));
                copie.Subject = sujet;
                copie.Date = DateTimeOffset.Now;

                // Les mêmes pièces liées que le message parti : la copie se relit
                // telle que le parent l'a reçue.
                var corps = new BodyBuilder { HtmlBody = html, TextBody = VersTexte(html) };
                await using (var flux = typeof(MessagerieService).Assembly
                    .GetManifestResourceStream("SchoolWebApp.Domain.Emails.Ressources.bann_mail.jpg"))
                {
                    if (flux is not null)
                    {
                        var image = corps.LinkedResources.Add("bann_mail.jpg", flux);
                        image.ContentId = "logoMimia";
                    }
                }
                ImagesSignature.Attacher(corps);
                foreach (var image in imagesIntegrees ?? [])
                {
                    var liee = corps.LinkedResources.Add(image.NomFichier, image.Donnees, ContentType.Parse(image.TypeMime));
                    liee.ContentId = image.Reference;
                }
                foreach (var piece in piecesJointes ?? [])
                {
                    corps.Attachments.Add(piece.NomFichier, piece.Donnees, ContentType.Parse(piece.TypeMime));
                }
                copie.Body = corps.ToMessageBody();

                using var client = await ConnecterAsync(ct);
                var depose = await DeposerDansEnvoyesAsync(client, copie, ct);
                await client.DisconnectAsync(true, CancellationToken.None);
                return depose;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Messagerie : copie de « {Sujet} » non deposee dans Envoyes. Le message est bien parti.", sujet);
                return false;
            }
        }

        /// <summary>Le dossier IMAP d'un dossier de l'écran : la réception, ou « Envoyés ».</summary>
        private async Task<IMailFolder?> DossierAsync(ImapClient client, string? dossier, CancellationToken ct) =>
            DossiersMessagerie.Normaliser(dossier) == DossiersMessagerie.Envoyes
                ? await DossierSpecialAsync(client, SpecialFolder.Sent, ["Sent", "INBOX.Sent", "Sent Items", "Envoyés"], ct)
                : client.Inbox;

        /// <summary>
        /// Un dossier spécial : celui que le serveur déclare d'abord — son NOM varie
        /// selon la langue de l'hébergeur —, sinon les noms courants.
        /// </summary>
        private static async Task<IMailFolder?> DossierSpecialAsync(
            ImapClient client, SpecialFolder special, string[] noms, CancellationToken ct)
        {
            IMailFolder? trouve = null;
            try { trouve = client.GetFolder(special); }
            catch (NotSupportedException) { }

            if (trouve is not null) return trouve;

            foreach (var nom in noms)
            {
                try
                {
                    trouve = await client.GetFolderAsync(nom, ct);
                    if (trouve is not null) return trouve;
                }
                catch (FolderNotFoundException) { }
            }

            return null;
        }

        /// <summary>
        /// Dépose une copie du message envoyé dans le dossier « Envoyés ».
        ///
        /// TOLÉRANTE À L'ÉCHEC, ET C'EST VOULU. Le message est déjà parti chez
        /// le parent ; si le dépôt échoue — dossier nommé autrement, droits
        /// refusés — on perd une trace, pas une réponse. Faire échouer l'appel
        /// ferait croire à l'administrateur que rien n'a été envoyé, et il
        /// écrirait deux fois.
        /// </summary>
        private async Task<bool> DeposerDansEnvoyesAsync(
            ImapClient client, MimeMessage message, CancellationToken ct)
        {
            try
            {
                // Le dossier spécial d'abord : c'est le serveur qui déclare
                // lequel il considère comme « Envoyés », et son NOM varie selon
                // la langue de l'hébergeur — « Sent », « Sent Items »,
                // « Éléments envoyés ».
                var envoyes = await DossierSpecialAsync(client, SpecialFolder.Sent,
                    ["Sent", "INBOX.Sent", "Sent Items", "Envoyés"], ct);

                if (envoyes is null)
                {
                    _logger.LogWarning(
                        "Messagerie : aucun dossier « Envoyes » trouve, copie non deposee.");
                    return false;
                }

                await envoyes.AppendAsync(message, MessageFlags.Seen, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Messagerie : copie dans « Envoyes » impossible. La reponse est bien partie.");
                return false;
            }
        }

        private async Task<ImapClient> ConnecterAsync(CancellationToken ct)
        {
            var client = new ImapClient();

            await client.ConnectAsync(
                _options.Hote!, _options.PortImap, SecureSocketOptions.SslOnConnect, ct);

            await client.AuthenticateAsync(_options.Adresse!, _options.MotDePasse!, ct);

            return client;
        }

        private static MailboxAddress? Expediteur(Envelope? enveloppe) =>
            enveloppe?.From?.Mailboxes?.FirstOrDefault();

        /// <summary>
        /// Remplace chaque `cid:` par l'image que le message porte sous cet
        /// identifiant, en base64 — le navigateur ne sait pas résoudre `cid:`.
        /// Plafonné à 10 Mo d'images : au-delà, elles restent à télécharger.
        /// </summary>
        private static string IncorporerImagesLiees(string html, MimeMessage message)
        {
            var total = 0L;
            foreach (var partie in message.BodyParts.OfType<MimePart>())
            {
                if (string.IsNullOrEmpty(partie.ContentId) || partie.ContentType?.MediaType != "image") continue;
                if (!html.Contains($"cid:{partie.ContentId}", StringComparison.OrdinalIgnoreCase)) continue;

                using var memoire = new MemoryStream();
                partie.Content.DecodeTo(memoire);
                total += memoire.Length;
                if (total > 10 * 1024 * 1024) break;

                html = Regex.Replace(html, $"cid:{Regex.Escape(partie.ContentId)}",
                    $"data:{partie.ContentType.MimeType};base64,{Convert.ToBase64String(memoire.ToArray())}",
                    RegexOptions.IgnoreCase);
            }

            return html;
        }

        /// <summary>La taille d'une pièce jointe, une fois décodée.</summary>
        private static long TailleDe(MimePart partie)
        {
            try
            {
                using var memoire = new MemoryStream();
                partie.Content?.DecodeTo(memoire);
                return memoire.Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>« Répondre à », seulement s'il diffère de l'expéditeur.</summary>
        private static string? AutreAdresse(MailboxAddress? repondreA, MailboxAddress? expediteur) =>
            repondreA is null || string.Equals(repondreA.Address, expediteur?.Address, StringComparison.OrdinalIgnoreCase)
                ? null
                : repondreA.Address;

        /// <summary>Une adresse qui ne lit pas ses courriels : no-reply, ou la boîte elle-même.</summary>
        private bool NeRecoitPas(string? adresse)
        {
            if (string.IsNullOrWhiteSpace(adresse)) return true;
            var locale = adresse.Split('@')[0].ToLowerInvariant().Replace("-", "").Replace("_", "").Replace(".", "");
            return locale is "noreply" or "donotreply" or "nepasrepondre"
                   || string.Equals(adresse, _options.Adresse, StringComparison.OrdinalIgnoreCase);
        }

        private static string? Raccourcir(string? texte, int taille)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = Regex.Replace(texte, @"\s+", " ").Trim();
            return propre.Length <= taille ? propre : propre[..taille] + "…";
        }

        /// <summary>
        /// Retire d'un HTML reçu ce qui peut s'exécuter.
        ///
        /// PREMIÈRE DES DEUX BARRIÈRES. L'écran affiche en plus le résultat
        /// dans un cadre isolé sans droit d'exécution. Aucune des deux ne
        /// suffirait seule : ce nettoyage est une liste de motifs, donc
        /// faillible par construction, et un cadre isolé mal configuré est
        /// une case à cocher qu'on oublie.
        /// </summary>
        private static string Nettoyer(string html)
        {
            var propre = BalisesDangereuses.Replace(html, string.Empty);
            propre = ImageLiee.Replace(propre, string.Empty);
            propre = AttributsEvenement.Replace(propre, string.Empty);
            propre = ProtocoleScript.Replace(propre, "$1=\"#\"");

            return propre;
        }

        /// <summary>La version texte : la même conversion que tous les autres envois (18/10/2026).</summary>
        private static string VersTexte(string html) => TexteBrut.Depuis(html);
    }
}
