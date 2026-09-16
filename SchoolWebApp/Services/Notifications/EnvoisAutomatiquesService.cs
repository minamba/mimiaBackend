using System.Globalization;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Notifications
{
    public interface IEnvoisAutomatiquesService
    {
        /// <summary>
        /// Envoie une occurrence d'un courriel automatique à tous ceux qu'il
        /// concerne. Rend le résultat en une phrase, pour l'écran.
        /// </summary>
        Task<string> ExecuterAsync(ModeleMailDetail modele, DateTime occurrenceUtc, CancellationToken ct = default);
    }

    /// <summary>
    /// Les courriels automatiques : qui les reçoit, et l'envoi lui-même.
    ///
    /// LES RÈGLES DE CAMARA, LE 15/09/2026
    /// ----------------------------------
    /// - Période d'essai : les parents dont l'essai se termine le lendemain.
    /// - Demande d'avis période d'essai : trois jours après l'inscription.
    /// - Demande d'avis général : les abonnés payants sans avis, au plus une
    ///   fois tous les trois mois.
    /// - Relance et rappels : au menu, sans règle — ils ne partent pas.
    ///
    /// RÉSERVÉ AVANT D'ENVOYER, pour chaque parent. Un redémarrage au milieu
    /// de la liste ne réécrit pas aux premiers : la ligne du journal est déjà
    /// là, et l'index unique refuse la seconde.
    ///
    /// UN PLAFOND PAR PASSAGE (100 par défaut). Le quota quotidien du service
    /// d'envoi est partagé avec les bilans ; une première activation qui
    /// trouverait des centaines de parents ne doit pas l'épuiser d'un coup.
    /// Le reste part à l'occurrence suivante.
    /// </summary>
    public class EnvoisAutomatiquesService : IEnvoisAutomatiquesService
    {
        /// <summary>
        /// La page où un parent laisse son avis. Publique : un lien ouvert
        /// depuis une messagerie arrive déconnecté, et la page le fait se
        /// connecter avant de l'emmener au formulaire.
        /// </summary>
        public const string CheminAvis = "/donner-mon-avis";

        private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

        private readonly IModeleMailRepository _modeles;
        private readonly IEnvoiAutomatiqueRepository _envois;
        private readonly IServiceEmail _email;
        private readonly IComposeurModeleMail _composeur;
        private readonly IDiffusionService _diffusion;
        private readonly IJetonDesabonnement _jetons;
        private readonly IConfiguration _configuration;
        private readonly OptionsEmail _options;
        private readonly ILogger<EnvoisAutomatiquesService> _logger;

        public EnvoisAutomatiquesService(
            IModeleMailRepository modeles,
            IEnvoiAutomatiqueRepository envois,
            IServiceEmail email,
            IComposeurModeleMail composeur,
            IDiffusionService diffusion,
            IJetonDesabonnement jetons,
            IConfiguration configuration,
            IOptions<OptionsEmail> options,
            ILogger<EnvoisAutomatiquesService> logger)
        {
            _modeles = modeles;
            _envois = envois;
            _email = email;
            _composeur = composeur;
            _diffusion = diffusion;
            _jetons = jetons;
            _configuration = configuration;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> ExecuterAsync(
            ModeleMailDetail modele, DateTime occurrenceUtc, CancellationToken ct = default)
        {
            if (!CodeModeleMail.EnvoiGenerique(modele.Code))
            {
                return "Ce courriel n’a pas de règle d’envoi.";
            }

            if (string.IsNullOrWhiteSpace(modele.Sujet) || string.IsNullOrWhiteSpace(modele.Texte))
            {
                return "Objet ou message vide : rien n’a été envoyé.";
            }

            var categorie = CodeModeleMail.Categorie(modele.Code);

            if (categorie is not null && !_jetons.Disponible)
            {
                return "Clé de désabonnement absente : rien n’a été envoyé.";
            }

            var limite = Math.Max(1, _configuration.GetValue("Courrier:EnvoisAutomatiques:ParPassage", 100));
            var destinataires = await DestinatairesAsync(modele.Code!, occurrenceUtc, limite, ct);

            if (destinataires.Count == 0) return "Aucun destinataire.";

            var pieces = await _modeles.GetPiecesAsync(modele.Id, ct);
            var site = (_options.UrlSite ?? "https://mimia.fr").TrimEnd('/');

            var envoyes = 0;
            var echecs = 0;

            await using var session = _email.OuvrirSession();

            foreach (var destinataire in destinataires)
            {
                ct.ThrowIfCancellationRequested();

                var cle = modele.Code switch
                {
                    CodeModeleMail.FinEssai => $"abonnement:{destinataire.AbonnementId}",
                    CodeModeleMail.AvisEssai => $"parent:{destinataire.ParentId}",
                    _ => null,
                };

                var reservation = await _envois.ReserverAsync(
                    modele.Code!, destinataire.ParentId, cle, occurrenceUtc, ct);

                if (reservation is null) continue;

                var lien = categorie is null ? null : _jetons.Lien(destinataire.ParentId, categorie);

                var courriel = _composeur.Composer(
                    modele,
                    pieces,
                    Variables(destinataire, site),
                    lien is null ? null : _diffusion.ComposerMentionPied(lien));

                bool envoye;

                try
                {
                    envoye = await session.EnvoyerAsync(
                        destinataire.Mail,
                        courriel.Sujet,
                        "diffusion",
                        courriel.Valeurs,
                        courriel.Images,
                        courriel.Documents,
                        categorie is null ? null : _jetons.EnTetes(destinataire.ParentId, categorie),
                        ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Courriel automatique {Code} : echec vers le parent {Parent}.",
                        modele.Code, destinataire.ParentId);
                    envoye = false;
                }

                await _envois.MarquerAsync(
                    reservation.Value,
                    envoye ? StatutEnvoiAutomatique.Envoye : StatutEnvoiAutomatique.Echec,
                    ct);

                if (envoye) envoyes++;
                else echecs++;
            }

            var resultat = $"{envoyes} envoyé(s), {echecs} échec(s)";

            if (destinataires.Count >= limite)
            {
                resultat += " — plafond atteint, la suite au prochain envoi";
            }

            _logger.LogInformation(
                "Courriel automatique {Code} (occurrence {Occurrence:u}) : {Resultat}.",
                modele.Code, occurrenceUtc, resultat);

            return resultat;
        }

        private Task<IReadOnlyList<DestinataireAutomatique>> DestinatairesAsync(
            string code, DateTime occurrenceUtc, int limite, CancellationToken ct)
        {
            switch (code)
            {
                case CodeModeleMail.FinEssai:
                {
                    // « LE LENDEMAIN », À L'HORLOGE DE PARIS : l'occurrence de
                    // mardi 10 h vise les essais qui finissent mercredi, de
                    // minuit à minuit — pas les vingt-quatre heures qui suivent.
                    var demain = HeureFrance.Locale(occurrenceUtc).Date.AddDays(1);

                    return _envois.GetEssaisExpirantAsync(
                        HeureFrance.VersUtc(demain),
                        HeureFrance.VersUtc(demain.AddDays(1)),
                        DateTime.UtcNow,
                        limite,
                        ct);
                }

                case CodeModeleMail.AvisEssai:
                {
                    // TROIS JOURS APRÈS L'INSCRIPTION, dans une fenêtre bornée :
                    // sans elle, la première activation écrirait à tous les
                    // parents inscrits depuis l'ouverture du site.
                    var fenetre = Math.Max(1, _configuration.GetValue("Courrier:AvisEssai:FenetreJours", 14));
                    var inscritAvant = occurrenceUtc.AddDays(-3);

                    return _envois.GetAvisEssaiAsync(inscritAvant.AddDays(-fenetre), inscritAvant, limite, ct);
                }

                case CodeModeleMail.AvisGeneral:
                    return _envois.GetAvisGeneralAsync(occurrenceUtc.AddMonths(-3), limite, ct);

                default:
                    return Task.FromResult<IReadOnlyList<DestinataireAutomatique>>([]);
            }
        }

        private static IReadOnlyDictionary<string, ValeurVariable> Variables(
            DestinataireAutomatique destinataire, string site)
        {
            var variables = new Dictionary<string, ValeurVariable>
            {
                ["prenom"] = new(destinataire.Prenom?.Trim() ?? string.Empty),
                ["lienTarifs"] = new("voir les formules", $"{site}/tarifs"),
                ["lienAvis"] = new("donner mon avis", $"{site}{CheminAvis}"),
            };

            if (destinataire.DateFinEssai is { } fin)
            {
                variables["dateFinEssai"] = new(HeureFrance.Locale(fin).ToString("dddd d MMMM", Francais));
            }

            return variables;
        }
    }
}
