namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Nature de la tâche demandée à l'agent. C'est elle qui décide du modèle :
    /// le dialogue courant tourne sur Sonnet, seules les tâches réellement
    /// difficiles justifient Opus.
    /// </summary>
    public enum TypeTache
    {
        /// <summary>Échange pédagogique courant — l'écrasante majorité des appels.</summary>
        Dialogue,

        /// <summary>
        /// Raisonnement lourd : diagnostic initial, correction d'un exercice
        /// multi-étapes, analyse d'une copie. Cas minoritaires mais où la
        /// qualité du raisonnement change le résultat.
        /// </summary>
        Complexe
    }

    /// <summary>
    /// Configuration Claude, section "Claude" de appsettings.
    /// Modifiable sans recompiler — utile pour arbitrer coût/qualité en production.
    /// </summary>
    public class OptionsClaude
    {
        public const string Section = "Claude";

        /// <summary>Modèle du dialogue courant.</summary>
        public string ModeleDialogue { get; set; } = "claude-sonnet-5";

        /// <summary>Modèle des tâches à fort raisonnement.</summary>
        public string ModeleComplexe { get; set; } = "claude-opus-5";

        /// <summary>
        /// Modèle de la transcription des documents.
        ///
        /// Lire ce qui est écrit sur une feuille ne demande aucun raisonnement
        /// pédagogique : c'est une tâche mécanique, et la faire tourner sur le
        /// modèle du dialogue serait payer une réflexion dont on n'a aucun
        /// usage. Haiku suffit et coûte environ un tiers.
        /// </summary>
        public string ModeleTranscription { get; set; } = "claude-haiku-4-5";

        /// <summary>
        /// Combien de jours on garde les octets d'un document.
        ///
        /// Au-delà, seule la transcription subsiste. Le document est alors de
        /// toute façon sorti de la fenêtre d'historique et n'est plus jamais
        /// envoyé au modèle. Le conserver douze mois ne servirait qu'à faire
        /// grossir la base — et à garder la photo de l'écriture d'un enfant
        /// bien après qu'elle ait servi.
        ///
        /// RAMENÉ DE SEPT JOURS À TROIS, ET LA MESURE COMMANDE.
        ///
        /// La base tourne sur SQL Server Express — vérifié, pas supposé :
        /// `SERVERPROPERTY('Edition')` rend « Express Edition (64-bit) ». Dix
        /// gigaoctets, et une base pleine n'accepte plus une seule écriture :
        /// ce n'est pas une dégradation, c'est l'application entière qui
        /// s'arrête. Le volume résident vaut « dépôts par jour × jours de
        /// rétention » ; c'est le seul facteur qu'on puisse changer d'une
        /// ligne, et il double presque la marge.
        ///
        /// TROIS JOURS SUFFISENT, ET CE N'EST PAS UN PARI. La fenêtre
        /// d'historique envoyée au professeur se compte en MESSAGES, pas en
        /// jours — voir `PlancherHistorique` et `PasHistorique`. Un document
        /// vieux de trois jours en est sorti depuis longtemps : ses octets ne
        /// partent plus vers le modèle, ils occupent de la place. Sept jours
        /// étaient un confort, pas un besoin.
        ///
        /// Ce qui change pour un élève : rouvrir une conversation de plus de
        /// trois jours n'affiche plus la photo, mais son texte relevé. La
        /// professeure sait toujours sur quoi on avait travaillé.
        /// </summary>
        public int JoursConservationDocuments { get; set; } = 3;

        /// <summary>low | medium | high | xhigh | max</summary>
        public string EffortDialogue { get; set; } = "medium";

        public string EffortComplexe { get; set; } = "high";

        /// <summary>
        /// Plafond de sortie. Il couvre la réflexion ET le texte de la réponse :
        /// la réflexion adaptative est active par défaut sur Sonnet 5, donc un
        /// plafond trop bas tronque la réponse en plein milieu.
        /// </summary>
        public int MaxTokensDialogue { get; set; } = 8192;

        public int MaxTokensComplexe { get; set; } = 16000;

        public string Modele(TypeTache tache) =>
            tache == TypeTache.Complexe ? ModeleComplexe : ModeleDialogue;

        public string Effort(TypeTache tache) =>
            tache == TypeTache.Complexe ? EffortComplexe : EffortDialogue;

        public int MaxTokens(TypeTache tache) =>
            tache == TypeTache.Complexe ? MaxTokensComplexe : MaxTokensDialogue;
    }
}
