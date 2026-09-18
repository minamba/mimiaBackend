using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Les textes d'expression écrite et leurs corrections.
    ///
    /// LE TROISIÈME DE LA FAMILLE. Pas d'audio, comme l'expression orale — mais
    /// DEUX colonnes de texte au lieu d'une, parce que ce qu'on archive ici,
    /// c'est la PAIRE : ce que l'enfant a écrit, et ce qu'il fallait écrire.
    ///
    /// SA CONTRE-VÉRIFICATION EST PARTICULIÈRE, voir <c>TexteTapeAsync</c> :
    /// ailleurs on rattrape ce que le modèle OUBLIE, ici on rattrape ce qu'il
    /// CORRIGE en recopiant. C'est son réflexe le plus profond, et il détruit
    /// exactement ce qu'on voulait garder.
    /// </summary>
    public class ExpressionEcriteRepository : IExpressionEcriteRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        /// <summary>
        /// Minuscules à l'écriture, insensible à la casse à la relecture — la
        /// seconde moitié est ce qui a fait perdre toutes les cartes de repères
        /// des planches pendant deux jours.
        /// </summary>
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        public ExpressionEcriteRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ExpressionEcriteEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string titre,
            string langue,
            string consigne,
            string? texte,
            IReadOnlyList<RepriseEcrite> corrections,
            string? remarque,
            DateTime? dateExercice = null,
            byte[]? photo = null,
            string? photoTypeMime = null,
            CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte du modèle.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            // La classe au moment de l'exercice, figée : un texte de 6e ne
            // devient pas un texte de 5e parce que l'année a tourné.
            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync(ct);

            // LES GENRES SONT NORMALISÉS À L'ÉCRITURE, PAS À LA LECTURE. Une
            // ligne déjà en base ne se renormalise plus : mieux vaut un seul
            // vocabulaire dans la colonne qu'une traduction à chaque affichage.
            var propres = corrections
                .Where(c => !string.IsNullOrWhiteSpace(c.Texte))
                .Select(c => new RepriseEcrite(GenreReprise.Normaliser(c.Genre), c.Texte.Trim()))
                .ToList();

            var ligne = new ExpressionEcrite
            {
                EleveId = eleveId,
                MatiereId = conversation.MatiereId,
                ConversationId = conversation.Id,
                Titre = Borner(titre, 300),
                Langue = langue.Trim().ToLowerInvariant(),
                Consigne = Borner(consigne, 1000),

                // NULL ET NON CHAÎNE VIDE : « pas encore recopié » n'est pas
                // « il n'a rien écrit ». La photo, elle, est là.
                Texte = string.IsNullOrWhiteSpace(texte) ? null : texte.Trim(),
                Photo = photo is { Length: > 0 } ? photo : null,
                PhotoTypeMime = photo is { Length: > 0 } ? photoTypeMime : null,
                Corrections = JsonSerializer.Serialize(propres, Json),
                Remarque = string.IsNullOrWhiteSpace(remarque) ? null : Borner(remarque, 2000),
                NiveauScolaireId = niveauId,
                DateCreation = dateExercice ?? DateTime.UtcNow,
            };

            _context.ExpressionsEcrites.Add(ligne);
            await _context.SaveChangesAsync(ct);

            return await GetDetailAsync(ligne.Id, eleveId, ct);
        }

        public async Task<IEnumerable<ExpressionEcriteEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default)
        {
            // NI LE TEXTE NI LA CORRECTION DANS LA LISTE. La consigne, si : elle
            // tient en une ligne, et c'est elle qui dit à l'élève ce qu'il
            // retrouvera en ouvrant.
            var lignes = await _context.ExpressionsEcrites
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
                    e.Consigne,
                    e.Texte,
                    e.Corrections,

                    // LE BOOLÉEN, JAMAIS LES OCTETS : trente lignes ne font pas
                    // voyager trente photos de trois mégaoctets.
                    APhoto = e.Photo != null && e.PhotoEffaceeLe == null,

                    e.Remarque,
                    e.NiveauScolaireId,
                    e.DateCreation,
                    e.DateConsultation,
                })
                .ToListAsync(ct);

            return lignes.Select(l => new ExpressionEcriteEleve
            {
                Id = l.Id,
                MatiereId = l.MatiereId,
                MatiereLibelle = l.MatiereLibelle,
                ProfPrenom = l.ProfPrenom,
                ProfCouleur = l.ProfCouleur,
                Titre = l.Titre,
                Langue = l.Langue,
                Consigne = l.Consigne,
                NombreReprises = Lire(l.Corrections).Count(c => c.Genre != GenreReprise.Reussi),
                NombreMots = CompterMots(l.Texte),
                APhoto = l.APhoto,
                Transcrit = l.Texte != null,
                Remarque = l.Remarque,
                NiveauScolaireId = l.NiveauScolaireId,
                DateCreation = l.DateCreation,
                DateConsultation = l.DateConsultation,
            }).ToList();
        }

        public async Task<ExpressionEcriteEleve?> GetDetailAsync(
            int id, int eleveId, CancellationToken ct = default)
        {
            // L'ÉLÈVE EST DANS LA CLAUSE : sans lui, un identifiant deviné
            // suffirait à lire le texte d'un autre enfant.
            var ligne = await _context.ExpressionsEcrites
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
                    e.Consigne,
                    e.Texte,
                    e.Corrections,
                    APhoto = e.Photo != null && e.PhotoEffaceeLe == null,
                    e.Remarque,
                    e.NiveauScolaireId,
                    e.DateCreation,
                    e.DateConsultation,
                })
                .FirstOrDefaultAsync(ct);

            if (ligne is null) return null;

            var corrections = Lire(ligne.Corrections);

            return new ExpressionEcriteEleve
            {
                Id = ligne.Id,
                MatiereId = ligne.MatiereId,
                MatiereLibelle = ligne.MatiereLibelle,
                ProfPrenom = ligne.ProfPrenom,
                ProfCouleur = ligne.ProfCouleur,
                Titre = ligne.Titre,
                Langue = ligne.Langue,
                Consigne = ligne.Consigne,
                Texte = ligne.Texte,
                APhoto = ligne.APhoto,
                Transcrit = ligne.Texte != null,
                Corrections = corrections,
                NombreReprises = corrections.Count(c => c.Genre != GenreReprise.Reussi),
                NombreMots = CompterMots(ligne.Texte),
                Remarque = ligne.Remarque,
                NiveauScolaireId = ligne.NiveauScolaireId,
                DateCreation = ligne.DateCreation,
                DateConsultation = ligne.DateConsultation,
            };
        }

        public async Task<bool> MarquerVueAsync(
            int id, int eleveId, CancellationToken ct = default)
        {
            // POSÉE UNE SEULE FOIS : la date dit QUAND il l'a découvert, pas
            // quand il l'a relu pour la dernière fois.
            var touchees = await _context.ExpressionsEcrites
                .Where(e => e.Id == id && e.EleveId == eleveId && e.DateConsultation == null)
                .ExecuteUpdateAsync(
                    m => m.SetProperty(e => e.DateConsultation, DateTime.UtcNow), ct);

            if (touchees > 0) return true;

            // Déjà vu : ce n'est pas un échec. On ne distingue le refus que pour
            // un texte qui n'est pas le sien.
            return await _context.ExpressionsEcrites
                .AsNoTracking()
                .AnyAsync(e => e.Id == id && e.EleveId == eleveId, ct);
        }

        public async Task<ExpressionEcriteEleve?> CompleterAsync(
            int id,
            int eleveId,
            string texte,
            IReadOnlyList<RepriseEcrite> corrections,
            string? remarque,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propres = corrections
                .Where(c => !string.IsNullOrWhiteSpace(c.Texte))
                .Select(c => new RepriseEcrite(GenreReprise.Normaliser(c.Genre), c.Texte.Trim()))
                .ToList();

            // `Texte == null` EST DANS LA CLAUSE, ET C'EST UNE GARDE, PAS UN
            // FILTRE. Une transcription déjà faite ne se réécrit jamais : le
            // professeur qui relit une vieille archive ne doit pas pouvoir la
            // remplacer par ce qu'il croit y avoir lu.
            var touchees = await _context.ExpressionsEcrites
                .Where(e => e.Id == id && e.EleveId == eleveId && e.Texte == null)
                .ExecuteUpdateAsync(
                    m => m.SetProperty(e => e.Texte, texte.Trim())
                          .SetProperty(e => e.Corrections, JsonSerializer.Serialize(propres, Json))
                          .SetProperty(e => e.Remarque,
                              string.IsNullOrWhiteSpace(remarque) ? null : Borner(remarque, 2000)),
                    ct);

            return touchees > 0 ? await GetDetailAsync(id, eleveId, ct) : null;
        }

        public async Task<TexteATranscrire?> ATranscrireAsync(
            int eleveId, int matiereId, CancellationToken ct = default)
        {
            // LE PLUS RÉCENT, PAS LE PLUS ANCIEN : la dernière chose qu'il a
            // faite est celle dont il se souvient, et celle qu'il risque
            // d'oublier si on ne la lui rappelle pas.
            var ligne = await _context.ExpressionsEcrites
                .AsNoTracking()
                .Where(e => e.EleveId == eleveId
                            && e.MatiereId == matiereId
                            && e.Texte == null
                            && e.Photo != null
                            && e.PhotoEffaceeLe == null)
                .OrderByDescending(e => e.DateCreation)
                .Select(e => new
                {
                    e.Id, e.Titre, e.Consigne, e.DateCreation, e.Photo, e.PhotoTypeMime,
                })
                .FirstOrDefaultAsync(ct);

            if (ligne?.Photo is not { Length: > 0 }) return null;

            return new TexteATranscrire(
                ligne.Id, ligne.Titre, ligne.Consigne, ligne.DateCreation,
                ligne.Photo, ligne.PhotoTypeMime ?? "image/jpeg");
        }

        public async Task<(byte[] Donnees, string TypeMime)?> GetPhotoAsync(
            int id, int eleveId, CancellationToken ct = default)
        {
            // L'ÉLÈVE EST DANS LA CLAUSE : sans lui, un identifiant deviné
            // suffirait à voir la copie d'un autre enfant.
            var ligne = await _context.ExpressionsEcrites
                .AsNoTracking()
                .Where(e => e.Id == id && e.EleveId == eleveId && e.PhotoEffaceeLe == null)
                .Select(e => new { e.Photo, e.PhotoTypeMime })
                .FirstOrDefaultAsync(ct);

            if (ligne?.Photo is not { Length: > 0 }) return null;

            return (ligne.Photo, ligne.PhotoTypeMime ?? "image/jpeg");
        }

        public async Task<(int Photos, long Octets)> PurgerPhotosAsync(
            TimeSpan anciennete, int limite, CancellationToken ct = default)
        {
            var limiteTemps = DateTime.UtcNow - anciennete;

            // `Texte != null` EST LA CONDITION, PAS UNE PRÉCAUTION. Effacer les
            // octets d'une copie dont le texte n'a pas encore été recopié la
            // perdrait définitivement, et sans bruit — la règle est reprise mot
            // pour mot de la purge des pièces jointes, où elle a été payée.
            var cibles = await _context.ExpressionsEcrites
                .AsNoTracking()
                .Where(e => e.PhotoEffaceeLe == null
                            && e.Photo != null
                            && e.Texte != null
                            && e.DateCreation < limiteTemps)
                .OrderBy(e => e.DateCreation)
                .Take(limite)
                .Select(e => new { e.Id, Taille = e.Photo!.Length })
                .ToListAsync(ct);

            if (cibles.Count == 0) return (0, 0);

            var ids = cibles.Select(c => c.Id).ToList();
            var maintenant = DateTime.UtcNow;

            await _context.ExpressionsEcrites
                .Where(e => ids.Contains(e.Id))
                .ExecuteUpdateAsync(
                    m => m.SetProperty(e => e.Photo, (byte[]?)null)
                          .SetProperty(e => e.PhotoEffaceeLe, maintenant),
                    ct);

            return (cibles.Count, cibles.Sum(c => (long)c.Taille));
        }

        public async Task<IReadOnlyDictionary<int, CompteurExpressionsEcrites>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default)
        {
            var lignes = await _context.ExpressionsEcrites
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
                x => x.MatiereId, x => new CompteurExpressionsEcrites(x.Total, x.Nouveautes));
        }

        // ------------------------------------------------------------------
        // La relecture des messages bruts
        // ------------------------------------------------------------------

        /// <summary>La balise qui ouvre l'exercice et pose la question du support.</summary>
        private static readonly Regex Ouverture =
            new(@"\[SUPPORT_ECRIT\]",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Le fait envoyé par l'application quand l'enfant a choisi le clavier.</summary>
        private static readonly Regex AuClavier =
            new(@"\[TEXTE AU CLAVIER",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Celui du cahier. On le reconnaît pour SAVOIR QU'IL N'Y A RIEN À
        /// RELIRE : le texte n'existe alors que dans une photo.
        /// </summary>
        private static readonly Regex AuCahier =
            new(@"\[TEXTE AU CAHIER",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Sous ce nombre de caractères, ce n'est pas un texte d'expression
        /// écrite.
        ///
        /// « ok », « c'est bon », « j'ai fini » sont des messages de séance, pas
        /// des copies. Le seuil est bas exprès : trois phrases de CE2 font déjà
        /// bien plus de quarante caractères, et rien ne doit être perdu en
        /// primaire sous prétexte que les textes y sont courts.
        /// </summary>
        private const int LongueurMinimale = 40;

        public async Task<string?> TexteTapeAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var messages = await MessagesAsync(conversationId, eleveId, ct);
            if (messages.Count == 0) return null;

            // ON PART DU DERNIER CHOIX « au clavier » : une séance peut porter
            // deux textes, et le premier n'est pas celui qu'on archive.
            var depuis = -1;

            for (var i = 0; i < messages.Count; i++)
            {
                if (messages[i].DeLEleve && AuClavier.IsMatch(messages[i].Contenu)) depuis = i;

                // Sur cahier, il n'y a rien à relire : on annule ce qu'on avait
                // trouvé plus haut plutôt que de rendre le texte d'un exercice
                // précédent.
                if (messages[i].DeLEleve && AuCahier.IsMatch(messages[i].Contenu)) depuis = -1;
            }

            if (depuis < 0) return null;

            // MÊME BORNE QUE POUR LE FILET. Un texte abandonné puis archivé plus
            // tard dans la séance irait sinon chercher, comme « sa copie », le
            // plus long message écrit depuis — celui d'un tout autre exercice.
            return PlusLongMessageEleve(
                messages, depuis + 1, Borne(messages, depuis + 1, messages.Count));
        }

        public async Task<IEnumerable<ExpressionEcriteReconstituee>> GetNonArchiveesAsync(
            int conversationId, int eleveId, CancellationToken ct = default)
        {
            var messages = await MessagesAsync(conversationId, eleveId, ct);
            if (messages.Count == 0) return [];

            // LA SÉANCE EN COURS, ET ELLE SEULE — même borne que pour les
            // conversations, et posée ici AVANT d'avoir eu le défaut : une
            // conversation de matière vit des mois, et un texte abandonné la
            // semaine dernière se serait reconstitué à chaque fin de séance.
            var depuis = FenetreExercice.DebutDeSeance(
                messages, m => !m.DeLEleve, m => m.Contenu);

            if (depuis > 0) messages = messages.Skip(depuis).ToList();

            // Et ce qui est déjà rangé pour cette séance : l'horodatage
            // d'ouverture ne dépend de personne, il est le même à chaque passe.
            var connues = (await _context.ExpressionsEcrites
                .AsNoTracking()
                .Where(e => e.ConversationId == conversationId && e.EleveId == eleveId)
                .Select(e => e.DateCreation)
                .ToListAsync(ct)).ToHashSet();

            // Les bornes des exercices : chaque [SUPPORT_ECRIT] en ouvre un, et
            // le suivant ferme le précédent.
            var debuts = new List<int>();

            for (var i = 0; i < messages.Count; i++)
            {
                if (!messages[i].DeLEleve && Ouverture.IsMatch(messages[i].Contenu)) debuts.Add(i);
            }

            var retrouvees = new List<ExpressionEcriteReconstituee>();

            for (var n = 0; n < debuts.Count; n++)
            {
                var debut = debuts[n];

                // LA FENÊTRE SE REFERME DÈS QU'AUTRE CHOSE S'OUVRE, et pas
                // seulement au texte suivant — Camara, le 18/09/2026 : « vérifie
                // si on quitte de manière inattendue pendant une expression
                // écrite, il se passe quoi ? »
                //
                // CE QUI SE SERAIT PASSÉ SANS CETTE BORNE. Un exercice abandonné
                // laissait sa fenêtre ouverte jusqu'au dernier message de la
                // séance. `PlusLongMessageEleve` y cherchant le plus long
                // message, la COPIE D'UNE DICTÉE faite dix minutes plus tard s'y
                // serait rangée comme « son texte », sous une consigne qui
                // n'était pas la sienne. Exactement le défaut que la carte des
                // vitesses venait de montrer à l'écran, au même endroit du
                // raisonnement.
                var fin = Borne(messages, debut + 1, n + 1 < debuts.Count ? debuts[n + 1] : messages.Count);

                // LE PROFESSEUR L'A ARCHIVÉ LUI-MÊME : on ne double pas.
                var dejaArchive = false;
                var surCahier = false;
                var choix = -1;

                for (var i = debut; i < fin; i++)
                {
                    if (!messages[i].DeLEleve
                        && messages[i].Contenu.Contains(
                            "[EXPRESSION_ECRITE]", StringComparison.OrdinalIgnoreCase))
                    {
                        dejaArchive = true;
                    }

                    if (messages[i].DeLEleve && AuCahier.IsMatch(messages[i].Contenu))
                    {
                        surCahier = true;
                        if (choix < 0) choix = i;
                    }

                    if (messages[i].DeLEleve && AuClavier.IsMatch(messages[i].Contenu) && choix < 0)
                    {
                        choix = i;
                    }
                }

                if (dejaArchive || choix < 0) continue;
                if (connues.Contains(messages[debut].DateCreation)) continue;

                connues.Add(messages[debut].DateCreation);

                // LA CONSIGNE EST LE PREMIER MESSAGE DU PROFESSEUR APRÈS LE
                // CHOIX, et la consigne du prompt le garantit : « tu t'arrêtes
                // là, la consigne vient APRÈS son choix, pas dans le même
                // message ».
                var consigne = PremierMessageProfesseur(messages, choix + 1, fin);
                if (consigne is null) continue;

                if (surCahier)
                {
                    // SUR CAHIER, ON N'INVENTE TOUJOURS PAS LE TEXTE — mais on
                    // SAUVE LA PHOTO. Voulu par Camara le 18/09/2026 : « comme ça
                    // on perdra rien et le prof pourra quand même refaire la
                    // transcription si elle a pas été faite. »
                    //
                    // CE QUE ÇA CHANGE : la photo appartient à la conversation,
                    // dont les octets s'effacent au bout de quelques jours. En la
                    // recopiant dans l'archive, on la sort de ce compte à rebours
                    // le temps qu'elle soit lue — et le texte de l'enfant cesse
                    // de dépendre du fait que le professeur ait pensé à
                    // l'archiver avant de partir.
                    var photo = await PhotoDuCahierAsync(messages, choix, fin, ct);
                    if (photo is null) continue;

                    retrouvees.Add(new ExpressionEcriteReconstituee(
                        LangueTrouvee(messages, debut, fin),
                        consigne,
                        Texte: null,
                        messages[debut].DateCreation,
                        photo.Value.Donnees,
                        photo.Value.TypeMime));

                    continue;
                }

                var texte = PlusLongMessageEleve(messages, choix + 1, fin);
                if (texte is null) continue;

                retrouvees.Add(new ExpressionEcriteReconstituee(
                    LangueTrouvee(messages, debut, fin),
                    consigne,
                    texte,
                    messages[debut].DateCreation));
            }

            return retrouvees;
        }

        /// <summary>
        /// Où s'arrête vraiment l'exercice ouvert en <paramref name="depuis"/> :
        /// au premier message du professeur qui ouvre autre chose, ou à
        /// <paramref name="maximum"/> si rien ne l'interrompt.
        ///
        /// LE MESSAGE QUI LA FERME EST EXCLU, contrairement à la fenêtre d'une
        /// conversation : là-bas il porte souvent la dernière réplique de
        /// l'échange, ici il appartient déjà à l'exercice suivant.
        /// </summary>
        private static int Borne(IReadOnlyList<MessageLu> messages, int depuis, int maximum)
        {
            for (var i = depuis; i < maximum; i++)
            {
                if (messages[i].DeLEleve) continue;

                if (FenetreExercice.OuvreAutreChose(
                    messages[i].Contenu, FenetreExercice.TexteEcrit))
                {
                    return i;
                }
            }

            return maximum;
        }

        /// <summary>
        /// LA PHOTO DE SA PAGE, cherchée parmi les messages de l'élève après
        /// qu'il a choisi le cahier.
        ///
        /// LA PREMIÈRE IMAGE, PAS LA PLUS GROSSE NI LA DERNIÈRE. Un enfant qui
        /// envoie deux photos envoie le recto puis le verso : c'est le début de
        /// son texte qu'on veut sauver en priorité, et le professeur voit de
        /// toute façon les deux dans la séance.
        ///
        /// LES OCTETS PEUVENT DÉJÀ AVOIR ÉTÉ PURGÉS — la pièce jointe a son
        /// propre compte à rebours. On rend alors null plutôt qu'une ligne
        /// vide : une archive sans texte ET sans photo ne dirait rien à
        /// personne.
        /// </summary>
        private async Task<(byte[] Donnees, string TypeMime)?> PhotoDuCahierAsync(
            IReadOnlyList<MessageLu> messages, int depuis, int fin, CancellationToken ct)
        {
            var ids = new List<int>();

            for (var i = depuis; i < fin; i++)
            {
                if (messages[i].DeLEleve) ids.Add(messages[i].Id);
            }

            if (ids.Count == 0) return null;

            var piece = await _context.PiecesJointes
                .AsNoTracking()
                .Where(p => p.MessageId != null
                            && ids.Contains(p.MessageId.Value)
                            && p.DonneesEffaceesLe == null
                            && p.TypeMime != null
                            && p.TypeMime.StartsWith("image/"))
                .OrderBy(p => p.Id)
                .Select(p => new { p.Donnees, p.TypeMime })
                .FirstOrDefaultAsync(ct);

            if (piece?.Donnees is not { Length: > 0 }) return null;

            return (piece.Donnees, piece.TypeMime ?? "image/jpeg");
        }

        /// <summary>Les balises de langue, pour retrouver dans laquelle on écrivait.</summary>
        private static readonly Regex Langue =
            new(@"\[(EN|FR|ES|DE|IT|ZH)\]",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// La langue de l'exercice, si le professeur a parlé dans la langue
        /// pendant la fenêtre.
        ///
        /// SOUVENT VIDE, ET CE N'EST PAS UNE PANNE : une expression écrite se
        /// donne très bien en français — « Raconte ta journée d'hier en cinq
        /// phrases » — sans que rien ne soit prononcé dans la langue cible.
        /// C'est l'appelant qui complète alors avec la langue de la MATIÈRE,
        /// qu'il connaît et que cette couche ignore.
        /// </summary>
        private static string LangueTrouvee(IReadOnlyList<MessageLu> messages, int debut, int fin)
        {
            for (var i = debut; i < fin; i++)
            {
                if (messages[i].DeLEleve) continue;

                var trouvee = Langue.Match(messages[i].Contenu);
                if (trouvee.Success) return trouvee.Groups[1].Value.ToLowerInvariant();
            }

            return string.Empty;
        }

        /// <summary>
        /// LE PLUS LONG MESSAGE DE L'ÉLÈVE DE LA FENÊTRE, et c'est lui son texte.
        ///
        /// POURQUOI LE PLUS LONG PLUTÔT QUE LE PREMIER : entre la consigne et sa
        /// copie, un enfant écrit « ok », « j'ai pas compris », « je peux le
        /// faire en 4 phrases ? ». Le premier message n'est presque jamais le
        /// texte ; le plus long l'est toujours, parce qu'aucun aparté de séance
        /// ne fait la longueur d'une rédaction.
        /// </summary>
        private static string? PlusLongMessageEleve(
            IReadOnlyList<MessageLu> messages, int debut, int fin)
        {
            string? meilleur = null;

            for (var i = debut; i < fin; i++)
            {
                if (!messages[i].DeLEleve) continue;

                var dit = FenetreExercice.SansCrochets(messages[i].Contenu);

                if (dit.Length < LongueurMinimale) continue;
                if (meilleur is null || dit.Length > meilleur.Length) meilleur = dit;
            }

            return meilleur;
        }

        private static string? PremierMessageProfesseur(
            IReadOnlyList<MessageLu> messages, int debut, int fin)
        {
            for (var i = debut; i < fin; i++)
            {
                if (messages[i].DeLEleve) continue;

                var dit = FenetreExercice.SansCrochets(messages[i].Contenu);
                if (dit.Length > 0) return Borner(dit, 1000);
            }

            return null;
        }

        /// <summary>
        /// L'IDENTIFIANT EST LÀ POUR LA PHOTO, et pour elle seule : c'est par
        /// lui qu'on retrouve la pièce jointe accrochée au message.
        /// </summary>
        private sealed record MessageLu(
            int Id, bool DeLEleve, string Contenu, DateTime DateCreation);

        /// <summary>
        /// Les derniers messages de la séance, dans l'ordre — et seulement si
        /// elle appartient bien à cet élève.
        ///
        /// Une soixantaine couvre largement une séance et ses quelques
        /// exercices ; une conversation vit des mois, on ne la relit pas
        /// entière.
        /// </summary>
        private async Task<List<MessageLu>> MessagesAsync(
            int conversationId, int eleveId, CancellationToken ct)
        {
            var appartient = await _context.Conversations
                .AsNoTracking()
                .AnyAsync(c => c.Id == conversationId && c.EleveId == eleveId, ct);

            if (!appartient) return [];

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Id)
                .Take(60)
                .Select(m => new { m.Id, m.Role, m.Contenu, m.DateCreation })
                .ToListAsync(ct);

            messages.Reverse();

            return messages
                .Select(m => new MessageLu(
                    m.Id,
                    !string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase),
                    m.Contenu ?? string.Empty,
                    m.DateCreation))
                .ToList();
        }

        /// <summary>
        /// La correction, relue depuis sa colonne.
        ///
        /// UN JSON ILLISIBLE N'EST PAS UNE PANNE : il vient d'un modèle, donc il
        /// peut être malformé. Le texte de l'élève, lui, est dans sa propre
        /// colonne — il reste lisible même si la correction se perd. Dégradé,
        /// jamais cassé.
        /// </summary>
        private static List<RepriseEcrite> Lire(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];

            try
            {
                return JsonSerializer.Deserialize<List<RepriseEcrite>>(json, Json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static int CompterMots(string? texte) =>
            string.IsNullOrWhiteSpace(texte)
                ? 0
                : texte.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

        private static string Borner(string texte, int maximum)
        {
            var propre = texte.Trim();
            return propre.Length <= maximum ? propre : propre[..maximum];
        }
    }
}
