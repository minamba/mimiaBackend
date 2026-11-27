using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Dal.Repositories;
using M = SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// LES DATES OFFICIELLES DE LA SESSION 2027 — semées le 25/11/2026, lues dans le texte.
    ///
    /// SOURCE : note de service du 24/08/2026, « Calendrier 2027 des épreuves du diplôme national du brevet, du
    /// baccalauréat… », NOR MENE2622686N, BO spécial n° 2 du 25 août 2026 (lue sur la copie Internet Archive du
    /// 06/11/2026), et ses annexes I (brevet, session normale), II (brevet, remplacement), III (bac général), IV (bac
    /// technologique) et V (bac professionnel), lues en PDF le 25/11/2026. Horaires de la MÉTROPOLE (heure de Paris).
    /// Les remplacements du bac général et technologique viennent de la page Éduscol « Dates des examens 2027 »
    /// (5697) : les annexes VI et VII n'ont pas été lues, d'où un remplacement laissé vide pour la philosophie et les
    /// spécialités.
    ///
    /// SEMÉ SI ABSENT, JAMAIS RÉÉCRIT : une fois en base, une date ne change que par une proposition de la veille que
    /// Camara valide (onglet « Sessions d'examens »). Réécrire ici écraserait sa validation.
    /// </summary>
    public static class CalendrierExamensSeeder
    {
        public const string Note = "https://www.education.gouv.fr/bo/2026/Special2/MENE2622686N";
        private const string NoteLibelle = "Note de service MENE2622686N, BO spécial n° 2 du 25 août 2026";
        private const string Doc = "https://www.education.gouv.fr/sites/default/files/document/";
        private const string AnnexeBrevet = Doc + "annexe-i-session-normale-2027-calendrier-des-epreuves-ecrites-du-diplome-national-du-brevet-519820.pdf";
        private const string AnnexeGeneral = Doc + "annexe-iii-session-2027-calendrier-des-epreuves-ecrites-du-baccalaureat-general-519826.pdf";
        private const string AnnexeTechno = Doc + "annexe-iv-session-2027-calendrier-des-epreuves-ecrites-du-baccalaureat-technologique-519829.pdf";
        private const string AnnexePro = Doc + "annexe-v-session-2027-calendrier-des-epreuves-ecrites-d-enseignement-general-du-baccalaureat.pdf";

        private static readonly DateTime J14 = new(2027, 6, 14), J15 = new(2027, 6, 15), J16 = new(2027, 6, 16),
            J17 = new(2027, 6, 17), J18 = new(2027, 6, 18), J21 = new(2027, 6, 21);

        private static TimeSpan H(int h, int m = 0) => new(h, m, 0);

        private static M.DateEpreuveOfficielle D(
            string code, DateTime jour, TimeSpan debut, TimeSpan fin, string annexe, string extrait,
            DateTime? remplacement = null, DateTime? autreJour = null) => new()
            {
                EpreuveCode = code,
                Nature = autreJour is null ? M.CalendrierExamens.Jour : M.CalendrierExamens.UnDesDeux,
                DateEpreuve = jour,
                DateFin = autreJour,
                HeureDebut = debut,
                HeureFin = fin,
                RemplacementLe = remplacement,
                SourceLibelle = NoteLibelle,
                SourceUrl = Note,
                AnnexeUrl = annexe,
                Extrait = extrait,
            };

        public static IReadOnlyList<M.DateEpreuveOfficielle> Session2027()
        {
            var spe = "Annexe III : spécialité le mercredi 16 ou le jeudi 17 juin 2027 selon la convocation "
                    + "(« un enseignement de spécialité par jour »)";
            var philo = "« Les épreuves de philosophie sont fixées le lundi 14 juin 2027 matin. » Annexe : 8 h – 12 h.";

            return
            [
                // ------------------------------------------------------------------ BREVET (annexes I et II)
                D("DNB_2027_FRANCAIS", new(2027, 6, 24), H(8), H(11, 15), AnnexeBrevet,
                    "Annexe I : jeudi 24 juin, français 1re partie 8 h – 9 h 30, 2e partie (rédaction) 9 h 45 – 11 h 15. "
                    + "Remplacement : jeudi 9 septembre.", new(2027, 9, 9)),
                D("DNB_2027_HG_EMC", new(2027, 6, 25), H(8), H(10), AnnexeBrevet,
                    "Annexe I : vendredi 25 juin, histoire et géographie, EMC 8 h – 10 h. Remplacement : vendredi 10 septembre.",
                    new(2027, 9, 10)),
                D("DNB_2027_SCIENCES", new(2027, 6, 28), H(8), H(9), AnnexeBrevet,
                    "Annexe I : lundi 28 juin, sciences (physique-chimie et/ou SVT et/ou technologie) 8 h – 9 h. "
                    + "Remplacement : vendredi 10 septembre.", new(2027, 9, 10)),
                D("DNB_2027_MATHS", new(2027, 6, 28), H(10), H(12), AnnexeBrevet,
                    "Annexe I : lundi 28 juin, mathématiques 10 h – 12 h. Remplacement : jeudi 9 septembre.", new(2027, 9, 9)),

                // ------------------------------------------------------------------ BAC DE FRANÇAIS ET MATHS ANTICIPÉES
                D("BACFR_G_2027_FRANCAIS", J15, H(8), H(12), AnnexeGeneral,
                    "« Les épreuves écrites anticipées de français […] auront lieu le mardi 15 juin 2027 matin. » "
                    + "L'oral : du 21 juin au 9 juillet. Remplacement de l'écrit : jeudi 9 septembre (Éduscol).", new(2027, 9, 9)),
                D("BACFR_T_2027_FRANCAIS", J15, H(8), H(12), AnnexeTechno,
                    "« Les épreuves écrites anticipées de français […] auront lieu le mardi 15 juin 2027 matin. » "
                    + "L'oral : du 21 juin au 9 juillet. Remplacement de l'écrit : jeudi 9 septembre (Éduscol).", new(2027, 9, 9)),
                D("BACFR_G_2027_MATHS_SPE", J21, H(8), H(10), AnnexeGeneral,
                    "« Les épreuves écrites anticipées de mathématiques […] auront lieu le lundi 21 juin 2027 matin. » "
                    + "Annexe : 8 h – 10 h. Remplacement : vendredi 10 septembre (Éduscol).", new(2027, 9, 10)),
                D("BACFR_G_2027_MATHS_ES", J21, H(8), H(10), AnnexeGeneral,
                    "« Les épreuves écrites anticipées de mathématiques […] auront lieu le lundi 21 juin 2027 matin. » "
                    + "Annexe : 8 h – 10 h. Remplacement : vendredi 10 septembre (Éduscol).", new(2027, 9, 10)),
                D("BACFR_T_2027_MATHS", J21, H(8), H(10), AnnexeTechno,
                    "« Les épreuves écrites anticipées de mathématiques […] auront lieu le lundi 21 juin 2027 matin. » "
                    + "Annexe : 8 h – 10 h. Remplacement : vendredi 10 septembre (Éduscol).", new(2027, 9, 10)),

                // ------------------------------------------------------------------ PHILOSOPHIE (toutes voies GT)
                D("BAC_GENERAL_2027_PHILOSOPHIE", J14, H(8), H(12), AnnexeGeneral, philo),
                D("BAC_STMG_2027_PHILOSOPHIE", J14, H(8), H(12), AnnexeTechno, philo),
                D("BAC_ST2S_2027_PHILOSOPHIE", J14, H(8), H(12), AnnexeTechno, philo),
                D("BAC_STL_2027_PHILOSOPHIE", J14, H(8), H(12), AnnexeTechno, philo),
                D("BAC_STI2D_2027_PHILOSOPHIE", J14, H(8), H(12), AnnexeTechno, philo),

                // ------------------------------------------------------------------ SPÉCIALITÉS DU BAC GÉNÉRAL (annexe III)
                D("BAC_GENERAL_2027_HGGSP", J16, H(8), H(12), AnnexeGeneral, spe + " ; 8 h – 12 h.", autreJour: J17),
                D("BAC_GENERAL_2027_HLP", J16, H(8), H(12), AnnexeGeneral, spe + " ; 8 h – 12 h.", autreJour: J17),
                D("BAC_GENERAL_2027_MATHS", J16, H(8), H(12), AnnexeGeneral, spe + " ; 8 h – 12 h.", autreJour: J17),
                D("BAC_GENERAL_2027_SES", J16, H(8), H(12), AnnexeGeneral, spe + " ; 8 h – 12 h.", autreJour: J17),
                D("BAC_GENERAL_2027_PHYSIQUE_CHIMIE", J16, H(8), H(11, 30), AnnexeGeneral, spe + " ; 8 h – 11 h 30.", autreJour: J17),
                D("BAC_GENERAL_2027_SVT", J16, H(8), H(11, 30), AnnexeGeneral, spe + " ; 8 h – 11 h 30.", autreJour: J17),
                D("BAC_GENERAL_2027_NSI", J16, H(8), H(11, 30), AnnexeGeneral, spe + " ; 8 h – 11 h 30.", autreJour: J17),
                D("BAC_GENERAL_2027_SI", J16, H(8), H(11, 30), AnnexeGeneral, spe + " ; 8 h – 11 h 30.", autreJour: J17),
                D("BAC_GENERAL_2027_EPPCS", J16, H(8), H(11, 30), AnnexeGeneral, spe + " ; 8 h – 11 h 30.", autreJour: J17),
                // Les arts : le 16 seulement ; les LLCER : le 17 seulement ; les LLCA : le 18 (annexe III).
                D("BAC_GENERAL_2027_ARTS_PLASTIQUES", J16, H(8), H(11, 30), AnnexeGeneral, "Annexe III : mercredi 16 juin, arts, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_HISTOIRE_ARTS", J16, H(8), H(11, 30), AnnexeGeneral, "Annexe III : mercredi 16 juin, arts, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_CINEMA", J16, H(8), H(11, 30), AnnexeGeneral, "Annexe III : mercredi 16 juin, arts, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_THEATRE", J16, H(8), H(11, 30), AnnexeGeneral, "Annexe III : mercredi 16 juin, arts, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_ARTS_CIRQUE", J16, H(8), H(11, 30), AnnexeGeneral, "Annexe III : mercredi 16 juin, arts, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_LLCER_ANGLAIS", J17, H(8), H(11, 30), AnnexeGeneral, "Annexe III : jeudi 17 juin, langues, littératures et cultures étrangères et régionales, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_AMC", J17, H(8), H(11, 30), AnnexeGeneral, "Annexe III : jeudi 17 juin, langues, littératures et cultures étrangères et régionales, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_LLCER_ESPAGNOL", J17, H(8), H(11, 30), AnnexeGeneral, "Annexe III : jeudi 17 juin, langues, littératures et cultures étrangères et régionales, 8 h – 11 h 30."),
                D("BAC_GENERAL_2027_LLCA_LATIN", J18, H(8), H(12), AnnexeGeneral, "Annexe III : vendredi 18 juin, littérature et langues et cultures de l'Antiquité, 8 h – 12 h."),
                D("BAC_GENERAL_2027_LLCA_GREC", J18, H(8), H(12), AnnexeGeneral, "Annexe III : vendredi 18 juin, littérature et langues et cultures de l'Antiquité, 8 h – 12 h."),

                // ------------------------------------------------------------------ SÉRIES TECHNOLOGIQUES (annexe IV)
                D("BAC_STMG_2027_MSGN", J16, H(8), H(12), AnnexeTechno, "Annexe IV : mercredi 16 juin, STMG, management, sciences de gestion et numérique, 8 h – 12 h."),
                D("BAC_STMG_2027_DROIT_ECO", J17, H(8), H(12), AnnexeTechno, "Annexe IV : jeudi 17 juin, STMG, droit et économie, 8 h – 12 h."),
                D("BAC_ST2S_2027_CBPH", J16, H(8), H(12), AnnexeTechno, "Annexe IV : mercredi 16 juin, ST2S, chimie, biologie et physiopathologie humaines, 8 h – 12 h."),
                D("BAC_ST2S_2027_STSS", J17, H(8), H(11), AnnexeTechno, "Annexe IV : jeudi 17 juin, ST2S, sciences et techniques sanitaires et sociales, 8 h – 11 h."),
                D("BAC_STL_2027_PCM", J16, H(8), H(11), AnnexeTechno, "Annexe IV : mercredi 16 juin, STL, physique-chimie et mathématiques, 8 h – 11 h."),
                D("BAC_STL_2027_BBB", J17, H(8), H(11), AnnexeTechno, "Annexe IV : jeudi 17 juin, STL, biochimie-biologie-biotechnologie ou SPCL, 8 h – 11 h."),
                D("BAC_STL_2027_SPCL", J17, H(8), H(11), AnnexeTechno, "Annexe IV : jeudi 17 juin, STL, biochimie-biologie-biotechnologie ou SPCL, 8 h – 11 h."),
                D("BAC_STI2D_2027_PCM", J16, H(8), H(11), AnnexeTechno, "Annexe IV : mercredi 16 juin, STI2D, physique-chimie et mathématiques, 8 h – 11 h."),
                D("BAC_STI2D_2027_2I2D", J17, H(8), H(11, 30), AnnexeTechno, "Annexe IV : jeudi 17 juin, STI2D, ingénierie, innovation et développement durable (2I2D), 8 h – 11 h 30."),

                // ------------------------------------------------------------------ BAC PROFESSIONNEL (annexe V)
                D("BAC_PRO_2027_FRANCAIS", new(2027, 6, 8), H(8), H(11), AnnexePro, "Annexe V : mardi 8 juin, français, 8 h – 11 h."),
                D("BAC_PRO_2027_HG_EMC", new(2027, 6, 9), H(8), H(10, 30), AnnexePro, "Annexe V : mercredi 9 juin, histoire-géographie et EMC, 8 h – 10 h 30."),
                D("BAC_PRO_2027_PSE", J14, H(8), H(10), AnnexePro, "Annexe V : lundi 14 juin, prévention, santé et environnement, 8 h – 10 h."),
                D("BAC_PRO_2027_ECO_DROIT", J15, H(8), H(10), AnnexePro, "Annexe V : mardi 15 juin, économie-droit et économie-gestion, 8 h – 10 h."),
                D("BAC_PRO_2027_ECO_GESTION", J15, H(8), H(10), AnnexePro, "Annexe V : mardi 15 juin, économie-droit et économie-gestion, 8 h – 10 h."),
            ];
        }

        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            var depot = new CalendrierExamensRepository(context);
            foreach (var date in Session2027())
            {
                await depot.SeedSiAbsenteAsync(date, ct);
            }
        }
    }
}
