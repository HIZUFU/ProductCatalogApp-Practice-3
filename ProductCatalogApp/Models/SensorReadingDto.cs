namespace ProductCatalogApp.Models
{
    public class SensorReadingDto
    {
        public int Id { get; set; }

        public int SensorId { get; set; }

        public string SensorName { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public double MinValue { get; set; }

        public double MaxValue { get; set; }

        public string EquipmentName { get; set; } = string.Empty;

        public int ProductionSiteId { get; set; }

        public string ProductionSiteName { get; set; } = string.Empty;

        public double Value { get; set; }

        public DateTime Timestamp { get; set; }

        public string Source { get; set; } = string.Empty;

        public bool IsAlarm { get; set; }

        public bool IsAcknowledged { get; set; }

        public string? AcknowledgedBy { get; set; }

        public DateTime? AcknowledgedAt { get; set; }

        public static SensorReadingDto FromEntity(SensorReading reading)
        {
            var sensor = reading.Sensor
                ?? throw new InvalidOperationException(
                    "Не загружен датчик показания.");

            var equipment = sensor.Equipment;

            return new SensorReadingDto
            {
                Id = reading.Id,
                SensorId = sensor.Id,
                SensorName = sensor.Name,
                Unit = sensor.Unit,
                MinValue = sensor.MinValue,
                MaxValue = sensor.MaxValue,
                EquipmentName = equipment?.Name ?? string.Empty,
                ProductionSiteId = equipment?.ProductionSiteId ?? 0,
                ProductionSiteName =
                    equipment?.ProductionSite?.Name ?? string.Empty,
                Value = reading.Value,
                Timestamp = AsUtc(reading.Timestamp),
                Source = reading.Source,
                IsAlarm = reading.IsAlarm,
                IsAcknowledged = reading.IsAcknowledged,
                AcknowledgedBy = reading.AcknowledgedBy,
                AcknowledgedAt = reading.AcknowledgedAt.HasValue
                    ? AsUtc(reading.AcknowledgedAt.Value)
                    : null
            };
        }

        private static DateTime AsUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
