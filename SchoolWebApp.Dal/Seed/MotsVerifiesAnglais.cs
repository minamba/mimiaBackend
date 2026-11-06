namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// LA LISTE FERMÉE DE L'ANGLAIS AU PRIMAIRE — Camara, le 05/11/2026 : « une liste fermée de tous
    /// les mots du CP au CM2, et bannir ceux qui ne sont pas reconnaissables ». 592 mots et lettres,
    /// écrits à la main par thème (salutations, nombres, couleurs, famille, corps, animaux, nourriture,
    /// vêtements, école, maison, temps, verbes et adjectifs courants), passés au vrai jugement par
    /// 4 voix de synthèse OpenAI. 452 sont reconnus au moins 3 fois sur 4 : Marine ne fait dire
    /// qu'eux. Les autres (three, the, this, water, orange...) sont bannis de fait. Rien ne s'y ajoute
    /// tout seul : un mot absent n'est pas testé à la volée, contrairement au français.
    /// </summary>
    public static class MotsVerifiesAnglais
    {
        public static readonly (string Mot, int Justes)[] Banc =
        [
            ("a", 4), ("afternoon", 3), ("am", 4), ("america", 4), ("an", 4), ("and", 2), ("angry", 1), ("animal", 2), ("apple", 4),
            ("april", 3), ("are", 2), ("arm", 4), ("arms", 4), ("art", 4), ("at", 4), ("august", 0), ("aunt", 1), ("autumn", 0),
            ("b", 2), ("baby", 3), ("back", 2), ("bad", 2), ("bag", 4), ("ball", 4), ("banana", 4), ("basketball", 3), ("bat", 3),
            ("bathroom", 3), ("be", 3), ("beach", 4), ("bear", 2), ("beat", 3), ("beautiful", 0), ("bed", 3), ("bedroom", 3), ("bee", 2),
            ("behind", 4), ("bicycle", 4), ("big", 3), ("bike", 4), ("bird", 4), ("birthday", 3), ("biscuit", 4), ("bit", 4), ("black", 4),
            ("blue", 4), ("board", 4), ("boat", 3), ("body", 4), ("book", 3), ("boots", 4), ("box", 4), ("boy", 4), ("bread", 3),
            ("breakfast", 2), ("brother", 4), ("brown", 3), ("burger", 4), ("bus", 2), ("but", 4), ("butter", 4), ("butterfly", 4), ("bye", 4),
            ("c", 4), ("cake", 3), ("can", 3), ("candy", 4), ("cap", 3), ("car", 4), ("carrot", 1), ("cat", 3), ("chair", 2),
            ("cheese", 4), ("cherry", 3), ("chicken", 0), ("child", 4), ("children", 3), ("chips", 1), ("chocolate", 0), ("christmas", 2), ("circle", 4),
            ("city", 3), ("clap", 2), ("class", 4), ("classroom", 4), ("clean", 3), ("climb", 2), ("clock", 3), ("close", 1), ("clothes", 1),
            ("cloud", 4), ("cloudy", 4), ("coat", 4), ("cold", 1), ("color", 2), ("colour", 2), ("come", 4), ("computer", 0), ("cook", 4),
            ("cookie", 4), ("cool", 4), ("count", 3), ("country", 4), ("cousin", 0), ("cow", 4), ("crocodile", 1), ("d", 3), ("dad", 4),
            ("daddy", 4), ("dance", 2), ("daughter", 1), ("day", 2), ("december", 2), ("desk", 4), ("dinner", 4), ("dirty", 4), ("do", 4),
            ("doctor", 4), ("does", 4), ("dog", 4), ("doll", 4), ("dolphin", 4), ("door", 4), ("draw", 4), ("dress", 3), ("drink", 4),
            ("driver", 4), ("duck", 4), ("e", 4), ("ear", 3), ("ears", 3), ("easter", 4), ("easy", 4), ("eat", 4), ("egg", 3),
            ("eggs", 4), ("eight", 3), ("eighteen", 3), ("eighty", 4), ("elephant", 0), ("eleven", 4), ("england", 3), ("english", 3), ("eraser", 4),
            ("evening", 4), ("eye", 4), ("eyes", 1), ("f", 1), ("face", 3), ("family", 4), ("farm", 3), ("farmer", 4), ("fast", 4),
            ("fat", 2), ("father", 3), ("february", 0), ("feet", 3), ("fifteen", 4), ("fifty", 4), ("finger", 3), ("firefighter", 0), ("first", 4),
            ("fish", 4), ("five", 4), ("floor", 4), ("flower", 0), ("fly", 4), ("fog", 3), ("foggy", 4), ("food", 4), ("foot", 3),
            ("football", 2), ("for", 4), ("forest", 0), ("forty", 2), ("four", 4), ("fourteen", 4), ("fox", 4), ("france", 2), ("french", 4),
            ("friday", 4), ("friend", 4), ("frog", 4), ("from", 4), ("front", 4), ("fruit", 4), ("funny", 4), ("g", 4), ("game", 3),
            ("garden", 3), ("ghost", 3), ("giraffe", 4), ("girl", 4), ("give", 2), ("gloves", 4), ("glue", 4), ("go", 4), ("goat", 3),
            ("good", 4), ("goodbye", 4), ("grandfather", 4), ("grandma", 3), ("grandmother", 0), ("grandpa", 1), ("granny", 4), ("grape", 3), ("grass", 4),
            ("gray", 4), ("green", 4), ("grey", 3), ("h", 4), ("hair", 4), ("halloween", 4), ("ham", 2), ("hamster", 3), ("hand", 4),
            ("hands", 1), ("happy", 4), ("hard", 4), ("has", 3), ("hat", 4), ("have", 3), ("he", 2), ("head", 3), ("hear", 3),
            ("heart", 4), ("hello", 4), ("help", 4), ("hen", 3), ("her", 4), ("here", 4), ("hi", 4), ("him", 3), ("his", 4),
            ("history", 2), ("home", 4), ("homework", 3), ("honey", 4), ("horse", 4), ("hospital", 3), ("hot", 4), ("hour", 2), ("house", 4),
            ("how", 3), ("hundred", 0), ("hungry", 3), ("i", 4), ("in", 3), ("is", 4), ("it", 4), ("j", 4), ("jacket", 2),
            ("jam", 4), ("january", 0), ("jeans", 4), ("job", 4), ("juice", 2), ("july", 4), ("jump", 4), ("jumper", 4), ("june", 4),
            ("k", 3), ("kangaroo", 3), ("king", 3), ("kitchen", 3), ("kite", 4), ("knee", 2), ("know", 4), ("l", 3), ("lamp", 3),
            ("leaf", 3), ("leg", 3), ("legs", 3), ("lemon", 4), ("lesson", 4), ("letter", 4), ("library", 4), ("like", 3), ("lion", 3),
            ("listen", 3), ("little", 3), ("london", 2), ("long", 2), ("love", 2), ("lunch", 4), ("m", 3), ("make", 3), ("man", 3),
            ("march", 4), ("maths", 0), ("may", 4), ("me", 4), ("meat", 4), ("melon", 2), ("milk", 4), ("minute", 4), ("mirror", 3),
            ("mom", 4), ("monday", 4), ("monkey", 4), ("month", 1), ("moon", 3), ("morning", 4), ("mother", 4), ("mountain", 0), ("mouse", 4),
            ("mouth", 0), ("mum", 4), ("mummy", 4), ("music", 4), ("my", 3), ("n", 4), ("name", 4), ("near", 4), ("neck", 2),
            ("need", 3), ("new", 4), ("next", 4), ("nice", 4), ("night", 4), ("nine", 4), ("nineteen", 3), ("ninety", 0), ("no", 4),
            ("nose", 1), ("not", 4), ("notebook", 4), ("november", 1), ("number", 4), ("nurse", 4), ("o", 4), ("october", 1), ("of", 4),
            ("okay", 4), ("old", 3), ("on", 3), ("one", 3), ("open", 0), ("or", 2), ("orange", 0), ("our", 4), ("over", 4),
            ("owl", 4), ("p", 2), ("paint", 2), ("panda", 3), ("pants", 1), ("paper", 3), ("parents", 3), ("park", 4), ("parrot", 0),
            ("party", 4), ("pasta", 3), ("pear", 4), ("pen", 2), ("pencil", 3), ("penguin", 4), ("people", 1), ("pet", 4), ("phone", 3),
            ("picture", 2), ("pig", 3), ("pilot", 3), ("pin", 4), ("pineapple", 4), ("pink", 4), ("pizza", 4), ("plane", 3), ("play", 4),
            ("please", 4), ("point", 2), ("policeman", 2), ("potato", 2), ("present", 4), ("pretty", 3), ("pumpkin", 4), ("pupil", 1), ("purple", 3),
            ("put", 3), ("q", 1), ("queen", 4), ("r", 3), ("rabbit", 4), ("rain", 4), ("rainy", 3), ("read", 3), ("red", 3),
            ("repeat", 4), ("rice", 4), ("right", 4), ("ring", 2), ("river", 3), ("robot", 3), ("roof", 1), ("room", 2), ("rubber", 4),
            ("ruler", 3), ("run", 2), ("s", 4), ("sad", 4), ("salad", 4), ("salt", 3), ("sandwich", 0), ("santa", 4), ("saturday", 0),
            ("say", 3), ("scared", 0), ("scarf", 3), ("school", 4), ("schoolbag", 4), ("science", 3), ("scissors", 4), ("sea", 4), ("season", 4),
            ("seat", 4), ("second", 4), ("see", 4), ("september", 3), ("seven", 3), ("seventeen", 4), ("seventy", 0), ("shape", 4), ("shark", 3),
            ("she", 4), ("sheep", 4), ("ship", 3), ("shirt", 4), ("shoes", 4), ("shop", 4), ("short", 4), ("shorts", 4), ("shoulder", 4),
            ("shower", 4), ("sing", 3), ("sister", 4), ("sit", 4), ("six", 4), ("sixteen", 4), ("sixty", 4), ("skirt", 4), ("slow", 3),
            ("small", 3), ("snake", 3), ("snow", 4), ("snowy", 4), ("socks", 4), ("sofa", 4), ("son", 4), ("song", 1), ("sorry", 4),
            ("soup", 4), ("speak", 4), ("spell", 4), ("spider", 4), ("sport", 4), ("spring", 3), ("square", 4), ("stairs", 4), ("stand", 3),
            ("star", 3), ("station", 4), ("stomach", 1), ("stop", 4), ("storm", 2), ("story", 4), ("strawberry", 4), ("street", 4), ("strong", 3),
            ("student", 3), ("sugar", 4), ("summer", 4), ("sun", 4), ("sunday", 4), ("sunny", 3), ("supermarket", 3), ("sweater", 3), ("sweet", 4),
            ("sweets", 4), ("swim", 4), ("t", 4), ("table", 3), ("take", 1), ("talk", 4), ("tall", 4), ("taxi", 3), ("tea", 4),
            ("teacher", 4), ("teddy", 3), ("teeth", 0), ("television", 0), ("ten", 4), ("tennis", 4), ("thank", 0), ("thanks", 1), ("that", 0),
            ("the", 0), ("their", 0), ("them", 0), ("there", 0), ("these", 0), ("they", 0), ("thin", 0), ("thing", 0), ("think", 0),
            ("third", 0), ("thirsty", 1), ("thirteen", 1), ("thirty", 0), ("this", 0), ("those", 0), ("thousand", 0), ("three", 0), ("thursday", 0),
            ("tie", 4), ("tiger", 4), ("time", 4), ("tired", 4), ("to", 2), ("today", 4), ("toe", 3), ("toilet", 0), ("tomato", 1),
            ("tomorrow", 1), ("tooth", 3), ("touch", 4), ("town", 3), ("toy", 4), ("train", 0), ("tree", 2), ("triangle", 4), ("trousers", 3),
            ("tuesday", 0), ("tummy", 3), ("turn", 4), ("turtle", 4), ("twelve", 4), ("twenty", 4), ("two", 2), ("u", 4), ("ugly", 4),
            ("uncle", 4), ("under", 4), ("us", 3), ("v", 2), ("vegetable", 4), ("village", 2), ("w", 3), ("walk", 4), ("wall", 4),
            ("want", 4), ("warm", 4), ("wash", 4), ("water", 0), ("we", 4), ("weather", 4), ("wednesday", 1), ("week", 4), ("weekend", 4),
            ("welcome", 4), ("whale", 3), ("what", 4), ("when", 4), ("where", 4), ("which", 4), ("white", 4), ("who", 3), ("why", 4),
            ("wind", 4), ("window", 4), ("windy", 4), ("winter", 3), ("witch", 4), ("with", 2), ("wolf", 3), ("woman", 0), ("word", 3),
            ("world", 4), ("write", 3), ("wrong", 3), ("x", 4), ("y", 4), ("year", 3), ("yellow", 3), ("yes", 3), ("yesterday", 4),
            ("you", 4), ("young", 2), ("your", 3), ("z", 2), ("zebra", 4), ("zero", 2), ("zoo", 2),
        ];
    }
}
