using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Le projet tel que la conseillère l'a écrit : un but, et une ligne par matière.</summary>
    /// <param name="Retire">Vrai quand l'élève ne veut plus qu'on en parle : le projet est effacé.</param>
    public record ProjetDeclare(string? Objectif, IReadOnlyList<(string Matiere, string Texte)> Matieres, bool Retire);

    /// <summary>
    /// Extrait le bloc [PROJET] d'un message de la conseillère d'orientation
    /// (11/10/2026) :
    ///
    ///   [PROJET]
    ///   but: ingénieur en informatique
    ///   Mathématiques: calcul littéral ; fonctions (fragiles)
    ///   Physique-chimie: à consolider
    ///   [/PROJET]
    ///
    /// Elle ne l'écrit qu'APRÈS l'accord de l'élève (voir <c>PromptsOrientation</c>).
    /// « but: retiré » efface le projet.
    ///
    /// LA MATIÈRE EST UN MOT, JAMAIS UN IDENTIFIANT : le serveur le rapproche
    /// des matières de l'élève (<c>ResolveurMatiereDeclaree</c>), et une ligne
    /// qu'il ne reconnaît pas est écartée — elle n'irait chez aucun professeur.
    ///
    /// SON PROPRE DÉCOUPAGE, PAS <c>LecteurBloc.Champs</c> : celui-ci n'accepte
    /// une clé qu'avant le 20e caractère, et « Sciences économiques et
    /// sociales » en compte plus.
    /// </summary>
    public static partial class LecteurProjet
    {
        private const int MaxLignes = 8;
        private const int MaxObjectif = 200;
        private const int MaxTexte = 300;

        [GeneratedRegex(@"\[PROJET\](?<corps>.*?)\[/PROJET\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        private static readonly HashSet<string> ClesDuBut = new(StringComparer.Ordinal) { "but", "objectif", "projet" };
        private static readonly HashSet<string> Retraits = new(StringComparer.Ordinal) { "retire", "retiree", "aucun", "efface" };

        /// <summary>Null s'il n'y a pas de bloc, ou pas de but.</summary>
        public static ProjetDeclare? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            string? objectif = null;
            var matieres = new List<(string, string)>();

            foreach (var ligne in bloc.Groups["corps"].Value.Split('\n'))
            {
                var propre = ligne.Trim().TrimStart('-', '•', '*').Trim();
                var separateur = propre.IndexOf(':');
                if (separateur <= 0) continue;

                var cle = propre[..separateur].Trim();
                var valeur = propre[(separateur + 1)..].Trim();
                if (valeur.Length == 0) continue;

                if (ClesDuBut.Contains(LecteurBloc.Normaliser(cle)))
                {
                    objectif ??= Couper(valeur, MaxObjectif);
                }
                else if (matieres.Count < MaxLignes)
                {
                    matieres.Add((cle, Couper(valeur, MaxTexte)));
                }
            }

            if (objectif is null) return null;

            return Retraits.Contains(LecteurBloc.Normaliser(objectif.TrimEnd('.', '!', ' ')))
                ? new ProjetDeclare(null, [], Retire: true)
                : new ProjetDeclare(objectif, matieres, Retire: false);
        }

        private static string Couper(string texte, int max) => texte.Length <= max ? texte : texte[..max];

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) => LecteurBloc.Retirer(message, Bloc());
    }
}
