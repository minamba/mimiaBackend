namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une venue sur le site public.
    ///
    /// POURQUOI CETTE TABLE EXISTE
    /// ---------------------------
    /// Le tableau de bord savait tout de ceux qui se sont inscrits, et rien de
    /// ceux qui sont passés. Sans ce chiffre, un mois sans inscription ne se
    /// distingue pas d'un mois sans visite — et ce ne sont pas les mêmes
    /// problèmes : l'un tient à l'offre, l'autre à ce qui amène les gens.
    ///
    /// CE QU'ELLE NE CONTIENT PAS
    /// --------------------------
    /// Ni adresse IP, ni agent utilisateur, ni page consultée, ni pays, ni
    /// référent. Un identifiant tiré au hasard par le navigateur, et une date.
    /// On ne peut rien reconstituer de qui est venu : on peut seulement
    /// compter combien de navigateurs distincts se sont présentés.
    ///
    /// L'IDENTIFIANT VIENT DU NAVIGATEUR, ET C'EST CE QUI LE REND ANODIN. Il
    /// est tiré au sort la première fois et rangé dans le stockage local du
    /// visiteur. Il ne traverse aucun autre site, aucune régie, et l'effacer —
    /// ce que fait n'importe quel nettoyage d'historique — le remet à zéro. On
    /// accepte de recompter cette personne : mieux vaut un chiffre légèrement
    /// haut qu'une empreinte qui suivrait quelqu'un malgré lui.
    ///
    /// UNE LIGNE PAR HEURE ET PAR VISITEUR, PAS PAR PAGE. Le navigateur ne
    /// signale sa venue qu'une fois par heure. Sans cette retenue, un enfant
    /// qui recharge dix fois pèserait dix fois plus qu'une famille qui lit la
    /// page une fois — et la table grossirait au rythme des clics.
    /// </summary>
    public partial class VisiteSite
    {
        public int Id { get; set; }

        /// <summary>
        /// L'identifiant tiré au sort par le navigateur. C'est lui qu'on
        /// dédoublonne pour compter des VISITEURS et non des passages.
        /// </summary>
        public string Visiteur { get; set; } = string.Empty;

        /// <summary>En UTC, comme tout le reste de la base.</summary>
        public DateTime Horodatage { get; set; }
    }
}
