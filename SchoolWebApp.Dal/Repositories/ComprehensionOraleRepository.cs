using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Repositories
{
    public class ComprehensionOraleRepository : IComprehensionOraleRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;
        private readonly IArchiveAudio _archive;

        public ComprehensionOraleRepository(
            SchoolWebAppDatabaseContext context, IArchiveAudio archive)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        }

        public async Task<ComprehensionOraleEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string? titre,
            string langue,
            string passage,
            string reponseEleve,
            string comprehension,
            string? remarque,
            byte[]? audioDonnees,
            DateTime? dateExercice = null,
            CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte du
            // modèle — même garde que pour les dictées.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            // La classe est lue MAINTENANT, pas à la relecture — même raison
            // que sur Dictee.NiveauScolaireId.
            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync(ct);

            var comprehensionOrale = new ComprehensionOrale
            {
                EleveId = eleveId,
                MatiereId = conversation.MatiereId,
                ConversationId = conversation.Id,
                Titre = Tronquer(titre, 300),
                Langue = langue.Trim().ToLowerInvariant(),
                Passage = passage.Trim(),
                ReponseEleve = reponseEleve.Trim(),
                Comprehension = comprehension.Trim(),
                Remarque = Tronquer(remarque, 2000),
                // L AUDIO PART SUR LE DISQUE, LA BASE N EN GARDE QUE L ADRESSE.
                //
                // Six cents kilo-octets par exercice, mesures : a mille eleves
                // c est une cinquantaine de giga-octets par an dans une base
                // qu il faut sauvegarder chaque nuit et restaurer en bloc. Le
                // son n a besoin d aucune de ces garanties.
                //
                // Une ecriture qui echoue rend null : la ligne texte s archive
                // quand meme, sans son. Perdre l exercice entier parce que le
                // disque est plein serait bien pire que le perdre a l ecoute.
                AudioChemin = audioDonnees is { Length: > 0 }
                    ? await _archive.EcrireAsync(audioDonnees, ".wav", ct)
                    : null,
                // L HEURE DE L EXERCICE, PAS CELLE DE L ARCHIVAGE.
                //
                // Le rattrapage tourne a la fin de la seance et ecrit tout en
                // quelques secondes : classees sur cette heure-la, les fiches
                // remontaient dans le desordre, un exercice du matin passant
                // devant celui de midi.
                DateCreation = dateExercice ?? DateTime.UtcNow,
                NiveauScolaireId = niveauId == 0 ? null : niveauId,
            };

            _context.ComprehensionsOrales.Add(comprehensionOrale);
            await _context.SaveChangesAsync(ct);

            return await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.Id == comprehensionOrale.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        /// <summary>
        /// Les balises d'écoute, une par langue enseignée. Le nom de la
        /// balise est déclaré côté prompt (voir <c>PromptsPedagogiques</c>) ;
        /// ici on ne fait que les reconnaître dans un message déjà écrit.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex Ecoute =
            new(@"\[(EN|FR|ES|DE|IT|ZH)\](?<passage>.*?)\[/\1\]",
                System.Text.RegularExpressions.RegexOptions.Singleline
                    | System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Tout bloc technique apparie : [COMPREHENSION_ORALE], [FICHE],
        /// [RAPPORT], [ARDOISE], les balises d'ecoute, et les marqueurs
        /// isoles comme [FIN_SEANCE].
        ///
        /// Le retour du professeur sert d'evaluation quand il n'a pas ecrit
        /// le bloc lui-meme. Or ce meme message contient souvent SES blocs :
        /// recopies tels quels, ils apparaissaient en clair dans « ce que tu
        /// as compris », balises comprises.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex BlocApparie =
            new(@"\[([A-Z_]+)\].*?\[/\1\]",
                System.Text.RegularExpressions.RegexOptions.Singleline
                    | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex MarqueurSeul =
            new(@"\[[A-Z_]+(?::[A-Z_]+)?\]", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex LignesVides =
            new(@"\n\s*\n\s*\n+", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Ce que le professeur propose de faire ENSUITE, et qui n a rien a
        /// voir avec l exercice qu on archive.
        /// </summary>
        private static readonly string[] Transitions =
        [
            "tu veux qu on", "tu veux que", "on a encore", "il te reste",
            "ca te dit", "tu preferes", "on passe a", "je te propose",
            "on enchaine", "veux tu", "on continue avec", "tu as encore",
            "on peut faire", "est ce qu on fait", "on attaque", "on commence",
            "la prochaine fois", "on se retrouve", "tu prefere",
        ];

        /// <summary>
        /// L EVALUATION SEULE : ce que le professeur dit DE CET EXERCICE,
        /// sans la suite du cours.
        ///
        /// Son message enchaine presque toujours deux choses : le retour sur
        /// ce que l eleve vient de comprendre, puis une proposition pour la
        /// suite — « on a encore du temps, tu veux qu on fasse cette
        /// evaluation ? ». Recopie en entier, la fiche se terminait sur une
        /// question sans rapport, posee des semaines plus tot, a laquelle
        /// personne ne repondrait jamais.
        ///
        /// On coupe donc au premier paragraphe, puis on retire les phrases
        /// finales qui proposent la suite.
        /// </summary>
        private static string EvaluationSeule(string? texte)
        {
            var propre = ParoleSeule(texte);
            if (propre.Length == 0) return propre;

            var saut = ((char)10).ToString() + ((char)10).ToString();
            var coupure = propre.IndexOf(saut, StringComparison.Ordinal);
            if (coupure > 0) propre = propre[..coupure].Trim();

            // La proposition tient parfois dans le meme paragraphe : on retire
            // alors les dernieres phrases, une a une, tant qu elles annoncent
            // la suite plutot que de parler de l exercice.
            var phrases = System.Text.RegularExpressions.Regex
                .Split(propre, "(?<=[.!?])" + ((char)92).ToString() + "s+")
                .Where(x => x.Trim().Length > 0)
                .ToList();

            while (phrases.Count > 1)
            {
                var derniere = Cle(phrases[^1]);
                if (!Transitions.Any(t => derniere.Contains(t, StringComparison.Ordinal))) break;

                phrases.RemoveAt(phrases.Count - 1);
            }

            return string.Join(" ", phrases).Trim();
        }

        /// <summary>Ce que le professeur a reellement DIT, blocs techniques retires.</summary>
        private static string ParoleSeule(string? texte)
        {
            var propre = BlocApparie.Replace(texte ?? string.Empty, string.Empty);
            propre = MarqueurSeul.Replace(propre, string.Empty);
            propre = LignesVides.Replace(propre, ((char)10).ToString() + ((char)10).ToString());

            return propre.Trim();
        }

        public async Task<int> SupprimerAsync(
            int conversationId, int eleveId, int? comprehensionOraleId,
            CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return 0;

            List<ComprehensionOrale> cibles;

            if (comprehensionOraleId is int id)
            {
                cibles = await _context.ComprehensionsOrales
                    .Where(c => c.Id == id && c.EleveId == eleveId)
                    .ToListAsync(ct);
            }
            else
            {
                // Le DERNIER passage écouté dans cette conversation. S'il n'a
                // jamais été archivé, il n'y a rien à supprimer : le marqueur,
                // resté dans le message, suffit à ce qu'il ne le soit jamais —
                // voir `GetNonArchiveesAsync`.
                var derniers = await _context.Messages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Id)
                    .Take(60)
                    .Select(m => new { m.Role, m.Contenu })
                    .ToListAsync(ct);

                var passage = derniers
                    .Where(m => string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(m => Ecoute.Matches(m.Contenu ?? string.Empty)
                        .Select(b => b.Groups["passage"].Value.Trim()))
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

                if (passage is null) return 0;

                var cle = Cle(passage);

                cibles = (await _context.ComprehensionsOrales
                    .Where(c => c.EleveId == eleveId && c.MatiereId == conversation.MatiereId)
                    .ToListAsync(ct))
                    .Where(c => string.Equals(Cle(c.Passage), cle, StringComparison.Ordinal))
                    .ToList();
            }

            if (cibles.Count == 0) return 0;

            // LE FICHIER D'ABORD, LA LIGNE ENSUITE — même ordre que la purge,
            // et pour la même raison : un arrêt entre les deux laisserait
            // sinon un son que plus aucune ligne ne désigne, invisible et
            // jamais repris. `Supprimer` est vrai même si le fichier avait
            // déjà disparu : c'est le résultat qui compte.
            foreach (var cible in cibles.Where(c => !string.IsNullOrWhiteSpace(c.AudioChemin)))
            {
                _archive.Supprimer(cible.AudioChemin!);
            }

            _context.ComprehensionsOrales.RemoveRange(cibles);
            await _context.SaveChangesAsync(ct);

            return cibles.Count;
        }

        public async Task<IEnumerable<ComprehensionOraleReconstituee>> GetNonArchiveesAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return [];

            // Une soixantaine de messages couvre largement une séance et ses
            // quelques exercices d'écoute — inutile de relire une conversation
            // qui vit des mois.
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Id)
                .Take(60)
                .Select(m => new { m.Id, m.Role, m.Contenu, m.DateCreation })
                .ToListAsync(ct);

            messages.Reverse();

            // Ce qui est DÉJÀ archivé, pour ne rien doubler. La comparaison
            // porte sur le passage normalisé : c'est lui qui identifie
            // l'exercice, le modèle ne connaît aucun identifiant.
            var dejaArchives = (await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId && c.MatiereId == conversation.MatiereId)
                .Select(c => c.Passage)
                .ToListAsync(ct))
                .Select(Cle)
                .ToHashSet(StringComparer.Ordinal);

            // UN MÊME PASSAGE EST LU PLUSIEURS FOIS, ET C'EST NORMAL.
            //
            // L'élève demande à réécouter — c'est même la bonne méthode, le
            // professeur la lui enseigne. Chaque relecture reparaît donc dans
            // les messages. On les regroupe par passage : un exercice, une
            // archive, quel que soit le nombre d'écoutes.
            var occurrences = new List<(int Index, string Langue, string Cle, string Passage, DateTime Quand)>();

            for (var i = 0; i < messages.Count; i++)
            {
                if (!string.Equals(messages[i].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (System.Text.RegularExpressions.Match bloc
                    in Ecoute.Matches(messages[i].Contenu ?? string.Empty))
                {
                    var passage = bloc.Groups["passage"].Value.Trim();
                    if (string.IsNullOrWhiteSpace(passage)) continue;

                    occurrences.Add((i, bloc.Groups[1].Value.ToLowerInvariant(), Cle(passage), passage,
                        messages[i].DateCreation));
                }
            }

            // UN EXTRAIT RELU RESTE UN AUDIO A PART ENTIERE.
            //
            // Le professeur relit une phrase pour aider l eleve a retrouver un
            // detail : cet extrait a son propre enregistrement, et l eleve doit
            // pouvoir le reecouter seul. On garde donc une fiche par passage.
            // C est L AFFICHAGE qui les imbrique sous l histoire dont ils sont
            // tires, en comparant les textes.
            // CE QUE L ELEVE A REFUSE NE SE RECONSTITUE PAS.
            //
            // Voulu par Camara le 12/09/2026. Sans cette exclusion, le filet de
            // fin de seance recreerait fidelement l exercice qu il venait de
            // faire supprimer : c est exactement le piege rencontre sur la
            // dictee abandonnee. Le marqueur vise le DERNIER passage ecoute
            // avant lui.
            var abandonnes = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < messages.Count; i++)
            {
                if (!string.Equals(messages[i].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (messages[i].Contenu?.Contains(
                        MarqueursComprehensionOrale.Suppression,
                        StringComparison.OrdinalIgnoreCase) != true)
                    continue;

                var avant = occurrences.Where(o => o.Index <= i).ToList();
                if (avant.Count > 0) abandonnes.Add(avant[^1].Cle);
            }

            var trouvees = new List<ComprehensionOraleReconstituee>();

            foreach (var groupe in occurrences.GroupBy(o => o.Cle))
            {
                if (dejaArchives.Contains(groupe.Key)) continue;
                if (abandonnes.Contains(groupe.Key)) continue;

                var premier = groupe.Min(o => o.Index);
                var dernier = groupe.Max(o => o.Index);

                // Jusqu ou chercher sa reponse : la prochaine ecoute d une
                // AUTRE histoire marque le debut d un autre exercice.
                var borne = occurrences
                    .Where(o => o.Index > dernier
                        && !string.Equals(o.Cle, groupe.Key, StringComparison.Ordinal))
                    .Select(o => (int?)o.Index)
                    .FirstOrDefault() ?? messages.Count;

                // CE QU ON RETIENT, C EST SON RESUME, PAS « TU PEUX REPETER ».
                //
                // Entre deux ecoutes, l eleve dit surtout « non », « repete »,
                // « attends ». On garde la reponse la plus etoffee, celle ou il
                // raconte vraiment ce qu il a compris.
                var reponse = messages
                    .Select((m, index) => (m, index))
                    .Where(x => x.index > premier && x.index < borne
                        && string.Equals(x.m.Role, "user", StringComparison.OrdinalIgnoreCase))
                    .Select(x => (Texte: ParoleSeule(x.m.Contenu), x.index))
                    .Where(x => x.Texte.Length >= LongueurMinimumReponse && !EstUneRelance(x.Texte))
                    .OrderByDescending(x => x.Texte.Length)
                    .FirstOrDefault();

                // Aucun vrai resume : l exercice n est pas alle a son terme.
                if (string.IsNullOrWhiteSpace(reponse.Texte)) continue;

                var retour = messages.Skip(reponse.index + 1)
                    .FirstOrDefault(m =>
                        string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    ?.Contenu;

                var comprehension = EvaluationSeule(retour);

                if (comprehension.Length > 2000) comprehension = comprehension[..2000];

                var passageRetenu = groupe.First().Passage;

                trouvees.Add(new ComprehensionOraleReconstituee(
                    groupe.First().Langue,
                    passageRetenu,
                    reponse.Texte.Length > 2000 ? reponse.Texte[..2000] : reponse.Texte,
                    string.IsNullOrWhiteSpace(comprehension)
                        ? "Exercice retrouve a la fin de la seance : le professeur n a pas laisse "
                          + "d observation ecrite."
                        : comprehension,
                    groupe.Min(o => o.Quand)));

                dejaArchives.Add(groupe.Key);
            }

            return trouvees;
        }

        /// <summary>
        /// En dessous, ce n'est pas un résumé : c'est un « oui », un « non »,
        /// ou un mot lâché entre deux écoutes.
        /// </summary>
        private const int LongueurMinimumReponse = 25;

        /// <summary>
        /// L'élève demande-t-il seulement à réécouter ?
        ///
        /// Ces tours-là font partie de l'exercice — réécouter est la méthode
        /// qu'on lui enseigne — mais ils ne disent rien de ce qu'il a compris,
        /// et n'ont donc rien à faire dans « ce que tu as dit ».
        /// </summary>
        private static bool EstUneRelance(string texte)
        {
            var propre = Cle(texte);

            return propre.Contains("repet", StringComparison.Ordinal)
                || propre.Contains("repete", StringComparison.Ordinal)
                || propre.Contains("reecout", StringComparison.Ordinal)
                || propre.Contains("redis", StringComparison.Ordinal)
                || propre.Contains("encore une fois", StringComparison.Ordinal)
                || propre.Contains("j ai pas compris", StringComparison.Ordinal)
                || propre.Contains("je n ai pas compris", StringComparison.Ordinal)
                || propre.Contains("j ai besoin de", StringComparison.Ordinal)
                || propre.Contains("attends", StringComparison.Ordinal)

                // Verifier que le micro passe fait partie du meme registre :
                // « tu m entends ? » n est pas une reponse a l exercice.
                || propre.Contains("tu m entends", StringComparison.Ordinal)
                || propre.Contains("tu entends", StringComparison.Ordinal)
                || propre.Contains("m entends tu", StringComparison.Ordinal)
                || propre.Contains("allo", StringComparison.Ordinal)
                || propre.Contains("c est bon", StringComparison.Ordinal);
        }

        /// <summary>
        /// La clé de rapprochement d'un passage : accents, casse et
        /// ponctuation écartés. Même principe que pour les dictées.
        /// </summary>
        private static string Cle(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return string.Empty;

            var lettres = texte
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                            != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');

            return string.Join(' ', new string(lettres.ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        public async Task<string?> MeilleureReponseAsync(
            int conversationId, int eleveId, string passage, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(passage)) return null;

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

            var recherche = Cle(passage);
            if (recherche.Length == 0) return null;

            // Ou le passage a-t-il ete lu ? On prend la premiere lecture : sa
            // reponse peut venir apres n importe laquelle des reecoutes.
            var premier = -1;
            var dernier = -1;

            for (var k = 0; k < messages.Count; k++)
            {
                if (!string.Equals(messages[k].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    continue;

                var contenu = Cle(messages[k].Contenu);
                if (!contenu.Contains(recherche, StringComparison.Ordinal)) continue;

                if (premier < 0) premier = k;
                dernier = k;
            }

            if (premier < 0) return null;

            // Jusqu ou chercher : la prochaine ecoute d un AUTRE passage.
            var borne = messages.Count;

            for (var k = dernier + 1; k < messages.Count; k++)
            {
                if (!string.Equals(messages[k].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    continue;

                var autre = Ecoute.Matches(messages[k].Contenu ?? string.Empty)
                    .Select(m => Cle(m.Groups["passage"].Value))
                    .Any(c => c.Length > 0 && !recherche.Contains(c, StringComparison.Ordinal)
                        && !c.Contains(recherche, StringComparison.Ordinal));

                if (autre) { borne = k; break; }
            }

            return messages
                .Select((m, index) => (m, index))
                .Where(x => x.index > premier && x.index < borne
                    && string.Equals(x.m.Role, "user", StringComparison.OrdinalIgnoreCase))
                .Select(x => ParoleSeule(x.m.Contenu))
                .Where(t => t.Length >= LongueurMinimumReponse && !EstUneRelance(t))
                .OrderByDescending(t => t.Length)
                .FirstOrDefault();
        }

        public async Task<IEnumerable<ComprehensionOraleEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default) =>
            await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId && c.MatiereId == matiereId)
                .OrderByDescending(c => c.DateCreation)
                .Select(Projection)
                .ToListAsync(ct);

        public async Task<ComprehensionOraleEleve?> GetDetailAsync(
            int id, int eleveId, CancellationToken ct = default) =>
            // Le filtre sur l'élève est la garde d'accès.
            await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.Id == id && c.EleveId == eleveId)
                .Select(c => new ComprehensionOraleEleve
                {
                    Id = c.Id,
                    MatiereId = c.MatiereId,
                    ConversationId = c.ConversationId,
                    MatiereLibelle = c.Matiere!.Libelle,
                    ProfPrenom = c.Matiere.ProfPrenom,
                    ProfCouleur = c.Matiere.ProfCouleur,
                    Titre = c.Titre,
                    Langue = c.Langue,
                    Passage = c.Passage,
                    ReponseEleve = c.ReponseEleve,
                    Comprehension = c.Comprehension,
                    Remarque = c.Remarque,
                    AudioDisponible = c.AudioChemin != null || c.AudioDonnees != null,
                    AudioEffaceLe = c.AudioEffaceLe,
                    DateCreation = c.DateCreation,
                    DateConsultation = c.DateConsultation,
                    ElevePrenom = c.Eleve!.Prenom,
                    EleveNom = c.Eleve.Nom,
                    EleveNiveau = c.Eleve.NiveauScolaire!.Libelle,
                })
                .FirstOrDefaultAsync(ct);

        // LE DISQUE D ABORD, LA COLONNE ENSUITE.
        //
        // Les archives ecrites avant le 10/09/2026 ont leur son en base et
        // aucun chemin : elles resteraient muettes si on ne lisait que le
        // disque. La purge finira par les vider, et ce repli deviendra alors
        // sans objet — mais tant qu elles existent, elles doivent s ecouter.
        public async Task<byte[]?> GetAudioAsync(
            int id, int eleveId, CancellationToken ct = default)
        {
            var range = await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.Id == id && c.EleveId == eleveId)
                .Select(c => new { c.AudioChemin, c.AudioDonnees })
                .FirstOrDefaultAsync(ct);

            if (range is null) return null;

            return string.IsNullOrWhiteSpace(range.AudioChemin)
                ? range.AudioDonnees
                : await _archive.LireAsync(range.AudioChemin, ct);
        }

        // LE FICHIER D ABORD, LA BASE ENSUITE — ET C EST L ORDRE QUI COMPTE.
        //
        // Si le processus s arrete entre les deux, la ligne pointe une seconde
        // vers un fichier absent : la lecture rend null, l ecran dit que
        // l audio est indisponible, et le passage suivant de la purge reprend
        // la ligne et finit le travail. L incoherence se repare toute seule.
        //
        // Dans l autre sens, un arret laisserait un fichier que plus aucune
        // ligne ne designe : invisible, jamais repris, il occuperait le disque
        // pour toujours — exactement ce que cette purge existe pour eviter.
        public async Task<(int Effaces, long Octets)> PurgerAudiosAsync(
            TimeSpan anciennete, int limite, CancellationToken ct = default)
        {
            var limiteTemps = DateTime.UtcNow - anciennete;

            var cibles = await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.AudioEffaceLe == null
                            && (c.AudioChemin != null || c.AudioDonnees != null)
                            && c.DateCreation < limiteTemps)
                .OrderBy(c => c.DateCreation)
                .Take(limite)
                .Select(c => new
                {
                    c.Id,
                    c.AudioChemin,
                    // La taille, pas les octets : charger le son pour le
                    // compter le ferait passer par la memoire du serveur, ce
                    // que la purge cherche justement a eviter.
                    Poids = c.AudioDonnees == null ? 0 : c.AudioDonnees.Length,
                })
                .ToListAsync(ct);

            if (cibles.Count == 0) return (0, 0);

            foreach (var cible in cibles.Where(c => c.AudioChemin != null))
            {
                _archive.Supprimer(cible.AudioChemin!);
            }

            var ids = cibles.Select(c => c.Id).ToList();
            var maintenant = DateTime.UtcNow;

            await _context.ComprehensionsOrales
                .Where(c => ids.Contains(c.Id))
                .ExecuteUpdateAsync(
                    m => m.SetProperty(c => c.AudioDonnees, (byte[]?)null)
                          .SetProperty(c => c.AudioChemin, (string?)null)
                          .SetProperty(c => c.AudioEffaceLe, maintenant),
                    ct);

            return (cibles.Count, cibles.Sum(c => (long)c.Poids));
        }

        public async Task<bool> MarquerVueAsync(
            int id, int eleveId, CancellationToken ct = default)
        {
            var comprehensionOrale = await _context.ComprehensionsOrales
                .FirstOrDefaultAsync(c => c.Id == id && c.EleveId == eleveId, ct);

            if (comprehensionOrale is null) return false;

            comprehensionOrale.DateConsultation = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<IReadOnlyDictionary<int, CompteurComprehensionsOrales>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default)
        {
            var lignes = await _context.ComprehensionsOrales
                .AsNoTracking()
                .Where(c => c.EleveId == eleveId)
                .GroupBy(c => c.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    Total = g.Count(),
                    Nouveautes = g.Count(c => c.DateConsultation == null),
                })
                .ToListAsync(ct);

            return lignes.ToDictionary(
                x => x.MatiereId, x => new CompteurComprehensionsOrales(x.Total, x.Nouveautes));
        }

        private static readonly System.Linq.Expressions.Expression<Func<ComprehensionOrale, ComprehensionOraleEleve>> Projection =
            c => new ComprehensionOraleEleve
            {
                Id = c.Id,
                MatiereId = c.MatiereId,
                    ConversationId = c.ConversationId,
                MatiereLibelle = c.Matiere!.Libelle,
                ProfPrenom = c.Matiere.ProfPrenom,
                ProfCouleur = c.Matiere.ProfCouleur,
                Titre = c.Titre,
                Langue = c.Langue,
                Passage = c.Passage,
                ReponseEleve = c.ReponseEleve,
                Comprehension = c.Comprehension,
                Remarque = c.Remarque,
                AudioDisponible = c.AudioChemin != null || c.AudioDonnees != null,
                AudioEffaceLe = c.AudioEffaceLe,
                DateCreation = c.DateCreation,
                DateConsultation = c.DateConsultation,
            };

        /// <summary>
        /// Le texte vient d'un modèle : rien ne garantit qu'il respecte la
        /// longueur de colonne. On tronque plutôt que de laisser SQL Server
        /// rejeter l'insertion et perdre l'archive.
        /// </summary>
        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }
    }
}
