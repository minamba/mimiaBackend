namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// L'avis qu'une famille laisse sur le produit.
    ///
    /// UN AVIS PAR PARENT, MODIFIABLE
    /// ------------------------------
    /// Un index unique sur le parent, et non une simple contrainte applicative :
    /// un double-clic sur « Envoyer » suffirait à en créer deux, et la page
    /// vitrine afficherait deux fois la même famille. Revenir sur son avis
    /// remplace donc le précédent — ce qui est aussi ce qu'on veut d'un parent
    /// dont l'expérience a changé après trois mois.
    ///
    /// PUBLIÉ SUR DÉCISION, JAMAIS PAR DÉFAUT
    /// --------------------------------------
    /// `Publie` vaut faux à l'arrivée. Un avis n'apparaît sur la page d'accueil
    /// qu'après relecture, et cette décision est réversible dans les deux sens.
    ///
    /// La raison n'est pas la flatterie — un avis critique publié vaut mieux
    /// qu'une page de cinq étoiles que personne ne croit. C'est que la page
    /// d'accueil d'un service destiné à des ENFANTS accepterait sinon du texte
    /// libre, écrit par n'importe quel compte, visible immédiatement par tout
    /// le monde et indexé par les moteurs. Une insulte, une adresse, un numéro
    /// de téléphone, le nom d'un enfant : rien de tout cela ne se rattrape après
    /// coup, et personne ne surveille une page d'accueil en permanence.
    ///
    /// CE QUI N'EST PAS STOCKÉ ICI
    /// ---------------------------
    /// Le nom affiché. Il est dérivé du parent à la lecture — prénom et
    /// initiale — pour qu'un changement de nom ou une suppression de compte
    /// emporte l'affichage avec lui. Le recopier ici en ferait une donnée
    /// personnelle de plus, dupliquée, qu'un effacement oublierait.
    /// </summary>
    public partial class AvisClient
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        /// <summary>De 1 à 5. Validé à l'entrée, et borné en base.</summary>
        public int Note { get; set; }

        /// <summary>Le titre court, façon « Très content ». Facultatif.</summary>
        public string? Titre { get; set; }

        /// <summary>Le corps de l'avis. Facultatif : une note seule est un avis.</summary>
        public string? Commentaire { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Renseignée seulement si le parent est revenu sur son avis.</summary>
        public DateTime? DateModification { get; set; }

        /// <summary>Visible sur la page d'accueil. Faux tant qu'il n'a pas été relu.</summary>
        public bool Publie { get; set; }

        public virtual Parent? Parent { get; set; }
    }
}
