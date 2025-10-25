using Microsoft.AspNetCore.SignalR;
using Server.Services;

namespace Server.Hubs
{
    public sealed class PerformanceHub : Hub
    {
        private readonly ConnectionStatisticsService _stats;

        public PerformanceHub(ConnectionStatisticsService stats)
        {
            _stats = stats;
        }

        public override Task OnConnectedAsync()
        {
            _stats.RegisterConnection(Context.ConnectionId);
            Console.WriteLine($"[Hub] Connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _stats.UnregisterConnection(Context.ConnectionId);
            Console.WriteLine($"[Hub] Disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }
    }
}
