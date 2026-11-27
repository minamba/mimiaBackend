namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES EXAMENS BLANCS — Camara, le 23/11/2026 : « un bouton Brevet blanc / Bac blanc selon la classe ; l'enfant
    /// choisit sa matière et envoie tous ses documents ; le professeur en tire les notions, les range en dossiers,
    /// propose un programme au tableau ; une page comme la préparation aux examens ; les documents s'effacent deux
    /// mois après l'examen blanc ».
    ///
    /// UN EXAMEN BLANC = UNE DATE, ET SES MATIÈRES. Une matière ajoutée pour une date où l'élève en a déjà un rejoint
    /// celui-là : la carte « Brevet blanc » regroupe ses matières, comme la préparation au brevet ses épreuves.
    ///
    /// ÉTAPE 1 (ce fichier) : la date, les matières, les documents. La lecture des documents, les notions, le
    /// programme et le point d'après viennent ensuite.
    /// </summary>
    public static class ExamensBlancs
    {
        public const string Brevet = "BREVET";
        public const string Bac = "BAC";
        public const string BacPro = "BAC_PRO";

        /// <summary>Vingt documents par matière (Camara : « un élève n'atteindra jamais cette limite »).</summary>
        public const int DocumentsMax = 20;

        /// <summary>
        /// AUCUNE LIMITE DE PAGES NI DE TAILLE propre aux examens blancs (Camara, le 23/11/2026 : « un enfant bloqué
        /// parce que son document est trop gros, je pourrais perdre des clients »). Seul ce plafond technique demeure,
        /// pour qu'un fichier de plusieurs gigaoctets ne fasse pas tomber le serveur pendant l'envoi.
        /// </summary>
        public const int TailleMax = 200 * 1024 * 1024; // 200 Mo depuis le 25/11/2026 (Camara), 100 avant

        /// <summary>Les fichiers s'effacent deux mois après la date de l'examen blanc.</summary>
        public const int MoisConservation = 2;

        /// <summary>
        /// Le type d'examen blanc d'une classe, ou null si elle n'en a pas : la 3e (prépa-métiers comprise) passe le
        /// brevet, la 1re et la terminale générales et technologiques le bac, les professionnelles le bac pro.
        /// </summary>
        public static string? TypeDe(string? niveauCode)
        {
            if (string.IsNullOrWhiteSpace(niveauCode)) return null;
            var code = niveauCode.Trim().ToUpperInvariant();

            if (code is "TROISIEME" or "TROISIEME_PREPA") return Brevet;
            if (code is "PREMIERE_PRO" or "TERMINALE_PRO") return BacPro;
            if (code.StartsWith("PREMIERE", StringComparison.Ordinal) || code.StartsWith("TERMINALE", StringComparison.Ordinal)) return Bac;
            return null;
        }

        public static string Libelle(string? type) => type switch
        {
            Brevet => "Brevet blanc",
            BacPro => "Bac pro blanc",
            _ => "Bac blanc",
        };

        /// <summary>Trois lectures ratées, et le document est marqué illisible : comme les cours de contrôle.</summary>
        public const int LecturesMax = 3;

        /// <summary>
        /// LE RANGEMENT ATTEND QUE L'ENFANT AIT FINI D'ENVOYER : une minute sans nouveau document. Sans ce délai,
        /// chaque photo d'une série de vingt relancerait le rangement (un appel au modèle à chaque fois).
        /// </summary>
        public static readonly TimeSpan AttenteAvantRangement = TimeSpan.FromMinutes(1);

        /// <summary>Le jour à partir duquel les fichiers d'un examen blanc de cette date peuvent être effacés.</summary>
        public static DateTime EffacementLe(DateTime dateExamen) => dateExamen.Date.AddMonths(MoisConservation);
    }

    /// <summary>Un examen blanc de l'élève, avec ses matières.</summary>
    public class ExamenBlanc
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public string Type { get; set; } = ExamensBlancs.Brevet;

        public string Libelle => ExamensBlancs.Libelle(Type);

        public DateTime DateExamen { get; set; }

        public TimeSpan? HeureExamen { get; set; }

        public DateTime DateCreation { get; set; }

        public List<ExamenBlancMatiere> Matieres { get; set; } = [];
    }

    /// <summary>Une matière d'un examen blanc : son professeur, ses documents.</summary>
    public class ExamenBlancMatiere
    {
        public int Id { get; set; }

        public int ExamenBlancId { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DocumentsModifiesLe { get; set; }

        public DateTime? NotionsLe { get; set; }

        /// <summary>Le contrôle caché de cette matière (étape 3) : sa préparation est celle d'un contrôle.</summary>
        public int? ControleId { get; set; }

        /// <summary>Quand l'élève a dit d'accord au programme proposé au tableau ; null tant que non.</summary>
        public DateTime? ProgrammeValideLe { get; set; }

        /// <summary>La préparation du contrôle caché (pourcentage, notions, verdict) ; posée par le service.</summary>
        public PreparationControle? Preparation { get; set; }

        public List<DocumentExamenBlanc> Documents { get; set; } = [];

        /// <summary>Les dossiers (étape 2) : une notion tirée des documents, et ceux qui en parlent.</summary>
        public List<NotionExamenBlanc> Notions { get; set; } = [];

        /// <summary>
        /// LES DOSSIERS SONT-ILS À JOUR ? Faux tant qu'un document attend d'être lu, ou que les documents ont changé
        /// depuis le dernier rangement : l'écran dit alors « ton professeur range tes documents… ».
        /// </summary>
        public bool DossiersAJour =>
            Documents.Count == 0
                ? Notions.Count == 0
                : Documents.All(d => d.Lu || !d.Consultable)
                    && NotionsLe is DateTime le
                    && (DocumentsModifiesLe is null || DocumentsModifiesLe <= le);
    }

    /// <summary>Un dossier : une notion, rattachée au référentiel si elle y est, et les documents qui en parlent.</summary>
    public class NotionExamenBlanc
    {
        public int Id { get; set; }

        public int? CompetenceId { get; set; }

        public string Libelle { get; set; } = string.Empty;

        public List<int> DocumentIds { get; set; } = [];
    }

    /// <summary>Un document à lire en tâche de fond (photo, PDF), avec de quoi retrouver son fichier.</summary>
    public record DocumentALire(int Id, int EleveId, int ExamenBlancId, string Chemin, string TypeMime, string NomFichier);

    /// <summary>Une matière dont les dossiers sont à (re)faire : tous ses documents sont lus.</summary>
    public record MatiereANotionner(
        int LigneId, int MatiereId, string? MatiereLibelle, int? NiveauScolaireId,
        IReadOnlyList<(int Id, string Nom, string Texte)> Documents);

    /// <summary>Un dossier tel que le professeur le propose : une notion et ses documents.</summary>
    public record NotionProposee(int? CompetenceId, string Libelle, IReadOnlyList<int> DocumentIds);

    /// <summary>Ce qu'il faut effacer après le retrait d'une matière ou d'un examen blanc : fichiers et contrôles cachés.</summary>
    public record RestesExamenBlanc(IReadOnlyList<string> Chemins, IReadOnlyList<int> Controles);

    /// <summary>
    /// Le résultat d'un changement de date (25/11/2026) : refusé si un autre examen blanc tombe déjà ce jour-là, sinon
    /// les contrôles cachés de ses matières, à déplacer à la nouvelle date.
    /// </summary>
    public record ModificationExamenBlanc(bool DateDejaPrise, IReadOnlyList<int> Controles)
    {
        public static readonly ModificationExamenBlanc Prise = new(true, []);
    }

    /// <summary>
    /// L'examen blanc vu depuis la séance de préparation de son contrôle caché (étape 3) : de quoi dire au professeur
    /// que c'est un brevet blanc, lui donner les dossiers et les documents, et savoir si le programme est validé.
    /// </summary>
    public record ExamenBlancPourSeance(
        int ExamenBlancId, int LigneId, string Type, DateTime DateExamen, string? MatiereLibelle,
        DateTime? ProgrammeValideLe,
        IReadOnlyList<NotionExamenBlanc> Notions,
        IReadOnlyList<(int Id, string Nom, string? Texte)> Documents)
    {
        public string Libelle => ExamensBlancs.Libelle(Type);
    }

    /// <summary>
    /// Un document envoyé pour une matière d'un examen blanc. Même forme qu'un document de contrôle
    /// (<see cref="DocumentControle"/>) : l'aperçu de l'élève est le même.
    /// </summary>
    public class DocumentExamenBlanc
    {
        public int Id { get; set; }

        public int ExamenBlancMatiereId { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        /// <summary>Le type LU dans les octets, jamais celui annoncé par le navigateur.</summary>
        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        /// <summary>Le chemin du fichier sous le dossier des examens blancs ; null une fois effacé.</summary>
        public string? Chemin { get; set; }

        public DateTime? FichierEffaceLe { get; set; }

        /// <summary>Le texte lu : extrait sans modèle pour Word, Excel et PowerPoint ; null tant qu'il n'est pas lu.</summary>
        public string? Transcription { get; set; }

        public int Tentatives { get; set; }

        public DateTime DateCreation { get; set; }

        public bool Consultable => Chemin is not null;

        public bool Purge => Chemin is null && FichierEffaceLe is not null;

        public bool Lu => Transcription is not null;

        public string Genre => DocumentControle.GenreDe(TypeMime);
    }
}
