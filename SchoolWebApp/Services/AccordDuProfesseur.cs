using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LE PROFESSEUR S'ACCORDE AVEC LUI-MÊME — Camara, le 04/10/2026, avec
    /// Marine : « elle m'a dit "content de te retrouver" ».
    ///
    /// TROISIÈME FOIS, ET LES DEUX PREMIÈRES RÉPONSES N'ONT PAS SUFFI. Le bloc
    /// d'identité dit le genre du professeur ; depuis le 30/09, un rappel le
    /// redit à chaque tour (<c>MarqueurGenre</c>). Marine a quand même écrit
    /// « content », face à un garçon : l'accord de l'élève tire le sien. Une
    /// règle ne vaut pas un fait, et ici même le fait n'a pas tenu — il reste
    /// à le FAIRE, dans le texte, avant qu'il ne parte à l'écran et à la voix.
    ///
    /// ÉTROIT, PARCE QUE « CONTENT » EST PRESQUE TOUJOURS L'ÉLÈVE. « Tu es
    /// content ? », « il était content » ne se touchent pas. On ne corrige que
    /// là où c'est le professeur qui parle DE LUI :
    ///   - après « je suis » : « je suis vraiment content de toi » ;
    ///   - en tête de proposition, « je suis » sous-entendu, devant « de » ou
    ///     « que » : « Salut Bilal, content de te retrouver ! », « Ravi que… » ;
    ///   - « Désolé » en tête de phrase : c'est toujours lui qui s'excuse.
    ///
    /// DANS LES DEUX SENS : un professeur qui écrit « contente » se corrige de
    /// même. La liste est celle des adjectifs qu'un professeur dit de lui.
    /// </summary>
    public static partial class AccordDuProfesseur
    {
        private static readonly (string Masculin, string Feminin)[] Adjectifs =
        [
            ("content", "contente"), ("ravi", "ravie"), ("désolé", "désolée"), ("navré", "navrée"),
            ("sûr", "sûre"), ("certain", "certaine"), ("fier", "fière"), ("heureux", "heureuse"),
            ("prêt", "prête"), ("curieux", "curieuse"), ("surpris", "surprise"),
            ("impressionné", "impressionnée"), ("enchanté", "enchantée"), ("épaté", "épatée"),
            ("persuadé", "persuadée"), ("convaincu", "convaincue"),
        ];

        private static readonly string Formes = string.Join("|",
            Adjectifs.SelectMany(a => new[] { a.Feminin, a.Masculin }).Select(Regex.Escape));

        /// <summary>« je suis [très] content », « je ne suis pas sûr ».</summary>
        private static readonly Regex ApresJeSuis = new(
            @"(?<=\b[Jj]e\s+(?:ne\s+)?suis\s+(?:pas\s+)?(?:(?:si|très|trop|vraiment|super|tellement|bien|tout|toute)\s+)?)"
            + $@"(?<adj>{Formes})(?![\p{{L}}])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>« …, content de te retrouver », « Ravi que tu sois là ».</summary>
        private static readonly Regex EnTete = new(
            @"(?<=(?:^|[.!?…,;:]\s+(?:et\s+)?|\n\s*))"
            + $@"(?<adj>{Formes})(?![\p{{L}}])(?=\s+(?:de|que)\b|\s+(?:d|qu)['’])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>« Désolé, je me suis trompé », « Désolé ! » — toujours lui.</summary>
        private static readonly Regex Excuse = new(
            @"(?<=(?:^|[.!?…]\s+|\n\s*))(?<adj>Désolée?|Navrée?)(?![\p{L}])(?=\s*[,!.…]|\s+pour\b)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static string Accorder(string adjectif, bool femme)
        {
            foreach (var (masculin, feminin) in Adjectifs)
            {
                var est = adjectif.Equals(masculin, StringComparison.OrdinalIgnoreCase) ? masculin
                    : adjectif.Equals(feminin, StringComparison.OrdinalIgnoreCase) ? feminin
                    : null;

                if (est is null) continue;

                var voulu = femme ? feminin : masculin;
                return char.IsUpper(adjectif[0]) ? char.ToUpperInvariant(voulu[0]) + voulu[1..] : voulu;
            }

            return adjectif;
        }

        /// <summary>
        /// CE QUI N'EST PAS SA VOIX À LUI : le texte d'une dictée, un passage
        /// d'écoute en langue étrangère, ce qu'il écrit au tableau, les blocs
        /// d'archive — et tout ce qu'il CITE entre guillemets, à commencer par
        /// la copie de l'élève. « Je suis content de mon voyage », dicté par une
        /// professeure, doit rester tel quel. Le bloc [FR] n'y est pas : en
        /// expression orale, c'est bien elle qui y parle.
        /// </summary>
        [GeneratedRegex(
            @"\[(?<ferme>/)?(?:DICTEE|ARDOISE|FICHE|RAPPORT|EN|ES|DE|IT|ZH|AR|DICTEE_CORRIGEE|"
            + @"COMPREHENSION_ORALE|EXPRESSION_ORALE|EXPRESSION_ECRITE)\]|(?<ouvre>«)|(?<clot>»)")]
        private static partial Regex Bornes();

        /// <summary>Où l'on en est : dans combien de blocs, et dans une citation ou non.</summary>
        public readonly record struct Etat(int Blocs, bool Citation)
        {
            public bool Protege => Blocs > 0 || Citation;

            public Etat Apres(Match borne) =>
                borne.Groups["ouvre"].Success ? this with { Citation = true }
                : borne.Groups["clot"].Success ? this with { Citation = false }
                : borne.Groups["ferme"].Success ? this with { Blocs = Math.Max(0, Blocs - 1) }
                : this with { Blocs = Blocs + 1 };
        }

        /// <summary>L'état au bout du texte, parti de <paramref name="depart"/>.</summary>
        public static Etat Avancer(Etat depart, string texte)
        {
            var etat = depart;
            foreach (Match borne in Bornes().Matches(texte)) etat = etat.Apres(borne);
            return etat;
        }

        /// <summary>Le texte, avec les accords du professeur à son genre.</summary>
        public static string Corriger(string? texte, bool femme) => Corriger(string.Empty, texte, femme, default);

        /// <summary>
        /// La même chose, avec ce qui PRÉCÈDE le texte pour seul contexte : rien
        /// n'y est changé, et il n'est pas rendu. Sert au flux, où « je suis »
        /// est parfois déjà parti quand « content » arrive.
        /// </summary>
        public static string Corriger(string avant, string? texte, bool femme, Etat depart)
        {
            if (string.IsNullOrEmpty(texte)) return texte ?? string.Empty;

            var entier = avant + texte;

            // Les zones protégées, relevées une fois sur le texte d'origine :
            // les corrections n'ajoutent ni ne retirent de borne.
            var bornes = Bornes().Matches(entier);

            bool Protege(int position)
            {
                var etat = depart;
                foreach (Match borne in bornes)
                {
                    if (borne.Index >= position) break;
                    etat = etat.Apres(borne);
                }

                return etat.Protege;
            }

            // Une correction allonge le texte d'une lettre : les positions des
            // bornes ne valent que pour la première passe. Les trois règles
            // sont donc jouées en une seule, sur le texte d'origine.
            var aCorriger = new SortedDictionary<int, (int Longueur, string Par)>();

            foreach (var regle in new[] { ApresJeSuis, EnTete, Excuse })
            {
                foreach (Match trouve in regle.Matches(entier))
                {
                    if (trouve.Index < avant.Length || Protege(trouve.Index)) continue;

                    var accorde = Accorder(trouve.Value, femme);
                    if (accorde != trouve.Value) aCorriger[trouve.Index] = (trouve.Length, accorde);
                }
            }

            if (aCorriger.Count == 0) return texte;

            var sortie = new System.Text.StringBuilder(entier.Length + aCorriger.Count);
            var curseur = avant.Length;

            foreach (var (position, (longueur, par)) in aCorriger)
            {
                sortie.Append(entier, curseur, position - curseur).Append(par);
                curseur = position + longueur;
            }

            return sortie.Append(entier, curseur, entier.Length - curseur).ToString();
        }
    }
}
