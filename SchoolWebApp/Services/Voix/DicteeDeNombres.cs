using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LES NOMBRES À ÉCRIRE APRÈS LES AVOIR ENTENDUS — Camara, le 14/11/2026 : « quand la prof demande d'écrire un
    /// nombre qu'elle vient de prononcer », il ne doit apparaître ni dans sa bulle ni au tableau. Le professeur met
    /// lui-même ce nombre dans [A_ECOUTER]…[/A_ECOUTER] : la voix le dit, l'écran montre 🔊 (reponseADictee.js).
    ///
    /// UNE RÈGLE DE PROMPT NE VAUT PAS UN FAIT : la consigne est dans la spécialité de maths, et CE rappel part avec
    /// chaque tour tant que la série dure. L'écran garde son filet (les tournures reconnues, le nombre seul au tableau)
    /// pour le tour où la balise manquerait.
    ///
    /// LE RAPPEL ÉVITE LES MOTS « dictée » ET « écoute » : tout texte joint au message de l'élève passe par le
    /// détecteur des exercices de langue (sans effet en maths aujourd'hui, mais un mot de trop y a déjà coûté cher).
    /// </summary>
    public static partial class DicteeDeNombres
    {
        public const string Balise = "[A_ECOUTER]";

        /// <summary>Le rappel du tour, tant qu'une série est en cours.</summary>
        public const string Rappel =
            "[Nombres donnés à voix haute en cours : chaque nombre que l'élève doit ÉCRIRE après l'avoir entendu va dans "
            + "[A_ECOUTER]…[/A_ECOUTER] — dit à voix haute, jamais affiché, jamais au tableau, même quand tu le redis. "
            + "Exemple : « Je te dis un autre nombre : [A_ECOUTER]soixante-cinq[/A_ECOUTER]. » Une fois sa réponse "
            + "donnée, tu peux l'écrire en clair pour corriger. Un nombre qu'il doit LIRE puis transformer (81 au "
            + "tableau, à écrire en lettres) n'est pas concerné.]";

        /// <summary>« je te dis un autre nombre : 65 », « écris le nombre que je dis », « je le redis : … ».</summary>
        [GeneratedRegex(
            @"(?<!\p{L})(?:je\s+te\s+(?:dis|redis|donne)|je\s+(?:le|la)\s+redis|que\s+je\s+(?:dis|vais\s+dire|viens\s+de\s+dire|prononce))(?!\p{L})"
            + @"|(?<!\p{L})(?:écris|écrire|note|tape)(?!\p{L})[^.!?\n]{0,40}(?<!\p{L})(?:entends|entendu)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceDuProfesseur();

        /// <summary>« Dicte-moi des nombres », « on écrit des nombres que tu me dis » : l'élève le demande.</summary>
        [GeneratedRegex(
            @"(?<!\p{L})dict\p{L}*(?!\p{L}).{0,25}(?<!\p{L})nombres?(?!\p{L})"
            + @"|(?<!\p{L})nombres?(?!\p{L}).{0,30}(?<!\p{L})(?:dict\p{L}*|tu\s+(?:me\s+)?(?:dis|dictes))(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex DemandeDeLEleve();

        /// <summary>Un nombre (en chiffres ou en lettres) dans le message du professeur.</summary>
        [GeneratedRegex(
            @"\d|(?<!\p{L})(?:un|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize|vingt|trente|quarante|cinquante|soixante|cent|mille)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex UnNombre();

        // ------------------------------------------------------------------ le verdict (14/11/2026)
        //
        // SIGNALEMENT DU 14/11/2026 : « le professeur a dicté 65 et 43 ; l'élève a écrit soixante-deux et
        // quarante-deux ; le professeur indique que les deux réponses sont correctes ». Le modèle compare mal un nombre
        // en lettres à un nombre en chiffres. Le CODE compare désormais, et le professeur reçoit le fait.

        private const string MotsDeNombre =
            "zéro|zero|un|une|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize"
            + "|vingt|vingts|trente|quarante|cinquante|soixante|cent|cents|mille|et";

        /// <summary>Un nombre dicté : des chiffres, ou des mots de nombre liés par traits d'union, espaces ou « et ».</summary>
        private const string NombreDicte =
            @"(?<n>\d{1,3}(?:[\s  ]\d{3})+|\d+|(?:" + MotsDeNombre + @")(?:[-\s]+(?:" + MotsDeNombre + @"))*)";

        /// <summary>Le nombre dans la balise : le cas sûr.</summary>
        [GeneratedRegex(@"\[A_ECOUTER\]\s*(?<n>[^\[]+?)\s*\[/A_ECOUTER\]", RegexOptions.IgnoreCase)]
        private static partial Regex NombreBalise();

        /// <summary>Sans balise : « je te dis (un autre nombre) : 65 », « je le redis : … », « écris soixante. ».</summary>
        [GeneratedRegex(
            @"(?<!\p{L})(?:je\s+te\s+(?:dis|redis|donne)|je\s+(?:le|la)\s+redis|écoute(?:\s+bien)?|(?<!s['’])(?:écris|note|tape))(?!\p{L})"
            + @"[^.!?\n:]{0,40}?(?::\s*|\s+)" + NombreDicte + @"(?!\p{L})\s*(?:[.!…]|$)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex NombreAnnonce();

        [GeneratedRegex(@"(?<!\p{L})en\s+(?<forme>lettres|chiffres)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex FormeDemandee();

        [GeneratedRegex(@"\p{L}+", RegexOptions.IgnoreCase)]
        private static partial Regex Mot();

        /// <summary>Les mots qu'un enfant met autour de sa réponse sans changer le nombre.</summary>
        private static readonly HashSet<string> Remplissage = new(StringComparer.OrdinalIgnoreCase)
        {
            "c", "est", "ce", "le", "la", "je", "j", "pense", "crois", "que", "ça", "ca", "fait", "euh", "alors", "donc",
            "bah", "ben", "hum", "il", "s", "écrit", "ecrit", "réponse", "reponse", "nombre",
        };

        private static readonly HashSet<string> Mots = new(MotsDeNombre.Split('|'), StringComparer.OrdinalIgnoreCase);

        /// <summary>Le nombre que le professeur vient de donner à écrire (dernier de son message), ou null.</summary>
        public static string? Cible(string? messageDuProfesseur)
        {
            if (string.IsNullOrWhiteSpace(messageDuProfesseur)) return null;
            var balise = NombreBalise().Matches(messageDuProfesseur);
            var annonce = NombreAnnonce().Matches(messageDuProfesseur);
            var texte = balise.Count > 0 ? balise[^1].Groups["n"].Value
                : annonce.Count > 0 ? annonce[^1].Groups["n"].Value
                : null;
            return texte is null ? null : LectureDesNombres.Canonique(texte);
        }

        /// <summary>« en lettres » ou « en chiffres », dit le plus récemment par le professeur ; null sinon.</summary>
        public static string? Forme(IEnumerable<string?> messagesDuProfesseur)
        {
            foreach (var message in messagesDuProfesseur.Reverse())
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                var m = FormeDemandee().Matches(message);
                if (m.Count > 0) return m[^1].Groups["forme"].Value.ToLowerInvariant();
            }
            return null;
        }

        /// <summary>Ce qu'il a écrit vaut-il un nombre, sans mot inconnu (une faute de frappe n'est pas un autre nombre) ?</summary>
        public static string? Lu(string? reponse)
        {
            if (string.IsNullOrWhiteSpace(reponse)) return null;
            foreach (Match mot in Mot().Matches(reponse))
            {
                if (!Mots.Contains(mot.Value) && !Remplissage.Contains(mot.Value)) return null;
            }
            return LectureDesNombres.Canonique(reponse);
        }

        /// <summary>
        /// Le fait du tour, ou null s'il n'y a rien à juger (pas de nombre donné au dernier message, ou une réponse qui
        /// n'est pas un nombre : une question, « je sais pas »).
        /// </summary>
        public static string? FaitVerdict(string? dernierDuProfesseur, IEnumerable<string?> messagesDuProfesseur, string? reponse)
        {
            var cible = Cible(dernierDuProfesseur);
            if (cible is null || string.IsNullOrWhiteSpace(reponse)) return null;

            var aDesMots = Mot().IsMatch(reponse);
            var aDesChiffres = reponse.Any(char.IsDigit);
            if (!aDesMots && !aDesChiffres) return null;

            var lu = Lu(reponse);
            var ecrit = reponse.Trim();

            if (lu is null)
            {
                // Des mots, mais pas tous des mots de nombre : une faute (« soixante-cinc ») ou une phrase. On ne
                // prononce pas de verdict de valeur ; on oblige la comparaison lettre à lettre.
                return aDesMots && LectureDesNombres.Canonique(reponse) is not null
                    ? $"[Vérifié par l'application : le nombre donné était {cible}. L'élève a écrit « {ecrit} », qui "
                      + "n'est pas l'écriture exacte d'un nombre (un mot mal écrit). Ce n'est donc PAS juste tel quel : "
                      + "dis-lui ce qui ne va pas sans lui écrire la réponse.]"
                    : null;
            }

            if (lu != cible)
            {
                return $"[Vérifié par l'application : le nombre donné était {cible}. L'élève a écrit « {ecrit} », "
                    + $"c'est-à-dire {lu} : c'est FAUX. Ne dis surtout pas que c'est juste. Dis-lui que ce n'est pas "
                    + "encore ça, sans lui écrire la réponse, et redis-lui le nombre dans [A_ECOUTER]…[/A_ECOUTER].]";
            }

            var forme = Forme(messagesDuProfesseur);
            if (forme == "lettres" && aDesChiffres && !aDesMots)
            {
                return $"[Vérifié par l'application : c'est le bon nombre ({cible}), mais l'élève l'a écrit en chiffres "
                    + "alors qu'il devait l'écrire en lettres. Demande-le-lui en lettres.]";
            }
            if (forme == "chiffres" && aDesMots && !aDesChiffres)
            {
                return $"[Vérifié par l'application : c'est le bon nombre ({cible}), mais l'élève l'a écrit en lettres "
                    + "alors qu'il devait l'écrire en chiffres. Demande-le-lui en chiffres.]";
            }

            return aDesMots
                ? $"[Vérifié par l'application : « {ecrit} » vaut bien {cible}, le nombre donné : c'est le BON nombre. "
                  + "Vérifie seulement son orthographe et ses traits d'union, lettre par lettre, avant de le féliciter.]"
                : $"[Vérifié par l'application : « {ecrit} » est bien {cible}, le nombre donné : c'est JUSTE.]";
        }

        /// <summary>
        /// Une série est-elle en cours ? Oui si l'un des DEUX derniers messages du professeur porte la balise, ou
        /// annonce un nombre à écrire en l'entendant ; ou si l'élève vient de le demander. Deux messages et non un :
        /// une correction intercalée (« Oui, c'est ça ! ») ne doit pas faire tomber le rappel au milieu de la série.
        /// </summary>
        public static bool EnCours(IEnumerable<string?> derniersMessagesDuProfesseur, string? messageDeLEleve)
        {
            if (!string.IsNullOrWhiteSpace(messageDeLEleve) && DemandeDeLEleve().IsMatch(messageDeLEleve)) return true;

            foreach (var message in derniersMessagesDuProfesseur.TakeLast(2))
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                if (message.Contains(Balise, StringComparison.OrdinalIgnoreCase)) return true;
                if (AnnonceDuProfesseur().IsMatch(message) && UnNombre().IsMatch(message)) return true;
            }

            return false;
        }
    }
}
