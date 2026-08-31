using System.Reflection;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// Envoi des mails transactionnels.
    ///
    /// Les gabarits sont embarqués dans l'assembly plutôt que lus sur le disque :
    /// deux applications distinctes (l'API et le serveur d'identité) envoient des
    /// mails, et un chemin relatif casserait selon celle qui s'exécute.
    /// </summary>
    public class ServiceEmail : IServiceEmail
    {
        private static readonly Regex Marqueur = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
        private static readonly Assembly Assemblee = typeof(ServiceEmail).Assembly;

        /// <summary>Gabarits déjà lus. Un mail hebdomadaire relit sinon le même fichier par parent.</summary>
        private static readonly Dictionary<string, string> Cache = new();
        private static readonly SemaphoreSlim Verrou = new(1, 1);

        private readonly OptionsEmail _options;
        private readonly ILogger<ServiceEmail> _logger;

        public ServiceEmail(IOptions<OptionsEmail> options, ILogger<ServiceEmail> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Disponible => _options.Configure;

        public async Task<bool> EnvoyerAsync(
            string destinataire,
            string sujet,
            string gabarit,
            IDictionary<string, string> valeurs,
            CancellationToken ct = default)
        {
            if (!Disponible)
            {
                _logger.LogWarning(
                    "SMTP non configure : mail « {Sujet} » vers {Destinataire} non envoye.",
                    sujet, destinataire);
                return false;
            }

            // Détournement de développement : une exécution du bilan sur des
            // données de test enverrait sinon de vrais mails à de vraies adresses.
            var cible = string.IsNullOrWhiteSpace(_options.RedirectionDev)
                ? destinataire
                : _options.RedirectionDev;

            try
            {
                var corps = await ConstruireCorpsAsync(sujet, gabarit, valeurs);

                var message = new MimeMessage();
                // Le `!` est sûr : `Disponible` a déjà écarté les champs vides.
                message.From.Add(new MailboxAddress(_options.SenderNom ?? "Mimia", _options.SenderEmail!));
                message.To.Add(MailboxAddress.Parse(cible));
                message.Subject = sujet;

                var constructeur = new BodyBuilder { HtmlBody = corps, TextBody = VersTexte(corps) };

                // Logo attaché plutôt que lié à une URL : il s'affiche même sans
                // connexion, et échappe aux filtres qui bloquent les images
                // distantes par défaut.
                await using var flux = Assemblee.GetManifestResourceStream(
                    "SchoolWebApp.Domain.Emails.Ressources.logo.png");

                if (flux is not null)
                {
                    var image = constructeur.LinkedResources.Add("logo.png", flux);
                    image.ContentId = "logoMimia";
                    corps = corps.Replace("cid:logoMimia", $"cid:{image.ContentId}");
                    constructeur.HtmlBody = corps;
                }

                message.Body = constructeur.ToMessageBody();

                using var client = new SmtpClient();

                // 465 = SSL dès la connexion. 587 = connexion claire puis STARTTLS.
                // Se tromper laisse le client attendre une négociation qui ne vient pas.
                var securite = _options.SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(_options.SmtpHost!, _options.SmtpPort, securite, ct);
                await client.AuthenticateAsync(_options.Identifiant!, _options.SenderPassword!, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);

                _logger.LogInformation("Mail « {Sujet} » envoye a {Destinataire}.", sujet, cible);
                return true;
            }
            catch (Exception ex)
            {
                // Un mail perdu ne doit jamais faire échouer l'action qui l'a
                // déclenché : on ne refuse pas une inscription parce que le SMTP
                // est tombé.
                _logger.LogError(ex, "Echec d'envoi du mail « {Sujet} » a {Destinataire}.", sujet, cible);
                return false;
            }
        }

        public async Task<ResultatDiffusion> DiffuserAsync(
            IReadOnlyList<string> destinataires,
            string sujet,
            string gabarit,
            IDictionary<string, string> valeurs,
            IReadOnlyList<PieceMail>? imagesIntegrees = null,
            IReadOnlyList<PieceMail>? piecesJointes = null,
            IProgress<int>? avancement = null,
            CancellationToken ct = default)
        {
            if (!Disponible)
            {
                _logger.LogError(
                    "SMTP non configure : diffusion « {Sujet} » ABANDONNEE.", sujet);
                return new ResultatDiffusion(0, destinataires.Count);
            }

            // LE CORPS EST CONSTRUIT UNE FOIS, PAS PAR DESTINATAIRE.
            //
            // Il est identique pour tout le monde — c'est la définition d'une
            // diffusion. Le reconstruire trois cents fois relirait les gabarits
            // et referait les substitutions pour un résultat rigoureusement
            // égal.
            var corps = await ConstruireCorpsAsync(sujet, gabarit, valeurs);
            var texte = VersTexte(corps);

            var envoyes = 0;
            var echecs = 0;

            try
            {
                // LA MÊME SESSION QUE LES BILANS.
                //
                // Elle apporte ici ce qui manquait : la RECONNEXION. Une
                // diffusion vers deux cents parents dure plusieurs minutes, et
                // jusqu'à présent une connexion fermée en cours de route
                // perdait tout le reste de la liste — sans possibilité de
                // reprendre, puisque relancer aurait réécrit aux premiers.
                await using var session = new SessionEnvoi(this);

                foreach (var destinataire in destinataires)
                {
                    ct.ThrowIfCancellationRequested();

                    var cible = string.IsNullOrWhiteSpace(_options.RedirectionDev)
                        ? destinataire
                        : _options.RedirectionDev;

                    try
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(
                            _options.SenderNom ?? "Mimia", _options.SenderEmail!));

                        // UN MESSAGE PAR PERSONNE, jamais une copie collective :
                        // trois cents adresses dans la même enveloppe, ce serait
                        // le carnet d'adresses de tous les clients exposé à
                        // chacun d'eux.
                        message.To.Add(MailboxAddress.Parse(cible));
                        message.Subject = sujet;

                        // RECONSTRUIT À CHAQUE TOUR, ET C'EST OBLIGATOIRE.
                        // MimeKit consomme les flux des pièces au moment de
                        // composer le corps ; réutiliser le même constructeur
                        // enverrait des pièces vides à partir du second
                        // destinataire.
                        var constructeur = new BodyBuilder
                        {
                            HtmlBody = corps,
                            TextBody = texte,
                        };

                        AttacherLogo(constructeur);

                        foreach (var image in imagesIntegrees ?? [])
                        {
                            var liee = constructeur.LinkedResources.Add(
                                image.NomFichier,
                                image.Donnees,
                                ContentType.Parse(image.TypeMime));

                            // L'identifiant EST la référence écrite dans le
                            // corps : sans ça, MimeKit en génère un aléatoire et
                            // le `<img>` pointe vers rien.
                            liee.ContentId = image.Reference;
                        }

                        foreach (var piece in piecesJointes ?? [])
                        {
                            constructeur.Attachments.Add(
                                piece.NomFichier,
                                piece.Donnees,
                                ContentType.Parse(piece.TypeMime));
                        }

                        message.Body = constructeur.ToMessageBody();

                        if (await session.EnvoyerMessageAsync(message, cible, ct))
                        {
                            envoyes++;
                        }
                        else
                        {
                            echecs++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // UNE ADRESSE MORTE NE PRIVE PAS LES SUIVANTS.
                        echecs++;
                        _logger.LogError(ex,
                            "Diffusion : echec vers {Destinataire}.", cible);
                    }

                    avancement?.Report(envoyes + echecs);

                    // La respiration entre deux envois et la fermeture de la
                    // connexion appartiennent à la session : les régler ici
                    // aussi donnerait deux cadences à accorder, et l'une des
                    // deux serait oubliée le jour où on change l'autre.
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Diffusion « {Sujet} » INTERROMPUE : {Envoyes} envoye(s), {Restants} non traite(s).",
                    sujet, envoyes, destinataires.Count - envoyes - echecs);
                throw;
            }
            catch (Exception ex)
            {
                // La connexion elle-même a lâché : tout ce qui reste est perdu,
                // et il faut le dire avec le compte exact — relancer une
                // diffusion à l'aveugle écrirait deux fois aux premiers.
                _logger.LogError(ex,
                    "Diffusion « {Sujet} » INTERROMPUE apres {Envoyes} envoi(s).", sujet, envoyes);

                echecs += destinataires.Count - envoyes - echecs;
            }

            _logger.LogWarning(
                "Diffusion « {Sujet} » terminee : {Envoyes} envoye(s), {Echecs} echec(s).",
                sujet, envoyes, echecs);

            return new ResultatDiffusion(envoyes, echecs);
        }

        public ISessionEnvoi OuvrirSession() => new SessionEnvoi(this);

        /// <summary>
        /// Une connexion SMTP tenue ouverte pour une série de messages.
        ///
        /// IMBRIQUÉE DANS `ServiceEmail` À DESSEIN : elle réutilise la
        /// construction du corps, l'attache du logo et la version texte. Les
        /// sortir dans une classe à part créerait deux chemins de composition
        /// qui finiraient par diverger — et un bilan envoyé en série ne
        /// ressemblerait plus à un bilan envoyé seul.
        /// </summary>
        private sealed class SessionEnvoi : ISessionEnvoi
        {
            private readonly ServiceEmail _service;
            private readonly SmtpClient _client = new();
            private bool _premier = true;
            private int _echecsConsecutifs;
            private bool _abandonnee;

            /// <summary>
            /// Vrai quand la session a renoncé : le service d'envoi ne répond
            /// plus et s'entêter ne ferait que retarder le constat.
            /// </summary>
            public bool Abandonnee => _abandonnee;

            public SessionEnvoi(ServiceEmail service) => _service = service;

            public int Envoyes { get; private set; }

            public int Echecs { get; private set; }

            public async Task<bool> EnvoyerAsync(
                string destinataire,
                string sujet,
                string gabarit,
                IDictionary<string, string> valeurs,
                CancellationToken ct = default)
            {
                var options = _service._options;

                if (!_service.Disponible)
                {
                    _service._logger.LogWarning(
                        "SMTP non configure : « {Sujet} » vers {Destinataire} non envoye.",
                        sujet, destinataire);
                    Echecs++;
                    return false;
                }

                var cible = string.IsNullOrWhiteSpace(options.RedirectionDev)
                    ? destinataire
                    : options.RedirectionDev;

                MimeMessage message;

                try
                {
                    var corps = await _service.ConstruireCorpsAsync(sujet, gabarit, valeurs);

                    message = new MimeMessage();
                    message.From.Add(new MailboxAddress(
                        options.SenderNom ?? "Mimia", options.SenderEmail!));
                    message.To.Add(MailboxAddress.Parse(cible));
                    message.Subject = sujet;

                    var constructeur = new BodyBuilder { HtmlBody = corps, TextBody = VersTexte(corps) };
                    AttacherLogo(constructeur);
                    message.Body = constructeur.ToMessageBody();
                }
                catch (Exception ex)
                {
                    // UNE ADRESSE ILLISIBLE NE DOIT PAS ARRÊTER LA SÉRIE.
                    // `MailboxAddress.Parse` lève sur une adresse mal formée ;
                    // sans ce filet, un seul enregistrement abîmé en base
                    // priverait tous les parents suivants de leur bilan.
                    _service._logger.LogError(ex,
                        "Composition impossible pour {Destinataire} : message abandonne.", cible);
                    Echecs++;
                    return false;
                }

                return await EnvoyerMessageAsync(message, cible, ct);
            }

            /// <summary>
            /// Envoie un message déjà composé, en gérant la connexion, le débit
            /// et les reprises.
            /// </summary>
            internal async Task<bool> EnvoyerMessageAsync(
                MimeMessage message, string cible, CancellationToken ct)
            {
                var options = _service._options;
                var journal = _service._logger;
                var maximum = Math.Max(1, options.TentativesParMessage);

                // LE COUPE-CIRCUIT EST OUVERT : on ne tente même plus.
                //
                // Chaque message perdu coûte une douzaine de secondes en
                // tentatives ; sur le reste d'une liste de trois cents parents,
                // c'est près d'une heure à s'acharner sur un serveur qui ne
                // répond pas. On compte l'échec et on rend la main tout de
                // suite.
                if (_abandonnee)
                {
                    Echecs++;
                    return false;
                }

                for (var tentative = 1; tentative <= maximum; tentative++)
                {
                    try
                    {
                        await AssurerConnexionAsync(ct);

                        // LA PAUSE EST AVANT L'ENVOI, PAS APRÈS.
                        //
                        // Après le dernier message, elle ferait attendre
                        // l'appelant pour rien. Avant, elle ne s'applique qu'aux
                        // messages qui ont un prédécesseur — c'est-à-dire
                        // exactement ceux qui risquent de former une rafale.
                        if (!_premier && options.PauseEntreEnvoisMs > 0)
                        {
                            await Task.Delay(options.PauseEntreEnvoisMs, ct);
                        }

                        await _client.SendAsync(message, ct);

                        _premier = false;
                        _echecsConsecutifs = 0;
                        Envoyes++;
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (SmtpCommandException ex) when ((int)ex.StatusCode >= 500)
                    {
                        // REFUS DÉFINITIF : l'adresse n'existe pas, ou le
                        // message est rejeté sur le fond. Réessayer ne changera
                        // rien et abîme la réputation de l'expéditeur.
                        journal.LogError(
                            "Refus definitif vers {Destinataire} ({Code}) : {Message}",
                            cible, (int)ex.StatusCode, ex.Message);
                        Echecs++;
                        VerifierLeCoupeCircuit();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        // INCIDENT PASSAGER : débit dépassé (4xx), connexion
                        // fermée par le serveur, délai réseau. On repart d'une
                        // connexion neuve — celle qu'on tenait est peut-être
                        // dans un état indéterminé, et continuer dessus ferait
                        // échouer tous les messages suivants.
                        await FermerAsync();

                        if (tentative >= maximum)
                        {
                            journal.LogError(ex,
                                "Echec definitif vers {Destinataire} apres {Tentatives} tentative(s).",
                                cible, tentative);
                            Echecs++;
                            VerifierLeCoupeCircuit();
                            return false;
                        }

                        // Attente croissante : si le serveur nous freine, le
                        // reprendre aussitôt au même rythme obtient le même
                        // refus.
                        var attente = TimeSpan.FromSeconds(2 * tentative);

                        journal.LogWarning(ex,
                            "Incident vers {Destinataire} (tentative {Tentative}/{Maximum}). "
                            + "Nouvel essai dans {Secondes} s.",
                            cible, tentative, maximum, attente.TotalSeconds);

                        await Task.Delay(attente, ct);
                    }
                }

                return false;
            }

            /// <summary>
            /// Ouvre le coupe-circuit au bout de N échecs de suite.
            ///
            /// CONSÉCUTIFS, ET NON CUMULÉS. Une base de trois cents parents
            /// contient toujours quelques adresses mortes ; les compter
            /// ensemble ferait renoncer une série parfaitement saine. Cinq
            /// d'affilée, en revanche, ne s'expliquent pas par le hasard des
            /// carnets d'adresses.
            /// </summary>
            private void VerifierLeCoupeCircuit()
            {
                _echecsConsecutifs++;

                var seuil = _service._options.EchecsConsecutifsAvantAbandon;
                if (seuil <= 0 || _echecsConsecutifs < seuil) return;

                _abandonnee = true;

                _service._logger.LogError(
                    "SERIE ABANDONNEE : {Nombre} echecs consecutifs. Le service d'envoi ne repond "
                    + "pas (panne, quota du jour epuise, ou identifiants refuses). {Envoyes} message(s) "
                    + "etaient partis avant l'arret.",
                    _echecsConsecutifs, Envoyes);
            }

            private async Task AssurerConnexionAsync(CancellationToken ct)
            {
                if (_client.IsConnected && _client.IsAuthenticated) return;

                if (_client.IsConnected)
                {
                    await FermerAsync();
                }

                var options = _service._options;

                var securite = options.SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await _client.ConnectAsync(options.SmtpHost!, options.SmtpPort, securite, ct);
                await _client.AuthenticateAsync(options.Identifiant!, options.SenderPassword!, ct);
            }

            /// <summary>
            /// Ferme sans jamais lever. On ferme parce que quelque chose s'est
            /// déjà mal passé ; une exception ici masquerait la vraie cause.
            /// </summary>
            private async Task FermerAsync()
            {
                try
                {
                    if (_client.IsConnected)
                    {
                        await _client.DisconnectAsync(true, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _service._logger.LogDebug(ex, "Fermeture SMTP sans consequence.");
                }
            }

            public async ValueTask DisposeAsync()
            {
                await FermerAsync();
                _client.Dispose();
            }
        }

        /// <summary>
        /// Le logo, en pièce liée. Extrait parce que l'envoi simple et la
        /// diffusion en ont besoin tous les deux, et qu'une seconde copie du
        /// nom de la ressource finirait par diverger.
        /// </summary>
        private static void AttacherLogo(BodyBuilder constructeur)
        {
            using var flux = Assemblee.GetManifestResourceStream(
                "SchoolWebApp.Domain.Emails.Ressources.logo.png");

            if (flux is null) return;

            var image = constructeur.LinkedResources.Add("logo.png", flux);
            image.ContentId = "logoMimia";
        }

        public Task<string> ComposerAsync(
            string sujet, string gabarit, IDictionary<string, string> valeurs) =>
            ConstruireCorpsAsync(sujet, gabarit, valeurs);

        public async Task<string> RendreAsync(
            string sujet, string gabarit, IDictionary<string, string> valeurs)
        {
            var corps = await ConstruireCorpsAsync(sujet, gabarit, valeurs);

            // Dans un mail le logo est une pièce jointe liée (cid:). Un
            // navigateur ne sait pas résoudre ces adresses : on l'incorpore en
            // base64 pour que l'aperçu montre exactement ce que verra le parent.
            await using var flux = Assemblee.GetManifestResourceStream(
                "SchoolWebApp.Domain.Emails.Ressources.logo.png");

            if (flux is null) return corps;

            using var memoire = new MemoryStream();
            await flux.CopyToAsync(memoire);

            return corps.Replace(
                "cid:logoMimia",
                $"data:image/png;base64,{Convert.ToBase64String(memoire.ToArray())}");
        }

        private async Task<string> ConstruireCorpsAsync(
            string sujet, string gabarit, IDictionary<string, string> valeurs)
        {
            var contenu = Remplacer(await LireAsync(gabarit), valeurs);

            var enveloppe = new Dictionary<string, string>(valeurs)
            {
                ["titre"] = valeurs.TryGetValue("titre", out var t) ? t : sujet,
                ["contenu"] = contenu,
                ["annee"] = DateTime.UtcNow.Year.ToString(),
                ["urlSite"] = _options.UrlSite ?? "https://mimia.fr",

                // LA MENTION DU PIED, AVEC UNE VALEUR PAR DÉFAUT.
                //
                // « Vous avez un compte Mimia » est vrai pour un bilan ou une
                // alerte de quota. C'est FAUX pour une réponse du support :
                // n'importe qui peut écrire à cette adresse, y compris
                // quelqu'un qui n'a jamais rien souscrit — et lui affirmer
                // qu'il a un compte est au mieux troublant.
                //
                // Le défaut est posé ici plutôt que dans chaque appel : sans
                // lui, un gabarit qui oublie ce marqueur verrait la ligne
                // disparaître, puisqu'un marqueur sans valeur devient une
                // chaîne vide.
                ["mentionPied"] = valeurs.TryGetValue("mentionPied", out var m)
                    ? m
                    : "Vous recevez ce message parce que vous avez un compte Mimia.",
            };

            return Remplacer(await LireAsync("enveloppe"), enveloppe);
        }

        private static async Task<string> LireAsync(string nom)
        {
            await Verrou.WaitAsync();
            try
            {
                if (Cache.TryGetValue(nom, out var connu)) return connu;

                var chemin = $"SchoolWebApp.Domain.Emails.Gabarits.{nom}.html";
                await using var flux = Assemblee.GetManifestResourceStream(chemin)
                    ?? throw new FileNotFoundException($"Gabarit de mail introuvable : {chemin}");

                using var lecteur = new StreamReader(flux);
                var texte = await lecteur.ReadToEndAsync();

                Cache[nom] = texte;
                return texte;
            }
            finally
            {
                Verrou.Release();
            }
        }

        /// <summary>
        /// Un marqueur sans valeur devient une chaîne vide plutôt que de rester
        /// affiché : mieux vaut un blanc qu'un « {{prenom}} » dans la boîte du parent.
        /// </summary>
        private static string Remplacer(string gabarit, IDictionary<string, string> valeurs) =>
            Marqueur.Replace(gabarit, correspondance =>
                valeurs.TryGetValue(correspondance.Groups[1].Value, out var valeur) ? valeur : string.Empty);

        /// <summary>
        /// Version texte, exigée par les filtres anti-spam : un message
        /// uniquement HTML est fortement pénalisé.
        /// </summary>
        private static string VersTexte(string html)
        {
            var texte = Regex.Replace(html, @"<(style|script)[^>]*>.*?</\1>", " ", RegexOptions.Singleline);
            texte = Regex.Replace(texte, @"<br\s*/?>|</p>|</tr>|</h[1-6]>", "\n", RegexOptions.IgnoreCase);
            texte = Regex.Replace(texte, "<.*?>", string.Empty, RegexOptions.Singleline);
            texte = System.Net.WebUtility.HtmlDecode(texte);
            texte = Regex.Replace(texte, @"[ \t]+", " ");
            texte = Regex.Replace(texte, @"\n\s*\n\s*\n+", "\n\n");
            return texte.Trim();
        }
    }
}
