using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Services.Jeux
{
    /// <summary>Un jeu d'une classe d'avant, et la notion fragile qui justifie de le proposer.</summary>
    public sealed record JeuAConsolider(JeuDuCatalogue Jeu, string Notion, double Score);

    /// <summary>
    /// Ce qu'un professeur a le droit de proposer à CET élève, dans SA matière,
    /// pour cette séance. Vide ne se produit pas : c'est <c>null</c>.
    /// </summary>
    public sealed record JeuxProposables(
        string ClasseLibelle,
        IReadOnlyList<JeuDuCatalogue> DeSaClasse,
        IReadOnlyList<JeuAConsolider> PourConsolider)
    {
        /// <summary>Tous les identifiants que la balise a le droit de porter.</summary>
        public IEnumerable<string> Identifiants =>
            DeSaClasse.Select(j => j.Identifiant).Concat(PourConsolider.Select(p => p.Jeu.Identifiant));
    }

    public interface IJeuxService
    {
        /// <summary>
        /// Les jeux proposables pour la séance, ou <c>null</c> quand le
        /// professeur ne doit pas en parler : porte fermée pour ce cycle,
        /// classe inconnue, ou aucun jeu dans cette matière.
        /// </summary>
        Task<JeuxProposables?> PourLaSeanceAsync(Eleve eleve, Conversation conversation, CancellationToken ct);

        /// <summary>Le jeu d'une balise, s'il fait partie de ce qu'on avait le droit de proposer.</summary>
        Task<JeuDuCatalogue?> AutoriseAsync(Eleve eleve, Conversation conversation, string? identifiant, CancellationToken ct);
    }

    /// <summary>
    /// LE PROFESSEUR NE VOIT QUE CE QU'IL A LE DROIT DE PROPOSER.
    ///
    /// Camara, le 23/09/2026 : « un professeur ne va pas proposer de voir des
    /// notions de CM2 à un CP, tout doit être cohérent avec ce que fait
    /// l'élève ». La cohérence n'est pas demandée au modèle, elle est
    /// construite : la liste qu'il reçoit ne contient QUE les jeux de sa
    /// matière, de la classe de l'élève ou d'en dessous — et, pour celles d'en
    /// dessous, seulement les jeux qui travaillent une notion que l'élève ne
    /// tient pas. Il n'a rien d'autre sous les yeux, il ne peut rien proposer
    /// d'autre. Et une balise forgée malgré tout est vérifiée contre la même
    /// liste avant d'être conservée.
    ///
    /// LA PORTE DES JEUX EST CELLE DU BOUTON : les trois interrupteurs
    /// d'administration qui affichent « Mes jeux » par cycle. Éteint, le
    /// professeur ne sait même pas que des jeux existent — proposer une porte
    /// que l'enfant ne trouve pas sur son écran serait pire que se taire.
    /// </summary>
    public sealed class JeuxService : IJeuxService
    {
        /// <summary>Cinq notions fragiles au plus : au-delà, la liste noie celle qui compte.</summary>
        private const int NotionsFragilesMax = 5;

        /// <summary>Plus large que la limite : plusieurs notions fragiles n'ont pas de jeu.</summary>
        private const int NotionsFragilesLues = 20;

        private readonly CatalogueJeux _catalogue;
        private readonly IMaitriseService _maitrises;
        private readonly IReglageRepository _reglages;
        private readonly ILogger<JeuxService> _logger;

        public JeuxService(
            CatalogueJeux catalogue,
            IMaitriseService maitrises,
            IReglageRepository reglages,
            ILogger<JeuxService> logger)
        {
            _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            _maitrises = maitrises ?? throw new ArgumentNullException(nameof(maitrises));
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<JeuxProposables?> PourLaSeanceAsync(Eleve eleve, Conversation conversation, CancellationToken ct)
        {
            if (!await PorteOuverteAsync(eleve.NiveauCycle, ct)) return null;

            var rang = CatalogueJeux.RangDeLaClasse(eleve.NiveauCode);
            if (rang is null) return null;

            var matiere = conversation.MatiereCode;
            var deSaClasse = _catalogue.DeLaClasse(matiere, rang.Value);

            // Les notions fragiles des classes d'avant, et pour chacune le ou
            // les jeux qui la travaillent. Une notion sans jeu ne compte pas ;
            // un jeu déjà retenu pour une notion plus fragile n'est pas répété.
            var pourConsolider = new List<JeuAConsolider>();
            var retenus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (rang.Value > 1)
            {
                var fragiles = await _maitrises.GetFragilesEnAmontAsync(
                    eleve.Id, conversation.MatiereId, rang.Value, NotionsFragilesLues);

                foreach (var fragile in fragiles)
                {
                    if (pourConsolider.Count >= NotionsFragilesMax) break;

                    foreach (var jeu in _catalogue.EnAmontPour(matiere, rang.Value, fragile.Code))
                    {
                        if (!retenus.Add(jeu.Identifiant)) continue;
                        pourConsolider.Add(new JeuAConsolider(jeu, fragile.Libelle ?? fragile.Code ?? "", fragile.Score));
                        break;
                    }
                }
            }

            if (deSaClasse.Count == 0 && pourConsolider.Count == 0) return null;

            return new JeuxProposables(eleve.NiveauLibelle ?? eleve.NiveauCode ?? "", deSaClasse, pourConsolider);
        }

        public async Task<JeuDuCatalogue?> AutoriseAsync(
            Eleve eleve, Conversation conversation, string? identifiant, CancellationToken ct)
        {
            var jeu = _catalogue.Trouver(identifiant);
            if (jeu is null) return null;

            var proposables = await PourLaSeanceAsync(eleve, conversation, ct);
            if (proposables is null) return null;

            return proposables.Identifiants.Contains(jeu.Identifiant, StringComparer.OrdinalIgnoreCase) ? jeu : null;
        }

        /// <summary>
        /// L'interrupteur du cycle de l'élève. Le même réflexe défensif que
        /// pour le mode développeur : une lecture qui échoue ferme la porte,
        /// elle ne l'ouvre pas.
        /// </summary>
        private async Task<bool> PorteOuverteAsync(string? cycle, CancellationToken ct)
        {
            var cle = (cycle ?? "").Trim().ToLowerInvariant() switch
            {
                "primaire" => ReglagesController.JeuxPrimaire,
                "college" => ReglagesController.JeuxCollege,
                "lycee" => ReglagesController.JeuxLycee,
                _ => null,
            };

            if (cle is null) return false;

            try
            {
                return await _reglages.EstActifAsync(cle, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lecture du réglage {Cle} impossible : jeux non proposés.", cle);
                return false;
            }
        }
    }
}
