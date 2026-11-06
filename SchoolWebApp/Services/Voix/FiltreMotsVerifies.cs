using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// IL PIOCHE DANS LA LISTE BLANCHE — Camara, le 05/11/2026 : « le professeur doit avoir en tête
    /// uniquement les mots de la liste », et « cette limitation est uniquement pour les syllabes ».
    /// Une consigne ne suffisait pas : en plein exercice, Adrien redemandait foin, poing, nain.
    ///
    /// Dans un exercice de sons ou de syllabes, un mot demandé hors de la liste est remplacé par un
    /// mot de la liste, du même son si possible, avant l'écran, la voix et l'historique.
    ///
    /// JAMAIS HORS DE CES EXERCICES, ET JAMAIS QUAND LE MOT PORTE UN SENS (« trois se dit three ») :
    /// remplacer enseignerait une fausse traduction. Le mot reste alors celui du professeur.
    ///
    /// IL FAIT AUSSI LE RESTE DE CES EXERCICES : la réponse cachée quand l'enfant écrit, la consigne et l'ordre d'un tour
    /// d'écoute, le bloc d'archive parasite. Pour cela, la réponse attend sa fin : voir Passer.
    /// </summary>
    public sealed class FiltreMotsVerifies
    {




        private readonly string _langue;
        private readonly IReadOnlyCollection<string> _fiables;
        private readonly IReadOnlyList<string> _recents;
        private readonly int _graine;

        private string _parti = string.Empty;
        private string _tampon = string.Empty;
        private string? _ancien;
        private string? _nouveau;
        private bool _cacher;
        private bool _balisePrise;
        private bool _sansVoix;
        private string? _ecoute;
        private string? _dernierEleve;
        private string _balise = string.Empty;
        private readonly bool _supportConnu;
        private readonly bool _photoAttendue;
        private readonly EtatDuMot? _etat;
        private readonly bool _retenirTout;

        /// <summary>La classe de l'enfant (CP ... CM2) : les mots dictés viennent de sa classe et des classes en dessous.</summary>
        private readonly string? _niveau;

        /// <summary>Le message de l'enfant de ce tour, absent de l'historique : il peut changer d'exercice.</summary>
        private readonly string? _messageDeLEleve;

        /// <param name="supportConnu">Le choix cahier ou clavier a déjà été posé : la carte ne s'ajoute pas.</param>
        /// <param name="photoAttendue">Il écrit sur son cahier et la photo n'est pas là : rien ne s'affiche, quoi que dise le professeur.</param>
        public FiltreMotsVerifies(string langue, IReadOnlyCollection<string> fiables, IReadOnlyList<string> messagesRecents, int graine, bool supportConnu = true, bool photoAttendue = false, EtatDuMot? etat = null, bool retenirTout = false, string? niveau = null, string? messageDeLEleve = null)
        {
            _niveau = niveau;
            _messageDeLEleve = messageDeLEleve;
            // L'ENFANT ÉCRIT CE QU'IL ENTEND : RIEN NE PART AVANT LA FIN. Le texte d'avant le premier repère pouvait
            // déjà porter le mot, et le mot n'est connu qu'en bout de message. La voix commence un peu plus tard.
            _retenirTout = retenirTout;
            _etat = etat;
            _supportConnu = supportConnu;
            _photoAttendue = photoAttendue;
            _langue = langue;
            _fiables = fiables;
            _recents = messagesRecents;
            _graine = graine;
        }

        /// <summary>Vrai si un mot a été remplacé dans cette réponse.</summary>
        public bool ARemplace => _ancien is not null;

        /// <summary>Vrai si l'enfant doit écrire ce qu'il entend : voir CacherLaReponse.</summary>
        public bool ACache => _cacher;

        /// <summary>
        /// TOUTE LA RÉPONSE ATTEND SA FIN : remettre un tour d'écoute en ordre, ou cacher le mot dicté, demande de la voir
        /// entière. Ce filtre n'existe que dans un exercice de sons : ailleurs, le flux part comme avant.
        /// </summary>
        public string Passer(string fragment)
        {
            _tampon += fragment;
            return string.Empty;
        }

        /// <summary>La réponse est finie : la demande est lisible en entier, on tranche.</summary>
        public string Terminer()
        {
            // LE MOT EN COURS COMMANDE : voir DicteeDeMots. Ni liste, ni choix, ni cache : ce tour a déjà sa règle.
            if (_retenirTout || _etat is not null)
            {
                var tenue = EnEcriture(_tampon);
                _parti += tenue;
                _tampon = string.Empty;
                return tenue;
            }

            var complet = _parti + _tampon;
            if (RegleVerificationSon.EstEcouteDeMot(complet, RegleVerificationSon.ContexteDEcoute(_recents)))
            {
                _ecoute = complet;
                var ecoute = RegleVerificationSon.EcouteDesMots(RegleVerificationSon.RetirerEcouteParasite(_tampon), _recents, complet, _langue, _niveau, _messageDeLEleve);
                _parti += ecoute;
                _tampon = string.Empty;
                return ecoute;
            }

            var entier = _parti + _tampon;
            _dernierEleve = _recents.Count > 0 ? _recents[^1] : null;

            var corrigee = RegleVerificationSon.BonneBaliseDeSupport(entier, _tampon, _dernierEleve);
            if (corrigee != _tampon)
            {
                _balisePrise = true;
                _tampon = corrigee;
                entier = _parti + _tampon;
            }

            var ecrire = RegleVerificationSon.DemandeDEcrireCeQuIlEntend(entier);

            if (ecrire || _photoAttendue)
            {
                _cacher = true;

                // IL A DEMANDÉ D'ÉCRIRE SANS POSER LA QUESTION : le code la pose, et rien ne se dit avant le
                // choix. Une règle ne vaut pas un fait.
                if (ecrire && !_supportConnu)
                {
                    _sansVoix = true;
                    _tampon = RegleVerificationSon.SansVoix(RegleVerificationSon.CacherLaReponse(_tampon, _langue));
                    if (!entier.Contains(RegleVerificationSon.BaliseSupportSon, StringComparison.OrdinalIgnoreCase))
                    {
                        _balise = "\n" + RegleVerificationSon.BaliseSupportSon;
                    }
                }

                var cachee = RegleVerificationSon.CacherLaReponse(_tampon, _langue) + _balise;
                _parti += cachee;
                _tampon = string.Empty;
                return cachee;
            }

            var cible = RegleVerificationSon.MotAHorsListe(entier, _langue, _fiables);

            if (cible is not null && RegleVerificationSon.Contient(_tampon, cible))
            {
                _ancien = cible;
                _nouveau = RegleVerificationSon.MotPermis(cible, _fiables, _recents, _graine, _langue);
                if (_nouveau is not null) _tampon = RegleVerificationSon.Remplacer(_tampon, _ancien, _nouveau);
                else _ancien = null;
            }

            var fin = _tampon;
            _parti += fin;
            _tampon = string.Empty;
            return fin;
        }

        /// <summary>Le texte enregistré dit la même chose que l'écran : l'historique ne reprendra pas ce mot.</summary>
        public string Corriger(string texte)
        {
            var propre = RegleVerificationSon.RetirerEcouteParasite(texte);
            if (_retenirTout || _etat is not null) return EnEcriture(propre);
            if (_ecoute is not null) return RegleVerificationSon.EcouteDesMots(propre, _recents, _ecoute, _langue, _niveau, _messageDeLEleve);
            if (_balisePrise) propre = RegleVerificationSon.BonneBaliseDeSupport(propre, propre, _dernierEleve);
            if (_sansVoix) propre = RegleVerificationSon.SansVoix(RegleVerificationSon.CacherLaReponse(propre, _langue));
            return CorrigerSuite(propre);
        }

        private string CorrigerSuite(string texte) =>
            _cacher ? RegleVerificationSon.CacherLaReponse(texte, _langue) + _balise
            : _ancien is null || _nouveau is null ? texte
            : RegleVerificationSon.Remplacer(texte, _ancien, _nouveau);

        /// <summary>Après le troisième essai, le mot est au tableau : c'est la seule exception.</summary>
        private string EnEcriture(string texte)
        {
            var propre = RegleVerificationSon.RetirerEcouteParasite(texte);

            if (_etat is null) return DicteeDeMots.MotALaFin(DicteeDeMots.MasquerMotDicte(RegleVerificationSon.CacherLaReponse(propre, _langue, enEcriture: true)));

            var tenue = DicteeDeMots.Imposer(propre, _etat, _langue);
            return _etat.Phase == PhaseDuMot.AuTableau ? tenue : DicteeDeMots.MotALaFin(DicteeDeMots.MasquerMotDicte(tenue));
        }

    }
}
