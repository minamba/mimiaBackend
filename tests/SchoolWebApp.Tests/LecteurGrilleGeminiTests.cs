using SchoolWebApp.Api.Services.Tarifs;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// LA PAGE DE TARIFS GEMINI EN ONGLETS (26/11/2026) : alerte Telegram « gemini-3.8-flash-lite-tts introuvable sur
    /// la page ». Le modèle y était toujours ; c'est la mise en page qui avait changé (onglets et listes au lieu de
    /// « ### Standard » et de lignes « | … | »). Extrait copié de la vraie page (aistudio.google.com/docs/pricing.md.txt).
    /// </summary>
    public class LecteurGrilleGeminiTests
    {
        private const string Page = """
            ## Gemini 3.8 Flash TTS  {% id="gemini-3.8-flash-tts" %}

            {% two-column-layout %}
              {% left-column %}
              *[`gemini-3.8-flash-tts`](/docs/models/gemini-3.8-flash-tts)*

              {% /left-column %}
            {% /two-column-layout %}

            {% tabs %}
              {% tab title="Standard" %}
              {% table %}
              * Feature
              * Free Tier
              * Paid Tier, per 1M tokens in USD

              ---

              * Input price
              * Free of charge
              * $0.50 (text) through December 31, 2026.
              $1.00 (text) starting January 1, 2027.

              ---

              * Output price
              * Free of charge
              * $9.00 (audio) through December 31, 2026.
              $18.00 (audio) starting January 1, 2027.
              Equivalent to $0.00225 per 10s audio{% sup /%} through December 31, 2026.
              {% /table %}

              {% /tab %}
            {% /tabs %}

            ## Gemini 3.8 Flash-Lite TTS  {% id="gemini-3.8-flash-lite-tts" %}

            {% two-column-layout %}
              {% left-column %}
              *[`gemini-3.8-flash-lite-tts`](/docs/models/gemini-3.8-flash-lite-tts)*

              {% /left-column %}
            {% /two-column-layout %}

            {% hr /%}

            Our 3.8 Flash-Lite Text-to-Speech audio model.

            {% tabs %}
              {% tab title="Standard" %}
              {% table %}
              * Feature
              * Free Tier
              * Paid Tier, per 1M tokens in USD

              ---

              * Input price
              * Free of charge
              * $0.50 (text) through December 31, 2026.
              $1.00 (text) starting January 1, 2027.

              ---

              * Output price
              * Free of charge
              * $6.00 (audio) through December 31, 2026.
              $12.00 (audio) starting January 1, 2027.
              Equivalent to $0.0015 per 10s audio{% sup /%} through December 31, 2026.
              Equivalent to $0.003 per 10s audio{% sup /%} starting January 1, 2027.

              ---

              * Used to improve our products
              * [Yes](/docs/terms)
              * [No](/docs/terms)
              {% /table %}

              {% /tab %}

              {% tab title="Batch" %}
              {% table %}
              * Feature
              * Free Tier
              * Paid Tier, per 1M tokens in USD

              ---

              * Output price
              * Not available
              * $3.00 (audio) through December 31, 2026.
              {% /table %}
              {% /tab %}
            {% /tabs %}
            """;

        [Fact]
        public void Lit_le_prix_standard_du_jour_dans_la_page_en_onglets()
        {
            var prix = LecteurGrilleTarifs.LireGemini(Page, "gemini-3.8-flash-lite-tts", new DateTime(2026, 11, 27));

            Assert.NotNull(prix);
            Assert.Equal(0.50m, prix!.Entree);
            Assert.Equal(6.00m, prix.Sortie);   // l'onglet Standard, pas Batch (3,00)
        }

        [Fact]
        public void Bascule_sur_le_prix_de_2027_le_1er_janvier()
        {
            var prix = LecteurGrilleTarifs.LireGemini(Page, "gemini-3.8-flash-lite-tts", new DateTime(2027, 1, 1));

            Assert.NotNull(prix);
            Assert.Equal(1.00m, prix!.Entree);
            Assert.Equal(12.00m, prix.Sortie);
        }

        [Fact]
        public void Ne_confond_pas_deux_modeles_voisins()
        {
            var prix = LecteurGrilleTarifs.LireGemini(Page, "gemini-3.8-flash-tts", new DateTime(2026, 11, 27));

            Assert.NotNull(prix);
            Assert.Equal(9.00m, prix!.Sortie);
        }
    }
}
