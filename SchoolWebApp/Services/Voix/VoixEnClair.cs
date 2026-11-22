using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// UN MOT DÉJÀ AU TABLEAU NE SE CACHE PAS — signalement #61 d'Adam, le 22/11/2026, Français CP : à la reprise de la
    /// dictée de la veille (« chat » dicté, « cha » écrit), Adrien a écrit « le mot à écrire était [FR]chat[/FR]. Tu as
    /// bien entendu le début, [FR]cha[/FR] ! … Écoute bien : [FR]t[/FR]. ». La balise de voix [FR] est faite pour les
    /// exercices d'ÉCOUTE : l'écran n'affiche jamais ce qu'elle entoure, pour que l'enfant écoute au lieu de lire (voir
    /// `ardoise.js`, LANGUES_ECOUTE). L'enfant lisait donc « le mot à écrire était . Tu as bien entendu le début,  ! ».
    ///
    /// Pendant la correction d'une dictée, il n'y a plus rien à cacher : le texte dicté et la copie sont au tableau.
    /// Le passage [FR]…[/FR] repasse donc EN CLAIR, entre guillemets (« chat »), affiché et lu à la voix normale.
    ///
    /// PORTÉE : générique — toute correction de dictée, quel que soit le professeur qui écrit en [FR]. Les autres
    /// balises de langue ([EN], [ES]…) ne sont pas touchées : elles portent la prononciation du mot étranger, que la
    /// voix française ne saurait pas rendre.
    /// </summary>
    public static partial class VoixEnClair
    {
        private const string Ouverture = "[FR]";
        private const string Fermeture = "[/FR]";

        /// <summary>
        /// Rappel pour le professeur, au tour où la dictée est au tableau. Sans crochet fermant interne : il ne
        /// s'affiche jamais.
        /// </summary>
        public const string Consigne =
            "[Les mots de cette dictée sont AU TABLEAU : quand tu les cites, écris-les en clair entre guillemets "
            + "(« … »), jamais dans la balise de voix du français (FR) — l'écran n'affiche pas ce qu'elle contient, "
            + "l'élève lirait un blanc.]";

        /// <summary>Le texte entier, chaque passage [FR]…[/FR] remis en clair entre guillemets.</summary>
        public static string Corriger(string? texte)
        {
            if (string.IsNullOrEmpty(texte) || !texte.Contains("FR]", StringComparison.Ordinal)) return texte ?? string.Empty;

            var clair = Passage().Replace(texte, m =>
            {
                var mot = m.Groups["mot"].Value.Trim();
                var ouvre = m.Groups["ouv"].Value;
                var ferme = m.Groups["ferm"].Value;

                if (mot.Length == 0) return ouvre.Length > 0 && ferme.Length > 0 ? string.Empty : ouvre + ferme;
                if (ouvre.Length == 0 && ferme.Length == 0) return $"« {mot} »";
                return ouvre + mot + ferme;
            });

            // Une balise orpheline (jamais refermée, ou fermeture seule) : l'écran cacherait tout ce qui la suit.
            return BaliseSeule().Replace(clair, string.Empty);
        }

        [GeneratedRegex(@"(?<ouv>«[   ]*|""|“)?\[FR\](?<mot>[^\[\]]*?)\[/FR\](?<ferm>[   ]*»|""|”)?")]
        private static partial Regex Passage();

        [GeneratedRegex(@"\[/?FR\]")]
        private static partial Regex BaliseSeule();

        /// <summary>
        /// Le même travail sur un texte qui arrive par morceaux (écran ET voix). Un passage n'est corrigé qu'entier :
        /// on retient ce qui peut encore devenir une balise, un passage ouvert, un passage fermé dont on ne connaît
        /// pas encore le guillemet suivant, et le guillemet ouvrant qui le précède. Un texte sans [FR] part sans retard.
        /// </summary>
        public sealed class Filtre
        {
            /// <summary>Au-delà, on n'attend plus la fermeture : mieux vaut un texte qui part.</summary>
            private const int AttenteMaximum = 300;

            private string _tampon = string.Empty;

            /// <summary>Vrai si au moins un passage a été remis en clair (pour le journal).</summary>
            public bool ARemplace { get; private set; }

            public string Passer(string fragment)
            {
                _tampon += fragment;

                var coupe = PointDeRetenue(_tampon);
                if (_tampon.Length - coupe > AttenteMaximum) return Terminer();

                var tete = _tampon[..coupe];
                _tampon = _tampon[coupe..];
                return Sortir(tete);
            }

            public string Terminer()
            {
                var reste = _tampon;
                _tampon = string.Empty;
                return Sortir(reste);
            }

            private string Sortir(string texte)
            {
                if (texte.Length == 0) return texte;
                var clair = Corriger(texte);
                if (!ReferenceEquals(clair, texte) && clair != texte) ARemplace = true;
                return clair;
            }
        }

        /// <summary>L'index à partir duquel le texte doit attendre la suite (sa longueur s'il peut tout partir).</summary>
        internal static int PointDeRetenue(string t)
        {
            // Un passage ouvert, pas encore refermé.
            var ouvert = t.LastIndexOf(Ouverture, StringComparison.Ordinal);
            if (ouvert >= 0 && t.IndexOf(Fermeture, ouvert, StringComparison.Ordinal) < 0)
            {
                return AvecGuillemetOuvrant(t, ouvert);
            }

            // Un passage refermé tout au bout : le guillemet fermant peut encore arriver.
            var ferme = t.LastIndexOf(Fermeture, StringComparison.Ordinal);
            if (ferme >= 0 && string.IsNullOrWhiteSpace(t[(ferme + Fermeture.Length)..]))
            {
                var debut = t.LastIndexOf(Ouverture, ferme, StringComparison.Ordinal);
                return AvecGuillemetOuvrant(t, debut >= 0 ? debut : ferme);
            }

            // Le début d'une balise, coupé par le flux : « [ », « [F », « [/F »…
            for (var k = Math.Min(Fermeture.Length - 1, t.Length); k >= 1; k--)
            {
                var fin = t[^k..];
                if (Ouverture.StartsWith(fin, StringComparison.Ordinal) || Fermeture.StartsWith(fin, StringComparison.Ordinal))
                {
                    return AvecGuillemetOuvrant(t, t.Length - k);
                }
            }

            // Un guillemet ouvrant tout au bout : une balise peut le suivre.
            var sansBlanc = t.TrimEnd(' ', ' ', ' ');
            if (sansBlanc.Length > 0 && sansBlanc[^1] is '«' or '"' or '“') return sansBlanc.Length - 1;

            return t.Length;
        }

        private static int AvecGuillemetOuvrant(string t, int index)
        {
            var j = index;
            while (j > 0 && t[j - 1] is ' ' or ' ' or ' ') j--;
            return j > 0 && t[j - 1] is '«' or '"' or '“' ? j - 1 : index;
        }
    }
}
