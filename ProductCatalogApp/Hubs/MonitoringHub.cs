using Microsoft.AspNetCore.SignalR;
using ProductCatalogApp.Services;

namespace ProductCatalogApp.Hubs
{
    public class MonitoringHub : Hub
    {
        private readonly OnlineUserTracker _onlineUsers;

        public MonitoringHub(OnlineUserTracker onlineUsers)
        {
            _onlineUsers = onlineUsers;
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
    }
}
