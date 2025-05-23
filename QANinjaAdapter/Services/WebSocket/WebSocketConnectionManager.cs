using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Services.Zerodha;
using QANinjaAdapter.Models.MarketData;
using System.Collections.Generic;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Manages WebSocket connections and authentication
    /// </summary>
    public class WebSocketConnectionManager
    {
        private static WebSocketConnectionManager _instance;
        private readonly ZerodhaClient _zerodhaClient;
        private readonly object _connectionLock = new object();
        private readonly Dictionary<string, WebSocketConnection> _connectionPool = new Dictionary<string, WebSocketConnection>();
        private readonly List<WebSocketConnection> _activeConnections = new List<WebSocketConnection>();
        private readonly WebSocketConnectionFactory _connectionFactory;
        private readonly WebSocketConnectionHealthMonitor _healthMonitor;
        private readonly WebSocketMessageParser _messageParser;

        /// <summary>
        /// Gets the singleton instance of the WebSocketConnectionManager
        /// </summary>
        public static WebSocketConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WebSocketConnectionManager();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private WebSocketConnectionManager()
        {
            _zerodhaClient = ZerodhaClient.Instance;
            _connectionFactory = WebSocketConnectionFactory.Instance;
            _healthMonitor = WebSocketConnectionHealthMonitor.Instance;
            _messageParser = WebSocketMessageParser.Instance;
            
            AppLogger.Log("WebSocketConnectionManager initialized", QANinjaAdapter.Logging.LogLevel.Information);
        }
        
        /// <summary>
        /// Creates a new WebSocket client
        /// </summary>
        /// <returns>A configured ClientWebSocket instance</returns>
        public ClientWebSocket CreateWebSocketClient()
        {
            var connection = _connectionFactory.CreateConnection();
            
            lock (_connectionLock)
            {
                _activeConnections.Add(connection);
            }
            
            return connection.WebSocket;
        }

        /// <summary>
        /// Connects to the Zerodha WebSocket
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ConnectAsync(ClientWebSocket ws)
        {
            string wsUrl = _zerodhaClient.GetWebSocketUrl();
            var configManager = QANinjaAdapter.Services.Configuration.ConfigurationManager.Instance;
            string apiKey = configManager.ApiKey;
            string accessToken = configManager.AccessToken;
            
            await ConnectAsync(ws, wsUrl, apiKey, accessToken);
        }
        
        /// <summary>
        /// Connects to a WebSocket with the specified URL, API key, and access token
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="wsUrl">The WebSocket URL</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ConnectAsync(ClientWebSocket ws, string wsUrl, string apiKey, string accessToken)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            
            WebSocketConnection connection = null;
            
            lock (_connectionLock)
            {
                connection = _activeConnections.FirstOrDefault(c => c.WebSocket == ws);
            }
            
            if (connection == null)
            {
                throw new InvalidOperationException("WebSocket not found in active connections");
            }
            
            await connection.ConnectAsync(wsUrl, apiKey, accessToken);
        }
        
        /// <summary>
        /// Creates a new WebSocket client and connects to the specified URL with API key and access token
        /// </summary>
        /// <param name="wsUrl">The WebSocket URL</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>A connected ClientWebSocket instance</returns>
        public async Task<ClientWebSocket> ConnectAsync(string wsUrl, string apiKey, string accessToken)
        {
            var connection = _connectionFactory.CreateConnection("General");
            
            lock (_connectionLock)
            {
                _activeConnections.Add(connection);
            }
            
            AppLogger.Log($"Initiating connection for WebSocket {connection.ConnectionId}", 
                QANinjaAdapter.Logging.LogLevel.Information);
                
            await connection.ConnectAsync(wsUrl, apiKey, accessToken);
            
            // Log connection metrics after successful connection
            LogConnectionMetrics();
            
            return connection.WebSocket;
        }
        
        /// <summary>
        /// Gets a dedicated WebSocket connection for a specific purpose
        /// </summary>
        /// <param name="purpose">A string identifying the purpose of this connection</param>
        /// <param name="wsUrl">The WebSocket URL</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>A dedicated ClientWebSocket instance</returns>
        public async Task<ClientWebSocket> GetDedicatedConnectionAsync(string purpose, string wsUrl, string apiKey, string accessToken)
        {
            string connectionKey = $"{purpose}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            AppLogger.Log($"Creating dedicated WebSocket connection for {purpose} with key {connectionKey}", 
                QANinjaAdapter.Logging.LogLevel.Information);
            
            var connection = _connectionFactory.CreateConnection(purpose);
            
            lock (_connectionLock)
            {
                _activeConnections.Add(connection);
                _connectionPool[connectionKey] = connection;
            }
            
            // Connect the WebSocket
            await connection.ConnectAsync(wsUrl, apiKey, accessToken);
            
            AppLogger.Log($"Successfully created dedicated connection for {purpose} with key {connectionKey}", 
                QANinjaAdapter.Logging.LogLevel.Information);
                
            return connection.WebSocket;
        }

        /// <summary>
        /// Sends a message through the WebSocket
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="message">The message to send</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SendMessageAsync(ClientWebSocket ws, string message)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            
            WebSocketConnection connection = null;
            
            lock (_connectionLock)
            {
                connection = _activeConnections.FirstOrDefault(c => c.WebSocket == ws);
            }
            
            if (connection == null)
            {
                throw new InvalidOperationException("WebSocket not found in active connections");
            }
            
            await connection.SendMessageAsync(message);
        }

        /// <summary>
        /// Receives a message from the WebSocket
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="buffer">The buffer to receive the message into</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The WebSocket receive result</returns>
        public async Task<WebSocketReceiveResult> ReceiveMessageAsync(ClientWebSocket ws, byte[] buffer, CancellationToken cancellationToken)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            
            WebSocketConnection connection = null;
            
            lock (_connectionLock)
            {
                connection = _activeConnections.FirstOrDefault(c => c.WebSocket == ws);
            }
            
            if (connection == null)
            {
                throw new InvalidOperationException("WebSocket not found in active connections");
            }
            
            return await connection.ReceiveMessageAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Closes the WebSocket connection
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task CloseAsync(ClientWebSocket ws)
        {
            if (ws == null) return;
            
            WebSocketConnection connection = null;
            string connectionKeyToRemove = null;
            
            lock (_connectionLock)
            {
                connection = _activeConnections.FirstOrDefault(c => c.WebSocket == ws);
                
                if (connection != null)
                {
                    _activeConnections.Remove(connection);
                    
                    // Find and remove from connection pool
                    foreach (var kvp in _connectionPool)
                    {
                        if (kvp.Value.WebSocket == ws)
                        {
                            connectionKeyToRemove = kvp.Key;
                            break;
                        }
                    }
                    
                    if (connectionKeyToRemove != null)
                    {
                        _connectionPool.Remove(connectionKeyToRemove);
                        AppLogger.Log($"WebSocket connection {connection.ConnectionId} with key {connectionKeyToRemove} removed from pool", 
                            QANinjaAdapter.Logging.LogLevel.Information);
                    }
                }
            }
            
            if (connection != null)
            {
                await connection.CloseAsync();
                
                // Log connection metrics after closing
                LogConnectionMetrics();
            }
        }
        
        /// <summary>
        /// Parses a binary message from Zerodha WebSocket
        /// </summary>
        /// <param name="data">The binary data</param>
        /// <param name="expectedToken">The expected instrument token</param>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="isMcxSegment">True if the instrument belongs to MCX segment, false otherwise</param>
        /// <returns>A MarketDataEventArgs object containing all market data fields</returns>
        public MarketDataEventArgs ParseBinaryMessage(byte[] data, long expectedToken, string nativeSymbolName, bool isMcxSegment = false)
        {
            return _messageParser.ParseBinaryMessage(data, expectedToken, nativeSymbolName, isMcxSegment);
        }
        
        /// <summary>
        /// Updates the activity timestamp for a WebSocket connection
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        public void UpdateConnectionActivity(ClientWebSocket ws)
        {
            if (ws == null) return;
            
            lock (_connectionLock)
            {
                var connection = _activeConnections.FirstOrDefault(c => c.WebSocket == ws);
                if (connection != null)
                {
                    connection.UpdateActivity();
                    AppLogger.Log($"Updated activity timestamp for WebSocket {connection.ConnectionId} (Purpose: {connection.Purpose})", 
                        QANinjaAdapter.Logging.LogLevel.Debug);
                }
            }
        }
        
        /// <summary>
        /// Logs metrics about current WebSocket connections
        /// </summary>
        private void LogConnectionMetrics()
        {
            try
            {
                lock (_connectionLock)
                {
                    int totalConnections = _activeConnections.Count;
                    int openConnections = _activeConnections.Count(c => c.State == WebSocketState.Open);
                    int connectingConnections = _activeConnections.Count(c => c.State == WebSocketState.Connecting);
                    int closedConnections = _activeConnections.Count(c => c.State == WebSocketState.Closed || c.State == WebSocketState.Aborted);
                    int poolSize = _connectionPool.Count;
                    
                    var oldestConnection = _activeConnections
                        .OrderByDescending(c => c.GetUptime())
                        .FirstOrDefault();
                    
                    string oldestConnectionInfo = oldestConnection != null 
                        ? $", Oldest connection: {oldestConnection.ConnectionId} (Uptime: {oldestConnection.GetUptime().TotalMinutes:F1} min)" 
                        : "";
                    
                    AppLogger.Log($"WebSocket Connection Metrics: Total: {totalConnections}, Open: {openConnections}, " +
                        $"Connecting: {connectingConnections}, Closed: {closedConnections}, Pool size: {poolSize}{oldestConnectionInfo}", 
                        QANinjaAdapter.Logging.LogLevel.Information);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error logging connection metrics: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Checks if a WebSocket connection is healthy and reconnects if needed
        /// </summary>
        /// <param name="ws">The WebSocket client to check</param>
        /// <param name="wsUrl">The WebSocket URL</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>True if the connection is healthy or was successfully reconnected, false otherwise</returns>
        public async Task<bool> EnsureConnectionHealthyAsync(ClientWebSocket ws, string wsUrl, string apiKey, string accessToken)
        {
            if (ws == null) return false;
            
            WebSocketConnection connection = null;
            
            lock (_connectionLock)
            {
                connection = _activeConnections.FirstOrDefault(c => c.WebSocket == ws);
            }
            
            if (connection == null)
            {
                return false;
            }
            
            if (connection.State == WebSocketState.Open)
            {
                return true; // Connection is already healthy
            }
            
            var (success, newConnection) = await _healthMonitor.EnsureConnectionHealthyAsync(
                connection, wsUrl, apiKey, accessToken);
                
            if (success && newConnection != null)
            {
                // Update connection pool if needed
                lock (_connectionLock)
                {
                    // Remove old connection
                    _activeConnections.Remove(connection);
                    
                    // Add new connection
                    _activeConnections.Add(newConnection);
                    
                    // Find the key for this connection in the pool
                    string keyToUpdate = null;
                    foreach (var kvp in _connectionPool)
                    {
                        if (kvp.Value == connection)
                        {
                            keyToUpdate = kvp.Key;
                            break;
                        }
                    }
                    
                    if (keyToUpdate != null)
                    {
                        _connectionPool[keyToUpdate] = newConnection;
                        AppLogger.Log($"Updated connection pool with new WebSocket for key {keyToUpdate}", 
                            QANinjaAdapter.Logging.LogLevel.Information);
                    }
                }
                
                LogConnectionMetrics();
            }
            
            return success;
        }
        
        /// <summary>
        /// Performs a health check on all active connections
        /// </summary>
        public void CheckConnectionsHealth()
        {
            List<WebSocketConnection> connectionsToCheck = new List<WebSocketConnection>();
            
            // Get a snapshot of connections to check under the lock
            lock (_connectionLock)
            {
                connectionsToCheck.AddRange(_activeConnections);
            }
            
            // Check the health of all connections
            _healthMonitor.CheckConnections(connectionsToCheck);
        }
    }
}
