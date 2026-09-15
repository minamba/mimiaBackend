namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// EST-IL PRÊT POUR SON CONTRÔLE ? La règle, en un seul endroit.
    ///
    /// CE QUI SE DÉDUIT ET CE QUI SE JUGE, ET POURQUOI C'EST SÉPARÉ
    /// -----------------------------------------------------------
    /// Deux natures de savoir se mélangeaient dans la demande initiale, et les
    /// confondre aurait produit un statut faux :
    ///
    /// - « Il n'a pas commencé à réviser » est un FAIT. L'application le sait
    ///   sans rien demander à personne : aucune séance de préparation, aucune
    ///   notion travaillée. Le demander au professeur serait lui offrir une
    ///   occasion de se tromper sur une donnée qu'on possède déjà.
    /// - « Il est prêt » est un JUGEMENT, et personne d'autre que le
    ///   professeur ne peut le porter.
    ///
    /// POURQUOI PAS UN SEUIL SUR LE POURCENTAGE
    /// ----------------------------------------
    /// Voulu par Camara le 13/09/2026 : « c'est pas parce qu'on est pas à 100 %
    /// partout que l'élève n'est pas prêt ». Un enfant à 76 % sur deux notions
    /// solides peut être prêt ; un autre à 90 % peut ne pas l'être si la seule
    /// notion qui lui manque est celle qui tombera. La barre mesure ce qui est
    /// acquis — elle ne sait pas ce que le contrôle demandera.
    ///
    /// Un seuil serait aussi une promesse : à 80 %, l'enfant lirait « prêt » et
    /// cesserait de réviser sans que personne n'ait regardé SES notions.
    /// </summary>
    public static class PretControle
    {
        // Les trois verdicts que le professeur peut poser, tels qu'ils sont
        // écrits en base. Les comparer ailleurs qu'ici serait recopier la règle.
        public const string VerdictPret = "PRET";
        public const string VerdictBientot = "BIENTOT";
        public const string VerdictPasPret = "PAS_PRET";

        // Les quatre statuts affichables. Le premier n'est jamais écrit en
        // base : il se déduit.
        public const string StatutPasCommence = "pas-commence";
        public const string StatutPasPret = "pas-pret";
        public const string StatutBientot = "bientot";
        public const string StatutPret = "pret";

        /// <summary>Le verdict écrit par le professeur est-il reconnu ?</summary>
        public static string? VerdictValide(string? verdict) =>
            (verdict ?? "").Trim().ToUpperInvariant() switch
            {
                VerdictPret => VerdictPret,
                VerdictBientot => VerdictBientot,
                VerdictPasPret => VerdictPasPret,
                _ => null,
            };

        /// <summary>
        /// UN VERDICT NE DÉPASSE PAS CE QUI A ÉTÉ PRÉPARÉ.
        ///
        /// Relevé par Camara le 14/09/2026 : un contrôle de maths posé sans
        /// information, un sujet encore incertain (« théorème de Pythagore il
        /// me semble »), aucune notion au programme, aucune préparation, 0 % —
        /// et la pastille disait « Bientôt prêt ». Le professeur avait seulement
        /// posé des questions sur le contrôle. On n'est pas « bientôt prêt »
        /// pour un programme que personne ne connaît, ni pour une révision
        /// qui n'a pas commencé.
        ///
        /// La liberté du professeur reste entière là où elle a un sens : un
        /// programme connu, une révision commencée, et il peut dire « prêt » à
        /// 76 % s'il le juge. Mais « prêt » et « bientôt prêt » exigent qu'il y
        /// ait quelque chose sur quoi l'être. Sinon, le verdict est ramené à
        /// « pas prêt » — le plafond est appliqué à l'ÉCRITURE, pour qu'un
        /// verdict prématuré ne ressurgisse pas plus tard.
        /// </summary>
        public static string? VerdictPlafonne(string? verdict, bool revisionCommencee, bool perimetreConnu)
        {
            var pose = VerdictValide(verdict);
            if (pose is null || pose == VerdictPasPret) return pose;

            return revisionCommencee && perimetreConnu ? pose : VerdictPasPret;
        }

        /// <summary>
        /// Le statut à afficher, des deux sources réunies.
        /// </summary>
        /// <param name="verdict">Ce que le professeur a tranché, ou null.</param>
        /// <param name="revisionCommencee">
        /// Au moins une séance de préparation, ou au moins une notion
        /// travaillée. L'un OU l'autre : un élève peut travailler la notion
        /// d'un contrôle en cours ordinaire, sans avoir jamais ouvert une
        /// séance « préparer ce contrôle ».
        /// </param>
        /// <param name="perimetreConnu">Au moins une notion au programme du contrôle.</param>
        /// <returns>
        /// Le statut à afficher, ou <c>null</c> quand il n'y a rien à
        /// afficher — et l'écran ne montre alors AUCUNE pastille.
        /// </returns>
        public static string? Statut(string? verdict, bool revisionCommencee, bool perimetreConnu)
        {
            // RIEN N'A COMMENCÉ : C'EST UN FAIT, ET IL L'EMPORTE.
            //
            // Cette règle disait l'inverse jusqu'au 14/09/2026 — « le verdict
            // l'emporte sur le fait », au motif qu'un enfant peut avoir
            // travaillé en classe. Mais le cas réel relevé par Camara était un
            // « Bientôt prêt » à 0 %, sans programme, après une séance où l'on
            // avait seulement parlé du contrôle. Un jugement qui ne repose sur
            // aucune préparation ne peut pas passer devant ce que l'application
            // mesure. S'il a travaillé ailleurs, une séance suffit à le montrer.
            if (!revisionCommencee) return StatutPasCommence;

            var pose = VerdictPlafonne(verdict, revisionCommencee, perimetreConnu);

            if (pose is not null) return Depuis(pose);

            // PERSONNE N'A JUGÉ : ON NE JUGE PAS À SA PLACE.
            //
            // Ce repli rendait « pas encore prêt », et c'était une faute.
            // Relevé par Camara le 13/09/2026, à la première utilisation
            // réelle : la pastille affirmait à un enfant qu'il n'était pas prêt
            // alors que son professeur ne s'était jamais prononcé. Pour
            // l'enfant, rien ne distinguait ce repli d'un vrai verdict — il
            // lisait un jugement là où il n'y en avait aucun.
            //
            // « Pas encore prêt » est un JUGEMENT, et il n'appartient qu'au
            // professeur. « La révision n'a pas commencé » est un FAIT, que
            // l'application mesure et peut donc dire. Entre les deux — il
            // travaille, personne ne l'a encore jugé — il n'y a rien à dire,
            // et on ne dit rien.
            return null;
        }

        private static string Depuis(string verdict) => verdict switch
        {
            VerdictPret => StatutPret,
            VerdictBientot => StatutBientot,
            _ => StatutPasPret,
        };
    }
}
