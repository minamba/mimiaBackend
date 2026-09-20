using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une dictée corrigée telle que le professeur l'a archivée.</summary>
    public record DicteeDeclaree(string? Titre, string? Etat, string Dicte, string Copie, string? Remarque);

    /// <summary>
    /// L'élève ne veut plus d'une dictée : la dernière de la conversation
    /// (<see cref="DicteeId"/> nul), ou une dictée archivée désignée par son numéro.
    /// </summary>
    public record SuppressionDictee(int? DicteeId);

    /// <summary>
    /// Extrait le bloc [DICTEE_CORRIGEE] d'un message du professeur.
    ///
    /// Trois corps de texte libre à la suite, pas un seul comme [FICHE] : le
    /// texte dicté, puis la copie de l'élève, puis la remarque. On coupe donc
    /// à chacune des lignes `dicte:`, `copie:` et `remarque:`, dans cet ordre,
    /// et on prend la suite telle quelle jusqu'à la coupure suivante. `titre`
    /// et `etat` sont des champs `clé: valeur` ordinaires, lus dans l'en-tête
    /// avant `dicte:` — voir <see cref="EtatDictee"/> pour ce que vaut `etat`.
    /// </summary>
    public static partial class LecteurDictee
    {
        /// <summary>La dictée interrompue avant sa copie — voir <see cref="Domain.Repositories.MarqueursDictee"/>.</summary>
        public const string Abandon = Domain.Repositories.MarqueursDictee.Abandon;

        [GeneratedRegex(@"\[DICTEE_CORRIGEE\](?<corps>.*?)\[/DICTEE_CORRIGEE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        // LES SÉPARATEURS ACCEPTENT DU TEXTE SUR LA MÊME LIGNE — corrigé le
        // 20/09/2026. Ils exigeaient « remarque: » SEUL sur sa ligne ; le
        // professeur a écrit « remarque: Deux erreurs corrigées… » à la suite,
        // le séparateur n'a pas été reconnu, et toute la remarque est partie
        // dans la COPIE de l'élève. Remise au tableau, la comparaison posait
        // des badges sur des mots que l'enfant n'avait jamais écrits.
        [GeneratedRegex(@"^[ \t]*dicte[ \t]*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SeparateurDicte();

        [GeneratedRegex(@"^[ \t]*copie[ \t]*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SeparateurCopie();

        [GeneratedRegex(@"^[ \t]*remarque[ \t]*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SeparateurRemarque();

        /// <summary>La dictée archivée dans ce message, ou null.</summary>
        public static DicteeDeclaree? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            return bloc.Success ? LireBloc(bloc.Groups["corps"].Value) : null;
        }

        private static DicteeDeclaree? LireBloc(string corps)
        {
            var coupureDicte = SeparateurDicte().Match(corps);

            // Sans « dicte: », on ne sait pas où finit l'en-tête (titre) et où
            // commence le texte dicté. Mieux vaut ne rien enregistrer qu'une
            // dictée dont le texte serait noyé dans autre chose.
            if (!coupureDicte.Success) return null;

            var champs = LecteurBloc.Champs(corps[..coupureDicte.Index]);
            var titre = LecteurBloc.Valeur(champs, "titre", "notion");
            var etat = LecteurBloc.Valeur(champs, "etat", "état", "statut");

            var apresDicte = corps[(coupureDicte.Index + coupureDicte.Length)..];
            var coupureCopie = SeparateurCopie().Match(apresDicte);

            // Sans « copie: », il n'y a rien à comparer au texte dicté — ce
            // n'est pas une dictée corrigée, seulement un texte dicté seul.
            if (!coupureCopie.Success) return null;

            var dicte = apresDicte[..coupureCopie.Index].Trim();

            var apresCopie = apresDicte[(coupureCopie.Index + coupureCopie.Length)..];
            var coupureRemarque = SeparateurRemarque().Match(apresCopie);

            var copie = (coupureRemarque.Success ? apresCopie[..coupureRemarque.Index] : apresCopie).Trim();
            var remarque = coupureRemarque.Success
                ? apresCopie[(coupureRemarque.Index + coupureRemarque.Length)..].Trim()
                : null;

            // `dicte` ET `copie` PEUVENT ÊTRE VIDES — depuis le 19/09/2026 : le
            // professeur n'a plus à les retaper, l'application les remplit
            // depuis les messages (voir EnregistrerDicteeAsync). Un texte vide
            // ici n'est plus un bloc rejeté, c'est un bloc à compléter.

            return new DicteeDeclaree(
                titre, etat, dicte, copie, string.IsNullOrWhiteSpace(remarque) ? null : remarque);
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());

        [GeneratedRegex(@"\[DICTEE_SUPPRIMEE\](?<corps>.*?)\[/DICTEE_SUPPRIMEE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex BlocSuppression();

        [GeneratedRegex(
            @"(ta|cette|la) dict[ée]e[^.!?\n]{0,60}(corrig[ée]e|termin[ée]e|finie|boucl[ée]e)"
            + @"|tout bon pour (cette|ta) dict[ée]e"
            + @"|fini de (la )?corriger",
            RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceCorrigee();

        [GeneratedRegex(@"\b(pas|plus|jamais|encore|sera|seront|quand|lorsque|avant|si)\b",
            RegexOptions.IgnoreCase)]
        private static partial Regex NegationOuFutur();

        /// <summary>
        /// Le professeur annonce-t-il, en toutes lettres, que la dictée est
        /// corrigée ?
        ///
        /// Relevé par Camara le 11/09/2026 : « Ta dictée est complète et bien
        /// corrigée… C'est tout bon pour cette dictée » — et aucun
        /// [DICTEE_CORRIGEE]. L'archive la montrait « en attente de
        /// correction », sans titre ni badges, alors qu'elle venait d'être
        /// corrigée devant l'élève. On lit donc l'annonce elle-même.
        ///
        /// « Pas encore corrigée », « quand elle sera corrigée » ne comptent
        /// pas : une négation ou un futur dans la tournure l'écarte.
        /// </summary>
        public static bool AnnonceLaCorrection(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            return AnnonceCorrigee().Matches(message)
                .Any(m => !NegationOuFutur().IsMatch(m.Value));
        }

        /// <summary>
        /// Une dictée archivée, remise au tableau par son numéro :
        /// <c>[DICTEE_AU_TABLEAU]42[/DICTEE_AU_TABLEAU]</c>.
        /// </summary>
        [GeneratedRegex(@"\[DICTEE_AU_TABLEAU\]\s*(?:n°\s*)?(?<id>\d+)\s*\[/DICTEE_AU_TABLEAU\]",
            RegexOptions.IgnoreCase)]
        public static partial Regex AuTableau();

        /// <summary>
        /// Le tableau de comparaison — « La dictée » puis « Ta copie » dans un
        /// même bloc — qui ouvre la correction.
        /// </summary>
        [GeneratedRegex(
            @"\[ARDOISE\][\s\S]*?^[ \t]*La dictée[ \t]*\r?$[\s\S]*?^[ \t]*Ta copie[ \t]*\r?$[\s\S]*?\[/ARDOISE\]",
            RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex ComparaisonAuTableau();

        /// <summary>
        /// LE TABLEAU DE COMPARAISON, ÉCRIT PAR L'APPLICATION — Camara, le
        /// 19/09/2026, pour réduire le coût des cours de langue : le professeur
        /// retapait les deux textes (3 900 jetons de sortie dans une séance),
        /// avec le risque d'altérer la copie. Ici, mot pour mot.
        /// </summary>
        public static string TableauDeComparaison(string dicte, string copie) =>
            "[ARDOISE]\nLa dictée\n" + dicte.Trim() + "\n\nTa copie\n" + copie.Trim() + "\n[/ARDOISE]";

        /// <summary>
        /// Retire un tableau de comparaison que le professeur aurait écrit quand
        /// même : celui de l'application, déjà au tableau, fait foi.
        /// </summary>
        public static string SansComparaison(string? texte) =>
            string.IsNullOrEmpty(texte)
                ? string.Empty
                : Regex.Replace(ComparaisonAuTableau().Replace(texte, string.Empty), @"\n{3,}", "\n\n").Trim();

        /// <summary>
        /// UNE CORRECTION DE DICTÉE EST EN COURS : le professeur a mis les deux
        /// textes au tableau et n'a pas encore écrit [DICTEE_CORRIGEE].
        ///
        /// Sert au rappel joint à chaque tour — Camara, le 19/09/2026 : vingt-
        /// quatre écarts numérotés, et le professeur a commencé par le n° 5.
        /// « Dans l'ordre du texte » était dans sa consigne ; comme pour le
        /// contrôle et l'expression écrite, il faut le lui redire au tour même.
        ///
        /// Une nouvelle dictée, un abandon, une fin de séance ou un tableau
        /// effacé referment la correction.
        /// </summary>
        public static bool CorrectionOuverte(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains("[DICTEE_CORRIGEE]", StringComparison.OrdinalIgnoreCase)
                    || message.Contains(Abandon, StringComparison.Ordinal)
                    || message.Contains("[FIN_SEANCE]", StringComparison.Ordinal)
                    || message.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal)
                    || message.Contains("[DICTEE]", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (ComparaisonAuTableau().IsMatch(message)) return true;
            }

            return false;
        }

        /// <summary>
        /// La dictée que l'élève ne veut plus — voulu par Camara le
        /// 11/09/2026 : passée, abandonnée ou refusée à la reprise, elle
        /// disparaît de partout. Null si le message n'en demande aucune.
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
