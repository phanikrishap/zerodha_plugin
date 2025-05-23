using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
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
                        _sharedWebSocketClient.Dispose();
                    }
                    
                    if (_connectionCts != null)
                    {
                        _connectionCts.Cancel();
                        _connectionCts.Dispose();
                    }
                    
                    _connectionCts = new CancellationTokenSource();
                    _sharedWebSocketClient = _webSocketManager.CreateWebSocketClient();
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
        
        private async Task ProcessMessagesAsync(ClientWebSocket webSocket, CancellationToken token)
        {
            var buffer = new byte[8192];
            var segment = new ArraySegment<byte>(buffer);
            
            try
            {
                while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(segment, token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }
                    
                    // Create a copy of the received data to avoid buffer reuse issues
                    byte[] receivedData = new byte[result.Count];
                    Array.Copy(buffer, receivedData, result.Count);
                    
                    // Trigger the message received event
                    MessageReceived?.Invoke(this, receivedData);
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
                _isConnectionActive = false;
                AppLogger.Log("WebSocket message loop ended", QANinjaAdapter.Logging.LogLevel.Information);
            }
        }
        
        public async Task<bool> SendMessageAsync(byte[] data)
        {
            if (!_isConnectionActive || _sharedWebSocketClient?.State != WebSocketState.Open)
                return false;
                
            try
            {
                await _sharedWebSocketClient.SendAsync(
                    new ArraySegment<byte>(data), 
                    WebSocketMessageType.Binary,
                    true,
                    _connectionCts.Token);
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error sending WebSocket message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                return false;
            }
        }
        
        public void Dispose()
        {
            try
            {
                _connectionCts?.Cancel();
                
                // Try to close the connection gracefully
                if (_sharedWebSocketClient?.State == WebSocketState.Open)
                {
                    _sharedWebSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service disposing", CancellationToken.None)
                        .GetAwaiter().GetResult();
                }
                
                _sharedWebSocketClient?.Dispose();
                _connectionCts?.Dispose();
                
                _isConnectionActive = false;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error during WebSocketConnectionManager disposal: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
    }
}
