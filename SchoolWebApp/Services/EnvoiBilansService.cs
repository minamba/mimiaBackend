using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services
{
    public interface IEnvoiBilansService
    {
        /// <summary>
        /// Rédige et envoie les bilans de la semaine indiquée.
        /// </summary>
        /// <param name="finSemaine">Lundi suivant la semaine à couvrir. Null = la semaine écoulée.</param>
        /// <param name="eleveId">Restreint à un élève, pour tester sans arroser toute la base.</param>
        /// <param name="groupeDuJour">
        /// Le groupe à traiter aujourd'hui, quand la série est étalée sur
        /// plusieurs jours. Null = tout le monde d'un coup.
        /// </param>
        /// <param name="nombreDeGroupes">Sur combien de jours la série est répartie.</param>
        Task<ResultatEnvoi> EnvoyerAsync(
            DateTime? finSemaine = null,
            int? eleveId = null,
            int? groupeDuJour = null,
            int? nombreDeGroupes = null,
            CancellationToken ct = default);

        /// <summary>
        /// Combien de bilans partiraient. Sert à dimensionner la série avant de
        /// l'entamer, sans rien envoyer.
        /// </summary>
        Task<int> CompterAsync(CancellationToken ct = default);

        /// <summary>Rend le bilan d'un élève en HTML sans l'envoyer. Null si l'élève n'existe pas.</summary>
        Task<string?> ApercuAsync(int eleveId, DateTime? finSemaine = null, CancellationToken ct = default);
    }

    public record ResultatEnvoi(int Traites, int Envoyes, int Echecs, DateTime Debut, DateTime Fin);

    public class EnvoiBilansService : IEnvoiBilansService
    {
        private static readonly CultureInfo Fr = new("fr-FR");

        private readonly IBilanRepository _bilans;
        private readonly IRedacteurBilanService _redacteur;
        private readonly IServiceEmail _email;
        private readonly OptionsEmail _optionsEmail;
        private readonly ILogger<EnvoiBilansService> _logger;

        public EnvoiBilansService(
            IBilanRepository bilans,
            IRedacteurBilanService redacteur,
            IServiceEmail email,
            IOptions<OptionsEmail> optionsEmail,
            ILogger<EnvoiBilansService> logger)
        {
            _bilans = bilans ?? throw new ArgumentNullException(nameof(bilans));
            _redacteur = redacteur ?? throw new ArgumentNullException(nameof(redacteur));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _optionsEmail = optionsEmail?.Value ?? throw new ArgumentNullException(nameof(optionsEmail));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<int> CompterAsync(CancellationToken ct = default) =>
            _bilans.CompterEligiblesAsync(ct);

        public async Task<ResultatEnvoi> EnvoyerAsync(
            DateTime? finSemaine = null,
            int? eleveId = null,
            int? groupeDuJour = null,
            int? nombreDeGroupes = null,
            CancellationToken ct = default)
        {
            var fin = (finSemaine ?? LundiPrecedent(DateTime.UtcNow)).Date;
            var debut = fin.AddDays(-7);

            var bilans = (await _bilans.GetBilansAsync(
                debut, fin, eleveId, groupeDuJour, nombreDeGroupes, ct)).ToList();

            if (nombreDeGroupes is > 1)
            {
                _logger.LogInformation(
                    "Bilans : groupe {Groupe}/{Total} de la semaine du {Debut:yyyy-MM-dd}, "
                    + "{Nombre} bilan(s) a traiter.",
                    groupeDuJour + 1, nombreDeGroupes, debut, bilans.Count);
            }

            // UNE SEULE CONNEXION SMTP POUR TOUTE LA SÉRIE.
            //
            // Avant, `EnvoyerAsync` ouvrait, authentifiait et fermait une
            // connexion PAR PARENT. À dix parents personne ne le voyait ; à
            // trois cents, c'est trois cents authentifications successives
            // depuis la même adresse — le profil exact qu'un service d'envoi
            // interprète comme une attaque, et qui a déjà valu à cette
            // application un blocage de vingt minutes chez l'hébergeur
            // précédent. Un lundi matin de forte croissance, les derniers
            // parents de la liste n'auraient rien reçu.
            //
            // La session apporte aussi la RECONNEXION, et elle est
            // indispensable ici précisément : entre deux bilans il y a un appel
            // au modèle de rédaction qui prend plusieurs secondes. Sur deux
            // cents parents la connexion reste inactive assez longtemps pour
            // que le serveur la ferme — et sans reprise, tout le reste de la
            // liste était perdu.
            await using var session = _email.OuvrirSession();

            foreach (var bilan in bilans)
            {
                ct.ThrowIfCancellationRequested();

                // En série et non en parallèle : chaque bilan actif déclenche un
                // appel au modèle, et vingt appels simultanés se feraient limiter.
                await _redacteur.RedigerAsync(bilan, ct);

                var (sujet, gabarit, valeurs) = Composer(bilan);
                await session.EnvoyerAsync(bilan.ParentMail!, sujet, gabarit, valeurs, ct);
            }

            var envoyes = session.Envoyes;
            var echecs = session.Echecs;

            _logger.LogInformation(
                "Bilans du {Debut:yyyy-MM-dd} au {Fin:yyyy-MM-dd} : {Envoyes} envoyes, {Echecs} echecs sur {Total}.",
                debut, fin, envoyes, echecs, bilans.Count);

            return new ResultatEnvoi(bilans.Count, envoyes, echecs, debut, fin);
        }

        /// <summary>
        /// Prépare le bilan d'un élève et le rend en HTML, sans rien envoyer.
        /// </summary>
        public async Task<string?> ApercuAsync(
            int eleveId, DateTime? finSemaine = null, CancellationToken ct = default)
        {
            var fin = (finSemaine ?? LundiPrecedent(DateTime.UtcNow)).Date;
            var debut = fin.AddDays(-7);

            var bilan = (await _bilans.GetBilansAsync(debut, fin, eleveId, ct: ct)).FirstOrDefault();
            if (bilan is null) return null;

            await _redacteur.RedigerAsync(bilan, ct);

            var (sujet, gabarit, valeurs) = Composer(bilan);
            return await _email.RendreAsync(sujet, gabarit, valeurs);
        }

        /// <summary>
        /// Objet, gabarit et valeurs du mail. Extrait de l'envoi pour que
        /// l'aperçu et le message réel ne puissent pas diverger.
        /// </summary>
        private (string Sujet, string Gabarit, Dictionary<string, string> Valeurs) Composer(BilanEleve bilan)
        {
            var periode = $"Semaine du {Jour(bilan.Debut)} au {Jour(bilan.Fin.AddDays(-1))}";
            var site = (_optionsEmail.UrlSite ?? "https://mimia.fr").TrimEnd('/');

            if (!bilan.Actif)
            {
                return (
                    $"{bilan.Prenom} n'a pas travaillé cette semaine",
                    "bilan-inactif",
                    new Dictionary<string, string>
                    {
                        ["apercu"] = $"Aucune séance pour {bilan.Prenom} cette semaine.",
                        ["prenom"] = Echapper(bilan.Prenom),
                        ["classe"] = Echapper(bilan.NiveauLibelle),
                        ["periode"] = periode,
                        ["derniereFois"] = DerniereFois(bilan.DerniereActivite),
                        ["rappel"] = Rappel(bilan),
                        ["lien"] = $"{site}/eleves",
                    });
            }

            return (
                $"Le bilan de {bilan.Prenom} — {Jour(bilan.Debut)}",
                "bilan",
                new Dictionary<string, string>
                {
                    ["apercu"] = Apercu(bilan),
                    ["prenom"] = Echapper(bilan.Prenom),
                    ["classe"] = Echapper(bilan.NiveauLibelle),
                    ["periode"] = periode,
                    ["nbSeances"] = bilan.NombreSeances.ToString(),
                    ["motSeances"] = bilan.NombreSeances > 1 ? "séances" : "séance",
                    ["nbEchanges"] = bilan.NombreEchanges.ToString(),
                    ["nbMatieres"] = bilan.Matieres.Count.ToString(),
                    ["motMatieres"] = bilan.Matieres.Count > 1 ? "matières" : "matière",
                    ["blocsMatieres"] = BlocsMatieres(bilan),
                    ["blocEvaluations"] = BlocEvaluations(bilan),
                    ["blocDifficultes"] = BlocDifficultes(bilan),
                    ["remarque"] = Paragraphes(bilan.Remarque) is { Length: > 0 } r
                        ? r
                        : "Bonne semaine de travail. On continue sur cette lancée.",
                    ["profPrenom"] = Echapper(bilan.SignatureProf ?? "L'équipe"),
                    ["profMatiere"] = Echapper(bilan.SignatureMatiere ?? "Mimia"),
                    ["lien"] = $"{site}/eleves/{bilan.EleveId}/matieres",
                });
        }

        /// <summary>Une carte par matière travaillée, assemblée en HTML de courrier.</summary>
        private static string BlocsMatieres(BilanEleve bilan)
        {
            var html = new StringBuilder();

            foreach (var matiere in bilan.Matieres)
            {
                var couleur = string.IsNullOrWhiteSpace(matiere.ProfCouleur) ? "#d9482a" : matiere.ProfCouleur;

                html.Append($"""
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
                           style="margin:0 0 12px 0; border:1px solid #ece6dc; border-radius:14px;">
                      <tr>
                        <td style="padding:18px 20px;">
                          <div style="font-size:16px; font-weight:600; color:#16233a;">
                            <span style="display:inline-block; width:8px; height:8px; border-radius:50%; background-color:{Echapper(couleur)}; margin-right:8px;"></span>
                            {Echapper(matiere.Libelle)}
                          </div>
                          <div style="margin-top:3px; font-size:13px; color:#8b93a3;">
                            avec {Echapper(matiere.ProfPrenom)} · {matiere.NombreSeances} séance{(matiere.NombreSeances > 1 ? "s" : "")}
                          </div>
                    """);

                if (!string.IsNullOrWhiteSpace(matiere.Travaille))
                {
                    html.Append($"""
                          <div style="margin-top:12px; font-size:15px; line-height:1.65; color:#16233a;">
                            {Paragraphes(matiere.Travaille)}
                          </div>
                        """);
                }

                html.Append("</td></tr></table>");
            }

            return html.ToString();
        }

        /// <summary>
        /// Les évaluations de la semaine, une carte par note.
        ///
        /// La couleur suit la note mais ne descend JAMAIS jusqu'au rouge : un
        /// parent qui ouvre le bilan et voit du rouge gronde son enfant avant
        /// d'avoir lu la remarque, et c'est exactement ce que le professeur
        /// s'interdit de faire à l'oral. Sous la moyenne, la note est grise.
        /// </summary>
        private static string BlocEvaluations(BilanEleve bilan)
        {
            if (bilan.Evaluations.Count == 0) return string.Empty;

            var cartes = new StringBuilder();

            foreach (var evaluation in bilan.Evaluations)
            {
                var couleur = CouleurNote(evaluation.Note);
                var note = evaluation.Note.ToString("0.#", Fr);

                cartes.Append($"""
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
                           style="margin:0 0 12px 0; border:1px solid #ece6dc; border-radius:14px;">
                      <tr>
                        <td width="76" valign="top" style="padding:18px 0 18px 20px;">
                          <div style="width:56px; height:56px; border-radius:14px; background-color:{couleur}; text-align:center;">
                            <div style="padding-top:11px; font-family:Georgia,'Times New Roman',serif; font-size:22px; font-weight:600; color:#ffffff; line-height:1;">{note}</div>
                            <div style="font-size:10px; color:#ffffff; opacity:0.85; line-height:1;">sur 20</div>
                          </div>
                        </td>
                        <td valign="top" style="padding:18px 20px 18px 14px;">
                          <div style="font-size:15px; font-weight:600; color:#16233a;">
                            {Echapper(evaluation.Notion ?? evaluation.MatiereLibelle)}
                          </div>
                          <div style="margin-top:2px; font-size:13px; color:#8b93a3;">
                            {Echapper(evaluation.MatiereLibelle)} · avec {Echapper(evaluation.ProfPrenom)}
                            · le {Jour(evaluation.DateCreation)}
                          </div>
                    """);

                if (!string.IsNullOrWhiteSpace(evaluation.Remarque))
                {
                    cartes.Append($"""
                          <div style="margin-top:10px; font-size:14.5px; line-height:1.6; color:#16233a;">
                            {Paragraphes(evaluation.Remarque)}
                          </div>
                        """);
                }

                if (!string.IsNullOrWhiteSpace(evaluation.ARevoir))
                {
                    cartes.Append($"""
                          <div style="margin-top:10px; font-size:13.5px; line-height:1.6; color:#5f6c81;">
                            <strong style="color:#16233a;">À reprendre ensemble :</strong> {Echapper(evaluation.ARevoir)}
                          </div>
                        """);
                }

                cartes.Append("</td></tr></table>");
            }

            var titre = bilan.Evaluations.Count > 1 ? "Ses évaluations" : "Son évaluation";

            return $"""
                <h2 style="margin:32px 0 14px 0; font-family:-apple-system,'Segoe UI',Helvetica,Arial,sans-serif; font-size:13px; font-weight:600; letter-spacing:0.08em; text-transform:uppercase; color:#8b93a3;">
                  {titre}
                </h2>
                {cartes}
                """;
        }

        /// <summary>
        /// Pas de rouge, jamais. Sous la moyenne, la note passe au gris : elle
        /// se lit, elle ne crie pas.
        /// </summary>
        private static string CouleurNote(double note) => note switch
        {
            >= 16 => "#0e7c7b",
            >= 12 => "#4d7c22",
            >= 10 => "#b26a08",
            _ => "#5f6c81",
        };

        /// <summary>
        /// Le bloc n'apparaît que si une difficulté a réellement été relevée :
        /// un encadré « Difficultés » vide inquiète pour rien.
        /// </summary>
        private static string BlocDifficultes(BilanEleve bilan)
        {
            var lignes = bilan.Matieres
                .Where(m => !string.IsNullOrWhiteSpace(m.Difficultes))
                .Select(m => $"<strong style=\"color:#16233a;\">{Echapper(m.Libelle)}</strong> — {Paragraphes(m.Difficultes)}")
                .ToList();

            if (lignes.Count == 0) return string.Empty;

            return $"""
                <h2 style="margin:32px 0 14px 0; font-family:-apple-system,'Segoe UI',Helvetica,Arial,sans-serif; font-size:13px; font-weight:600; letter-spacing:0.08em; text-transform:uppercase; color:#8b93a3;">
                  Ce qui coince encore
                </h2>
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
                       style="background-color:#faf7f2; border-radius:14px;">
                  <tr>
                    <td style="padding:18px 22px; font-size:15px; line-height:1.7; color:#5f6c81;">
                      {string.Join("<br /><br />", lignes)}
                    </td>
                  </tr>
                </table>
                """;
        }

        private static string Apercu(BilanEleve bilan) =>
            $"{bilan.NombreSeances} séance{(bilan.NombreSeances > 1 ? "s" : "")} cette semaine, "
            + $"{string.Join(" et ", bilan.Matieres.Select(m => m.Libelle))}.";

        private static string Rappel(BilanEleve bilan) =>
            bilan.DerniereActivite is null
                ? $"{bilan.Prenom} n'a encore jamais ouvert de cours. Le premier est le plus difficile à lancer — ensuite, l'habitude fait le reste."
                : $"Son professeur se souvient de tout ce qu'{(bilan.Sexe == Sexe.Fille ? "elle" : "il")} a vu : reprendre ne veut pas dire recommencer.";

        private static string DerniereFois(DateTime? date) =>
            date is null ? "l'inscription" : $"le {Jour(date.Value)}";

        /// <summary>
        /// Un jour, à l'heure de Paris. Une séance de 00 h 30 est stockée la
        /// veille en UTC : formatée telle quelle, elle daterait le bilan d'un
        /// jour trop tôt.
        /// </summary>
        private static string Jour(DateTime utc) =>
            HeureFrance.Locale(utc).ToString("d MMMM", Fr);

        /// <summary>
        /// Le texte vient d'un modèle : il est échappé avant d'entrer dans le
        /// HTML du mail. Les sauts de ligne deviennent des paragraphes.
        /// </summary>
        private static string Paragraphes(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return string.Empty;

            var lignes = texte
                .Replace("\r\n", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(WebUtility.HtmlEncode);

            return string.Join("<br /><br />", lignes);
        }

        private static string Echapper(string? texte) => WebUtility.HtmlEncode(texte ?? string.Empty);

        /// <summary>
        /// Le lundi qui vient de s'écouler, à minuit. Sert de borne haute : le
        /// bilan couvre toujours une semaine close, jamais celle en cours.
        /// </summary>
        private static DateTime LundiPrecedent(DateTime reference)
        {
            var jours = ((int)reference.DayOfWeek + 6) % 7; // dimanche = 6
            return reference.Date.AddDays(-jours);
        }
    }
}
