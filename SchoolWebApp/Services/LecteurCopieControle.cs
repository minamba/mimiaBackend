using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>L'élève a répondu à « L'énoncé et ta copie sont-ils séparés ? ».</summary>
    public record ChoixCopie(int Controle, bool Separee);

    /// <summary>Une pièce envoyée depuis la carte de copie : l'énoncé, ou la copie.</summary>
    public record PieceCopie(int Controle, bool EstEnonce);

    /// <summary>
    /// L'élève a choisi comment corriger sa copie : <c>aide</c> (« Je t'aide à
    /// te corriger ») ou <c>explique</c> (« Je corrige et t'explique »).
    /// </summary>
    public record StyleCorrectionChoisi(int Controle, string Style);

    /// <summary>
    /// Où en est la correction, erreur par erreur : l'erreur en cours (null
    /// tant qu'aucune n'est présentée), le total s'il est connu, le style
    /// choisi POUR CETTE ERREUR (null = la question est à l'écran), et si
    /// tout est fini.
    /// </summary>
    public record EtatCorrectionCopie(int? Erreur, int? Total, string? Style, bool Termine);

    /// <summary>
    /// LA COPIE D'UN CONTRÔLE PASSÉ, REGARDÉE AVEC LE PROFESSEUR — voulu par
    /// Camara le 13/09/2026.
    ///
    /// Trois signaux, deux auteurs :
    ///   - le PROFESSEUR écrit [COPIE_CONTROLE] quand il propose de regarder
    ///     la copie : l'écran affiche la question et les boutons ;
    ///   - l'ÉLÈVE répond Oui/Non, puis envoie ses pièces. Chacun de ses gestes
    ///     part avec un marqueur en français, lisible tel quel par le
    ///     professeur dans l'historique : « [ÉNONCÉ DU CONTRÔLE n° 42] ».
    ///
    /// LES MARQUEURS DE L'ÉLÈVE SONT ÉCRITS PAR LE NAVIGATEUR, pas tapés :
    /// `src/lib/storage/copieControle.js` les produit, et les deux côtés
    /// doivent rester d'accord mot pour mot. Les motifs tolèrent quand même
    /// les accents manquants — un marqueur qui passe par un copier-coller ne
    /// doit pas se perdre pour un « E » sans accent.
    /// </summary>
    public static partial class LecteurCopieControle
    {
        [GeneratedRegex(@"\[COPIE_CONTROLE\](?<corps>.*?)\[/COPIE_CONTROLE\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Demande();

        [GeneratedRegex(@"\d+")]
        private static partial Regex Numero();

        [GeneratedRegex(@"\[COPIE DU CONTR[ÔO]LE n°\s?(?<id>\d+)\s?:\s?(?<choix>[^\]]+)\]",
            RegexOptions.IgnoreCase)]
        private static partial Regex Choix();

        // Crochet fermant JUSTE après le numéro : c'est ce qui distingue une
        // pièce (« [COPIE DU CONTRÔLE n° 42] ») d'un choix (« … n° 42 : … »).
        [GeneratedRegex(@"\[(?<role>[ÉE]NONC[ÉE]|COPIE) DU CONTR[ÔO]LE n°\s?(?<id>\d+)\]",
            RegexOptions.IgnoreCase)]
        private static partial Regex Piece();

        // « [CORRECTION DU CONTRÔLE n° 42 : aide-moi à me corriger] » ou
        // « … : corrige et explique-moi ». Écrit par `copieControle.js`.
        [GeneratedRegex(@"\[CORRECTION DU CONTR[ÔO]LE n°\s?(?<id>\d+)\s?:\s?(?<choix>[^\]]+)\]",
            RegexOptions.IgnoreCase)]
        private static partial Regex Style();

        /// <summary>Le numéro du contrôle dont le professeur propose de regarder la copie. Null sinon.</summary>
        public static int? LireDemande(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Demande().Match(message);
            if (!bloc.Success) return null;

            var numero = Numero().Match(bloc.Groups["corps"].Value);
            return numero.Success && int.TryParse(numero.Value, out var id) ? id : null;
        }

        // « Envoie-moi ta copie », « tu peux me montrer l'énoncé ? », « prends
        // ta copie en photo » : un verbe d'envoi, puis la copie ou l'énoncé,
        // dans la même phrase.
        [GeneratedRegex(
            @"\b(envoie|envoies|envoyer|montre|montres|montrer|transmets|transmettre|partage|partager|d[ée]pose|d[ée]poser|importe|importer|scanne|scanner|photographie|photographier|prends?\s+en\s+photo)\b[^.?!\n]{0,80}\b(copie|copies|[ée]nonc[ée]s?|sujet)\b",
            RegexOptions.IgnoreCase)]
        private static partial Regex VerbeAvantCopie();

        // « ta copie, tu peux me l'envoyer ? »
        [GeneratedRegex(
            @"\b(copie|[ée]nonc[ée])\b[^.?!\n]{0,60}\b(m'envoyer|me\s+l'envoyer|me\s+les\s+envoyer|me\s+la\s+montrer|me\s+les\s+montrer|me\s+l'envoies|me\s+les\s+envoies)\b",
            RegexOptions.IgnoreCase)]
        private static partial Regex CopieAvantVerbe();

        [GeneratedRegex(@"contr[ôo]le|\bcopie|[ée]nonc[ée]|\bnote\b|/\s?20\b|sur\s+20\b", RegexOptions.IgnoreCase)]
        private static partial Regex ParleDuControle();

        [GeneratedRegex(@"dict[ée]e|\[DICTEE", RegexOptions.IgnoreCase)]
        private static partial Regex ParleDeDictee();

        /// <summary>
        /// LE PROFESSEUR DEMANDE-T-IL LA COPIE OU L'ÉNONCÉ D'UN CONTRÔLE, SANS
        /// AVOIR ÉCRIT [COPIE_CONTROLE] ?
        ///
        /// Relevé par Camara le 13/09/2026 : « Envoie-moi ta copie et l'énoncé,
        /// on regarde ça ensemble » suivi de [DEMANDE_DOCUMENT] — le signal
        /// générique — et la question « L'énoncé et ta copie sont-ils
        /// séparés ? » n'est jamais apparue. Sa règle : dès qu'il s'agit d'une
        /// copie ou d'un énoncé de contrôle, la question passe, OBLIGATOIREMENT.
        ///
        /// Vrai si la réponse demande une copie ou un énoncé, OU si elle pose
        /// [DEMANDE_DOCUMENT] alors que l'échange parle du contrôle. Jamais
        /// pendant une dictée : « rends-moi ta copie » y a un autre sens.
        /// </summary>
        public static bool DemandeLaCopieSansLeBloc(string? reponse, string? messageEleve)
        {
            if (string.IsNullOrWhiteSpace(reponse)) return false;
            if (LireDemande(reponse) is not null) return false;
            if (ParleDeDictee().IsMatch(reponse) || ParleDeDictee().IsMatch(messageEleve ?? "")) return false;

            if (VerbeAvantCopie().IsMatch(reponse) || CopieAvantVerbe().IsMatch(reponse)) return true;

            return reponse.Contains("[DEMANDE_DOCUMENT]", StringComparison.Ordinal)
                && (ParleDuControle().IsMatch(reponse) || ParleDuControle().IsMatch(messageEleve ?? ""));
        }

        public static ChoixCopie? LireChoix(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var choix = Choix().Match(message);
            if (!choix.Success || !int.TryParse(choix.Groups["id"].Value, out var id)) return null;

            // « énoncé et réponses sur la MÊME copie » = non séparés. Tout le
            // reste — « énoncé et copie séparés » — veut dire oui.
            var texte = choix.Groups["choix"].Value;
            var memeCopie = texte.Contains("même", StringComparison.OrdinalIgnoreCase)
                || texte.Contains("meme", StringComparison.OrdinalIgnoreCase);

            return new ChoixCopie(id, Separee: !memeCopie);
        }

        // LA CORRECTION ERREUR PAR ERREUR — Camara, le 26/09/2026. Le professeur
        // ouvre chaque erreur par « [ERREUR n/N] » et clôt par
        // « [CORRECTION_TERMINEE] ». Lu aussi par `copieControle.js`.
        [GeneratedRegex(@"\[ERREUR\s+(?<n>\d+)(?:\s*/\s*(?<total>\d+))?\]", RegexOptions.IgnoreCase)]
        private static partial Regex Erreur();

        public const string CorrectionTerminee = "[CORRECTION_TERMINEE]";

        /// <summary>
        /// L'ÉTAT DE LA CORRECTION, lu dans l'historique — la même règle que
        /// l'écran, à la lettre (`lireCorrection` dans copieControle.js) : deux
        /// calculs divergents afficheraient une question à l'élève en en
        /// annonçant une autre au professeur.
        ///
        /// Depuis la dernière demande de copie de ce contrôle : la dernière
        /// balise [ERREUR n/N] donne l'erreur en cours ; le dernier choix de
        /// l'élève APRÈS elle donne son style ; [CORRECTION_TERMINEE] après
        /// elle clôt tout. <paramref name="contenuEleve"/> est le message de ce
        /// tour, pas encore dans l'historique.
        /// </summary>
        public static EtatCorrectionCopie EtatCorrection(
            IReadOnlyList<(string Role, string Contenu)> messages, string? contenuEleve, int controle)
        {
            var debut = 0;
            for (var i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].Role == "assistant" && LireDemande(messages[i].Contenu) == controle)
                {
                    debut = i;
                    break;
                }
            }

            int? erreur = null;
            int? total = null;
            var indexErreur = -1;
            var termine = false;

            for (var i = debut; i < messages.Count; i++)
            {
                if (messages[i].Role != "assistant") continue;

                var contenu = messages[i].Contenu ?? string.Empty;
                var balises = Erreur().Matches(contenu);
                var derniere = balises.Count > 0 ? balises[^1] : null;
                var fin = contenu.LastIndexOf(CorrectionTerminee, StringComparison.OrdinalIgnoreCase);

                if (derniere is not null)
                {
                    indexErreur = i;
                    erreur = int.Parse(derniere.Groups["n"].Value);
                    if (derniere.Groups["total"].Success) total = int.Parse(derniere.Groups["total"].Value);
                    termine = false;
                }

                if (fin >= 0 && (derniere is null || fin > derniere.Index)) termine = true;
            }

            string? style = null;

            if (indexErreur >= 0 && !termine)
            {
                var suivants = messages.Skip(indexErreur + 1)
                    .Where(m => m.Role == "user")
                    .Select(m => m.Contenu)
                    .Append(contenuEleve);

                foreach (var contenu in suivants)
                {
                    if (LireStyle(contenu) is { } choix && choix.Controle == controle) style = choix.Style;
                }
            }

            return new EtatCorrectionCopie(erreur, total, style, termine);
        }

        /// <summary>
        /// Le choix « Je t'aide à te corriger » / « Je corrige et t'explique »,
        /// ou null. « aide-moi » ou « me corriger » veut dire qu'il se corrige
        /// lui-même ; tout le reste, qu'il veut la correction expliquée.
        /// </summary>
        public static StyleCorrectionChoisi? LireStyle(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var style = Style().Match(message);
            if (!style.Success || !int.TryParse(style.Groups["id"].Value, out var id)) return null;

            var texte = style.Groups["choix"].Value;
            var seul = texte.Contains("aide-moi", StringComparison.OrdinalIgnoreCase)
                || texte.Contains("me corriger", StringComparison.OrdinalIgnoreCase);

            return new StyleCorrectionChoisi(id,
                seul ? global::SchoolWebApp.Domain.Models.StylesCorrection.Aide : global::SchoolWebApp.Domain.Models.StylesCorrection.Explique);
        }

        /// <summary>
        /// TOUTES les pièces annoncées dans le message, dans l'ordre — depuis
        /// le 26/09/2026, l'énoncé et la copie partent ENSEMBLE, dans un seul
        /// message, une fois les deux importés. Les documents du message sont
        /// dans le même ordre que les marqueurs.
        /// </summary>
        public static IReadOnlyList<PieceCopie> LirePieces(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return Array.Empty<PieceCopie>();

            return Piece().Matches(message)
                .Where(m => int.TryParse(m.Groups["id"].Value, out _))
                .Select(m => new PieceCopie(
                    int.Parse(m.Groups["id"].Value),
                    !m.Groups["role"].Value.StartsWith("COPIE", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        /// <summary>
        /// La réponse sans le bloc [COPIE_CONTROLE] de ce contrôle — quand la
        /// copie est DÉJÀ en cours. Relevé par Camara le 26/09/2026 : l'énoncé
        /// reçu, le professeur réécrivait le bloc en réclamant la copie, et
        /// chaque bloc repartait de zéro — la question « séparés ? » revenait
        /// et l'énoncé était oublié. Le bloc en double part du texte gardé.
        /// </summary>
        public static string SansDemande(string texte, int controle) =>
            Demande().Replace(texte, m =>
            {
                var numero = Numero().Match(m.Groups["corps"].Value);
                return numero.Success && numero.Value == controle.ToString() ? string.Empty : m.Value;
            }).Trim();

        public static PieceCopie? LirePiece(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var piece = Piece().Match(message);
            if (!piece.Success || !int.TryParse(piece.Groups["id"].Value, out var id)) return null;

            var estEnonce = !piece.Groups["role"].Value.StartsWith("COPIE", StringComparison.OrdinalIgnoreCase);
            return new PieceCopie(id, estEnonce);
        }
    }
}
