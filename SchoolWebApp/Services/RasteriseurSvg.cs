using SkiaSharp;
using Svg.Skia;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Transforme une planche SVG en image que le modèle de vision sait lire.
    ///
    /// POURQUOI C'EST INDISPENSABLE, ET PAS UN CONFORT
    /// ----------------------------------------------
    /// Un tiers des planches sont des SVG. Pour celles-là, le professeur ne
    /// recevait AUCUNE image quand l'élève cliquait dessus — le code s'arrêtait
    /// sur un `return null` faute de savoir quoi joindre. Il ne lui restait que
    /// « en bas à gauche », et il nommait au hasard : « alvéoles » trois fois de
    /// suite pour trois clics différents, dont un sur le diaphragme.
    ///
    /// L'extraction de la carte des étiquettes butait sur le même mur.
    ///
    /// Rasteriser lève les deux d'un coup : la planche redevient une image
    /// comme les autres, avec sa marque dessinée dessus et ses étiquettes
    /// lisibles.
    ///
    /// POURQUOI PAS UNE LECTURE DU BALISAGE
    /// ------------------------------------
    /// Tentant : les `text` d'un SVG portent leurs coordonnées, on pourrait
    /// bâtir la carte sans modèle et sans erreur. Mais ces coordonnées se lisent
    /// à travers la pile des `transform` des groupes qui les contiennent, et
    /// les SVG de Wikimedia en empilent volontiers trois. Les lire à la main,
    /// c'est réécrire un moteur de rendu — et une position fausse ici est une
    /// erreur permanente que personne ne verra jamais.
    ///
    /// On rend donc l'image, et on la traite comme une image.
    /// </summary>
    public static class RasteriseurSvg
    {
        /// <summary>
        /// Côté maximal du rendu. Assez grand pour que les étiquettes restent
        /// lisibles par un modèle de vision, assez petit pour que la facture en
        /// jetons reste celle d'une image ordinaire.
        /// </summary>
        private const int CoteMax = 1600;

        /// <summary>
        /// Le PNG correspondant, ou null si le document n'est pas rendable.
        ///
        /// Un échec n'est jamais fatal : l'appelant retombe sur le comportement
        /// d'avant — pas d'image pour ce tour — et le cours continue.
        /// </summary>
        public static byte[]? EnPng(byte[]? svg)
        {
            if (svg is not { Length: > 0 }) return null;

            try
            {
                using var flux = new MemoryStream(svg);
                using var document = new SKSvg();

                if (document.Load(flux) is not { } dessin) return null;

                // LA FENÊTRE DÉCLARÉE, PAS L'ÉTENDUE DU DESSIN.
                //
                // `CullRect` donne les bornes de ce qui est tracé. Le navigateur,
                // lui, affiche le `viewBox` — et un dessin qui déborde de sa
                // fenêtre est courant : mesuré sur tes planches, l'un dépasse de
                // 10 %, un autre de 50 %.
                //
                // L'écart ne se verrait nulle part : l'image partirait au
                // professeur, complète et lisible, simplement cadrée autrement
                // que chez l'élève. Les positions cliquées seraient décalées du
                // même pourcentage, à chaque clic, sur ces planches-là
                // seulement. Impossible à diagnostiquer depuis une réponse.
                var bornes = FenetreDeclaree(svg) ?? dessin.CullRect;
                if (bornes.Width <= 0 || bornes.Height <= 0) return null;

                var echelle = Math.Min(
                    1f, CoteMax / Math.Max(bornes.Width, bornes.Height));

                // Jamais en dessous de un : beaucoup de SVG déclarent une
                // surface minuscule et comptent sur l'affichage pour les
                // agrandir. Rendus à leur taille déclarée, leurs étiquettes
                // feraient six pixels de haut et seraient illisibles.
                echelle = Math.Max(echelle, Math.Min(4f, 900f / Math.Max(bornes.Width, bornes.Height)));

                var largeur = Math.Max(1, (int)(bornes.Width * echelle));
                var hauteur = Math.Max(1, (int)(bornes.Height * echelle));

                using var surface = SKSurface.Create(new SKImageInfo(largeur, hauteur));
                var toile = surface.Canvas;

                // FOND BLANC OBLIGATOIRE. Un SVG n'a pas de fond : sans lui, le
                // PNG sort sur du transparent, que le modèle reçoit comme du
                // noir — et un schéma au trait noir sur fond noir n'existe pas.
                toile.Clear(SKColors.White);

                toile.Scale(echelle);
                toile.Translate(-bornes.Left, -bornes.Top);
                toile.DrawPicture(dessin);

                using var rendu = surface.Snapshot();
                using var encode = rendu.Encode(SKEncodedImageFormat.Png, 90);

                return encode.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Le `viewBox` du document, ou null s'il n'en déclare pas.
        ///
        /// Lu à la main plutôt que par un analyseur XML complet : on ne cherche
        /// qu'un attribut de la balise racine, il est dans les premiers
        /// caractères, et charger deux mégaoctets de balisage pour ça serait
        /// disproportionné.
        ///
        /// Sans `viewBox`, le navigateur cadre sur `width`/`height` — et à
        /// défaut sur le contenu, ce que rend justement `CullRect`. Rendre null
        /// laisse donc l'appelant retomber sur le bon comportement.
        /// </summary>
        private static SKRect? FenetreDeclaree(byte[] svg)
        {
            var entete = System.Text.Encoding.UTF8.GetString(
                svg, 0, Math.Min(4000, svg.Length));

            var trouve = System.Text.RegularExpressions.Regex.Match(
                entete,
                @"viewBox\s*=\s*[""']\s*([-\d.eE]+)[\s,]+([-\d.eE]+)[\s,]+([-\d.eE]+)[\s,]+([-\d.eE]+)\s*[""']",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!trouve.Success) return null;

            var nombres = new float[4];
            for (var i = 0; i < 4; i++)
            {
                if (!float.TryParse(
                        trouve.Groups[i + 1].Value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out nombres[i]))
                {
                    return null;
                }
            }

            if (nombres[2] <= 0 || nombres[3] <= 0) return null;

            return new SKRect(
                nombres[0], nombres[1],
                nombres[0] + nombres[2], nombres[1] + nombres[3]);
        }

        /// <summary>
        /// Les octets à envoyer à un modèle de vision pour cette planche, et
        /// leur type — l'image telle quelle, ou le rendu du SVG.
        ///
        /// UN SEUL ENDROIT OÙ CE CHOIX SE FAIT. Il se prenait à deux endroits,
        /// chacun avec sa règle, et l'un des deux oubliait les SVG vectoriels.
        /// </summary>
        public static (byte[] Donnees, string TypeMime)? PourLaVision(
            byte[]? donnees, string? typeMime)
        {
            if (donnees is not { Length: > 0 }) return null;

            if (!string.Equals(typeMime, "image/svg+xml", StringComparison.OrdinalIgnoreCase))
            {
                return (donnees, typeMime ?? "image/png");
            }

            // Une enveloppe SVG autour d'une image matricielle : l'image
            // embarquée vaut mieux que le rendu, elle n'a rien perdu.
            var embarquee = ImageEmbarqueeSvg.Extraire(donnees);
            if (embarquee is { } image) return image;

            var png = EnPng(donnees);
            return png is null ? null : (png, "image/png");
        }
    }
}
