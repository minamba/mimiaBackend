using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Le verdict du professeur sur la préparation d'un contrôle.</summary>
    public record ControlePretDeclare(int Controle, string Verdict, string? Observation);

    /// <summary>
    /// Extrait le bloc [CONTROLE_PRET] : à la fin d'une séance de préparation,
    /// le professeur dit si l'élève est prêt, et pourquoi.
    ///
    /// POURQUOI CE VERDICT N'EST PAS CALCULÉ DEPUIS LA BARRE
    /// ----------------------------------------------------
    /// Voulu par Camara le 13/09/2026 : « c'est pas parce qu'on est pas à 100 %
    /// partout que l'élève n'est pas prêt, c'est au prof de déterminer ». Le
    /// pourcentage mesure ce qui est acquis ; il ignore ce que le contrôle
    /// demandera. Un enfant à 76 % sur les deux notions qui tomberont est prêt ;
    /// un enfant à 90 % à qui il manque précisément celle-là ne l'est pas.
    ///
    /// L'OBSERVATION N'EST PAS FACULTATIVE POUR RIEN
    /// --------------------------------------------
    /// Elle est ce qui rend la bulle utile : un « prêt » vert sans raison
    /// n'apprend rien, et un « pas encore prêt » rouge sans raison décourage
    /// sans dire quoi faire. Le bloc est accepté sans elle — mieux vaut un
    /// verdict nu qu'aucun verdict — mais la consigne la réclame.
    ///
    /// LE NUMÉRO NE S'INVENTE PAS, comme pour [CONTROLE_NOTIONS] : il vient du
    /// contexte de séance. Sans numéro lisible, le bloc est ignoré.
    /// </summary>
    public static partial class LecteurControlePret
    {
        [GeneratedRegex(@"\[CONTROLE_PRET\](?<corps>.*?)\[/CONTROLE_PRET\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        [GeneratedRegex(@"\d+")]
        private static partial Regex Numero();

        /// <summary>
        /// Null s'il n'y a pas de bloc, pas de numéro lisible, ou pas de
        /// verdict reconnu.
        /// </summary>
        public static ControlePretDeclare? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = LecteurBloc.Champs(bloc.Groups["corps"].Value);

            var brutControle = LecteurBloc.Valeur(champs, "controle", "controle_id", "numero");
            if (brutControle is null) return null;

            var numero = Numero().Match(brutControle);
            if (!numero.Success || !int.TryParse(numero.Value, out var controle)) return null;

            // UN VERDICT INCONNU EST UN BLOC REJETÉ, PAS UN REPLI SILENCIEUX.
            //
            // Replier « presque » ou « ça va » sur « pas prêt » inventerait un
            // jugement que le professeur n'a pas porté, et l'enfant verrait une
            // bulle rouge sortie de nulle part. On préfère ne rien afficher.
            var verdict = Traduire(LecteurBloc.Valeur(champs, "pret", "verdict", "statut"));
            if (verdict is null) return null;

            // L'OBSERVATION GARDE SES PARAGRAPHES : lue par `ChampLong`, et non
            // par `Champs`, qui les recollait en un seul bloc (voir LecteurBloc).
            var observation = LecteurBloc.ChampLong(
                bloc.Groups["corps"].Value,
                ["observation", "remarque"],
                ["controle", "controle_id", "numero", "pret", "verdict", "statut", "observation", "remarque"]);

            return new ControlePretDeclare(controle, verdict, observation);
        }

        /// <summary>
        /// Les mots que le professeur peut écrire, ramenés aux trois verdicts.
        ///
        /// Tolérant sur la forme parce que le bloc est produit par un modèle de
        /// langue et non par un formulaire : « oui », « prêt », « PRET » disent
        /// la même chose. Fermé sur le fond : ce qui n'est pas dans cette liste
        /// n'est pas un verdict.
        /// </summary>
        internal static string? Traduire(string? brut)
        {
            var propre = LecteurBloc.Normaliser(brut ?? "");

            return propre switch
            {
                "pret" or "oui" or "prete" => PretControle.VerdictPret,
                "bientot" or "presque" or "bientot pret" or "presque pret"
                    => PretControle.VerdictBientot,
                "pas pret" or "pas encore pret" or "non" or "pas prete"
                    => PretControle.VerdictPasPret,
                _ => PretControle.VerdictValide(brut),
            };
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) =>
            LecteurBloc.Retirer(message, Bloc());
    }
}
