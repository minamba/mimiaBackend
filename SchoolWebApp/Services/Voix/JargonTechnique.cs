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
        // choisit », « c'est l'exercice qui choisit le nombre », « la machine choisit », « le site va t'afficher »,
        // « ce n'est pas moi qui choisis, c'est le jeu ».
        //
        // CONTRÔLE D'OMAR (#50/#51) : le filet retirait aussi des phrases de classe (« le code du coffre », « le code de
        // la route », « code postal », « au CM1, le programme change », « le programme te montre… », « l'algorithme de
        // l'addition posée », « l'ordinateur compte en binaire », « l'application des règles »). Désormais, un de ces
        // mots ne compte que SUJET d'un verbe qui agit sur l'exercice (choisir, afficher, décider, tirer au sort,
        // sélectionner, générer, mettre, s'occuper / se charger de) — jamais « change », « montre » ni « compte ».
        [GeneratedRegex(
            @"[^.!?\n]*(?:"
            + @"(?:(?<!\p{L})(?:le|la|un|une|ce|cet|cette|mon|ma|notre|son|sa|du|au)\s+|(?<!\p{L})l['’]\s*)"
            + @"(?:code|algorithme|ordinateur|application|appli|programme|logiciel|syst[èe]me|machine|site|jeu|exercice|outil|robot)(?!\p{L})"
            + @"(?:\s*(?:(?:qui|va|vont|vient\s+de|a|te|le|la|les|en|me|lui|nous|tout\s+seul|lui-m[êe]me|au\s+hasard|pour\s+toi)(?!\p{L})|[tlms]['’]))*\s*"
            + @"(?:choisi\w*|choisir|affich\w*|d[ée]cid\w*|tir\w*|s[ée]lectionn\w*|g[ée]n[èe]r\w*|met|mettra|mettre|mis|s['’]\s*occup\w*|occup\w*|s['’]\s*en\s+charg\w*|en\s+charg\w*|se\s+charg\w*|charg\w*)(?!\p{L})"
            + @"|(?<!\p{L})(?:ce\s+n['’]\s*est|c['’]\s*est|ce\s+n['’]\s*sera)\s+pas\s+moi\s+qui\s+(?:choisi\w*|d[ée]cid\w*|affich\w*)"
            + @")[^.!?\n]*[.!?]*",
            RegexOptions.IgnoreCase)]
        private static partial Regex PhraseTechnique();

        /// <summary>Le message sans ses phrases sur le fonctionnement interne. Le reste ne bouge pas.</summary>
        public static string Retirer(string? texte) => PhraseTechnique().Replace(texte ?? string.Empty, string.Empty);

        /// <summary>Le message parle-t-il du fonctionnement interne ? (pour le journal)</summary>
        public static bool Contient(string? texte) => !string.IsNullOrEmpty(texte) && PhraseTechnique().IsMatch(texte);
    }
}
