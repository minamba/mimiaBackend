namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// LE COURS IMPORTÉ AVEC UN CONTRÔLE, SUR LE DISQUE — Camara, le 04/11/2026 : « je veux
    /// aussi que l'enfant puisse consulter le document », « stocké dans un nouveau dossier
    /// sur le VPS dans données », « supprimé physiquement une semaine après le contrôle ».
    /// Règle du 15/10/2026 : aucun document en base. Un dossier par élève, un sous-dossier
    /// par contrôle : <c>eleveId/controleId/&lt;nom tiré au sort&gt;.ext</c>.
    ///
    /// CES FICHIERS NE SONT PAS SAUVEGARDÉS : ils vivent une semaine après le contrôle, et
    /// ce que le professeur garde, c'est leur transcription, qui est en base.
    /// </summary>
    public interface IStockageCoursControles
    {
        /// <summary>Vrai quand la racine porte la marque <c>STOCKAGE-MIMIA</c> posée sur l'hôte.</summary>
        bool Pret { get; }

        /// <summary>Range le fichier, le relit, le compare, et rend son chemin relatif.</summary>
        Task<string> EcrireAsync(int eleveId, int controleId, byte[] donnees, string nom, CancellationToken ct = default);

        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        /// <summary>Efface un fichier, et son dossier de contrôle s'il est devenu vide.</summary>
        void Supprimer(string? cheminRelatif);

        void SupprimerDossier(int eleveId, int controleId);

        /// <summary>Les dossiers de contrôle présents sur le disque depuis plus d'un jour : (élève, contrôle).</summary>
        IReadOnlyList<(int EleveId, int ControleId)> DossiersAnciens();
    }
}
