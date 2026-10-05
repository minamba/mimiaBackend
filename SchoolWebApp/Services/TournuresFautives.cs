using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// UNE FAUTE DE FRANÇAIS QUE LE PROFESSEUR SE RECOPIE — Camara, le
    /// 04/10/2026, en anglais : « la prof dit une phrase qui ne veut rien dire
    /// en français : "je te réécoute l'histoire" ».
    ///
    /// CE QUI S'EST PASSÉ, EN BASE. À la reprise d'une compréhension orale,
    /// Marine a écrit « Je te la réécoute » le 28/09, puis « je te réécoute
    /// l'histoire » la séance suivante, puis encore le 04/10. La première fois
    /// est une faute du modèle — il confond celui qui LIT et celui qui ÉCOUTE.
    /// Les suivantes sont une imitation : ses propres messages sont dans la
    /// fenêtre d'historique, et il reprend sa formule.
    ///
    /// La consigne de l'exercice dit désormais la bonne tournure. Mais une
    /// consigne ne vaut pas un fait, et ici le fait, c'est l'historique : tant
    /// que la phrase fautive y figure, elle reviendra. On la corrige donc là où
    /// elle se propage — dans le texte enregistré, et dans l'historique relu —
    /// par une règle étroite, qui ne touche à rien d'autre.
    ///
    /// ÉTROITE, PARCE QUE « JE TE RÉÉCOUTE » SEUL EST JUSTE : « redis-le, je te
    /// réécoute » veut bien dire que le professeur écoute l'élève. La faute,
    /// c'est le COMPLÉMENT : « je te LA réécoute », « je te réécoute
    /// L'HISTOIRE ». Seules ces deux formes sont réécrites.
    /// </summary>
    public static partial class TournuresFautives
    {
        /// <summary>« je te la réécoute », « je vous le réécoute ».</summary>
        [GeneratedRegex(@"\b(?<je>[Jj]e)\s+(?<a>te|vous)\s+(?<quoi>la|le|les)\s+réécoute\b")]
        private static partial Regex AvecPronom();

        /// <summary>« je te réécoute l'histoire », « je te réécoute ce passage ».</summary>
        [GeneratedRegex(
            @"\b(?<je>[Jj]e)\s+(?<a>te|vous)\s+réécoute(?=\s+(?:l['’]|le\s|la\s|les\s|ce\s|cet\s|cette\s|ces\s|ton\s|ta\s|tes\s|un\s|une\s|tout\s|toute\s))")]
        private static partial Regex AvecComplement();

        public static string Corriger(string? texte)
        {
            if (string.IsNullOrEmpty(texte)) return texte ?? string.Empty;
            if (!texte.Contains("réécoute", StringComparison.Ordinal)) return texte;

            var corrige = AvecPronom().Replace(texte, "${je} ${a} ${quoi} fais réécouter");
            return AvecComplement().Replace(corrige, "${je} ${a} fais réécouter");
        }
    }
}
