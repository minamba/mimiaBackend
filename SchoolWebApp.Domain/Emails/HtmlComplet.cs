using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using MimeKit;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// LE MODE « HTML COMPLET » DU MESSAGE DE DIFFUSION — Min's, le 24/11/2026 : « faire en sorte que ça accepte le HTML,
    /// le client doit recevoir le mail fait par Slimane surtout quand derrière j'ai validé ». Le mail Black Friday validé
    /// (maquette sombre de 600 px) partait dans le gabarit blanc du site.
    ///
    /// UN MARQUEUR EN FIN DE TEXTE, PAS UNE COLONNE (aucune migration) : un template dont le texte se termine par
    /// <see cref="Marqueur"/> est un HTML complet. L'écran le pose et le cache ; le serveur le retire avant d'envoyer.
    /// En FIN de texte pour que les insertions au curseur de l'écran (variables) gardent leurs positions.
    ///
    /// CE QUE LE SERVEUR GARDE POUR LUI dans ce mode (voir `DiffusionService.ComposerHtmlComplet`) : le lien de
    /// désinscription signé à la place de `{{lien_desinscription}}`, l'en-tête `List-Unsubscribe`, la case Promotion
    /// (envoi aux seuls consentants), le pied légal si la balise manque, et les images du site en pièces liées (`cid:`).
    /// Le HTML collé est NETTOYÉ à chaque composition : ni script, ni `on…=`, ni `javascript:`.
    /// </summary>
    public static class HtmlComplet
    {
        /// <summary>La marque d'un texte en HTML complet, à la toute fin du texte enregistré.</summary>
        public const string Marqueur = "<!--mimia:html-complet-->";

        /// <summary>
        /// La valeur de gabarit qui court-circuite l'enveloppe du site : `ServiceEmail` rend ce HTML TEL QUEL, sans
        /// en-tête, bannière ni pied (le HTML de Slimane porte les siens).
        /// </summary>
        public const string Cle = "htmlComplet";

        /// <summary>Vrai si le texte d'un template est un HTML complet.</summary>
        public static bool Est(string? texte) =>
            !string.IsNullOrEmpty(texte) && texte.TrimEnd().EndsWith(Marqueur, StringComparison.Ordinal);

        /// <summary>Le HTML sans le marqueur (le texte inchangé s'il n'en porte pas).</summary>
        public static string Contenu(string? texte)
        {
            if (!Est(texte)) return texte ?? string.Empty;
            var t = texte!.TrimEnd();
            return t[..^Marqueur.Length].TrimEnd();
        }

        // ----------------------------------------------------------------- nettoyage

        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;

        /// <summary>Les éléments retirés AVEC leur contenu.</summary>
        private static readonly Regex ElementsDangereux = new(
            @"<(script|iframe|object|embed|applet|frameset|frame)\b[^>]*>.*?</\1\s*>", Options);

        /// <summary>Les balises seules qui restent (non fermées, ou orphelines), et `base` / `meta refresh`.</summary>
        private static readonly Regex BalisesDangereuses = new(
            @"</?(script|iframe|object|embed|applet|frameset|frame|base)\b[^>]*>|<meta\b[^>]*http-equiv\s*=\s*[""']?refresh[^>]*>",
            Options);

        /// <summary>
        /// Un commentaire ordinaire (notes de la maquette). Les commentaires conditionnels d'Outlook
        /// (`&lt;!--[if mso]&gt;…&lt;![endif]--&gt;`, `&lt;!--[if !mso]&gt;&lt;!--&gt;`, `&lt;!--&lt;![endif]--&gt;`) restent.
        /// </summary>
        private static readonly Regex Commentaire = new(@"<!--(?!\[if|<!\[endif|>)(?!mimia:).*?-->", Options);

        private static readonly Regex Balise = new(@"<[a-zA-Z][^>]*>", Options);

        private static readonly Regex AttributEvenement = new(
            @"\s+on[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", Options);

        private static readonly Regex Attribut = new(
            @"(?<nom>\s[\w:-]+)(?<egal>\s*=\s*)(?<valeur>""[^""]*""|'[^']*'|[^\s""'>]+)", Options);

        private static readonly Regex Invisibles = new(@"[\u0000- \u007f-\u009f​-‍﻿]+", RegexOptions.Compiled);

        /// <summary>
        /// Le HTML collé, sans ce qui peut s'exécuter : `script` (et `iframe`, `object`, `embed`…), les attributs
        /// `on…=`, et toute valeur d'attribut en `javascript:` / `vbscript:` / `data:text/html` (entités et espaces
        /// d'obfuscation compris), remplacée par `#`. Les commentaires ordinaires partent aussi : ils n'ont rien à faire
        /// chez le parent, et une balise citée dans une note ne doit pas compter comme présente.
        /// </summary>
        public static string Nettoyer(string? html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            var propre = Commentaire.Replace(html, string.Empty);

            // Jusqu'à stabilité : `<scr<script></script>ipt>` ne doit pas se recomposer après un premier passage.
            string avant;
            do
            {
                avant = propre;
                propre = ElementsDangereux.Replace(propre, string.Empty);
                propre = BalisesDangereuses.Replace(propre, string.Empty);
            }
            while (propre != avant);

            return Balise.Replace(propre, b =>
            {
                var balise = b.Value;
                string precedente;
                do
                {
                    precedente = balise;
                    balise = AttributEvenement.Replace(balise, string.Empty);
                }
                while (balise != precedente);

                return Attribut.Replace(balise, a =>
                {
                    var brute = a.Groups["valeur"].Value.Trim('"', '\'');
                    var lue = Invisibles.Replace(WebUtility.HtmlDecode(WebUtility.HtmlDecode(brute)), string.Empty)
                        .ToLowerInvariant();

                    var dangereuse = lue.Contains("javascript:") || lue.Contains("vbscript:")
                                     || lue.StartsWith("data:text/html") || lue.Contains("expression(");

                    return dangereuse ? $"{a.Groups["nom"].Value}{a.Groups["egal"].Value}\"#\"" : a.Value;
                });
            });
        }

        // ------------------------------------------------------- images du site (cid:)

        private static readonly Assembly Assemblee = typeof(HtmlComplet).Assembly;

        private const string PrefixeRessource = "SchoolWebApp.Domain.Emails.Ressources.email-";

        /// <summary>Le préfixe des pièces liées des images du site : `cid:mimia-email-logo-sombre`.</summary>
        public const string PrefixeCid = "mimia-email-";

        /// <summary>
        /// `src="https://mimia.fr/email/logo-sombre.png"` (ou `www.`, ou `/email/…`) : une image du dossier `email/` du
        /// site. Si l'assembly en porte une copie (`Ressources/email-&lt;nom&gt;`), elle part en pièce liée — Outlook
        /// bloque les images distantes par défaut, Gmail les affiche toujours en pièce liée. Sinon, l'adresse reste.
        /// </summary>
        private static readonly Regex ImageDuSite = new(
            @"(?<avant>\bsrc\s*=\s*[""'])(?:https?://(?:www\.)?mimia\.fr)?/email/(?<nom>[\w-]+)\.(?<ext>png|jpe?g|gif)(?<apres>[""'])",
            Options);

        private static readonly Regex CidDuSite = new(@"cid:" + PrefixeCid + @"(?<nom>[\w-]+)", RegexOptions.Compiled);

        private static string? Ressource(string nom)
        {
            foreach (var ext in new[] { "png", "jpg", "jpeg", "gif" })
            {
                var chemin = $"{PrefixeRessource}{nom}.{ext}";
                if (Assemblee.GetManifestResourceInfo(chemin) is not null) return chemin;
            }

            return null;
        }

        /// <summary>Remplace les images du site connues de l'assembly par leur `cid:`.</summary>
        public static string LierImagesDuSite(string html) =>
            ImageDuSite.Replace(html, m =>
                Ressource(m.Groups["nom"].Value) is null
                    ? m.Value
                    : $"{m.Groups["avant"].Value}cid:{PrefixeCid}{m.Groups["nom"].Value}{m.Groups["apres"].Value}");

        private static IEnumerable<(string Nom, string Chemin)> ImagesCitees(string corps) =>
            CidDuSite.Matches(corps)
                .Select(m => m.Groups["nom"].Value)
                .Distinct()
                .Select(nom => (nom, Ressource(nom)))
                .Where(x => x.Item2 is not null)
                .Select(x => (x.nom, x.Item2!));

        /// <summary>Joint au message les images du site que son HTML cite par `cid:`.</summary>
        public static void AttacherImagesDuSite(BodyBuilder constructeur)
        {
            foreach (var (nom, chemin) in ImagesCitees(constructeur.HtmlBody ?? string.Empty))
            {
                using var flux = Assemblee.GetManifestResourceStream(chemin);
                if (flux is null) continue;

                var image = constructeur.LinkedResources.Add($"{nom}{Path.GetExtension(chemin)}", flux);
                image.ContentId = $"{PrefixeCid}{nom}";
            }
        }

        /// <summary>Pour un aperçu dans un navigateur : les mêmes images en base64.</summary>
        public static string ImagesDuSiteEnDataUri(string corps)
        {
            foreach (var (nom, chemin) in ImagesCitees(corps).ToList())
            {
                using var flux = Assemblee.GetManifestResourceStream(chemin);
                if (flux is null) continue;

                using var memoire = new MemoryStream();
                flux.CopyTo(memoire);
                var type = Path.GetExtension(chemin).TrimStart('.').ToLowerInvariant() switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "gif" => "image/gif",
                    _ => "image/png",
                };

                corps = Regex.Replace(
                    corps,
                    $@"cid:{Regex.Escape(PrefixeCid + nom)}(?![\w-])",
                    $"data:{type};base64,{Convert.ToBase64String(memoire.ToArray())}");
            }

            return corps;
        }
    }
}
