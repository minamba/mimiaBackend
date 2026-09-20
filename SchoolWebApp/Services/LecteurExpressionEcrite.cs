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

        /// <summary>
        /// Un mot surligné au tableau : <c>==frend==</c>.
        ///
        /// LA CONSIGNE DIT DE NE PAS LES RECOPIER DANS L'ARCHIVE, ET ÇA NE SUFFIT
        /// PAS. Le professeur reprend son texte depuis le tableau — c'est ce qu'on
        /// lui demande, pour qu'il ne le retape pas de mémoire — et le tableau
        /// porte justement les marques. Une archive à
        /// <c>Last weekend I ==go== to the park</c> se relirait dans six mois
        /// comme des fautes de frappe de l'enfant.
        ///
        /// Même motif que le front (`surlignesTableau.js`) : une seule ligne, pas
        /// de <c>=</c> à l'intérieur — un <c>==</c> orphelin ne mange rien.
        /// </summary>
        [GeneratedRegex(@"==([^=\n]+?)==")]
        private static partial Regex Surligne();

        /// <summary>
        /// UNE EXPRESSION ÉCRITE EST-ELLE EN COURS, pas encore archivée ?
        ///
        /// SERT À LA REDIRE AU PROFESSEUR À CHAQUE TOUR — voir
        /// <c>MarqueurExpressionEcriteEnCours</c>. Camara, le 18/09/2026 : « j'ai
        /// redémarré l'API, fait F5, il m'a remis l'expression écrite, mais j'ai
        /// toujours aucun badge ». Le professeur avait la consigne de surligner,
        /// et il ne l'a pas appliquée : il REPRENAIT un texte d'une séance
        /// précédente, de lui-même, sans qu'aucun envoi de l'élève ne vienne le
        /// lui rappeler. Même patron que <c>LecteurEvaluation.EstOuvert</c>, et
        /// pour la même raison — le serveur sait, la mémoire du modèle non.
        ///
        /// CE QUI L'OUVRE : la question du support, ou la consigne au tableau.
        /// CE QUI LE REFERME : son archivage, ou un autre exercice qui commence.
        ///
        /// LA FIN DE SÉANCE NE LE REFERME PAS, et c'est tout le cas de Camara :
        /// la séance s'était arrêtée en pleine correction, et il la reprenait à
        /// la suivante. Un texte non archivé est un texte qu'on n'a pas fini.
        /// </summary>
        public static bool EstOuvert(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains("[EXPRESSION_ECRITE]", StringComparison.OrdinalIgnoreCase)) return false;

                if (message.Contains("[SUPPORT_ECRIT]", StringComparison.OrdinalIgnoreCase)
                    || ConsigneAuTableau().IsMatch(message))
                {
                    return true;
                }

                if (OuvreAutreExercice(message)) return false;
            }

            return false;
        }

        /// <summary>
        /// « La consigne » en titre, dans un tableau : le texte à écrire ou à corriger.
        ///
        /// LE <c>\r?</c> AVANT <c>$</c> N'EST PAS UNE PRÉCAUTION DE STYLE. En .NET,
        /// <c>$</c> en mode multiligne s'arrête avant <c>\n</c>, pas avant
        /// <c>\r</c>. Mesuré sur le message réel : avec des fins de ligne Windows,
        /// le motif sans lui ne reconnaissait pas le tableau — et le rappel ne
        /// serait jamais parti.
        /// </summary>
        [GeneratedRegex(@"\[ARDOISE\][^\[]*?^[ \t]*la\s+consigne[ \t]*:?[ \t]*\r?$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline)]
        private static partial Regex ConsigneAuTableau();

        /// <summary>Le nom d'une balise technique, quelle qu'elle soit.</summary>
        [GeneratedRegex(@"\[/?([A-Z_]+)(?::[^\]]*)?\]", RegexOptions.IgnoreCase)]
        private static partial Regex NomDeBalise();

        /// <summary>
        /// Ce qui ne dit PAS « on est passé à autre chose » : les répliques, le
        /// tableau, une image — et la fin de séance, qui interrompt un texte sans
        /// l'abandonner.
        /// </summary>
        private static readonly HashSet<string> Neutres = new(StringComparer.OrdinalIgnoreCase)
        {
            "EN", "FR", "ES", "DE", "IT", "ZH",
            "ARDOISE", "TABLEAU_EFFACE", "SCHEMA", "POINTAGE",
            "SUPPORT_ECRIT", "EXPRESSION_ECRITE",
            "FIN_SEANCE", "RAPPORT", "FICHE",
        };

        private static bool OuvreAutreExercice(string message)
        {
            foreach (Match trouvee in NomDeBalise().Matches(message))
            {
                if (!Neutres.Contains(trouvee.Groups[1].Value)) return true;
            }

            return false;
        }

        [GeneratedRegex(@"^(ce qui est r[ée]ussi|[àa] revoir|[àa] r[ée][ée]crire)\s*:?\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex TitreDeSection();

        /// <summary>
        /// LE TEXTE DE L'ÉLÈVE, TEL QU'IL EST AU TABLEAU sous « Ton texte », sans
        /// les surlignés — le dernier tableau qui en porte un. Sert à l'archive
        /// quand le professeur laisse `texte` vide (voulu le 19/09/2026 : il ne
        /// retape plus ce que l'application a déjà) et que l'élève a écrit sur
        /// son cahier — au clavier, c'est son message qui fait foi.
        /// </summary>
        public static string? TexteDuTableau(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                foreach (var bloc in BlocArdoise().Matches(message).Select(m => m.Value).Reverse())
                {
                    if (SectionTonTexte(bloc) is { } section)
                    {
                        return Surligne().Replace(section, "$1").Trim();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// LE TEXTE AVEC SES BADGES, pour l'archive — Camara, le 19/09/2026 : « je
        /// veux les badges dans l'archive aussi ». C'est la section « Ton texte »
        /// du PREMIER tableau surligné de l'exercice en cours — celui qui porte
        /// toutes les marques, avant que le professeur en retire à mesure de la
        /// correction. L'exercice en cours est celui du dernier tableau surligné ;
        /// on reconnaît les tableaux d'un même exercice à leur consigne.
        /// </summary>
        public static string? TexteSurligneDuTableau(IEnumerable<string?> messagesProfesseur)
        {
            var premiers = new Dictionary<string, string>(StringComparer.Ordinal);
            string? courante = null;

            foreach (var message in messagesProfesseur)
            {
                if (string.IsNullOrEmpty(message)) continue;

                foreach (var bloc in BlocArdoise().Matches(message).Select(m => m.Value))
                {
                    if (!bloc.Contains("==", StringComparison.Ordinal)) continue;
                    if (SectionTonTexte(bloc) is not { } section || !section.Contains("==", StringComparison.Ordinal)) continue;

                    var consigne = ConsigneDe(bloc) ?? string.Empty;
                    premiers.TryAdd(consigne, section.Trim());
                    courante = consigne;
                }
            }

            return courante is null ? null : premiers[courante];
        }

        /// <summary>La section « Ton texte » d'un tableau, marques comprises, ou null.</summary>
        private static string? SectionTonTexte(string bloc)
        {
            var lignes = bloc.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            var debut = lignes.FindIndex(l => TitreTexte().IsMatch(l.Trim()));
            if (debut < 0) return null;

            var corps = new List<string>();
            for (var i = debut + 1; i < lignes.Count; i++)
            {
                var l = lignes[i];
                if (TitreDeSection().IsMatch(l.Trim()) || l.TrimStart().StartsWith("[/", StringComparison.Ordinal)) break;
                corps.Add(l);
            }

            var texte = string.Join('\n', corps).Trim();
            return texte.Length == 0 ? null : texte;
        }

        // ------------------------------------------------------------------
        // Le tableau de correction, écrit une seule fois
        // ------------------------------------------------------------------

        [GeneratedRegex(@"\[ARDOISE\][\s\S]*?\[/ARDOISE\]", RegexOptions.IgnoreCase)]
        private static partial Regex BlocArdoise();

        [GeneratedRegex(@"^la\s+consigne\s*:?$", RegexOptions.IgnoreCase)]
        private static partial Regex TitreConsigne();

        [GeneratedRegex(@"^ton\s+texte", RegexOptions.IgnoreCase)]
        private static partial Regex TitreTexte();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Blancs();

        /// <summary>
        /// La consigne d'un tableau, normalisée — la clé qui dit « c'est le même
        /// exercice ». Null si le tableau n'en porte pas.
        /// </summary>
        private static string? ConsigneDe(string bloc)
        {
            var lignes = bloc.Split('\n').Select(l => l.Trim()).ToList();
            var debut = lignes.FindIndex(l => TitreConsigne().IsMatch(l));
            if (debut < 0) return null;

            var corps = new List<string>();
            for (var i = debut + 1; i < lignes.Count; i++)
            {
                var l = lignes[i];
                if (l.Length == 0 || TitreTexte().IsMatch(l) || l.StartsWith("[/", StringComparison.Ordinal)) break;
                corps.Add(l);
            }

            var texte = Blancs().Replace(Surligne().Replace(string.Join(' ', corps), "$1"), " ").Trim();
            return texte.Length == 0 ? null : texte;
        }

        /// <summary>
        /// La consigne du tableau de correction déjà écrit dans la séance — le
        /// dernier tableau qui porte une consigne ET des surlignés —, ou null.
        /// </summary>
        private static string? ConsigneFigee(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains("[EXPRESSION_ECRITE]", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal)
                    || message.Contains("[FIN_SEANCE]", StringComparison.Ordinal)
                    || OuvreAutreExercice(message))
                {
                    return null;
                }

                foreach (var bloc in BlocArdoise().Matches(message).Select(m => m.Value).Reverse())
                {
                    if (bloc.Contains("==", StringComparison.Ordinal) && ConsigneDe(bloc) is { } consigne)
                    {
                        return consigne;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// LE TABLEAU DE CORRECTION NE SE RÉÉCRIT PAS — Camara, le 19/09/2026 :
        /// « on a déjà les badges pour dire à l'élève où regarder ».
        ///
        /// Relevé le jour même : six réécritures en une correction, malgré la
        /// consigne et malgré le rappel joint à chaque tour — le professeur
        /// corrigeait le texte lui-même à chaque faute, et une fois a tout
        /// réécrit sans surligné. Retiré ICI, de ce qui est persisté : l'écran
        /// garde le premier tableau avec ses badges, et l'historique ne relit
        /// pas six copies du même texte. La parole du message reste entière.
        /// </summary>
        /// <param name="retire">Vrai si un tableau a été retiré.</param>
        public static string SansTableauReecrit(
            string? contenu, IEnumerable<string?> messagesProfesseur, out bool retire)
        {
            retire = false;
            if (string.IsNullOrEmpty(contenu)
                || !contenu.Contains("[ARDOISE]", StringComparison.OrdinalIgnoreCase))
            {
                return contenu ?? string.Empty;
            }

            var figee = ConsigneFigee(messagesProfesseur);
            if (figee is null) return contenu;

            var supprime = false;
            var resultat = BlocArdoise().Replace(contenu, m =>
            {
                if (ConsigneDe(m.Value) != figee) return m.Value;
                supprime = true;
                return string.Empty;
            });

            retire = supprime;
            return supprime ? Regex.Replace(resultat, @"\n{3,}", "\n\n").Trim() : contenu;
        }

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
                    case "texte": texte = Surligne().Replace(valeur, "$1"); break;
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
            // ET LE TEXTE NON PLUS, depuis le 19/09/2026 : vide, il est repris
            // tel que l'élève l'a tapé, ou tel que le professeur l'a mis au
            // tableau sous « Ton texte » — voir EnregistrerExpressionEcriteAsync.

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
