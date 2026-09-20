namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une ligne de la grille tarifaire, telle que l'écran « Anthropic / OpenAI ›
    /// Tarifs » la montre : le prix officiel relevé, le prix appliqué dans nos
    /// calculs, et ce que l'un dit de l'autre.
    /// </summary>
    public class LigneTarif
    {
        public int Id { get; set; }

        public string Fournisseur { get; set; } = string.Empty;

        public string Modele { get; set; } = string.Empty;

        public string? Usage { get; set; }

        public decimal? PrixEntree { get; set; }

        public decimal? PrixSortie { get; set; }

        public decimal? PrixMinute { get; set; }

        public decimal? PrixEntreeApplique { get; set; }

        public decimal? PrixSortieApplique { get; set; }

        public decimal? PrixMinuteApplique { get; set; }

        /// <summary>Le jour où le prix a changé pour la dernière fois.</summary>
        public DateTime DateMiseAJour { get; set; }

        /// <summary>La dernière lecture de la page du fournisseur par la veille.</summary>
        public DateTime? DerniereVerification { get; set; }

        /// <summary>
        /// <c>a_jour</c> : nos calculs utilisent le prix officiel relevé.
        /// <c>pas_a_jour</c> : ils utilisent un autre prix — ne devrait plus
        /// arriver depuis que la veille et la correction manuelle appliquent
        /// tout de suite, mais une donnée ancienne le dirait.
        /// <c>non_compte</c> : la ligne n'entre dans aucun calcul.
        /// </summary>
        public string Statut =>
            PrixEntreeApplique is null && PrixSortieApplique is null && PrixMinuteApplique is null
                ? StatutsTarif.NonCompte
                : PrixEntreeApplique == PrixEntree
                  && PrixSortieApplique == PrixSortie
                  && PrixMinuteApplique == PrixMinute
                    ? StatutsTarif.AJour
                    : StatutsTarif.PasAJour;
    }

    public static class StatutsTarif
    {
        public const string AJour = "a_jour";
        public const string PasAJour = "pas_a_jour";
        public const string NonCompte = "non_compte";
    }
}
