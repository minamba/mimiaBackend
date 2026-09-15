using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Un contrôle à venir, annoncé par l'élève et noté par son professeur.</summary>
    public record ControleProgrammeDeclare(
        string? Matiere,
        string? Sujet,
        DateTime? Date,
        TimeSpan? Heure,
        IReadOnlyList<string> Notions);

    /// <summary>
    /// Extrait le bloc [CONTROLE_PROGRAMME] d'un message du professeur.
    ///
    /// LA DATE EST LE SEUL CHAMP OBLIGATOIRE — contrairement à
    /// [EVALUATION_PREVUE] où `notion` est facultatif. Un contrôle sans date
    /// n'a aucun jour où s'épingler dans le calendrier : sans elle, mieux
    /// vaut ne rien archiver que d'archiver une ligne inutilisable.
    ///
    /// `matiere` EST UN MOT, JAMAIS UN IDENTIFIANT. Le professeur peut
    /// désormais noter un contrôle qui n'est pas de sa matière (l'enfant
    /// l'annonce là où il se trouve, pas là où il faudrait) — mais ce mot est
    /// rapproché côté serveur d'une liste fermée, celle des matières
    /// réellement au programme de l'élève. Voir `ResolveurMatiereDeclaree`.
    /// </summary>
    public static partial class LecteurControleProgramme
    {
        /// <summary>Au-delà, c'est que le modèle recopie un chapitre entier.</summary>
        private const int MaxNotions = 8;

        private const int MaxLongueurNotion = 200;

        [GeneratedRegex(@"\[CONTROLE_PROGRAMME\](?<corps>.*?)\[/CONTROLE_PROGRAMME\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        /// <summary>Null s'il n'y a pas de bloc.</summary>
        public static ControleProgrammeDeclare? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            return new ControleProgrammeDeclare(
                LecteurBloc.Valeur(champs, "matiere"),
                LecteurBloc.Valeur(champs, "sujet"),
                LecteurBloc.Date(champs, "date"),
                LecteurBloc.Heure(champs, "heure"),
                DecouperNotions(LecteurBloc.Valeur(champs, "notions", "notion")));
        }

        /// <summary>
        /// « a ; b ; c » → trois notions. Le point-virgule plutôt que la
        /// virgule : une notion en contient souvent une (« additionner,
        /// simplifier et comparer »), et couper là-dessus produirait des
        /// moitiés de phrase.
        /// </summary>
        internal static IReadOnlyList<string> DecouperNotions(string? brut)
        {
            if (string.IsNullOrWhiteSpace(brut)) return [];

            return brut
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(n => n.Length > 0)
                .Select(n => n.Length <= MaxLongueurNotion ? n : n[..MaxLongueurNotion])
                .Take(MaxNotions)
                .ToList();
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
