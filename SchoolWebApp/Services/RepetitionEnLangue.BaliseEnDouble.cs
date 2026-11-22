using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    public static partial class RepetitionEnLangue
    {
        /// <summary>La demande de répétition que le code écrit (la même que l'exercice : « … [EN]Blue[/EN]. Tu répètes ? »).</summary>
        public const string DemandeDuCode = "Tu répètes ?";

        /// <summary>
        /// LE MOT DIT DEUX FOIS, DONT UNE AVANT TOUTE EXPLICATION — contrôle d'Omar sur le lot groupé v3 (89f010e, point 10 ;
        /// bout en bout QR2, conv. de test 1156, 1er essai après le retour) : « [EN]Blue[/EN]⏎⏎C'est le mot en français, tu as
        /// bien compris ce que ça voulait dire ! Maintenant écoute le son anglais et essaie de le répéter : [EN]Blue[/EN] ».
        /// L'élève entend le mot, puis l'explication, puis le mot encore, et la question ne vient pas en dernier. Quand le
        /// message COMMENCE par la balise de voix et que le même mot revient plus loin, la 1re balise part (celle qui suit
        /// l'explication reste) ; s'il ne finit pas par une demande, le code ajoute « Tu répètes ? ». Rien d'autre ne bouge :
        /// une balise unique, ou en tête mais seule, reste telle quelle. Générique : tout professeur de langue, toute
        /// répétition en langue.
        ///
        /// TÂCHE 0000o (22/11/2026) — DÉCISION DU COORDINATEUR, règle de Min's « des consignes plutôt que des filtres » : plus
        /// branché dans le flux ni dans l'historique (la demande ajoutée par le code doublait celle du professeur, point 7
        /// d'Omar sur la v4). La consigne « une seule balise, après l'explication » est dans le fait (<see cref="Fait"/>).
        /// </summary>
        public static string SansBaliseEnDouble(string? texte)
        {
            if (string.IsNullOrEmpty(texte)) return texte ?? string.Empty;
            var premiere = Balise().Match(texte);
            if (!premiere.Success || !string.IsNullOrWhiteSpace(texte[..premiere.Index])) return texte;

            var mot = premiere.Groups["mot"].Value.Trim().TrimEnd('.', '!', '?').Trim();
            var reprise = Balise().Matches(texte).Skip(1).LastOrDefault(m =>
                string.Equals(m.Groups["b"].Value, premiere.Groups["b"].Value, StringComparison.OrdinalIgnoreCase)
                && string.Equals(m.Groups["mot"].Value.Trim().TrimEnd('.', '!', '?').Trim(), mot, StringComparison.OrdinalIgnoreCase));
            if (reprise is null) return texte;

            // La balise de tête, sa ponctuation et ses blancs.
            var apresTete = Regex.Match(texte[(premiere.Index + premiere.Length)..], @"^[\s.!,;:]*").Length;
            var reste = texte[..premiere.Index] + texte[(premiere.Index + premiere.Length + apresTete)..];

            // Ce qui suit la dernière balise : une question ou une demande, sinon la demande du code.
            var derniere = Balise().Matches(reste).Last();
            var fin = reste[(derniere.Index + derniere.Length)..];
            if (!fin.Contains('?') && !DemandeDeRepetition().IsMatch(fin)) reste = reste.TrimEnd() + "\n\n" + DemandeDuCode;
            return reste;
        }

        /// <summary>
        /// Le filtre du flux : un message qui commence par une balise de voix est retenu en entier (il faut savoir si le mot
        /// revient plus loin) ; tout autre message part sans retard.
        /// </summary>
        public sealed class FiltreBaliseEnDouble
        {
            private readonly StringBuilder _tampon = new();
            private bool? _retenir;

            public bool ARetire { get; private set; }

            public string Passer(string? fragment)
            {
                if (string.IsNullOrEmpty(fragment)) return string.Empty;
                if (_retenir == false) return fragment;
                _tampon.Append(fragment);
                var texte = _tampon.ToString();
                var i = 0;
                while (i < texte.Length && char.IsWhiteSpace(texte[i])) i++;
                if (i == texte.Length) return string.Empty;
                _retenir = texte[i] == '[';
                if (_retenir == true) return string.Empty;
                _tampon.Clear();
                return texte;
            }

            public string Terminer()
            {
                var texte = _tampon.ToString();
                _tampon.Clear();
                if (_retenir != true) return texte;
                var sortie = SansBaliseEnDouble(texte);
                ARetire = sortie != texte;
                return sortie;
            }
        }
    }
}
