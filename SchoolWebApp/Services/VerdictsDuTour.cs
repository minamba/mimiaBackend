namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// PAS DE VERDICT SUR UNE PRÉPARATION OÙ L'ENFANT N'A RIEN RÉPONDU.
    ///
    /// Retest #54 d'Adam (21/11/2026, scénario 3), reproduit sur master le
    /// 22/11 : l'enfant prépare un contrôle de sciences (les plantes), sort,
    /// ouvre la préparation d'un AUTRE contrôle de sciences avec son cours
    /// (les animaux), puis sort sans avoir rien répondu. La carte du nouveau
    /// contrôle affiche 60 % « Pas encore prêt » : 60 %, c'est une notion
    /// jugée « en cours » par le professeur (`PretControle.PourcentEtat`).
    /// Deux écrivains l'avaient posé, chacun sur un travail qui n'avait pas eu
    /// lieu :
    /// <list type="bullet">
    /// <item>la conclusion de départ (tour <c>DepartAnticipe</c> du builder),
    /// à qui l'on demande un verdict pour le contrôle de la séance ;</item>
    /// <item>le filet de l'observateur (<c>ConclurePreparationAsync</c>), dont la
    /// « séance » regroupe tous les messages pas encore observés — donc aussi
    /// ceux de la préparation des plantes, quand les deux ont tenu en moins de
    /// dix minutes.</item>
    /// </list>
    /// Et dès le tour d'arrivée, les notions lues dans le cours étaient posées
    /// « travaillées » (25 %, « en cours ») avant toute réponse.
    ///
    /// LA RÈGLE, TENUE PAR LE CODE : une préparation commence à son ouverture
    /// (`DernierePreparationLe`, posée à chaque arrivée par le bouton). Tant que
    /// l'enfant n'a envoyé aucun message depuis, aucun verdict ne s'écrit et
    /// aucune notion n'est « travaillée » ; ce qui a été dit avant appartient à
    /// une autre séance. La consigne de conclusion disait déjà « s'il n'a RIEN
    /// produit, n'écris pas de verdict » : le modèle ne l'a pas tenue.
    ///
    /// PORTÉE GÉNÉRIQUE : tous les professeurs, contrôles ([CONTROLE_PRET],
    /// [CONTROLE_NOTIONS]) et épreuves d'examen ([EXAMEN_PRET], au tour
    /// d'arrivée).
    /// </summary>
    public static class VerdictsDuTour
    {
        /// <summary>
        /// L'enfant a-t-il répondu dans cette préparation ?
        /// </summary>
        /// <param name="tourDArrivee">Le professeur parle en premier : personne n'a encore rien dit.</param>
        /// <param name="tourDeLEleve">Ce tour répond à un message que l'enfant vient d'envoyer.</param>
        /// <param name="preparationOuverteLe">
        /// L'ouverture de la préparation, ou null si on ne la connaît pas — on
        /// ne bloque alors rien (comportement d'avant).
        /// </param>
        /// <param name="messages">Les messages déjà enregistrés (rôle, date).</param>
        public static bool EnfantARepondu(
            bool tourDArrivee, bool tourDeLEleve, DateTime? preparationOuverteLe,
            IEnumerable<(string? Role, DateTime Date)> messages)
        {
            if (tourDArrivee) return false;
            if (tourDeLEleve) return true;
            if (preparationOuverteLe is not DateTime ouverture) return true;

            return messages.Any(m => string.Equals(m.Role, "user", StringComparison.Ordinal) && m.Date >= ouverture);
        }

        /// <summary>
        /// Les messages qui appartiennent à la préparation ouverte à
        /// <paramref name="preparationOuverteLe"/> — ceux d'avant viennent
        /// d'une autre séance (un autre contrôle, un cours ordinaire). Tous si
        /// la date est inconnue.
        /// </summary>
        public static List<T> DeLaPreparation<T>(
            IEnumerable<T> messages, Func<T, DateTime> date, DateTime? preparationOuverteLe) =>
            preparationOuverteLe is DateTime ouverture
                ? messages.Where(m => date(m) >= ouverture).ToList()
                : messages.ToList();
    }
}
