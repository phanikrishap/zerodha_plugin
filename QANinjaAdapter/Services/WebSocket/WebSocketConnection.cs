using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Logging;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Represents a single WebSocket connection with its state and operations
    /// </summary>
    public class WebSocketConnection
    {
        private readonly ClientWebSocket _webSocket;
        private readonly string _connectionId;
        private readonly string _purpose;
        private readonly Stopwatch _connectionUptime;
        private DateTime _lastActivity;
        private bool _isActive;
        private int _reconnectAttempts;
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets the connection ID
        /// </summary>
        public string ConnectionId => _connectionId;

        /// <summary>
        /// Gets the purpose of this connection
        /// </summary>
        public string Purpose => _purpose;

        /// <summary>
        /// Gets the underlying WebSocket client
        /// </summary>
        public ClientWebSocket WebSocket => _webSocket;

        /// <summary>
        /// Gets whether the connection is active
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Gets the last activity time of this connection
        /// </summary>
        public DateTime LastActivity => _lastActivity;

        /// <summary>
        /// Gets the number of reconnect attempts for this connection
        /// </summary>
        public int ReconnectAttempts => _reconnectAttempts;

        /// <summary>
        /// Gets the current state of the WebSocket
        /// </summary>
        public WebSocketState State => _webSocket.State;

        /// <summary>
        /// Creates a new WebSocket connection
        /// </summary>
        /// <param name="webSocket">The WebSocket client</param>
        /// <param name="purpose">The purpose of this connection</param>
        public WebSocketConnection(ClientWebSocket webSocket, string purpose)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _purpose = purpose ?? "General";
            _connectionId = Guid.NewGuid().ToString().Substring(0, 8);
            _lastActivity = DateTime.Now;
            _isActive = true;
            _reconnectAttempts = 0;
            _connectionUptime = Stopwatch.StartNew();
        }

        /// <summary>
        /// Updates the activity timestamp for this connection
        /// </summary>
        public void UpdateActivity()
        {
            _lastActivity = DateTime.Now;
        }

        /// <summary>
        /// Gets the uptime of this connection
        /// </summary>
        /// <returns>The connection uptime</returns>
        public TimeSpan GetUptime()
        {
            return _connectionUptime.Elapsed;
        }

        /// <summary>
        /// Gets the time since last activity
        /// </summary>
        /// <returns>The time since last activity</returns>
        public TimeSpan GetInactiveTime()
        {
            return DateTime.Now - _lastActivity;
        }

        /// <summary>
        /// Increments the reconnect attempts counter
        /// </summary>
        public void IncrementReconnectAttempts()
        {
            _reconnectAttempts++;
        }

        /// <summary>
        /// Resets the reconnect attempts counter
        /// </summary>
        public void ResetReconnectAttempts()
        {
            _reconnectAttempts = 0;
        }

        /// <summary>
        /// Sets the active state of this connection
        /// </summary>
        /// <param name="isActive">Whether the connection is active</param>
        public void SetActive(bool isActive)
        {
            _isActive = isActive;
        }

        /// <summary>
        /// Connects to a WebSocket with the specified URL, API key, and access token
        /// </summary>
        /// <param name="wsUrl">The WebSocket URL</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ConnectAsync(string wsUrl, string apiKey, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(wsUrl)) throw new ArgumentNullException(nameof(wsUrl));
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));

            // Construct the full WebSocket URL with API key and access token
            string fullUrl = $"{wsUrl}?api_key={apiKey}&access_token={accessToken}";
            
            AppLogger.Log($"WebSocketConnection: Connecting WebSocket {_connectionId} to URL: {fullUrl}", QANinjaAdapter.Logging.LogLevel.Information);
            
            try
            {
                // Connect to the WebSocket with authentication in URL and timeout
                var connectTask = _webSocket.ConnectAsync(new Uri(fullUrl), CancellationToken.None);
                
                // Add timeout to the connection attempt
                if (await Task.WhenAny(connectTask, Task.Delay(_connectionTimeout)) != connectTask)
                {
                    throw new TimeoutException($"WebSocket connection timed out after {_connectionTimeout.TotalSeconds} seconds");
                }
                
                // Ensure the task is complete
                await connectTask;
                
                // Update connection info after successful connection
                UpdateActivity();
                ResetReconnectAttempts();
                SetActive(true);
                
                AppLogger.Log($"WebSocketConnection: Successfully connected WebSocket {_connectionId}", QANinjaAdapter.Logging.LogLevel.Information);
                
                // Send mode message after connection
                string setModeMessage = $"{{\"a\":\"mode\",\"v\":[\"full\"]}}";
                
                var messageBytes = Encoding.UTF8.GetBytes(setModeMessage);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                AppLogger.Log($"WebSocketConnection: Sent mode message to WebSocket {_connectionId}", QANinjaAdapter.Logging.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error connecting WebSocket {_connectionId}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                
                IncrementReconnectAttempts();
                SetActive(false);
                
                throw;
            }
        }

        /// <summary>
        /// Sends a message through the WebSocket
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));
            
            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                UpdateActivity();
                AppLogger.Log($"Sent message to WebSocket {_connectionId}: {message}", QANinjaAdapter.Logging.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error sending message to WebSocket {_connectionId}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Receives a message from the WebSocket
        /// </summary>
        /// <param name="buffer">The buffer to receive the message into</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The WebSocket receive result</returns>
        public async Task<WebSocketReceiveResult> ReceiveMessageAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                UpdateActivity();
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error receiving message from WebSocket {_connectionId}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Closes the WebSocket connection
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task CloseAsync()
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    // Use a timeout for closing to avoid hanging
                    var closeTask = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, $"Connection {_connectionId} closed", CancellationToken.None);
                    
                    // Add timeout to the close attempt
                    if (await Task.WhenAny(closeTask, Task.Delay(_connectionTimeout)) != closeTask)
                    {
                        AppLogger.Log($"WebSocket {_connectionId} close operation timed out after {_connectionTimeout.TotalSeconds} seconds", 
                            QANinjaAdapter.Logging.LogLevel.Warning);
                    }
                    else
                    {
                        await closeTask; // Ensure task completes
                        AppLogger.Log($"WebSocket {_connectionId} closed normally", QANinjaAdapter.Logging.LogLevel.Information);
                    }
                }
                else
                {
                    AppLogger.Log($"WebSocket {_connectionId} was not in Open state (State: {_webSocket.State}), skipping CloseAsync", 
                        QANinjaAdapter.Logging.LogLevel.Debug);
                }
            }
            catch (ObjectDisposedException ex)
            {
                AppLogger.Log($"WebSocket {_connectionId} was already disposed: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error closing WebSocket {_connectionId}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
            finally
            {
                try
                {
                    _webSocket.Dispose();
                    AppLogger.Log($"WebSocket {_connectionId} disposed successfully", QANinjaAdapter.Logging.LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error disposing WebSocket {_connectionId}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                }
            }
        }
    }
}
