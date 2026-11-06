using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>Ce que le professeur doit faire après ce que l'enfant vient de taper.</summary>
    public enum PhaseDuMot
    {
        /// <summary>C'est faux, il reste des essais : on reste sur ce mot.</summary>
        Reessayer,

        /// <summary>Troisième essai raté : le mot s'écrit au tableau, et on s'arrête là pour ce tour.</summary>
        AuTableau,

        /// <summary>Juste, ou mot déjà montré au tableau : le tableau s'efface, le mot suivant arrive.</summary>
        MotSuivant,

        /// <summary>Il a parlé au lieu de taper : ce n'est ni un essai ni une réponse. On reste sur ce mot.</summary>
        AuMicro,
    }

    public sealed record EtatDuMot(string Mot, string Tape, PhaseDuMot Phase, int Essai);

    /// <summary>
    /// L'ENFANT ÉCRIT CE QU'IL ENTEND, AU CLAVIER : UN MOT À LA FOIS, TROIS ESSAIS — Camara, le 05/11/2026 :
    /// « le professeur doit l'aider à corriger son mot avant de passer à un autre. Il fait 3 essais et s'il
    /// n'y arrive pas, il écrit le mot au tableau. Ensuite il pourra passer au prochain mot et supprimer du
    /// tableau le mot qui vient d'être corrigé ».
    ///
    /// LE CODE COMPTE ET COMPARE, PAS LE MODÈLE : il disait « Bravo » sur une faute et enchaînait avec le mot
    /// suivant dans la même réponse. Comparer deux mots et compter jusqu'à trois ne se demande pas à un modèle.
    /// La lecture du fil donne un fait pour le tour ; <see cref="Imposer"/> tient la réponse si le fait ne suffit pas.
    ///
    /// LES ACCENTS NE COMPTENT PAS POUR FAUX : au CP, « velo » tapé pour « vélo » est le clavier, pas l'oreille.
    /// </summary>
    public static partial class DicteeDeMots
    {
        public const int EssaisMax = 3;

        private const string MarqueurClavier = "[SON AU CLAVIER";
        private const string MarqueurTape = "CE MESSAGE EST TAPÉ";
        private const string MarqueurMicro = "VIENT DU MICRO";
        private const string Efface = "[TABLEAU_EFFACE]";

        /// <summary>Au-delà, il dit autre chose qu'un mot ou « répète » : le professeur reste libre (« j'arrête », une question).</summary>
        private const int MotsAuMicroMax = 3;

        [GeneratedRegex(@"\[(FR|EN)\](.*?)\[/\1\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex BaliseDeVoix();

        [GeneratedRegex(@"\[ARDOISE\](.*?)\[/ARDOISE\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"\[VITESSE(?::[^\]]*)?\]", RegexOptions.IgnoreCase)]
        private static partial Regex Vitesse();

        private static string Propre(string mot) => mot.Trim().Trim('.', '!', '?', ',', ';', ':', '«', '»', '"', ' ');

        private static bool EstUnMot(string texte) =>
            texte.Length > 0 && !texte.Any(char.IsWhiteSpace);

        private static string SansAccents(string texte)
        {
            var sortie = new StringBuilder(texte.Length);
            foreach (var c in texte.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sortie.Append(c);
            }
            return sortie.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace("œ", "oe");
        }

        /// <summary>Le dernier mot seul dit à voix haute dans ce message ; null s'il n'y en a pas.</summary>
        public static string? MotDit(string? contenu)
        {
            if (string.IsNullOrEmpty(contenu)) return null;

            foreach (var balise in BaliseDeVoix().Matches(contenu).Reverse())
            {
                var mot = Propre(balise.Groups[2].Value);
                if (EstUnMot(mot)) return mot;
            }
            return null;
        }

        /// <summary>Ce que l'enfant a tapé, sans le fait que l'écran y a joint.</summary>
        private static string? TexteTape(string? contenu)
        {
            if (contenu is null || !contenu.Contains(MarqueurTape, StringComparison.Ordinal)) return null;

            var fin = contenu.IndexOf(MarqueurClavier, StringComparison.Ordinal);
            return Propre(fin >= 0 ? contenu[..fin] : contenu);
        }

        /// <summary>L'état de ce tour ; null hors de cet exercice au clavier, ou si l'enfant n'a pas répondu par un mot tapé.</summary>
        public static EtatDuMot? Lire(string? messageEleve, IReadOnlyList<(string? Role, string? Contenu)> historique)
        {
            if (messageEleve is null || !messageEleve.Contains(MarqueurClavier, StringComparison.Ordinal)) return null;

            var dernier = historique.LastOrDefault(m => m.Role == "assistant").Contenu;

            // LE MOT EST AU TABLEAU DEPUIS LE TOUR D'AVANT : l'enfant l'a vu, quoi qu'il réponde.
            if (dernier is not null && Ardoise().Matches(dernier).LastOrDefault() is { } auTableau
                && EstUnMot(Propre(auTableau.Groups[1].Value)) && MotDit(dernier) is null)
            {
                return new EtatDuMot(Propre(auTableau.Groups[1].Value), string.Empty, PhaseDuMot.MotSuivant, EssaisMax);
            }

            // AU MICRO : relevé à l'essai, « Moto. » dit à voix haute a reçu « Bravo, c'est bien écrit » et le mot suivant.
            if (messageEleve.Contains(MarqueurMicro, StringComparison.Ordinal))
            {
                var fin = messageEleve.IndexOf(MarqueurClavier, StringComparison.Ordinal);
                var ditAuMicro = Propre(fin >= 0 ? messageEleve[..fin] : messageEleve);
                var enCours = historique.Where(m => m.Role == "assistant").Select(m => MotDit(m.Contenu)).LastOrDefault(m => m is not null);

                return enCours is not null && ditAuMicro.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length <= MotsAuMicroMax
                    ? new EtatDuMot(enCours, ditAuMicro, PhaseDuMot.AuMicro, 0)
                    : null;
            }

            var tape = TexteTape(messageEleve);
            if (tape is null || !EstUnMot(tape)) return null;

            // LE MOT EN COURS, ET DEPUIS QUAND : les messages du professeur qui le disent, en remontant. Une réponse
            // n'est un essai que si elle suit l'un d'eux : celle d'avant répondait au mot d'avant.
            string? mot = null;
            var depuis = -1;

            for (var i = historique.Count - 1; i >= 0; i -= 1)
            {
                if (historique[i].Role != "assistant") continue;

                var dit = MotDit(historique[i].Contenu);
                if (dit is null) continue;

                mot ??= dit;
                if (!string.Equals(SansAccents(dit), SansAccents(mot), StringComparison.Ordinal)) break;
                depuis = i;
            }

            if (mot is null) return null;

            var essaisPasses = historique.Skip(depuis + 1)
                .Count(m => m.Role != "assistant" && TexteTape(m.Contenu) is { } passe && EstUnMot(passe));

            var essai = essaisPasses + 1;

            if (string.Equals(SansAccents(tape), SansAccents(mot), StringComparison.Ordinal))
            {
                return new EtatDuMot(mot, tape, PhaseDuMot.MotSuivant, essai);
            }

            return new EtatDuMot(mot, tape, essai >= EssaisMax ? PhaseDuMot.AuTableau : PhaseDuMot.Reessayer, essai);
        }

        /// <summary>Le fait pour le professeur, à ce tour-ci. Sans crochet fermant avant la fin : il ne s'affiche jamais.</summary>
        public static string Fait(EtatDuMot etat, string langue)
        {
            var voix = langue == LanguesDesSons.Anglais ? "[EN]" : "[FR]";

            return etat.Phase switch
            {
                PhaseDuMot.Reessayer =>
                    $"[Fait pour ce tour, qui l'emporte sur tout le reste : tu as dicté « {etat.Mot} » et il a tapé « {etat.Tape} » : ce n'est PAS encore juste, essai {etat.Essai} sur {EssaisMax}. "
                    + $"Ne dis pas bravo. NE PASSE PAS à un autre mot. N'écris « {etat.Mot} » ni au tableau ni dans ta phrase, ni entier, ni épelé, ni découpé en syllabes. Encourage-le et donne un seul indice sur ce qui change, par exemple une lettre en trop ou le son du milieu, en toutes lettres dans ta phrase, sans balise. "
                    + $"Le code redira le mot à voix haute à la fin de ton message : termine par « réessaie ».]",
                PhaseDuMot.AuMicro =>
                    $"[Fait pour ce tour, qui l'emporte sur tout le reste : tu as dicté « {etat.Mot} » et il vient de PARLER au micro au lieu de taper : ce n'est pas une réponse écrite, et ça ne compte pas comme un essai. "
                    + $"Ne dis ni bravo, ni que c'est juste, ni que c'est faux, et ne passe pas à un autre mot. N'écris jamais « {etat.Mot} » dans ta phrase. S'il demande de répéter, dis que tu répètes ; sinon, dis-lui de le taper avec le clavier. "
                    + "Le code redira le mot à voix haute à la fin de ton message.]",
                PhaseDuMot.AuTableau =>
                    $"[Fait pour ce tour, qui l'emporte sur tout le reste : tu as dicté « {etat.Mot} » et il a tapé « {etat.Tape} » : troisième essai, c'est encore faux. "
                    + $"Encourage-le, dis-lui que tu écris le mot au tableau pour qu'il voie la bonne écriture, et explique en une phrase ce qui change. "
                    + "NE DICTE PAS de nouveau mot dans ce message. Termine en lui demandant de te dire quand il est prêt pour le mot suivant. Le code écrit le mot au tableau.]",
                _ =>
                    $"[Fait pour ce tour, qui l'emporte sur tout le reste : {(etat.Tape.Length > 0 ? $"il a tapé « {etat.Tape} », c'est juste. Félicite-le en une phrase." : "il a vu le mot au tableau.")} "
                    + $"Le code efface le tableau. Dicte maintenant UN NOUVEAU mot, jamais « {etat.Mot} », seul sur sa ligne à la fin de ton message, dans {voix}. Rien d'autre après.]",
            };
        }

        /// <summary>
        /// LA RÉPONSE TENUE, si le fait n'a pas suffi : jamais de mot suivant avant son tour, jamais de mot écrit
        /// avant le troisième essai. Les balises placées en pleine phrase deviennent du texte : elles laissaient
        /// des trous à l'écran (« il y a un  à la fin de  pour faire . »).
        /// </summary>
        public static string Imposer(string texte, EtatDuMot etat, string langue)
        {
            var sansVitesse = Vitesse().Replace(texte, string.Empty);
            var etiquette = langue == LanguesDesSons.Anglais ? "EN" : "FR";
            var voix = $"[{etiquette}]{etat.Mot}[/{etiquette}]";

            switch (etat.Phase)
            {
                case PhaseDuMot.Reessayer:
                case PhaseDuMot.AuMicro:
                {
                    var t = Ardoise().Replace(sansVitesse.Replace(Efface, string.Empty, StringComparison.OrdinalIgnoreCase), m => CourtOuGarde(m));
                    t = BaliseDeVoix().Replace(t, m => Propre(m.Groups[2].Value).Equals(etat.Mot, StringComparison.OrdinalIgnoreCase) || EstUnMotDicte(t, m) ? string.Empty : m.Groups[2].Value);
                    t = SansMorceaux(RegleVerificationSon.Remplacer(t, etat.Mot, "ce mot"), etat.Mot);
                    return $"{t.TrimEnd()}\n\n{voix}";
                }

                case PhaseDuMot.AuTableau:
                {
                    var t = Ardoise().Replace(sansVitesse, m => CourtOuGarde(m));
                    t = BaliseDeVoix().Replace(t, m => Propre(m.Groups[2].Value).Equals(etat.Mot, StringComparison.OrdinalIgnoreCase) || EstUnMotDicte(t, m) ? string.Empty : m.Groups[2].Value);
                    return $"{t.TrimEnd()}\n\n[ARDOISE] {etat.Mot} [/ARDOISE]";
                }

                default:
                {
                    var t = Ardoise().Replace(sansVitesse, m => CourtOuGarde(m));
                    var balises = BaliseDeVoix().Matches(t);
                    if (balises.Count > 0)
                    {
                        var derniere = balises[^1];
                        t = BaliseDeVoix().Replace(t, m => m.Index == derniere.Index ? m.Value : m.Groups[2].Value);
                    }
                    return t.Contains(Efface, StringComparison.OrdinalIgnoreCase) ? t : $"{Efface}\n\n{t.TrimStart()}";
                }
            }
        }

        /// <summary>
        /// LE MOT QUE L'ENFANT DOIT TROUVER NE S'ÉCRIT NULLE PART, partout où ce message le dit : hors de la balise de
        /// voix, il devient « ce mot ». Le mot montré au tableau après le troisième essai n'est plus à trouver : il
        /// ne passe pas ici.
        /// </summary>
        public static string MasquerMotDicte(string texte)
        {
            var mot = MotDit(texte);
            if (mot is null) return texte;

            // UN MOT DE LA PHRASE N'EST PAS LA RÉPONSE — Camara, le 05/11/2026, 21h34 : la syllabe dictée était « tu », et
            // « Qu'est-ce que tu as entendu ? » devenait « Qu'est-ce que ce mot as entendu ? ». Quand le son dicté est
            // aussi un petit mot de la langue (tu, le, la, te, de, ma, son…), seule sa forme citée (« tu ») est masquée.
            var outil = MotsOutils.Contains(mot.Trim());
            string Masquer(string morceau) => outil
                ? Regex.Replace(morceau, $@"(«\s*|""|“){Regex.Escape(mot.Trim())}(\s*»|""|”)", "$1ce mot$2", RegexOptions.IgnoreCase)
                : RegleVerificationSon.Remplacer(morceau, mot, "ce mot");

            var sortie = new StringBuilder();
            var position = 0;
            foreach (Match balise in BaliseDeVoix().Matches(texte))
            {
                sortie.Append(Masquer(texte[position..balise.Index]));
                sortie.Append(balise.Value);
                position = balise.Index + balise.Length;
            }
            sortie.Append(Masquer(texte[position..]));
            return sortie.ToString();
        }

        /// <summary>Les petits mots de la langue qui sont aussi des syllabes dictées : ils ne se masquent que cités.</summary>
        private static readonly HashSet<string> MotsOutils = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "à", "ai", "as", "au", "bon", "ce", "ces", "de", "des", "dit", "du", "en", "es", "est", "et", "il", "je", "la", "là",
            "le", "les", "lu", "ma", "me", "mes", "mon", "ne", "ni", "nu", "on", "ou", "où", "pas", "pu", "sa", "se", "ses", "si",
            "son", "su", "ta", "te", "tes", "ton", "tu", "un", "va", "vu", "y",
        };

        [GeneratedRegex(@"(?:«\s*|""|“)(?<morceau>\p{L}{2,})(?:\s*»|""|”)")]
        private static partial Regex MorceauCite();

        [GeneratedRegex(@"\p{L}(?:\s*-\s*\p{L}){1,}")]
        private static partial Regex MotEpele();

        /// <summary>
        /// L'INDICE NE REDONNE PAS LE MOT EN MORCEAUX : à l'essai, « ce mot a juste deux sons : "mo" et "to" ». Une
        /// lettre seule reste permise : « un o en trop » aide sans donner la réponse.
        /// </summary>
        private static string SansMorceaux(string texte, string mot)
        {
            var cible = SansAccents(mot);
            var sansCites = MorceauCite().Replace(texte, m => cible.Contains(SansAccents(m.Groups["morceau"].Value), StringComparison.Ordinal) ? "ce morceau" : m.Value);

            return MotEpele().Replace(sansCites, m =>
                SansAccents(new string(m.Value.Where(char.IsLetter).ToArray())) == cible ? "ce mot" : m.Value);
        }

        /// <summary>
        /// LE MOT À ÉCOUTER FINIT LE MESSAGE — Camara, le 05/11/2026 : « il faut qu'on distingue le mot à écouter et
        /// la phrase du professeur, parfois il enchaîne le mot avec une autre phrase ». Plus rien ne le suit : la
        /// question passe avant lui, et l'enfant répond juste après l'avoir entendu.
        /// </summary>
        public static string MotALaFin(string texte)
        {
            var balise = BaliseDeVoix().Matches(texte).LastOrDefault();
            if (balise is null || !EstUnMot(Propre(balise.Groups[2].Value))) return texte;

            var avant = texte[..balise.Index].TrimEnd();
            var apres = texte[(balise.Index + balise.Length)..].Trim().TrimStart('.', '!', ',', ';', ':').Trim();
            var corps = apres.Length == 0 ? avant : $"{avant}\n\n{apres}";
            return corps.Trim().Length == 0 ? balise.Value : $"{corps.TrimEnd()}\n\n{balise.Value}";
        }

        /// <summary>Seule la dernière balise reste une voix sans texte : les autres, en pleine phrase, se lisent.</summary>
        public static string GarderSeulementLaDerniereVoix(string texte)
        {
            var balises = BaliseDeVoix().Matches(texte);
            if (balises.Count < 2) return texte;

            var derniere = balises[^1];
            return BaliseDeVoix().Replace(texte, m => m.Index == derniere.Index ? m.Value : m.Groups[2].Value);
        }

        private static string CourtOuGarde(Match bloc) =>
            bloc.Groups[1].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length <= 3 ? string.Empty : bloc.Value;

        private static bool EstUnMotDicte(string texte, Match balise)
        {
            // Seul sur sa ligne, ou tout au bout du message : c'est un mot dicté, pas un mot de la phrase.
            var apres = texte[(balise.Index + balise.Length)..];
            if (apres.All(c => char.IsWhiteSpace(c) || char.IsPunctuation(c))) return true;

            var debut = balise.Index == 0 ? 0 : texte.LastIndexOf('\n', balise.Index - 1) + 1;
            var fin = texte.IndexOf('\n', balise.Index + balise.Length);
            var ligne = texte[debut..(fin < 0 ? texte.Length : fin)];
            return ligne.Trim().Length == balise.Value.Trim().Length;
        }
    }
}
