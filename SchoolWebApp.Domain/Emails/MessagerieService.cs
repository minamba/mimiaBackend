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

        public async Task<ResultatBoite> ListerAsync(CancellationToken ct = default)
        {
            if (!Disponible) return new ResultatBoite([], "La boite n'est pas configuree.");

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = client.Inbox;
                await boite.OpenAsync(FolderAccess.ReadOnly, ct);

                var total = boite.Count;
                if (total == 0) return new ResultatBoite([]);

                // Les N derniers, dans l'ordre d'arrivée : une boîte de support
                // se lit par le haut, et rapatrier des années de courrier à
                // chaque ouverture d'écran serait absurde.
                var depuis = Math.Max(0, total - _options.TailleListe);

                var resumes = await boite.FetchAsync(
                    depuis,
                    -1,
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
                return new ResultatBoite(messages);
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
            uint identifiant, bool afficherImages = false, CancellationToken ct = default)
        {
            if (!Disponible) return null;

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = client.Inbox;

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
                    html = Nettoyer(html!);

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
                            Taille = 0,
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
            uint identifiant, string nomFichier, CancellationToken ct = default)
        {
            if (!Disponible) return null;

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = client.Inbox;
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
            CancellationToken ct = default)
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

                // Le corps est composé À PARTIR du message déjà en main : plus
                // aucune relecture, donc plus aucune connexion supplémentaire.
                var texte = await composer(new MessageDetail
                {
                    Identifiant = identifiant,
                    DeNom = origine.From.Mailboxes.FirstOrDefault()?.Name,
                    DeAdresse = origine.From.Mailboxes.FirstOrDefault()?.Address,
                    Sujet = origine.Subject,
                    Date = origine.Date.UtcDateTime,
                    Lu = true,
                    Texte = origine.TextBody,
                });

                var destinataire = origine.From.Mailboxes.FirstOrDefault();

                if (destinataire is null)
                {
                    return new ResultatEnvoi(false, Detail: "Le message d'origine n'a pas d'expediteur lisible.");
                }

                var reponse = new MimeMessage();
                reponse.From.Add(new MailboxAddress(
                    _options.Nom ?? "Support Mimia", _options.Adresse!));
                reponse.To.Add(destinataire);

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

                // LE LOGO, EN PIÈCE LIÉE ET NON EN BASE64.
                //
                // Gmail et Outlook ignorent les images en `data:` — le logo
                // n'apparaîtrait tout simplement pas chez la moitié des
                // parents, et l'entête du message resterait vide.
                await using (var flux = typeof(MessagerieService).Assembly
                    .GetManifestResourceStream("SchoolWebApp.Domain.Emails.Ressources.logo.png"))
                {
                    if (flux is not null)
                    {
                        var image = corps.LinkedResources.Add("logo.png", flux);
                        image.ContentId = "logoMimia";
                    }
                }

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

        public async Task MarquerLuAsync(uint identifiant, bool lu, CancellationToken ct = default)
        {
            if (!Disponible) return;

            try
            {
                using var client = await ConnecterAsync(ct);
                var boite = client.Inbox;
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
                var envoyes = client.GetFolder(SpecialFolder.Sent);

                if (envoyes is null)
                {
                    var noms = new[] { "Sent", "INBOX.Sent", "Sent Items", "Envoyés" };

                    foreach (var nom in noms)
                    {
                        try
                        {
                            envoyes = await client.GetFolderAsync(nom, ct);
                            if (envoyes is not null) break;
                        }
                        catch (FolderNotFoundException) { }
                    }
                }

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

        private static string VersTexte(string html) =>
            Regex.Replace(
                Regex.Replace(html, "<br\\s*/?>|</p>", "\n", RegexOptions.IgnoreCase),
                "<[^>]+>", string.Empty).Trim();
    }
}
