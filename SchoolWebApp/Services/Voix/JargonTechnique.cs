using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// JAMAIS DE « CODE » DANS LA BOUCHE DU PROFESSEUR — signalement #51 (Adam, 20/11/2026) : en lecture des nombres,
    /// Nora a dit à un enfant de CP « c'est le code qui va choisir un nombre pour toi ». Elle recopiait le fait qu'on lui
    /// donnait (« C'est le CODE qui choisit le nombre »). Deux parades, pour les exercices tenus par l'application
    /// (lecture des nombres, lecture en images) :
    /// - LES FAITS ne parlent plus du code, et disent au professeur que, pour l'enfant, c'est LUI qui montre le tableau
    ///   (<see cref="Regle"/>) ;
    /// - LE FILET : une phrase de son message qui parle du fonctionnement interne (le code, l'application, le
    ///   programme, l'algorithme, l'ordinateur) s'en va avant d'arriver à l'enfant (<see cref="Retirer"/>).
    /// </summary>
    public static partial class JargonTechnique
    {
        /// <summary>La règle, jointe aux faits des exercices tenus par l'application.</summary>
        public const string Regle =
            "Pour l'élève, c'est TOI qui lui montres ce qui est au tableau : ne lui parle JAMAIS de « code », "
            + "d'« application », de « programme » ni de qui choisit, et ne dis jamais que ce n'est pas toi qui choisis.";

        // « c'est le code qui choisit », « l'application va t'afficher », « le programme a choisi », « l'ordinateur
        // choisit ». Pas « code secret » ni « code d'accès » (des mots d'enfant), ni « au programme du CP » (ce n'est pas
        // le fonctionnement interne) : « programme » seulement suivi d'un verbe qui agit.
        [GeneratedRegex(
            @"[^.!?\n]*(?:"
            + @"(?<!\p{L})(?:le|du|au|un|ce|mon|notre|son)\s+code(?!\p{L})(?!\s+(?:secret|d['’]\s*acc[eè]s|de\s+la\s+porte))"
            + @"|(?<!\p{L})l['’]\s*(?:application|appli|algorithme|ordinateur)(?!\p{L})"
            + @"|(?<!\p{L})(?:le|ce)\s+(?:programme|logiciel|syst[èe]me)\s+(?:qui\s+|va\s+|vient\s+de\s+|a\s+|t['’]\s*|te\s+|le\s+|la\s+|l['’]\s*)*(?:choisi\w*|affich\w*|montr\w*|d[ée]cid\w*|chang\w*|met\w*|mis|pass\w*|tir\w*|s['’]\s*occup\w*|g[èe]r\w*)"
            + @")[^.!?\n]*[.!]*",
            RegexOptions.IgnoreCase)]
        private static partial Regex PhraseTechnique();

        /// <summary>Le message sans ses phrases sur le fonctionnement interne. Le reste ne bouge pas.</summary>
        public static string Retirer(string? texte) => PhraseTechnique().Replace(texte ?? string.Empty, string.Empty);

        /// <summary>Le message parle-t-il du fonctionnement interne ? (pour le journal)</summary>
        public static bool Contient(string? texte) => !string.IsNullOrEmpty(texte) && PhraseTechnique().IsMatch(texte);
    }
}
