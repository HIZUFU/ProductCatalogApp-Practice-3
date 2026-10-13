namespace ProductCatalogApp.Models
{
    public class MonitoringSnapshot
    {
        public int? ProductionSiteId { get; set; }

        public List<SensorStateDto> Sensors { get; set; } = new();

        public List<SensorReadingDto> RecentReadings { get; set; } = new();

        public List<SensorReadingDto> ActiveAlarms { get; set; } = new();
    }
}
