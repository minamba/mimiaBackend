namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// Où vivent les fichiers audio des archives d'exercices.
    ///
    /// POURQUOI PAS DANS LA BASE. Mesuré le 10/09/2026 chez le premier élève :
    /// six cents kilo-octets par compréhension orale, soit une cinquantaine de
    /// giga-octets par an à mille élèves. Une base porte mal ce poids : elle se
    /// sauvegarde chaque nuit, se restaure en bloc, se migre — et l'audio
    /// n'a besoin d'aucune de ces garanties. Le disque, lui, en a quatre cents
    /// giga-octets qui ne servent à rien d'autre.
    ///
    /// Les chemins rendus sont TOUJOURS RELATIFS à la racine configurée : la
    /// racine change entre le poste de développement et le conteneur, et un
    /// chemin absolu en base rendrait toutes les archives illisibles au premier
    /// déplacement.
    /// </summary>
    public interface IArchiveAudio
    {
        /// <summary>
        /// Range un audio et rend son chemin relatif. Null si l'écriture a
        /// échoué — l'appelant archive alors la ligne texte sans son, ce qui
        /// vaut mieux que de tout perdre pour un disque plein.
        /// </summary>
        Task<string?> EcrireAsync(byte[] donnees, string extension, CancellationToken ct = default);

        /// <summary>Le contenu d'un audio rangé, ou null s'il n'y est plus.</summary>
        Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default);

        /// <summary>
        /// Efface un audio. Vrai si le fichier n'est plus là après l'appel —
        /// donc vrai aussi s'il avait déjà disparu, puisque le résultat voulu
        /// est atteint.
        /// </summary>
        bool Supprimer(string cheminRelatif);
    }
}
