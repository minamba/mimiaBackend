namespace SchoolWebApp.Dal.Entities
{
    /// <summary>Un mot ou une syllabe passé au banc des voix de synthèse. Voir <c>ReglesSonsVerifies</c>.</summary>
    public partial class MotVerifie
    {
        /// <summary>En minuscules, lettres seules, 20 au plus : la forme de <c>RegleVerificationSon</c>.</summary>
        public string Mot { get; set; } = null!;

        /// <summary>Voir <c>LanguesDesSons</c> : un résultat vaut pour une langue.</summary>
        public string Langue { get; set; } = "fr";

        public int Justes { get; set; }

        public int Total { get; set; }

        public DateTime DateTest { get; set; }
    }
}
