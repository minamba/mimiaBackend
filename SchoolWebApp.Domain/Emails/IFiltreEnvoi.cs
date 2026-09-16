namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// Qui ne doit plus rien recevoir.
    ///
    /// UNE ADRESSE BANNIE NE REÇOIT PLUS RIEN DU TOUT — Camara, le 15/09/2026 :
    /// ni diffusion, ni bilan, ni alerte, ni message écrit à la main. Le
    /// contrôle est posé sur le service d'envoi lui-même (voir
    /// `ServiceEmailFiltre`), par où passent tous les courriels des deux
    /// serveurs : un filtre recopié dans chaque requête de destinataires
    /// finirait par manquer à l'une d'elles.
    ///
    /// Chaque serveur fournit sa lecture de la liste : l'API par son dépôt,
    /// le serveur d'identité par sa connexion à la base métier.
    /// </summary>
    public interface IFiltreEnvoi
    {
        /// <summary>Vrai si aucun courriel ne doit partir vers cette adresse.</summary>
        Task<bool> EstBloqueAsync(string? adresse, CancellationToken ct = default);
    }
}
