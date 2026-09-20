namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un texte écrit par l'élève dans la langue étudiée, et sa correction.
    ///
    /// LE TROISIÈME DE LA FAMILLE — voulu par Camara le 18/09/2026, et il faut
    /// les distinguer parce que leurs noms se ressemblent :
    ///
    ///   - <see cref="ComprehensionOrale"/> : le professeur LIT, l'élève ÉCOUTE
    ///     et dit EN FRANÇAIS ce qu'il a compris. On garde l'audio.
    ///   - <see cref="ExpressionOrale"/> : les deux PARLENT dans la langue. On
    ///     garde la conversation.
    ///   - ici : l'élève ÉCRIT dans la langue. On garde son texte ET sa
    ///     correction — c'est le seul exercice où son orthographe se voit.
    ///
    /// CE QU'ON ARCHIVE VRAIMENT, C'EST LA PAIRE. Le texte seul ne vaut rien à
    /// relire, la correction seule encore moins : ce qui apprend, c'est de voir
    /// ce qu'on a écrit à côté de ce qu'il fallait écrire. C'est la leçon des
    /// copies d'évaluation, que Camara a fait rendre à l'élève pour cette
    /// raison exacte.
    ///
    /// L'ENJEU A CHANGÉ EN 2027 : la maîtrise de la langue entre dans les
    /// barèmes de toutes les épreuves du brevet et du bac. Ces textes ne sont
    /// donc plus une activité de cours de langue, ce sont des traces d'une
    /// compétence qui rapporte des points partout.
    /// </summary>
    public partial class ExpressionEcrite
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        /// <summary>Séance pendant laquelle le texte a été écrit.</summary>
        public int ConversationId { get; set; }

        /// <summary>De quoi il a parlé — « Raconter son week-end ». Obligatoire.</summary>
        public string Titre { get; set; } = null!;

        /// <summary>Code de la langue : fr, en, es, de, it, zh.</summary>
        public string Langue { get; set; } = null!;

        /// <summary>
        /// Ce que le professeur a demandé, mot pour mot.
        ///
        /// SANS ELLE, LE TEXTE NE SE RELIT PAS. « Five sentences about my
        /// weekend » sans la consigne, c'est un texte qui sort de nulle part :
        /// on ne sait plus si l'élève a répondu à côté, ni pourquoi il s'est
        /// arrêté là.
        /// </summary>
        public string Consigne { get; set; } = null!;

        /// <summary>
        /// Le texte de l'élève, TEL QU'IL L'A ÉCRIT — fautes comprises.
        ///
        /// JAMAIS LA VERSION CORRIGÉE. C'est tout l'intérêt de l'archive :
        /// dans six mois, il doit pouvoir voir ce qu'il écrivait. Une copie
        /// nettoyée ne montrerait aucun chemin parcouru.
        /// </summary>
        /// <summary>
        /// NULL VEUT DIRE « PAS ENCORE RECOPIÉ », et non « il n'a rien écrit ».
        ///
        /// Une ligne peut exister avec sa PHOTO et sans sa transcription : le
        /// professeur est parti, ou la séance s'est terminée, avant qu'il ait
        /// recopié la copie du cahier. Elle attend alors qu'il la reprenne.
        /// </summary>
        public string? Texte { get; set; }

        /// <summary>
        /// La même copie, avec les mots que le professeur a surlignés au tableau
        /// (<c>==mot==</c>), pour que l'archive montre les badges comme en
        /// séance — voulu par Camara le 19/09/2026. NULL quand on ne l'a pas.
        /// </summary>
        public string? TexteSurligne { get; set; }

        /// <summary>
        /// LA PHOTO DU CAHIER, tant qu'elle n'a pas été transcrite.
        ///
        /// Voulu par Camara le 18/09/2026 : « comme ça on perdra rien et le prof
        /// pourra quand même refaire la transcription si elle a pas été faite. »
        ///
        /// UNE COPIE DES OCTETS, PAS UN RENVOI VERS LA PIÈCE JOINTE. La pièce
        /// appartient à la conversation : ses octets sont effacés au bout de
        /// quelques jours, et la conversation finit par être purgée. Un renvoi
        /// aurait donc pointé vers du vide exactement quand on en aurait eu
        /// besoin.
        ///
        /// ELLE NE SURVIT PAS À SA TRANSCRIPTION. Voir
        /// <see cref="PhotoEffaceeLe"/> : garder l'écriture manuscrite d'un
        /// enfant au-delà de ce qui sert n'a aucune justification.
        /// </summary>
        public byte[]? Photo { get; set; }

        /// <summary>image/jpeg, image/png… Le type de la photo, pour la servir.</summary>
        public string? PhotoTypeMime { get; set; }

        /// <summary>
        /// Quand les octets de la photo ont été effacés. Null tant qu'elle est
        /// encore consultable — même mécanique que
        /// <see cref="PieceJointe.DonneesEffaceesLe"/>.
        ///
        /// LA PURGE N'EMPORTE QUE CE QUI A ÉTÉ LU : tant que <see cref="Texte"/>
        /// est null, la photo reste. C'est la règle déjà écrite pour les pièces
        /// jointes, et elle tient en une phrase — effacer les octets d'un
        /// document dont le texte n'a pas été extrait le perdrait sans bruit.
        /// </summary>
        public DateTime? PhotoEffaceeLe { get; set; }

        /// <summary>
        /// Ce que le professeur a relevé, en JSON :
        /// <c>[{"genre":"grammaire","texte":"« I go » devient « I went »…"}]</c>
        ///
        /// `genre` vaut `reussi`, `orthographe`, `grammaire`, `vocabulaire` ou
        /// `construction` — LA GRILLE DE L'EXAMEN, à un mot près, pour que
        /// l'élève voie ce qui revient chez lui dans les termes où il sera
        /// noté.
        ///
        /// `reussi` EN FAIT PARTIE, et ce n'est pas de la décoration : la
        /// consigne impose de commencer par ce qui est réussi, et une archive
        /// qui ne garderait que les fautes transformerait chaque relecture en
        /// rappel d'échec.
        ///
        /// Une colonne JSON et non une table : elle s'écrit d'un bloc, se lit
        /// d'un bloc, et on n'interroge jamais son intérieur. Même raisonnement
        /// que pour l'échange d'une conversation.
        /// </summary>
        public string Corrections { get; set; } = null!;

        /// <summary>Ce que le professeur retient, pour lui et pour ses parents.</summary>
        public string? Remarque { get; set; }

        /// <summary>La classe au moment de l'exercice, figée comme les autres archives.</summary>
        public int? NiveauScolaireId { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Quand l'élève l'a ouvert. Null tant qu'il ne l'a pas fait.</summary>
        public DateTime? DateConsultation { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }
    }
}
