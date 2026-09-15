namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Le strict nécessaire pour reconnaître une matière : son identifiant et
    /// les deux mots sous lesquels elle peut être nommée. Volontairement un
    /// tuple et non un `MatiereViewModel` — le rapprochement doit marcher
    /// aussi bien depuis le contrôleur (ViewModels) que depuis le flux de chat
    /// (modèles de domaine), et rien d'autre ne le regarde.
    /// </summary>
    public readonly record struct MatiereReconnaissable(int Id, string? Code, string? Libelle);

    /// <summary>
    /// Rapproche la matière ÉCRITE par le professeur d'une matière réelle de
    /// l'élève.
    ///
    /// POURQUOI CE FICHIER EXISTE. Depuis le 13/09/2026, un professeur note un
    /// contrôle même hors de sa matière — l'enfant l'annonce là où il se
    /// trouve, pas là où il faudrait, et un contrôle qu'on refuse de noter est
    /// un contrôle qu'il oublie. Mais la règle de fond du projet ne bouge pas
    /// d'un pouce : LE MODÈLE NE FOURNIT JAMAIS UN IDENTIFIANT. Il fournit un
    /// mot, et ce mot est rapproché d'une liste fermée — les matières
    /// réellement au programme de cet élève. Rien d'autre ne peut sortir d'ici.
    ///
    /// EN CAS DE DOUTE, LA MATIÈRE DE LA CONVERSATION. Un rapprochement
    /// approximatif qui se trompe range le contrôle d'un enfant dans une
    /// matière où personne n'ira le chercher, et où aucun professeur ne le
    /// verra jamais. Retomber sur la matière du professeur qui parle donne, au
    /// pire, un contrôle visible et corrigeable en deux clics.
    /// </summary>
    public static class ResolveurMatiereDeclaree
    {
        /// <summary>
        /// Les mots qu'un enfant emploie vraiment, rapprochés d'un code du
        /// référentiel. Table FERMÉE et écrite à la main : une correspondance
        /// approximative (distance d'édition, sous-chaîne) se tromperait un
        /// jour en silence.
        /// </summary>
        private static readonly Dictionary<string, string> Synonymes = new(StringComparer.Ordinal)
        {
            ["math"] = "MATHEMATIQUES",
            ["maths"] = "MATHEMATIQUES",
            ["mathematique"] = "MATHEMATIQUES",
            ["mathematiques"] = "MATHEMATIQUES",
            ["calcul"] = "MATHEMATIQUES",

            ["francais"] = "FRANCAIS",
            ["french"] = "FRANCAIS",
            ["dictee"] = "FRANCAIS",
            ["orthographe"] = "FRANCAIS",
            ["grammaire"] = "FRANCAIS",
            ["conjugaison"] = "FRANCAIS",

            ["anglais"] = "ANGLAIS",
            ["english"] = "ANGLAIS",

            ["espagnol"] = "ESPAGNOL",
            ["allemand"] = "ALLEMAND",
            ["italien"] = "ITALIEN",
            ["chinois"] = "CHINOIS",

            ["svt"] = "SVT",
            ["bio"] = "SVT",
            ["biologie"] = "SVT",
            ["sciences_de_la_vie_et_de_la_terre"] = "SVT",

            ["physique"] = "PHYSIQUE_CHIMIE",
            ["chimie"] = "PHYSIQUE_CHIMIE",
            ["physique_chimie"] = "PHYSIQUE_CHIMIE",

            ["histoire"] = "HISTOIRE_GEO",
            ["geo"] = "HISTOIRE_GEO",
            ["geographie"] = "HISTOIRE_GEO",
            ["histoire_geo"] = "HISTOIRE_GEO",
            ["histoire_geographie"] = "HISTOIRE_GEO",

            ["philo"] = "PHILOSOPHIE",
            ["philosophie"] = "PHILOSOPHIE",

            ["sciences"] = "SCIENCES",
        };

        /// <summary>
        /// L'identifiant de matière à retenir. Toujours l'une de celles de
        /// l'élève, ou celle de la conversation.
        /// </summary>
        /// <param name="declaree">Le mot écrit par le professeur. Null ou vide = sa matière.</param>
        /// <param name="matieresDeLEleve">
        /// Les matières réellement à son emploi du temps, DÉJÀ filtrées par
        /// l'appelant (au programme de sa classe, ouvertes).
        /// </param>
        /// <param name="matiereDeLaConversation">Le repli, toujours valide.</param>
        public static int Resoudre(
            string? declaree,
            IEnumerable<MatiereReconnaissable> matieresDeLEleve,
            int matiereDeLaConversation)
        {
            if (string.IsNullOrWhiteSpace(declaree)) return matiereDeLaConversation;

            var candidates = matieresDeLEleve.ToList();
            if (candidates.Count == 0) return matiereDeLaConversation;

            var cherche = LecteurBloc.Normaliser(declaree);
            if (cherche.Length == 0) return matiereDeLaConversation;

            // 1. Le code du référentiel, tel quel.
            var parCode = Premier(candidates, m => m.Code is not null && LecteurBloc.Normaliser(m.Code) == cherche);
            if (parCode is int idCode) return idCode;

            // 2. Le libellé affiché, tel quel.
            var parLibelle = Premier(candidates, m => m.Libelle is not null && LecteurBloc.Normaliser(m.Libelle) == cherche);
            if (parLibelle is int idLibelle) return idLibelle;

            // 3. Les mots que les enfants emploient.
            if (Synonymes.TryGetValue(cherche, out var code))
            {
                var attendu = LecteurBloc.Normaliser(code);
                var parSynonyme = Premier(candidates, m => m.Code is not null && LecteurBloc.Normaliser(m.Code) == attendu);
                if (parSynonyme is int idSynonyme) return idSynonyme;
            }

            // 4. Un préfixe, et UN SEUL candidat : « mathé » va bien, « s » ne
            //    va nulle part. Deux candidats et plus, c'est une ambiguïté, et
            //    une ambiguïté se tranche en faveur du repli.
            var parPrefixe = candidates
                .Where(m => m.Libelle is not null && LecteurBloc.Normaliser(m.Libelle).StartsWith(cherche, StringComparison.Ordinal))
                .ToList();

            return parPrefixe.Count == 1 ? parPrefixe[0].Id : matiereDeLaConversation;
        }

        /// <summary>
        /// L'identifiant du premier candidat qui correspond, ou null. Rend un
        /// `int?` plutôt qu'une matière : `FirstOrDefault` sur un type valeur
        /// renvoie un enregistrement vide, qu'on confondrait avec un
        /// identifiant 0.
        /// </summary>
        private static int? Premier(
            IEnumerable<MatiereReconnaissable> candidates, Func<MatiereReconnaissable, bool> critere)
        {
            foreach (var candidate in candidates)
            {
                if (critere(candidate)) return candidate.Id;
            }

            return null;
        }
    }
}
