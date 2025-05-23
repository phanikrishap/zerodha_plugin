using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using QANinjaAdapter.Services.WebSocket;
using QANinjaAdapter.Services.Configuration;
using QANinjaAdapter.Logging;

namespace QANinjaAdapter.Services.MarketData.Connection
{
    public class WebSocketConnectionManager : IDisposable
    {
        private readonly WebSocketManager _webSocketManager;
        private ClientWebSocket _sharedWebSocketClient;
        private CancellationTokenSource _connectionCts;
        private bool _isConnectionActive = false;
        private readonly object _connectionLock = new object();
        private Task _messageLoopTask;
        private readonly Dictionary<string, ClientWebSocket> _dedicatedConnections = new Dictionary<string, ClientWebSocket>();
        private readonly Dictionary<string, CancellationTokenSource> _dedicatedCts = new Dictionary<string, CancellationTokenSource>();
        
        // Dictionary to track which WebSockets are currently receiving messages
        private readonly ConcurrentDictionary<ClientWebSocket, bool> _isReceiving = new ConcurrentDictionary<ClientWebSocket, bool>();
        private readonly object _receiveLock = new object();
        
        // Lock objects for send operations to prevent concurrent SendAsync calls
        private readonly object _sharedSendLock = new object();
        private readonly ConcurrentDictionary<string, object> _dedicatedSendLocks = new ConcurrentDictionary<string, object>();

        // Event for message received
        public event EventHandler<byte[]> MessageReceived;

        public WebSocketConnectionManager()
        {
            _webSocketManager = WebSocketManager.Instance;
            _connectionCts = new CancellationTokenSource();
        }

        public async Task<bool> EnsureConnectionAsync()
        {
            lock (_connectionLock)
            {
                if (_isConnectionActive && _sharedWebSocketClient?.State == WebSocketState.Open)
                    return true;

                // Connection setup logic
                try
                {
                    AppLogger.Log("Initializing WebSocket connection", QANinjaAdapter.Logging.LogLevel.Information);

                    // Clean up existing connection if needed
                    if (_sharedWebSocketClient != null)
                    {
                        try
                        {
                            if (_sharedWebSocketClient.State == WebSocketState.Open)
                            {
                                // Don't await here to avoid deadlocks
                                Task.Run(() => _webSocketManager.CloseAsync(_sharedWebSocketClient)).ConfigureAwait(false);
                            }
                            else
                            {
                                _sharedWebSocketClient.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Log($"Error disposing WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
                        }
                    }

                    if (_connectionCts != null)
                    {
                        _connectionCts.Cancel();
                        _connectionCts.Dispose();
                    }

                    _connectionCts = new CancellationTokenSource();
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error initializing WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                    _isConnectionActive = false;
                    return false;
                }
            }

            // Connect outside the lock
            try
            {
                var config = ConfigurationManager.Instance;
                string apiKey = config.ApiKey;
                string accessToken = config.AccessToken;

                // Create a new WebSocket client for the shared connection
                _sharedWebSocketClient = _webSocketManager.CreateWebSocketClient();
                await _webSocketManager.ConnectAsync(_sharedWebSocketClient, "wss://ws.kite.trade/", apiKey, accessToken);
                _isConnectionActive = true;

                // Start the message processing loop
                _messageLoopTask = ProcessMessagesAsync(_sharedWebSocketClient, _connectionCts.Token);

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error connecting WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                _isConnectionActive = false;
                return false;
            }
        }

        /// <summary>
        /// Creates a dedicated WebSocket connection for a specific symbol
        /// </summary>
        /// <param name="symbol">The symbol to create a dedicated connection for</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> CreateDedicatedConnectionAsync(string symbol)
        {
            try
            {
                var config = ConfigurationManager.Instance;
                string apiKey = config.ApiKey;
                string accessToken = config.AccessToken;

                AppLogger.Log($"Creating dedicated WebSocket connection for symbol {symbol}", QANinjaAdapter.Logging.LogLevel.Information);

                // Create a new dedicated connection for this symbol
                var dedicatedClient = _webSocketManager.CreateWebSocketClient();
                await _webSocketManager.ConnectAsync(dedicatedClient, "wss://ws.kite.trade/", apiKey, accessToken);
                var dedicatedCts = new CancellationTokenSource();

                lock (_connectionLock)
                {
                    _dedicatedConnections[symbol] = dedicatedClient;
                    _dedicatedCts[symbol] = dedicatedCts;
                }

                // Start a dedicated message processing loop for this connection
                // Use ConfigureAwait(false) to avoid deadlocks
                _ = Task.Run(() => ProcessMessagesAsync(dedicatedClient, dedicatedCts.Token))
                    .ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error creating dedicated connection for {symbol}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Gets a WebSocket client for a specific symbol, creating a dedicated connection if needed
        /// </summary>
        /// <param name="symbol">The symbol to get a connection for</param>
        /// <returns>A WebSocket client</returns>
        public async Task<ClientWebSocket> GetConnectionForSymbolAsync(string symbol)
        {
            lock (_connectionLock)
            {
                if (_dedicatedConnections.TryGetValue(symbol, out var dedicatedClient) &&
                    dedicatedClient.State == WebSocketState.Open)
                {
                    return dedicatedClient;
                }
            }

            // No dedicated connection exists or it's not open, create one
            bool success = await CreateDedicatedConnectionAsync(symbol);
            if (success)
            {
                lock (_connectionLock)
                {
                    return _dedicatedConnections[symbol];
                }
            }

            // Fall back to shared connection if dedicated connection failed
            if (!_isConnectionActive)
            {
                await EnsureConnectionAsync();
            }

            return _sharedWebSocketClient;
        }

        /// <summary>
        /// Sends a message through the shared WebSocket connection
        /// </summary>
        /// <param name="data">The data to send</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SendMessageAsync(byte[] data)
        {
            if (!_isConnectionActive || _sharedWebSocketClient?.State != WebSocketState.Open)
                return false;
                
            // Use a lock to prevent concurrent SendAsync calls on the same WebSocket
            Task<bool> sendTask = null;
            lock (_sharedSendLock)
            {
                sendTask = SendMessageInternalAsync(_sharedWebSocketClient, data, _connectionCts.Token);
            }
            
            return await sendTask;
        }
        
        private async Task<bool> SendMessageInternalAsync(ClientWebSocket webSocket, byte[] data, CancellationToken token)
        {
            try
            {
                AppLogger.Log("Sending WebSocket message", QANinjaAdapter.Logging.LogLevel.Information);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(data), 
                    WebSocketMessageType.Text, // Use Text for JSON messages
                    true,
                    token);
                AppLogger.Log("WebSocket message sent successfully", QANinjaAdapter.Logging.LogLevel.Information);
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error sending WebSocket message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Sends a message through a specific WebSocket connection for a symbol
        /// </summary>
        /// <param name="symbol">The symbol to send the message for</param>
        /// <param name="data">The data to send</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SendMessageForSymbolAsync(string symbol, byte[] data)
        {
            try
            {
                // Get or create a dedicated connection for this symbol
                var client = await GetConnectionForSymbolAsync(symbol);
                
                if (client?.State != WebSocketState.Open)
                {
                    AppLogger.Log($"WebSocket for symbol {symbol} is not in Open state", QANinjaAdapter.Logging.LogLevel.Warning);
                    return false;
                }
                
                // Get or create a lock object for this symbol
                var sendLock = _dedicatedSendLocks.GetOrAdd(symbol, _ => new object());
                
                // Use a lock to prevent concurrent SendAsync calls on the same WebSocket
                Task<bool> sendTask = null;
                lock (sendLock)
                {
                    // Get the cancellation token for this symbol if available
                    CancellationToken token = CancellationToken.None;
                    if (_dedicatedCts.TryGetValue(symbol, out var cts))
                    {
                        token = cts.Token;
                    }
                    
                    sendTask = SendMessageInternalAsync(client, data, token);
                }
                
                return await sendTask;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error sending WebSocket message for symbol {symbol}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                return false;
            }
        }

        private async Task ProcessMessagesAsync(ClientWebSocket webSocket, CancellationToken token)
        {
            var buffer = new byte[8192];
            var segment = new ArraySegment<byte>(buffer);

            // Register this WebSocket as receiving
            lock (_receiveLock)
            {
                if (!_isReceiving.ContainsKey(webSocket))
                {
                    _isReceiving[webSocket] = true;
                }
                else
                {
                    AppLogger.Log("Skipping duplicate receive loop for WebSocket", QANinjaAdapter.Logging.LogLevel.Warning);
                    return; // Another thread is already receiving on this WebSocket
                }
            }

            try
            {
                while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    try
                    {
                        WebSocketReceiveResult result = await webSocket.ReceiveAsync(segment, token);
                        
                        AppLogger.Log($"WebSocket received message: Type={result.MessageType}, Count={result.Count}, EndOfMessage={result.EndOfMessage}", QANinjaAdapter.Logging.LogLevel.Information);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            AppLogger.Log("WebSocket received Close message", QANinjaAdapter.Logging.LogLevel.Warning);
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            break;
                        }

                        // Create a copy of the received data to avoid buffer reuse issues
                        byte[] receivedData = new byte[result.Count];
                        Array.Copy(buffer, receivedData, result.Count);
                        
                        // Log first few bytes of the received data
                        if (result.Count > 0)
                        {
                            string hexData = BitConverter.ToString(receivedData, 0, Math.Min(receivedData.Length, 16)).Replace("-", "");
                            AppLogger.Log($"WebSocket received data: {hexData}...", QANinjaAdapter.Logging.LogLevel.Information);
                        }

                        // Trigger the message received event
                        MessageReceived?.Invoke(this, receivedData);
                    }
                    catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        AppLogger.Log($"WebSocket connection closed prematurely: {wsEx.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
                        break;
                    }
                    catch (InvalidOperationException opEx) when (opEx.Message.Contains("outstanding 'ReceiveAsync' call"))
                    {
                        AppLogger.Log($"Concurrent ReceiveAsync detected: {opEx.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                AppLogger.Log("WebSocket message loop canceled", QANinjaAdapter.Logging.LogLevel.Information);
            }
            catch (WebSocketException wsEx)
            {
                AppLogger.Log($"WebSocket error: {wsEx.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error in WebSocket message loop: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
            finally
            {
                // Unregister this WebSocket from receiving
                lock (_receiveLock)
                {
                    if (_isReceiving.ContainsKey(webSocket))
                    {
                        bool removed;
                        _isReceiving.TryRemove(webSocket, out removed);
                    }
                }
                
                _isConnectionActive = false;
                AppLogger.Log("WebSocket message loop ended", QANinjaAdapter.Logging.LogLevel.Information);
            }
        }

        public void Dispose()
        {
            try
            {
                _connectionCts?.Cancel();
                
                // Cancel all dedicated cancellation token sources
                lock (_connectionLock)
                {
                    foreach (var cts in _dedicatedCts.Values)
                    {
                        try
                        {
                            cts?.Cancel();
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Log($"Error cancelling dedicated CTS: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
                        }
                    }
                }
                
                // Close and dispose all dedicated connections
                List<ClientWebSocket> connectionsToDispose = new List<ClientWebSocket>();
                
                lock (_connectionLock)
                {
                    // Collect all connections that need to be disposed
                    foreach (var connection in _dedicatedConnections.Values)
                    {
                        if (connection != null)
                        {
                            connectionsToDispose.Add(connection);
                        }
                    }
                    
                    // Add the shared connection if it exists
                    if (_sharedWebSocketClient != null)
                    {
                        connectionsToDispose.Add(_sharedWebSocketClient);
                    }
                    
                    // Clear the collections
                    _dedicatedConnections.Clear();
                    _dedicatedCts.Clear();
                    _sharedWebSocketClient = null;
                }
                
                // Close and dispose connections outside the lock to avoid deadlocks
                foreach (var connection in connectionsToDispose)
                {
                    try
                    {
                        if (connection.State == WebSocketState.Open)
                        {
                            // Try to close gracefully
                            connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service disposing", CancellationToken.None)
                                .GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Log($"Error closing WebSocket connection: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
                    }
                    finally
                    {
                        try
                        {
                            connection.Dispose();
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Log($"Error disposing WebSocket connection: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
                        }
                    }
                }
                
                // Dispose the cancellation token source
                _connectionCts?.Dispose();
                
                _isConnectionActive = false;
                AppLogger.Log("WebSocketConnectionManager disposed successfully", QANinjaAdapter.Logging.LogLevel.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error during WebSocketConnectionManager disposal: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
    }
}
