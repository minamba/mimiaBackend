namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un appel au modèle qui N'EST PAS un tour de dialogue.
    ///
    /// POURQUOI CETTE TABLE EXISTE
    /// ---------------------------
    /// Quatre choses appellent Claude dans l'application, et une seule laissait
    /// une trace : le dialogue, dont chaque tour porte ses jetons sur la table
    /// `Message`. Les trois autres — décrire une planche, en cartographier les
    /// repères, observer les compétences d'une séance — tournent en tâche de
    /// fond toutes les une à dix minutes et ne disaient rien de ce qu'elles
    /// consommaient.
    ///
    /// Conséquence vécue : une facture qui monte sans que le tableau de bord ne
    /// bouge, et une demi-journée d'enquête dans les journaux pour deviner
    /// laquelle des trois s'emballait. Ce qu'on ne mesure pas, on l'accuse ou
    /// on l'innocente au hasard.
    ///
    /// POURQUOI UNE TABLE À PART ET NON UNE COLONNE SUR `Message`
    /// ----------------------------------------------------------
    /// Un message appartient à une conversation, donc à un élève, donc à un
    /// parent. Ces appels-là n'appartiennent à personne : décrire une planche
    /// sert à toutes les familles à la fois. Les loger dans `Message`
    /// obligerait à inventer une conversation fictive, et fausserait du même
    /// coup le coût par famille.
    ///
    /// LA VOIX Y EST AUSSI, DEPUIS LE 03/10/2026 (origine `voix`), et elle est
    /// l'exception à ce qui précède : une phrase prononcée appartient bien à
    /// une famille. Mais un tour de parole fait plusieurs appels de synthèse,
    /// et la synthèse ne connaît pas le message qu'elle lit : une ligne par
    /// appel, avec les jetons facturés par le fournisseur, et en `Reference`
    /// le « sub » du parent. Avant cette date, la voix était ESTIMÉE à partir
    /// du nombre de tours — et l'estimation valait le double de la réalité.
    /// </summary>
    public class AppelClaude
    {
        public int Id { get; set; }

        /// <summary>
        /// Qui a appelé : `description-planche`, `reperes-planche`,
        /// `transcription-document`, `observation-competences`, `bilan`.
        ///
        /// Une chaîne et non une énumération : la valeur est écrite une fois et
        /// relue en agrégat. Une énumération obligerait à une migration chaque
        /// fois qu'un nouveau worker appelle le modèle, et c'est précisément le
        /// moment où l'on veut que ce soit facile.
        /// </summary>
        public string Origine { get; set; } = string.Empty;

        public string? Modele { get; set; }

        public int TokensEntree { get; set; }

        public int TokensSortie { get; set; }

        /// <summary>
        /// POUR L'ORIGINE `voix`, CES DEUX COLONNES NE SONT PAS DES JETONS —
        /// depuis le 04/10/2026. La voix n'a pas de cache, et son coût ne lit
        /// que l'entrée et la sortie : elles portent le verdict de la garde de
        /// redite, en centièmes de seconde, pour qu'on puisse le relire sans la
        /// console.
        ///
        ///   `TokensCacheLecture`  : le son relu par la transcription. 0 = rien
        ///                           à relire ; -1 = la garde a renoncé.
        ///   `TokensCacheEcriture` : le son RETIRÉ parce qu'il redisait le
        ///                           texte. Plus de 0 = une redite coupée.
        ///
        /// Une redite fabriquée et non coupée se lit donc : sortie double de
        /// l'ordinaire, et rien de retiré.
        /// </summary>
        public int TokensCacheLecture { get; set; }

        public int TokensCacheEcriture { get; set; }

        /// <summary>
        /// De quoi il s'agissait — la clé d'une planche, l'identifiant d'une
        /// séance. Libre et facultatif : sert à retrouver le coupable quand un
        /// poste s'emballe, pas à établir une comptabilité.
        /// </summary>
        public string? Reference { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
