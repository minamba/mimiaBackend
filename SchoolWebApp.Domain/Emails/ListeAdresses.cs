using System.Net.Mail;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// LES ADRESSES EN COPIE (Cc) ET EN COPIE CACHÉE (Cci) — Camara, le
    /// 19/10/2026 : « comme sur Gmail ». L'écran envoie une liste ; chaque
    /// élément peut aussi en contenir plusieurs, séparées par une virgule, un
    /// point-virgule ou un espace (une adresse collée d'un tableur).
    ///
    /// Rend la liste nettoyée — sans doublon, sans celles à écarter — ou la
    /// première adresse illisible, que l'écran montre telle quelle.
    /// </summary>
    public static class ListeAdresses
    {
        /// <summary>Au-delà, ce n'est plus un courriel personnel mais une diffusion.</summary>
        public const int Maximum = 20;

        private static readonly char[] Separateurs = [',', ';', ' ', '\n', '\r', '\t'];

        public static (IReadOnlyList<string> Adresses, string? Erreur) Lire(
            IEnumerable<string?>? brutes, IEnumerable<string?>? aEcarter = null)
        {
            var ecartees = new HashSet<string>(
                (aEcarter ?? []).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a!.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var vues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var adresses = new List<string>();

            foreach (var morceau in (brutes ?? [])
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .SelectMany(b => b!.Split(Separateurs, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            {
                string adresse;
                try
                {
                    adresse = new MailAddress(morceau).Address;
                }
                catch (FormatException)
                {
                    return ([], $"L'adresse « {morceau} » n'est pas valide.");
                }

                if (ecartees.Contains(adresse) || !vues.Add(adresse)) continue;
                adresses.Add(adresse);
            }

            return adresses.Count > Maximum
                ? ([], $"Pas plus de {Maximum} adresses en copie.")
                : (adresses, null);
        }

        /// <summary>Pour un brouillon : la liste, en une ligne.</summary>
        public static string? EnLigne(IEnumerable<string?>? brutes)
        {
            var texte = string.Join(", ", (brutes ?? []).Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b!.Trim()));
            return texte.Length == 0 ? null : texte[..Math.Min(texte.Length, 2000)];
        }
    }
}
