using System.Text.Json;
using System.Text.Json.Nodes;

namespace SchoolWebApp.Api.Services.MesFiches
{
    /// <summary>
    /// LE CONTENU D'UNE FICHE, VÉRIFIÉ AVANT D'ÊTRE GARDÉ.
    ///
    /// Le modèle ne dessine rien : il rend des BRIQUES (paragraphe, tableau,
    /// liste, étapes, encadré, questions, arbre, carte), que le front met en
    /// page (`components/mesFiches/`). Ce qui sort d'ici est exactement ce que
    /// le front sait afficher :
    ///   - une brique d'un type inconnu est écartée ;
    ///   - un champ manquant écarte la brique, pas la fiche ;
    ///   - chaque texte est borné, chaque liste aussi — une fiche est courte ;
    ///   - aucun HTML n'est interprété nulle part : le front affiche du texte.
    ///
    /// Une fiche sans aucune brique valide est refusée (null) : l'appelant
    /// la compte en échec plutôt que d'enregistrer une page blanche.
    /// </summary>
    public static class ContenuFiche
    {
        private const int TexteMax = 900;
        private const int TitreMax = 160;

        /// <summary>
        /// Les accents restent des accents : ce JSON part dans une réponse d'API,
        /// jamais dans une page HTML, et `é` pèse moins que `\u00E9`.
        /// </summary>
        private static readonly JsonSerializerOptions Ecriture = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly HashSet<string> Tons =
            ["definition", "cle", "piege", "astuce", "souvent", "modele", "exemple"];

        public sealed record Fiche(string Titre, string BlocsJson);

        /// <summary>
        /// Lit la réponse du modèle — un objet `{ "titre": …, "blocs": [ … ] }`,
        /// éventuellement entouré de texte ou d'un bloc de code — et rend la
        /// fiche normalisée, ou null.
        /// </summary>
        public static Fiche? Lire(string? reponse, string? type = null)
        {
            var racine = ObjetJson(reponse);
            if (racine is null) return null;

            var titre = Texte(racine["titre"], TitreMax);
            if (titre is null) return null;

            var blocs = new List<JsonObject>();
            if (racine["blocs"] is JsonArray bruts)
            {
                foreach (var brut in bruts)
                {
                    if (brut is JsonObject bloc && Normaliser(bloc) is { } propre) blocs.Add(propre);
                }
            }

            // UNE FICHE À BRIQUE UNIQUE NE GARDE QUE CETTE BRIQUE — relevé au
            // premier essai réel (14/10/2026) : une fiche questions/réponses
            // arrivait précédée d'encadrés et d'étapes, alors que sa recette
            // n'en veut aucun. La règle tient ici, pas seulement dans la
            // consigne : si la brique attendue est là, le reste part.
            if (BriqueUnique(type) is { } attendue && blocs.Any(b => Texte(b["type"], 20) == attendue))
            {
                blocs = blocs.Where(b => Texte(b["type"], 20) == attendue).ToList();
            }

            return blocs.Count == 0
                ? null
                : new Fiche(titre, new JsonArray(blocs.Select(b => (JsonNode?)b).ToArray()).ToJsonString(Ecriture));
        }

        /// <summary>La seule brique d'une fiche express, visuelle ou questions/réponses.</summary>
        private static string? BriqueUnique(string? type) => type switch
        {
            "express" => "arbre",
            "visuelle" => "carte",
            "questions" => "questions",
            _ => null,
        };

        /// <summary>Le premier objet JSON de la réponse, ou null.</summary>
        public static JsonObject? ObjetJson(string? reponse)
        {
            if (string.IsNullOrWhiteSpace(reponse)) return null;

            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return null;

            try
            {
                return JsonNode.Parse(reponse[debut..(fin + 1)]) as JsonObject;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static JsonObject? Normaliser(JsonObject bloc) => Texte(bloc["type"], 20) switch
        {
            "paragraphe" => AvecTexte("paragraphe", bloc),
            "intertitre" => AvecTexte("intertitre", bloc, TitreMax),
            "tableau" => Tableau(bloc),
            "liste" => Liste(bloc),
            "etapes" => Etapes(bloc),
            "encadre" => Encadre(bloc),
            "questions" => Questions(bloc),
            "arbre" => Arbre(bloc),
            "carte" => Carte(bloc),
            _ => null,
        };

        private static JsonObject? AvecTexte(string type, JsonObject bloc, int max = TexteMax) =>
            Texte(bloc["texte"], max) is { } texte ? new JsonObject { ["type"] = type, ["texte"] = texte } : null;

        private static JsonObject? Tableau(JsonObject bloc)
        {
            var colonnes = Textes(bloc["colonnes"], 4, 60);
            if (colonnes.Count < 2) return null;

            var lignes = new JsonArray();
            foreach (var brute in Liste(bloc["lignes"], 14))
            {
                if (brute is not JsonArray cellules || cellules.Count == 0) continue;

                var ligne = new JsonArray();
                for (var i = 0; i < colonnes.Count; i += 1)
                {
                    var cellule = i < cellules.Count ? cellules[i] : null;

                    // Une cellule est un texte, ou { texte, ajoute } quand
                    // l'exemple n'était pas dans le cours.
                    if (cellule is JsonObject objet && Texte(objet["texte"]) is { } texteObjet)
                    {
                        ligne.Add(Vrai(objet["ajoute"])
                            ? new JsonObject { ["texte"] = texteObjet, ["ajoute"] = true }
                            : JsonValue.Create(texteObjet));
                    }
                    else
                    {
                        ligne.Add(JsonValue.Create(Texte(cellule) ?? string.Empty));
                    }
                }

                // Une ligne sans son premier terme ne dit pas de quoi elle parle.
                var premiere = ligne[0] is JsonObject objetPremier ? objetPremier["texte"] : ligne[0];
                if (Texte(premiere) is not null) lignes.Add(ligne);
            }

            if (lignes.Count == 0) return null;

            var propre = new JsonObject { ["type"] = "tableau", ["colonnes"] = Tableau(colonnes), ["lignes"] = lignes };
            Titre(bloc, propre);
            return propre;
        }

        private static JsonObject? Liste(JsonObject bloc)
        {
            var elements = Textes(bloc["elements"], 12);
            if (elements.Count == 0) return null;

            var propre = new JsonObject
            {
                ["type"] = "liste",
                ["style"] = Texte(bloc["style"], 10) == "coches" ? "coches" : "puces",
                ["elements"] = Tableau(elements),
            };
            Titre(bloc, propre);
            return propre;
        }

        private static JsonObject? Etapes(JsonObject bloc)
        {
            var etapes = new JsonArray();
            foreach (var brute in Liste(bloc["etapes"], 10))
            {
                if (brute is not JsonObject etape || Texte(etape["titre"], TitreMax) is not { } titre) continue;

                var propre = new JsonObject { ["titre"] = titre };
                if (Texte(etape["texte"]) is { } texte) propre["texte"] = texte;
                etapes.Add(propre);
            }

            if (etapes.Count == 0) return null;

            var resultat = new JsonObject { ["type"] = "etapes", ["etapes"] = etapes };
            Titre(bloc, resultat);
            return resultat;
        }

        private static JsonObject? Encadre(JsonObject bloc)
        {
            var texte = Texte(bloc["texte"]);
            var elements = Textes(bloc["elements"], 8);
            if (texte is null && elements.Count == 0) return null;

            var ton = Texte(bloc["ton"], 20);
            var propre = new JsonObject { ["type"] = "encadre", ["ton"] = ton is not null && Tons.Contains(ton) ? ton : "cle" };
            Titre(bloc, propre);
            if (texte is not null) propre["texte"] = texte;
            if (elements.Count > 0) propre["elements"] = Tableau(elements);
            return propre;
        }

        private static JsonObject? Questions(JsonObject bloc)
        {
            var questions = new JsonArray();
            foreach (var brute in Liste(bloc["questions"], 15))
            {
                if (brute is not JsonObject q
                    || Texte(q["question"], 300) is not { } question
                    || Texte(q["reponse"]) is not { } reponse)
                {
                    continue;
                }

                var propre = new JsonObject { ["question"] = question, ["reponse"] = reponse };
                if (Texte(q["exemple"], 300) is { } exemple)
                {
                    propre["exemple"] = exemple;
                    if (Vrai(q["ajoute"])) propre["ajoute"] = true;
                }
                questions.Add(propre);
            }

            if (questions.Count == 0) return null;

            var resultat = new JsonObject { ["type"] = "questions", ["questions"] = questions };
            Titre(bloc, resultat);
            return resultat;
        }

        private static JsonObject? Arbre(JsonObject bloc)
        {
            if (Texte(bloc["centre"], 60) is not { } centre) return null;

            var branches = new JsonArray();
            foreach (var brute in Liste(bloc["branches"], 6))
            {
                if (brute is not JsonObject branche || Texte(branche["titre"], 90) is not { } titre) continue;

                var feuilles = Textes(branche["feuilles"], 6, 80);
                if (feuilles.Count == 0) continue;

                branches.Add(new JsonObject { ["titre"] = titre, ["feuilles"] = Tableau(feuilles) });
            }

            return branches.Count == 0
                ? null
                : new JsonObject { ["type"] = "arbre", ["centre"] = centre, ["branches"] = branches };
        }

        private static JsonObject? Carte(JsonObject bloc)
        {
            if (Texte(bloc["centre"], 60) is not { } centre) return null;

            var branches = new JsonArray();
            foreach (var brute in Liste(bloc["branches"], 8))
            {
                if (brute is not JsonObject branche || Texte(branche["titre"], 60) is not { } titre) continue;

                var propre = new JsonObject { ["titre"] = titre };
                if (Texte(branche["texte"], 300) is { } texte) propre["texte"] = texte;
                if (Texte(branche["exemple"], 200) is { } exemple)
                {
                    propre["exemple"] = exemple;
                    if (Vrai(branche["ajoute"])) propre["ajoute"] = true;
                }
                branches.Add(propre);
            }

            if (branches.Count == 0) return null;

            var resultat = new JsonObject { ["type"] = "carte", ["centre"] = centre, ["branches"] = branches };
            if (Texte(bloc["sousTitre"], 120) is { } sousTitre) resultat["sousTitre"] = sousTitre;
            return resultat;
        }

        // ----------------------------------------------------------------
        // Lectures prudentes
        // ----------------------------------------------------------------

        private static void Titre(JsonObject source, JsonObject cible)
        {
            if (Texte(source["titre"], TitreMax) is { } titre) cible["titre"] = titre;
        }

        /// <summary>Un texte non vide, sans caractères de contrôle, borné ; ou null.</summary>
        public static string? Texte(JsonNode? noeud, int max = TexteMax)
        {
            if (noeud is not JsonValue valeur || !valeur.TryGetValue<string>(out var brut)) return null;

            var propre = new string(brut.Where(c => c == '\n' || !char.IsControl(c)).ToArray()).Trim();
            if (propre.Length == 0) return null;

            return propre.Length <= max ? propre : propre[..max].TrimEnd() + "…";
        }

        private static List<string> Textes(JsonNode? noeud, int nombreMax, int max = TexteMax) =>
            Liste(noeud, nombreMax).Select(n => Texte(n, max)).OfType<string>().ToList();

        private static IEnumerable<JsonNode?> Liste(JsonNode? noeud, int nombreMax) =>
            noeud is JsonArray tableau ? tableau.Take(nombreMax) : [];

        private static bool Vrai(JsonNode? noeud) =>
            noeud is JsonValue valeur && valeur.TryGetValue<bool>(out var oui) && oui;

        private static JsonArray Tableau(IEnumerable<string> textes) =>
            new(textes.Select(t => (JsonNode?)JsonValue.Create(t)).ToArray());
    }
}
