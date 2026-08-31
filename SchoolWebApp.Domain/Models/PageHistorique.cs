namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une tranche d'un historique, avec de quoi demander la suivante.
    ///
    /// POURQUOI UN CURSEUR ET NON UN NUMÉRO DE PAGE
    /// -------------------------------------------
    /// « Page 3 » se traduit en SQL par OFFSET 20 : la base doit lire et jeter
    /// les vingt premières lignes avant de servir la vingt-et-unième. Le coût
    /// grandit avec la profondeur, alors que le curseur attaque directement
    /// l'index — la trentième page coûte exactement ce que coûte la première.
    ///
    /// Et surtout, l'OFFSET compte des LIGNES, pas des données. Un parent lit
    /// la fiche pendant que son enfant termine une séance : le compte rendu qui
    /// s'insère en tête décale tout d'un rang, et la page suivante lui
    /// re-servira la ligne qu'il vient de lire. Le curseur dit « ce qui est
    /// plus ancien que CELLE-CI » — une insertion en tête ne le déplace pas.
    /// </summary>
    public class PageHistorique<T>
    {
        public IEnumerable<T> Elements { get; set; } = [];

        /// <summary>
        /// Le nombre total de lignes de l'historique, pas seulement de la page.
        /// C'est ce qui permet d'écrire « 10 sur 214 » : sans lui, l'écran ne
        /// peut pas dire au parent ce qu'il ne voit pas encore.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Le curseur à renvoyer pour la tranche suivante, `null` quand on est
        /// au bout. Opaque : le client le repasse tel quel sans le lire, ce qui
        /// laisse libre d'en changer le contenu plus tard.
        /// </summary>
        public string? Suite { get; set; }
    }
}
