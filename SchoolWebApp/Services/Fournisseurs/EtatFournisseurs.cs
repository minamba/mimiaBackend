using System.Collections.Concurrent;
using System.Text.Json;

namespace SchoolWebApp.Api.Services.Fournisseurs
{
    /// <summary>
    /// Les états possibles d'un fournisseur d'IA, tels que l'onglet
    /// « Anthropic / OpenAI » les affiche.
    /// </summary>
    public static class StatutFournisseur
    {
        /// <summary>Pas encore vérifié depuis le démarrage du serveur.</summary>
        public const string Inconnu = "inconnu";

        /// <summary>Aucune clé dans la configuration : rien à vérifier.</summary>
        public const string NonConfigure = "non_configure";

        public const string Ok = "ok";

        /// <summary>
        /// Plus de crédit prépayé. Le cas du 14/09/2026 : la clé était
        /// acceptée, mais chaque appel payant répondait 429
        /// `insufficient_quota` — la voix et le micro étaient coupés.
        /// </summary>
        public const string CreditEpuise = "credit_epuise";

        /// <summary>Clé révoquée, erronée, ou sans droit sur le modèle.</summary>
        public const string CleRefusee = "cle_refusee";

        /// <summary>Trop de requêtes : le fournisseur freine, il ne refuse pas.</summary>
        public const string Limite = "limite";

        /// <summary>Panne ou surcharge chez le fournisseur (5xx, 529).</summary>
        public const string Indisponible = "indisponible";

        /// <summary>Le serveur n'a pas pu joindre le fournisseur (réseau, délai).</summary>
        public const string Injoignable = "injoignable";

        public const string Erreur = "erreur";

        /// <summary>
        /// Le quota du JOUR est atteint — Camara, le 28/09/2026 : « Gemini est
        /// signalé en vert comme si tout était ok alors que ça ne marche pas ».
        /// Orange et non rouge : rien n'est coupé, la voix passe par OpenAI
        /// jusqu'à la remise à zéro. Mais c'est un défaut, et Telegram le dit.
        /// </summary>
        public const string QuotaAtteint = "quota_atteint";

        /// <summary>
        /// LES DEUX SEULS ÉTATS QUI MÉRITENT UN COURRIEL. Ils ne se règlent pas
        /// tout seuls : il faut recharger, ou changer la clé. Une surcharge ou
        /// un freinage passent en quelques minutes, et un courriel pour chacun
        /// apprendrait vite à ne plus lire les autres.
        /// </summary>
        public static bool Bloquant(string statut) => statut is CreditEpuise or CleRefusee;

        /// <summary>
        /// Rouge OU orange : tout ce qui n'est ni « fonctionne », ni un état
        /// neutre (pas encore vérifié, pas de clé). C'est la règle du message
        /// Telegram — plus large que celle du courriel, voulu par Camara le
        /// 15/09/2026 : une panne passagère se lit d'un coup d'œil sur le
        /// téléphone, elle ne mérite pas pour autant une boîte de réception.
        /// </summary>
        public static bool EnDefaut(string statut) =>
            statut is CreditEpuise or CleRefusee or Limite or Indisponible or Injoignable or Erreur or QuotaAtteint;

        /// <summary>L'état en toutes lettres, comme l'écran l'écrit.</summary>
        public static string Libelle(string statut) => statut switch
        {
            Ok => "fonctionne",
            CreditEpuise => "crédit épuisé",
            CleRefusee => "clé refusée",
            Limite => "freiné (trop de requêtes)",
            QuotaAtteint => "quota du jour atteint — la voix passe par OpenAI",
            Indisponible => "panne chez le fournisseur",
            Injoignable => "injoignable",
            NonConfigure => "non configuré",
            Inconnu => "pas encore vérifié",
            _ => "réponse inattendue",
        };
    }

    /// <summary>Ce qu'une sonde a constaté, avant d'être daté et rangé.</summary>
    public sealed record ResultatSonde(
        string Statut,
        int? CodeHttp = null,
        string? CodeErreur = null,
        string? Detail = null,
        long? DureeMs = null);

    /// <summary>Un fournisseur, tel que l'onglet l'affiche.</summary>
    public sealed record EtatFournisseur
    {
        /// <summary>`anthropic` ou `openai`.</summary>
        public required string Code { get; init; }

        public required string Nom { get; init; }

        /// <summary>Ce qui s'arrête dans le produit quand ce fournisseur tombe.</summary>
        public required string Usage { get; init; }

        /// <summary>La page où l'on recharge — le seul endroit où se lit le vrai solde.</summary>
        public required string LienFacturation { get; init; }

        /// <summary>Le modèle appelé par la sonde.</summary>
        public string? Modele { get; init; }

        public string Statut { get; init; } = StatutFournisseur.Inconnu;

        public int? CodeHttp { get; init; }

        /// <summary>Le code d'erreur du fournisseur, tel quel (`insufficient_quota`…).</summary>
        public string? CodeErreur { get; init; }

        /// <summary>Le message du fournisseur, en anglais, tronqué.</summary>
        public string? Detail { get; init; }

        public long? DureeMs { get; init; }

        public DateTime? VerifieLe { get; init; }

        /// <summary>Depuis quand ce statut dure — la première vérification qui l'a constaté.</summary>
        public DateTime? StatutDepuis { get; init; }

        public DateTime? DernierSuccesLe { get; init; }

        /// <summary>
        /// Où en est la consommation face aux quotas — Google seulement, compté
        /// par nous (voir <c>CompteurGemini</c>). Null pour les autres.
        /// </summary>
        public IReadOnlyList<SchoolWebApp.Api.Services.Voix.JaugeQuota>? Jauges { get; init; }

        /// <summary>Les refus 429 du jour de Google : le plafond a été touché.</summary>
        public int? RefusDuJour { get; init; }

        /// <summary>
        /// Jusqu'à quand la voix passe par OpenAI parce que Google a dit non — le
        /// disjoncteur de <c>CompteurGemini</c>. Null quand il est fermé.
        /// </summary>
        public DateTime? BloqueJusqua { get; init; }
    }

    /// <summary>
    /// L'état des fournisseurs d'IA, gardé EN MÉMOIRE.
    ///
    /// PAS DE TABLE, ET C'EST VOULU. Un état vieux de la veille ne dit rien
    /// de maintenant : la première vérification a lieu une minute après le
    /// démarrage, et c'est elle qui fait foi. Une table n'aurait conservé que
    /// de quoi se tromper.
    ///
    /// LE REVERS, ASSUMÉ : la mémoire des courriels envoyés part aussi avec un
    /// redémarrage. Si le crédit est encore épuisé à ce moment-là, un second
    /// courriel part — ce qui est plutôt un rappel utile qu'un doublon.
    /// </summary>
    public sealed class EtatFournisseurs
    {
        public const string Anthropic = "anthropic";
        public const string OpenAi = "openai";

        /// <summary>
        /// Google, pour la voix des professeurs depuis le 28/09/2026 — Camara :
        /// « tout ce qu'on a mis en place pour OpenAI, je veux pareil pour Gemini ».
        /// </summary>
        public const string Gemini = "gemini";

        private readonly ConcurrentDictionary<string, EtatFournisseur> _etats = new();
        private readonly ConcurrentDictionary<string, string> _signales = new();

        /// <summary>
        /// Une seule vérification à la fois : le worker et le bouton « Vérifier
        /// maintenant » tomberaient sinon sur la même transition, et
        /// enverraient chacun leur courriel.
        /// </summary>
        public SemaphoreSlim Verrou { get; } = new(1, 1);

        public EtatFournisseurs()
        {
            _etats[Anthropic] = new EtatFournisseur
            {
                Code = Anthropic,
                Nom = "Anthropic",
                Usage = "Claude : les professeurs en séance, les bilans, les observations, les planches et les documents.",
                LienFacturation = "https://console.anthropic.com/settings/billing",
            };

            _etats[OpenAi] = new EtatFournisseur
            {
                Code = OpenAi,
                Nom = "OpenAI",
                Usage = "La voix des professeurs et le micro de l'élève (transcription).",
                LienFacturation = "https://platform.openai.com/settings/organization/billing/overview",
            };

            // SA PANNE NE COUPE RIEN, ET L'ALERTE DOIT LE DIRE : quand Google ne
            // répond pas, chaque phrase repasse d'elle-même par OpenAI (voir
            // SyntheseGemini). Le courriel et Telegram reprennent cet usage mot
            // pour mot — un « une partie de Mimia est coupée » sans cette phrase
            // ferait courir pour rien.
            _etats[Gemini] = new EtatFournisseur
            {
                Code = Gemini,
                Nom = "Google Gemini",
                Usage = "La voix des professeurs quand l'interrupteur « Voix Gemini » est allumé (Modes). En panne, la voix repasse d'elle-même par OpenAI.",
                LienFacturation = "https://console.cloud.google.com/billing",
            };
        }

        /// <summary>Anthropic, OpenAI, Google — toujours le même ordre à l'écran.</summary>
        public IReadOnlyList<EtatFournisseur> Tous() => [_etats[Anthropic], _etats[OpenAi], _etats[Gemini]];

        public EtatFournisseur Get(string code) => _etats[code];

        /// <summary>Range le résultat d'une sonde et rend l'état précédent.</summary>
        public EtatFournisseur Enregistrer(string code, string? modele, ResultatSonde resultat, DateTime maintenant)
        {
            var ancien = _etats[code];
            var memeStatut = ancien.Statut == resultat.Statut;

            _etats[code] = ancien with
            {
                Modele = modele,
                Statut = resultat.Statut,
                CodeHttp = resultat.CodeHttp,
                CodeErreur = resultat.CodeErreur,
                Detail = resultat.Detail,
                DureeMs = resultat.DureeMs,
                VerifieLe = maintenant,
                StatutDepuis = memeStatut ? ancien.StatutDepuis ?? maintenant : maintenant,
                DernierSuccesLe = resultat.Statut == StatutFournisseur.Ok ? maintenant : ancien.DernierSuccesLe,
            };

            return ancien;
        }

        /// <summary>Le dernier statut pour lequel un courriel est parti, ou null.</summary>
        public string? DernierSignale(string code) => _signales.TryGetValue(code, out var s) ? s : null;

        public void MarquerSignale(string code, string statut) => _signales[code] = statut;

        private readonly ConcurrentDictionary<string, string> _signalesTelegram = new();

        /// <summary>
        /// Le dernier statut annoncé sur Telegram, ou null. Tenu à part de
        /// celui des courriels : les deux canaux n'ont pas la même règle.
        /// </summary>
        public string? DernierSignaleTelegram(string code) =>
            _signalesTelegram.TryGetValue(code, out var s) ? s : null;

        public void MarquerSignaleTelegram(string code, string statut) => _signalesTelegram[code] = statut;

        /// <summary>
        /// Lit une réponse d'erreur et dit ce qu'elle signifie.
        ///
        /// LES DEUX FOURNISSEURS N'ÉCRIVENT PAS LE CRÉDIT ÉPUISÉ PAREIL. OpenAI
        /// répond 429 avec `insufficient_quota` — le même code HTTP que le
        /// simple freinage, d'où la lecture du corps : sans elle, un compte à
        /// sec passerait pour « trop de requêtes, patientez ». Anthropic répond
        /// 400 (ou `billing_error`) avec « Your credit balance is too low ».
        /// </summary>
        public static ResultatSonde Classer(int codeHttp, string? corps, long? dureeMs = null)
        {
            var (type, codeErreur, message) = LireErreur(corps);
            var indices = $"{type} {codeErreur} {message}".ToLowerInvariant();

            var statut =
                codeHttp == 402
                || indices.Contains("insufficient_quota")
                || indices.Contains("credit_balance")
                || indices.Contains("credit balance")
                || indices.Contains("billing")
                    ? StatutFournisseur.CreditEpuise
                : codeHttp is 401 or 403 ? StatutFournisseur.CleRefusee
                : codeHttp == 429 ? StatutFournisseur.Limite
                : codeHttp >= 500 ? StatutFournisseur.Indisponible
                : StatutFournisseur.Erreur;

            return new ResultatSonde(
                statut,
                codeHttp,
                codeErreur ?? type,
                Tronquer(message ?? corps, 300),
                dureeMs);
        }

        /// <summary>
        /// Le classement de GOOGLE, qui n'écrit pas ses erreurs comme les deux autres.
        ///
        /// `{"error":{"code":429,"message":…,"status":"RESOURCE_EXHAUSTED"}}` : le
        /// code est un nombre, la nature est dans `status`. Et SURTOUT, son simple
        /// freinage dit « You exceeded your current quota, please check your plan
        /// and BILLING details » — lu par `Classer`, le mot « billing » en ferait
        /// un crédit épuisé : alerte rouge et courriel pour un ralentissement
        /// passager. D'où cette lecture à part :
        ///
        ///   - clé invalide (`API_KEY_INVALID`, « API key not valid ») → clé refusée ;
        ///   - 401 / 403 → clé refusée, sauf facturation coupée → crédit épuisé ;
        ///   - 429 → freinage, sauf crédit prépayé épuisé (« prepay », « credits ») ;
        ///   - 5xx → indisponible ; le reste → erreur.
        /// </summary>
        public static ResultatSonde ClasserGemini(int codeHttp, string? corps, long? dureeMs = null)
        {
            var (type, codeErreur, message) = LireErreur(corps);
            var statutGoogle = LireStatutGoogle(corps);
            var indices = $"{statutGoogle} {type} {codeErreur} {message} {corps}".ToLowerInvariant();

            var cleInvalide = indices.Contains("api_key_invalid") || indices.Contains("api key not valid");
            var facturation = indices.Contains("billing_disabled") || indices.Contains("billing account")
                              || indices.Contains("prepay") || indices.Contains("credits are depleted")
                              || indices.Contains("credit balance");

            var statut =
                cleInvalide ? StatutFournisseur.CleRefusee
                : codeHttp is 401 or 403 ? (facturation ? StatutFournisseur.CreditEpuise : StatutFournisseur.CleRefusee)
                : codeHttp == 429 ? (facturation ? StatutFournisseur.CreditEpuise : StatutFournisseur.Limite)
                : codeHttp >= 500 ? StatutFournisseur.Indisponible
                : StatutFournisseur.Erreur;

            return new ResultatSonde(
                statut,
                codeHttp,
                statutGoogle ?? codeErreur ?? type,
                Tronquer(message ?? corps, 300),
                dureeMs);
        }

        /// <summary>Le `status` d'une erreur Google (RESOURCE_EXHAUSTED…), ou null.</summary>
        private static string? LireStatutGoogle(string? corps)
        {
            if (string.IsNullOrWhiteSpace(corps)) return null;

            try
            {
                using var document = JsonDocument.Parse(corps);
                return document.RootElement.TryGetProperty("error", out var erreur)
                       && erreur.ValueKind == JsonValueKind.Object
                    ? Texte(erreur, "status")
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// `{"error":{"type":…,"code":…,"message":…}}` — la forme commune aux
        /// deux fournisseurs, à un niveau près. Un corps illisible n'empêche
        /// pas le classement : le code HTTP suffit alors.
        /// </summary>
        private static (string? Type, string? Code, string? Message) LireErreur(string? corps)
        {
            if (string.IsNullOrWhiteSpace(corps)) return (null, null, null);

            try
            {
                using var document = JsonDocument.Parse(corps);
                if (!document.RootElement.TryGetProperty("error", out var erreur)
                    || erreur.ValueKind != JsonValueKind.Object)
                {
                    return (null, null, null);
                }

                return (Texte(erreur, "type"), Texte(erreur, "code"), Texte(erreur, "message"));
            }
            catch (JsonException)
            {
                return (null, null, null);
            }
        }

        private static string? Texte(JsonElement element, string propriete) =>
            element.TryGetProperty(propriete, out var valeur) && valeur.ValueKind == JsonValueKind.String
                ? valeur.GetString()
                : null;

        private static string? Tronquer(string? texte, int taille)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;
            var propre = texte.Trim();
            return propre.Length <= taille ? propre : propre[..taille] + "…";
        }
    }
}
