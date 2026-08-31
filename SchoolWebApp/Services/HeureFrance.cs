namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'heure de Paris, pour tout ce qui est mis en forme côté serveur.
    ///
    /// Le navigateur convertit tout seul les dates UTC qu'on lui envoie ; un
    /// e-mail, non. Une évaluation passée à 00 h 30 le 6 juillet est stockée
    /// 22 h 30 le 5 en UTC, et le bilan hebdomadaire l'annoncerait « le 5
    /// juillet » — la veille du jour où l'enfant l'a passée.
    ///
    /// Le service tourne peut-être un jour sur un serveur réglé sur UTC ou
    /// ailleurs : on ne se repose donc pas sur le fuseau de la machine.
    /// </summary>
    public static class HeureFrance
    {
        private static readonly TimeZoneInfo Paris = Trouver();

        /// <summary>Ramène une date UTC à l'heure de Paris.</summary>
        public static DateTime Locale(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(
                utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc),
                Paris);

        /// <inheritdoc cref="Locale(DateTime)"/>
        public static DateTime? Locale(DateTime? utc) =>
            utc.HasValue ? Locale(utc.Value) : null;

        /// <summary>
        /// « Europe/Paris » est l'identifiant IANA, que .NET 8 accepte sur les
        /// deux plateformes. On garde l'ancien nom Windows en secours au cas
        /// où la machine n'aurait pas les données ICU, et l'UTC en dernier
        /// recours : un e-mail décalé d'une heure vaut mieux qu'un service qui
        /// refuse de démarrer.
        /// </summary>
        private static TimeZoneInfo Trouver()
        {
            foreach (var identifiant in new[] { "Europe/Paris", "Romance Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(identifiant);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
