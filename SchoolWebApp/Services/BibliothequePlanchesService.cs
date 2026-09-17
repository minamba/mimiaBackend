using Microsoft.Extensions.Caching.Memory;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Dit au professeur ce que contiennent RÉELLEMENT les planches importées
    /// de sa matière.
    ///
    /// LE PROBLÈME QU'IL RÉSOUT
    /// -----------------------
    /// Le catalogue du prompt annonce un titre — « Appareil respiratoire » — et
    /// c'est tout ce que le professeur sait de la figure. Tant qu'elle était
    /// dessinée par lui, ça suffisait : il connaissait son propre dessin. Depuis
    /// qu'on importe des planches, ce n'est plus vrai. Le titre est le même, la
    /// figure est une autre : une planche de manuel porte souvent des légendes
    /// que le dessin n'avait pas, et parfois pas celles qu'il avait.
    ///
    /// Le professeur affiche donc une image qu'il n'a jamais vue, et interroge
    /// dessus au jugé — il demande où va l'air après les bronches sur une figure
    /// qui s'arrête là, et il passe à côté de l'encart alvéolaire.
    ///
    /// POURQUOI CE BLOC EST MIS EN CACHE MÉMOIRE
    /// ----------------------------------------
    /// Il entre dans le préfixe du prompt, relu à chaque tour de chaque séance.
    /// Interroger la base à chaque message pour un texte qui ne bouge qu'à
    /// l'import serait une requête par phrase prononcée.
    /// </summary>
    public interface IBibliothequePlanchesService
    {
        /// <summary>
        /// Le bloc à insérer dans le prompt, ou une chaîne vide si aucune
        /// planche de cette matière n'a encore de description.
        /// </summary>
        Task<string> ConstruireAsync(string? matiereCode, CancellationToken ct = default);

        /// <summary>
        /// Jette le bloc gardé pour une matière : le prochain qui le demande le
        /// reconstruira à partir de la base.
        ///
        /// APPELÉ À CHAQUE ÉCRITURE SUR LES PLANCHES — import, remplacement,
        /// retrait, description fraîchement extraite. Sans ça, réveiller le
        /// worker ne servirait à rien : la description serait en base dans la
        /// seconde, et le professeur continuerait de recevoir pendant cinq
        /// minutes un bloc construit avant qu'elle existe.
        /// </summary>
        void Oublier(string? matiereCode);
    }

    public class BibliothequePlanchesService : IBibliothequePlanchesService
    {
        private readonly IPlancheRepository _planches;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Cinq minutes. Une planche importée doit se voir arriver dans les
        /// séances en cours sans redémarrage, mais rien n'exige la seconde :
        /// sa description n'est de toute façon extraite qu'au tour suivant du
        /// worker.
        /// </summary>
        private static readonly TimeSpan Duree = TimeSpan.FromMinutes(5);

        public BibliothequePlanchesService(IPlancheRepository planches, IMemoryCache cache)
        {
            _planches = planches ?? throw new ArgumentNullException(nameof(planches));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<string> ConstruireAsync(
            string? matiereCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(matiereCode)) return string.Empty;

            var code = matiereCode.Trim().ToUpperInvariant();
            var cle = $"planches:bloc:{code}";

            if (_cache.TryGetValue<string>(cle, out var enCache) && enCache is not null)
            {
                return enCache;
            }

            // GetToutesAsync ne charge pas les octets : on lit trente titres,
            // pas trente mégaoctets.
            var toutes = (await _planches.GetToutesAsync(ct)).ToList();

            // LES CLÉS QUI ONT UNE MUETTE, pour l'annoncer sur la ligne de leur
            // légendée. Une muette ne prend JAMAIS de ligne à elle : elle n'a pas
            // de description — rien à lire dessus — et le professeur la verrait
            // comme une planche vide dont il ne saurait quoi faire.
            var avecMuette = toutes
                .Where(p => p.Variante == Domain.Models.VariantePlanche.Muette)
                .Select(p => p.Cle)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var planches = toutes
                .Where(p => p.Variante == Domain.Models.VariantePlanche.Legende)
                // Les siennes, et celles qu'elle emprunte clé par clé à une
                // autre matière — voir `EmpruntsDePlanches`.
                .Where(p => EmpruntsDePlanches.EstServie(p.Cle, p.MatiereCode, code))
                .Where(p => !string.IsNullOrWhiteSpace(p.Contenu))
                // Une planche sans légende lisible est marquée en base pour ne
                // plus repasser au worker. Elle n'a rien à dire au professeur.
                .Where(p => !string.Equals(
                    p.Contenu!.Trim(),
                    Workers.DescriptionPlanchesWorker.AucuneLegende,
                    StringComparison.OrdinalIgnoreCase))
                // Une planche légendée en chinois, en norvégien ou en anglais ne
                // sert à rien à un élève français — elle le met en échec sur des
                // mots qu'il n'a aucune raison de connaître. Elle reste en base
                // pour que l'administration la remplace, mais le professeur ne
                // doit jamais apprendre qu'elle existe.
                .Where(p => !p.Contenu!.TrimStart().StartsWith(
                    Workers.DescriptionPlanchesWorker.LangueEtrangere,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Cle)
                .ToList();

            var bloc = planches.Count == 0
                ? string.Empty
                : Rediger(planches.Select(p =>
                    (p.Cle, p.Niveau, p.Contenu!, avecMuette.Contains(p.Cle))));

            _cache.Set(cle, bloc, Duree);

            return bloc;
        }

        public void Oublier(string? matiereCode)
        {
            if (string.IsNullOrWhiteSpace(matiereCode)) return;

            _cache.Remove($"planches:bloc:{matiereCode.Trim().ToUpperInvariant()}");

            // Une planche de SVT réimportée change aussi le bloc des matières qui
            // l'empruntent : sans ça, la biologie de ST2S garderait cinq minutes
            // l'ancienne liste de légendes. Quatre matières, quatre suppressions.
            foreach (var emprunteuse in EmpruntsDePlanches.ParMatiere.Keys)
            {
                _cache.Remove($"planches:bloc:{emprunteuse.ToUpperInvariant()}");
            }
        }

        /// <summary>
        /// Le bloc est AUTOPORTANT, et ce n'est pas un détail de rédaction.
        ///
        /// Seule la consigne de SVT explique la syntaxe `SCHEMA:` — elle y a été
        /// écrite quand la SVT était la seule matière à avoir des figures. Les
        /// six autres professeurs ne la connaissent pas. Ce bloc doit donc tout
        /// leur apprendre d'un coup : que le mécanisme existe, comment on
        /// l'écrit, et ce que chaque planche contient.
        ///
        /// Il ne liste QUE des planches réellement en base. Un professeur ne
        /// peut donc pas y prendre une clé qui laisserait un tableau vide : ce
        /// qu'il ne voit pas ici n'existe pas pour lui, et il dessine.
        /// </summary>
        private static string Rediger(
            IEnumerable<(string Cle, string? Niveau, string Contenu, bool AMuette)> planches)
        {
            // LE NIVEAU EST ÉCRIT À CÔTÉ DE LA CLÉ, et c'est tout l'intérêt de la
            // colonne : sans lui, le professeur choisissait sur le nom de la clé et
            // les légendes — « droite-graduee » ne dit pas si c'est du CP ou de la
            // 6e. Une planche sans niveau ne porte aucune mention : mieux vaut
            // qu'il n'en sache rien que de lui laisser croire une valeur inventée.
            var lignes = planches.Select(p =>
            {
                var niveau = string.IsNullOrWhiteSpace(p.Niveau) ? "" : $" ({p.Niveau})";

                // LA MUETTE S'ANNONCE SUR LA LIGNE DE SA LÉGENDÉE, parce que
                // c'est la même figure : deux lignes se seraient lues comme
                // deux documents, et le professeur aurait cru devoir choisir.
                var muette = p.AMuette
                    ? $" — VERSION MUETTE : `SCHEMA:{p.Cle}/muette`"
                    : "";

                return $"- `SCHEMA:{p.Cle}`{niveau} — montre : {Abreger(p.Contenu)}{muette}";
            });

            // LA PLANCHE DE SA CLASSE D'ABORD — Camara, le 17/09/2026 : un élève
            // de 6e s'est vu proposer la droite graduée du CP, avec les nombres de
            // 0 à 10.
            //
            // UNE PRIORITÉ, PAS UN FILTRE, et Camara y tient : revenir sur une
            // planche de niveau inférieur pour débloquer un prérequis est une bonne
            // façon d'enseigner. Ce qui est fautif est de le faire SANS LE VOULOIR,
            // en prenant la première planche qui porte le bon mot.
            //
            // LA CONSIGNE DISAIT LITTÉRALEMENT LE CONTRAIRE : « ni le niveau de
            // l'élève, ni le programme du jour ne sont des raisons de dire non ».
            // Elle protégeait d'un vrai défaut — un professeur qui refuse
            // d'afficher quoi que ce soit — mais elle ratissait trop large.
            //
            // CE N'EST QU'UNE CONSIGNE, ET UNE CONSIGNE NE VAUT PAS UN FAIT. Le
            // vrai correctif est de ne PAS ANNONCER les planches d'un autre niveau,
            // ce qui suppose de connaître celui de l'élève : la bibliothèque est
            // construite par matière seulement, et le niveau d'une planche n'existe
            // nulle part en base — il ne vit que dans l'écran d'import. Tant que
            // c'est le cas, cette consigne est tout ce qu'on a.
            return $"""
                ## LES PLANCHES QUE TU PEUX AFFICHER

                Tu disposes de planches déjà prêtes — des documents propres,
                vérifiés, bien plus lisibles que ce que tu dessinerais. Pour en
                afficher une, l'ardoise ne contient QUE sa clé, rien d'autre :

                [ARDOISE]
                SCHEMA:une-cle-de-la-liste
                [/ARDOISE]

                Voici celles qui existent, avec la liste exacte de leurs légendes,
                relevée sur l'image elle-même :

                {string.Join("\n", lignes)}

                **N'écris jamais une clé absente de cette liste.** Elle
                n'afficherait rien du tout, et tu ne le verrais pas. Si le sujet
                n'a pas sa planche ici, dessine comme d'habitude.

                DEUX RÈGLES QUAND TU EN AFFICHES UNE :

                - **Tu n'interroges que sur ce qui est dans sa liste.** Demander
                  « et après les bronches ? » sur une planche qui s'arrête aux
                  bronches met l'élève en échec devant une image muette.
                - **Tu ne laisses pas de côté ce qui y est.** Si la liste porte un
                  encart ou une légende que tu n'avais pas prévu d'aborder, il est
                  sous les yeux de l'élève : nomme-le.

                Ce qui ne figure pas dans la liste n'est PAS sur la planche, même
                si la notion l'exigerait. Ne l'annonce pas, ne le pointe pas.

                **Une carte muette se MONTRE du doigt.** L'élève peut cliquer
                dessus — voir « L'élève peut montrer du doigt » plus haut. C'est
                exactement l'usage de ces planches-là : « montre-moi la région
                qui borde la Méditerranée » vaut mieux que n'importe quelle
                question posée avec des mots.

                **« SANS AUCUN NOM ÉCRIT » veut dire ce que ça dit.** Cette
                planche-là ne porte aucun mot : c'est une carte muette, une
                silhouette à légender. Ne demande jamais de LIRE quoi que ce soit
                dessus — l'élève ne verrait rien et se croirait fautif. Demande de
                MONTRER, de reconnaître à la forme, de nommer de mémoire. C'est
                d'ailleurs un bien meilleur exercice.

                **LE NIVEAU EST ÉCRIT ENTRE PARENTHÈSES** après la clé, quand il est
                connu : `SCHEMA:math-droite-graduee (CP)`. La règle du niveau est
                dans tes consignes générales — « ce que tu montres est au niveau de
                ta classe » — et cette mention est ce qui te permet de
                l'appliquer ici plutôt que de deviner d'après le nom de la clé.

                Une planche SANS mention de niveau ne dit rien de son âge : juges-en
                par ses légendes.

                ## LA VERSION MUETTE, POUR INTERROGER

                Certaines lignes ci-dessus se terminent par **VERSION MUETTE**,
                suivie d'une clé en `/muette`. C'est EXACTEMENT LA MÊME FIGURE,
                au même cadrage, dont on a effacé tous les mots.

                **La légendée sert à enseigner. La muette sert à vérifier.**
                On montre, on nomme, on explique sur celle qui porte les mots ;
                puis on affiche la muette et on demande à l'élève de retrouver
                ce qu'il vient de voir.

                [ARDOISE]
                SCHEMA:une-cle-de-la-liste/muette
                [/ARDOISE]

                **SUR LA MUETTE, TU NE PRONONCES PAS LES NOMS.** Tu les connais —
                ils sont écrits plus haut — et c'est précisément ce que tu fais
                chercher. « Montre-moi la Bretagne » est une consigne ; « la
                Bretagne est à gauche, montre-la » est la réponse donnée.

                **TU SAURAS S'IL A JUSTE.** L'élève clique, et le nom exact de
                l'endroit qu'il a montré t'est donné, calculé — comme sur la
                légendée. Tu corriges sans avoir à deviner : « oui, c'est bien la
                Bretagne » ou « non, là c'est la Normandie, la Bretagne est
                juste en dessous ».

                **UNE À LA FOIS AU TABLEAU.** Afficher la muette efface la
                légendée, et inversement. Si l'élève sèche, réaffiche la légendée
                pour qu'il relise, puis remets la muette : c'est une bonne façon
                de faire, et ça ne coûte qu'une ligne.

                Cela mis à part, afficher une planche ne se refuse jamais : c'est
                une clé de vingt caractères, elle ne coûte aucun temps de séance, et
                le programme du jour n'est pas une raison de dire non.
                """;
        }

        /// <summary>
        /// Une description trop longue mangerait le prompt sans rien apporter :
        /// au-delà de quelques dizaines de légendes, ce n'est plus un inventaire
        /// mais du commentaire, et le modèle en a assez pour cadrer ses
        /// questions.
        /// </summary>
        private static string Abreger(string contenu)
        {
            var propre = contenu.Replace('\n', ' ').Replace("\r", string.Empty).Trim();

            return propre.Length <= 600 ? propre : propre[..600] + "…";
        }
    }
}
