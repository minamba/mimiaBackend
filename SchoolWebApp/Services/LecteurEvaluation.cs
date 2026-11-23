using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une évaluation telle que le professeur l'a déclarée dans son message.</summary>
    /// <param name="CorrectionReportee">
    /// La note est donnée mais la correction orale attend le prochain cours,
    /// faute de temps — voir <c>Evaluation.CorrectionReportee</c>. Faux par
    /// défaut : un bloc qui ne dit rien est une correction faite sur-le-champ.
    /// </param>
    public record EvaluationDeclaree(
        string? Notion,
        double Note,
        string? Remarque,
        string? ARevoir,
        List<QuestionEvaluation> Questions,
        bool CorrectionReportee = false);

    /// <summary>
    /// Extrait le bloc [EVALUATION] d'un message du professeur.
    ///
    /// Tolérant par construction : le bloc est produit par un modèle de langue,
    /// pas par un formulaire. Une clé absente, un accent, un « 11/20 » au lieu
    /// de « 11 », une virgule décimale — tout cela doit passer. Ce qui ne passe
    /// pas, c'est l'absence de note : sans elle il n'y a rien à enregistrer.
    /// </summary>
    public static partial class LecteurEvaluation
    {
        [GeneratedRegex(@"\[EVALUATION\](?<corps>.*?)\[/EVALUATION\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>Null s'il n'y a pas de bloc exploitable.</summary>
        public static EvaluationDeclaree? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            // Sans note, il n'y a rien à enregistrer : une évaluation EST une
            // note. Le rapport de séance, lui, tolère l'absence.
            var note = LecteurBloc.Note(champs, "note");
            if (note is null) return null;

            var questions = Questions(champs);

            // `correction: reportee` — la note est donnée, la reprise des
            // erreurs attend le prochain cours. Tolérant sur la forme
            // (« reportée », « prochain cours », « plus tard »), fermé sur le
            // fond : tout ce qui n'est pas explicitement un report compte comme
            // une correction faite. Un doute ne doit pas rouvrir une copie à
            // chaque cours.
            var correction = LecteurBloc.Normaliser(LecteurBloc.Valeur(champs, "correction") ?? "");
            var reportee = correction.Contains("report")
                || correction.Contains("prochain")
                || correction.Contains("plus tard");

            return new EvaluationDeclaree(
                LecteurBloc.Valeur(champs, "notion"),
                NoteCalculee(questions) ?? note.Value,
                LecteurBloc.Valeur(champs, "remarque"),
                LecteurBloc.Valeur(champs, "a_revoir", "arevoir"),
                questions,
                reportee);
        }

        /// <summary>
        /// LA NOTE VIENT DES VERDICTS, JAMAIS DE CE QU'ÉCRIT LE PROFESSEUR EN
        /// FACE DE `note:`.
        ///
        /// Relevé en production : une copie à deux questions fausses sur six,
        /// « 4 juste, 2 faux » affiché noir sur blanc — notée 20 sur 20. Le
        /// modèle avait écrit une note et des verdicts par question dans le
        /// même bloc, sans que rien ne les rapproche l'un de l'autre ; les
        /// deux se sont contredits, et c'est le chiffre qu'un parent regarde
        /// en premier qui était faux.
        ///
        /// On calcule donc la note à partir des verdicts eux-mêmes — juste
        /// vaut un point, partiel un demi, faux zéro, ramené sur 20 — plutôt
        /// que de faire confiance à une arithmétique que le modèle vient de
        /// démontrer peu fiable. Le professeur continue de DIRE une note à
        /// l'oral ; c'est celle-ci, calculée, qui est enregistrée et qui
        /// s'affiche sur la copie — les deux peuvent différer d'un cran sans
        /// que personne ne l'entende, mais jamais la copie n'affichera une
        /// note incohérente avec ses propres verdicts.
        ///
        /// Null — donc on retombe sur le mot du professeur — dès qu'UN SEUL
        /// verdict manque ou n'est pas reconnu : calculer sur une question
        /// sans verdict compterait une faute qui n'en est pas forcément une.
        /// </summary>
        private static double? NoteCalculee(List<QuestionEvaluation> questions)
        {
            if (questions.Count == 0) return null;

            double poids = 0;
            var comptees = 0;

            foreach (var question in questions)
            {
                double? contribution = question.Verdict switch
                {
                    "juste" => 1.0,
                    "partiel" => 0.5,
                    "faux" => 0.0,
                    _ => null,
                };

                // UNE QUESTION ILLISIBLE NE DOIT PAS EMPORTER LA NOTE ENTIERE.
                //
                // Le calcul renoncait des qu UN seul verdict n etait pas
                // reconnu, et laissait alors la place au chiffre annonce par le
                // professeur. Une evaluation de six questions dont cinq etaient
                // parfaitement lisibles finissait donc notee a l estime. On
                // ecarte desormais la question douteuse et on note sur les
                // autres : mieux vaut une note assise sur cinq reponses
                // verifiees que sur aucune.
                if (contribution is null) continue;

                poids += contribution.Value;
                comptees++;
            }

            // Aucune question exploitable : la, et la seulement, on rend la
            // main au chiffre du professeur.
            if (comptees == 0) return null;

            return Math.Round(20.0 * poids / comptees, 1);
        }

        /// <summary>
        /// Les lignes `q1`, `q2`… converties en questions de copie.
        ///
        /// Une ligne incomplète n'est pas rejetée : une question dont le
        /// professeur a oublié le verdict vaut mieux, sur la copie de l'élève,
        /// qu'une question absente.
        /// </summary>
        private static List<QuestionEvaluation> Questions(Dictionary<string, string> champs)
        {
            var questions = new List<QuestionEvaluation>();

            foreach (var (cle, valeur) in champs)
            {
                if (cle.Length < 2 || cle[0] != 'q') continue;
                if (!int.TryParse(cle[1..], out var numero)) continue;

                var parts = valeur.Split('|');

                questions.Add(new QuestionEvaluation
                {
                    Numero = numero,
                    Enonce = Propre(parts.ElementAtOrDefault(0)),
                    Reponse = Propre(parts.ElementAtOrDefault(1)),
                    Verdict = Verdict(parts.ElementAtOrDefault(2)),
                    Commentaire = Propre(parts.ElementAtOrDefault(3)),
                });
            }

            return questions.OrderBy(q => q.Numero).ToList();
        }

        /// <summary>
        /// Trois verdicts seulement. Tout ce qui n'est pas reconnu devient
        /// null : la copie affichera la question sans pastille plutôt qu'avec
        /// une pastille fausse.
        /// </summary>
        private static string? Verdict(string? brut)
        {
            var propre = Propre(brut)?.ToLowerInvariant();
            if (propre is null) return null;

            // TROIS LISTES LARGES, ET C EST VOLONTAIRE.
            //
            // Le modele n ecrit pas toujours le mot attendu : « ok »,
            // « reussi », « exact », « presque », « incomplet » revenaient
            // souvent. Chacun de ces mots rendait le verdict illisible, la note
            // calculee etait alors abandonnee, et c est le chiffre annonce par
            // le professeur — 20, le plus souvent — qui etait enregistre.
            // Elargir la reconnaissance, c est faire vivre le calcul.
            if (propre.StartsWith("juste") || propre.StartsWith("correct")
                || propre.StartsWith("bon") || propre.StartsWith("ok")
                || propre.StartsWith("exact") || propre.StartsWith("vrai")
                || propre.StartsWith("reussi") || propre.StartsWith("acquis")
                || propre.StartsWith("oui"))
                return "juste";

            if (propre.StartsWith("partiel") || propre.StartsWith("moiti")
                || propre.StartsWith("incomplet") || propre.StartsWith("presque")
                || propre.StartsWith("partielle") || propre.StartsWith("en partie")
                || propre.StartsWith("approximatif") || propre.StartsWith("imprecis"))
                return "partiel";

            if (propre.StartsWith("faux") || propre.StartsWith("incorrect")
                || propre.StartsWith("mauvais") || propre.StartsWith("non")
                || propre.StartsWith("rate") || propre.StartsWith("erreur")
                || propre.StartsWith("pas de reponse") || propre.StartsWith("sans reponse")
                || propre.StartsWith("aucune"))
                return "faux";

            return null;
        }

        private static string? Propre(string? texte) =>
            string.IsNullOrWhiteSpace(texte) ? null : texte.Trim();

        /// <summary>Le marqueur d'ouverture, écrit quand le contrôle commence.</summary>
        public const string Debut = "[DEBUT_EVALUATION]";

        /// <summary>
        /// Le contrôle a été abandonné : l'élève a quitté le cours en plein
        /// milieu.
        ///
        /// Nécessaire parce qu'un contrôle ouvert et jamais refermé restait
        /// ouvert POUR TOUJOURS — l'élève revenait le lendemain et reprenait à
        /// la question trois, sans se souvenir des deux premières, avec une
        /// note qui n'aurait rien voulu dire.
        ///
        /// Distinct de la fermeture du bloc : celle-là annonce une copie rendue
        /// et une note à lire. Ici il n'y a ni copie ni note, seulement un
        /// contrôle à refaire depuis le début.
        /// </summary>
        public const string Abandon = "[EVALUATION_ABANDONNEE]";

        /// <summary>
        /// Un contrôle est-il resté ouvert dans cet historique ?
        ///
        /// On lit les messages du professeur du plus RÉCENT au plus ancien et
        /// on s'arrête au premier verdict : une copie rendue ou un abandon
        /// referment, une ouverture confirme.
        /// </summary>
        /// <summary>
        /// Le dernier contrôle a-t-il été abandonné, sans qu'un nouveau ait été
        /// lancé depuis ?
        ///
        /// Sert à l'accueil du retour : c'est ce qui distingue « bonjour, on
        /// reprend » de « bonjour, ton contrôle est annulé, on le refait ».
        /// </summary>
        public static bool EstAbandonne(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains(Abandon, StringComparison.Ordinal)) return true;

                // Un contrôle relancé, ou une copie rendue depuis : l'abandon
                // est derrière nous, il n'y a plus rien à annoncer.
                if (message.Contains(Debut, StringComparison.Ordinal)) return false;
                if (message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)) return false;
            }

            return false;
        }

        public static bool EstOuvert(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains(Abandon, StringComparison.Ordinal)) return false;

                // La fermeture du bloc signe une copie rendue.
                if (message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)) return false;

                if (message.Contains(Debut, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        /// <summary>
        /// L'évaluation est-elle ENGAGÉE sans être encore ouverte ?
        ///
        /// Signalement #68, deuxième temps (Omar, 23/11/2026) : entre le « oui »
        /// de l'élève et le top départ, il y a deux ou trois tours (choix du
        /// support, consignes, « je suis prêt »). `EstOuvert` reste faux
        /// pendant tout ce temps, et le minuteur redisait « PAS de nouvelle
        /// évaluation » à chacun : le professeur pouvait encore la retirer.
        ///
        /// Engagée = depuis la dernière évaluation rendue, abandonnée,
        /// reportée ou ouverte, le professeur a posé [SUPPORT_EVALUATION], ou
        /// l'élève a répondu au choix du support (le fait joint par l'écran,
        /// « [ÉVALUATION AU CAHIER … » / « [ÉVALUATION À L'ORDINATEUR … »).
        /// On lit TOUT le fil (les deux rôles), du plus récent au plus ancien,
        /// et on s'arrête au premier marqueur. Une ouverture ([DEBUT_EVALUATION])
        /// rend faux : c'est alors `EstOuvert` qui prend le relais.
        ///
        /// #68 v3 (Omar point 3) — L'ENGAGEMENT SE LÈVE AUSSI :
        /// - si l'élève y renonce lui-même : le professeur la reporte avec le
        ///   bloc [EVALUATION_PREVUE] (la sortie balisée, déjà lue ci-dessous) ;
        /// - si la séance se termine ([FIN_SEANCE] ou [RAPPORT]), ou si une
        ///   nouvelle séance a commencé ([TABLEAU_EFFACE], #68 v4) ;
        /// - s'il n'y a pas eu de top départ en <see cref="ToursSansTopDepart"/>
        ///   réponses du professeur après le marqueur : le fil a changé de
        ///   sujet, la consigne « tu la mènes à son terme » n'a plus d'objet.
        ///   Compté en messages (deux par échange), le message de l'élève en
        ///   cours compris : marqueur à 2 × N messages de la fin ou plus = levé.
        /// </summary>
        public static bool EstEngagee(IEnumerable<string?> messagesDuFil)
        {
            var distance = 0;
            foreach (var message in messagesDuFil.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains(Abandon, StringComparison.Ordinal)) return false;
                if (message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)) return false;
                if (message.Contains(Debut, StringComparison.Ordinal)) return false;
                // Reportée au prochain cours (ou l'élève y a renoncé) : l'engagement est levé.
                if (message.Contains("[EVALUATION_PREVUE]", StringComparison.OrdinalIgnoreCase)) return false;
                // Séance terminée.
                if (message.Contains("[FIN_SEANCE]", StringComparison.Ordinal)
                    || message.Contains("[RAPPORT]", StringComparison.OrdinalIgnoreCase)) return false;

                if (message.Contains(BaliseSupport, StringComparison.OrdinalIgnoreCase)
                    || message.Contains("[ÉVALUATION AU CAHIER", StringComparison.Ordinal)
                    || message.Contains("[ÉVALUATION À L’ORDINATEUR", StringComparison.Ordinal)
                    || message.Contains("[ÉVALUATION À L'ORDINATEUR", StringComparison.Ordinal))
                    return distance < 2 * ToursSansTopDepart;

                // Nouvelle séance (#68 v4, Omar point 4) : le premier message
                // d'une reprise commence par [TABLEAU_EFFACE] (voir le builder,
                // `TableauEfface`). Ce qui était engagé avant est derrière nous.
                // Testé APRÈS le support : un choix fait dans ce même message
                // vient après l'effacement, il compte.
                if (message.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal)) return false;

                distance++;
            }

            return false;
        }

        /// <summary>
        /// Combien de réponses du professeur, après le choix du support, sans
        /// top départ, avant que l'évaluation ne compte plus comme engagée
        /// (#68 v3). Le déroulé normal en prend deux (consignes et « prêt ? »,
        /// puis le top départ) ; quatre laisse deux tours de questions sur les
        /// consignes.
        /// </summary>
        public const int ToursSansTopDepart = 4;

        /// <summary>
        /// La première question d'un contrôle, écrite comme telle : « Question 1 : »,
        /// « **Question 1** — », « Question 1. », « Question un. », « Question 1 sur 3 »,
        /// « Question 1 » seule sur sa ligne, « Première question. / , / : »,
        /// « Q1 : », ou une ligne qui commence par « 1. » / « 1) ».
        /// Pas « Question 12 », ni « la question 1 de tout à l'heure », ni « les
        /// questions une par une ».
        ///
        /// #68 v5 (Omar, régression v4 : « …Dès que tu me dis que tu es prêt, je
        /// lance la première question. ») — « Première question » et « Question 1 »
        /// seule ne comptent qu'EN TITRE ou EN DÉBUT DE PHRASE (<see cref="DebutDePhrase"/> :
        /// début de ligne, après . ! ? : ; …, un émoji ou un « Allez, », ou après
        /// « Voici / Voilà la (ta) ») ; jamais comme complément d'un verbe (« je
        /// lance la première question »). Les formes fortes (« Question 1 : »,
        /// « Question 1 sur 3 ») valent partout, SAUF juste après « je lance /
        /// pose / donne / envoie / affiche / écris la (ta, une, cette) » (<see cref="ApresVerbeDeLancement"/>).
        /// </summary>
        [GeneratedRegex(ApresVerbeDeLancement + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b\**(?:\s*[:.)\-–—]|\s+sur\s+(?:\d+|deux|trois|quatre|cinq)\b)"
            + @"|" + DebutDePhrase + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b\**[ \t]*\**[ \t]*\r?$"
            + @"|" + DebutDePhrase + @"\bPremière\s+question\b\**(?:\s*[:.,!\-–—]|[ \t]*\r?$)"
            + @"|\bQ\s?1\s*[:.)\-–—]"
            + @"|^[ \t]*\**1[.)]\**[ \t]+\S",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
        private static partial Regex PremiereQuestion();

        /// <summary>
        /// Ce qui précède un titre ou un début de phrase (#68 v5) : début de ligne,
        /// ponctuation forte, émoji, puces de mise en forme (** ## > _), puis au plus
        /// une interjection suivie d'une virgule (« Allez, », « Bon, », « C'est
        /// parti ! ») et au plus « Voici / Voilà la (ta, ma) » (« Voici ta première
        /// question. », wBO3-5 t3) ou « (Allez) c'est parti pour la » (tEO3-1 t3). Bornes gourmandes, jamais paresseuses : le
        /// générateur .NET ne compile pas une boucle paresseuse dans un lookbehind.
        /// </summary>
        private const string DebutDePhrase =
            @"(?<=(?:^|[.!?…:;]|[\p{So}\p{Cs}])[ \t]*(?:[*_#>]+[ \t]*){0,3}(?:[\p{So}\p{Cs}]+[ \t]*)?"
            + @"(?:(?:allez|alors|bon|bien|ok|okay|parfait|super|très\s+bien|c['’]est\s+parti|go)[ \t]*[,!]+[ \t]*)?"
            + @"(?:(?:allez|alors|bon)[ \t]+)?"
            + @"(?:(?:voici|voilà)[ \t]+(?:la|ta|ma)[ \t]+|c['’]est[ \t]+parti[ \t]+pour[ \t]+(?:la|ta)[ \t]+)?)";

        /// <summary>
        /// Pas juste après un verbe qui ANNONCE la question (#68 v5) : « Tu me fais
        /// signe et je te pose la question 1. » parle de la question à venir.
        /// </summary>
        private const string ApresVerbeDeLancement =
            @"(?<!\b(?:lance|lancer|lancerai|lancerais|lançons|pose|poser|poserai|poserais|posons|donne|donner|donnerai|donnerais|donnons"
            + @"|envoie|envoyer|enverrai|enverrais|affiche|afficher|afficherai|écris|écrire|écrirai)\s+(?:la|ta|ma|une|cette)\s+)";

        /// <summary>
        /// La réponse attend ENCORE le « prêt » de l'élève : c'est un aperçu du
        /// sujet, pas le top départ (#68 v4, Omar point 1 : « Tu es prêt ? »,
        /// « Dis-moi quand tu es prêt à démarrer, et je lance le chrono »).
        ///
        /// #68 v5 (Omar, régression v4) — reconnaît aussi :
        /// - « que tu es prêt » (« Dès que tu me dis que tu es prêt, je lance… »),
        ///   sauf après un verbe de constat (« Je vois que tu es prêt : Question 1 : … »)
        ///   ou « maintenant / vu que » ;
        /// - « quand tu veux » quand il s'agit de COMMENCER (« On commence quand tu
        ///   veux. », « Tu me dis quand tu veux et je lance la première question. ») —
        ///   pas « Réponds quand tu veux » après une question posée ;
        /// - « fais-moi signe » / « tu me fais signe », sauf « si tu bloques, fais-moi
        ///   signe » et « fais-moi signe si… » ;
        /// - le « go » qu'il doit dire (« Tu me dis go ? », gEO3-3 t2 ; « dis-moi
        ///   go », « ton go », « « go » ») — pas le « Go ! » du professeur lui-même.
        /// </summary>
        [GeneratedRegex(@"\bprête?s?\s*(?:à\s+(?:démarrer|commencer|te\s+lancer|y\s+aller|partir)\s*)?\?"
            + @"|\b(?:quand|dès\s+que|lorsque|si|une\s+fois\s+que|aussitôt\s+que)\s+tu\s+(?:es|seras|te\s+sens)\s+prête?s?\b"
            + @"|(?<!\b(?:vois|voit|sais|sens|sent|constate|remarque|comprends|maintenant|vu|sûr|sûre|certain|certaine|content|contente|ravi|ravie|heureux|heureuse)\s+)"
            + @"\bque\s+tu\s+(?:es|seras|te\s+sens)\s+prête?s?\b"
            + @"|\b(?:et|puis)\s+je\s+lance(?:rai)?\s+le\s+chrono"
            + @"|\b(?:on\s+(?:commence|démarre|y\s+va|attaque|lance|peut\s+(?:commencer|démarrer|y\s+aller))|je\s+(?:te\s+)?(?:lance|commence|démarre|pose|donne|envoie)\w*|c['’]est\s+parti)\b"
            + @"[^.!?\n]{0,60}\b(?:quand|dès\s+que|lorsque)\s+tu\s+(?:le\s+)?(?:veux|voudras|souhaites)\b"
            + @"|\b(?:quand|dès\s+que|lorsque)\s+tu\s+(?:le\s+)?(?:veux|voudras|souhaites)\s*[,!.]?\s*(?:(?:et|puis)\s+)?"
            + @"(?:on\s+(?:commence|démarre|y\s+va|attaque|lance)|je\s+(?:te\s+)?(?:lance|commence|démarre|pose|donne|envoie)|c['’]est\s+parti)"
            + @"|(?<!\bsi\s[^.!?\n]{0,40})\b(?:(?:fais|faites)[- ]moi|tu\s+me\s+fais|me\s+faire)\s+(?:un\s+)?signe\b"
            + @"(?!\s*(?:si\b|s['’]il|quand\s+tu\s+(?:as|bloques|ne)|dès\s+que\s+tu\s+(?:as|bloques)|en\s+cas))"
            + @"|\b(?:dis|dites|écris|écrivez|tape|tapez|envoie|réponds)(?:[- ]moi)?\s+[«""“]?\s*go\b"
            + @"|\btu\s+me\s+(?:dis|écris|tapes|envoies)\s+[«""“]?\s*go\b|\bton\s+[«""“]?\s*go\b|[«""“]\s*go\s*!?\s*[»""”]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex AttendLePret();

        /// <summary>
        /// … sauf si elle lui laisse, à la place, répondre tout de suite (« Tu me
        /// dis quand tu es prêt, OU tu réponds directement », wEO3-1 t2) : la
        /// question est posée et il peut y répondre, c'est parti. Le « ou » est
        /// exigé : « tu réponds directement ici » est une consigne (répondre à
        /// l'écran), pas un départ (bEO-2, bEO-5, bSO-7, bTO-2, bTO2-1 t2).
        /// </summary>
        [GeneratedRegex(@"\bou\s+(?:alors\s+)?(?:tu\s+(?:me\s+)?)?réponds(?:-moi)?\s+(?:directement|tout\s+de\s+suite)|\bou\s+(?:alors\s+)?tu\s+peux\s+(?:déjà\s+|directement\s+)?(?:me\s+)?répondre",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex InviteARepondre();

        /// <summary>
        /// LE FILET DU TOP DÉPART — #68 v3, Omar point 2 : 3 parcours sur 45 à
        /// l'ordinateur posaient « Question 1 » sans [DEBUT_EVALUATION]. Pour
        /// l'écran, le contrôle n'existait pas (ni chrono ni dépassement), et
        /// pour le serveur non plus (`EstOuvert` faux : pas de consigne du
        /// contrôle, pas de note). Même patron que le filet de la copie de
        /// contrôle : quand l'évaluation est ENGAGÉE et que la réponse pose la
        /// première question sans la balise, le code l'écrit pour lui.
        ///
        /// #68 v4 (Omar, rejeu sur tout le corpus des bancs v2/v3) :
        /// - pas de filet tant que la réponse attend le « prêt » (aperçu du
        ///   sujet) : sinon le contrôle s'ouvrait, et « Je suis prêt. » était
        ///   noté comme la réponse à la question 1 (10 aperçus sur le corpus) ;
        /// - le texte du bloc [EVALUATION] ne compte pas (« q1: » d'une note,
        ///   vEC-1 t4), et une réponse qui clôt (note, report, abandon, fin de
        ///   séance) n'est jamais un top départ ;
        /// - le motif reconnaît aussi « Question 1 sur 3 », « Première
        ///   question. », « Question 1 » seule sur sa ligne et « 1. … ».
        /// </summary>
        public static bool TopDepartSansBalise(string? reponse)
        {
            if (string.IsNullOrEmpty(reponse) || reponse.Contains(Debut, StringComparison.Ordinal)) return false;

            if (reponse.Contains(Abandon, StringComparison.Ordinal)
                || reponse.Contains("[EVALUATION_PREVUE]", StringComparison.OrdinalIgnoreCase)
                || reponse.Contains("[FIN_SEANCE]", StringComparison.Ordinal)
                || reponse.Contains("[RAPPORT]", StringComparison.OrdinalIgnoreCase)) return false;

            var texte = Bloc().Replace(reponse, string.Empty);

            if (AttendLePret().IsMatch(texte) && !InviteARepondre().IsMatch(texte)) return false;

            return PremiereQuestion().IsMatch(texte);
        }

        /// <summary>La balise du choix du support (deux boutons à l'écran).</summary>
        public const string BaliseSupport = "[SUPPORT_EVALUATION]";

        // ===================== #68 v5 : la note écourtée =====================
        //
        // gEO3-3 (banc v4, conversation 4225) : trois questions annoncées, la
        // première intitulée « Troisième et dernière question » (le modèle a
        // compté les exercices faits juste avant), et la note tombe après cette
        // seule question : 0/20. La consigne dit déjà « toutes les questions
        // annoncées, jamais moins de trois » ; le code la tient maintenant.

        /// <summary>Le plancher d'un contrôle noté (consigne du contrôle ouvert).</summary>
        public const int QuestionsMinimum = 3;

        /// <summary>
        /// « Trois questions », « 3 questions », « Question 1 sur 3 », « Troisième et
        /// dernière question » : le nombre que le professeur a annoncé à l'élève.
        /// </summary>
        [GeneratedRegex(@"\b(?<n>\d{1,2}|deux|trois|quatre|cinq|six)\s+(?:petites\s+|courtes\s+|dernières\s+)?questions\b"
            + @"|\bquestion\s*(?:n°\s*)?(?:\d{1,2}|un|une|deux|trois|quatre|cinq)\s+sur\s+(?<n>\d{1,2}|deux|trois|quatre|cinq|six)\b"
            + @"|\b(?<o>deuxième|troisième|quatrième|cinquième|sixième)\s+et\s+dernière\s+question\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex NombreDeQuestions();

        private static int Valeur(string mot) => mot.ToLowerInvariant() switch
        {
            "deux" or "deuxième" => 2,
            "trois" or "troisième" => 3,
            "quatre" or "quatrième" => 4,
            "cinq" or "cinquième" => 5,
            "six" or "sixième" => 6,
            var chiffres when int.TryParse(chiffres, out var n) => n,
            _ => 0,
        };

        /// <summary>
        /// Le nombre de questions ANNONCÉ pour le contrôle en cours (0 si aucun). On
        /// lit les messages du professeur du plus récent au plus ancien, jusqu'au
        /// choix du support ([SUPPORT_EVALUATION]) inclus ; on s'arrête avant une
        /// évaluation rendue, abandonnée ou reportée, ou une nouvelle séance. Le
        /// plus grand nombre lu l'emporte (borné à 6 : au-delà, c'est un autre sujet).
        /// </summary>
        public static int QuestionsAnnoncees(IEnumerable<string?> messagesProfesseur)
        {
            var annonce = 0;
            var apresDebut = 0;
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;
                if (message.Contains(Abandon, StringComparison.Ordinal)
                    || message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("[EVALUATION_PREVUE]", StringComparison.OrdinalIgnoreCase)) break;

                foreach (Match m in NombreDeQuestions().Matches(Bloc().Replace(message, string.Empty)))
                {
                    var n = Valeur(m.Groups["n"].Success ? m.Groups["n"].Value : m.Groups["o"].Value);
                    if (n is >= 1 and <= 6 && n > annonce) annonce = n;
                }

                if (message.Contains(BaliseSupport, StringComparison.OrdinalIgnoreCase)
                    || message.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal)) break;

                // Sans choix du support balisé : les messages qui précèdent le top
                // départ (consignes, « prêt ? ») comptent encore, pas au-delà.
                if (apresDebut > 0 && ++apresDebut > ToursSansTopDepart + 1) break;
                if (apresDebut == 0 && message.Contains(Debut, StringComparison.Ordinal)) apresDebut = 1;
            }

            return annonce;
        }

        /// <summary>Le nombre de questions qu'une note doit compter : l'annonce, jamais moins de trois.</summary>
        public static int QuestionsRequises(IEnumerable<string?> messagesProfesseur) =>
            Math.Max(QuestionsMinimum, QuestionsAnnoncees(messagesProfesseur));

        /// <summary>
        /// Le début de la phrase que le code ajoute quand il refuse une note écourtée.
        /// Sert aussi à ne refuser qu'UNE fois par contrôle (<see cref="DejaRefusee"/>) :
        /// si le modèle rend encore une note courte après, elle est gardée plutôt que
        /// de tourner en rond.
        /// </summary>
        public const string DebutPhraseNoteRefusee = "Attends, je reprends le compte :";

        /// <summary>Une note écourtée a-t-elle déjà été refusée dans ce contrôle ?</summary>
        public static bool DejaRefusee(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;
                if (message.Contains(DebutPhraseNoteRefusee, StringComparison.Ordinal)) return true;
                if (message.Contains(Debut, StringComparison.Ordinal)
                    || message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)
                    || message.Contains(Abandon, StringComparison.Ordinal)) return false;
            }
            return false;
        }

        private static string EnLettres(int n) => n switch
        {
            1 => "une", 2 => "deux", 3 => "trois", 4 => "quatre", 5 => "cinq", 6 => "six",
            _ => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        /// <summary>
        /// La phrase vraie, ajoutée à la fin de la réponse (écran ET message gardé) quand
        /// la note est refusée. Sans accord de genre (le professeur peut être une
        /// professeure). Au cahier, la copie est déjà là : il la corrige en entier.
        /// </summary>
        public static string PhraseNoteRefusee(int requises, int notees, bool auCahier)
        {
            var compte = notees switch
            {
                0 => "ma note n'en comptait aucune",
                1 => "ma note n'en comptait qu'une",
                _ => $"ma note n'en comptait que {EnLettres(notees)}",
            };
            var suite = auCahier
                ? "je corrige ta copie en entier, question par question, dans mon prochain message."
                : notees == 0 ? "on continue." : $"on continue avec la question {notees + 1}.";
            return $"\n\n{DebutPhraseNoteRefusee} cette évaluation compte {EnLettres(requises)} questions, et {compte}. "
                + $"Cette note ne compte pas : {suite}";
        }

        /// <summary>Le dernier support choisi dans le fil est-il le cahier ?</summary>
        public static bool AuCahier(IEnumerable<string?> messagesDuFil)
        {
            foreach (var message in messagesDuFil.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;
                if (message.Contains("[ÉVALUATION AU CAHIER", StringComparison.Ordinal)) return true;
                if (message.Contains("[ÉVALUATION À L’ORDINATEUR", StringComparison.Ordinal)
                    || message.Contains("[ÉVALUATION À L'ORDINATEUR", StringComparison.Ordinal)) return false;
            }
            return false;
        }

        /// <summary>Retire le bloc [EVALUATION] d'un texte (note refusée : elle ne doit pas être gardée).</summary>
        public static string SansBloc(string texte) => Bloc().Replace(texte, string.Empty);

        /// <summary>
        /// LE GARDE-FOU DE LA NOTE ÉCOURTÉE, DANS LE FLUX (#68 v5, Omar point 2).
        ///
        /// Pendant un contrôle OUVERT, le bloc [EVALUATION] est retenu dès son
        /// ouverture jusqu'à sa fermeture — l'écran ne l'affiche ni ne le lit de
        /// toute façon (school_ia, ardoise.js : retiré dès l'ouverture), donc rien ne
        /// se voit du retard. Fermé, il est lu : s'il compte moins de questions que
        /// <c>requises</c>, il ne part pas (l'écran garde le contrôle ouvert, chrono
        /// compris) et <see cref="Notees"/> dit combien il en comptait ; sinon il part
        /// tel quel. Un bloc jamais fermé part tel quel à la fin.
        /// </summary>
        public sealed class FiltreNoteEcourtee
        {
            private const string Ouverture = "[EVALUATION]";
            private const string Fermeture = "[/EVALUATION]";

            private readonly int _requises;
            private readonly System.Text.StringBuilder _retenu = new();
            private string _attente = string.Empty;
            private bool _dansLeBloc;

            public FiltreNoteEcourtee(int requises) => _requises = requises;

            /// <summary>Le nombre de questions du bloc refusé, ou null si aucun bloc n'a été refusé.</summary>
            public int? Notees { get; private set; }

            /// <summary>Le bloc refusé, tel qu'il a été écrit (pour le rendre si la réponse clôt la séance).</summary>
            public string? BlocRefuse { get; private set; }

            public bool Refusee => Notees is not null;

            public string Passer(string fragment)
            {
                if (string.IsNullOrEmpty(fragment)) return string.Empty;
                var sortie = new System.Text.StringBuilder();
                var texte = _attente + fragment;
                _attente = string.Empty;

                while (texte.Length > 0)
                {
                    if (!_dansLeBloc)
                    {
                        var i = texte.IndexOf(Ouverture, StringComparison.OrdinalIgnoreCase);
                        if (i < 0)
                        {
                            // Garder la fin qui pourrait être le début de la balise.
                            var garde = PrefixeEnFin(texte, Ouverture);
                            sortie.Append(texte, 0, texte.Length - garde);
                            _attente = texte[(texte.Length - garde)..];
                            break;
                        }

                        sortie.Append(texte, 0, i);
                        _dansLeBloc = true;
                        _retenu.Clear();
                        texte = texte[i..];
                    }

                    _retenu.Append(texte);
                    texte = string.Empty;
                    var retenu = _retenu.ToString();
                    var f = retenu.IndexOf(Fermeture, StringComparison.OrdinalIgnoreCase);
                    if (f < 0) break;

                    var bloc = retenu[..(f + Fermeture.Length)];
                    texte = retenu[(f + Fermeture.Length)..];
                    _dansLeBloc = false;
                    _retenu.Clear();

                    var lu = Lire(bloc);
                    if (Notees is null && lu is not null && lu.Questions.Count < _requises)
                    {
                        Notees = lu.Questions.Count;
                        BlocRefuse = bloc;
                    }
                    else
                    {
                        sortie.Append(bloc);
                    }
                }

                return sortie.ToString();
            }

            /// <summary>La fin du flux : ce qui attendait encore (un bloc jamais fermé part tel quel).</summary>
            public string Terminer()
            {
                var reste = _attente + (_dansLeBloc ? _retenu.ToString() : string.Empty);
                _attente = string.Empty;
                _retenu.Clear();
                _dansLeBloc = false;
                return reste;
            }

            private static int PrefixeEnFin(string texte, string balise)
            {
                for (var n = Math.Min(balise.Length - 1, texte.Length); n > 0; n--)
                {
                    if (string.Compare(texte, texte.Length - n, balise, 0, n, StringComparison.OrdinalIgnoreCase) == 0) return n;
                }
                return 0;
            }
        }

        /// <summary>Retire le bloc et le marqueur d'ouverture. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc(), Debut);
    }
}
