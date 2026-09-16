namespace SchoolWebApp.Domain.Models
{
    /// <summary>Les deux natures de modèle de courriel.</summary>
    public static class NatureModeleMail
    {
        /// <summary>Un message qu'on envoie à la main, à tous ou à un parent.</summary>
        public const string Diffusion = "Diffusion";

        /// <summary>Un courriel que le planificateur envoie seul.</summary>
        public const string Automatique = "Automatique";

        public static bool EstConnue(string? nature) => nature is Diffusion or Automatique;
    }

    /// <summary>La cadence d'un courriel automatique.</summary>
    public static class FrequenceEnvoi
    {
        public const string Aucune = "Aucune";
        public const string Jour = "Jour";
        public const string Semaine = "Semaine";
        public const string Mois = "Mois";

        public static bool EstConnue(string? frequence) => frequence is Aucune or Jour or Semaine or Mois;
    }

    /// <summary>Le genre d'une pièce : intégrée au texte, ou jointe.</summary>
    public static class GenrePieceMail
    {
        /// <summary>Placée dans le message par un marqueur `[image:N]`.</summary>
        public const string Image = "Image";

        /// <summary>Téléchargeable depuis le courriel, listée à la fin.</summary>
        public const string Document = "Document";

        public static bool EstConnu(string? genre) => genre is Image or Document;
    }

    /// <summary>
    /// Un modèle de courriel SANS SON TEXTE NI SES OCTETS : ce qu'affiche la
    /// liste des templates.
    /// </summary>
    public class ModeleMailResume
    {
        public int Id { get; set; }

        public string Nature { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Sujet { get; set; } = string.Empty;

        public string Frequence { get; set; } = FrequenceEnvoi.Aucune;

        public TimeOnly? HeureEnvoi { get; set; }

        public int? JourSemaine { get; set; }

        public int? JourMois { get; set; }

        public bool Actif { get; set; }

        public DateTime? DerniereOccurrence { get; set; }

        public DateTime? DernierEnvoiLe { get; set; }

        public string? DernierResultat { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        public int NombreImages { get; set; }

        public int NombreDocuments { get; set; }

        /// <summary>Le poids de toutes les pièces, en octets.</summary>
        public int PoidsTotal { get; set; }
    }

    /// <summary>Un modèle complet, texte compris, pièces sans leurs octets.</summary>
    public class ModeleMailDetail : ModeleMailResume
    {
        public string Titre { get; set; } = string.Empty;

        public string Texte { get; set; } = string.Empty;

        /// <summary>Par rang croissant : `[image:1]` est la première.</summary>
        public List<PieceModeleMailInfo> Images { get; set; } = [];

        public List<PieceModeleMailInfo> Documents { get; set; } = [];
    }

    /// <summary>Ce qu'on sait d'une pièce sans lire ses octets.</summary>
    public class PieceModeleMailInfo
    {
        public int Id { get; set; }

        public string Genre { get; set; } = string.Empty;

        public int Rang { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }
    }

    /// <summary>Une pièce avec ses octets — pour l'envoi et l'aperçu seulement.</summary>
    public class PieceModeleMailContenu : PieceModeleMailInfo
    {
        public byte[] Donnees { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Un fichier reçu, pas encore rangé dans un modèle.</summary>
    public record NouvellePieceMail(string NomFichier, string TypeMime, byte[] Donnees);

    /// <summary>L'issue d'un ajout de pièce.</summary>
    public record ResultatAjoutPiece(PieceModeleMailInfo? Piece, bool Introuvable, bool TropLourd);
}
