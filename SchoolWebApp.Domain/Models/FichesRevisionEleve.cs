namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES FICHES DE RÉVISION DE L'ÉLÈVE — Camara, le 14/10/2026.
    ///
    /// Un module À PART : l'enfant importe son cours (fichier, photo, scan),
    /// choisit une matière et un type, et les fiches sont générées. Rien n'est
    /// transmis aux professeurs, rien n'entre dans le suivi des compétences.
    ///
    /// UN COURS LONG DONNE UNE FICHE PAR NOTION : on révise une chose à la
    /// fois, la fiche reste courte. Sauf l'express (« l'essentiel sur une
    /// page ») et la spéciale contrôle (une évaluation porte sur tout le
    /// chapitre), qui couvrent le cours entier.
    ///
    /// Les mêmes six types que le front (`lib/mesFiches/typesFiches.js`) :
    /// les deux listes se tiennent, un test le vérifie côté front.
    /// </summary>
    public static class FichesRevisionEleve
    {
        /// <summary>Le module coûte de l'argent à chaque fiche : éteint tant que Camara ne l'allume pas.</summary>
        public const string Reglage = "MODULE_FICHES_REVISION";

        public const bool ActifParDefaut = false;

        public sealed record TypeFiche(string Code, string Libelle, bool ParNotion, int RangMin);

        public static readonly IReadOnlyList<TypeFiche> Types =
        [
            new("classique", "Fiche classique", ParNotion: true, RangMin: 1),
            new("express", "Fiche express", ParNotion: false, RangMin: 1),
            new("visuelle", "Fiche visuelle", ParNotion: true, RangMin: 1),
            new("questions", "Fiche questions/réponses", ParNotion: true, RangMin: 1),
            new("controle", "Fiche spéciale contrôle et examens", ParNotion: false, RangMin: 3),
            new("methode", "Fiche méthode", ParNotion: true, RangMin: 3),
        ];

        public static TypeFiche? Type(string? code) =>
            Types.FirstOrDefault(t => string.Equals(t.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

        // LES GARDE-FOUS — réglages acceptés par Camara le 14/10/2026. Un
        // chapitre tient largement en vingt pages ; au-delà, c'est un manuel.

        /// <summary>Pages (photos ou pages de PDF) au plus par import.</summary>
        public const int PagesMax = 20;

        /// <summary>Pièces au plus par import (photos ou fichiers).</summary>
        public const int PiecesMax = 20;

        /// <summary>Notions au plus par import : au-delà, un manuel entier donnerait trente fiches.</summary>
        public const int NotionsMax = 8;

        // L'ESPACE PARENT (16/10/2026) : un parent révise pour lui-même. Il n'a
        // pas de classe ; ses fiches s'écrivent pour un adulte (`ConsignesFiches`).

        /// <summary>Le « rang » d'un adulte : au-delà de la terminale (12).</summary>
        public const int RangAdulte = 13;

        /// <summary>La « classe » d'un adulte, telle que les consignes la reçoivent.</summary>
        public const string ClasseAdulte = "adulte";

        /// <summary>Une catégorie de parent : 60 caractères au plus, 30 catégories au plus.</summary>
        public const int CategorieLibelleMax = 60;

        public const int CategoriesMax = 30;

        /// <summary>Les couleurs proposées pour une catégorie (lisibles en clair comme en sombre).</summary>
        public static readonly string[] CouleursCategories =
            ["#2563EB", "#7C3AED", "#DB2777", "#DC2626", "#EA580C", "#CA8A04", "#16A34A", "#0D9488", "#0891B2", "#475569"];

        /// <summary>Le poids d'un fichier, comme partout ailleurs (`ValidationPieceJointe.TailleMax`).</summary>
        public const int TailleFichierMax = 5 * 1024 * 1024;

        // LES ADMINISTRATEURS SANS LIMITE — Camara, le 16/10/2026 : « quand un
        // admin ou un super admin fait des fiches de révision, il n'y a pas de
        // limite dans la taille du document ni dans le nombre de notions ». Le
        // compte d'un enfant est « admin » quand son PARENT l'est : drapeau
        // `EstAdministrateur` en base, ou super-administrateur.
        //
        // « SANS LIMITE » S'ARRÊTE OÙ LA TECHNIQUE S'ARRÊTE, pas avant. Tout le
        // cours part à Claude en UNE requête de lecture : 32 Mo au plus, encodage
        // base64 compris (+33 %), et 100 pages de PDF au plus. Ces plafonds-là ne
        // sont pas des règles du produit ; les dépasser ferait échouer la lecture.
        // Le quota du mois, lui, ne change pas.

        /// <summary>Pages au plus pour un administrateur : la limite de lecture d'un PDF par Claude.</summary>
        public const int PagesMaxAdmin = 100;

        /// <summary>Pièces au plus pour un administrateur.</summary>
        public const int PiecesMaxAdmin = 100;

        /// <summary>Un fichier au plus, pour un administrateur : 20 Mo, soit 27 en base64.</summary>
        public const int TailleFichierMaxAdmin = 20 * 1024 * 1024;

        /// <summary>Le cours entier au plus, pour un administrateur : 22 Mo, soit 29 en base64, sous les 32 de la requête.</summary>
        public const int TailleCoursMaxAdmin = 22 * 1024 * 1024;

        /// <summary>Notions au plus pour un administrateur : « toutes les notions du cours », bornées pour que la réponse tienne.</summary>
        public const int NotionsMaxAdmin = 40;

        /// <summary>Les plafonds d'un import, selon que la famille est administratrice ou non.</summary>
        public sealed record LimitesImport(
            bool SansLimite, int PagesMax, int PiecesMax, int TailleFichierMax, int? TailleCoursMax, int NotionsMax);

        public static readonly LimitesImport LimitesStandard = new(
            false, PagesMax, PiecesMax, TailleFichierMax, null, NotionsMax);

        public static readonly LimitesImport LimitesAdministrateur = new(
            true, PagesMaxAdmin, PiecesMaxAdmin, TailleFichierMaxAdmin, TailleCoursMaxAdmin, NotionsMaxAdmin);

        public static LimitesImport Limites(bool sansLimite) => sansLimite ? LimitesAdministrateur : LimitesStandard;

        /// <summary>
        /// LE QUOTA, EN COURS IMPORTÉS — Camara, le 16/10/2026 : « quand on
        /// importe un cours, même s'il est divisé en plusieurs sous-fiches, ça
        /// compte pour 1 ». Dix cours par mois et par enfant, remis à zéro
        /// avec les heures (la période de l'abonnement de la famille ; le mois
        /// civil sans abonnement). Au-delà, les crédits de la famille
        /// (`CreditsFiches`) : 1 crédit = 1 cours, quel que soit le nombre de
        /// fiches qu'il donne.
        ///
        /// Un cours n'est DÉCOMPTÉ qu'une fois ses fiches prêtes (raté = rien),
        /// mais il RÉSERVE sa place dès qu'il part à la lecture : on ne peut
        /// pas avoir plus de cours en route que de cours restants. C'est ce qui
        /// remplace l'ancien plafond de 30 imports, qui aurait bloqué les
        /// enfants dont les parents ont acheté des crédits.
        ///
        /// DIX, PLUS VINGT — Camara, le 16/10/2026 : « mets 10 cours par mois ;
        /// ça peut être un bon levier pour avoir pas mal de revenu si ça plaît ».
        /// Au-delà, les crédits achetés par le parent.
        /// </summary>
        public const int CoursParMois = 10;

        /// <summary>
        /// UN GROS COURS COMPTE DOUBLE — Camara, le 16/10/2026. Au-delà de ce
        /// nombre de pages, le cours compte pour 2 dans le quota (et prend 2
        /// crédits). Mesuré le même jour : un PDF de 20 pages coûte 0,27 $ de
        /// génération contre 0,13 $ pour 6 pages — le coût suit le poids des
        /// pages, relues à chaque fiche. Les 20 pages restent possibles, mais
        /// un mois de gros cours tient en 10 cours.
        /// </summary>
        public const int PagesCoursSimple = 10;

        /// <summary>Ce que compte un cours de tant de pages : 1, ou 2 au-delà de PagesCoursSimple.</summary>
        public static int Poids(int pages) => pages > PagesCoursSimple ? 2 : 1;

        /// <summary>
        /// LE COURS N'EST PAS GARDÉ — Camara, le 15/10/2026 : « le cours on ne le
        /// sauvegarde pas, il doit être supprimé après la création des fiches ».
        /// Ses pages sont effacées dès que ses fiches sont prêtes (ou que la
        /// lecture échoue). Seul cas d'attente : l'enfant doit choisir ses
        /// notions ; s'il ne l'a pas fait au bout de ce délai, les pages partent
        /// aussi, et il réimportera son cours.
        /// </summary>
        public const int HeuresChoixDesNotions = 24;

        /// <summary>Un brouillon jamais envoyé est effacé au bout de ce temps.</summary>
        public const int HeuresConservationBrouillon = 24;

        /// <summary>États d'un cours importé.</summary>
        public const string EtatBrouillon = "brouillon";
        public const string EtatALire = "a_lire";
        public const string EtatNotions = "notions";
        public const string EtatAGenerer = "a_generer";
        public const string EtatPret = "pret";
        public const string EtatEchec = "echec";
    }
}
