using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Services.Zerodha;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Manages WebSocket connections and authentication
    /// </summary>
    public class WebSocketConnectionManager
    {
        private static WebSocketConnectionManager _instance;
        private readonly ZerodhaClient _zerodhaClient;

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
        }

        /// <summary>
        /// Creates a new WebSocket client
        /// </summary>
        /// <returns>A configured ClientWebSocket instance</returns>
        public ClientWebSocket CreateWebSocketClient()
        {
            var ws = new ClientWebSocket();
            
            // Set WebSocket options for performance
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            ws.Options.SetBuffer(16384, 16384); // Increase buffer sizes
            
            return ws;
        }

        /// <summary>
        /// Connects to the Zerodha WebSocket
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ConnectAsync(ClientWebSocket ws)
        {
            string wsUrl = _zerodhaClient.GetWebSocketUrl();
            await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
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
            if (string.IsNullOrWhiteSpace(wsUrl)) throw new ArgumentNullException(nameof(wsUrl));
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));

            // Construct the full WebSocket URL with API key and access token
            string fullUrl = $"{wsUrl}?api_key={apiKey}&access_token={accessToken}";
            
            AppLogger.Log($"WebSocketConnectionManager: Connecting to WebSocket URL: {fullUrl}", QANinjaAdapter.Logging.LogLevel.Information);
            
            try
            {
                // Connect to the WebSocket with authentication in URL
                await ws.ConnectAsync(new Uri(fullUrl), CancellationToken.None);
                AppLogger.Log($"WebSocketConnectionManager: Successfully connected to WebSocket", QANinjaAdapter.Logging.LogLevel.Information);
                
                // Send mode message after connection
                string setModeMessage = $"{{\"a\":\"mode\",\"v\":[\"full\"]}}"; // Set mode to full
                byte[] setModeBytes = Encoding.UTF8.GetBytes(setModeMessage);
                await ws.SendAsync(new ArraySegment<byte>(setModeBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error connecting to WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                throw new Exception($"Error connecting to WebSocket: {ex.Message}", ex);
            }
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
            if (string.IsNullOrWhiteSpace(wsUrl)) throw new ArgumentNullException(nameof(wsUrl));
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));

            // Construct the full WebSocket URL with API key and access token
            string fullUrl = $"{wsUrl}?api_key={apiKey}&access_token={accessToken}";
            
            AppLogger.Log($"WebSocketConnectionManager: Connecting to WebSocket URL: {fullUrl}", QANinjaAdapter.Logging.LogLevel.Information);

            var ws = new ClientWebSocket();
            // Set WebSocket options for performance
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            ws.Options.SetBuffer(16384, 16384); // Increase buffer sizes
            
            try
            {
                // Connect to the WebSocket with authentication in URL
                await ws.ConnectAsync(new Uri(fullUrl), CancellationToken.None);
                AppLogger.Log($"WebSocketConnectionManager: Successfully connected to WebSocket", QANinjaAdapter.Logging.LogLevel.Information);
                
                // Send mode message after connection
                string setModeMessage = $"{{\"a\":\"mode\",\"v\":[\"full\"]}}"; // Set mode to full
                byte[] setModeBytes = Encoding.UTF8.GetBytes(setModeMessage);
                await ws.SendAsync(new ArraySegment<byte>(setModeBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error connecting to WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                throw new Exception($"Error connecting to WebSocket: {ex.Message}", ex);
            }
            return ws;
        }

        /// <summary>
        /// Closes the WebSocket connection
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task CloseAsync(ClientWebSocket ws)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                    AppLogger.Log("WebSocket connection closed normally", QANinjaAdapter.Logging.LogLevel.Information);
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error closing WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                }
                finally
                {
                    ws.Dispose();
                }
            }
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
            try
            {
                AppLogger.Log($"Waiting for WebSocket message, WebSocket state: {ws.State}", QANinjaAdapter.Logging.LogLevel.Debug);
                
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                AppLogger.Log($"Received WebSocket message, Type: {result.MessageType}, Count: {result.Count}, EndOfMessage: {result.EndOfMessage}", 
                    QANinjaAdapter.Logging.LogLevel.Debug);
                
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error receiving WebSocket message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                throw;
            }
        }
    }
}
