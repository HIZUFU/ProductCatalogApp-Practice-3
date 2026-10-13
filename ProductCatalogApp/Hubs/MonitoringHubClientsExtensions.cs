using Microsoft.AspNetCore.SignalR;
using ProductCatalogApp.Models;

namespace ProductCatalogApp.Hubs
{
    public static class MonitoringHubClientsExtensions
    {
        public static async Task SendReadingAsync(
            this IHubClients<IClientProxy> clients,
            SensorReadingDto reading,
            CancellationToken cancellationToken = default)
        {
            await clients
                .Groups(
                    MonitoringGroups.AllSites,
                    MonitoringGroups.ForSite(reading.ProductionSiteId))
                .SendAsync(
                    MonitoringClientMethods.ReceiveReading,
                    reading,
                    cancellationToken);

            if (reading.IsAlarm)
            {
                await clients.All.SendAsync(
                    MonitoringClientMethods.AlarmRaised,
                    reading,
                    cancellationToken);
            }
        }
    }
}
