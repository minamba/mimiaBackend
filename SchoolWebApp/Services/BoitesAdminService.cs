using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// La boîte de la personne connectée, prête à servir — ou pourquoi elle ne
    /// l'est pas (17/10/2026). <c>Raison</c> : « a_configurer » (première
    /// visite), « cle_absente » (la clé de chiffrement manque sur le serveur),
    /// « illisible » (mot de passe chiffré avec une autre clé : à ressaisir).
    /// </summary>
    public record BoiteResolue(
        SignataireAdmin Qui,
        IMessagerieService? Boite,
        string? Raison,
        string Role,
        string? Adresse)
    {
        public Expediteur? Expediteur => Boite is null || Adresse is null
            ? null
            : new Expediteur(Adresse, NomAffiche(Qui));

        public static string? NomAffiche(SignataireAdmin qui)
        {
            var nom = string.Join(' ', new[] { qui.Prenom, qui.Nom }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.IsNullOrWhiteSpace(nom) ? null : $"{nom} — Mimia";
        }
    }

    /// <summary>
    /// QUI ENVOIE UN NOUVEAU MESSAGE (17/10/2026) : sa boîte, à son nom et avec sa
    /// signature — ou la boîte commune du support, signée « L'équipe Mimia ».
    /// <c>Boite</c> range la copie dans « Messages envoyés » de la bonne boîte.
    /// </summary>
    public record ExpediteurMessage(IMessagerieService Boite, Expediteur Expediteur, string Signature);

    public interface IBoitesAdminService
    {
        /// <summary>
        /// L'expéditeur d'un nouveau message : <paramref name="support"/> vrai pour
        /// la boîte commune. Null + la boîte non prête (409 à l'écran) sinon.
        /// </summary>
        Task<(ExpediteurMessage? Expediteur, BoiteResolue? NonPrete)> ExpediteurAsync(bool support, CancellationToken ct = default);

        /// <summary>Null si la personne connectée n'a pas de compte parent.</summary>
        Task<BoiteResolue?> ResoudreAsync(CancellationToken ct = default);

        /// <summary>La signature de la personne connectée, mise en page ; l'ancienne signature fixe à défaut.</summary>
        Task<string> SignatureHtmlAsync(BoiteResolue boite, CancellationToken ct = default);
    }

    /// <summary>
    /// QUE LES MESSAGES PARTENT SOUS SON ADRESSE — Camara, le 17/10/2026. Chaque
    /// administrateur lit et répond depuis SA boîte LWS : même hébergeur, même
    /// service d'envoi que le support, ses identifiants à lui (mot de passe
    /// chiffré, déchiffré le temps de la requête).
    /// </summary>
    public class BoitesAdminService : IBoitesAdminService
    {
        private readonly ICurrentUserAccessor _utilisateur;
        private readonly IMessagerieAdminRepository _depot;
        private readonly IChiffrementBoites _chiffrement;
        private readonly IMessagerieService _messagerie;
        private readonly ComptesProteges _comptesProteges;
        private readonly OptionsMessagerie _optionsSupport;

        public BoitesAdminService(
            ICurrentUserAccessor utilisateur,
            IMessagerieAdminRepository depot,
            IChiffrementBoites chiffrement,
            IMessagerieService messagerie,
            ComptesProteges comptesProteges,
            Microsoft.Extensions.Options.IOptions<OptionsMessagerie> optionsSupport)
        {
            _optionsSupport = optionsSupport.Value;
            _utilisateur = utilisateur;
            _depot = depot;
            _chiffrement = chiffrement;
            _messagerie = messagerie;
            _comptesProteges = comptesProteges;
        }

        public async Task<(ExpediteurMessage? Expediteur, BoiteResolue? NonPrete)> ExpediteurAsync(
            bool support, CancellationToken ct = default)
        {
            // LA BOÎTE COMMUNE : toujours branchée, l'adresse et le nom du support.
            if (support && _messagerie.Disponible && !string.IsNullOrWhiteSpace(_optionsSupport.Adresse))
            {
                return (new ExpediteurMessage(
                    _messagerie,
                    new Expediteur(_optionsSupport.Adresse!, _optionsSupport.Nom ?? "Support Mimia"),
                    SignatureRendu.Equipe), null);
            }

            var b = await ResoudreAsync(ct);
            if (support || b?.Boite is null || b.Expediteur is null) return (null, b);

            return (new ExpediteurMessage(b.Boite, b.Expediteur, await SignatureHtmlAsync(b, ct)), null);
        }

        public async Task<BoiteResolue?> ResoudreAsync(CancellationToken ct = default)
        {
            var qui = await _depot.SignataireAsync(_utilisateur.Mail, ct);
            if (qui is null) return null;

            // Le rôle sert à la signature : le super-administrateur y porte sa fonction.
            var role = _comptesProteges.Protege(qui.Mail)
                ? SignatureRendu.FonctionSuperAdmin
                : qui.Role ?? "Utilisateur";

            if (!_chiffrement.Disponible) return new BoiteResolue(qui, null, "cle_absente", role, null);

            var boite = await _depot.BoiteAsync(qui.ParentId, ct);
            if (boite is null) return new BoiteResolue(qui, null, "a_configurer", role, null);

            var motDePasse = _chiffrement.Dechiffrer(boite.Value.MotDePasseChiffre);
            if (motDePasse is null) return new BoiteResolue(qui, null, "illisible", role, boite.Value.Adresse);

            var compte = new CompteMessagerie(boite.Value.Adresse, motDePasse, BoiteResolue.NomAffiche(qui));
            return new BoiteResolue(qui, _messagerie.Pour(compte), null, role, boite.Value.Adresse);
        }

        public async Task<string> SignatureHtmlAsync(BoiteResolue boite, CancellationToken ct = default)
        {
            var modele = await _depot.SignatureDuParentAsync(boite.Qui.ParentId, ct);
            if (modele is null) return SignatureRendu.Equipe;

            return SignatureRendu.Html(modele, new Signataire(
                boite.Qui.Prenom, boite.Qui.Nom, boite.Role, boite.Adresse ?? boite.Qui.Mail),
                // Nul : les images partent jointes au message (cid:), pas
                // servies par le site — Outlook bloque les images distantes.
                urlSite: null);
        }
    }
}
