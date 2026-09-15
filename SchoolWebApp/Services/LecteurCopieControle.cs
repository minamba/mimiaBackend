using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>L'élève a répondu à « L'énoncé et ta copie sont-ils séparés ? ».</summary>
    public record ChoixCopie(int Controle, bool Separee);

    /// <summary>Une pièce envoyée depuis la carte de copie : l'énoncé, ou la copie.</summary>
    public record PieceCopie(int Controle, bool EstEnonce);

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
