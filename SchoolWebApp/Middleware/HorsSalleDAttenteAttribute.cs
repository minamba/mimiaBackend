namespace SchoolWebApp.Api.Middleware
{
    /// <summary>
    /// Cette route ne passe jamais par la salle d'attente.
    ///
    /// UN ATTRIBUT PLUTÔT QU'UNE LISTE DE CHEMINS. Une liste vit dans le
    /// garde, loin des contrôleurs qu'elle concerne ; on y ajoute une route le
    /// jour où on la crée, on oublie de l'y retirer le jour où on la renomme,
    /// et l'exemption survit à ce qu'elle exemptait. Posé ici, le fait se lit
    /// à l'endroit où il compte — sur la route elle-même.
    ///
    /// À NE POSER QUE POUR UNE RAISON ÉNONÇABLE, et l'énoncer. Chaque
    /// exemption est une porte ouverte dans le plafond d'admission : il en
    /// faut, il n'en faut pas beaucoup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class HorsSalleDAttenteAttribute : Attribute
    {
    }
}
