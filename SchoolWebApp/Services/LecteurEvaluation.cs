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

        /// <summary>Retire le bloc et le marqueur d'ouverture. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc(), Debut);
    }
}
