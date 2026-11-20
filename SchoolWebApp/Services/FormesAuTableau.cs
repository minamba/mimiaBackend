using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LES FORMES GÉOMÉTRIQUES, MONTRÉES ET JUGÉES AVEC L'AIDE DU CODE — Minamba, le 20/11/2026, signalement #48 (Adam,
    /// jeu CP / Maths). « Les formes : carré, triangle, rond » : Nora écrit les trois MOTS au tableau, aucun dessin, et
    /// demande « combien de côtés a un carré ? ». L'élève répond « 3 » ; elle répond « Regarde bien l'image de la forme »
    /// (il n'y en a pas), puis « Un carré a 4 côtés […] Compte avec moi : 1, 2, 3, 4 » : la réponse, dès la 1re erreur.
    ///
    /// TROIS DÉFAUTS, TROIS FAITS DU TOUR (une règle de prompt ne vaut pas un fait) :
    /// 1. LE TABLEAU. Le code sait s'il porte un dessin (bloc [FIGURE], SVG ou planche) : s'il n'en porte pas, il le dit,
    ///    interdit de parler d'une image, et donne les lignes [FIGURE] exactes que l'écran sait tracer.
    /// 2. LE VERDICT. « Combien de côtés / sommets / coins / angles (droits) a un carré ? » : la réponse attendue est
    ///    connue (polygones usuels seulement ; le rond n'est jamais jugé, « un côté courbe » se dit aussi en classe).
    /// 3. LES ESSAIS. Même règle que la lecture des nombres (#28) et les sons (Camara, 07/11/2026) : 3 essais, pas plus,
    ///    comptés par le code ; avant le 3e, la réponse n'est NI dite NI comptée à sa place ; au 3e, on passe sans elle.
    ///
    /// Quand la question ou la réponse ne se lisent pas sans ambiguïté, pas de verdict : seulement le fait du tableau.
    /// </summary>
    public static partial class FormesAuTableau
    {
        public const int EssaisMaximum = 3;

        /// <summary>Combien de messages du professeur une relance (« Combien ça fait, en tout ? ») peut remonter.</summary>
        private const int Remontee = 3;

        public enum Propriete { Cotes, Sommets, Coins, Angles, AnglesDroits }

        /// <summary>La question en cours. <paramref name="Attendu"/> est null quand le code ne la juge pas (le rond).</summary>
        public sealed record Question(string Forme, Propriete Propriete, int? Attendu);

        /// <summary>
        /// Ce que le code sait à ce tour. <paramref name="Question"/> null : l'élève parle de formes, sans question en
        /// cours. <paramref name="Donne"/> null : pas de nombre lisible dans sa réponse (ce n'est pas un essai).
        /// </summary>
        public sealed record Tour(Question? Question, int? Donne, bool? Juste, int Essai, bool FigureAuTableau);

        private sealed record Forme(string Nom, int? Cotes, int? AnglesDroits, string? Trace);

        private static readonly Dictionary<string, Forme> Formes = new(StringComparer.Ordinal)
        {
            ["carre"] = new("carré", 4, 4, "carré ABCD de côté 3"),
            ["rectangle"] = new("rectangle", 4, 4, "rectangle ABCD de longueur 5 et de largeur 3"),
            ["triangle"] = new("triangle", 3, null, "triangle ABC équilatéral"),
            ["losange"] = new("losange", 4, null, null),
            ["quadrilatere"] = new("quadrilatère", 4, null, null),
            ["pentagone"] = new("pentagone", 5, null, null),
            ["hexagone"] = new("hexagone", 6, null, null),
            ["octogone"] = new("octogone", 8, null, null),
            ["rond"] = new("rond", null, null, "cercle de centre O de rayon 2 cm"),
            ["cercle"] = new("cercle", null, null, "cercle de centre O de rayon 2 cm"),
            ["disque"] = new("disque", null, null, "cercle de centre O de rayon 2 cm"),
        };

        private const string MotsFormes = "carre|rectangle|triangle|losange|quadrilatere|pentagone|hexagone|octogone|rond|cercle|disque";

        // Texte normalisé : minuscules, sans accents, apostrophes droites.
        [GeneratedRegex(@"combien\s+(?:(?:a-t-il|a-t-elle|a t il|a t elle|il a|elle a|y a-t-il|y a t il|il y a|tu vois|vois-tu|vois tu|tu comptes|comptes-tu)\s+)?(?:de\s+|d')(?<p>cotes?|sommets?|coins?|angles?\s+droits?|angles?)\b")]
        private static partial Regex QuestionPropriete();

        [GeneratedRegex(@"combien\s+en\s+(?:a|as|a-t-il|a-t-elle|a t il|compte|comptes|vois)\b")]
        private static partial Regex QuestionEnA();

        [GeneratedRegex(@"(?<!\p{L})(?<f>" + MotsFormes + @")(?<pl>s)?(?!\p{L})")]
        private static partial Regex MotForme();

        [GeneratedRegex(@"(?:\d+|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|plusieurs|des|les)\s+$")]
        private static partial Regex PlurielAvant();

        [GeneratedRegex(@"\[ARDOISE\]([\s\S]*?)(?:\[/ARDOISE\]|$)", RegexOptions.IgnoreCase)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"\[FIGURE\]([\s\S]*?)(?:\[/FIGURE\]|$)", RegexOptions.IgnoreCase)]
        private static partial Regex Figure();

        [GeneratedRegex(@"\[[^\]]*\]")]
        private static partial Regex Crochets();

        [GeneratedRegex(@"combien\s+(?:de\s+|d')(?<x>\p{L}+)")]
        private static partial Regex CombienDe();

        [GeneratedRegex(@"\d\s*[+\-x×*:÷=]\s*\d")]
        private static partial Regex Calcul();

        [GeneratedRegex(@"(?<!\p{L})(?:combien|compte|comptes|recompte|essaie|essaye|encore|en tout|ca fait|tu es sur|es-tu sur|regarde|nombre de|cotes?|sommets?|coins?|angles?|" + MotsFormes + @")(?!\p{L})")]
        private static partial Regex MotDeRelance();

        [GeneratedRegex(@"^\W*(?:oui|bravo|exactement|exact|super|parfait|tres bien|bien joue|c'est ca|c'est juste|c'est bien ca|felicitations|genial)(?!\p{L})")]
        private static partial Regex Felicitation();

        [GeneratedRegex(@"(?<!\p{L})(?<n>\d+|zero|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|une?(?!\s+\p{L}))(?!\p{L})")]
        private static partial Regex Nombre();

        [GeneratedRegex(@"(?<!\p{L})(?:aucune?|pas de (?:cotes?|sommets?|coins?|angles?)|n'en a pas|n'en a aucune?|il en a pas)(?!\p{L})")]
        private static partial Regex Zero();

        private static readonly Dictionary<string, int> NombresEnLettres = new(StringComparer.Ordinal)
        {
            ["zero"] = 0, ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6,
            ["sept"] = 7, ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11, ["douze"] = 12,
        };

        /// <summary>
        /// Le fait de ce tour, ou null. <paramref name="fil"/> : du plus ancien au plus récent, SANS le message de ce tour
        /// (<paramref name="contenuEleve"/>), les messages techniques du professeur ([FICHE], [RAPPORT]) déjà retirés.
        /// </summary>
        public static Tour? Lire(IReadOnlyList<(string Role, string? Contenu)> fil, string? contenuEleve)
        {
            var figure = FigureAuTableau(fil, out var formesTracees);
            var dernierProf = DernierProf(fil, fil.Count);
            var question = dernierProf < 0 ? null : QuestionDe(fil, dernierProf, formesTracees, 0);

            if (question is null)
            {
                return ParleDeFormes(contenuEleve) && !figure ? new Tour(null, null, null, 0, false) : null;
            }

            var donne = LireReponse(contenuEleve);
            bool? juste = question.Attendu is { } attendu && donne is { } d ? d == attendu : null;
            var essai = juste == false ? 1 + EssaisRatesAvant(fil, question, formesTracees) : 0;

            return new Tour(question, donne, juste, essai, figure);
        }

        /// <summary>Le fait à joindre au tour de l'élève.</summary>
        public static string Fait(Tour tour)
        {
            var sb = new StringBuilder("[Fait pour ce tour, vérifié par l'application, qui l'emporte sur tes souvenirs — LES FORMES. ");

            if (tour.Question is { } q)
            {
                var forme = Formes[q.Forme];
                var prop = Nom(q.Propriete);
                sb.Append($"Ta question en cours : « combien de {prop} a un {forme.Nom} ? ». ");

                if (tour.Juste == true)
                {
                    sb.Append($"Sa réponse, {tour.Donne}, est JUSTE : un {forme.Nom} a bien {q.Attendu} {prop}. Dis-le franchement "
                        + "(« oui, c'est ça »), sans « presque » ni « tu es sûr ? ». ");
                }
                else if (tour.Juste == false && tour.Essai >= EssaisMaximum)
                {
                    sb.Append($"Sa réponse, {tour.Donne}, est FAUSSE, et c'est son {tour.Essai}e essai sans y arriver. NE LA LUI REPOSE PAS. "
                        + "Dis-lui chaleureusement, en une phrase, que ce n'est pas grave et que vous y reviendrez un peu plus tard, "
                        + "puis passe à une autre forme ou à une autre question. Ne lui dis pas combien d'essais il a faits. ");
                    sb.Append(PasLaReponse(q, forme));
                }
                else if (tour.Juste == false)
                {
                    sb.Append($"Sa réponse, {tour.Donne}, est FAUSSE. C'est son essai n° {tour.Essai} sur {EssaisMaximum} : il a encore le "
                        + "droit d'essayer. Dis-lui gentiment que ce n'est pas ça, donne-lui un indice de MÉTHODE — suivre le bord de "
                        + $"la forme avec le doigt et compter chaque {Singulier(q.Propriete)} une seule fois, en partant d'un {Depart(q.Propriete)} "
                        + "qu'on retient — puis repose-lui la même question. ");
                    sb.Append(PasLaReponse(q, forme));
                }
                else if (tour.Donne is null)
                {
                    sb.Append("Il n'a pas donné de nombre : ce n'est pas un essai. Aide-le à compter LUI-MÊME sur le dessin, sans "
                        + "donner la réponse. ");
                    if (q.Attendu is not null) sb.Append(PasLaReponse(q, forme));
                }

                // Au 3e essai on passe : le dessin de CETTE forme n'est plus utile.
                var aMontrer = !(tour.Juste == false && tour.Essai >= EssaisMaximum) && tour.Juste != true;
                if (!tour.FigureAuTableau && aMontrer) sb.Append(TableauSansDessin(forme));
                else if (!tour.FigureAuTableau) sb.Append(TableauSansDessin(null));
            }
            else
            {
                sb.Append("Il parle de formes géométriques. ").Append(TableauSansDessin(null));
            }

            return sb.ToString().TrimEnd() + "]";
        }

        /// <summary>
        /// Le professeur a-t-il donné la réponse trop tôt ? Pour le journal seulement (grep « Reponse de forme donnee trop
        /// tot ») : on ne réécrit pas sa réponse.
        /// </summary>
        public static bool DonneeTropTot(Tour tour, string? reponse)
        {
            if (tour.Question is not { Attendu: { } attendu } q || tour.Juste == true || string.IsNullOrWhiteSpace(reponse)) return false;

            var texte = Normaliser(Ardoise().Replace(reponse, " "));
            var motNombre = NombresEnLettres.FirstOrDefault(kv => kv.Value == attendu && kv.Key != "une").Key;
            var nombre = motNombre is null ? $"{attendu}" : $"(?:{attendu}|{motNombre})";
            var prop = q.Propriete switch
            {
                Propriete.Cotes => "cotes",
                Propriete.Sommets => "sommets",
                Propriete.Coins => "coins",
                Propriete.Angles => "angles",
                _ => @"angles\s+droits",
            };
            if (Regex.IsMatch(texte, $@"(?<![\p{{L}}\d]){nombre}\s+{prop}(?!\p{{L}})")) return true;

            // « Compte avec moi : 1, 2, 3, 4. » : compter jusqu'au bout, c'est la donner.
            var suite = string.Join(@"\W+", Enumerable.Range(1, attendu).Select(i => $"(?:{i}|{NombresEnLettres.First(kv => kv.Value == i).Key})"));
            return attendu >= 2 && Regex.IsMatch(texte, $@"(?<![\p{{L}}\d]){suite}(?![\p{{L}}\d])");
        }

        private static string PasLaReponse(Question q, Forme forme) =>
            $"NE DONNE PAS LA RÉPONSE : n'écris pas et ne dis pas le bon nombre, ni « un {forme.Nom} a … {Nom(q.Propriete)} », et "
            + "ne compte pas à sa place jusqu'au bout (pas de « 1, 2, 3… » qui finit sur la réponse). ";

        private static string TableauSansDessin(Forme? forme)
        {
            var trace = forme?.Trace is { } t
                ? $"Dans CE message, trace le {forme.Nom} : [ARDOISE] [FIGURE] {t} [/FIGURE] [/ARDOISE] (une ligne par balise, "
                  + "c'est l'écran qui dessine ; les lettres nomment les coins, n'en parle pas à un élève de primaire). "
                : "Une forme se MONTRE : trace au tableau celle dont tu parles, UNE à la fois, dans [ARDOISE] [FIGURE] … [/FIGURE] "
                  + "[/ARDOISE], avec une de ces lignes exactes : « carré ABCD de côté 3 », « rectangle ABCD de longueur 5 et de "
                  + "largeur 3 », « triangle ABC équilatéral », « cercle de centre O de rayon 2 cm ». Une autre forme ne se trace "
                  + "pas : décris-la en mots. ";

            return "LE TABLEAU NE MONTRE AUCUN DESSIN DE FORME (au mieux des mots). Ne lui parle donc d'aucune image, d'aucun "
                + "dessin, d'aucune forme « au tableau » ou « à l'écran » tant que tu ne l'as pas tracée : il ne la voit pas. " + trace;
        }

        /// <summary>L'élève parle-t-il de formes géométriques ?</summary>
        private static bool ParleDeFormes(string? contenu)
        {
            if (string.IsNullOrWhiteSpace(contenu)) return false;
            var texte = Normaliser(Crochets().Replace(contenu, " "));
            return Regex.IsMatch(texte, @"(?<!\p{L})(?:formes?|geometri\p{L}*|carres?|triangles?|ronds?|cercles?|rectangles?|losanges?)(?!\p{L})");
        }

        /// <summary>
        /// Le tableau porte-t-il un dessin ? Même lecture que l'écran (et qu'`EtatDuTableau`) : le dernier bloc [ARDOISE]
        /// gagne, un effacement avant lui veut dire vide. <paramref name="formes"/> : les formes tracées par [FIGURE].
        /// </summary>
        private static bool FigureAuTableau(IReadOnlyList<(string Role, string? Contenu)> fil, out List<string> formes)
        {
            formes = [];
            for (var i = fil.Count - 1; i >= 0; i--)
            {
                if (fil[i].Role != "assistant") continue;
                var texte = fil[i].Contenu ?? string.Empty;
                var ardoises = Ardoise().Matches(texte);
                if (ardoises.Count > 0)
                {
                    var corps = ardoises[^1].Groups[1].Value;
                    foreach (Match f in Figure().Matches(corps))
                    {
                        foreach (var ligne in f.Groups[1].Value.Split('\n'))
                        {
                            var l = Normaliser(ligne).Trim();
                            var m = Regex.Match(l, @"^(carre|rectangle|triangle|cercle|polygone|quadrilatere)(?!\p{L})");
                            if (m.Success) formes.Add(m.Groups[1].Value);
                        }
                    }
                    return Figure().IsMatch(corps)
                        || corps.Contains("<svg", StringComparison.OrdinalIgnoreCase)
                        || corps.TrimStart().StartsWith("SCHEMA:", StringComparison.OrdinalIgnoreCase);
                }
                if (texte.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal)) return false;
            }
            return false;
        }

        private static int DernierProf(IReadOnlyList<(string Role, string? Contenu)> fil, int avant)
        {
            for (var i = Math.Min(avant, fil.Count) - 1; i >= 0; i--)
            {
                if (fil[i].Role == "assistant" && !string.IsNullOrWhiteSpace(fil[i].Contenu)) return i;
            }
            return -1;
        }

        /// <summary>La question que pose le message du professeur à l'index <paramref name="i"/>, relances comprises.</summary>
        private static Question? QuestionDe(IReadOnlyList<(string Role, string? Contenu)> fil, int i, List<string> formesTracees, int profondeur)
        {
            var texte = Normaliser(Ardoise().Replace(fil[i].Contenu ?? string.Empty, " "));
            var precedent = profondeur < Remontee && DernierProf(fil, i) is var p && p >= 0 ? p : -1;

            // La dernière question de propriété du message (« combien de côtés… »), ou « combien en a… » (propriété héritée).
            var propriete = QuestionPropriete().Matches(texte).LastOrDefault();
            var enA = QuestionEnA().Matches(texte).LastOrDefault();

            if (propriete is not null && (enA is null || propriete.Index > enA.Index))
            {
                var prop = LirePropriete(propriete.Groups["p"].Value);
                var forme = FormeAutour(texte, propriete.Index, propriete.Index + propriete.Length, formesTracees, out var pluriel);
                if (pluriel) return null;
                return forme is null ? null : Construire(forme, prop);
            }

            if (enA is not null)
            {
                var heritee = precedent >= 0 ? QuestionDe(fil, precedent, formesTracees, profondeur + 1) : null;
                var forme = FormeAutour(texte, enA.Index, enA.Index + enA.Length, formesTracees, out var pluriel);
                if (heritee is null || forme is null || pluriel) return null;
                return Construire(forme, heritee.Propriete);
            }

            // UNE RELANCE SANS NOUVELLE QUESTION (« Compte avec moi. Combien ça fait, en tout ? ») garde la question d'avant.
            return precedent >= 0 && EstRelance(texte) ? QuestionDe(fil, precedent, formesTracees, profondeur + 1) : null;
        }

        private static bool EstRelance(string texte)
        {
            if (!texte.Contains('?') || Felicitation().IsMatch(texte) || Calcul().IsMatch(texte)) return false;

            // « Combien de pommes… ? » : une autre question.
            foreach (Match m in CombienDe().Matches(texte))
            {
                if (!Regex.IsMatch(m.Groups["x"].Value, @"^(?:cotes?|sommets?|coins?|angles?)$")) return false;
            }

            // Une nouvelle activité annoncée n'est pas une relance.
            if (Regex.IsMatch(texte, @"(?<!\p{L})(?:on passe|passons|autre (?:forme|question|exercice)|maintenant)(?!\p{L})")) return false;

            return MotDeRelance().IsMatch(texte);
        }

        /// <summary>
        /// La forme visée par une question située entre <paramref name="debut"/> et <paramref name="fin"/> : la première
        /// nommée après dans la même phrase, sinon la dernière nommée avant (même phrase ou la précédente), sinon la seule
        /// forme tracée au tableau.
        /// </summary>
        private static string? FormeAutour(string texte, int debut, int fin, List<string> formesTracees, out bool pluriel)
        {
            pluriel = false;
            var finPhrase = texte.IndexOfAny(['?', '.', '!', '\n'], fin);
            var apres = texte[fin..(finPhrase < 0 ? texte.Length : finPhrase)];
            var m = MotForme().Match(apres);
            if (m.Success)
            {
                pluriel = m.Groups["pl"].Success || PlurielAvant().IsMatch(apres[..m.Index]);
                return m.Groups["f"].Value;
            }

            // Avant : la phrase de la question et celle d'avant.
            var coupures = 0;
            var depart = debut;
            while (depart > 0 && coupures < 2)
            {
                depart--;
                if (texte[depart] is '?' or '.' or '!' or '\n') coupures++;
            }
            var avant = texte[depart..debut];
            var dernier = MotForme().Matches(avant).LastOrDefault();
            if (dernier is not null)
            {
                pluriel = dernier.Groups["pl"].Success || PlurielAvant().IsMatch(avant[..dernier.Index]);
                return dernier.Groups["f"].Value;
            }

            var traces = formesTracees.Distinct().ToList();
            return traces.Count == 1 && Formes.ContainsKey(traces[0]) ? traces[0] : null;
        }

        private static Question? Construire(string cle, Propriete prop)
        {
            if (!Formes.TryGetValue(cle, out var forme)) return null;
            int? attendu = prop == Propriete.AnglesDroits ? forme.AnglesDroits : forme.Cotes;
            return new Question(cle, prop, attendu);
        }

        /// <summary>Les réponses fausses d'affilée à la même question, avant ce tour. Une réponse sans nombre ne compte pas et ne remet pas à zéro.</summary>
        private static int EssaisRatesAvant(IReadOnlyList<(string Role, string? Contenu)> fil, Question question, List<string> formesTracees)
        {
            var rates = 0;
            for (var k = fil.Count - 1; k >= 0 && rates < EssaisMaximum - 1; k--)
            {
                if (fil[k].Role != "user") continue;
                var prof = DernierProf(fil, k);
                if (prof < 0) break;
                var avant = QuestionDe(fil, prof, formesTracees, 0);
                if (avant is null || avant.Forme != question.Forme || avant.Propriete != question.Propriete) break;

                var donne = LireReponse(fil[k].Contenu);
                if (donne is null) continue;
                if (donne == question.Attendu) break;
                rates++;
            }
            return rates;
        }

        /// <summary>
        /// Le nombre que donne l'élève, ou null. Une suite « 1, 2, 3, 4 » vaut son dernier nombre ; deux nombres différents
        /// (« 3 ou 4 ») ne se jugent pas. « un carré » n'est pas un nombre, « il en a un » si.
        /// </summary>
        public static int? LireReponse(string? contenu)
        {
            if (string.IsNullOrWhiteSpace(contenu)) return null;
            var texte = Normaliser(Crochets().Replace(contenu, " "));

            var nombres = Nombre().Matches(texte)
                .Select(m => int.TryParse(m.Groups["n"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var v)
                    ? v
                    : NombresEnLettres[m.Groups["n"].Value])
                .ToList();

            if (nombres.Count == 0) return Zero().IsMatch(texte) ? 0 : null;
            if (nombres.Distinct().Count() == 1) return nombres[0];

            // « 1, 2, 3, 4 » : il a compté.
            var compte = nombres[0] == 1 && nombres.Select((n, i) => n == i + 1).All(x => x);
            return compte ? nombres[^1] : null;
        }

        private static Propriete LirePropriete(string mot) => mot switch
        {
            _ when mot.StartsWith("cote", StringComparison.Ordinal) => Propriete.Cotes,
            _ when mot.StartsWith("sommet", StringComparison.Ordinal) => Propriete.Sommets,
            _ when mot.StartsWith("coin", StringComparison.Ordinal) => Propriete.Coins,
            _ when mot.Contains("droit", StringComparison.Ordinal) => Propriete.AnglesDroits,
            _ => Propriete.Angles,
        };

        private static string Nom(Propriete p) => p switch
        {
            Propriete.Cotes => "côtés",
            Propriete.Sommets => "sommets",
            Propriete.Coins => "coins",
            Propriete.Angles => "angles",
            _ => "angles droits",
        };

        private static string Singulier(Propriete p) => p switch
        {
            Propriete.Cotes => "côté",
            Propriete.Sommets => "sommet",
            Propriete.Coins => "coin",
            Propriete.Angles => "angle",
            _ => "angle droit",
        };

        private static string Depart(Propriete p) => p == Propriete.Cotes ? "côté" : "coin";

        /// <summary>Minuscules, sans accents, apostrophes droites, espaces insécables en espaces.</summary>
        public static string Normaliser(string texte)
        {
            var d = texte.ToLowerInvariant().Replace('’', '\'').Replace('‘', '\'').Replace(' ', ' ').Replace(' ', ' ')
                .Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (var c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
