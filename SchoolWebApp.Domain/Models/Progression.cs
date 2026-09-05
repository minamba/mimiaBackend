namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// La carte des compétences d'un enfant.
    ///
    /// CE QU'ELLE EST, ET CE QU'ELLE N'EST PAS
    /// ---------------------------------------
    /// Une TRACE, pas un chemin imposé. Le professeur laisse l'élève choisir
    /// son sujet — c'est une règle du produit, écrite en majuscules dans son
    /// prompt. Une carte qui verrouillerait les notions suivantes la
    /// contredirait.
    ///
    /// Elle montre donc ce qui est déjà acquis, ce qui est en cours, et ce qui
    /// reste à découvrir : la progression se lit derrière l'enfant, elle ne le
    /// pousse pas devant.
    ///
    /// TOUT VIENT DE DONNÉES DÉJÀ CALCULÉES. `MaitriseEleve` est renseignée par
    /// l'observateur qui relit les séances ; rien ici ne juge, ne note, ni ne
    /// décide. C'est ce qui rend cet écran sans risque pédagogique : il rend
    /// visible un travail qui se faisait déjà, en silence.
    /// </summary>
    public class Progression
    {
        public string? Niveau { get; set; }

        /// <summary>
        /// Acquises DANS SON ANNÉE, et elles seules. Les « points ».
        ///
        /// Le compteur et le pourcentage portent sur le programme de sa classe :
        /// une carte titrée « programme de 3e » dont le total comptait des
        /// notions de CM1 ne voulait rien dire.
        /// </summary>
        public int Acquises { get; set; }

        /// <summary>Compétences du niveau, acquises ou non. Le total à atteindre.</summary>
        public int Total { get; set; }

        /// <summary>
        /// Ce qu'il a consolidé des années précédentes.
        ///
        /// Compté à part, JAMAIS dans le pourcentage de l'année — mais compté
        /// quand même. La méthode du produit est de chercher où ça bloque, donc
        /// en amont : un élève de 3e progresse d'abord sur des notions de 6e, et
        /// une carte qui les ignorerait afficherait zéro à celui qui a le plus
        /// besoin d'être encouragé.
        /// </summary>
        public int AcquisesAutresAnnees { get; set; }

        /// <summary>
        /// Ce qui a été acquis DEPUIS LA DERNIÈRE VISITE.
        ///
        /// C'est le seul champ qui justifie une animation. Vide la plupart du
        /// temps — et c'est bien : une récompense qui se déclenche à chaque
        /// ouverture cesse d'en être une.
        /// </summary>
        public List<CompetenceVue> Nouvelles { get; set; } = [];

        /// <summary>
        /// L'enfant n'avait encore jamais ouvert sa carte.
        ///
        /// Dans ce cas, `Nouvelles` contient TOUT ce qu'il a déjà acquis — c'est
        /// voulu, on ne l'accueille pas par un écran vide après des mois de
        /// travail. Mais ce n'est pas la même nouvelle à annoncer : il n'a rien
        /// maîtrisé aujourd'hui, on lui présente son bilan. Sans ce drapeau, la
        /// carte lui dirait « tu as maîtrisé 120 NOUVELLES notions » le jour où
        /// il la découvre.
        /// </summary>
        public bool PremiereVisite { get; set; }

        public List<MatiereProgression> Matieres { get; set; } = [];
    }

    public class MatiereProgression
    {
        public int MatiereId { get; set; }

        public string? Libelle { get; set; }

        public string? Couleur { get; set; }

        public int Acquises { get; set; }

        public int Total { get; set; }

        public List<CompetenceVue> Competences { get; set; } = [];
    }

    /// <summary>Une compétence, telle qu'un enfant la voit.</summary>
    public class CompetenceVue
    {
        public int Id { get; set; }

        public string? Libelle { get; set; }

        public string? Domaine { get; set; }

        public string? Matiere { get; set; }

        /// <summary>
        /// `acquise`, `en-cours`, `fragile` ou `a-decouvrir`.
        ///
        /// `fragile` et `a-decouvrir` ont toutes deux un score bas, et c'est
        /// justement pourquoi il faut les distinguer : la première a été
        /// travaillée sans encore tenir, la seconde n'a jamais été ouverte.
        /// C'est l'existence d'une ligne de maîtrise qui tranche, pas le score.
        ///
        /// TROIS ÉTATS ET NON UN POURCENTAGE. Un score de 0,73 ne veut rien
        /// dire à un enfant de huit ans, et le montrer transformerait une carte
        /// en bulletin. Trois états se lisent d'un coup d'œil et se colorient.
        /// </summary>
        public string Etat { get; set; } = "a-decouvrir";

        /// <summary>Le niveau de la notion : CM1, 6e…</summary>
        public string? Niveau { get; set; }

        /// <summary>
        /// La notion vient-elle d un AUTRE niveau que celui de l enfant ?
        ///
        /// Elles sont fréquentes, et c est tout l intérêt du graphe : un
        /// blocage en 3e vient souvent d une notion de CM1. Les cacher aurait
        /// fait afficher zéro à un élève qui a réellement acquis sept notions
        /// — mesuré sur les données de développement.
        ///
        /// Signalées plutôt que mélangées : un enfant doit pouvoir distinguer
        /// ce qui est de son année de ce qu il rattrape.
        /// </summary>
        public bool AutreNiveau { get; set; }

        /// <summary>
        /// Le RANG du niveau de la notion. C'est lui qui regroupe et ordonne la
        /// section des années précédentes : trier sur le libellé rangerait
        /// « CM2 » avant « 6e ».
        /// </summary>
        public int NiveauOrdre { get; set; }
    }
}
