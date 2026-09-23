using System.Reflection;
using System.Text.Json;

namespace SchoolWebApp.Api.Services.Jeux
{
    /// <summary>Un jeu, pour UNE classe : la même clé change de titre et de compétences d'une classe à l'autre.</summary>
    public sealed class JeuDuCatalogue
    {
        public string Classe { get; init; } = "";

        public string Cle { get; init; } = "";

        public string Titre { get; init; } = "";

        public string MatiereCode { get; init; } = "";

        public IReadOnlyList<string> Competences { get; init; } = [];

        /// <summary>Le rang de l'année du jeu, 1 (CP) à 12 — voir <see cref="CatalogueJeux.RangDeLaClasse"/>.</summary>
        public int Rang { get; init; }

        /// <summary>
        /// Ce que le professeur recopie dans la balise : « CE1/course-des-tables ».
        /// La classe fait partie du nom parce que le front ouvre le jeu À CETTE
        /// classe-là — un CM2 renvoyé vers un jeu de CE1 doit le retrouver tel
        /// qu'il était au CE1.
        /// </summary>
        public string Identifiant => $"{Classe}/{Cle}";
    }

    /// <summary>
    /// LE CATALOGUE DES JEUX, TEL QUE LE FRONT LE PUBLIE.
    ///
    /// Camara, le 23/09/2026 : à la fin du cours, le professeur peut proposer un
    /// jeu en rapport avec la notion travaillée. Pour ça, il faut qu'il sache
    /// quels jeux existent, pour quelle classe, et quelles compétences chacun
    /// travaille — et cette connaissance vit dans le front, dans
    /// <c>src/lib/jeux/catalogue.js</c>, qui fait foi.
    ///
    /// ELLE N'EST PAS RECOPIÉE, ELLE EST GÉNÉRÉE : <c>scripts/jeux-vers-api.mjs</c>
    /// écrit <c>Ressources/jeux.json</c> à partir du vrai catalogue. Le fichier
    /// est une RESSOURCE EMBARQUÉE dans la DLL : impossible de l'oublier au
    /// déploiement, ce qui a déjà mis un conteneur en boucle avec un autre
    /// fichier. Un catalogue absent est une erreur au démarrage, pas un
    /// professeur silencieux qu'on découvre trois semaines plus tard.
    ///
    /// RELANCER LE SCRIPT ET REPUBLIER après tout jeu ajouté, renommé ou
    /// déplacé de classe — un test du front tombe si l'on oublie le script.
    /// </summary>
    public sealed class CatalogueJeux
    {
        private const string Ressource = "SchoolWebApp.Api.Ressources.jeux.json";

        /// <summary>
        /// La liste et son index, REMPLACÉS D'UN BLOC. Le worker de
        /// rechargement écrit pendant que les séances lisent : une seule
        /// référence, échangée d'un coup, et chaque lecteur voit soit l'ancien
        /// état entier, soit le nouveau — jamais une liste neuve avec un index
        /// vieux.
        /// </summary>
        private sealed record Etat(
            IReadOnlyList<JeuDuCatalogue> Jeux,
            IReadOnlyDictionary<string, JeuDuCatalogue> ParIdentifiant);

        private volatile Etat _etat;

        public CatalogueJeux() : this(LireRessource())
        {
        }

        /// <summary>Le constructeur des tests, et de l'état initial.</summary>
        public CatalogueJeux(IEnumerable<JeuDuCatalogue> jeux)
        {
            _etat = Construire(jeux);
        }

        public IReadOnlyList<JeuDuCatalogue> Jeux => _etat.Jeux;

        /// <summary>
        /// LE CATALOGUE CHANGE SANS REDÉMARRER — Camara, le 23/09/2026 : « un
        /// worker qui nourrit le cerveau du professeur à chaque nouveau jeu ».
        /// Remplace la liste par celle que le site publie, et dit ce qui a
        /// changé pour le journal. Une liste vide est refusée : un site en
        /// panne ne doit pas faire oublier tous les jeux au professeur.
        /// </summary>
        public (IReadOnlyList<string> Nouveaux, IReadOnlyList<string> Retires) Remplacer(IEnumerable<JeuDuCatalogue> jeux)
        {
            var neuf = Construire(jeux);
            if (neuf.Jeux.Count == 0)
            {
                throw new InvalidOperationException("Le catalogue reçu est vide : l'ancien est conservé.");
            }

            var ancien = _etat;
            var nouveaux = neuf.Jeux.Select(j => j.Identifiant)
                .Where(id => !ancien.ParIdentifiant.ContainsKey(id)).ToList();
            var retires = ancien.Jeux.Select(j => j.Identifiant)
                .Where(id => !neuf.ParIdentifiant.ContainsKey(id)).ToList();

            _etat = neuf;
            return (nouveaux, retires);
        }

        /// <summary>Le jeu que désigne une balise, ou <c>null</c> si l'identifiant n'existe pas.</summary>
        public JeuDuCatalogue? Trouver(string? identifiant) =>
            identifiant is not null && _etat.ParIdentifiant.TryGetValue(identifiant.Trim(), out var jeu) ? jeu : null;

        /// <summary>Les jeux d'une matière, pour une année précise.</summary>
        public IReadOnlyList<JeuDuCatalogue> DeLaClasse(string? matiereCode, int rang) =>
            _etat.Jeux.Where(j => j.Rang == rang && MemeMatiere(j, matiereCode)).ToList();

        /// <summary>Les jeux d'une matière, strictement en dessous d'un rang, qui portent une compétence donnée.</summary>
        public IReadOnlyList<JeuDuCatalogue> EnAmontPour(string? matiereCode, int rangMax, string? codeCompetence) =>
            codeCompetence is null
                ? []
                : _etat.Jeux
                    .Where(j => j.Rang < rangMax && MemeMatiere(j, matiereCode)
                        && j.Competences.Contains(codeCompetence, StringComparer.OrdinalIgnoreCase))
                    .ToList();

        private static Etat Construire(IEnumerable<JeuDuCatalogue> jeux)
        {
            var liste = jeux
                .Where(j => !string.IsNullOrWhiteSpace(j.Cle) && !string.IsNullOrWhiteSpace(j.Titre))
                .Select(j => new JeuDuCatalogue
                {
                    Classe = j.Classe.Trim().ToUpperInvariant(),
                    Cle = j.Cle.Trim(),
                    Titre = j.Titre,
                    MatiereCode = j.MatiereCode,
                    Competences = j.Competences,
                    Rang = RangDeLaClasse(j.Classe) ?? 0,
                })
                .Where(j => j.Rang > 0)
                .OrderBy(j => j.Rang).ThenBy(j => j.Cle, StringComparer.Ordinal)
                .ToList();

            return new Etat(liste, liste.ToDictionary(j => j.Identifiant, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>Lit une liste au format publié par le front (<c>jeux.json</c>).</summary>
        public static IReadOnlyList<JeuDuCatalogue> Lire(Stream flux)
        {
            var jeux = JsonSerializer.Deserialize<List<JeuDuCatalogue>>(flux, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return jeux ?? [];
        }

        private static bool MemeMatiere(JeuDuCatalogue jeu, string? matiereCode) =>
            string.Equals(jeu.MatiereCode, matiereCode, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// L'année d'une classe, 1 (CP) à 12 (terminale), ou <c>null</c>.
        ///
        /// MIROIR DE <c>ReferentielSeeder</c> (les vingt-six classes et leur
        /// <c>Ordre</c>), tenu ici pour ne pas lire la base à chaque séance : ce
        /// tableau ne bouge qu'avec le référentiel. Trois classes de lycée
        /// partagent le même rang, c'est voulu — le rang dit l'année, pas la
        /// voie. Le front porte la même table dans <c>src/lib/jeux/frise.js</c>.
        /// </summary>
        public static int? RangDeLaClasse(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            return code.Trim().ToUpperInvariant() switch
            {
                "CP" => 1,
                "CE1" => 2,
                "CE2" => 3,
                "CM1" => 4,
                "CM2" => 5,
                "SIXIEME" => 6,
                "CINQUIEME" => 7,
                "QUATRIEME" => 8,
                "TROISIEME" or "TROISIEME_PREPA" => 9,
                "SECONDE" or "SECONDE_PRO" => 10,
                "PREMIERE" or "PREMIERE_TECHNO" or "PREMIERE_PRO"
                    or "PREMIERE_STMG" or "PREMIERE_ST2S" or "PREMIERE_STL" => 11,
                "TERMINALE" or "TERMINALE_TECHNO" or "TERMINALE_PRO"
                    or "TERMINALE_STMG" or "TERMINALE_ST2S" or "TERMINALE_STL" => 12,
                _ => null,
            };
        }

        private static IEnumerable<JeuDuCatalogue> LireRessource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var flux = assembly.GetManifestResourceStream(Ressource)
                ?? throw new InvalidOperationException(
                    $"Le catalogue des jeux « {Ressource} » n'est pas embarqué : lancer " +
                    "`node scripts/jeux-vers-api.mjs` dans le front, puis recompiler l'API.");

            var jeux = Lire(flux);

            if (jeux.Count == 0)
            {
                throw new InvalidOperationException("Le catalogue des jeux embarqué est vide.");
            }

            return jeux;
        }
    }
}
