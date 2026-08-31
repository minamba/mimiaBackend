namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Ce que l'oreille d'un enfant a réellement attendu avant d'entendre le
    /// professeur.
    ///
    /// POURQUOI CETTE TABLE EXISTE
    /// ---------------------------
    /// Le délai « texte affiché → première syllabe » était déjà mesuré, mais il
    /// ne sortait pas du navigateur : un `console.info` coupé en production.
    /// Autrement dit, la seule question qu'on se pose vraiment — « est-ce que ça
    /// paraît lent chez eux ? » — n'avait aucune réponse chez eux, précisément.
    ///
    /// Le repli compte autant que le délai. Quand la synthèse serveur échoue, la
    /// voix du navigateur prend le relais : elle sonne robotique, et c'est
    /// exactement le reproche qu'on cherche à éviter. Sans cette colonne, un
    /// parent dirait « la voix fait robot » et on chercherait dans le modèle de
    /// synthèse, alors que le vrai coupable serait un serveur qui a flanché.
    ///
    /// CE QU'ELLE NE CONTIENT PAS
    /// --------------------------
    /// Ni élève, ni parent, ni conversation, ni matière. Rien qui remonte à
    /// quelqu'un, et rien à effacer quand un compte est supprimé — la question
    /// ne se pose donc jamais.
    ///
    /// <see cref="Seance"/> est un identifiant tiré au sort par le navigateur à
    /// l'ouverture du cours. Il sert à regrouper les mesures d'une même séance
    /// pour en calculer une médiane, et il ne survit pas à la fermeture de
    /// l'onglet. Deux séances du même enfant portent deux identifiants
    /// différents et rien ne permet de les rapprocher.
    /// </summary>
    public partial class MesureVoix
    {
        public int Id { get; set; }

        /// <summary>
        /// Tirage au sort du navigateur, le temps d'une séance. Ne désigne
        /// personne et ne se réutilise pas.
        /// </summary>
        public string Seance { get; set; } = string.Empty;

        /// <summary>
        /// Millisecondes entre « le professeur a quelque chose à dire » et la
        /// première syllabe entendue.
        /// </summary>
        public int DelaiMs { get; set; }

        /// <summary>
        /// Du silence détecté au texte final reçu du service de transcription.
        /// </summary>
        public int? TranscriptionMs { get; set; }

        /// <summary>
        /// L attente VOLONTAIRE avant d envoyer le tour : 150 ms d ordinaire,
        /// 2500 quand la phrase semble inachevée ou que le professeur venait
        /// de demander une justification. C est le maillon qu on soupçonne.
        /// </summary>
        public int? AssemblageMs { get; set; }

        /// <summary>
        /// Du message envoyé au PREMIER caractère de la réponse du modèle.
        /// </summary>
        public int? ReponseMs { get; set; }

        /// <summary>
        /// Vrai si ce passage a été prononcé par la voix du navigateur, la
        /// synthèse serveur ayant échoué.
        /// </summary>
        public bool Repli { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
