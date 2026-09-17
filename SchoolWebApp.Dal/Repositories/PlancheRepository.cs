using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using DomainPlanche = SchoolWebApp.Domain.Models.Planche;

namespace SchoolWebApp.Dal.Repositories
{
    public class PlancheRepository : IPlancheRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public PlancheRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// LES PLANCHES LÉGENDÉES SEULES — le défaut de presque tout ce fichier.
        ///
        /// Depuis qu'une planche peut avoir une variante muette, chaque requête
        /// écrite avant le 17/09/2026 en verrait deux là où elle en voyait une.
        /// Le danger n'est pas l'affichage, c'est les FILES : une muette n'a
        /// aucune légende à lire, donc rien à extraire, donc elle resterait
        /// éternellement « à décrire » et « à cartographier », à se faire relire
        /// par un modèle de vision qui répondrait à chaque fois la même chose.
        ///
        /// Passer par cette propriété plutôt que par le DbSet est donc la règle.
        /// Les exceptions sont nommées une par une, et il n'y en a que quatre :
        /// servir une planche, la lister pour l'administration, l'importer, la
        /// supprimer — plus les crédits, qui sont dus sur les deux images.
        /// </summary>
        private IQueryable<PlancheSchema> Legendees =>
            _context.PlanchesSchemas.Where(p => p.Variante == VariantePlanche.Legende);

        /// <summary>
        /// La projection est explicite et n'inclut PAS les octets : sans elle,
        /// EF chargerait chaque varbinary pour le jeter aussitôt, et afficher
        /// une liste de trente cases à cocher ferait transiter cent mégaoctets.
        /// </summary>
        /// <summary>
        /// Les trois files, avec les mêmes conditions EXACTES que le worker.
        ///
        /// Copiées et non partagées, c'est le défaut de cette méthode : si l'un
        /// des filtres change là-bas sans changer ici, l'écran montrera une file
        /// vide pendant que le worker travaille. À relire ensemble.
        /// </summary>
        public async Task<FilesPlanches> GetFilesAsync(CancellationToken ct = default)
        {
            var toutes = await _context.PlanchesSchemas
                .AsNoTracking()
                .Where(p => p.Contenu == null
                         || p.Reperes == null
                         || (!p.Maison && p.Auteur == null && p.Licence == null && p.Source == null))
                .OrderByDescending(p => p.DateCreation)
                // LES OCTETS RESTENT EN BASE. Une file de soixante planches
                // ferait passer plusieurs dizaines de mégaoctets pour afficher
                // soixante noms de fichiers.
                .Select(p => new LigneFile(
                    p.Cle, p.MatiereCode, p.Taille, p.DateCreation,
                    p.Contenu, p.Reperes, p.Maison, p.Auteur, p.Licence, p.Source,
                    p.Variante))
                .ToListAsync(ct);

            static PlancheEnAttente Ligne(LigneFile p) => new()
            {
                Cle = p.Cle,
                MatiereCode = p.MatiereCode,
                Taille = p.Taille,
                DateCreation = p.DateCreation,
            };

            // LES MUETTES NE SONT NI DÉCRITES NI CARTOGRAPHIÉES, MAIS ELLES SONT
            // CRÉDITÉES.
            //
            // Elles n'ont aucun mot à lire : les deux premières files les
            // garderaient indéfiniment. Le crédit, lui, est dû — une muette est
            // un fichier de Wikimedia comme un autre, affiché devant un enfant.
            static bool Legendee(LigneFile p) => p.Variante == VariantePlanche.Legende;

            return new FilesPlanches
            {
                ADecrire = toutes
                    .Where(p => Legendee(p) && p.Contenu == null)
                    .Select(Ligne).ToList(),

                ACartographier = toutes
                    .Where(p => Legendee(p)
                             && p.Contenu != null
                             && p.Reperes == null
                             && !p.Contenu.StartsWith("SANS AUCUN NOM")
                             && !p.Contenu.StartsWith("aucune légende lisible")
                             && !p.Contenu.StartsWith("LANGUE ÉTRANGÈRE"))
                    .Select(Ligne).ToList(),

                ACrediter = toutes
                    .Where(p => !p.Maison && p.Auteur == null && p.Licence == null && p.Source == null)
                    .Select(Ligne).ToList(),
            };
        }

        /// <summary>
        /// Le strict nécessaire pour trier les trois files, sans les octets.
        ///
        /// Un type nommé plutôt qu'anonyme : le tri se fait en mémoire, dans une
        /// méthode locale, et un type anonyme l'aurait obligée à passer par
        /// <c>dynamic</c> — donc à perdre le contrôle du compilateur.
        /// </summary>
        private sealed record LigneFile(
            string Cle, string? MatiereCode, int Taille, DateTime DateCreation,
            string? Contenu, string? Reperes, bool Maison,
            string? Auteur, string? Licence, string? Source,
            string Variante);

        public async Task<int> ViderLesFilesAsync(CancellationToken ct = default)
        {
            // ON ÉCRIT UN CONSTAT, ON N'INVENTE RIEN.
            //
            // Ces valeurs sortent les planches des files ET disent pourquoi.
            // Un texte vide les y aurait laissées ; un faux contenu aurait menti
            // au professeur, qui interroge l'élève d'après ce qu'il lit là.
            var decrites = await Legendees
                .Where(p => p.Contenu == null)
                .ExecuteUpdateAsync(m => m.SetProperty(
                    p => p.Contenu, "aucune légende lisible (traitement annulé par l'administrateur)"), ct);

            var reperees = await Legendees
                .Where(p => p.Reperes == null)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.Reperes, "[]"), ct);

            var creditees = await _context.PlanchesSchemas
                .Where(p => !p.Maison && p.Auteur == null && p.Licence == null && p.Source == null)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.Source, "origine non recherchée"), ct);

            return decrites + reperees + creditees;
        }

        public async Task<IEnumerable<DomainPlanche>> GetToutesAsync(CancellationToken ct = default) =>
            await _context.PlanchesSchemas
                .AsNoTracking()
                .OrderBy(p => p.MatiereCode).ThenBy(p => p.Cle)
                .Select(p => new DomainPlanche
                {
                    Id = p.Id,
                    Cle = p.Cle,
                    MatiereCode = p.MatiereCode,

                    // L'ADMINISTRATION VOIT LES DEUX : c'est le seul écran où une
                    // muette doit apparaître, sous sa légendée.
                    Variante = p.Variante,

                    Niveau = p.Niveau,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                    Auteur = p.Auteur,
                    Source = p.Source,
                    Licence = p.Licence,
                    Maison = p.Maison,
                    Reperes = p.Reperes,
                    Contenu = p.Contenu,
                    DateCreation = p.DateCreation,
                    DateModification = p.DateModification,
                })
                .ToListAsync(ct);

        public async Task<DomainPlanche?> GetAsync(
            string cle, string? variante = null, CancellationToken ct = default)
        {
            var v = VariantePlanche.Normaliser(variante);

            var entity = await _context.PlanchesSchemas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Cle == cle && p.Variante == v, ct);

            return entity is null ? null : Map(entity, avecDonnees: true);
        }

        public Task<DomainPlanche?> GetSansDonneesAsync(
            string cle, string? variante = null, CancellationToken ct = default)
        {
            var v = VariantePlanche.Normaliser(variante);

            return _context.PlanchesSchemas
                .AsNoTracking()
                .Where(p => p.Cle == cle && p.Variante == v)
                .Select(p => new DomainPlanche
                {
                    Id = p.Id,
                    Cle = p.Cle,
                    MatiereCode = p.MatiereCode,
                    Variante = p.Variante,
                    Niveau = p.Niveau,

                    // LE NOM, LE TYPE ET LA TAILLE EN FONT PARTIE AUSSI.
                    //
                    // Ils manquaient. Trois champs de quelques octets, absents
                    // d une méthode qui ne promet d écarter que l IMAGE :
                    // n importe quel appelant les croit remplis, et affiche du
                    // vide sans qu une erreur ne se produise nulle part.
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,

                    Auteur = p.Auteur,
                    Source = p.Source,
                    Licence = p.Licence,
                    Maison = p.Maison,
                    Reperes = p.Reperes,

                    // LE RELEVÉ DES LÉGENDES EN FAIT PARTIE, ET C'EST TOUT
                    // L'INTÉRÊT DE CETTE MÉTHODE.
                    //
                    // « Sans données » veut dire sans les OCTETS de l'image —
                    // plusieurs mégaoctets qu'on ne veut pas charger pour lire
                    // trois chaînes. Le contenu, lui, est justement ce qu'on
                    // vient chercher quand on ne veut pas l'image : c'est le
                    // texte que le professeur reçoit pour savoir ce que la
                    // figure porte.
                    //
                    // Son absence ici a coûté une soirée : le rappel du tour
                    // courant lisait une description toujours nulle et se
                    // taisait, pendant qu'on cherchait pourquoi le professeur
                    // ne voyait pas les légendes.
                    Contenu = p.Contenu,

                    DateCreation = p.DateCreation,
                    DateModification = p.DateModification,
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<DomainPlanche> ImporterAsync(
            DomainPlanche planche, CancellationToken ct = default)
        {
            var variante = VariantePlanche.Normaliser(planche.Variante);

            var entity = await _context.PlanchesSchemas
                .FirstOrDefaultAsync(
                    p => p.Cle == planche.Cle && p.Variante == variante, ct);

            // LES MÊMES OCTETS QU'AVANT ? ALORS RIEN N'A CHANGÉ.
            //
            // Comparé AVANT toute écriture, sinon on compare la planche à
            // elle-même. C'est ce test qui rend le ré-import gratuit.
            //
            // CE QU'IL EMPÊCHE, ET ÇA A COÛTÉ CHER. Un agent d'import
            // programmé toutes les quatre minutes rappelait cette méthode avec
            // les mêmes fichiers. Chaque passage effaçait la description,
            // remettait la planche dans la file et sonnait le worker, qui la
            // relisait — deux appels au modèle par planche, toutes les quatre
            // minutes, indéfiniment. Trois nuits à chercher dans le worker un
            // défaut qui était dans cette ligne-ci.
            var memesOctets = entity is not null
                && entity.Donnees is { Length: > 0 }
                && planche.Donnees is { Length: > 0 }
                && entity.Donnees.AsSpan().SequenceEqual(planche.Donnees);

            if (entity is null)
            {
                entity = new PlancheSchema
                {
                    Cle = planche.Cle,
                    Variante = variante,
                    DateCreation = DateTime.UtcNow,
                };
                _context.PlanchesSchemas.Add(entity);
            }
            else if (!memesOctets)
            {
                // La date de modification ne bouge QUE si quelque chose a
                // changé : sinon l'administration afficherait une planche
                // « modifiée » toutes les quatre minutes sans qu'elle le soit.
                entity.DateModification = DateTime.UtcNow;
            }

            entity.MatiereCode = planche.MatiereCode;
            entity.Niveau = planche.Niveau;
            entity.NomFichier = planche.NomFichier;
            entity.TypeMime = planche.TypeMime;
            entity.Taille = planche.Taille;
            entity.Donnees = planche.Donnees ?? Array.Empty<byte>();
            entity.Auteur = planche.Auteur;
            entity.Source = planche.Source;
            entity.Licence = planche.Licence;
            entity.Maison = planche.Maison;

            // LA DESCRIPTION EST REMISE À ZÉRO QUAND LES OCTETS CHANGENT.
            //
            // Elle décrit les octets, pas la clé. Remplacer la planche sans
            // effacer sa description laisserait le professeur annoncer les
            // légendes de l'ancienne — le pire des deux mondes, puisqu'il
            // croirait savoir. L'extraction repassera dessus.
            //
            // Mais à octets IDENTIQUES, l'effacer serait payer deux lectures
            // d'image pour retrouver mot pour mot ce qu'on avait déjà.
            if (!memesOctets)
            {
                entity.Contenu = null;

                // LES REPÈRES AUSSI, et ils étaient oubliés. Ce sont des
                // POSITIONS sur l'image : gardés sur une figure remplacée, les
                // flèches du tableau désignent le vide, avec l'aplomb de
                // coordonnées écrites en base.
                entity.Reperes = null;
            }

            await _context.SaveChangesAsync(ct);

            return Map(entity, avecDonnees: false);
        }

        /// <summary>
        /// RETIRER LA LÉGENDÉE EMPORTE SA MUETTE, et il le faut.
        ///
        /// La muette n'a ni description ni carte de repères à elle : elle lit
        /// celles de sa légendée. Seule, elle deviendrait une image sur laquelle
        /// l'élève clique sans que personne ne puisse dire ce qu'il a montré.
        ///
        /// Retirer la muette, à l'inverse, ne touche pas la légendée : on renonce
        /// à interroger, on continue à enseigner.
        /// </summary>
        public async Task<bool> SupprimerAsync(
            string cle, string? variante = null, CancellationToken ct = default)
        {
            var v = VariantePlanche.Normaliser(variante);

            var effacees = v == VariantePlanche.Legende
                ? await _context.PlanchesSchemas
                    .Where(p => p.Cle == cle)
                    .ExecuteDeleteAsync(ct)
                : await _context.PlanchesSchemas
                    .Where(p => p.Cle == cle && p.Variante == v)
                    .ExecuteDeleteAsync(ct);

            return effacees > 0;
        }

        /// <summary>
        /// Les planches écartées pour cause de langue, sans leurs octets.
        ///
        /// SANS `avecDonnees` : re-juger une langue se fait sur les LÉGENDES
        /// déjà relevées, pas sur l image. Charger les octets ferait transiter
        /// plusieurs mégaoctets pour relire trois lignes de texte.
        /// </summary>
        public async Task<IEnumerable<DomainPlanche>> GetMarqueesEtrangeresAsync(
            CancellationToken ct = default)
        {
            var entities = await Legendees
                .AsNoTracking()
                .Where(p => p.Contenu != null && p.Contenu.StartsWith("LANGUE ÉTRANGÈRE"))
                .OrderBy(p => p.MatiereCode).ThenBy(p => p.Cle)
                .ToListAsync(ct);

            return entities.Select(e => Map(e, avecDonnees: false)).ToList();
        }

        public async Task<IEnumerable<DomainPlanche>> GetADecrireAsync(
            int limite, CancellationToken ct = default)
        {
            var entities = await Legendees
                .AsNoTracking()
                .Where(p => p.Contenu == null)
                // LA PLUS RÉCENTE D ABORD.
                //
                // On traitait du plus ancien au plus récent : la planche qu on
                // vient d importer passait derrière tout l arriéré, et c est
                // pourtant celle qu on attend de voir apparaître.
                .OrderByDescending(p => p.DateCreation)
                .Take(limite)
                .ToListAsync(ct);

            return entities.Select(e => Map(e, avecDonnees: true)).ToList();
        }

        /// <summary>
        /// ExecuteUpdate plutôt qu'un chargement suivi d'un SaveChanges : la
        /// ligne porte un varbinary de plusieurs mégaoctets, et EF le
        /// ramènerait en mémoire pour écrire une colonne de texte.
        /// </summary>
        public Task EnregistrerContenuAsync(int id, string contenu, CancellationToken ct = default) =>
            _context.PlanchesSchemas
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.Contenu, contenu), ct);

        public async Task<IEnumerable<DomainPlanche>> GetSansCreditAsync(
            int limite, CancellationToken ct = default)
        {
            var entities = await _context.PlanchesSchemas
                .AsNoTracking()
                // `!p.Maison` EST LA CONDITION QUI COMPTE ICI.
                //
                // Sans elle, une illustration produite par nous — donc sans
                // auteur tiers, légitimement — tombe dans ce lot, et le worker
                // lui invente un crédit Wikimedia. On afficherait alors sous
                // les yeux d'un enfant une attribution fausse, écrite par nous.
                //
                // Un crédit vide veut dire deux choses opposées ; c'est ce
                // drapeau qui les sépare.
                .Where(p => !p.Maison
                         && p.Auteur == null && p.Licence == null && p.Source == null)
                // LA PLUS RÉCENTE D ABORD.
                //
                // On traitait du plus ancien au plus récent : la planche qu on
                // vient d importer passait derrière tout l arriéré, et c est
                // pourtant celle qu on attend de voir apparaître.
                .OrderByDescending(p => p.DateCreation)
                .Take(limite)
                .ToListAsync(ct);

            return entities.Select(e => Map(e, avecDonnees: true)).ToList();
        }

        public async Task<IEnumerable<DomainPlanche>> GetSansReperesAsync(
            int limite, CancellationToken ct = default)
        {
            var entities = await _context.PlanchesSchemas
                .AsNoTracking()
                // DÉCRITE D'ABORD, REPÉRÉE ENSUITE. Une planche dont on ne sait
                // pas encore ce qu'elle porte n'a pas de raison qu'on en cherche
                // les positions : les deux lectures se paient, autant ne pas
                // faire la seconde sur une figure qu'on remplacera peut-être.
                //
                // Et rien à repérer sur une planche sans le moindre mot écrit.
                // TROIS FAÇONS DE N'AVOIR RIEN À CARTOGRAPHIER.
                //
                // « SANS AUCUN NOM » et « aucune légende lisible » désignent des
                // figures muettes : carte à légender, silhouette, fond de carte.
                // Y envoyer un modèle de vision coûte un appel pour s'entendre
                // répondre `[]` — sept planches sur cinquante-neuf, mesuré.
                //
                // « LANGUE ÉTRANGÈRE » marque celles que le professeur n'apprend
                // jamais : elles ne s'afficheront pas, l'élève ne cliquera donc
                // jamais dessus.
                .Where(p => p.Variante == VariantePlanche.Legende
                         && p.Contenu != null
                         && p.Reperes == null
                         && !p.Contenu.StartsWith("SANS AUCUN NOM")
                         && !p.Contenu.StartsWith("aucune légende lisible")
                         && !p.Contenu.StartsWith("LANGUE ÉTRANGÈRE"))
                // LA PLUS RÉCENTE D ABORD.
                //
                // On traitait du plus ancien au plus récent : la planche qu on
                // vient d importer passait derrière tout l arriéré, et c est
                // pourtant celle qu on attend de voir apparaître.
                .OrderByDescending(p => p.DateCreation)
                .Take(limite)
                .ToListAsync(ct);

            return entities.Select(e => Map(e, avecDonnees: true)).ToList();
        }

        public Task EnregistrerReperesAsync(
            int id, string reperes, CancellationToken ct = default) =>
            _context.PlanchesSchemas
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.Reperes, reperes), ct);

        public Task EnregistrerCreditAsync(
            int id, string? auteur, string? source, string? licence,
            CancellationToken ct = default) =>
            _context.PlanchesSchemas
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(p => p.Auteur, auteur)
                    .SetProperty(p => p.Source, source)
                    .SetProperty(p => p.Licence, licence), ct);

        private static DomainPlanche Map(PlancheSchema e, bool avecDonnees) => new()
        {
            Id = e.Id,
            Cle = e.Cle,
            MatiereCode = e.MatiereCode,
            Variante = e.Variante,
            Niveau = e.Niveau,
            NomFichier = e.NomFichier,
            TypeMime = e.TypeMime,
            Taille = e.Taille,
            Donnees = avecDonnees ? e.Donnees : null,
            Auteur = e.Auteur,
            Source = e.Source,
            Licence = e.Licence,
            Maison = e.Maison,
            Reperes = e.Reperes,
            Contenu = e.Contenu,
            DateCreation = e.DateCreation,
            DateModification = e.DateModification,
        };
    }
}
