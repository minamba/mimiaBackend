namespace SchoolWebApp.Api.ViewModels
{
    public class NiveauScolaireViewModel
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? Cycle { get; set; }

        public int Ordre { get; set; }
    }

    public class MatiereViewModel
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? AgentSlug { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        public int Ordre { get; set; }

        /// <summary>
        /// Bornes incluses de l'ordre du niveau où la matière existe. Le front
        /// s'en sert pour ne proposer à l'élève que ce qui figure réellement à
        /// son emploi du temps — un CM1 ne doit pas voir « Physique-Chimie ».
        /// </summary>
        public int NiveauOrdreMin { get; set; }

        /// <inheritdoc cref="NiveauOrdreMin"/>
        public int NiveauOrdreMax { get; set; }

        public bool Active { get; set; }

        /// <summary>
        /// Ce que la matière promet à l enfant, en une phrase.
        ///
        /// « Calculs, problèmes et géométrie ». Un nom de matière seul ne dit
        /// rien à un enfant de neuf ans ; cette phrase lui dit ce qu il va
        /// FAIRE. Elle vient de la base et non du front : une matière se décrit
        /// à un seul endroit.
        /// </summary>
        public string? Promesse { get; set; }

        /// <summary>
        /// Les classes où cette matière ne figure pas, malgré un rang d année
        /// compris dans ses bornes.
        ///
        /// Les bornes ne suffisent plus depuis que trois classes partagent le
        /// même rang : la terminale professionnelle a du français et pas de
        /// philosophie, la terminale générale l inverse. Voir
        /// `VoiesScolaires`.
        /// </summary>
        public string[] NiveauCodesExclus { get; set; } = [];
    }

    /// <summary>
    /// Un professeur, tel que la page d accueil le présente.
    ///
    /// POURQUOI CE MODÈLE EXISTE PLUTÔT QUE DE RÉUTILISER LES MATIÈRES
    /// ---------------------------------------------------------------
    /// L équipe n est pas la liste des matières : un professeur peut en tenir
    /// deux. Yann enseigne les sciences puis la physique-chimie, et il ne faut
    /// pas l afficher deux fois sous deux visages identiques.
    ///
    /// Le regroupement est fait par le SERVEUR parce qu il découle des données
    /// — deux matières portant le même visage sont le même professeur. Laissé
    /// au front, il aurait fallu le refaire à chaque endroit qui montre
    /// l équipe.
    /// </summary>
    public class ProfesseurViewModel
    {
        public string? Prenom { get; set; }

        /// <summary>Le nom du visage à dessiner : nora, adrien, camille...</summary>
        public string? Avatar { get; set; }

        public string? Couleur { get; set; }

        /// <summary>
        /// Ses matières, dans l ordre d affichage. Le front décide du
        /// séparateur : c est de la présentation, elle ne se décide pas ici.
        /// </summary>
        public List<string> Matieres { get; set; } = [];

        /// <summary>
        /// Le code de sa matière principale — la première de la liste. Sert au
        /// motif dessiné en fond de sa carte.
        /// </summary>
        public string? Code { get; set; }
    }
}
