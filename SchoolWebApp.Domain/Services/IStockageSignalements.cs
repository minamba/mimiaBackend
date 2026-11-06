namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// LES PIÈCES JOINTES DES SIGNALEMENTS, SUR LE DISQUE — règle de Camara du 15/10/2026 :
    /// aucun document en base. Un dossier par signalement.
    ///
    /// CES FICHIERS PEUVENT MONTRER LE COURS D'UN ENFANT : sur le Drive, ils partent
    /// chiffrés (étape 7 de exploitation/sauvegarde-base.sh), contrairement aux planches.
    /// </summary>
    public interface IStockageSignalements
    {
        /// <summary>Vrai quand la racine porte la marque <c>STOCKAGE-MIMIA</c> posée sur l'hôte.</summary>
        bool Pret { get; }

        /// <summary>Range le fichier, le relit, le compare, et rend son chemin relatif.</summary>
        Task<string> EcrireAsync(int signalementId, byte[] donnees, string nom, CancellationToken ct = default);

        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        void Supprimer(string? cheminRelatif);

        void SupprimerDossier(int signalementId);

        /// <summary>Les signalements qui ont un dossier, posé il y a plus d'un jour.</summary>
        IReadOnlyList<int> DossiersAnciens();
    }
}
