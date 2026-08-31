namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// La trace qu'une adresse a déjà eu son essai gratuit.
    ///
    /// POURQUOI ELLE SURVIT À LA SUPPRESSION DU COMPTE
    /// ----------------------------------------------
    /// Tout le reste s'efface, et c'est la promesse faite au parent. Mais si
    /// TOUT s'efface, supprimer son compte puis se réinscrire rend l'essai —
    /// trente minutes gratuites, à volonté, pour qui a compris la manœuvre.
    /// Cette table est la seule exception, et elle est réduite au strict
    /// nécessaire pour la justifier.
    ///
    /// CE QU'ELLE NE CONTIENT PAS
    /// -------------------------
    /// Ni adresse, ni nom, ni identifiant de parent, ni rien qui permette de
    /// remonter à quelqu'un. Une empreinte, et la date. On ne peut pas lire
    /// qui s'est inscrit ; on peut seulement répondre « cette adresse-là
    /// a-t-elle déjà eu son essai ? » quand on la connaît déjà — c'est-à-dire
    /// au moment où le parent nous la donne lui-même.
    ///
    /// L'empreinte est SALÉE, avec un secret qui ne vit pas en base. Sans sel,
    /// une fuite de cette table permettrait de tester des adresses au
    /// dictionnaire et de savoir lesquelles ont un compte chez nous.
    ///
    /// À DÉCLARER dans la politique de confidentialité : c'est une donnée
    /// conservée après une demande d'effacement, au titre de la prévention des
    /// abus. Elle est légitime, elle n'est pas invisible.
    /// </summary>
    public partial class EssaiConsomme
    {
        public int Id { get; set; }

        /// <summary>Empreinte salée de l'adresse normalisée. Jamais l'adresse.</summary>
        public string MailHache { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }
    }
}
