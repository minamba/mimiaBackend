using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class ExamenRepository : IExamenRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ExamenRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>La session que passe un élève cette année scolaire : 2027 pour 2026-2027.</summary>
        private static int SessionEnCours() => AnneeScolaire.Debut(DateTime.UtcNow) + 1;

        private static string[] Codes(string? liste) =>
            (liste ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        /// <summary>
        /// L'ESPAGNOL EN LV2 N'A PAS D'ÉPREUVE FINALE AU BAC — il compte en contrôle
        /// continu, coefficient 3 en première et 3 en terminale (arrêtés du
        /// 16/07/2018 consolidés, voies générale et technologique ; Éduscol,
        /// « Modalités d'évaluation de langues vivantes »). Seulement pour les
        /// examens du bac : au brevet, les langues n'ont pas cette règle-là.
        /// </summary>
        private static List<string> NotesControleContinu(
            Examen examen, string? niveauCode, bool lv2Espagnol, IReadOnlyCollection<string> specialites)
        {
            // TOUS LES ÉLÈVES DU BAC ONT UNE LVA ET UNE LVB — l'anglais pour
            // presque tous, l'espagnol pour ceux dont la famille l'a coché. Camara,
            // le 14/09/2026 : « pour l'anglais, c'est la même logique ». Aucune
            // n'a d'épreuve finale ; seules les spécialités de langue en ont une.
            if (!examen.Code.StartsWith("BAC", StringComparison.Ordinal) || !VoiesScolaires.AUneLv2(niveauCode))
            {
                return [];
            }

            var langues = lv2Espagnol ? "l'anglais comme l'espagnol" : "l'anglais par exemple";

            var note = $"Tes langues vivantes, ta LVA et ta LVB ({langues}), n'ont pas d'épreuve finale au bac : "
                + "elles comptent en contrôle continu, avec tes moyennes de première et de terminale.";

            var specialitesDeLangue = new[]
                {
                    ("LLCER_ANGLAIS", "LLCER anglais"),
                    ("AMC", "LLCER anglais, monde contemporain"),
                    ("LLCER_ESPAGNOL", "LLCER espagnol"),
                }
                .Where(s => specialites.Contains(s.Item1, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.Item2)
                .ToList();

            if (specialitesDeLangue.Count > 0)
            {
                var noms = string.Join(" et ", specialitesDeLangue);
                var pluriel = specialitesDeLangue.Count > 1;

                note += VoiesScolaires.NombreSpecialites(niveauCode) == 2
                    ? $" {(pluriel ? "Tes spécialités" : "Ta spécialité")} {noms}, {(pluriel ? "elles, ont" : "elle, a")} "
                      + "une épreuve terminale : c'est une autre carte."
                    : $" {(pluriel ? "Tes spécialités" : "Ta spécialité")} {noms} {(pluriel ? "auront" : "aura")} "
                      + "une épreuve en terminale si tu la gardes.";
            }

            return [note];
        }

        private static bool DependDesSpecialites(EpreuveExamen e) =>
            !string.IsNullOrWhiteSpace(e.SpecialiteRequise) || !string.IsNullOrWhiteSpace(e.SpecialiteExclue);

        /// <summary>
        /// L'ÉPREUVE CONCERNE-T-ELLE CET ÉLÈVE, VU SES SPÉCIALITÉS ?
        ///
        /// Une épreuve « sans la spécialité » ne s'affiche pas tant que la famille
        /// n'a rien coché : on ne sait pas encore si l'élève a la spécialité, et
        /// montrer les mathématiques de l'enseignement scientifique à un élève
        /// qui prépare celles de la spécialité serait pire que ne rien montrer.
        /// </summary>
        private static bool ConcerneEleve(EpreuveExamen e, IReadOnlyCollection<string> specialites)
        {
            if (!string.IsNullOrWhiteSpace(e.SpecialiteRequise)
                && !specialites.Contains(e.SpecialiteRequise, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(e.SpecialiteExclue)
                && (specialites.Count == 0
                    || specialites.Contains(e.SpecialiteExclue, StringComparer.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        /// <summary>Le domaine d'une notion entre-t-il dans le périmètre de l'épreuve ?</summary>
        private static bool DomaineRetenu(string? domaine, string[] inclus, string[] exclus)
        {
            var d = domaine ?? string.Empty;

            if (inclus.Length > 0 && !inclus.Any(p => d.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return false;

            return !exclus.Any(p => d.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<string>> SpecialitesDeLEleveAsync(int eleveId, CancellationToken ct) =>
            VoiesScolaires.LireSpecialites(await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.Specialites)
                .FirstOrDefaultAsync(ct));

        private async Task<Examen?> ExamenApplicableAsync(string? niveauCode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(niveauCode)) return null;

            var session = SessionEnCours();

            var examens = await _context.Examens
                .AsNoTracking()
                .Include(e => e.Epreuves)
                .Where(e => e.Actif && e.Session == session)
                .ToListAsync(ct);

            return examens.FirstOrDefault(e =>
                Codes(e.NiveauxCodes).Contains(niveauCode, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<EpreuveExamenInfo?> GetEpreuveApplicableAsync(
            string? niveauCode, string? epreuveCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(epreuveCode)) return null;

            var examen = await ExamenApplicableAsync(niveauCode, ct);
            var epreuve = examen?.Epreuves.FirstOrDefault(e =>
                string.Equals(e.Code, epreuveCode, StringComparison.OrdinalIgnoreCase));

            if (examen is null || epreuve is null) return null;

            return new EpreuveExamenInfo
            {
                Id = epreuve.Id,
                Code = epreuve.Code,
                Libelle = epreuve.Libelle,
                ExamenCode = examen.Code,
                ExamenLibelle = examen.Libelle,
                Session = examen.Session,
                Matieres = Codes(epreuve.MatieresCodes),
                NiveauxProgramme = Codes(epreuve.NiveauxProgrammeCodes),
                Description = epreuve.Description,
                Remarque = epreuve.Remarque,
            };
        }

        public async Task<PreparationExamenEleve?> GetPreparationExamenAsync(
            int eleveId, string? niveauCode, CancellationToken ct = default)
        {
            var examen = await ExamenApplicableAsync(niveauCode, ct);
            if (examen is null) return null;

            var profil = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => new { e.Specialites, e.Lv2Espagnol })
                .FirstOrDefaultAsync(ct);

            var specialites = VoiesScolaires.LireSpecialites(profil?.Specialites);

            // COMBIEN IL EN MANQUE, et non « y en a-t-il au moins une ». La
            // classe dit combien la voie générale en attend (3 en première,
            // 2 en terminale) ; tout ce qui n'y est pas est une épreuve que
            // l'élève ne voit pas et qu'il passera quand même.
            var manquantes = Math.Max(
                0, VoiesScolaires.NombreSpecialites(niveauCode) - specialites.Count);

            var epreuves = await AssemblerAsync(
                eleveId, examen,
                examen.Epreuves.Where(e => ConcerneEleve(e, specialites)).OrderBy(e => e.Ordre).ToList(), ct);

            // Une épreuve sans aucune matière ouverte n'a rien à montrer.
            epreuves = epreuves.Where(e => e.Matieres.Count > 0).ToList();

            return new PreparationExamenEleve
            {
                Code = examen.Code,
                Libelle = examen.Libelle,
                TitreSection = examen.TitreSection,
                Session = examen.Session,
                Pourcent = epreuves.Count == 0 ? 0 : (int)Math.Round(epreuves.Average(e => (double)e.Pourcent)),
                SpecialitesARenseigner = manquantes > 0 && examen.Epreuves.Any(DependDesSpecialites),
                SpecialitesManquantes = manquantes,
                NotesControleContinu = NotesControleContinu(examen, niveauCode, profil?.Lv2Espagnol == true, specialites),
                Epreuves = epreuves,
            };
        }

        public async Task<EpreuvePreparation?> GetPreparationEpreuveAsync(
            int eleveId, string? niveauCode, string epreuveCode, CancellationToken ct = default)
        {
            var examen = await ExamenApplicableAsync(niveauCode, ct);
            var epreuve = examen?.Epreuves.FirstOrDefault(e =>
                string.Equals(e.Code, epreuveCode, StringComparison.OrdinalIgnoreCase));

            if (examen is null || epreuve is null) return null;

            // Une adresse recopiée vers l'épreuve d'une spécialité que l'élève n'a
            // pas ne mène à rien, comme une épreuve d'une autre classe.
            if (!ConcerneEleve(epreuve, await SpecialitesDeLEleveAsync(eleveId, ct))) return null;

            var assemblee = (await AssemblerAsync(eleveId, examen, [epreuve], ct)).FirstOrDefault();
            return assemblee is null || assemblee.Matieres.Count == 0 ? null : assemblee;
        }

        /// <summary>
        /// LE PÉRIMÈTRE EST UNE RÈGLE, PAS UNE LISTE : les compétences ACTIVES
        /// des matières de l'épreuve, aux niveaux de programme de l'épreuve. Une
        /// notion retirée du programme sort donc de la carte toute seule.
        ///
        /// Trois requêtes pour toutes les épreuves ensemble — compétences,
        /// maîtrises, préparations — et non trois par épreuve : la section
        /// d'accueil en affiche quatre.
        /// </summary>
        private async Task<List<EpreuvePreparation>> AssemblerAsync(
            int eleveId, Examen examen, List<EpreuveExamen> epreuves, CancellationToken ct)
        {
            var codesMatieres = epreuves.SelectMany(e => Codes(e.MatieresCodes))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var codesNiveaux = epreuves.SelectMany(e => Codes(e.NiveauxProgrammeCodes))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var matieres = await _context.Matieres
                .AsNoTracking()
                .Where(m => m.Code != null && codesMatieres.Contains(m.Code))
                .Select(m => new { m.Id, m.Code, m.Libelle, m.ProfPrenom, m.ProfCouleur, m.Active })
                .ToListAsync(ct);

            var niveaux = await _context.NiveauxScolaires
                .AsNoTracking()
                .Where(n => n.Code != null && codesNiveaux.Contains(n.Code))
                .Select(n => new { n.Id, n.Code, n.Ordre })
                .ToListAsync(ct);

            var matiereIds = matieres.Select(m => m.Id).ToList();
            var niveauIds = niveaux.Select(n => n.Id).ToList();

            var competences = await _context.Competences
                .AsNoTracking()
                .Where(c => c.Actif
                    && matiereIds.Contains(c.MatiereId)
                    && niveauIds.Contains(c.NiveauScolaireId)
                    // LES OPTIONS DE TERMINALE NE SONT PAS À L'ÉCRIT. Leurs lignes
                    // sont rangées dans la matière sous un domaine « Option … »
                    // (voir `ReferentielsSeriesTechnologiques`) ; l'élève n'en suit
                    // qu'une, et l'épreuve écrite ne porte que sur la partie
                    // commune. Les compter ferait mentir la barre.
                    && !(c.Domaine != null && c.Domaine.StartsWith("Option ")))
                .Select(c => new { c.Id, c.MatiereId, c.NiveauScolaireId, c.Libelle, c.Domaine, c.Ordre })
                .ToListAsync(ct);

            var competenceIds = competences.Select(c => c.Id).ToList();

            var maitrises = competenceIds.Count == 0
                ? []
                : await _context.MaitrisesEleves
                    .AsNoTracking()
                    .Where(m => m.EleveId == eleveId && competenceIds.Contains(m.CompetenceId))
                    .Select(m => new { m.CompetenceId, m.Score, m.NombreObservations, m.DerniereEvaluation, m.Source })
                    .ToListAsync(ct);

            var parCompetence = maitrises.ToDictionary(m => m.CompetenceId);

            var epreuveIds = epreuves.Select(e => e.Id).ToList();

            var preparations = await _context.PreparationsEpreuves
                .AsNoTracking()
                .Where(p => p.EleveId == eleveId && epreuveIds.Contains(p.EpreuveId))
                .ToListAsync(ct);

            var maintenant = DateTime.UtcNow;
            var resultat = new List<EpreuvePreparation>();

            foreach (var epreuve in epreuves)
            {
                var niveauxEpreuve = Codes(epreuve.NiveauxProgrammeCodes);
                var domainesInclus = Codes(epreuve.DomainesInclus);
                var domainesExclus = Codes(epreuve.DomainesExclus);

                var ordreParNiveau = niveaux
                    .Where(n => niveauxEpreuve.Contains(n.Code, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(n => n.Id, n => n.Ordre);

                var matieresEpreuve = new List<MatiereEpreuvePreparation>();

                foreach (var code in Codes(epreuve.MatieresCodes))
                {
                    var matiere = matieres.FirstOrDefault(m =>
                        string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase));

                    // Une matière fermée ne se prépare pas : pas de professeur à
                    // qui ouvrir la séance.
                    if (matiere is null || !matiere.Active) continue;

                    var notions = new List<NotionControle>();

                    foreach (var c in competences
                        .Where(c => c.MatiereId == matiere.Id
                            && ordreParNiveau.ContainsKey(c.NiveauScolaireId)
                            && DomaineRetenu(c.Domaine, domainesInclus, domainesExclus))
                        .OrderBy(c => ordreParNiveau[c.NiveauScolaireId])
                        .ThenBy(c => c.Domaine)
                        .ThenBy(c => c.Ordre))
                    {
                        var mesuree = parCompetence.TryGetValue(c.Id, out var m) ? m : null;

                        var etat = mesuree is null
                            ? "a-decouvrir"
                            : SeuilsMaitrise.Etat(
                                mesuree.Score, mesuree.NombreObservations, mesuree.DerniereEvaluation, maintenant);

                        notions.Add(new NotionControle
                        {
                            Id = c.Id,
                            CompetenceId = c.Id,
                            Libelle = c.Libelle ?? string.Empty,
                            Domaine = c.Domaine,
                            Etat = etat,
                            EtatMesure = etat,
                            Pourcent = mesuree is null ? 0 : Math.Clamp((int)Math.Round(mesuree.Score * 100), 0, 100),
                            ValideeParMesure = mesuree?.Source is "Evaluation" or "Controle",
                        });
                    }

                    var preparation = preparations.FirstOrDefault(p =>
                        p.EpreuveId == epreuve.Id && p.MatiereId == matiere.Id);

                    var revisionCommencee = (preparation?.NombrePreparations ?? 0) > 0
                        || notions.Any(n => parCompetence.ContainsKey(n.Id));

                    var perimetreConnu = notions.Count > 0;

                    matieresEpreuve.Add(new MatiereEpreuvePreparation
                    {
                        MatiereId = matiere.Id,
                        Code = matiere.Code!,
                        Libelle = matiere.Libelle ?? matiere.Code!,
                        ProfPrenom = matiere.ProfPrenom,
                        ProfCouleur = matiere.ProfCouleur,
                        NombrePreparations = preparation?.NombrePreparations ?? 0,
                        DernierePreparationLe = preparation?.DernierePreparationLe,
                        RevisionCommencee = revisionCommencee,
                        Preparation = new PreparationControle
                        {
                            Pourcent = notions.Count == 0 ? 0 : (int)Math.Round(notions.Average(n => (double)n.Pourcent)),
                            PerimetreConnu = perimetreConnu,
                            Total = notions.Count,
                            Acquises = notions.Count(n => n.EtatMesure == "acquise"),
                            DernierePreparationLe = preparation?.DernierePreparationLe,
                            PretStatut = PretControle.Statut(preparation?.PretVerdict, revisionCommencee, perimetreConnu),
                            PretObservation = preparation?.PretObservation,
                            PretLe = preparation?.PretLe,
                            Notions = notions,
                        },
                    });
                }

                resultat.Add(new EpreuvePreparation
                {
                    Id = epreuve.Id,
                    Code = epreuve.Code,
                    Libelle = epreuve.Libelle,
                    Description = epreuve.Description,
                    Remarque = epreuve.Remarque,
                    Ordre = epreuve.Ordre,
                    ExamenCode = examen.Code,
                    ExamenLibelle = examen.Libelle,
                    Session = examen.Session,
                    NiveauxProgramme = niveauxEpreuve,
                    Pourcent = matieresEpreuve.Count == 0
                        ? 0
                        : (int)Math.Round(matieresEpreuve.Average(x => (double)x.Preparation.Pourcent)),
                    Matieres = matieresEpreuve,
                });
            }

            return resultat;
        }

        public async Task MarquerPreparationAsync(
            int eleveId, int epreuveId, int matiereId, DateTime maintenant, CancellationToken ct = default)
        {
            var preparation = await TrouverOuCreerAsync(eleveId, epreuveId, matiereId, maintenant, ct);

            preparation.NombrePreparations += 1;
            preparation.DernierePreparationLe = maintenant;

            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> PoserVerdictAsync(
            int eleveId, int epreuveId, int matiereId, string verdict, string? observation,
            DateTime maintenant, CancellationToken ct = default)
        {
            var valide = PretControle.VerdictValide(verdict);
            if (valide is null) return false;

            var epreuve = await _context.EpreuvesExamens
                .AsNoTracking()
                .Include(e => e.Examen)
                .FirstOrDefaultAsync(e => e.Id == epreuveId, ct);

            if (epreuve?.Examen is null) return false;

            var matiere = (await AssemblerAsync(eleveId, epreuve.Examen, [epreuve], ct))
                .FirstOrDefault()?.Matieres.FirstOrDefault(m => m.MatiereId == matiereId);

            if (matiere is null) return false;

            var preparation = await TrouverOuCreerAsync(eleveId, epreuveId, matiereId, maintenant, ct);

            // LE MÊME PLAFOND QUE POUR UN CONTRÔLE — voir PretControle.VerdictPlafonne.
            preparation.PretVerdict = PretControle.VerdictPlafonne(
                valide,
                matiere.RevisionCommencee || preparation.NombrePreparations > 0,
                matiere.Preparation.PerimetreConnu);

            var propre = observation?.Trim();
            preparation.PretObservation = string.IsNullOrEmpty(propre)
                ? null
                // AUCUNE LIMITE depuis le 14/09/2026 (migration
                // `ObservationPretParagraphes`) — Camara : « si le prof fait un jour
                // une énorme justification, il ne doit pas être bloqué ». Tronquer
                // coupait en plein mot, sur le dernier domaine.
                : propre;

            preparation.PretLe = maintenant;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// LE MÊME PÉRIMÈTRE QUE LA CARTE DE L'ÉLÈVE — compétences actives, options
        /// de terminale écartées, `DomaineRetenu` pour les parties retenues ou
        /// exclues. Une seconde règle écrite ici pour vérifier la première finirait
        /// par ne plus rien vérifier.
        /// </summary>
        public async Task<List<VerificationExamen>> VerifierAsync(CancellationToken ct = default)
        {
            var examens = await _context.Examens
                .AsNoTracking()
                .Include(e => e.Epreuves)
                .Where(e => e.Actif)
                .OrderBy(e => e.Session).ThenBy(e => e.Code)
                .ToListAsync(ct);

            var matieres = await _context.Matieres
                .AsNoTracking()
                .Where(m => m.Code != null)
                .Select(m => new { m.Id, Code = m.Code!, m.Libelle, m.Active })
                .ToListAsync(ct);

            var niveaux = await _context.NiveauxScolaires
                .AsNoTracking()
                .Where(n => n.Code != null)
                .Select(n => new { n.Id, Code = n.Code! })
                .ToListAsync(ct);

            var competences = await _context.Competences
                .AsNoTracking()
                .Where(c => c.Actif && !(c.Domaine != null && c.Domaine.StartsWith("Option ")))
                .Select(c => new { c.MatiereId, c.NiveauScolaireId, c.Domaine })
                .ToListAsync(ct);

            var codesSpecialites = VoiesScolaires.SpecialitesGenerales
                .Select(s => s.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var resultat = new List<VerificationExamen>();

            foreach (var examen in examens)
            {
                var verification = new VerificationExamen
                {
                    Code = examen.Code,
                    Libelle = examen.TitreSection,
                    Session = examen.Session,
                };

                var classes = Codes(examen.NiveauxCodes);

                foreach (var classe in classes.Where(c => !niveaux.Any(n => n.Code.Equals(c, StringComparison.OrdinalIgnoreCase))))
                {
                    verification.Problemes.Add($"Classe inconnue : « {classe} ». Aucun élève de cette classe ne verra l'examen.");
                }

                foreach (var epreuve in examen.Epreuves.OrderBy(e => e.Ordre))
                {
                    var carte = new VerificationEpreuve { Code = epreuve.Code, Libelle = epreuve.Libelle };
                    verification.Epreuves.Add(carte);

                    // --- Les niveaux de programme
                    var niveauxEpreuve = Codes(epreuve.NiveauxProgrammeCodes);
                    var idsNiveaux = niveaux
                        .Where(n => niveauxEpreuve.Contains(n.Code, StringComparer.OrdinalIgnoreCase))
                        .Select(n => n.Id)
                        .ToHashSet();

                    foreach (var code in niveauxEpreuve.Where(c => !niveaux.Any(n => n.Code.Equals(c, StringComparison.OrdinalIgnoreCase))))
                    {
                        carte.Problemes.Add($"Niveau de programme inconnu : « {code} ».");
                    }

                    // --- Les spécialités
                    foreach (var specialite in new[] { epreuve.SpecialiteRequise, epreuve.SpecialiteExclue }
                                 .Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        if (!codesSpecialites.Contains(specialite!))
                        {
                            carte.Problemes.Add($"Spécialité inconnue : « {specialite} ». Aucune case ne permet de la cocher.");
                        }
                    }

                    if ((!string.IsNullOrWhiteSpace(epreuve.SpecialiteRequise) || !string.IsNullOrWhiteSpace(epreuve.SpecialiteExclue))
                        && !classes.Any(c => VoiesScolaires.NombreSpecialites(c) > 0))
                    {
                        carte.Problemes.Add("Carte réservée à une spécialité dans un examen dont aucune classe n'a de "
                            + "spécialités : personne ne la verra.");
                    }

                    // --- Les matières et leurs notions
                    var inclus = Codes(epreuve.DomainesInclus);
                    var exclus = Codes(epreuve.DomainesExclus);
                    var domainesDuProgramme = new List<string?>();

                    foreach (var codeMatiere in Codes(epreuve.MatieresCodes))
                    {
                        var matiere = matieres.FirstOrDefault(m => m.Code.Equals(codeMatiere, StringComparison.OrdinalIgnoreCase));

                        if (matiere is null)
                        {
                            carte.Problemes.Add($"Matière inconnue : « {codeMatiere} ». La carte n'aura pas de professeur pour elle.");
                            carte.Matieres.Add(new VerificationMatiere { Code = codeMatiere });
                            continue;
                        }

                        var duProgramme = competences
                            .Where(c => c.MatiereId == matiere.Id && idsNiveaux.Contains(c.NiveauScolaireId))
                            .ToList();

                        domainesDuProgramme.AddRange(duProgramme.Select(c => c.Domaine));

                        var retenues = duProgramme.Count(c => DomaineRetenu(c.Domaine, inclus, exclus));

                        carte.Matieres.Add(new VerificationMatiere
                        {
                            Code = matiere.Code,
                            Libelle = matiere.Libelle,
                            Active = matiere.Active,
                            NotionsDuProgramme = duProgramme.Count,
                            NotionsRetenues = retenues,
                        });

                        if (!matiere.Active)
                        {
                            carte.Problemes.Add($"{matiere.Libelle} est fermée : la carte ne la montre pas.");
                        }

                        if (retenues == 0)
                        {
                            carte.Problemes.Add(duProgramme.Count == 0
                                ? $"{matiere.Libelle} : aucune notion au programme de ces niveaux. La carte est vide."
                                : $"{matiere.Libelle} : toutes les notions sont écartées par les parties retenues ou exclues. La carte est vide.");
                        }
                    }

                    // --- Chaque partie retenue ou exclue doit correspondre à quelque chose
                    foreach (var prefixe in inclus.Where(p => !domainesDuProgramme.Any(d => (d ?? "").StartsWith(p, StringComparison.OrdinalIgnoreCase))))
                    {
                        carte.Problemes.Add($"Partie retenue « {prefixe} » : aucune notion ne s'appelle ainsi. Faute de frappe ?");
                    }

                    foreach (var prefixe in exclus.Where(p => !domainesDuProgramme.Any(d => (d ?? "").StartsWith(p, StringComparison.OrdinalIgnoreCase))))
                    {
                        carte.Problemes.Add($"Partie exclue « {prefixe} » : elle n'a rien retiré. Faute de frappe ?");
                    }
                }

                resultat.Add(verification);
            }

            return resultat;
        }

        private async Task<PreparationEpreuve> TrouverOuCreerAsync(
            int eleveId, int epreuveId, int matiereId, DateTime maintenant, CancellationToken ct)
        {
            var preparation = await _context.PreparationsEpreuves
                .FirstOrDefaultAsync(p => p.EleveId == eleveId && p.EpreuveId == epreuveId && p.MatiereId == matiereId, ct);

            if (preparation is not null) return preparation;

            preparation = new PreparationEpreuve
            {
                EleveId = eleveId,
                EpreuveId = epreuveId,
                MatiereId = matiereId,
                DateCreation = maintenant,
            };

            _context.PreparationsEpreuves.Add(preparation);
            return preparation;
        }
    }
}
