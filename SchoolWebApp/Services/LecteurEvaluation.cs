using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une évaluation telle que le professeur l'a déclarée dans son message.</summary>
    public record EvaluationDeclaree(
        string? Notion,
        double Note,
        string? Remarque,
        string? ARevoir,
        List<QuestionEvaluation> Questions);

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

            return new EvaluationDeclaree(
                LecteurBloc.Valeur(champs, "notion"),
                note.Value,
                LecteurBloc.Valeur(champs, "remarque"),
                LecteurBloc.Valeur(champs, "a_revoir", "arevoir"),
                Questions(champs));
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

            if (propre.StartsWith("juste") || propre.StartsWith("correct") || propre.StartsWith("bon"))
                return "juste";

            if (propre.StartsWith("partiel") || propre.StartsWith("moiti"))
                return "partiel";

            if (propre.StartsWith("faux") || propre.StartsWith("incorrect") || propre.StartsWith("mauvais"))
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
