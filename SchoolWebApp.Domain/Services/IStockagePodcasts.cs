namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// L'AUDIO DES PODCASTS, SUR LE DISQUE — 15/11/2026. Règle du 15/10/2026 : aucun document en base. Sous la racine de
    /// l'audio des archives (`Stockage:Audio`, déjà montée sur le VPS : /donnees/audio), un dossier `podcasts`, puis un
    /// dossier par élève et un par cours : <c>podcasts/eleveId/coursId/&lt;nom tiré au sort&gt;.opus</c>.
    ///
    /// CE DOSSIER N'A PAS BESOIN D'ÊTRE SAUVEGARDÉ, comme l'audio des archives : le texte de chaque épisode est en base,
    /// et l'audio se refait à partir de lui. Rangé par élève et par cours, il se nettoie sans rien lire en base : un
    /// dossier d'élève ou de cours qui n'existe plus part (voir `PodcastsWorker`).
    /// </summary>
    public interface IStockagePodcasts
    {
        /// <summary>Range l'audio d'un épisode et rend son chemin relatif ; null si l'écriture a échoué.</summary>
        Task<string?> EcrireAsync(int eleveId, int coursId, byte[] donnees, CancellationToken ct = default);

        /// <summary>Un flux de lecture sur l'audio, ou null s'il n'y est plus.</summary>
        Stream? Ouvrir(string cheminRelatif);

        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        /// <summary>Efface le dossier d'un cours (ses épisodes).</summary>
        void SupprimerCours(int eleveId, int coursId);

        /// <summary>Efface tout le dossier d'un élève.</summary>
        void SupprimerEleve(int eleveId);

        /// <summary>Les dossiers présents sur le disque : (élève, cours) — pour le nettoyage.</summary>
        IReadOnlyList<(int EleveId, int CoursId)> Dossiers();
    }
}
