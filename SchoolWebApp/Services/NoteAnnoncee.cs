using System.Globalization;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LA NOTE DITE À L'ÉLÈVE EST CELLE DE SA COPIE — signalement #75 (23/11/2026).
    ///
    /// Relevé en production, chez Yann, à la fin d'une évaluation : « 11 sur 20 » annoncé à l'oral, 15 sur 20 sur la
    /// copie corrigée. Les deux chiffres ne viennent pas du même endroit :
    /// - la copie porte la note RECALCULÉE par l'application à partir des verdicts de chaque question (juste = 1,
    ///   partiel = ½, faux = 0, ramené sur 20 — voir <see cref="LecteurEvaluation"/>, NoteCalculee) ;
    /// - l'oral, lui, est le chiffre que le modèle a choisi en tête de son message, AVANT d'écrire ses verdicts dans
    ///   le bloc [EVALUATION] qui le termine. Rien ne rapprochait les deux.
    ///
    /// Le texte part en flux, la note orale est déjà chez l'élève (écran et voix) quand le bloc arrive : on ne peut pas
    /// la réécrire. On fait donc comme pour les autres faits que le serveur rétablit dans le même tour : une PHRASE
    /// VRAIE, ajoutée à la fin du message (écran, voix et historique), qui donne la note de la copie.
    ///
    /// Portée : GÉNÉRIQUE — le bloc [EVALUATION] et son calcul sont communs à tous les professeurs.
    /// </summary>
    public static partial class NoteAnnoncee
    {
        /// <summary>
        /// En dessous, les deux notes disent la même chose : « 13 sur 20 » pour une copie à 13,3 n'est pas une
        /// contradiction qu'un enfant ou un parent relèverait. À partir d'un demi-point, si.
        /// </summary>
        public const double Ecart = 0.5;

        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        // Le nombre, en chiffres (« 11 », « 12,5 ») ou en lettres (« onze », « dix-sept »), un « et demi » facultatif,
        // un « points » facultatif, puis « sur 20 », « sur vingt » ou « /20 ». Jamais collé à une lettre, un chiffre ou
        // une barre devant (« 3/4 », « x11 ») ; jamais suivi d'un autre chiffre (« /2025 »).
        [GeneratedRegex(
            @"(?<![\p{L}\d/,.])(?<nombre>\d{1,2}(?:[.,]\d{1,2})?|dix[- ]sept|dix[- ]huit|dix[- ]neuf|z[ée]ro|une?|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize|vingt)"
            + @"(?<demi>\s+et\s+demie?)?\s*(?:points?\s+)?(?<sep>sur\s+|/\s*)(?:20|vingt)(?![\d\p{L}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Mention();

        // Une fraction de calcul (« 3/20 + 1/20 », « = 7/20 ») n'est pas une note : un opérateur juste avant ou juste
        // après une forme « /20 » l'écarte.
        [GeneratedRegex(@"[+\-−×*÷=:]\s*$")]
        private static partial Regex OperateurAvant();

        [GeneratedRegex(@"^\s*[+\-−×*÷=]")]
        private static partial Regex OperateurApres();

        [GeneratedRegex(@"\[ARDOISE\].*?(\[/ARDOISE\]|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"\[EVALUATION\]", RegexOptions.IgnoreCase)]
        private static partial Regex OuvertureDuBloc();

        private static readonly Dictionary<string, double> Mots = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zero"] = 0, ["zéro"] = 0, ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4,
            ["cinq"] = 5, ["six"] = 6, ["sept"] = 7, ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11,
            ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14, ["quinze"] = 15, ["seize"] = 16,
            ["dix-sept"] = 17, ["dix-huit"] = 18, ["dix-neuf"] = 19, ["vingt"] = 20,
        };

        /// <summary>
        /// La première note sur 20 que le professeur DIT dans ce message, avant son bloc [EVALUATION] — c'est l'ordre
        /// de la consigne : la note d'abord, seule. Le tableau ([ARDOISE]) est écarté : c'est là que s'écrivent les
        /// calculs et les fractions. Null s'il n'en dit aucune.
        /// </summary>
        public static double? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var parle = Parle(message);

            foreach (Match m in Mention().Matches(parle))
            {
                if (m.Groups["sep"].Value.StartsWith('/')
                    && (OperateurAvant().IsMatch(parle[..m.Index])
                        || OperateurApres().IsMatch(parle[(m.Index + m.Length)..])))
                    continue;

                if (Valeur(m.Groups["nombre"].Value) is not double valeur) continue;
                if (m.Groups["demi"].Success) valeur += 0.5;
                if (valeur is < 0 or > 20) continue;

                return valeur;
            }

            return null;
        }

        /// <summary>
        /// La phrase à ajouter à la fin du message quand la note dite et celle de la copie ne disent pas la même chose ;
        /// null sinon (pas de copie enregistrable, pas de note dite, ou les deux d'accord à moins d'un demi-point).
        /// <paramref name="message"/> est le texte du modèle, bloc compris.
        /// </summary>
        public static string? Rectification(string? message) => Rectification(message, out _, out _);

        public static string? Rectification(string? message, out double? dite, out double? copie)
        {
            dite = null;
            copie = null;

            // Pas de bloc lisible, ou un bloc sans ligne q : rien n'est enregistré (voir EnregistrerEvaluationAsync),
            // il n'y a pas de copie à laquelle se conformer.
            var declaree = LecteurEvaluation.Lire(message);
            if (declaree is null || declaree.Questions.Count == 0) return null;

            copie = declaree.Note;
            dite = Lire(message);
            if (dite is null) return null;

            if (Math.Abs(dite.Value - copie.Value) < Ecart - 1e-9) return null;

            // LA QUESTION RESTE LA DERNIÈRE CHOSE DITE. Relevé dans le message réel du #75 : le professeur rend la note,
            // commence la correction et finit sur une question (« … et l'autre, à ton avis, placée où ? »). La phrase
            // ajoutée APRÈS l'enterrerait : l'élève entendrait une rectification et ne saurait plus qu'on attend sa
            // réponse. On la lui repose donc, telle quelle.
            var question = QuestionEnSuspens(message);

            return Phrase(copie.Value, dite.Value)
                + (question is null ? string.Empty : " Je te repose ma question. " + question);
        }

        /// <summary>
        /// La question sur laquelle finit ce que le professeur DIT (avant le bloc, tableau et balises écartés) ; null si
        /// son texte ne finit pas sur une question. Une question courte (« Et l'autre, placée où ? ») reprend la phrase
        /// d'avant, dans le même paragraphe, pour être comprise seule — jamais si cette phrase contient une note.
        /// </summary>
        public static string? QuestionEnSuspens(string? message)
        {
            var parle = Balise().Replace(Parle(message), " ").TrimEnd();
            if (!parle.EndsWith('?')) return null;

            var fin = parle.Length;
            var debut = DebutDePhrase(parle, fin - 1);
            var question = parle[debut..fin].Trim();
            if (question.Length == 0) return null;

            if (question.Length < 80 && debut > 0 && parle[debut - 1] != '\n')
            {
                var avant = DebutDePhrase(parle, debut - 1);
                var phrase = parle[avant..debut].Trim();
                if (phrase.Length > 0 && !Mention().IsMatch(phrase)) question = phrase + " " + question;
            }

            return question;
        }

        /// <summary>Juste après la fin de la phrase précédente (. ! ? … ou saut de ligne), en remontant depuis <paramref name="avant"/> exclu.</summary>
        private static int DebutDePhrase(string texte, int avant)
        {
            for (var i = avant - 1; i >= 0; i--)
            {
                if (texte[i] is '\n') return i + 1;
                if (texte[i] is '.' or '!' or '?' or '…' && (i + 1 >= texte.Length || char.IsWhiteSpace(texte[i + 1])))
                    return i + 1;
            }

            return 0;
        }

        /// <summary>Ce que le professeur dit avant son bloc [EVALUATION], sans le tableau.</summary>
        private static string Parle(string? message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;

            var ouverture = OuvertureDuBloc().Match(message);
            var parle = ouverture.Success ? message[..ouverture.Index] : message;
            return Ardoise().Replace(parle, " ");
        }

        [GeneratedRegex(@"\[/?[A-Z_]+(?::[^\]]*)?\]")]
        private static partial Regex Balise();

        /// <summary>
        /// Dite par le professeur, à la deuxième personne, sans rien reprocher à personne : il a recompté. Elle donne
        /// les deux chiffres, sinon l'élève ne sait pas lequel croire.
        /// </summary>
        public static string Phrase(double copie, double dite) =>
            $"\n\nPetite rectification : en recomptant tes réponses une par une, ta note est de {Format(copie)} sur 20, "
            + $"et pas {Format(dite)}. C'est cette note-là qui est sur ta copie.";

        /// <summary>Comme sur la copie (« 0.# ») : 15, 13,3, 12,5.</summary>
        public static string Format(double note) => note.ToString("0.#", Fr);

        private static double? Valeur(string brut)
        {
            var propre = brut.Trim().Replace(' ', '-');
            if (Mots.TryGetValue(propre, out var mot)) return mot;

            return double.TryParse(propre.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                out var nombre)
                ? nombre
                : null;
        }
    }
}
