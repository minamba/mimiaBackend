using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Emails;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    public class MessageContactRequest
    {
        [Required, StringLength(80, MinimumLength = 2)]
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Ignorée pour un visiteur connecté : son adresse est prise dans le
        /// jeton. Un formulaire ne décide pas de qui on est.
        /// </summary>
        [EmailAddress, StringLength(180)]
        public string? Mail { get; set; }

        [Required, StringLength(120, MinimumLength = 3)]
        public string Sujet { get; set; } = string.Empty;

        [Required, StringLength(4000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Piège à robots : le champ est caché à l'écran, donc toujours vide
        /// chez un humain. Rempli, le message est accepté en apparence et
        /// jeté — un robot qui reçoit une erreur réessaie autrement.
        /// </summary>
        public string? Site { get; set; }
    }

    /// <summary>
    /// Le formulaire de contact du site.
    ///
    /// Ouvert sans connexion, à dessein : les personnes qui ont le plus besoin
    /// d'écrire sont souvent celles qui n'arrivent pas à entrer — mot de passe
    /// perdu, paiement refusé, compte introuvable. Exiger d'être connecté pour
    /// signaler qu'on n'y arrive pas serait absurde.
    /// </summary>
    [ApiController]
    [Route("contact")]
    public class ContactController : ControllerBase
    {
        private readonly IServiceEmail _email;
        private readonly OptionsEmail _options;
        private readonly ILogger<ContactController> _logger;

        // Mémoire des envois récents, par adresse IP.
        //
        // Un point d'entrée public qui expédie des mails est une arme prêtée :
        // sans limite, il sert à inonder la boîte de support, ou pire, à s'en
        // servir comme relais. Trois messages par heure suffisent largement à
        // un humain de bonne foi.
        private static readonly ConcurrentDictionary<string, List<DateTime>> Envois = new();
        private const int MaxParHeure = 3;

        public ContactController(
            IServiceEmail email,
            IOptions<OptionsEmail> options,
            ILogger<ContactController> logger)
        {
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [AllowAnonymous]
        [SwaggerResponse(200, "Message transmis.")]
        [SwaggerResponse(400, "Formulaire incomplet ou adresse manquante.")]
        [SwaggerResponse(429, "Trop de messages envoyés récemment.")]
        public async Task<IActionResult> Envoyer(
            [FromBody] MessageContactRequest requete,
            [FromServices] Services.Notifications.ITelegramService telegram,
            CancellationToken ct)
        {
            // Le piège à robots : on remercie et on ne fait rien.
            if (!string.IsNullOrWhiteSpace(requete.Site))
            {
                _logger.LogInformation("Message de contact ignoré : piège à robots déclenché.");
                return Ok(new { message = "Message transmis." });
            }

            // L'adresse du visiteur connecté vient du JETON, pas du formulaire.
            // Sinon n'importe qui pourrait écrire sous l'adresse d'un autre, et
            // la réponse du support partirait au mauvais destinataire.
            var mailJeton = User.FindFirst("email")?.Value
                            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var connecte = !string.IsNullOrWhiteSpace(mailJeton);
            var mail = connecte ? mailJeton! : requete.Mail?.Trim();

            if (string.IsNullOrWhiteSpace(mail))
            {
                return BadRequest(new { message = "Merci d'indiquer une adresse e-mail pour la réponse." });
            }

            if (TropDeMessages())
            {
                return StatusCode(429, new
                {
                    message = "Vous avez déjà envoyé plusieurs messages. "
                              + "Laissez-nous le temps d'y répondre, puis réessayez dans une heure.",
                });
            }

            var destinataire = string.IsNullOrWhiteSpace(_options.RecipientEmail)
                ? _options.SenderEmail
                : _options.RecipientEmail;

            if (string.IsNullOrWhiteSpace(destinataire) || !_email.Disponible)
            {
                // On journalise le message en entier : sans SMTP, c'est la
                // seule trace qui reste, et perdre la demande d'un parent
                // serait pire que tout.
                _logger.LogError(
                    "Contact NON ENVOYÉ (SMTP indisponible) — de {Mail} ({Nom}) : {Sujet} | {Message}",
                    mail, requete.Nom, requete.Sujet, requete.Message);

                return StatusCode(500, new
                {
                    message = "L'envoi est momentanément indisponible. Réessayez plus tard.",
                });
            }

            var envoye = await _email.EnvoyerAsync(
                destinataire,
                $"[Contact] {requete.Sujet}",
                "contact",
                new Dictionary<string, string>
                {
                    ["sujet"] = requete.Sujet,
                    ["nom"] = requete.Nom,
                    ["mail"] = mail,
                    ["message"] = requete.Message,
                    ["statutCompte"] = connecte ? "membre connecté" : "visiteur non connecté",
                    ["sujetReponse"] = Uri.EscapeDataString($"Re : {requete.Sujet}"),
                },
                ct);

            if (!envoye)
            {
                _logger.LogError(
                    "Contact NON ENVOYÉ — de {Mail} ({Nom}) : {Sujet} | {Message}",
                    mail, requete.Nom, requete.Sujet, requete.Message);

                return StatusCode(500, new
                {
                    message = "L'envoi a échoué. Réessayez, ou écrivez-nous directement.",
                });
            }

            Enregistrer();
            _logger.LogInformation("Message de contact reçu de {Mail} : {Sujet}", mail, requete.Sujet);

            // ALERTE APRÈS L'ENVOI DU COURRIEL, ET SEULEMENT S'IL A ABOUTI.
            //
            // Le courriel reste le canal de travail : c'est de là qu'on
            // répond. Telegram ne fait que prévenir — signaler un message qui
            // n'est jamais arrivé dans la boîte enverrait chercher dans le
            // vide. Les branches d'échec plus haut journalisent déjà le
            // message en entier, qui est alors la seule trace qui subsiste.
            await telegram.NotifierContactAsync(
                requete.Nom, mail, requete.Sujet, requete.Message, connecte);

            return Ok(new { message = "Message transmis." });
        }

        private string Empreinte() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnue";

        private bool TropDeMessages()
        {
            var depuis = DateTime.UtcNow.AddHours(-1);
            var recents = Envois.GetOrAdd(Empreinte(), _ => new List<DateTime>());

            lock (recents)
            {
                recents.RemoveAll(d => d < depuis);
                return recents.Count >= MaxParHeure;
            }
        }

        private void Enregistrer()
        {
            var recents = Envois.GetOrAdd(Empreinte(), _ => new List<DateTime>());
            lock (recents) { recents.Add(DateTime.UtcNow); }
        }
    }
}
