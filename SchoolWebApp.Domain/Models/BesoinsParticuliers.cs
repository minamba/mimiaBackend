namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES BESOINS PARTICULIERS DE L'ÉLÈVE — Camara, le 11/10/2026 : « comment
    /// l'application peut s'adapter aux dyslexiques et aux TDAH ? ». Proposition
    /// validée le même jour (Documents › « Mimia - Proposition dyslexie,
    /// dyscalculie et TDAH.pdf »).
    ///
    /// SEUL LE PARENT COCHE, ET SA COCHE VAUT ACCORD (décision de Camara). Ce
    /// que l'enfant dit en séance ne déclenche rien. La date de la coche est
    /// gardée comme preuve de l'accord (<c>Eleve.BesoinsAccordLe</c>).
    ///
    /// DONNÉES DE SANTÉ (RGPD, article 9) : trois cases, rien d'autre. Elles ne
    /// servent qu'à adapter la manière d'expliquer du professeur et l'affichage.
    /// Elles ne paraissent ni dans les bilans, ni dans la fiche, ni dans
    /// l'administration courante ; l'enfant ne les voit pas. Le navigateur de
    /// l'enfant ne reçoit que les réglages d'affichage qui en découlent.
    ///
    /// DERRIÈRE UN INTERRUPTEUR (Admin › Modes), ÉTEINT PAR DÉFAUT : éteint, les
    /// cases disparaissent des formulaires et rien ne s'applique ; les coches
    /// déjà posées sont gardées.
    /// </summary>
    public static class BesoinsParticuliers
    {
        public const string Dyslexie = "DYSLEXIE";
        public const string Dyscalculie = "DYSCALCULIE";
        public const string Tdah = "TDAH";

        /// <summary>Les codes connus, dans l'ordre d'affichage, avec leur libellé.</summary>
        public static readonly IReadOnlyList<(string Code, string Libelle)> Tous =
        [
            (Dyslexie, "Dyslexie ou dysorthographie"),
            (Dyscalculie, "Dyscalculie"),
            (Tdah, "TDAH (trouble de l'attention)"),
        ];

        /// <summary>La clé de l'interrupteur dans la table des réglages.</summary>
        public const string Reglage = "MODULE_BESOINS_PARTICULIERS";

        public const bool ActifParDefaut = false;

        /// <summary>Les codes stockés (« DYSLEXIE;TDAH »), en liste, inconnus écartés.</summary>
        public static List<string> Lire(string? stockes) =>
            string.IsNullOrWhiteSpace(stockes)
                ? []
                : Normaliser(stockes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        /// <summary>La liste à stocker, ou null si elle est vide.</summary>
        public static string? Ecrire(IEnumerable<string>? codes)
        {
            var propres = Normaliser(codes);
            return propres.Count == 0 ? null : string.Join(';', propres);
        }

        /// <summary>Seulement les codes connus, sans doublon, dans l'ordre de <see cref="Tous"/>.</summary>
        public static List<string> Normaliser(IEnumerable<string>? codes)
        {
            var demandes = (codes ?? []).Select(c => c?.Trim().ToUpperInvariant()).ToHashSet();
            return Tous.Select(t => t.Code).Where(demandes.Contains).ToList();
        }

        /// <summary>
        /// L'AFFICHAGE QUI EN DÉCOULE — c'est tout ce que reçoit le navigateur de
        /// l'enfant. La lecture aérée pour la dyslexie, le calme (jeux en mode
        /// tranquille conseillé, séance courte conseillée, décor apaisé) pour le
        /// TDAH. Rien si le module est éteint ou si le parent a coupé l'écran adapté.
        /// </summary>
        public static Affichage AffichageDe(IReadOnlyCollection<string> besoins, bool ecranAdapte, bool moduleActif)
        {
            if (!moduleActif || !ecranAdapte) return new Affichage(false, false);
            return new Affichage(besoins.Contains(Dyslexie), besoins.Contains(Tdah));
        }

        /// <summary>Ce que l'écran adapte.</summary>
        public record Affichage(bool LectureAeree, bool Calme);
    }
}
