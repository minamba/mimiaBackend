using System.Text.RegularExpressions;

namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// L'AVATAR D'UN ENFANT — Camara, le 16/11/2026 : à la création ou à la modification de l'enfant, le parent (ou
    /// l'administration) peut choisir un portrait d'après sa classe ; ce n'est pas obligatoire.
    ///
    /// LE SERVEUR NE GARDE QU'UN CODE, PAS UNE IMAGE. Le catalogue (155 portraits : tranche d'âge, origine, sexe,
    /// cheveux) vit dans le front, `assets/avatars-enfants`, et se nomme « avatar-{code}.png » ; le code est par exemple
    /// « cp-ce1-asie-garcon-courts ». Le serveur ne connaît donc pas la liste : il vérifie seulement la forme du code
    /// (minuscules, chiffres, tirets, 60 caractères au plus), pour qu'aucun chemin ni texte arbitraire n'entre en base.
    /// Un code qui ne correspond à aucune image (une image retirée) est sans danger : l'écran retombe sur l'avatar par
    /// défaut.
    /// </summary>
    public static class AvatarEnfant
    {
        private static readonly Regex Forme = new("^[a-z0-9-]{1,60}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Le code à enregistrer : null pour « aucun choix » (vide, espaces, ou une forme refusée), sinon le code tel
        /// quel. La requête est déjà validée par l'API ; ceci est la ceinture sous les bretelles.
        /// </summary>
        public static string? Retenu(string? code)
        {
            var propre = code?.Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(propre) || !Forme.IsMatch(propre) ? null : propre;
        }
    }
}
