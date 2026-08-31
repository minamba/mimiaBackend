namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// La boîte de support, lue et écrite depuis l'administration.
    ///
    /// POURQUOI DES IDENTIFIANTS DISTINCTS DE `EmailSettings`
    /// -----------------------------------------------------
    /// Les courriels transactionnels partent de `no-reply@mimia.fr` — une
    /// adresse qui n'attend aucune réponse, et c'est très bien ainsi pour un
    /// bilan hebdomadaire. Une réponse au support, elle, DOIT partir de
    /// `support@mimia.fr` : sinon le parent qui répond écrit à une boîte que
    /// personne ne lit, et sa relance se perd.
    ///
    /// Deux boîtes, deux comptes, deux jeux d'identifiants.
    /// </summary>
    public class OptionsMessagerie
    {
        public const string Section = "Messagerie";

        /// <summary>
        /// LE SERVEUR DE L'HÉBERGEUR, PAS L'ALIAS DU DOMAINE.
        ///
        /// `mail64.lwspanel.com` et non `mail.mimia.fr`. Le certificat présenté
        /// est un joker `*.lwspanel.com` : par l'alias du domaine, TLS le
        /// rejette et la connexion échoue AVANT l'authentification — avec un
        /// message qui parle de certificat et non de nom d'hôte, ce qui envoie
        /// chercher du côté des mots de passe.
        ///
        /// La même erreur a déjà coûté une soirée sur le SMTP ; elle est
        /// documentée dans `appsettings.json`, et elle vaut pour l'IMAP.
        /// </summary>
        public string? Hote { get; set; }

        /// <summary>993 : IMAP sur TLS dès la connexion.</summary>
        public int PortImap { get; set; } = 993;

        /// <summary>465 : SSL dès la connexion. 587 serait STARTTLS.</summary>
        public int PortSmtp { get; set; } = 465;

        /// <summary>L'adresse de la boîte : celle qui reçoit ET qui répond.</summary>
        public string? Adresse { get; set; }

        public string? MotDePasse { get; set; }

        /// <summary>
        /// LE SERVEUR D'ENVOI, quand il n'est plus celui de l'hébergeur.
        ///
        /// La réception ne bouge pas : les messages arrivent chez LWS, et c'est
        /// bien là qu'il faut aller les lire en IMAP. L'ENVOI, lui, part
        /// désormais par un service dédié — parce que le SMTP mutualisé bloque
        /// l'expéditeur vingt minutes dès la deuxième réponse envoyée coup sur
        /// coup, et qu'une réponse au support qui ne part pas ne se voit nulle
        /// part côté parent.
        ///
        /// Vides, ces trois clés retombent sur `Hote`, `Adresse` et
        /// `MotDePasse` : la configuration d'avant continue de fonctionner
        /// telle quelle.
        /// </summary>
        public string? HoteSmtp { get; set; }

        /// <summary>L'identifiant SMTP, s'il diffère de l'adresse.</summary>
        public string? LoginSmtp { get; set; }

        public string? MotDePasseSmtp { get; set; }

        public string ServeurEnvoi => string.IsNullOrWhiteSpace(HoteSmtp) ? Hote! : HoteSmtp;

        public string IdentifiantEnvoi => string.IsNullOrWhiteSpace(LoginSmtp) ? Adresse! : LoginSmtp;

        public string SecretEnvoi => string.IsNullOrWhiteSpace(MotDePasseSmtp) ? MotDePasse! : MotDePasseSmtp;

        /// <summary>Le nom affiché sur les réponses.</summary>
        public string? Nom { get; set; } = "Support Mimia";

        /// <summary>
        /// Combien de messages on rapatrie au maximum.
        ///
        /// UNE BOÎTE DE SUPPORT N'EST PAS UNE ARCHIVE. Les cinquante derniers
        /// couvrent tout ce à quoi on répond encore ; aller au-delà ferait
        /// télécharger des années de courrier à chaque ouverture de l'écran,
        /// pour des messages que personne ne rouvrira.
        /// </summary>
        public int TailleListe { get; set; } = 50;

        public bool Configure =>
            !string.IsNullOrWhiteSpace(Hote)
            && !string.IsNullOrWhiteSpace(Adresse)
            && !string.IsNullOrWhiteSpace(MotDePasse);
    }
}
