namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un courriel automatique parti — ou sur le point de partir — vers un
    /// parent.
    ///
    /// LE JOURNAL EST LA GARDE CONTRE LE DOUBLON. Une ligne est écrite AVANT
    /// l'envoi ; un index unique sur (code, clé) refuse la seconde. Un
    /// redémarrage en plein envoi ne peut donc pas réécrire « votre essai se
    /// termine demain » à un parent qui l'a déjà reçu : entre un courriel
    /// manquant et un courriel en double, le premier se pardonne.
    ///
    /// LA CLÉ DIT CE QUI NE DOIT ARRIVER QU'UNE FOIS : « abonnement:12 » pour la
    /// fin d'un essai, « parent:7 » pour la demande d'avis d'essai. Elle est
    /// nulle quand la règle est une fenêtre de temps — la demande d'avis
    /// générale, au plus une fois tous les trois mois, se lit sur `DateEnvoi`.
    /// </summary>
    public partial class EnvoiAutomatique
    {
        public int Id { get; set; }

        /// <summary>Le code du courriel automatique : FIN_ESSAI, AVIS_ESSAI…</summary>
        public string CodeModele { get; set; } = string.Empty;

        public int ParentId { get; set; }

        public string? Cle { get; set; }

        /// <summary>L'occurrence programmée qui a déclenché l'envoi, en UTC.</summary>
        public DateTime Occurrence { get; set; }

        public DateTime DateEnvoi { get; set; }

        /// <summary>Reserve, Envoye ou Echec.</summary>
        public string Statut { get; set; } = string.Empty;
    }
}
