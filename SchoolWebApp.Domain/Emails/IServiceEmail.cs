namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// Un fichier joint à un courriel — image intégrée ou document à
    /// télécharger. Les octets voyagent en mémoire : une diffusion vit le
    /// temps d'un envoi, rien n'a besoin d'être stocké.
    /// </summary>
    /// <param name="Reference">
    /// Le nom par lequel le corps du message désigne l'image : `cid:{Reference}`.
    /// Ignoré pour une pièce jointe, qui n'est référencée nulle part.
    /// </param>
    public record PieceMail(string Reference, string NomFichier, string TypeMime, byte[] Donnees);

    /// <summary>Ce qu'une diffusion a produit.</summary>
    public record ResultatDiffusion(int Envoyes, int Echecs);

    /// <summary>
    /// UNE connexion SMTP ouverte, dans laquelle on envoie PLUSIEURS messages
    /// DIFFÉRENTS.
    ///
    /// POURQUOI CE TROISIÈME MODE D'ENVOI
    /// ----------------------------------
    /// `EnvoyerAsync` ouvre et ferme une connexion par message : parfait pour
    /// une bienvenue, ruineux pour deux cents bilans. `DiffuserAsync` n'ouvre
    /// qu'une connexion mais envoie le MÊME corps à tout le monde — un bilan
    /// hebdomadaire est personnel, chacun a le sien.
    ///
    /// Il manquait le croisement des deux : une connexion, des messages
    /// distincts. C'est ce que fait une session.
    ///
    /// CE QU'ELLE APPORTE EN PLUS D'ÉCONOMISER DES CONNEXIONS
    /// -----------------------------------------------------
    /// - Elle ESPACE les envois. Un service d'envoi coupe la connexion quand on
    ///   le bouscule, et on perd le reste de la liste sans savoir où on en
    ///   était.
    /// - Elle SE RECONNECTE si la connexion tombe. Entre deux bilans il y a un
    ///   appel au modèle de rédaction, qui prend plusieurs secondes ; un serveur
    ///   ferme les connexions inactives bien avant la fin d'une liste de deux
    ///   cents parents.
    /// - Elle distingue un refus DÉFINITIF (adresse qui n'existe pas) d'un
    ///   incident PASSAGER (débit dépassé, coupure). Le premier ne se réessaie
    ///   pas, le second si — les confondre fait soit perdre des messages
    ///   récupérables, soit s'acharner sur une adresse morte.
    /// </summary>
    public interface ISessionEnvoi : IAsyncDisposable
    {
        /// <summary>Envoie un message mis en page. Faux si l'envoi a échoué définitivement.</summary>
        Task<bool> EnvoyerAsync(
            string destinataire,
            string sujet,
            string gabarit,
            IDictionary<string, string> valeurs,
            CancellationToken ct = default);

        int Envoyes { get; }

        int Echecs { get; }
    }

    public interface IServiceEmail
    {
        /// <summary>Faux si le SMTP n'est pas configuré : l'appelant journalise au lieu d'échouer.</summary>
        bool Disponible { get; }

        /// <summary>
        /// Envoie un mail HTML mis en page dans le gabarit du site.
        /// </summary>
        /// <param name="destinataire">Adresse du parent.</param>
        /// <param name="sujet">Objet du message.</param>
        /// <param name="gabarit">Nom du gabarit, sans extension (bienvenue, reinitialisation, bilan).</param>
        /// <param name="valeurs">Valeurs des marqueurs {{cle}} du gabarit.</param>
        Task<bool> EnvoyerAsync(
            string destinataire,
            string sujet,
            string gabarit,
            IDictionary<string, string> valeurs,
            CancellationToken ct = default);

        /// <summary>
        /// Envoie le MÊME message à plusieurs destinataires.
        ///
        /// POURQUOI UNE MÉTHODE À PART PLUTÔT QU'UNE BOUCLE SUR LA PREMIÈRE
        /// ---------------------------------------------------------------
        /// `EnvoyerAsync` ouvre, authentifie et ferme une connexion SMTP par
        /// message. Sur trois cents parents, ce sont trois cents connexions —
        /// lent, et surtout le profil exact que les hébergeurs bloquent pour
        /// suspicion de spam. Celle-ci ouvre UNE connexion et envoie tout
        /// dedans.
        ///
        /// CHAQUE DESTINATAIRE REÇOIT SON PROPRE MESSAGE. Une seule enveloppe
        /// avec trois cents adresses en copie exposerait le carnet d'adresses
        /// de tous les clients à chacun d'eux — une fuite de données, pas une
        /// maladresse de mise en forme.
        ///
        /// UN ÉCHEC N'ARRÊTE PAS LES SUIVANTS : une adresse morte dans la liste
        /// ne doit pas priver les deux cents parents d'après.
        /// </summary>
        /// <param name="avancement">
        /// Appelé après chaque envoi, avec le nombre traité. Sert à afficher une
        /// progression : une diffusion de plusieurs minutes sans retour se
        /// termine toujours par un second clic, donc un double envoi.
        /// </param>
        Task<ResultatDiffusion> DiffuserAsync(
            IReadOnlyList<string> destinataires,
            string sujet,
            string gabarit,
            IDictionary<string, string> valeurs,
            IReadOnlyList<PieceMail>? imagesIntegrees = null,
            IReadOnlyList<PieceMail>? piecesJointes = null,
            IProgress<int>? avancement = null,
            CancellationToken ct = default);

        /// <summary>
        /// Compose le corps mis en page, SANS l.envoyer et SANS toucher au logo.
        ///
        /// La différence avec `RendreAsync` tient à une ligne : celle-ci laisse
        /// `cid:logoMimia` intact, pour un message qui partira vraiment et
        /// portera le logo en pièce liée. `RendreAsync`, lui, le remplace par
        /// du base64 pour un aperçu dans un navigateur — que Gmail et Outlook
        /// refuseraient d.afficher dans un vrai courriel.
        /// </summary>
        Task<string> ComposerAsync(string sujet, string gabarit, IDictionary<string, string> valeurs);

        /// <summary>
        /// Rend le mail en HTML sans l.envoyer, logo compris.
        ///
        /// Sert à relire une maquette dans un navigateur : sans cela, vérifier
        /// une virgule dans le gabarit du bilan imposerait d'envoyer un vrai
        /// message à un vrai parent.
        /// </summary>
        Task<string> RendreAsync(string sujet, string gabarit, IDictionary<string, string> valeurs);

        /// <summary>
        /// Ouvre une session d'envoi. À libérer avec `await using`.
        ///
        /// La connexion n'est PAS établie ici mais au premier message : ouvrir
        /// une session pour une liste finalement vide ne doit rien coûter, et
        /// une panne SMTP doit se signaler au moment d'envoyer, pas au moment
        /// de préparer.
        /// </summary>
        ISessionEnvoi OuvrirSession();
    }
}
