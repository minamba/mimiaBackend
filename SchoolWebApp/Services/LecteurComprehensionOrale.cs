using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une compréhension orale telle que le professeur l'a archivée.</summary>
    public record ComprehensionOraleDeclaree(
        string? Titre, string Langue, string Passage, string ReponseEleve,
        string Comprehension, string? Remarque);

    /// <summary>
    /// Extrait le bloc [COMPREHENSION_ORALE] d'un message du professeur.
    ///
    /// Même construction que <see cref="LecteurDictee"/> : quatre corps de
    /// texte libre à la suite (passage, reponse, comprehension, remarque),
    /// coupés à chacune des lignes `passage:`, `reponse:`, `comprehension:`
    /// et `remarque:`, dans cet ordre. `titre` et `langue` sont des champs
    /// `clé: valeur` ordinaires, lus dans l'en-tête avant `passage:`.
    ///
    /// `reponse` ET `comprehension` NE DISENT PAS LA MÊME CHOSE : la
    /// première est ce que l'élève a VRAIMENT dit, la seconde est ce que le
    /// professeur en a compris — son évaluation. Les confondre a été la
    /// version 1 du bloc, corrigée le 10/09/2026 après que la fiche ne
    /// montrait plus que la paraphrase du professeur, jamais la réponse
    /// elle-même.
    ///
    /// PAS DE NOTION D'ÉTAT, contrairement à la dictée : l'exercice se
    /// conclut en un seul échange, le bloc n'est donc jamais réécrit.
    /// </summary>
    public static partial class LecteurComprehensionOrale
    {
        [GeneratedRegex(@"\[COMPREHENSION_ORALE\](?<corps>.*?)\[/COMPREHENSION_ORALE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        // LE MARQUEUR N EST PAS TOUJOURS SEUL SUR SA LIGNE.
        //
        // Ces motifs exigeaient une fin de ligne juste apres les deux-points.
        // Le professeur ecrit pourtant souvent « remarque: Bilal demande a
        // reecouter... » d une traite : le marqueur ne correspondait alors a
        // rien, et tout le bloc restait colle dans le champ precedent —
        // « remarque: » compris, affiche tel quel a l eleve le 10/09/2026.
        [GeneratedRegex(@"^\s*passage\s*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SeparateurPassage();

        [GeneratedRegex(@"^\s*reponse\s*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SeparateurReponse();

        [GeneratedRegex(@"^\s*comprehension\s*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SeparateurComprehension();

        [GeneratedRegex(@"^\s*remarque\s*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SeparateurRemarque();

        /// <summary>La compréhension orale archivée dans ce message, ou null.</summary>
        public static ComprehensionOraleDeclaree? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            return bloc.Success ? LireBloc(bloc.Groups["corps"].Value) : null;
        }

        private static ComprehensionOraleDeclaree? LireBloc(string corps)
        {
            var coupurePassage = SeparateurPassage().Match(corps);

            // Sans « passage: », on ne sait pas où finit l'en-tête et où
            // commence le texte lu — mieux vaut ne rien enregistrer.
            if (!coupurePassage.Success) return null;

            var champs = LecteurBloc.Champs(corps[..coupurePassage.Index]);
            var titre = LecteurBloc.Valeur(champs, "titre", "notion");
            var langue = LecteurBloc.Valeur(champs, "langue", "code");

            // Sans langue, impossible de choisir la bonne consigne de
            // prononciation à la régénération de l'audio.
            if (string.IsNullOrWhiteSpace(langue)) return null;

            var apresPassage = corps[(coupurePassage.Index + coupurePassage.Length)..];
            var coupureReponse = SeparateurReponse().Match(apresPassage);
            var coupureComprehensionAvantReponse = SeparateurComprehension().Match(apresPassage);

            // « reponse: » MANQUANT N'EST PAS UN MOTIF DE REJET.
            //
            // C'est arrivé le 10/09/2026 : un bloc parfaitement rempli
            // (titre/langue/passage/comprehension/remarque), sans le
            // nouveau champ « reponse: » que le professeur n'avait pas
            // encore pris l'habitude de poser — et TOUT l'archivage a été
            // jeté pour ce seul champ manquant. L'élève avait pourtant
            // vraiment fait l'exercice ; seule la citation exacte de sa
            // réponse manquait. On retombe donc sur l'ancien format à
            // trois corps (passage/comprehension/remarque) dès que
            // « reponse: » est absent, plutôt que de perdre tout le reste.
            var reponseAbsente = !coupureReponse.Success
                || (coupureComprehensionAvantReponse.Success
                    && coupureComprehensionAvantReponse.Index < coupureReponse.Index);

            string passage;
            string reponseEleve;
            string apresReponseOuPassage;

            if (reponseAbsente)
            {
                if (!coupureComprehensionAvantReponse.Success) return null;

                passage = apresPassage[..coupureComprehensionAvantReponse.Index].Trim();
                reponseEleve = "";
                apresReponseOuPassage =
                    apresPassage[(coupureComprehensionAvantReponse.Index + coupureComprehensionAvantReponse.Length)..];
            }
            else
            {
                passage = apresPassage[..coupureReponse.Index].Trim();

                var apresReponse = apresPassage[(coupureReponse.Index + coupureReponse.Length)..];
                var coupureComprehension = SeparateurComprehension().Match(apresReponse);

                // Sans « comprehension: », il n'y a rien à montrer de
                // l'évaluation du professeur.
                if (!coupureComprehension.Success) return null;

                reponseEleve = apresReponse[..coupureComprehension.Index].Trim();
                apresReponseOuPassage = apresReponse[(coupureComprehension.Index + coupureComprehension.Length)..];
            }

            var coupureRemarque = SeparateurRemarque().Match(apresReponseOuPassage);

            var comprehension = (coupureRemarque.Success
                ? apresReponseOuPassage[..coupureRemarque.Index]
                : apresReponseOuPassage).Trim();

            var remarque = coupureRemarque.Success
                ? apresReponseOuPassage[(coupureRemarque.Index + coupureRemarque.Length)..].Trim()
                : null;

            if (string.IsNullOrWhiteSpace(passage) || string.IsNullOrWhiteSpace(comprehension))
            {
                return null;
            }

            return new ComprehensionOraleDeclaree(
                titre, langue.Trim().ToLowerInvariant(), passage, reponseEleve, comprehension,
                string.IsNullOrWhiteSpace(remarque) ? null : remarque);
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());

        [GeneratedRegex(@"\[COMPREHENSION_SUPPRIMEE\](?<corps>.*?)\[/COMPREHENSION_SUPPRIMEE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex BlocSuppression();

        /// <summary>
        /// L'exercice d'écoute dont l'élève ne veut plus : le dernier de la
        /// conversation (numéro nul), ou une fiche désignée par son numéro.
        ///
        /// Voulu par Camara le 12/09/2026, même mécanique que la dictée
        /// abandonnée : ce qu'il refuse disparaît, au lieu de traîner dans ses
        /// archives et de lui être ressorti.
        /// </summary>
        public static SuppressionDictee? LireSuppression(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = BlocSuppression().Match(message);
            if (!bloc.Success) return null;

            var numero = Regex.Match(bloc.Groups["corps"].Value, @"\d+");

            return new SuppressionDictee(
                numero.Success && int.TryParse(numero.Value, out var id) ? id : null);
        }
    }
}
