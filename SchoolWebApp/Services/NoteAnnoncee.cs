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
        // #75 v2 (Omar point B.4) : « N virgule M » dit en toutes lettres (« dix-sept virgule cinq sur vingt » = 17,5,
        // « 13 virgule 3 sur 20 » = 13,3) — lu 5 et 3 en v1.
        [GeneratedRegex(
            @"(?<![\p{L}\d/,.])(?<!virgule\s+)(?<nombre>\d{1,2}(?:[.,]\d{1,2})?|dix[- ]sept|dix[- ]huit|dix[- ]neuf|z[ée]ro|une?|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize|vingt)"
            + @"(?:\s+virgule\s+(?<decimale>\d{1,2}|z[ée]ro|une?|deux|trois|quatre|cinq|six|sept|huit|neuf|vingt-cinq|cinquante|soixante-quinze)(?![\p{L}\d]))?"
            + @"(?<demi>\s+et\s+demie?)?\s*(?:points?\s+)?(?<sep>sur\s+|/\s*)(?:20|vingt)(?![\d\p{L}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Mention();

        // Une fraction de calcul (« 3/20 + 1/20 », « = 7/20 ») n'est pas une note : un opérateur juste avant ou juste
        // après une forme « /20 » l'écarte.
        // #75 v2 (Omar point B.1) : ni « : » ni « * » — « Note : 7,5/20 » et « Note : **8,3/20** » sont LA forme réelle
        // du modèle (fEC-2, fEC-3, cBO3-6) et n'étaient jamais lues.
        [GeneratedRegex(@"[+\-−×÷=]\s*$")]
        private static partial Regex OperateurAvant();

        [GeneratedRegex(@"^\s*[+\-−×÷=]")]
        private static partial Regex OperateurApres();

        // #75 v2 (Omar point B.3) : un calcul dit en toutes lettres (« trois sur vingt plus quatre sur vingt ») n'est pas
        // une note, quel que soit le séparateur.
        [GeneratedRegex(@"\b(?:plus|moins|fois|divisée?\s+par|multipliée?\s+par|font|égale?n?t?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MotOperateurAvant();

        [GeneratedRegex(@"^\s*(?:plus|moins|fois|divisée?|multipliée?|égale?n?t?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MotOperateurApres();

        // #75 v2 (Omar point B.3) : une mention qui, dans sa proposition (depuis la dernière ponctuation), parle d'autre
        // chose que la note de cette copie — une note PASSÉE (« La dernière fois tu avais 9 sur 20 », « 17,5/20 au contrôle
        // d'avant »), une note de CLASSE (« ton contrôle de classe : 8 sur 20 »), une fraction, une probabilité, une
        // moyenne. Elle n'est ni « la note dite » ni une note à comparer.
        [GeneratedRegex(@"\b(?:avais|aviez|dernière\s+fois|fois\s+d['’]avant|précédente?|d['’]avant|dernier\s+contrôle|classe|hier"
            + @"|semaine\s+dernière|fraction|probabilité|chances?|proportion|moyenne|pourcentage)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex AutreNote();

        // #75 v2 (Omar point B.3) : la mention qu'on PRÉFÈRE comme note dite — introduite par « tu as (eu) », « ta note
        // (est de) », « tu obtiens », « note : »… juste avant le nombre.
        [GeneratedRegex(@"\b(?:tu\s+as(?:\s+eu)?|ta\s+note(?:\s+(?:est|sera|finale))?(?:\s+de)?|tu\s+obtiens|tu\s+décroches|tu\s+fais|note(?:\s+finale)?\s*:|ça\s+fait|ce\s+qui\s+fait|soit)[\s*_«""]*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Introduite();

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
        /// La note sur 20 que le professeur DIT dans ce message, avant son bloc [EVALUATION]. Le tableau ([ARDOISE])
        /// est écarté : c'est là que s'écrivent les calculs et les fractions. Null s'il n'en dit aucune.
        ///
        /// #75 v2 (Omar point B.3) : ce n'est plus la PREMIÈRE mention « sur 20 » (« La dernière fois tu avais 9 sur 20.
        /// Aujourd'hui, tu as 15 sur 20 » donnait « et pas 9 »). Parmi les notes lues (<see cref="Lectures"/> : calculs,
        /// notes passées, de classe, fractions, probabilités et moyennes écartés), on préfère celle qu'introduit « tu as »,
        /// « ta note », « tu obtiens », « note : » ; à défaut, la première.
        /// </summary>
        public static double? Lire(string? message)
        {
            var lues = Lectures(message);
            if (lues.Count == 0) return null;

            foreach (var (valeur, introduite) in lues)
                if (introduite) return valeur;

            return lues[0].Valeur;
        }

        /// <summary>
        /// Toutes les notes sur 20 lues dans ce que dit le professeur, dans l'ordre, avec « introduite » vrai si la
        /// mention suit « tu as », « ta note », « tu obtiens », « note : »… Écartées : un calcul (opérateur ou « plus »,
        /// « moins », « fois » juste avant ou après), et une mention dont la proposition parle d'autre chose que cette
        /// copie (<see cref="AutreNote"/> : « tu avais », « la dernière fois », « classe », « fraction », « probabilité »,
        /// « moyenne »…).
        /// </summary>
        public static IReadOnlyList<(double Valeur, bool Introduite)> Lectures(string? message)
        {
            var lues = new List<(double, bool)>();
            if (string.IsNullOrWhiteSpace(message)) return lues;

            var parle = Parle(message);
            var finPrecedente = 0;

            foreach (Match m in Mention().Matches(parle))
            {
                var avant = parle[..m.Index];
                var apres = parle[(m.Index + m.Length)..];
                var debutProposition = Math.Max(finPrecedente, avant.LastIndexOfAny(['.', '!', '?', ';', ',', ':', '\n']) + 1);
                var proposition = avant[debutProposition..];
                var finPrecedenteAvant = finPrecedente;
                finPrecedente = m.Index + m.Length;

                if (m.Groups["sep"].Value.StartsWith('/') && (OperateurAvant().IsMatch(avant) || OperateurApres().IsMatch(apres)))
                    continue;
                if (MotOperateurAvant().IsMatch(avant) || MotOperateurApres().IsMatch(apres)) continue;
                if (AutreNote().IsMatch(proposition)) continue;

                // Toute la phrase compte pour une mention qui n'est pas introduite (« Au contrôle de classe, 8 sur 20. »,
                // « Avec 12 et 14 sur 20, ta moyenne est de 13 ») ; seule sa proposition pour « … tu avais 9 sur 20,
                // aujourd'hui tu as 15 sur 20 » ou « tu as 15 sur 20, ta moyenne monte à 14 ».
                var introduite = Introduite().IsMatch(avant[Math.Max(0, avant.Length - 40)..]);
                if (!introduite)
                {
                    var debutPhrase = Math.Max(finPrecedenteAvant, avant.LastIndexOfAny(['.', '!', '?', ';', '\n']) + 1);
                    var finDePhrase = apres.IndexOfAny(['.', '!', '?', ';', '\n']);
                    if (AutreNote().IsMatch(avant[debutPhrase..]) || AutreNote().IsMatch(finDePhrase < 0 ? apres : apres[..finDePhrase]))
                        continue;
                }

                if (Valeur(m.Groups["nombre"].Value) is not double valeur) continue;
                if (m.Groups["decimale"].Success)
                {
                    if (m.Groups["nombre"].Value.Contains(',') || m.Groups["nombre"].Value.Contains('.')) continue;
                    valeur += Decimale(m.Groups["decimale"].Value);
                }
                if (m.Groups["demi"].Success) valeur += 0.5;
                if (valeur is < 0 or > 20) continue;

                lues.Add((valeur, introduite));
            }

            return lues;
        }

        /// <summary>« virgule cinq » = 0,5 ; « virgule 3 » = 0,3 ; « virgule vingt-cinq » = 0,25 ; « virgule 50 » = 0,5.</summary>
        private static double Decimale(string brut)
        {
            var chiffres = brut.Trim().ToLowerInvariant() switch
            {
                "zéro" or "zero" => "0", "un" or "une" => "1", "deux" => "2", "trois" => "3", "quatre" => "4",
                "cinq" or "cinquante" => "5", "six" => "6", "sept" => "7", "huit" => "8", "neuf" => "9",
                "vingt-cinq" => "25", "soixante-quinze" => "75", var autre => autre,
            };
            return double.TryParse("0." + chiffres, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d) ? d : 0;
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

            // #75 v2 (Omar point B.3) : aucune rectification si UNE des notes lues vaut celle de la copie (« La dernière
            // fois 9 sur 20, aujourd'hui 15 sur 20 » pour une copie à 15) — on ne corrige que ce qui est sûrement faux.
            var noteCopie = copie.Value;
            if (Lectures(message).Any(l => Math.Abs(l.Valeur - noteCopie) < Ecart - 1e-9)) return null;

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

            // #75 v2 (Omar point B.5) : une question qui contient elle-même une note (« Tu as 11 sur 20, tu veux qu'on
            // reprenne la question 3 ? ») n'est pas reposée : elle redirait la note fausse juste après la rectification.
            if (Mention().IsMatch(question)) return null;

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

        /// <summary>
        /// Les balises CONNUES de l'application, et elles seules (#75 v2, Omar point B.2) : la v1 effaçait tout
        /// « [MAJUSCULES] », dont la concentration « [A] » de la question reposée (« calcule [A] à t = 20 s ? » devenait
        /// « calcule à t = 20 s ? », 3 fois sur 10 au corpus). Une balise de l'application a un nom à tiret bas
        /// (SUPPORT_EVALUATION, TABLEAU_EFFACE, FIN_SEANCE, LECTURE_IMAGE:…) ou est l'une des balises d'un mot ci-dessous.
        /// </summary>
        [GeneratedRegex(@"\[/?(?:[A-Z]+(?:_[A-Z]+)+|EVALUATION|RAPPORT|FICHE|ARDOISE|DICTEE|FIGURE|FR|EN)(?::[^\]]*)?\]")]
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
