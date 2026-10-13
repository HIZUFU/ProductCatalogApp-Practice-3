namespace ProductCatalogApp.Services
{
    public class MonitoringOptions
    {
        public const string SectionName = "Monitoring";

        public bool SimulationEnabled { get; set; } = true;

        public int SimulationIntervalSeconds { get; set; } = 5;

        public double AlarmProbability { get; set; } = 0.01;
    }
}
