using System.Text.RegularExpressions;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// OÙ COMMENCE ET OÙ FINIT UN EXERCICE, relu depuis les messages bruts.
    ///
    /// TROIS FENÊTRES S'EN SERVENT, et elles n'en demandent pas la même chose :
    /// <c>ExpressionOraleRepository</c> veut savoir ce qu'il doit RAMASSER,
    /// <c>ComprehensionOraleRepository</c> ce qu'il doit IGNORER — les répliques
    /// d'une conversation portent les mêmes balises de langue qu'un passage lu à
    /// voix haute —, et <c>ExpressionEcriteRepository</c> où s'arrête le texte
    /// qu'il reconstitue.
    ///
    /// LA RÈGLE VIT ICI ET NULLE PART AILLEURS. Elle a été écrite en double une
    /// fois, et les deux copies ont eu le même défaut le même jour : c'est
    /// précisément ce qui arrive à une règle recopiée. Son pendant à l'écran est
    /// dans <c>src/lib/storage/fenetreExercice.js</c>, et porte la même liste.
    /// </summary>
    internal static class FenetreExercice
    {
        /// <summary>
        /// CE QUI NE VEUT JAMAIS DIRE « ON EST PASSÉ À AUTRE CHOSE ».
        ///
        /// Les répliques du professeur dans la langue du cours, le tableau, une
        /// image. Montrer un mot au tableau pendant qu'on travaille est
        /// exactement ce qu'un professeur doit faire.
        /// </summary>
        private static readonly HashSet<string> Neutres =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Ses répliques : une balise par langue enseignée.
                "EN", "FR", "ES", "DE", "IT", "ZH",

                "ARDOISE", "TABLEAU_EFFACE", "SCHEMA", "POINTAGE",
            };

        /// <summary>Les balises propres à une conversation d'expression orale.</summary>
        public static readonly string[] Conversation = ["CONVERSATION"];

        /// <summary>
        /// Celles d'un texte à rédiger : celle qui pose la question du support,
        /// et celle qui range le texte corrigé.
        /// </summary>
        public static readonly string[] TexteEcrit = ["SUPPORT_ECRIT", "EXPRESSION_ECRITE"];

        /// <summary>Le nom d'une balise technique, quelle qu'elle soit.</summary>
        private static readonly Regex NomDeBalise =
            new(@"\[/?([A-Z_]+)(?::[^\]]*)?\]",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// UNE FENÊTRE SE REFERME DÈS QU'AUTRE CHOSE S'OUVRE.
        ///
        /// LE DÉFAUT QUE CETTE RÈGLE REMPLACE — Camara, en séance le 18/09/2026 :
        /// « j'avais demandé une expression orale que j'ai pas faite, je viens de
        /// demander un exercice d'expression écrite mais la fenêtre du choix de
        /// vitesse pour l'expression orale est toujours là et ne part pas. »
        ///
        /// LA CONVERSATION NE SE FERMAIT QU'À SON ARCHIVAGE. C'était juste pour
        /// une conversation qu'on FAIT — mais une conversation qu'on abandonne ne
        /// s'archive jamais. Elle restait ouverte pour le reste de la séance.
        ///
        /// À L'ÉCRAN, ÇA EMPILAIT LES QUATRE VITESSES SOUS L'EXERCICE SUIVANT.
        /// EN BASE, C'ÉTAIT PIRE : le filet de la compréhension orale ne
        /// ramassait plus RIEN jusqu'à la fin de la séance — le filet devenait
        /// aveugle, ce qui est exactement ce contre quoi il existe.
        ///
        /// UNE LISTE BLANCHE PLUTÔT QU'UNE LISTE NOIRE, et c'est le point : on ne
        /// peut pas énumérer tout ce qui n'est pas une conversation, mais on peut
        /// énumérer ce qui EN FAIT PARTIE — c'est court, et ça ne bouge pas. Un
        /// exercice ajouté l'an prochain refermera la fenêtre sans que personne
        /// ait à y penser. C'est la leçon du bloc [EXPRESSION_ORALE] qui s'est
        /// affiché et prononcé en séance : ce qui dépend d'une liste à tenir à
        /// jour finit par ne pas l'être.
        ///
        /// L'ARCHIVAGE LA REFERME TOUJOURS, du même coup : [EXPRESSION_ORALE]
        /// n'est pas dans la liste blanche.
        /// </summary>
        /// <summary>
        /// LE MARQUEUR QUI CLÔT UNE SÉANCE.
        /// </summary>
        private static readonly Regex FinDeSeance =
            new(@"\[FIN_SEANCE\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// OÙ COMMENCE LA SÉANCE EN COURS dans une liste de messages rangés du
        /// plus ancien au plus récent. Zéro si elle est la première.
        ///
        /// LE DÉFAUT QUE CETTE BORNE ÉVITE — Camara, le 18/09/2026 : « y'a un
        /// truc qui se ferme pas et qui ajoute des conversations à l'infini ». Il
        /// a vu DIX fois la même conversation archivée, toutes à la même minute.
        ///
        /// LA CAUSE : une conversation de matière VIT DES MOIS. Les filets
        /// relisaient ses soixante derniers messages sans se demander à quelle
        /// SÉANCE ils appartenaient — donc un exercice abandonné la semaine
        /// dernière était reconstitué à la fin de chaque séance suivante, encore
        /// et encore.
        ///
        /// LE DERNIER [FIN_SEANCE] EST IGNORÉ QUAND IL EST LE DERNIER MESSAGE :
        /// les filets tournent JUSTEMENT au tour qui le porte. Le prendre pour
        /// borne rendrait une fenêtre vide, et plus rien ne serait jamais rattrapé
        /// — un filet silencieusement mort, ce qui est pire que des doublons.
        /// </summary>
        public static int DebutDeSeance<T>(
            IReadOnlyList<T> messages, Func<T, bool> duProfesseur, Func<T, string> contenu)
        {
            for (var i = messages.Count - 2; i >= 0; i--)
            {
                if (duProfesseur(messages[i]) && FinDeSeance.IsMatch(contenu(messages[i]) ?? string.Empty))
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// TOUT CE QUI EST ENTRE CROCHETS DISPARAÎT DE LA PAROLE DE L'ÉLÈVE.
        ///
        /// LE DÉFAUT, VU PAR CAMARA LE 18/09/2026 dans l'archive d'un enfant :
        /// « J'écris sur mon cahier. [TEXTE AU CAHIER : il écrit sur son cahier.
        /// Sa copie ne peut te parvenir qu'en PHOTO…] » — la consigne interne au
        /// professeur, affichée telle quelle dans la conversation que l'enfant
        /// relit.
        ///
        /// L'ANCIEN MOTIF NE PRENAIT QUE `[MOT_EN_MAJUSCULES]`. Les faits que
        /// l'application accroche aux messages sont des PHRASES — « [L'élève a
        /// choisi la vitesse…] », « [TEXTE AU CAHIER : …] » —, avec des espaces,
        /// des minuscules et de la ponctuation. Aucun ne correspondait.
        ///
        /// ON RETIRE DONC TOUT CROCHET, sans chercher à reconnaître lequel. Un
        /// enfant qui écrit un crochet dans sa phrase est assez rare, et le
        /// perdre est sans commune mesure avec afficher à un élève de dix ans les
        /// instructions qu'on donne à son professeur.
        /// </summary>
        private static readonly Regex ToutCrochet =
            new(@"\[[^\]]*\]", RegexOptions.Singleline | RegexOptions.Compiled);

        public static string SansCrochets(string? contenu) =>
            ToutCrochet.Replace(contenu ?? string.Empty, " ")
                .Replace("  ", " ")
                .Trim();

        public static bool OuvreAutreChose(string? contenu, IReadOnlyList<string> propres)
        {
            foreach (Match trouvee in NomDeBalise.Matches(contenu ?? string.Empty))
            {
                var nom = trouvee.Groups[1].Value;

                if (Neutres.Contains(nom)) continue;

                // LES BALISES DE LA FENÊTRE ELLE-MÊME NE LA REFERMENT PAS : sans
                // cette garde, un exercice se fermerait sur sa propre balise
                // d'ouverture au moment même où il commence.
                if (propres.Contains(nom, StringComparer.OrdinalIgnoreCase)) continue;

                return true;
            }

            return false;
        }
    }
}
