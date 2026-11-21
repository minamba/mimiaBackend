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
            if (string.IsNullOrWhiteSpace(sujetActuel)) return propre;

            var actuel = sujetActuel.Trim();
            if (!EstGenerique(actuel, matiereLibelle)) return propre;

            if (EstGenerique(propre, matiereLibelle)) return actuel;

            var cleActuel = Cle(actuel);
            if (Cle(propre).StartsWith(cleActuel, StringComparison.Ordinal)) return propre;

            // ESPACE INSÉCABLE AVANT LES DEUX-POINTS : sur une carte étroite, le
            // titre se coupait juste avant « : », qui commençait la ligne suivante.
            return $"{actuel.TrimEnd(' ', ' ', ':', '-', '–', '—', '.')}{Insecable}: {propre}";
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
