namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une fiche de révision, telle qu'elle s'affiche à l'élève.</summary>
    public class FicheRevisionEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Notion { get; set; }

        /// <summary>Sert à regrouper les fiches. « Autres notions » quand il est vide.</summary>
        public string? Domaine { get; set; }

        public string? Contenu { get; set; }

        /// <summary>« en_cours » ou « acquise ».</summary>
        public string? Etat { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime DateMiseAJour { get; set; }

        public DateTime? DateConsultation { get; set; }

        /// <summary>La notion a-t-elle été vérifiée par une évaluation réussie ?</summary>
        public bool Acquise => Etat == "acquise";

        /// <summary>L'élève ne l'a jamais ouverte.</summary>
        public bool JamaisLue => DateConsultation is null;

        /// <summary>
        /// Le professeur l'a réécrite depuis la dernière lecture. Distinct de
        /// <see cref="JamaisLue"/> : « à consulter » et « mise à jour » ne
        /// disent pas la même chose à l'élève.
        /// </summary>
        public bool MiseAJourNonLue =>
            DateConsultation is not null && DateConsultation < DateMiseAJour;

        // ------------------------------------------- l'élève, pour l'en-tête
        public string? ElevePrenom { get; set; }

        public string? EleveNom { get; set; }

        public string? EleveNiveau { get; set; }

        /// <summary>
        /// Les premières lignes, pour la carte de la liste.
        ///
        /// Renseigné par le dépôt et non calculé à la lecture de
        /// <see cref="Contenu"/> : les listes ne transportent PLUS le contenu.
        /// Une matière suivie pendant deux ans compte des dizaines de fiches, et
        /// envoyer le texte intégral de chacune pour n'en afficher que deux
        /// lignes faisait grossir la réponse sans rien apporter.
        /// </summary>
        public string? Apercu { get; set; }

        /// <summary>
        /// Extrait l'aperçu d'un début de contenu.
        ///
        /// On saute les lignes de titre et les puces : un aperçu qui commence
        /// par « ## À retenir » ne dit rien du sujet.
        /// </summary>
        public static string? ExtraireApercu(string? debut)
        {
            if (string.IsNullOrWhiteSpace(debut)) return null;

            var ligne = debut
                .Replace("\r\n", "\n")
                .Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0 && !l.StartsWith('#') && !l.StartsWith('-'));

            if (ligne is null) return null;

            return ligne.Length <= 140 ? ligne : ligne[..140].TrimEnd() + "…";
        }
    }
}
