namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// D'OÙ VIENT UN CHANGEMENT DU CONSENTEMENT AUX OFFRES PAR E-MAIL (19/11/2026, retour d'Omar) : gardé avec le parent
    /// (`offres_email_source`, dernier geste) et dans le journal (`ConsentementOffresJournal`, tous les gestes). Une preuve
    /// de consentement dit QUI, QUAND et COMMENT.
    /// </summary>
    public static class SourceConsentement
    {
        /// <summary>Case cochée sur le formulaire d'inscription (serveur d'identité).</summary>
        public const string Inscription = "inscription";

        /// <summary>Case cochée dans la fenêtre de mise à jour des conditions (`VerrouConditions`).</summary>
        public const string FenetreConditions = "fenetre_conditions";

        /// <summary>Interrupteur de l'espace parent connecté (Mon compte › Paramètres).</summary>
        public const string EspaceParent = "espace_parent";

        /// <summary>Bouton de la page ouverte depuis le lien de désinscription d'un courriel.</summary>
        public const string LienCourriel = "lien_courriel";

        /// <summary>Désinscription en un clic de la messagerie (RFC 8058, `List-Unsubscribe-Post`).</summary>
        public const string UnClic = "un_clic";

        /// <summary>« Je me suis trompé » juste après une désinscription, avec le jeton d'annulation de cette page.</summary>
        public const string AnnulationDesinscription = "annulation_desinscription";

        /// <summary>
        /// Désinscription faite pendant un retour arrière, par l'ANCIEN code (20/11/2026, revue d'Omar) : il écrivait une
        /// ligne `DesabonnementMail` « diffusion » sans toucher au consentement. Le retrait est inscrit au journal à la
        /// date de cette ligne, quand le parent repasse par la désinscription ou par l'interrupteur de son espace.
        /// </summary>
        public const string RetourArriere = "retour_arriere";
    }
}
