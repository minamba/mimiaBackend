using System.Text;
using System.Text.Json;
using SchoolWebApp.Api.Services.Fournisseurs;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LA VOIX DES PROFESSEURS PAR GOOGLE (Gemini TTS), À L'ESSAI — Camara, le
    /// 28/09/2026 : « est-ce qu'il y a plus performant au même prix ? », puis
    /// « je veux entendre par moi-même, tester un échange ».
    ///
    /// Allumée par <c>Voix:Fournisseur = "gemini"</c> ; éteinte par défaut. Seule
    /// la voix EN DIRECT des séances passe par ici (<see
    /// cref="SyntheseVocaleService.CopierAudioAsync"/>) : les voix pré-enregistrées
    /// des jeux et le mode de secours restent chez OpenAI.
    ///
    /// LE MÊME FORMAT QUE CELUI QU'ATTEND LE LECTEUR. Google diffuse du PCM
    /// 16 bits, 24 kHz, mono, en base64 dans un flux SSE : on décode chaque
    /// morceau et on le recopie tel quel. Le navigateur ne voit aucune
    /// différence — ni le front, ni le lecteur n'ont à changer.
    ///
    /// FLASH-LITE, CHOISI À L'OREILLE — Camara, le 28/09/2026, après un échange en
    /// direct : « c'est une dinguerie, ça n'a strictement rien à voir ». Et au banc :
    /// premier son en 0,55 à 0,90 s à chaque appel, quand OpenAI avait des pics à 24
    /// et 32 s. Flash (au-dessus) démarrait plus lentement (1,2 à 1,9 s) pour un coût
    /// supérieur d'environ 50 %.
    /// </summary>
    internal static class SyntheseGemini
    {
        private const string Base = "https://generativelanguage.googleapis.com/v1beta/models/";

        /// <summary>
        /// Une voix par professeur, parmi les trente de Gemini. Sept voix de
        /// femme pour sept professeures — ce que la table d'OpenAI ne pouvait
        /// pas offrir (Jeanne y parle avec un timbre d'homme).
        ///
        /// PROVISOIRE : choisie sur la description que Google donne de chaque
        /// voix, pas encore à l'oreille. Une voix se juge sur trois prises.
        /// </summary>
        private static readonly Dictionary<string, string> Voix = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nora"] = "Zephyr",        // lumineuse — choisie à l'oreille par Camara le 28/09/2026, parmi huit (audition)
            ["marine"] = "Aoede",       // légère
            ["ines"] = "Vindemiatrix",  // douce
            ["camille"] = "Gacrux",     // posée, mûre
            ["lucia"] = "Laomedeia",    // enjouée
            ["elodie"] = "Callirrhoe",  // détendue
            ["jeanne"] = "Despina",     // fluide
            ["adrien"] = "Sadaltager",  // savante
            ["salim"] = "Charon",       // informative
            ["yann"] = "Iapetus",       // claire
            ["karim"] = "Schedar",      // égale
            ["theo"] = "Puck",          // enjouée
            ["minamba"] = "Achird",     // amicale

            // Les quatre professeurs de langue du 04/10/2026 — choisis, comme
            // les autres, sur la description de Google. Jamais entendus.
            ["jonas"] = "Orus",         // allemand — ferme
            ["giulia"] = "Leda",        // italien — jeune
            ["lin"] = "Autonoe",        // chinois — lumineuse
            ["youssef"] = "Algieba",    // arabe — douce

            // La conseillère d'orientation (11/10/2026) — choisie sur la
            // description de Google (« chaleureuse »), jamais entendue.
            ["maelle"] = "Sulafat",

            // LE PROFESSEUR DE PSE (16/10/2026) — Gemini EN PREMIER, voulu par
            // Camara : « la priorité c'est Gemini ». Choisie À L'OREILLE par
            // Camara le 16/10/2026, parmi six voix d'homme encore libres
            // (audition, une même phrase).
            ["hugo"] = "Sadachbia",
        };

        private const string VoixParDefaut = "Kore";

        public static string ChoisirVoix(string? avatar) =>
            avatar is not null && Voix.TryGetValue(avatar, out var voix) ? voix : VoixParDefaut;

        /// <summary>
        /// Diffuse l'audio vers <paramref name="destination"/> au fil de l'arrivée.
        /// Rend faux si rien n'a pu être écrit (l'appelant repasse alors par
        /// OpenAI) ; lève si la coupure survient APRÈS le premier octet — le code
        /// de réponse est déjà parti, le navigateur verra un passage écourté.
        /// </summary>
        /// <summary>
        /// Le temps qu'on laisse toujours à la garde de redite pour relire la
        /// fin, en secondes. Deux d'abord ; au premier contrôle du journal
        /// (04/10/2026), 2 phrases sur 22 étaient parties sans contrôle parce
        /// que la transcription n'avait pas répondu à temps. Camara : « 2
        /// secondes c'est négligeable au vu de l'amélioration de la qualité ».
        /// Ce n'est un blanc que lorsque la transcription traîne.
        /// </summary>
        private const double DelaiMinimumGarde = 4.0;

        public static async Task<UsageSynthese> CopierAsync(
            HttpClient http, string cle, string modele, string texte, string? avatar, int age,
            bool dictee, string? langue, string? vitesse, Stream destination, ILogger logger,
            CancellationToken ct, CompteurGemini? compteur = null, GardeRedite? garde = null)
        {
            var corps = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = Habiller(texte, avatar, age, dictee, langue, vitesse) } } },
                },
                generationConfig = new
                {
                    responseModalities = new[] { "AUDIO" },
                    speechConfig = new
                    {
                        voiceConfig = new { prebuiltVoiceConfig = new { voiceName = ChoisirVoix(avatar) } },
                    },
                },
            };

            using var requete = new HttpRequestMessage(
                HttpMethod.Post, $"{Base}{modele}:streamGenerateContent?alt=sse")
            {
                Content = new StringContent(JsonSerializer.Serialize(corps), Encoding.UTF8, "application/json"),
            };
            requete.Headers.Add("x-goog-api-key", cle);

            HttpResponseMessage reponse;
            try
            {
                reponse = await http.SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Synthese Gemini injoignable : on repasse par OpenAI.");
                return UsageSynthese.Rien;
            }

            using (reponse)
            {
                if (!reponse.IsSuccessStatusCode)
                {
                    // Un crédit épuisé d'abord : Google le dit en 429 comme un
                    // freinage, et le prendre pour un freinage, c'était se taire
                    // (voir CompteurGemini.SignalerCreditEpuise). Puis un 429 est un
                    // plafond touché ; le reste compte comme une requête — Google
                    // la compte aussi.
                    var detail = await reponse.Content.ReadAsStringAsync(ct);
                    var lu = EtatFournisseurs.ClasserGemini((int)reponse.StatusCode, detail);

                    if (lu.Statut == StatutFournisseur.CreditEpuise)
                        compteur?.SignalerCreditEpuise(DateTime.UtcNow, (int)reponse.StatusCode, lu.CodeErreur);
                    else if ((int)reponse.StatusCode == 429) compteur?.Refuser(DateTime.UtcNow, detail);
                    else compteur?.Enregistrer(0, DateTime.UtcNow);

                    // LU COMME : c'est ce qui se cherche au journal. Le message
                    // exact d'un solde prépayé à zéro n'a encore jamais été vu ;
                    // s'il passe pour un freinage (« limite ») alors que le solde
                    // est vide, c'est ici qu'on lira les mots à ajouter à ClasserGemini.
                    logger.LogWarning(
                        "Synthese Gemini en echec ({Code}, lu comme {Statut}) : on repasse par OpenAI. {Detail}",
                        (int)reponse.StatusCode, lu.Statut, detail.Length > 400 ? detail[..400] : detail);
                    return UsageSynthese.Rien;
                }

                await using var flux = await reponse.Content.ReadAsStreamAsync(ct);
                using var lecteur = new StreamReader(flux, Encoding.UTF8);

                var ecrit = false;
                var interrompu = false;
                var jetonsEntree = 0;
                var jetonsSortie = 0;
                var octetsEcrits = 0L;

                var plafond = PlafondOctets(texte, lectureLente: dictee || langue is not null);

                // Depuis combien de temps l'élève a-t-il du son ? C'est ce qui
                // dit combien de temps la garde peut retenir la fin.
                System.Diagnostics.Stopwatch? depuisLePremierSon = null;
                var emballe = false;

                // LA GARDE DE REDITE — voir GardeRedite. Le début part tout de
                // suite ; au-delà de `avance`, le son est RETENU, relu par la
                // transcription une fois le flux fini, et transmis entier ou
                // coupé au début de la redite.
                var avance = garde is null ? long.MaxValue : GardeRedite.OctetsTransmisDAvance(texte);
                using var retenu = new MemoryStream();

                try
                {
                    string? ligne;
                    while (!emballe && (ligne = await lecteur.ReadLineAsync(ct)) is not null)
                    {
                        if (!ligne.StartsWith("data:", StringComparison.Ordinal)) continue;

                        using var evenement = JsonDocument.Parse(ligne.AsMemory(5));
                        jetonsEntree = JetonsEntree(evenement.RootElement) ?? jetonsEntree;
                        jetonsSortie = JetonsSortie(evenement.RootElement) ?? jetonsSortie;

                        foreach (var octets in Morceaux(evenement.RootElement))
                        {
                            // LE MODÈLE S'EMBALLE : on ferme, et le son déjà parti
                            // suffit — voir PlafondOctets. Sortir de la boucle
                            // dispose la réponse, ce qui coupe la génération.
                            if (octetsEcrits + retenu.Length + octets.Length > plafond)
                            {
                                emballe = true;
                                break;
                            }

                            // Ce qui tient encore dans l'avance part ; le reste
                            // attend. Un morceau peut chevaucher la borne : il
                            // se coupe sur un échantillon entier.
                            var place = (int)Math.Clamp(avance - octetsEcrits, 0, octets.Length) & ~1;

                            if (place > 0)
                            {
                                depuisLePremierSon ??= System.Diagnostics.Stopwatch.StartNew();
                                await destination.WriteAsync(octets.AsMemory(0, place), ct);
                                await destination.FlushAsync(ct);
                                ecrit = true;
                                octetsEcrits += place;
                            }

                            if (place < octets.Length) retenu.Write(octets, place, octets.Length - place);
                        }
                    }

                    if (retenu.Length > 0)
                    {
                        // LA GARDE A LE TEMPS QUE L'ÉLÈVE MET À ÉCOUTER LE DÉBUT. Il
                        // a reçu `avance` secondes de son, il en a déjà écouté une
                        // partie : c'est ce qui reste.
                        //
                        // ET JAMAIS MOINS QUE `DelaiMinimumGarde` — corrigé le 04/10/2026.
                        // Ce calcul retirait une seconde et demie de marge, sans
                        // plancher. Sur une phrase ordinaire (75 caractères : 3,4 s
                        // d'avance), il ne restait rien : la garde renonçait « faute
                        // de temps » — et d'autant plus sûrement qu'il y avait une
                        // redite, puisque Gemini met alors deux fois plus longtemps
                        // à produire le son. Elle ne coupait donc presque plus
                        // jamais, et Camara a réentendu ses phrases en double.
                        //
                        // Le prix du plancher : si la transcription traîne, un
                        // blanc d'une seconde au plus avant la fin de la phrase.
                        // Il a tranché : la redite est ce qui le dérange.
                        var reste = octetsEcrits / 48000d - (depuisLePremierSon?.Elapsed.TotalSeconds ?? 0);
                        var marge = TimeSpan.FromSeconds(Math.Max(DelaiMinimumGarde, reste));

                        var fin = garde is null
                            ? new ReadOnlyMemory<byte>(retenu.ToArray())
                            : await garde.FiltrerAsync(
                                texte, retenu.ToArray(), langue, marge, ct, octetsEcrits / 48000d);

                        if (fin.Length > 0)
                        {
                            await destination.WriteAsync(fin, ct);
                            await destination.FlushAsync(ct);
                            ecrit = true;
                            octetsEcrits += fin.Length;
                        }
                    }
                }
                catch (OperationCanceledException) when (ecrit)
                {
                    // L'ÉLÈVE A COUPÉ LA PAROLE, OU LA REQUÊTE DOUBLÉE A PERDU :
                    // le navigateur a fermé le flux. Google, lui, a facturé ce
                    // qu'il avait produit. Ce n'est pas une erreur, c'est une
                    // dépense — on la rend à l'appelant au lieu de la perdre.
                    interrompu = true;
                }

                compteur?.Enregistrer(jetonsEntree, DateTime.UtcNow);
                compteur?.CreditRetrouve();

                if (emballe)
                {
                    logger.LogWarning(
                        "Synthese Gemini EMBALLEE ({Modele}) : {Secondes:F1} s de son pour {Caracteres} caracteres, flux coupe.",
                        modele, octetsEcrits / 48000d, texte.Length);
                }

                if (!ecrit)
                {
                    logger.LogWarning("Synthese Gemini sans audio : on repasse par OpenAI.");
                    return UsageSynthese.Rien;
                }

                // Le compte des jetons audio n'arrive qu'avec les derniers
                // évènements : un flux coupé n'en a pas. On le déduit alors des
                // secondes reçues, au débit mesuré du modèle.
                if (jetonsSortie == 0)
                {
                    jetonsSortie = (int)Math.Round(octetsEcrits / 48000d * JetonsAudioParSeconde);
                }

                return new UsageSynthese(true, jetonsEntree, jetonsSortie, octetsEcrits, interrompu);
            }
        }

        /// <summary>
        /// Jetons audio par seconde de parole, mesurés le 28/09 et le 03/10/2026
        /// sur Flash-Lite (38 à 41 ; Google en annonce 25).
        /// </summary>
        private const double JetonsAudioParSeconde = 39d;

        /// <summary>
        /// LE MODÈLE PEUT S'EMBALLER — mesuré le 04/10/2026 : sur 200 appels,
        /// Flash a produit une fois 85 secondes de son pour un paragraphe qui en
        /// fait 13 ; Flash-Lite, lui, relisait tout son texte une seconde fois
        /// dans 6 % des appels. Comparer le son au son ne marche pas (deux
        /// lectures d'une même phrase se ressemblent autant que deux phrases
        /// différentes) ; comparer le son au TEXTE, si : aucune lecture
        /// légitime ne descend sous QUATRE caractères par seconde — une dictée
        /// lente en fait six à huit, une phrase ordinaire quinze à vingt. Au-delà
        /// de cette borne, on ferme : le son déjà parti contient la phrase.
        ///
        /// Large exprès. Une redite simple (2×) y passe encore ; c'est le choix
        /// du modèle Flash qui la règle. La borne ne vise que l'emballement, et
        /// ne doit JAMAIS couper une vraie phrase.
        /// </summary>
        private const double CaracteresParSecondeMinimum = 4d;

        /// <summary>
        /// Une dictée ou un passage d'écoute se lit bien plus lentement, exprès —
        /// mot à mot, parfois syllabe par syllabe : la borne y est deux fois plus
        /// large, pour ne jamais couper une lecture voulue lente.
        /// </summary>
        private const double CaracteresParSecondeMinimumLent = 2d;

        private static long PlafondOctets(string texte, bool lectureLente)
        {
            var allure = lectureLente ? CaracteresParSecondeMinimumLent : CaracteresParSecondeMinimum;
            var secondes = Math.Max(6d, texte.Length / allure + 2d);
            return (long)(secondes * 48000);
        }

        /// <summary>
        /// Ce qu'un appel a consommé, DANS LES UNITÉS QUE GOOGLE FACTURE — Camara,
        /// le 03/10/2026 : « comment tu vérifies que les bons chiffres sont
        /// bons sur Gemini ? ». L'administration estimait la voix à 0,4 minute
        /// par tour de parole ; la réalité mesurée était 0,2. Plutôt qu'une
        /// meilleure estimation, on relève ce que Google dit lui-même : chaque
        /// réponse porte `promptTokenCount` (le texte et les notes de mise en
        /// scène) et `candidatesTokenCount` (l'audio produit, ~39 par seconde).
        /// Ce sont exactement les nombres de la page « Usage » d'AI Studio : la
        /// somme de l'administration et la facture de Google doivent coïncider.
        /// </summary>
        public readonly record struct UsageSynthese(
            bool Ecrit, int JetonsEntree, int JetonsSortie, long Octets, bool Interrompu = false)
        {
            public static readonly UsageSynthese Rien = new(false, 0, 0, 0);
        }

        /// <summary>
        /// LA CONSIGNE DE JEU, À LA MANIÈRE DE GOOGLE — mesuré le 28/09/2026.
        ///
        /// Les modèles de voix Gemini 3.8 REFUSENT la consigne séparée
        /// (« Developer instruction is not enabled for this model »). Elle doit
        /// voyager dans le texte, et la façon compte :
        ///
        ///   - notre consigne OpenAI entière (2 000 caractères) glissée devant le
        ///     texte : lue à voix haute — 121 à 155 s de son pour 8 s de phrase ;
        ///   - une phrase courte « Dis ceci d'un ton… : » : lue aussi (12 à 14 s
        ///     au lieu de 7) ;
        ///   - la structure que Google documente (profil, notes de mise en scène,
        ///     transcription) : appliquée, jamais lue (6,9 à 7,9 s).
        ///
        /// D'où ces notes, courtes, en anglais comme dans la documentation, et
        /// seulement ce qui change la diction. Le reste de nos règles — sigles,
        /// tirets, symboles — est déjà réglé dans le texte par le front
        /// (`prononcable`), avant même d'arriver ici.
        /// </summary>
        internal static string Habiller(
            string texte, string? avatar, int age, bool dictee, string? langue, string? vitesse)
        {
            var prenom = string.IsNullOrWhiteSpace(avatar)
                ? "The tutor"
                : char.ToUpperInvariant(avatar[0]) + avatar[1..];
            var eleve = age <= 11 ? "a child" : "a teenager";

            var notes = new List<string>
            {
                $"# AUDIO PROFILE: {prenom}, a warm French private tutor talking one-on-one with {eleve}.",
                "### DIRECTOR'S NOTES",
            };

            if (langue is not null && NomsLangues.TryGetValue(langue, out var nomLangue))
            {
                // Un passage d'écoute : la langue étudiée, dite par un natif.
                notes.Add($"Language: the transcript is in {nomLangue}; read it as a native {nomLangue} speaker would.");
                notes.Add(vitesse switch
                {
                    "tres_lent" => "Pace: very slow, for a beginner listener; a clear silence between words. The dots between syllables and the ellipses are pauses, never read aloud.",
                    "lent" => "Pace: slow and clear, for a learner; the ellipses between words are short silences, never read aloud.",
                    "rapide" => "Pace: natural native speed, fluent, not rushed.",
                    _ => "Pace: calm and clear, for a learner, but natural.",
                });
            }
            else if (dictee)
            {
                notes.Add("Style: a French teacher giving a dictation — calm, clear, every word articulated, never syllable by syllable.");
                notes.Add("Pace: slow and even, as in a school dictation.");
                notes.Add("Accent: native French from France.");
            }
            else
            {
                // UNE PROFESSEURE, PAS UNE CONFIDENTE — Camara, le 28/09/2026 : « la
                // voix de Nora est trop sexuelle ». Les premières notes disaient
                // « warm, complicit » et « you smile while talking » : en anglais,
                // « complicit » tire vers la connivence, et Google en faisait sur
                // certaines voix un souffle intime. Le ton d'une enseignante —
                // clair, énergique, encourageant — et ce qu'il ne doit JAMAIS
                // devenir, écrit en toutes lettres : c'est ce que le modèle lit.
                notes.Add(age <= 11
                    ? "Style: a friendly, encouraging schoolteacher talking to a child — clear, bright, cheerful and reassuring. Talking, not reading."
                    : "Style: a friendly, confident schoolteacher talking to a teenager — clear, energetic, encouraging, direct. Talking, not reading.");
                notes.Add("Voice quality: a clear, full speaking voice. Never breathy, whispery, husky, intimate, sultry or seductive — this is a lesson, not a confidence.");
                notes.Add("Pace: lively, the speed of a real conversation; no dragging, no exaggerated articulation, no long pauses.");
                notes.Add("Accent: native French from France.");
                notes.Add("Foreign words inside a French sentence are said in their own language; the French that follows goes straight back to a plain French accent.");

                // LES MOTS ANGLAIS, POUR TOUS LES PROFESSEURS — Camara, le
                // 04/10/2026 : Minamba disait « dèke » pour `deque`, « c'est
                // ridicule ».
                //
                // DES NOTES COURTES, ET LE MOT NOMMÉ QUAND IL EST LÀ. Mesuré le
                // jour même, trois prises par forme, sur « … avec `deque` et
                // `popleft()` » :
                //   - une longue note générale + la liste des termes du métier
                //     (ce qui tournait) : 2 prises sur 3 REDITES en entier, et
                //     « deque » dit une fois « di-kiou » — la liste contenait
                //     « dequeue », et le modèle a pris l'un pour l'autre ;
                //   - une ligne qui donne la prononciation des mots présents :
                //     6 prises sur 6 justes.
                // La liste entière faisait confondre les mots voisins : on ne
                // nomme donc que ceux de CETTE phrase. La redite, elle, n'en
                // dépend pas (1 prise sur 3 encore avec cette forme) : c'est
                // l'affaire de GardeRedite, pas de la consigne.
                notes.Add("English words and code (anything between backticks) are said the English way; never read the backticks aloud.");

                var guide = GuideDePrononciation(texte);
                if (guide is not null) notes.Add(guide);
            }

            notes.Add("#### TRANSCRIPT");
            notes.Add(texte);
            return string.Join("\n", notes);
        }

        /// <summary>
        /// Les mots que le modèle lit mal sans qu'on lui dise comment, et la
        /// façon de les dire. Un mot ne se nomme que s'il est DANS le texte :
        /// une liste entière en consigne fait redire la phrase et confondre
        /// les mots voisins. Un nouveau mot mal dit s'ajoute ici.
        /// </summary>
        private static readonly (string Mot, string Dire)[] Prononciations =
        [
            // « DECK », LA VRAIE PRONONCIATION — celle de la documentation de
            // Python. Camara l'a d'abord prise pour un mot lu à la française
            // (« dèke ») et a demandé « di-kiou » ; apprenant que c'était la
            // bonne, il a tranché le 04/10/2026 : « ne le force pas, faut
            // laisser ». Ne pas le changer sur une impression d'oreille.
            ("deque", "the English word \"deck\" (one syllable)"),
            ("dequeue", "\"dee-kyoo\""),
            ("enqueue", "\"en-kyoo\""),
            ("queue", "\"kyoo\""),
            ("popleft", "\"pop left\""),
            ("appendleft", "\"append left\""),
        ];

        internal static string? GuideDePrononciation(string texte)
        {
            var presents = Prononciations
                .Where(p => System.Text.RegularExpressions.Regex.IsMatch(
                    texte, $@"(?<![\p{{L}}]){p.Mot}(?![\p{{L}}])",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .Select(p => $"\"{p.Mot}\" = {p.Dire}")
                .ToList();

            return presents.Count == 0
                ? null
                : "Pronunciation guide — English words, they must sound English: " + string.Join("; ", presents) + ".";
        }

        private static readonly Dictionary<string, string> NomsLangues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "English",
            ["es"] = "Spanish",
            ["de"] = "German",
            ["it"] = "Italian",
            ["zh"] = "Mandarin Chinese",
            ["ar"] = "Modern Standard Arabic",
            ["fr"] = "French",
        };

        /// <summary>Les jetons d'entrée facturés, quand l'évènement les porte.</summary>
        private static int? JetonsEntree(JsonElement racine) =>
            racine.TryGetProperty("usageMetadata", out var usage)
            && usage.TryGetProperty("promptTokenCount", out var jetons)
            && jetons.TryGetInt32(out var nombre)
                ? nombre
                : null;

        /// <summary>
        /// Les jetons audio produits, quand l'évènement les porte. Le compte est
        /// cumulatif : le dernier évènement du flux donne le total facturé.
        /// </summary>
        private static int? JetonsSortie(JsonElement racine) =>
            racine.TryGetProperty("usageMetadata", out var usage)
            && usage.TryGetProperty("candidatesTokenCount", out var jetons)
            && jetons.TryGetInt32(out var nombre)
                ? nombre
                : null;

        private static IEnumerable<byte[]> Morceaux(JsonElement racine)
        {
            if (!racine.TryGetProperty("candidates", out var candidats)) yield break;

            foreach (var candidat in candidats.EnumerateArray())
            {
                if (!candidat.TryGetProperty("content", out var contenu)
                    || !contenu.TryGetProperty("parts", out var parties)) continue;

                foreach (var partie in parties.EnumerateArray())
                {
                    if (partie.TryGetProperty("inlineData", out var donnees)
                        && donnees.TryGetProperty("data", out var base64)
                        && base64.GetString() is { Length: > 0 } texte)
                    {
                        yield return Convert.FromBase64String(texte);
                    }
                }
            }
        }
    }
}
