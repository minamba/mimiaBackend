namespace SchoolWebApp.IdentityServer.Helpers
{
    /// <summary>
    /// L'alerte « nouveau parent », envoyée depuis le serveur d'identité.
    ///
    /// POURQUOI ICI ET NON DANS L'API
    /// ------------------------------
    /// C'est ici que naît un compte, et nulle part ailleurs. L'API ne voit le
    /// parent qu'à sa première visite — après confirmation de son adresse,
    /// parfois des jours plus tard, et jamais s'il ne revient pas. Alerter de
    /// là-bas raterait donc précisément les inscriptions qui n'aboutissent
    /// pas : celles qu'on veut le plus voir.
    ///
    /// STATIQUE, ET C'EST ASSUMÉ. Le serveur d'identité n'a pas de couche de
    /// services ; lui en fabriquer une pour trois lignes ajouterait plus de
    /// structure que de clarté. La contrepartie est un `HttpClient` créé à
    /// l'appel — acceptable pour un envoi par inscription, ce qui n'est pas un
    /// débit.
    /// </summary>
    internal static class TelegramHelper
    {
        internal static async Task NotifierNouveauParentAsync(
            IConfiguration config, ILogger logger, string? prenom, string? nom, string? email)
        {
            var jeton = config["TelegramNouveauParent:BotToken"];
            var salon = config["TelegramNouveauParent:ChatId"];

            // Non configuré : on se tait. C'est l'état normal en développement.
            if (string.IsNullOrWhiteSpace(jeton) || string.IsNullOrWhiteSpace(salon)) return;

            var maintenant = DateTime.UtcNow;

            var texte =
                "👋 *Nouveau parent inscrit*\n"
                + $"— Prénom : {Echapper(prenom)}\n"
                + $"— Nom : {Echapper(nom)}\n"
                + $"— Email : {Echapper(email)}\n"
                + $"— Le : {Echapper($"{maintenant:dd/MM/yyyy} à {maintenant:HH:mm} UTC")}";

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                var reponse = await client.PostAsync(
                    $"https://api.telegram.org/bot{jeton}/sendMessage",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["chat_id"] = salon,
                        ["text"] = texte,
                        ["parse_mode"] = "MarkdownV2",
                    }));

                if (!reponse.IsSuccessStatusCode)
                {
                    var corps = await reponse.Content.ReadAsStringAsync();
                    logger.LogError(
                        "Telegram : envoi refuse ({Statut}) — {Corps}",
                        (int)reponse.StatusCode, corps);
                }
            }
            catch (Exception ex)
            {
                // UNE ALERTE NE DOIT JAMAIS FAIRE ÉCHOUER UNE INSCRIPTION.
                // Le compte est créé, le courriel de confirmation est parti :
                // un message perdu est sans conséquence, un formulaire en
                // erreur ferait fuir le parent.
                logger.LogError(ex, "Telegram : alerte de nouvelle inscription non envoyee.");
            }
        }

        /// <summary>
        /// Échappe les caractères réservés de MarkdownV2 — sans quoi un point
        /// dans une adresse suffit à faire refuser le message entier.
        /// </summary>
        internal static string Echapper(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "—";

            return s.Replace("\\", "\\\\")
                    .Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[")
                    .Replace("]", "\\]").Replace("(", "\\(").Replace(")", "\\)")
                    .Replace("~", "\\~").Replace("`", "\\`")
                    .Replace(">", "\\>").Replace("#", "\\#").Replace("+", "\\+")
                    .Replace("-", "\\-").Replace("=", "\\=").Replace("|", "\\|")
                    .Replace("{", "\\{").Replace("}", "\\}").Replace(".", "\\.")
                    .Replace("!", "\\!");
        }
    }
}
