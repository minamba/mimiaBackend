namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// LES IMAGES ET DOCUMENTS DES TEMPLATES DE COURRIEL, SUR LE DISQUE — Camara,
    /// le 18/10/2026 : « il faut les déplacer sur le disque du VPS, dans un
    /// dossier spécifique aux templates de mails ». Règle du 15/10/2026 : aucun
    /// document en base (SQL Server Express plafonne à 10 Go). La base ne garde
    /// que le chemin.
    ///
    /// COMME LES PLANCHES, UN TEMPLATE NE SE RÉGÉNÈRE PAS : son dossier est monté
    /// depuis l'hôte ET sauvegardé. Tant qu'il n'est pas déclaré prêt (`Pret`,
    /// la marque STOCKAGE-MIMIA posée à la main), rien ne quitte la base.
    /// </summary>
    public interface IStockageModelesMail
    {
        /// <summary>Vrai quand la racine porte la marque <c>STOCKAGE-MIMIA</c> posée sur l'hôte.</summary>
        bool Pret { get; }

        /// <summary>
        /// Range un fichier et rend son chemin relatif, après l'avoir RELU et
        /// comparé. Lève une exception si l'écriture ou la relecture échoue :
        /// l'appelant garde alors le fichier en base.
        /// </summary>
        Task<string> EcrireAsync(byte[] donnees, string? nomFichier, CancellationToken ct = default);

        /// <summary>Le contenu d'un fichier rangé, ou null s'il n'y est plus.</summary>
        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        /// <summary>Efface un fichier ; sans erreur s'il n'y est déjà plus.</summary>
        void Supprimer(string? cheminRelatif);
    }
}
