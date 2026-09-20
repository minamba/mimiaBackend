using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'ÉLÈVE VIENT DE DONNER UN RÉSULTAT — relevé par Camara le 19/09/2026 :
    /// « il arrive même pas à faire des calculs simples, je lui donne la réponse
    /// et il me dit que c'est faux alors qu'il se trompe ».
    ///
    /// CE QUI S'EST PASSÉ, EN BASE. « Si je te donne 3 dans la machine qui
    /// multiplie par 2 ? » — « six ». Réponse : « Presque ! 3 fois 2, ça donne
    /// quoi ? ». Puis « dix » pour 5 × 2 : « tu es sûr que c'est 10 ? ». Les deux
    /// réponses du professeur tiennent en 47 et 86 jetons, réflexion comprise :
    /// il n'a RIEN vérifié. En effort moyen, la réflexion adaptative saute ce que
    /// le modèle croit trivial — et c'est précisément là qu'il répond de travers.
    ///
    /// LA RÈGLE EXISTAIT : « Avant de dire que c'est faux, vérifie que c'est
    /// faux », dans le noyau, avec un exemple presque identique. Mais à des
    /// milliers de lignes du message qu'il est en train de lire. Le serveur, lui,
    /// voit qu'un nombre vient d'arriver : il le dit au tour même, juste à côté
    /// de la réponse — la dernière chose lue avant d'écrire.
    /// </summary>
    public static partial class ReponseChiffree
    {
        /// <summary>
        /// Un chiffre, ou un nombre écrit en toutes lettres par la reconnaissance
        /// vocale — « six », « dix », « trois quarts ». « un » et « une » sont
        /// exclus : trop fréquents comme articles pour signaler un résultat.
        /// </summary>
        [GeneratedRegex(
            @"\d|(?<!\p{L})(?:z[ée]ro|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize"
            + @"|quatorze|quinze|seize|vingt|trente|quarante|cinquante|soixante|cent|mille|million"
            + @"|moiti[ée]|demi|tiers|quarts?)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex Nombre();

        /// <summary>
        /// Vrai quand le message de l'élève porte un nombre. Les faits entre
        /// crochets que l'application accroche au message sont écartés : un
        /// « [Nous sommes le 19 septembre] » n'est pas une réponse.
        /// </summary>
        public static bool Contient(string? messageEleve)
        {
            if (string.IsNullOrWhiteSpace(messageEleve)) return false;

            var dit = Regex.Replace(messageEleve, @"\[[^\]]*\]", " ");
            return Nombre().IsMatch(dit);
        }

        /// <summary>
        /// Le rappel, en cours ordinaire. Court : il part non mis en cache à
        /// chaque réponse chiffrée, soit une fraction de centime.
        /// </summary>
        public const string Rappel =
            "[SA RÉPONSE CONTIENT UN RÉSULTAT. AVANT de dire si c'est juste ou faux, "
            + "refais le calcul toi-même, étape par étape. Un « presque », un « tu es sûr ? » "
            + "ou un « vérifions » posé sur une réponse JUSTE lui apprend à douter de ce qu'il "
            + "sait. Si c'est juste, dis-le franchement : « oui, c'est ça ».]";

        /// <summary>
        /// Pendant un contrôle, le professeur ne dit ni juste ni faux : il note.
        /// Il doit pourtant noter juste — le même calcul, sans verdict prononcé.
        /// </summary>
        public const string RappelControle =
            "[SA RÉPONSE CONTIENT UN RÉSULTAT : refais le calcul toi-même avant de "
            + "le noter. Tu ne lui dis toujours pas si c'est juste.]";
    }
}
