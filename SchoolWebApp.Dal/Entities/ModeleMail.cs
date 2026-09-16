namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un modèle de courriel : une diffusion qu'on réutilise, ou un courriel
    /// automatique programmé.
    ///
    /// UNE SEULE TABLE POUR LES DEUX NATURES
    /// ------------------------------------
    /// Une diffusion enregistrée et un courriel automatique se composent
    /// exactement de la même façon — objet, titre, message, images, documents —
    /// et s'éditent dans le même formulaire. Ce qui les sépare, c'est QUI
    /// déclenche l'envoi : l'administrateur d'un clic, ou le planificateur. Les
    /// colonnes de planification restent donc vides pour une diffusion.
    ///
    /// LES COURRIELS AUTOMATIQUES ONT UN CODE, les diffusions n'en ont pas. Le
    /// code est ce que le planificateur cherche (« FIN_ESSAI ») : le nom affiché
    /// peut changer sans que rien ne casse.
    ///
    /// L'HEURE EST CELLE DE PARIS, pas UTC. « Tous les jours à 10 h » veut dire
    /// 10 h à l'horloge des parents, hiver comme été ; la conversion se fait au
    /// moment de calculer la prochaine occurrence.
    /// </summary>
    public partial class ModeleMail
    {
        public int Id { get; set; }

        /// <summary>Diffusion ou Automatique — voir `NatureModeleMail`.</summary>
        public string Nature { get; set; } = string.Empty;

        /// <summary>Le code d'un courriel automatique. Nul pour une diffusion.</summary>
        public string? Code { get; set; }

        /// <summary>Le nom dans la liste des templates.</summary>
        public string Nom { get; set; } = string.Empty;

        /// <summary>À quoi sert ce courriel, en une ou deux phrases.</summary>
        public string? Description { get; set; }

        public string Sujet { get; set; } = string.Empty;

        public string Titre { get; set; } = string.Empty;

        /// <summary>
        /// Le message tel qu'on l'écrit : lignes vides pour les paragraphes,
        /// `**gras**`, `[image:N]`. Jamais du HTML.
        /// </summary>
        public string Texte { get; set; } = string.Empty;

        // --------------------------------------------------- la planification

        /// <summary>Aucune, Jour, Semaine ou Mois — voir `FrequenceEnvoi`.</summary>
        public string Frequence { get; set; } = "Aucune";

        /// <summary>L'heure d'envoi, à l'horloge de Paris.</summary>
        public TimeOnly? HeureEnvoi { get; set; }

        /// <summary>Pour une fréquence hebdomadaire : 1 = lundi … 7 = dimanche.</summary>
        public int? JourSemaine { get; set; }

        /// <summary>
        /// Pour une fréquence mensuelle : 1 à 31. Un mois plus court prend son
        /// dernier jour — « le 31 » part le 30 avril et le 28 février.
        /// </summary>
        public int? JourMois { get; set; }

        /// <summary>Programmé ? Éteint, le planificateur ne l'envoie jamais.</summary>
        public bool Actif { get; set; }

        /// <summary>
        /// L'occurrence prévue la plus récente déjà prise en charge, en UTC.
        ///
        /// C'est la garde contre le double envoi : posée AVANT d'envoyer, elle
        /// empêche un redémarrage pendant l'envoi de relancer le même créneau.
        /// </summary>
        public DateTime? DerniereOccurrence { get; set; }

        /// <summary>Le dernier envoi réel — programmé, ou lancé à la main.</summary>
        public DateTime? DernierEnvoiLe { get; set; }

        /// <summary>« 12 envoyés, 0 échec », « Envoi manqué »… pour l'écran.</summary>
        public string? DernierResultat { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Affichée comme « Enregistré à 14:32 » après chaque sauvegarde.</summary>
        public DateTime? DateModification { get; set; }

        public virtual ICollection<PieceModeleMail> Pieces { get; set; } = new List<PieceModeleMail>();
    }
}
