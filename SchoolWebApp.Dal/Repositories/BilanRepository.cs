using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class BilanRepository : IBilanRepository
    {
        /// <summary>
        /// Messages remontés par matière pour faire rédiger le bilan. Au-delà,
        /// on ne gagne plus en justesse et on paie des jetons pour rien.
        /// </summary>
        private const int ExtraitsMax = 40;

        /// <summary>Longueur d'un extrait. Une réponse de professeur dépasse rarement cela.</summary>
        private const int LongueurExtrait = 400;

        private readonly SchoolWebAppDatabaseContext _context;

        public BilanRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Combien d'élèves recevraient un bilan automatique aujourd'hui.
        ///
        /// SERT À DIMENSIONNER L'ENVOI AVANT DE LE LANCER. Le nombre de
        /// courriels d'un lundi n'est pas le nombre de parents : un parent de
        /// trois enfants reçoit trois bilans. Le seul chiffre qui compte pour
        /// tenir dans un quota d'envoi est celui-ci.
        /// </summary>
        public Task<int> CompterEligiblesAsync(CancellationToken ct = default) =>
            Eligibles().CountAsync(ct);

        /// <summary>
        /// QUI REÇOIT LE BILAN HEBDOMADAIRE.
        ///
        /// Il n'y avait aucune condition : tout enfant dont le parent avait une
        /// adresse en recevait un. Mesuré sur la base de développement, six
        /// familles sur huit n'avaient AUCUN abonnement — et comme un enfant
        /// sans activité déclenche le mail « n'a pas travaillé cette semaine »,
        /// elles recevaient un rappel hebdomadaire pour un service qu'elles
        /// n'ont jamais pris. C'est ainsi qu'on se fait classer en indésirable.
        ///
        /// POURQUOI L'ESSAI EN EST EXCLU AUSSI.
        /// Il dure sept jours et donne trente minutes. Le bilan part le lundi :
        /// un parent en essai en recevrait au plus un, et le plus souvent celui
        /// qui constate qu'il n'a rien fait. On reproche son inactivité à
        /// quelqu'un qui vient d'arriver.
        ///
        /// EN PAUSE, ON SE TAIT. La pause est une absence choisie et payée :
        /// lui signaler chaque lundi que son enfant n'a pas travaillé, c'est
        /// lui reprocher ce qu'il a demandé.
        ///
        /// Les profils retirés ou anonymisés sont écartés au passage : sans
        /// cela, un parent recevrait « Profil supprimé n'a pas travaillé cette
        /// semaine ».
        ///
        /// EXTRAIT EN MÉTHODE parce que le COMPTAGE et l'ENVOI doivent porter
        /// sur exactement la même population. Deux copies de ce filtre
        /// finiraient par diverger, et on découperait les groupes d'après un
        /// total qui ne correspond plus à qui reçoit vraiment.
        /// </summary>
        private IQueryable<Entities.Eleve> Eligibles() =>
            _context.Eleves
                .AsNoTracking()
                .Where(e => e.Parent!.Mail != null
                            && e.Parent.Mail != ""
                            && e.ArchiveLe == null
                            && e.AnonymiseLe == null
                            && e.Parent.Abonnements.Any(a =>
                                a.Statut == StatutAbonnement.Actif && !a.Offre!.EstEssai));

        public async Task<IEnumerable<BilanEleve>> GetBilansAsync(
            DateTime debut,
            DateTime fin,
            int? eleveId = null,
            int? groupeDuJour = null,
            int? nombreDeGroupes = null,
            CancellationToken ct = default)
        {
            IQueryable<Entities.Eleve> elevesQuery;

            if (eleveId.HasValue)
            {
                // UN ÉLÈVE NOMMÉ ÉCHAPPE AU FILTRE, ET C'EST VOULU.
                //
                // Ce chemin n'est emprunté que par l'administration : l'aperçu
                // d'un bilan, et l'envoi d'essai sur un enfant précis. Y
                // appliquer les mêmes règles rendrait l'aperçu impossible
                // justement pour les comptes qu'on veut inspecter — ceux en
                // essai, ceux sans abonnement. C'est un geste délibéré d'un
                // administrateur, pas un envoi automatique.
                elevesQuery = _context.Eleves
                    .AsNoTracking()
                    .Where(e => e.Parent!.Mail != null
                                && e.Parent.Mail != ""
                                && e.Id == eleveId.Value);
            }
            else
            {
                elevesQuery = Eligibles();
            }

            // LE DÉCOUPAGE EN GROUPES, FAIT PAR LA BASE.
            //
            // Au-delà d'un certain nombre d'enfants, tous les bilans ne peuvent
            // plus partir le même jour : le service d'envoi borne le volume
            // quotidien. La série est donc étalée, un groupe par jour.
            //
            // LE RESTE DE LA DIVISION DE L'IDENTIFIANT, et non un tri par date
            // d'inscription ou un découpage par tranches. Trois raisons :
            // l'appartenance d'un enfant à son groupe est STABLE d'une semaine
            // à l'autre — il reçoit son bilan le même jour, ce qui devient une
            // habitude ; la répartition est ÉQUILIBRÉE sans rien calculer ; et
            // un nouvel inscrit ne décale personne.
            //
            // Filtré en SQL et non après chargement : découper en mémoire
            // rapatrierait toute la base chaque jour pour n'en garder qu'un
            // septième.
            if (groupeDuJour.HasValue && nombreDeGroupes is > 1)
            {
                var parts = nombreDeGroupes.Value;
                var mien = groupeDuJour.Value;
                elevesQuery = elevesQuery.Where(e => e.Id % parts == mien);
            }

            var eleves = await elevesQuery
                .Select(e => new BilanEleve
                {
                    EleveId = e.Id,
                    Prenom = e.Prenom,
                    Age = e.Age,
                    Sexe = e.Sexe,
                    NiveauLibelle = e.NiveauScolaire!.Libelle,
                    ParentId = e.ParentId,
                    ParentMail = e.Parent!.Mail,
                    ParentPrenom = e.Parent.Prenom,
                    DerniereActivite = e.DerniereActivite,
                    Debut = debut,
                    Fin = fin,
                })
                .ToListAsync(ct);

            if (eleves.Count == 0) return eleves;

            var identifiants = eleves.Select(e => e.EleveId).ToList();

            // Les messages de la semaine, une seule requête pour tout le monde :
            // une requête par élève ferait autant d'allers-retours qu'il y a
            // d'enfants inscrits, et le worker tourne sur toute la base.
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => identifiants.Contains(m.Conversation!.EleveId)
                            && m.DateCreation >= debut
                            && m.DateCreation < fin)
                .OrderBy(m => m.DateCreation)
                .Select(m => new
                {
                    m.Conversation!.EleveId,
                    m.Conversation.MatiereId,
                    m.ConversationId,
                    MatiereLibelle = m.Conversation.Matiere!.Libelle,
                    ProfPrenom = m.Conversation.Matiere.ProfPrenom,
                    ProfCouleur = m.Conversation.Matiere.ProfCouleur,
                    m.Role,
                    m.Contenu,
                })
                .ToListAsync(ct);

            foreach (var bilan in eleves)
            {
                var siens = messages.Where(m => m.EleveId == bilan.EleveId).ToList();
                if (siens.Count == 0) continue;

                bilan.NombreEchanges = siens.Count(m => m.Role == "assistant");
                bilan.NombreSeances = siens.Select(m => m.ConversationId).Distinct().Count();

                foreach (var groupe in siens.GroupBy(m => m.MatiereId))
                {
                    var premier = groupe.First();

                    bilan.Matieres.Add(new BilanMatiere
                    {
                        MatiereId = groupe.Key,
                        Libelle = premier.MatiereLibelle,
                        ProfPrenom = premier.ProfPrenom,
                        ProfCouleur = premier.ProfCouleur,
                        NombreSeances = groupe.Select(m => m.ConversationId).Distinct().Count(),
                        NombreEchanges = groupe.Count(m => m.Role == "assistant"),

                        // Les derniers échanges plutôt que les premiers : c'est
                        // là que se voit où l'enfant en est arrivé.
                        Extraits = groupe
                            .TakeLast(ExtraitsMax)
                            .Select(m => Condenser(m.Role, m.Contenu))
                            .ToList(),
                    });
                }

                bilan.Matieres = bilan.Matieres
                    .OrderByDescending(m => m.NombreEchanges)
                    .ToList();

                var principale = bilan.Matieres.FirstOrDefault();
                bilan.SignatureProf = principale?.ProfPrenom;
                bilan.SignatureMatiere = principale?.Libelle;
            }

            // Les notes de la semaine, pour tout le monde d'un coup. Rattachées
            // même aux élèves sans message : une évaluation passée le dimanche
            // soir compte, quelle que soit l'activité du reste de la semaine.
            var notes = await _context.Evaluations
                .AsNoTracking()
                .Where(e => identifiants.Contains(e.EleveId)
                            && e.DateCreation >= debut
                            && e.DateCreation < fin)
                .OrderBy(e => e.DateCreation)
                .Select(e => new
                {
                    e.EleveId,
                    Evaluation = new EvaluationEleve
                    {
                        Id = e.Id,
                        MatiereId = e.MatiereId,
                        MatiereLibelle = e.Matiere!.Libelle,
                        ProfPrenom = e.Matiere.ProfPrenom,
                        ProfCouleur = e.Matiere.ProfCouleur,
                        Notion = e.Notion,
                        Note = e.Note,
                        Remarque = e.Remarque,
                        ARevoir = e.ARevoir,
                        DateCreation = e.DateCreation
                    }
                })
                .ToListAsync(ct);

            foreach (var bilan in eleves)
            {
                bilan.Evaluations = notes
                    .Where(n => n.EleveId == bilan.EleveId)
                    .Select(n => n.Evaluation)
                    .ToList();
            }

            return eleves;
        }

        /// <summary>
        /// Une ligne de transcription. L'ardoise est retirée : elle contient des
        /// calculs bruts qui gonflent le contexte sans rien dire de ce que
        /// l'élève a compris.
        /// </summary>
        private static string Condenser(string? role, string? contenu)
        {
            var texte = contenu ?? string.Empty;

            var debut = texte.IndexOf("[ARDOISE]", StringComparison.Ordinal);
            while (debut >= 0)
            {
                var fin = texte.IndexOf("[/ARDOISE]", debut, StringComparison.Ordinal);
                if (fin < 0)
                {
                    texte = texte[..debut];
                    break;
                }

                texte = texte.Remove(debut, fin - debut + "[/ARDOISE]".Length);
                debut = texte.IndexOf("[ARDOISE]", StringComparison.Ordinal);
            }

            texte = texte.Trim();
            if (texte.Length > LongueurExtrait) texte = texte[..LongueurExtrait] + "…";

            return $"{(role == "assistant" ? "Professeur" : "Élève")} : {texte}";
        }
    }
}
