namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le carnet des délais de parole.
    ///
    /// Une seule écriture, aucune lecture : ce qu'on en tire se lit en SQL, au
    /// moment où l'on veut savoir. Ajouter ici un « donne-moi la médiane »
    /// figerait une question dans du code alors qu'on ne sait pas encore
    /// lesquelles on se posera après le premier test avec des familles.
    /// </summary>
    public interface IMesureVoixRepository
    {
        /// <summary>
        /// Enregistre un délai mesuré. <paramref name="seance"/> est le tirage
        /// au sort du navigateur, qui ne désigne personne.
        /// </summary>
        /// <param name="transcriptionMs">Du silence détecté au texte final reçu.</param>
        /// <param name="assemblageMs">L attente volontaire avant d envoyer le tour.</param>
        /// <param name="reponseMs">Du message envoyé au premier caractère du modèle.</param>
        Task EnregistrerAsync(
            string seance,
            int delaiMs,
            bool repli,
            int? transcriptionMs = null,
            int? assemblageMs = null,
            int? reponseMs = null,
            CancellationToken ct = default);
    }
}
