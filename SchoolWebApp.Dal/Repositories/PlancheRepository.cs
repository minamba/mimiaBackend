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
                    p.Contenu, p.Reperes, p.Maison, p.Auteur, p.Licence, p.Source))
                .ToListAsync(ct);

            static PlancheEnAttente Ligne(LigneFile p) => new()
            {
                Cle = p.Cle,
                MatiereCode = p.MatiereCode,
                Taille = p.Taille,
                DateCreation = p.DateCreation,
            };

            return new FilesPlanches
            {
                ADecrire = toutes
                    .Where(p => p.Contenu == null)
                    .Select(Ligne).ToList(),

                ACartographier = toutes
                    .Where(p => p.Contenu != null
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
            string? Auteur, string? Licence, string? Source);

        public async Task<int> ViderLesFilesAsync(CancellationToken ct = default)
        {
            // ON ÉCRIT UN CONSTAT, ON N'INVENTE RIEN.
            //
            // Ces valeurs sortent les planches des files ET disent pourquoi.
            // Un texte vide les y aurait laissées ; un faux contenu aurait menti
            // au professeur, qui interroge l'élève d'après ce qu'il lit là.
            var decrites = await _context.PlanchesSchemas
                .Where(p => p.Contenu == null)
                .ExecuteUpdateAsync(m => m.SetProperty(
                    p => p.Contenu, "aucune légende lisible (traitement annulé par l'administrateur)"), ct);

            var reperees = await _context.PlanchesSchemas
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

        public async Task<DomainPlanche?> GetAsync(string cle, CancellationToken ct = default)
        {
            var entity = await _context.PlanchesSchemas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Cle == cle, ct);

            return entity is null ? null : Map(entity, avecDonnees: true);
        }

        public Task<DomainPlanche?> GetSansDonneesAsync(string cle, CancellationToken ct = default) =>
            _context.PlanchesSchemas
                .AsNoTracking()
                .Where(p => p.Cle == cle)
                .Select(p => new DomainPlanche
                {
                    Id = p.Id,
                    Cle = p.Cle,
                    MatiereCode = p.MatiereCode,

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

        public async Task<DomainPlanche> ImporterAsync(
            DomainPlanche planche, CancellationToken ct = default)
        {
            var entity = await _context.PlanchesSchemas
                .FirstOrDefaultAsync(p => p.Cle == planche.Cle, ct);

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
                entity = new PlancheSchema { Cle = planche.Cle, DateCreation = DateTime.UtcNow };
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

        public async Task<bool> SupprimerAsync(string cle, CancellationToken ct = default)
        {
            var effacees = await _context.PlanchesSchemas
                .Where(p => p.Cle == cle)
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
            var entities = await _context.PlanchesSchemas
                .AsNoTracking()
                .Where(p => p.Contenu != null && p.Contenu.StartsWith("LANGUE ÉTRANGÈRE"))
                .OrderBy(p => p.MatiereCode).ThenBy(p => p.Cle)
                .ToListAsync(ct);

            return entities.Select(e => Map(e, avecDonnees: false)).ToList();
        }

        public async Task<IEnumerable<DomainPlanche>> GetADecrireAsync(
            int limite, CancellationToken ct = default)
        {
            var entities = await _context.PlanchesSchemas
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
                .Where(p => p.Contenu != null
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
