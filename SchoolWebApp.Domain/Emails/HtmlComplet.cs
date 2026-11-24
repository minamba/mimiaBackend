using System.Net;
using System.Reflection;
using System.Text;
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
    /// Le HTML collé est NETTOYÉ à chaque composition (voir <see cref="Nettoyer"/>).
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

        /// <summary>
        /// Le HTML collé, sans rien qui puisse s'exécuter.
        ///
        /// UN ANALYSEUR, PAS DES REGEX (contrôle d'Omar, v2 du 24/11/2026) : la v1 cherchait les attributs par regex et
        /// laissait passer `&lt;svg/onload=…&gt;`, `&lt;img src="x"onerror=…&gt;`, `&lt;img alt="&gt;" src=x onerror=…&gt;`,
        /// `href="jav&amp;Tab;ascript:…"` ou `href="javascript&amp;colon;…"`. Ici, le texte est découpé en balises et en
        /// attributs EXACTEMENT comme le fait un navigateur (algorithme de découpage du HTML5 : un attribut commence
        /// après une espace OU un `/`, ou juste après une valeur entre guillemets ; un `&gt;` entre guillemets ne ferme
        /// pas la balise ; les entités du HTML5 sont décodées avant de juger une adresse). Ce qu'on juge est donc ce
        /// que le navigateur verra.
        ///
        /// Ce qui part :
        /// - `script`, `iframe`, `noscript`, `noembed`, `noframes`, `xmp` AVEC leur contenu ; les balises `object`,
        ///   `embed`, `applet`, `frameset`, `frame`, `base`, `svg`, `math`, `template`, `plaintext`, `meta refresh` ;
        /// - tout attribut `on…` (quelle que soit la façon de l'écrire), `srcdoc`, `formaction`, et tout attribut au nom
        ///   invalide ;
        /// - toute adresse (`href`, `src`, `action`, `background`…) dont le protocole n'est pas `http`, `https`,
        ///   `mailto`, `tel`, `sms` ou `cid` (ou `data:image/…` pour une image) : remplacée par `#` ;
        /// - un `style` qui contient `expression(`, `javascript:`, `behavior:` ou `-moz-binding` ;
        /// - les commentaires ordinaires (les commentaires conditionnels d'Outlook restent, leur contenu nettoyé).
        ///
        /// Une balise sans rien à retirer est recopiée À L'IDENTIQUE (la maquette ne bouge pas) ; une balise corrigée
        /// est réécrite proprement (valeurs entre guillemets doubles, `"` `&lt;` `&gt;` échappés). Le nettoyage est
        /// stable : le repasser ne change plus rien.
        /// </summary>
        public static string Nettoyer(string? html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            var sortie = new StringBuilder(html.Length);
            Analyser(html, sortie);
            return sortie.ToString();
        }

        /// <summary>Retirés avec leur contenu (texte brut jusqu'à la balise de fin, comme le lit le navigateur).</summary>
        private static readonly HashSet<string> RetiresAvecContenu = new(StringComparer.OrdinalIgnoreCase)
        {
            "script", "iframe", "noscript", "noembed", "noframes", "xmp",
        };

        /// <summary>
        /// Retirés seuls (leur contenu reste, nettoyé comme du HTML ordinaire). `svg` et `math` partent aussi : sans
        /// eux, rien n'est lu en « contenu étranger », où les règles de découpage changent.
        /// </summary>
        private static readonly HashSet<string> RetiresSeuls = new(StringComparer.OrdinalIgnoreCase)
        {
            "object", "embed", "applet", "frameset", "frame", "base", "svg", "math", "template", "plaintext",
            "param", "portal", "isindex",
        };

        /// <summary>Gardés, mais leur contenu est du texte brut jusqu'à la balise de fin (rien n'y est une balise).</summary>
        private static readonly HashSet<string> TexteBrutGarde = new(StringComparer.OrdinalIgnoreCase)
        {
            "style", "title", "textarea",
        };

        /// <summary>Les attributs qui portent une adresse.</summary>
        private static readonly HashSet<string> AttributsAdresse = new(StringComparer.OrdinalIgnoreCase)
        {
            "href", "src", "action", "background", "poster", "cite", "longdesc", "usemap", "lowsrc", "dynsrc",
            "codebase", "data", "xlink:href", "profile", "manifest", "ping", "srcset", "imagesrcset", "archive",
            "classid", "icon", "itemtype",
        };

        /// <summary>Les attributs d'image, seuls à accepter une image en `data:image/…`.</summary>
        private static readonly HashSet<string> AttributsImage = new(StringComparer.OrdinalIgnoreCase)
        {
            "src", "background", "poster", "lowsrc", "srcset", "imagesrcset",
        };

        private static readonly HashSet<string> AttributsInterdits = new(StringComparer.OrdinalIgnoreCase)
        {
            "srcdoc", "formaction",
        };

        private static readonly HashSet<string> ProtocolesPermis = new(StringComparer.Ordinal)
        {
            "http", "https", "mailto", "tel", "sms", "cid",
        };

        private static readonly Regex NomValide = new(@"^[a-zA-Z][a-zA-Z0-9:_.-]*$", RegexOptions.Compiled);

        private static readonly Regex NomAttributValide = new(@"^[a-zA-Z_:][a-zA-Z0-9:_.-]*$", RegexOptions.Compiled);

        private static readonly Regex ImageData = new(@"^data:image/(png|gif|jpe?g|webp|bmp);", RegexOptions.Compiled);

        private static readonly Regex CssDangereux = new(
            @"expression\s*\(|javascript\s*:|vbscript\s*:|behavior\s*:|-moz-binding",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Espaces, contrôles et caractères invisibles : retirés avant de lire un protocole.</summary>
        private static readonly Regex Invisibles = new(
            @"[\x00-\x20\x7f-\xa0\xad\u200b-\u200f\u2028\u2029\u2060\ufeff]+", RegexOptions.Compiled);

        private static bool EstEspace(char c) => c is ' ' or '\t' or '\n' or '\f' or '\r';

        private static bool EstLettre(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

        private static void Analyser(string html, StringBuilder sortie)
        {
            var i = 0;
            var n = html.Length;

            while (i < n)
            {
                var c = html[i];
                if (c != '<')
                {
                    sortie.Append(c);
                    i++;
                    continue;
                }

                // `<!--` : un commentaire.
                if (string.CompareOrdinal(html, i, "<!--", 0, 4) == 0)
                {
                    i = Commentaire(html, i, sortie);
                    continue;
                }

                // `<!…>` et `<?…>` : un « commentaire de fortune » jusqu'au premier `>`. Seuls le DOCTYPE et les marques
                // d'Outlook `<![if …]>` / `<![endif]>` (inertes pour un navigateur) sont gardés.
                if (i + 1 < n && html[i + 1] is '!' or '?')
                {
                    var fin = html.IndexOf('>', i + 2);
                    var bout = fin < 0 ? html[i..] : html[i..(fin + 1)];
                    if (Regex.IsMatch(bout, @"^<!(doctype\s[^<>]*|\[if\s[^<>\]]*\]|\[endif\])>$", RegexOptions.IgnoreCase))
                    {
                        sortie.Append(bout);
                    }

                    i = fin < 0 ? n : fin + 1;
                    continue;
                }

                // `</nom …>` : une balise de fin, réécrite `</nom>` (ses attributs, ignorés du navigateur, partent).
                if (i + 1 < n && html[i + 1] == '/')
                {
                    if (i + 2 < n && EstLettre(html[i + 2]))
                    {
                        var balise = LireBalise(html, i + 2);
                        if (!RetiresAvecContenu.Contains(balise.Nom) && !RetiresSeuls.Contains(balise.Nom)
                            && NomValide.IsMatch(balise.Nom))
                        {
                            sortie.Append("</").Append(balise.Nom).Append('>');
                        }

                        i = balise.Fin;
                        continue;
                    }

                    // `</>` est ignoré, `</ autre chose>` est un commentaire de fortune : rien ne part.
                    var fin = html.IndexOf('>', i + 2);
                    i = fin < 0 ? n : fin + 1;
                    continue;
                }

                // `<` suivi d'une lettre : une balise d'ouverture. Sinon, c'est du texte (écrit `&lt;`, même rendu).
                if (i + 1 >= n || !EstLettre(html[i + 1]))
                {
                    sortie.Append("&lt;");
                    i++;
                    continue;
                }

                var ouverture = LireBalise(html, i + 1);
                var nom = ouverture.Nom;

                if (RetiresAvecContenu.Contains(nom))
                {
                    // Le contenu part jusqu'à la balise de fin. Sans balise de fin, seule l'ouverture part et la suite
                    // est nettoyée comme le reste (rien n'est perdu, rien ne s'exécute).
                    var finContenu = FinDeTexteBrut(html, ouverture.Fin, nom);
                    i = finContenu < 0 ? ouverture.Fin : finContenu;
                    continue;
                }

                if (RetiresSeuls.Contains(nom) || !NomValide.IsMatch(nom) || EstMetaRefresh(ouverture))
                {
                    i = ouverture.Fin;
                    continue;
                }

                sortie.Append(Reecrire(html, i, ouverture));
                i = ouverture.Fin;

                if (TexteBrutGarde.Contains(nom))
                {
                    var debutFin = DebutBaliseDeFin(html, i, nom);
                    if (debutFin < 0) continue;  // pas de fin : la suite est lue comme du HTML ordinaire

                    var contenu = html[i..debutFin];
                    if (nom.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        contenu = CssDangereux.Replace(
                            contenu, m => m.Value.StartsWith('-') ? "x-moz-bloque" : "x-" + m.Value.Replace(":", "-").Replace("(", "-"));
                    }
                    else
                    {
                        // `title` et `textarea` décodent les entités : `&lt;` s'y affiche `<`, même rendu, aucune balise.
                        contenu = contenu.Replace("<", "&lt;");
                    }

                    sortie.Append(contenu);
                    i = debutFin;  // la balise de fin est lue au tour suivant
                }
            }
        }

        /// <summary>
        /// Un commentaire à partir de `&lt;!--` ; rend la position qui suit. Il se ferme au premier `--&gt;` ou `--!&gt;`
        /// (ou tout de suite : `&lt;!--&gt;`, `&lt;!---&gt;`). Les ordinaires partent ; ceux d'Outlook restent.
        /// </summary>
        private static int Commentaire(string html, int debut, StringBuilder sortie)
        {
            var n = html.Length;
            var corps = debut + 4;

            int finCorps, apres;
            if (corps < n && html[corps] == '>') { finCorps = corps; apres = corps + 1; }
            else if (corps + 1 < n && html[corps] == '-' && html[corps + 1] == '>') { finCorps = corps; apres = corps + 2; }
            else
            {
                var a = html.IndexOf("-->", corps, StringComparison.Ordinal);
                var b = html.IndexOf("--!>", corps, StringComparison.Ordinal);
                if (a < 0 && b < 0) return n;  // jamais fermé : le navigateur en fait un commentaire jusqu'au bout
                if (a >= 0 && (b < 0 || a <= b)) { finCorps = a; apres = a + 3; }
                else { finCorps = b; apres = b + 4; }
            }

            var contenu = html[corps..finCorps];

            if (contenu.StartsWith("mimia:", StringComparison.Ordinal) && !contenu.Contains('<') && !contenu.Contains('>'))
            {
                sortie.Append(html, debut, apres - debut);
                return apres;
            }

            // `<!--[if !mso]><!-->`, `<!--<![endif]-->`, `<!-->` : des marques sans contenu, gardées telles quelles.
            if (Regex.IsMatch(contenu, @"^(\[if\s[^<>\]]{0,80}\]><!|<!\[endif\]|>?)$", RegexOptions.IgnoreCase))
            {
                sortie.Append(html, debut, apres - debut);
                return apres;
            }

            // `<!--[if mso]> … <![endif]-->` : le HTML d'Outlook, nettoyé comme le reste.
            var conditionnel = Regex.Match(
                contenu, @"^(?<si>\[if\s[^<>\]]{0,80}\]>)(?<dedans>.*?)(?<fin><!\[endif\])?$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (conditionnel.Success)
            {
                var dedans = Nettoyer(conditionnel.Groups["dedans"].Value);
                // Le contenu nettoyé ne doit pas pouvoir fermer le commentaire avant l'heure.
                if (!dedans.Contains("-->") && !dedans.Contains("--!>") && !dedans.EndsWith("-") && !dedans.EndsWith("--!"))
                {
                    sortie.Append("<!--").Append(conditionnel.Groups["si"].Value).Append(dedans)
                        .Append(conditionnel.Groups["fin"].Value).Append(html, finCorps, apres - finCorps);
                }
            }

            return apres;
        }

        private sealed record AttributLu(string Nom, string? Valeur);

        private sealed record BaliseLue(string Nom, List<AttributLu> Attributs, bool AutoFermante, int Fin);

        /// <summary>
        /// Une balise lue comme le fait le navigateur (HTML5, « tokenization ») à partir de la première lettre du nom ;
        /// `Fin` = position après le `&gt;` (ou la fin du texte).
        /// </summary>
        private static BaliseLue LireBalise(string html, int debut)
        {
            var n = html.Length;
            var i = debut;
            while (i < n && !EstEspace(html[i]) && html[i] != '/' && html[i] != '>') i++;
            var nom = html[debut..i].ToLowerInvariant();

            var attributs = new List<AttributLu>();
            var autoFermante = false;

            while (i < n)
            {
                var c = html[i];
                if (EstEspace(c)) { i++; continue; }
                if (c == '>') { i++; return new BaliseLue(nom, attributs, autoFermante, i); }
                if (c == '/')
                {
                    i++;
                    autoFermante = i < n && html[i] == '>';
                    continue;
                }

                autoFermante = false;

                // Le nom : jusqu'à une espace, `/`, `>` ou `=` (un `=` en tête fait partie du nom).
                var debutNom = i;
                i++;
                while (i < n && !EstEspace(html[i]) && html[i] != '/' && html[i] != '>' && html[i] != '=') i++;
                var nomAttribut = html[debutNom..i].ToLowerInvariant();

                var j = i;
                while (j < n && EstEspace(html[j])) j++;
                if (j >= n || html[j] != '=')
                {
                    attributs.Add(new AttributLu(nomAttribut, null));
                    i = j;
                    continue;
                }

                i = j + 1;
                while (i < n && EstEspace(html[i])) i++;
                if (i >= n) { attributs.Add(new AttributLu(nomAttribut, string.Empty)); break; }

                string valeur;
                if (html[i] is '"' or '\'')
                {
                    var guillemet = html[i];
                    var finValeur = html.IndexOf(guillemet, i + 1);
                    if (finValeur < 0) { valeur = html[(i + 1)..]; i = n; }
                    else { valeur = html[(i + 1)..finValeur]; i = finValeur + 1; }
                    // Juste après le guillemet, un autre attribut peut commencer SANS espace : `src="x"onerror=…`.
                }
                else if (html[i] == '>')
                {
                    valeur = string.Empty;
                }
                else
                {
                    var debutValeur = i;
                    while (i < n && !EstEspace(html[i]) && html[i] != '>') i++;
                    valeur = html[debutValeur..i];
                }

                attributs.Add(new AttributLu(nomAttribut, valeur));
            }

            return new BaliseLue(nom, attributs, autoFermante, n);
        }

        /// <summary>La position du `&lt;` de la balise de fin `&lt;/nom` d'un texte brut, ou -1.</summary>
        private static int DebutBaliseDeFin(string html, int depuis, string nom)
        {
            var i = depuis;
            while (true)
            {
                var p = html.IndexOf("</", i, StringComparison.Ordinal);
                if (p < 0) return -1;
                var apresNom = p + 2 + nom.Length;
                if (apresNom <= html.Length
                    && string.Compare(html, p + 2, nom, 0, nom.Length, StringComparison.OrdinalIgnoreCase) == 0
                    && (apresNom == html.Length || EstEspace(html[apresNom]) || html[apresNom] is '/' or '>'))
                {
                    return p;
                }

                i = p + 2;
            }
        }

        /// <summary>La position après la balise de fin d'un élément retiré avec son contenu, ou -1.</summary>
        private static int FinDeTexteBrut(string html, int depuis, string nom)
        {
            var debutFin = DebutBaliseDeFin(html, depuis, nom);
            return debutFin < 0 ? -1 : LireBalise(html, debutFin + 2).Fin;
        }

        private static bool EstMetaRefresh(BaliseLue balise) =>
            balise.Nom == "meta"
            && balise.Attributs.Any(a => a.Nom == "http-equiv"
                                         && Invisibles.Replace(Decoder(a.Valeur ?? string.Empty), string.Empty)
                                             .Equals("refresh", StringComparison.OrdinalIgnoreCase));

        /// <summary>La balise recopiée telle quelle si rien n'est à retirer, sinon réécrite avec ce qui reste.</summary>
        private static string Reecrire(string html, int debut, BaliseLue balise)
        {
            var garde = new List<AttributLu>();
            var change = false;

            foreach (var attribut in balise.Attributs)
            {
                var juge = Juger(attribut);
                if (!ReferenceEquals(juge, attribut)) change = true;
                if (juge is not null) garde.Add(juge);
            }

            var brut = html[debut..balise.Fin];

            // Recopiée telle quelle seulement si elle est fermée et qu'aucune valeur ne contient `<` ou `>` (une valeur
            // `alt=">"` est réécrite `alt="&gt;"` : le texte nettoyé n'a plus de chevron hors des balises).
            if (!change && brut.EndsWith('>') && !balise.Attributs.Any(a => a.Valeur is not null && a.Valeur.IndexOfAny(['<', '>']) >= 0))
            {
                return brut;
            }

            var sb = new StringBuilder("<").Append(balise.Nom);
            foreach (var a in garde)
            {
                sb.Append(' ').Append(a.Nom);
                if (a.Valeur is not null)
                {
                    sb.Append("=\"")
                        .Append(a.Valeur.Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;"))
                        .Append('"');
                }
            }

            if (balise.AutoFermante) sb.Append(" /");
            return sb.Append('>').ToString();
        }

        /// <summary>L'attribut tel quel, un attribut corrigé (adresse remplacée par `#`), ou null s'il part.</summary>
        private static AttributLu? Juger(AttributLu attribut)
        {
            var nom = attribut.Nom;
            if (!NomAttributValide.IsMatch(nom)) return null;
            if (nom.StartsWith("on", StringComparison.OrdinalIgnoreCase)) return null;
            if (AttributsInterdits.Contains(nom)) return null;
            if (attribut.Valeur is null) return attribut;

            var lue = Decoder(attribut.Valeur);

            if (nom == "style")
            {
                var css = Invisibles.Replace(Regex.Replace(lue, @"/\*.*?\*/|\\", string.Empty, RegexOptions.Singleline), string.Empty);
                return CssDangereux.IsMatch(css) ? null : attribut;
            }

            if (AttributsAdresse.Contains(nom))
            {
                var permis = nom is "srcset" or "imagesrcset"
                    ? lue.Split(',').All(candidat => AdressePermise(candidat.Trim().Split(' ', '\t', '\n')[0], image: true))
                    : AdressePermise(lue, AttributsImage.Contains(nom));
                return permis ? attribut : attribut with { Valeur = "#" };
            }

            // Tout autre attribut : une adresse exécutable n'a rien à y faire (`values`, `to`, `content`…).
            var compacte = Invisibles.Replace(lue, string.Empty).ToLowerInvariant();
            return compacte.Contains("javascript:") || compacte.Contains("vbscript:") || compacte.Contains("data:text/html")
                ? attribut with { Valeur = "#" }
                : attribut;
        }

        /// <summary>
        /// Vrai si l'adresse (déjà décodée) est relative ou d'un protocole permis. Les espaces et invisibles sont retirés
        /// AVANT de lire le protocole (`java&#9;script:` est du `javascript:` pour un navigateur). Une entité restée
        /// non décodée avant le premier `/`, `?` ou `#` rend l'adresse suspecte.
        /// </summary>
        private static bool AdressePermise(string adresse, bool image)
        {
            var a = Invisibles.Replace(adresse, string.Empty);
            if (a.Length == 0) return true;

            var deuxPoints = a.IndexOf(':');
            var separateur = a.IndexOfAny(['/', '?', '#']);
            var tete = separateur < 0 ? a : a[..separateur];

            if (deuxPoints < 0 || (separateur >= 0 && separateur < deuxPoints))
            {
                return !tete.Contains('&') && !tete.Contains('\\');  // relative
            }

            var protocole = a[..deuxPoints].ToLowerInvariant();
            if (ProtocolesPermis.Contains(protocole)) return true;
            return image && protocole == "data" && ImageData.IsMatch(a.ToLowerInvariant());
        }

        // ------------------------------------------------------- entités du HTML5

        private static readonly Regex Entite = new(
            @"&(?:#(?<dec>[0-9]{1,8});?|#[xX](?<hex>[0-9a-fA-F]{1,7});?|(?<nom>[a-zA-Z][a-zA-Z0-9]{1,31});)", RegexOptions.Compiled);

        /// <summary>
        /// Les entités du HTML5 qui ne sont pas dans le HTML 4 (que décode `WebUtility.HtmlDecode`) et qui servent à
        /// cacher un protocole ou un séparateur : `&amp;Tab;`, `&amp;colon;`, `&amp;NewLine;`, `&amp;lpar;`…
        /// </summary>
        private static readonly Dictionary<string, string> EntitesHtml5 = new(StringComparer.Ordinal)
        {
            ["Tab"] = "\t", ["NewLine"] = "\n", ["colon"] = ":", ["sol"] = "/", ["quest"] = "?", ["num"] = "#",
            ["lpar"] = "(", ["rpar"] = ")", ["period"] = ".", ["excl"] = "!", ["comma"] = ",", ["semi"] = ";",
            ["equals"] = "=", ["plus"] = "+", ["lowbar"] = "_", ["UnderBar"] = "_", ["dollar"] = "$", ["percnt"] = "%",
            ["ast"] = "*", ["midast"] = "*", ["bsol"] = "\\", ["Hat"] = "^", ["grave"] = "`", ["DiacriticalGrave"] = "`",
            ["lsqb"] = "[", ["lbrack"] = "[", ["rsqb"] = "]", ["rbrack"] = "]", ["lcub"] = "{", ["lbrace"] = "{",
            ["rcub"] = "}", ["rbrace"] = "}", ["verbar"] = "|", ["vert"] = "|", ["VerticalLine"] = "|", ["apos"] = "'",
            ["QUOT"] = "\"", ["AMP"] = "&", ["LT"] = "<", ["GT"] = ">", ["NonBreakingSpace"] = "\u00a0",
            ["ZeroWidthSpace"] = "\u200b", ["NegativeVeryThinSpace"] = "\u200b", ["NegativeThinSpace"] = "\u200b",
            ["NegativeMediumSpace"] = "\u200b", ["NegativeThickSpace"] = "\u200b", ["NoBreak"] = "\u2060",
            ["shy"] = "\u00ad", ["commat"] = "@", ["newline"] = "\n",
        };

        /// <summary>Une valeur d'attribut telle que le navigateur la lit (un seul décodage, comme lui).</summary>
        private static string Decoder(string valeur) =>
            valeur.IndexOf('&') < 0
                ? valeur
                : Entite.Replace(valeur, m =>
                {
                    if (m.Groups["dec"].Success || m.Groups["hex"].Success)
                    {
                        var code = m.Groups["dec"].Success
                            ? long.Parse(m.Groups["dec"].Value)
                            : Convert.ToInt64(m.Groups["hex"].Value, 16);
                        if (code is <= 0 or > 0x10FFFF or (>= 0xD800 and <= 0xDFFF)) return "\ufffd";
                        return char.ConvertFromUtf32((int)code);
                    }

                    var nom = m.Groups["nom"].Value;
                    if (EntitesHtml5.TryGetValue(nom, out var texte)) return texte;
                    var html4 = WebUtility.HtmlDecode(m.Value);
                    return html4 != m.Value ? html4 : m.Value;
                });

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
