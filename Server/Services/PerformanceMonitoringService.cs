using Microsoft.AspNetCore.SignalR;
using Server.Hubs;
using Server.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Server.Services
{
    public sealed class PerformanceMonitoringService : BackgroundService
    {
        private readonly IHubContext<PerformanceHub> _hubContext;
        private readonly ConnectionStatisticsService _stats;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _availableMemoryCounter;
        private readonly double _totalMemoryMb;
        private readonly Stopwatch _uptimeWatch = Stopwatch.StartNew();
        private DateTime _lastLogTime = DateTime.UtcNow;

        public PerformanceMonitoringService(IHubContext<PerformanceHub> hubContext, ConnectionStatisticsService stats)
        {
            _hubContext = hubContext;
            _stats = stats;
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            _totalMemoryMb = GetTotalMemoryMb();
        }

        public static PerformanceMetrics LatestMetrics { get; private set; } = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cpuCounter.NextValue();
            _availableMemoryCounter.NextValue();
            await Task.Delay(1000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var cpu = _cpuCounter.NextValue();
                var availableMb = _availableMemoryCounter.NextValue();
                var usedPercent = 100.0 * (1.0 - (availableMb / _totalMemoryMb));

                var metrics = new PerformanceMetrics
                {
                    CpuUsage = Math.Round(cpu, 1),
                    MemoryUsage = Math.Round(usedPercent, 1),
                    AvailableMemoryGb = Math.Round(availableMb / 1024.0, 2),
                    TotalProcesses = Process.GetProcesses().Length,
                    SystemUptimeSec = Math.Round(_uptimeWatch.Elapsed.TotalSeconds, 1)
                };

                LatestMetrics = metrics;
                await _hubContext.Clients.All.SendAsync("ReceivePerformanceData", metrics, stoppingToken);
                _stats.IncrementMessageCount();

                if ((DateTime.UtcNow - _lastLogTime).TotalSeconds >= 10)
                {
                    _lastLogTime = DateTime.UtcNow;
                    LogStatistics();
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private void LogStatistics()
        {
            Console.WriteLine($"[Server Stats] {DateTime.Now:T}");
            Console.WriteLine($"Active connections: {_stats.ActiveConnections}");
            Console.WriteLine($"Messages sent: {_stats.TotalMessages}");

            foreach (var (id, duration) in _stats.ConnectionDurations)
                Console.WriteLine($"  {id}: connected for {duration.TotalSeconds:0}s");
        }

        private static double GetTotalMemoryMb()
        {
            var mem = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(mem))
                return mem.ullTotalPhys / (1024.0 * 1024.0);

            return 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() => dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    }
}
