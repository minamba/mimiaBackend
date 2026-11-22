using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// CE POUR QUOI UNE SÉANCE A ÉTÉ OUVERTE — et si c'est la même chose que la
    /// fois d'avant.
    ///
    /// Retest #54 d'Adam (21/11/2026, scénario 3) : un enfant prépare un
    /// contrôle de sciences sur les plantes, quitte, puis ouvre la préparation
    /// d'un AUTRE contrôle de sciences quelques minutes plus tard. Une
    /// matière = une conversation : le bas de l'historique est donc encore la
    /// séance des plantes, et la consigne de relance dit « repars là où vous
    /// en étiez ». Le professeur a ouvert par « On reprend : une plante… »
    /// avant d'annoncer le bon sujet.
    ///
    /// Le mode de la séance (cours, préparation, bilan, examen) et sa cible
    /// sont gardés sur la conversation et réécrits à chaque arrivée (voir
    /// <c>DefinirModeSeanceAsync</c>) : juste avant de les réécrire, le code
    /// sait donc ce qu'était la séance précédente. Si elle portait sur autre
    /// chose, il le DIT au professeur au tour d'arrivée — une règle générale
    /// dans le prompt système ne pèse rien face à un historique qui montre une
    /// question en suspens.
    ///
    /// PORTÉE GÉNÉRIQUE : tous les professeurs, toutes les matières, pour les
    /// trois arrivées qui ont un sujet imposé (préparation de contrôle, bilan
    /// de contrôle, préparation d'examen). Une arrivée en cours ordinaire n'a
    /// pas de sujet imposé : la continuité y reste la règle.
    /// </summary>
    public static class SujetDeSeance
    {
        /// <summary>Le mode de la séance et sa cible, normalisés comme le builder les relit.</summary>
        public sealed record Sujet(string Mode, int? ControleId, string? EpreuveCode);

        /// <summary>
        /// Le sujet tel que le builder le relit à chaque tour : un mode
        /// reconnu sans sa cible retombe sur le cours ordinaire.
        /// </summary>
        public static Sujet De(string? mode, int? controleId, string? epreuveCode)
        {
            var normalise = ModesSeance.Normaliser(mode);

            if ((normalise is ModesSeance.Controle or ModesSeance.Bilan && controleId is null)
                || (normalise == ModesSeance.Examen && string.IsNullOrWhiteSpace(epreuveCode)))
            {
                return new Sujet(ModesSeance.Cours, null, null);
            }

            return normalise switch
            {
                ModesSeance.Controle or ModesSeance.Bilan => new Sujet(normalise, controleId, null),
                ModesSeance.Examen => new Sujet(normalise, null, epreuveCode!.Trim()),
                _ => new Sujet(ModesSeance.Cours, null, null),
            };
        }

        /// <summary>Vrai si la nouvelle séance ne porte pas sur la même chose que la précédente.</summary>
        public static bool Change(Sujet avant, Sujet apres) =>
            avant.Mode != apres.Mode
            || avant.ControleId != apres.ControleId
            || !string.Equals(avant.EpreuveCode, apres.EpreuveCode, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Le fait posé au tour d'arrivée quand la séance change de sujet, ou
        /// null quand il n'y a rien à dire (même sujet, ou arrivée en cours
        /// ordinaire).
        /// </summary>
        /// <param name="avant">Ce qu'était la séance précédente.</param>
        /// <param name="apres">Ce qu'est celle-ci.</param>
        /// <param name="titreControleAvant">
        /// Le titre du contrôle de la séance précédente quand elle en
        /// préparait ou en débriefait un (null s'il n'existe plus).
        /// </param>
        public static string? Fait(Sujet avant, Sujet apres, string? titreControleAvant)
        {
            if (!Change(avant, apres)) return null;

            var cetteFois = apres.Mode switch
            {
                ModesSeance.Controle => "pour préparer CE contrôle-ci",
                ModesSeance.Bilan => "pour faire le point sur CE contrôle-ci",
                ModesSeance.Examen => "pour préparer CETTE épreuve-ci",
                _ => null,
            };

            if (cetteFois is null) return null;

            var titre = string.IsNullOrWhiteSpace(titreControleAvant) ? null : $" (« {titreControleAvant.Trim()} »)";

            var laFoisDAvant = avant.Mode switch
            {
                ModesSeance.Controle => "la préparation d'un AUTRE contrôle" + titre,
                ModesSeance.Bilan => "le bilan d'un AUTRE contrôle" + titre,
                ModesSeance.Examen => "la préparation d'une épreuve d'examen",
                _ => "un cours ordinaire",
            };

            return $"[CETTE SÉANCE NE CONTINUE PAS LA PRÉCÉDENTE. Les derniers messages de ton historique "
                + $"viennent de {laFoisDAvant} ; aujourd'hui, l'élève a ouvert la séance {cetteFois}. "
                + "Ne reprends NI la dernière question NI l'exercice de cette séance-là, même s'ils "
                + "n'étaient pas finis, et ne commence pas par « on reprend ». Si ta consigne d'accueil "
                + "te demande de repartir là où vous en étiez, elle ne vaut ici que pour le ton : ta "
                + "première phrase parle déjà du sujet de CETTE séance.]";
        }
    }
}
