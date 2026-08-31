namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel de philosophie, terminale.
    ///
    /// UNE SEULE ANNÉE, ET C'EST LA PREMIÈRE
    /// -------------------------------------
    /// Toutes les autres matières arrivent en terminale après des années
    /// d'acquis ; la philosophie commence là. L'élève n'a aucun prérequis
    /// interne, et c'est ce qui la rend redoutable : on lui demande en juin un
    /// exercice qu'il découvre en septembre.
    ///
    /// LA MÉTHODE PÈSE AUTANT QUE LES NOTIONS, PROBABLEMENT PLUS
    /// --------------------------------------------------------
    /// L'épreuve n'est pas un contrôle de connaissances. Un élève qui a lu
    /// tout le programme et qui ne sait pas construire un problème rend une
    /// récitation ; un élève qui sait problématiser s'en sort avec trois
    /// notions. Le domaine « Méthode » est donc le plus fourni du référentiel,
    /// et il est délibérément placé en tête : c'est là que se joue la note.
    ///
    /// POURQUOI LES NOTIONS ET NON LES AUTEURS
    /// ---------------------------------------
    /// Le programme est un programme de NOTIONS, accompagné de repères. Les
    /// auteurs y servent d'appuis, pas de chapitres — et un référentiel par
    /// auteur pousserait l'élève à réciter des doctrines, ce que l'épreuve
    /// sanctionne précisément.
    ///
    /// LE TRONC COMMUN, PAS LA SPÉCIALITÉ
    /// ----------------------------------
    /// Les notions retenues sont celles du tronc commun de terminale générale.
    /// La voie technologique en travaille un sous-ensemble ; rien à retirer
    /// pour elle, ses notions sont incluses. La voie professionnelle n'a pas
    /// de philosophie du tout — elle en est écartée par `VoiesScolaires`, et
    /// non ici.
    /// </summary>
    public static class ReferentielPhilosophie
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- Méthode : ce qui fait la note ---
                ("TERMINALE", "PHI_TLE_METH_PROBLEME",     "Méthode", "Transformer une question en problème : montrer que deux réponses sensées s'opposent", 1),
                ("TERMINALE", "PHI_TLE_METH_ANALYSE",      "Méthode", "Analyser les termes du sujet avant d'y répondre, sans plaquer une définition de dictionnaire", 2),
                ("TERMINALE", "PHI_TLE_METH_DISTINCTION",  "Méthode", "Distinguer deux notions voisines pour faire avancer l'argument (croire/savoir, désirer/vouloir)", 3),
                ("TERMINALE", "PHI_TLE_METH_THESE",        "Méthode", "Formuler une thèse claire et s'y tenir sur toute une partie", 4),
                ("TERMINALE", "PHI_TLE_METH_ARGUMENT",     "Méthode", "Construire un argument qui démontre, au lieu d'affirmer puis d'illustrer", 5),
                ("TERMINALE", "PHI_TLE_METH_EXEMPLE",      "Méthode", "Se servir d'un exemple pour prouver quelque chose, et non pour meubler", 6),
                ("TERMINALE", "PHI_TLE_METH_OBJECTION",    "Méthode", "Opposer à sa propre thèse l'objection la plus forte, et lui répondre", 7),
                ("TERMINALE", "PHI_TLE_METH_PLAN",         "Méthode", "Bâtir un plan dont chaque partie répond au problème sous un angle nouveau", 8),
                ("TERMINALE", "PHI_TLE_METH_INTRO",        "Méthode", "Rédiger une introduction qui amène le sujet, l'analyse, pose le problème et annonce", 9),
                ("TERMINALE", "PHI_TLE_METH_TRANSITION",   "Méthode", "Écrire une transition qui montre pourquoi la partie précédente ne suffisait pas", 10),
                ("TERMINALE", "PHI_TLE_METH_CONCLUSION",   "Méthode", "Conclure en répondant au problème, sans ouvrir sur une question décorative", 11),
                ("TERMINALE", "PHI_TLE_METH_TEXTE",        "Méthode", "Expliquer un texte en suivant son mouvement plutôt qu'en le paraphrasant", 12),
                ("TERMINALE", "PHI_TLE_METH_ENJEU",        "Méthode", "Dégager l'enjeu d'un texte : ce que l'auteur combat, et pourquoi c'est important", 13),
                ("TERMINALE", "PHI_TLE_METH_REPERES",      "Méthode", "Employer les repères du programme à bon escient (en fait/en droit, absolu/relatif, contingent/nécessaire)", 14),

                // --- L'existence humaine et la culture ---
                ("TERMINALE", "PHI_TLE_EXIS_CONSCIENCE",   "L'existence humaine et la culture", "Interroger la conscience de soi : est-elle une connaissance de soi ?", 15),
                ("TERMINALE", "PHI_TLE_EXIS_INCONSCIENT",  "L'existence humaine et la culture", "Examiner l'hypothèse de l'inconscient et ce qu'elle change pour la responsabilité", 16),
                ("TERMINALE", "PHI_TLE_EXIS_TEMPS",        "L'existence humaine et la culture", "Penser le temps : durée vécue, mémoire, et rapport à la mort", 17),
                ("TERMINALE", "PHI_TLE_EXIS_LANGAGE",      "L'existence humaine et la culture", "Interroger le langage : exprime-t-il la pensée ou la forme-t-il ?", 18),
                ("TERMINALE", "PHI_TLE_EXIS_ART",          "L'existence humaine et la culture", "Distinguer l'art du beau naturel et de la simple technique", 19),
                ("TERMINALE", "PHI_TLE_EXIS_TECHNIQUE",    "L'existence humaine et la culture", "Interroger la technique : simple moyen, ou transformation de nos fins ?", 20),
                ("TERMINALE", "PHI_TLE_EXIS_TRAVAIL",      "L'existence humaine et la culture", "Penser le travail entre contrainte subie et réalisation de soi", 21),
                ("TERMINALE", "PHI_TLE_EXIS_NATURE",       "L'existence humaine et la culture", "Distinguer nature et culture, et mesurer ce que cette distinction engage", 22),
                ("TERMINALE", "PHI_TLE_EXIS_RELIGION",     "L'existence humaine et la culture", "Interroger la religion : foi, raison, et ce que croire veut dire", 23),

                // --- La morale et la politique ---
                ("TERMINALE", "PHI_TLE_MOR_LIBERTE",       "La morale et la politique", "Interroger la liberté : absence de contrainte, ou autonomie de la volonté ?", 24),
                ("TERMINALE", "PHI_TLE_MOR_DEVOIR",        "La morale et la politique", "Distinguer agir par devoir et agir conformément au devoir", 25),
                ("TERMINALE", "PHI_TLE_MOR_BONHEUR",       "La morale et la politique", "Examiner le bonheur : fin de l'action, ou idéal indéterminé ?", 26),
                ("TERMINALE", "PHI_TLE_MOR_JUSTICE",       "La morale et la politique", "Distinguer le juste du légal, et l'égalité de l'équité", 27),
                ("TERMINALE", "PHI_TLE_MOR_ETAT",          "La morale et la politique", "Interroger l'État : ce qui fonde son autorité et ce qui la limite", 28),

                // --- La connaissance ---
                ("TERMINALE", "PHI_TLE_CONN_RAISON",       "La connaissance", "Interroger la raison : ses pouvoirs et ses limites", 29),
                ("TERMINALE", "PHI_TLE_CONN_VERITE",       "La connaissance", "Distinguer vérité, certitude et opinion", 30),
                ("TERMINALE", "PHI_TLE_CONN_SCIENCE",      "La connaissance", "Comprendre ce qui fait la scientificité d'une science : preuve, expérience, réfutation", 31),
            };

        /// <summary>
        /// Les arêtes du graphe.
        ///
        /// LA PHILOSOPHIE N'A PAS DE PRÉREQUIS INTERNES D'UNE ANNÉE SUR
        /// L'AUTRE — elle n'a qu'une année. Ses arêtes sont donc de deux sortes,
        /// et les deux sont utiles.
        ///
        /// D'ABORD, LA MÉTHODE COMMANDE LES NOTIONS. Un élève qui ne sait pas
        /// problématiser n'échoue pas « sur la liberté » : il échoue sur tous
        /// les sujets. Faire remonter le diagnostic vers la méthode plutôt que
        /// vers la notion du jour évite de lui faire réviser un cours qu'il
        /// connaît déjà.
        ///
        /// ENSUITE, LE FRANÇAIS DE PREMIÈRE. C'est le vrai amont de cette
        /// matière : la dissertation de philosophie suppose l'argumentation et
        /// la rédaction travaillées l'année d'avant. Un élève qui n'a jamais su
        /// construire un paragraphe argumenté ne butera pas sur Kant, il butera
        /// sur le paragraphe — et c'est là qu'il faut l'aider.
        /// </summary>
        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // La méthode se tient elle-même : on ne bâtit pas un plan sans
                // problème, ni une introduction sans analyse des termes.
                ("PHI_TLE_METH_PLAN",        "PHI_TLE_METH_PROBLEME",    3),
                ("PHI_TLE_METH_PROBLEME",    "PHI_TLE_METH_ANALYSE",     3),
                ("PHI_TLE_METH_PROBLEME",    "PHI_TLE_METH_DISTINCTION", 2),
                ("PHI_TLE_METH_INTRO",       "PHI_TLE_METH_PROBLEME",    3),
                ("PHI_TLE_METH_ARGUMENT",    "PHI_TLE_METH_THESE",       3),
                ("PHI_TLE_METH_EXEMPLE",     "PHI_TLE_METH_ARGUMENT",    2),
                ("PHI_TLE_METH_OBJECTION",   "PHI_TLE_METH_ARGUMENT",    3),
                ("PHI_TLE_METH_TRANSITION",  "PHI_TLE_METH_PLAN",        2),
                ("PHI_TLE_METH_CONCLUSION",  "PHI_TLE_METH_PROBLEME",    3),
                ("PHI_TLE_METH_TEXTE",       "PHI_TLE_METH_ANALYSE",     3),
                ("PHI_TLE_METH_ENJEU",       "PHI_TLE_METH_TEXTE",       2),

                // Toute notion repose sur la capacité à problématiser : c'est
                // l'arête qui fait remonter le diagnostic là où il faut.
                ("PHI_TLE_EXIS_CONSCIENCE",  "PHI_TLE_METH_PROBLEME",    3),
                ("PHI_TLE_EXIS_INCONSCIENT", "PHI_TLE_EXIS_CONSCIENCE",  3),
                ("PHI_TLE_EXIS_ART",         "PHI_TLE_EXIS_TECHNIQUE",   1),
                ("PHI_TLE_EXIS_TRAVAIL",     "PHI_TLE_EXIS_TECHNIQUE",   2),
                ("PHI_TLE_EXIS_RELIGION",    "PHI_TLE_CONN_VERITE",      2),
                ("PHI_TLE_MOR_DEVOIR",       "PHI_TLE_MOR_LIBERTE",      3),
                ("PHI_TLE_MOR_JUSTICE",      "PHI_TLE_MOR_ETAT",         2),
                ("PHI_TLE_MOR_ETAT",         "PHI_TLE_MOR_LIBERTE",      2),
                ("PHI_TLE_MOR_BONHEUR",      "PHI_TLE_MOR_LIBERTE",      1),
                ("PHI_TLE_CONN_VERITE",      "PHI_TLE_CONN_RAISON",      3),
                ("PHI_TLE_CONN_SCIENCE",     "PHI_TLE_CONN_VERITE",      3),
                ("PHI_TLE_EXIS_LANGAGE",     "PHI_TLE_CONN_VERITE",      1),

                // LE VRAI AMONT : le français de première.
                //
                // Un code inconnu est écarté sans faire échouer le semis — ces
                // arêtes se poseront d'elles-mêmes si les codes existent, et
                // s'effaceront si le référentiel de français change.
                ("PHI_TLE_METH_ARGUMENT",    "FR_1RE_ECR_PROBLEMATISER", 3),
                ("PHI_TLE_METH_INTRO",       "FR_2DE_ECR_INTRODUCTION",  3),
                ("PHI_TLE_METH_TEXTE",       "FR_1RE_METH_LINEAIRE",     3),
                ("PHI_TLE_METH_PLAN",        "FR_1RE_ECR_DISSERTATION",  3),
            };
    }
}
