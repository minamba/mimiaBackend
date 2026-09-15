namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une notion au programme d'un contrôle — le PÉRIMÈTRE, et rien d'autre.
    ///
    /// ELLE NE PORTE AUCUN SCORE, VOLONTAIREMENT. L'état d'une notion est
    /// mesuré par le moteur de maîtrise (<see cref="MaitriseEleve"/>, alimenté
    /// après chaque séance par l'observateur), avec ses seuils, sa confiance et
    /// sa péremption à 90 jours. Dupliquer cette mesure ici obligerait à
    /// réinventer les quatre, et à décider qui les écrit — alors que
    /// l'écrivain existe déjà. Ce qui n'existait nulle part, c'est « sur quoi
    /// porte ce contrôle » : c'est tout ce que cette table ajoute.
    /// </summary>
    public partial class ControleNotion
    {
        public int Id { get; set; }

        public int ControleId { get; set; }

        /// <summary>
        /// La compétence du référentiel, quand la notion a pu y être
        /// rapprochée.
        ///
        /// NULLABLE, ET C'EST INDISPENSABLE : six niveaux n'ont aucune
        /// compétence au référentiel (3e prépa-métiers, voies pro et techno).
        /// Sans repli sur un libellé libre, ces élèves n'auraient jamais de
        /// périmètre — donc jamais de préparation. Même chaînage par libellé
        /// que <see cref="FicheRevision.Notion"/>, qui n'a pas non plus de clé
        /// étrangère vers le référentiel.
        /// </summary>
        public int? CompetenceId { get; set; }

        /// <summary>
        /// Toujours rempli : le libellé du référentiel quand la compétence est
        /// connue, le texte du professeur sinon. À l'affichage, celui de la
        /// compétence l'emporte — le référentiel peut être corrigé après coup.
        /// </summary>
        public string Libelle { get; set; } = null!;

        /// <summary>
        /// Quand cette notion a été travaillée en préparation.
        ///
        /// C'EST ICI, ET NULLE PART AILLEURS, QUE VIT « L'EFFORT VISIBLE ».
        /// Une notion travaillée ne peut plus être présentée comme « à
        /// découvrir » : l'enfant y a passé du temps, l'écran doit le montrer.
        /// Mais rien de tout cela ne touche au score de maîtrise, qui reste
        /// une mesure — et une mesure ne se gonfle pas parce qu'on a révisé.
        /// </summary>
        public DateTime? TravailleeLe { get; set; }

        /// <summary>DECLAREE (posée à l'annonce) ou PREPARATION (ajoutée en séance).</summary>
        public string Source { get; set; } = null!;

        /// <summary>
        /// Ce que la copie corrigée a montré : "reussie" ou "ratee". Null tant
        /// que le contrôle n'a pas été repris avec l'élève.
        ///
        /// CE N'EST PAS UN DOUBLON DU SCORE DE MAÎTRISE. Le score dit ce que
        /// l'élève sait aujourd'hui, et il continue de bouger après ; ceci dit
        /// ce qui est arrivé CE JOUR-LÀ, sur cette copie — un fait daté, qui
        /// ne change plus. Les deux se nourrissent pourtant : une notion ratée
        /// au contrôle est appliquée au moteur de maîtrise comme une
        /// observation, ce qui la fait ressortir dans les lacunes du
        /// professeur à la séance suivante.
        /// </summary>
        public string? Resultat { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual ControleScolaire? Controle { get; set; }

        public virtual Competence? Competence { get; set; }
    }
}
