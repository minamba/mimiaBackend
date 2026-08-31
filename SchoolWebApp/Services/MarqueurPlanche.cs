using SkiaSharp;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Dessine sur la planche l'endroit que l'élève a montré du doigt.
    ///
    /// POURQUOI DESSINER, ALORS QU'ON ENVOIE DÉJÀ L'IMAGE ET LA POSITION
    /// ----------------------------------------------------------------
    /// Parce que joindre les deux séparément ne suffit pas, et l'essai en
    /// séance l'a prouvé deux fois de suite.
    ///
    /// Premier essai : la position seule. Le professeur devait deviner où
    /// tombait « 53 % de la largeur » sur une carte qu'il ne voyait pas. Il a
    /// répondu « Grand Est » pour un clic sur l'Île-de-France.
    ///
    /// Deuxième essai : la position ET l'image. Il a répondu « Hauts-de-France,
    /// mais je ne suis pas certain ». Mieux, toujours faux — parce qu'on lui
    /// demandait encore de reporter lui-même deux pourcentages sur une image,
    /// c'est-à-dire un calcul de coordonnées à l'œil. C'est exactement ce qu'un
    /// modèle de vision fait le plus mal.
    ///
    /// Avec la marque dessinée, la question change de nature : ce n'est plus
    /// « où tombe 53 % / 27 % ? » mais « dans quelle région se trouve ce
    /// cercle ? ». Reconnaître, au lieu de calculer.
    ///
    /// RIEN N'EST CONSERVÉ. L'image marquée vit le temps d'un appel : elle
    /// n'est ni écrite sur disque, ni enregistrée en base, ni transcrite.
    /// </summary>
    public static class MarqueurPlanche
    {
        /// <summary>
        /// Au-delà, on rétrécit. Un modèle de vision ne gagne rien à recevoir
        /// du 4000 pixels, et chaque pixel se paie en jetons.
        /// </summary>
        private const int CoteMax = 1400;

        /// <summary>
        /// Part du petit côté reprise dans l'agrandissement, autour du clic.
        ///
        /// POURQUOI UN AGRANDISSEMENT EN PLUS DE LA CIBLE
        /// ---------------------------------------------
        /// Troisième essai, et troisième leçon. Avec la cible dessinée, le
        /// professeur regarde le bon endroit — mais sur une planche d'anatomie,
        /// l'urètre et le canal déférent sont séparés de quelques pixels, et
        /// leurs étiquettes se ressemblent. Il a nommé le voisin.
        ///
        /// La cible dit OÙ. L'agrandissement dit QUOI : à cette échelle, le
        /// trait de rappel qui part de la zone et le mot au bout deviennent
        /// lisibles. On ne lui demande plus de reconnaître un organe à sa
        /// forme, mais de lire une étiquette.
        ///
        /// LA LARGEUR EST CE QUI COMPTE, PAS LA SURFACE. Un carré serré autour
        /// du clic montrait l'anatomie et coupait les étiquettes — or ce sont
        /// les étiquettes qu'on vient lire. Sur ces planches, les mots sont
        /// rejetés dans les marges gauche et droite, à bonne distance de ce
        /// qu'ils désignent : il faut donc prendre LARGE horizontalement, et le
        /// gros plan est un bandeau, pas une loupe ronde.
        ///
        /// 45 % de la largeur : assez pour attraper la colonne de légendes la
        /// plus proche, et il reste un grossissement d'environ deux fois.
        /// </summary>
        private const double PartAgrandie = 0.45;

        /// <summary>
        /// Les octets de la planche avec une cible à l'endroit montré, ou les
        /// octets d'origine si le dessin échoue — un format inattendu ne doit
        /// pas priver le professeur de l'image.
        /// </summary>
        public static (byte[] Donnees, string TypeMime) Marquer(
            byte[] donnees, double x, double y)
        {
            try
            {
                using var origine = SKBitmap.Decode(donnees);
                if (origine is null) return (donnees, "image/png");

                var echelle = Math.Min(
                    1.0,
                    (double)CoteMax / Math.Max(origine.Width, origine.Height));

                var largeur = Math.Max(1, (int)(origine.Width * echelle));
                var hauteur = Math.Max(1, (int)(origine.Height * echelle));

                // L'agrandissement occupe un bandeau sous la planche. Carré, et
                // aussi large que la place le permet : c'est lui qu'on vient
                // lire, il ne doit pas être le parent pauvre de la composition.
                var cote = (int)(largeur * PartAgrandie);
                var bandeau = Math.Min(largeur, (int)(hauteur * 0.45));

                using var surface = SKSurface.Create(
                    new SKImageInfo(largeur, hauteur + bandeau));
                var toile = surface.Canvas;

                // Fond blanc : beaucoup de planches ont un fond transparent, et
                // sans lui elles arriveraient sur du noir, illisibles.
                toile.Clear(SKColors.White);

                using var image = SKImage.FromBitmap(origine);
                var lissage = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

                toile.DrawImage(image, new SKRect(0, 0, largeur, hauteur), lissage);

                var cx = (float)(x * largeur);
                var cy = (float)(y * hauteur);

                Dessiner(toile, cx, cy, largeur, hauteur);
                Agrandir(toile, image, cx, cy, largeur, hauteur, cote, bandeau);

                using var rendu = surface.Snapshot();
                using var encode = rendu.Encode(SKEncodedImageFormat.Png, 90);

                return (encode.ToArray(), "image/png");
            }
            catch (Exception)
            {
                return (donnees, "image/png");
            }
        }

        /// <summary>
        /// Le bandeau d'agrandissement, sous la planche.
        ///
        /// On y redessine la même zone à trois ou quatre fois sa taille, cible
        /// comprise. Le professeur y lit l'étiquette au bout du trait de rappel
        /// plutôt que de reconnaître une forme — c'est ce qu'il sait faire de
        /// loin le mieux, et ce qu'on lui demandait pourtant le moins.
        ///
        /// Le carré de découpe est BORNÉ AUX BORDS de la planche : un clic près
        /// d'un coin donnerait sinon un agrandissement à moitié vide, et la
        /// zone désignée se retrouverait excentrée dans son propre gros plan.
        /// </summary>
        private static void Agrandir(
            SKCanvas toile, SKImage image, float cx, float cy,
            int largeur, int hauteur, int cote, int bandeau)
        {
            var titre = Math.Max(18f, bandeau * 0.075f);
            var marge = bandeau * 0.05f;

            // La destination occupe TOUTE la largeur disponible : c'est elle qui
            // fixe le rapport du gros plan, et non l'inverse. Un cadre carré
            // gâchait la moitié du bandeau en marges latérales.
            var vers = new SKRect(
                marge, hauteur + titre + marge,
                largeur - marge, hauteur + bandeau - marge);

            // La découpe reprend ce rapport, sinon l'image serait étirée — et
            // une anatomie étirée, sur une planche de SVT, est un contresens.
            var largeurDecoupe = cote;
            var hauteurDecoupe = largeurDecoupe * vers.Height / vers.Width;

            var gauche = Math.Clamp(
                cx - largeurDecoupe / 2f, 0, Math.Max(0, largeur - largeurDecoupe));
            var haut = Math.Clamp(
                cy - hauteurDecoupe / 2f, 0, Math.Max(0, hauteur - hauteurDecoupe));

            var decoupe = new SKRect(
                gauche, haut, gauche + largeurDecoupe, haut + hauteurDecoupe);

            // Le fond du bandeau, pour séparer franchement les deux vues : sans
            // lui, l'agrandissement passerait pour une partie de la planche.
            using var fond = new SKPaint { Color = new SKColor(0xF2, 0xF4, 0xF6) };
            toile.DrawRect(new SKRect(0, hauteur, largeur, hauteur + bandeau), fond);

            // ET IL FAUT LE NOMMER, PAS SEULEMENT LE SÉPARER.
            //
            // Beaucoup de planches portent déjà leur propre encart intitulé
            // « Zoom : … ». Posé juste en dessous, notre agrandissement se
            // confondait avec lui : le professeur a annoncé « ton clic tombe
            // sur l'épididyme, dans l'encart zoomé » alors que la cible était
            // ailleurs. Il lisait le zoom de la planche, pas le nôtre.
            //
            // Un titre en capitales sur bandeau sombre, dans une formulation
            // qu'aucune planche n'emploie, lève l'ambiguïté.
            var hauteurTitre = Math.Max(18f, bandeau * 0.075f);
            using var barre = new SKPaint { Color = new SKColor(0x14, 0x14, 0x18) };
            toile.DrawRect(
                new SKRect(0, hauteur, largeur, hauteur + hauteurTitre), barre);

            using var police = new SKFont(SKTypeface.Default, hauteurTitre * 0.62f)
            {
                Embolden = true,
            };
            using var encre = new SKPaint { Color = SKColors.White, IsAntialias = true };

            toile.DrawText(
                "AGRANDISSEMENT DE L'ENDROIT CLIQUE PAR L'ELEVE",
                largeur / 2f, hauteur + hauteurTitre * 0.72f,
                SKTextAlign.Center, police, encre);

            toile.DrawImage(
                image, ProjeterSurOrigine(decoupe, image, largeur, hauteur), vers,
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

            // La cible, redessinée à l'échelle du gros plan.
            var facteur = vers.Width / decoupe.Width;
            Dessiner(
                toile,
                vers.Left + (cx - decoupe.Left) * facteur,
                vers.Top + (cy - decoupe.Top) * facteur,
                (int)vers.Width, (int)vers.Height);

            using var cadre = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(0x14, 0x14, 0x18),
                StrokeWidth = Math.Max(2f, vers.Height * 0.008f),
                IsAntialias = true,
            };
            toile.DrawRect(vers, cadre);
        }

        /// <summary>
        /// Le rectangle équivalent dans les pixels d'ORIGINE de l'image.
        ///
        /// La planche a été réduite pour l'affichage : découper dans les
        /// coordonnées réduites puis piocher dans l'originale décalerait le gros
        /// plan de tout le facteur de réduction.
        /// </summary>
        private static SKRect ProjeterSurOrigine(
            SKRect decoupe, SKImage image, int largeur, int hauteur)
        {
            var fx = (float)image.Width / largeur;
            var fy = (float)image.Height / hauteur;

            return new SKRect(
                decoupe.Left * fx, decoupe.Top * fy,
                decoupe.Right * fx, decoupe.Bottom * fy);
        }

        /// <summary>
        /// UNE ÉPINGLE, COMME SUR UNE CARTE.
        ///
        /// L'anneau a été essayé, agrandi, doublé d'un liseré noir — et le
        /// professeur nommait encore l'organe voisin. Le défaut n'était pas sa
        /// visibilité mais sa FORME : un cercle posé sur une planche d'anatomie
        /// ressemble à ce qu'il y a dessus. Coupes rondes, alvéoles, noyaux,
        /// cellules : la figure est pleine de cercles, et le nôtre en devenait
        /// un de plus.
        ///
        /// Une épingle ne ressemble à rien de biologique. Sa silhouette est
        /// reconnue d'un coup, et surtout elle DÉSIGNE : la pointe touche le
        /// pixel exact, la tête est au-dessus et ne cache pas ce qu'on montre.
        /// Un anneau, lui, entoure une zone — et sur deux conduits voisins,
        /// « entourer » laisse le choix entre les deux.
        ///
        /// Le corps est presque noir, la tête blanche cerclée de noir, le point
        /// central rouge : trois valeurs très écartées, donc au moins deux qui
        /// tranchent sur n'importe quel fond.
        /// </summary>
        private static void Dessiner(SKCanvas toile, float x, float y, int largeur, int hauteur)
        {
            // Proportionnelle à l'image : une épingle de taille fixe serait
            // énorme sur une petite figure et invisible sur une grande.
            //
            // 2,1 % et non 3,0 % : à la taille précédente, la tête recouvrait
            // deux organes voisins sur une planche dense — et ce qu'elle cachait
            // était précisément ce que l'élève désignait. La pointe touche
            // toujours le pixel exact, c'est le corps qui a maigri.
            var rayon = Math.Max(8f, Math.Min(largeur, hauteur) * 0.021f);
            var hauteurTotale = rayon * 3.1f;

            // Le centre de la tête, au-dessus de la pointe.
            var tete = new SKPoint(x, y - hauteurTotale + rayon);

            // La goutte : un cercle, deux tangentes qui descendent vers la
            // pointe. Tracée à la main plutôt qu'avec un arc approché — la
            // silhouette est ce qui fait tout le travail, elle doit être franche.
            using var corps = new SKPath();
            var ouverture = 0.62f;            // demi-angle des tangentes, en radians
            var dx = rayon * (float)Math.Sin(ouverture);
            var dy = rayon * (float)Math.Cos(ouverture);

            corps.MoveTo(x, y);
            corps.LineTo(tete.X - dx, tete.Y + dy);
            corps.ArcTo(rayon, rayon, 0, SKPathArcSize.Large, SKPathDirection.Clockwise,
                        tete.X + dx, tete.Y + dy);
            corps.Close();

            using var halo = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(3f, rayon * 0.42f),
                Color = SKColors.White,
                IsAntialias = true,
                StrokeJoin = SKStrokeJoin.Round,
            };

            using var plein = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(0x14, 0x14, 0x18),
                IsAntialias = true,
            };

            using var pastille = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(0xE5, 0x39, 0x35),
                IsAntialias = true,
            };

            // Le halo d'abord : il détoure toute la silhouette, y compris la
            // pointe, et c'est lui qui la détache d'un fond sombre.
            toile.DrawPath(corps, halo);
            toile.DrawPath(corps, plein);
            toile.DrawCircle(tete.X, tete.Y, rayon * 0.42f, pastille);

            // LA POINTE DOIT RESTER VISIBLE, c'est elle qui porte la précision.
            // Un petit disque à l'endroit exact, cerclé de blanc : sans lui, on
            // sait quelle épingle regarder mais pas à quel pixel elle touche.
            using var socle = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(2f, rayon * 0.16f),
                Color = SKColors.White,
                IsAntialias = true,
            };

            toile.DrawCircle(x, y, rayon * 0.20f, socle);
            toile.DrawCircle(x, y, rayon * 0.20f, pastille);
        }

    }
}
