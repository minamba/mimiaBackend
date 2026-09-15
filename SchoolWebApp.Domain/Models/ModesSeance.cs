namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// D'OÙ VIENT L'ÉLÈVE — et donc de quoi son professeur a le droit de lui
    /// parler.
    ///
    /// Voulu par Camara le 14/09/2026 : « la préparation passe uniquement
    /// lorsque l'enfant clique sur préparer ou continuer la préparation, au
    /// niveau des cartes examen ou contrôle. En passant par les cartes de cours
    /// traditionnel, l'élève fera des cours normaux. Si je viens pour me
    /// préparer à un contrôle, ne me parle jamais d'examen de fin d'année ; si
    /// je viens d'un bouton d'examen, ne parle jamais du contrôle prévu la
    /// semaine prochaine. Le professeur sait d'où vient l'élève et sait de quoi
    /// il va parler. »
    ///
    /// CE QUI EST CLOISONNÉ, C'EST LA PAROLE — PAS LA MESURE. Un cours normal
    /// sur Pythagore fait monter la barre du contrôle et celle du brevet : c'est
    /// la même compétence, mesurée une seule fois. Seul le professeur se tait
    /// sur ce qui n'est pas le sujet de la séance.
    ///
    /// Le mode est posé à l'arrivée, vérifié par le serveur — un contrôle qui
    /// n'est pas à cet élève, une épreuve qui ne le concerne pas, ramènent au
    /// cours normal — puis relu à chaque tour.
    /// </summary>
    public static class ModesSeance
    {
        /// <summary>Entré par la carte de sa matière : un cours ordinaire.</summary>
        public const string Cours = "cours";

        /// <summary>Entré par « Commencer / Continuer la préparation » d'un contrôle.</summary>
        public const string Controle = "controle";

        /// <summary>Entré par « Faire le point sur ce contrôle », une fois passé.</summary>
        public const string Bilan = "bilan";

        /// <summary>Entré par la préparation d'une épreuve d'examen (brevet, bac…).</summary>
        public const string Examen = "examen";

        /// <summary>Tout ce qui n'est pas reconnu est un cours normal : le cloisonnement le plus sûr.</summary>
        public static string Normaliser(string? mode) => (mode ?? "").Trim().ToLowerInvariant() switch
        {
            Controle => Controle,
            Bilan => Bilan,
            Examen => Examen,
            _ => Cours,
        };
    }
}
