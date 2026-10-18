namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN MODÈLE DE SIGNATURE DE COURRIEL (17/10/2026) — Camara : « des templates
    /// de signature qui apparaîtront à la fin du mail, attribués aux utilisateurs ;
    /// le modèle de base prend les informations et le rôle de la personne
    /// connectée, dans le thème du site ».
    ///
    /// DES CHOIX, PAS DU HTML : la mise en page est celle du site
    /// (`SignatureRendu`) ; le modèle ne dit que quoi afficher. Le prénom, le
    /// nom, le rôle et l'adresse sont ceux de l'expéditeur, au moment de l'envoi.
    /// </summary>
    public partial class SignatureMail
    {
        public int Id { get; set; }

        public string Nom { get; set; } = null!;

        /// <summary>« Bien à vous, », « Belle journée, »… (vide : aucune).</summary>
        public string? Formule { get; set; }

        /// <summary>Une ligne libre sous le nom : un slogan, un téléphone.</summary>
        public string? LigneLibre { get; set; }

        public bool AfficherRole { get; set; } = true;

        public bool AfficherAdresse { get; set; } = true;

        /// <summary>Le numéro affiché sous l'adresse mail (17/10/2026) ; vide = pas de ligne.</summary>
        public string? Telephone { get; set; }

        public bool AfficherSite { get; set; } = true;

        /// <summary>La couleur du trait et du nom (« #RRGGBB »).</summary>
        public string Couleur { get; set; } = "#d63f1c";

        /// <summary>Le modèle de qui n'en a pas reçu d'autre. Un seul à la fois.</summary>
        public bool ParDefaut { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
