namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// La sonnette de <see cref="DescriptionPlanchesWorker"/>. Sonnée par
    /// l'import d'une planche, et par rien d'autre.
    ///
    /// POURQUOI ELLE EXISTE
    /// --------------------
    /// Le worker relisait les planches toutes les deux minutes, et le bloc de
    /// consigne était caché cinq. Une planche importée mettait donc jusqu'à
    /// sept minutes à devenir utilisable — et dans les six matières autres que
    /// la SVT, elle n'existait pas du tout pour le professeur pendant ce
    /// temps-là : les clés ne lui viennent que des planches DÉJÀ décrites.
    ///
    /// Sept minutes d'attente pour un import fait à la main, c'est une éternité
    /// quand on en enchaîne vingt en vérifiant chacun.
    ///
    /// Le mécanisme est dans <see cref="Sonnette"/> ; ce type n'existe que pour
    /// que l'injection de dépendances distingue les deux sonnettes.
    /// </summary>
    public sealed class ReveilPlanches : Sonnette
    {
    }
}
