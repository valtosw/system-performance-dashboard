namespace Server.Models
{
    public sealed class PerformanceMetrics
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double AvailableMemoryGb { get; set; }
        public int TotalProcesses { get; set; }
        public double SystemUptimeSec { get; set; }
    }
}
