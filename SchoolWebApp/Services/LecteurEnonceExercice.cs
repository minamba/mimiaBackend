using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'ÉNONCÉ SEUL : l'élève n'a pas de copie, il veut juste comprendre.
    ///
    /// Pendant français de `enonceExercice.js`. La balise [ENONCE_EXERCICE]
    /// affiche à l'élève les trois boutons d'envoi ; ici, elle sert à une
    /// autre chose, et c'est la plus importante : FAIRE TAIRE LA RÉCLAMATION
    /// DE COPIE.
    ///
    /// POURQUOI CE FICHIER EXISTE — Camara, le 20/09/2026, capture à l'appui.
    /// L'élève a dit « Non, en fait, je voudrais juste t'envoyer l'énoncé. Y a
    /// pas de copie. » Le professeur a répondu « D'accord, pas de souci —
    /// envoie-moi juste l'énoncé », et a quand même écrit [COPIE_CONTROLE] :
    /// l'écran a reposé « L'énoncé et ta copie sont-ils séparés ? ». Deux fois
    /// de suite, API redémarrée, la nouvelle consigne bien chargée.
    ///
    /// LA RAISON N'EST PAS QU'IL A MAL LU. En séance de bilan, un FAIT part
    /// avec chaque tour et dit : « sa copie et l'énoncé passent par
    /// [COPIE_CONTROLE], jamais par [DEMANDE_DOCUMENT] », plus l'état de ce
    /// qui manque encore. Une consigne lue une fois dans le préfixe en cache
    /// ne pèse rien contre un fait répété à chaque tour — c'est la leçon
    /// écrite dans `regle-de-prompt-ne-vaut-pas-un-fait`, et elle vient de se
    /// vérifier une fois de plus.
    ///
    /// LA CORRECTION EST DONC AU NIVEAU DU FAIT : dès que le professeur a
    /// écrit [ENONCE_EXERCICE], la réclamation de copie ne part plus, et un
    /// fait contraire prend sa place. Le professeur n'a plus rien à retenir.
    /// </summary>
    public static partial class LecteurEnonceExercice
    {
        [GeneratedRegex(@"\[ENONCE_EXERCICE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Balise();

        [GeneratedRegex(@"\[COPIE_CONTROLE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Copie();

        /// <summary>
        /// L'élève a-t-il déclaré n'avoir que l'énoncé ?
        ///
        /// Relu à l'envers et on s'arrête à la PREMIÈRE réponse : une demande
        /// de copie postérieure rouvre la réclamation — le professeur peut
        /// très bien passer à un autre contrôle dans la même séance — et une
        /// fin de séance referme tout.
        /// </summary>
        public static bool DemandeOuverte(IEnumerable<string?> messagesProfesseur)
        {
            foreach (var message in messagesProfesseur.Reverse())
            {
                if (string.IsNullOrEmpty(message)) continue;

                if (message.Contains("[FIN_SEANCE]", StringComparison.Ordinal)) return false;

                // LES DEUX DANS LE MÊME MESSAGE : l'énoncé seul l'emporte. Le
                // professeur qui écrit les deux a compris la demande et gardé
                // son ancien réflexe ; c'est sa phrase la plus récente qui dit
                // ce qu'il a compris, pas celle qu'il répète par habitude.
                if (Balise().IsMatch(message)) return true;

                if (Copie().IsMatch(message)) return false;
            }

            return false;
        }

        /// <summary>
        /// L'élève vient de cliquer « Ta copie et l'énoncé » : il veut les deux.
        ///
        /// Le marqueur est écrit par l'écran (`marquerAvecCopie`), mot pour mot.
        /// Il referme la fenêtre AU TOUR MÊME : sans cela, le professeur aurait
        /// reçu dans le même message « il n'a pas de copie » et « il veut
        /// envoyer les deux ».
        /// </summary>
        public static bool VeutAussiSaCopie(string? messageEleve) =>
            messageEleve?.Contains("[IL VEUT ENVOYER LES DEUX", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// LE FAIT QUI REMPLACE LA RÉCLAMATION DE COPIE, joint à chaque tour
        /// tant que la demande est ouverte.
        ///
        /// Il dit les trois choses que le professeur oublierait sinon : il n'y
        /// a pas de copie, l'écran a déjà posé les boutons, et la première
        /// question à poser n'est pas « où est ta copie » mais « sur quel
        /// exercice bloques-tu ».
        /// </summary>
        public static string Marqueur(int? controleId) =>
            "[ÉNONCÉ SEUL, DÉCLARÉ PAR L'ÉLÈVE"
            + (controleId is int id ? $" (contrôle n° {id})" : string.Empty)
            + " : il N'A PAS DE COPIE à te montrer et il te l'a dit. "
            + "NE LA RÉCLAME PLUS, sous aucune forme — ni maintenant, ni dans trois tours. "
            + "N'écris PAS [COPIE_CONTROLE] : l'écran lui a déjà affiché les boutons d'envoi "
            + "de l'énoncé, et ce bloc ferait réapparaître la question de la copie. "
            + "Quand l'énoncé arrive, ta première phrase lui demande SUR QUEL EXERCICE il a "
            + "bloqué, et tu n'expliques rien avant sa réponse.]";
    }
}
