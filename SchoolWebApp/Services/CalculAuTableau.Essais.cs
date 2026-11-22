namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LE COMPTE DES ERREURS SUR LA QUESTION EN COURS, FAIT PAR LE CODE — contrôle d'Omar sur le lot groupé (22/11/2026,
    /// e2e #53, conversation de test 871) : la règle des essais (<see cref="Prompts.PromptsPedagogiques.RegleDesTroisEssais"/>)
    /// demande au professeur de compter EN SILENCE ; il a annoncé « c'est ton dernier essai » dès la DEUXIÈME erreur, puis
    /// est passé à une autre question à la troisième (1 séance sur 3). Leçon de #29/#45/#46 : seul un fait calculé au tour
    /// même tient. Quand le code juge une réponse FAUSSE (calcul ou suite des nombres), il compte les réponses fausses de
    /// suite à LA MÊME question (même réponse attendue), et dit au professeur à quelle erreur on en est.
    ///
    /// Ne comptent pas : un « je ne sais pas », une question, tout message sans verdict (règle des essais). Le compte
    /// s'arrête à une réponse juste, à une autre question, et à une fin de séance ([RAPPORT]/[FIN_SEANCE] : même frontière
    /// que les essais d'écoute, décision de Min's du 22/11/2026 07h58, voir <c>EssaisRates.FinDeSeance</c>).
    /// Générique : tous les professeurs, tous les calculs et suites jugés par le code. Rien pendant un contrôle noté
    /// (l'appelant ne juge pas alors).
    /// </summary>
    public static partial class CalculAuTableau
    {
        /// <summary>Au-delà, on ne remonte plus : la règle s'arrête à la 4e erreur.</summary>
        private const int ReponsesRemontees = 8;

        /// <summary>
        /// Le rang de l'erreur que l'élève vient de faire sur la question de <paramref name="courant"/> (1 = première), en
        /// relisant l'historique AVANT son message du tour. <paramref name="historique"/> : les messages dans l'ordre, sans
        /// le message en cours. Null si le verdict du tour n'est pas « faux ».
        /// </summary>
        public static int? RangDeLErreur(IReadOnlyList<(string? Role, string? Contenu)> historique, Verdict courant)
        {
            if (courant.Jugement != Jugement.Faux) return null;

            var rang = 1;
            var reponsesLues = 0;
            for (var i = historique.Count - 1; i >= 0 && reponsesLues < ReponsesRemontees; i--)
            {
                var (role, contenu) = historique[i];
                if (role == "assistant")
                {
                    if (Voix.EssaisRates.FinDeSeance(role, contenu)) break;
                    continue;
                }

                if (role != "user" || string.IsNullOrWhiteSpace(contenu)) continue;
                reponsesLues++;

                var avant = new List<string?>();
                for (var j = 0; j < i; j++)
                {
                    var (r, c) = historique[j];
                    if (r == "assistant" && !string.IsNullOrWhiteSpace(c)
                        && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                        && !c.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))
                    {
                        avant.Add(c);
                    }
                }

                var v = Juger(avant.TakeLast(MessagesRemontes), contenu);
                if (v is null) continue;                                    // « je sais pas », question : ne compte pas
                if (!string.Equals(v.Attendu, courant.Attendu, StringComparison.Ordinal)) break;   // une autre question
                if (v.Jugement != Jugement.Faux) break;                     // trouvée (ou à moitié) : le compte repart
                rang++;
            }

            return rang;
        }

        /// <summary>
        /// Le fait à ajouter au verdict faux : à quelle erreur il en est, et ce que la règle demande À CE TOUR. Pas de
        /// chiffre à recopier pour l'élève (« ne lui dis pas combien de fois », règle des essais).
        /// </summary>
        public static string FaitRangDeLErreur(int rang) => rang switch
        {
            1 => "[Fait pour ce tour, compté par l'application : c'est sa PREMIÈRE erreur sur cette question. Ce n'est pas son "
                 + "dernier essai : aide-le par un indice, et ne lui parle pas de « dernier essai ».]",
            2 => "[Fait pour ce tour, compté par l'application : c'est sa DEUXIÈME erreur sur cette question. Ce n'est PAS "
                 + "encore son dernier essai : ne dis surtout pas « dernier essai » à ce tour. Aide-le par un indice, et laisse-le "
                 + "réessayer.]",
            3 => "[Fait pour ce tour, compté par l'application : c'est sa TROISIÈME erreur sur cette question. Reprends avec lui "
                 + "la notion, sans donner la réponse, puis laisse-lui UN DERNIER essai et dis-le-lui clairement : « c'est ton "
                 + "dernier essai ». Ne passe pas encore à une autre question.]",
            _ => "[Fait pour ce tour, compté par l'application : c'est sa QUATRIÈME erreur sur cette question (son dernier essai "
                 + "est passé). Ne t'acharne pas : dis-lui que ce n'est pas grave, passe à la question suivante SANS lui donner "
                 + "la réponse de celle-ci, et propose-lui d'y revenir plus tard s'il le souhaite.]",
        };
    }
}
