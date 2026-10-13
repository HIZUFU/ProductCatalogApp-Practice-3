using Microsoft.AspNetCore.SignalR;
using ProductCatalogApp.Services;

namespace ProductCatalogApp.Hubs
{
    public class MonitoringHub : Hub
    {
        private const string CurrentGroupKey = "CurrentGroup";

        private readonly OnlineUserTracker _onlineUsers;
        private readonly SensorReadingService _readingService;

        public MonitoringHub(
            OnlineUserTracker onlineUsers,
            SensorReadingService readingService)
        {
            _onlineUsers = onlineUsers;
            _readingService = readingService;
        }

        public override async Task OnConnectedAsync()
        {
            string? userName = Context.User?.Identity?.IsAuthenticated == true
                ? Context.User.Identity.Name
                : null;

            _onlineUsers.Connect(Context.ConnectionId, userName);

            await Clients.All.SendAsync(
                MonitoringClientMethods.OnlineChanged,
                _onlineUsers.GetSummary());

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _onlineUsers.Disconnect(Context.ConnectionId);

            await Clients.All.SendAsync(
                MonitoringClientMethods.OnlineChanged,
                _onlineUsers.GetSummary());

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinSite(int? productionSiteId)
        {
            if (productionSiteId.HasValue &&
                !await _readingService.ProductionSiteExistsAsync(
                    productionSiteId.Value))
            {
                throw new HubException(
                    "Производственная площадка не найдена.");
            }

            string newGroup = productionSiteId.HasValue
                ? MonitoringGroups.ForSite(productionSiteId.Value)
                : MonitoringGroups.AllSites;

            if (Context.Items.TryGetValue(CurrentGroupKey, out var value) &&
                value is string currentGroup &&
                currentGroup != newGroup)
            {
                await Groups.RemoveFromGroupAsync(
                    Context.ConnectionId,
                    currentGroup);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, newGroup);
            Context.Items[CurrentGroupKey] = newGroup;

            var snapshot = await _readingService.GetSnapshotAsync(
                productionSiteId);

            await Clients.Caller.SendAsync(
                MonitoringClientMethods.ReceiveSnapshot,
                snapshot);
        }
    }
}
