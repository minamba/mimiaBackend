namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Un tour de parole de la conversation : qui a parlé, et ce qu'il a dit.
    ///
    /// `Qui` vaut `eleve` ou `professeur`. Deux valeurs, en toutes lettres :
    /// une ligne lue directement en base doit se comprendre sans aller
    /// chercher la table de correspondance dans le code — même règle que pour
    /// les statuts d'idée et les urgences.
    /// </summary>
    public record TourExpressionOrale(string Qui, string Texte);

    /// <summary>
    /// Une conversation d'expression orale, telle que l'élève et le parent la
    /// relisent.
    ///
    /// SANS AUCUN AUDIO — voulu par Camara le 18/09/2026 : « on aura juste la
    /// discussion affichée comme dans une messagerie classique ». La
    /// compréhension orale, elle, garde son fichier pour être réécoutée.
    /// </summary>
    public class ExpressionOraleEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        /// <summary>
        /// Le prénom du professeur et sa couleur — pour les pastilles de la
        /// conversation, qui disent qui a parlé.
        ///
        /// Le prénom de l'ÉLÈVE n'est pas ici : l'écran l'a déjà sous la main,
        /// c'est lui qui est connecté. Le faire voyager à chaque ligne serait le
        /// répéter trente fois pour une information qu'on connaît.
        /// </summary>
        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        /// <summary>De quoi on a parlé. Toujours renseigné.</summary>
        public string Titre { get; set; } = string.Empty;

        /// <summary>Code de la langue parlée : en, fr, es, de, it, zh.</summary>
        public string Langue { get; set; } = string.Empty;

        /// <summary>
        /// La conversation entière, dans l'ordre.
        ///
        /// VIDE DANS LA LISTE, REMPLIE DANS LE DÉTAIL. Une liste de trente
        /// conversations ferait transiter trente échanges complets pour
        /// afficher trente titres — c'est la même règle que partout ailleurs
        /// ici : les listes ne portent pas leur contenu.
        /// </summary>
        public List<TourExpressionOrale> Echange { get; set; } = [];

        /// <summary>Le nombre de tours, pour l'afficher dans la liste sans la charger.</summary>
        public int NombreTours { get; set; }

        public string? Remarque { get; set; }

        public int? NiveauScolaireId { get; set; }

        public string? NiveauLibelle { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateConsultation { get; set; }
    }
}
