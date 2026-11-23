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
        ///
        /// #68 v6 (Omar point 3 : 15 tops départs réels vus par la v4, ratés par la v5) —
        /// « Question 1 : » (deux-points) vaut PARTOUT, verbe de lancement compris
        /// (« Je te pose la question 1 : calcule… », « Je t'affiche la question 1 : ») :
        /// l'énoncé suit, c'est la question posée. Un aperçu qui attend le « prêt »
        /// (« Je lance la question 1 : tu es prêt ? ») reste écarté par <see cref="AttendLePret"/>.
        /// <see cref="DebutDePhrase"/> accepte en plus le prénom en apostrophe (« Très bien,
        /// Adam, première question : »), « Et voici la », « Commençons par la » et
        /// « Allez c'est parti, » sans virgule après « Allez ».
        ///
        /// #68 v7 (Omar, verdict sur 6ef6e24) :
        /// - « Question 1 : » et « (la) première question : » suivis de l'énoncé valent
        ///   partout, AU PRÉSENT seulement : jamais après un futur, un conditionnel ou
        ///   « vais te + infinitif » (<see cref="PasAuFutur"/> : « Je vais te poser la
        ///   question 1 : elle portera sur k. », « Ensuite, je te poserai la question 1 : »),
        ///   ni juste après un mot de discours (<see cref="PasApresMotDeDiscours"/> :
        ///   « Attention, première question : », « Rappel, … »), ni après « pour / de / sur /
        ///   dans / en la » (une correction : « Pour la première question : tu avais… »).
        ///   Rattrape « Je te pose la première question : », « Voici donc la première
        ///   question : », « Pour commencer, première question : », « On y va. La première
        ///   question : ».
        /// - Le prénom en apostrophe de <see cref="DebutDePhrase"/> n'accepte plus les mots
        ///   de discours (« Attention, », « Consigne, »…) et accepte les prénoms composés
        ///   (« Jean-Pierre, »).
        ///
        /// #68 v8 (Omar, régression v7 : « (la) première question : » partout se déclenchait sur
        /// les CORRECTIONS au présent — la faille existait déjà pour « Question 1 : ») :
        /// - ce qui suit les deux-points ne doit pas être un verdict ou une correction
        ///   (<see cref="PasUnVerdict"/> : « juste », « faux », « bien vu », « parfait », « c'est
        ///   juste », « tu avais… », « il manquait… », « elle était… ») ;
        /// - pas après un verbe de reprise (<see cref="PasApresReprise"/> : « Je reprends / corrige /
        ///   relis la », « Revenons à la », « Regardons la », « On reprend la »), ni après « à la »
        ///   en début de phrase (« À la première question : tu as… » ; « Passons à la » reste un
        ///   départ), ni après « ta » (<see cref="PasApresPossessif"/> : « Super, ta première
        ///   question : », sauf « Voici ta », « Je te pose ta ») ;
        /// - un message qui FINIT par une demande de support ou de pause n'est jamais un top
        ///   départ (voir <see cref="TopDepartSansBalise"/>) ;
        /// - « Ensuite, / Puis, question 1 : » suivis d'un énoncé à l'IMPÉRATIF (« calcule k. »)
        ///   sont rattrapés (<see cref="ApresMotDeSuite"/>) : lus vrais en v6, faux en v7.
        /// </summary>
        [GeneratedRegex(PasAuFutur + PasApresMotDeDiscours + PasApresPreposition + PasApresReprise + PasApresPossessif + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b\**[ \t]*:" + PasUnVerdict
            + @"|" + PasAuFutur + PasApresMotDeDiscours + PasApresPreposition + PasApresReprise + PasApresPossessif + @"\bpremière\s+question\b\**[ \t]*:" + PasUnVerdict
            + @"|" + ApresMotDeSuite + @"(?:\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b|\bpremière\s+question\b)\**[ \t]*:[ \t]*\**[ \t]*(?=" + Imperatif + @")"
            + @"|" + ApresVerbeDeLancement + PasAuFutur + PasApresReprise + PasApresPossessif + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b\**(?:\s*[.)\-–—]|\s+sur\s+(?:\d+|deux|trois|quatre|cinq)\b)" + PasUnVerdict
            + @"|" + DebutDePhrase + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b\**[ \t]*\**[ \t]*\r?$"
            + @"|" + DebutDePhrase + @"\bPremière\s+question\b\**(?:\s*[:.,!\-–—]|[ \t]*\r?$)" + PasUnVerdict
            + @"|" + PasAuFutur + PasApresReprise + PasApresPossessif + @"\bQ\s?1\s*[:.)\-–—]" + PasUnVerdict
            + @"|^[ \t]*\**1[.)]\**[ \t]+(?=\S)" + PasUnVerdict,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
        private static partial Regex PremiereQuestion();

        /// <summary>
        /// Ce qui suit « Question 1 : » n'est pas un VERDICT ni une CORRECTION (#68 v8, Omar point A.1) :
        /// « La première question : bien vu. », « Ta première question : juste, la deuxième : à revoir. »,
        /// « Je reprends la première question : tu avais oublié l'unité. », « … : il manquait l'unité. »,
        /// « … : elle était fausse. », « Très bien, Lina, première question : c'est juste. ».
        /// Volontairement étroit : « il manque un mot, lequel ? », « tu as 3 pommes… » restent des énoncés.
        /// </summary>
        private const string PasUnVerdict =
            @"(?![ \t]*\**[ \t]*(?:"
            + @"(?:juste|faux|fausse|exacte?|inexacte?|correcte?|incorrecte?|parfaite?|presque|partielle?|partiellement|incomplète?"
            + @"|réussie?|ratée?|validée?|bravo|bonne\s+réponse|mauvaise\s+réponse|à\s+revoir|à\s+reprendre|à\s+corriger)\b"
            + @"|bien\s+(?:vu|joué|trouvé|répondu|fait)\b"
            + @"|c['’]est\s+(?:juste|faux|bon|bien|correct|exact|parfait|réussi|presque|ça)\b"
            + @"|tu\s+(?:avais|étais|n['’]avais|n['’]as\s+pas|as\s+(?:bien|mal|juste|trouvé|répondu|eu|oublié|écrit|fait|donné|réussi|raté|confondu|mis|calculé|compris|su|perdu))\b"
            + @"|il\s+(?:manquait|fallait|aurait\s+fallu|y\s+avait)\b"
            + @"|elle\s+(?:était|a\s+été|n['’]était)\b"
            // « elle est » seul n'est pas un verdict (« Première question, elle est déjà au tableau », bEC-8 t3, vrai départ) ;
            // suivi d'un jugement, si (« Tu verras, la première question : elle est facile. »).
            + @"|elle\s+(?:est|sera|n['’]est\s+pas)\s+(?:(?:très|plutôt|assez|pas|toute)\s+)?(?:juste|fausse|exacte|correcte|incorrecte|bonne|réussie|ratée"
            + @"|facile|difficile|simple|dure|compliquée|longue|courte)\b"
            + @"|celle-ci\b|ta\s+réponse\b|ton\s+(?:résultat|calcul)\b"
            + @"|[✅❌✔✓✗✘]))";

        /// <summary>
        /// Pas après un verbe de REPRISE (#68 v8) : « Je reprends / Je corrige / Je relis (ta) la première
        /// question : », « Revenons à la question 1 : », « Regardons la première question : », « On
        /// reprend la… » parlent d'une question déjà faite.
        /// </summary>
        private const string PasApresReprise =
            @"(?<!\b(?:reprends|reprend|reprenons|reprendre|corrige|corrigeons|corriger|relis|relit|relisons|relire"
            + @"|regardons|revoyons|revoir|revenons|revenir|reviens|retournons|retourne|retourner|repassons)"
            + @"[ \t]+(?:(?:à|sur)[ \t]+)?(?:(?:ensemble|vite|rapidement|maintenant|d['’]abord)[ \t]+)?(?:la|ta|ma|cette|une)[ \t]+)"
            // « À la première question : tu as bien répondu. » — « à la » en début de phrase ; « Passons à la » reste un départ.
            + @"(?<!(?:^|[.!?…:;,])[ \t]*\**[ \t]*à[ \t]+(?:la|ta)[ \t]+)";

        /// <summary>
        /// Pas après « ta » (#68 v8) : « C'est noté. Ta première question : … », « Super, ta première
        /// question : parfait. », « Je relis ta première question : » — sauf quand « ta » suit un mot de
        /// départ (« Voici ta », « Je te pose ta », « C'est parti pour ta », « Commençons par ta »).
        /// </summary>
        private const string PasApresPossessif =
            @"(?<!(?<!\b(?:voici|voilà|pour|par|avec|pose|donne|envoie|affiche|écris|lance)[ \t]+)\bta[ \t]+)";

        /// <summary>
        /// « Ensuite, question 1 : calcule k. », « Puis, première question : calcule k. » (#68 v8, Omar
        /// point A.2) : un mot de suite, puis un énoncé à l'impératif — c'est un départ. Sans énoncé à
        /// l'impératif (« Ensuite, première question. », « Ensuite, question 1 : tu avais… »), non.
        /// </summary>
        private const string ApresMotDeSuite =
            @"(?<=(?:^|[.!?…:;]|[\p{So}\p{Cs}])[ \t]*\**[ \t]*(?:et[ \t]+)?\b(?:ensuite|puis)[ \t]*,?[ \t]*(?:la[ \t]+)?)" + PasAuFutur;

        /// <summary>Un énoncé qui commence par un verbe de consigne à l'impératif (« calcule », « donne », « écris »…).</summary>
        private const string Imperatif =
            @"(?:calcule|calculez|donne|donnez|écris|écrivez|trouve|trouvez|explique|expliquez|cite|citez|résous|résolvez|complète|complétez"
            + @"|indique|indiquez|dis|dites|conjugue|conjuguez|traduis|traduisez|simplifie|simplifiez|développe|développez|factorise|factorisez"
            + @"|détermine|déterminez|justifie|justifiez|compare|comparez|range|rangez|classe|classez|souligne|soulignez|entoure|entourez"
            + @"|relie|reliez|lis|lisez|recopie|recopiez|propose|proposez|décris|décrivez|nomme|nommez|choisis|choisissez|montre|montrez"
            + @"|démontre|démontrez|convertis|convertissez|place|placez|vérifie|vérifiez|réponds|répondez|exprime|exprimez|rédige|rédigez"
            + @"|associe|associez|accorde|accordez|transforme|transformez|remplace|remplacez|réécris|réécrivez|résume|résumez|identifie"
            + @"|identifiez|précise|précisez|repère|repérez|construis|construisez|trace|tracez|mesure|mesurez|estime|estimez|arrondis|arrondissez)\b";

        /// <summary>
        /// Ce qui précède un titre ou un début de phrase (#68 v5) : début de ligne,
        /// ponctuation forte, émoji, puces de mise en forme (** ## > _), puis au plus
        /// une interjection suivie d'une virgule (« Allez, », « Bon, », « C'est
        /// parti ! ») et au plus « Voici / Voilà la (ta, ma) » (« Voici ta première
        /// question. », wBO3-5 t3) ou « (Allez) c'est parti pour la » (tEO3-1 t3). Bornes gourmandes, jamais paresseuses : le
        /// générateur .NET ne compile pas une boucle paresseuse dans un lookbehind.
        ///
        /// #68 v6 (Omar point 3) : jusqu'à trois interjections, virgule facultative
        /// (« Allez c'est parti, », « Et voici »), puis au plus UN prénom en
        /// apostrophe — un seul mot à majuscule suivi d'une virgule (« Très bien,
        /// Adam, ») —, puis au plus « Voici / Voilà la », « c'est parti pour la » ou
        /// « Commençons / On commence / On attaque par (avec) la ». Ces mots ne
        /// déclenchent rien seuls : il faut encore « Première question » ou
        /// « Question 1 » suivis d'une ponctuation ou d'une fin de ligne.
        ///
        /// #68 v7 (Omar point 2) : le prénom n'est plus « n'importe quel mot à majuscule » —
        /// les mots de discours (<see cref="MotsDeDiscours"/> : « Attention, », « Rappel, »,
        /// « Consigne, », « Ensuite, »…) et de correction (« Bravo, », « Merci, », « Oui, »)
        /// sont exclus ; les prénoms composés passent (« Jean-Pierre, », « Marie-Ève, »).
        /// </summary>
        private const string DebutDePhrase =
            @"(?<=(?:^|[.!?…:;]|[\p{So}\p{Cs}])[ \t]*(?:[*_#>]+[ \t]*){0,3}(?:[\p{So}\p{Cs}]+[ \t]*)?"
            + @"(?:(?:et|allez|alors|bon|bien|donc|ok|okay|parfait|super|très\s+bien|c['’]est\s+parti|go)[ \t]*[,!]*[ \t]*){0,3}"
            + @"(?:(?-i:\p{Lu}\p{Ll}*(?:['’\-]\p{Lu}?\p{Ll}+)*)(?<!\b(?:" + MotsDeDiscours + @"|merci|oui|non|désolée?|pardon|raté|correct|incorrect|perdu))[ \t]*,[ \t]*)?"
            + @"(?:(?:voici|voilà)[ \t]+(?:la|ta|ma)[ \t]+|c['’]est[ \t]+parti[ \t]+pour[ \t]+(?:la|ta)[ \t]+"
            + @"|(?:commençons|démarrons|attaquons|on[ \t]+(?:commence|démarre|attaque))[ \t]+(?:par|avec)[ \t]+(?:la|ta)[ \t]+)?)";

        /// <summary>
        /// Les mots de discours qui ne sont ni un prénom ni un départ (#68 v7, Omar point 2) :
        /// « Attention, première question : … », « Rappel, … », « Consigne, … » annoncent une
        /// remarque, pas le top départ ; « Bravo, », « Juste, », « Faux, » corrigent.
        /// </summary>
        private const string MotsDeDiscours =
            @"attention|rappel|consigne|remarque|ensuite|puis|enfin|note|nb|important|astuce|conseil|indice|correction"
            + @"|rectification|exemple|bravo|exact|juste|faux|presque|dommage|bref|sinon|rappelle-toi|souviens-toi";

        /// <summary>Pas juste après un mot de discours (« Attention, première question : »), article compris.</summary>
        private const string PasApresMotDeDiscours =
            @"(?<!\b(?:" + MotsDeDiscours + @")[ \t]*[,:;!]?[ \t]*(?:(?:la|ta|ma|une|cette)[ \t]+)?)";

        /// <summary>
        /// Pas après « pour / de / sur / dans / en / sauf la » (#68 v7) : « Pour la première
        /// question : tu avais oublié l'unité » parle d'une question passée. « C'est parti pour
        /// la question 1 : » reste un départ.
        /// </summary>
        private const string PasApresPreposition =
            @"(?<!(?:\b(?:de|sur|dans|en|sauf)|(?<!\bparti[ \t]+)\bpour)[ \t]+(?:la|ta|ma|cette)[ \t]+)";

        /// <summary>
        /// Pas après un FUTUR, un CONDITIONNEL ou « aller + infinitif » (#68 v7, Omar point 1) :
        /// « Je vais te poser la question 1 : elle portera sur k. », « Ensuite, je te poserai la
        /// question 1 : », « Dans un instant, je te donnerai la question 1 : », « Tout à l'heure,
        /// je t'enverrai la question 1 : » annoncent la question, ils ne la posent pas — même avec
        /// les deux-points. Futur et conditionnel reconnus à leur terminaison (-rai, -ras, -ra,
        /// -erons/-irons, -erez/-irez, -ront, -rais, -rait, -rions, -riez, -raient), suivis ou non
        /// d'un infinitif (« je voudrais te poser la »), au plus un adverbe (« ensuite »,
        /// « tout de suite »), puis « (à / par / avec) la (ta, ma, une, cette) ». Le présent
        /// (« Je te pose la question 1 : calcule k. ») n'est pas touché.
        /// </summary>
        private const string PasAuFutur =
            @"(?<!\b(?:(?:vais|vas|va|allons|allez|vont|" + Futur + @")[ \t]+(?:(?:te|me|lui|vous|nous|leur)[ \t]+|[tm]['’])?\p{L}+(?:er|ir|re|oir)"
            + @"|" + Futur + @")"
            + @"(?:[ \t]+(?:ensuite|après|bientôt|alors|donc|maintenant|d['’]abord|tout[ \t]+de[ \t]+suite|tout[ \t]+à[ \t]+l['’]heure|plus[ \t]+tard|juste[ \t]+après))?"
            + @"[ \t]+(?:(?:à|par|avec)[ \t]+)?(?:la|ta|ma|une|cette)[ \t]+)";

        /// <summary>Un verbe au futur ou au conditionnel, reconnu à sa terminaison.</summary>
        private const string Futur = @"\p{L}+(?:rai|ras|ra|ront|erons|irons|erez|irez|rais|rait|rions|riez|raient)";

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
            + @"|\b(?:on\s+(?:commence|démarre|y\s+va|attaque|lance|peut\s+(?:commencer|démarrer|y\s+aller))|commençons|démarrons|attaquons|je\s+(?:te\s+)?(?:lance|commence|démarre|pose|donne|envoie)\w*|c['’]est\s+parti)\b"
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

            // #68 v8 (Omar, état armé : « Je corrige la première question : il manquait l'unité. Tu choisis
            // l'écrit ou l'oral ? ») : le support n'est pas encore choisi — la balise ouvrirait le contrôle avant
            // le choix et « à l'oral » serait noté comme la réponse à la question 1.
            if (reponse.Contains(BaliseSupport, StringComparison.OrdinalIgnoreCase)) return false;

            var texte = Bloc().Replace(reponse, string.Empty);

            if (AttendLePret().IsMatch(texte) && !InviteARepondre().IsMatch(texte)) return false;

            if (FinitSurSupportOuPause().IsMatch(texte)) return false;

            return PremiereQuestion().IsMatch(texte);
        }

        /// <summary>
        /// La réponse FINIT par une demande de support ou de pause (#68 v8) : « Tu choisis l'écrit ou l'oral ? »,
        /// « Cahier ou ordinateur ? », « On fait une petite pause ? », « Prenons une pause. ». L'élève doit
        /// encore répondre à cela : ce n'est pas le top départ, quoi qu'il y ait plus haut.
        /// </summary>
        [GeneratedRegex(@"(?:\b(?:écrit|cahier|papier|oral|ordinateur|écran|clavier)\b[^.!?\n]*\bou\b[^.!?\n]*\b(?:écrit|cahier|papier|oral|ordinateur|écran|clavier)\b[^.!?\n]*\?"
            + @"|\bpause\b[^.!?\n]*\?"
            + @"|\b(?:on\s+fait|faisons|prenons|prends|on\s+prend)\s+une\s+(?:petite\s+)?pause\b[^.!?\n]*[.!…])"
            + @"[\s\p{So}\p{Cs}*_»""”)]*\z",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex FinitSurSupportOuPause();

        /// <summary>La balise du choix du support (deux boutons à l'écran).</summary>
        public const string BaliseSupport = "[SUPPORT_EVALUATION]";

        /// <summary>Retire le bloc et le marqueur d'ouverture. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc(), Debut);
    }
}
