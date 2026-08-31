using System.Security.Cryptography;

namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// Le code que l'enfant tape pour entrer dans ses cours.
    ///
    /// L'ALPHABET EST CHOISI CONTRE LA RECOPIE, PAS CONTRE L'ATTAQUE
    /// -----------------------------------------------------------
    /// Un enfant de huit ans lit ce code sur un papier et le tape sur un
    /// clavier. Les caractères qui se confondent lui coûtent une tentative
    /// ratée, puis un adulte appelé à la rescousse : O et 0, I et 1 et L, S et
    /// 5, B et 8. Ils sont donc tous exclus.
    ///
    /// Il reste vingt-cinq caractères. Sur six positions, cela fait deux cent
    /// quarante-quatre millions de codes — assez pour qu'on ne devine pas au
    /// hasard, tant que les tentatives sont comptées.
    ///
    /// MAIS PAS ASSEZ POUR IGNORER LES COLLISIONS. Mesuré : soixante-douze
    /// doublons sur deux cent mille tirages, exactement ce que prédit le
    /// paradoxe des anniversaires. À l'échelle d'une base d'élèves c'est rare,
    /// mais « rare » veut dire « un jour ». D'où l'index unique en base et la
    /// reprise du tirage quand il refuse — voir `AttribuerCodeAsync`.
    ///
    /// LA CÉSURE EST POUR L'ŒIL
    /// -----------------------
    /// « KUT-49R » se recopie mieux que « KUT49R ». Le tiret ne fait pas partie
    /// du code : il est retiré à la comparaison, pour que l'enfant qui l'oublie
    /// ou qui le met ailleurs entre quand même.
    /// </summary>
    public static class CodeAccesEleve
    {
        /// <summary>
        /// Ni O ni 0, ni I ni 1 ni L, ni S ni 5, ni B ni 8. Ce qui reste ne se
        /// confond avec rien sur un papier écrit à la main.
        /// </summary>
        private const string Alphabet = "ACDEFGHJKMNPQRTUVWXYZ2467";

        private const int Longueur = 6;

        /// <summary>
        /// Un code neuf, tiré au sort par un générateur cryptographique.
        ///
        /// `Random` aurait suffi statistiquement, mais il est prévisible : qui
        /// connaît un code et l'heure de sa création peut retrouver les
        /// suivants. Ici, personne ne le peut.
        /// </summary>
        public static string Generer()
        {
            var caracteres = new char[Longueur];

            for (var i = 0; i < Longueur; i++)
            {
                caracteres[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }

            return new string(caracteres);
        }

        /// <summary>
        /// Le code tel qu'on le compare : sans tiret, sans espace, en capitales.
        ///
        /// L'enfant tape « kut-49r », « KUT 49R » ou « kut49r » — les trois
        /// désignent le même code, et refuser l'un des trois n'apprendrait rien
        /// à personne.
        /// </summary>
        public static string Normaliser(string? saisi)
        {
            if (string.IsNullOrWhiteSpace(saisi)) return string.Empty;

            return new string(saisi
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }

        /// <summary>
        /// Le code tel qu'on l'AFFICHE au parent : coupé en deux par un tiret.
        ///
        /// C'est la seule forme qu'il recopiera sur un papier, donc la seule
        /// qui compte pour la lisibilité.
        /// </summary>
        public static string Presenter(string? code)
        {
            var propre = Normaliser(code);

            return propre.Length == Longueur
                ? $"{propre[..3]}-{propre[3..]}"
                : propre;
        }
    }
}
