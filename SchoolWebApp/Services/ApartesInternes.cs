using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LES NOTES DE RÉGIE DU PROFESSEUR NE PARTENT PAS CHEZ L'ENFANT — trouvé au bout en bout du signalement #53
    /// (21/11/2026). Environ une fois sur vingt tours faux, Nora commençait sa réponse par
    /// « (On garde 3e réponse fausse → réexplication douce + dernier essai) » : la règle commune des essais
    /// (<see cref="Prompts.PromptsPedagogiques.RegleDesTroisEssais"/>) recopiée en aparté, puis affichée, lue à voix haute
    /// et enregistrée. Aucun filtre de flux ne la retirait.
    ///
    /// PORTÉE GÉNÉRIQUE : la règle des essais est commune à tous les professeurs, l'aparté peut donc venir de n'importe
    /// lequel. Le filet est posé dans le flux commun de <c>ChatViewModelBuilder</c> (écran ET voix), dans le texte
    /// enregistré, et dans l'historique relu (sinon le professeur imite son propre aparté au tour suivant).
    ///
    /// PRUDENCE : les parenthèses pédagogiques restent — « (3 + 4) × 2 », « (le chat) », « (bleu + jaune → vert) »,
    /// « (dernier essai !) ». Un aparté n'est retiré que s'il remplit DEUX conditions :
    /// 1. au moins DEUX familles d'indices de régie parmi : un compteur d'essais (« 3e réponse », « dernier essai »), un
    ///    verdict technique (« réponse fausse », ou « FAUX » en capitales dans la forme d'un fait seulement), un mot de régie
    ///    (« on garde », « règle des », « réexplication », « l'élève », « → indice »), et des symboles télégraphiques
    ///    (flèche, « mot + mot »), qui ne comptent jamais pour plus d'une famille ;
    /// 2. au moins UN indice FORT, qu'on ne dit jamais à un enfant : un compteur numéroté qui ne lui est pas adressé
    ///    (« 3e réponse », pas « ta 2e réponse » ni « ton dernier essai »), « on garde » (hors retenue : « on garde 1 »),
    ///    « règle des essais », « réexplication », « je compte les essais », « vérifié par l'application », « note pour moi »,
    ///    « passage à la suite ».
    /// Revue d'Omar (21/11/2026) : sans la condition 2, « (on compte les dizaines → 3) », « (VRAI ou FAUX → à toi) »,
    /// « (l'élève → les élèves) » ou « (c'est ton dernier essai → concentre-toi !) » étaient retirés. En cas de doute, on
    /// GARDE : un aparté qui passe est un défaut rare (aucun en ligne depuis le 11/11), une phrase coupée chez l'enfant non.
    ///
    /// UN SEUL CODE POUR LE FLUX ET LE TEXTE ENTIER : <see cref="Retirer"/> passe le texte entier dans le même
    /// <see cref="Filtre"/>, donc l'écran, la voix et l'historique disent exactement la même chose.
    /// </summary>
    public static partial class ApartesInternes
    {
        /// <summary>Au-delà, ce n'est pas un aparté de régie : le texte retenu part tel quel.</summary>
        public const int LongueurMaximum = 240;

        /// <summary>Le texte entier, sans ses apartés de régie (même résultat que le flux).</summary>
        public static string Retirer(string? texte) => Retirer(texte, out _);

        public static string Retirer(string? texte, out int retires)
        {
            if (string.IsNullOrEmpty(texte) || texte.IndexOf('(') < 0)
            {
                retires = 0;
                return texte ?? string.Empty;
            }

            var filtre = new Filtre();
            var sortie = filtre.Passer(texte) + filtre.Terminer();
            retires = filtre.Retires;
            return sortie;
        }

        /// <summary>
        /// Le contenu ENTRE les parenthèses (ou avec elles) est-il une note de régie ? Au moins un indice FORT, et au moins
        /// deux familles d'indices.
        /// </summary>
        public static bool EstUnAparte(string contenu)
        {
            if (string.IsNullOrWhiteSpace(contenu)) return false;

            var compteurFort = CompteurFort().IsMatch(contenu);
            var regieForte = RegieForte().IsMatch(contenu);

            // Sans indice fort, on garde : un mot de régie isolé (« l'élève », « on compte », « FAUX », « dernier
            // essai ») peut appartenir à une phrase dite à l'enfant.
            if (!compteurFort && !regieForte) return false;

            var familles = 0;
            if (compteurFort || Compteur().IsMatch(contenu)) familles++;
            if (VerdictTechnique().IsMatch(contenu) || VerdictDeFait().IsMatch(contenu)) familles++;
            if (regieForte || Regie().IsMatch(contenu)) familles++;
            if (Fleche().IsMatch(contenu) || PlusEntreMots().IsMatch(contenu)) familles++;

            // Les symboles seuls ne suffisent jamais : il faut au moins un indice en mots.
            return familles >= 2;
        }

        // Ce qui n'est pas dit à l'enfant : « ta 2e réponse », « ton troisième essai » lui sont adressés.
        private const string PasAdresseALEnfant = @"(?<!(?<!\p{L})(?:ta|ton|tes|votre|vos)\s+)";

        // INDICE FORT — un compteur numéroté : « 3e réponse », « 1re erreur », « troisième erreur », « réponse fausse n°2 »,
        // « 3 réponses fausses », « essai 3/4 ». « Premier » et « dernier » n'en sont pas : « c'est ton dernier essai » est
        // justement ce que la règle demande de dire.
        [GeneratedRegex(
            PasAdresseALEnfant
            + @"(?:(?<![\p{L}\d])(?:\d+\s*(?:e|ème|eme|re|ère|er)|deuxi[eè]me|seconde?|troisi[eè]me|quatri[eè]me|cinqui[eè]me)\s+(?:réponses?|essais?|erreurs?|tentatives?|fautes?)(?!\p{L})"
            + @"|(?<!\p{L})(?:réponses?|essais?|erreurs?|tentatives?)(?:\s+(?:fausses?|faux|ratée?s?|manquée?s?))?\s+(?:n°|no\.?|numéro|#)\s*\d"
            + @"|(?<![\p{L}\d])\d+\s+(?:réponses?|essais?|erreurs?|tentatives?)\s+(?:fausses?|faux|ratée?s?|manquée?s?)(?!\p{L})"
            + @"|(?<!\p{L})essais?\s+\d\s*/\s*\d)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex CompteurFort();

        // Famille « compteur » (indice faible) : tout compteur, y compris « premier essai », « dernier essai », « ta 2e
        // réponse ».
        [GeneratedRegex(
            @"(?<![\p{L}\d])(?:\d+\s*(?:e|ème|eme|re|ère|er)|premi[eè]re?|deuxi[eè]me|seconde?|troisi[eè]me|quatri[eè]me|cinqui[eè]me|derni[eè]re?)\s+(?:réponses?|essais?|erreurs?|tentatives?|fautes?)(?!\p{L})"
            + @"|(?<!\p{L})(?:réponses?|essais?|erreurs?|tentatives?)(?:\s+(?:fausses?|faux|ratée?s?|manquée?s?))?\s+(?:n°|no\.?|numéro|#)\s*\d"
            + @"|(?<![\p{L}\d])\d+\s+(?:réponses?|essais?|erreurs?|tentatives?)\s+(?:fausses?|faux|ratée?s?|manquée?s?)(?!\p{L})"
            + @"|(?<!\p{L})essais?\s+\d\s*/\s*\d",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Compteur();

        // « réponse fausse », « essai raté », « réponses justes ».
        [GeneratedRegex(
            @"(?<!\p{L})(?:réponses?|essais?|tentatives?)\s+(?:fausses?|faux|ratée?s?|manquée?s?|incorrecte?s?|justes?|correcte?s?|bonnes?)(?!\p{L})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex VerdictTechnique();

        // Le verdict en capitales SEULEMENT dans la forme d'un fait du code : « Vérifié par l'application … FAUX », ou
        // « … → FAUX » en fin de parenthèse. « (VRAI ou FAUX → à toi) » n'en est pas un. Capitales exigées : « c'est juste »
        // reste.
        [GeneratedRegex(
            @"(?i:v[ée]rifi[ée]\s+par\s+l['’]application)[^)]*?(?<!\p{L})(?:FAUX|FAUSSE|JUSTE)(?!\p{L})"
            + @"|(?:→|->|=>|⇒|:)\s*(?:FAUX|FAUSSE|JUSTE)\s*[.!]?\s*\)?\s*$",
            RegexOptions.CultureInvariant)]
        private static partial Regex VerdictDeFait();

        // INDICE FORT — les mots de la régie qu'on ne dit jamais à un enfant. « On garde » au sens de la retenue (« on
        // garde 1 », « on garde la retenue ») n'en est pas un ; « réexpliquer » non plus (« je vais te réexpliquer »).
        [GeneratedRegex(
            @"(?<!\p{L})(?:(?:on|je)\s+garde(?![^)]{0,25}?(?:retenue|(?<![\p{L}\d])\d+(?![\p{L}\d])))"
            + @"|r[eè]gle\s+des\s+(?:(?:trois|3|quatre|4)\s+)?(?:essais|erreurs)"
            + @"|r[ée]-?explications?"
            + @"|(?:je|on)\s+compte\s+(?:les|mes|ses|des)\s+(?:essais|erreurs|r[ée]ponses)"
            + @"|compteur\s+(?:d['’]|des\s+)(?:essais|erreurs)|compte\s+des\s+(?:essais|erreurs)"
            + @"|v[ée]rifi[ée]\s+par\s+l['’]application|note\s+pour\s+moi|pour\s+moi\s+seule?"
            + @"|passage\s+[àa]\s+la\s+(?:suite|question))(?!\p{L})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RegieForte();

        // Famille « régie » (indice faible) : ce que le professeur décide, compte ou applique, et les fiches qu'on lui
        // donne. « On compte » / « je compte » seuls n'y sont plus (« (on compte les dizaines → 3) », revue d'Omar).
        [GeneratedRegex(
            @"(?<!\p{L})(?:on\s+garde|je\s+garde|on\s+reste\s+sur|je\s+reste\s+sur|compteur|compte\s+des\s+essais"
            + @"|r[eè]gle\s+des|selon\s+la\s+r[eè]gle|r[ée]-?explications?|r[ée]-?expliquer|l'[ée]l[eè]ve|l’[ée]l[eè]ve"
            + @"|historique|indice\s+exact|v[ée]rifi[ée]\s+par\s+l|note\s+pour\s+moi|pour\s+moi\s+seul|sans\s+donner\s+la\s+r[ée]ponse"
            + @"|ne\s+pas\s+donner\s+la\s+r[ée]ponse|passage\s+[àa]\s+la\s+(?:suite|question)|je\s+passe\s+[àa]\s+la\s+(?:suite|question))(?!\p{L})"
            + @"|(?:→|->|=>|⇒|:)\s*(?:indice|r[ée]explication|dernier\s+essai|passage|on\s+passe|question\s+suivante)(?!\p{L})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Regie();

        [GeneratedRegex(@"→|⇒|⟶|->|=>")]
        private static partial Regex Fleche();

        // « réexplication douce + dernier essai » : un plus entre deux MOTS (pas entre deux nombres).
        [GeneratedRegex(@"\p{L}{3,}\s*\+\s*\p{L}{3,}")]
        private static partial Regex PlusEntreMots();

        /// <summary>
        /// LE FILTRE DE FLUX. Tout passe sans retard, sauf ce qui suit une parenthèse ouvrante : retenu jusqu'à la
        /// fermante (puis rendu, ou retiré si c'est un aparté), ou jusqu'à un saut de ligne ou <see cref="LongueurMaximum"/>
        /// caractères (rendu tel quel). Les blancs en fin de morceau attendent le caractère suivant, pour recoller
        /// proprement le texte autour d'un aparté retiré : rien en tête de message, une seule espace au milieu, aucune
        /// devant « . » ou « , », et le saut de ligne le plus large s'il y en avait un.
        /// </summary>
        public sealed class Filtre
        {
            private readonly StringBuilder _retenu = new();
            private string _blancsAvant = string.Empty;
            private string _blancs = string.Empty;
            private int _profondeur;
            private bool _apresRetrait;
            private bool _texteEnvoye;

            /// <summary>Nombre d'apartés retirés depuis le début du flux.</summary>
            public int Retires { get; private set; }

            public string Passer(string? fragment)
            {
                if (string.IsNullOrEmpty(fragment)) return string.Empty;

                var sortie = new StringBuilder();

                foreach (var c in fragment)
                {
                    if (_profondeur > 0)
                    {
                        Retenir(c, sortie);
                        continue;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        _blancs += c;
                        continue;
                    }

                    if (c == '(')
                    {
                        // Les blancs d'avant restent en attente : ils dépendent de la décision.
                        _retenu.Clear().Append(c);
                        _profondeur = 1;
                        continue;
                    }

                    Ecrire(c, sortie);
                }

                return sortie.ToString();
            }

            public string Terminer()
            {
                var sortie = new StringBuilder();

                if (_profondeur > 0)
                {
                    // Parenthèse jamais fermée : rien à juger, le texte part tel quel.
                    Rendre(sortie);
                }

                // Les blancs de la toute fin : gardés, sauf derrière un aparté retiré.
                if (!_apresRetrait) sortie.Append(_blancs);
                _blancs = string.Empty;
                _blancsAvant = string.Empty;
                _apresRetrait = false;
                return sortie.ToString();
            }

            private void Retenir(char c, StringBuilder sortie)
            {
                _retenu.Append(c);

                if (c == '(') _profondeur++;
                else if (c == ')') _profondeur--;

                if (_profondeur == 0)
                {
                    if (EstUnAparte(_retenu.ToString()))
                    {
                        Retires++;
                        _retenu.Clear();
                        // Les blancs d'avant attendent le caractère suivant, qui décide comment recoller.
                        _blancsAvant = _apresRetrait ? Fusion(_blancsAvant, _blancs) : _blancs;
                        _blancs = string.Empty;
                        _apresRetrait = true;
                        return;
                    }

                    Rendre(sortie);
                    return;
                }

                if (c == '\n' || _retenu.Length > LongueurMaximum)
                {
                    Rendre(sortie);
                }
            }

            /// <summary>Le texte retenu part tel quel, précédé de ses blancs.</summary>
            private void Rendre(StringBuilder sortie)
            {
                var texte = _retenu.ToString();
                _retenu.Clear();
                _profondeur = 0;

                sortie.Append(BlancsDevant(texte[0]));
                sortie.Append(texte);
                _texteEnvoye = true;

                // Une parenthèse rendue sur un saut de ligne ou une longueur : la suite se lit normalement, et une
                // parenthèse ouvrante qui y figurait déjà n'est plus suivie (elle est partie avec le texte).
            }

            private void Ecrire(char c, StringBuilder sortie)
            {
                sortie.Append(BlancsDevant(c));
                sortie.Append(c);
                _texteEnvoye = true;
            }

            /// <summary>Les blancs à écrire devant le caractère <paramref name="suivant"/>.</summary>
            private string BlancsDevant(char suivant)
            {
                string blancs;

                if (!_apresRetrait)
                {
                    blancs = _blancs;
                }
                else if (!_texteEnvoye)
                {
                    // Un aparté en tête de message : le message commence au vrai texte.
                    blancs = string.Empty;
                }
                else
                {
                    var fusion = Fusion(_blancsAvant, _blancs);
                    blancs = fusion.Contains('\n') ? fusion
                        : fusion.Length == 0 ? string.Empty
                        : suivant is '.' or ',' or ')' or '…' ? string.Empty
                        : " ";
                }

                _blancs = string.Empty;
                _blancsAvant = string.Empty;
                _apresRetrait = false;
                return blancs;
            }

            /// <summary>Le plus large des deux : un paragraphe, une ligne, une espace, ou rien.</summary>
            private static string Fusion(string a, string b)
            {
                static int Poids(string s) => s.Contains("\n\n") ? 3 : s.Contains('\n') ? 2 : s.Length > 0 ? 1 : 0;
                var pa = Poids(a);
                var pb = Poids(b);
                var max = Math.Max(pa, pb);
                return max switch
                {
                    3 => "\n\n",
                    2 => "\n",
                    1 => " ",
                    _ => string.Empty,
                };
            }
        }
    }
}
