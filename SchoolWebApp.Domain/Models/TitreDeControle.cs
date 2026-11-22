using System.Globalization;
using System.Text;

namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Ce qu'a donné une précision de sujet venue du professeur
    /// (<c>IControleScolaireRepository.PreciserSujetAsync</c>).
    /// </summary>
    public enum PrecisionDuSujet
    {
        /// <summary>Le contrôle n'est pas à cet élève dans cette matière.</summary>
        Introuvable,

        /// <summary>Le titre était vide, générique, ou posé par le professeur : il est remplacé.</summary>
        Remplace,

        /// <summary>
        /// Le titre a été choisi par la famille : il reste. La précision sert
        /// seulement à poser le programme.
        /// </summary>
        TitreGarde,
    }

    /// <summary>
    /// LE TITRE D'UN CONTRÔLE APPARTIENT À CELUI QUI L'A ÉCRIT.
    ///
    /// Signalement #54 (Adam, 21/11/2026) : un contrôle nommé par la famille
    /// « TEST ADAM - les plantes annuelles et vivaces » est devenu « Les plantes
    /// annuelles et vivaces » après une séance de révision, sans prévenir
    /// personne. Le professeur écrit un `sujet:` dans [CONTROLE_NOTIONS] et le
    /// serveur remplaçait le titre à chaque fois. Un parent qui nomme ses
    /// contrôles (« Contrôle de Mme Durand », « Évaluation n° 3 ») perdait son
    /// libellé.
    ///
    /// LA PRÉCISION GARDE SON BUT D'ORIGINE (Camara) : « contrôle de maths »
    /// tapé à la va-vite se complète en « Thalès » en parlant. On remplace donc
    /// encore un titre VIDE, un titre GÉNÉRIQUE (fait seulement de mots comme
    /// contrôle, évaluation, interro et d'un nom de matière — la carte affiche
    /// déjà la matière, rien n'est perdu) et un titre posé par le professeur
    /// que la famille n'a jamais modifié. Tout autre titre reste tel quel.
    /// </summary>
    public static class TitreDeControle
    {
        /// <summary>
        /// Le titre actuel peut-il être remplacé par une précision du
        /// professeur ?
        /// </summary>
        /// <param name="sujetActuel">Le titre en base.</param>
        /// <param name="posePar">ELEVE, PARENT ou PROFESSEUR.</param>
        /// <param name="modifieParLaFamille">
        /// Vrai si le parent ou l'enfant a déjà modifié le contrôle (la date
        /// de modification n'est posée que par eux).
        /// </param>
        /// <param name="matiereLibelle">La matière du contrôle (« Sciences »).</param>
        public static bool PeutEtreRemplace(
            string? sujetActuel, string? posePar, bool modifieParLaFamille, string? matiereLibelle)
        {
            if (string.IsNullOrWhiteSpace(sujetActuel)) return true;

            if (string.Equals(posePar, "PROFESSEUR", StringComparison.OrdinalIgnoreCase)
                && !modifieParLaFamille)
            {
                return true;
            }

            return EstGenerique(sujetActuel, matiereLibelle);
        }

        /// <summary>
        /// LE TITRE APRÈS UNE PRÉCISION — décision de Camara, le 21/11/2026 :
        /// « Contrôle de maths » précisé en « additions avec retenue » devient
        /// « Contrôle de maths : additions avec retenue ». Le titre de la
        /// famille reste en tête ; la précision le complète.
        ///
        /// À n'appeler que si <see cref="PeutEtreRemplace"/> a dit oui :
        /// <list type="bullet">
        /// <item>titre vide → la précision ;</item>
        /// <item>titre générique → « titre : précision » (la précision seule si
        /// elle reprend déjà le titre ; le titre inchangé si elle est elle-même
        /// générique — « contrôle de maths » n'apprend rien) ;</item>
        /// <item>titre posé par le professeur → la précision, comme avant.</item>
        /// </list>
        /// </summary>
        public static string NouveauTitre(string? sujetActuel, string precision, string? matiereLibelle)
        {
            var propre = precision.Trim();
            if (string.IsNullOrWhiteSpace(sujetActuel)) return UnSeulDeuxPoints(propre);

            var actuel = sujetActuel.Trim();
            if (!EstGenerique(actuel, matiereLibelle)) return UnSeulDeuxPoints(propre);

            // Le titre de la famille, rendu tel quel : on ne réécrit pas ce
            // qu'elle a tapé.
            if (EstGenerique(propre, matiereLibelle)) return actuel;

            var cleActuel = Cle(actuel);
            if (Cle(propre).StartsWith(cleActuel, StringComparison.Ordinal)) return UnSeulDeuxPoints(propre);

            // ESPACE INSÉCABLE AVANT LES DEUX-POINTS : sur une carte étroite, le
            // titre se coupait juste avant « : », qui commençait la ligne suivante.
            //
            // LA PRÉCISION N'APPORTE PAS SES PROPRES DEUX-POINTS (retest #54 d'Adam,
            // 21/11/2026) : « Contrôle de sciences : les animaux et leur
            // alimentation : herbivores, carnivores, omnivores ». Les deux-points
            // du titre complété sont les seuls ; ceux de la précision se
            // reformulent — voir SansDeuxPoints.
            propre = SansDeuxPoints(propre);
            return $"{actuel.TrimEnd(' ', ' ', ':', '-', '–', '—', '.')}{Insecable}: {propre}";
        }

        /// <summary>
        /// UN SEUL « : » DANS UN TITRE. Le premier reste (c'est lui qui sépare
        /// le contrôle de son sujet) ; ce qui suit passe par
        /// <see cref="SansDeuxPoints"/>.
        /// </summary>
        public static string UnSeulDeuxPoints(string titre)
        {
            var i = IndexDeuxPoints(titre, 0);
            if (i < 0) return titre;

            var suite = SansDeuxPoints(titre[(i + 1)..].TrimStart(' ', ' '));
            return suite.Length == 0 ? titre[..(i + 1)] : $"{titre[..(i + 1)]} {suite}";
        }

        /// <summary>
        /// Le texte sans aucun « : » de ponctuation. Le DERNIER, suivi d'une
        /// suite sans parenthèses, devient une parenthèse — « les animaux et
        /// leur alimentation (herbivores, carnivores, omnivores) » ; les autres
        /// deviennent un tiret « – ». Un « : » entre deux chiffres (« 10:30 »)
        /// n'est pas de la ponctuation : il reste.
        /// </summary>
        public static string SansDeuxPoints(string texte)
        {
            var positions = new List<int>();
            for (var i = IndexDeuxPoints(texte, 0); i >= 0; i = IndexDeuxPoints(texte, i + 1)) positions.Add(i);
            if (positions.Count == 0) return texte;

            var dernier = positions[^1];
            var gaucheDuDernier = texte[..dernier].TrimEnd(' ', ' ');
            var suite = texte[(dernier + 1)..].Trim(' ', ' ');

            string resultat;
            if (suite.Length == 0)
            {
                // « … : » en bout de texte : il n'annonce rien, il part.
                resultat = gaucheDuDernier;
            }
            else if (gaucheDuDernier.Length == 0)
            {
                resultat = suite;
            }
            else if (!suite.Contains('(') && !suite.Contains(')') && !gaucheDuDernier.EndsWith(')'))
            {
                resultat = $"{gaucheDuDernier} ({suite.TrimEnd('.', ' ', ' ')})";
            }
            else
            {
                resultat = $"{gaucheDuDernier} – {suite}";
            }

            // Les précédents, de droite à gauche : un tiret. Leurs positions
            // n'ont pas bougé, tout ce qui a changé est à leur droite.
            for (var k = positions.Count - 2; k >= 0; k--)
            {
                var p = positions[k];
                var gauche = resultat[..p].TrimEnd(' ', ' ');
                var droite = resultat[(p + 1)..].TrimStart(' ', ' ');
                resultat = gauche.Length == 0 ? droite : droite.Length == 0 ? gauche : $"{gauche} – {droite}";
            }

            return resultat;
        }

        /// <summary>
        /// La position du prochain « : » (ou « ： ») de ponctuation à partir de
        /// <paramref name="depart"/>, -1 s'il n'y en a plus. Entre deux
        /// chiffres, ce n'est pas de la ponctuation (une heure, un rapport).
        /// </summary>
        private static int IndexDeuxPoints(string texte, int depart)
        {
            for (var i = depart; i < texte.Length; i++)
            {
                if (texte[i] is not (':' or '：')) continue;

                var entreChiffres = i > 0 && i + 1 < texte.Length
                    && char.IsDigit(texte[i - 1]) && char.IsDigit(texte[i + 1]);

                if (!entreChiffres) return i;
            }

            return -1;
        }

        /// <summary>L'espace insécable posée devant les deux-points du titre complété.</summary>
        public const char Insecable = ' ';

        /// <summary>Le texte en minuscules, sans accents ni ponctuation, pour comparer deux titres.</summary>
        private static string Cle(string texte)
        {
            var sb = new StringBuilder();
            foreach (var c in SansAccents(texte.ToLowerInvariant()))
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Vrai si le titre ne dit rien de plus que « un contrôle dans cette
        /// matière » : chaque mot est un mot de contrôle, un petit mot ou un
        /// nom de matière. Un chiffre, un nom propre, un thème : ce n'est plus
        /// générique.
        /// </summary>
        public static bool EstGenerique(string? titre, string? matiereLibelle)
        {
            if (string.IsNullOrWhiteSpace(titre)) return true;

            var mots = Mots(titre);
            if (mots.Count == 0) return true;

            var matiere = Mots(matiereLibelle ?? string.Empty);

            foreach (var mot in mots)
            {
                if (mot.Any(char.IsDigit)) return false;
                if (MotsGeneriques.Contains(mot) || matiere.Contains(mot)) continue;
                return false;
            }

            return true;
        }

        private static readonly HashSet<string> MotsGeneriques = new(StringComparer.Ordinal)
        {
            // Le contrôle lui-même.
            "controle", "controles", "control", "ctrl", "evaluation", "evaluations", "eval", "evals",
            "interro", "interros", "interrogation", "interrogations", "devoir", "devoirs",
            "ds", "dst", "test", "tests", "examen", "examens", "exam", "epreuve",
            "surveille", "surveilles", "ecrite", "ecrit",
            // Les petits mots.
            "de", "d", "du", "des", "en", "la", "le", "les", "l", "un", "une", "mon", "ma",
            "a", "au", "aux", "sur", "et",
            // Les matières et leurs abréviations courantes.
            "maths", "math", "mathematiques", "mathematique", "francais", "fr", "sciences", "science",
            // PAS les sous-domaines (conjugaison, géométrie…) : ils disent déjà
            // sur quoi porte le contrôle, la carte ne les affiche pas ailleurs.
            "svt", "physique", "chimie", "pc", "histoire", "geo", "geographie", "hg",
            "anglais", "espagnol", "allemand", "italien", "latin", "grec", "emc", "techno", "technologie",
            "philo", "philosophie", "ses", "nsi", "snt", "eps", "musique", "arts", "plastiques",
        };

        private static HashSet<string> Mots(string texte)
        {
            var sansAccents = SansAccents(texte.ToLowerInvariant());
            var mots = new HashSet<string>(StringComparer.Ordinal);
            var courant = new StringBuilder();

            foreach (var c in sansAccents)
            {
                if (char.IsLetterOrDigit(c))
                {
                    courant.Append(c);
                    continue;
                }

                if (courant.Length > 0) mots.Add(courant.ToString());
                courant.Clear();
            }

            if (courant.Length > 0) mots.Add(courant.ToString());
            return mots;
        }

        private static string SansAccents(string texte)
        {
            var decompose = texte.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decompose.Length);

            foreach (var c in decompose)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
