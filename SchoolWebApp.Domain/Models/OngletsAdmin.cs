namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Les sections du tableau de bord, et qui a le droit d'y entrer —
    /// voulu par Camara le 17/09/2026.
    ///
    /// POURQUOI UNE LISTE DE CLÉS ET NON UN RÔLE PAR SECTION
    /// ----------------------------------------------------
    /// Un rôle par section aurait donné quatorze rôles à créer, à attribuer et
    /// à tenir d'accord avec la barre d'onglets. Ici, le droit EST la clé de
    /// l'onglet : ajouter une section demande une ligne ici et une ligne dans
    /// la barre, et le super-administrateur la voit apparaître décochée dans la
    /// fenêtre des droits sans qu'on touche à rien d'autre.
    ///
    /// RIEN N'EST ACCORDÉ PAR DÉFAUT. Camara : « de base quand le super admin
    /// passe un utilisateur en admin, tout est décoché ». Un nouvel
    /// administrateur voit donc un tableau de bord vide tant qu'on ne lui a
    /// rien ouvert — ce qui est le bon sens d'une délégation : on donne ce
    /// qu'on veut donner, on n'enlève pas ce qu'on aurait oublié d'enlever.
    ///
    /// LE SUPER-ADMINISTRATEUR N'EST PAS CONCERNÉ. Il voit tout, toujours, et
    /// ses droits ne se stockent nulle part : il est le seul à pouvoir se les
    /// rendre, donc les lui retirer n'aurait aucun sens.
    /// </summary>
    public static class OngletsAdmin
    {
        /// <summary>
        /// Les clés, dans l'ordre de la barre d'onglets.
        ///
        /// ELLES SONT CELLES DU FRONT, à la lettre près : `Admin.js` les écrit
        /// dans son tableau `items`, et c'est le même mot qui voyage jusqu'ici.
        /// Une clé qui divergerait rendrait un onglet invisible pour tout le
        /// monde, sans erreur nulle part.
        /// </summary>
        public static readonly string[] Toutes =
        [
            "stats",
            "frequentation",
            "parents",

            // DEUX BLOCS DE L'ONGLET PARENTS, ET NON DES ONGLETS — voulu par
            // Camara le 19/09/2026 : ce que le produit coûte, ce qu'il
            // rapporte. Ils ne se montrent qu'aux administrateurs qui les ont
            // cochés ; masqués, ils restent MONTÉS, parce que la période
            // qu'ils portent pilote aussi le tableau des parents. Rangés ici
            // pour profiter de la même colonne et de la même fenêtre des droits
            // — la barre d'onglets ne les connaît pas, donc ils n'y créent rien.
            "cout",
            "revenu",

            "mails",
            "promos",
            "eleves",
            "avis",
            "schemas",
            "programme",
            "periodes",
            "signalements",
            "idees",
            "fournisseurs",
        ];

        /// <summary>
        /// Lit la liste stockée en base : des clés séparées par des virgules.
        ///
        /// UNE COLONNE ET NON UNE TABLE DE JOINTURE. C'est un petit ensemble
        /// fixe, attaché à un compte et jamais interrogé dans l'autre sens — on
        /// ne demande jamais « qui a le droit sur les idées ? ». Une table
        /// aurait ajouté une entité, une migration et un `Include` à chaque
        /// lecture de parent pour ranger quatorze mots.
        ///
        /// LES CLÉS INCONNUES SONT ÉCARTÉES À LA LECTURE : une section
        /// supprimée du produit laisserait sinon un droit orphelin en base, et
        /// il ressortirait coché dans une fenêtre qui ne sait pas l'afficher.
        /// </summary>
        public static string[] Lire(string? brut)
        {
            if (string.IsNullOrWhiteSpace(brut)) return [];

            return brut
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries
                          | System.StringSplitOptions.TrimEntries)
                .Select(c => c.ToLowerInvariant())
                .Where(c => Toutes.Contains(c, System.StringComparer.Ordinal))
                .Distinct(System.StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Prépare la liste à écrire : nettoyée, dédoublonnée, et REMISE DANS
        /// L'ORDRE DE LA BARRE.
        ///
        /// L'ordre n'a aucune importance pour l'autorisation — mais une colonne
        /// qu'on relit à l'œil en base est plus facile à comparer d'un compte à
        /// l'autre quand elle est toujours écrite pareil.
        /// </summary>
        public static string Ecrire(IEnumerable<string>? onglets)
        {
            if (onglets is null) return string.Empty;

            var demandes = onglets
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToLowerInvariant())
                .ToHashSet(System.StringComparer.Ordinal);

            return string.Join(',', Toutes.Where(demandes.Contains));
        }
    }
}
