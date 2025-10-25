# System Performance Dashboard

This project is a real-time System Performance Dashboard built with .NET 9, consisting of two parts: a Server (ASP.NET Core Web API with SignalR) and a Client (WPF desktop application using LiveCharts). Its purpose is to monitor and visualize system performance metrics such as CPU usage, memory usage, available memory, total running processes and system uptime. The dashboard collects these metrics on the server side and streams them live to connected clients.

The Server runs a background service `PerformanceMonitoringService` that uses Windows performance counters to sample system statistics every second. It calculates total and available memory, CPU utilization, process count and system uptime. The data is structured into a `PerformanceMetrics` model and sent both via SignalR for real-time updates and exposed through a REST endpoint `/api/performance/metrics` for manual or polling-based retrieval.

The Client is a WPF application that visualizes this data using LiveCharts gauges and a metrics card. It supports three connection modes:

- **WebSockets** – persistent, bidirectional communication where the server pushes data to the client instantly.
- **Long Polling** – the client repeatedly opens a long-lived HTTP request; when new data arrives, the server responds, and the client immediately reopens the connection.
- **Frequent Polling** – the client performs standard HTTP GET requests at a fixed interval (every second) to fetch the latest metrics from the REST endpoint.

### Comparative analysis of interaction methods:
**WebSockets** are the most resource-efficient for continuous real-time updates. A single TCP connection remains open, minimizing latency and bandwidth overhead since only minimal framing data is exchanged per message. CPU and memory overhead on both client and server sides are low once the connection is established. This mode is optimal for dashboards, monitoring tools or collaborative apps requiring constant updates.
**Long Polling** is less efficient but reliable in environments where WebSockets are unavailable (e.g., proxies, restricted networks). Each polling cycle opens a new HTTP request after the previous one completes, introducing higher latency and additional HTTP header traffic. The overhead increases linearly with the number of clients due to more frequent connection handling and thread management.
**Frequent Polling** is the least efficient, generating a new HTTP request every second regardless of whether new data exists. This leads to unnecessary network load and higher CPU utilization on the server due to repeated connection setup and teardown. However, it is simple to implement and easier to debug, making it useful for testing or limited-scope monitoring with few clients.
