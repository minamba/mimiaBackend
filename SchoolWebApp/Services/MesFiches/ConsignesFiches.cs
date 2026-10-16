using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.MesFiches
{
    /// <summary>
    /// CE QU'ON DEMANDE AU MODÈLE — lire le cours, puis écrire chaque fiche.
    ///
    /// Les recettes reprennent les prototypes validés par Camara le 14/10/2026
    /// (`lib/mesFiches/exemples.js` côté front) : mêmes briques, même ordre.
    /// Le format des briques est celui que `ContenuFiche` vérifie ; un champ
    /// ajouté ici sans l'être là-bas serait silencieusement écarté.
    /// </summary>
    public static class ConsignesFiches
    {
        /// <summary>
        /// La lecture d'un COMPLÉMENT (16/10/2026) : la consigne ordinaire, puis
        /// les notions déjà dans le cours, pour que la lecture reconnaisse celles
        /// qui reviennent. Placée après les pages, elle ne touche pas au cache.
        /// </summary>
        public static string Lecture(string classe, string matiere, int notionsMax, IReadOnlyList<string> existantes) =>
            existantes.Count == 0
                ? Lecture(classe, matiere, notionsMax)
                : Lecture(classe, matiere, notionsMax) + $$"""


                # Ces pages COMPLÈTENT un cours déjà commencé

                L'élève a déjà des fiches sur ces notions de son cours, numérotées :
                {{string.Join("\n", existantes.Select((t, i) => $"{i + 1}. {t}"))}}

                Donne toutes les notions de CES pages, comme d'habitude. Quand l'une
                d'elles est LA MÊME qu'une notion de cette liste (même règle, même
                méthode, même sujet — pas seulement un mot en commun), ajoute
                `"deja": son numéro` à cette notion. Sinon, n'écris pas `deja`. Une
                notion de la liste ne sert qu'une fois.
                """;

        /// <summary>La lecture : le titre du cours et ses notions, sans rien rédiger encore.</summary>
        public static string Lecture(string classe, string matiere, int notionsMax) => $$"""
            Tu lis le cours qu'{{Lecteur(classe)}} vient d'importer en {{matiere}} :
            ses photos ou son document sont joints. Tu ne rédiges rien encore : tu
            dis ce que contient ce cours, pour qu'il choisisse ce qu'il veut réviser.

            Rends UNIQUEMENT ce JSON :
            {"lisible": true, "titreCours": "Le théorème de Thalès", "notions": [{"titre": "La configuration de Thalès", "resume": "Deux droites sécantes coupées par deux parallèles."}]}

            - `titreCours` : le titre du cours, celui de la page s'il y en a un, sinon
              un titre court et fidèle.
            - `notions` : les notions du cours, dans l'ordre du cours, de 1 à
              {{notionsMax}}. Une notion se révise en cinq minutes : une règle, une
              méthode, une famille de figures — pas un mot isolé, pas le chapitre
              entier. Le titre reprend les mots du cours ; le résumé tient en une
              phrase. Un cours court qui ne parle que d'une chose donne UNE notion.
            - Si les pages sont illisibles, ou si ce n'est pas un cours (une photo
              sans texte, un document vide), rends
              {"lisible": false, "remarque": "…"} — la remarque dit à l'élève, en
              une phrase simple et en le tutoyant, ce qui ne va pas (photo floue,
              page coupée…). N'invente jamais un contenu que tu ne peux pas lire.
            """;

        /// <summary>
        /// La rédaction d'une fiche. `notion` est null pour un type qui couvre le
        /// cours entier (express, spéciale contrôle).
        /// </summary>
        public static string Redaction(
            string type, string classe, int rang, string matiere, string titreCours,
            string? notion, int notionRang, int notionTotal, IEnumerable<string> autresNotions) => $$"""
            Tu rédiges une fiche de révision pour {{Lecteur(classe)}}, en {{matiere}},
            à partir de SON cours : « {{titreCours}} ». Les pages du cours sont jointes.

            {{Portee(notion, notionRang, notionTotal, autresNotions)}}

            # Fidèle au cours

            Tout ce que dit la fiche vient du cours : les définitions, les règles, les
            mots employés, les exemples. N'ajoute AUCUNE notion, règle, date ou
            formule qui n'y figure pas — {{RaisonFidelite(classe)}}
            Si le cours ne donne pas d'exemple là où il en faut un, tu peux en ajouter
            un, simple et juste, et tu le marques `"ajoute": true`. Ce qui est
            illisible sur les pages, tu ne le devines pas : tu le laisses de côté.

            # À sa hauteur

            {{Niveau(rang)}}

            # La forme du texte

            Dans tous les textes, quatre marques seulement :
            - **gras** pour les mots à retenir, avec mesure (deux ou trois par brique) ;
            - *italique* pour un exemple cité ;
            - {AM/AB} pour une fraction, dans une formule ;
            - [[…]] pour un blanc à remplir, UNIQUEMENT dans une phrase modèle.
            Jamais de HTML, de titres Markdown, de puces écrites à la main, d'emoji.
            Tutoiement si tu t'adresses à l'élève. Typographie française.

            # La fiche : {{Libelle(type)}}

            {{Recette(type)}}

            # Ce que tu rends

            UNIQUEMENT ce JSON, sans texte autour :
            {"titre": "…", "blocs": [ … ]}

            `titre` : le titre de la fiche (la notion, ou le cours pour une fiche du
            chapitre entier). Les briques possibles :
            - {"type": "paragraphe", "texte": "…"}
            - {"type": "intertitre", "texte": "1. Les figures de ressemblance"}
            - {"type": "tableau", "titre": "…", "colonnes": ["Figure", "Définition", "Exemple"], "lignes": [["Métaphore", "…", {"texte": "*…*", "ajoute": true}]]}
            - {"type": "liste", "titre": "À retenir", "style": "puces", "elements": ["…"]}  — `style` vaut "puces" ou "coches"
            - {"type": "etapes", "titre": "La méthode en 3 étapes", "etapes": [{"titre": "Identifier", "texte": "…"}]}
            - {"type": "encadre", "ton": "definition", "titre": "…", "texte": "…", "elements": ["…"]}  — `ton` : definition, cle, piege, astuce, souvent, modele ou exemple ; `texte` ou `elements`
            - {"type": "questions", "titre": "Les questions à connaître", "questions": [{"question": "…", "reponse": "…", "exemple": "…", "ajoute": false}]}
            - {"type": "arbre", "centre": "…", "branches": [{"titre": "figures de la **ressemblance**", "feuilles": ["Comparaison", "Métaphore"]}]}
            - {"type": "carte", "centre": "…", "sousTitre": "…", "branches": [{"titre": "Hyperbole", "texte": "…", "exemple": "…", "ajoute": false}]}
            """;

        private static string Portee(string? notion, int rang, int total, IEnumerable<string> autres)
        {
            if (notion is null)
            {
                return "Cette fiche couvre le cours ENTIER, toutes ses notions.";
            }

            var voisines = autres.Where(a => a != notion).ToList();
            return $"Cette fiche porte sur UNE notion du cours : « {notion} » (notion {rang} sur {total}). "
                + "N'y mets que ce qui la concerne"
                + (voisines.Count > 0
                    ? $" — les autres notions ({string.Join(" ; ", voisines)}) ont leur propre fiche."
                    : ".");
        }

        /// <summary>
        /// FLASH CARDS ET QUIZ (16/10/2026) : dix questions tirées de la fiche
        /// seule — jamais du cours —, chacune avec sa réponse, trois leurres et
        /// une explication. Un seul appel sert aux deux modes.
        /// </summary>
        public static string Entrainement(string classe, int rang, string matiere, string titre, string contenu) => $$"""
            Voici la fiche de révision « {{titre}} » d'{{Lecteur(classe)}}, en {{matiere}},
            écrite en briques JSON (paragraphes, tableaux, listes, encadrés, questions,
            arbre ou carte) :

            {{contenu}}

            Écris de quoi l'aider à se tester sur CETTE fiche : des flash cards (une
            question, la réponse au dos) qui servent aussi de quiz à choix multiple.

            Rends UNIQUEMENT ce JSON :
            {"questions": [{"question": "Que dit le théorème de Thalès ?", "reponse": "Les longueurs sont proportionnelles", "leurres": ["Les angles sont égaux", "Les côtés sont égaux", "Les aires sont égales"], "explication": "Deux parallèles coupent deux sécantes en longueurs proportionnelles."}]}

            - 8 à 10 questions, qui couvrent toute la fiche, dans son ordre. Tout vient
              de la fiche : aucune notion, date ou formule qu'elle ne contient pas.
            - `question` : une seule chose demandée, claire sans voir la fiche.
            - `reponse` : courte (douze mots au plus), juste, celle de la fiche.
            - `leurres` : trois réponses FAUSSES mais plausibles, de la même nature
              et de la même longueur que la réponse — jamais absurdes, jamais
              « toutes les réponses », jamais une variante de la bonne.
            - `explication` : une phrase qui dit pourquoi la réponse est juste.
            - Tu tutoies l'élève. {{Niveau(rang)}}
            - Tu peux écrire **gras**, *italique* et {a/b} pour une fraction, comme dans
              la fiche. Rien d'autre : pas de HTML, pas de LaTeX.
            """;

        /// <summary>
        /// À QUI S'ADRESSE LA FICHE : un élève de sa classe, ou un adulte — le
        /// parent qui révise pour lui-même dans son espace (16/10/2026).
        /// </summary>
        private static string Lecteur(string classe) =>
            classe == FichesRevisionEleve.ClasseAdulte ? "un adulte" : $"un élève de {classe}";

        private static string RaisonFidelite(string classe) =>
            classe == FichesRevisionEleve.ClasseAdulte
                ? "c'est SON document qui fait foi."
                : "l'élève révise pour le contrôle de SA classe.";

        private static string Niveau(int rang) => rang switch
        {
            >= FichesRevisionEleve.RangAdulte => "Adulte : un parent qui révise pour lui-même. Dense et précis, douze "
                + "éléments au plus par brique ; le vocabulaire exact du document, sans l'infantiliser.",
            <= 5 => "École primaire : des phrases courtes et des mots simples, de deux à cinq éléments par "
                + "brique (six au plus en CM1-CM2), un exemple concret pour chaque idée.",
            <= 9 => "Collège : précis et lisible, huit éléments au plus par brique ; le vocabulaire du cours, "
                + "expliqué s'il est nouveau.",
            _ => "Lycée : dense mais lisible, douze éléments au plus par brique ; le vocabulaire exact du "
                + "cours, sans simplifier ce qu'il dit.",
        };

        private static string Libelle(string type) =>
            Domain.Models.FichesRevisionEleve.Type(type)?.Libelle ?? "Fiche classique";

        private static string Recette(string type) => type switch
        {
            "express" => """
                L'essentiel du cours sur une page, en ARBRE : UNE seule brique `arbre`,
                rien d'autre. `centre` : le thème en deux ou trois mots. De deux à six
                branches — les familles ou les grandes parties du cours, avec le mot
                important en gras — et sous chacune, de deux à six feuilles : des
                MOTS-CLÉS, jamais des phrases ni des définitions. Une formule courte
                peut être une feuille.
                """,
            "visuelle" => """
                Une CARTE MENTALE qui explique : UNE seule brique `carte`, rien d'autre.
                `centre` : la notion en deux à quatre mots ; `sousTitre` : à quoi elle
                sert, en une demi-phrase. De trois à huit branches (six est idéal) :
                chacune a un `titre` d'un à trois mots, un `texte` d'UNE phrase, et un
                `exemple` court tiré du cours.
                """,
            "questions" => """
                Pour mémoriser activement : UNE seule brique `questions`, titrée
                « Les questions à connaître », et AUCUNE autre brique — ni encadré, ni
                définition, ni étapes : tout passe par les questions. De cinq à douze questions, de la plus
                simple à la plus fine, celles qu'un professeur poserait sur cette
                notion. Chaque réponse est courte (une ou deux phrases) et se suffit à
                elle-même ; un `exemple` quand il aide. Une question qui fait
                distinguer deux notions proches vaut de l'or.
                """,
            "controle" => """
                La synthèse du chapitre pour une évaluation, dans cet ordre :
                1. un `encadre` de ton `definition` : de quoi parle le chapitre ;
                2. pour chaque partie du cours, un `tableau` titré « 1. … », « 2. … »,
                   colonnes « Notion », « À reconnaître » (ou « Ce qu'il faut savoir »),
                   « Exemple » ;
                3. une `liste` de style `coches` titrée « À savoir par cœur » : trois à
                   six points, les plus demandés ;
                4. un `encadre` de ton `piege` titré « Les pièges fréquents », en
                   `elements` : les confusions classiques sur ce chapitre ;
                5. un `encadre` de ton `souvent` titré « Ce qui tombe souvent », en
                   `elements` : les exercices types.
                Les pièges et ce qui tombe souvent peuvent dépasser la lettre du cours :
                ce sont des conseils de professeur, pas des notions nouvelles.
                """,
            "methode" => """
                Les étapes pour réussir un exercice sur cette notion, dans cet ordre :
                1. si la notion s'y prête, un `tableau` pour reconnaître le cas :
                   « Si tu repères… », « Pense à… », « Exemple » (ou « On te demande
                   de… », « Utilise… », « Indice ») ;
                2. une brique `etapes` titrée « La méthode en N étapes » : de trois à
                   six étapes, chacune un `titre` d'action et un `texte` d'une phrase ;
                3. un `encadre` de ton `modele` : la phrase ou la rédaction modèle, avec
                   des blancs [[…]] à la place de ce qui change d'un exercice à l'autre ;
                4. un `encadre` de ton `exemple` : un exemple rédigé et résolu.
                """,
            _ => """
                Définitions, notions essentielles et exemples, dans cet ordre :
                1. un `encadre` de ton `definition` : la définition ou la règle ;
                2. s'il y a plusieurs éléments à comparer, un `tableau` (par exemple
                   « Notion », « Définition », « Exemple ») ; sinon une `liste` titrée
                   « À retenir » ;
                3. un `encadre` de ton `exemple` si le cours en développe un ;
                4. un `encadre` de ton `piege` si le cours signale une confusion, ou
                   s'il y en a une évidente entre deux éléments de la fiche.
                """,
        };
    }
}
