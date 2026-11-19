namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// LE JOURNAL DU CONSENTEMENT AUX OFFRES PAR E-MAIL (19/11/2026, retour d'Omar) : une ligne à chaque fois que le
    /// consentement d'un parent change d'état — donné ou retiré —, avec la date et l'endroit du geste (`SourceConsentement`).
    ///
    /// EN AJOUT SEUL : le code n'y fait que des INSERT, jamais de mise à jour ni d'effacement. Les colonnes de `Parent`
    /// disent l'état présent ; ce journal en garde l'histoire, c'est la preuve du consentement. Il suit le compte : la
    /// suppression d'un parent emporte ses lignes (plus aucun traitement, plus rien à prouver).
    /// </summary>
    public partial class JournalConsentementOffres
    {
        public long Id { get; set; }

        public int ParentId { get; set; }

        /// <summary>Vrai : consentement donné ; faux : consentement retiré.</summary>
        public bool Consenti { get; set; }

        /// <summary>Où le parent a fait ce geste : voir `SourceConsentement`.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Quand (UTC) : l'heure du clic.</summary>
        public DateTime Le { get; set; }
    }
}
