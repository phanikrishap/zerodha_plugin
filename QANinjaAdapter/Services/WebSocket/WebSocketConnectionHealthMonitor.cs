using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Services.Zerodha;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Monitors the health of WebSocket connections and handles reconnection
    /// </summary>
    public class WebSocketConnectionHealthMonitor
    {
        private static WebSocketConnectionHealthMonitor _instance;
        private readonly Timer _connectionHealthCheckTimer;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);
        private readonly ZerodhaClient _zerodhaClient;

        /// <summary>
        /// Gets the singleton instance of the WebSocketConnectionHealthMonitor
        /// </summary>
        public static WebSocketConnectionHealthMonitor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WebSocketConnectionHealthMonitor();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private WebSocketConnectionHealthMonitor()
        {
            _zerodhaClient = ZerodhaClient.Instance;
            
            // Start connection health check timer
            _connectionHealthCheckTimer = new Timer(CheckConnectionsHealth, null, _healthCheckInterval, _healthCheckInterval);
            AppLogger.Log("WebSocketConnectionHealthMonitor initialized with automatic health checks", 
                QANinjaAdapter.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Checks the health of the provided connections
        /// </summary>
        /// <param name="connections">The connections to check</param>
        public void CheckConnections(IEnumerable<WebSocketConnection> connections)
        {
            var connectionsList = connections.ToList();
            
            if (connectionsList.Count > 0)
            {
                AppLogger.Log($"Health check running for {connectionsList.Count} WebSocket connections", 
                    QANinjaAdapter.Logging.LogLevel.Debug);
            }
            
            foreach (var connection in connectionsList)
            {
                try
                {
                    var inactiveTime = connection.GetInactiveTime();
                    var uptime = connection.GetUptime();
                    
                    AppLogger.Log($"Connection {connection.ConnectionId} (Purpose: {connection.Purpose}) - " +
                        $"State: {connection.State}, Uptime: {uptime.TotalMinutes:F1} min, " +
                        $"Last activity: {inactiveTime.TotalSeconds:F1} sec ago", 
                        QANinjaAdapter.Logging.LogLevel.Debug);
                    
                    // Check if connection is in a bad state or inactive for too long
                    if (connection.State != WebSocketState.Open || inactiveTime > TimeSpan.FromMinutes(5))
                    {
                        AppLogger.Log($"Unhealthy connection detected: {connection.ConnectionId} " +
                            $"(Purpose: {connection.Purpose}), State: {connection.State}, " +
                            $"Inactive for: {inactiveTime.TotalSeconds:F1} sec", 
                            QANinjaAdapter.Logging.LogLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error checking connection health: {ex.Message}", 
                        QANinjaAdapter.Logging.LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// Checks the health of all active connections and attempts to reconnect if needed
        /// </summary>
        /// <param name="state">The state object (not used)</param>
        private void CheckConnectionsHealth(object state)
        {
            try
            {
                // This method will be called by the timer
                // The actual implementation will be in the WebSocketConnectionManager
                // which will pass its connections to the CheckConnections method
                AppLogger.Log("Connection health check timer triggered", QANinjaAdapter.Logging.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error in connection health check: {ex.Message}", 
                    QANinjaAdapter.Logging.LogLevel.Error);
            }
        }

        /// <summary>
        /// Checks if a WebSocket connection is healthy and reconnects if needed
        /// </summary>
        /// <param name="connection">The connection to check</param>
        /// <param name="wsUrl">The WebSocket URL</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>A tuple containing a boolean indicating success and the new connection if created</returns>
        public async Task<(bool Success, WebSocketConnection NewConnection)> EnsureConnectionHealthyAsync(
            WebSocketConnection connection, string wsUrl, string apiKey, string accessToken)
        {
            if (connection == null)
            {
                AppLogger.Log("Cannot check health of null connection", QANinjaAdapter.Logging.LogLevel.Warning);
                return (false, null);
            }

            if (connection.State == WebSocketState.Open)
            {
                return (true, connection); // Connection is already healthy
            }

            AppLogger.Log($"WebSocket {connection.ConnectionId} is not healthy (State: {connection.State}), " +
                $"attempting to reconnect (Attempt {connection.ReconnectAttempts + 1})", 
                QANinjaAdapter.Logging.LogLevel.Warning);
            
            try
            {
                // Try to close and dispose the existing connection
                await connection.CloseAsync();
                
                // Create a new connection
                var newConnection = WebSocketConnectionFactory.Instance.CreateConnection(connection.Purpose);
                await newConnection.ConnectAsync(wsUrl, apiKey, accessToken);
                
                AppLogger.Log($"Successfully reconnected WebSocket {connection.ConnectionId} " +
                    $"with new connection {newConnection.ConnectionId}", 
                    QANinjaAdapter.Logging.LogLevel.Information);
                
                // Return the new connection status
                return (newConnection.State == WebSocketState.Open, newConnection);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Failed to reconnect WebSocket {connection.ConnectionId}: {ex.Message}", 
                    QANinjaAdapter.Logging.LogLevel.Error);
                return (false, null);
            }
        }
    }
}
