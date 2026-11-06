using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LES SONS CHOISIS À L'OREILLE, REJOUÉS TELS QUELS — Camara, le 05/11/2026 : « y'a trop d'invention ». Aucune voix de
    /// synthèse ne dit une syllabe seule de façon fiable (mesuré : 14/24 Gemini, 12/24 OpenAI ; « ri » devenait « Réel,
    /// c'est là »). Camara a écouté 1 132 prises de la voix d'Adrien et en a gardé une par son : la voix ne génère plus ces
    /// sons, donc elle ne peut ni les déformer ni broder derrière. La ressource est produite une fois pour toutes, à partir de
    /// son choix, par le script du dossier de la banque ; embarquée dans la DLL comme le catalogue des jeux, elle ne peut
    /// pas manquer à une copie FileZilla. Format : nombre de sons, puis pour chacun son nom en UTF-8 et son PCM 16 bits 24 kHz.
    /// </summary>
    public static partial class BanqueDeSons
    {
        private const string Ressource = "SchoolWebApp.Api.Ressources.banque-sons-adrien-fr.bin";

        /// <summary>
        /// LES MOTS DU CP AU CM2 — Camara, le 05/11/2026 : 501 mots écrits classe par classe, une prise choisie par mot.
        /// Même format, avec la classe de chaque mot (1 = CP ... 5 = CM2) entre le nom et le son.
        /// </summary>
        private const string RessourceMots = "SchoolWebApp.Api.Ressources.banque-mots-adrien-fr.bin";
        private const string Voix = "adrien";
        private const string Langue = "fr";

        /// <summary>Le primaire seulement, comme la phrase-support : au-delà, rien ne change.</summary>
        private const int AgeMaximum = 11;

        /// <summary>Les sons dont Camara a écouté toutes les prises et coché « aucune » : ils ne se dictent plus.</summary>
        private static readonly HashSet<string> Refuses = new(StringComparer.Ordinal)
        {
            "chin", "dro", "gan", "gni", "gné", "gu", "nan", "pru", "tru", "vro", "zu",
        };

        private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> Sons = new(Charger);

        private static readonly Lazy<(IReadOnlyDictionary<string, byte[]> Sons, IReadOnlyDictionary<string, int> Classes)> Mots = new(ChargerMots);

        /// <summary>Le rang de la classe : 1 = CP ... 5 = CM2 ; 0 hors du primaire ou inconnu.</summary>
        public static int RangDeClasse(string? niveauCode) => niveauCode?.Trim().ToUpperInvariant() switch
        {
            "CP" => 1,
            "CE1" => 2,
            "CE2" => 3,
            "CM1" => 4,
            "CM2" => 5,
            _ => 0,
        };

        public static int NombreDeMots => Mots.Value.Classes.Count;

        /// <summary>Un mot de la banque que l'enfant de cette classe peut entendre : sa classe ou une classe en dessous.</summary>
        public static bool EstUnMotPourLaClasse(string? mot, int rang) =>
            mot is not null && rang > 0 && Mots.Value.Classes.TryGetValue(Normaliser(mot), out var r) && r <= rang;

        /// <summary>
        /// Un mot pour un enfant de cette classe, pas entendu récemment : de sa classe d'abord, des classes en dessous si
        /// elle est épuisée. Stable pour un même tour, comme SyllabeDisponible.
        /// </summary>
        public static string? MotDisponible(int rang, string graine, IEnumerable<string?> dejaDits)
        {
            if (rang <= 0) return null;
            var vus = dejaDits.Where(d => d is not null).Select(d => Normaliser(d!)).ToHashSet(StringComparer.Ordinal);
            var siens = MotsDeRang(r => r == rang).Where(m => !vus.Contains(m)).ToList();
            var dessous = MotsDeRang(r => r < rang).Where(m => !vus.Contains(m)).ToList();
            var pool = siens.Count > 0 ? siens : dessous.Count > 0 ? dessous : MotsDeRang(r => r <= rang).ToList();
            return pool.Count == 0 ? null : pool[Indice(graine, pool.Count)];
        }

        /// <summary>Les mots proposés au professeur pour ce tour : deux tiers de sa classe, un tiers en dessous.</summary>
        public static IReadOnlyList<string> PropositionDeMots(int rang, string graine, IEnumerable<string?> dejaDits, int nombre)
        {
            if (rang <= 0) return [];
            var vus = dejaDits.Where(d => d is not null).Select(d => Normaliser(d!)).ToHashSet(StringComparer.Ordinal);
            var siens = MotsDeRang(r => r == rang).Where(m => !vus.Contains(m)).ToList();
            var dessous = MotsDeRang(r => r < rang).Where(m => !vus.Contains(m)).ToList();
            var duDessous = dessous.Count == 0 ? 0 : Math.Min(dessous.Count, nombre / 3);
            return Prendre(siens, nombre - duDessous, graine).Concat(Prendre(dessous, duDessous, graine + "|dessous"))
                .Distinct().ToList();
        }

        private static IEnumerable<string> MotsDeRang(Func<int, bool> garder) =>
            Mots.Value.Classes.Where(c => garder(c.Value)).Select(c => c.Key).OrderBy(m => m, StringComparer.Ordinal);

        private static int Indice(string graine, int taille)
        {
            var somme = 0;
            foreach (var c in graine) somme = unchecked(somme * 31 + c);
            return (int)((uint)somme % (uint)taille);
        }

        private static IEnumerable<string> Prendre(IReadOnlyList<string> liste, int nombre, string graine)
        {
            if (liste.Count == 0 || nombre <= 0) return [];
            var depart = Indice(graine, liste.Count);
            var pas = Math.Max(1, liste.Count / nombre);
            return Enumerable.Range(0, Math.Min(nombre, liste.Count)).Select(i => liste[(depart + i * pas) % liste.Count]);
        }

        private static (IReadOnlyDictionary<string, byte[]>, IReadOnlyDictionary<string, int>) ChargerMots()
        {
            var sons = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var classes = new Dictionary<string, int>(StringComparer.Ordinal);
            using var flux = Assembly.GetExecutingAssembly().GetManifestResourceStream(RessourceMots);
            if (flux is null) return (sons, classes);

            using var lecteur = new BinaryReader(flux, Encoding.UTF8);
            var nombre = lecteur.ReadInt32();
            for (var i = 0; i < nombre; i++)
            {
                var nom = Encoding.UTF8.GetString(lecteur.ReadBytes(lecteur.ReadUInt16()));
                classes[nom] = lecteur.ReadByte();
                sons[nom] = lecteur.ReadBytes(lecteur.ReadInt32());
            }

            return (sons, classes);
        }

        private static readonly Lazy<IReadOnlyList<string>> Simples = new(() =>
            Sons.Value.Keys.Where(s => SyllabeSimple().IsMatch(s)).OrderBy(s => s, StringComparer.Ordinal).ToList());

        [GeneratedRegex(@"^(?:b|c|ch|d|f|g|j|l|m|n|p|r|s|t|v|z)(?:a|e|é|i|o|u|ou)$")]
        private static partial Regex SyllabeSimple();

        private static IReadOnlyDictionary<string, byte[]> Charger()
        {
            var sons = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using var flux = Assembly.GetExecutingAssembly().GetManifestResourceStream(Ressource);
            if (flux is null) return sons;

            using var lecteur = new BinaryReader(flux, Encoding.UTF8);
            var nombre = lecteur.ReadInt32();
            for (var i = 0; i < nombre; i++)
            {
                var nom = Encoding.UTF8.GetString(lecteur.ReadBytes(lecteur.ReadUInt16()));
                sons[nom] = lecteur.ReadBytes(lecteur.ReadInt32());
            }

            return sons;
        }

        private static string Normaliser(string texte) =>
            texte.Trim().TrimEnd('.', '!', '?', '…', ',', ';', ':').Trim().ToLowerInvariant().Normalize(NormalizationForm.FormC);

        public static int Nombre => Sons.Value.Count;

        /// <summary>Le son déjà choisi pour ce passage, ou null : la voix le génère alors comme d'habitude.</summary>
        public static byte[]? Cherche(string? texte, string? avatar, int age, string? langue, bool dictee)
        {
            if (dictee || age > AgeMaximum || langue != Langue || avatar != Voix || string.IsNullOrWhiteSpace(texte)) return null;
            var cle = Normaliser(texte);
            return Sons.Value.GetValueOrDefault(cle) ?? Mots.Value.Sons.GetValueOrDefault(cle);
        }

        public static bool Contient(string? syllabe) =>
            syllabe is not null && Sons.Value.ContainsKey(Normaliser(syllabe));

        public static bool EstRefuse(string mot) => Refuses.Contains(Normaliser(mot));

        /// <summary>
        /// Une syllabe simple de la banque, pas entendue récemment. Stable pour un même tour : le texte diffusé et le texte
        /// gardé en mémoire doivent dicter la même.
        /// </summary>
        public static string? SyllabeDisponible(string graine, IEnumerable<string?> dejaDites)
        {
            var pool = Simples.Value;
            if (pool.Count == 0) return null;

            var vues = dejaDites.Where(d => d is not null).Select(d => Normaliser(d!)).ToHashSet(StringComparer.Ordinal);
            var libres = pool.Where(s => !vues.Contains(s)).ToList();
            if (libres.Count == 0) libres = pool.ToList();

            var somme = 0;
            foreach (var c in graine) somme = unchecked(somme * 31 + c);
            return libres[(int)((uint)somme % (uint)libres.Count)];
        }

        /// <summary>Quelques syllabes à proposer au professeur pour ce tour, celles entendues récemment en moins.</summary>
        public static IReadOnlyList<string> Proposition(string graine, IEnumerable<string?> dejaDites, int nombre)
        {
            var vues = dejaDites.Where(d => d is not null).Select(d => Normaliser(d!)).ToHashSet(StringComparer.Ordinal);
            var libres = Simples.Value.Where(s => !vues.Contains(s)).ToList();
            if (libres.Count == 0) return [];

            var somme = 0;
            foreach (var c in graine) somme = unchecked(somme * 31 + c);
            var depart = (int)((uint)somme % (uint)libres.Count);
            var pas = Math.Max(1, libres.Count / Math.Max(1, nombre));

            return Enumerable.Range(0, Math.Min(nombre, libres.Count))
                .Select(i => libres[(depart + i * pas) % libres.Count])
                .Distinct()
                .ToList();
        }
    }
}
