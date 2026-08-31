using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class MesureVoixRepository : IMesureVoixRepository
    {
        /// <summary>
        /// Au-delà, on ne croit plus le navigateur.
        ///
        /// Un onglet mis en veille par le système repart avec une horloge qui a
        /// continué de tourner : la mesure suivante annonce alors trois cent
        /// mille millisecondes, et une seule de ces valeurs suffit à écraser une
        /// moyenne. On les écarte à l'entrée plutôt que de découvrir la bosse
        /// dans les chiffres et de devoir l'expliquer après coup.
        ///
        /// Trente secondes : très au-delà de tout délai réel, très en deçà d'une
        /// mise en veille.
        /// </summary>
        private const int PlafondCredible = 30_000;

        /// <summary>
        /// Garde une durée seulement si elle est plausible. Même plafond que
        /// le délai principal : au-delà, c est un onglet endormi, pas une
        /// lenteur.
        /// </summary>
        private static int? Credible(int? ms) =>
            ms is >= 0 and <= PlafondCredible ? ms : null;

        private readonly SchoolWebAppDatabaseContext _context;

        public MesureVoixRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task EnregistrerAsync(
            string seance,
            int delaiMs,
            bool repli,
            int? transcriptionMs = null,
            int? assemblageMs = null,
            int? reponseMs = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(seance)) return;
            if (delaiMs < 0 || delaiMs > PlafondCredible) return;

            _context.MesuresVoix.Add(new MesureVoix
            {
                // Tronqué à la longueur de la colonne : ce champ vient du
                // navigateur, donc de l'extérieur. Un identifiant trop long
                // ferait échouer l'insertion, et une mesure ne vaut pas une
                // exception dans les journaux.
                Seance = seance.Length > 36 ? seance[..36] : seance,
                DelaiMs = delaiMs,

                // CHAQUE MAILLON EST FILTRÉ SÉPARÉMENT, ET GARDÉ À NULL S IL
                // EST ABERRANT.
                //
                // Rejeter la mesure ENTIÈRE parce qu un seul chiffre est
                // faux perdrait les trois autres — et un onglet mis en veille
                // au mauvais moment ne fausse qu un maillon. `NULL` dit « on
                // ne sait pas » sans contaminer les moyennes des autres.
                TranscriptionMs = Credible(transcriptionMs),
                AssemblageMs = Credible(assemblageMs),
                ReponseMs = Credible(reponseMs),
                Repli = repli,
                DateCreation = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync(ct);
        }
    }
}
