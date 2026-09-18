using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une chose relevée par le professeur : son genre, et ce qu'il en dit.</summary>
    public record RepriseDeclaree(string Genre, string Texte);

    /// <summary>Un texte d'expression écrite tel que le professeur l'archive.</summary>
    public record ExpressionEcriteDeclaree(
        string Titre,
        string Langue,
        string Consigne,
        string Texte,
        IReadOnlyList<RepriseDeclaree> Corrections,
        string? Remarque,

        /// <summary>
        /// LE NUMÉRO D'UNE LIGNE DÉJÀ EN BASE, quand le professeur reprend une
        /// copie restée en photo. Null pour un texte tout neuf.
        ///
        /// Voulu par Camara le 18/09/2026 : « le prof pourra quand même refaire
        /// la transcription si elle a pas été faite. » Un seul bloc pour les
        /// deux cas, et une ligne de plus : demander au modèle de retenir deux
        /// balises presque identiques serait lui demander de se tromper.
        /// </summary>
        int? Numero = null);

    /// <summary>
    /// Extrait le bloc [EXPRESSION_ECRITE] d'un message du professeur.
    ///
    /// LA MÊME MÉCANIQUE DE LIGNES PRÉFIXÉES QUE L'EXPRESSION ORALE, et pour
    /// la même raison : le nombre de reprises n'est pas connu d'avance. Une
    /// ligne mal formée se perd toute seule sans emporter la copie.
    ///
    ///     [EXPRESSION_ECRITE]
    ///     titre: Raconter son week-end
    ///     langue: en
    ///     consigne: Écris cinq phrases sur ton week-end.
    ///     texte: Last weekend I go to the park with my frend. We play football.
    ///     reussi: « with my friend », la phrase tient debout du début à la fin.
    ///     grammaire: Tu as écrit « I go ». Au passé on dit « I went ».
    ///     orthographe: « frend » s'écrit « friend », avec un i.
    ///     remarque: Il ose des phrases longues.
    ///     [/EXPRESSION_ECRITE]
    ///
    /// LES GENRES SONT DES PRÉFIXES, PAS UN CHAMP `type:`. Écrire
    /// `grammaire: …` est plus court, plus lisible en base, et surtout
    /// impossible à désynchroniser : il n'y a pas de couple (type, texte) à
    /// tenir aligné sur deux lignes.
    ///
    /// PLUSIEURS LIGNES DU MÊME GENRE SONT ATTENDUES — trois réussites, deux
    /// fautes d'orthographe. Chacune est une reprise à part, dans l'ordre où
    /// le professeur les a écrites : la consigne lui impose de commencer par
    /// ce qui est réussi, et cet ordre-là doit se retrouver à l'écran.
    ///
    /// `texte:` TIENT SUR PLUSIEURS LIGNES, forcément : c'est une rédaction.
    /// Tout ce qui suit sans préfixe reconnu lui appartient.
    /// </summary>
    public static partial class LecteurExpressionEcrite
    {
        [GeneratedRegex(@"\[EXPRESSION_ECRITE\](?<corps>.*?)\[/EXPRESSION_ECRITE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>
        /// Le préfixe d'une ligne : un champ d'en-tête, un genre de reprise, ou
        /// la remarque finale.
        ///
        /// LES VARIANTES SONT ACCEPTÉES SANS ÉTAT D'ÂME — `réussi` accentué,
        /// `ortho`, `conjugaison`, `lexique`, `syntaxe`. La consigne en montre
        /// une, un modèle écrit l'autre, et une copie entière serait perdue pour
        /// deux lettres. `GenreReprise.Normaliser` les ramène ensuite aux cinq
        /// genres de l'examen.
        /// </summary>
        [GeneratedRegex(
            @"^\s*(?<champ>titre|langue|consigne|texte|remarque|num[ée]ro"
            + @"|r[ée]ussi[et]?|orthographe|ortho|grammaire|conjugaison|accord"
            + @"|vocabulaire|lexique|construction|syntaxe|style)\s*:[ \t]*(?<v>.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex Ligne();

        /// <summary>Le texte archivé dans ce message, ou null.</summary>
        public static ExpressionEcriteDeclaree? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            return bloc.Success ? LireBloc(bloc.Groups["corps"].Value) : null;
        }

        /// <summary>Les champs d'en-tête, qui ne sont pas des reprises.</summary>
        private static readonly HashSet<string> EnTete =
            new(StringComparer.OrdinalIgnoreCase)
            { "titre", "langue", "consigne", "texte", "remarque", "numero", "numéro" };

        private static ExpressionEcriteDeclaree? LireBloc(string corps)
        {
            var titre = string.Empty;
            var langue = string.Empty;
            var consigne = string.Empty;
            var texte = string.Empty;
            string? remarque = null;
            int? numero = null;

            var corrections = new List<RepriseDeclaree>();

            // À QUOI APPARTIENT LA LIGNE EN COURS. Null tant qu'aucun préfixe
            // n'a été rencontré : une ligne vide en tête ne tombe nulle part.
            string? champ = null;
            var morceaux = new List<string>();

            void Fermer()
            {
                if (champ is null) return;

                var valeur = string.Join("\n", morceaux).Trim();
                morceaux.Clear();

                if (valeur.Length == 0) return;

                switch (champ.ToLowerInvariant())
                {
                    case "titre": titre = valeur; break;
                    case "langue": langue = valeur.ToLowerInvariant(); break;
                    case "consigne": consigne = valeur; break;
                    case "texte": texte = valeur; break;
                    case "remarque": remarque = valeur; break;

                    // UN NUMÉRO ILLISIBLE EST IGNORÉ, PAS FATAL : le bloc vaut
                    // alors pour un texte neuf. Mieux vaut une ligne en trop
                    // qu'une copie perdue parce que le modèle a écrit « n° 12 ».
                    case "numero":
                    case "numéro":
                        if (int.TryParse(
                            new string(valeur.Where(char.IsDigit).ToArray()),
                            out var lu) && lu > 0)
                        {
                            numero = lu;
                        }

                        break;

                    // TOUT LE RESTE EST UNE REPRISE, dans l'ordre d'écriture. La
                    // normalisation du genre se fait au dépôt, en un seul
                    // endroit : ce lecteur ne fait que découper.
                    default: corrections.Add(new RepriseDeclaree(champ, valeur)); break;
                }
            }

            foreach (var ligne in corps.Replace("\r\n", "\n").Split('\n'))
            {
                var debut = Ligne().Match(ligne);

                if (debut.Success)
                {
                    Fermer();

                    champ = debut.Groups["champ"].Value;
                    morceaux.Add(debut.Groups["v"].Value);
                    continue;
                }

                // Une ligne sans préfixe prolonge le champ en cours — c'est ce
                // qui permet à une rédaction de tenir sur dix lignes.
                if (champ is not null) morceaux.Add(ligne);
            }

            Fermer();

            // SANS TEXTE, ON N'ARCHIVE JAMAIS RIEN : une ligne vide apparaîtrait
            // dans la liste de l'élève, il cliquerait, et il n'y aurait rien à
            // lire.
            //
            // LA CORRECTION, ELLE, N'EST PAS EXIGÉE. Elle devrait toujours être
            // là — mais entre un texte d'enfant sans ses reprises et RIEN DU
            // TOUT, le texte gagne : c'est lui qui ne se retrouve nulle part
            // ailleurs, et l'écran sait dire qu'il n'a pas été corrigé.
            if (texte.Length == 0) return null;

            // UNE REPRISE N'A QUE SON NUMÉRO ET SON TEXTE À FOURNIR. Le titre, la
            // langue et la consigne sont DÉJÀ EN BASE, écrits le jour de
            // l'exercice ; les redemander ferait réécrire au modèle un énoncé
            // qu'il ne fait que relire, et une copie serait perdue chaque fois
            // qu'il en oublierait un.
            //
            // Pour un texte NEUF, en revanche, les trois sont indispensables :
            // sans titre l'élève ne retrouve rien, sans consigne son texte ne se
            // relit pas, sans langue on ne sait pas dans quoi il écrivait.
            if (numero is null
                && (titre.Length == 0 || langue.Length == 0 || consigne.Length == 0))
            {
                return null;
            }

            // L'en-tête ne doit jamais se retrouver dans les reprises : la garde
            // est ici plutôt que dans le `switch`, pour qu'un champ ajouté plus
            // tard à `EnTete` soit exclu sans qu'on ait à y penser deux fois.
            corrections.RemoveAll(c => EnTete.Contains(c.Genre));

            return new ExpressionEcriteDeclaree(
                titre, langue, consigne, texte, corrections, remarque, numero);
        }
    }
}
