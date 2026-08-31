using System.Text;

namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Le repère qui dit où reprendre un historique.
    ///
    /// POURQUOI LA DATE NE SUFFIT PAS
    /// -----------------------------
    /// Deux comptes rendus peuvent porter le même instant : l'élève enchaîne
    /// deux matières, les deux séances se concluent dans la même seconde, et
    /// SQL Server range les DATETIME2 par pas de cent nanosecondes mais rien
    /// n'oblige deux lignes à différer. Un curseur qui ne retiendrait que la
    /// date sauterait alors la jumelle de la dernière ligne servie — une
    /// séance disparue de l'historique, sans que personne ne le remarque.
    ///
    /// L'identifiant tranche cette égalité. Il croît avec l'insertion, donc
    /// (date décroissante, id décroissant) est un ordre TOTAL : chaque ligne a
    /// une place et une seule, et « ce qui vient après celle-ci » ne désigne
    /// jamais deux choses.
    ///
    /// POURQUOI C'EST ENCODÉ
    /// --------------------
    /// Pas pour cacher quoi que ce soit — un curseur n'est pas un secret, il ne
    /// donne accès à rien que l'appelant n'ait déjà. C'est pour qu'il reste
    /// opaque À L'USAGE : un client qui lirait « 638…_412 » finirait par le
    /// fabriquer lui-même, et le format deviendrait un contrat qu'on ne peut
    /// plus changer.
    /// </summary>
    public static class CurseurHistorique
    {
        public static string Ecrire(DateTime date, int id) =>
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{date.Ticks}_{id}"));

        /// <summary>
        /// Relit un curseur. Rend `null` sur tout ce qui n'en est pas un —
        /// curseur tronqué par un copier-coller, forgé à la main, ou hérité
        /// d'une version antérieure. L'appelant repart alors du début de
        /// l'historique : une première page servie deux fois est un incident
        /// invisible, une erreur 400 devant un parent n'en est pas un.
        /// </summary>
        public static (DateTime Date, int Id)? Lire(string? curseur)
        {
            if (string.IsNullOrWhiteSpace(curseur)) return null;

            try
            {
                var texte = Encoding.ASCII.GetString(Convert.FromBase64String(curseur));
                var morceaux = texte.Split('_');

                if (morceaux.Length != 2) return null;
                if (!long.TryParse(morceaux[0], out var ticks)) return null;
                if (!int.TryParse(morceaux[1], out var id)) return null;
                if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) return null;

                // Les dates sont stockées en UTC : le curseur doit repartir dans
                // le même fuseau, sinon la comparaison décale de deux heures en
                // été et la page suivante recouvre la précédente.
                return (new DateTime(ticks, DateTimeKind.Utc), id);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
