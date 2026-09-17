using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// Sème les échéances de révision du référentiel — la version structurée
    /// du tableau de vérification qui vivait en commentaire dans
    /// <see cref="ReferentielSeeder"/> — et la sentinelle qui veille sur
    /// tout le reste.
    ///
    /// REPRIS TEL QUEL DEPUIS CE TABLEAU, vérifié le 5 septembre 2026. Seules
    /// les échéances FUTURES à cette date y figurent — celles déjà passées à
    /// cette date-là sont considérées couvertes par cette même vérification.
    /// Idempotent, comme le reste du seed : sûr à rejouer à chaque démarrage.
    /// Une adresse corrigée ici atteint la ligne déjà en base (voir
    /// `SeedSiAbsenteAsync`).
    ///
    /// « Rentrée AAAA » est prise au 1er septembre de l'année indiquée — la
    /// date précise de rentrée varie de quelques jours d'une académie à
    /// l'autre, ce qui n'a aucune importance pour une alerte à 60 jours.
    ///
    /// LES ADRESSES (`url`) NE SONT POSÉES QUE LORSQU'ELLES ONT ÉTÉ LUES ET
    /// CONFRONTÉES AU TITRE ATTENDU — jamais devinées à partir d'un numéro
    /// de BO. Une adresse fausse serait pire qu'aucune adresse : voir
    /// <see cref="EcheanceReferentiel.Url"/>.
    ///
    /// education.gouv.fr, ET NON LÉGIFRANCE — mesuré le 13/09/2026, avec le
    /// User-Agent du worker : education.gouv.fr répond 200 et le texte de sa
    /// région principale est stable d'une lecture à l'autre ; Légifrance
    /// sert par moments une page-défi « Enable JavaScript » qu'un hachage
    /// prendrait pour un changement de texte. Une première version avait
    /// choisi Légifrance sur la foi d'un 403 qui venait de l'outil de
    /// lecture, pas du site.
    /// </summary>
    public static class EcheanceReferentielSeeder
    {
        /// <summary>
        /// LA PAGE-CARREFOUR DES PROGRAMMES, de la maternelle au lycée. Elle
        /// change quand un texte est publié ou déplacé — pour N'IMPORTE
        /// QUELLE matière, y compris celles qu'aucune échéance ne suit. C'est
        /// le seul filet contre l'inconnu, et il est surveillé en permanence.
        /// Lue et jugée stable le 13/09/2026 (quatre lectures, une empreinte).
        /// </summary>
        private const string UrlCarrefourProgrammes = "https://www.education.gouv.fr/programmes-scolaires-41483";

        // Programme de mathématiques du cycle 4 (5e/4e/3e) : arrêté du
        // 18/02/2026, BO n° 10 du 5 mars 2026 — DÉJÀ PUBLIÉ, applicable
        // progressivement (5e à la rentrée 2026, 4e en 2027, 3e en 2028).
        // Une seule et même annexe pour les trois niveaux.
        private const string UrlMathsCycle4 = "https://www.education.gouv.fr/bo/2026/Hebdo10/MENE2602912A";

        // Langues vivantes à l'école élémentaire (cycles 2 et 3) : arrêté du
        // 26/02/2026, BO n° 12 du 19 mars 2026 — déjà publié.
        private const string UrlLanguesElementaire = "https://www.education.gouv.fr/bo/2026/Hebdo12/MENE2602911A";

        // Histoire-géographie et EPS, cycles 2 et 3 : arrêté du 22/04/2026,
        // BO n° 22 du 28 mai 2026 — déjà publié.
        private const string UrlHistoireGeoCycle23 = "https://www.education.gouv.fr/bo/2026/Hebdo22/MENE2608631A";

        // Spécialité de mathématiques, terminale générale : arrêté du
        // 26/02/2026, BO n° 14 du 2 avril 2026 — déjà publié.
        private const string UrlMathsSpeTleGenerale = "https://www.education.gouv.fr/bo/2026/Hebdo14/MENE2602919A";

        // Langues vivantes du collège et du lycée GT : arrêté du 5/05/2025,
        // BO n° 22 du 29 mai 2025 — déjà publié, applicable par niveau.
        private const string UrlLanguesCollegeLycee = "https://www.education.gouv.fr/bo/2025/Hebdo22/MENE2504621A";

        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            var repository = new Repositories.EcheanceReferentielRepository(context);

            // LA SENTINELLE EN PREMIER. Sa « date » n'en est pas une : la
            // clé naturelle en veut une, et celle-ci dit simplement « depuis
            // toujours ». Le worker ne la lit jamais pour une sentinelle.
            await repository.SeedSiAbsenteAsync(
                "Tous les programmes", "Toutes les classes", new DateTime(2026, 9, 1),
                dateConnue: false, texteOfficiel: "Page-carrefour des programmes scolaires (ministère)",
                notes: "Surveillée en permanence. Un changement ici signale un texte publié ou "
                    + "déplacé pour n'importe quelle matière — y compris celles qu'aucune échéance "
                    + "ne suit encore.",
                url: UrlCarrefourProgrammes, sentinelle: true, ct: ct);

            // LES CODES (`matieresCodes`, `niveauxCodes`) SONT CEUX DES TABLES
            // `Matiere` ET `NiveauScolaire` — c'est par eux que l'écran
            // d'administration range chaque échéance sous la bonne matière de
            // la bonne classe. Le libellé reste libre et lisible ; il n'est
            // jamais rapproché par ressemblance.
            await repository.SeedSiAbsenteAsync(
                "Français et mathématiques", "4e", new DateTime(2027, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 10 du 5 mars 2026",
                notes: "Texte déjà publié. Déjà en partie ajouté au référentiel : les compétences "
                    + "valables sous l'ancien texte sont présentes. Vérifier qu'aucune ne manque et "
                    + "qu'aucune notion retirée ne traîne encore.",
                url: UrlMathsCycle4, matieresCodes: "FRANCAIS;MATHS", niveauxCodes: "QUATRIEME", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Français et mathématiques", "3e", new DateTime(2028, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 10 du 5 mars 2026", notes: "Texte déjà publié.",
                url: UrlMathsCycle4, matieresCodes: "FRANCAIS;MATHS", niveauxCodes: "TROISIEME", ct: ct);

            // À l'élémentaire, « Langues vivantes » = l'anglais : l'espagnol ne
            // s'y enseigne pas.
            await repository.SeedSiAbsenteAsync(
                "Langues vivantes", "CE1, CE2, CM2", new DateTime(2027, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 12 du 19 mars 2026", notes: "Texte déjà publié.",
                url: UrlLanguesElementaire, matieresCodes: "ANGLAIS", niveauxCodes: "CE1;CE2;CM2", ct: ct);

            // Collège (4e, 3e) : UN SEUL ARRÊTÉ POUR TOUTES LES LANGUES —
            // MENE2504621A, annexe 3 pour l'anglais, annexe 9 pour l'espagnol,
            // mêmes dates (article 4 : 4e en 2027, 3e en 2028). L'espagnol
            // rejoint donc ces lignes plutôt que d'en doubler l'alerte. Adresse
            // lue le 14/09/2026 (ReferentielEspagnol) et relue avec le
            // User-Agent du worker : 200, même empreinte sur cinq lectures.
            await repository.SeedSiAbsenteAsync(
                "Langues vivantes", "4e", new DateTime(2027, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 22 du 29/05/2025",
                notes: "Anglais : annexe 3. Espagnol : annexe 9 — vérifier les compétences de 4e contre elle.",
                url: UrlLanguesCollegeLycee, matieresCodes: "ANGLAIS;ESPAGNOL", niveauxCodes: "QUATRIEME", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Langues vivantes", "3e", new DateTime(2028, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 22 du 29/05/2025",
                notes: "Anglais : annexe 3. Espagnol : annexe 9 — vérifier les compétences de 3e contre elle.",
                url: UrlLanguesCollegeLycee, matieresCodes: "ANGLAIS;ESPAGNOL", niveauxCodes: "TROISIEME", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Histoire-géographie", "CM2, 6e", new DateTime(2027, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 22 du 28 mai 2026", notes: "Texte déjà publié.",
                url: UrlHistoireGeoCycle23, matieresCodes: "HISTOIRE_GEO", niveauxCodes: "CM2;SIXIEME", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Sciences et technologie", "CE1", new DateTime(2027, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 24 du 11/06/2026", notes: null,
                matieresCodes: "SCIENCES", niveauxCodes: "CE1", ct: ct);

            // SÉPARÉ EN DEUX LIGNES, ET C'EST VOULU : générale et technologique
            // sont deux arrêtés distincts, avec deux adresses distinctes. Les
            // regrouper aurait posé une seule adresse sur deux textes.
            await repository.SeedSiAbsenteAsync(
                "Lycée général", "Terminale (spécialité mathématiques)", new DateTime(2027, 9, 1),
                dateConnue: true, texteOfficiel: "BO n° 14 du 2 avril 2026",
                notes: "Texte déjà publié — arrêté du 26/02/2026, même série que la première déjà "
                    + "appliquée à la rentrée 2026.",
                url: UrlMathsSpeTleGenerale, matieresCodes: "MATHS", niveauxCodes: "TERMINALE", ct: ct);

            // Terminale technologique : l'adresse précise de SON arrêté n'a
            // pas été retrouvée le 13/09/2026 (celle lue portait le titre de
            // la PREMIÈRE technologique, pas de la terminale). Laissée vide
            // plutôt que devinée.
            await repository.SeedSiAbsenteAsync(
                "Lycée technologique", "Terminale (mathématiques)", new DateTime(2027, 9, 1),
                dateConnue: true, texteOfficiel: "Arrêté du 26/02/2026, série BO n° 14 d'avril 2026",
                notes: "Texte déjà publié — adresse précise à rechercher, non devinée à partir de "
                    + "celle de la spécialité générale.",
                matieresCodes: "MATHS", niveauxCodes: "TERMINALE_TECHNO", ct: ct);

            // LES ÉPREUVES D'EXAMEN AUSSI SE REVÉRIFIENT CHAQUE ANNÉE — voir
            // `ExamenSeeder`. Sans nouvelle ligne DNB_2028 écrite à temps, la
            // section « Préparation au brevet » disparaît le 1er août 2027 : on
            // préfère rien à un examen périmé, et cette alerte l'évite.
            await repository.SeedSiAbsenteAsync(
                "Brevet (épreuves et périmètre)", "3e", new DateTime(2027, 8, 1),
                dateConnue: true, texteOfficiel: "Arrêté du 31/12/2015 modifié (10/04/2025, 17/07/2026)",
                notes: "Avant la session 2028 : vérifier que les épreuves écrites n'ont pas changé, puis "
                    + "écrire l'examen DNB_2028 dans ExamenSeeder. Français et maths restent sur le cycle 4 "
                    + "jusqu'à ce que le programme de 3e soit en vigueur (rentrée 2028, donc session 2029).",
                url: "https://www.education.gouv.fr/le-diplome-national-du-brevet-10613",
                matieresCodes: "FRANCAIS;MATHS;HISTOIRE_GEO;PHYSIQUE_CHIMIE;SVT", niveauxCodes: "TROISIEME", ct: ct);

            // Même raison pour le bac : sans examen de la session suivante écrit à
            // temps, les sections « Préparation au bac » et « Préparation au bac
            // de français » disparaissent le 1er août 2027.
            await repository.SeedSiAbsenteAsync(
                "Bac (épreuves et périmètre)", "1re et Tle", new DateTime(2027, 8, 1),
                dateConnue: true, texteOfficiel: "Arrêté du 16/07/2018 modifié (10/06/2025) et notes de service des épreuves",
                notes: "Avant l'année 2027-2028 : vérifier les épreuves écrites (français anticipé, philosophie, "
                    + "spécialités de chaque série), puis écrire les examens de 2028 dans ExamenSeeder.",
                matieresCodes: "FRANCAIS;PHILOSOPHIE;DROIT_ECONOMIE;MANAGEMENT;SCIENCES_GESTION;SANITAIRE_SOCIAL;BIOLOGIE_HUMAINE;PHYSIQUE_CHIMIE;BIOTECHNOLOGIES;SPCL",
                niveauxCodes: "PREMIERE;PREMIERE_TECHNO;TERMINALE;TERMINALE_STMG;TERMINALE_ST2S;TERMINALE_STL", ct: ct);

            // ------------------------------------------------------------------
            // LES RÉFÉRENTIELS DU 14/09/2026 ENTRENT DANS LA VEILLE — voulu par
            // Camara le même jour : « tous les nouveaux référentiels doivent
            // rentrer dans le worker pour la mise à jour des programmes ».
            //
            // ÉDUSCOL ET NON LE BULLETIN OFFICIEL, et c'est le point. Une page de
            // BO ne change jamais : un nouveau programme paraît dans un AUTRE
            // numéro, que le worker ne relirait pas. Les pages « Programmes et
            // ressources » d'Éduscol, elles, changent quand le texte en vigueur
            // change. Chacune a été lue le 14/09/2026 avec le User-Agent du
            // worker : réponse 200, titre attendu, même empreinte d'une lecture
            // à l'autre. Deux d'entre elles (épreuve anticipée de mathématiques,
            // langues vivantes GT) ont opposé UNE fois un défi anti-robot avant
            // de répondre normalement : le worker classe ce défi en injoignable,
            // jamais en changement.
            //
            // Les textes cités sont ceux des tableaux de provenance des
            // référentiels (Referentiels/Techno, ReferentielEspagnol) — pas un
            // numéro de plus.
            //
            // Programmes stables : date de rappel provisoire au 1er août 2027,
            // pour une revérification annuelle avant la rentrée. Ce sont les
            // pages qui donnent l'alerte si un texte sort avant.
            // ------------------------------------------------------------------
            await repository.SeedSiAbsenteAsync(
                "STMG (spécialités)", "1re et Tle STMG", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés du 17/01/2019 (MENE1901646A) et du 19/07/2019 (MENE1921262A, modifié le 25/01/2024)",
                notes: "Revérification annuelle. La page Éduscol de la série signale un nouveau programme.",
                url: "https://eduscol.education.gouv.fr/5862/programmes-et-ressources-en-serie-stmg",
                matieresCodes: "SCIENCES_GESTION;MANAGEMENT;DROIT_ECONOMIE", niveauxCodes: "PREMIERE_STMG;TERMINALE_STMG", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "ST2S (spécialités)", "1re et Tle ST2S", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés du 17/01/2019 (MENE1901642A) et du 19/07/2019 (MENE1921258A)",
                notes: "Revérification annuelle. La page Éduscol de la série signale un nouveau programme.",
                url: "https://eduscol.education.gouv.fr/5847/programmes-et-ressources-en-serie-st2s",
                matieresCodes: "SANITAIRE_SOCIAL;BIOLOGIE_HUMAINE;PHYSIQUE_CHIMIE", niveauxCodes: "PREMIERE_ST2S;TERMINALE_ST2S", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "STL (spécialités)", "1re et Tle STL", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés du 17/01/2019 (MENE1901645A) et du 19/07/2019 (MENE1921260A)",
                notes: "Revérification annuelle. La page Éduscol de la série signale un nouveau programme.",
                url: "https://eduscol.education.gouv.fr/5859/programmes-et-ressources-en-serie-stl",
                matieresCodes: "BIOTECHNOLOGIES;SPCL", niveauxCodes: "PREMIERE_STL;TERMINALE_STL", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Français (voie technologique)", "1re technologique", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêté du 17/01/2019 (MENE1901575A, annexe 2), œuvres renouvelées chaque année",
                notes: "Revérification annuelle : le programme d'œuvres change chaque rentrée.",
                url: "https://eduscol.education.gouv.fr/5793/programmes-et-ressources-en-francais-voie-gt",
                matieresCodes: "FRANCAIS", niveauxCodes: "PREMIERE_TECHNO", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Histoire-géographie et EMC (voie technologique)", "1re et Tle technologiques", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901577A (annexe 3) et MENE1921243A (annexe 2) ; EMC : MENE2413934A",
                notes: "Revérification annuelle.",
                url: "https://eduscol.education.gouv.fr/5799/programmes-et-ressources-en-histoire-geographie-voie-gt",
                matieresCodes: "HISTOIRE_GEO", niveauxCodes: "PREMIERE_TECHNO;TERMINALE_TECHNO", ct: ct);

            // La page Éduscol de la philosophie (5826), trouvée par l'audit de la
            // voie générale le même jour. Elle porte aussi des ressources de
            // séminaires : son empreinte peut changer sans que le programme change.
            await repository.SeedSiAbsenteAsync(
                "Philosophie (voie technologique)", "Tle technologique", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêté du 19/07/2019 (MENE1921238A, annexe 2)",
                notes: "Revérification annuelle. Un changement de la page peut n'être qu'une ressource ajoutée.",
                url: "https://eduscol.education.gouv.fr/5826/programmes-et-ressources-en-philosophie-voie-gt",
                matieresCodes: "PHILOSOPHIE", niveauxCodes: "TERMINALE_TECHNO", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Philosophie (voie générale)", "Tle générale", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêté du 19/07/2019 (MENE1921238A) ; épreuve MENE2001789N",
                notes: "Revérification annuelle. Un changement de la page peut n'être qu'une ressource ajoutée.",
                url: "https://eduscol.education.gouv.fr/5826/programmes-et-ressources-en-philosophie-voie-gt",
                matieresCodes: "PHILOSOPHIE", niveauxCodes: "TERMINALE", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Physique-chimie (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901635A (1re) et MENE1921249A (Tle) ; épreuve MENE2001798N",
                notes: "Revérification annuelle. La note organisant l'ECE de 2027 n'était pas parue au 14/09/2026.",
                url: "https://eduscol.education.gouv.fr/5829/programmes-et-ressources-en-physique-chimie-voie-gt",
                matieresCodes: "PHYSIQUE_CHIMIE", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "SVT (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901648A (1re) et MENE1921252A (Tle) ; épreuve MENE2001799N",
                notes: "Revérification annuelle. La note organisant l'ECE de 2027 n'était pas parue au 14/09/2026.",
                url: "https://eduscol.education.gouv.fr/5835/programmes-et-ressources-en-sciences-de-la-vie-et-de-la-terre-voie-gt",
                matieresCodes: "SVT", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "LLCA latin et grec (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901582A (1re) et MENE1921257A (Tle) ; épreuve MENE2001795N ; limitatif MENE2605298N",
                notes: "Œuvres de terminale valables pour 2026-2027 et 2027-2028 : les lignes « Œuvres au "
                    + "programme » sont à revoir pour 2028-2029.",
                url: "https://eduscol.education.gouv.fr/5808/programmes-et-ressources-en-langues-et-cultures-de-l-antiquite-voie-gt",
                matieresCodes: "LLCA_LATIN;LLCA_GREC", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            // Lycée GT, toutes classes déjà sur le texte de 2025 (seconde en 2025,
            // première et terminale en 2026). L'espagnol du collège est suivi
            // plus haut, avec l'anglais, sur les lignes « Langues vivantes ».
            await repository.SeedSiAbsenteAsync(
                "Espagnol LV2 (lycée)", "2de, 1re, Tle", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêté du 5/05/2025 (MENE2504621A, annexe 10)",
                notes: "Revérification annuelle. La page Éduscol des langues vivantes signale un nouveau texte.",
                url: "https://eduscol.education.gouv.fr/5811/programmes-et-ressources-en-langues-vivantes-voie-gt",
                matieresCodes: "ESPAGNOL", niveauxCodes: "SECONDE;PREMIERE;TERMINALE", ct: ct);

            // Les épreuves du bac, par les pages Éduscol qui les décrivent. Notes
            // de service : celles lues pour `ExamenSeeder`.
            await repository.SeedSiAbsenteAsync(
                "Bac STMG (épreuves)", "Tle STMG", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Notes de service MENE2001095N (consolidée) et MENE2323020N",
                notes: "Revérifier les épreuves avant d'écrire l'examen BAC_STMG_2028.",
                url: "https://eduscol.education.gouv.fr/5619/baccalaureat-technologique-serie-sciences-et-technologies-du-management-et-de-la-gestion-stmg",
                matieresCodes: "DROIT_ECONOMIE;MANAGEMENT;SCIENCES_GESTION", niveauxCodes: "TERMINALE_STMG", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Bac ST2S (épreuves)", "Tle ST2S", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Note de service MENE2001091N (consolidée)",
                notes: "Revérifier les épreuves avant d'écrire l'examen BAC_ST2S_2028.",
                url: "https://eduscol.education.gouv.fr/5634/baccalaureat-technologique-serie-sciences-et-technologies-de-la-sante-et-du-social-st2s",
                matieresCodes: "SANITAIRE_SOCIAL;BIOLOGIE_HUMAINE;PHYSIQUE_CHIMIE", niveauxCodes: "TERMINALE_ST2S", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Bac technologique (épreuves terminales)", "Tle STL et philosophie", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Notes de service MENE2001092N (STL) et MENE2001090N (philosophie)",
                notes: "Revérifier les épreuves avant d'écrire les examens de 2028.",
                url: "https://eduscol.education.gouv.fr/5616/les-epreuves-terminales-du-baccalaureat-technologique",
                matieresCodes: "BIOTECHNOLOGIES;SPCL;PHILOSOPHIE", niveauxCodes: "TERMINALE_STL;TERMINALE_TECHNO", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Épreuve anticipée de mathématiques", "1re", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêté du 10/06/2025 (MENE2508110A) ; note de service MENE2515469N",
                notes: "Première passation le 21/06/2027 (au titre de la session 2028), sur les programmes de "
                    + "première de 2026. La liste des automatismes 2026-2027 n'était pas publiée au 14/09/2026.",
                url: "https://eduscol.education.gouv.fr/5688/epreuve-anticipee-de-mathematiques-aux-baccalaureats-general-et-technologique",
                matieresCodes: "MATHS", niveauxCodes: "PREMIERE;PREMIERE_TECHNO", ct: ct);

            // ------------------------------------------------------------------
            // LES SPÉCIALITÉS DE LA VOIE GÉNÉRALE (14/09/2026). Même règle : la
            // page Éduscol « Programmes et ressources » de chacune, lue trois fois
            // avec le User-Agent du worker. Textes cités : ceux des tableaux de
            // provenance de Referentiels/Generale/.
            // ------------------------------------------------------------------
            await repository.SeedSiAbsenteAsync(
                "HGGSP (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901576A (1re) et MENE1921254A (Tle) ; épreuve MENE2521923N",
                notes: "Revérification annuelle. Les thèmes évaluables à l'écrit tournent : 2, 4, 5, 6 en 2027 ; "
                    + "1, 2, 3, 5 en 2028. Mettre à jour l'épreuve de l'examen de 2028.",
                url: "https://eduscol.education.gouv.fr/5802/programmes-et-ressources-en-histoire-geographie-geopolitique-et-sciences-politiques-voie-g",
                matieresCodes: "HGGSP", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            // Une lecture sur trois a reçu le défi anti-robot : compté en injoignable.
            await repository.SeedSiAbsenteAsync(
                "HLP (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901578A (1re) et MENE1921255A (Tle) ; épreuve MENE2001793N modifiée par MENE2323020N",
                notes: "Revérification annuelle.",
                url: "https://eduscol.education.gouv.fr/5805/programmes-et-ressources-en-humanites-litterature-et-philosophie-voie-g",
                matieresCodes: "HLP", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "SES (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901639A (1re) et MENE1921253A (Tle) ; épreuve MENE2001800N, MENE2416667N",
                notes: "Revérification annuelle. Les questionnements évaluables sont fixés par MENE2416667N "
                    + "(depuis la session 2025) : vérifier qu'aucune note ne les change pour 2028.",
                url: "https://eduscol.education.gouv.fr/5838/programmes-et-ressources-en-sciences-economiques-et-sociales-voie-gt",
                matieresCodes: "SES", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "NSI (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901633A (1re) et MENE1921247A (Tle) ; épreuve MENE2516123N",
                notes: "Revérification annuelle.",
                url: "https://eduscol.education.gouv.fr/5823/programmes-et-ressources-en-numerique-et-sciences-informatiques-voie-g",
                matieresCodes: "NSI", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            // Une page pour les trois LLCER : les programmes limitatifs d'œuvres y
            // changent chaque printemps.
            await repository.SeedSiAbsenteAsync(
                "LLCER anglais, AMC et LLCER espagnol (spécialités)", "1re et Tle générales", new DateTime(2027, 5, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901590A et MENE1921256A (LLCER), MENE2017287A et MENE2017292A (AMC) ; limitatifs MENE2504606N, MENE2611474N",
                notes: "Nouveau programme limitatif de première attendu au printemps 2027 : mettre à jour les "
                    + "lignes d'œuvres et l'épreuve de 2028.",
                url: "https://eduscol.education.gouv.fr/5814/programmes-et-ressources-en-langues-litteratures-et-cultures-etrangeres-et-regionales-voie-g",
                matieresCodes: "LLCER_ANGLAIS;AMC;LLCER_ESPAGNOL", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            await repository.SeedSiAbsenteAsync(
                "Sciences de l'ingénieur (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêtés MENE1901640A (SI) et MENE1921269A (sciences physiques) ; épreuve MENE2408179N",
                notes: "Revérification annuelle.",
                url: "https://eduscol.education.gouv.fr/5832/programmes-et-ressources-en-sciences-de-l-ingenieur-voie-gt",
                matieresCodes: "SI", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            // Pas de page propre à l'EPPCS : sa rubrique est dans la page EPS voie GT.
            await repository.SeedSiAbsenteAsync(
                "EPPCS (spécialité)", "1re et Tle générales", new DateTime(2027, 8, 1),
                dateConnue: false, texteOfficiel: "Arrêté MENE2116606A ; épreuve MENE2121283N modifiée par MENE2317750N",
                notes: "Revérification annuelle. Page EPS voie GT : la première lecture tombe souvent sur le défi anti-robot.",
                url: "https://eduscol.education.gouv.fr/5784/programmes-et-ressources-en-education-physique-et-sportive-eps-voie-gt",
                matieresCodes: "EPPCS", niveauxCodes: "PREMIERE;TERMINALE", ct: ct);

            // LES ARTS : un programme limitatif par enseignement, renouvelé par
            // moitié ou par tiers chaque année. Rappel au 1er mai, avant la note.
            foreach (var (libelle, code, url) in new[]
            {
                ("Arts plastiques (spécialité)", "ARTS_PLASTIQUES", "https://eduscol.education.gouv.fr/5772/programmes-et-ressources-en-arts-plastiques-voie-gt"),
                ("Histoire des arts (spécialité)", "HISTOIRE_ARTS", "https://eduscol.education.gouv.fr/5796/programmes-et-ressources-en-histoire-des-arts-voie-gt"),
                ("Cinéma-audiovisuel (spécialité)", "CINEMA_AUDIOVISUEL", "https://eduscol.education.gouv.fr/5775/programmes-et-ressources-en-cinema-audiovisuel-voie-gt"),
                ("Théâtre (spécialité)", "THEATRE", "https://eduscol.education.gouv.fr/5865/programmes-et-ressources-en-theatre-voie-gt"),
                ("Arts du cirque (spécialité)", "ARTS_CIRQUE", "https://eduscol.education.gouv.fr/5769/programmes-et-ressources-en-arts-du-cirque-voie-gt"),
            })
            {
                await repository.SeedSiAbsenteAsync(
                    libelle, "1re et Tle générales", new DateTime(2027, 5, 1),
                    dateConnue: false, texteOfficiel: "Arrêtés MENE1901567A et MENE1921245A ; épreuve MENE2121271N ; limitatifs MENE2536492N",
                    notes: "Le programme limitatif change chaque année : mettre à jour les lignes « Programme "
                        + "limitatif » ou « Questions limitatives » et l'épreuve de 2028.",
                    url: url, matieresCodes: code, niveauxCodes: "PREMIERE;TERMINALE", ct: ct);
            }

            // TEXTE PAS ENCORE PUBLIÉ : date de rappel provisoire, pas une
            // vraie échéance. Revoir à cette date si le texte est sorti ; si
            // oui, corriger cette ligne avec sa vraie date de bascule et son
            // adresse. D'ici là, c'est la sentinelle qui veille.
            await repository.SeedSiAbsenteAsync(
                "Physique-chimie et SVT (cycle 4)", "5e, 4e, 3e", new DateTime(2027, 1, 1),
                dateConnue: false, texteOfficiel: null,
                notes: "En consultation nationale en mai-juin 2026. À surveiller.",
                matieresCodes: "PHYSIQUE_CHIMIE;SVT", niveauxCodes: "CINQUIEME;QUATRIEME;TROISIEME", ct: ct);
        }
    }
}
