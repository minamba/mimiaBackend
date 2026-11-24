using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Une évaluation telle que le professeur l'a déclarée dans son message.</summary>
    /// <param name="CorrectionReportee">
    /// La note est donnée mais la correction orale attend le prochain cours,
    /// faute de temps — voir <c>Evaluation.CorrectionReportee</c>. Faux par
    /// défaut : un bloc qui ne dit rien est une correction faite sur-le-champ.
    /// </param>
    public record EvaluationDeclaree(
        string? Notion,
        double Note,
        string? Remarque,
        string? ARevoir,
        List<QuestionEvaluation> Questions,
        bool CorrectionReportee = false);

    /// <summary>
    /// Extrait le bloc [EVALUATION] d'un message du professeur.
    ///
    /// Tolérant par construction : le bloc est produit par un modèle de langue,
    /// pas par un formulaire. Une clé absente, un accent, un « 11/20 » au lieu
    /// de « 11 », une virgule décimale — tout cela doit passer. Ce qui ne passe
    /// pas, c'est l'absence de note : sans elle il n'y a rien à enregistrer.
    /// </summary>
    public static partial class LecteurEvaluation
    {
        [GeneratedRegex(@"\[EVALUATION\](?<corps>.*?)\[/EVALUATION\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>Null s'il n'y a pas de bloc exploitable.</summary>
        public static EvaluationDeclaree? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            // Sans note, il n'y a rien à enregistrer : une évaluation EST une
            // note. Le rapport de séance, lui, tolère l'absence.
            var note = LecteurBloc.Note(champs, "note");
            if (note is null) return null;

            var questions = Questions(champs);

            // `correction: reportee` — la note est donnée, la reprise des
            // erreurs attend le prochain cours. Tolérant sur la forme
            // (« reportée », « prochain cours », « plus tard »), fermé sur le
            // fond : tout ce qui n'est pas explicitement un report compte comme
            // une correction faite. Un doute ne doit pas rouvrir une copie à
            // chaque cours.
            var correction = LecteurBloc.Normaliser(LecteurBloc.Valeur(champs, "correction") ?? "");
            var reportee = correction.Contains("report")
                || correction.Contains("prochain")
                || correction.Contains("plus tard");

            return new EvaluationDeclaree(
                LecteurBloc.Valeur(champs, "notion"),
                NoteCalculee(questions) ?? note.Value,
                LecteurBloc.Valeur(champs, "remarque"),
                LecteurBloc.Valeur(champs, "a_revoir", "arevoir"),
                questions,
                reportee);
        }

        /// <summary>
        /// LA NOTE VIENT DES VERDICTS, JAMAIS DE CE QU'ÉCRIT LE PROFESSEUR EN
        /// FACE DE `note:`.
        ///
        /// Relevé en production : une copie à deux questions fausses sur six,
        /// « 4 juste, 2 faux » affiché noir sur blanc — notée 20 sur 20. Le
        /// modèle avait écrit une note et des verdicts par question dans le
        /// même bloc, sans que rien ne les rapproche l'un de l'autre ; les
        /// deux se sont contredits, et c'est le chiffre qu'un parent regarde
        /// en premier qui était faux.
        ///
        /// On calcule donc la note à partir des verdicts eux-mêmes — juste
        /// vaut un point, partiel un demi, faux zéro, ramené sur 20 — plutôt
        /// que de faire confiance à une arithmétique que le modèle vient de
        /// démontrer peu fiable. Le professeur continue de DIRE une note à
        /// l'oral ; c'est celle-ci, calculée, qui est enregistrée et qui
        /// s'affiche sur la copie — les deux peuvent différer d'un cran sans
        /// que personne ne l'entende, mais jamais la copie n'affichera une
        /// note incohérente avec ses propres verdicts.
        ///
        /// Null — donc on retombe sur le mot du professeur — dès qu'UN SEUL
        /// verdict manque ou n'est pas reconnu : calculer sur une question
        /// sans verdict compterait une faute qui n'en est pas forcément une.
        /// </summary>
        private static double? NoteCalculee(List<QuestionEvaluation> questions)
        {
            if (questions.Count == 0) return null;

            double poids = 0;
            var comptees = 0;

            foreach (var question in questions)
            {
                double? contribution = question.Verdict switch
                {
                    "juste" => 1.0,
                    "partiel" => 0.5,
                    "faux" => 0.0,
                    _ => null,
                };

                // UNE QUESTION ILLISIBLE NE DOIT PAS EMPORTER LA NOTE ENTIERE.
                //
                // Le calcul renoncait des qu UN seul verdict n etait pas
                // reconnu, et laissait alors la place au chiffre annonce par le
                // professeur. Une evaluation de six questions dont cinq etaient
                // parfaitement lisibles finissait donc notee a l estime. On
                // ecarte desormais la question douteuse et on note sur les
                // autres : mieux vaut une note assise sur cinq reponses
                // verifiees que sur aucune.
                if (contribution is null) continue;

                poids += contribution.Value;
                comptees++;
            }

            // Aucune question exploitable : la, et la seulement, on rend la
            // main au chiffre du professeur.
            if (comptees == 0) return null;

            return Math.Round(20.0 * poids / comptees, 1);
        }

        /// <summary>
        /// Les lignes `q1`, `q2`… converties en questions de copie.
        ///
        /// Une ligne incomplète n'est pas rejetée : une question dont le
        /// professeur a oublié le verdict vaut mieux, sur la copie de l'élève,
        /// qu'une question absente.
        /// </summary>
        private static List<QuestionEvaluation> Questions(Dictionary<string, string> champs)
        {
            var questions = new List<QuestionEvaluation>();

            foreach (var (cle, valeur) in champs)
            {
                if (cle.Length < 2 || cle[0] != 'q') continue;
                if (!int.TryParse(cle[1..], out var numero)) continue;

                var parts = valeur.Split('|');

                questions.Add(new QuestionEvaluation
                {
                    Numero = numero,
                    Enonce = Propre(parts.ElementAtOrDefault(0)),
                    Reponse = Propre(parts.ElementAtOrDefault(1)),
                    Verdict = Verdict(parts.ElementAtOrDefault(2)),
                    Commentaire = Propre(parts.ElementAtOrDefault(3)),
                });
            }

            return questions.OrderBy(q => q.Numero).ToList();
        }

        /// <summary>
        /// Trois verdicts seulement. Tout ce qui n'est pas reconnu devient
        /// null : la copie affichera la question sans pastille plutôt qu'avec
        /// une pastille fausse.
        /// </summary>
        private static string? Verdict(string? brut)
        {
            var propre = Propre(brut)?.ToLowerInvariant();
            if (propre is null) return null;

            // TROIS LISTES LARGES, ET C EST VOLONTAIRE.
            //
            // Le modele n ecrit pas toujours le mot attendu : « ok »,
            // « reussi », « exact », « presque », « incomplet » revenaient
            // souvent. Chacun de ces mots rendait le verdict illisible, la note
            // calculee etait alors abandonnee, et c est le chiffre annonce par
            // le professeur — 20, le plus souvent — qui etait enregistre.
            // Elargir la reconnaissance, c est faire vivre le calcul.
            if (propre.StartsWith("juste") || propre.StartsWith("correct")
                || propre.StartsWith("bon") || propre.StartsWith("ok")
                || propre.StartsWith("exact") || propre.StartsWith("vrai")
                || propre.StartsWith("reussi") || propre.StartsWith("acquis")
                || propre.StartsWith("oui"))
                return "juste";

            if (propre.StartsWith("partiel") || propre.StartsWith("moiti")
                || propre.StartsWith("incomplet") || propre.StartsWith("presque")
                || propre.StartsWith("partielle") || propre.StartsWith("en partie")
                || propre.StartsWith("approximatif") || propre.StartsWith("imprecis"))
                return "partiel";

            if (propre.StartsWith("faux") || propre.StartsWith("incorrect")
                || propre.StartsWith("mauvais") || propre.StartsWith("non")
                || propre.StartsWith("rate") || propre.StartsWith("erreur")
                || propre.StartsWith("pas de reponse") || propre.StartsWith("sans reponse")
                || propre.StartsWith("aucune"))
                return "faux";

            return null;
        }

        private static string? Propre(string? texte) =>
            string.IsNullOrWhiteSpace(texte) ? null : texte.Trim();

        /// <summary>Le marqueur d'ouverture, écrit quand le contrôle commence.</summary>
        public const string Debut = "[DEBUT_EVALUATION]";

        /// <summary>
        /// Le contrôle a été abandonné : l'élève a quitté le cours en plein
        /// milieu.
        ///
        /// Nécessaire parce qu'un contrôle ouvert et jamais refermé restait
        /// ouvert POUR TOUJOURS — l'élève revenait le lendemain et reprenait à
        /// la question trois, sans se souvenir des deux premières, avec une
        /// note qui n'aurait rien voulu dire.
        ///
        /// Distinct de la fermeture du bloc : celle-là annonce une copie rendue
        /// et une note à lire. Ici il n'y a ni copie ni note, seulement un
        /// contrôle à refaire depuis le début.
        /// </summary>
        public const string Abandon = "[EVALUATION_ABANDONNEE]";

        /// <summary>
        /// Un contrôle est-il resté ouvert dans cet historique ?
        ///
        /// On lit les messages du professeur du plus RÉCENT au plus ancien et
        /// on s'arrête au premier verdict : une copie rendue ou un abandon
        /// referment, une ouverture confirme.
        /// </summary>
        /// <summary>
        /// Le dernier contrôle a-t-il été abandonné, sans qu'un nouveau ait été
        /// lancé depuis ?
        ///
        /// Sert à l'accueil du retour : c'est ce qui distingue « bonjour, on
        /// reprend » de « bonjour, ton contrôle est annulé, on le refait ».
        /// </summary>
        public static bool EstAbandonne(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains(Abandon, StringComparison.Ordinal)) return true;

                // Un contrôle relancé, ou une copie rendue depuis : l'abandon
                // est derrière nous, il n'y a plus rien à annoncer.
                if (message.Contains(Debut, StringComparison.Ordinal)) return false;
                if (message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)) return false;
            }

            return false;
        }

        public static bool EstOuvert(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains(Abandon, StringComparison.Ordinal)) return false;

                // La fermeture du bloc signe une copie rendue.
                if (message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)) return false;

                if (message.Contains(Debut, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        /// <summary>
        /// L'évaluation est-elle ENGAGÉE sans être encore ouverte ?
        ///
        /// Signalement #68, deuxième temps (Omar, 23/11/2026) : entre le « oui »
        /// de l'élève et le top départ, il y a deux ou trois tours (choix du
        /// support, consignes, « je suis prêt »). `EstOuvert` reste faux
        /// pendant tout ce temps, et le minuteur redisait « PAS de nouvelle
        /// évaluation » à chacun : le professeur pouvait encore la retirer.
        ///
        /// Engagée = depuis la dernière évaluation rendue, abandonnée,
        /// reportée ou ouverte, le professeur a posé [SUPPORT_EVALUATION], ou
        /// l'élève a répondu au choix du support (le fait joint par l'écran,
        /// « [ÉVALUATION AU CAHIER … » / « [ÉVALUATION À L'ORDINATEUR … »).
        /// On lit TOUT le fil (les deux rôles), du plus récent au plus ancien,
        /// et on s'arrête au premier marqueur. Une ouverture ([DEBUT_EVALUATION])
        /// rend faux : c'est alors `EstOuvert` qui prend le relais.
        ///
        /// #68 v3 (Omar point 3) — L'ENGAGEMENT SE LÈVE AUSSI :
        /// - si l'élève y renonce lui-même : le professeur la reporte avec le
        ///   bloc [EVALUATION_PREVUE] (la sortie balisée, déjà lue ci-dessous) ;
        /// - si la séance se termine ([FIN_SEANCE] ou [RAPPORT]), ou si une
        ///   nouvelle séance a commencé ([TABLEAU_EFFACE], #68 v4) ;
        /// - s'il n'y a pas eu de top départ en <see cref="ToursSansTopDepart"/>
        ///   réponses du professeur après le marqueur : le fil a changé de
        ///   sujet, la consigne « tu la mènes à son terme » n'a plus d'objet.
        ///   Compté en messages (deux par échange), le message de l'élève en
        ///   cours compris : marqueur à 2 × N messages de la fin ou plus = levé.
        /// </summary>
        public static bool EstEngagee(IEnumerable<string?> messagesDuFil)
        {
            var distance = 0;
            foreach (var message in messagesDuFil.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains(Abandon, StringComparison.Ordinal)) return false;
                if (message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)) return false;
                if (message.Contains(Debut, StringComparison.Ordinal)) return false;
                // Reportée au prochain cours (ou l'élève y a renoncé) : l'engagement est levé.
                if (message.Contains("[EVALUATION_PREVUE]", StringComparison.OrdinalIgnoreCase)) return false;
                // Séance terminée.
                if (message.Contains("[FIN_SEANCE]", StringComparison.Ordinal)
                    || message.Contains("[RAPPORT]", StringComparison.OrdinalIgnoreCase)) return false;

                if (message.Contains(BaliseSupport, StringComparison.OrdinalIgnoreCase)
                    || message.Contains("[ÉVALUATION AU CAHIER", StringComparison.Ordinal)
                    || message.Contains("[ÉVALUATION À L’ORDINATEUR", StringComparison.Ordinal)
                    || message.Contains("[ÉVALUATION À L'ORDINATEUR", StringComparison.Ordinal))
                    return distance < 2 * ToursSansTopDepart;

                // Nouvelle séance (#68 v4, Omar point 4) : le premier message
                // d'une reprise commence par [TABLEAU_EFFACE] (voir le builder,
                // `TableauEfface`). Ce qui était engagé avant est derrière nous.
                // Testé APRÈS le support : un choix fait dans ce même message
                // vient après l'effacement, il compte.
                if (message.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal)) return false;

                distance++;
            }

            return false;
        }

        /// <summary>
        /// Combien de réponses du professeur, après le choix du support, sans
        /// top départ, avant que l'évaluation ne compte plus comme engagée
        /// (#68 v3). Le déroulé normal en prend deux (consignes et « prêt ? »,
        /// puis le top départ) ; quatre laisse deux tours de questions sur les
        /// consignes.
        /// </summary>
        public const int ToursSansTopDepart = 4;

        /// <summary>
        /// LE SUPPORT DE L'ÉVALUATION EST-IL CHOISI ? — #68 v9, décision du coordinateur (piste d'Omar,
        /// 23/11/2026) : on arrête les listes d'exclusions de phrases. Le filet du top départ ne peut se
        /// déclencher qu'APRÈS le choix du support ; avant, jamais, quelle que soit la phrase. Les
        /// corrections au présent (« Je corrige la première question : il manquait l'unité. ») arrivent
        /// avant ce moment : elles ne déclenchent plus rien, sans qu'aucune liste ait à les connaître.
        ///
        /// Choisi = depuis le dernier événement qui clôt (copie rendue, abandon, report, ouverture, fin de
        /// séance, nouvelle séance — les mêmes que <see cref="EstEngagee"/>) et dans la même fenêtre de
        /// <see cref="ToursSansTopDepart"/> réponses :
        /// - l'élève a cliqué sur un des deux boutons (le fait joint par l'écran, « [ÉVALUATION AU CAHIER … » /
        ///   « [ÉVALUATION À L'ORDINATEUR … ») ;
        /// - OU le professeur a posé [SUPPORT_EVALUATION] et l'élève a répondu APRÈS par écrit ou à l'oral en
        ///   nommant un support (« cahier », « l'ordi », « à l'oral », « par écrit »…) : un message court qui
        ///   n'hésite pas entre deux (<see cref="SupportDit"/>, même lexique que l'écran,
        ///   `lireReponseSupportEvaluation`, plus l'oral et l'écrit). L'écran ne joint le fait qu'au clic : la
        ///   réponse dite reste un message d'élève.
        ///
        /// Un message du professeur qui repose [SUPPORT_EVALUATION] rouvre la question : ce qui a été répondu
        /// AVANT lui ne compte plus.
        /// </summary>
        /// <param name="fil">Tout le fil, du plus ancien au plus récent, le message de l'élève du tour compris ;
        /// le rôle vaut « user » pour l'élève, autre chose pour le professeur.</param>
        public static bool SupportChoisi(IEnumerable<(string? Role, string? Contenu)> fil)
        {
            var distance = 0;
            var repondu = false;
            foreach (var (role, message) in fil.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains(Abandon, StringComparison.Ordinal)) return false;
                if (message.Contains("[/EVALUATION]", StringComparison.OrdinalIgnoreCase)) return false;
                if (message.Contains(Debut, StringComparison.Ordinal)) return false;
                if (message.Contains("[EVALUATION_PREVUE]", StringComparison.OrdinalIgnoreCase)) return false;
                if (message.Contains("[FIN_SEANCE]", StringComparison.Ordinal)
                    || message.Contains("[RAPPORT]", StringComparison.OrdinalIgnoreCase)) return false;

                var eleve = role == "user";

                // Le clic : le fait de l'écran.
                if (eleve && (message.Contains("[ÉVALUATION AU CAHIER", StringComparison.Ordinal)
                    || message.Contains("[ÉVALUATION À L’ORDINATEUR", StringComparison.Ordinal)
                    || message.Contains("[ÉVALUATION À L'ORDINATEUR", StringComparison.Ordinal)))
                    return distance < 2 * ToursSansTopDepart;

                // La question posée : choisie seulement si l'élève a nommé un support APRÈS elle.
                if (!eleve && message.Contains(BaliseSupport, StringComparison.OrdinalIgnoreCase))
                    return repondu && distance < 2 * ToursSansTopDepart;

                if (message.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal)) return false;

                if (eleve && SupportDit(message)) repondu = true;

                distance++;
            }

            return false;
        }

        /// <summary>
        /// L'élève nomme-t-il un support dans ce message (#68 v9) ? Mêmes règles que l'écran
        /// (`lireReponseSupportEvaluation`) : un message court (120 caractères au plus) qui nomme un support
        /// sans en opposer deux (« cahier ou ordi ? » ne choisit rien).
        ///
        /// #68 v10 (Omar, verdict sur 4b8198c) — un mot du lexique ne suffit plus :
        /// - le message ne POSE PAS DE QUESTION (point d'interrogation ou mot interrogatif) : « j'ai écrit 0,07 à
        ///   la 1, c'est juste ? », « c'est quoi l'oral déjà ? » ne choisissent rien ;
        /// - il ne contient ni NÉGATION ni ABSENCE (« pas », « n' », « jamais », « sans », « oublié », « perdu »,
        ///   « en panne », « à l'école »…) : « mon cahier est à l'école » ne choisit pas le cahier ;
        /// - il ne contient pas de CHIFFRE (une réponse à une question, pas un support) ;
        /// - lexique élargi (rappel perdu de la v8) : écrire / écris, taper / en tapant, oralement, parler,
        ///   « ici », ordis.
        /// Les réponses qui ne nomment rien (« comme tu veux », « peu importe ») sont couvertes par le
        /// professeur qui nomme le support avant le départ (<see cref="SupportNommeAvantDepart"/>).
        ///
        /// #68 v11 (Omar, verdict sur 6e01fcf : 12 faux choix sur 12, « J'écris encore. » lu 18 fois comme un choix
        /// dans le corpus) — un mot du lexique dans une phrase quelconque ne suffit plus : le message doit être une
        /// CONSTRUCTION DE CHOIX (<see cref="ConstructionDeChoix"/>), c'est-à-dire réduit au(x) support(s), précédé au
        /// plus d'un verbe de choix (préfère, veux, choisis, prends, plutôt, je reste, je fais / compose, je vais…) et
        /// accompagné au plus d'UNE formule de politesse (stp, merci, oui, ok…). « je l'ai écrit, c'est douze »,
        /// « c'est écrit dans le livre », « ici c'est Paris », « mon micro grésille », « J'écris encore. » ne
        /// choisissent rien.
        /// </summary>
        public static bool SupportDit(string? message)
        {
            var dit = (message ?? string.Empty).Trim();
            if (dit.Length == 0 || dit.Length > 120) return false;
            if (dit.Contains('?') || MotInterrogatif().IsMatch(dit)) return false;
            if (NegationOuAbsence().IsMatch(dit) || dit.Any(char.IsDigit)) return false;
            if (!MotDeSupport().IsMatch(dit) || SupportsOpposes().IsMatch(dit)) return false;

            // #68 v11 : une construction de choix, pas une phrase qui contient un mot de support.
            return ConstructionDeChoix().IsMatch(dit);
        }

        /// <summary>Ce qui sépare les mots d'une construction de choix : espaces, ponctuation (sauf « ? » et
        /// l'apostrophe), guillemets, émojis. #68 v12 : astérisque et tiret bas du gras / de l'italique
        /// (« **Très bien, au cahier !** »).</summary>
        private const string Bruit = @"[\s,.!…;:\-–—""«»()*_\p{So}\p{Cs}\p{Sk}\p{M}]";

        /// <summary>Une formule de politesse ou d'acquiescement (#68 v11) — deux au plus par message (#68 v12 :
        /// « oui l'ordi stp », « l'ordi stp merci »).</summary>
        private const string Politesse =
            @"(?:stp|svp|s['’]il\s+te\s+pla[iî]t|s['’]il\s+vous\s+pla[iî]t|merci|oui|ouais|ok|okay|d['’]accord|euh|bah|ben|bon|alors|allez)";

        /// <summary>Un verbe de choix devant le support (#68 v11) : « je préfère », « je veux », « je choisis »,
        /// « je prends », « je reste », « je fais / compose », « je vais », « on fait », « plutôt », « va pour »,
        /// suivis au besoin d'un infinitif de travail (« je préfère le faire sur le cahier »).</summary>
        private const string VerbeDeChoix =
            @"(?:(?:moi\s*,?\s*)?(?:je\s+|j['’]\s*)(?:préfère|préfèrerais|préférerais|prefere|preferes?|veux|voudrais|choisis|prends|reste|fais|compose|vais)"
            + @"(?:\s+(?:le\s+faire|faire|composer|répondre|repondre|rester|prendre|utiliser))?"
            + @"|on\s+(?:fait|reste|prend)|plutôt|plutot|moi|va\s+pour)";

        /// <summary>Un support dit par l'élève dans une construction de choix (#68 v11) : un nom de support avec
        /// au plus une préposition et un article (« cahier », « l'ordi », « à l'oral », « sur le cahier »,
        /// « par écrit »), ou un adverbe / infinitif / gérondif (oralement, ici, en tapant, écrire, taper, parler),
        /// ou « j'écris / je tape / je parle » SEULS. Pas « j'ai écrit », ni « c'est écrit ». #68 v12 : l'apostrophe
        /// oubliée (« sur l ordi », « lordi »).</summary>
        private const string SupportEleve =
            @"(?:(?:(?:au|aux|le|la|les|l\s*['’]?|mon|ma|mes|sur|à|a|en|par|avec)\s*(?:(?:le|la|les|l\s*['’]?|mon|ma|mes|une|un|ton|ta)\s*)?)?"
            + @"(?:cahiers?|cachier|papier|feuilles?|ordinateurs?|ordis?|claviers?|écran|ecran|oral|orale|écrit|ecrit|écrite|ecrite|micro|voix\s+haute|la\s+main)"
            + @"|oralement|ici|en\s+tapant|en\s+écrivant|en\s+ecrivant|écrire|ecrire|taper|parler|j['’]\s*écris|j['’]\s*ecris|je\s+tape|je\s+parle)";

        /// <summary>Le cœur d'une construction de choix : [verbe de choix] support [support] [plutôt / c'est mieux].</summary>
        private const string CoeurDuChoix =
            @"(?:" + VerbeDeChoix + Bruit + @"+)?" + SupportEleve + @"(?:" + Bruit + @"+" + SupportEleve + @")?"
            + @"(?:" + Bruit + @"+(?:plutôt|plutot|c['’]est\s+mieux))?";

        /// <summary>
        /// LA CONSTRUCTION DE CHOIX (#68 v11) : le message entier est le cœur du choix, avec au plus une formule de
        /// politesse avant OU après (« à l'oral stp », « oui l'ordi », « moi je veux à l'écrit », « je reste ici »).
        /// #68 v12 (Omar, verdict sur c53fe78) : DEUX formules au plus, avant et / ou après (« oui l'ordi stp »,
        /// « l'ordi stp merci », « ok oui cahier »), jamais trois.
        /// </summary>
        [GeneratedRegex(@"^" + Bruit + @"*(?:"
            + Politesse + Bruit + @"+" + Politesse + Bruit + @"+" + CoeurDuChoix
            + @"|" + Politesse + Bruit + @"+" + CoeurDuChoix + @"(?:" + Bruit + @"+" + Politesse + @")?"
            + @"|" + CoeurDuChoix + @"(?:" + Bruit + @"+" + Politesse + @"){0,2}"
            + @")" + Bruit + @"*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
        private static partial Regex ConstructionDeChoix();

        /// <summary>Les mots d'un support : ceux de l'écran (cahier, papier, feuille, à la main / ordinateur, ordi,
        /// clavier, écran, taper) et ceux d'une question « écrit ou oral ? » du professeur. #68 v10 : écrire,
        /// écris, tape / en tapant, oralement, parler, ici, ordis.</summary>
        private const string MotsDeSupport =
            @"cahier|cachier|papier|feuille|à\s+la\s+main|a\s+la\s+main|ordinateurs?|ordis?|lordis?|lordinateurs?|clavier|écran|ecran"
            + @"|tap(?:er|e|es|ant|é|ée)|oral|orale|oralement|à\s+voix\s+haute|au\s+micro|micro"
            + @"|écrit|ecrit|écrite|ecrite|par\s+écrit|écrire|ecrire|écris|ecris|en\s+écrivant|parl(?:er|e|ant)|ici";

        /// <summary>Un mot interrogatif (#68 v10) : l'élève pose une question, même sans « ? ».</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:quoi|comment|pourquoi|combien|quel|quelle|quels|quelles|qui|où|est-ce|c['’]est\s+juste)(?!\p{L})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MotInterrogatif();

        /// <summary>Une négation ou une absence (#68 v10) : « je n'ai pas de cahier », « j'ai oublié mon cahier »,
        /// « mon cahier est à l'école », « l'ordi est en panne », « sans le cahier ».</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:pas|jamais|aucun|aucune|sans|ni|ne|n['’]|oublié|oubliée|oublie|perdu|perdue|cassé|cassée|panne"
            + @"|à\s+l['’]école|a\s+l['’]ecole|au\s+collège|au\s+lycée|en\s+classe|dans\s+(?:mon|ma|le|la)\s+(?:sac|casier|cartable)"
            + @"|chez\s+(?:moi|mon|ma|mes|mamie|papi|papa|maman))(?!\p{L})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex NegationOuAbsence();

        /// <summary>
        /// LE PROFESSEUR NOMME LE SUPPORT AVANT LE DÉPART (#68 v10, Omar point 2) : « Très bien, au cahier !
        /// Question 1 : … », « D'accord, tu composes ici à l'écran. Question 1 : … ». Couvre les élèves qui
        /// choisissent sans nommer (« comme tu veux », « peu importe ») ou avec des mots hors lexique. Le support doit
        /// être nommé AVANT la première question, dans une phrase qui n'est pas une question et qui n'en oppose
        /// pas deux (« Tu préfères le cahier ou l'ordi ? Bon, question 1 : » ne choisit rien).
        ///
        /// #68 v11 (Omar, verdict sur 6e01fcf : 14 faux positifs sur 14) — une phrase qui CITE un support ne suffit
        /// plus (« Je regarde ce que tu as écrit sur ton cahier. », « Relis ta feuille d'exercices. », « Tu feras
        /// l'évaluation au cahier plus tard. ») : il faut une PHRASE DE CONFIRMATION ÉTROITE
        /// (<see cref="ConfirmationDuSupport"/>) — accord (« Très bien », « D'accord », « Parfait », « Va pour »…),
        /// au plus un sujet + verbe au PRÉSENT de composition (« tu composes », « on reste », « on fait ça »), puis
        /// le support, et rien d'autre : pas de passé, pas de futur ni de « plus tard », pas d'impératif (relis /
        /// note / prends), pas de « feuille » / « papier » nus, pas de « l'écrit » suivi d'un complément.
        /// Et le builder ne l'écoute que JUSTE APRÈS une réponse de l'élève à [SUPPORT_EVALUATION]
        /// (<see cref="ReponseALaQuestionDuSupport"/>, <see cref="TopDepartAuTour"/>).
        /// </summary>
        public static bool SupportNommeAvantDepart(string? reponse)
        {
            if (string.IsNullOrEmpty(reponse)) return false;
            var texte = Bloc().Replace(reponse, string.Empty);
            var depart = PremiereQuestion().Match(texte);
            if (!depart.Success || depart.Index == 0) return false;

            foreach (Match phrase in PhraseAvecFin().Matches(texte[..depart.Index]))
            {
                var p = phrase.Value.Trim();
                if (p.EndsWith('?')) continue;
                if (ConfirmationDuSupport().IsMatch(p) && !SupportsOpposes().IsMatch(p)) return true;
            }
            return false;
        }

        /// <summary>Un accord du professeur devant le support confirmé (#68 v11). #68 v12 : « super / excellent
        /// choix », « comme tu veux », « dans ce cas », « je choisis pour toi » (l'élève a dit « comme tu veux »).</summary>
        private const string AccordDuProf =
            @"(?:très\s+bien|bien|parfait|super|génial|d['’]accord|ok|okay|entendu|c['’]est\s+noté|noté|top|excellent"
            + @"|(?:(?:très\s+)?bon|super|excellent|beau)\s+choix|ça\s+marche|allez|alors|bon|oui|c['’]est\s+parti|pas\s+de\s+souci|aucun\s+souci"
            + @"|bien\s+reçu|va\s+pour|ok\s+pour|comme\s+tu\s+veux|comme\s+tu\s+préfères|dans\s+ce\s+cas|je\s+choisis\s+pour\s+toi)";

        /// <summary>Le prénom de l'élève, en majuscule, juste après un accord (#68 v12 : « Très bien, Adam, au
        /// cahier ! », « Très bien Inès, à l'écran. ») — jamais un impératif ni un mot de phrase écrit en tête
        /// (« Très bien, Prends ton cahier. » reste refusé).</summary>
        private const string PrenomApresAccord =
            @"(?-i:(?!(?:Question|Voici|Voilà|Prends|Prenez|Relis|Note|Ouvre|Sors|Garde|Range|Pose|Regarde|Prépare|Utilise|Mets"
            + @"|Écris|Ecris|Réponds|Fais|Tu|On|Je|Nous|Vous|Ici|Ce|Cette|Le|La|Les|Au|Sur|Ton|Ta|Pour)(?!\p{L}))"
            + @"\p{Lu}\p{Ll}+(?:-\p{Lu}\p{Ll}+)?)";

        /// <summary>Sujet + verbe de composition au PRÉSENT (#68 v11) : « tu composes », « on reste », « on fait
        /// ça », « tu réponds ». Ni « tu as … », ni « tu feras », ni « tu peux », ni « tu travailles ».
        /// #68 v12 (Omar, 268 réponses réelles) : « on part sur », « on le fait à », « on continue sur », « on est
        /// parti à », « tu fais l'évaluation sur », « je te propose (de rester) », « ce sera », « c'est parti pour
        /// ton évaluation sur », « allons-y », « (c'est noté) que tu restes ».</summary>
        private const string VerbeDuProf =
            @"(?:(?:tu|on|nous)\s+(?:composes|compose|composons|réponds|répond|répondons|fais|fait|faisons|restes|reste|restons"
            + @"|passes|passe|passons|choisis|choisit|prends|prend|écris|écrit|écrivons|y\s+vas|y\s+va"
            + @"|pars|part|partons|continues|continue|continuons|est\s+partis?|le\s+fais|le\s+fait|la\s+fais|la\s+fait)"
            + @"(?:\s+(?:ça|donc|alors|tout|l['’]évaluation|ton\s+évaluation|le\s+contrôle|ton\s+contrôle))?"
            + @"|je\s+te\s+propose(?:\s+de\s+(?:rester|faire|composer|continuer|répondre|travailler)(?:\s+(?:ça|l['’]évaluation))?)?"
            + @"|ce\s+sera|allons-y|on\s+y\s+va|que\s+tu\s+(?:restes|composes|réponds|fais|prends|choisis)"
            + @"|c['’]est\s+parti\s+pour\s+(?:l['’]évaluation|ton\s+évaluation|le\s+contrôle|ton\s+contrôle))";

        /// <summary>Le support confirmé par le professeur (#68 v11) : « au cahier », « le cahier », « sur ta
        /// feuille », « à la main », « à l'écran », « sur l'ordinateur », « à l'oral », « à l'écrit », « l'oral »,
        /// « au clavier », « oralement », « à voix haute », « au micro », « par écrit », éventuellement après
        /// « pour » (« Va pour le cahier », « C'est parti pour l'oral »). Pas « feuille » ni « papier » nus.
        /// #68 v12 : le support sans article (« sur ordinateur », « sur cahier », « cahier choisi »).</summary>
        private const string SupportDuProf =
            @"(?:pour\s+)?(?:au\s+cahier|(?:sur\s+)?(?:ton|le)\s+cahier|sur\s+(?:une|ta)\s+feuille|à\s+la\s+main"
            + @"|(?:à|sur)\s+l['’](?:écran|ordinateur|ordi)|à\s+l['’](?:oral|écrit)|l['’](?:ordinateur|ordi|oral|écrit|écran)"
            + @"|(?:au|le)\s+clavier|oralement|à\s+voix\s+haute|au\s+micro|par\s+écrit"
            + @"|(?:(?:sur|à|en)\s+)?(?:ordinateur|ordi|cahier|écran))";

        /// <summary>Ce qui peut suivre le support dans une confirmation (#68 v11, élargi en v12) : « ici », « alors »,
        /// « donc », « c'est parti », « c'est noté », « bon choix », « très bien », « parfait », « ici même »,
        /// « choisi », « c'est très bien pour poser tes calculs », « marche très bien aussi », « est tout à fait
        /// adapté », « donc je t'affiche le sujet en entier », « en main », « il est », « trois questions ».
        /// #68 v13 (Ismail, verdict sur la v12) : plus AUCUN complément libre — le texte libre après « c'est … pour » et
        /// « donc je t'affiche » faisait lire des refus, des renvois et des corrections (« L'oral, c'est bien pour
        /// réviser, mais pas pour une évaluation. », « Le cahier, c'est noté pour la prochaine fois. », « L'écran, donc
        /// je t'affiche la correction de l'exercice. »). Les compléments sont une LISTE FERMÉE tirée des 268 réponses
        /// réelles (<see cref="ComplementPourDuSupport"/>, <see cref="ComplementSujetAffiche"/>) : un complément qui
        /// contient « pas », « mais », « prochaine », « demain », « plus tard », « correction » ou « réviser » n'y est
        /// jamais.</summary>
        private const string SuiteDuSupport =
            @"(?:ici\s+même|ici|alors|donc|c['’]est\s+parti|bon\s+choix|très\s+bien|parfait|choisi|choisie"
            + @"|c['’]est\s+(?:noté|parfait|très\s+bien|bien|top|idéal|mieux)(?:\s+" + ComplementPourDuSupport + @")?"
            + @"|(?:marche|convient|va)\s+(?:très\s+|tout\s+aussi\s+)?bien(?:\s+aussi)?"
            + @"|est\s+(?:tout\s+à\s+fait\s+|très\s+)?adapté(?:\s+pour\s+ça)?"
            + @"|donc\s+je\s+t['’](?:affiche|écris|donne|mets)\s+" + ComplementSujetAffiche
            + @"|en\s+main|il\s+est|(?:deux|trois|quatre|cinq)\s+questions)";

        /// <summary>Le complément de « c'est très bien / parfait … » après le support (#68 v13), formes RÉELLES
        /// seulement : « pour ça », « pour poser tes / les calculs (proprement) », « pour des (petits) calculs (à poser
        /// au propre) ».</summary>
        private const string ComplementPourDuSupport =
            @"pour\s+(?:ça|cela"
            + @"|poser\s+(?:tes|les)\s+(?:petits\s+)?calculs(?:\s+(?:proprement|au\s+propre))?"
            + @"|(?:des|tes|les)\s+(?:petits\s+)?calculs(?:\s+à\s+poser)?(?:\s+(?:proprement|au\s+propre))?)";

        /// <summary>Ce que le professeur affiche après « donc je t'affiche » (#68 v13), forme RÉELLE seulement :
        /// « le sujet (en entier / complet)(, dès maintenant) ».</summary>
        private const string ComplementSujetAffiche =
            @"(?:le|ton|tout\s+le)\s+sujet(?:\s+(?:en\s+entier|complet))?(?:,?\s+dès\s+maintenant)?";

        /// <summary>
        /// LA PHRASE DE CONFIRMATION DU SUPPORT (#68 v11) — la phrase entière : [accord(s)] [prénom] [sujet + verbe au
        /// présent] [ici] support [suite]. « Très bien, au cahier ! », « D'accord, tu composes ici à l'écran. »,
        /// « Parfait, à l'oral. », « Super, on reste ici à l'écran. », « Alors on fait ça à l'oral. », « Va pour le
        /// cahier ! ». #68 v12 (Omar, verdict sur c53fe78 : sur les 268 réponses réelles du modèle, la v11 n'en lisait
        /// plus que 12) : « Bien, on part sur ton cahier. », « Bien reçu, on le fait à l'ordinateur. », « D'accord,
        /// cahier choisi. », « Parfait, le cahier, c'est très bien… », « Très bien, Adam, au cahier ! », « Comme tu
        /// veux, je choisis pour toi : on fait ça à l'écran. », « Je te propose de rester à l'écran. »,
        /// « **Très bien, au cahier !** ». La porte « juste après » (<see cref="TopDepartAuTour"/>) reste la seule
        /// à l'écouter.
        /// </summary>
        [GeneratedRegex(@"^" + Bruit + @"*(?:(?:" + AccordDuProf + Bruit + @"+)+" + PrenomApresAccord + Bruit + @"+)?"
            + @"(?:" + AccordDuProf + Bruit + @"+)*(?:" + VerbeDuProf + Bruit + @"+)?(?:ici" + Bruit + @"+)?"
            + SupportDuProf + @"(?:" + Bruit + @"+" + SuiteDuSupport + @")?"
            + Bruit + @"*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
        private static partial Regex ConfirmationDuSupport();

        /// <summary>Une phrase et sa ponctuation finale (le reste sans ponctuation compte aussi).</summary>
        [GeneratedRegex(@"[^.!?…\n]+[.!?…]*", RegexOptions.CultureInvariant)]
        private static partial Regex PhraseAvecFin();

        /// <summary>
        /// LE FILET DU TOP DÉPART SANS LE FIL (#68 v10, restreint en v11) : le support est choisi
        /// (<see cref="SupportChoisi"/>) ET la réponse pose la première question sans balise
        /// (<see cref="TopDepartSansBalise"/>). #68 v11 : le support nommé par le professeur n'y compte plus — il ne
        /// compte que juste après une réponse de l'élève à [SUPPORT_EVALUATION], ce que seul le fil dit : voir
        /// <see cref="TopDepartAuTour"/>, que le builder appelle.
        /// </summary>
        public static bool TopDepartApresSupport(bool supportChoisi, string? reponse) =>
            TopDepartAuTour(supportChoisi, false, reponse);

        /// <summary>
        /// LE FILET COMPLET DU TOP DÉPART (#68 v11), tel que le builder l'appelle quand l'évaluation est engagée :
        /// le support est choisi (<see cref="SupportChoisi"/> : clic, ou support dit par l'élève) OU — seulement
        /// quand la réponse suit JUSTE une réponse de l'élève à [SUPPORT_EVALUATION]
        /// (<paramref name="reponseALaQuestionDuSupport"/>, <see cref="ReponseALaQuestionDuSupport"/>) — le
        /// professeur confirme le support avant la première question (<see cref="SupportNommeAvantDepart"/>) ; ET
        /// la réponse pose la première question sans balise (<see cref="TopDepartSansBalise"/>).
        /// </summary>
        public static bool TopDepartAuTour(bool supportChoisi, bool reponseALaQuestionDuSupport, string? reponse) =>
            (supportChoisi || (reponseALaQuestionDuSupport && SupportNommeAvantDepart(reponse)))
            && TopDepartSansBalise(reponse);

        /// <summary>
        /// LA RÉPONSE DU PROFESSEUR SUIT-ELLE JUSTE UNE RÉPONSE DE L'ÉLÈVE À [SUPPORT_EVALUATION] ? (#68 v11, même
        /// logique que <see cref="SupportChoisi"/>) — le dernier message du fil est celui de l'élève (le message du
        /// tour) et le message qui le précède est un message du professeur qui porte [SUPPORT_EVALUATION]. C'est le
        /// seul moment où « Très bien, au cahier ! Question 1 : … » confirme un choix (« comme tu veux », « peu
        /// importe ») ; à tout autre moment, un support cité par le professeur est une correction, un rappel, un
        /// projet.
        /// </summary>
        /// <param name="fil">Tout le fil, du plus ancien au plus récent, le message de l'élève du tour compris ;
        /// le rôle vaut « user » pour l'élève, autre chose pour le professeur.</param>
        public static bool ReponseALaQuestionDuSupport(IEnumerable<(string? Role, string? Contenu)> fil)
        {
            var eleveVu = false;
            foreach (var (role, message) in fil.Reverse())
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                if (!eleveVu)
                {
                    if (role != "user") return false;
                    eleveVu = true;
                    continue;
                }
                return role != "user" && message.Contains(BaliseSupport, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        [GeneratedRegex(@"(?<!\p{L})(?:" + MotsDeSupport + @")(?!\p{L})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MotDeSupport();

        [GeneratedRegex(@"(?<!\p{L})(?:" + MotsDeSupport + @")(?!\p{L})[^.!?\n]{0,40}(?<!\p{L})ou(?!\p{L})[^.!?\n]{0,40}(?<!\p{L})(?:" + MotsDeSupport + @")(?!\p{L})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex SupportsOpposes();

        /// <summary>
        /// La première question d'un contrôle, écrite comme telle : « Question 1 : »,
        /// « **Question 1** — », « Question 1. », « Question un. », « Question 1 sur 3 »,
        /// « Question 1 » seule sur sa ligne, « Première question. / , / : »,
        /// « Q1 : », ou une ligne qui commence par « 1. » / « 1) ».
        /// Pas « Question 12 », ni « la question 1 de tout à l'heure », ni « les
        /// questions une par une ».
        ///
        /// #68 v5 (Omar, régression v4 : « …Dès que tu me dis que tu es prêt, je
        /// lance la première question. ») — « Première question » et « Question 1 »
        /// seule ne comptent qu'EN TITRE ou EN DÉBUT DE PHRASE (<see cref="DebutDePhrase"/> :
        /// début de ligne, après . ! ? : ; …, un émoji ou un « Allez, », ou après
        /// « Voici / Voilà la (ta) ») ; jamais comme complément d'un verbe (« je
        /// lance la première question »). Les formes fortes (« Question 1 : »,
        /// « Question 1 sur 3 ») valent partout, SAUF juste après « je lance /
        /// pose / donne / envoie / affiche / écris la (ta, une, cette) » (<see cref="ApresVerbeDeLancement"/>).
        ///
        /// #68 v6 (Omar point 3 : 15 tops départs réels vus par la v4, ratés par la v5) —
        /// « Question 1 : » (deux-points) vaut PARTOUT, verbe de lancement compris
        /// (« Je te pose la question 1 : calcule… », « Je t'affiche la question 1 : ») :
        /// l'énoncé suit, c'est la question posée. Un aperçu qui attend le « prêt »
        /// (« Je lance la question 1 : tu es prêt ? ») reste écarté par <see cref="AttendLePret"/>.
        /// <see cref="DebutDePhrase"/> accepte en plus le prénom en apostrophe (« Très bien,
        /// Adam, première question : »), « Et voici la », « Commençons par la » et
        /// « Allez c'est parti, » sans virgule après « Allez ».
        ///
        /// #68 v7 (Omar, verdict sur 6ef6e24) :
        /// - « Question 1 : » et « (la) première question : » suivis de l'énoncé valent
        ///   partout, AU PRÉSENT seulement : jamais après un futur, un conditionnel ou
        ///   « vais te + infinitif » (<see cref="PasAuFutur"/> : « Je vais te poser la
        ///   question 1 : elle portera sur k. », « Ensuite, je te poserai la question 1 : »),
        ///   ni juste après un mot de discours (<see cref="PasApresMotDeDiscours"/> :
        ///   « Attention, première question : », « Rappel, … »), ni après « pour / de / sur /
        ///   dans / en la » (une correction : « Pour la première question : tu avais… »).
        ///   Rattrape « Je te pose la première question : », « Voici donc la première
        ///   question : », « Pour commencer, première question : », « On y va. La première
        ///   question : ».
        /// - Le prénom en apostrophe de <see cref="DebutDePhrase"/> n'accepte plus les mots
        ///   de discours (« Attention, », « Consigne, »…) et accepte les prénoms composés
        ///   (« Jean-Pierre, »).
        ///
        /// #68 v8 (Omar, régression v7 : « (la) première question : » partout se déclenchait sur
        /// les CORRECTIONS au présent — la faille existait déjà pour « Question 1 : ») :
        /// - ce qui suit les deux-points ne doit pas être un verdict ou une correction
        ///   (`PasUnVerdict`, retiré en v9 : « juste », « faux », « bien vu », « parfait », « c'est
        ///   juste », « tu avais… », « il manquait… », « elle était… ») ;
        /// - pas après un verbe de reprise (`PasApresReprise`, retiré en v9 : « Je reprends / corrige /
        ///   relis la », « Revenons à la », « Regardons la », « On reprend la »), ni après « à la »
        ///   en début de phrase (« À la première question : tu as… » ; « Passons à la » reste un
        ///   départ), ni après « ta » (`PasApresPossessif`, retiré en v9 : « Super, ta première
        ///   question : », sauf « Voici ta », « Je te pose ta ») ;
        /// - un message qui FINIT par une demande de support ou de pause n'est jamais un top
        ///   départ (`FinitSurSupportOuPause`, retiré en v9) ;
        /// - « Ensuite, / Puis, question 1 : » suivis d'un énoncé à l'IMPÉRATIF (« calcule k. »)
        ///   sont rattrapés (`ApresMotDeSuite`, retiré en v9) : lus vrais en v6, faux en v7.
        ///
        /// #68 v9 (décision du coordinateur, piste d'Omar : « la liste noire ne pourra jamais être complète ») —
        /// les exclusions de phrases de la v8 sont RETIRÉES (verdict après les deux-points, verbe de reprise,
        /// « à la », « ta », fin sur une demande de support ou de pause, mot de suite + impératif) : chacune
        /// cassait de vrais départs (« Question 1 : tu avais 12 billes… », « Super, ta première question : … »,
        /// « Question 1 : … Tu réponds à l'écrit ou à l'oral ? »). Les corrections qu'elles écartaient arrivent
        /// AVANT le choix du support, et le filet ne joue plus qu'APRÈS (<see cref="SupportChoisi"/>, appelé
        /// par le builder). Restent celles de la v7 (futur, mots de discours, préposition), utiles après le
        /// choix (« Je vais te poser la question 1 : elle portera sur k. »). « Ensuite, » et « Puis, » ne sont
        /// plus des mots de discours : « Ensuite, question 1 : combien font 7 × 8 ? » est un départ.
        /// </summary>
        [GeneratedRegex(PasAuFutur + PasApresMotDeDiscours + PasApresPreposition + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b" + AvantLesDeuxPoints
            + @"|" + PasAuFutur + PasApresMotDeDiscours + PasApresPreposition + @"\bpremière\s+question\b" + AvantLesDeuxPoints
            + @"|" + ApresVerbeDeLancement + PasAuFutur + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b\**(?:\s*[.)\-–—]|\s+sur\s+(?:\d+|deux|trois|quatre|cinq)\b)"
            + @"|" + DebutDePhrase + @"\bQuestion\s*(?:n°\s*|numéro\s*)?(?:1|un|une)\b\**[ \t]*\**[ \t]*\r?$"
            + @"|" + DebutDePhrase + @"\bPremière\s+question\b\**(?:\s*[:.,!\-–—]|[ \t]*\r?$)"
            + @"|" + PasAuFutur + @"\bQ\s?1\s*[:.)\-–—]"
            + @"|^[ \t]*\**1[.)]\**[ \t]+(?=\S)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
        private static partial Regex PremiereQuestion();

        /// <summary>
        /// Entre « Question 1 » / « première question » et les deux-points (#68 v10, les deux trous signalés par Omar) :
        /// le gras, le nombre de questions entre parenthèses (« Première question (sur 3) : ») et un adverbe de moment
        /// (« je pose la question 1 tout de suite : »). Pas une parenthèse quelconque : « Question 1 (à revoir) : »
        /// est une correction (corpus wEO3-5 t5).
        /// </summary>
        private const string AvantLesDeuxPoints =
            @"\**(?:[ \t]*\([ \t]*sur[ \t]+(?:\d+|deux|trois|quatre|cinq)[ \t]*\))?(?:[ \t]+(?:tout[ \t]+de[ \t]+suite|maintenant|directement|alors|donc))?\**[ \t]*:";

        /// <summary>
        /// Ce qui précède un titre ou un début de phrase (#68 v5) : début de ligne,
        /// ponctuation forte, émoji, puces de mise en forme (** ## > _), puis au plus
        /// une interjection suivie d'une virgule (« Allez, », « Bon, », « C'est
        /// parti ! ») et au plus « Voici / Voilà la (ta, ma) » (« Voici ta première
        /// question. », wBO3-5 t3) ou « (Allez) c'est parti pour la » (tEO3-1 t3). Bornes gourmandes, jamais paresseuses : le
        /// générateur .NET ne compile pas une boucle paresseuse dans un lookbehind.
        ///
        /// #68 v6 (Omar point 3) : jusqu'à trois interjections, virgule facultative
        /// (« Allez c'est parti, », « Et voici »), puis au plus UN prénom en
        /// apostrophe — un seul mot à majuscule suivi d'une virgule (« Très bien,
        /// Adam, ») —, puis au plus « Voici / Voilà la », « c'est parti pour la » ou
        /// « Commençons / On commence / On attaque par (avec) la ». Ces mots ne
        /// déclenchent rien seuls : il faut encore « Première question » ou
        /// « Question 1 » suivis d'une ponctuation ou d'une fin de ligne.
        ///
        /// #68 v7 (Omar point 2) : le prénom n'est plus « n'importe quel mot à majuscule » —
        /// les mots de discours (<see cref="MotsDeDiscours"/> : « Attention, », « Rappel, »,
        /// « Consigne, », « Ensuite, »…) et de correction (« Bravo, », « Merci, », « Oui, »)
        /// sont exclus ; les prénoms composés passent (« Jean-Pierre, », « Marie-Ève, »).
        ///
        /// #68 v9 : « Ensuite, » et « Puis, » comptent parmi les interjections (« Ensuite, première question. »).
        /// </summary>
        private const string DebutDePhrase =
            @"(?<=(?:^|[.!?…:;]|[\p{So}\p{Cs}])[ \t]*(?:[*_#>]+[ \t]*){0,3}(?:[\p{So}\p{Cs}]+[ \t]*)?"
            + @"(?:(?:et|allez|alors|bon|bien|donc|ok|okay|parfait|super|très\s+bien|c['’]est\s+parti|go|ensuite|puis)[ \t]*[,!]*[ \t]*){0,3}"
            + @"(?:(?-i:\p{Lu}\p{Ll}*(?:['’\-]\p{Lu}?\p{Ll}+)*)(?<!\b(?:" + MotsDeDiscours + @"|merci|oui|non|désolée?|pardon|raté|correct|incorrect|perdu))[ \t]*,[ \t]*)?"
            + @"(?:(?:voici|voilà)[ \t]+(?:la|ta|ma)[ \t]+|c['’]est[ \t]+parti[ \t]+pour[ \t]+(?:la|ta)[ \t]+"
            + @"|(?:commençons|démarrons|attaquons|on[ \t]+(?:commence|démarre|attaque))[ \t]+(?:par|avec)[ \t]+(?:la|ta)[ \t]+)?)";

        /// <summary>
        /// Les mots de discours qui ne sont ni un prénom ni un départ (#68 v7, Omar point 2) :
        /// « Attention, première question : … », « Rappel, … », « Consigne, … » annoncent une
        /// remarque, pas le top départ ; « Bravo, », « Juste, », « Faux, » corrigent.
        /// #68 v9 : « Ensuite, » et « Puis, » n'en sont plus (« Ensuite, question 1 : combien font 7 × 8 ? » est un
        /// départ) ; « Ensuite, je te poserai la question 1 : » reste écarté par le futur.
        /// </summary>
        private const string MotsDeDiscours =
            @"attention|rappel|consigne|remarque|enfin|note|nb|important|astuce|conseil|indice|correction"
            + @"|rectification|exemple|bravo|exact|juste|faux|presque|dommage|bref|sinon|rappelle-toi|souviens-toi";

        /// <summary>Pas juste après un mot de discours (« Attention, première question : »), article compris.</summary>
        private const string PasApresMotDeDiscours =
            @"(?<!\b(?:" + MotsDeDiscours + @")[ \t]*[,:;!]?[ \t]*(?:(?:la|ta|ma|une|cette)[ \t]+)?)";

        /// <summary>
        /// Pas après « pour / de / sur / dans / en / sauf la » (#68 v7) : « Pour la première
        /// question : tu avais oublié l'unité » parle d'une question passée. « C'est parti pour
        /// la question 1 : » reste un départ.
        /// </summary>
        private const string PasApresPreposition =
            @"(?<!(?:\b(?:de|sur|dans|en|sauf)|(?<!\bparti[ \t]+)\bpour)[ \t]+(?:la|ta|ma|cette)[ \t]+)";

        /// <summary>
        /// Pas après un FUTUR, un CONDITIONNEL ou « aller + infinitif » (#68 v7, Omar point 1) :
        /// « Je vais te poser la question 1 : elle portera sur k. », « Ensuite, je te poserai la
        /// question 1 : », « Dans un instant, je te donnerai la question 1 : », « Tout à l'heure,
        /// je t'enverrai la question 1 : » annoncent la question, ils ne la posent pas — même avec
        /// les deux-points. Futur et conditionnel reconnus à leur terminaison (-rai, -ras, -ra,
        /// -erons/-irons, -erez/-irez, -ront, -rais, -rait, -rions, -riez, -raient), suivis ou non
        /// d'un infinitif (« je voudrais te poser la »), au plus un adverbe (« ensuite »,
        /// « tout de suite »), puis « (à / par / avec) la (ta, ma, une, cette) ». Le présent
        /// (« Je te pose la question 1 : calcule k. ») n'est pas touché.
        /// </summary>
        private const string PasAuFutur =
            @"(?<!\b(?:(?:vais|vas|va|allons|allez|vont|" + Futur + @")[ \t]+(?:(?:te|me|lui|vous|nous|leur)[ \t]+|[tm]['’])?\p{L}+(?:er|ir|re|oir)"
            + @"|" + Futur + @")"
            + @"(?:[ \t]+(?:ensuite|après|bientôt|alors|donc|maintenant|d['’]abord|tout[ \t]+de[ \t]+suite|tout[ \t]+à[ \t]+l['’]heure|plus[ \t]+tard|juste[ \t]+après))?"
            + @"[ \t]+(?:(?:à|par|avec)[ \t]+)?(?:la|ta|ma|une|cette)[ \t]+)";

        /// <summary>Un verbe au futur ou au conditionnel, reconnu à sa terminaison.</summary>
        private const string Futur = @"\p{L}+(?:rai|ras|ra|ront|erons|irons|erez|irez|rais|rait|rions|riez|raient)";

        /// <summary>
        /// Pas juste après un verbe qui ANNONCE la question (#68 v5) : « Tu me fais
        /// signe et je te pose la question 1. » parle de la question à venir.
        /// </summary>
        private const string ApresVerbeDeLancement =
            @"(?<!\b(?:lance|lancer|lancerai|lancerais|lançons|pose|poser|poserai|poserais|posons|donne|donner|donnerai|donnerais|donnons"
            + @"|envoie|envoyer|enverrai|enverrais|affiche|afficher|afficherai|écris|écrire|écrirai)\s+(?:la|ta|ma|une|cette)\s+)";

        /// <summary>
        /// La réponse attend ENCORE le « prêt » de l'élève : c'est un aperçu du
        /// sujet, pas le top départ (#68 v4, Omar point 1 : « Tu es prêt ? »,
        /// « Dis-moi quand tu es prêt à démarrer, et je lance le chrono »).
        ///
        /// #68 v5 (Omar, régression v4) — reconnaît aussi :
        /// - « que tu es prêt » (« Dès que tu me dis que tu es prêt, je lance… »),
        ///   sauf après un verbe de constat (« Je vois que tu es prêt : Question 1 : … »)
        ///   ou « maintenant / vu que » ;
        /// - « quand tu veux » quand il s'agit de COMMENCER (« On commence quand tu
        ///   veux. », « Tu me dis quand tu veux et je lance la première question. ») —
        ///   pas « Réponds quand tu veux » après une question posée ;
        /// - « fais-moi signe » / « tu me fais signe », sauf « si tu bloques, fais-moi
        ///   signe » et « fais-moi signe si… » ;
        /// - le « go » qu'il doit dire (« Tu me dis go ? », gEO3-3 t2 ; « dis-moi
        ///   go », « ton go », « « go » ») — pas le « Go ! » du professeur lui-même.
        /// </summary>
        [GeneratedRegex(@"\bprête?s?\s*(?:à\s+(?:démarrer|commencer|te\s+lancer|y\s+aller|partir)\s*)?\?"
            + @"|\b(?:quand|dès\s+que|lorsque|si|une\s+fois\s+que|aussitôt\s+que)\s+tu\s+(?:es|seras|te\s+sens)\s+prête?s?\b"
            + @"|(?<!\b(?:vois|voit|sais|sens|sent|constate|remarque|comprends|maintenant|vu|sûr|sûre|certain|certaine|content|contente|ravi|ravie|heureux|heureuse)\s+)"
            + @"\bque\s+tu\s+(?:es|seras|te\s+sens)\s+prête?s?\b"
            + @"|\b(?:et|puis)\s+je\s+lance(?:rai)?\s+le\s+chrono"
            + @"|\b(?:on\s+(?:commence|démarre|y\s+va|attaque|lance|peut\s+(?:commencer|démarrer|y\s+aller))|commençons|démarrons|attaquons|je\s+(?:te\s+)?(?:lance|commence|démarre|pose|donne|envoie)\w*|c['’]est\s+parti)\b"
            + @"[^.!?\n]{0,60}\b(?:quand|dès\s+que|lorsque)\s+tu\s+(?:le\s+)?(?:veux|voudras|souhaites)\b"
            + @"|\b(?:quand|dès\s+que|lorsque)\s+tu\s+(?:le\s+)?(?:veux|voudras|souhaites)\s*[,!.]?\s*(?:(?:et|puis)\s+)?"
            + @"(?:on\s+(?:commence|démarre|y\s+va|attaque|lance)|je\s+(?:te\s+)?(?:lance|commence|démarre|pose|donne|envoie)|c['’]est\s+parti)"
            + @"|(?<!\bsi\s[^.!?\n]{0,40})\b(?:(?:fais|faites)[- ]moi|tu\s+me\s+fais|me\s+faire)\s+(?:un\s+)?signe\b"
            + @"(?!\s*(?:si\b|s['’]il|quand\s+tu\s+(?:as|bloques|ne)|dès\s+que\s+tu\s+(?:as|bloques)|en\s+cas))"
            + @"|\b(?:dis|dites|écris|écrivez|tape|tapez|envoie|réponds)(?:[- ]moi)?\s+[«""“]?\s*go\b"
            + @"|\btu\s+me\s+(?:dis|écris|tapes|envoies)\s+[«""“]?\s*go\b|\bton\s+[«""“]?\s*go\b|[«""“]\s*go\s*!?\s*[»""”]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex AttendLePret();

        /// <summary>
        /// … sauf si elle lui laisse, à la place, répondre tout de suite (« Tu me
        /// dis quand tu es prêt, OU tu réponds directement », wEO3-1 t2) : la
        /// question est posée et il peut y répondre, c'est parti. Le « ou » est
        /// exigé : « tu réponds directement ici » est une consigne (répondre à
        /// l'écran), pas un départ (bEO-2, bEO-5, bSO-7, bTO-2, bTO2-1 t2).
        /// </summary>
        [GeneratedRegex(@"\bou\s+(?:alors\s+)?(?:tu\s+(?:me\s+)?)?réponds(?:-moi)?\s+(?:directement|tout\s+de\s+suite)|\bou\s+(?:alors\s+)?tu\s+peux\s+(?:déjà\s+|directement\s+)?(?:me\s+)?répondre",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex InviteARepondre();

        /// <summary>
        /// LE FILET DU TOP DÉPART — #68 v3, Omar point 2 : 3 parcours sur 45 à
        /// l'ordinateur posaient « Question 1 » sans [DEBUT_EVALUATION]. Pour
        /// l'écran, le contrôle n'existait pas (ni chrono ni dépassement), et
        /// pour le serveur non plus (`EstOuvert` faux : pas de consigne du
        /// contrôle, pas de note). Même patron que le filet de la copie de
        /// contrôle : quand l'évaluation est ENGAGÉE et que la réponse pose la
        /// première question sans la balise, le code l'écrit pour lui.
        ///
        /// #68 v4 (Omar, rejeu sur tout le corpus des bancs v2/v3) :
        /// - pas de filet tant que la réponse attend le « prêt » (aperçu du
        ///   sujet) : sinon le contrôle s'ouvrait, et « Je suis prêt. » était
        ///   noté comme la réponse à la question 1 (10 aperçus sur le corpus) ;
        /// - le texte du bloc [EVALUATION] ne compte pas (« q1: » d'une note,
        ///   vEC-1 t4), et une réponse qui clôt (note, report, abandon, fin de
        ///   séance) n'est jamais un top départ ;
        /// - le motif reconnaît aussi « Question 1 sur 3 », « Première
        ///   question. », « Question 1 » seule sur sa ligne et « 1. … ».
        /// </summary>
        public static bool TopDepartSansBalise(string? reponse)
        {
            if (string.IsNullOrEmpty(reponse) || reponse.Contains(Debut, StringComparison.Ordinal)) return false;

            if (reponse.Contains(Abandon, StringComparison.Ordinal)
                || reponse.Contains("[EVALUATION_PREVUE]", StringComparison.OrdinalIgnoreCase)
                || reponse.Contains("[FIN_SEANCE]", StringComparison.Ordinal)
                || reponse.Contains("[RAPPORT]", StringComparison.OrdinalIgnoreCase)) return false;

            // #68 v8 (Omar, état armé : « Je corrige la première question : il manquait l'unité. Tu choisis
            // l'écrit ou l'oral ? ») : le support n'est pas encore choisi — la balise ouvrirait le contrôle avant
            // le choix et « à l'oral » serait noté comme la réponse à la question 1. Gardé en v9 : c'est la même
            // règle structurelle que `SupportChoisi` (une réponse qui repose la question du support la rouvre).
            if (reponse.Contains(BaliseSupport, StringComparison.OrdinalIgnoreCase)) return false;

            var texte = Bloc().Replace(reponse, string.Empty);

            if (AttendLePret().IsMatch(texte) && !InviteARepondre().IsMatch(texte)) return false;

            // #68 v10 (Omar point 1) : la règle STRUCTURELLE de la v8 revient, partie SUPPORT seulement — une réponse
            // qui FINIT en redemandant le support (« Alors, cahier ou ordinateur ? », « Tu choisis lequel ? ») n'est
            // pas un départ : l'élève doit encore choisir.
            if (FinitSurSupport().IsMatch(texte)) return false;

            return PremiereQuestion().IsMatch(texte);
        }

        /// <summary>
        /// La réponse FINIT par une demande de support (#68 v8, partie support remise en v10) : « Tu choisis l'écrit ou
        /// l'oral ? », « Cahier ou ordinateur ? », « Tu choisis lequel ? », « Lequel tu préfères ? ». La partie
        /// « pause » de la v8 n'est pas remise.
        /// </summary>
        [GeneratedRegex(@"(?:\b(?:écrit|cahier|papier|oral|ordinateur|ordi|écran|clavier)\b[^.!?\n]*\bou\b[^.!?\n]*\b(?:écrit|cahier|papier|oral|ordinateur|ordi|écran|clavier)\b[^.!?\n]*\?"
            + @"|\b(?:tu\s+(?:choisis|prends|préfères)\s+(?:lequel|laquelle)|(?:lequel|laquelle)\s+(?:tu\s+)?(?:choisis|prends|préfères))\b[^.!?\n]*\?)"
            + @"[\s\p{So}\p{Cs}*_»""”)]*\z",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex FinitSurSupport();

        /// <summary>La balise du choix du support (deux boutons à l'écran).</summary>
        public const string BaliseSupport = "[SUPPORT_EVALUATION]";

        /// <summary>Retire le bloc et le marqueur d'ouverture. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc(), Debut);
    }
}
