using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
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

        private double _cpuUsage;
        public double CpuUsage
        {
            get => _cpuUsage;
            set { _cpuUsage = value; OnPropertyChanged(); }
        }

        private double _memoryUsage;
        public double MemoryUsage
        {
            get => _memoryUsage;
            set { _memoryUsage = value; OnPropertyChanged(); }
        }

        public double TotalMemoryGb { get; } = 8.0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _httpClient = new HttpClient { BaseAddress = new Uri(ServerUrl) };
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _pollTimer.Tick += PollTimer_Tick;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ConnectWithWebSockets();
        }

        private async void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedMethod = (sender as RadioButton)?.Content.ToString();
            await Disconnect();

            switch (selectedMethod)
            {
                case "Web Sockets":
                    await ConnectWithWebSockets();
                    break;
                case "Long Polling":
                    await ConnectWithLongPolling();
                    break;
                case "Frequent Polls":
                    StartFrequentPolling();
                    break;
            }
        }

        private async Task ConnectWithWebSockets()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{ServerUrl}/performanceHub", options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                })
                .Build();
            await StartSignalRConnection();
        }

        private async Task ConnectWithLongPolling()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{ServerUrl}/performanceHub", options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                })
                .Build();
            await StartSignalRConnection();
        }

        private async Task StartSignalRConnection()
        {
            _hubConnection.On<PerformanceMetrics>("ReceivePerformanceData", (metrics) =>
            {
                Dispatcher.Invoke(() =>
                {
                    CpuUsage = metrics.CpuUsage;
                    MemoryUsage = metrics.MemoryUsage;
                });
            });

            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SignalR Connection Error: {ex.Message}");
            }
        }

        private void StartFrequentPolling()
        {
            _pollTimer.Start();
        }

        private async void PollTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var metrics = await _httpClient.GetFromJsonAsync<PerformanceMetrics>("/api/performance/metrics");
                if (metrics != null)
                {
                    CpuUsage = metrics.CpuUsage;
                    MemoryUsage = metrics.MemoryUsage;
                }
            }
            catch (Exception ex)
            {
                _pollTimer.Stop();
                MessageBox.Show($"Frequent Polling Error: {ex.Message}");
            }
        }


        private async Task Disconnect()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            if (_pollTimer.IsEnabled)
            {
                _pollTimer.Stop();
            }

            CpuUsage = 0;
            MemoryUsage = 0;
        }

        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            await Disconnect();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

    public class PerformanceMetrics
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
    }
}