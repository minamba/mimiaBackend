namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// TROIS ESSAIS, PAS PLUS — Camara, le 07/11/2026 : « quand un enfant n'arrive pas à dire une syllabe ou un mot, au
    /// bout de 3 essais il faut passer à la suivante, en lui disant que c'est pas grave et qu'on reviendra dessus un peu
    /// plus tard ; faut pas laisser l'enfant essayer à l'infini, il va finir frustré ».
    ///
    /// COMPTÉ PAR LE CODE, PAS PAR LE PROFESSEUR (une règle de prompt ne vaut pas un fait) : les réponses JUGÉES fausses
    /// d'affilée sur le même son, celui de la dernière balise de voix du professeur. Une demande sans verdict (« tu peux
    /// répéter ? ») ne compte pas et ne remet pas à zéro ; une bonne réponse ou un autre son, si. Écoute et prononciation,
    /// français et anglais : partout où le serveur juge un son.
    ///
    /// ET LE FILET QUI NE DÉPEND D'AUCUN VERDICT (08/11/2026) : voir MontreSansFin. La même image montrée 3 fois, le même
    /// son dicté 5 fois : on passe au suivant, même si rien n'a été jugé.
    ///
    /// LE COMPTE REPART DE ZÉRO QUAND L'ENFANT REVIENT — décision de Min's (22/11/2026, 07 h 58) : après « Quitter » puis
    /// un retour (nouvelle séance, relance), le premier essai raté sur le même mot est le PREMIER, plus le troisième. Avant,
    /// le compte suivait la conversation. La frontière : le message de fin de séance du professeur (<see cref="FinDeSeance"/>),
    /// écrit au départ par le worker ([FICHE]…[RAPPORT]…[FIN_SEANCE]) ou par le professeur lui-même (au revoir, minuteur).
    /// Il peut arriver APRÈS l'accueil du retour (course de 1 à 10 s) : où qu'il soit entre deux essais, il les sépare. Les
    /// deux compteurs (essais jugés faux, filet <see cref="MontreSansFin"/>) et celui de la répétition en langue
    /// (<c>RepetitionEnLangue.EssaisDeSuite</c>) s'arrêtent à cette frontière. Un rechargement de page écrit le même
    /// message de départ que « Quitter » (le serveur ne les distingue pas dans l'historique) : il remet aussi à zéro.
    /// </summary>
    public static class EssaisRates
    {
        public const int Maximum = 3;

        /// <summary>
        /// Un message du professeur qui CLÔT une séance : il porte [RAPPORT] ou [FIN_SEANCE], où que ce soit (le message
        /// technique de départ, l'au revoir, la conclusion du minuteur). Entre deux essais, il les sépare : le compte repart
        /// de zéro (décision de Min's, 22/11/2026).
        /// </summary>
        public static bool FinDeSeance(string? role, string? contenu) =>
            role == "assistant" && !string.IsNullOrEmpty(contenu)
            && (contenu.Contains("[FIN_SEANCE]", StringComparison.OrdinalIgnoreCase)
                || contenu.Contains("[RAPPORT]", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// LE MÊME SON DICTÉ TANT DE FOIS DE SUITE, à l'écoute ou à la prononciation : plus large qu'en lecture, car un enfant
        /// peut y demander de répéter sans que ce soit un essai. Mais jamais sans fin.
        /// </summary>
        public const int DicteMaximum = 5;

        public sealed record Abandon(string Son, int Essais);

        /// <summary>
        /// Le son à abandonner à ce tour, ou null. `historique` : du plus ancien au plus récent, SANS le message de ce
        /// tour, qui est `sonJuste`.
        /// </summary>
        public static Abandon? Lire(IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> historique, bool? sonJuste, string? contenu = null)
        {
            // JAMAIS BLOQUÉ, QUELLE QU'EN SOIT LA RAISON — Camara, le 08/11/2026 : « certes il y avait un bug, mais le fait que
            // l'enfant reste bloqué, c'est pire ». Compter les seuls verdicts « faux » laissait passer tout ce qui n'a pas de
            // verdict : un bug de jugement, un micro qui écrit une phrase, l'interrupteur éteint. On compte donc aussi ce que
            // l'enfant a sous les yeux : la même image trois fois, c'est trois essais, et on passe, sauf bonne réponse à ce
            // tour ou demande d'un autre exercice.
            if (sonJuste != true && !LectureDImage.ChangeDExercice(contenu) && MontreSansFin(historique) is { } bloque) return bloque;

            // LE MOT ENTIER LU AU LIEU DE LA SYLLABE N'EST PAS UNE ERREUR, MAIS C'EST UN ESSAI (08/11/2026) : sans verdict,
            // il n'était jamais compté, et l'enfant restait sur la même image sans fin.
            if (sonJuste != false && !LectureDImage.EstMotEntier(contenu)) return null;

            var son = SonDemande(historique, historique.Count);
            if (son is null) return null;

            var essais = 1;
            for (var i = historique.Count - 1; i >= 0 && essais < Maximum; i--)
            {
                var (role, dit, juste) = historique[i];
                // L'enfant est parti puis revenu entre cet essai et le suivant : le compte repart de zéro (Min's, 22/11/2026).
                if (FinDeSeance(role, dit)) break;
                if (role != "user") continue;
                if (juste is null && LectureDImage.EstMotEntier(dit)) juste = false;
                if (juste is null) continue;
                if (juste == true) break;

                var avant = SonDemande(historique, i);
                if (avant is null || !MemeSon(avant, son)) break;
                essais++;
            }

            return essais >= Maximum ? new Abandon(son, essais) : null;
        }

        /// <summary>Le même son redemandé par les derniers messages du professeur, s'il l'a été trop de fois de suite ; sinon null.</summary>
        private static Abandon? MontreSansFin(IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> historique)
        {
            string? son = null;
            var limite = DicteMaximum;
            var fois = 0;

            for (var i = historique.Count - 1; i >= 0; i--)
            {
                var (role, dit, juste) = historique[i];
                // Ce qui a été montré AVANT un départ ne compte plus au retour (Min's, 22/11/2026) : sinon, l'image montrée
                // deux fois avant « Quitter » et remontrée à l'accueil serait abandonnée dès le premier essai du retour.
                if (FinDeSeance(role, dit)) break;
                if (role == "user")
                {
                    if (juste == true) break;
                    continue;
                }

                if (role != "assistant" || ImageAuTableau.EstTechnique(dit)) continue;

                var ce = SonDuMessage(dit);
                if (ce is null || (son is not null && !MemeSon(ce, son))) break;
                if (son is null)
                {
                    son = ce;
                    if (LectureDImage.Affiche(dit) is not null) limite = Maximum;
                }

                fois++;
            }

            return son is not null && fois >= limite ? new Abandon(son, fois) : null;
        }

        /// <summary>
        /// `francais` : son dernier essai était la TRADUCTION française du mot entendu (« deux » pour two, contrôle d'Omar sur le
        /// #57 v2, 21/11/2026) ; avant de passer, le professeur réexplique ce qui était attendu (règle des trois essais :
        /// réexplication, puis passage), sans redire le mot abandonné.
        /// </summary>
        public static string Fait(Abandon abandon, bool syllabe, string? francais = null, string langue = "anglais") =>
            $"[Fait pour ce tour, qui l'emporte sur tous les faits précédents : c'est son {abandon.Essais}e essai raté sur « {abandon.Son} ». "
            + "NE LE LUI REDEMANDE PAS. "
            // Au bout en bout, « réexplique-lui d'abord » seul était fondu dans « Pas de souci ! » : la phrase attendue est
            // donnée telle quelle (le modèle recopie ce qu'on lui donne — ici, c'est voulu).
            + (francais is null ? string.Empty
                : $"Il a répondu « {francais} », le mot en FRANÇAIS : ne dis pas que c'est juste. Ta PREMIÈRE phrase le lui réexplique, "
                  + $"sans le gronder, par exemple : « {Majuscule(francais)}, c'est le sens du mot en français ; ici, il fallait redire le mot "
                  + $"{langue} que tu as entendu. » Ensuite seulement, ")
            + (francais is null ? "Dis-lui" : "dis-lui")
            + ", chaleureusement et en une phrase, que ce n'est pas grave, que vous y reviendrez un peu plus "
            + $"tard, puis passe {(syllabe ? "à la syllabe suivante" : "au mot suivant")}, une autre que « {abandon.Son} ». Ne lui dis pas combien d'essais il a faits, "
            + $"et n'écris pas « {abandon.Son} » dans ta phrase : ta voix le lirait mal."
            + (francais is null ? string.Empty : RepetitionEnLangue.SansFelicitation()) + "]";

        private static string Majuscule(string mot) =>
            string.IsNullOrEmpty(mot) ? mot : char.ToUpperInvariant(mot[0]) + mot[1..];

        /// <summary>Le son du dernier message du professeur avant l'index `avant`.</summary>
        private static string? SonDemande(IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> historique, int avant)
        {
            for (var i = avant - 1; i >= 0; i--)
            {
                if (historique[i].Role != "assistant" || ImageAuTableau.EstTechnique(historique[i].Contenu)) continue;
                return SonDuMessage(historique[i].Contenu);
            }
            return null;
        }

        /// <summary>Le son qu'un message du professeur demande : l'image à lire, sa balise de voix, ou un mot seul au tableau ; sinon null.</summary>
        private static string? SonDuMessage(string? contenu)
        {
            // LA LECTURE EN IMAGES (07/11/2026) : ce que l'image lui fait lire.
            if (LectureDImage.Affiche(contenu) is { } aLire) return aLire.Cible;
            if (DicteeDeMots.MotDit(contenu) is { } son) return son;

            // LA PRONONCIATION SE DEMANDE SOUVENT AU TABLEAU (« lis la syllabe au tableau ») : son dernier bloc d'un mot.
            var tableau = System.Text.RegularExpressions.Regex.Matches(contenu ?? string.Empty, @"\[ARDOISE\]([\s\S]*?)\[/ARDOISE\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (tableau.Count > 0 && tableau[^1].Groups[1].Value.Trim() is { Length: > 0 } ecrit && !ecrit.Any(char.IsWhiteSpace)) return ecrit.Trim('.', '!', '?');
            return null;
        }

        /// <summary>« bé » et « be » sont deux sons : on compare l'écriture, accents compris.</summary>
        private static bool MemeSon(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
