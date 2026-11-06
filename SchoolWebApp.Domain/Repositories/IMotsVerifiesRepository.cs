namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// LES SONS QU'ON SAIT JUGER — Camara, le 04/11/2026 : « si tous les mots que tu peux utiliser
    /// pour les syllabes du CP au CM2 sont couverts, ça me va ». Une liste de mots à bannir ne
    /// serait jamais complète : la règle est inversée. Un mot ou une syllabe n'est jugé que s'il a
    /// été testé et trouvé fiable. Les voix de synthèse servent de banc : jamais celle d'un enfant.
    /// </summary>
    public static class ReglesSonsVerifies
    {
        public const int Voix = 4;

        /// <summary>Reconnu au moins 3 fois sur 4 : en dessous, un enfant serait trop souvent refusé à tort.</summary>
        public const int JustesMin = 3;

        public static bool EstFiable(int justes) => justes >= JustesMin;
    }

    /// <summary>« chat » ou « pain » n'ont pas le même son en français et en anglais : un résultat vaut pour une langue.</summary>
    public static class LanguesDesSons
    {
        public const string Francais = "fr";
        public const string Anglais = "en";
    }

    public interface IMotsVerifiesRepository
    {
        /// <summary>Vrai ou faux si ce son a été testé ; null s'il ne l'a jamais été.</summary>
        Task<bool?> FiableAsync(string mot, CancellationToken ct = default, string langue = LanguesDesSons.Francais);

        /// <summary>Les sons fiables, toujours dans le même ordre : ils entrent dans un bloc mis en cache.</summary>
        Task<IReadOnlyList<string>> FiablesAsync(string langue, CancellationToken ct = default);

        Task EnregistrerAsync(string mot, int justes, int total, CancellationToken ct = default, string langue = LanguesDesSons.Francais);
    }
}
