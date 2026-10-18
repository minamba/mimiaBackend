namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// LES PIÈCES DES BROUILLONS, SUR LE DISQUE — Camara, le 18/10/2026 : une image
    /// insérée ou un document joint à un message pas encore envoyé doit être là
    /// quand on rouvre le brouillon. Jamais en base (règle du 15/10/2026 : la base
    /// n'a que 10 Go) : la base ne garde que le chemin.
    ///
    /// Des fichiers de passage : effacés quand le message part ou que le
    /// brouillon est supprimé.
    /// </summary>
    public interface IStockageBrouillonsMail
    {
        /// <summary>Range un fichier et rend son chemin relatif.</summary>
        Task<string> EcrireAsync(byte[] donnees, string? nomFichier, CancellationToken ct = default);

        /// <summary>Le contenu d'un fichier rangé, ou null s'il n'y est plus.</summary>
        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        /// <summary>Efface un fichier ; sans erreur s'il n'y est déjà plus.</summary>
        void Supprimer(string? cheminRelatif);
    }
}
