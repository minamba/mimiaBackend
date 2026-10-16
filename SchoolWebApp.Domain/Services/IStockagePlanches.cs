namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// LES IMAGES DES PLANCHES, SUR LE DISQUE ET NON EN BASE — Camara, le
    /// 15/10/2026 : « ma base n'a que 10 Go, je dois à tout prix éviter de
    /// sauvegarder des documents en base ». Le VPS a 400 Go de disque ; SQL
    /// Server Express plafonne à 10 Go par base. La base ne garde que le
    /// chemin et l'empreinte de chaque image.
    ///
    /// À LA DIFFÉRENCE DE L'AUDIO, UNE PLANCHE NE SE RÉGÉNÈRE PAS : son dossier
    /// doit être monté depuis l'hôte ET sauvegardé. Tant que le dossier n'est
    /// pas déclaré prêt (`Pret`), rien ne quitte la base.
    /// </summary>
    public interface IStockagePlanches
    {
        /// <summary>
        /// Vrai quand la racine porte la marque posée à la main sur l'hôte
        /// (`STOCKAGE-MIMIA`). C'est la preuve que le dossier est le montage du
        /// disque du VPS, pas un dossier du conteneur qui disparaîtrait avec
        /// lui. Sans elle, les images restent en base, comme avant.
        /// </summary>
        bool Pret { get; }

        /// <summary>
        /// Range une image et rend son chemin relatif, après l'avoir RELUE et
        /// comparée. Lève une exception si l'écriture ou la relecture échoue :
        /// l'appelant garde alors l'image en base.
        /// </summary>
        Task<string> EcrireAsync(string cle, string variante, byte[] donnees, string empreinte, string? typeMime, CancellationToken ct = default);

        /// <summary>Le contenu d'une image rangée, ou null si le fichier n'y est plus.</summary>
        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        /// <summary>Efface une image. Vrai si le fichier n'est plus là après l'appel.</summary>
        bool Supprimer(string cheminRelatif);

        /// <summary>L'empreinte SHA-256 (hexadécimal, minuscules) d'un contenu.</summary>
        static string Empreinte(byte[] donnees) =>
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(donnees)).ToLowerInvariant();
    }
}
