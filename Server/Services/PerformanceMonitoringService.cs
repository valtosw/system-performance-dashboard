using Microsoft.AspNetCore.SignalR;
using Server.Hubs;
using Server.Models;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Server.Services
{
    [SupportedOSPlatform("windows")]
    public sealed class PerformanceMonitoringService : BackgroundService
    {
        private readonly IHubContext<PerformanceHub> _hubContext;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;

        public PerformanceMonitoringService(IHubContext<PerformanceHub> hubContext)
        {
            _hubContext = hubContext;
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        public static PerformanceMetrics LatestMetrics { get; private set; } = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cpuCounter.NextValue();
            _memoryCounter.NextValue();
            await Task.Delay(1000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var metrics = new PerformanceMetrics
                {
                    CpuUsage = _cpuCounter.NextValue(),
                    MemoryUsage = _memoryCounter.NextValue()
                };

                Console.WriteLine($"SERVER DATA: CPU: {metrics.CpuUsage:F2}%, Memory: {metrics.MemoryUsage:F2}%");

                LatestMetrics = metrics;

                await _hubContext.Clients.All.SendAsync("ReceivePerformanceData", metrics, stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
