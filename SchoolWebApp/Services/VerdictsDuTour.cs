namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// CE QU'UN TOUR DU PROFESSEUR A LE DROIT D'ÉCRIRE SUR LA FICHE D'UN
    /// CONTRÔLE OU D'UNE ÉPREUVE.
    ///
    /// Retest #54 d'Adam (21/11/2026, scénario 3) : contrôle neuf « Contrôle de
    /// sciences » avec son cours. Au tour d'arrivée, le professeur lit le cours,
    /// pose le programme — et la carte affiche aussitôt 60 % « Pas encore
    /// prêt », alors que l'enfant n'a encore rien répondu. 60 %, c'est la
    /// valeur d'une notion jugée « en cours » par le professeur
    /// (`PretControle.PourcentEtat`) : un verdict a été écrit sur un travail
    /// qui n'avait pas eu lieu.
    ///
    /// LE TOUR D'ARRIVÉE EST LE SEUL OÙ L'ON SAIT, SANS RIEN DEVINER, QUE
    /// L'ÉLÈVE N'A RIEN PRODUIT DANS CETTE SÉANCE : c'est le professeur qui
    /// parle en premier. Ce tour peut poser le programme (il entre au
    /// périmètre), jamais l'état des notions ni le verdict — même règle que la
    /// consigne de conclusion (« s'il n'a RIEN produit, n'écris pas de
    /// verdict »), tenue ici par le code.
    ///
    /// PORTÉE GÉNÉRIQUE : tous les professeurs, contrôles ([CONTROLE_PRET],
    /// [CONTROLE_NOTIONS]) et épreuves d'examen ([EXAMEN_PRET]).
    /// </summary>
    public static class VerdictsDuTour
    {
        /// <summary>Un [CONTROLE_PRET] / [EXAMEN_PRET] écrit dans ce tour s'enregistre-t-il ?</summary>
        public static bool VerdictAccepte(bool tourDArrivee) => !tourDArrivee;

        /// <summary>
        /// Les notions d'un [CONTROLE_NOTIONS] de ce tour sont-elles marquées
        /// « travaillées » (plancher de 25 %, état « en cours ») ? Au tour
        /// d'arrivée, elles entrent seulement au programme.
        /// </summary>
        public static bool NotionsTravaillees(bool tourDArrivee) => !tourDArrivee;
    }
}
