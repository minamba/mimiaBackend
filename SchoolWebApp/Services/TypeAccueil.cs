namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Nature de la prise de parole initiale du professeur.
    ///
    /// Un booléen ne suffisait plus : accueillir un élève pour la première fois
    /// et retrouver un élève après trois semaines n'appellent pas le même
    /// message, et c'est précisément cette différence qui fait qu'on a affaire
    /// à un professeur plutôt qu'à un formulaire.
    /// </summary>
    public enum TypeAccueil
    {
        /// <summary>L'élève écrit le premier : le professeur ne prend pas les devants.</summary>
        Aucun = 0,

        /// <summary>Première séance dans cette matière : le professeur se présente.</summary>
        PremiereSeance = 1,

        /// <summary>L'élève revient : le professeur reprend le fil de la dernière fois.</summary>
        Retour = 2,

        /// <summary>Il reste cinq minutes de séance : le professeur prévient.</summary>
        FinProche = 3,

        /// <summary>
        /// Trente secondes : le professeur salue l'élève. C'est LUI qui clôt la
        /// séance, à la seconde près — jamais avant, et jamais de sa propre
        /// initiative quelques minutes plus tôt.
        /// </summary>
        FinImminente = 8,

        /// <summary>La séance est terminée : le professeur conclut.</summary>
        FinSeance = 4,

        /// <summary>
        /// L'élève enchaîne une séance de plus, tout de suite après la
        /// précédente. Ni un premier accueil, ni un retour après absence : le
        /// professeur vient de dire au revoir il y a dix secondes.
        /// </summary>
        NouvelleSeance = 5,

        /// <summary>
        /// Dix minutes de dépassement : le contrôle s'éternise. Le professeur
        /// annonce qu'il clôturera dans deux minutes.
        /// </summary>
        ClotureProche = 6,

        /// <summary>
        /// Douze minutes de dépassement : le professeur clôture le contrôle
        /// sur ce qui a été fait, rend la note, et conclut la séance.
        /// </summary>
        ClotureForcee = 7,

        /// <summary>
        /// L'élève revient après être parti EN PLEIN CONTRÔLE.
        ///
        /// Un retour comme un autre ne suffit pas : il y a quelque chose à
        /// dire — le contrôle est annulé et sera refait en entier — et il vaut
        /// mieux que ce soit le professeur qui l'annonce, plutôt que l'enfant
        /// qui le découvre en voyant réapparaître la question un.
        /// </summary>
        RetourControleAbandonne = 9,
    }
}
