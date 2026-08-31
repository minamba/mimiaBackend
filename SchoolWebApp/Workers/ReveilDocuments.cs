namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// La sonnette de <see cref="TranscriptionDocumentsWorker"/>. Sonnée quand
    /// un élève dépose un document, et par rien d'autre.
    ///
    /// POURQUOI ELLE EXISTE
    /// --------------------
    /// Le worker regardait la base toutes les dix minutes. Le tour à vide ne
    /// coûtait rien au modèle — une requête SQL, puis il repartait dormir —
    /// mais c'était le dernier travail qui tournait au chronomètre, et un
    /// travail qui tourne au chronomètre est un travail qu'on ne peut pas
    /// raisonner : pour savoir ce qu'il dépense, il faut connaître son
    /// intervalle ET son taux d'échec. Sur les planches, cette combinaison-là
    /// a fait relire un tableau périodique sept mille fois en dix jours.
    ///
    /// Adossé à un envoi, il devient énonçable en une phrase : il ne travaille
    /// que si un élève a envoyé quelque chose.
    ///
    /// CE QU'ON PERD, ET POURQUOI CE N'EST PAS GRAVE
    /// --------------------------------------------
    /// Un document déposé pendant que l'API redémarre ne sonne personne : il
    /// attend le dépôt suivant. Ça ne prive de rien — la transcription ne sert
    /// qu'après l'effacement des octets, trois jours plus tard, et la purge
    /// REFUSE d'effacer un document non transcrit. Le seul effet d'un retard
    /// est de la place occupée en base.
    /// </summary>
    public sealed class ReveilDocuments : Sonnette
    {
    }
}
