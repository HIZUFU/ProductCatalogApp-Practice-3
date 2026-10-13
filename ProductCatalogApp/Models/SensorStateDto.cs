namespace ProductCatalogApp.Models
{
    public class SensorStateDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public double MinValue { get; set; }

        public double MaxValue { get; set; }

        public string EquipmentName { get; set; } = string.Empty;

        public int ProductionSiteId { get; set; }

        public string ProductionSiteName { get; set; } = string.Empty;

        public SensorReadingDto? LastReading { get; set; }

        public static SensorStateDto FromEntity(
            Sensor sensor,
            SensorReadingDto? lastReading)
        {
            return new SensorStateDto
            {
                Id = sensor.Id,
                Name = sensor.Name,
                Unit = sensor.Unit,
                MinValue = sensor.MinValue,
                MaxValue = sensor.MaxValue,
                EquipmentName = sensor.Equipment?.Name ?? string.Empty,
                ProductionSiteId = sensor.Equipment?.ProductionSiteId ?? 0,
                ProductionSiteName =
                    sensor.Equipment?.ProductionSite?.Name ?? string.Empty,
                LastReading = lastReading
            };
        }
    }
}
