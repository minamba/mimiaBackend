using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.Notifications
{
    /// <summary>La règle d'un courriel automatique : quand il part.</summary>
    public record RegleEnvoi(string Frequence, TimeOnly? Heure, int? JourSemaine, int? JourMois)
    {
        public static RegleEnvoi De(ModeleMailResume m) =>
            new(m.Frequence, m.HeureEnvoi, m.JourSemaine, m.JourMois);
    }

    /// <summary>
    /// Les dates d'envoi d'un courriel programmé.
    ///
    /// DES FONCTIONS PURES, SANS HORLOGE CACHÉE : « maintenant » est toujours
    /// un paramètre. C'est ce qui permet de raisonner sur un changement d'heure
    /// ou sur un 31 février sans attendre qu'ils arrivent.
    ///
    /// TOUT SE CALCULE À L'HORLOGE DE PARIS, puis se convertit en UTC.
    /// « Chaque lundi à 9 h » veut dire 9 h pour les parents, été comme hiver ;
    /// calculé en UTC, l'envoi glisserait d'une heure deux fois par an.
    /// </summary>
    public static class Planification
    {
        /// <summary>
        /// Au-delà de ce retard, une occurrence manquée (serveur arrêté) n'est
        /// plus envoyée — Camara, le 15/09/2026. « Votre essai se termine
        /// demain » reçu le lendemain serait faux.
        /// </summary>
        public static readonly TimeSpan RetardMax = TimeSpan.FromHours(6);

        /// <summary>La règle dit-elle tout ce qu'il faut pour tomber à une date ?</summary>
        public static bool EstComplete(RegleEnvoi regle) =>
            regle.Heure is not null && regle.Frequence switch
            {
                FrequenceEnvoi.Jour => true,
                FrequenceEnvoi.Semaine => regle.JourSemaine is >= 1 and <= 7,
                FrequenceEnvoi.Mois => regle.JourMois is >= 1 and <= 31,
                _ => false,
            };

        /// <summary>La dernière occurrence prévue, à cet instant compris. En UTC.</summary>
        public static DateTime? OccurrencePrecedente(DateTime maintenantUtc, RegleEnvoi regle)
        {
            if (!EstComplete(regle)) return null;

            var local = HeureFrance.Locale(maintenantUtc);
            var heure = regle.Heure!.Value.ToTimeSpan();

            DateTime candidat;

            switch (regle.Frequence)
            {
                case FrequenceEnvoi.Jour:
                    candidat = local.Date + heure;
                    if (candidat > local) candidat = candidat.AddDays(-1);
                    break;

                case FrequenceEnvoi.Semaine:
                    candidat = local.Date.AddDays(-Recul(local.DayOfWeek, regle.JourSemaine!.Value)) + heure;
                    if (candidat > local) candidat = candidat.AddDays(-7);
                    break;

                default:
                    candidat = JourDuMois(local.Year, local.Month, regle.JourMois!.Value) + heure;
                    if (candidat > local)
                    {
                        var avant = local.AddMonths(-1);
                        candidat = JourDuMois(avant.Year, avant.Month, regle.JourMois!.Value) + heure;
                    }
                    break;
            }

            return HeureFrance.VersUtc(candidat);
        }

        /// <summary>La prochaine occurrence prévue, strictement après cet instant. En UTC.</summary>
        public static DateTime? OccurrenceSuivante(DateTime maintenantUtc, RegleEnvoi regle)
        {
            if (!EstComplete(regle)) return null;

            var local = HeureFrance.Locale(maintenantUtc);
            var heure = regle.Heure!.Value.ToTimeSpan();

            DateTime candidat;

            switch (regle.Frequence)
            {
                case FrequenceEnvoi.Jour:
                    candidat = local.Date + heure;
                    if (candidat <= local) candidat = candidat.AddDays(1);
                    break;

                case FrequenceEnvoi.Semaine:
                    candidat = local.Date.AddDays((7 - Recul(local.DayOfWeek, regle.JourSemaine!.Value)) % 7) + heure;
                    if (candidat <= local) candidat = candidat.AddDays(7);
                    break;

                default:
                    candidat = JourDuMois(local.Year, local.Month, regle.JourMois!.Value) + heure;
                    if (candidat <= local)
                    {
                        var apres = local.AddMonths(1);
                        candidat = JourDuMois(apres.Year, apres.Month, regle.JourMois!.Value) + heure;
                    }
                    break;
            }

            return HeureFrance.VersUtc(candidat);
        }

        /// <summary>
        /// Ce que l'écran annonce comme « Prochain envoi ». Nul si le courriel
        /// n'est pas programmé.
        ///
        /// Une occurrence passée mais pas encore prise en charge — le
        /// planificateur passe toutes les cinq minutes — est la prochaine :
        /// elle part dans l'instant.
        /// </summary>
        public static DateTime? ProchaineExecution(
            RegleEnvoi regle, bool actif, DateTime? derniereOccurrence, DateTime maintenantUtc)
        {
            if (!actif) return null;

            var precedente = OccurrencePrecedente(maintenantUtc, regle);
            if (precedente is null) return null;

            var enAttente = (derniereOccurrence is null || derniereOccurrence < precedente)
                            && maintenantUtc - precedente <= RetardMax;

            return enAttente ? precedente : OccurrenceSuivante(maintenantUtc, regle);
        }

        /// <summary>Combien de jours séparent aujourd'hui du dernier jour voulu (0 à 6).</summary>
        private static int Recul(DayOfWeek aujourdhui, int jourIso)
        {
            // DayOfWeek compte dimanche = 0 ; la règle compte lundi = 1 … dimanche = 7.
            var iso = aujourdhui == DayOfWeek.Sunday ? 7 : (int)aujourdhui;
            return (iso - jourIso + 7) % 7;
        }

        /// <summary>« Le 31 » d'un mois de 30 jours tombe le 30 : jamais un mois sauté.</summary>
        private static DateTime JourDuMois(int annee, int mois, int jour) =>
            new(annee, mois, Math.Min(jour, DateTime.DaysInMonth(annee, mois)));
    }
}
