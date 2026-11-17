namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LES MOTS DE L'ANGLAIS QUI ONT UNE IMAGE — Camara, le 07/11/2026 : « les images pour les mots en anglais sont prêtes,
    /// le dossier c'est anglais-mots-CP-CM2-images ; tu peux mettre ça en place ». 242 mots du CP au CM2, chaque dessin
    /// porte son mot anglais et sa traduction (« apple / Pomme »). Servis depuis le dossier de données `images-mots-en`.
    ///
    /// Ils servent à la lecture des mots (LectureDImage : l'enfant lit le mot anglais de l'image), à l'écoute des mots
    /// (l'image s'affiche quand il a trouvé : ImageAuTableau) et aux mots proposés à Marine pour l'écoute. La même table
    /// vit côté navigateur (`lib/imagesMotsAnglais.js`) : les deux se modifient ensemble. Les 242 sont dans la liste des
    /// mots anglais vérifiés du micro (mesuré le 07/11/2026).
    /// </summary>
    public static class MotsAnglaisIllustres
    {
        public sealed record MotIllustre(string Mot, string Francais, string Niveau);

        public static readonly IReadOnlyList<MotIllustre> Tous =
        [
            new("red", "rouge", "CP"), new("blue", "bleu", "CP"), new("green", "vert", "CP"),
            new("yellow", "jaune", "CP"), new("pink", "rose", "CP"), new("black", "noir", "CP"),
            new("white", "blanc", "CP"), new("brown", "marron", "CP"), new("purple", "violet", "CP"),
            new("grey", "gris", "CP"), new("one", "un", "CP"), new("four", "quatre", "CP"),
            new("five", "cinq", "CP"), new("six", "six", "CP"), new("seven", "sept", "CP"),
            new("eight", "huit", "CP"), new("nine", "neuf", "CP"), new("ten", "dix", "CP"),
            new("cat", "chat", "CP"), new("dog", "chien", "CP"), new("bird", "oiseau", "CP"),
            new("fish", "poisson", "CP"), new("duck", "canard", "CP"), new("cow", "vache", "CP"),
            new("pig", "cochon", "CP"), new("horse", "cheval", "CP"), new("sheep", "mouton", "CP"),
            new("frog", "grenouille", "CP"), new("lion", "lion", "CP"), new("mouse", "souris", "CP"),
            new("rabbit", "lapin", "CP"), new("hen", "poule", "CP"), new("head", "tête", "CP"),
            new("hand", "main", "CP"), new("eye", "œil", "CP"), new("ear", "oreille", "CP"),
            new("arm", "bras", "CP"), new("leg", "jambe", "CP"), new("foot", "pied", "CP"),
            new("hair", "cheveux", "CP"), new("face", "visage", "CP"), new("mum", "maman", "CP"),
            new("dad", "papa", "CP"), new("baby", "bébé", "CP"), new("sister", "sœur", "CP"),
            new("brother", "frère", "CP"), new("ball", "ballon", "CP"), new("doll", "poupée", "CP"),
            new("teddy", "nounours", "CP"), new("car", "voiture", "CP"), new("kite", "cerf-volant", "CP"),
            new("apple", "pomme", "CP"), new("banana", "banane", "CP"), new("monkey", "singe", "CE1"),
            new("tiger", "tigre", "CE1"), new("giraffe", "girafe", "CE1"), new("zebra", "zèbre", "CE1"),
            new("snake", "serpent", "CE1"), new("spider", "araignée", "CE1"), new("owl", "chouette", "CE1"),
            new("goat", "chèvre", "CE1"), new("panda", "panda", "CE1"), new("penguin", "manchot", "CE1"),
            new("turtle", "tortue", "CE1"), new("kangaroo", "kangourou", "CE1"), new("dolphin", "dauphin", "CE1"),
            new("whale", "baleine", "CE1"), new("shark", "requin", "CE1"), new("fox", "renard", "CE1"),
            new("wolf", "loup", "CE1"), new("hamster", "hamster", "CE1"), new("butterfly", "papillon", "CE1"),
            new("cake", "gâteau", "CE1"), new("bread", "pain", "CE1"), new("milk", "lait", "CE1"),
            new("cheese", "fromage", "CE1"), new("egg", "œuf", "CE1"), new("pizza", "pizza", "CE1"),
            new("burger", "hamburger", "CE1"), new("jam", "confiture", "CE1"), new("honey", "miel", "CE1"),
            new("candy", "bonbon", "CE1"), new("cookie", "cookie", "CE1"), new("pear", "poire", "CE1"),
            new("cherry", "cerise", "CE1"), new("grape", "raisin", "CE1"), new("lemon", "citron", "CE1"),
            new("pineapple", "ananas", "CE1"), new("strawberry", "fraise", "CE1"), new("hat", "chapeau", "CE1"),
            new("cap", "casquette", "CE1"), new("coat", "manteau", "CE1"), new("dress", "robe", "CE1"),
            new("shoes", "chaussures", "CE1"), new("socks", "chaussettes", "CE1"), new("scarf", "écharpe", "CE1"),
            new("gloves", "gants", "CE1"), new("boots", "bottes", "CE1"), new("skirt", "jupe", "CE1"),
            new("shirt", "chemise", "CE1"), new("trousers", "pantalon", "CE1"), new("shorts", "short", "CE1"),
            new("jeans", "jean", "CE1"), new("jumper", "pull", "CE1"), new("tie", "cravate", "CE1"),
            new("book", "livre", "CE1"), new("pencil", "crayon", "CE1"), new("ruler", "règle", "CE1"),
            new("rubber", "gomme", "CE1"), new("glue", "colle", "CE1"), new("scissors", "ciseaux", "CE1"),
            new("schoolbag", "cartable", "CE1"), new("desk", "bureau", "CE1"), new("board", "tableau", "CE1"),
            new("notebook", "cahier", "CE1"), new("paper", "papier", "CE1"), new("house", "maison", "CE2"),
            new("door", "porte", "CE2"), new("window", "fenêtre", "CE2"), new("table", "table", "CE2"),
            new("sofa", "canapé", "CE2"), new("bed", "lit", "CE2"), new("lamp", "lampe", "CE2"),
            new("bathroom", "salle de bains", "CE2"), new("bedroom", "chambre", "CE2"), new("kitchen", "cuisine", "CE2"),
            new("shower", "douche", "CE2"), new("stairs", "escalier", "CE2"), new("mirror", "miroir", "CE2"),
            new("clock", "horloge", "CE2"), new("phone", "téléphone", "CE2"), new("garden", "jardin", "CE2"),
            new("sun", "soleil", "CE2"), new("rain", "pluie", "CE2"), new("snow", "neige", "CE2"),
            new("cloud", "nuage", "CE2"), new("wind", "vent", "CE2"), new("fog", "brouillard", "CE2"),
            new("sunny", "ensoleillé", "CE2"), new("rainy", "pluvieux", "CE2"), new("cloudy", "nuageux", "CE2"),
            new("windy", "venteux", "CE2"), new("snowy", "neigeux", "CE2"), new("hot", "chaud", "CE2"),
            new("winter", "hiver", "CE2"), new("summer", "été", "CE2"), new("spring", "printemps", "CE2"),
            new("eleven", "onze", "CE2"), new("twelve", "douze", "CE2"), new("fourteen", "quatorze", "CE2"),
            new("fifteen", "quinze", "CE2"), new("sixteen", "seize", "CE2"), new("seventeen", "dix-sept", "CE2"),
            new("eighteen", "dix-huit", "CE2"), new("nineteen", "dix-neuf", "CE2"), new("twenty", "vingt", "CE2"),
            new("bike", "vélo", "CM1"), new("plane", "avion", "CM1"), new("boat", "bateau", "CM1"),
            new("ship", "navire", "CM1"), new("taxi", "taxi", "CM1"), new("school", "école", "CM1"),
            new("hospital", "hôpital", "CM1"), new("library", "bibliothèque", "CM1"), new("park", "parc", "CM1"),
            new("shop", "magasin", "CM1"), new("supermarket", "supermarché", "CM1"), new("station", "gare", "CM1"),
            new("street", "rue", "CM1"), new("city", "ville", "CM1"), new("river", "rivière", "CM1"),
            new("sea", "mer", "CM1"), new("beach", "plage", "CM1"), new("farm", "ferme", "CM1"),
            new("doctor", "médecin", "CM1"), new("nurse", "infirmière", "CM1"), new("teacher", "professeur", "CM1"),
            new("farmer", "fermier", "CM1"), new("pilot", "pilote", "CM1"), new("driver", "chauffeur", "CM1"),
            new("king", "roi", "CM1"), new("queen", "reine", "CM1"), new("witch", "sorcière", "CM1"),
            new("ghost", "fantôme", "CM1"), new("Santa", "père Noël", "CM1"), new("robot", "robot", "CM1"),
            new("happy", "content", "CM1"), new("sad", "triste", "CM1"), new("hungry", "affamé", "CM1"),
            new("tired", "fatigué", "CM1"), new("big", "grand", "CM1"), new("small", "petit", "CM1"),
            new("tall", "grand (taille)", "CM1"), new("short", "petit (taille)", "CM1"), new("fast", "rapide", "CM1"),
            new("slow", "lent", "CM1"), new("strong", "fort", "CM1"), new("funny", "drôle", "CM1"),
            new("dirty", "sale", "CM1"), new("clean", "propre", "CM1"), new("old", "vieux", "CM1"),
            new("ugly", "laid", "CM1"), new("jump", "sauter", "CM2"), new("swim", "nager", "CM2"),
            new("sing", "chanter", "CM2"), new("read", "lire", "CM2"), new("write", "écrire", "CM2"),
            new("draw", "dessiner", "CM2"), new("cook", "cuisiner", "CM2"), new("eat", "manger", "CM2"),
            new("drink", "boire", "CM2"), new("walk", "marcher", "CM2"), new("fly", "voler", "CM2"),
            new("play", "jouer", "CM2"), new("sit", "s'asseoir", "CM2"), new("stand", "se lever", "CM2"),
            new("wash", "laver", "CM2"), new("listen", "écouter", "CM2"), new("speak", "parler", "CM2"),
            new("basketball", "basket", "CM2"), new("tennis", "tennis", "CM2"), new("birthday", "anniversaire", "CM2"),
            new("Halloween", "Halloween", "CM2"), new("Easter", "Pâques", "CM2"), new("pumpkin", "citrouille", "CM2"),
            new("present", "cadeau", "CM2"), new("party", "fête", "CM2"), new("circle", "cercle", "CM2"),
            new("square", "carré", "CM2"), new("triangle", "triangle", "CM2"), new("star", "étoile", "CM2"),
            new("heart", "cœur", "CM2"), new("shoulder", "épaule", "CM2"), new("finger", "doigt", "CM2"),
            new("toe", "orteil", "CM2"), new("tooth", "dent", "CM2"), new("tummy", "ventre", "CM2"),
            new("feet", "pieds", "CM2"), new("England", "Angleterre", "CM2"), new("America", "Amérique", "CM2"),
            new("world", "monde", "CM2"), new("moon", "lune", "CM2"),
        ];

        private static readonly string[] Classes = ["CP", "CE1", "CE2", "CM1", "CM2"];

        private static int Rang(string? niveau) => Array.IndexOf(Classes, niveau?.Trim().ToUpperInvariant());

        /// <summary>Les mots d'un enfant de cette classe : sa classe et les classes en dessous ; vide hors de l'élémentaire.</summary>
        public static IReadOnlyList<MotIllustre> JusquA(string? niveau)
        {
            var rang = Rang(niveau);
            return rang < 0 ? [] : Tous.Where(m => Rang(m.Niveau) <= rang).ToList();
        }

        /// <summary>Le mot illustré (« Apple. » → apple), ou null.</summary>
        public static MotIllustre? Trouve(string? mot)
        {
            var propre = new string((mot ?? string.Empty).ToLowerInvariant().Where(c => c is >= 'a' and <= 'z').ToArray());
            return propre.Length == 0 ? null : Tous.FirstOrDefault(m => m.Mot.Equals(propre, StringComparison.OrdinalIgnoreCase));
        }
    }
}
