using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string ServerUrl = "https://localhost:7079";
        private HubConnection _hubConnection;
        private readonly HttpClient _httpClient;
        private readonly DispatcherTimer _pollTimer;
        private readonly Stopwatch _latencyWatch = new();
        private int _messageCount;
        private DateTime _startTime;

        private double _cpuUsage;
        public double CpuUsage { get => _cpuUsage; set { _cpuUsage = value; OnPropertyChanged(); } }

        private double _memoryUsage;
        public double MemoryUsage { get => _memoryUsage; set { _memoryUsage = value; OnPropertyChanged(); } }

        private double _availableMemoryGb;
        public double AvailableMemoryGb { get => _availableMemoryGb; set { _availableMemoryGb = value; OnPropertyChanged(); } }

        private int _totalProcesses;
        public int TotalProcesses { get => _totalProcesses; set { _totalProcesses = value; OnPropertyChanged(); } }

        private double _systemUptimeSec;
        public double SystemUptimeSec { get => _systemUptimeSec; set { _systemUptimeSec = value; OnPropertyChanged(); } }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _httpClient = new HttpClient { BaseAddress = new Uri(ServerUrl) };
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _pollTimer.Tick += PollTimer_Tick;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e) => await ConnectWithWebSockets();

        private async void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            var selected = (sender as RadioButton)?.Content.ToString();
            await Disconnect();
            switch (selected)
            {
                case "Web Sockets": await ConnectWithWebSockets(); break;
                case "Long Polling": await ConnectWithLongPolling(); break;
                case "Frequent Polls": StartFrequentPolling(); break;
            }
        }

        private async Task ConnectWithWebSockets()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{ServerUrl}/performanceHub", o => o.Transports = HttpTransportType.WebSockets)
                .Build();
            await StartSignalRConnection();
        }

        private async Task ConnectWithLongPolling()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{ServerUrl}/performanceHub", o => o.Transports = HttpTransportType.LongPolling)
                .Build();
            await StartSignalRConnection();
        }

        private async Task StartSignalRConnection()
        {
            _messageCount = 0;
            _startTime = DateTime.Now;
            _latencyWatch.Restart();

            _hubConnection.On<PerformanceMetrics>("ReceivePerformanceData", m =>
            {
                Dispatcher.Invoke(() =>
                {
                    CpuUsage = m.CpuUsage;
                    MemoryUsage = m.MemoryUsage;
                    AvailableMemoryGb = m.AvailableMemoryGb;
                    TotalProcesses = m.TotalProcesses;
                    SystemUptimeSec = m.SystemUptimeSec;
                    _messageCount++;

                    if (_messageCount % 10 == 0)
                    {
                        var elapsed = (DateTime.Now - _startTime).TotalSeconds;
                        var rate = _messageCount / elapsed;
                        Debug.WriteLine($"[Client] Mode=SignalR | {rate:0.0} msg/s | Latency~{_latencyWatch.ElapsedMilliseconds} ms");
                        _latencyWatch.Restart();
                    }
                });
            });

            try { await _hubConnection.StartAsync(); }
            catch (Exception ex) { MessageBox.Show($"SignalR Error: {ex.Message}"); }
        }

        private async void PollTimer_Tick(object sender, EventArgs e)
        {
            _messageCount++;
            try
            {
                var sw = Stopwatch.StartNew();
                var m = await _httpClient.GetFromJsonAsync<PerformanceMetrics>("/api/performance/metrics");
                sw.Stop();

                if (m != null)
                {
                    CpuUsage = m.CpuUsage;
                    MemoryUsage = m.MemoryUsage;
                    AvailableMemoryGb = m.AvailableMemoryGb;
                    TotalProcesses = m.TotalProcesses;
                    SystemUptimeSec = m.SystemUptimeSec;
                }

                if (_messageCount % 10 == 0)
                {
                    var elapsed = (DateTime.Now - _startTime).TotalSeconds;
                    var rate = _messageCount / elapsed;
                    Debug.WriteLine($"[Client] Mode=Polling | {rate:0.0} msg/s | Avg latency={sw.ElapsedMilliseconds} ms");
                }
            }
            catch (Exception ex)
            {
                _pollTimer.Stop();
                MessageBox.Show($"Polling Error: {ex.Message}");
            }
        }

        private void StartFrequentPolling() => _pollTimer.Start();

        private async Task Disconnect()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            if (_pollTimer.IsEnabled) _pollTimer.Stop();

            CpuUsage = 0;
            MemoryUsage = 0;
            AvailableMemoryGb = 0;
            TotalProcesses = 0;
            SystemUptimeSec = 0;
        }

        private async void MainWindow_Closing(object sender, CancelEventArgs e) => await Disconnect();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PerformanceMetrics
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double AvailableMemoryGb { get; set; }
        public int TotalProcesses { get; set; }
        public double SystemUptimeSec { get; set; }
    }
}