using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Marque en UTC toute date relue de la base.
    ///
    /// SQL Server stocke des `datetime2`, qui n'ont pas de fuseau. EF les rend
    /// donc avec un Kind « Unspecified », et System.Text.Json les sérialise
    /// sans suffixe : « 2026-07-30T21:54:08 ». Le navigateur lit ça comme de
    /// l'heure LOCALE — deux heures de retard en France l'été, une l'hiver.
    ///
    /// Avec le Kind posé à Utc, la même date sort en « …T21:54:08Z » et le
    /// navigateur la ramène tout seul à 23 h 54, heure de Paris.
    ///
    /// À l'écriture, une date locale est convertie ; une date déjà UTC ou non
    /// qualifiée passe telle quelle — tout le code applicatif utilise
    /// DateTime.UtcNow.
    /// </summary>
    public class ConvertisseurDateUtc : ValueConverter<DateTime, DateTime>
    {
        public ConvertisseurDateUtc()
            : base(
                v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    /// <inheritdoc cref="ConvertisseurDateUtc"/>
    public class ConvertisseurDateUtcNullable : ValueConverter<DateTime?, DateTime?>
    {
        public ConvertisseurDateUtcNullable()
            : base(
                v => v.HasValue && v.Value.Kind == DateTimeKind.Local
                    ? v.Value.ToUniversalTime()
                    : v,
                v => v.HasValue
                    ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                    : v)
        {
        }
    }
}
