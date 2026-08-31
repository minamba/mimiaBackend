using Microsoft.Extensions.Logging;

namespace SchoolWebApp.Domain.Emails
{
    public class OptionsEmail
    {
        public const string Section = "EmailSettings";

        public string? SmtpHost { get; set; }

        /// <summary>465 = SSL implicite, 587 = STARTTLS. Le service s'adapte au port.</summary>
        public int SmtpPort { get; set; } = 465;

        public string? SenderEmail { get; set; }

        /// <summary>Nom affiché à côté de l'adresse dans la boîte du destinataire.</summary>
        public string? SenderNom { get; set; } = "Mimia";

        public string? SenderPassword { get; set; }

        /// <summary>
        /// L'identifiant SMTP, quand il DIFFÈRE de l'adresse d'expédition.
        ///
        /// POURQUOI CETTE CLÉ EXISTE
        /// -------------------------
        /// Chez un hébergeur mutualisé, on s'authentifie avec l'adresse dont on
        /// se sert : `no-reply@mimia.fr` sert de login et d'expéditeur. Chez un
        /// service d'envoi (Brevo, Sendgrid, Postmark), les deux sont séparés :
        /// on se connecte avec un identifiant technique — `b6xxxxx@smtp-brevo.com`
        /// — et on expédie AU NOM de `no-reply@mimia.fr`. Sans cette
        /// distinction, l'authentification échoue avec un message qui parle de
        /// mot de passe, alors que c'est le login qui est faux.
        ///
        /// Vide, on retombe sur `SenderEmail` : le comportement de toujours.
        /// </summary>
        public string? SmtpLogin { get; set; }

        /// <summary>Le login réellement présenté au serveur SMTP.</summary>
        public string? Identifiant => string.IsNullOrWhiteSpace(SmtpLogin)
            ? SenderEmail
            : SmtpLogin;

        /// <summary>
        /// Boîte qui reçoit les messages du formulaire de contact.
        ///
        /// La clé existait déjà dans la configuration mais n'était liée à
        /// aucune propriété : elle ne servait à rien. Si elle est vide, les
        /// messages partent vers l'adresse d'expédition — mieux vaut qu'ils
        /// arrivent quelque part que nulle part.
        /// </summary>
        public string? RecipientEmail { get; set; }

        /// <summary>
        /// Adresse de repli en développement. Quand elle est renseignée, TOUS les
        /// mails y sont détournés : sans ce garde-fou, une exécution du bilan
        /// hebdomadaire sur des données de test enverrait de vrais mails à de
        /// vraies adresses.
        /// </summary>
        public string? RedirectionDev { get; set; }

        /// <summary>
        /// Respiration entre deux messages d'une même session, en millisecondes.
        ///
        /// UN ENVOI EN RAFALE EST LE PROFIL EXACT D'UN SPAMMEUR. Les services
        /// d'envoi ne répondent pas « ralentissez » : ils coupent la connexion,
        /// ou bloquent l'expéditeur — c'est ce qu'a fait LWS avec un blocage de
        /// vingt minutes dès le deuxième message rapproché. Un quart de seconde
        /// entre deux envois donne quatre messages par seconde, soit deux cents
        /// bilans en moins d'une minute : personne ne le remarque, et aucun
        /// filtre ne s'énerve.
        /// </summary>
        public int PauseEntreEnvoisMs { get; set; } = 250;

        /// <summary>
        /// Nombre de tentatives par message avant de l'abandonner.
        ///
        /// Ne s'applique qu'aux échecs PASSAGERS. Un refus définitif — adresse
        /// inexistante — n'est jamais réessayé : insister ne la fera pas
        /// apparaître, et répéter un envoi refusé abîme la réputation de
        /// l'expéditeur auprès du destinataire.
        /// </summary>
        public int TentativesParMessage { get; set; } = 3;

        /// <summary>
        /// Nombre d'échecs CONSÉCUTIFS après lequel une série est abandonnée.
        ///
        /// LE COUPE-CIRCUIT, ET POURQUOI IL FAUT EN AVOIR UN
        /// ------------------------------------------------
        /// Un message qui échoue coûte trois tentatives et deux attentes, soit
        /// une douzaine de secondes. Mesuré au banc d'essai. Si le service
        /// d'envoi est INDISPONIBLE — panne, quota du jour épuisé, clé
        /// révoquée — ce coût se paie sur CHAQUE parent : trois cents bilans
        /// deviennent une heure de tâche de fond qui s'acharne sur un serveur
        /// mort, un lundi à trois heures du matin.
        ///
        /// Cinq échecs de suite ne sont plus une malchance, c'est une panne.
        /// La série s'arrête et le dit. Le compte des non-traités est exact,
        /// ce qui permet de relancer en connaissance de cause — au lieu de
        /// découvrir le lendemain une tâche qui tourne encore.
        ///
        /// Le compteur repart à zéro dès qu'un envoi réussit : une adresse
        /// morte isolée n'arrête rien.
        /// </summary>
        public int EchecsConsecutifsAvantAbandon { get; set; } = 5;

        /// <summary>URL du site, pour les liens contenus dans les mails.</summary>
        public string? UrlSite { get; set; } = "https://mimia.fr";

        public bool Configure => !string.IsNullOrWhiteSpace(SmtpHost)
                                 && !string.IsNullOrWhiteSpace(SenderEmail)
                                 && !string.IsNullOrWhiteSpace(SenderPassword);

        /// <summary>
        /// Refuse de démarrer si le détournement du courrier est actif hors
        /// développement.
        ///
        /// POURQUOI UN ARRÊT ET NON UN AVERTISSEMENT
        /// ----------------------------------------
        /// `RedirectionDev` détourne TOUT : les bilans hebdomadaires, les
        /// mails de bienvenue, les confirmations de suppression, les alertes de
        /// quota. En production, plus aucun parent ne recevrait rien — et tout
        /// arriverait dans une boîte de test, sans la moindre erreur nulle
        /// part. C'est une panne totale et parfaitement silencieuse : elle ne
        /// se découvre que par une réclamation, des semaines plus tard.
        ///
        /// Un avertissement dans les logs se lit après coup, quand le courrier
        /// est déjà perdu. Le même raisonnement que pour la clé Stripe de
        /// production en développement : on veut que l'application refuse de
        /// se lancer.
        ///
        /// Le coût de se tromper dans ce sens est faible : un site qui ne
        /// démarre pas se voit tout de suite, et se répare en retirant une
        /// ligne de configuration.
        /// </summary>
        public void Verifier(bool environnementDeDeveloppement, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(RedirectionDev)) return;

            if (!environnementDeDeveloppement)
            {
                throw new InvalidOperationException(
                    $"EmailSettings:RedirectionDev est renseigné ({RedirectionDev}) alors que "
                    + "l'environnement n'est PAS le développement. Tout le courrier — bilans, "
                    + "bienvenue, confirmations — serait détourné vers cette adresse et aucun "
                    + "parent ne recevrait rien. Retirez cette clé de la configuration.");
            }

            // En développement, c'est au contraire ce qu'on veut : on le dit,
            // pour que personne ne s'étonne de ne rien recevoir sur sa vraie
            // adresse pendant un test.
            logger.LogWarning(
                "Courrier detourne vers {Adresse} : aucun mail ne partira aux vraies adresses.",
                RedirectionDev);
        }
    }
}
