using Microsoft.Extensions.Options;

namespace ProductCatalogApp.Services
{
    public class SimulationState
    {
        private volatile bool _isEnabled;

        public SimulationState(IOptions<MonitoringOptions> options)
        {
            _isEnabled = options.Value.SimulationEnabled;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
    }
}
