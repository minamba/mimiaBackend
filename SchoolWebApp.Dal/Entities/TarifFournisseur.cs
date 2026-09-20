namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une ligne de la grille tarifaire d'un fournisseur d'IA — voulue par
    /// Camara le 19/09/2026, le jour où l'on a découvert que le coût affiché
    /// comptait Sonnet 5 à 3 $ / 15 $ alors que la facture disait 2 $ / 10 $.
    ///
    /// C'EST ELLE QUI SERT AU CALCUL DES COÛTS DE L'ADMINISTRATION, et plus un
    /// tarif écrit dans le code. Quand Anthropic ou OpenAI change sa grille, on
    /// la corrige dans l'écran « Anthropic / OpenAI › Tarifs », sans redéployer,
    /// et la date de mise à jour dit à quand remonte la dernière relecture.
    ///
    /// DEUX FAMILLES DE PRIX : les modèles de texte se paient au million de
    /// jetons (entrée, sortie), la voix à la minute d'audio. Une ligne ne remplit
    /// que ceux qui la concernent.
    /// </summary>
    public class TarifFournisseur
    {
        public int Id { get; set; }

        /// <summary><c>anthropic</c> ou <c>openai</c>.</summary>
        public string Fournisseur { get; set; } = string.Empty;

        /// <summary>
        /// L'identifiant du modèle, tel que l'API le renvoie — ou son début :
        /// « claude-haiku-4-5 » vaut aussi pour « claude-haiku-4-5-20251001 ».
        /// </summary>
        public string Modele { get; set; } = string.Empty;

        /// <summary>À quoi il sert dans Mimia, en clair.</summary>
        public string? Usage { get; set; }

        // ------------------------------------------------ le prix OFFICIEL
        // Celui qu'on relève sur le site du fournisseur, à la main.

        /// <summary>Dollars par million de jetons d'entrée.</summary>
        public decimal? PrixEntree { get; set; }

        /// <summary>Dollars par million de jetons de sortie.</summary>
        public decimal? PrixSortie { get; set; }

        /// <summary>Dollars par minute d'audio (voix, transcription).</summary>
        public decimal? PrixMinute { get; set; }

        // ------------------------------------------------ le prix APPLIQUÉ
        // Celui que les calculs de coût utilisent vraiment. NULL partout : la
        // ligne n'entre dans aucun calcul (la transcription, par exemple).
        //
        // DEUX PRIX ET NON UN, voulu par Camara le 19/09/2026 : « un statut à
        // jour ou pas à jour par rapport à nos calculs ». Relever un nouveau
        // tarif ne change rien tant qu'on ne l'a pas appliqué ; l'écart se voit,
        // et on décide de basculer en connaissance de cause.

        public decimal? PrixEntreeApplique { get; set; }

        public decimal? PrixSortieApplique { get; set; }

        public decimal? PrixMinuteApplique { get; set; }

        public int Ordre { get; set; }

        /// <summary>Le jour où le prix a changé pour la dernière fois.</summary>
        public DateTime DateMiseAJour { get; set; }

        /// <summary>
        /// La dernière lecture de la page du fournisseur par la veille, que le
        /// prix ait changé ou non. Vide tant qu'elle n'est jamais passée.
        /// </summary>
        public DateTime? DerniereVerification { get; set; }
    }
}
