using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Les conversations d'expression orale.
    ///
    /// PLUS COURT QUE <c>ComprehensionOraleRepository</c> : pas d'audio à
    /// ranger, à servir ni à purger, puisqu'on n'en garde aucun.
    ///
    /// LE FILET DE RECONSTITUTION, LUI, EST BIEN LÀ — Camara, le 18/09/2026 :
    /// « je veux que tu mettes le filet directement ». Voir
    /// <c>GetNonArchiveesAsync</c> : le bloc d'archivage dépend du bon vouloir
    /// du modèle, et la compréhension orale a déjà payé pour l'apprendre.
    /// </summary>
    public class ExpressionOraleRepository : IExpressionOraleRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        /// <summary>
        /// LE JSON EST ÉCRIT EN MINUSCULES, ET RELU SANS SE SOUCIER DE LA CASSE.
        ///
        /// La deuxième moitié n'est pas une précaution de style : c'est
        /// exactement ce qui a fait perdre toutes les cartes de repères des
        /// planches pendant deux jours. `System.Text.Json` est sensible à la
        /// casse par défaut, rien ne se liait, chaque carte se relisait VIDE —
        /// sans exception, sans journal, sans rien.
        /// </summary>
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        public ExpressionOraleRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ExpressionOraleEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string titre,
            string langue,
            IReadOnlyList<TourExpressionOrale> echange,
            string? remarque,
            DateTime? dateExercice = null,
            CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte du modèle —
            // même garde que pour les dictées et les compréhensions orales.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            // La classe au moment de la conversation, figée : une conversation
            // de 6e ne devient pas une conversation de 5e parce que l'année a
            // tourné.
            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync(ct);

            var ligne = new ExpressionOrale
            {
                EleveId = eleveId,
                MatiereId = conversation.MatiereId,
                ConversationId = conversation.Id,
                Titre = Borner(titre, 300),
                Langue = langue.Trim().ToLowerInvariant(),
                Echange = JsonSerializer.Serialize(echange, Json),
                Remarque = string.IsNullOrWhiteSpace(remarque) ? null : Borner(remarque, 2000),
                NiveauScolaireId = niveauId,
                DateCreation = dateExercice ?? DateTime.UtcNow,
            };

            _context.ExpressionsOrales.Add(ligne);
            await _context.SaveChangesAsync(ct);

            return await GetDetailAsync(ligne.Id, eleveId, ct);
        }

        public async Task<IEnumerable<ExpressionOraleEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default)
        {
            // L'ÉCHANGE NE PART PAS DANS LA LISTE. Trente conversations
            // feraient transiter trente échanges complets pour afficher trente
            // titres. Son nombre de tours, lui, se compte en base.
            var lignes = await _context.ExpressionsOrales
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId && e.MatiereId == matiereId)
                .OrderByDescending(e => e.DateCreation)
                .Select(e => new
                {
                    e.Id,
                    e.MatiereId,
                    MatiereLibelle = e.Matiere!.Libelle,
                    e.Matiere.ProfPrenom,
                    e.Matiere.ProfCouleur,
                    e.Titre,
                    e.Langue,
                    e.Echange,
                    e.Remarque,
                    e.NiveauScolaireId,
                    e.DateCreation,
                    e.DateConsultation,
                })
                .ToListAsync(ct);

            return lignes.Select(l => new ExpressionOraleEleve
            {
                Id = l.Id,
                MatiereId = l.MatiereId,
                MatiereLibelle = l.MatiereLibelle,
                ProfPrenom = l.ProfPrenom,
                ProfCouleur = l.ProfCouleur,
                Titre = l.Titre,
                Langue = l.Langue,
                NombreTours = Lire(l.Echange).Count,
                Remarque = l.Remarque,
                NiveauScolaireId = l.NiveauScolaireId,
                DateCreation = l.DateCreation,
                DateConsultation = l.DateConsultation,
            }).ToList();
        }

        public async Task<ExpressionOraleEleve?> GetDetailAsync(
            int id, int eleveId, CancellationToken ct = default)
        {
            // L'ÉLÈVE EST DANS LA CLAUSE : sans lui, un identifiant deviné
            // suffirait à lire la conversation d'un autre enfant.
            var ligne = await _context.ExpressionsOrales
                .AsNoTracking()
                .Where(e => e.Id == id && e.EleveId == eleveId)
                .Select(e => new
                {
                    e.Id,
                    e.MatiereId,
                    MatiereLibelle = e.Matiere!.Libelle,
                    e.Matiere.ProfPrenom,
                    e.Matiere.ProfCouleur,
                    e.Titre,
                    e.Langue,
                    e.Echange,
                    e.Remarque,
                    e.NiveauScolaireId,
                    e.DateCreation,
                    e.DateConsultation,
                })
                .FirstOrDefaultAsync(ct);

            if (ligne is null) return null;

            var echange = Lire(ligne.Echange);

            return new ExpressionOraleEleve
            {
                Id = ligne.Id,
                MatiereId = ligne.MatiereId,
                MatiereLibelle = ligne.MatiereLibelle,
                ProfPrenom = ligne.ProfPrenom,
                ProfCouleur = ligne.ProfCouleur,
                Titre = ligne.Titre,
                Langue = ligne.Langue,
                Echange = echange,
                NombreTours = echange.Count,
                Remarque = ligne.Remarque,
                NiveauScolaireId = ligne.NiveauScolaireId,
                DateCreation = ligne.DateCreation,
                DateConsultation = ligne.DateConsultation,
            };
        }

        public async Task<bool> MarquerVueAsync(
            int id, int eleveId, CancellationToken ct = default)
        {
            // POSÉE UNE SEULE FOIS. La date dit QUAND il l'a découverte ; la
            // réécrire à chaque ouverture en ferait une « dernière visite »,
            // qui ne sert à personne ici.
            var touchees = await _context.ExpressionsOrales
                .Where(e => e.Id == id && e.EleveId == eleveId && e.DateConsultation == null)
                .ExecuteUpdateAsync(
                    m => m.SetProperty(e => e.DateConsultation, DateTime.UtcNow), ct);

            if (touchees > 0) return true;

            // Déjà vue : ce n'est pas un échec. On ne distingue le refus que
            // pour une fiche qui n'est pas la sienne.
            return await _context.ExpressionsOrales
                .AsNoTracking()
                .AnyAsync(e => e.Id == id && e.EleveId == eleveId, ct);
        }

        public async Task<IReadOnlyDictionary<int, CompteurExpressionsOrales>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default)
        {
            var lignes = await _context.ExpressionsOrales
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId)
                .GroupBy(e => e.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    Total = g.Count(),
                    Nouveautes = g.Count(e => e.DateConsultation == null),
                })
                .ToListAsync(ct);

            return lignes.ToDictionary(
                x => x.MatiereId, x => new CompteurExpressionsOrales(x.Total, x.Nouveautes));
        }

        /// <summary>
        /// Les balises de langue, une par langue enseignée. Le nom de la balise
        /// est déclaré côté prompt ; ici on ne fait que les reconnaître dans un
        /// message déjà écrit — même motif que `ComprehensionOraleRepository`.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex Langue =
            new(@"\[(EN|FR|ES|DE|IT|ZH)\](?<texte>.*?)\[/\1\]",
                System.Text.RegularExpressions.RegexOptions.Singleline
                    | System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>La balise qui ouvre une conversation.</summary>
        private static readonly System.Text.RegularExpressions.Regex Debut =
            new(@"\[CONVERSATION\]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    | System.Text.RegularExpressions.RegexOptions.Compiled);

        public async Task<string?> PremiereRepliqueAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var appartient = await _context.Conversations
                .AsNoTracking()
                .AnyAsync(c => c.Id == conversationId && c.EleveId == eleveId, ct);

            if (!appartient) return null;

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Id)
                .Take(60)
                .Select(m => new { m.Id, m.Role, m.Contenu })
                .ToListAsync(ct);

            messages.Reverse();

            // ON PART DE LA DERNIÈRE OUVERTURE : une séance peut porter deux
            // conversations, et la première phrase de la première ne concerne pas
            // la seconde.
            var depuis = -1;

            for (var i = 0; i < messages.Count; i++)
            {
                if (!string.Equals(messages[i].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Debut.IsMatch(messages[i].Contenu ?? string.Empty)) depuis = i;
            }

            if (depuis < 0) return null;

            for (var i = depuis; i < messages.Count; i++)
            {
                if (!string.Equals(messages[i].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    continue;

                var bloc = Langue.Match(messages[i].Contenu ?? string.Empty);
                if (!bloc.Success) continue;

                var texte = bloc.Groups["texte"].Value.Trim();
                if (texte.Length > 0) return texte;
            }

            return null;
        }

        public async Task<IEnumerable<ExpressionOraleReconstituee>> GetNonArchiveesAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return [];

            // Une soixantaine de messages couvre largement une séance et ses
            // quelques échanges — inutile de relire une conversation qui vit des
            // mois.
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Id)
                .Take(60)
                .Select(m => new { m.Id, m.Role, m.Contenu, m.DateCreation })
                .ToListAsync(ct);

            messages.Reverse();

            // LA SÉANCE EN COURS, ET ELLE SEULE. Sans cette borne, un exercice
            // abandonné il y a trois semaines était réarchivé à la fin de chaque
            // séance suivante — voir `FenetreExercice.DebutDeSeance`.
            var depuis = FenetreExercice.DebutDeSeance(
                messages,
                m => string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase),
                m => m.Contenu ?? string.Empty);

            if (depuis > 0) messages = messages.Skip(depuis).ToList();

            // ET CE QUI EST DÉJÀ RANGÉ POUR CETTE SÉANCE, pour ne rien doubler.
            //
            // LA CEINTURE EN PLUS DES BRETELLES, et elle a sa raison : la borne
            // ci-dessus dépend d'un [FIN_SEANCE] que le professeur peut oublier
            // d'écrire. L'horodatage d'ouverture, lui, ne dépend de personne —
            // il vient du message, il est le même à chaque relecture, et il est
            // recopié tel quel dans `DateCreation` par le rattrapage.
            var dejaRangees = await _context.ExpressionsOrales
                .AsNoTracking()
                .Where(e => e.ConversationId == conversationId && e.EleveId == eleveId)
                .Select(e => e.DateCreation)
                .ToListAsync(ct);

            var connues = dejaRangees.ToHashSet();

            var retrouvees = new List<ExpressionOraleReconstituee>();

            var tours = new List<TourExpressionOrale>();
            string? langue = null;
            DateTime debutLe = default;
            var ouverte = false;
            var dejaArchivee = false;

            void Fermer()
            {
                // UN VRAI ALLER-RETOUR, PAS UNE OUVERTURE RESTÉE SANS SUITE.
                //
                // LE DÉFAUT — Camara, le 18/09/2026 : dix archives portant pour
                // tout contenu « C'est bon pour moi. » et « Salut Bilal ! Alors,
                // dis-moi, qu'est-ce que tu as fait le week-end dernier ? ». Il
                // avait choisi la vitesse, puis demandé autre chose : la
                // conversation n'a jamais eu lieu.
                //
                // « LES DEUX VOIX » NE SUFFISAIT PAS À L'ÉCARTER, parce que la
                // phrase du bouton de vitesse EST un message de l'élève. Ce qui
                // distingue une conversation d'une ouverture, c'est que le
                // professeur y REBONDIT : il parle, l'enfant répond, il repart.
                // D'où deux répliques de sa part au minimum.
                //
                // Une archive vide n'est pas neutre : l'enfant clique, ne trouve
                // rien, et apprend que ses archives ne veulent rien dire.
                var vraiEchange = tours.Count(t => t.Qui == "professeur") >= 2
                                   && tours.Any(t => t.Qui == "eleve");

                if (ouverte && !dejaArchivee && langue is not null && vraiEchange
                    && !connues.Contains(debutLe))
                {
                    retrouvees.Add(new ExpressionOraleReconstituee(langue, tours.ToList(), debutLe));

                    // Deux ouvertures au même instant n'existent pas, mais une
                    // même passe pourrait sinon rendre deux fois la même.
                    connues.Add(debutLe);
                }

                tours.Clear();
                langue = null;
                ouverte = false;
                dejaArchivee = false;
            }

            foreach (var message in messages)
            {
                var contenu = message.Contenu ?? string.Empty;
                var duProfesseur = string.Equals(
                    message.Role, "assistant", StringComparison.OrdinalIgnoreCase);

                if (duProfesseur && Debut.IsMatch(contenu))
                {
                    // Une conversation en ouvre une autre : on ferme la
                    // précédente plutôt que de les coller bout à bout.
                    Fermer();

                    ouverte = true;
                    debutLe = message.DateCreation;
                }

                if (!ouverte) continue;

                // TOUT CE QUI N'EST PAS LA CONVERSATION LA REFERME — son archivage
                // comme n'importe quel autre exercice. Voir `FenetreExercice` :
                // une conversation qu'on abandonne ne s'archive jamais, et la
                // fenêtre restait ouverte jusqu'à la fin de la séance.
                //
                // LA NUANCE EST DANS LE DRAPEAU. Le bloc d'archivage dit « déjà
                // rangée », donc on ne double pas. Un AUTRE exercice ne dit que
                // « on est passé à autre chose » : si un vrai échange a eu lieu
                // avant, il doit être sauvé — c'est le rôle même de ce filet.
                if (duProfesseur && FenetreExercice.OuvreAutreChose(
                        contenu, FenetreExercice.Conversation))
                {
                    dejaArchivee = contenu.Contains(
                        "[EXPRESSION_ORALE]", StringComparison.OrdinalIgnoreCase);

                    Fermer();
                    continue;
                }

                if (duProfesseur)
                {
                    // SES RÉPLIQUES SONT DÉJÀ ENTRE BALISES DE LANGUE : c'est ce
                    // qui les fait prononcer dans la bonne langue, et c'est ce qui
                    // rend la reconstitution possible. Ce qui est hors balise est
                    // du français d'explication : il ne fait pas partie de
                    // l'échange.
                    foreach (System.Text.RegularExpressions.Match bloc in Langue.Matches(contenu))
                    {
                        langue ??= bloc.Groups[1].Value.ToLowerInvariant();

                        var texte = bloc.Groups["texte"].Value.Trim();
                        if (texte.Length > 0) tours.Add(new TourExpressionOrale("professeur", texte));
                    }

                    continue;
                }

                // L'ÉLÈVE, LUI, PARLE SANS BALISE : sa voix arrive transcrite, ou
                // tapée. TOUT CE QUI EST ENTRE CROCHETS PART — voir
                // `FenetreExercice.SansCrochets` : ce sont les faits que
                // l'application accroche à ses messages, et Camara les a vus
                // s'afficher en clair dans l'archive d'un enfant.
                var dit = FenetreExercice.SansCrochets(contenu);
                if (dit.Length > 0) tours.Add(new TourExpressionOrale("eleve", dit));
            }

            Fermer();

            return retrouvees;
        }

        /// <summary>
        /// L'échange, relu depuis sa colonne.
        ///
        /// UN JSON ILLISIBLE N'EST PAS UNE PANNE : il vient d'un modèle, donc
        /// il peut être malformé. Dans ce cas la conversation s'affiche vide
        /// plutôt que de faire tomber l'écran — dégradé, jamais cassé, comme
        /// la carte de repères d'une planche.
        /// </summary>
        private static List<TourExpressionOrale> Lire(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];

            try
            {
                return JsonSerializer.Deserialize<List<TourExpressionOrale>>(json, Json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string Borner(string texte, int maximum)
        {
            var propre = texte.Trim();
            return propre.Length <= maximum ? propre : propre[..maximum];
        }
    }
}
