namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// QUELLES PLANCHES UN ÉLÈVE PEUT SE VOIR PROPOSER, SELON SA CLASSE —
    /// signalement #47 du 20/11/2026.
    ///
    /// LE BUG. Un élève de CP demande à comparer des longueurs ; Nora affiche
    /// `math-perimetre-aire` (6e) : un rectangle L × l, un cercle de rayon r,
    /// P = 2 × π × r. Elle le voyait bien annoncée « classe : 6e » — mais ce
    /// n'était qu'une consigne, et une consigne ne vaut pas un fait (voir
    /// `BibliothequePlanchesService.Rediger`). Le titre parlait de longueur, la
    /// planche est partie.
    ///
    /// LE CORRECTIF : ne plus ANNONCER au professeur les planches trop au-dessus
    /// de la classe de l'élève. Ce qu'il ne voit pas dans sa liste n'existe pas
    /// pour lui : il dessine, comme pour un sujet sans planche.
    ///
    /// UN AN D'AVANCE EST TOLÉRÉ (`Marge`). Un CM2 qui prépare la 6e, un
    /// élève en avance, un pont entre deux années : la planche de l'année
    /// suivante reste utile. Au-delà, c'est une autre notion.
    ///
    /// LES PLANCHES DES CLASSES INFÉRIEURES RESTENT TOUTES — Camara y tient
    /// (17/09/2026) : revenir sur la droite graduée du CP pour débloquer un
    /// prérequis est une bonne façon d'enseigner.
    ///
    /// UNE PLANCHE SANS NIVEAU RESTE ANNONCÉE À TOUS. Les planches importées
    /// avant le 12/10/2026 n'en ont pas (migration `PlancheNiveau`) ; on ne
    /// sait pas les juger, et les retirer priverait tout le monde.
    ///
    /// UNE CLASSE INCONNUE NE FILTRE RIEN : un élève sans niveau garde la liste
    /// entière, comme avant ce correctif.
    /// </summary>
    public static class NiveauxDesPlanches
    {
        /// <summary>Années d'avance tolérées sur la classe de l'élève.</summary>
        public const int Marge = 1;

        /// <summary>Rang le plus haut (terminale).</summary>
        public const int RangMax = 12;

        /// <summary>
        /// Le rang d'année d'une PLANCHE, d'après l'étiquette de l'écran
        /// d'import (« CP », « CE1 », « 6e », « 2de », « 1re », « Tle »). Les
        /// formes longues (« 6ème », « Seconde », « Terminale ») sont lues aussi.
        /// Null si l'étiquette est absente ou illisible.
        /// </summary>
        public static int? RangPlanche(string? niveau)
        {
            if (string.IsNullOrWhiteSpace(niveau)) return null;

            var n = Normaliser(niveau);

            switch (n)
            {
                case "cp": return 1;
                case "ce1": return 2;
                case "ce2": return 3;
                case "cm1": return 4;
                case "cm2": return 5;
                case "2de": case "2nde": case "seconde": return 10;
                case "1re": case "1ere": case "premiere": return 11;
                case "tle": case "term": case "terminale": return 12;
            }

            if (n.StartsWith("seconde")) return 10;
            if (n.StartsWith("premiere")) return 11;
            if (n.StartsWith("terminale")) return 12;

            // « 6e », « 6eme », « 5e »… jusqu'à « 3e ».
            if (n.Length >= 1 && n[0] >= '3' && n[0] <= '6'
                && (n.Length == 1 || n[1..] is "e" or "eme"))
            {
                return 6 + ('6' - n[0]);
            }

            return null;
        }

        /// <summary>
        /// Le rang d'année d'un ÉLÈVE, d'après le code de sa classe
        /// (`NiveauScolaire.Code` : CP, CE1… SIXIEME… SECONDE_PRO,
        /// TERMINALE_STMG). Même échelle que `NiveauScolaire.Ordre`.
        /// </summary>
        public static int? RangEleve(string? niveauCode)
        {
            if (string.IsNullOrWhiteSpace(niveauCode)) return null;

            var c = niveauCode.Trim().ToUpperInvariant();

            return c switch
            {
                "CP" => 1,
                "CE1" => 2,
                "CE2" => 3,
                "CM1" => 4,
                "CM2" => 5,
                _ when c.StartsWith("SIXIEME") => 6,
                _ when c.StartsWith("CINQUIEME") => 7,
                _ when c.StartsWith("QUATRIEME") => 8,
                _ when c.StartsWith("TROISIEME") => 9,
                _ when c.StartsWith("SECONDE") => 10,
                _ when c.StartsWith("PREMIERE") => 11,
                _ when c.StartsWith("TERMINALE") => 12,
                _ => null,
            };
        }

        /// <summary>
        /// Le rang le plus haut qu'on annonce à cet élève, ou null pour ne rien
        /// filtrer (classe inconnue, ou déjà en terminale).
        /// </summary>
        public static int? Plafond(string? niveauCodeEleve)
        {
            var rang = RangEleve(niveauCodeEleve);
            if (rang is null) return null;

            var plafond = rang.Value + Marge;
            return plafond >= RangMax ? null : plafond;
        }

        /// <summary>La planche peut-elle être annoncée sous ce plafond ?</summary>
        public static bool EstAnnoncee(string? niveauPlanche, int? plafond)
        {
            if (plafond is null) return true;

            var rang = RangPlanche(niveauPlanche);
            return rang is null || rang.Value <= plafond.Value;
        }

        private static string Normaliser(string niveau)
        {
            var bas = niveau.Trim().ToLowerInvariant()
                .Replace("è", "e").Replace("é", "e").Replace("ᵉ", "e")
                .Replace(" ", string.Empty).Replace(".", string.Empty);

            return bas;
        }
    }
}
