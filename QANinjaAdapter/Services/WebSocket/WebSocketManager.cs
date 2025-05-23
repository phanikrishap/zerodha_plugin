using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Models.MarketData;
using QANinjaAdapter.Services.Zerodha;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Facade that manages WebSocket operations by delegating to specialized classes
    /// </summary>
    public class WebSocketManager
    {
        private static WebSocketManager _instance;
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly WebSocketSubscriptionManager _subscriptionManager;
        private readonly WebSocketMessageParser _messageParser;

        /// <summary>
        /// Gets the singleton instance of the WebSocketManager
        /// </summary>
        public static WebSocketManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WebSocketManager();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private WebSocketManager()
        {
            _connectionManager = WebSocketConnectionManager.Instance;
            _subscriptionManager = WebSocketSubscriptionManager.Instance;
            _messageParser = WebSocketMessageParser.Instance;
        }

        /// <summary>
        /// Creates a new WebSocket client
        /// </summary>
        /// <returns>A configured ClientWebSocket instance</returns>
        public ClientWebSocket CreateWebSocketClient()
        {
            return _connectionManager.CreateWebSocketClient();
        }

        /// <summary>
        /// Connects to the Zerodha WebSocket
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ConnectAsync(ClientWebSocket ws)
        {
            await _connectionManager.ConnectAsync(ws);
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
            await _connectionManager.ConnectAsync(ws, wsUrl, apiKey, accessToken);
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
            return await _connectionManager.ConnectAsync(wsUrl, apiKey, accessToken);
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
            return await _connectionManager.GetDedicatedConnectionAsync(purpose, wsUrl, apiKey, accessToken);
        }

        /// <summary>
        /// Subscribes to a symbol in the specified mode
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentToken">The instrument token</param>
        /// <param name="mode">The subscription mode (ltp, quote, full)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SubscribeAsync(ClientWebSocket ws, int instrumentToken, string mode)
        {
            await _subscriptionManager.SubscribeAsync(ws, instrumentToken, mode);
        }
        
        /// <summary>
        /// Batch subscribes to multiple instruments in the specified mode
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentTokens">List of instrument tokens to subscribe to</param>
        /// <param name="mode">The subscription mode (ltp, quote, full)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task BatchSubscribeAsync(ClientWebSocket ws, List<int> instrumentTokens, string mode)
        {
            await _subscriptionManager.BatchSubscribeAsync(ws, instrumentTokens, mode);
        }

        /// <summary>
        /// Unsubscribes from a symbol
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentToken">The instrument token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task UnsubscribeAsync(ClientWebSocket ws, int instrumentToken)
        {
            await _subscriptionManager.UnsubscribeAsync(ws, instrumentToken);
        }

        /// <summary>
        /// Closes the WebSocket connection
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task CloseAsync(ClientWebSocket ws)
        {
            await _connectionManager.CloseAsync(ws);
        }

        /// <summary>
        /// Sends a text message over the WebSocket
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="message">The message to send</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SendTextMessageAsync(ClientWebSocket ws, string message)
        {
            await _subscriptionManager.SendTextMessageAsync(ws, message);
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
            return await _connectionManager.ReceiveMessageAsync(ws, buffer, cancellationToken);
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
        /// Checks if a WebSocket connection is healthy and reconnects if needed
        /// </summary>
        /// <param name="ws">The WebSocket client to check</param>
        /// <param name="wsUrl">The WebSocket URL</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>True if the connection is healthy or was successfully reconnected, false otherwise</returns>
        public async Task<bool> EnsureConnectionHealthyAsync(ClientWebSocket ws, string wsUrl, string apiKey, string accessToken)
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                AppLogger.Log("WebSocket connection is not healthy, attempting to reconnect", QANinjaAdapter.Logging.LogLevel.Warning);
                
                try
                {
                    // Try to close and dispose the existing connection if it exists
                    if (ws != null)
                    {
                        try
                        {
                            await _connectionManager.CloseAsync(ws);
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Log($"Error closing unhealthy WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Warning);
                        }
                    }
                    
                    // Create a new connection
                    var newWs = CreateWebSocketClient();
                    await ConnectAsync(newWs, wsUrl, apiKey, accessToken);
                    
                    // Return the new connection status
                    return newWs.State == WebSocketState.Open;
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Failed to reconnect WebSocket: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                    return false;
                }
            }
            
            return true; // Connection is already healthy
        }
    }
}
