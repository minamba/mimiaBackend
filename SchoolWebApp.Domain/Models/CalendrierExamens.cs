namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES DATES OFFICIELLES DES ÉPREUVES — Camara, le 25/11/2026 : « récupérer les dates des sessions officielles
    /// (bac, brevet…) et un système automatique comme pour les programmes », formule B : la veille repère un
    /// changement du calendrier officiel, l'API le lit et PROPOSE les nouvelles dates, Camara les VALIDE (onglet
    /// « Sessions d'examens », avec les liens des sources) avant que les familles les voient.
    /// </summary>
    public static class CalendrierExamens
    {
        public const string Validee = "VALIDEE";
        public const string Proposee = "PROPOSEE";

        /// <summary>Un jour, une heure.</summary>
        public const string Jour = "JOUR";

        /// <summary>Le 16 OU le 17 juin, selon la convocation (spécialités du bac général).</summary>
        public const string UnDesDeux = "UN_DES_DEUX";

        /// <summary>Une période (le grand oral, l'oral de français).</summary>
        public const string Periode = "PERIODE";

        public static readonly IReadOnlySet<string> Natures = new HashSet<string> { Jour, UnDesDeux, Periode };

        /// <summary>
        /// Le « matière » des pages que la veille surveille pour le calendrier (EcheanceReferentiel) : c'est à ce nom que
        /// le worker reconnaît une page dont le changement doit produire des propositions de dates.
        /// </summary>
        public const string LibelleVeille = "Calendrier des examens";
    }

    /// <summary>La date officielle d'une épreuve : validée (visible des familles) ou proposée (en attente).</summary>
    public class DateEpreuveOfficielle
    {
        public int Id { get; set; }

        public string EpreuveCode { get; set; } = string.Empty;

        public string Etat { get; set; } = CalendrierExamens.Validee;

        public string Nature { get; set; } = CalendrierExamens.Jour;

        public DateTime DateEpreuve { get; set; }

        public DateTime? DateFin { get; set; }

        public TimeSpan? HeureDebut { get; set; }

        public TimeSpan? HeureFin { get; set; }

        public DateTime? RemplacementLe { get; set; }

        public string SourceLibelle { get; set; } = string.Empty;

        public string SourceUrl { get; set; } = string.Empty;

        public string? AnnexeUrl { get; set; }

        public string? Extrait { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? ProposeLe { get; set; }

        public DateTime? ValideLe { get; set; }

        /// <summary>Même date, même heure, même remplacement : une proposition identique ne se propose pas.</summary>
        public bool MemeDateQue(DateEpreuveOfficielle autre) =>
            Nature == autre.Nature
            && DateEpreuve.Date == autre.DateEpreuve.Date
            && DateFin?.Date == autre.DateFin?.Date
            && HeureDebut == autre.HeureDebut
            && HeureFin == autre.HeureFin
            && RemplacementLe?.Date == autre.RemplacementLe?.Date;
    }

    /// <summary>Une épreuve que Mimia connaît, telle que la veille la cherche dans le calendrier officiel.</summary>
    public record EpreuveCalendrier(string Code, string Libelle, string ExamenCode, string ExamenLibelle, int Session);
}
