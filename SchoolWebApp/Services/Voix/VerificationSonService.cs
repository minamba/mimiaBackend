using SchoolWebApp.Domain.Repositories;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>Ce que l'oreille a entendu, quand le professeur attendait un son.</summary>
    /// <param name="SansJugement">
    /// Un mot que l'oreille ne sait pas reconnaître : ni juste ni faux, voir
    /// <see cref="RegleVerificationSon.Inreconnaissable"/>.
    /// </param>
    /// <param name="Francais">
    /// À l'écoute d'un mot en langue étrangère : la traduction française reconnue dans sa réponse (« deux » pour two), ou
    /// null. Le refus vient alors d'une traduction, pas d'une mauvaise écoute (décision de Min's du 22/11/2026, voir
    /// <see cref="RegleVerificationSon.TexteDEcoute"/>).
    /// </param>
    public sealed record VerdictSon(string Attendu, string Entendu, bool Juste, bool SansJugement = false, string? Francais = null);

    /// <summary>
    /// LA VÉRIFICATION DES SYLLABES AU PRIMAIRE — Camara, le 04/11/2026 : « on doit envoyer
    /// les voix au lieu du texte », « c'est pour régler le problème de reconnaissance des
    /// syllabes ». Le transcripteur écrit « Fut » pour « fu » et rend du vide sur « ffff » ;
    /// le professeur, qui ne reçoit que du texte, jugeait donc sur une erreur du micro.
    /// D'abord au CP seul ; le soir même : « les syllabes on en fait jusqu'en CM2 ».
    ///
    /// CE QUI EST VÉRIFIÉ : un tour court d'un élève du primaire, en français, juste après que
    /// le professeur a demandé de dire un son. Tout le reste passe comme avant, sans aucun
    /// appel de plus : le micro, la voix des professeurs et les autres classes ne bougent pas.
    ///
    /// À L'AVEUGLE, ET C'EST CE QUI A DIVISÉ LES ERREURS PAR DEUX — mesuré le 04/11/2026
    /// sur des voix de SYNTHÈSE, gpt-audio-1.5. Quand on lui donnait la bonne réponse
    /// (« l'élève devait dire su »), le modèle l'entendait : 9 mauvais sons acceptés sur 48.
    /// Quand il doit CHOISIR parmi des sons proches sans savoir lequel est attendu : 0 sur 48,
    /// et 49 bons sons sur 66 acceptés (33 + 16). Les refus à tort restants sont presque tous
    /// un « u » entendu « eu » ou « ou », peut-être la faute de la voix de synthèse. Sur de
    /// vraies voix d'enfants : inconnu. D'où un réglage éteint par défaut, et un fait qui
    /// demande de faire redire plutôt que de dire « c'est faux ».
    ///
    /// DEUX TEMPS : (1) quel son le professeur a demandé, lu dans SES mots, sans l'audio —
    /// une expression régulière d'abord, un appel texte sinon ; (2) l'audio seul, devant la
    /// liste des sons proches, mélangée. Le modèle qui écoute ne voit jamais la demande.
    ///
    /// LE CODE EST ICI ET PAS DANS LE RELAIS : des règles sans réseau se lisent et se testent.
    /// </summary>
    public static partial class RegleVerificationSon
    {
        /// <summary>Le réglage d'Admin › Modes. Éteint : aucune voix ne part, rien ne change.</summary>
        public const string Reglage = "VERIFICATION_SYLLABES_PRIMAIRE";

        public const bool ActifParDefaut = false;

        private const int OctetsParSeconde = 24000 * 2;

        /// <summary>Le son le plus long qu'on garde pour un tour : au-delà, ce n'est pas une syllabe.</summary>
        public const int OctetsMax = OctetsParSeconde * 8;

        /// <summary>En dessous, il n'y a que la traîne du micro : personne n'a parlé.</summary>
        private const int OctetsMin = OctetsParSeconde / 4;

        /// <summary>Au-delà, le transcripteur a compris une phrase : ce n'est ni une syllabe ni un mot.</summary>
        private const int TexteMax = 24;

        private const int ParoleMax = 400;

        /// <summary>La réponse du modèle quand aucun son de la liste ne colle.</summary>
        public const string Autre = "autre";

        /// <summary>Lire la demande dans les mots du professeur, sans l'audio.</summary>
        public const string ConsigneCible =
            "You read the last words of a French primary-school teacher (pupils aged 6 to 11). "
            + "\"demande\" is true ONLY if these words explicitly order the child to say, read or repeat aloud ONE specific sound, syllable or single word (\"dis le son fu\", \"répète : ni\", \"lis le mot lapin\"). "
            + "A question about a lesson, about counting, about meaning, or an order to read a sentence or several words is NOT such an order. "
            + "Reply with strict JSON on one line: {\"demande\":true|false,\"attendu\":\"<the sound, syllable or word asked, in French spelling, empty if none>\"}.";

        /// <summary>
        /// UN MOT SE TRANSCRIT EN PHONÉTIQUE, UNE SYLLABE SE CHOISIT — mesuré le 04/11/2026 sur
        /// des prises à prononciation imposée. La liste de voisins acceptait 18 mauvais mots sur
        /// 80 (le mot dit n'y était pas, le modèle prenait le plus proche). Écrire le mot en
        /// français tenait sur des jumeaux qui sont de vrais mots (lapin/sapin : 3 sur 80), mais
        /// sur un mot long mal lu le modèle CORRIGE : « ordinatur », « champinon » devenaient
        /// « ordinateur », « champignon », 60 mauvais acceptés sur 64. En alphabet phonétique,
        /// sans droit de corriger : 5 sur 64, et 49 bons sur 64 (56 sur 80 pour les mots courts).
        /// Comparé à l'IPA de référence du mot, donné par deux modèles texte (voir ConsigneIpa).
        /// </summary>
        public const string ConsigneMot =
            "You hear a French primary-school child (aged 6 to 11) reading ONE French word aloud. Children reading aloud often get one sound wrong, so the result may not be a real word: "
            + "transcribe EXACTLY the sounds you hear in IPA, sound by sound, without correcting them towards any known word. Reply with strict JSON on one line: {\"ipa\":\"<ipa>\"}";

        /// <summary>
        /// L'IPA de référence d'un mot, par un modèle TEXTE, avec ses variantes (« extraordinaire »
        /// se dit avec ou sans son « a »). Chaque modèle se trompe sur un mot sur trente-six,
        /// jamais le même : on en interroge deux et on garde l'union.
        /// </summary>
        public const string ConsigneIpa =
            "Give every standard France-French pronunciation of the word in IPA (no stress marks, no syllable dots, no optional final schwa), including common variants. "
            + "Reply with strict JSON on one line: {\"ipa\":[\"<variant 1>\",\"<variant 2>\"]}";

        /// <summary>Lire la demande chez le professeur d'anglais, qui explique en français.</summary>
        public const string ConsigneCibleAnglais =
            "You read the last words of a primary-school English teacher in France (pupils aged 6 to 11), who explains in French and practises in English. "
            + "\"demande\" is true ONLY if these words explicitly order the child to say, read or repeat aloud ONE specific single English word. "
            + "A question about meaning, an order to answer freely, or an order to read or repeat a sentence or several words is NOT such an order. "
            + "Reply with strict JSON on one line: {\"demande\":true|false,\"attendu\":\"<the English word asked, lowercase, empty if none>\"}.";

        public const string ConsigneMotAnglais =
            "You hear a French primary-school child (aged 6 to 11) saying ONE English word aloud. Children learning English often get one sound wrong, so the result may not be a real word: "
            + "transcribe EXACTLY the sounds you hear in IPA, sound by sound, without correcting them towards any known word. Reply with strict JSON on one line: {\"ipa\":\"<ipa>\"}";

        public const string ConsigneIpaAnglais =
            "Give every standard pronunciation of this English word in IPA, both British and American, including common variants (no stress marks, no syllable dots). "
            + "If it is a single letter, give the name of the letter. Reply with strict JSON on one line: {\"ipa\":[\"<variant 1>\",\"<variant 2>\"]}";

        /// <summary>Choisir à l'aveugle : le modèle ne sait pas lequel est attendu.</summary>
        public static string ConsigneChoix(IReadOnlyList<string> liste) =>
            "You hear a French primary-school child (aged 6 to 11) saying one short sound. "
            + $"Choose the one sound, among this list, that is really audible: {string.Join(", ", liste.Select(s => $"\"{s}\""))}. "
            + $"If none fits, answer \"{Autre}\". Listen carefully to the consonant and to the vowel, in French. "
            + "Reply with strict JSON on one line: {\"entendu\":\"<one item of the list, or " + Autre + ">\"}.";

        /// <summary>
        /// LES FORMES QU'UN PROFESSEUR EMPLOIE VRAIMENT — essai réel de Camara, le 04/11/2026 :
        /// « Tu peux me lire ce mot ? », « redire le mot », « ça se lit comment ? ». Le filtre ne
        /// connaissait que « lis » et « dis » : quatre demandes sur sept passaient à côté, et le
        /// texte du micro partait en clair. « Dis-moi » n'est pas un ordre de dire un son.
        /// </summary>
        [GeneratedRegex(@"\b(dis(?!-moi)|redis|dire|redire|répète|repete|répéter|repeter|prononce|prononcer|lis|relis|lire|relire|fais|refais|essaie|essaye|essayer|se\s+lit|se\s+prononce)\b", RegexOptions.IgnoreCase)]
        private static partial Regex OrdreDeDire();

        [GeneratedRegex(@"\b(say|repeat|read|pronounce|try|répètes|repetes|à toi|a toi)\b", RegexOptions.IgnoreCase)]
        private static partial Regex OrdreAnglais();

        [GeneratedRegex(@"\[ARDOISE\](.*?)\[/ARDOISE\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"\[EN\](.*?)\[/EN\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex BaliseAnglaise();

        [GeneratedRegex(@"\[FR\](.*?)\[/FR\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex BaliseFrancaise();

        /// <summary>« le son oin », « les syllabes » : ce qui ouvre l'exercice. Pas « la prononciation », qu'une leçon de vocabulaire dit aussi.</summary>
        [GeneratedRegex(@"\b(?:le|du|ce|un|les|des|au|mon|ton)\s+sons?\b|\bsyllabes?\b|\bécout\w*\s+(?:des|les)\s+mots\b|\bj.ai\s+entendu\s+le\s+mot\b|\bqu.est-ce\s+que\s+tu\s+as\s+entendu\b", RegexOptions.IgnoreCase)]
        private static partial Regex MentionDeSons();

        [GeneratedRegex(@"\bsyllabes?\b|\b(?:le|du|ce|un)\s+son\b", RegexOptions.IgnoreCase)]
        private static partial Regex ParleDeSyllabe();

        [GeneratedRegex(@"\bj.ai\s+entendu\s+la\s+syllabe\b", RegexOptions.IgnoreCase)]
        private static partial Regex ConsigneDeLaSyllabe();

        /// <summary>« Qu'est-ce que tu as entendu ? » : il répond une phrase, ce n'est pas une prononciation à juger.</summary>
        [GeneratedRegex(@"\bqu.as-tu\s+entendu\b|\bqu.est-ce\s+que\s+tu\s+(?:as\s+)?entend\w*|\bce\s+que\s+tu\s+(?:as\s+)?entend\w*|\bj.ai\s+entendu\s+(?:le\s+mot|la\s+syllabe)\b", RegexOptions.IgnoreCase)]
        private static partial Regex AttendCeQuIlAEntendu();

        [GeneratedRegex(@"\bj.ai\s+entendu\s+le\s+mot\b", RegexOptions.IgnoreCase)]
        private static partial Regex ConsigneDeLEcoute();

        /// <summary>« Écris le son que tu entends », « écoute bien, puis note-le » : l'enfant doit trouver à l'oreille.</summary>
        [GeneratedRegex(@"\b(?:écris|ecris|écrire|ecrire|note|notes|noter|tape|tapes|taper)\b.{0,70}\b(?:entends|entendu|entendre|je\s+dis|je\s+prononce|je\s+vais\s+dire)\b"
            + @"|\b(?:entends|entendu|écoute|ecoute|je\s+dis|je\s+prononce|je\s+vais\s+dire)\b.{0,90}\b(?:écris|ecris|écrire|ecrire|note|notes|noter|tape|tapes|taper)\b",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex EcrireCeQuIlEntend();

        [GeneratedRegex(@"\[ARDOISE\](?<corps>.*?)\[/ARDOISE\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex BlocAuTableau();

        [GeneratedRegex(@"(?:«\s*|""|“)(?<mot>[\p{L}'’-]{1,20})(?:\s*»|""|”)")]
        private static partial Regex MotEntreGuillemets();

        /// <summary>« Écris le mot moto. », « Écoute bien : moto. » : le mot annoncé en clair, sans guillemets ni balise.</summary>
        [GeneratedRegex(@"(?:\ble\s+mot\s+|\ble\s+son\s+|\bla\s+syllabe\s+|:\s*)(?<mot>[\p{L}'’-]{1,20})(?=\s*(?:[.!?…]|$))", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex MotAnnonceEnClair();

        private static readonly HashSet<string> PasUnMotDicte = new(StringComparer.OrdinalIgnoreCase)
        {
            "suivant", "entier", "dicté", "dicte", "juste", "bien", "encore", "ici", "là", "oui", "non", "toi", "moi", "réessaie", "vas-y", "fini", "prêt",
        };

        /// <summary>« trois se dit three » : le mot porte un sens, le remplacer enseignerait une fausse traduction.</summary>
        [GeneratedRegex(@"\b(?:se\s+dit|veut\s+dire|signifie|traduit|traduction|contraire|synonyme|en\s+anglais|en\s+français|comme\s+dans)\b", RegexOptions.IgnoreCase)]
        private static partial Regex MotQuiPorteUnSens();

        /// <summary>« Tu peux me dire ce que ça veut dire ? » : le mot est au tableau, mais rien n'est à prononcer.</summary>
        [GeneratedRegex(@"\b(veut\s+dire|signifie|combien|pourquoi|quels?|quelles?|qui|où|phrases?|trouve|cherche|écris|calcule|compte)\b", RegexOptions.IgnoreCase)]
        private static partial Regex QuestionSansSon();

        /// <summary>
        /// « le son « fu » », « : ni », « le mot lapin » — UN son ou UN mot annoncé, après le
        /// dernier ordre de dire. Entre guillemets, le mot doit être seul (« le lapin dort »
        /// n'en est pas un) ; sans guillemets, il doit finir la phrase ou le groupe.
        /// </summary>
        [GeneratedRegex(@"(?:«\s*|""|“)([a-zà-ÿ]{1,20})(?=\s*(?:»|""|”))|(?::\s*|\bson\s+|\bmot\s+)([a-zà-ÿ]{1,20})(?=\s*(?:[.!?,;]|$))", RegexOptions.IgnoreCase)]
        private static partial Regex SonAnnonce();

        [GeneratedRegex(@"\[/?[A-Z_]+\]")]
        private static partial Regex Balise();

        [GeneratedRegex("[aeiouy]+")]
        private static partial Regex GroupeDeVoyelles();

        private static readonly string[] Voyelles = ["ou", "an", "on", "in", "eu", "oi", "ai", "au", "eau", "a", "e", "é", "è", "i", "o", "u", "y"];

        private static readonly Dictionary<string, string[]> ConsonnesProches = new()
        {
            ["m"] = ["n", "b"], ["n"] = ["m", "gn"], ["b"] = ["p", "d"], ["p"] = ["b", "t"], ["d"] = ["t", "b"], ["t"] = ["d", "p"],
            ["f"] = ["s", "v", "ch"], ["v"] = ["f", "b"], ["s"] = ["f", "ch", "z"], ["z"] = ["s", "j"], ["ch"] = ["s", "j"], ["j"] = ["ch", "z"],
            ["l"] = ["r", "n"], ["r"] = ["l", "v"], ["g"] = ["k", "d"], ["k"] = ["g", "t"], ["c"] = ["g", "t"], ["gn"] = ["n", "m"],
        };

        private static readonly Dictionary<string, string[]> VoyellesProches = new()
        {
            ["i"] = ["é", "u"], ["u"] = ["ou", "i", "eu"], ["a"] = ["o", "é"], ["o"] = ["a", "ou"], ["é"] = ["i", "è"], ["è"] = ["é", "a"],
            ["e"] = ["eu", "o"], ["eu"] = ["u", "o"], ["ou"] = ["u", "o"], ["an"] = ["on", "a"], ["on"] = ["an", "o"], ["in"] = ["an", "è"],
            ["oi"] = ["a", "ou"], ["ai"] = ["é", "a"], ["au"] = ["o", "a"], ["eau"] = ["o", "a"], ["y"] = ["i", "u"],
        };

        /// <summary>Les sons tenus qu'un professeur de CP demande ; « ffff » se reconnaît à sa lettre répétée.</summary>
        private static readonly string[] Tenus = ["ffff", "ssss", "chchch", "vvvv", "zzzz", "mmmm", "nnnn", "llll", "rrrr"];

        /// <summary>
        /// LE PRIMAIRE, en français : rien d'autre n'est concerné. D'abord borné au CP ; Camara,
        /// le 04/11/2026 : « les syllabes on en fait jusqu'en CM2 ». Le cycle est celui de l'élève
        /// (« Primaire »), le même test que pour <c>MarqueurSonsIsoles</c>.
        /// </summary>
        public static bool Concerne(string? niveauCycle, string? matiereCode) =>
            LangueDe(niveauCycle, matiereCode) is not null;

        /// <summary>
        /// LA LANGUE DU SON — l'anglais au primaire depuis le 05/11/2026 (Camara : « une liste
        /// fermée de tous les mots du CP au CM2 »). Null : ce cours n'est pas concerné.
        /// </summary>
        public static string? LangueDe(string? niveauCycle, string? matiereCode)
        {
            if (!string.Equals(niveauCycle, "Primaire", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.Equals(matiereCode, "FRANCAIS", StringComparison.OrdinalIgnoreCase)) return LanguesDesSons.Francais;
            if (string.Equals(matiereCode, "ANGLAIS", StringComparison.OrdinalIgnoreCase)) return LanguesDesSons.Anglais;
            return null;
        }

        /// <summary>Ce tour peut-il être un son, à sa durée et à ce qu'en a écrit le micro ?</summary>
        public static bool TourCourt(int octets, string? texte) =>
            octets >= OctetsMin && octets <= OctetsMax && (texte?.Trim().Length ?? 0) <= TexteMax;

        /// <summary>
        /// Le professeur vient-il de donner un ordre de dire ? Un filtre large : la lecture
        /// de la cible tranche ensuite. Il évite d'appeler quoi que ce soit pour « oui » ou « 12 ».
        /// </summary>
        public static bool ProfesseurDemandeUnSon(string? parole, string langue = LanguesDesSons.Francais) =>
            !string.IsNullOrWhiteSpace(parole) && EstUnOrdre(DerniersMots(parole), langue);

        /// <summary>Marine explique en français et fait pratiquer en anglais : les deux formes comptent chez elle.</summary>
        private static bool EstUnOrdre(string texte, string langue) =>
            !AttendCeQuIlAEntendu().IsMatch(texte)
            && (OrdreDeDire().IsMatch(texte) || (langue == LanguesDesSons.Anglais && OrdreAnglais().IsMatch(texte)));

        /// <summary>La fin de son message, sans ses balises : c'est là qu'est la demande.</summary>
        public static string DerniersMots(string parole)
        {
            var propre = Balise().Replace(parole, " ").Trim();
            return propre.Length <= ParoleMax ? propre : propre[^ParoleMax..];
        }

        /// <summary>
        /// Le son demandé, lu dans les mots du professeur sans aucun appel ; null quand la
        /// phrase ne le dit pas assez clairement, et c'est alors le modèle texte qui lit.
        /// </summary>
        public static string? CibleSansModele(string parole, string langue = LanguesDesSons.Francais)
        {
            if (langue == LanguesDesSons.Anglais) return CibleAnglaise(parole);

            var mots = DerniersMots(parole);
            if (AttendCeQuIlAEntendu().IsMatch(mots)) return null;

            // Après le DERNIER ordre de dire, et le DERNIER son annoncé : « dis ba, puis « pa » » = pa.
            var ordre = OrdreDeDire().Matches(mots).LastOrDefault();
            if (ordre is null) return null;

            var annonces = SonAnnonce().Matches(mots[ordre.Index..]);
            var dernier = annonces.LastOrDefault();

            // ENTRE GUILLEMETS, LE PROFESSEUR L'A DIT EN TOUTES LETTRES : c'est lui, même si le
            // tableau porte autre chose (« riz » au tableau, « dis "ri" »).
            if (dernier is not null && dernier.Groups[1].Success)
            {
                var citees = annonces.Where(a => a.Groups[1].Success).Select(a => Normaliser(a.Groups[1].Value)).ToList();
                var citee = MotEntierDesCitees(citees, CibleAuTableau(parole)) ?? citees[^1];
                return Verifiable(citee) ? citee : null;
            }

            // Sinon le tableau passe avant « le mot X » : « lire ce mot entier » désignait « entier ».
            if (CibleAuTableau(parole) is { } auTableau) return auTableau;

            // APRÈS « ON RÉESSAIE », ADRIEN REDIT LE MOT À VOIX HAUTE AU LIEU DE L'ÉCRIRE — Camara, le
            // 05/11/2026 : « quand je fais une erreur, après il retranscrit ». « Je te la redis. [FR]tortue[/FR]
            // Tu peux me lire ce mot ? » : les balises ôtées, le modèle texte lisait « pas de demande »
            // trois fois sur quatre, et le micro écrivait la réponse. Le mot est dans la balise.
            if (CibleEcrite(BaliseFrancaise().Matches(parole).LastOrDefault(), parole, LanguesDesSons.Francais) is { } aVoixHaute) return aVoixHaute;

            if (dernier is null) return null;

            var cible = Normaliser(dernier.Groups[2].Value);
            return Verifiable(cible) ? cible : null;
        }

        /// <summary>
        /// LE MOT ANGLAIS DEMANDÉ : entre guillemets, au tableau, ou dans la dernière balise [EN] quand
        /// elle ne porte qu'un mot. Une balise de plusieurs mots est une phrase : le modèle tranche.
        /// </summary>
        public static string? CibleAnglaise(string parole)
        {
            var mots = DerniersMots(parole);
            if (!EstUnOrdre(mots, LanguesDesSons.Anglais)) return null;

            var citees = SonAnnonce().Matches(mots).Where(a => a.Groups[1].Success).Select(a => Normaliser(a.Groups[1].Value)).ToList();
            if (citees.Count > 0)
            {
                var citee = MotEntierDesCitees(citees, null) ?? citees[^1];
                return Verifiable(citee) ? citee : null;
            }

            if (CibleEcrite(Ardoise().Matches(parole).LastOrDefault(), parole, LanguesDesSons.Anglais) is { } auTableau) return auTableau;
            return CibleEcrite(BaliseAnglaise().Matches(parole).LastOrDefault(), parole, LanguesDesSons.Anglais);
        }

        /// <summary>
        /// LE MOT ENTIER, PAS LE MORCEAU QUI L'EXPLIQUE — Camara, le 04/11/2026 : « il me fait répéter
        /// le même mot à l'infini ». Adrien dit « dire "pain" en entier : "p" collé à "ain" » ; retenir
        /// la dernière citation validait « ain », il félicitait et redemandait « pain », sans fin.
        /// Une citation qui contient les autres est le mot demandé ; des morceaux du mot au
        /// tableau (« "p" et puis "ain" ») le désignent. Null s'il n'y a qu'une citation, ou si
        /// elles n'ont rien à voir (« dis "ba", puis "pa" ») : la dernière vaut alors.
        /// </summary>
        public static string? MotEntierDesCitees(IReadOnlyList<string> citees, string? auTableau)
        {
            if (citees.Count < 2) return null;

            var englobante = citees.FirstOrDefault(c => citees.All(c.Contains));
            if (englobante is not null) return englobante;

            return auTableau is not null && citees.All(auTableau.Contains) ? auTableau : null;
        }

        /// <summary>
        /// LE MOT AU TABLEAU, QUAND LE PROFESSEUR DEMANDE DE LE LIRE — Camara, le 04/11/2026 : il
        /// écrit « riz » et dit « Tu peux me lire ce mot ? ». Le son attendu n'est pas dans sa
        /// phrase : le modèle qui lisait ces phrases répondait « pas de demande » (3 sur 7). Ici,
        /// sans aucun appel. Null si le tableau porte autre chose qu'un seul mot, ou si la phrase
        /// ne demande pas de le dire.
        /// </summary>
        public static string? CibleAuTableau(string parole)
        {
            return CibleEcrite(Ardoise().Matches(parole).LastOrDefault(), parole, LanguesDesSons.Francais);
        }

        private static string? CibleEcrite(Match? ecriture, string parole, string langue)
        {
            if (ecriture is null) return null;

            var ecrit = ecriture.Groups[1].Value.Trim().Trim('.', '!', '?', ',', ';', ':');
            if (ecrit.Length == 0 || !ecrit.All(char.IsLetter) || !Verifiable(ecrit)) return null;

            // La demande suit le mot écrit ; s'il finit le message, elle le précède (« Lis ce mot : »).
            var apres = Balise().Replace(parole[(ecriture.Index + ecriture.Length)..], " ").Trim();
            var demande = apres.Any(char.IsLetter) ? apres : Balise().Replace(parole[..ecriture.Index], " ").Trim();

            return EstUnOrdre(demande, langue) && !QuestionSansSon().IsMatch(demande)
                ? Normaliser(ecrit)
                : null;
        }

        /// <summary>
        /// UNE SYLLABE : un groupe de voyelles au plus — « ni », « chat », « oi » ; aucun :
        /// « ffff ». Elle se juge par choix à l'aveugle parmi ses voisins.
        /// </summary>
        public static bool UneSyllabeAuPlus(string attendu)
        {
            var mot = SansAccents(attendu).ToLowerInvariant();
            return mot.Length is > 0 and <= 10
                && mot.All(char.IsLetter)
                && GroupeDeVoyelles().Count(mot) <= 1;
        }

        /// <summary>
        /// UN MOT — Camara, le 04/11/2026, levier retenu : « fais dire la syllabe dans un
        /// mot », puis « si j'ai un mot comme ordinateur ? » : un seul mot, aussi long soit-il
        /// (« extraordinaire »), est vérifiable ; une phrase (un espace) reste au micro.
        /// </summary>
        public static bool Verifiable(string attendu)
        {
            var mot = SansAccents(attendu).ToLowerInvariant();
            return mot.Length is > 0 and <= LettresMax && mot.All(char.IsLetter);
        }

        /// <summary>Le plus long mot qu'on fait lire à l'école (« anticonstitutionnellement » n'en est pas).</summary>
        private const int LettresMax = 20;

        public static bool EstUnMot(string cible) => Verifiable(cible) && !UneSyllabeAuPlus(cible);

        /// <summary>En anglais, tout se transcrit : les listes de sons voisins sont françaises.</summary>
        public static bool SeTranscrit(string cible, string langue) =>
            langue == LanguesDesSons.Anglais ? Verifiable(cible) : EstUnMot(cible);

        /// <summary>
        /// ON COMPARE DES SONS, JAMAIS DES ÉCRITURES — Camara, le 04/11/2026, après son essai :
        /// « lit » bien lu, refusé trois fois. Le modèle avait raison, il entendait « li » ; c'est
        /// la comparaison qui avait tort, « li » ne s'écrit pas « lit ». Deux graphies se valent
        /// quand leurs prononciations de référence se recoupent : « cha » et « chat », « lou » et
        /// « loup », mais pas « sa » et « sac ». Un son tenu n'a pas de référence à comparer.
        /// </summary>
        /// <summary>
        /// LES MOTS BANNIS DES EXERCICES — voir `PromptsPedagogiques.MotsBannis`. Le professeur a la
        /// consigne de ne pas les employer ; s'il en écrit un quand même, il n'est pas jugé, pour
        /// que l'enfant ne reste pas bloqué sur un mot que personne ne sait lui valider.
        /// </summary>
        private static readonly HashSet<string> MotsInreconnaissables = new(Prompts.PromptsPedagogiques.MotsBannis.Split(", "), StringComparer.OrdinalIgnoreCase);

        public static bool Inreconnaissable(string cible) => MotsInreconnaissables.Contains(Normaliser(cible));

        /// <summary>Un message de clôture de séance : son rapport ne demande rien à l'élève.</summary>
        public static bool EstUneCloture(string? contenu) =>
            contenu is not null
            && (contenu.Contains("[FIN_SEANCE]", StringComparison.Ordinal) || contenu.Contains("[RAPPORT]", StringComparison.Ordinal));

        /// <summary>
        /// UNE NOTE DE SÉANCE ([FICHE], [RAPPORT], [FIN_SEANCE]) N'EST NI UN EXERCICE NI UN CHOIX — contrôle d'Omar sur le lot
        /// groupé (22/11/2026, e2e QE1/QR1). Écrite pour les parents au départ de l'élève, elle résume la séance avec ses
        /// propres mots : « répéter un mot avec les sons anglais », « reproduire le son anglais ». Lue comme un message du fil,
        /// elle faisait d'un MOT une syllabe au retour (« Je te la redis. [EN]Two[/EN] ») et ouvrait un exercice de sons
        /// après une simple répétition de couleurs (la carte « [CHOIX_EXERCICE_SONS] » à la place de « Blue »).
        /// </summary>
        public static bool EstUneNoteDeSeance(string? contenu) =>
            EstUneCloture(contenu) || (contenu is not null && contenu.Contains("[FICHE]", StringComparison.OrdinalIgnoreCase));

        public static bool ACompareAuSon(VerdictSon verdict) =>
            !verdict.Juste && !string.IsNullOrEmpty(verdict.Entendu) && !EstTenu(verdict.Attendu);

        public static bool MemeSon(IReadOnlySet<string> reference, IReadOnlySet<string> entendu) =>
            reference.Overlaps(entendu);

        /// <summary>Les voyelles qu'un CP travaille : avec la consonne de la cible, elles donnent une issue à « fa » dit pour « fu ».</summary>
        private static readonly string[] VoyellesCourantes = ["a", "é", "i", "o", "u", "ou", "eu"];

        /// <summary>
        /// La cible et ses voisins : même voyelle avec les consonnes qu'un enfant confond,
        /// même consonne avec TOUTES les voyelles courantes. Construite ici, jamais par le
        /// modèle. Mesuré le 04/11/2026 : sans « fa » dans la liste de « fu », un « fa » dit à
        /// la place passait pour « fu » trois fois sur quatre voix — le modèle prend le plus
        /// proche plutôt que « autre ». Toujours au moins quatre sons, pour que le choix en soit un.
        /// </summary>
        public static IReadOnlyList<string> SonsProches(string cible)
        {
            var son = Normaliser(cible);

            if (EstTenu(son))
            {
                return Tenus.Select(t => MemeLettre(t, son) ? son : t).Distinct().ToList();
            }

            var (consonne, voyelle) = Decouper(son);
            var liste = new List<string> { son };

            if (voyelle.Length > 0 && ConsonnesProches.TryGetValue(consonne, out var consonnes))
            {
                liste.AddRange(consonnes.Select(c => c + voyelle));
            }

            if (VoyellesProches.TryGetValue(voyelle, out var voyelles))
            {
                liste.AddRange(voyelles.Select(v => consonne + v));
            }

            if (voyelle.Length > 0) liste.AddRange(VoyellesCourantes.Select(v => consonne + v));

            foreach (var secours in new[] { "ma", "ti", "po", "lu" })
            {
                if (liste.Count >= 4) break;
                if (!liste.Contains(secours)) liste.Add(secours);
            }

            return liste.Distinct().ToList();
        }

        /// <summary>Le même mélange pour la même liste : un test rejoue ce que la séance a fait.</summary>
        public static IReadOnlyList<string> Melanger(IReadOnlyList<string> liste, int graine) =>
            liste.Select((son, i) => (cle: ((i + 1) * 7919 * (graine + 3)) % 101, son))
                .OrderBy(x => x.cle).ThenBy(x => x.son, StringComparer.Ordinal)
                .Select(x => x.son).ToList();

        /// <summary>Lit la réponse du premier temps ; null quand il n'y a pas de demande d'une syllabe.</summary>
        public static string? LireCible(string? reponse)
        {
            var racine = Json(reponse);
            if (racine is null) return null;

            if (!racine.Value.TryGetProperty("demande", out var demande) || demande.ValueKind != JsonValueKind.True) return null;

            var attendu = Propre(racine.Value, "attendu");
            return Verifiable(attendu) ? attendu : null;
        }

        /// <summary>
        /// Lit la transcription phonétique et la compare aux prononciations de référence du mot,
        /// toutes normalisées. Illisible : rien d'entendu, refus. L'entendu d'un refus est l'IPA
        /// entre barres (« /ʒokola/ ») : c'est ce que le professeur lit.
        /// </summary>
        public static VerdictSon LireMot(string? reponse, string cible, IReadOnlySet<string> ipaAttendus, string langue = LanguesDesSons.Francais)
        {
            var racine = Json(reponse);
            var ipa = racine is null ? string.Empty : NormaliserIpa(Chaine(racine.Value, "ipa"), langue);

            var juste = ipa.Length > 0 && ipaAttendus.Contains(ipa);
            return new VerdictSon(cible, juste || ipa.Length == 0 ? string.Empty : $"/{ipa}/", juste);
        }

        /// <summary>Les prononciations de référence lues dans la réponse d'un modèle texte ; vide si illisible.</summary>
        public static IReadOnlySet<string> LireIpa(string? reponse, string langue = LanguesDesSons.Francais)
        {
            var racine = Json(reponse);
            var sortie = new HashSet<string>(StringComparer.Ordinal);
            if (racine is null || !racine.Value.TryGetProperty("ipa", out var ipa)) return sortie;

            var valeurs = ipa.ValueKind switch
            {
                JsonValueKind.Array => ipa.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString() ?? string.Empty),
                JsonValueKind.String => [ipa.GetString() ?? string.Empty],
                _ => [],
            };

            foreach (var v in valeurs)
            {
                var propre = NormaliserIpa(v, langue);
                if (propre.Length > 0) sortie.Add(propre);
            }

            return sortie;
        }

        /// <summary>
        /// LES PHONÈMES QU'UNE LECTURE JUSTE PEUT FAIRE VARIER sont confondus : ouverture des
        /// voyelles (é/è, o ouvert ou fermé, eu/e muet), les trois « r », « ɲ » et « nj », les
        /// semi-voyelles et leur voyelle. Tout le reste compte : c'est là que sont les erreurs de
        /// lecture (f/v, s/z, ch/j, an/on, u/ou, un son oublié).
        /// </summary>
        public static string NormaliserIpa(string? ipa, string langue = LanguesDesSons.Francais)
        {
            if (string.IsNullOrWhiteSpace(ipa)) return string.Empty;
            if (langue == LanguesDesSons.Anglais) return NormaliserIpaAnglais(ipa);

            var decompose = ipa.Normalize(NormalizationForm.FormD).ToLowerInvariant();
            var sortie = new StringBuilder(decompose.Length);

            for (var i = 0; i < decompose.Length; i += 1)
            {
                var c = decompose[i];
                var nasale = i + 1 < decompose.Length && decompose[i + 1] == '̃';

                if (nasale)
                {
                    sortie.Append(c switch { 'ɑ' or 'a' => 'A', 'ɛ' or 'e' or 'œ' or 'ø' => 'E', 'ɔ' or 'o' => 'O', _ => c });
                    i += 1;
                    continue;
                }

                if (CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpaceSeparator
                    or UnicodeCategory.OtherPunctuation or UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation
                    or UnicodeCategory.ClosePunctuation or UnicodeCategory.ModifierLetter or UnicodeCategory.ModifierSymbol) continue;

                switch (c)
                {
                    case 'ʀ' or 'r': sortie.Append('ʁ'); break;
                    case 'ɑ': sortie.Append('a'); break;
                    case 'ɛ': sortie.Append('e'); break;
                    case 'ɔ': sortie.Append('o'); break;
                    case 'œ' or 'ø' or 'ə': sortie.Append('ø'); break;
                    case 'ɲ': sortie.Append("nj"); break;
                    case 'ɥ': sortie.Append('y'); break;
                    case 'w': sortie.Append('u'); break;
                    case 'ɡ': sortie.Append('g'); break;
                    case 'ˈ' or 'ˌ' or 'ː' or '.' or '/' or '[' or ']' or '-' or '‿': break;
                    default: sortie.Append(c); break;
                }
            }

            return sortie.ToString();
        }

        /// <summary>
        /// L'ANGLAIS D'UN ENFANT FRANÇAIS. Ce qu'il ne distingue pas encore, et que le modèle transcrit
        /// au hasard, est confondu : voyelles longues et brèves (ship/sheep), æ/ʌ/ə/a, britannique
        /// et américain, « r ». Restent distincts : les consonnes, « th », « h », les diphtongues.
        /// </summary>
        private static string NormaliserIpaAnglais(string ipa)
        {
            var texte = ipa.Normalize(NormalizationForm.FormC).ToLowerInvariant();

            foreach (var (groupe, forme) in DiphtonguesAnglaises) texte = texte.Replace(groupe, forme);

            var sortie = new StringBuilder(texte.Length);
            foreach (var c in texte.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpaceSeparator
                    or UnicodeCategory.OtherPunctuation or UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation
                    or UnicodeCategory.ClosePunctuation or UnicodeCategory.ModifierLetter or UnicodeCategory.ModifierSymbol) continue;

                switch (c)
                {
                    case 'ɪ' or 'y': sortie.Append('i'); break;
                    case 'ʊ': sortie.Append('u'); break;
                    case 'ʌ' or 'ɐ' or 'æ' or 'ɑ' or 'ə': sortie.Append('a'); break;
                    case 'ɒ' or 'ɔ': sortie.Append('o'); break;
                    case 'ɛ': sortie.Append('e'); break;
                    case 'ɜ' or 'œ' or 'ø': sortie.Append('ø'); break;
                    case 'ɹ' or 'ʁ' or 'ʀ': sortie.Append('r'); break;
                    case 'ɾ': sortie.Append('t'); break;
                    case 'ɡ': sortie.Append('g'); break;
                    case 'ˈ' or 'ˌ' or 'ː' or '.' or '/' or '[' or ']' or '-' or '‿': break;
                    default: sortie.Append(c); break;
                }
            }

            return sortie.ToString();
        }

        /// <summary>Avant les voyelles seules : « əʊ » ne doit pas devenir « au ». Majuscules : des sons qui restent d'un bloc.</summary>
        private static readonly (string Groupe, string Forme)[] DiphtonguesAnglaises =
        [
            ("tʃ", "C"), ("ʧ", "C"), ("dʒ", "J"), ("ʤ", "J"),
            ("əʊ", "O"), ("oʊ", "O"), ("ɔʊ", "O"), ("eɪ", "E"), ("ɛɪ", "E"),
            ("aɪ", "A"), ("ʌɪ", "A"), ("ɑɪ", "A"), ("ɔɪ", "Y"), ("aʊ", "W"), ("ɑʊ", "W"),
            ("ɝ", "ɜr"), ("ɚ", "ər"),
        ];

        /// <summary>
        /// Lit le choix du second temps. Juste quand le son choisi est la cible, à la
        /// longueur près pour un son tenu (« sss » pour « ssss »). Illisible ou hors liste :
        /// « autre », ce qui vaut un refus avec « aucun son clair ».
        /// </summary>
        public static VerdictSon LireChoix(string? reponse, string cible, IReadOnlyList<string> liste)
        {
            var racine = Json(reponse);
            var entendu = racine is null ? Autre : Propre(racine.Value, "entendu");

            if (!liste.Contains(entendu)) entendu = Autre;

            var juste = entendu == cible || (EstTenu(cible) && MemeLettre(entendu, cible));
            return new VerdictSon(cible, entendu == Autre ? string.Empty : entendu, juste);
        }

        /// <summary>Ce qui s'écrit à la place du texte du micro : vide, il reste ce que le micro a écrit.</summary>
        public static string TexteDeLEleve(VerdictSon verdict) =>
            verdict.SansJugement ? Prompts.PromptsPedagogiques.SonFait : verdict.Juste ? verdict.Attendu : verdict.Entendu;

        /// <summary>
        /// LE FAIT DONNÉ AU PROFESSEUR, jamais persisté, comme les autres marqueurs. Sans le
        /// mot « écoute » : tout texte joint au tour passe par le détecteur d'exercices de
        /// langue, et ce mot-là chargerait des consignes pour rien.
        /// </summary>
        public static string FaitPourLeProfesseur(VerdictSon verdict, string suite = "") => verdict.SansJugement
            ? $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : l'élève a bien parlé, mais « {verdict.Attendu} » ne fait pas partie des MOTS VÉRIFIÉS : personne ne peut dire si sa prononciation est juste. "
              + $"Ne redemande plus JAMAIS « {verdict.Attendu} » de toute la séance. Ne dis ni que c'est juste ni que c'est faux. Ne dis pas que tu l'entends mal, et ne lui demande jamais de taper au clavier. "
              + $"Encourage-le en une phrase, puis passe à un autre mot.{suite} Ne parle jamais de cet outil à l'élève.]"
            : verdict.Juste
            ? $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : un outil a analysé la voix de l'élève et a bien entendu « {verdict.Attendu} », le son demandé. "
              + "Son écran affiche déjà « Bonne prononciation » en vert : valide-le, sans le contredire, et ne le lui fais PAS redire : passe à un autre son ou à un autre mot.{suite} Ne parle jamais de cet outil à l'élève.]"
            : $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : un outil a analysé la voix de l'élève et a entendu « {Lisible(verdict.Entendu)} » alors que tu as demandé « {verdict.Attendu} ». "
              + "Cet outil se trompe souvent : la réponse était peut-être juste. Ne dis ni « c'est faux » ni « pas tout à fait ». Son écran affiche « On réessaie » : "
              + $"redis le son toi-même et fais-le redire, avec encouragement. Si tu viens déjà de le faire redire, encourage-le et passe à la suite.{suite} Ne parle jamais de cet outil à l'élève.]";

        /// <summary>Les graphies qui font le même son : « foin » et « loin », « lapin » et « rein ». Les plus longues d'abord.</summary>
        private static readonly string[][] FamillesDeSons =
        [
            ["oin", "oing"], ["ain", "aim", "ein", "in", "im"], ["an", "am", "en", "em"], ["on", "om"],
            ["oi"], ["ou"], ["eau", "au"], ["eu", "œu"], ["ai", "ei"],
        ];

        private static IEnumerable<string> MemeSon(string mot, IReadOnlyCollection<string> fiables)
        {
            var famille = FamillesDeSons.FirstOrDefault(f => f.Any(mot.Contains));

            return famille is not null
                ? fiables.Where(m => FamillesDeSons.FirstOrDefault(f => f.Any(m.Contains)) == famille)
                : mot.Length <= 3
                    ? fiables.Where(m => m.Length <= 3 && m[0] == mot[0])
                    : [];
        }

        /// <summary>
        /// LES MOTS ENCORE PERMIS, DITS AU TOUR MÊME — Camara, le 05/11/2026, capture à l'appui : Adrien
        /// redemandait « foin » trois fois, puis « poing », puis « pain ». La famille « oin » n'a que
        /// quatre sons vérifiés : sitôt « coin » et « loin » faits, il en inventait d'autres. La liste du
        /// profil, soixante mille jetons plus haut, ne pesait pas. Vide en anglais : pas de familles.
        /// </summary>
        public static string SuiteDesSons(string cible, IReadOnlyCollection<string> fiables, string langue = LanguesDesSons.Francais)
        {
            if (langue != LanguesDesSons.Francais || fiables.Count == 0) return string.Empty;

            var mot = Normaliser(cible);
            var famille = FamillesDeSons.FirstOrDefault(f => f.Any(mot.Contains));

            var autres = MemeSon(mot, fiables).Where(m => m != mot && !Inreconnaissable(m)).Take(14).ToList();

            if (autres.Count > 0)
            {
                return $" Pour ce son, les SEULS autres mots ou syllabes que tu peux faire dire sont : {string.Join(", ", autres)}. "
                    + "Prends-en un que tu n'as pas encore fait dire ; s'ils sont tous faits, change de son, sans chercher d'autre mot de celui-ci.";
            }

            return famille is null
                ? " Prends-le dans la liste MOTS VÉRIFIÉS."
                : " Aucun autre mot de ce son n'est vérifié : change de son, en prenant un mot de la liste MOTS VÉRIFIÉS.";
        }

        /// <summary>
        /// SOMMES-NOUS DANS UN EXERCICE DE SONS OU DE SYLLABES ? Camara, le 05/11/2026 : « cette
        /// limitation est uniquement pour les syllabes, pour le reste on reste comme on était ». Lu
        /// dans les derniers messages : le professeur a parlé d'un son, de syllabes, ou un verdict vient
        /// de passer. HORS DE CET EXERCICE : ni juge, ni liste, ni filtre, ni rappel, le micro normal.
        /// </summary>
        public static bool EnExerciceDeSons(IEnumerable<string?> messagesRecents) =>
            // Une note de séance ne parle que de la séance finie (voir EstUneNoteDeSeance) : « reproduire le son anglais ».
            messagesRecents.Any(m => m is not null && !EstUneNoteDeSeance(m)
                && (m.Contains(Prompts.PromptsPedagogiques.SonFait, StringComparison.Ordinal)
                    // « Essaie de reproduire le son. [EN]Blue[/EN] Tu répètes ? » : une répétition dans la langue, pas un exercice
                    // de sons (RepetitionEnLangue.EstUneDemandeDeRepetitionEnLangue, conv. de test 1073).
                    || (MentionDeSons().IsMatch(m) && !RepetitionEnLangue.EstUneDemandeDeRepetitionEnLangue(m))
                    // LA LECTURE EN IMAGES (07/11/2026) : l'image montrée, ou « je veux lire des mots », qui ne nomme aucun son.
                    || LectureDImage.Affiche(m) is not null || LectureDImage.ChoixDeLEnfant(m) is not null));

        /// <summary>
        /// UN VERDICT RÉCENT PROUVE QUE L'EXERCICE CONTINUE — Camara, le 05/11/2026 : « on est toujours sur
        /// les syllabes et ça s'est mis à écrire ». Tant que tout allait bien, Adrien répétait « On continue
        /// avec un autre mot » : douze messages sans « syllabe », et le code croyait l'exercice fini, au
        /// milieu d'une série de « Bonne prononciation ». Ce sont les messages tels qu'en base.
        /// </summary>
        public static bool EnExerciceDeSons(IEnumerable<(string? Contenu, bool? SonJuste)> messagesRecents)
        {
            var messages = messagesRecents.ToList();
            // SAUF LA RÉPÉTITION EN LANGUE TAPÉE (21/11/2026, voir RepetitionEnLangue.MarqueDeLaReponse) : son verdict est posé
            // HORS de tout exercice de sons, il ne doit pas en ouvrir un.
            return messages.Any(m => m.SonJuste is not null && !RepetitionEnLangue.EstUneReponseJugee(m.Contenu))
                || EnExerciceDeSons(messages.Select(m => m.Contenu));
        }

        /// <summary>
        /// UN MESSAGE DU PROFESSEUR PORTE-T-IL UN VRAI SIGNE D'EXERCICE DE SONS ? Après que l'enfant a demandé à changer
        /// d'exercice (14/11/2026, voir ChangementDExercice), « d'accord, on laisse les syllabes » ne doit pas rouvrir
        /// l'exercice : le mot seul ne suffit plus. Il faut ce qui FAIT l'exercice : une balise de voix, un tableau, une
        /// image à lire, la carte du choix, ou la consigne (« qu'as-tu entendu », « voici la prochaine syllabe »).
        /// </summary>
        public static bool PorteUnExercice(string? messageDuProfesseur) =>
            !string.IsNullOrWhiteSpace(messageDuProfesseur)
            && (PassageDeVoix().IsMatch(messageDuProfesseur)
                || messageDuProfesseur.Contains("[ARDOISE]", StringComparison.OrdinalIgnoreCase)
                || messageDuProfesseur.Contains(Prompts.PromptsPedagogiques.SonFait, StringComparison.Ordinal)
                || LectureDImage.Affiche(messageDuProfesseur) is not null
                || messageDuProfesseur.Contains(LectureDImage.BaliseChoix[..^1], StringComparison.OrdinalIgnoreCase)
                || AttendCeQuIlAEntendu().IsMatch(messageDuProfesseur)
                || AnnonceDuSon().IsMatch(messageDuProfesseur));

        public const string BaliseSupportSon = "[SUPPORT_SON]";

        /// <summary>
        /// L'ÉCOUTE DES MOTS — Camara, le 05/11/2026 : « je veux qu'il lui dise "Tu diras : j'ai entendu le mot + le mot",
        /// car sans ça le mot peut ne pas partir ». Dite avant le premier mot.
        /// </summary>
        public const string ConsigneEcouteDesMots = "Tu diras : j'ai entendu le mot, puis le mot que tu as entendu.";

        /// <summary>Camara, le 05/11/2026 : « pour l'écoute des syllabes aussi : "J'ai entendu la syllabe + la syllabe" ».</summary>
        public const string ConsigneEcouteDesSyllabes = "Tu diras : j'ai entendu la syllabe, puis la syllabe que tu as entendue.";

        [GeneratedRegex(@"\b(?:ce|le|un|nouveau|autre|des|les|aux|du|dernier|prochain)\s+mots?\b|\bmot\s+(?:suivant|entier)", RegexOptions.IgnoreCase)]
        private static partial Regex ParleDeMot();

        /// <summary>
        /// MOT OU SYLLABE : CE QUE LE PROFESSEUR ANNONCE, DANS CE MESSAGE D'ABORD — relevé le 05/11/2026 : « On n'est plus sur
        /// les syllabes, je te fais écouter des mots » suivi de « four », une syllabe, recevait la consigne des syllabes.
        /// Le premier texte est le message du tour ; les suivants, le fil, du plus ancien au plus récent.
        /// </summary>
        public static bool EstEcouteDeSyllabe(string? mot, params string?[] textes)
        {
            if (mot is null || textes.Length == 0 || textes[0] is null) return false;

            // LE CHOIX DE L'ENFANT D'ABORD — Camara, le 05/11/2026 : « il passe à l'écoute des mots sans que l'enfant l'ait
            // demandé ». Adrien appelait « nouveau mot » les syllabes qu'il dictait, et la consigne suivait son mot.
            switch (TypeDEcoute(textes.Skip(1)))
            {
                case Syllabe: return true;
                case Mot: return false;
            }

            if (!UneSyllabeAuPlus(mot)) return false;

            var courant = BaliseEtMotsOtes(textes[0]!);
            if (ParleDeMot().IsMatch(courant)) return false;
            if (ParleDeSyllabe().IsMatch(courant)) return true;

            // Sinon la dernière consigne donnée, puis le dernier message qui tranche.
            // Une note de séance ne tranche rien (voir EstUneNoteDeSeance) : « des mots entiers » écrit pour les parents.
            foreach (var t in textes.Skip(1).Reverse())
            {
                if (t is null || EstUneNoteDeSeance(t)) continue;
                if (ConsigneDeLaSyllabe().IsMatch(t)) return true;
                if (ConsigneDeLEcoute().IsMatch(t)) return false;
            }

            foreach (var t in textes.Skip(1).Reverse())
            {
                if (t is null || EstUneNoteDeSeance(t)) continue;
                var propre = BaliseEtMotsOtes(t);
                if (ParleDeMot().IsMatch(propre)) return false;
                if (ParleDeSyllabe().IsMatch(propre)) return true;
            }

            return false;
        }

        private static string BaliseEtMotsOtes(string t) => PassageDeVoix().Replace(t, " ");

        /// <summary>Le professeur attend-il que l'enfant dise le mot qu'il vient d'entendre ? Sans contexte : la question doit être dans le message.</summary>
        public static bool EstEcouteDeMot(string? parole) => EstEcouteDeMot(parole, false);

        /// <summary>
        /// AVEC LE CONTEXTE — relevé le 05/11/2026 : après trois tours jugés, Adrien a écrit « Écoute ce nouveau mot. » sans
        /// la question, et les verdicts ont disparu. En plein exercice d'écoute, un mot seul dit à voix haute suffit.
        /// Jamais quand il fait LIRE un mot au tableau, ni quand il fait écrire : ce sont d'autres exercices.
        /// </summary>
        public static bool EstEcouteDeMot(string? parole, bool contexteDEcoute)
        {
            if (parole is null || EstUneCloture(parole) || DicteeDeMots.MotDit(parole) is null) return false;

            var mots = DerniersMots(parole);
            if (EcrireCeQuIlEntend().IsMatch(mots) || CibleAuTableau(parole) is not null) return false;

            return AttendCeQuIlAEntendu().IsMatch(mots) || AnnonceDuSon().IsMatch(mots) || contexteDEcoute;
        }

        public const string Syllabe = "syllabe";
        public const string Mot = "mot";

        [GeneratedRegex(@"\b(?:des|les|aux|de)\s+mots\b|\bmots\s+entiers\b|\bécoute\s+des\s+mots\b|\bj.ai\s+entendu\s+le\s+mot\b", RegexOptions.IgnoreCase)]
        private static partial Regex LEnfantParleDeMots();

        [GeneratedRegex(@"\b(?:[ée]cout\w*|entend\w*)\b", RegexOptions.IgnoreCase)]
        private static partial Regex VerbeDEcoute();

        // « Je veux écouter des sons » : le bouton de l'anglais, où les « syllabes » sont les sons (07/11/2026).
        [GeneratedRegex(@"\bsyllabes?\b|\b(?:des|les)\s+sons\b", RegexOptions.IgnoreCase)]
        private static partial Regex LEnfantParleDeSyllabes();

        // « T'as encore changé d'exercice », « pourquoi pied ? » : une plainte. « Je veux changer d'exercice, je vais faire
        // les syllabes » : une DEMANDE (Camara, 05/11/2026, 21h01 : elle était prise pour une plainte, et ignorée).
        [GeneratedRegex(@"\bt.as\s+(?:encore\s+)?chang|\btu\s+as\s+(?:encore\s+)?chang|\bpourquoi\b|\bon\s+est\s+sur\b|\bc.est\s+quoi\b|\?", RegexOptions.IgnoreCase)]
        private static partial Regex PlainteOuQuestion();

        /// <summary>
        /// CE QUE L'ENFANT A CHOISI : syllabes ou mots. Lu dans SES messages, du plus récent au plus ancien ; un message
        /// qui dit les deux (« syllabes ou mots ? ») ne tranche pas. Le professeur ne compte que par la consigne déjà
        /// donnée, jamais par sa façon de nommer ce qu'il dicte. Null : rien de choisi.
        /// </summary>
        public static string? TypeDEcoute(IEnumerable<string?> recents)
        {
            // LE TYPE SE LIT DANS LE FIL D'AVANT LA NOTE DE SÉANCE — contrôle d'Omar sur le lot groupé (22/11/2026, e2e QE1) :
            // au retour après « Quitter », la fiche de fin (« répéter un mot avec les sons anglais ») passait pour le choix
            // de l'enfant (« des/les sons » = syllabes), et l'écoute de Two reprenait en « Je te la redis. ». Une note de
            // séance n'est ni un choix ni une consigne : elle est sautée, le fil d'avant décide. Voir EstUneNoteDeSeance.
            IReadOnlyList<string?> liste = recents.Where(r => !EstUneNoteDeSeance(r)).ToList();

            // L'EXERCICE QUE LE PROFESSEUR TIENT EN CE MOMENT — production, le 07/11/2026 : en pleine lecture en images,
            // « change de syllabe » ou « je suis bloqué sur cette syllabe » faisait basculer vers l'ÉCOUTE des syllabes
            // (le mot « syllabe » suffisait), et Adrien repartait sur son tableau et le juge de prononciation. Tant qu'une
            // image à lire est le dernier exercice ouvert, il faut un verbe (« écouter », « entendre », « lire ») pour changer.
            var enLectureDImages = liste.Reverse().Where(r => r is not null).Select(r => r!)
                .Select(r => LectureDImage.TypeDuMessage(r) is not null ? (bool?)true
                    : ConsigneDeLaSyllabe().IsMatch(r) || AnnonceDeSyllabe().IsMatch(r) || ConsigneDeLEcoute().IsMatch(r) || AnnonceDeMot().IsMatch(r) ? false
                    : null)
                .FirstOrDefault(e => e is not null) == true;

            foreach (var r in liste.Reverse())
            {
                if (r is null) continue;

                // LA LECTURE EN IMAGES (07/11/2026) : l'image que le code vient de montrer dit l'exercice en cours.
                if (LectureDImage.TypeDuMessage(r) is { } montre) return montre;

                var estDuProfesseur = PassageDeVoix().IsMatch(r) || r.Contains("[ARDOISE]", StringComparison.OrdinalIgnoreCase)
                    || ConsigneDeLEcoute().IsMatch(r) || ConsigneDeLaSyllabe().IsMatch(r) || AttendCeQuIlAEntendu().IsMatch(r)
                    || AnnonceDuSon().IsMatch(r);

                if (estDuProfesseur)
                {
                    if (ConsigneDeLaSyllabe().IsMatch(r) || AnnonceDeSyllabe().IsMatch(r)) return Syllabe;
                    if (ConsigneDeLEcoute().IsMatch(r) || AnnonceDeMot().IsMatch(r)) return Mot;
                    continue;
                }

                // UNE PLAINTE OU UNE QUESTION N'EST PAS UN CHOIX : « là on est sur des mots, t'as encore changé » (05/11/2026).
                if (PlainteOuQuestion().IsMatch(r)) continue;

                // « JE VEUX LIRE DES SYLLABES » : la lecture, avant « syllabes » qui dirait l'écoute.
                if (LectureDImage.ChoixDeLEnfant(r) is { } lecture) return lecture;

                // « UN EXERCICE SUR LES MOTS », SANS DIRE LEQUEL (07/11/2026) : rien n'est choisi, la carte le lui demande.
                if (LectureDImage.DemandeSansPrecision(r)) return null;

                // EN LECTURE EN IMAGES, « SYLLABE » SEUL NE CHANGE RIEN : voir enLectureDImages.
                if (enLectureDImages && !VerbeDEcoute().IsMatch(r)) continue;

                // LES DEUX DANS LA MÊME PHRASE : LE DERNIER NOMMÉ EST CE QU'IL VEUT — « je veux plus faire de mots, je veux
                // faire les syllabes » (Camara, 05/11/2026, 21h03). Une question (« syllabes ou mots ? ») est déjà écartée.
                var syllabes = LEnfantParleDeSyllabes().Matches(r).LastOrDefault();
                var mots = LEnfantParleDeMots().Matches(r).LastOrDefault();
                if (syllabes is not null && (mots is null || syllabes.Index > mots.Index)) return Syllabe;
                if (mots is not null) return Mot;
            }

            return null;
        }

        /// <summary>Le fait du tour, quand l'exercice est connu : le professeur n'en change pas de lui-même.</summary>
        public static string FaitTypeDEcoute(string type, IEnumerable<string?>? recents = null, string? niveau = null, bool anglais = false)
        {
            // EN ANGLAIS, JAMAIS DE SYLLABE FRANÇAISE — Camara, le 06/11/2026 : « c'est une erreur super grave, je suis en
            // anglais et elle me sort des syllabes en français ». Ce fait donnait à Marine les exemples du français
            // (jou, bon, cha, ri), et elle dictait « ri ». En anglais, les « syllabes » sont les SONS de l'anglais, travaillés
            // dans de petits mots anglais de la liste vérifiée, en attendant la banque des sons de Marine.
            if (anglais)
            {
                // LES MOTS QUI ONT UNE IMAGE (07/11/2026) : 242 mots de Camara, du CP à sa classe ; l'image s'affiche quand il
                // a trouvé. Une douzaine par tour, ceux déjà dictés en moins : une règle ne vaut pas un fait.
                if (type != Syllabe)
                {
                    var illustres = MotsAnglaisIllustres.JusquA(niveau);
                    if (illustres.Count == 0) return FaitEcouteDeMotsAnglais;
                    var fil = recents?.ToList() ?? [];
                    var dicte = fil.Where(r => r is not null && PassageDeVoix().IsMatch(r)).Select(r => DicteeDeMots.MotDit(r!))
                        .Where(d => d is not null).Select(d => d!).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var libres = illustres.Where(m => !dicte.Contains(m.Mot)).ToList();
                    if (libres.Count == 0) libres = illustres.ToList();
                    var pas = Math.Max(1, libres.Count / 12);
                    var proposes = Enumerable.Range(0, Math.Min(12, libres.Count))
                        .Select(i => libres[(fil.Count + i * pas) % libres.Count].Mot).Distinct();
                    return FaitEcouteDeMotsAnglais[..^1] + " Choisis de préférence un mot qui a son image : " + string.Join(", ", proposes) + ".]";
                }

                // LES MOTS DES IMAGES DE SA CLASSE (06/11/2026) : chaque son de l'anglais a une image, qui s'affiche au
                // tableau quand l'enfant trouve SON mot (front, lib/imagesSonsAnglais.js). Le professeur les reçoit
                // pour les choisir : une règle ne vaut pas un fait.
                var sons = SonsAnglaisIllustres.DeLaClasse(niveau);
                return sons.Count == 0 ? FaitSonsAnglais
                    : FaitSonsAnglais[..^1] + " Pour les sons de sa classe, prends de préférence le mot de leur image : "
                        + string.Join(", ", sons.Select(s => $"{s.Son} → {s.Mot}")) + ".]";
            }

            if (type != Syllabe)
            {
                // LES MOTS DE SA CLASSE, DITS AU PROFESSEUR — voir BanqueDeSons.PropositionDeMots.
                var rang = BanqueDeSons.RangDeClasse(niveau);
                if (recents is null || rang == 0) return FaitEcouteDeMots;
                var tous = recents.ToList();
                var dits = tous.Where(r => r is not null && PassageDeVoix().IsMatch(r)).Select(r => DicteeDeMots.MotDit(r!));
                var mots = BanqueDeSons.PropositionDeMots(rang, tous.Count.ToString(), dits, 12);
                return mots.Count == 0 ? FaitEcouteDeMots
                    : FaitEcouteDeMots[..^1] + " Tu choisis le mot parmi : " + string.Join(", ", mots) + ".]";
            }

            // LES SYLLABES DE LA BANQUE, DITES AU PROFESSEUR : une règle ne vaut pas un fait.
            string? choix = null;
            if (recents is not null)
            {
                var liste = recents.ToList();
                var dites = liste.Where(r => r is not null && PassageDeVoix().IsMatch(r)).Select(r => DicteeDeMots.MotDit(r!));
                var proposees = BanqueDeSons.Proposition(liste.Count.ToString(), dites, 12);
                if (proposees.Count > 0) choix = " Tu choisis la syllabe parmi : " + string.Join(", ", proposees) + ".";
            }

            return "[Fait pour ce tour : l'élève a choisi l'écoute de SYLLABES. Tu dictes UNE syllabe (jou, bon, cha, ri), jamais un mot entier, "
                + "et tu dis « syllabe », jamais « mot ». Tu ne passes aux mots que s'il le demande lui-même." + choix + "]";
        }

        private const string FaitEcouteDeMots = "[Fait pour ce tour : l'élève a choisi l'écoute de MOTS. Tu dictes un mot court et tu dis « mot ». Tu ne passes aux syllabes que s'il le demande lui-même.]";

        /// <summary>Les sons de l'anglais, dans de petits mots anglais (06/11/2026) : voir FaitTypeDEcoute.</summary>
        public const string FaitSonsAnglais =
            "[Fait pour ce tour : cours d'ANGLAIS, l'élève travaille les SONS DE L'ANGLAIS. Il n'y a pas de syllabes françaises ici : "
            + "ne dicte JAMAIS une syllabe française (ri, cha, bon, jou, ba) ni un mot français. Tu annonces en français le son travaillé "
            + "(« le son sh », « le a de cat »), puis tu dictes UN petit mot anglais qui le contient, pris dans la liste MOTS VÉRIFIÉS, "
            + "dans ta balise [EN] (ship, fish, sheep, moon, cat, dog), et tu dis « Voici le prochain mot. ».]";

        public const string FaitEcouteDeMotsAnglais =
            "[Fait pour ce tour : cours d'ANGLAIS, l'élève a choisi l'écoute de MOTS ANGLAIS. Tu dictes UN mot anglais court de la "
            + "liste MOTS VÉRIFIÉS dans ta balise [EN], jamais un mot ni une syllabe en français, et tu dis « mot ».]";

        /// <summary>« chapeau » dicté en plein exercice de syllabes devient « cha » : jusqu'au premier groupe de voyelles, avec sa nasale.</summary>
        public static string PremiereSyllabe(string mot)
        {
            var propre = mot.Trim().ToLowerInvariant();
            var sans = SansAccents(propre);
            var i = 0;
            while (i < sans.Length && !"aeiouy".Contains(sans[i])) i++;
            if (i >= sans.Length) return propre;
            while (i < sans.Length && "aeiouy".Contains(sans[i])) i++;
            if (i < sans.Length && (sans[i] == 'n' || sans[i] == 'm') && (i + 1 >= sans.Length || !"aeiouy".Contains(sans[i + 1]))) i++;
            return propre[..i];
        }

        /// <summary>
        /// Le fil est-il dans un exercice d'écoute ? Le dernier signe l'emporte : la consigne, la question ou une réponse
        /// « j'ai entendu le mot » ouvrent ; un fait d'écriture ([SON AU) ferme.
        /// </summary>
        public static bool ContexteDEcoute(IEnumerable<string?> recents)
        {
            foreach (var r in recents.Reverse())
            {
                if (r is null) continue;
                if (r.Contains(MarqueurSonAuCahier, StringComparison.Ordinal) || r.Contains(MarqueurSonAuClavier, StringComparison.Ordinal)) return false;
                if (ConsigneDeLEcoute().IsMatch(r) || ConsigneDeLaSyllabe().IsMatch(r) || AttendCeQuIlAEntendu().IsMatch(r) || AnnonceDuSon().IsMatch(r)) return true;
            }

            return false;
        }

        [GeneratedRegex(@"entendu\s+(?:l[ea]\s+)?(?:(?:mot|syllabe)\s+)?(?<mot>[\p{L}'’-]+)", RegexOptions.IgnoreCase)]
        private static partial Regex ReponseEntendu();

        private static readonly HashSet<string> PasUneReponse = new(StringComparer.OrdinalIgnoreCase)
        {
            "répète", "repete", "répéter", "repeter", "quoi", "oui", "non", "pardon", "comment", "encore", "aide", "stop", "attends",
        };

        /// <summary>
        /// Le mot que l'enfant dit avoir entendu ; null si ce n'est pas une réponse (« Quoi ? Répète. »,
        /// « Je ne sais pas »). Relevé en séance : « Sac. J'ai entendu le mossac. » : le mot collé se retrouve à la comparaison.
        /// </summary>
        public static string? ReponseDEcoute(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var trouve = ReponseEntendu().Matches(texte).LastOrDefault();
            if (trouve is not null)
            {
                var entendu = Normaliser(trouve.Groups["mot"].Value);
                return entendu is "mot" or "syllabe" or "le" or "la" ? null : entendu;
            }

            var mots = texte.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Normaliser).Where(m => m.Length > 0).ToList();
            return mots.Count is >= 1 and <= 2 && !mots.Any(PasUneReponse.Contains) ? mots[^1] : null;
        }

        /// <summary>
        /// LE MOT BANAL QUI PEUT ÊTRE LA RÉPONSE — Camara, le 07/11/2026 : il lit « coi » (coiffeur), le micro écrit
        /// « Quoi ? », et « quoi » est dans la liste de ce qui n'est pas une réponse : aucun verdict, « c'est pas bon ».
        /// Un mot seul de cette liste est rendu ici ; l'appelant ne le retient que s'il SONNE comme la cible : « quoi »
        /// pour « coi » est juste, « quoi ? » devant « bi » reste une question sans verdict.
        /// </summary>
        public static string? MotBanal(string? texte)
        {
            var mots = MotsDUneReponseCourte(texte);
            return mots is { Count: 1 } && EstBanal(mots[0]) ? mots[0] : null;
        }

        /// <summary>Les mots d'une réponse courte (un ou deux), normalisés ; null si c'est vide ou une phrase.</summary>
        public static IReadOnlyList<string>? MotsDUneReponseCourte(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;
            var mots = texte.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Normaliser).Where(m => m.Length > 0).ToList();
            return mots.Count is >= 1 and <= 2 ? mots : null;
        }

        public static bool EstBanal(string mot) => PasUneReponse.Contains(mot);

        /// <summary>L'écriture ne compte pas ici : « Riz », « ris » se comparent ensuite au son. Le micro colle parfois « mot » et le mot.</summary>
        public static bool MemeMotEntendu(string dit, string mot)
        {
            var a = SansAccents(Normaliser(dit));
            var b = SansAccents(Normaliser(mot));
            if (a == b) return true;

            // « le mossac » : le micro colle « mot » au mot, et le t tombe dans la liaison.
            return a.Length > b.Length && a.EndsWith(b, StringComparison.Ordinal) && a[..^b.Length] is "mo" or "mos" or "mot";
        }

        public static string FaitEcoute(VerdictSon verdict, bool syllabe = false, string? francais = null)
        {
            var nom = syllabe ? "cette syllabe" : "ce mot";
            var autre = syllabe ? "une nouvelle syllabe" : "un nouveau mot";
            const string bon = "Bonne écoute";
            const string faux = "Mauvaise écoute";

            // LE MOT FRANÇAIS (retest KO #57, 21/11/2026) : la même consigne qu'au clavier (RepetitionEnLangue.Fait), sans
            // « Oui » ni « Bravo », et le même mot redit. Son écran dit « On réessaie », pas « Mauvaise écoute » : décision de
            // Min's du 22/11/2026, voir TexteDEcoute.
            if (!verdict.Juste && francais is not null)
            {
                return $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : tu as dit « {verdict.Attendu} » et il a répondu "
                    + $"« {verdict.Entendu} » : c'est le mot en FRANÇAIS (« {francais} »), pas le mot qu'il a entendu. Son écran affiche "
                    + "« On réessaie ». Ce n'est pas la réponse : ne commence ni par « Oui », ni par « Parfait », ni par « Bravo », ni par « C'est bien ça », et ne dis "
                    + "pas que c'est juste. Sans le gronder, dis-lui que ça, c'est le mot en français — il en a bien compris le sens —, "
                    + $"puis redis LE MÊME MOT « {verdict.Attendu} » dans ta balise de voix et redemande ce qu'il a entendu. NE PASSE PAS à "
                    + "un autre mot à ce tour. Ne parle jamais de cet outil à l'élève." + RepetitionEnLangue.SansFelicitation() + "]";
            }

            return verdict.Juste
                ? $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : il a bien entendu « {verdict.Attendu} ». Son écran affiche déjà « {bon} » en vert : valide-le en une phrase et dis {autre} dans ta balise de voix. Ne parle jamais de cet outil à l'élève.]"
                : $"[Fait pour ce tour, qui l'emporte sur le rappel précédent : tu as dit « {verdict.Attendu} » et il dit avoir entendu « {verdict.Entendu} ». Son écran affiche « {faux} ». Encourage-le, n'écris pas {nom}, ne passe pas à autre chose : redis-la dans ta balise de voix et redemande ce qu'il a entendu. Ne parle jamais de cet outil à l'élève.]".Replace("redis-la", syllabe ? "redis-la" : "redis-le");
        }

        /// <summary>
        /// Ce qui s'enregistre à la place de sa réponse d'écoute jugée : « J'ai entendu le mot two. » (ou « la syllabe »). Le
        /// MÊME texte au micro (EcouteController) et au clavier (ChatViewModelBuilder) : c'est ce début que l'écran lit pour
        /// écrire « Bonne écoute » ou « Mauvaise écoute », et le compte des essais relit le mot dans la balise du professeur.
        ///
        /// LE REFUS QUI VIENT D'UNE TRADUCTION — décision de Min's (22/11/2026, 00 h 04) : « deux » pour two n'est pas une
        /// mauvaise écoute (il a compris le mot), l'écran dit « On réessaie ». Le texte gardé porte alors la marque
        /// <see cref="MarqueDeTraduction"/> (« J'ai entendu le mot deux (en français). ») : c'est elle que l'écran lit, aussi
        /// après rechargement, sans rien changer en base. Le début reste le même : le compte des essais, la reprise du mot et
        /// le classement de l'exercice ne changent pas. Au micro comme au clavier.
        /// </summary>
        public static string TexteDEcoute(VerdictSon ecoute, string? typeDEcoute, string? parole, string? texte) =>
            (typeDEcoute == Syllabe || (typeDEcoute is null && EstEcouteDeSyllabe(ecoute.Attendu, parole, texte))
                ? $"J'ai entendu la syllabe {ecoute.Entendu}"
                : $"J'ai entendu le mot {ecoute.Entendu}")
            + (!ecoute.Juste && ecoute.Francais is not null ? MarqueDeTraduction : string.Empty)
            + ".";

        /// <summary>
        /// Ce qui suit le mot dans le texte gardé quand sa réponse était la traduction française du mot à écouter. Le même
        /// texte côté écran (src/components/VerdictSon.js, `estTraductionEntendue`) : ne pas le changer d'un seul côté.
        /// </summary>
        public const string MarqueDeTraduction = " (en français)";

        /// <summary>Un verdict d'écoute gardé avec la marque de traduction ?</summary>
        public static bool EstUneTraductionEntendue(string? contenu) =>
            contenu is not null && DebutJaiEntendu().IsMatch(contenu)
            && contenu.TrimEnd().TrimEnd('.').EndsWith(MarqueDeTraduction, StringComparison.Ordinal);

        [GeneratedRegex(@"^\s*j\W?\s*ai\s+entendu\s", RegexOptions.IgnoreCase)]
        private static partial Regex DebutJaiEntendu();

        /// <summary>
        /// L'ÉCOUTE DES MOTS, TAPÉE AU CLAVIER — contrôle d'Omar sur le #57 v2, accord de Min's (21/11/2026, 16 h : « oui faut
        /// modifier l'écran pour compter 3 essais au lieu de 5 »). Seul le relais du micro jugeait l'écoute : une réponse tapée
        /// n'avait ni verdict à l'écran ni essai compté (« Deux! » pour Two, cinq fois de suite, jusqu'au filet des cinq
        /// dictées). Ce qu'il tape est maintenant jugé au serveur par le même service que le micro (VerifierEcouteAsync).
        ///
        /// Est-ce une TENTATIVE à juger ? Oui : un ou deux mots (« Deux! », « two ? »), ou « j'ai entendu le mot … ». Non : une
        /// demande (« je sais pas », « tu peux répéter ? », « c'est quoi ? », « ok »), un changement d'exercice, un au revoir,
        /// un message marqué par l'écran (« [EXERCICE CHOISI : …] »), plusieurs lignes. Comme la lecture tapée (#49) : au
        /// clavier, rien ne déforme, une demande part telle quelle au professeur et ne compte pas.
        /// </summary>
        public static bool EstUneEcouteTapee(string? message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Contains('[') || message.Contains('\n')) return false;
            if (ChangementDExercice.Demande(message) || LectureDImage.ChangeDExercice(message) || LecteurAuRevoir.DitAuRevoir(message)) return false;
            if (DebutJaiEntendu().IsMatch(message)) return message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length <= 7;
            return MotsDUneReponseCourte(message) is { } mots && !LectureDImage.EstUneDemande(mots);
        }

        /// <summary>La réponse attend que l'enfant dise ce qu'il a entendu, à l'oral, sans écrire.</summary>
        public static bool DemandeCeQuIlAEntendu(string reponse) =>
            !EstUneCloture(reponse) && !EcrireCeQuIlEntend().IsMatch(DerniersMots(reponse)) && AttendCeQuIlAEntendu().IsMatch(DerniersMots(reponse));

        /// <summary>
        /// LE MOT À ÉCOUTER NE S'AFFICHE PAS, ET LA CONSIGNE EST DONNÉE. Si la conversation ne l'a jamais portée, le code
        /// la place devant le mot. Les balises placées en pleine phrase laissaient un trou : « tu as bien entendu  ! ».
        /// </summary>
        public static string EcouteDesMots(string texte, IEnumerable<string> recents, string entier, string langue = LanguesDesSons.Francais, string? niveau = null, string? messageDeLEleve = null, string? motGarde = null, string? abandonne = null, string? motDeLImage = null, RedireLeMot redire = RedireLeMot.Non, bool arrivee = false)
        {
            var liste = recents as IReadOnlyList<string> ?? recents.ToList();

            // LE MOT OUBLIÉ DANS LA VOIX (Adam/Omar, #57, 21/11/2026) : Marine refuse « Bleu! » et redemande le mot sans le
            // redire — l'enfant doit répondre sans l'avoir entendu. Quand le mot doit rester, le code le remet (AvecLeMotARedire).
            texte = AvecLeMotARedire(texte, liste, langue, redire, abandonne);

            var propre = DicteeDeMots.MotALaFin(DicteeDeMots.MasquerMotDicte(DicteeDeMots.GarderSeulementLaDerniereVoix(texte, motGarde)));
            var dit = DicteeDeMots.MotDit(propre);

            // APRÈS UN REFUS, LE MÊME MOT — retest KO #57 (Adam, 21/11/2026, 15h05) : « Un! » pour « one » est refusé, puis
            // « Voici le prochain mot. Dog » : l'enfant ne peut jamais redire le bon mot, et « One! » tapé ensuite tombe sur
            // « dog ». Même chose après « Orange! » hors sujet. Le fait « redis-le » (FaitEcoute, RepetitionEnLangue.Fait) ne
            // suffisait pas : c'est le code qui garde le mot. Toujours : le code a lui-même refusé (verdict du micro faux,
            // mot français reconnu). Si refus : rien n'est jugé, le professeur a refusé dans sa phrase sans rien valider.
            // Jamais quand le mot est abandonné (trois essais : EssaisRates) — l'appelant ne le demande pas alors.
            var dernierDicte = DernierMotDicte(liste);
            if (redire != RedireLeMot.Non && dit is not null && dernierDicte is not null
                && SansAccents(dit) != SansAccents(dernierDicte)
                && (abandonne is null || SansAccents(dernierDicte) != SansAccents(abandonne))
                && (redire == RedireLeMot.Toujours || RefusSansValidation(PassageDeVoix().Replace(propre, " "))))
            {
                var nouveau = dit;
                propre = PassageDeVoix().Replace(propre, m => m.Value.Contains(nouveau, StringComparison.OrdinalIgnoreCase)
                    ? m.Value.Replace(nouveau, dernierDicte, StringComparison.OrdinalIgnoreCase)
                    : m.Value);
                propre = PhraseDePassage().Replace(propre, string.Empty);
                // Le mot remis ne s'écrit pas plus que l'autre : au retour, « Cat. Juste ! » citait le mot d'avant en clair.
                propre = DicteeDeMots.MasquerMotDicte(propre);
                dit = DicteeDeMots.MotDit(propre);
            }

            // LA DEMANDE DE CE TOUR COMPTE TOUT DE SUITE — Camara, le 05/11/2026 : « je voudrais faire un exercice sur les
            // mots » et Adrien dicte « fa ». L'historique est lu avant d'enregistrer le message de l'enfant : sans lui, le
            // dernier choix connu était encore « syllabes ».
            var syllabe = EstEcouteDeSyllabe(dit, messageDeLEleve is null ? [entier, .. liste] : [entier, .. liste, messageDeLEleve]);

            string Dicter(string ancien, string nouveau) => PassageDeVoix().Replace(propre, m => m.Value.Contains(ancien, StringComparison.OrdinalIgnoreCase)
                ? m.Value.Replace(ancien, nouveau, StringComparison.OrdinalIgnoreCase)
                : m.Value);

            if (syllabe && dit is not null && !UneSyllabeAuPlus(dit))
            {
                var premiere = PremiereSyllabe(dit);
                propre = Dicter(dit, premiere);
                dit = premiere;
            }

            // DES SYLLABES, SEULEMENT CELLES QUE CAMARA A VALIDÉES : un son refusé ou absent de la banque se dicte autrement.
            if (syllabe && dit is not null && langue == LanguesDesSons.Francais && BanqueDeSons.Nombre > 0 && !BanqueDeSons.Contient(dit))
            {
                var dites = liste.Where(r => r is not null && PassageDeVoix().IsMatch(r)).Select(r => DicteeDeMots.MotDit(r));
                if (BanqueDeSons.SyllabeDisponible($"{dit}|{liste.Count}", dites) is { } autre)
                {
                    propre = Dicter(dit, autre);
                    dit = autre;
                }
            }
            // DES MOTS, CEUX DE SA CLASSE ET DES CLASSES EN DESSOUS — Camara, le 05/11/2026 : un mot hors banque, ou d'une
            // classe au-dessus, se dicte autrement. Hors du primaire, rien ne change.
            var rang = BanqueDeSons.RangDeClasse(niveau);
            if (!syllabe && dit is not null && langue == LanguesDesSons.Francais && rang > 0 && BanqueDeSons.NombreDeMots > 0
                && !BanqueDeSons.EstUnMotPourLaClasse(dit, rang))
            {
                var dits = liste.Where(r => r is not null && PassageDeVoix().IsMatch(r)).Select(r => DicteeDeMots.MotDit(r));
                if (BanqueDeSons.MotDisponible(rang, $"{dit}|{liste.Count}", dits) is { } autre)
                {
                    propre = Dicter(dit, autre);
                    dit = autre;
                }
            }

            // TROIS ESSAIS, PAS PLUS — Camara, le 07/11/2026 : « au bout de 3 essais il faut passer à la suivante ; faut pas
            // laisser l'enfant essayer à l'infini, il va finir frustré ». Le professeur le sait (EssaisRates.Fait) ; s'il
            // redemande quand même le même son, le code en dicte un autre.
            if (abandonne is not null && dit is not null && SansAccents(dit) == SansAccents(abandonne))
            {
                var dejaDits = liste.Where(r => r is not null && PassageDeVoix().IsMatch(r)).Select(r => DicteeDeMots.MotDit(r)).ToList();
                string? autre;
                if (langue == LanguesDesSons.Anglais)
                {
                    var choix = SonsAnglaisIllustres.DeLaClasse(niveau).Select(x => x.Mot.ToLowerInvariant())
                        .Where(m => m is not "thumb" and not "knee" && !string.Equals(m, abandonne, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (choix.Count == 0) choix = ["cat", "dog", "sun", "ship", "fish", "moon"];
                    var neufs = choix.Where(m => !dejaDits.Any(d => string.Equals(d, m, StringComparison.OrdinalIgnoreCase))).ToList();
                    var pool = neufs.Count > 0 ? neufs : choix;
                    autre = pool[liste.Count % pool.Count];
                }
                else if (syllabe)
                {
                    autre = BanqueDeSons.Nombre > 0 ? BanqueDeSons.SyllabeDisponible($"{dit}|abandon|{liste.Count}", dejaDits.Append(dit)) : null;
                }
                else
                {
                    autre = rang > 0 && BanqueDeSons.NombreDeMots > 0 ? BanqueDeSons.MotDisponible(rang, $"{dit}|abandon|{liste.Count}", dejaDits.Append(dit)) : null;
                }

                if (autre is not null && SansAccents(autre) != SansAccents(dit))
                {
                    propre = Dicter(dit, autre);
                    dit = autre;
                }
            }

            // PLUS DE CONSIGNE « TU DIRAS : J'AI ENTENDU… » — Camara, le 05/11/2026 : « ça ne sert plus à rien ». L'enfant
            // dit simplement ce qu'il a entendu ; ReponseDEcoute lit une réponse d'un ou deux mots. Celle qu'Adrien
            // écrirait encore par habitude est retirée.
            var annonceDuType = syllabe ? AnnonceDeSyllabe() : AnnonceDeMot();
            var consigneDuType = syllabe ? ConsigneDeLaSyllabe() : ConsigneDeLEcoute();
            // « VOICI LE PREMIER MOT » APRÈS TWO (Adam, #57, 21/11/2026) : Two avait été dit dans ce fil sans l'annonce du
            // code (répétition, « Tu me dis blue ? »…), et le mot suivant passait pour le premier. Un mot déjà dit à voix
            // haute dans l'exercice en cours suffit : le suivant est « le prochain ».
            var premiereDuType = DernierMotDicte(liste) is null
                && !liste.Any(r => r is not null && (annonceDuType.IsMatch(r) || consigneDuType.IsMatch(r)));

            // L'ORDRE D'UN TOUR D'ÉCOUTE — Camara, le 05/11/2026, capture à l'appui : « Écoute bien celle-ci. Tu peux me
            // dire ce que tu entends ? Tu diras : ... », et le son après. Ce qu'il a à dire, puis le son, et rien entre
            // les deux. « Écoute bien » s'en va : la voix le dit déjà juste devant le son. La question aussi, quand la
            // consigne la remplace : le verdict reconnaît cette consigne.
            var voix = PassageDeVoix().Matches(propre).LastOrDefault();
            if (voix is null) return LignesVides().Replace(PhraseConsigneDeReponse().Replace(propre, string.Empty), "\n\n").Trim();

            var corps = PhraseEcouteBien().Replace(propre[..voix.Index], string.Empty);
            corps = PhraseQuestionEcoute().Replace(corps, string.Empty);
            corps = RediteDeLAnnonce().Replace(corps, "$1");
            corps = PhraseConsigneDeReponse().Replace(corps, string.Empty);

            // L'ANNONCE EST CELLE DU CODE — c'est elle qui dit à l'enfant, et aux tours suivants, s'il écoute une syllabe ou
            // un mot. « Voici le prochain mot » écrit par Adrien sur une syllabe (05/11/2026, 20h49) se retire.
            corps = PhraseDAnnonce().Replace(corps, string.Empty);

            // CE QU'IL DOIT DIRE NE PASSE PAS AVANT LE SON (e2e #57, 21/11/2026) : « À toi de le dire à voix haute ! Voici le
            // prochain mot. » — on lui demandait de dire un mot qu'il n'avait pas encore entendu. La question du code suit
            // le son. Même chose pour l'annonce libre « Je te redis le mot. », doublée par celle du code.
            corps = PhraseADireAvantLeSon().Replace(corps, string.Empty);
            corps = AnnonceLibre().Replace(corps, string.Empty);

            // AU RETOUR, RIEN N'EST JUGÉ (e2e, 22/11/2026) : l'enfant revient sans avoir répondu, et Marine écrivait « Cat.
            // Juste ! C'est bien cat. » ou « Pas tout à fait ! » — un verdict sur une réponse qui n'existe pas.
            if (arrivee) corps = PhraseQuiJuge().Replace(corps, string.Empty);

            // L'ANNONCE AVANT LE SON — Camara, le 05/11/2026 : « il faut qu'il prévienne avant de dire la syllabe ou le mot ».
            // Le même son redit après une erreur n'est pas « le prochain » : « Je te le redis. »
            string? precedent = null;
            foreach (var r in liste.Reverse())
            {
                if (r is null || !PassageDeVoix().IsMatch(r)) continue;
                precedent = DicteeDeMots.MotDit(r);
                if (precedent is not null) break;
            }

            var meme = dit is not null && precedent is not null && SansAccents(dit) == SansAccents(precedent);
            // « Pas de souci, je te le répète encore une fois. » vaut l'annonce : pas de « Je te le redis. » en plus.
            var dejaRedit = meme && (AnnonceDuSon().IsMatch(corps) || RepeteDansLaPhrase().IsMatch(corps));
            var annonce = dejaRedit ? null
                : meme ? (syllabe ? "Je te la redis." : "Je te le redis.")
                : premiereDuType ? (syllabe ? "Voici la première syllabe." : "Voici le premier mot.")
                : syllabe ? "Voici la prochaine syllabe." : "Voici le prochain mot.";

            var morceaux = new List<string>();
            // « « Sin », comme dans singe ! » : la syllabe seule se lit mal hors de sa balise (voir ImageAuTableau.SansSyllabeCitee).
            // « Comme dans… » : le mot de l'image ou rien, en français (en anglais, « le a de cat » annonce le son).
            // EN ANGLAIS, UN MOT FRANÇAIS CITÉ RESTE (retest KO #57, 21/11/2026) : « Tu m'as dit le mot en français, « un » »
            // devenait « …en français, cette syllabe » (« un » est une syllabe de la méthode de lecture). Ce masque protège la
            // voix d'une syllabe isolée du cours de FRANÇAIS ; en anglais, le mot cité est la traduction, il se lit bien (seule
            // la citation devant « comme dans » part encore).
            var relu = ImageAuTableau.SansSyllabeCitee(LignesVides().Replace(corps, "\n\n").Trim(), seulementCommeDans: langue != LanguesDesSons.Francais);
            if (langue == LanguesDesSons.Francais) relu = ImageAuTableau.CommeDans(relu, motDeLImage);
            if (relu is { Length: > 0 } reste) morceaux.Add(reste);
            if (annonce is not null) morceaux.Add(annonce);
            morceaux.Add(voix.Value);

            // LA QUESTION APRÈS LE SON — Camara, le 05/11/2026 : « après avoir fait écouter l'audio, le professeur doit lui
            // dire "Qu'as-tu entendu ?" ». Toujours la même, écrite par le code, dans sa propre phrase : la voix lit le son
            // seul, puis la question (voir viderAvantBorne côté navigateur).
            morceaux.Add(QuestionApresLeSon);

            return string.Join("\n\n", morceaux);
        }

        [GeneratedRegex(@"\bvoici\s+(?:la\s+(?:prochaine|première|premiere|nouvelle)\s+syllabe|le\s+(?:prochain|premier|nouveau)\s+mot)\b|\bje\s+te\s+l[ae]\s+redis\b", RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceDuSon();

        // « Pas de souci, je te la redis. Je te la redis. » : Adrien imite l'annonce du tour d'avant (message 9698, 05/11/2026).
        [GeneratedRegex(@"(\bje\s+te\s+l[ae]\s+redis\b[^.!?\n]*[.!?])\s*je\s+te\s+l[ae]\s+redis\s*[.!?]", RegexOptions.IgnoreCase)]
        private static partial Regex RediteDeLAnnonce();

        public const string QuestionApresLeSon = "Qu'as-tu entendu ?";

        [GeneratedRegex(@"\bje\s+te\s+l[ae]\s+(?:répète|repete)\b", RegexOptions.IgnoreCase)]
        private static partial Regex RepeteDansLaPhrase();

        /// <summary>« À toi de le dire à voix haute ! », « À toi de me dire le mot ! », « À toi de le redire maintenant ! » : la phrase entière.</summary>
        [GeneratedRegex(
            @"(?<=^|[.!?…\n])[^\S\n]*(?:(?:et|alors|mais),?\s+)?(?:maintenant,?\s+)?(?:"
            + @"à\s+toi\s+(?:de\s+(?:me\s+)?(?:l[ea]\s+|l['’])(?:re)?(?:dire|répéter|repeter)|de\s+me\s+(?:re)?dire\s+(?:le|ce)\s+mot)\b[^.!?…\n]*[.!?…]*"
            // « Tu me le redis ? », « Tu peux le répéter comme moi ? », « Répète après moi, comme moi. » (e2e du 22/11/2026).
            + @"|tu\s+(?:me\s+)?l[ea]\s+(?:re)?dis\b[^.!?…\n]*\?+"
            + @"|tu\s+peux\s+(?:me\s+)?l[ea]\s+(?:re)?(?:dire|répéter|repeter)\b[^.!?…\n]*[.!?…]+"
            + @"|répète(?:-l[ea])?\s+(?:après|comme)\s+moi\b[^.!?…\n]*[.!?…]*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex PhraseADireAvantLeSon();

        /// <summary>« Je te redis le mot. », « Je te répète la syllabe. », « Je te le répète. » : seule dans sa phrase.</summary>
        [GeneratedRegex(
            @"(?<=^|[.!?…\n])[^\S\n]*(?:je\s+te\s+(?:re)?(?:dis|répète|repete)\s+(?:(?:le|ce)\s+mot|(?:la|cette)\s+syllabe)|je\s+te\s+l[ae]\s+(?:répète|repete))(?:\s+encore(?:\s+une\s+fois)?)?[^\S\n]*[.!:…]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex AnnonceLibre();

        /// <summary>Au retour : la phrase qui valide ou refuse une réponse (« Juste ! », « C'est bien cat. », « Pas tout à fait ! », « Bravo »).</summary>
        [GeneratedRegex(
            @"(?<=^|[.!?…\n])[^\S\n]*(?:oui|non)\b[^.!?…\n]*[.!?…]*"
            + @"|(?<=^|[.!?…\n])[^.!?…\n]*(?:\b(?:bravo|juste|exact\w*|parfait\w*|super|génial|excellent|correct\w*|faux|presque|réussi|trouvé|gagné)\b"
            + @"|\bbien\s+(?:entendu|joué|vu|dit|trouvé)\b|\bc['’]est\s+(?:bien|ça|ca)\b|\bpas\s+(?:encore|tout\s+à\s+fait|ça|ca|le\s+bon|la\s+bonne|juste)\b"
            + @"|\bessaie\s+encore\b|\br[ée]essa\w*|\bmauvaise\s+écoute\b|\bbonne\s+écoute\b)[^.!?…\n]*[.!?…]*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex PhraseQuiJuge();

        /// <summary>
        /// LE MOT OUBLIÉ DANS LA VOIX — Adam et Omar, #57 (21/11/2026) : après un refus, Marine redemandait le mot sans le
        /// redire, et l'enfant devait répondre à un mot qu'il n'avait pas entendu à ce tour. Quand le mot doit rester
        /// (<see cref="RedireLeMot.Toujours"/>, ou <see cref="RedireLeMot.SiRefus"/> et une phrase qui refuse sans rien
        /// valider) et que sa réponse ne dit AUCUN mot à voix haute, le mot dicté au tour d'avant est ajouté à la fin, dans
        /// la balise de la langue : <see cref="EcouteDesMots"/> pose ensuite l'annonce et la question. Rien hors d'un tour
        /// d'écoute en attente, rien sur une clôture, une demande d'écrire, un mot abandonné aux trois essais.
        /// </summary>
        public static string AvecLeMotARedire(string texte, IReadOnlyList<string> recents, string langue, RedireLeMot redire, string? abandonne = null)
        {
            if (redire == RedireLeMot.Non || string.IsNullOrWhiteSpace(texte)) return texte;
            if (DicteeDeMots.MotDit(texte) is not null || EstUneCloture(texte) || DemandeDEcrireCeQuIlEntend(texte)) return texte;
            if (!EcouteEnAttente(recents) || DernierMotDicte(recents) is not { } mot) return texte;
            if (abandonne is not null && string.Equals(SansAccents(mot), SansAccents(abandonne), StringComparison.OrdinalIgnoreCase)) return texte;
            if (redire == RedireLeMot.SiRefus && !RefusSansValidation(PassageDeVoix().Replace(texte, " "))) return texte;

            var etiquette = langue == LanguesDesSons.Anglais ? "EN" : "FR";
            return $"{texte.TrimEnd()}\n\n[{etiquette}]{mot}[/{etiquette}]";
        }

        /// <summary>
        /// Le dernier message du professeur est un tour d'écoute qui attend sa réponse : un mot seul dans sa balise de voix,
        /// puis « Qu'as-tu entendu ? » (ou une autre question d'écoute).
        /// </summary>
        public static bool EcouteEnAttente(IReadOnlyList<string> recents)
        {
            for (var i = recents.Count - 1; i >= 0; i--)
            {
                if (recents[i] is not { } r || !PassageDeVoix().IsMatch(r)) continue;
                return DicteeDeMots.MotDit(r) is not null && !EstUneCloture(r) && !EcrireCeQuIlEntend().IsMatch(DerniersMots(r))
                    && (r.Contains(QuestionApresLeSon, StringComparison.OrdinalIgnoreCase) || AttendCeQuIlAEntendu().IsMatch(DerniersMots(r)));
            }

            return false;
        }

        /// <summary>
        /// LE FAIT DU RETOUR PENDANT L'ÉCOUTE DES MOTS (e2e, 22/11/2026) : au retour, Marine jugeait une réponse qui n'existait
        /// pas (« Juste ! C'est bien cat. », « Pas tout à fait ! ») ou citait le mot (« Ce mot, c'était le mot que je viens
        /// de te dire. », retest d'Adam, 21/11/2026, 19h50). Le mot attendu est redit tel quel à la fin de son message.
        /// </summary>
        public static string FaitRepriseDeLEcoute(string mot) =>
            $"[Fait pour ce tour : il revient dans l'écoute des mots. Il n'a PAS encore répondu au dernier mot que tu lui as dit (« {mot} »). "
            + "Ne juge rien : ni bravo, ni juste, ni faux, ni « pas tout à fait ». N'écris pas ce mot et ne parle pas de lui. "
            + "Dis-lui seulement, en une phrase, que vous reprenez l'écoute. Ce même mot lui sera redit à voix haute à la fin de ton message.]";

        /// <summary>Garder le mot dicté au tour d'avant (retest KO #57) : voir EcouteDesMots.</summary>
        public enum RedireLeMot
        {
            /// <summary>Le professeur dicte ce qu'il veut (réponse juste, abandon, changement d'exercice).</summary>
            Non,

            /// <summary>Rien n'est jugé : le mot reste seulement si sa phrase refuse sans rien valider.</summary>
            SiRefus,

            /// <summary>Le code a refusé (verdict faux, mot français reconnu) : le mot reste.</summary>
            Toujours,
        }

        /// <summary>Le dernier mot dicté dans une balise de voix, du plus récent au plus ancien ; null s'il n'y en a pas.</summary>
        public static string? DernierMotDicte(IReadOnlyList<string> recents)
        {
            for (var i = recents.Count - 1; i >= 0; i--)
            {
                if (recents[i] is not { } r || !PassageDeVoix().IsMatch(r)) continue;
                if (DicteeDeMots.MotDit(r) is { } mot) return mot;
            }

            return null;
        }

        /// <summary>
        /// Sa phrase refuse (« Pas encore tout à fait », « Presque », « c'est le mot en français », « Non, ») et ne valide
        /// rien (« Bravo », « Oui », « Bonne écoute », « c'est ça »). Les deux à la fois : on ne tranche pas, le mot est libre.
        /// </summary>
        public static bool RefusSansValidation(string? texte) =>
            !string.IsNullOrWhiteSpace(texte) && PhraseDeRefus().IsMatch(texte) && !PhraseDeValidation().IsMatch(texte);

        [GeneratedRegex(
            @"(?:^|[.!?…\n]\s*)non\b|\bpas\s+encore\b|\bpresque\b|\bpas\s+tout\s+[àa]\s+fait\b"
            + @"|\bpas\s+(?:vraiment\s+)?(?:le\s+bon|la\s+bonne|ça|ca|celui-?là|ce\s+mot|le\s+mot)\b|\bessaie\s+encore\b|\br[ée]essa(?:ie|yons)\b|\bmauvaise\s+écoute\b"
            + @"|\b(?:mot|réponse)\s+(?:en\s+)?français\b|\ben\s+français\b|\bpas\s+(?:tout\s+)?(?:juste|exact|bon)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex PhraseDeRefus();

        [GeneratedRegex(
            @"\bbravo\b|\bbien\s+entendu\b|\bbonne\s+écoute\b|\bparfait\b|\bsuper\b|\bexactement\b|\bc['’]est\s+(?:ça|ca|juste|exact|gagné)\b"
            + @"|\bgénial\b|\bexcellent\b|\btrès\s+bien\b|\btu\s+as\s+(?:trouvé|raison|réussi)\b|\bbien\s+joué\b|\bwell\s+done\b|\bgreat\b|(?:^|[.!?…\n]\s*)oui\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex PhraseDeValidation();

        /// <summary>« On continue avec un autre mot. », « On passe au mot suivant. » : la phrase entière, quand le mot reste.</summary>
        [GeneratedRegex(
            @"(?<=^|[.!?…\n])[^.!?…\n]*(?:\b(?:un\s+)?(?:autre|nouveau)\s+mot\b|\bmot\s+suivant\b|\bprochain\s+mot\b|\bon\s+(?:continue|passe|change)\b|\bpassons\b)[^.!?…\n]*[.!?…]?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex PhraseDePassage();

        /// <summary>« Tu diras : j'ai entendu la syllabe, puis… » : la phrase entière, où qu'elle soit.</summary>
        [GeneratedRegex(@"[^.!?…\n]*\btu\s+(?:diras|dis|vas\s+dire|peux\s+dire|me\s+diras|me\s+dis)\b[^.!?…\n]*\bj.ai\s+entendu\b[^.!?…\n]*[.!?…]?", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseConsigneDeReponse();

        /// <summary>
        /// Une annonce seule dans sa phrase : « Voici la syllabe. », « Voici le prochain mot : », « Je te la redis. », et la forme
        /// courte « Voici la prochaine. » (Camara, capture du 07/11/2026 : elle restait, et l'annonce du code suivait — en double).
        /// </summary>
        // « Je te le redis en anglais. » : la phrase avec son complément SANS CONTENU (tâche 1600, 21/11/2026, bout en bout) —
        // seul « Je te le redis » partait, et il restait « c'est pas grave !en anglais. ». Le code remet sa propre annonce
        // juste avant le son. Un autre complément (un indice : « …, écoute bien le début ») reste, comme avant.
        // « Voici un autre mot. » et « Je te le dis. » (même tâche) : deux autres annonces, en double avec celle du code.
        // « Voici le mot encore une fois. » (lot groupé, bout en bout QE2 sur c0836ad, conv. de test 1040) : seul « Voici le mot »
        // partait, et il restait « c'est malin !encore une fois. » (collé). Les mêmes compléments sans contenu valent pour
        // « voici le mot » que pour « je te le redis », et l'annonce ne part que SEULE dans sa phrase (ponctuation ou fin de
        // ligne juste après) : sinon la phrase reste entière, jamais un morceau collé au « ! » d'avant.
        [GeneratedRegex(@"(?<=^|[.!?…\n]\s*)\s*(?:voici\s+(?:la|le)\s+(?:(?:prochaine|première|premiere|nouvelle|prochain|premier|nouveau)(?:\s+(?:syllabe|mot)|(?=\s*(?:[.!:…\n,]|$)))|syllabe|mot)|voici\s+une?\s+(?:autre|nouveau|nouvelle)\s+(?:syllabe|mot)|je\s+te\s+l[ae]\s+dis|je\s+te\s+l[ae]\s+redis)(?:\s*,?\s+(?:en\s+(?:anglais|français|francais|espagnol|allemand|italien)|encore(?:\s+une\s+fois)?|une\s+(?:autre|dernière|derniere|nouvelle)\s+fois|une\s+fois\s+de\s+plus|tout\s+de\s+suite|maintenant|lentement|doucement|bien\s+lentement|pour\s+toi))*[^\S\n]*(?:[.!:…]+|(?=\n|$))", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseDAnnonce();

        [GeneratedRegex(@"\bvoici\s+la\s+(?:(?:prochaine|première|premiere|nouvelle)\s+)?syllabe\b|\bje\s+te\s+la\s+redis\b", RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceDeSyllabe();

        [GeneratedRegex(@"\bvoici\s+le\s+(?:(?:prochain|premier|nouveau)\s+)?mot\b|\bje\s+te\s+le\s+redis\b", RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceDeMot();

        [GeneratedRegex(@"[^.!?…\n]*\b[ÉEée]coute\s+(?:bien|encore|attentivement|cette|ce|celle|celui)\b[^.!?…:\n]*[.!?…:]?", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseEcouteBien();

        [GeneratedRegex(@"[^.!?…\n]*(?:\bqu.as-tu\s+entendu\w*|\b(?:qu.est-ce\s+que\s+tu|ce\s+que\s+tu)\s+(?:as\s+)?entend\w*)[^.!?…\n]*[.!?…]?", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseQuestionEcoute();

        [GeneratedRegex(@"\s*\n\s*\n\s*")]
        private static partial Regex LignesVides();

        /// <summary>Le fait que l'écran joint au message de l'enfant : voir supportSon.js.</summary>
        public const string MarqueurSonAuCahier = "[SON AU CAHIER";

        public const string MarqueurSonAuClavier = "[SON AU CLAVIER";

        [GeneratedRegex(@"\[SUPPORT_(?:EVALUATION|ECRIT)\]", RegexOptions.IgnoreCase)]
        private static partial Regex AutreBaliseDeSupport();

        [GeneratedRegex(@"\b(?:évaluation|evaluation|contrôle|controle|interro\w*|rédaction|redaction|ton\s+texte|un\s+texte)\b", RegexOptions.IgnoreCase)]
        private static partial Regex ParleDUnDevoir();

        /// <summary>
        /// LA MAUVAISE CARTE — essai du 05/11/2026 : « tu préfères écrire les sons sur ton cahier ou au
        /// clavier ? [SUPPORT_EVALUATION] » une fois sur six. Dans cet exercice, elle ouvrirait la
        /// fenêtre d'un devoir. Jamais si la réponse ou l'élève parle vraiment d'un devoir ou d'un texte.
        /// </summary>
        public static string BonneBaliseDeSupport(string reponse, string texte, string? messageEleve) =>
            AutreBaliseDeSupport().IsMatch(texte)
                && !ParleDUnDevoir().IsMatch(reponse)
                && !ParleDUnDevoir().IsMatch(messageEleve ?? string.Empty)
                ? AutreBaliseDeSupport().Replace(texte, BaliseSupportSon)
                : texte;

        [GeneratedRegex(@"\s*\[COMPREHENSION_ORALE\](?<corps>.*?)\[/COMPREHENSION_ORALE\]\s*", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex BlocEcoute();

        [GeneratedRegex(@"passage\s*:\s*(?<passage>.*?)\s*reponse\s*:", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex PassageDuBloc();

        [GeneratedRegex(@"\[(?:FR|EN)\].*?\[/(?:FR|EN)\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex PassageDeVoix();

        /// <summary>
        /// LE BLOC QUI N'ARCHIVE RIEN — relevé 4 fois sur 56 messages, puis encore le 05/11/2026 : « passage :
        /// cha, comprehension : sans objet » après un seul mot. Il rouvrait les consignes d'écoute au
        /// tour suivant, s'enregistrait comme un faux exercice, et fermait la fenêtre de choix. Un vrai
        /// passage, plus de trois mots et un vrai résultat, reste.
        /// </summary>
        public static string RetirerEcouteParasite(string texte) =>
            BlocEcoute().Replace(texte, m =>
            {
                var corps = m.Groups["corps"].Value;
                var passage = PassageDuBloc().Match(corps).Groups["passage"].Value;
                var parasite = corps.Contains("sans objet", StringComparison.OrdinalIgnoreCase)
                    || passage.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length <= 3;
                return parasite ? "\n\n" : m.Value;
            });

        /// <summary>Rien ne se dit avant le choix : il ne saurait pas encore où l'écrire.</summary>
        public static string SansVoix(string texte) => PassageDeVoix().Replace(texte, string.Empty);

        /// <summary>
        /// LA QUESTION A-T-ELLE ÉTÉ POSÉE DANS CETTE SÉANCE ? Le choix d'une séance passée ne vaut pas pour
        /// celle-ci : c'est ce qui l'a fait disparaître le 05/11/2026. On remonte jusqu'à la fin de séance.
        /// </summary>
        public static bool SupportSonConnu(string? messageEleve, IEnumerable<string?> historique)
        {
            bool Porte(string? c) => c is not null
                && (c.Contains(MarqueurSonAuCahier, StringComparison.Ordinal) || c.Contains(MarqueurSonAuClavier, StringComparison.Ordinal));

            if (Porte(messageEleve)) return true;

            foreach (var contenu in historique.Reverse())
            {
                if (contenu is null) continue;
                if (contenu.Contains("[FIN_SEANCE]", StringComparison.Ordinal) || contenu.Contains("[RAPPORT]", StringComparison.Ordinal)) return false;
                if (Porte(contenu) || contenu.Contains(BaliseSupportSon, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        public static bool DemandeDEcrireCeQuIlEntend(string reponse) =>
            !EstUneCloture(reponse) && EcrireCeQuIlEntend().IsMatch(DerniersMots(reponse));

        /// <summary>
        /// LA RÉPONSE NE S'AFFICHE PAS AVANT LA SIENNE — signalement du 04/11/2026, Camara le 05/11 :
        /// « l'enfant peut simplement lire la réponse », « le professeur ne doit pas écrire au tableau,
        /// c'est interdit ». Ce qu'il écrit au tableau ou entre guillemets passe dans la balise de voix :
        /// dit, jamais affiché. L'enfant l'entend donc toujours. Un long texte au tableau reste : c'est
        /// une consigne, pas la réponse.
        /// </summary>
        public static string CacherLaReponse(string texte, string langue, bool enEcriture = false)
        {
            var balise = langue == LanguesDesSons.Anglais ? BaliseAnglaise() : BaliseFrancaise();
            var etiquette = langue == LanguesDesSons.Anglais ? "EN" : "FR";

            var dits = balise.Matches(texte).Select(m => m.Groups[1].Value.Trim().Trim('.', '!', '?', ',').ToLowerInvariant()).ToHashSet();

            string EnVoix(string mot)
            {
                var propre = mot.Trim().Trim('.', '!', '?', ',', ';', ':');
                return propre.Length == 0 || !dits.Add(propre.ToLowerInvariant()) ? string.Empty : $"[{etiquette}]{propre}[/{etiquette}]";
            }

            // UN MOT ANNONCÉ EN CLAIR PASSE EN VOIX AUSSI — Camara, le 05/11/2026 : « le mot écrit en clair ne
            // peut pas s'afficher ». Sauf au tableau après trois essais, et ce n'est pas ce chemin-là.
            var sansClair = enEcriture || EcrireCeQuIlEntend().IsMatch(DerniersMots(texte))
                ? MotAnnonceEnClair().Replace(texte, m =>
                    PasUnMotDicte.Contains(m.Groups["mot"].Value)
                        ? m.Value
                        : m.Value[..(m.Groups["mot"].Index - m.Index)] + EnVoix(m.Groups["mot"].Value))
                : texte;

            var sansTableau = BlocAuTableau().Replace(sansClair, m =>
            {
                var corps = m.Groups["corps"].Value;
                return corps.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length <= 3 ? EnVoix(corps) : m.Value;
            });

            var sansGuillemets = MotEntreGuillemets().Replace(sansTableau, m => EnVoix(m.Groups["mot"].Value));

            // UNE CONSONNE NUE NE SE DICTE PAS — essai du 05/11/2026 : [FR]ch[/FR] juste après le choix, et
            // la voix a inventé des phrases. « ch » devient « cha », qu'elle sait dire et qu'il sait écrire.
            return balise.Replace(sansGuillemets, m =>
            {
                var corps = m.Groups[1].Value.Trim().Trim('.', '!', '?', ',');
                return corps.Length is > 0 and <= 3 && corps.All(char.IsLetter) && !corps.Any(EstVoyelle)
                    ? $"[{etiquette}]{corps}a[/{etiquette}]"
                    : m.Value;
            });
        }

        private static bool EstVoyelle(char c) => "aeiouyàâäéèêëîïôöùûü".Contains(char.ToLowerInvariant(c));

        /// <summary>Le mot que cette réponse fait dire, s'il est hors liste et que le code doit le remplacer ; null sinon. Le filtre n'existe que dans l'exercice.</summary>
        public static string? MotAHorsListe(string reponse, string langue, IReadOnlyCollection<string> fiables)
        {
            if (fiables.Count == 0 || EstUneCloture(reponse)) return null;
            if (MotQuiPorteUnSens().IsMatch(DerniersMots(reponse))) return null;

            var cible = CibleSansModele(reponse, langue);

            if (cible is null || EstTenu(cible) || CiteCommeMotFrancais(reponse, cible)) return null;
            return fiables.Contains(cible, StringComparer.OrdinalIgnoreCase) ? null : cible;
        }

        /// <summary>
        /// LE MOT FRANÇAIS CITÉ POUR LE RÉEXPLIQUER N'EST PAS UN MOT À FAIRE DIRE (tâche 1600, 21/11/2026, bout en bout) :
        /// « « Bleu », c'est le mot français — on y reviendra » devenait « « Homework », c'est le mot français » (remplacé par un
        /// mot de la liste). Un mot cité dans une phrase qui parle du « mot (en) français » ou du « sens en français » reste.
        /// </summary>
        private static bool CiteCommeMotFrancais(string reponse, string cible) =>
            PhraseDuTexte().Matches(reponse).Any(p => Contient(p.Value, cible) && MotFrancaisCite().IsMatch(p.Value));

        [GeneratedRegex(@"[^.!?…\n]+")]
        private static partial Regex PhraseDuTexte();

        [GeneratedRegex(@"\b(?:mot|sens)\s+(?:en\s+)?français\b|\ben\s+français\b", RegexOptions.IgnoreCase)]
        private static partial Regex MotFrancaisCite();

        /// <summary>Le mot qui le remplace : du même son d'abord, jamais déjà demandé si on peut l'éviter.</summary>
        public static string? MotPermis(string cible, IReadOnlyCollection<string> fiables, IReadOnlyList<string> recents, int graine, string langue = LanguesDesSons.Francais)
        {
            bool DejaDemande(string m) => recents.Any(r => Contient(r, m));
            bool Utilisable(string m) => m != cible && (langue != LanguesDesSons.Francais || !Inreconnaissable(m));

            var memeSon = langue == LanguesDesSons.Francais ? MemeSon(Normaliser(cible), fiables).Where(Utilisable).ToList() : [];

            var neufs = memeSon.Where(m => !DejaDemande(m)).ToList();
            var aSaTaille = cible.Length > 3 ? neufs.Where(m => m.Length > 3).ToList() : neufs;
            if (aSaTaille.Count > 0) return aSaTaille[Math.Abs(graine) % aSaTaille.Count];
            if (neufs.Count > 0) return neufs[Math.Abs(graine) % neufs.Count];

            var ailleurs = fiables.Where(m => Utilisable(m) && m.Length >= 3 && !DejaDemande(m)).ToList();
            if (ailleurs.Count > 0) return ailleurs[Math.Abs(graine) % ailleurs.Count];

            var reste = fiables.Where(Utilisable).ToList();
            return reste.Count > 0 ? reste[Math.Abs(graine) % reste.Count] : null;
        }

        /// <summary>Le mot entier seulement : « foin » ne touche pas « foins » ni « sainfoin ».</summary>
        public static bool Contient(string texte, string mot) =>
            Regex.IsMatch(texte, MotEntier(mot), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        public static string Remplacer(string texte, string ancien, string nouveau) =>
            Regex.Replace(
                texte,
                MotEntier(ancien),
                m => char.IsUpper(m.Value[0]) ? char.ToUpperInvariant(nouveau[0]) + nouveau[1..] : nouveau,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1));

        private static string MotEntier(string mot) => $@"(?<!\p{{L}}){Regex.Escape(mot)}(?!\p{{L}})";

        /// <summary>Du PCM 16 bits mono à 24 kHz, avec l'en-tête que le modèle demande.</summary>
        public static byte[] EnWav(byte[] pcm)
        {
            using var flux = new MemoryStream(44 + pcm.Length);
            using var ecriture = new BinaryWriter(flux);

            ecriture.Write(Encoding.ASCII.GetBytes("RIFF"));
            ecriture.Write(36 + pcm.Length);
            ecriture.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            ecriture.Write(16);
            ecriture.Write((short)1);
            ecriture.Write((short)1);
            ecriture.Write(24000);
            ecriture.Write(OctetsParSeconde);
            ecriture.Write((short)2);
            ecriture.Write((short)16);
            ecriture.Write(Encoding.ASCII.GetBytes("data"));
            ecriture.Write(pcm.Length);
            ecriture.Write(pcm);
            ecriture.Flush();

            return flux.ToArray();
        }

        private static (string consonne, string voyelle) Decouper(string son)
        {
            var sansAccents = SansAccents(son);
            var i = 0;
            while (i < sansAccents.Length && !"aeiouy".Contains(sansAccents[i])) i += 1;

            var consonne = son[..i];
            var reste = son[i..];
            var voyelle = Voyelles.FirstOrDefault(v => reste.StartsWith(v, StringComparison.Ordinal)) ?? reste;

            return (consonne, voyelle);
        }

        private static bool EstTenu(string son) => son.Length >= 2 && SansRepetition(son) is { Length: 1 } or "ch";

        private static bool MemeLettre(string a, string b) => SansRepetition(a) == SansRepetition(b);

        private static string SansRepetition(string son) =>
            new(son.Where((c, i) => i == 0 || c != son[i - 1]).ToArray());

        private static string Lisible(string entendu) => string.IsNullOrEmpty(entendu) ? "aucun son clair" : entendu;

        private static JsonElement? Json(string? reponse)
        {
            if (string.IsNullOrWhiteSpace(reponse)) return null;

            try
            {
                using var document = JsonDocument.Parse(
                    reponse.Replace("```json", string.Empty).Replace("```", string.Empty).Trim());
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Des lettres seulement : ce texte revient d'un modèle et part dans le message d'un
        /// autre. Il n'a aucune raison de porter un crochet ou une consigne.
        /// </summary>
        private static string Propre(JsonElement racine, string champ)
        {
            var valeur = racine.TryGetProperty(champ, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? string.Empty
                : string.Empty;

            return Normaliser(valeur);
        }

        private static string Chaine(JsonElement racine, string champ) =>
            racine.TryGetProperty(champ, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

        private static string Normaliser(string valeur) =>
            new string(valeur.Where(char.IsLetter).Take(20).ToArray()).ToLowerInvariant();

        private static string SansAccents(string texte)
        {
            var sortie = new StringBuilder(texte.Length);

            foreach (var c in texte.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sortie.Append(c);
            }

            return sortie.ToString();
        }
    }

    public interface IVerificationSonService
    {
        /// <summary>
        /// Fait juger le son par le modèle et garde le fait pour le prochain tour du
        /// professeur. Null s'il n'y a rien à retenir, ou en cas de panne : un micro qui
        /// marche passe toujours avant la vérification.
        /// </summary>
        Task<VerdictSon?> VerifierAsync(int conversationId, string paroleDuProfesseur, byte[] pcm, CancellationToken ct, string langue = LanguesDesSons.Francais, bool enExercice = true);

        /// <summary>Le jugement seul, pour le banc des voix de synthèse : ni fait pour le professeur, ni prise gardée.</summary>
        Task<VerdictSon?> JugerAsync(string cible, byte[] pcm, CancellationToken ct, string langue = LanguesDesSons.Francais);

        /// <summary>« Bonne écoute » ou « Mauvaise écoute » : ce qu'il dit avoir entendu, comparé au son au mot dit. Null : pas une réponse, ou panne.</summary>
        Task<VerdictSon?> VerifierEcouteAsync(int conversationId, string paroleDuProfesseur, string? texte, CancellationToken ct, string langue = LanguesDesSons.Francais, string? typeDEcoute = null);

        /// <summary>
        /// LA LECTURE EN IMAGES (07/11/2026) : ce qu'il a lu, comparé à ce que montre l'image, exactement comme l'écoute.
        /// Null : pas une réponse (trop long, vide), ou panne.
        /// `auClavier` (signalement #49, 20/11/2026) : il l'a ÉCRITE. Une panne de la comparaison au son ne laisse pas le tour
        /// sans verdict : l'écriture tranche (la cible est sous ses yeux, il n'y a pas de micro à excuser).
        /// </summary>
        Task<VerdictSon?> VerifierLectureAsync(int conversationId, LectureDImage.Element aLire, string? texte, CancellationToken ct, string langue = LanguesDesSons.Francais, bool auClavier = false);

        /// <summary>Le fait à joindre à ce tour, une seule fois ; null s'il n'y en a pas.</summary>
        string? PrendreFait(int conversationId);

        /// <summary>
        /// Le professeur vient-il de demander un son vérifiable ? La même lecture que la
        /// vérification elle-même (règle, puis modèle texte en cache) : c'est ce qui décide,
        /// dès le premier provisoire, que rien ne s'écrit pendant que l'enfant le dit.
        /// Faux à la moindre panne : le micro reprend alors son cours normal.
        /// </summary>
        Task<bool> DemandeDeSonAsync(string? paroleDuProfesseur, CancellationToken ct, string langue = LanguesDesSons.Francais, bool enExercice = true);

        /// <summary>Le filtre d'une réponse dans un exercice de sons : la liste blanche, et la réponse cachée quand l'enfant doit l'écrire.</summary>
        Task<FiltreMotsVerifies?> FiltrePourAsync(string langue, IReadOnlyList<string> messagesRecents, int graine, CancellationToken ct, bool supportConnu = true, bool photoAttendue = false, EtatDuMot? etat = null, bool retenirTout = false, string? niveau = null, string? messageDeLEleve = null, string? motGarde = null, string? abandonne = null, LectureDImage.Tour? lecture = null, string? poserLeChoix = null, string? motDeLImage = null, RegleVerificationSon.RedireLeMot redire = RegleVerificationSon.RedireLeMot.Non, bool arrivee = false);
    }

    public sealed class VerificationSonService : IVerificationSonService
    {
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";

        /// <summary>L'enfant attend sa réponse : passé ce délai, on garde le texte du micro.</summary>
        private static readonly TimeSpan Delai = TimeSpan.FromSeconds(4);

        /// <summary>Le temps pour le message de partir, délai d'assemblage du CP compris.</summary>
        private static readonly TimeSpan DureeDuFait = TimeSpan.FromSeconds(60);

        /// <summary>La cible lue dans une parole du professeur : la même pour toutes ses redites.</summary>
        private static readonly TimeSpan DureeDeLaCible = TimeSpan.FromMinutes(10);

        /// <summary>La prononciation d'un mot ne change pas : un jour, pour tous les élèves.</summary>
        private static readonly TimeSpan DureeDeLIpa = TimeSpan.FromHours(24);

        private readonly HttpClient _http;
        private readonly OptionsVoix _options;
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _environnement;
        private readonly IFiabiliteDesSons _fiabilite;
        private readonly ILogger<VerificationSonService> _logger;

        public VerificationSonService(
            HttpClient http,
            IOptions<OptionsVoix> options,
            IMemoryCache cache,
            IHostEnvironment environnement,
            IFiabiliteDesSons fiabilite,
            ILogger<VerificationSonService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _environnement = environnement ?? throw new ArgumentNullException(nameof(environnement));
            _fiabilite = fiabilite ?? throw new ArgumentNullException(nameof(fiabilite));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// LES PRISES DE TEST, EN LOCAL SEULEMENT — Camara, le 04/11/2026 : mesurer le jugement
        /// sur de vraies voix avant toute mise en production. Le son d'un tour jugé et son verdict
        /// partent dans un dossier. JAMAIS EN PRODUCTION : il faut l'environnement de développement
        /// ET un dossier configuré. Une panne ici ne gêne pas le cours.
        /// </summary>
        private void GarderPriseDeTest(int conversationId, VerdictSon verdict, byte[] pcm, long ms)
        {
            if (!_environnement.IsDevelopment() || string.IsNullOrWhiteSpace(_options.DossierPrisesDeTest)) return;

            try
            {
                Directory.CreateDirectory(_options.DossierPrisesDeTest);

                var maintenant = DateTime.Now;
                var nom = $"{maintenant:yyyyMMdd-HHmmss-fff}_{verdict.Attendu}_{(verdict.Juste ? "juste" : "refuse")}.wav";

                File.WriteAllBytes(Path.Combine(_options.DossierPrisesDeTest, nom), RegleVerificationSon.EnWav(pcm));
                File.AppendAllText(
                    Path.Combine(_options.DossierPrisesDeTest, "prises.csv"),
                    $"{maintenant:yyyy-MM-dd HH:mm:ss};{conversationId};{verdict.Attendu};{verdict.Entendu};{verdict.Juste};{ms};{pcm.Length / 48};{nom}{Environment.NewLine}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning("Prise de test non gardee : {Cause}.", ex.GetType().Name);
            }
        }

        public async Task<VerdictSon?> VerifierAsync(
            int conversationId, string paroleDuProfesseur, byte[] pcm, CancellationToken ct, string langue = LanguesDesSons.Francais, bool enExercice = true)
        {
            // Le fait du tour d'avant ne doit pas parler de celui-ci.
            _cache.Remove(CleFait(conversationId));

            if (!enExercice || string.IsNullOrWhiteSpace(_options.ApiKey)) return null;

            using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
            delai.CancelAfter(Delai);
            var debut = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var cible = await CibleAsync(paroleDuProfesseur, delai.Token, langue);
                if (cible is null) return null;

                // UN SON N'EST JUGÉ QUE S'IL A ÉTÉ TESTÉ ET TROUVÉ FIABLE (04/11/2026). Sinon l'enfant lit
                // « Bien reçu » et le cours continue ; un son jamais vu part au banc, sans le faire attendre.
                var suite = RegleVerificationSon.SuiteDesSons(cible, await _fiabilite.ListeAsync(langue, delai.Token), langue);

                var fiable = await _fiabilite.LireAsync(cible, delai.Token, langue);

                if (langue == LanguesDesSons.Francais && RegleVerificationSon.Inreconnaissable(cible)) return SansJugement(conversationId, cible, suite);

                if (fiable != true)
                {
                    // L'ANGLAIS EST UNE LISTE FERMÉE : un mot absent ne part pas au banc.
                    if (fiable is null && langue == LanguesDesSons.Francais) _fiabilite.Demander(cible);
                    return SansJugement(conversationId, cible, suite);
                }

                var verdict = await JugerCibleAsync(cible, pcm, conversationId + pcm.Length, delai.Token, langue);
                if (verdict is null) return null;

                // AUCUN CONTENU DANS LE JOURNAL : ni le son attendu, ni ce que l'enfant a dit.
                _logger.LogInformation(
                    "Verification de son (conversation {ConversationId}) : juste={Juste}, {Ms} ms.",
                    conversationId, verdict.Juste, debut.ElapsedMilliseconds);

                GarderPriseDeTest(conversationId, verdict, pcm, debut.ElapsedMilliseconds);

                _cache.Set(CleFait(conversationId), RegleVerificationSon.FaitPourLeProfesseur(verdict, suite), DureeDuFait);
                return verdict;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Verification de son : pas de reponse sous {Delai} s.", Delai.TotalSeconds);
                return null;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
                or InvalidOperationException or IndexOutOfRangeException)
            {
                _logger.LogWarning("Verification de son impossible : {Cause}.", ex.GetType().Name);
                return null;
            }
        }

        public async Task<VerdictSon?> JugerAsync(string cible, byte[] pcm, CancellationToken ct, string langue = LanguesDesSons.Francais)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey)) return null;

            using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
            delai.CancelAfter(TimeSpan.FromSeconds(20));

            try
            {
                return await JugerCibleAsync(cible, pcm, pcm.Length, delai.Token, langue);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return null;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
                or InvalidOperationException or IndexOutOfRangeException)
            {
                _logger.LogWarning("Jugement au banc impossible : {Cause}.", ex.GetType().Name);
                return null;
            }
        }

        private async Task<VerdictSon?> JugerCibleAsync(string cible, byte[] pcm, int graine, CancellationToken ct, string langue = LanguesDesSons.Francais)
        {
                // Un mot se transcrit en phonétique, une syllabe se choisit : voir ConsigneMot.
                var estUnMot = RegleVerificationSon.SeTranscrit(cible, langue);
                var enAnglais = langue == LanguesDesSons.Anglais;
                var liste = estUnMot
                    ? Array.Empty<string>()
                    : RegleVerificationSon.Melanger(RegleVerificationSon.SonsProches(cible), graine);

                // L'IPA de référence du mot part en même temps que l'audio : l'enfant attend.
                var ipaAttendus = estUnMot ? IpaAttendusAsync(cible, ct, langue) : Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

                var reponse = await AppelerAsync(
                    _options.ModeleVerificationSon,
                    enAnglais ? RegleVerificationSon.ConsigneMotAnglais
                        : estUnMot ? RegleVerificationSon.ConsigneMot : RegleVerificationSon.ConsigneChoix(liste),
                    new object[]
                    {
                        new
                        {
                            type = "input_audio",
                            input_audio = new { data = Convert.ToBase64String(RegleVerificationSon.EnWav(pcm)), format = "wav" },
                        },
                    },
                    ct);

                if (reponse is null) return null;

                var references = await ipaAttendus;
                if (estUnMot && references.Count == 0) return null;

                var verdict = estUnMot
                    ? RegleVerificationSon.LireMot(reponse, cible, references, langue)
                    : RegleVerificationSon.LireChoix(reponse, cible, liste);

                // LE MODÈLE A CHOISI UNE AUTRE ÉCRITURE : EST-CE UN AUTRE SON ? Seule la référence le
                // dit. Si elle ne répond pas, le refus reste : le professeur fait redire, sans plus.
                if (!enAnglais && RegleVerificationSon.ACompareAuSon(verdict))
                {
                    var attendus = IpaAttendusAsync(cible, ct);
                    var entendus = IpaAttendusAsync(verdict.Entendu, ct);
                    await Task.WhenAll(attendus, entendus);

                    if (RegleVerificationSon.MemeSon(attendus.Result, entendus.Result))
                    {
                        verdict = new VerdictSon(cible, string.Empty, true);
                    }
                }

                return verdict;
        }

        public async Task<VerdictSon?> VerifierEcouteAsync(int conversationId, string paroleDuProfesseur, string? texte, CancellationToken ct, string langue = LanguesDesSons.Francais, string? typeDEcoute = null)
        {
            _cache.Remove(CleFait(conversationId));

            var mot = DicteeDeMots.MotDit(paroleDuProfesseur);
            var dit = RegleVerificationSon.ReponseDEcoute(texte);
            if (mot is null) return null;

            // « Quoi ? » entendu pour « coi » (07/11/2026) : un mot banal n'est retenu que s'il sonne comme le mot dit.
            if (dit is null)
            {
                dit = RegleVerificationSon.MotBanal(texte);
                if (dit is null || !await MemeSonQueAsync(dit, mot, langue, ct)) return null;
            }

            var juste = RegleVerificationSon.MemeMotEntendu(dit, mot);

            // LE MOT FRANÇAIS N'EST PAS LE MOT ENTENDU, MÊME S'IL SE LIT PAREIL — retest KO #57 (Adam, 21/11/2026, 18h20) :
            // « bleu » dit pour « Blue » était jugé JUSTE au son (la prononciation anglaise de « bleu » est celle de « blue »).
            // Au clavier, il était refusé. Le même juge que le clavier passe donc AVANT le son : voir RepetitionEnLangue.TraductionDite.
            var francais = juste || langue == LanguesDesSons.Francais
                ? null
                : RepetitionEnLangue.TraductionDite(langue, mot, dit, [paroleDuProfesseur]);

            // COMPARÉ AU SON, ENSUITE — la règle de Camara : « riz » écrit « ris » par le micro est une bonne écoute.
            if (!juste && francais is null && !string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
                delai.CancelAfter(Delai);

                try
                {
                    var attendus = IpaAttendusAsync(mot, delai.Token, langue);
                    var entendus = IpaAttendusAsync(dit, delai.Token, langue);
                    await Task.WhenAll(attendus, entendus);
                    // UNE SYLLABE SE PRONONCE PAR SES RÈGLES DE LECTURE (07/11/2026) : voir PrononciationDesSyllabes.
                    juste = RegleVerificationSon.MemeSon(PrononciationDesSyllabes.AvecLesRegles(attendus.Result, mot, langue), entendus.Result);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Comparaison d'une ecoute : pas de reponse sous {Delai} s.", Delai.TotalSeconds);
                    return null;
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
                {
                    _logger.LogWarning("Comparaison d'une ecoute impossible : {Cause}.", ex.GetType().Name);
                    return null;
                }
            }

            var verdict = new VerdictSon(mot, dit, juste, Francais: juste ? null : francais);
            var syllabe = typeDEcoute == RegleVerificationSon.Syllabe
                || (typeDEcoute is null && RegleVerificationSon.EstEcouteDeSyllabe(mot, paroleDuProfesseur, texte));
            _cache.Set(CleFait(conversationId), RegleVerificationSon.FaitEcoute(verdict, syllabe, francais), DureeDuFait);
            if (francais is not null)
            {
                _logger.LogInformation("Ecoute : mot francais reconnu au micro ({Langue}, attendu « {Attendu} », traduction « {Francais} », conversation {ConversationId}).", langue, mot, francais, conversationId);
            }

            return verdict;
        }

        public async Task<VerdictSon?> VerifierLectureAsync(int conversationId, LectureDImage.Element aLire, string? texte, CancellationToken ct, string langue = LanguesDesSons.Francais, bool auClavier = false)
        {
            _cache.Remove(CleFait(conversationId));

            var dit = RegleVerificationSon.ReponseDEcoute(texte);
            if (dit is null)
            {
                // UN MOT OU DEUX, DONT UN « BANAL » — production, le 07/11/2026 : il lit « coi », le micro écrit « Quoi ? »,
                // aucun verdict, rien de compté, bloqué sur la même image bien au-delà de trois essais. Ici, dans la
                // lecture en images, tout ce qui est court est une tentative : le mot utile s'il y en a un ; un mot banal
                // qui SONNE comme la cible est juste (« quoi » pour « coi ») ; sinon c'est un essai raté, compté.
                // Une vraie phrase (« je veux changer d'exercice ») n'est pas une tentative et part au professeur.
                var mots = RegleVerificationSon.MotsDUneReponseCourte(texte);
                if (mots is null) return null;

                var utile = mots.LastOrDefault(m => !RegleVerificationSon.EstBanal(m));
                if (utile is not null) dit = utile;
                else
                {
                    var juste = await MemeSonQueAsync(mots[^1], aLire.Cible, langue, ct);

                    // Au clavier (contrôle d'Omar, signalement #49) : « oui », « stop », « aide » tapés seuls sont une
                    // réaction, pas une lecture déformée par le micro. Juste s'ils sonnent comme la cible, sinon pas de verdict.
                    if (auClavier && !juste) return null;

                    var verdictBanal = new VerdictSon(aLire.Cible, juste ? mots[^1] : string.Join(" ", mots), juste);
                    _cache.Set(CleFait(conversationId), LectureDImage.FaitVerdict(verdictBanal, aLire, auClavier), DureeDuFait);
                    return verdictBanal;
                }
            }

            VerdictSon verdict;

            // LE MOT ENTIER AU LIEU DE LA SYLLABE (« joker » pour « jo ») : ni juste, ni faux, il n'a pas compris la consigne.
            //
            // SAUF S'IL SE DIT COMME LA SYLLABE — responsable du pôle pédagogique, le 08/11/2026 : « joie », « noix » : l'enfant
            // lit juste « joi », le micro écrit « joie », et l'exercice restait bloqué sur cette syllabe. 18 images sont
            // dans ce cas (chat, nid, fée, loup, roue, pont, rond, deux, toit, bras, grue…). Le son d'abord : c'est alors juste.
            if (aLire.MotDeLImage is { } motDeLImage && RegleVerificationSon.MemeMotEntendu(dit, motDeLImage)
                && !await MemeSonQueAsync(dit, aLire.Cible, langue, ct))
            {
                verdict = new VerdictSon(aLire.Cible, dit, false, SansJugement: true);
            }
            else
            {
                var juste = RegleVerificationSon.MemeMotEntendu(dit, aLire.Cible);

                // COMPARÉ AU SON, ENSUITE — comme l'écoute : « beau » écrit par le micro est une bonne lecture de « bo ».
                if (!juste && !string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    delai.CancelAfter(Delai);

                    try
                    {
                        var attendus = IpaAttendusAsync(aLire.Cible, delai.Token, langue);
                        var entendus = IpaAttendusAsync(dit, delai.Token, langue);
                        await Task.WhenAll(attendus, entendus);
                        // « can » lu, « quand » écrit par le micro (10105-10109) : la syllabe se prononce par ses règles.
                        juste = RegleVerificationSon.MemeSon(PrononciationDesSyllabes.AvecLesRegles(attendus.Result, aLire.Cible, langue), entendus.Result);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        _logger.LogWarning("Comparaison d'une lecture : pas de reponse sous {Delai} s.", Delai.TotalSeconds);
                        if (!auClavier) return null;
                    }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
                    {
                        _logger.LogWarning("Comparaison d'une lecture impossible : {Cause}.", ex.GetType().Name);
                        if (!auClavier) return null;
                    }
                }

                verdict = new VerdictSon(aLire.Cible, dit, juste);
            }

            _cache.Set(CleFait(conversationId), LectureDImage.FaitVerdict(verdict, aLire, auClavier), DureeDuFait);
            return verdict;
        }

        /// <summary>Deux mots se prononcent-ils pareil ? L'écriture d'abord, puis le son (règles des syllabes comprises) ; faux en cas de panne.</summary>
        private async Task<bool> MemeSonQueAsync(string dit, string cible, string langue, CancellationToken ct)
        {
            if (RegleVerificationSon.MemeMotEntendu(dit, cible)) return true;
            if (string.IsNullOrWhiteSpace(_options.ApiKey)) return false;

            using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
            delai.CancelAfter(Delai);
            try
            {
                var attendus = IpaAttendusAsync(cible, delai.Token, langue);
                var entendus = IpaAttendusAsync(dit, delai.Token, langue);
                await Task.WhenAll(attendus, entendus);
                return RegleVerificationSon.MemeSon(PrononciationDesSyllabes.AvecLesRegles(attendus.Result, cible, langue), entendus.Result);
            }
            catch (Exception ex) when (ex is OperationCanceledException && !ct.IsCancellationRequested || ex is HttpRequestException or JsonException or InvalidOperationException)
            {
                _logger.LogWarning("Comparaison d'un mot banal impossible : {Cause}.", ex.GetType().Name);
                return false;
            }
        }

        public async Task<FiltreMotsVerifies?> FiltrePourAsync(string langue, IReadOnlyList<string> messagesRecents, int graine, CancellationToken ct, bool supportConnu = true, bool photoAttendue = false, EtatDuMot? etat = null, bool retenirTout = false, string? niveau = null, string? messageDeLEleve = null, string? motGarde = null, string? abandonne = null, LectureDImage.Tour? lecture = null, string? poserLeChoix = null, string? motDeLImage = null, RegleVerificationSon.RedireLeMot redire = RegleVerificationSon.RedireLeMot.Non, bool arrivee = false)
        {
            // INTERRUPTEUR ÉTEINT, LA LISTE NE S'APPLIQUE PAS, MAIS LA RÉPONSE RESTE CACHÉE : le signalement
            // vient de la production, où il l'est. Une liste vide ne remplace aucun mot.
            IReadOnlyList<string> fiables = await _fiabilite.ActifAsync(ct) ? await _fiabilite.ListeAsync(langue, ct) : [];
            return new FiltreMotsVerifies(langue, fiables, messagesRecents, graine, supportConnu, photoAttendue, etat, retenirTout, niveau, messageDeLEleve, motGarde, abandonne, lecture, poserLeChoix, motDeLImage, redire, arrivee);
        }

        public async Task<bool> DemandeDeSonAsync(string? paroleDuProfesseur, CancellationToken ct, string langue = LanguesDesSons.Francais, bool enExercice = true)
        {
            if (!enExercice || string.IsNullOrWhiteSpace(_options.ApiKey)) return false;
            if (!RegleVerificationSon.ProfesseurDemandeUnSon(paroleDuProfesseur, langue)) return false;

            using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
            delai.CancelAfter(Delai);

            try
            {
                return await CibleAsync(paroleDuProfesseur!, delai.Token, langue) is not null;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Lecture de la demande de son impossible : {Cause}.", ex.GetType().Name);
                return false;
            }
        }

        /// <summary>Le professeur reçoit aussi un fait ici : sans lui, il improvisait « tape-moi ce mot ».</summary>
        private VerdictSon SansJugement(int conversationId, string cible, string suite)
        {
            var verdict = new VerdictSon(cible, string.Empty, false, SansJugement: true);
            _cache.Set(CleFait(conversationId), RegleVerificationSon.FaitPourLeProfesseur(verdict, suite), DureeDuFait);
            return verdict;
        }

        public string? PrendreFait(int conversationId)
        {
            if (!_cache.TryGetValue(CleFait(conversationId), out string? fait)) return null;

            _cache.Remove(CleFait(conversationId));
            return fait;
        }

        /// <summary>
        /// Le son demandé : lu sans appel quand la phrase est nette, par le modèle texte
        /// sinon, et gardé le temps des redites. Une réponse « pas de demande » est gardée
        /// aussi : le même message ne fait pas deux appels.
        /// </summary>
        private async Task<string?> CibleAsync(string parole, CancellationToken ct, string langue = LanguesDesSons.Francais)
        {
            var directe = RegleVerificationSon.CibleSansModele(parole, langue);
            if (directe is not null) return directe;

            var cle = $"verification-son:cible:{langue}:{parole.GetHashCode()}";
            if (_cache.TryGetValue(cle, out string? connue)) return string.IsNullOrEmpty(connue) ? null : connue;

            // Un modèle TEXTE : les modèles audio refusent une requête sans audio.
            var reponse = await AppelerAsync(
                _options.ModeleLectureDemande,
                langue == LanguesDesSons.Anglais ? RegleVerificationSon.ConsigneCibleAnglais : RegleVerificationSon.ConsigneCible,
                new object[] { new { type = "text", text = "Teacher's last words: " + RegleVerificationSon.DerniersMots(parole) } },
                ct);

            if (reponse is null) return null;

            var cible = RegleVerificationSon.LireCible(reponse);
            _cache.Set(cle, cible ?? string.Empty, DureeDeLaCible);
            return cible;
        }

        /// <summary>
        /// Les prononciations de référence d'un mot, par deux modèles texte dont on garde
        /// l'union, un jour en cache : les mots qu'on fait lire reviennent. Vide quand aucun
        /// des deux ne répond : on ne juge pas sans référence.
        /// </summary>
        private async Task<IReadOnlySet<string>> IpaAttendusAsync(string mot, CancellationToken ct, string langue = LanguesDesSons.Francais)
        {
            var cle = $"verification-son:ipa:{langue}:{mot}";
            var consigne = langue == LanguesDesSons.Anglais ? RegleVerificationSon.ConsigneIpaAnglais : RegleVerificationSon.ConsigneIpa;
            if (_cache.TryGetValue(cle, out IReadOnlySet<string>? connues) && connues is { Count: > 0 }) return connues;

            var contenu = new object[] { new { type = "text", text = mot } };
            var reponses = await Task.WhenAll(
                AppelerAsync(_options.ModeleLectureDemande, consigne, contenu, ct),
                AppelerAsync(_options.ModeleIpaReference, consigne, contenu, ct));

            var union = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reponse in reponses) union.UnionWith(RegleVerificationSon.LireIpa(reponse, langue));

            if (union.Count > 0) _cache.Set(cle, (IReadOnlySet<string>)union, DureeDeLIpa);
            return union;
        }

        private async Task<string?> AppelerAsync(string modele, string systeme, object[] contenu, CancellationToken ct)
        {
            using var requete = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        model = modele,

                        // La même voix doit donner le même verdict d'une fois sur l'autre.
                        temperature = 0,

                        // Rien à garder côté fournisseur : ce réglage n'est pas le ZDR.
                        store = false,
                        messages = new object[]
                        {
                            new { role = "system", content = systeme },
                            new { role = "user", content = contenu },
                        },
                    }),
                    Encoding.UTF8,
                    "application/json"),
            };
            requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var reponse = await _http.SendAsync(requete, ct);
            var corps = await reponse.Content.ReadAsStringAsync(ct);

            if (!reponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Verification de son : le fournisseur repond {Statut}.", (int)reponse.StatusCode);
                return null;
            }

            using var document = JsonDocument.Parse(corps);
            return document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

        private static string CleFait(int conversationId) => $"verification-son:{conversationId}";
    }
}
