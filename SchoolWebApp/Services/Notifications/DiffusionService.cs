using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Notifications
{
    /// <summary>L'état de la diffusion en cours, ou de la dernière.</summary>
    public class EtatDiffusion
    {
        public bool EnCours { get; set; }

        public string? Sujet { get; set; }

        public int Total { get; set; }

        public int Traites { get; set; }

        public int Envoyes { get; set; }

        public int Echecs { get; set; }

        public DateTime? Debut { get; set; }

        public DateTime? Fin { get; set; }

        /// <summary>Renseigné si la diffusion s'est arrêtée sur une erreur.</summary>
        public string? Erreur { get; set; }
    }

    /// <summary>
    /// La valeur d'une variable `{{cle}}` d'un message : un texte, et un lien
    /// s'il doit être cliquable (« donner mon avis »).
    /// </summary>
    public record ValeurVariable(string Texte, string? Lien = null);

    public interface IDiffusionService
    {
        /// <summary>L'état courant, pour l'écran qui suit l'avancement.</summary>
        EtatDiffusion Etat { get; }

        /// <summary>
        /// Compose le corps du message à partir du texte saisi.
        ///
        /// Sert à l'aperçu comme à l'envoi : deux compositions différentes
        /// donneraient un aperçu qui ne montre pas ce qui part.
        /// </summary>
        string ComposerCorps(string texte, int nombreImages);

        /// <summary>
        /// Le même corps, les variables `{{cle}}` remplacées. Une variable
        /// inconnue reste visible : dans l'aperçu, c'est une faute de frappe
        /// qui se voit.
        /// </summary>
        string ComposerCorps(string texte, int nombreImages, IReadOnlyDictionary<string, ValeurVariable>? variables);

        /// <summary>Les variables d'un texte simple — un objet, un titre. Sans HTML.</summary>
        string RemplacerVariables(string? texte, IReadOnlyDictionary<string, ValeurVariable>? variables);

        /// <summary>Le bloc de la liste des pièces jointes, ou une chaîne vide.</summary>
        string ComposerPiecesJointes(IReadOnlyList<string> nomsFichiers);

        /// <summary>
        /// La mention de pied commune à la diffusion et au message à un
        /// parent : le lien vers le site est déjà dans l'enveloppe commune à
        /// tous les mails, celle-ci ajoute le rappel qu'on peut écrire au
        /// support.
        /// </summary>
        string ComposerMentionPied();

        /// <summary>La même mention, suivie du lien « Ne plus recevoir ces messages ».</summary>
        string ComposerMentionPied(string? lienDesabonnement);

        /// <summary>
        /// Lance la diffusion en arrière-plan. Faux si une autre est déjà en
        /// cours — deux diffusions simultanées écriraient deux fois à tout le
        /// monde.
        /// </summary>
        bool Lancer(
            string sujet,
            string titre,
            string texte,
            IReadOnlyList<PieceMail> images,
            IReadOnlyList<PieceMail> documents);
    }

    /// <summary>
    /// La diffusion d'un message à tous les parents.
    ///
    /// EN ARRIÈRE-PLAN, ET CE N'EST PAS UN CONFORT
    /// -------------------------------------------
    /// Trois cents envois espacés d'un quart de seconde font plus d'une minute.
    /// Une requête HTTP qui dure ce temps-là expire chez le navigateur ou dans
    /// le proxy, l'écran affiche une erreur, et l'administrateur reclique —
    /// alors que l'envoi, lui, continuait. Tout le monde reçoit deux fois.
    ///
    /// L'écran interroge donc l'état pendant que le travail se fait ailleurs.
    ///
    /// SINGLETON, ET UNE SEULE DIFFUSION À LA FOIS. C'est le verrou qui
    /// empêche deux envois concurrents, et il n'a de sens que s'il n'existe
    /// qu'un seul objet.
    ///
    /// UN COURRIEL PAR PARENT, COMPOSÉ POUR LUI — Camara, le 15/09/2026
    /// ----------------------------------------------------------------
    /// Chaque diffusion porte un lien « Ne plus recevoir ces messages » propre
    /// au parent, et peut dire « Bonjour {{prenom}} ». Le corps n'est donc plus
    /// construit une fois pour tous : il l'est pour chacun, dans la même
    /// connexion SMTP.
    /// </summary>
    public class DiffusionService : IDiffusionService
    {
        /// <summary>
        /// `**gras**`, comme dans un carnet Markdown.
        ///
        /// L'éditeur reste du texte — voir `ComposerCorps` — mais deux
        /// astérisques pour obtenir du gras est un geste que la plupart des
        /// administrateurs connaissent déjà (Slack, WhatsApp), sans qu'aucun
        /// HTML de leur cru n'entre jamais dans le message.
        /// </summary>
        private static readonly Regex Gras = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

        /// <summary>
        /// `{{prenom}}`, avec l'espace qui le précède : une variable vide
        /// l'emporte avec elle, et « Bonjour {{prenom}}, » devient « Bonjour, »
        /// plutôt que « Bonjour , ».
        /// </summary>
        private static readonly Regex Variable = new(@"(\s?)\{\{(\w+)\}\}", RegexOptions.Compiled);

        private readonly IServiceScopeFactory _scopes;
        private readonly IJetonDesabonnement _jetons;
        private readonly ILogger<DiffusionService> _logger;

        // `object` et non `Lock` : ce dernier est une nouveauté .NET 9, et le
        // projet cible net8.0.
        private readonly object _verrou = new();
        private readonly EtatDiffusion _etat = new();

        public DiffusionService(
            IServiceScopeFactory scopes,
            IJetonDesabonnement jetons,
            ILogger<DiffusionService> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _jetons = jetons ?? throw new ArgumentNullException(nameof(jetons));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public EtatDiffusion Etat
        {
            get
            {
                // Copie sous verrou : l'écran lit pendant que la boucle écrit,
                // et rendre l'objet vivant laisserait voir des compteurs
                // incohérents entre deux champs.
                lock (_verrou)
                {
                    return new EtatDiffusion
                    {
                        EnCours = _etat.EnCours,
                        Sujet = _etat.Sujet,
                        Total = _etat.Total,
                        Traites = _etat.Traites,
                        Envoyes = _etat.Envoyes,
                        Echecs = _etat.Echecs,
                        Debut = _etat.Debut,
                        Fin = _etat.Fin,
                        Erreur = _etat.Erreur,
                    };
                }
            }
        }

        public string ComposerCorps(string texte, int nombreImages) =>
            ComposerCorps(texte, nombreImages, null);

        /// <summary>
        /// Le texte saisi devient du HTML sûr.
        ///
        /// TOUT EST ÉCHAPPÉ D'ABORD, puis on réintroduit ce qu'on autorise :
        /// les paragraphes, les images et les variables. L'inverse — laisser
        /// passer le HTML de l'administrateur — marcherait très bien jusqu'au
        /// jour où un copier-coller depuis un traitement de texte injecte des
        /// balises qui cassent la mise en page chez le parent, sans qu'on puisse
        /// le relire après coup.
        /// </summary>
        public string ComposerCorps(
            string texte, int nombreImages, IReadOnlyDictionary<string, ValeurVariable>? variables)
        {
            if (string.IsNullOrWhiteSpace(texte)) return string.Empty;

            var sb = new StringBuilder();

            // Un paragraphe par bloc séparé d'une ligne vide. Les sauts simples
            // deviennent des retours à la ligne : c'est ce qu'attend quelqu'un
            // qui tape dans une zone de texte.
            var blocs = texte.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

            foreach (var bloc in blocs)
            {
                var contenu = WebUtility.HtmlEncode(bloc.Trim()).Replace("\n", "<br>");

                // LE GRAS, RÉINTRODUIT APRÈS L'ÉCHAPPEMENT LUI AUSSI.
                //
                // `**` ne fait partie d'aucune syntaxe HTML : l'appliquer sur
                // le texte déjà échappé ne risque pas de rouvrir une balise.
                contenu = Gras.Replace(contenu, "<strong>$1</strong>");

                // LES MARQUEURS D'IMAGE, RÉINTRODUITS APRÈS L'ÉCHAPPEMENT.
                //
                // `[image:1]` devient une image liée par son identifiant. Le
                // faire avant l'échappement aurait transformé les chevrons de
                // la balise en entités, et le parent aurait lu le code source.
                for (var i = 1; i <= nombreImages; i++)
                {
                    contenu = contenu.Replace(
                        $"[image:{i}]",
                        $"<img src=\"cid:diffusion{i}\" alt=\"\" "
                        + "style=\"max-width:100%; height:auto; border-radius:12px; margin:6px 0;\">");
                }

                // LES VARIABLES EN DERNIER, et leurs valeurs échappées à leur
                // tour : un prénom saisi par un parent n'est pas du HTML.
                if (variables is { Count: > 0 })
                {
                    contenu = Variable.Replace(contenu, m =>
                    {
                        if (!variables.TryGetValue(m.Groups[2].Value, out var valeur)) return m.Value;

                        return string.IsNullOrEmpty(valeur.Texte)
                            ? string.Empty
                            : m.Groups[1].Value + EnHtml(valeur);
                    });
                }

                sb.AppendLine(
                    $"<p style=\"margin:0 0 18px 0; font-size:15px; line-height:1.7; color:#16233a;\">{contenu}</p>");
            }

            return sb.ToString();
        }

        public string RemplacerVariables(string? texte, IReadOnlyDictionary<string, ValeurVariable>? variables)
        {
            if (string.IsNullOrEmpty(texte) || variables is not { Count: > 0 }) return texte ?? string.Empty;

            return Variable.Replace(texte, m =>
            {
                if (!variables.TryGetValue(m.Groups[2].Value, out var valeur)) return m.Value;

                return string.IsNullOrEmpty(valeur.Texte)
                    ? string.Empty
                    : m.Groups[1].Value + valeur.Texte;
            });
        }

        private static string EnHtml(ValeurVariable valeur) =>
            valeur.Lien is null
                ? WebUtility.HtmlEncode(valeur.Texte)
                : $"<a href=\"{WebUtility.HtmlEncode(valeur.Lien)}\" "
                  + "style=\"color:#e8603c; font-weight:600; text-decoration:underline;\">"
                  + $"{WebUtility.HtmlEncode(valeur.Texte)}</a>";

        /// <summary>
        /// LE LIEN DU SITE EST DÉJÀ DANS L'ENVELOPPE — voir `enveloppe.html`,
        /// qui porte `mimia.fr` en pied sur TOUS les mails, celui-ci compris.
        /// Ce qui manquait à une diffusion ou un message ciblé, c'est
        /// l'adresse du support pour qui a une question suite au message.
        /// </summary>
        public string ComposerMentionPied() => ComposerMentionPied(null);

        public string ComposerMentionPied(string? lienDesabonnement)
        {
            var mention = "Vous recevez ce message parce que vous avez un compte Mimia. "
                          + "Une question ? Écrivez-nous à "
                          + "<a href=\"mailto:support@mimia.fr\" style=\"color:#a5abb8; text-decoration:underline;\">"
                          + "support@mimia.fr</a>.";

            return lienDesabonnement is null
                ? mention
                : mention
                  + $"<br><a href=\"{WebUtility.HtmlEncode(lienDesabonnement)}\" "
                  + "style=\"color:#a5abb8; text-decoration:underline;\">Ne plus recevoir ces messages</a>";
        }

        public string ComposerPiecesJointes(IReadOnlyList<string> nomsFichiers)
        {
            if (nomsFichiers.Count == 0) return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine(
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" "
                + "style=\"margin:26px 0 0 0; background-color:#faf7f2; border-radius:14px;\">");
            sb.AppendLine("<tr><td style=\"padding:20px 22px;\">");
            sb.AppendLine(
                "<div style=\"font-size:13px; font-weight:600; letter-spacing:0.07em; "
                + "text-transform:uppercase; color:#8b93a3;\">"
                + (nomsFichiers.Count == 1 ? "Document joint" : "Documents joints")
                + "</div>");

            sb.AppendLine("<div style=\"margin-top:10px; font-size:15px; line-height:1.8; color:#16233a;\">");

            foreach (var nom in nomsFichiers)
            {
                sb.AppendLine($"📎 {WebUtility.HtmlEncode(nom)}<br>");
            }

            sb.AppendLine("</div></td></tr></table>");

            return sb.ToString();
        }

        public bool Lancer(
            string sujet,
            string titre,
            string texte,
            IReadOnlyList<PieceMail> images,
            IReadOnlyList<PieceMail> documents)
        {
            lock (_verrou)
            {
                // LE VERROU EST ICI, PAS DANS LE CONTRÔLEUR. Un double clic, deux
                // onglets ouverts, un rechargement pendant l'envoi : trois façons
                // d'arriver ici deux fois, et une seule d'y répondre.
                if (_etat.EnCours) return false;

                _etat.EnCours = true;
                _etat.Sujet = sujet;
                _etat.Total = 0;
                _etat.Traites = 0;
                _etat.Envoyes = 0;
                _etat.Echecs = 0;
                _etat.Debut = DateTime.UtcNow;
                _etat.Fin = null;
                _etat.Erreur = null;
            }

            // `Task.Run` sans attendre : la requête HTTP rend la main tout de
            // suite, et l'écran suit l'avancement en interrogeant `Etat`.
            _ = Task.Run(() => DiffuserAsync(sujet, titre, texte, images, documents));

            return true;
        }

        private async Task DiffuserAsync(
            string sujet,
            string titre,
            string texte,
            IReadOnlyList<PieceMail> images,
            IReadOnlyList<PieceMail> documents)
        {
            try
            {
                // PAS DE LIEN, PAS D'ENVOI. Une annonce commerciale sans moyen de
                // s'en désabonner n'a pas à partir chez trois cents parents ; la
                // configuration manquante se voit ici, dans l'écran, au lieu de
                // se découvrir dans les plaintes.
                if (!_jetons.Disponible)
                {
                    const string raison =
                        "Clé de désabonnement absente (Courrier:CleDesabonnement) : la diffusion n'est pas partie.";

                    _logger.LogError("Diffusion « {Sujet} » : {Raison}", sujet, raison);
                    lock (_verrou) { _etat.Erreur = raison; }
                    return;
                }

                using var portee = _scopes.CreateScope();
                var envois = portee.ServiceProvider.GetRequiredService<IEnvoiAutomatiqueRepository>();
                var email = portee.ServiceProvider.GetRequiredService<IServiceEmail>();

                var destinataires = await envois.GetDestinatairesDiffusionAsync();

                lock (_verrou) { _etat.Total = destinataires.Count; }

                if (destinataires.Count == 0)
                {
                    _logger.LogWarning("Diffusion « {Sujet} » : aucun destinataire.", sujet);
                    return;
                }

                var blocPieces = ComposerPiecesJointes(documents.Select(d => d.NomFichier).ToList());
                var envoyes = 0;
                var echecs = 0;

                // UNE CONNEXION POUR TOUTE LA LISTE : la session espace les
                // envois, se reconnecte et renonce si le service ne répond plus.
                await using var session = email.OuvrirSession();

                foreach (var destinataire in destinataires)
                {
                    var variables = new Dictionary<string, ValeurVariable>
                    {
                        ["prenom"] = new(destinataire.Prenom?.Trim() ?? string.Empty),
                    };

                    var sujetPersonnel = RemplacerVariables(sujet, variables);

                    var valeurs = new Dictionary<string, string>
                    {
                        ["titre"] = sujetPersonnel,
                        ["titreMessage"] = RemplacerVariables(titre, variables),
                        ["corpsMessage"] = ComposerCorps(texte, images.Count, variables),
                        ["blocPiecesJointes"] = blocPieces,
                        ["mentionPied"] = ComposerMentionPied(
                            _jetons.Lien(destinataire.ParentId, CategorieDesabonnement.Diffusion)),
                    };

                    bool envoye;

                    try
                    {
                        envoye = await session.EnvoyerAsync(
                            destinataire.Mail,
                            sujetPersonnel,
                            "diffusion",
                            valeurs,
                            images,
                            documents,
                            _jetons.EnTetes(destinataire.ParentId, CategorieDesabonnement.Diffusion));
                    }
                    catch (Exception ex)
                    {
                        // UNE ADRESSE MORTE NE PRIVE PAS LES SUIVANTS.
                        _logger.LogError(ex, "Diffusion : echec vers le parent {Parent}.", destinataire.ParentId);
                        envoye = false;
                    }

                    if (envoye) envoyes++;
                    else echecs++;

                    lock (_verrou)
                    {
                        _etat.Traites = envoyes + echecs;
                        _etat.Envoyes = envoyes;
                        _etat.Echecs = echecs;
                    }
                }

                _logger.LogWarning(
                    "Diffusion « {Sujet} » terminee : {Envoyes} envoye(s), {Echecs} echec(s).",
                    sujet, envoyes, echecs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diffusion « {Sujet} » : echec general.", sujet);
                lock (_verrou) { _etat.Erreur = ex.Message; }
            }
            finally
            {
                // TOUJOURS RELÂCHÉ, quoi qu'il arrive. Un verrou resté fermé sur
                // une exception interdirait toute diffusion jusqu'au prochain
                // redémarrage, sans que rien ne l'explique.
                lock (_verrou)
                {
                    _etat.EnCours = false;
                    _etat.Fin = DateTime.UtcNow;
                }
            }
        }
    }
}
