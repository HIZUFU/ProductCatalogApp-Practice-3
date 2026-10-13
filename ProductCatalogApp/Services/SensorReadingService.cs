using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Data;
using ProductCatalogApp.Models;

namespace ProductCatalogApp.Services
{
    public class SensorReadingService
    {
        public const int RecentReadingsCount = 50;

        public const int ActiveAlarmsCount = 50;

        private readonly ApplicationDbContext _context;

        public SensorReadingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> ProductionSiteExistsAsync(int productionSiteId)
        {
            return _context.ProductionSites
                .AnyAsync(site => site.Id == productionSiteId);
        }

        public async Task<MonitoringSnapshot> GetSnapshotAsync(
            int? productionSiteId)
        {
            var sensorsQuery = _context.Sensors
                .AsNoTracking()
                .Include(sensor => sensor.Equipment)
                    .ThenInclude(equipment => equipment!.ProductionSite)
                .AsQueryable();

            var readingsQuery = ReadingsWithDetails();

            if (productionSiteId.HasValue)
            {
                sensorsQuery = sensorsQuery.Where(sensor =>
                    sensor.Equipment!.ProductionSiteId ==
                    productionSiteId.Value);

                readingsQuery = readingsQuery.Where(reading =>
                    reading.Sensor!.Equipment!.ProductionSiteId ==
                    productionSiteId.Value);
            }

            var sensors = await sensorsQuery
                .OrderBy(sensor => sensor.Equipment!.Name)
                .ThenBy(sensor => sensor.Name)
                .ToListAsync();

            var lastReadings = await readingsQuery
                .Where(reading => reading.Id == _context.SensorReadings
                    .Where(other => other.SensorId == reading.SensorId)
                    .OrderByDescending(other => other.Timestamp)
                    .ThenByDescending(other => other.Id)
                    .Select(other => other.Id)
                    .FirstOrDefault())
                .ToDictionaryAsync(
                    reading => reading.SensorId,
                    SensorReadingDto.FromEntity);

            var recentReadings = await readingsQuery
                .OrderByDescending(reading => reading.Id)
                .Take(RecentReadingsCount)
                .ToListAsync();

            var activeAlarms = await ReadingsWithDetails()
                .Where(reading => reading.IsAlarm && !reading.IsAcknowledged)
                .OrderByDescending(reading => reading.Id)
                .Take(ActiveAlarmsCount)
                .ToListAsync();

            return new MonitoringSnapshot
            {
                ProductionSiteId = productionSiteId,
                Sensors = sensors
                    .Select(sensor => SensorStateDto.FromEntity(
                        sensor,
                        lastReadings.GetValueOrDefault(sensor.Id)))
                    .ToList(),
                RecentReadings = recentReadings
                    .Select(SensorReadingDto.FromEntity)
                    .ToList(),
                ActiveAlarms = activeAlarms
                    .Select(SensorReadingDto.FromEntity)
                    .ToList()
            };
        }

        private IQueryable<SensorReading> ReadingsWithDetails()
        {
            return _context.SensorReadings
                .AsNoTracking()
                .Include(reading => reading.Sensor)
                    .ThenInclude(sensor => sensor!.Equipment)
                        .ThenInclude(equipment => equipment!.ProductionSite);
        }
    }
}
