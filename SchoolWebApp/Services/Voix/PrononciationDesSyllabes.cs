using System.Text;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LA PRONONCIATION D'UNE SYLLABE, PAR LES RÈGLES DE LECTURE DU CP — Camara, le 07/11/2026 : « can » bien lu trois fois,
    /// trois fois refusé (« J'ai lu la syllabe quand », messages 10105 à 10109), puis abandonné. Le micro écrivait « quand »,
    /// juste ; mais les deux modèles qui donnent la prononciation de référence recevaient « can » comme un MOT, et
    /// répondaient /kan/ (comme « Cannes ») au lieu de /kɑ̃/. Une syllabe de la méthode de lecture se prononce par ses
    /// graphèmes : an, en, am → ɑ̃ ; in, ain, ein → ɛ̃ ; on → ɔ̃ ; ou → u ; oi → wa ; ch → ʃ ; gn → ɲ ; c, g devant e, i…
    ///
    /// LA SORTIE EST DÉJÀ DANS L'ALPHABET DE LA COMPARAISON (`RegleVerificationSon.NormaliserIpa`) : é et è valent e, o
    /// ouvert et fermé valent o, eu et e muet valent ø, w vaut u, ɲ vaut nj, les nasales sont A, E, O. Elle s'ajoute à celle
    /// des modèles (l'union se compare) : elle ne retire rien, elle répare les syllabes qui ne sont pas des mots.
    ///
    /// SEULEMENT POUR UNE SYLLABE DE LA MÉTHODE (banque d'Adrien ou images des syllabes) : « chat » d'une syllabe se dit
    /// sans son t, et ce n'est pas une syllabe de la méthode.
    /// </summary>
    public static class PrononciationDesSyllabes
    {
        /// <summary>Les graphèmes, du plus long au plus court : le premier qui convient l'emporte.</summary>
        private static readonly (string Graphie, string Son)[] Voyelles =
        [
            ("ouil", "uj"), ("euil", "øj"), ("eau", "o"), ("ain", "E"), ("ein", "E"), ("oin", "uE"), ("ien", "jE"), ("ion", "jO"),
            ("ail", "aj"), ("eil", "ej"), ("aim", "E"),
            ("an", "A"), ("am", "A"), ("en", "A"), ("em", "A"), ("in", "E"), ("im", "E"), ("yn", "E"), ("ym", "E"), ("un", "E"), ("um", "E"),
            ("on", "O"), ("om", "O"), ("ou", "u"), ("oi", "ua"), ("au", "o"), ("ai", "e"), ("ei", "e"), ("eu", "ø"), ("œu", "ø"),
            ("é", "e"), ("è", "e"), ("ê", "e"), ("ë", "e"), ("a", "a"), ("à", "a"), ("â", "a"), ("i", "i"), ("î", "i"), ("ï", "i"),
            ("o", "o"), ("ô", "o"), ("u", "y"), ("û", "y"), ("y", "i"),
        ];

        private const string LettresVoyelles = "aeiouyéèêëàâîïôûœ";

        /// <summary>Vrai si c'est une syllabe de la méthode de lecture : la banque d'Adrien, ou les images des syllabes.</summary>
        public static bool EstUneSyllabeDeLaMethode(string? cible)
        {
            var propre = (cible ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormC);
            return propre.Length > 0 && (BanqueDeSons.Contient(propre) || ImageAuTableau.SyllabesIllustrees.ContainsKey(propre));
        }

        /// <summary>La prononciation de la syllabe, dans l'alphabet de la comparaison ; vide si elle n'est pas lisible.</summary>
        public static string Ipa(string? syllabe)
        {
            var s = (syllabe ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormC);
            if (s.Length == 0 || !s.All(char.IsLetter)) return string.Empty;

            var sortie = new StringBuilder();
            var i = 0;
            while (i < s.Length)
            {
                var reste = s[i..];
                var suivante = (int k) => i + k < s.Length ? s[i + k] : '\0';

                // « y » devant une voyelle est une consonne : « ya » → ja.
                if (reste[0] == 'y' && LettresVoyelles.Contains(suivante(1)))
                {
                    sortie.Append('j'); i += 1; continue;
                }

                var voyelle = Voyelles.FirstOrDefault(v => reste.StartsWith(v.Graphie, StringComparison.Ordinal)
                    // Une nasale n'en est une que si aucune voyelle ne suit (« an » oui, « ane » non).
                    && (!EstNasale(v.Son) || !LettresVoyelles.Contains(i + v.Graphie.Length < s.Length ? s[i + v.Graphie.Length] : '\0')));
                if (voyelle.Graphie is not null)
                {
                    sortie.Append(voyelle.Son);
                    i += voyelle.Graphie.Length;
                    continue;
                }

                // « e » seul : muet en fin de syllabe ouverte (be → bø), è devant une consonne (ber, fer → bɛʁ).
                if (reste[0] == 'e')
                {
                    sortie.Append(i + 1 < s.Length ? "e" : "ø"); i += 1; continue;
                }

                // Les consonnes.
                if (reste.StartsWith("ch", StringComparison.Ordinal)) { sortie.Append('ʃ'); i += 2; continue; }
                if (reste.StartsWith("gn", StringComparison.Ordinal)) { sortie.Append("nj"); i += 2; continue; }
                if (reste.StartsWith("ph", StringComparison.Ordinal)) { sortie.Append('f'); i += 2; continue; }
                if (reste.StartsWith("qu", StringComparison.Ordinal)) { sortie.Append('k'); i += 2; continue; }
                if (reste.StartsWith("gu", StringComparison.Ordinal) && "eiéèy".Contains(suivante(2))) { sortie.Append('g'); i += 2; continue; }

                var c = reste[0];
                switch (c)
                {
                    case 'c': sortie.Append("eiéèêy".Contains(suivante(1)) ? 's' : 'k'); break;
                    case 'ç': sortie.Append('s'); break;
                    case 'g': sortie.Append("eiéèêy".Contains(suivante(1)) ? 'ʒ' : 'g'); break;
                    case 'j': sortie.Append('ʒ'); break;
                    case 'r': sortie.Append('ʁ'); break;
                    case 'x': sortie.Append("ks"); break;
                    case 'h': break;
                    case 'w': sortie.Append('u'); break;
                    default: sortie.Append(c); break;
                }

                i += 1;
            }

            return sortie.ToString();
        }

        private static bool EstNasale(string son) => son.Contains('A') || son.Contains('E') || son.Contains('O');

        /// <summary>
        /// Les prononciations de référence, celle des règles ajoutée quand la cible est une syllabe de la méthode (en
        /// français) : voir le résumé de la classe.
        /// </summary>
        public static IReadOnlySet<string> AvecLesRegles(IReadOnlySet<string> references, string cible, string langue)
        {
            if (langue != SchoolWebApp.Domain.Repositories.LanguesDesSons.Francais || !EstUneSyllabeDeLaMethode(cible)) return references;
            var regle = Ipa(cible);
            if (regle.Length == 0 || references.Contains(regle)) return references;
            return new HashSet<string>(references, StringComparer.Ordinal) { regle };
        }
    }
}
