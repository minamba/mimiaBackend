namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>Une jauge de quota, telle que la carte « Google Gemini » la dessine.</summary>
    public sealed record JaugeQuota(string Cle, string Libelle, long Utilise, long Limite, DateTime? RemiseLe);

    /// <summary>
    /// CE QUE NOUS AVONS DEMANDÉ À GOOGLE, COMPTÉ PAR NOUS — Camara, le 28/09/2026 :
    /// « y a pas moyen d'avoir l'info côté Gemini pour voir où j'en suis dans ma
    /// limitation ? Une barre de progression de 0 à 100 % ».
    ///
    /// GOOGLE NE LE DIT PAS À UNE CLÉ. Sa page « Rate limit » ne se lit qu'avec
    /// un compte connecté ; l'API ne renvoie ni compteur ni en-tête de quota. On
    /// compte donc nous-mêmes, au plus près de ce que Google compte :
    ///
    ///   - chaque appel de synthèse (<see cref="SyntheseGemini"/>) est une requête,
    ///     pour la minute et pour le jour ;
    ///   - ses jetons d'ENTRÉE (`usageMetadata.promptTokenCount`) pour la minute —
    ///     c'est eux que la limite « TPM » de Google mesure ;
    ///   - chaque refus 429 à part : il dit qu'on a touché le plafond.
    ///
    /// LE JOUR DE GOOGLE COMMENCE À MINUIT EN CALIFORNIE (9 h à Paris) : c'est à
    /// cette heure-là que son compteur quotidien retombe, et le nôtre avec.
    ///
    /// EN MÉMOIRE, SEULEMENT : un redémarrage de l'API remet tout à zéro, et un
    /// autre serveur branché sur le même projet (le poste de développement)
    /// n'est pas vu. La carte le dit ; la page d'AI Studio fait foi.
    /// </summary>
    public sealed class CompteurGemini
    {
        private static readonly TimeZoneInfo Californie = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

        private readonly object _verrou = new();
        private readonly Queue<(DateTime Le, int Jetons)> _derniereMinute = new();

        private DateTime _jourGoogle = DateTime.MinValue;
        private int _requetesDuJour;
        private int _refusDuJour;

        // CE QUE GOOGLE A DIT EN REFUSANT — voir `Refuser`. Le jour plein tient
        // jusqu'à la remise à zéro ; la minute pleine, une minute.
        private bool _jourPlein;
        private DateTime _minutePleineJusqua = DateTime.MinValue;
        private DateTime _jetonsPleinsJusqua = DateTime.MinValue;

        /// <summary>Un appel abouti (ou refusé autrement que par le quota).</summary>
        public void Enregistrer(int jetonsEntree, DateTime maintenantUtc)
        {
            lock (_verrou)
            {
                ChangerDeJourSiBesoin(maintenantUtc);
                _requetesDuJour++;
                _derniereMinute.Enqueue((maintenantUtc, Math.Max(0, jetonsEntree)));
                Purger(maintenantUtc);
            }
        }

        /// <summary>
        /// Un refus 429 : un plafond est touché — et Google dit LEQUEL.
        ///
        /// LA BARRE DOIT LE MONTRER, MÊME QUAND NOUS N'AVONS RIEN COMPTÉ — Camara,
        /// le 28/09/2026 : « 4 refus, mais pas de barre pour voir où en est mon
        /// quota ». Le serveur venait de redémarrer : notre compteur disait 0 / 100
        /// pendant que Google en était à 103. Le message du refus nomme la limite
        /// touchée (`generate_requests_per_model_per_day`, `…per_minute`,
        /// `input_token…`) : c'est un FAIT, et il l'emporte sur notre compte.
        /// </summary>
        public void Refuser(DateTime maintenantUtc, string? detail = null)
        {
            var vientDeSeRemplir = false;

            lock (_verrou)
            {
                ChangerDeJourSiBesoin(maintenantUtc);
                _refusDuJour++;

                var texte = (detail ?? string.Empty).ToLowerInvariant();

                if (texte.Contains("per_day") || texte.Contains("perday"))
                {
                    vientDeSeRemplir = !_jourPlein;
                    _jourPlein = true;
                }
                else if (texte.Contains("token"))
                {
                    _jetonsPleinsJusqua = maintenantUtc.AddMinutes(1);
                }
                else
                {
                    // Sans précision, c'est le freinage à la minute : le plus courant.
                    _minutePleineJusqua = maintenantUtc.AddMinutes(1);
                }
            }

            // HORS DU VERROU : l'abonné relance une vérification, qui relit ce compteur.
            if (vientDeSeRemplir) JourPlein?.Invoke();
        }

        /// <summary>
        /// Le quota du jour vient d'être atteint — une seule fois par jour. Écouté
        /// par <c>AlerteQuotaGemini</c>, qui relance la surveillance tout de suite
        /// pour que la carte passe à l'orange et que Telegram le dise, sans
        /// attendre le prochain passage (jusqu'à trente minutes).
        ///
        /// LA MINUTE NE PRÉVIENT PAS : au Niveau 1, on la touche toutes les dix
        /// phrases ; une alerte à chaque fois serait du bruit. Sa barre suffit.
        /// </summary>
        public event Action? JourPlein;

        /// <summary>Le quota du jour est-il atteint ? Lu par la sonde de surveillance.</summary>
        public bool JourEstPlein(DateTime maintenantUtc)
        {
            lock (_verrou)
            {
                ChangerDeJourSiBesoin(maintenantUtc);
                return _jourPlein;
            }
        }

        /// <summary>
        /// LE DISJONCTEUR : Google refusera-t-il à coup sûr en ce moment ? Vrai après
        /// un refus, jusqu'à la remise à zéro de la limite touchée. Pendant ce
        /// temps, la voix ne l'appelle plus — un aller-retour voué au refus ajoutait
        /// du délai à chaque phrase, et faisait alterner les deux voix au milieu
        /// d'un cours dès qu'une place se libérait.
        /// </summary>
        public bool Bloque(DateTime maintenantUtc)
        {
            lock (_verrou)
            {
                ChangerDeJourSiBesoin(maintenantUtc);
                return _jourPlein || _minutePleineJusqua > maintenantUtc || _jetonsPleinsJusqua > maintenantUtc;
            }
        }

        /// <summary>Jusqu'à quand le disjoncteur tient, ou null s'il est fermé.</summary>
        public DateTime? BloqueJusqua(DateTime maintenantUtc)
        {
            lock (_verrou)
            {
                ChangerDeJourSiBesoin(maintenantUtc);
                if (_jourPlein) return ProchaineRemise(maintenantUtc);

                var fin = new[] { _minutePleineJusqua, _jetonsPleinsJusqua }.Max();
                return fin > maintenantUtc ? fin : null;
            }
        }

        public int RefusDuJour(DateTime maintenantUtc)
        {
            lock (_verrou)
            {
                ChangerDeJourSiBesoin(maintenantUtc);
                return _refusDuJour;
            }
        }

        /// <summary>Les trois jauges de la carte, face aux limites configurées.</summary>
        public IReadOnlyList<JaugeQuota> Jauges(OptionsVoix options, DateTime maintenantUtc)
        {
            lock (_verrou)
            {
                ChangerDeJourSiBesoin(maintenantUtc);
                Purger(maintenantUtc);

                // Un plafond que Google a dit touché s'affiche PLEIN, quel que soit
                // notre compte — voir `Refuser`.
                long Plein(long compte, long limite, bool touche) => touche ? Math.Max(compte, limite) : compte;

                return
                [
                    new JaugeQuota("jour", "Requêtes aujourd'hui",
                        Plein(_requetesDuJour, options.GeminiLimiteRequetesJour, _jourPlein),
                        options.GeminiLimiteRequetesJour, ProchaineRemise(maintenantUtc)),
                    new JaugeQuota("minute", "Requêtes cette minute",
                        Plein(_derniereMinute.Count, options.GeminiLimiteRequetesMinute, _minutePleineJusqua > maintenantUtc),
                        options.GeminiLimiteRequetesMinute, null),
                    new JaugeQuota("jetons", "Jetons d'entrée cette minute",
                        Plein(_derniereMinute.Sum(e => (long)e.Jetons), options.GeminiLimiteJetonsMinute, _jetonsPleinsJusqua > maintenantUtc),
                        options.GeminiLimiteJetonsMinute, null),
                ];
            }
        }

        private void ChangerDeJourSiBesoin(DateTime maintenantUtc)
        {
            var jour = TimeZoneInfo.ConvertTimeFromUtc(maintenantUtc, Californie).Date;
            if (jour == _jourGoogle) return;

            _jourGoogle = jour;
            _requetesDuJour = 0;
            _refusDuJour = 0;
            _jourPlein = false;
        }

        private void Purger(DateTime maintenantUtc)
        {
            while (_derniereMinute.Count > 0 && maintenantUtc - _derniereMinute.Peek().Le >= TimeSpan.FromMinutes(1))
            {
                _derniereMinute.Dequeue();
            }
        }

        private static DateTime ProchaineRemise(DateTime maintenantUtc)
        {
            var demain = TimeZoneInfo.ConvertTimeFromUtc(maintenantUtc, Californie).Date.AddDays(1);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(demain, DateTimeKind.Unspecified), Californie);
        }
    }
}
