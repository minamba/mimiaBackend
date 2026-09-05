using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les adresses à qui le service est fermé.
    ///
    /// LA NORMALISATION EST FAITE ICI, PAS PAR L'APPELANT. Une adresse arrive
    /// d'un formulaire, d'un compte, ou d'un collage : « Jean@X.fr  » et
    /// « jean@x.fr » désignent la même personne, et laisser chaque appelant s'en
    /// souvenir garantit que l'un d'eux l'oubliera. Le bannissement tiendrait
    /// alors sur une casse.
    /// </summary>
    public interface IBannissementRepository
    {
        /// <summary>Toute la liste, la plus récente en tête.</summary>
        Task<IEnumerable<MailBanniVue>> GetTousAsync(CancellationToken ct = default);

        /// <summary>
        /// Cette adresse est-elle bannie ?
        ///
        /// La seule question que pose le serveur d'identité — mais il ne passe
        /// PAS par ici : il lit la base directement, pour ne dépendre d'aucun
        /// autre service au moment de la connexion. Cette méthode-ci sert à
        /// l'API et aux écrans.
        /// </summary>
        Task<bool> EstBanniAsync(string? mail, CancellationToken ct = default);

        /// <summary>
        /// Ajoute une adresse. Rend faux si elle y était déjà.
        ///
        /// PAS UNE ERREUR QUAND ELLE Y EST DÉJÀ : bannir deux fois quelqu'un
        /// n'a aucun sens, et faire échouer le second appel obligerait chaque
        /// écran à distinguer « déjà banni » de « impossible de bannir ». Le
        /// résultat voulu est atteint dans les deux cas.
        /// </summary>
        Task<bool> BannirAsync(
            string mail, string? motif, string? par, CancellationToken ct = default);

        /// <summary>Retire une adresse de la liste. Faux si elle n'y était pas.</summary>
        Task<bool> LeverAsync(string mail, CancellationToken ct = default);
    }
}
