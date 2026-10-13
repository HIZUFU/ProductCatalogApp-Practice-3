using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProductCatalogApp.Services;

namespace ProductCatalogApp.Hubs
{
    public class MonitoringHub : Hub
    {
        private const string CurrentGroupKey = "CurrentGroup";

        private const double MaxAbsoluteReadingValue = 1_000_000;

        private readonly OnlineUserTracker _onlineUsers;
        private readonly SensorReadingService _readingService;
        private readonly SimulationState _simulation;

        public MonitoringHub(
            OnlineUserTracker onlineUsers,
            SensorReadingService readingService,
            SimulationState simulation)
        {
            _onlineUsers = onlineUsers;
            _readingService = readingService;
            _simulation = simulation;
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

            snapshot.SimulationEnabled = _simulation.IsEnabled;

            await Clients.Caller.SendAsync(
                MonitoringClientMethods.ReceiveSnapshot,
                snapshot);
        }

        [Authorize]
        public async Task SubmitReading(int sensorId, double value)
        {
            if (!double.IsFinite(value) ||
                Math.Abs(value) > MaxAbsoluteReadingValue)
            {
                throw new HubException("Некорректное значение показания.");
            }

            string operatorName = Context.User?.Identity?.Name ?? "Оператор";

            var reading = await _readingService.AddReadingAsync(
                sensorId,
                value,
                operatorName);

            if (reading == null)
            {
                throw new HubException("Датчик не найден.");
            }

            await Clients.SendReadingAsync(reading);

            await Clients.Caller.SendAsync(
                MonitoringClientMethods.ReadingAccepted,
                reading);
        }

        [Authorize(Roles = "Admin")]
        public async Task AcknowledgeAlarm(int readingId)
        {
            string adminName = Context.User?.Identity?.Name ?? "Администратор";

            var alarm = await _readingService.AcknowledgeAlarmAsync(
                readingId,
                adminName);

            if (alarm == null)
            {
                throw new HubException("Тревога не найдена.");
            }

            await Clients.All.SendAsync(
                MonitoringClientMethods.AlarmAcknowledged,
                alarm);
        }

        [Authorize(Roles = "Admin")]
        public async Task SetSimulationEnabled(bool enabled)
        {
            _simulation.IsEnabled = enabled;

            await Clients.All.SendAsync(
                MonitoringClientMethods.SimulationStateChanged,
                enabled);
        }
    }
}
