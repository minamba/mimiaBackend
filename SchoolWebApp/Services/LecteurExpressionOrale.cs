using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Un tour de parole déclaré par le professeur.</summary>
    public record TourDeclare(string Qui, string Texte);

    /// <summary>Une conversation d'expression orale telle que le professeur l'archive.</summary>
    public record ExpressionOraleDeclaree(
        string Titre, string Langue, IReadOnlyList<TourDeclare> Echange, string? Remarque);

    /// <summary>
    /// Extrait le bloc [EXPRESSION_ORALE] d'un message du professeur.
    ///
    /// CE N'EST PAS UNE COMPRÉHENSION ORALE, et le bloc ne lui ressemble pas :
    /// là-bas, quatre corps de texte à la suite (passage, réponse,
    /// compréhension, remarque) ; ici, une CONVERSATION, c'est-à-dire une
    /// suite de tours dont on ne connaît pas le nombre à l'avance.
    ///
    /// UNE LIGNE PAR TOUR, PRÉFIXÉE PAR QUI PARLE :
    ///
    ///     [EXPRESSION_ORALE]
    ///     titre: Commander au restaurant
    ///     langue: en
    ///     eleve: Hello, I would like a pizza please.
    ///     prof: Of course! Which one would you like?
    ///     eleve: The one with… euh… mushrooms?
    ///     prof: Perfect. And something to drink?
    ///     remarque: Il ose des phrases complètes.
    ///     [/EXPRESSION_ORALE]
    ///
    /// POURQUOI DES PRÉFIXES ET NON DES SÉPARATEURS. Un modèle qui écrit
    /// quinze tours oubliera une borne fermante bien avant d'oublier le mot
    /// qui commence sa ligne. Ici, une ligne mal formée se perd toute seule
    /// sans emporter la conversation — et c'est le seul bloc de ce fichier
    /// dont la longueur n'est pas connue d'avance.
    ///
    /// UN TOUR PEUT TENIR SUR PLUSIEURS LIGNES : tout ce qui suit sans préfixe
    /// reconnu appartient au tour en cours. Un élève qui raconte ses vacances
    /// fait des paragraphes, et les couper au premier retour à la ligne
    /// hacherait sa parole.
    /// </summary>
    public static partial class LecteurExpressionOrale
    {
        public const string Eleve = "eleve";
        public const string Professeur = "professeur";

        [GeneratedRegex(@"\[EXPRESSION_ORALE\](?<corps>.*?)\[/EXPRESSION_ORALE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        [GeneratedRegex(@"^\s*titre\s*:[ \t]*(?<v>.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex Titre();

        [GeneratedRegex(@"^\s*langue\s*:[ \t]*(?<v>.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex Langue();

        /// <summary>
        /// Le préfixe d'une ligne de tour, ou de la remarque finale.
        ///
        /// `prof` ET `professeur` SONT ACCEPTÉS, et ce n'est pas de la
        /// complaisance : la consigne en montre un, un modèle écrit l'autre, et
        /// une conversation entière serait perdue pour quatre lettres. Même
        /// chose pour `eleve` accentué ou non.
        /// </summary>
        [GeneratedRegex(@"^\s*(?<qui>[ée]l[eè]ve|prof|professeur|remarque)\s*:[ \t]*(?<v>.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex Ligne();

        /// <summary>La conversation archivée dans ce message, ou null.</summary>
        public static ExpressionOraleDeclaree? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            return bloc.Success ? LireBloc(bloc.Groups["corps"].Value) : null;
        }

        private static ExpressionOraleDeclaree? LireBloc(string corps)
        {
            var titre = Titre().Match(corps) is { Success: true } t
                ? t.Groups["v"].Value.Trim()
                : string.Empty;

            var langue = Langue().Match(corps) is { Success: true } l
                ? l.Groups["v"].Value.Trim().ToLowerInvariant()
                : string.Empty;

            var tours = new List<TourDeclare>();
            string? remarque = null;

            // L'ÉTAT COURANT : à quoi appartient la ligne qu'on est en train de
            // lire. Null tant qu'aucun préfixe n'a été rencontré — l'en-tête
            // (`titre`, `langue`) ne tombe donc nulle part.
            string? qui = null;
            var morceaux = new List<string>();

            void Fermer()
            {
                if (qui is null) return;

                var texte = string.Join("\n", morceaux).Trim();
                morceaux.Clear();

                if (texte.Length == 0) return;

                if (qui == "remarque") remarque = texte;
                else tours.Add(new TourDeclare(qui, texte));
            }

            foreach (var ligne in corps.Replace("\r\n", "\n").Split('\n'))
            {
                var debut = Ligne().Match(ligne);

                if (debut.Success)
                {
                    Fermer();

                    var brut = debut.Groups["qui"].Value.ToLowerInvariant();

                    qui = brut switch
                    {
                        "remarque" => "remarque",
                        "prof" or "professeur" => Professeur,
                        _ => Eleve,
                    };

                    morceaux.Add(debut.Groups["v"].Value);
                    continue;
                }

                // Une ligne sans préfixe prolonge le tour en cours. Hors de tout
                // tour — l'en-tête, une ligne vide avant le premier `eleve:` —
                // elle est ignorée.
                if (qui is not null) morceaux.Add(ligne);
            }

            Fermer();

            // SANS TITRE, SANS LANGUE OU SANS CONVERSATION, ON N'ARCHIVE PAS.
            //
            // Une ligne vide en base vaudrait moins que rien : elle
            // apparaîtrait dans la liste de l'élève, il cliquerait, et il
            // n'y aurait rien à lire. Mieux vaut ne rien écrire et laisser le
            // professeur recommencer — l'échec est silencieux, comme pour les
            // autres blocs.
            if (titre.Length == 0 || langue.Length == 0 || tours.Count == 0) return null;

            return new ExpressionOraleDeclaree(titre, langue, tours, remarque);
        }
    }
}
