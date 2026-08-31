namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// Réglages de la synthèse vocale.
    ///
    /// La clé ne figure jamais dans appsettings.json versionné : elle vient
    /// d'appsettings.Development.json en local et d'une variable
    /// d'environnement en production.
    /// </summary>
    public class OptionsVoix
    {
        public const string Section = "Voix";

        public string? ApiKey { get; set; }

        /// <summary>
        /// gpt-4o-mini-tts est le modèle utilisé par le mode vocal de ChatGPT.
        ///
        /// LE DÉBIT SE PILOTE PAR LA CONSIGNE DE JEU (<c>instructions</c>), qui
        /// donne un résultat bien plus naturel qu un ralentissement mécanique.
        ///
        /// CORRECTION D UNE AFFIRMATION QUI ÉTAIT FAUSSE ICI : ce modèle
        /// n ignore PAS `speed`. Mesuré sur la même phrase — 6,40 s sans
        /// paramètre, 6,51 s à 0,75, 7,73 s à 0,5, 23,82 s à 0,25. Il est donc
        /// quasiment sourd au-dessus de 0,75, et brutal en dessous de 0,5 : la
        /// consigne de jeu reste le bon levier, mais pour la raison inverse de
        /// celle qui était écrite.
        ///
        /// Et la consigne ne fait pas tout : demandée d allonger les silences,
        /// elle n allonge la phrase que de 1,43× et ne pose aucun vrai blanc.
        /// Un modèle de parole comprime les silences — il est fait pour parler.
        /// Les blancs d une dictée sont donc programmés côté front, sur
        /// l horloge audio.
        /// </summary>
        public string Modele { get; set; } = "gpt-4o-mini-tts";

        /// <summary>
        /// Longueur maximale d'une requête. Chaque appel est facturé : sans
        /// plafond, un compte compromis pourrait générer des heures d'audio.
        /// Une phrase de professeur dépasse rarement 200 caractères.
        /// </summary>
        public int LongueurMax { get; set; } = 800;

        // ------------------------------------------------ transcription

        /// <summary>
        /// Modèle de transcription en flux. Mesuré à 0,003 $/minute d'audio,
        /// soit une part négligeable du coût d'une séance — à condition de ne
        /// diffuser que le son utile : l'élève ne parle que 5 % du temps.
        /// </summary>
        public string ModeleTranscription { get; set; } = "gpt-4o-mini-transcribe";

        /// <summary>
        /// Langue imposée à la transcription, en code ISO 639-1.
        ///
        /// Sans elle, le modèle détecte la langue à chaque tour — et sur un mot
        /// isolé il se trompe : « deux » revenait transcrit en chinois, « bravo »
        /// en arabe. Un élève français parle français, il n'y a rien à deviner.
        ///
        /// SAUF EN COURS DE LANGUE, où il parle les deux. C'est
        /// VocabulaireTranscription.Langue qui tranche, matière par matière, et
        /// cette valeur n'est plus lue que comme repli historique.
        /// </summary>
        public string LangueTranscription { get; set; } = "fr";


        /// <summary>
        /// Point d'entrée temps réel d'OpenAI.
        ///
        /// `intent=transcription` ouvre une session de transcription seule, sans
        /// modèle de conversation : c'est ce qu'on veut, le raisonnement reste
        /// chez Claude. Sans ce paramètre, le service attend un modèle de
        /// dialogue et refuse la configuration.
        /// </summary>
        public string UrlTempsReel { get; set; } =
            "wss://api.openai.com/v1/realtime?intent=transcription";

        /// <summary>
        /// Fréquence d'échantillonnage de l'audio envoyé, en hertz.
        ///
        /// Le navigateur rééchantillonne à cette valeur avant d'émettre : le
        /// micro tourne souvent à 48 kHz, ce qui doublerait le volume transmis
        /// sans rien apporter à la reconnaissance de la parole.
        /// </summary>
        public int FrequenceAudio { get; set; } = 24000;

        /// <summary>
        /// Plafond d'audio par séance, en minutes. Garde-fou de facturation :
        /// un onglet laissé ouvert avec un micro qui capte du bruit ne doit pas
        /// pouvoir diffuser indéfiniment.
        /// </summary>
        public int MinutesAudioMax { get; set; } = 60;

        /// <summary>La transcription serveur est-elle active ?</summary>
        public bool TranscriptionActive { get; set; } = true;

        /// <summary>
        /// Comment on décide que l'élève a fini de parler.
        ///
        /// `semantic_vad` juge d'après LES MOTS prononcés, pas d'après le
        /// silence. C'est la différence qui compte ici : un enfant qui
        /// réfléchit dit « euh… hmm… », se tait deux secondes, puis reprend.
        /// Une détection à l'énergie prend ce silence pour une fin de phrase et
        /// le professeur lui coupe sa réflexion.
        ///
        /// `server_vad` reste disponible si la détection sémantique se révélait
        /// trop lente : elle attend, elle, un silence de durée fixe.
        /// </summary>
        public string TypeDetectionTour { get; set; } = "semantic_vad";

        /// <summary>
        /// Empressement de la détection sémantique : `low`, `medium`, `high`
        /// ou `auto`.
        ///
        /// `low` a été essayé — le plus patient — et il attendait trop : une
        /// phrase terminée restait sans réponse tant que l'élève ne reprenait
        /// pas la parole. `medium` referme le tour dans un délai normal.
        ///
        /// La patience qu'on cherchait est obtenue autrement, et mieux : le
        /// filtre d'hésitations écarte les « euh » et les « hmm » sans toucher
        /// à la détection de fin de tour. Un enfant peut donc réfléchir à voix
        /// haute sans se faire couper, ET obtenir une réponse quand il a fini.
        /// </summary>
        public string EmpressementTour { get; set; } = "medium";

        /// <summary>
        /// Silence exigé avant de clore un tour, en millisecondes. N'a d'effet
        /// qu'avec `server_vad`. Le défaut du fournisseur est de 500 ms, bien
        /// trop court pour un enfant qui pose un calcul dans sa tête.
        /// </summary>
        public int SilenceFinDeTourMs { get; set; } = 1800;
    }
}
