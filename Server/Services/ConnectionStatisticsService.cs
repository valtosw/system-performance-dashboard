using System.Collections.Concurrent;

namespace Server.Services
{
    public sealed class ConnectionStatisticsService
    {
        private long _messagesSent;
        private readonly ConcurrentDictionary<string, DateTime> _connectedClients = new();

        public void RegisterConnection(string connectionId) => _connectedClients[connectionId] = DateTime.UtcNow;

        public void UnregisterConnection(string connectionId)
        {
            _connectedClients.TryRemove(connectionId, out _);
        }

        public void IncrementMessageCount() => Interlocked.Increment(ref _messagesSent);

        public long TotalMessages => Interlocked.Read(ref _messagesSent);
        public int ActiveConnections => _connectedClients.Count;

        public IEnumerable<(string Id, TimeSpan Duration)> ConnectionDurations =>
            _connectedClients.Select(c => (c.Key, DateTime.UtcNow - c.Value));
    }
}
