namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LES SONS DE L'ANGLAIS QUI ONT UNE IMAGE — Camara, le 06/11/2026 : « pour les sons, j'ai un dossier pour les
    /// illustrations, un dossier par classe du CP au CM2 ». 66 images, une par son, chacune dessinant UN mot anglais
    /// (le son en orange). Elle s'affiche au tableau quand l'enfant trouve ce mot ; la même table vit côté navigateur
    /// (`lib/imagesSonsAnglais.js`) : les deux se modifient ensemble.
    ///
    /// « thumb » (mb) et « knee » (kn) ne sont pas dans la liste du micro : ils ne sont pas proposés au professeur.
    /// </summary>
    public static class SonsAnglaisIllustres
    {
        public sealed record SonIllustre(string Son, string Mot, string Niveau);

        public static readonly IReadOnlyList<SonIllustre> Tous =
        [
            new("a", "cat", "CP"), new("e", "egg", "CP"), new("i", "pig", "CP"), new("o", "dog", "CP"), new("u", "sun", "CP"),
            new("b", "ball", "CP"), new("c", "car", "CP"), new("d", "duck", "CP"), new("f", "frog", "CP"), new("g", "goat", "CP"),
            new("h", "hat", "CP"), new("j", "jam", "CP"), new("k", "kite", "CP"), new("l", "lion", "CP"), new("m", "moon", "CP"),
            new("n", "nurse", "CP"), new("p", "pizza", "CP"), new("r", "rabbit", "CP"), new("s", "snake", "CP"), new("t", "tiger", "CP"),
            new("v", "vegetable", "CP"), new("w", "window", "CP"), new("y", "yellow", "CP"), new("z", "zebra", "CP"), new("x", "box", "CP"),
            new("qu", "queen", "CP"),
            new("sh", "ship", "CE1"), new("ch", "cheese", "CE1"), new("th", "tooth", "CE1"), new("ng", "king", "CE1"), new("ck", "duck", "CE1"),
            new("wh", "whale", "CE1"), new("ph", "phone", "CE1"),
            new("ai", "rain", "CE2"), new("ay", "play", "CE2"), new("ee", "sheep", "CE2"), new("ea", "sea", "CE2"), new("oa", "boat", "CE2"),
            new("ie", "tie", "CE2"), new("igh", "night", "CE2"), new("oo long", "moon", "CE2"), new("oo court", "book", "CE2"),
            new("ow de snow", "snow", "CE2"), new("ow de cow", "cow", "CE2"), new("ou", "house", "CE2"), new("oy", "toy", "CE2"),
            new("ar", "car", "CM1"), new("or", "horse", "CM1"), new("er", "teacher", "CM1"), new("ir", "bird", "CM1"), new("ur", "turtle", "CM1"),
            new("air", "hair", "CM1"), new("ear", "ear", "CM1"), new("a-e", "cake", "CM1"), new("i-e", "kite", "CM1"), new("o-e", "home", "CM1"),
            new("u-e", "June", "CM1"),
            new("aw", "draw", "CM2"), new("wr", "write", "CM2"), new("c doux", "circle", "CM2"),
            new("g doux", "giraffe", "CM2"), new("y de fly", "fly", "CM2"), new("y de sunny", "sunny", "CM2"), new("tion", "station", "CM2"),
        ];

        /// <summary>
        /// LE MOT EN FRANÇAIS — Camara, le 07/11/2026 : « l'enfant doit lire le mot en anglais ; s'il le dit en français, la
        /// prof dit que c'est bien le mot en français, mais elle attend qu'il le dise en anglais : c'est une erreur ». L'image
        /// porte la traduction sous le mot (« ship / Navire ») : la lire est une erreur prévisible, qu'il faut reconnaître.
        /// Les synonymes courants d'un enfant comptent (« bateau » pour ship). Lion et pizza s'écrivent pareil : non listés.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> Traductions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["cat"] = ["chat", "chaton"], ["egg"] = ["oeuf", "œuf"], ["pig"] = ["cochon"], ["dog"] = ["chien"], ["sun"] = ["soleil"],
            ["ball"] = ["balle", "ballon"], ["car"] = ["voiture"], ["duck"] = ["canard"], ["frog"] = ["grenouille"], ["goat"] = ["chèvre"],
            ["hat"] = ["chapeau"], ["jam"] = ["confiture"], ["kite"] = ["cerf-volant", "cerf volant"], ["moon"] = ["lune"],
            ["nurse"] = ["infirmière", "infirmier"], ["rabbit"] = ["lapin"], ["snake"] = ["serpent"], ["tiger"] = ["tigre"],
            ["vegetable"] = ["légume", "légumes"], ["window"] = ["fenêtre"], ["yellow"] = ["jaune"], ["zebra"] = ["zèbre"],
            ["box"] = ["boîte", "carton"], ["queen"] = ["reine"], ["ship"] = ["navire", "bateau"], ["cheese"] = ["fromage"],
            ["tooth"] = ["dent"], ["king"] = ["roi"], ["whale"] = ["baleine"], ["phone"] = ["téléphone"], ["rain"] = ["pluie"],
            ["play"] = ["jouer", "joue"], ["sheep"] = ["mouton"], ["sea"] = ["mer"], ["boat"] = ["bateau", "barque"],
            ["tie"] = ["cravate"], ["night"] = ["nuit"], ["book"] = ["livre"], ["snow"] = ["neige"], ["cow"] = ["vache"],
            ["house"] = ["maison"], ["toy"] = ["jouet"], ["horse"] = ["cheval"], ["teacher"] = ["professeur", "prof", "maîtresse", "maître", "enseignant"],
            ["bird"] = ["oiseau"], ["turtle"] = ["tortue"], ["hair"] = ["cheveux", "cheveu"], ["ear"] = ["oreille"], ["cake"] = ["gâteau"],
            ["home"] = ["maison"], ["June"] = ["juin"], ["draw"] = ["dessiner", "dessin"], ["write"] = ["écrire"], ["circle"] = ["cercle", "rond"],
            ["giraffe"] = ["girafe"], ["fly"] = ["voler", "mouche"], ["sunny"] = ["ensoleillé", "soleil"], ["station"] = ["gare"],
        };

        /// <summary>Les sons de cette classe (CP à CM2), vide ailleurs.</summary>
        public static IReadOnlyList<SonIllustre> DeLaClasse(string? niveau) =>
            Tous.Where(s => string.Equals(s.Niveau, niveau?.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
