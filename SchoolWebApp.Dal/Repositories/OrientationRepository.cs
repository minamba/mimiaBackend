using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// L'offre des lycées et le journal de son import — voir
    /// <see cref="IOrientationRepository"/>.
    /// </summary>
    public class OrientationRepository : IOrientationRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public OrientationRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<OffreLycee?> GetParUaiAsync(string uai, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(uai)) return null;

            var cle = uai.Trim().ToUpperInvariant();
            var entite = await _context.EtablissementsOrientation
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Uai == cle, ct);

            return entite is null ? null : Map(entite);
        }

        /// <summary>
        /// UN CADRE D'ABORD, LA DISTANCE ENSUITE : la base filtre sur un carré de
        /// latitude et de longitude autour du point (index inutile, mais ~5 600
        /// lignes au plus), puis la vraie distance se calcule ici, à vol d'oiseau.
        /// </summary>
        public async Task<IReadOnlyList<LyceeProche>> ProchesAsync(
            double latitude, double longitude, int combien, double rayonKm, CancellationToken ct = default)
        {
            var deltaLat = rayonKm / 111.0;
            var deltaLon = rayonKm / (111.0 * Math.Max(0.1, Math.Cos(latitude * Math.PI / 180)));

            var candidats = await _context.EtablissementsOrientation
                .AsNoTracking()
                .Where(e => e.Latitude != null && e.Longitude != null
                    && e.Latitude >= latitude - deltaLat && e.Latitude <= latitude + deltaLat
                    && e.Longitude >= longitude - deltaLon && e.Longitude <= longitude + deltaLon)
                .ToListAsync(ct);

            return candidats
                .Select(e => new LyceeProche(Map(e), Distance(latitude, longitude, e.Latitude!.Value, e.Longitude!.Value)))
                .Where(p => p.DistanceKm <= rayonKm)
                .OrderBy(p => p.DistanceKm)
                .Take(combien)
                .ToList();
        }

        public Task<int> CompterAsync(CancellationToken ct = default) =>
            _context.EtablissementsOrientation.CountAsync(ct);

        public async Task<int> RemplacerToutAsync(IReadOnlyList<OffreLycee> lycees, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            await _context.EtablissementsOrientation.ExecuteDeleteAsync(ct);

            // Par paquets : un seul SaveChanges de 5 600 lignes tient, mais garde
            // tout le suivi en mémoire ; mille à la fois suffit et reste léger.
            foreach (var paquet in lycees.Chunk(1000))
            {
                _context.EtablissementsOrientation.AddRange(paquet.Select(VersEntite));
                await _context.SaveChangesAsync(ct);
                _context.ChangeTracker.Clear();
            }

            await transaction.CommitAsync(ct);
            return lycees.Count;
        }

        public async Task NoterPassageAsync(PassageImportOrientation passage, CancellationToken ct = default)
        {
            _context.ImportsOrientation.Add(new ImportOrientation
            {
                Date = passage.Date,
                Reussi = passage.Reussi,
                Message = Couper(passage.Message, 1000),
                Lycees = passage.Lycees,
                Sources = Couper(JsonSerializer.Serialize(passage.Sources), 2000),
                DureeSecondes = passage.DureeSecondes,
            });

            await _context.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<PassageImportOrientation>> DerniersPassagesAsync(
            int combien, CancellationToken ct = default)
        {
            var lignes = await _context.ImportsOrientation
                .AsNoTracking()
                .OrderByDescending(i => i.Date)
                .Take(combien)
                .ToListAsync(ct);

            return lignes.Select(i => new PassageImportOrientation
            {
                Date = i.Date,
                Reussi = i.Reussi,
                Message = i.Message,
                Lycees = i.Lycees,
                Sources = LireSources(i.Sources),
                DureeSecondes = i.DureeSecondes,
            }).ToList();
        }

        private static Dictionary<string, string> LireSources(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        private static string? Couper(string? texte, int max) =>
            texte is null || texte.Length <= max ? texte : texte[..max];

        /// <summary>La distance à vol d'oiseau, en kilomètres (formule de haversine).</summary>
        private static double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            const double RayonTerre = 6371.0;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return RayonTerre * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static OffreLycee Map(EtablissementOrientation e) => new()
        {
            Uai = e.Uai,
            Libelle = e.Libelle,
            Statut = e.Statut,
            Commune = e.Commune,
            CodePostal = e.CodePostal,
            Departement = e.Departement,
            Academie = e.Academie,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            SiteWeb = e.SiteWeb,
            VoieGenerale = e.VoieGenerale,
            VoieTechnologique = e.VoieTechnologique,
            VoieProfessionnelle = e.VoieProfessionnelle,
            SectionEuropeenne = e.SectionEuropeenne,
            SectionInternationale = e.SectionInternationale,
            SectionSportive = e.SectionSportive,
            SectionArts = e.SectionArts,
            PostBac = e.PostBac,
            Apprentissage = e.Apprentissage,
            Specialites = e.Specialites,
            Options2de = e.Options2de,
            Formations = e.Formations,
            MisAJourLe = e.MisAJourLe,
        };

        private static EtablissementOrientation VersEntite(OffreLycee o) => new()
        {
            Uai = o.Uai,
            Libelle = Couper(o.Libelle, 250) ?? string.Empty,
            Statut = Couper(o.Statut, 60),
            Commune = Couper(o.Commune, 120),
            CodePostal = Couper(o.CodePostal, 10),
            Departement = Couper(o.Departement, 100),
            Academie = Couper(o.Academie, 100),
            Latitude = o.Latitude,
            Longitude = o.Longitude,
            SiteWeb = Couper(o.SiteWeb, 300),
            VoieGenerale = o.VoieGenerale,
            VoieTechnologique = o.VoieTechnologique,
            VoieProfessionnelle = o.VoieProfessionnelle,
            SectionEuropeenne = o.SectionEuropeenne,
            SectionInternationale = o.SectionInternationale,
            SectionSportive = o.SectionSportive,
            SectionArts = o.SectionArts,
            PostBac = o.PostBac,
            Apprentissage = o.Apprentissage,
            Specialites = o.Specialites,
            Options2de = o.Options2de,
            Formations = o.Formations,
            MisAJourLe = o.MisAJourLe,
        };
    }
}
