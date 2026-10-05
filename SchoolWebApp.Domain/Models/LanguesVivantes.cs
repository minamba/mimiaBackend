namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES LANGUES QU'ON PEUT MONTRER OU CACHER, UNE PAR UNE — Camara, le
    /// 04/10/2026 : « un mode pour afficher ou pas les différentes langues,
    /// comme ça on peut les implémenter, je teste en local, et je ne les
    /// affiche pas en prod tant qu'elles ne sont pas prêtes ».
    ///
    /// CE QUE VEUT DIRE « ÉTEINTE » : la langue n'existe plus pour personne.
    /// Ses matières disparaissent de la grille, son professeur de la page
    /// d'accueil, sa case des options de l'élève, et aucun cours ne s'ouvre.
    /// Ce n'est PAS « à venir » : une matière inactive (<c>Matiere.Active</c>)
    /// reste, elle, connue du produit.
    ///
    /// UNE LANGUE, PLUSIEURS MATIÈRES. L'espagnol, c'est la LVB et la
    /// spécialité LLCER : éteindre l'une sans l'autre laisserait Lucía sur la
    /// page d'accueil, avec une seule discipline. On éteint la famille.
    ///
    /// L'ANGLAIS N'EST PAS ICI. C'est la langue obligatoire de tous les
    /// élèves ; un interrupteur qui le ferait disparaître serait un piège.
    ///
    /// LES DÉFAUTS : l'espagnol est ouvert depuis le 14/09/2026, et un réglage
    /// neuf ne doit rien éteindre au premier démarrage. Les autres naissent
    /// ÉTEINTES — c'est tout l'objet : une langue se montre quand on le décide.
    /// </summary>
    public static class LanguesVivantes
    {
        /// <param name="Code">Le nom de la langue dans le réglage : <c>LANGUE_ESPAGNOL</c>.</param>
        /// <param name="Libelle">Ce que lit l'administrateur.</param>
        /// <param name="Matieres">Les codes de matière qui s'allument et s'éteignent ensemble.</param>
        /// <param name="AllumeeParDefaut">L'état tant que personne n'a touché l'interrupteur.</param>
        public record Langue(string Code, string Libelle, string[] Matieres, bool AllumeeParDefaut)
        {
            /// <summary>La clé de la table des réglages.</summary>
            public string Cle => Prefixe + Code;
        }

        public const string Prefixe = "LANGUE_";

        public static readonly IReadOnlyList<Langue> Toutes =
        [
            new("ESPAGNOL", "Espagnol", ["ESPAGNOL", "LLCER_ESPAGNOL"], AllumeeParDefaut: true),
            new("ALLEMAND", "Allemand", ["ALLEMAND", "LLCER_ALLEMAND"], AllumeeParDefaut: false),
            new("ITALIEN", "Italien", ["ITALIEN", "LLCER_ITALIEN"], AllumeeParDefaut: false),
            new("CHINOIS", "Chinois", ["CHINOIS"], AllumeeParDefaut: false),
            new("ARABE", "Arabe", ["ARABE"], AllumeeParDefaut: false),
            new("JAPONAIS", "Japonais", ["JAPONAIS"], AllumeeParDefaut: false),
        ];

        public static Langue? Trouver(string? code) =>
            Toutes.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));

        /// <summary>Cette langue est-elle allumée, au vu des réglages enregistrés ?</summary>
        public static bool EstAllumee(Langue langue, IReadOnlyDictionary<string, string> reglages) =>
            reglages.TryGetValue(langue.Cle, out var valeur)
                ? string.Equals(valeur, "true", StringComparison.OrdinalIgnoreCase)
                : langue.AllumeeParDefaut;

        /// <summary>
        /// Les codes de matière à faire disparaître, au vu des réglages. Les
        /// clés sont celles de la table (<see cref="Langue.Cle"/>) ; une clé
        /// absente vaut le défaut de la langue.
        /// </summary>
        public static HashSet<string> MatieresEteintes(IReadOnlyDictionary<string, string> reglages) =>
            Toutes
                .Where(l => !EstAllumee(l, reglages))
                .SelectMany(l => l.Matieres)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
