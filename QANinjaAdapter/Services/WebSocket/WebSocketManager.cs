using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Services.Zerodha;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Manages WebSocket connections and message parsing
    /// </summary>
    public class WebSocketManager
    {
        private static WebSocketManager _instance;
        private readonly ZerodhaClient _zerodhaClient;

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
        /// Subscribes to a symbol in the specified mode
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentToken">The instrument token</param>
        /// <param name="mode">The subscription mode (ltp, quote, full)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SubscribeAsync(ClientWebSocket ws, int instrumentToken, string mode)
        {
            // First subscribe to the instrument
            string subscribeMsg = $@"{{""a"":""subscribe"",""v"":[{instrumentToken}]}}";
            await SendTextMessageAsync(ws, subscribeMsg);

            // Then set the mode
            string modeMsg = $@"{{""a"":""mode"",""v"":[""{ mode }"",[{instrumentToken}]]}}";
            await SendTextMessageAsync(ws, modeMsg);

            NinjaTrader.NinjaScript.NinjaScript.Log($"[WEBSOCKET] Subscribed to token {instrumentToken} in {mode} mode", NinjaTrader.Cbi.LogLevel.Information);
        }

        /// <summary>
        /// Unsubscribes from a symbol
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentToken">The instrument token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task UnsubscribeAsync(ClientWebSocket ws, int instrumentToken)
        {
            string unsubscribeMsg = $@"{{""a"":""unsubscribe"",""v"":[{instrumentToken}]}}";
            await SendTextMessageAsync(ws, unsubscribeMsg);
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
                }
                catch (Exception ex)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[WEBSOCKET] Error closing WebSocket: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                }
                finally
                {
                    ws.Dispose();
                }
            }
        }

        /// <summary>
        /// Sends a text message over the WebSocket
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="message">The message to send</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SendTextMessageAsync(ClientWebSocket ws, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(
                new ArraySegment<byte>(messageBytes),
                WebSocketMessageType.Text, true, CancellationToken.None);
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
            return await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        }

        /// <summary>
        /// Parses a binary message from Zerodha WebSocket
        /// </summary>
        /// <param name="data">The binary data</param>
        /// <param name="expectedToken">The expected instrument token</param>
        /// <returns>A tuple containing the last traded price, last traded quantity, and volume</returns>
        public (double LastPrice, int LastQuantity, int Volume, DateTime Timestamp) ParseBinaryMessage(byte[] data, int expectedToken)
        {
            if (data.Length < 2)
            {
                return (0, 0, 0, DateTime.Now);
            }

            int offset = 0;
            int packetCount = ReadInt16BE(data, offset);
            offset += 2;

            for (int i = 0; i < packetCount; i++)
            {
                // Check if we have enough data for packet length
                if (offset + 2 > data.Length)
                    break;

                int packetLength = ReadInt16BE(data, offset);
                offset += 2;

                // Check if we have enough data for the packet content
                if (offset + packetLength > data.Length)
                    break;

                // Only process packets with valid length
                if (packetLength != 8 && packetLength != 44 && packetLength != 184)
                {
                    offset += packetLength; // Skip this packet
                    continue;
                }

                // Check if this is our subscribed token
                int iToken = ReadInt32BE(data, offset);
                if (iToken != expectedToken)
                {
                    offset += packetLength; // Skip this packet
                    continue;
                }

                // Parse the packet
                int lastTradedPrice = ReadInt32BE(data, offset + 4);
                double ltp = lastTradedPrice / 100.0;

                int lastTradedQty = 0;
                int volume = 0;
                DateTime timestamp = DateTime.Now;

                if (packetLength >= 44)
                {
                    lastTradedQty = ReadInt32BE(data, offset + 8);
                    volume = ReadInt32BE(data, offset + 16);

                    // Get exchange timestamp if available
                    int exchangeTimestampOffset = offset + (packetLength >= 64 ? 60 : 44);
                    if (exchangeTimestampOffset + 4 <= offset + packetLength)
                    {
                        int exchangeTimestamp = ReadInt32BE(data, exchangeTimestampOffset);
                        if (exchangeTimestamp > 0)
                        {
                            timestamp = UnixSecondsToLocalTime(exchangeTimestamp);
                        }
                    }
                }

                return (ltp, lastTradedQty, volume, timestamp);
            }

            return (0, 0, 0, DateTime.Now);
        }

        /// <summary>
        /// Reads a 16-bit integer in big-endian format
        /// </summary>
        /// <param name="buffer">The buffer to read from</param>
        /// <param name="offset">The offset to start reading at</param>
        /// <returns>The 16-bit integer</returns>
        public static short ReadInt16BE(byte[] buffer, int offset)
        {
            return (short)((buffer[offset] << 8) | buffer[offset + 1]);
        }

        /// <summary>
        /// Reads a 32-bit integer in big-endian format
        /// </summary>
        /// <param name="buffer">The buffer to read from</param>
        /// <param name="offset">The offset to start reading at</param>
        /// <returns>The 32-bit integer</returns>
        public static int ReadInt32BE(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) |
                   (buffer[offset + 1] << 16) |
                   (buffer[offset + 2] << 8) |
                   buffer[offset + 3];
        }

        /// <summary>
        /// Converts a Unix timestamp to local time
        /// </summary>
        /// <param name="unixTimestamp">The Unix timestamp</param>
        /// <returns>The local DateTime</returns>
        private static DateTime UnixSecondsToLocalTime(int unixTimestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTimestamp).ToLocalTime();
        }
    }
}
