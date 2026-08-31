using System.Net;
using System.Text;
using SchoolWebApp.Domain.Emails;
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

        /// <summary>Le bloc de la liste des pièces jointes, ou une chaîne vide.</summary>
        string ComposerPiecesJointes(IReadOnlyList<string> nomsFichiers);

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
    /// </summary>
    public class DiffusionService : IDiffusionService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<DiffusionService> _logger;

        // `object` et non `Lock` : ce dernier est une nouveauté .NET 9, et le
        // projet cible net8.0.
        private readonly object _verrou = new();
        private readonly EtatDiffusion _etat = new();

        public DiffusionService(IServiceScopeFactory scopes, ILogger<DiffusionService> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
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

        /// <summary>
        /// Le texte saisi devient du HTML sûr.
        ///
        /// TOUT EST ÉCHAPPÉ D'ABORD, puis on réintroduit ce qu'on autorise :
        /// les paragraphes et les images. L'inverse — laisser passer le HTML de
        /// l'administrateur — marcherait très bien jusqu'au jour où un copier-
        /// coller depuis un traitement de texte injecte des balises qui cassent
        /// la mise en page chez le parent, sans qu'on puisse le relire après
        /// coup.
        /// </summary>
        public string ComposerCorps(string texte, int nombreImages)
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

                sb.AppendLine(
                    $"<p style=\"margin:0 0 18px 0; font-size:15px; line-height:1.7; color:#16233a;\">{contenu}</p>");
            }

            return sb.ToString();
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
                using var portee = _scopes.CreateScope();
                var parents = portee.ServiceProvider.GetRequiredService<IParentRepository>();
                var email = portee.ServiceProvider.GetRequiredService<IServiceEmail>();

                var adresses = (await parents.GetAdressesParentsAsync())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                lock (_verrou) { _etat.Total = adresses.Count; }

                if (adresses.Count == 0)
                {
                    _logger.LogWarning("Diffusion « {Sujet} » : aucun destinataire.", sujet);
                    return;
                }

                var valeurs = new Dictionary<string, string>
                {
                    ["titre"] = sujet,
                    ["titreMessage"] = titre,
                    ["corpsMessage"] = ComposerCorps(texte, images.Count),
                    ["blocPiecesJointes"] =
                        ComposerPiecesJointes(documents.Select(d => d.NomFichier).ToList()),
                };

                var avancement = new Progress<int>(n =>
                {
                    lock (_verrou) { _etat.Traites = n; }
                });

                var resultat = await email.DiffuserAsync(
                    adresses, sujet, "diffusion", valeurs, images, documents, avancement);

                lock (_verrou)
                {
                    _etat.Envoyes = resultat.Envoyes;
                    _etat.Echecs = resultat.Echecs;
                }
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
