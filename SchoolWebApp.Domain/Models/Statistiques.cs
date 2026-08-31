namespace SchoolWebApp.Domain.Models
{
    public enum Granularite
    {
        /// <summary>Découpage horaire — sert à détailler une seule journée.</summary>
        Heure,
        Jour,
        Semaine,
        Mois,
        Annee
    }

    /// <summary>Un point d'une série temporelle.</summary>
    /// <summary>
    /// Les abonnements sur une période : un stock et deux flux.
    ///
    /// POURQUOI LES TROIS VOYAGENT ENSEMBLE
    /// -----------------------------------
    /// Ils répondent à une seule question — « où en est le parc d'abonnés ? » —
    /// et se lisent l'un par rapport à l'autre. Trois routes séparées feraient
    /// trois allers-retours pour un seul écran, et ouvriraient la porte à des
    /// séries désynchronisées quand un filtre change.
    /// </summary>
    public class PointAbonnements
    {
        public DateTime Periode { get; set; }

        /// <summary>
        /// Abonnements en cours à la FIN de la période. Un stock, pas un flux :
        /// un abonnement compte dans toutes les périodes qu'il traverse.
        /// </summary>
        public int Actifs { get; set; }

        /// <summary>
        /// Abonnements COMMENCÉS pendant la période.
        ///
        /// Un flux, à ne pas confondre avec `Actifs` juste au-dessus. Les deux
        /// répondent à des questions différentes : celui-ci dit combien de
        /// familles sont arrivées, l'autre combien sont là. Un mois peut très
        /// bien en gagner cinq et en perdre six.
        /// </summary>
        public int Nouveaux { get; set; }

        /// <summary>Résiliations demandées pendant la période.</summary>
        public int Demandes { get; set; }

        /// <summary>Abonnements réellement arrivés à leur terme pendant la période.</summary>
        public int Arrets { get; set; }
    }

    public class PointSerie
    {
        /// <summary>Début de la période (lundi pour une semaine, 1er du mois, etc.).</summary>
        public DateTime Periode { get; set; }

        public int Valeur { get; set; }

        /// <summary>Total cumulé depuis l'origine — utile pour les inscriptions.</summary>
        public int Cumul { get; set; }
    }

    /// <summary>Chiffres de tête du tableau de bord.</summary>
    public class ResumeAdmin
    {
        public int NombreParents { get; set; }

        public int NombreEleves { get; set; }

        public int NombreConversations { get; set; }

        /// <summary>Messages de l'agent — une requête facturée chacun.</summary>
        public int NombreRequetes { get; set; }

        public int RequetesAujourdhui { get; set; }

        public int RequetesTrenteJours { get; set; }

        public long TokensEntree { get; set; }

        public long TokensSortie { get; set; }

        public long TokensCacheLecture { get; set; }
    }

    /// <summary>Ligne du tableau des comptes parents.</summary>
    public class ParentAdmin
    {
        public int Id { get; set; }

        public string? Prenom { get; set; }

        public string? Nom { get; set; }

        public string? Mail { get; set; }

        public DateTime DateCreation { get; set; }

        public int NombreEleves { get; set; }

        public int NombreRequetes { get; set; }

        public DateTime? DerniereActivite { get; set; }

        /// <summary>
        /// LA CONSOMMATION DE LA PÉRIODE EN COURS, ET POURQUOI ELLE COMPTE.
        ///
        /// Le prix d'une formule est calculé sur un pot d'heures. Tant qu'on
        /// ignore quelle PART de ce pot est réellement consommée, la marge est
        /// une hypothèse : au pot plein elle vaut la moitié de ce qu'elle vaut
        /// à moitié consommé. C'est le seul chiffre qui dise si une formule
        /// gagne ou perd de l'argent, et il ne se devine pas — une famille de
        /// quatre enfants n'épuise pas ses 1 440 minutes, mais de combien ?
        ///
        /// Nulles quand le parent n'a aucun abonnement actif : c'est un état
        /// normal — compte tout neuf, essai terminé, abonnement résilié — et
        /// zéro y serait un mensonge, il se lirait comme « n'utilise rien ».
        /// </summary>
        public string? Formule { get; set; }

        /// <summary>Pot de la période, recharges comprises. En minutes.</summary>
        public int? MinutesPot { get; set; }

        /// <summary>Ce qui en a été consommé, tous enfants confondus.</summary>
        public int? MinutesConsommees { get; set; }

        /// <summary>
        /// CE QUE CETTE FAMILLE A RÉELLEMENT COÛTÉ, EN DOLLARS.
        ///
        /// Calculé sur les jetons ENREGISTRÉS tour par tour, pas sur une
        /// moyenne : chaque message porte son entrée, sa sortie, ses lectures
        /// et écritures de cache, et le modèle qui les a produits. C'est donc
        /// une mesure, et le seul chiffre à mettre en face du prix payé.
        ///
        /// CE QU'IL COMPREND, ET CE QU'IL NE COMPREND PAS
        /// ----------------------------------------------
        /// Il comprend le dialogue — de loin le premier poste — et la synthèse
        /// vocale, ESTIMÉE d'après les minutes consommées : rien ne la mesure
        /// par famille, alors que le dialogue l'est. Les deux sont additionnés
        /// parce que c'est le total qui répond à « suis-je déficitaire », mais
        /// le détail reste lisible au survol.
        ///
        /// Il ne comprend pas les frais Stripe, qui pèsent sur l'encaissement
        /// et non sur l'usage.
        ///
        /// SUR TOUTE LA VIE DU COMPTE, pas sur la période en cours : c'est ce
        /// qu'on veut comparer à ce que la famille a payé depuis son
        /// inscription.
        /// </summary>
        public decimal? CoutDollars { get; set; }

        /// <summary>La part du coût qui vient du dialogue. Mesurée.</summary>
        public decimal? CoutDialogueDollars { get; set; }

        /// <summary>
        /// Ce que la famille paie par mois, en centimes.
        ///
        /// SANS LUI, LE COÛT NE VEUT RIEN DIRE. « 22 $ » est un chiffre nu :
        /// c'est excellent en face de 99 €, catastrophique en face de 39,90 €.
        /// La seule question qui compte est le RAPPORT entre les deux, et c'est
        /// lui qui décide de la couleur affichée.
        ///
        /// Un abonnement annuel est ramené au mois : les coûts se lisent par
        /// mois, et comparer un coût mensuel à un prix annuel ferait croire à
        /// une marge douze fois trop belle.
        /// </summary>
        public int? PrixMensuelCentimes { get; set; }
    }

    /// <summary>
    /// Ce que l'ensemble des familles a coûté sur une période.
    ///
    /// LE CHIFFRE QU'ON REGARDE AVANT DE DORMIR. La colonne du tableau dit ce
    /// qu'une famille a coûté depuis son inscription ; celui-ci dit ce que le
    /// produit brûle en ce moment. Les deux sont nécessaires : le premier
    /// juge une formule, le second juge une trésorerie.
    /// </summary>
    public class CoutPeriode
    {
        public DateTime Debut { get; set; }

        public DateTime Fin { get; set; }

        /// <summary>Mesuré, jeton par jeton.</summary>
        public decimal DialogueDollars { get; set; }

        /// <summary>Estimé d'après le nombre de tours de parole.</summary>
        public decimal VoixDollars { get; set; }

        /// <summary>
        /// Ce que les tâches de fond ont consommé. Mesuré, comme le dialogue.
        ///
        /// Décrire une planche, en cartographier les repères, observer une
        /// séance, transcrire un document : quatre postes qui appellent le
        /// modèle sans appartenir à aucune conversation. Ils dépensaient sans
        /// laisser de trace — une facture qui monte pendant qu'un tableau de
        /// bord reste immobile n'aide personne.
        /// </summary>
        public decimal TachesDeFondDollars { get; set; }

        /// <summary>Le détail par poste, du plus cher au moins cher.</summary>
        public List<PosteCout> Postes { get; set; } = [];

        public decimal TotalDollars { get; set; }

        /// <summary>Tours de parole du professeur sur la période.</summary>
        public int Tours { get; set; }

        /// <summary>
        /// Le temps de cours de la période, en minutes.
        ///
        /// RECALCULÉ, ET PAS RELEVÉ. Le compteur officiel — celui qui décrémente
        /// le forfait — est cumulé par période d'abonnement : il ne se découpe
        /// ni par jour ni par semaine. Pour répondre « combien d'heures cette
        /// semaine », il faut donc refaire le calcul depuis les messages, avec
        /// la règle exacte du décompte : l'écart depuis le tour précédent,
        /// plancher à vingt secondes, et vingt secondes forfaitaires au-delà de
        /// trois minutes de silence.
        ///
        /// IL EST LÉGÈREMENT INFÉRIEUR AU COMPTEUR, d'environ un dixième sur les
        /// données actuelles, et c'est normal : les conversations purgées au
        /// bout d'un an ont emporté leurs messages, pas les minutes déjà
        /// décomptées. Ce chiffre mesure donc le temps de cours ENCORE
        /// TRAÇABLE, ce qui est exactement ce qu'on veut voir par période.
        /// </summary>
        public int MinutesTravaillees { get; set; }
    }

    /// <summary>
    /// Un poste de dépense de fond : lequel, combien d'appels, combien de
    /// dollars. C'est ce détail qui transforme « ma facture monte » en « la
    /// cartographie des repères tourne en boucle ».
    /// </summary>
    public class PosteCout
    {
        public string Origine { get; set; } = string.Empty;

        public int Appels { get; set; }

        public decimal Dollars { get; set; }
    }

    /// <summary>Ligne du tableau des élèves, et options du filtre.</summary>
    public class EleveAdmin
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public string? Prenom { get; set; }

        public string? Nom { get; set; }

        public int Age { get; set; }

        public Sexe Sexe { get; set; }

        public string? NiveauLibelle { get; set; }

        public string? ParentMail { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DerniereActivite { get; set; }

        public int NombreRequetes { get; set; }
    }

    /// <summary>
    /// La place occupée par la base, et ce qui la fait grossir.
    ///
    /// POURQUOI CET ÉCRAN EXISTE
    /// -------------------------
    /// SQL Server Express plafonne à dix gigaoctets — vérifié en production,
    /// pas supposé. Une base pleine n'accepte plus une seule écriture : ce
    /// n'est pas une lenteur, c'est l'application entière qui s'arrête, sans
    /// prévenir et un dimanche soir aussi bien qu'un mardi.
    ///
    /// Le chiffre ne s'obtenait qu'en ouvrant un client SQL sur la production.
    /// Personne ne fait ça toutes les semaines, et c'est exactement le genre de
    /// mur qu'on ne voit qu'une fois dedans.
    /// </summary>
    public class EtatBase
    {
        /// <summary>
        /// Ce que rend `SERVERPROPERTY('Edition')`.
        ///
        /// AFFICHÉ TEL QUEL, ET NON INTERPRÉTÉ. Le plafond de dix gigaoctets
        /// n'existe que sur Express : le déduire au lieu de le lire ferait
        /// afficher une jauge fausse le jour d'une migration vers Standard.
        /// </summary>
        public string Edition { get; set; } = string.Empty;

        /// <summary>Le fichier de données. C'est LUI que le plafond borne.</summary>
        public int DonneesMo { get; set; }

        /// <summary>
        /// Le journal des transactions.
        ///
        /// COMPTÉ À PART, PARCE QU'IL N'ENTRE PAS DANS LE PLAFOND. Il occupe du
        /// disque, souvent plusieurs fois les données, mais Express ne borne
        /// que le fichier de données. Les additionner ferait paniquer pour
        /// rien — ou rassurer à tort.
        /// </summary>
        public int JournalMo { get; set; }

        /// <summary>Le plafond de l'édition, ou null si elle n'en a pas.</summary>
        public int? PlafondMo { get; set; }

        /// <summary>
        /// Les octets des documents encore consultables.
        ///
        /// LE SEUL POSTE QU'ON PILOTE. Le reste — messages, fiches, rapports —
        /// croît avec l'usage et ne se règle pas. Celui-ci vaut « dépôts par
        /// jour × jours de rétention », et la rétention est une valeur de
        /// configuration. C'est donc ce chiffre-là qu'on regarde quand la base
        /// grossit, avant de toucher à quoi que ce soit d'autre.
        /// </summary>
        public int DocumentsMo { get; set; }

        /// <summary>Combien de documents portent encore leurs octets.</summary>
        public int DocumentsNombre { get; set; }
    }

    /// <summary>
    /// Une ligne du pot d'heures supplémentaires : achat payé, ou ajustement
    /// de l'exploitant.
    ///
    /// POURQUOI CET HISTORIQUE EXISTE. Le motif d'un ajustement était
    /// enregistré sans être lisible nulle part : il aurait fallu une requête
    /// SQL pour le retrouver, c'est-à-dire jamais. Un motif qu'on ne peut pas
    /// relire ne justifie rien le jour où un parent conteste.
    /// </summary>
    public class LigneHeures
    {
        public DateTime Date { get; set; }

        /// <summary>Négatif pour un retrait.</summary>
        public int Minutes { get; set; }

        /// <summary>Zéro pour un ajustement manuel : rien n'a été encaissé.</summary>
        public int PrixCentimes { get; set; }

        /// <summary>Renseigné sur les ajustements, nul sur les achats.</summary>
        public string? Motif { get; set; }

        /// <summary>Le paiement Stripe, pour retrouver la transaction. Nul si offert.</summary>
        public string? StripePaiementId { get; set; }

        /// <summary>Datée quand les heures ont été reprises après remboursement.</summary>
        public DateTime? DateRemboursement { get; set; }

        /// <summary>
        /// La période sur laquelle ces heures étaient utilisables.
        ///
        /// Affichée parce qu'une recharge NE SE REPORTE PAS : une ligne d'un
        /// mois passé n'explique pas le pot d'aujourd'hui, et sans cette date
        /// on chercherait longtemps pourquoi les comptes ne tombent pas juste.
        /// </summary>
        public DateTime PeriodeDebut { get; set; }
    }
}
