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
            var planches = (await _planches.GetToutesAsync(ct))
                .Where(p => string.Equals(p.MatiereCode, code, StringComparison.OrdinalIgnoreCase))
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
                : Rediger(planches.Select(p => (p.Cle, p.Contenu!)));

            _cache.Set(cle, bloc, Duree);

            return bloc;
        }

        public void Oublier(string? matiereCode)
        {
            if (string.IsNullOrWhiteSpace(matiereCode)) return;

            _cache.Remove($"planches:bloc:{matiereCode.Trim().ToUpperInvariant()}");
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
        private static string Rediger(IEnumerable<(string Cle, string Contenu)> planches)
        {
            var lignes = planches.Select(p =>
                $"- `SCHEMA:{p.Cle}` — montre : {Abreger(p.Contenu)}");

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

                Et afficher une planche ne se refuse jamais : c'est une clé de
                vingt caractères, elle ne coûte aucun temps de séance. Ni le
                niveau de l'élève, ni le programme du jour ne sont des raisons de
                dire non.
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
