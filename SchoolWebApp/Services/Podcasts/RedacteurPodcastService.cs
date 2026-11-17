using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.MesFiches;
using SchoolWebApp.Api.Services.Prompts;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.Podcasts
{
    /// <summary>Le texte d'un épisode : son titre et ses paragraphes, chacun lu d'une traite par la voix.</summary>
    public sealed record TexteEpisode(string Titre, IReadOnlyList<string> Paragraphes);

    /// <summary>
    /// ÉCRIRE UN ÉPISODE DE PODCAST À PARTIR DU COURS DE L'ÉLÈVE — 15/11/2026.
    ///
    /// UN TEXTE FAIT POUR L'OREILLE, PAS POUR L'ŒIL : le professeur de la matière parle à l'élève, le tutoie, explique
    /// UNE notion de SON cours, donne des exemples, puis résume et pose deux ou trois questions de rappel dont il donne
    /// la réponse après un temps. Rien qui ne se lise mal à voix haute : ni symbole, ni formule écrite, ni liste, ni
    /// tableau — « trois quarts », pas « 3/4 ».
    ///
    /// LE MÊME APPEL QUE LES FICHES (`RedacteurFichesService.AppelerAsync`) : les pages du cours d'abord, mises en
    /// cache par la lecture, la consigne ensuite. Un cours de vingt pages ne paie pas ses pages à chaque épisode.
    /// </summary>
    public class RedacteurPodcastService
    {
        /// <summary>Un épisode de six minutes fait ~900 mots, soit ~1 500 jetons ; de la marge pour le JSON.</summary>
        private const int MaxTokens = 4000;

        /// <summary>Un paragraphe se dit d'une traite : au-delà, la voix dérape plus souvent (relectures, inventions).</summary>
        public const int ParagrapheMax = 600;

        private readonly RedacteurFichesService _redacteur;
        private readonly OptionsClaude _options;

        public RedacteurPodcastService(RedacteurFichesService redacteur, IOptions<OptionsClaude> options)
        {
            _redacteur = redacteur ?? throw new ArgumentNullException(nameof(redacteur));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<TexteEpisode?> EcrireAsync(CoursATraiter cours, int rang, CancellationToken ct)
        {
            var notion = rang >= 1 && rang <= cours.Notions.Count ? cours.Notions[rang - 1] : null;
            if (notion is null) return null;

            var libre = cours.Pages.Count > 0 && cours.Pages.All(p => FichesRevisionEleve.EstUnTexte(p.TypeMime));
            var livre = libre && cours.Pages.All(p => PodcastsEleve.EstUnLivre(p.TypeMime));
            var consigne = Consigne(cours, notion, rang, libre, livre);

            // LE MÊME EFFORT QUE LA LECTURE : sinon le cache des pages ne se partage pas (mesuré le 14/10/2026).
            var effort = AgentPedagogiqueService.ConvertirEffort(_options.Effort(TypeTache.Dialogue));
            var reponse = await _redacteur.AppelerAsync(cours, consigne, MaxTokens, effort, "podcasts-episode", ct);

            return Lire(reponse);
        }

        /// <summary>Le JSON du modèle, vérifié : un titre, des paragraphes non vides, chacun coupé s'il est trop long.</summary>
        public static TexteEpisode? Lire(string reponse)
        {
            var racine = ContenuFiche.ObjetJson(reponse);
            if (racine is null || racine["paragraphes"] is not JsonArray brutes) return null;

            var paragraphes = new List<string>();
            foreach (var brut in brutes)
            {
                if (ContenuFiche.Texte(brut, 4000) is not { } texte) continue;
                paragraphes.AddRange(Couper(Nettoyer(texte)));
            }

            paragraphes.RemoveAll(string.IsNullOrWhiteSpace);
            if (paragraphes.Count < 2) return null;

            var titre = ContenuFiche.Texte(racine["titre"], 160) ?? "Épisode";
            return new TexteEpisode(titre, paragraphes);
        }

        /// <summary>Ce que la voix lirait mal : marques de mise en forme et balises oubliées.</summary>
        private static string Nettoyer(string texte) =>
            System.Text.RegularExpressions.Regex.Replace(texte.Replace("**", "").Replace("*", "").Replace("#", ""), @"\[/?[A-Z_]+[^\]]*\]", " ")
                .Replace("  ", " ")
                .Trim();

        /// <summary>Un paragraphe trop long est coupé entre deux phrases.</summary>
        public static IEnumerable<string> Couper(string texte)
        {
            if (texte.Length <= ParagrapheMax)
            {
                yield return texte;
                yield break;
            }

            var phrases = System.Text.RegularExpressions.Regex.Split(texte, @"(?<=[.!?…])\s+");
            var courant = new System.Text.StringBuilder();
            foreach (var phrase in phrases)
            {
                if (courant.Length > 0 && courant.Length + phrase.Length + 1 > ParagrapheMax)
                {
                    yield return courant.ToString().Trim();
                    courant.Clear();
                }

                courant.Append(phrase).Append(' ');
            }

            if (courant.Length > 0) yield return courant.ToString().Trim();
        }

        private static string Consigne(CoursATraiter cours, NotionDuCours notion, int rang, bool libre, bool livre = false)
        {
            var prof = string.IsNullOrWhiteSpace(cours.ProfPrenom) ? "son professeur" : cours.ProfPrenom!;
            var mots = PodcastsEleve.MotsVises(cours.Rang);
            var minutes = PodcastsEleve.MinutesVisees(cours.Rang);
            var premier = rang == 1;
            var dernier = rang == cours.Notions.Count;
            var autres = cours.Notions.Where((n, i) => i != rang - 1).Select(n => n.Titre).ToList();
            // L'ESPACE PARENT (15/11/2026) : un adulte qui révise ses propres notes, vouvoyé comme partout dans son espace.
            var adulte = cours.Rang == FichesRevisionEleve.RangAdulte;
            var auditeur = adulte ? "un adulte qui révise ses propres notes" : $"un élève de {cours.ClasseLibelle}";
            var lui = adulte ? "l'auditeur" : "l'élève";

            return $$"""
                Tu écris le texte d'un ÉPISODE DE PODCAST de révision. Il sera LU À VOIX HAUTE par {{prof}}, le professeur
                de {{cours.MatiereLibelle}} de l'élève, et l'élève l'ÉCOUTERA, sans le lire. Tu écris donc ce que {{prof}}
                dit, à la première personne, à {{auditeur}}, à partir de {{(livre ? "ce LIVRE" : "SON cours")}} :
                « {{cours.Titre ?? (livre ? "le livre qu'il a demandé" : "son cours")}} ».

                # Le sujet de cet épisode

                UNE {{(livre ? "partie du résumé du livre" : "notion du cours")}} : « {{notion.Titre}} » (épisode {{rang}} sur {{cours.Notions.Count}}).
                {{(string.IsNullOrWhiteSpace(notion.Resume) ? "" : $"Ce qu'il raconte : {notion.Resume}")}}
                {{(autres.Count > 0 ? $"N'y mets que ce qui la concerne : les autres notions ({string.Join(" ; ", autres)}) ont leur propre épisode." : "")}}

                # {{(livre ? "Fidèle au livre" : libre ? "Ce que l'élève a écrit ou dicté fait foi" : "Fidèle au cours")}}

                {{(livre
                    ? "L'élève a demandé le résumé de ce livre. Tu racontes avec TES mots ce qui se passe vraiment dans le livre : les événements, les personnages, leurs noms, l'ordre du récit. Tu ne recopies jamais de passage du livre ; une citation très courte et célèbre au plus. Tu n'inventes RIEN : un détail dont tu n'es pas sûr, tu le laisses de côté. Tu racontes la fin quand l'épisode y arrive : c'est un résumé pour réviser."
                    : libre
                    ? "L'élève n'a pas importé de pages : il a écrit ou dicté son sujet ou ses notes. Ce qu'il a écrit passe en premier ; s'il ne donne qu'un sujet, tu expliques ce que le programme de sa classe enseigne sous ce sujet, juste et à son niveau, rien au-delà. S'il a écrit une erreur, tu dis la version juste. Un texte dicté contient des mots mal entendus : comprends le sens."
                    : "Tout ce que tu expliques vient du cours : ses définitions, ses règles, ses mots, ses exemples. N'ajoute aucune notion, règle, date ou formule qui n'y figure pas. Tu peux ajouter un exemple simple et juste là où le cours n'en a pas. Ce qui est illisible, tu le laisses de côté.")}}

                # Un texte pour l'oreille

                - {{(adulte ? "Le VOUVOIEMENT, un ton chaleureux et vivant, comme un bon professeur qui parle à un seul adulte." : "Le tutoiement, un ton chaleureux et vivant, comme un bon professeur qui parle à un seul élève.")}}
                - Des phrases courtes. Une idée par phrase. Tu reformules ce qui est important.
                - AUCUN symbole ni formule écrite : tout s'écrit comme cela se DIT. « trois quarts », « x au carré »,
                  « deux virgule cinq », « mille sept cent quatre-vingt-neuf », « H deux O ». Pas de chiffres en
                  numération, pas d'abréviations, pas de parenthèses, pas de tirets de liste, pas d'emoji, pas de Markdown.
                - Pas de « comme tu le vois », « regarde le schéma », « ci-dessous » : l'élève ne voit rien.
                - Entièrement en français{{(cours.MatiereLibelle.Contains("nglais", StringComparison.OrdinalIgnoreCase)
                    || cours.MatiereLibelle.Contains("spagnol", StringComparison.OrdinalIgnoreCase)
                    || cours.MatiereLibelle.Contains("llemand", StringComparison.OrdinalIgnoreCase)
                    || cours.MatiereLibelle.Contains("talien", StringComparison.OrdinalIgnoreCase)
                    || cours.MatiereLibelle.Contains("hinois", StringComparison.OrdinalIgnoreCase)
                    || cours.MatiereLibelle.Contains("rabe", StringComparison.OrdinalIgnoreCase)
                    ? ", sauf les mots et les courtes phrases de la langue étudiée, que tu cites tels quels"
                    : ", sans aucun mot d'anglais")}}.

                # Le déroulé

                {{(premier
                    ? $"1. Tu salues {lui} et tu te présentes en une phrase (« {(adulte ? "Bonjour, c'est" : "Salut, c'est")} {prof} ! »), puis tu annonces le sujet de l'épisode."
                    : $"1. Tu annonces en une phrase le sujet de cet épisode, sans te présenter à nouveau.")}}
                {{(livre
                    ? "2. Tu racontes cette partie pas à pas, comme une histoire, en nommant les personnages et les moments importants."
                    : "2. Tu expliques la notion pas à pas, avec les exemples du cours.")}}
                3. Tu fais un petit résumé : ce qu'il faut retenir, en deux ou trois phrases.
                4. Tu poses deux ou trois questions de rappel. Après chaque question, tu dis « Prends quelques secondes
                   pour y réfléchir. », puis tu donnes la réponse.
                {{(dernier
                    ? $"5. Tu conclus en une phrase chaleureuse : c'est la fin {(livre ? "du résumé de ce livre" : "de ce cours")}."
                    : $"5. Tu conclus en une phrase et tu annonces que l'épisode suivant parle d'autre chose {(livre ? "du livre" : "de son cours")}.")}}

                Longueur : environ {{mots}} mots en tout, soit environ {{minutes}} minutes à l'oral.

                # Ce que tu rends

                UNIQUEMENT ce JSON, sans texte autour :
                {"titre": "La configuration de Thalès", "paragraphes": ["…", "…"]}

                `titre` : le titre de l'épisode, court (la notion). `paragraphes` : le texte, découpé en paragraphes de deux
                à quatre phrases (six cents caractères au plus chacun) — chacun est lu d'une traite, avec une courte pause
                entre deux.
                """;
        }
    }
}
