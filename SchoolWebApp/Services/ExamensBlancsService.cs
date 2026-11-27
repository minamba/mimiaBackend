using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Services
{
    /// <param name="Document">Le document enregistré, ou null.</param>
    /// <param name="Motif">Pourquoi il ne l'est pas, écrit pour l'élève.</param>
    /// <param name="StockageIndisponible">Le dossier du disque n'est pas prêt : 503, et non 400.</param>
    public record ResultatDocumentExamenBlanc(DocumentExamenBlanc? Document, string? Motif, bool StockageIndisponible = false);

    /// <summary>
    /// LES DOCUMENTS DES EXAMENS BLANCS — Camara, le 23/11/2026. Le pendant de <see cref="CoursDuControleService"/> :
    /// le fichier sur le disque (sous-dossier <c>examens-blancs/</c>), la ligne en base, le texte des documents Word,
    /// Excel et PowerPoint extrait tout de suite et sans modèle.
    ///
    /// CE QUI CHANGE : vingt documents par matière, et AUCUN plafond de pages ni de taille propre (200 Mo par
    /// fichier, pour le serveur seulement). La lecture des photos et des PDF, les notions et le programme viennent
    /// aux étapes suivantes.
    /// </summary>
    public class ExamensBlancsService
    {
        public const string MessageStockageIndisponible =
            "L'envoi des documents n'est pas encore ouvert. Réessaie un peu plus tard.";

        /// <summary>Qui a posé le contrôle caché d'une matière d'examen blanc (colonne <c>pose_par</c>, 20 caractères).</summary>
        public const string PoseParExamenBlanc = "EXAMEN_BLANC";

        private readonly IExamenBlancRepository _examens;
        private readonly IStockageExamensBlancs _stockage;
        private readonly IControleScolaireRepository _controles;
        private readonly ILogger<ExamensBlancsService> _logger;

        public ExamensBlancsService(
            IExamenBlancRepository examens,
            IStockageExamensBlancs stockage,
            IControleScolaireRepository controles,
            ILogger<ExamensBlancsService> logger)
        {
            _examens = examens ?? throw new ArgumentNullException(nameof(examens));
            _stockage = stockage ?? throw new ArgumentNullException(nameof(stockage));
            _controles = controles ?? throw new ArgumentNullException(nameof(controles));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>« Brevet blanc de Mathématiques » : le sujet du contrôle caché, que le professeur lit en séance.</summary>
        public static string SujetDuControle(string type, string? matiereLibelle) =>
            $"{ExamensBlancs.Libelle(type)} de {matiereLibelle ?? "cette matière"}";

        /// <summary>
        /// ÉTAPE 3 — chaque matière a son contrôle caché (créé à la volée s'il manque : matières d'avant l'étape 3), puis
        /// sa préparation est lue d'un coup pour toutes : pourcentage, notions, verdict du professeur.
        /// </summary>
        public async Task CompleterAsync(int eleveId, ExamenBlanc examen, CancellationToken ct)
        {
            foreach (var matiere in examen.Matieres.Where(m => m.ControleId is null))
            {
                var controle = await _controles.EnregistrerDepuisCalendrierAsync(
                    eleveId, matiere.MatiereId, PoseParExamenBlanc,
                    SujetDuControle(examen.Type, matiere.MatiereLibelle),
                    examen.DateExamen, examen.HeureExamen, ct);

                if (controle is null) continue;

                await _examens.LierControleAsync(matiere.Id, controle.Id, ct);
                matiere.ControleId = controle.Id;
            }

            var ids = examen.Matieres.Select(m => m.ControleId).OfType<int>().ToList();
            if (ids.Count == 0) return;

            var preparations = await _controles.GetPreparationsAsync(eleveId, ids, ct);
            foreach (var matiere in examen.Matieres)
            {
                if (matiere.ControleId is int id && preparations.TryGetValue(id, out var preparation))
                {
                    matiere.Preparation = preparation;
                }
            }
        }

        private async Task EffacerAsync(int eleveId, RestesExamenBlanc restes, CancellationToken ct)
        {
            foreach (var chemin in restes.Chemins) _stockage.Supprimer(chemin);

            foreach (var controleId in restes.Controles)
            {
                try
                {
                    await _controles.SupprimerAsync(eleveId, controleId, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Controle cache {ControleId} d'un examen blanc non supprime.", controleId);
                }
            }
        }

        public async Task<ResultatDocumentExamenBlanc> AjouterAsync(
            int eleveId, int examenBlancId, int matiereLigneId,
            string? nomFichier, string? typeAnnonce, byte[] donnees, CancellationToken ct)
        {
            if (!_stockage.Pret)
            {
                _logger.LogWarning("Stockage des examens blancs NON PRET (marque STOCKAGE-MIMIA absente) : document refuse.");
                return new ResultatDocumentExamenBlanc(null, MessageStockageIndisponible, StockageIndisponible: true);
            }

            // UN PDF AVEC QUELQUES OCTETS DEVANT « %PDF » (25/11/2026) : rendu propre AVANT tout le reste, pour que le
            // disque, la lecture et le modèle voient un PDF ordinaire.
            var tailleRecue = donnees.Length;
            donnees = ValidationPieceJointe.RognerAvantPdf(donnees);

            var verdict = ValidationDocumentCours.ValiderSansPlafond(donnees, typeAnnonce, ExamensBlancs.TailleMax);
            if (!verdict.Accepte)
            {
                // LE REFUS DANS LES JOURNAUX (25/11/2026) : il ne partait qu'à l'écran, et un « je n'arrive plus à
                // importer » ne se diagnostiquait qu'avec une capture. Le motif, le type annoncé, la taille et les huit
                // premiers octets (la signature) : rien du contenu.
                _logger.LogInformation(
                    "Document d'examen blanc REFUSE (eleve {EleveId}, examen {ExamenBlancId}, ligne {Ligne}) : {Motif} "
                    + "| type annonce {Type} | {Taille} octets | debut {Debut}",
                    eleveId, examenBlancId, matiereLigneId, verdict.Motif, typeAnnonce ?? "(aucun)", tailleRecue,
                    Convert.ToHexString(donnees.AsSpan(0, Math.Min(8, donnees.Length))));
                return new ResultatDocumentExamenBlanc(null, verdict.Motif);
            }

            if (donnees.Length != tailleRecue)
            {
                _logger.LogInformation(
                    "Document d'examen blanc : {Octets} octet(s) retire(s) avant l'en-tete %PDF (examen {ExamenBlancId}).",
                    tailleRecue - donnees.Length, examenBlancId);
            }

            var nom = string.IsNullOrWhiteSpace(nomFichier) ? "document" : Path.GetFileName(nomFichier).Trim();
            if (nom.Length > 200) nom = nom[^200..];

            var type = verdict.TypeMime!;
            string? transcription = null;

            if (ExtracteurTexteOffice.EstOffice(type))
            {
                try
                {
                    transcription = ExtracteurTexteOffice.Extraire(donnees, type);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Texte d'un document Office d'examen blanc illisible ({Type}).", type);
                }

                if (string.IsNullOrWhiteSpace(transcription)) transcription = DocumentControle.MentionVide;
            }

            // LE FICHIER D'ABORD, puis la ligne : si la ligne refuse (matière qui n'est pas à lui, vingt documents
            // déjà), le fichier est effacé aussitôt.
            var chemin = await _stockage.EcrireAsync(eleveId, examenBlancId, donnees, nom, ct);

            DocumentExamenBlanc? document;
            try
            {
                document = await _examens.AjouterDocumentAsync(eleveId, examenBlancId, matiereLigneId, new DocumentExamenBlanc
                {
                    NomFichier = nom,
                    TypeMime = type,
                    Taille = donnees.Length,
                    NombrePages = verdict.NombrePages,
                    Chemin = chemin,
                    Transcription = transcription,
                    DateCreation = DateTime.UtcNow,
                }, ct);
            }
            catch
            {
                _stockage.Supprimer(chemin);
                throw;
            }

            if (document is null)
            {
                _stockage.Supprimer(chemin);
                return new ResultatDocumentExamenBlanc(null,
                    $"Tu as déjà envoyé {ExamensBlancs.DocumentsMax} documents pour cette matière. "
                    + "Retires-en un pour en ajouter un autre.");
            }

            _logger.LogInformation(
                "Examen blanc {ExamenBlancId} : document {Id} ({Type}, {Taille} octets, {Pages} pages) sur le disque.",
                examenBlancId, document.Id, document.TypeMime, document.Taille, document.NombrePages);

            return new ResultatDocumentExamenBlanc(document, null);
        }

        public async Task<bool> RetirerDocumentAsync(int eleveId, int examenBlancId, int documentId, CancellationToken ct)
        {
            var retire = await _examens.SupprimerDocumentAsync(eleveId, examenBlancId, documentId, ct);
            if (retire is null) return false;

            _stockage.Supprimer(retire.Chemin);
            return true;
        }

        public async Task<bool> RetirerMatiereAsync(int eleveId, int examenBlancId, int matiereLigneId, CancellationToken ct)
        {
            var restes = await _examens.RetirerMatiereAsync(eleveId, examenBlancId, matiereLigneId, ct);
            if (restes is null) return false;

            await EffacerAsync(eleveId, restes, ct);
            return true;
        }

        /// <summary>
        /// MODIFIER LA DATE ET L'HEURE — Camara, le 25/11/2026 : « il faudrait qu'on puisse le modifier comme pour les
        /// contrôles ». Le contrôle caché de chaque matière suit, SUJET INCHANGÉ : un sujet changé ferait repartir son
        /// programme (ControleScolaireRepository.ModifierAsync), et la préparation déjà faite doit rester. Rend null si
        /// l'examen blanc n'est pas à l'élève.
        /// </summary>
        public async Task<ModificationExamenBlanc?> ModifierDateAsync(
            int eleveId, int examenBlancId, DateTime dateExamen, TimeSpan? heureExamen, CancellationToken ct)
        {
            var resultat = await _examens.ModifierDateAsync(eleveId, examenBlancId, dateExamen, heureExamen, DateTime.UtcNow, ct);
            if (resultat is null || resultat.DateDejaPrise) return resultat;

            foreach (var controleId in resultat.Controles)
            {
                var controle = await _controles.GetAsync(eleveId, controleId, ct);
                if (controle is null) continue;
                await _controles.ModifierAsync(eleveId, controleId, controle.Sujet, dateExamen.Date, heureExamen, ct);
            }

            return resultat;
        }

        public async Task<bool> SupprimerAsync(int eleveId, int examenBlancId, CancellationToken ct)
        {
            var restes = await _examens.SupprimerAsync(eleveId, examenBlancId, ct);
            if (restes is null) return false;

            await EffacerAsync(eleveId, restes, ct);
            _stockage.SupprimerDossier(eleveId, examenBlancId);
            return true;
        }

        /// <summary>Le document et ses octets ; null s'il n'est pas à l'élève ou n'est plus sur le disque.</summary>
        public async Task<(DocumentExamenBlanc Document, byte[] Octets)?> LireAsync(
            int eleveId, int examenBlancId, int documentId, CancellationToken ct)
        {
            var document = await _examens.GetDocumentAsync(eleveId, examenBlancId, documentId, ct);
            if (document?.Chemin is null) return null;

            var octets = await _stockage.LireAsync(document.Chemin, ct);
            return octets is null ? null : (document, octets);
        }
    }
}
