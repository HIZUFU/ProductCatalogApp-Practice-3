using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductCatalogApp.Data;
using ProductCatalogApp.Hubs;
using ProductCatalogApp.Models;

namespace ProductCatalogApp.Services
{
    public class SensorSimulationService : BackgroundService
    {
        public const string SourceName = "Имитатор";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly SimulationState _state;
        private readonly MonitoringOptions _options;
        private readonly ILogger<SensorSimulationService> _logger;

        private readonly Dictionary<int, double> _lastValues = new();

        public SensorSimulationService(
            IServiceScopeFactory scopeFactory,
            IHubContext<MonitoringHub> hubContext,
            SimulationState state,
            IOptions<MonitoringOptions> options,
            ILogger<SensorSimulationService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _state = state;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(
                Math.Max(1, _options.SimulationIntervalSeconds));

            using var timer = new PeriodicTimer(interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (!_state.IsEnabled)
                {
                    continue;
                }

                try
                {
                    await GenerateReadingsAsync(stoppingToken);
                }
                catch (Exception exception)
                    when (exception is not OperationCanceledException)
                {
                    _logger.LogError(
                        exception,
                        "Ошибка при формировании показаний датчиков.");
                }
            }
        }

        private async Task GenerateReadingsAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var sensors = await context.Sensors
                .Include(sensor => sensor.Equipment)
                    .ThenInclude(equipment => equipment!.ProductionSite)
                .ToListAsync(cancellationToken);

            if (sensors.Count == 0)
            {
                return;
            }

            var readings = sensors
                .Select(sensor => SensorReadingService.CreateReading(
                    sensor,
                    NextValue(sensor),
                    SourceName))
                .ToList();

            context.SensorReadings.AddRange(readings);
            await context.SaveChangesAsync(cancellationToken);

            foreach (var reading in readings)
            {
                await _hubContext.Clients.SendReadingAsync(
                    SensorReadingDto.FromEntity(reading),
                    cancellationToken);
            }
        }

        private double NextValue(Sensor sensor)
        {
            double range = sensor.MaxValue - sensor.MinValue;
            double nominal = sensor.MinValue + range / 2;

            if (Random.Shared.NextDouble() < _options.AlarmProbability)
            {
                double excess = range * (0.05 + Random.Shared.NextDouble() * 0.2);

                bool canGoBelow = sensor.MinValue < 0 ||
                    sensor.MinValue - excess >= 0;

                bool upward = !canGoBelow || Random.Shared.NextDouble() < 0.5;

                return upward
                    ? sensor.MaxValue + excess
                    : sensor.MinValue - excess;
            }

            double previous = _lastValues.TryGetValue(sensor.Id, out var last)
                ? last
                : nominal;

            double next = previous
                + (nominal - previous) * 0.3
                + (Random.Shared.NextDouble() - 0.5) * range * 0.15;

            next = Math.Clamp(next, sensor.MinValue, sensor.MaxValue);
            _lastValues[sensor.Id] = next;

            return next;
        }
    }
}
