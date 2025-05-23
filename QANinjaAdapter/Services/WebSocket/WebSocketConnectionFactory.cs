using System;
using System.Net.WebSockets;
using QANinjaAdapter.Logging;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Factory for creating WebSocket connections
    /// </summary>
    public class WebSocketConnectionFactory
    {
        private static WebSocketConnectionFactory _instance;
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets the singleton instance of the WebSocketConnectionFactory
        /// </summary>
        public static WebSocketConnectionFactory Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WebSocketConnectionFactory();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private WebSocketConnectionFactory()
        {
        }

        /// <summary>
        /// Creates a new WebSocket connection
        /// </summary>
        /// <param name="purpose">The purpose of this connection</param>
        /// <returns>A new WebSocketConnection instance</returns>
        public WebSocketConnection CreateConnection(string purpose = "General")
        {
            var ws = CreateWebSocketClient();
            var connection = new WebSocketConnection(ws, purpose);
            
            AppLogger.Log($"Created new WebSocket connection with ID {connection.ConnectionId} for purpose: {purpose}", 
                QANinjaAdapter.Logging.LogLevel.Debug);
                
            return connection;
        }

        /// <summary>
        /// Creates a new WebSocket client with appropriate configuration
        /// </summary>
        /// <returns>A configured ClientWebSocket instance</returns>
        private ClientWebSocket CreateWebSocketClient()
        {
            var ws = new ClientWebSocket();
            
            // Set WebSocket options for performance
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            ws.Options.SetBuffer(16384, 16384); // Increase buffer sizes
            
            // Set reasonable timeouts
            var timeout = Convert.ToInt32(_connectionTimeout.TotalMilliseconds);
            if (timeout > 0)
            {
                try
                {
                    // Use reflection to set internal timeout property if available
                    var connectTimeoutProperty = ws.Options.GetType().GetProperty("ConnectTimeout");
                    if (connectTimeoutProperty != null)
                    {
                        connectTimeoutProperty.SetValue(ws.Options, _connectionTimeout);
                        AppLogger.Log($"Set WebSocket connection timeout to {_connectionTimeout.TotalSeconds} seconds", 
                            QANinjaAdapter.Logging.LogLevel.Debug);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Could not set WebSocket connection timeout: {ex.Message}", 
                        QANinjaAdapter.Logging.LogLevel.Warning);
                }
            }
            
            return ws;
        }
    }
}
