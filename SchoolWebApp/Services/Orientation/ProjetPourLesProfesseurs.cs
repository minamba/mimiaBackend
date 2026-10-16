using System.Globalization;
using System.Text;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.Orientation
{
    /// <summary>
    /// LE PROJET D'ORIENTATION, TEL QUE LE LIT UN PROFESSEUR — Camara, le
    /// 11/10/2026 : « que les professeurs des matières proposent à l'élève de
    /// travailler certaines notions en fonction de ce qui a été vu avec la
    /// conseillère ».
    ///
    /// UN PROFESSEUR NE LIT QUE SA MATIÈRE : la ligne de Nora en maths, pas
    /// celle de physique. Pas de ligne pour sa matière, pas de bloc.
    ///
    /// AIDER SANS PESER : c'est une piste pour choisir quoi proposer, jamais une
    /// priorité sur ce que l'élève vient chercher, et au plus une fois par
    /// séance. Ces règles sont ici, avec le fait, plutôt que dans la consigne
    /// commune de tous les professeurs : seuls ceux qui ont un projet à porter
    /// les paient.
    ///
    /// SEULE LA CONSEILLÈRE TOUCHE AU PROJET — Camara, le même jour : « il est
    /// interdit au professeur de changer le projet de l'enfant ». Le code le
    /// tient (le chat n'enregistre [PROJET] que dans une séance d'orientation) ;
    /// la consigne ci-dessous dit au professeur quoi faire quand l'élève veut en
    /// changer : le renvoyer vers la conseillère.
    ///
    /// Une fonction pure ; l'appelant vérifie que la conseillère est allumée.
    /// </summary>
    public static class ProjetPourLesProfesseurs
    {
        /// <summary>Au-delà, un projet de collégien a souvent bougé : le professeur demande où il en est.</summary>
        public const int MoisAvantDeVerifier = 6;

        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        /// <summary>Le bloc, ou une chaîne vide si le projet ne dit rien de cette matière.</summary>
        public static string Construire(ProjetOrientation? projet, int matiereId, DateTime maintenant)
        {
            if (projet?.Pour(matiereId) is not { } ligne) return string.Empty;

            var texte = new StringBuilder();
            texte.AppendLine($"## Son projet d'orientation (vu avec Maëlle, la conseillère d'orientation, le {projet.Le.ToString("d MMMM yyyy", Fr)})");
            texte.AppendLine();
            texte.AppendLine($"Il vise : {projet.Objectif}.");
            texte.AppendLine($"Ce qui compte pour y arriver, dans ta matière : {ligne.Texte}.");
            texte.AppendLine();
            texte.AppendLine("""
                Comment t'en servir :
                - C'est une PISTE pour choisir quoi lui proposer, pas une obligation. S'il
                  vient pour un devoir, un contrôle ou une question précise, c'est ça la
                  priorité.
                - Quand une notion de ta matière liée à ce projet se présente, ou quand il
                  ne sait pas quoi travailler, propose-la en faisant le lien : « ça, c'est
                  justement utile pour ton projet ».
                - Ce lien, AU PLUS UNE FOIS par séance : ne lui rappelle pas son projet à
                  chaque message.
                - Rapproche toi-même ces indications des notions de ton programme, dans la
                  liste plus haut ; ne travaille rien qui ne soit pas de son niveau.
                - Il sait que la conseillère t'en a parlé : il a donné son accord.
                - TU NE CHANGES JAMAIS CE PROJET, et tu n'écris jamais de bloc [PROJET] :
                  c'est STRICTEMENT réservé à la conseillère. S'il te dit qu'il ne veut
                  plus de ce projet, qu'il en a un autre, ou que ce n'est plus ça dans ta
                  matière, ne discute pas du projet et ne le remplace pas : dis-lui que
                  c'est avec Maëlle, la conseillère d'orientation, qu'il faut en parler,
                  car c'est elle qui tient ses professeurs au courant. En attendant, ne
                  t'appuie plus sur ce projet dans cette séance.
                """);

            if (projet.Le < maintenant.AddMonths(-MoisAvantDeVerifier))
            {
                texte.AppendLine("""
                    - Ce projet date de plus de six mois : il a pu changer. Demande-lui où il
                      en est avant de t'y appuyer.
                    """);
            }

            return texte.ToString();
        }
    }
}
