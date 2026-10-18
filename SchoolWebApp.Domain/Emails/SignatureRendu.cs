using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>Un modèle de signature, tel que l'administration le voit et le règle.</summary>
    public record ModeleSignature(
        int Id,
        string Nom,
        string? Formule,
        string? LigneLibre,
        bool AfficherRole,
        bool AfficherAdresse,
        bool AfficherSite,
        string Couleur,
        bool ParDefaut,
        int NombreComptes,
        string? Telephone = null);

    /// <summary>Ce que l'administration saisit pour créer ou modifier un modèle.</summary>
    public record SaisieSignature(
        string? Nom,
        string? Formule,
        string? LigneLibre,
        bool AfficherRole,
        bool AfficherAdresse,
        bool AfficherSite,
        string? Couleur,
        bool ParDefaut,
        string? Telephone = null);

    /// <summary>Qui signe : la personne connectée, au moment de l'envoi.</summary>
    public record Signataire(string? Prenom, string? Nom, string? Role, string? Adresse);

    /// <summary>
    /// LA SIGNATURE MISE EN PAGE, DANS LE THÈME DU SITE (17/10/2026).
    ///
    /// Un tableau et des styles en ligne, comme tout le reste du courriel :
    /// Outlook ignore le reste. La formule, puis un liseré à la couleur du
    /// modèle avec le nom de l'expéditeur, son rôle, son adresse, la ligne libre
    /// et le lien du site. Tout ce qui vient d'une saisie est échappé.
    /// </summary>
    public static class SignatureRendu
    {
        private static readonly Regex Couleur = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

        /// <summary>Une couleur sûre : celle du modèle si elle est bien formée, sinon le corail du site.</summary>
        /// <summary>
        /// LA FONCTION DU SUPER-ADMINISTRATEUR SUR SA SIGNATURE — Camara, le
        /// 17/10/2026. Son rôle (« Super-admin ») ne parle pas à un parent ; sa
        /// carte de visite dit sa vraie fonction. Le tableau garde « Super-admin ».
        /// D'abord « Président-Directeur Général », puis « Fondateur de Mimia »
        /// (17/10/2026) : plus juste pour une jeune marque, et sans dépendre de
        /// la forme juridique de la société.
        /// </summary>
        public const string FonctionSuperAdmin = "Fondateur de Mimia";

        public static string CouleurSure(string? couleur) =>
            couleur is not null && Couleur.IsMatch(couleur) ? couleur : "#d63f1c";

        /// <summary>
        /// L'ANCIENNE SIGNATURE FIXE du support, pour qui n'a pas de modèle (ou
        /// répond depuis la boîte partagée) : rien ne change pour lui.
        /// </summary>
        public const string Equipe =
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:26px 0 0 0;\">"
            + "<tr><td style=\"font-size:15px; line-height:1.7; color:#16233a;\">Bien à vous,<br /><strong>L'équipe Mimia</strong></td></tr>"
            + "</table>";

        /// <summary>
        /// LA MISE EN PAGE DE CAMARA (17/10/2026, d'après son image) : le logo à
        /// gauche, un trait dégradé orange → bleu, puis le nom en gras, la
        /// fonction, l'adresse et le site avec leurs icônes, un petit trait et la
        /// phrase en italique. Des images PNG, pas de SVG ni de dégradé CSS :
        /// les messageries les ignorent.
        ///
        /// `urlSite` : nul pour un courriel (images jointes au message, en
        /// `cid:` — voir ImagesSignature) ; vide pour un aperçu dans
        /// l'administration, qui les prend sur le site où il s'affiche.
        /// </summary>
        public static string Html(ModeleSignature modele, Signataire qui, string? urlSite)
        {
            static string E(string? s) => WebUtility.HtmlEncode(s?.Trim() ?? string.Empty);

            string Image(string nom) => ImagesSignature.Source(nom, urlSite);
            var couleur = CouleurSure(modele.Couleur);
            var police = "-apple-system,'Segoe UI',Helvetica,Arial,sans-serif";
            var nom = string.Join(' ', new[] { qui.Prenom, qui.Nom }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
            if (string.IsNullOrWhiteSpace(nom)) nom = "L'équipe Mimia";

            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(modele.Formule))
            {
                sb.Append($"<p style=\"margin:14px 0 0 0; font-family:{police}; font-size:15px; line-height:1.6; color:#16233a;\">{E(modele.Formule)}</p>");
            }

            // Un ensemble compact avec le texte (17/10/2026) : le dernier
            // paragraphe du message garde déjà sa marge du bas.
            sb.Append($"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:12px 0 0 0;\"><tr>");

            // Le logo.
            sb.Append($"<td style=\"padding:0 18px 0 0; vertical-align:middle;\"><img src=\"{Image("signature-logo")}\" width=\"112\" alt=\"mimia.fr\" style=\"display:block; width:112px; height:auto; border:0;\" /></td>");

            // Le trait vertical, orange en haut, bleu en bas.
            sb.Append($"<td style=\"width:3px; vertical-align:middle;\"><img src=\"{Image("signature-trait")}\" width=\"3\" height=\"132\" alt=\"\" style=\"display:block; width:3px; height:132px; border:0;\" /></td>");

            // Le texte.
            sb.Append($"<td style=\"padding:0 0 0 20px; vertical-align:middle; font-family:{police};\">");
            sb.Append($"<div style=\"font-size:21px; line-height:1.2; font-weight:800; color:{couleur};\">{E(nom)}</div>");

            if (modele.AfficherRole && !string.IsNullOrWhiteSpace(qui.Role))
            {
                sb.Append($"<div style=\"margin-top:3px; font-size:15px; line-height:1.4; color:#6b7a90;\">{E(qui.Role)}</div>");
            }

            if (modele.AfficherAdresse && !string.IsNullOrWhiteSpace(qui.Adresse))
            {
                sb.Append(LigneIcone(Image("signature-mail"), "✉",
                    $"<a href=\"mailto:{E(qui.Adresse)}\" style=\"color:#16233a; text-decoration:none;\">{E(qui.Adresse)}</a>", 10));
            }

            // LE TÉLÉPHONE, SOUS L'ADRESSE MAIL (Camara, le 17/10/2026).
            if (!string.IsNullOrWhiteSpace(modele.Telephone))
            {
                var numero = modele.Telephone.Trim();
                var appel = Regex.Replace(numero, @"[^0-9+]", string.Empty);
                sb.Append(LigneIcone(Image("signature-tel"), "☎",
                    $"<a href=\"tel:{E(appel)}\" style=\"color:#16233a; text-decoration:none;\">{E(numero)}</a>", 4));
            }

            if (modele.AfficherSite)
            {
                sb.Append(LigneIcone(Image("signature-web"), "🌐",
                    "<a href=\"https://mimia.fr\" style=\"color:#16233a; text-decoration:none;\">www.mimia.fr</a>", 4));
            }

            if (!string.IsNullOrWhiteSpace(modele.LigneLibre))
            {
                sb.Append("<div style=\"width:64px; height:2px; margin:10px 0 7px 0; background-color:#d5dbe5; font-size:0; line-height:0;\">&nbsp;</div>");
                sb.Append($"<div style=\"font-size:14px; line-height:1.45; font-style:italic; color:#56657a;\">{E(modele.LigneLibre)}</div>");
            }

            sb.Append("</td></tr></table>");
            return sb.ToString();
        }

        /// <summary>Une ligne « icône + texte » (adresse, site), en tableau pour Outlook.</summary>
        private static string LigneIcone(string image, string texteDeRepli, string contenu, int marge) =>
            $"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin-top:{marge}px;\"><tr>"
            + $"<td style=\"padding:0 10px 0 0; vertical-align:middle;\"><img src=\"{image}\" width=\"18\" height=\"18\" alt=\"{texteDeRepli}\" style=\"display:block; width:18px; height:18px; border:0;\" /></td>"
            + $"<td style=\"vertical-align:middle; font-size:14.5px; line-height:1.4;\">{contenu}</td>"
            + "</tr></table>";
    }
}
