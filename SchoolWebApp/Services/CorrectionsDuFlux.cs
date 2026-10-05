namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// CE QUE LE PROFESSEUR ÉCRIT MAL, CORRIGÉ AVANT L'ÉCRAN ET LA VOIX — Camara,
    /// le 04/10/2026 au soir, en cours d'arabe : « Je te la réécoute ? ».
    ///
    /// La tournure était déjà réécrite — voir <see cref="TournuresFautives"/> —
    /// mais seulement dans le texte ENREGISTRÉ et dans l'historique relu, pour
    /// que le professeur ne se recopie pas. L'élève, lui, lit et entend le FLUX :
    /// il voyait toujours la faute, une fois, à chaque séance où le modèle la
    /// refaisait. Les accords du professeur (<see cref="AccordDuProfesseur"/>)
    /// avaient déjà leur filtre de flux ; celui-ci le remplace et porte les deux.
    ///
    /// COMMENT ON CORRIGE UN TEXTE QUI ARRIVE PAR MORCEAUX. Une correction a
    /// besoin de voir ce qui SUIT le mot (« content » + « de te retrouver ») :
    /// les derniers caractères sont donc retenus. Et elle peut changer la
    /// longueur du texte (« réécoute » → « fais réécouter ») : on ne peut pas
    /// recoller par position. La règle est donc celle-ci —
    ///
    ///   on ne laisse partir un début de texte que s'il se corrige DE LA MÊME
    ///   FAÇON avec ou sans ce qui le suit, et si la fin retenue n'a rien à
    ///   corriger.
    ///
    /// Tant que ce n'est pas vrai — une correction est à cheval sur la coupure,
    /// ou dans la fin retenue —, on attend le morceau suivant. Un texte sans
    /// faute, c'est-à-dire presque tous, part avec une trentaine de caractères
    /// de retard et rien d'autre.
    /// </summary>
    public sealed class CorrectionsDuFlux(bool professeure)
    {
        /// <summary>Assez pour voir « de te retrouver » ou « l'histoire » après le mot.</summary>
        private const int Retenue = 32;

        /// <summary>Assez pour « je ne suis vraiment pas » avant lui.</summary>
        private const int Contexte = 40;

        /// <summary>Au-delà, on n'attend plus : mieux vaut un texte qui part.</summary>
        private const int AttenteMaximum = 600;

        private string _parti = string.Empty;
        private string _tampon = string.Empty;

        /// <summary>Dans un bloc ou une citation, au DÉBUT du contexte gardé.</summary>
        private AccordDuProfesseur.Etat _etat;

        public string Passer(string fragment)
        {
            _tampon += fragment;
            if (_tampon.Length <= Retenue) return string.Empty;

            // On coupe sur un blanc : un mot ne part jamais à moitié.
            var coupe = _tampon.LastIndexOfAny([' ', '\n'], _tampon.Length - Retenue);
            if (coupe <= 0) return string.Empty;

            var tete = _tampon[..coupe];
            var queue = _tampon[coupe..];

            var teteCorrigee = Corriger(tete);

            if (Corriger(_tampon) == teteCorrigee + queue)
            {
                _tampon = queue;
                return Partir(teteCorrigee);
            }

            if (_tampon.Length < AttenteMaximum) return string.Empty;

            return Terminer();
        }

        public string Terminer()
        {
            var sortie = Corriger(_tampon);
            _tampon = string.Empty;
            return Partir(sortie);
        }

        /// <summary>
        /// Le texte corrigé, lu avec ce qui est déjà parti pour contexte. Ce
        /// contexte, lui, ne change plus : il est chez l'élève.
        /// </summary>
        private string Corriger(string texte)
        {
            // La tournure d'abord : elle peut changer la longueur, et l'accord
            // se lit ensuite sur le texte tel qu'il partira.
            var entier = TournuresFautives.Corriger(_parti + texte);

            var tournure = entier.StartsWith(_parti, StringComparison.Ordinal)
                ? entier[_parti.Length..]
                : texte;

            return AccordDuProfesseur.Corriger(_parti, tournure, professeure, _etat);
        }

        private string Partir(string sortie)
        {
            _parti += sortie;

            if (_parti.Length > Contexte)
            {
                // Ce qui sort du contexte emporte ses bornes : l'état les retient.
                _etat = AccordDuProfesseur.Avancer(_etat, _parti[..^Contexte]);
                _parti = _parti[^Contexte..];
            }

            return sortie;
        }
    }
}
