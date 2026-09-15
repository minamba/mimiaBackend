namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// Les enseignements de spécialité de la série ST2S, tels que les textes officiels les
    /// définissent — provenance détaillée en tête du fichier. Réparti entre les matières par
    /// `ReferentielsSeriesTechnologiques`.
    /// </summary>
    public static class ReferentielSt2s
    {
        // =====================================================================================
        // RÉFÉRENTIELS DE LA SÉRIE TECHNOLOGIQUE ST2S (sciences et technologies de la santé et du social)
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis le texte
        // officiel téléchargé et lu le 14/09/2026, rien de mémoire. Sources gardées dans techno\sources\st2s\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau          | Intitulé exact (page de titre de l'annexe)                              |
        // |---------|-----------------|-------------------------------------------------------------------------|
        // | BPH1    | PREMIERE_ST2S   | Programme de biologie et physiopathologie humaines de première ST2S     |
        // | PCS1    | PREMIERE_ST2S   | Programme de physique-chimie pour la santé de première ST2S             |
        // | STSS1   | PREMIERE_ST2S   | Programme de sciences et techniques sanitaires et sociales de première ST2S |
        // | CBPHT   | TERMINALE_ST2S  | Programme de chimie, biologie et physiopathologie humaines de terminale ST2S |
        // | STSST   | TERMINALE_ST2S  | Programme de sciences et techniques sanitaires et sociales de terminale ST2S |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901642A,
        //   BO spécial n° 1 du 22 janvier 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2019 ».
        //   Trois annexes, TOUTES lues : annexe 1 = BPH, annexe 2 = PCS, annexe 3 = STSS.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901642A.htm
        //   Annexe 1 (BPH)  : https://eduscol.education.gouv.fr/sites/default/files/document/spe642annexe11063551pdf-82500.pdf
        //   Annexe 2 (PCS)  : https://eduscol.education.gouv.fr/sites/default/files/document/spe642annexe21063553pdf-82503.pdf
        //   Annexe 3 (STSS) : https://eduscol.education.gouv.fr/sites/default/files/document/spe642annexe31063555pdf-82506.pdf
        //
        // TERMINALE : arrêté du 19-7-2019, J.O. du 23-7-2019, NOR MENE1921258A,
        //   BO spécial n° 8 du 25 juillet 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2020 ».
        //   Deux annexes, TOUTES lues : annexe 1 = CBPH, annexe 2 = STSS.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921258A.htm
        //   Annexe 1 (CBPH) : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/19/6/spe258_annexe1_1159196.pdf
        //                     (copie identique lue sur éduscol : spe258annexe11159196pdf-82509.pdf)
        //   Annexe 2 (STSS) : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/19/8/spe258_annexe2_1159198.pdf
        //                     (copie identique lue sur éduscol : spe258annexe21159198pdf-82512.pdf)
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : la page éduscol « Programmes et ressources en série ST2S »
        //   (https://eduscol.education.gouv.fr/5847/programmes-et-ressources-en-serie-st2s, datée « mars 2026 »)
        //   liste ces cinq programmes, et eux seuls, sous « Programmes en vigueur ». Aucun arrêté modificatif
        //   de 2020 à 2026 n'a été trouvé (recherche BO et éduscol le 14/09/2026). Les changements récents
        //   touchent les ÉPREUVES, pas les programmes (voir st2s.prof.md).
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - Aucun sur la liste des cinq programmes : elle correspond exactement aux annexes.
        //   - Le module de STSS de première a deux titres dans le même texte officiel : « Modes d'intervention
        //     sociale et en santé » (sommaire et titre de partie) et « Modes d'intervention en santé et action
        //     sociale » (présentation). Le Domaine retenu reprend le titre de la partie.
        //   - CBPH de terminale : le texte fixe 3 h hebdomadaires pour la Chimie et 5 h pour la Biologie et
        //     physiopathologie humaines. Le tableau dépasse donc la fourchette habituelle (78 lignes), mais
        //     chaque partie reste dedans (40 en chimie, 38 en biologie).
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   PCS1 46 · BPH1 44 · STSS1 44 · STSST 30 · CBPHT 78 (Chimie 40 + Biologie 38) · total 242
        //   - Les programmes de chimie reprennent les trois thèmes de PCS de première (« Prévenir et sécuriser »,
        //     « Analyser et diagnostiquer », « Faire des choix autonomes et responsables ») : c'est voulu par le
        //     texte, pas un doublon.
        // =====================================================================================
        
        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE ST2S — PHYSIQUE-CHIMIE POUR LA SANTÉ (annexe 2 de MENE1901642A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCS1 =
        {
            // Mesure et incertitudes
            ("PREMIERE_ST2S", "PCS1_MESURE_SERIE",           "Mesure et incertitudes", "Exploiter une série de mesures indépendantes : histogramme, moyenne, écart-type et incertitude-type", 1),
            ("PREMIERE_ST2S", "PCS1_MESURE_REFERENCE",       "Mesure et incertitudes", "Écrire un résultat avec un nombre adapté de chiffres significatifs et le comparer à une valeur de référence", 2),
        
            // Thème 1 : Prévenir et sécuriser — sécurité chimique et électrique dans l'habitat
            ("PREMIERE_ST2S", "PCS1_QTE_MATIERE",            "Prévenir et sécuriser — Habitat", "Calculer une masse molaire et utiliser la relation n = m/M", 3),
            ("PREMIERE_ST2S", "PCS1_CONCENTRATIONS",         "Prévenir et sécuriser — Habitat", "Utiliser les relations n = C × V et m = Cm × V pour une solution aqueuse", 4),
            ("PREMIERE_ST2S", "PCS1_DISSOLUTION_DILUTION",   "Prévenir et sécuriser — Habitat", "Mettre en œuvre un protocole de dissolution ou de dilution pour préparer une solution de concentration donnée", 5),
            ("PREMIERE_ST2S", "PCS1_PH",                     "Prévenir et sécuriser — Habitat", "Utiliser [H3O+] = 10^-pH, définir une solution acide, basique ou neutre et relier [H3O+] et [HO-]", 6),
            ("PREMIERE_ST2S", "PCS1_ACIDE_BASE",             "Prévenir et sécuriser — Habitat", "Définir un acide et une base selon Brønsted et écrire l'équation d'une réaction acido-basique", 7),
            ("PREMIERE_ST2S", "PCS1_SECURITE_ACIDE_BASE",    "Prévenir et sécuriser — Habitat", "Appliquer les pictogrammes et règles de sécurité des acides et bases concentrés et neutraliser une solution", 8),
            ("PREMIERE_ST2S", "PCS1_OXYDOREDUCTION",         "Prévenir et sécuriser — Habitat", "Identifier oxydant et réducteur et écrire une réaction d'oxydoréduction à partir des demi-équations", 9),
            ("PREMIERE_ST2S", "PCS1_ANTISEPTIQUES",          "Prévenir et sécuriser — Habitat", "Analyser l'action oxydante d'un antiseptique et le risque du mélange eau de Javel-détartrant", 10),
            ("PREMIERE_ST2S", "PCS1_TENSION_SECTEUR",        "Prévenir et sécuriser — Habitat", "Connaître les caractéristiques de la tension du secteur et exploiter un oscillogramme", 11),
            ("PREMIERE_ST2S", "PCS1_RISQUE_ELECTRIQUE",      "Prévenir et sécuriser — Habitat", "Expliquer électrisation et électrocution, le rôle du disjoncteur et de la mise à la Terre", 12),
            ("PREMIERE_ST2S", "PCS1_INFRAROUGES",            "Prévenir et sécuriser — Habitat", "Situer visible, infrarouges et ultraviolets et exploiter la loi de Wien pour le corps humain", 13),
        
            // Thème 1 : Prévenir et sécuriser — sécurité routière
            ("PREMIERE_ST2S", "PCS1_DISTANCE_ARRET",         "Prévenir et sécuriser — Sécurité routière", "Utiliser l'énergie cinétique et relier vitesse, masse et état de la route à la distance d'arrêt", 14),
        
            // Thème 2 : Analyser et diagnostiquer — ondes sonores et audition
            ("PREMIERE_ST2S", "PCS1_SON_CARACTERISTIQUES",   "Analyser et diagnostiquer — Audition", "Situer sons audibles, ultrasons et infrasons et distinguer sons graves, médiums et aigus", 15),
            ("PREMIERE_ST2S", "PCS1_NIVEAU_SONORE",          "Analyser et diagnostiquer — Audition", "Expliquer l'émission, la propagation et la perception d'un son et mesurer un niveau sonore en dB", 16),
            ("PREMIERE_ST2S", "PCS1_AUDIOGRAMME",            "Analyser et diagnostiquer — Audition", "Analyser un audiogramme et expliquer le principe de compensation d'une déficience auditive", 17),
        
            // Thème 2 : Analyser et diagnostiquer — lumière et vision
            ("PREMIERE_ST2S", "PCS1_OEIL_MODELE",            "Analyser et diagnostiquer — Vision", "Connaître les composants optiques de l'œil et représenter son modèle optique", 18),
            ("PREMIERE_ST2S", "PCS1_LENTILLE_RAYONS",        "Analyser et diagnostiquer — Vision", "Tracer la marche des rayons passant par O, F et F' d'une lentille convergente ou divergente", 19),
            ("PREMIERE_ST2S", "PCS1_IMAGE_LENTILLE",         "Analyser et diagnostiquer — Vision", "Construire l'image d'un objet par une lentille convergente, dire si elle est réelle et évaluer le grandissement", 20),
            ("PREMIERE_ST2S", "PCS1_DEFAUTS_VISION",         "Analyser et diagnostiquer — Vision", "Expliquer l'accommodation, la presbytie et définir un œil myope et un œil hypermétrope", 21),
            ("PREMIERE_ST2S", "PCS1_VERRE_CORRECTEUR",       "Analyser et diagnostiquer — Vision", "Justifier le choix d'un verre correcteur avec la vergence de deux lentilles minces accolées", 22),
        
            // Thème 2 : Analyser et diagnostiquer — fluides et pression sanguine
            ("PREMIERE_ST2S", "PCS1_DEBIT",                  "Analyser et diagnostiquer — Pression sanguine", "Appliquer les relations D = v × S et DC = fC × VES", 23),
            ("PREMIERE_ST2S", "PCS1_PRESSION",               "Analyser et diagnostiquer — Pression sanguine", "Appliquer la relation P = F/S avec les unités internationales", 24),
            ("PREMIERE_ST2S", "PCS1_STATIQUE_FLUIDES",       "Analyser et diagnostiquer — Pression sanguine", "Utiliser la loi fondamentale de la statique des fluides P2 - P1 = ρg(z1 - z2)", 25),
            ("PREMIERE_ST2S", "PCS1_TENSION_ARTERIELLE",     "Analyser et diagnostiquer — Pression sanguine", "Distinguer pression et tension artérielle et analyser des mesures de tension", 26),
        
            // Thème 2 : Analyser et diagnostiquer — analyse chimique des milieux biologiques
            ("PREMIERE_ST2S", "PCS1_REPRESENTATIONS",        "Analyser et diagnostiquer — Milieux biologiques", "Passer d'une formule brute à une formule développée, semi-développée ou topologique", 27),
            ("PREMIERE_ST2S", "PCS1_FONCTIONS_ORGANIQUES",   "Analyser et diagnostiquer — Milieux biologiques", "Identifier les fonctions alcool, aldéhyde, cétone, acide carboxylique, ester, étheroxyde, amine, amide", 28),
            ("PREMIERE_ST2S", "PCS1_ISOMERIE_NOMENCLATURE",  "Analyser et diagnostiquer — Milieux biologiques", "Identifier des isomères et nommer alcanes, alcools, acides et dérivés carbonylés jusqu'à six carbones", 29),
            ("PREMIERE_ST2S", "PCS1_BIOMOLECULES",           "Analyser et diagnostiquer — Milieux biologiques", "Reconnaître les structures des glucides, acides gras, triglycérides, acides α-aminés et la liaison peptidique", 30),
            ("PREMIERE_ST2S", "PCS1_EAU_POLARITE",           "Analyser et diagnostiquer — Milieux biologiques", "Expliquer la polarité de l'eau, la liaison hydrogène et ses changements d'état", 31),
            ("PREMIERE_ST2S", "PCS1_SOLUBILITE_MICELLES",    "Analyser et diagnostiquer — Milieux biologiques", "Justifier la solubilité des glucides dans l'eau et interpréter la formation de micelles", 32),
            ("PREMIERE_ST2S", "PCS1_EXTRACTION",             "Analyser et diagnostiquer — Milieux biologiques", "Situer phases aqueuse et organique par les densités et réaliser une séparation ou une extraction", 33),
        
            // Thème 3 : Faire des choix autonomes et responsables — besoins énergétiques
            ("PREMIERE_ST2S", "PCS1_DEPENSE_ENERGETIQUE",    "Choix responsables — Besoins énergétiques", "Estimer la dépense énergétique journalière par la relation de Harris et Benedict et convertir calories et joules", 34),
            ("PREMIERE_ST2S", "PCS1_TRANSFERTS_THERMIQUES",  "Choix responsables — Besoins énergétiques", "Identifier les pertes de chaleur de l'organisme : rayonnement, convection, conduction, évaporation", 35),
            ("PREMIERE_ST2S", "PCS1_BILAN_MUSCLE",           "Choix responsables — Besoins énergétiques", "Établir le bilan énergétique d'un muscle et définir une transformation endothermique ou exothermique", 36),
            ("PREMIERE_ST2S", "PCS1_VALEUR_ENERGETIQUE",     "Choix responsables — Besoins énergétiques", "Identifier les nutriments d'un aliment et calculer la valeur énergétique d'une ration alimentaire", 37),
            ("PREMIERE_ST2S", "PCS1_COMBUSTION_HYDROLYSE",   "Choix responsables — Besoins énergétiques", "Écrire les transformations du glucose en aérobie et anaérobie, une combustion et l'hydrolyse du lactose", 38),
        
            // Thème 3 : Faire des choix autonomes et responsables — biomolécules et prévention
            ("PREMIERE_ST2S", "PCS1_GLUCIDES_CLASSIFICATION","Choix responsables — Biomolécules", "Distinguer glucides simples et complexes et reconnaître un polymère du glucose", 39),
            ("PREMIERE_ST2S", "PCS1_HYDROLYSE_GLUCIDE",      "Choix responsables — Biomolécules", "Écrire l'hydrolyse d'un glucide complexe et étudier sans formalisme la cinétique de l'hydrolyse de l'amidon", 40),
            ("PREMIERE_ST2S", "PCS1_GLYCEMIE_STOCKAGE",      "Choix responsables — Biomolécules", "Analyser des documents sur le stockage des glucides et le contrôle de la glycémie", 41),
        
            // Thème 3 : Faire des choix autonomes et responsables — ressources naturelles
            ("PREMIERE_ST2S", "PCS1_POTABILITE",             "Choix responsables — Ressources naturelles", "Interpréter la composition ionique d'une eau potable par comparaison aux données de référence", 42),
            ("PREMIERE_ST2S", "PCS1_POLLUTION_EAU",          "Choix responsables — Ressources naturelles", "Connaître les causes de pollution des eaux et analyser les pratiques qui préservent l'eau", 43),
            ("PREMIERE_ST2S", "PCS1_SOLS_ENGRAIS",           "Choix responsables — Ressources naturelles", "Décrire le rôle du complexe argilo-humique et des ions nitrate, phosphate et potassium des engrais", 44),
            ("PREMIERE_ST2S", "PCS1_PESTICIDES",             "Choix responsables — Ressources naturelles", "Décrire insecticides, fongicides et herbicides et doser une espèce à l'aide d'une échelle de teinte", 45),
            ("PREMIERE_ST2S", "PCS1_BIOCARBURANT",           "Choix responsables — Ressources naturelles", "Analyser la compétition entre rôle de nutriment et rôle de biocarburant d'une céréale", 46),
        };
        
        // -------------------------------------------------------------------------------------
        // 2. PREMIÈRE ST2S — BIOLOGIE ET PHYSIOPATHOLOGIE HUMAINES (annexe 1 de MENE1901642A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] BPH1 =
        {
            // Préambule : trame commune et terminologie
            ("PREMIERE_ST2S", "BPH1_DEMARCHE_MEDICALE",      "Démarche et terminologie", "Suivre la démarche médicale : signes cliniques et paracliniques, diagnostic, traitement, suivi", 1),
            ("PREMIERE_ST2S", "BPH1_TERMINOLOGIE",           "Démarche et terminologie", "Construire et analyser un terme médical à partir de ses préfixes, racines et suffixes", 2),
        
            // Organisation et fonctionnement intégré de l'être humain
            ("PREMIERE_ST2S", "BPH1_NIVEAUX_ORGANISATION",   "Organisation de l'être humain", "Caractériser les niveaux d'organisation : appareil, organe, tissu, cellule, ultrastructure, molécule", 3),
            ("PREMIERE_ST2S", "BPH1_COUPES_ORIENTATION",     "Organisation de l'être humain", "Orienter un cliché anatomique et différencier coupes sagittale, frontale et transversale", 4),
            ("PREMIERE_ST2S", "BPH1_CAVITES",                "Organisation de l'être humain", "Localiser les organes des cavités crânienne, rachidienne, thoracique, abdominale et pelvienne", 5),
            ("PREMIERE_ST2S", "BPH1_TISSUS",                 "Organisation de l'être humain", "Comparer tissu épithélial et tissu conjonctif et relier la structure d'un tissu à sa fonction", 6),
            ("PREMIERE_ST2S", "BPH1_CELLULE_ULTRASTRUCTURE", "Organisation de l'être humain", "Identifier les ultrastructures cellulaires, leur rôle principal et les molécules qui les composent", 7),
            ("PREMIERE_ST2S", "BPH1_TECHNIQUES_EXPLORATION", "Organisation de l'être humain", "Relier imagerie médicale, microscopie et analyse biochimique au niveau d'organisation étudié", 8),
            ("PREMIERE_ST2S", "BPH1_INTERDEPENDANCE",        "Organisation de l'être humain", "Repérer les échanges de matière et d'information entre les systèmes de l'organisme", 9),
        
            // Appareil locomoteur et motricité
            ("PREMIERE_ST2S", "BPH1_SQUELETTE",              "Appareil locomoteur et motricité", "Identifier les éléments des squelettes axial et appendiculaire et d'une articulation mobile", 10),
            ("PREMIERE_ST2S", "BPH1_SYSTEME_NERVEUX",        "Appareil locomoteur et motricité", "Identifier les éléments des systèmes nerveux central et périphérique et décrire l'organisation d'un nerf", 11),
            ("PREMIERE_ST2S", "BPH1_NEURONE_INFLUX",         "Appareil locomoteur et motricité", "Schématiser un neurone et caractériser l'influx nerveux : potentiel de repos et potentiel d'action", 12),
            ("PREMIERE_ST2S", "BPH1_MUSCLE_STRIE",           "Appareil locomoteur et motricité", "Décrire l'organisation hiérarchisée du muscle strié squelettique jusqu'à la myofibrille", 13),
            ("PREMIERE_ST2S", "BPH1_CONTRACTION",            "Appareil locomoteur et motricité", "Schématiser un sarcomère et présenter le glissement des myofilaments", 14),
            ("PREMIERE_ST2S", "BPH1_JONCTION_NEUROMUSC",     "Appareil locomoteur et motricité", "Identifier les constituants d'une jonction neuromusculaire et expliquer son fonctionnement", 15),
            ("PREMIERE_ST2S", "BPH1_IMAGERIE_OSSEUSE",       "Appareil locomoteur et motricité", "Expliquer radiographie, scanographie et IRM : principe, intérêt diagnostique, dangers", 16),
            ("PREMIERE_ST2S", "BPH1_PATHO_LOCOMOTEUR",       "Appareil locomoteur et motricité", "Étudier une atteinte de l'appareil locomoteur : signes, facteurs de risque, symptômes, traitements", 17),
            ("PREMIERE_ST2S", "BPH1_MOELLE_AVC",             "Appareil locomoteur et motricité", "Expliquer les conséquences d'une lésion de la moelle épinière ou d'un AVC selon leur localisation", 18),
        
            // Appareil digestif et nutrition
            ("PREMIERE_ST2S", "BPH1_NUTRIMENTS",             "Appareil digestif et nutrition", "Différencier aliments et nutriments, classer les nutriments et associer chacun à son rôle", 19),
            ("PREMIERE_ST2S", "BPH1_BIOMOLECULES_EAU",       "Appareil digestif et nutrition", "Distinguer polymères, dimères et monomères et exposer l'importance de l'eau dans l'organisme", 20),
            ("PREMIERE_ST2S", "BPH1_BESOINS_RATION",         "Appareil digestif et nutrition", "Établir un bilan énergétique, comparer une ration aux valeurs de référence et interpréter l'IMC", 21),
            ("PREMIERE_ST2S", "BPH1_DESEQUILIBRES",          "Appareil digestif et nutrition", "Étudier l'obésité et une malnutrition par carence : facteurs de risque, conséquences, traitements", 22),
            ("PREMIERE_ST2S", "BPH1_APPAREIL_DIGESTIF",      "Appareil digestif et nutrition", "Identifier les organes du tube digestif et les glandes annexes et relier histologie et fonction", 23),
            ("PREMIERE_ST2S", "BPH1_DIGESTION",              "Appareil digestif et nutrition", "Présenter et localiser les phénomènes mécaniques et chimiques de la digestion des biomolécules", 24),
            ("PREMIERE_ST2S", "BPH1_ENZYMES_BILE",           "Appareil digestif et nutrition", "Déduire d'expériences l'action des enzymes digestives, le rôle de la bile et du microbiote intestinal", 25),
            ("PREMIERE_ST2S", "BPH1_ABSORPTION",             "Appareil digestif et nutrition", "Expliquer l'absorption intestinale, l'osmose et les voies sanguine et lymphatique", 26),
            ("PREMIERE_ST2S", "BPH1_FIBROSCOPIE_MALABS",     "Appareil digestif et nutrition", "Présenter la fibroscopie et associer les symptômes d'une malabsorption à leur dysfonctionnement", 27),
        
            // Appareil cardio-vasculaire et circulation sanguine
            ("PREMIERE_ST2S", "BPH1_COEUR",                  "Appareil cardiovasculaire", "Identifier les structures du cœur et les relier à leur fonction", 28),
            ("PREMIERE_ST2S", "BPH1_REVOLUTION_CARDIAQUE",   "Appareil cardiovasculaire", "Identifier les phases de la révolution cardiaque et calculer fC, VES et débit cardiaque", 29),
            ("PREMIERE_ST2S", "BPH1_TISSU_NODAL",            "Appareil cardiovasculaire", "Identifier le tissu nodal et analyser les résultats montrant ses propriétés", 30),
            ("PREMIERE_ST2S", "BPH1_VAISSEAUX",              "Appareil cardiovasculaire", "Schématiser le système circulatoire et relier la paroi des artères, veines et capillaires à leur fonction", 31),
            ("PREMIERE_ST2S", "BPH1_TENSION_ARTERIELLE",     "Appareil cardiovasculaire", "Présenter la mesure de la tension artérielle et repérer une hypertension ou une hypotension", 32),
            ("PREMIERE_ST2S", "BPH1_ARC_REFLEXE",            "Appareil cardiovasculaire", "Construire l'arc réflexe de régulation du rythme cardiaque lors d'une hémorragie", 33),
            ("PREMIERE_ST2S", "BPH1_IMAGERIE_CARDIO",        "Appareil cardiovasculaire", "Expliquer échographie, Doppler, scintigraphie et angiographie et leur intérêt diagnostique", 34),
            ("PREMIERE_ST2S", "BPH1_ECG",                    "Appareil cardiovasculaire", "Relier les ondes d'un ECG au cycle cardiaque, calculer la fréquence et comparer à un ECG pathologique", 35),
            ("PREMIERE_ST2S", "BPH1_ATHEROSCLEROSE",         "Appareil cardiovasculaire", "Expliquer la pathogénie de l'athérosclérose, ses conséquences et les mesures de prévention", 36),
            ("PREMIERE_ST2S", "BPH1_ANGOR_INFARCTUS",        "Appareil cardiovasculaire", "Comparer angor et infarctus du myocarde : signes, ECG, enzymes cardiaques et traitements", 37),
        
            // Appareil respiratoire et échanges gazeux
            ("PREMIERE_ST2S", "BPH1_APPAREIL_RESPIRATOIRE",  "Appareil respiratoire", "Identifier les organes respiratoires et relier la barrière alvéolo-capillaire à sa fonction", 38),
            ("PREMIERE_ST2S", "BPH1_ECHANGES_GAZEUX",        "Appareil respiratoire", "Justifier le sens de diffusion des gaz entre air alvéolaire, sang et tissus", 39),
            ("PREMIERE_ST2S", "BPH1_HEMOGLOBINE",            "Appareil respiratoire", "Exploiter les courbes de saturation de l'hémoglobine et l'effet du pH, du CO2 et de la température", 40),
            ("PREMIERE_ST2S", "BPH1_TRANSPORT_GAZ",          "Appareil respiratoire", "Comparer les formes de transport du dioxygène et du dioxyde de carbone dans le sang", 41),
            ("PREMIERE_ST2S", "BPH1_RESPIRATION_CELL",       "Appareil respiratoire", "Repérer les molécules consommées et produites par la respiration cellulaire et sa localisation", 42),
            ("PREMIERE_ST2S", "BPH1_SPIROMETRIE",            "Appareil respiratoire", "Présenter la spirométrie et déterminer volumes et capacités pulmonaires sur un spirogramme", 43),
            ("PREMIERE_ST2S", "BPH1_ASTHME_TABAGISME",       "Appareil respiratoire", "Étudier l'asthme et le tabagisme : signes, facteurs de risque, traitements et prévention", 44),
        };
        
        // -------------------------------------------------------------------------------------
        // 3. PREMIÈRE ST2S — SCIENCES ET TECHNIQUES SANITAIRES ET SOCIALES (annexe 3 de MENE1901642A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] STSS1 =
        {
            // Pôle thématique — Santé, bien-être et cohésion sociale
            ("PREMIERE_ST2S", "STSS1_SANTE_NOTIONS",         "Santé, bien-être et cohésion sociale", "Montrer la relativité des notions de santé : globale, individuelle, collective, publique", 1),
            ("PREMIERE_ST2S", "STSS1_PREOCCUPATIONS_SP",     "Santé, bien-être et cohésion sociale", "Analyser une question sanitaire et identifier les préoccupations en santé publique", 2),
            ("PREMIERE_ST2S", "STSS1_SOCIALISATION",         "Santé, bien-être et cohésion sociale", "Mobiliser le processus et les instances de socialisation pour appréhender un fait social", 3),
            ("PREMIERE_ST2S", "STSS1_INTEGRATION",           "Santé, bien-être et cohésion sociale", "Identifier le rôle de la socialisation dans l'intégration sociale", 4),
            ("PREMIERE_ST2S", "STSS1_COHESION_NORMES",       "Santé, bien-être et cohésion sociale", "Repérer les facteurs de cohésion sociale et l'influence des normes sociales sur la santé", 5),
            ("PREMIERE_ST2S", "STSS1_INDICATEURS",           "Santé, bien-être et cohésion sociale", "Mobiliser les indicateurs adaptés pour évaluer la santé, le bien-être ou la cohésion d'une population", 6),
            ("PREMIERE_ST2S", "STSS1_INDICATEURS_CRITIQUE",  "Santé, bien-être et cohésion sociale", "Présenter la complémentarité des indicateurs et porter un regard critique sur la mesure", 7),
            ("PREMIERE_ST2S", "STSS1_DONNEES_POPULATION",    "Santé, bien-être et cohésion sociale", "Recueillir, traiter et analyser des données pour caractériser une population", 8),
            ("PREMIERE_ST2S", "STSS1_INEGALITES",            "Santé, bien-être et cohésion sociale", "Repérer contrastes et inégalités au sein des populations et entre elles", 9),
            ("PREMIERE_ST2S", "STSS1_DETERMINANTS",          "Santé, bien-être et cohésion sociale", "Mobiliser les déterminants de santé pour explorer une question de santé", 10),
            ("PREMIERE_ST2S", "STSS1_DETERMINANTS_SOCIAUX",  "Santé, bien-être et cohésion sociale", "Présenter l'impact des déterminants sociaux et analyser les interactions entre déterminants", 11),
            ("PREMIERE_ST2S", "STSS1_DIMENSION_SOCIALE",     "Santé, bien-être et cohésion sociale", "Identifier la dimension sociale des questions de santé", 12),
            ("PREMIERE_ST2S", "STSS1_PROBLEME_SANTE",        "Santé, bien-être et cohésion sociale", "Analyser comment une société identifie un risque sanitaire ou un problème de santé publique", 13),
            ("PREMIERE_ST2S", "STSS1_EPIDEMIOLOGIE",         "Santé, bien-être et cohésion sociale", "Montrer la place relative de l'épidémiologie dans la reconnaissance des problèmes de santé publique", 14),
            ("PREMIERE_ST2S", "STSS1_CRISE_SANITAIRE",       "Santé, bien-être et cohésion sociale", "Analyser les composantes d'une situation de crise sanitaire", 15),
            ("PREMIERE_ST2S", "STSS1_INEGALITES_COHESION",   "Santé, bien-être et cohésion sociale", "Expliquer comment les inégalités sociales et territoriales fragilisent la cohésion sociale", 16),
            ("PREMIERE_ST2S", "STSS1_PRECARITE_EXCLUSION",   "Santé, bien-être et cohésion sociale", "Distinguer précarité, pauvreté et exclusion et montrer que l'exclusion résulte d'un processus", 17),
            ("PREMIERE_ST2S", "STSS1_PROBLEME_SOCIAL",       "Santé, bien-être et cohésion sociale", "Analyser l'émergence d'un problème social et sa reconnaissance par la collectivité", 18),
        
            // Pôle thématique — Protection sociale
            ("PREMIERE_ST2S", "STSS1_PS_ACCES_DROITS",       "Protection sociale", "Montrer que la protection sociale participe de l'accès aux droits", 19),
            ("PREMIERE_ST2S", "STSS1_PS_SYSTEME_FRANCAIS",   "Protection sociale", "Caractériser le système de protection sociale français et le situer au regard d'un autre système", 20),
            ("PREMIERE_ST2S", "STSS1_RISQUE_SOCIAL",         "Protection sociale", "Identifier un risque social et repérer les différentes réponses de protection sociale", 21),
            ("PREMIERE_ST2S", "STSS1_PS_TECHNIQUES",         "Protection sociale", "Présenter les principes et techniques de protection sociale : assurance et assistance", 22),
            ("PREMIERE_ST2S", "STSS1_PS_COMPOSANTES",        "Protection sociale", "Illustrer le caractère complémentaire, subsidiaire ou supplémentaire des composantes du système", 23),
            ("PREMIERE_ST2S", "STSS1_AM_COMPLEMENTAIRE",     "Protection sociale", "Repérer la complémentarité entre assurance maladie et organismes complémentaires", 24),
            ("PREMIERE_ST2S", "STSS1_AM_UNIVERSALITE",       "Protection sociale", "Illustrer le principe d'universalité de l'assurance maladie", 25),
            ("PREMIERE_ST2S", "STSS1_REGIME_PRINCIPAL",      "Protection sociale", "Présenter l'organisation du principal régime de sécurité sociale", 26),
        
            // Pôle thématique — Modes d'intervention sociale et en santé
            ("PREMIERE_ST2S", "STSS1_INTERV_SANTE_MODES",    "Modes d'intervention sociale et en santé", "Caractériser les modes d'intervention en santé : veille, promotion, éducation, prévention, restauration", 27),
            ("PREMIERE_ST2S", "STSS1_ACTION_SANTE_ORIGINE",  "Modes d'intervention sociale et en santé", "Mettre en relation une action de santé avec la question de santé qui en est à l'origine", 28),
            ("PREMIERE_ST2S", "STSS1_ACTION_DETERMINANTS",   "Modes d'intervention sociale et en santé", "Repérer le lien entre actions de santé et déterminants sociaux et territoriaux de santé", 29),
            ("PREMIERE_ST2S", "STSS1_PS_SANTE_POPULATIONS",  "Modes d'intervention sociale et en santé", "Illustrer l'apport de la protection sociale à la santé des populations", 30),
            ("PREMIERE_ST2S", "STSS1_ACTEURS_SANTE",         "Modes d'intervention sociale et en santé", "Présenter le rôle des acteurs et analyser la participation de la personne à une action en santé", 31),
            ("PREMIERE_ST2S", "STSS1_INTERV_SOCIALE_MODES",  "Modes d'intervention sociale et en santé", "Caractériser les modes d'intervention sociale : diagnostic, accompagnement, développement social local", 32),
            ("PREMIERE_ST2S", "STSS1_INTERV_SOCIALE_ORIGINE","Modes d'intervention sociale et en santé", "Mettre en relation une intervention sociale avec la question sociale qui en est à l'origine", 33),
            ("PREMIERE_ST2S", "STSS1_ACTEURS_SOCIAL",        "Modes d'intervention sociale et en santé", "Présenter le rôle des acteurs et la participation de la personne ou du groupe à une intervention sociale", 34),
            ("PREMIERE_ST2S", "STSS1_PS_EXCLUSION",          "Modes d'intervention sociale et en santé", "Illustrer le rôle de la protection sociale dans la lutte contre l'exclusion et les inégalités sociales", 35),
        
            // Pôle méthodologique — Méthodologies appliquées au secteur sanitaire et social
            ("PREMIERE_ST2S", "STSS1_CORPUS_DOCUMENTAIRE",   "Méthodologies appliquées", "Constituer et structurer un corpus documentaire sur un sujet du domaine sanitaire et social", 36),
            ("PREMIERE_ST2S", "STSS1_RECHERCHE_DOC_APPORT",  "Méthodologies appliquées", "Expliquer l'apport de la recherche documentaire à une étude et évaluer la fiabilité d'une source", 37),
            ("PREMIERE_ST2S", "STSS1_ETUDE_INTERET",         "Méthodologies appliquées", "Argumenter l'intérêt d'une étude pour connaître un état de santé ou un fait social, ou pour un projet", 38),
            ("PREMIERE_ST2S", "STSS1_OBJET_ETUDE",           "Méthodologies appliquées", "Mettre en relation un objet d'étude avec la demande, la commande initiale et le contexte institutionnel", 39),
            ("PREMIERE_ST2S", "STSS1_ETHIQUE_ETUDE",         "Méthodologies appliquées", "Repérer les questions éthiques et réglementaires posées par une étude", 40),
            ("PREMIERE_ST2S", "STSS1_METHODE_RECUEIL",       "Méthodologies appliquées", "Argumenter le choix de la méthode, qualitative ou quantitative, et des outils de recueil de données", 41),
            ("PREMIERE_ST2S", "STSS1_ECHANTILLON",           "Méthodologies appliquées", "Présenter le choix de construction de l'échantillon d'une étude", 42),
            ("PREMIERE_ST2S", "STSS1_TRAITEMENT_DONNEES",    "Méthodologies appliquées", "Traiter des données quantitatives, notamment au tableur, pour produire une information", 43),
            ("PREMIERE_ST2S", "STSS1_DIFFUSION_ETUDE",       "Méthodologies appliquées", "Expliquer l'importance de la présentation d'une étude et de sa diffusion", 44),
        };
        
        // -------------------------------------------------------------------------------------
        // 4. TERMINALE ST2S — SCIENCES ET TECHNIQUES SANITAIRES ET SOCIALES (annexe 2 de MENE1921258A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] STSST =
        {
            // Pôle thématique — Politiques et dispositifs de santé publique
            ("TERMINALE_ST2S", "STSST_POLSANTE_ELABORATION",  "Politiques de santé publique", "Présenter le processus d'élaboration d'une politique de santé en la situant dans son contexte", 1),
            ("TERMINALE_ST2S", "STSST_POLSANTE_HISTOIRE",     "Politiques de santé publique", "Retracer l'évolution de la politique et des objectifs de santé", 2),
            ("TERMINALE_ST2S", "STSST_ECHELONS_DEMOCRATIE",   "Politiques de santé publique", "Situer l'élaboration de la politique de santé aux échelons local, national, international et la démocratie sanitaire", 3),
            ("TERMINALE_ST2S", "STSST_POLSANTE_DETERMINANTS", "Politiques de santé publique", "Montrer que la politique de santé vise à agir sur les déterminants de santé", 4),
            ("TERMINALE_ST2S", "STSST_SYSTEME_SANTE",         "Politiques de santé publique", "Présenter l'organisation du système de santé : composantes, gouvernance et financement", 5),
            ("TERMINALE_ST2S", "STSST_PLACE_PERSONNE",        "Politiques de santé publique", "Illustrer la place de la personne dans le système de santé et le système de soins", 6),
            ("TERMINALE_ST2S", "STSST_VEILLE_SANITAIRE",      "Politiques de santé publique", "Présenter un système de veille sanitaire", 7),
            ("TERMINALE_ST2S", "STSST_PROMOTION_PREVENTION",  "Politiques de santé publique", "Analyser une intervention en promotion ou en prévention de la santé", 8),
            ("TERMINALE_ST2S", "STSST_PS_SYSTEME_SOINS",      "Politiques de santé publique", "Présenter la place de la protection sociale dans le système de soins", 9),
            ("TERMINALE_ST2S", "STSST_SOINS_TERRITOIRE",      "Politiques de santé publique", "Montrer la complémentarité des composantes du système de soins sur un territoire", 10),
            ("TERMINALE_ST2S", "STSST_INEGALITES_ACCES_SOINS","Politiques de santé publique", "Analyser les dispositifs de lutte contre les inégalités de santé et pour l'accès aux soins", 11),
        
            // Pôle thématique — Politiques sociales et dispositifs d'action sociale
            ("TERMINALE_ST2S", "STSST_POLSOC_HISTOIRE",       "Politiques sociales et action sociale", "Retracer l'évolution des politiques sociales, de l'identification d'un problème à la politique", 12),
            ("TERMINALE_ST2S", "STSST_POLSOC_COHESION",       "Politiques sociales et action sociale", "Montrer comment une politique sociale participe au bien-être et à la cohésion sociale", 13),
            ("TERMINALE_ST2S", "STSST_USAGER_PLACE",          "Politiques sociales et action sociale", "Illustrer l'évolution de la place de l'usager dans les politiques sociales", 14),
            ("TERMINALE_ST2S", "STSST_POLSOC_PROBLEME",       "Politiques sociales et action sociale", "Présenter une politique sociale au regard d'un problème social contextualisé", 15),
            ("TERMINALE_ST2S", "STSST_FINANCEMENT_AS",        "Politiques sociales et action sociale", "Illustrer la pluralité des sources de financement de l'action sociale", 16),
            ("TERMINALE_ST2S", "STSST_DIAGNOSTIC_BESOINS",    "Politiques sociales et action sociale", "Montrer l'intérêt d'un diagnostic des besoins sociaux sur un territoire pour une politique locale", 17),
            ("TERMINALE_ST2S", "STSST_DISPOSITIF_ANALYSE",    "Politiques sociales et action sociale", "Analyser un dispositif d'une politique sociale : population, missions, acteurs, financement", 18),
            ("TERMINALE_ST2S", "STSST_LUTTE_EXCLUSION",       "Politiques sociales et action sociale", "Étudier un dispositif de lutte contre la précarité, la pauvreté ou l'exclusion sociale", 19),
            ("TERMINALE_ST2S", "STSST_INTERVENTIONS_COMPLEM", "Politiques sociales et action sociale", "Analyser la diversité et la complémentarité des interventions sociales face à un problème social", 20),
        
            // Pôle méthodologique — la démarche de projet
            ("TERMINALE_ST2S", "STSST_PROJET_SPECIFICITES",   "Méthodologies — démarche de projet", "Présenter les spécificités de la démarche de projet en santé-social", 21),
            ("TERMINALE_ST2S", "STSST_PROJET_ANALYSER",       "Méthodologies — démarche de projet", "Analyser une démarche de projet du secteur de la santé ou du secteur social", 22),
            ("TERMINALE_ST2S", "STSST_PROJET_CORPUS",         "Méthodologies — démarche de projet", "Construire un corpus documentaire à l'appui de l'analyse d'une démarche de projet", 23),
            ("TERMINALE_ST2S", "STSST_PROJET_CONTEXTE",       "Méthodologies — démarche de projet", "Identifier le contexte dans lequel s'inscrit un projet", 24),
            ("TERMINALE_ST2S", "STSST_PROJET_DEMARCHE_ETUDE", "Méthodologies — démarche de projet", "Mobiliser la démarche d'étude pour analyser une situation dans le cadre d'un projet", 25),
            ("TERMINALE_ST2S", "STSST_PROJET_APPORT_ETUDE",   "Méthodologies — démarche de projet", "Justifier l'apport d'une étude à la démarche de projet dans laquelle elle s'inscrit", 26),
            ("TERMINALE_ST2S", "STSST_PROJET_PHASES",         "Méthodologies — démarche de projet", "Concevoir tout ou partie d'un projet : étude, plan d'actions, mise en œuvre, évaluation", 27),
            ("TERMINALE_ST2S", "STSST_PROJET_ACTEURS",        "Méthodologies — démarche de projet", "Expliquer le rôle des différents acteurs dans un projet et leur coordination", 28),
            ("TERMINALE_ST2S", "STSST_PROJET_POP_CIBLE",      "Méthodologies — démarche de projet", "Situer la place de la population cible dans la démarche de projet", 29),
            ("TERMINALE_ST2S", "STSST_QUESTION_ORALE",        "Méthodologies — démarche de projet", "Questionner une question de santé ou sociale contextualisée et en tirer un projet d'étude", 30),
        };
        
        // -------------------------------------------------------------------------------------
        // 5. TERMINALE ST2S — CHIMIE, BIOLOGIE ET PHYSIOPATHOLOGIE HUMAINES (annexe 1 de MENE1921258A)
        //    Deux parties à expertises distinctes : Chimie (3 h) et Biologie et physiopathologie humaines (5 h).
        //    Chaque Domaine commence par « Chimie — » ou « Biologie — ».
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] CBPHT =
        {
            // Chimie — Thème 1 : Prévenir et sécuriser
            ("TERMINALE_ST2S", "CBPHT_AIRBAG_BILAN",          "Chimie — Prévenir et sécuriser", "Faire un bilan de matière et utiliser V = n × Vm pour expliquer le gonflement d'un airbag", 1),
            ("TERMINALE_ST2S", "CBPHT_ALCOOTEST",             "Chimie — Prévenir et sécuriser", "Écrire la réaction d'oxydoréduction d'un alcootest et analyser la détection d'une substance illicite", 2),
            ("TERMINALE_ST2S", "CBPHT_DEGRADATION_ALIMENTS",  "Chimie — Prévenir et sécuriser", "Identifier les facteurs de dégradation des aliments et analyser la qualité d'une huile ou d'un beurre", 3),
            ("TERMINALE_ST2S", "CBPHT_CONSERVATION",          "Chimie — Prévenir et sécuriser", "Identifier les facteurs d'une technique de conservation et distinguer procédés physiques et chimiques", 4),
            ("TERMINALE_ST2S", "CBPHT_QUALITE_DJA",           "Chimie — Prévenir et sécuriser", "Contrôler un aliment par dosage et calculer à partir des doses de référence DJA et DJT", 5),
            ("TERMINALE_ST2S", "CBPHT_CONDUCTIVITE",          "Chimie — Prévenir et sécuriser", "Expliquer la solubilité des composés ioniques et interpréter la conductivité d'une eau", 6),
            ("TERMINALE_ST2S", "CBPHT_DOSAGE_CONDUCTIMETRIE", "Chimie — Prévenir et sécuriser", "Mettre en œuvre un dosage conductimétrique et repérer et exploiter l'équivalence", 7),
            ("TERMINALE_ST2S", "CBPHT_POTABILITE_MILIEUX",    "Chimie — Prévenir et sécuriser", "Exploiter les critères de potabilité et les effets des activités humaines sur les eaux", 8),
            ("TERMINALE_ST2S", "CBPHT_AIR_COMPOSITION",       "Chimie — Prévenir et sécuriser", "Exprimer la composition de l'air en fractions molaires et mettre en évidence CO2, H2O et O2", 9),
            ("TERMINALE_ST2S", "CBPHT_GAZ_PARFAIT",           "Chimie — Prévenir et sécuriser", "Utiliser la loi du gaz parfait dans le cas d'une bouteille de dioxygène", 10),
            ("TERMINALE_ST2S", "CBPHT_MONOXYDE_CARBONE",      "Chimie — Prévenir et sécuriser", "Décrire l'action du monoxyde de carbone sur l'hémoglobine et analyser les risques d'inhalation", 11),
            ("TERMINALE_ST2S", "CBPHT_OZONE_GES",             "Chimie — Prévenir et sécuriser", "Distinguer rôle protecteur et nocif de l'ozone et définir un gaz à effet de serre", 12),
            ("TERMINALE_ST2S", "CBPHT_POLLUANTS",             "Chimie — Prévenir et sécuriser", "Différencier macro et micropolluants, polluants primaires et secondaires et leurs origines", 13),
            ("TERMINALE_ST2S", "CBPHT_DEPOLLUTION",           "Chimie — Prévenir et sécuriser", "Analyser l'efficacité d'un procédé de dépollution : adsorption sur charbon actif, ozonation", 14),
        
            // Chimie — Thème 2 : Analyser et diagnostiquer
            ("TERMINALE_ST2S", "CBPHT_ECHOGRAPHIE_DOPPLER",   "Chimie — Analyser et diagnostiquer", "Calculer le temps de parcours d'un ultrason et connaître le principe de l'échographie Doppler", 15),
            ("TERMINALE_ST2S", "CBPHT_ONDES_RAYONS_X",        "Chimie — Analyser et diagnostiquer", "Positionner les rayons X parmi les ondes électromagnétiques et relier fréquence et longueur d'onde", 16),
            ("TERMINALE_ST2S", "CBPHT_RADIOGRAPHIE",          "Chimie — Analyser et diagnostiquer", "Interpréter un cliché radiographique et comparer radiographie et radiothérapie", 17),
            ("TERMINALE_ST2S", "CBPHT_CONTRASTE_IRM",         "Chimie — Analyser et diagnostiquer", "Identifier les groupes fonctionnels d'un produit de contraste et situer les radiofréquences de l'IRM", 18),
            ("TERMINALE_ST2S", "CBPHT_NOYAU_RADIOACTIVITE",   "Chimie — Analyser et diagnostiquer", "Décrire un noyau, identifier des isotopes et la nature d'une émission radioactive α, β-, β+ ou γ", 19),
            ("TERMINALE_ST2S", "CBPHT_DEMI_VIE",              "Chimie — Analyser et diagnostiquer", "Définir la période d'un radio-isotope et la déterminer graphiquement", 20),
            ("TERMINALE_ST2S", "CBPHT_MARQUEURS_RADIOACTIFS", "Chimie — Analyser et diagnostiquer", "Comparer les marqueurs radioactifs d'imagerie, les doses et les précautions d'emploi en milieu médical", 21),
            ("TERMINALE_ST2S", "CBPHT_CONCENTRATIONS",        "Chimie — Analyser et diagnostiquer", "Écrire une dissolution et calculer concentrations et prélèvements pour une application médicale", 22),
            ("TERMINALE_ST2S", "CBPHT_DOSAGE_ETALONNAGE",     "Chimie — Analyser et diagnostiquer", "Déterminer la concentration d'une espèce par étalonnage ou spectrophotométrie", 23),
            ("TERMINALE_ST2S", "CBPHT_ANALYSE_MEDICALE",      "Chimie — Analyser et diagnostiquer", "Interpréter le résultat d'une analyse médicale au regard des normes", 24),
            ("TERMINALE_ST2S", "CBPHT_POLLUANT_SANTE",        "Chimie — Analyser et diagnostiquer", "Analyser dangerosité, traçabilité, bioaccumulation et faibles doses d'un polluant", 25),
            ("TERMINALE_ST2S", "CBPHT_COURBE_CINETIQUE",      "Chimie — Analyser et diagnostiquer", "Interpréter une courbe d'évolution cinétique d'une substance", 26),
            ("TERMINALE_ST2S", "CBPHT_ACIDIFICATION",         "Chimie — Analyser et diagnostiquer", "Expliquer l'acidification des océans et des pluies par les couples acido-basiques du CO2 et du SO2", 27),
        
            // Chimie — Thème 3 : Faire des choix autonomes et responsables
            ("TERMINALE_ST2S", "CBPHT_ACIDE_AMINE_ASYM",      "Chimie — Choix autonomes et responsables", "Définir un acide α-aminé et repérer un atome de carbone asymétrique", 28),
            ("TERMINALE_ST2S", "CBPHT_CHIRALITE",             "Chimie — Choix autonomes et responsables", "Identifier deux énantiomères en représentations de Cram et de Fischer et utiliser la nomenclature D et L", 29),
            ("TERMINALE_ST2S", "CBPHT_PEPTIDE",               "Chimie — Choix autonomes et responsables", "Écrire la condensation de deux acides α-aminés, repérer la liaison peptidique et retrouver les acides aminés", 30),
            ("TERMINALE_ST2S", "CBPHT_PROTEINE_3D",           "Chimie — Choix autonomes et responsables", "Exploiter le lien entre structure tridimensionnelle et action des protéines", 31),
            ("TERMINALE_ST2S", "CBPHT_SAPONIFICATION",        "Chimie — Choix autonomes et responsables", "Écrire hydrolyse et saponification d'un triglycéride, faire un bilan de matière et calculer un rendement", 32),
            ("TERMINALE_ST2S", "CBPHT_LIPIDES_SANTE",         "Chimie — Choix autonomes et responsables", "Relier la structure des acides gras et du cholestérol à leurs effets sur la santé et à leur transport", 33),
            ("TERMINALE_ST2S", "CBPHT_VITAMINES",             "Chimie — Choix autonomes et responsables", "Relier le caractère liposoluble ou hydrosoluble des vitamines A, C et D au besoin journalier", 34),
            ("TERMINALE_ST2S", "CBPHT_IONOGRAMME",            "Chimie — Choix autonomes et responsables", "Interpréter un ionogramme sanguin et le déséquilibre ionique d'une déshydratation", 35),
            ("TERMINALE_ST2S", "CBPHT_TITRAGE_VITAMINE_C",    "Chimie — Choix autonomes et responsables", "Mettre en œuvre un dosage par titrage de la vitamine C d'un aliment ou d'un médicament", 36),
            ("TERMINALE_ST2S", "CBPHT_ADDITIFS",              "Chimie — Choix autonomes et responsables", "Analyser colorants, texturants et arômes alimentaires et doser un colorant par étalonnage", 37),
            ("TERMINALE_ST2S", "CBPHT_MEDICAMENT",            "Chimie — Choix autonomes et responsables", "Commenter une molécule active du XXe siècle et argumenter sur nanomédicaments et médicaments hybrides", 38),
            ("TERMINALE_ST2S", "CBPHT_COSMETIQUES",           "Chimie — Choix autonomes et responsables", "Analyser la composition d'un cosmétique, reconnaître un solvant et situer la chimie verte", 39),
            ("TERMINALE_ST2S", "CBPHT_PROTECTION_SOLAIRE",    "Chimie — Choix autonomes et responsables", "Distinguer UVA et UVB, interpréter l'indice d'une crème solaire et les actions hydratante et antioxydante", 40),
        
            // Biologie — Milieu intérieur et homéostasie
            ("TERMINALE_ST2S", "CBPHT_COMPARTIMENTS",         "Biologie — Milieu intérieur et homéostasie", "Distinguer les compartiments liquidiens et repérer leurs échanges avec le milieu extérieur", 41),
            ("TERMINALE_ST2S", "CBPHT_REIN_NEPHRON",          "Biologie — Milieu intérieur et homéostasie", "Identifier l'appareil urinaire et déduire les fonctions du néphron de la composition plasma-urines", 42),
            ("TERMINALE_ST2S", "CBPHT_GLYCEMIE_REGULATION",   "Biologie — Milieu intérieur et homéostasie", "Construire le schéma de régulation de la glycémie et présenter les notions d'hormone et d'homéostasie", 43),
            ("TERMINALE_ST2S", "CBPHT_DIABETES_SIGNES",       "Biologie — Milieu intérieur et homéostasie", "Comparer les diabètes de type 1 et 2 et expliquer l'origine de l'hyperglycémie pour chacun", 44),
            ("TERMINALE_ST2S", "CBPHT_DIABETES_TRAITEMENTS",  "Biologie — Milieu intérieur et homéostasie", "Citer les conséquences des diabètes et relier traitements et prévention aux facteurs de risque", 45),
            ("TERMINALE_ST2S", "CBPHT_XENOBIOTIQUES",         "Biologie — Milieu intérieur et homéostasie", "Décrire le devenir d'un xénobiotique dans l'organisme et repérer ses conséquences", 46),
        
            // Biologie — Système immunitaire et défense de l'organisme
            ("TERMINALE_ST2S", "CBPHT_AGENTS_PATHOGENES",     "Biologie — Système immunitaire", "Comparer bactéries et virus, leur mode de reproduction, et repérer les étapes d'un cycle viral", 47),
            ("TERMINALE_ST2S", "CBPHT_ANTIBIOGRAMME",         "Biologie — Système immunitaire", "Lire un antibiogramme et repérer les principales cibles cellulaires des antibiotiques", 48),
            ("TERMINALE_ST2S", "CBPHT_RESISTANCE_ATB",        "Biologie — Système immunitaire", "Distinguer résistance naturelle et acquise et relier usage des antibiotiques et sélection de souches", 49),
            ("TERMINALE_ST2S", "CBPHT_SOI_NON_SOI",           "Biologie — Système immunitaire", "Distinguer soi et non-soi, identifier les marqueurs du soi et définir antigène et épitope", 50),
            ("TERMINALE_ST2S", "CBPHT_ORGANES_LYMPHOIDES",    "Biologie — Système immunitaire", "Localiser les organes lymphoïdes primaires et secondaires et identifier les éléments figurés du sang", 51),
            ("TERMINALE_ST2S", "CBPHT_GRIPPE_INNEE",          "Biologie — Système immunitaire", "Relier grippe, barrières cutanéo-muqueuses, réaction inflammatoire et phagocytose", 52),
            ("TERMINALE_ST2S", "CBPHT_REPONSE_HUMORALE",      "Biologie — Système immunitaire", "Présenter l'activation des lymphocytes B et relier l'ultrastructure des plasmocytes à leur fonction", 53),
            ("TERMINALE_ST2S", "CBPHT_ANTICORPS_IGG",         "Biologie — Système immunitaire", "Localiser les sites d'une IgG et expliquer neutralisation, opsonisation et activation du complément", 54),
            ("TERMINALE_ST2S", "CBPHT_REPONSE_CELLULAIRE",    "Biologie — Système immunitaire", "Présenter l'activation des lymphocytes T8 et le rôle des T cytotoxiques dans la cytolyse", 55),
            ("TERMINALE_ST2S", "CBPHT_LT4_COOPERATION",       "Biologie — Système immunitaire", "Montrer le rôle central des lymphocytes T auxiliaires dans les réponses humorale et cellulaire", 56),
            ("TERMINALE_ST2S", "CBPHT_VACCINATION",           "Biologie — Système immunitaire", "Relier réponses primaire et secondaire à la vaccination et la variabilité du virus à la vaccination annuelle", 57),
            ("TERMINALE_ST2S", "CBPHT_ANALYSES_SANGUINES",    "Biologie — Système immunitaire", "Analyser une NFS, des marqueurs de l'inflammation et un sérodiagnostic", 58),
        
            // Biologie — Appareil reproducteur et transmission de la vie
            ("TERMINALE_ST2S", "CBPHT_GAMETOGENESE",          "Biologie — Reproduction et transmission de la vie", "Identifier les organes reproducteurs, les cellules de la gamétogenèse et les stades du follicule", 59),
            ("TERMINALE_ST2S", "CBPHT_FECONDATION_NIDATION",  "Biologie — Reproduction et transmission de la vie", "Localiser fécondation et nidation et différencier embryon et fœtus", 60),
            ("TERMINALE_ST2S", "CBPHT_PLACENTA",              "Biologie — Reproduction et transmission de la vie", "Mettre en évidence les échanges placentaires et justifier la prévention chez la femme enceinte", 61),
            ("TERMINALE_ST2S", "CBPHT_TESTOSTERONE",          "Biologie — Reproduction et transmission de la vie", "Identifier les rôles de la testostérone et schématiser sa régulation par rétrocontrôle négatif", 62),
            ("TERMINALE_ST2S", "CBPHT_CYCLE_FEMININ",         "Biologie — Reproduction et transmission de la vie", "Relier cycles ovarien et utérin, œstrogènes, progestérone et rétrocontrôles négatif et positif", 63),
            ("TERMINALE_ST2S", "CBPHT_CONTRACEPTION",         "Biologie — Reproduction et transmission de la vie", "Expliquer l'action des moyens de contraception et identifier ceux qui protègent des IST", 64),
            ("TERMINALE_ST2S", "CBPHT_INTERRUPTION_GROSSESSE","Biologie — Reproduction et transmission de la vie", "Relever les causes d'interruption physiologique et présenter les IVG médicamenteuse et chirurgicale", 65),
            ("TERMINALE_ST2S", "CBPHT_SUIVI_GROSSESSE",       "Biologie — Reproduction et transmission de la vie", "Présenter l'intérêt de l'échographie et d'une sérologie dans le suivi de grossesse", 66),
            ("TERMINALE_ST2S", "CBPHT_AMNIOCENTESE",          "Biologie — Reproduction et transmission de la vie", "Repérer une anomalie sur un caryotype et citer risques et intérêts d'une amniocentèse", 67),
            ("TERMINALE_ST2S", "CBPHT_INFERTILITE_AMP",       "Biologie — Reproduction et transmission de la vie", "Identifier les causes d'une infertilité et justifier le choix d'une technique d'AMP", 68),
        
            // Biologie — Gènes et transmission de l'information génétique
            ("TERMINALE_ST2S", "CBPHT_ADN_CHROMOSOME",        "Biologie — Gènes et information génétique", "Distinguer nucléotide, ADN, chromatine, chromatides, chromosomes et leurs états au cours du cycle cellulaire", 69),
            ("TERMINALE_ST2S", "CBPHT_TRANSCRIPTION",         "Biologie — Gènes et information génétique", "Localiser transcription et traduction et traduire une séquence à l'aide du code génétique", 70),
            ("TERMINALE_ST2S", "CBPHT_MUTATION",              "Biologie — Gènes et information génétique", "Repérer une mutation ponctuelle et déterminer sa conséquence sur la séquence polypeptidique", 71),
            ("TERMINALE_ST2S", "CBPHT_VOCABULAIRE_GENETIQUE", "Biologie — Gènes et information génétique", "Distinguer gène et allèle, génotype et phénotype, dominance, codominance, récessivité, gonosomes", 72),
            ("TERMINALE_ST2S", "CBPHT_ARBRE_GENEALOGIQUE",    "Biologie — Gènes et information génétique", "Analyser un arbre généalogique et construire un échiquier de croisement", 73),
            ("TERMINALE_ST2S", "CBPHT_CANCER_ETAPES",         "Biologie — Gènes et information génétique", "Décrire les étapes du développement d'un cancer, des mutations aux métastases", 74),
            ("TERMINALE_ST2S", "CBPHT_CANCER_PREVENTION",     "Biologie — Gènes et information génétique", "Repérer agents mutagènes et facteurs de risque d'un cancer et les relier à la prévention", 75),
            ("TERMINALE_ST2S", "CBPHT_CANCER_DIAGNOSTIC",     "Biologie — Gènes et information génétique", "Montrer l'intérêt de l'anatomopathologie, de l'imagerie et des marqueurs tumoraux dans le dépistage", 76),
            ("TERMINALE_ST2S", "CBPHT_CANCER_TRAITEMENTS",    "Biologie — Gènes et information génétique", "Relier traitements anticancéreux et mécanismes physiopathologiques et expliquer leurs effets secondaires", 77),
            ("TERMINALE_ST2S", "CBPHT_TERMINOLOGIE",          "Biologie — Gènes et information génétique", "Analyser les termes médicaux de terminale à partir de leurs racines, préfixes et suffixes", 78),
        };
    }
}
