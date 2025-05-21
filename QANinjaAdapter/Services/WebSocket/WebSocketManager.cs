using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Models;
using QANinjaAdapter.Models.MarketData;
using QANinjaAdapter.Services.Zerodha;
using QANinjaAdapter;

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

            Logger.Info($"WebSocketManager: Subscribed to token {instrumentToken} in {mode} mode.");
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
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="isMcxSegment">True if the instrument belongs to MCX segment, false otherwise</param>
        /// <returns>A ZerodhaTickData object containing all market data fields</returns>
        public Models.MarketData.ZerodhaTickData ParseBinaryMessage(byte[] data, int expectedToken, string nativeSymbolName, bool isMcxSegment)
        {
            if (data.Length < 2)
            {
                return null;
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
                bool isLtpMode = packetLength == 8;
                bool isQuoteMode = packetLength == 44;
                bool isFullMode = packetLength == 184;

                // If it's an MCX segment, override isFullMode to false if it was true.
                // This ensures MCX instruments are always parsed as if they are at most 44-byte quote packets.
                if (isMcxSegment && isFullMode)
                {
                    isFullMode = false; // Force to not parse beyond quote mode for MCX
                    // isQuoteMode remains true if packetLength was 44, or becomes effectively true for parsing if packetLength was 184.
                    // If packetLength was 184, it will now fall into the (isQuoteMode || isFullMode) block but the subsequent isFullMode checks will be false.
                }

                if (!isLtpMode && !isQuoteMode && !(packetLength == 184)) // Adjusted condition to account for the MCX override of isFullMode
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

                // Create a new ZerodhaTickData object
                var tickData = new Models.MarketData.ZerodhaTickData
                {
                    InstrumentToken = iToken,
                    InstrumentIdentifier = nativeSymbolName,
                    HasMarketDepth = isFullMode,
                    IsIndex = false // Determine if it's an index based on token range or other logic
                };

                // Parse the packet based on mode
                if (isLtpMode)
                {
                    // LTP mode - only last traded price
                    int lastTradedPrice = ReadInt32BE(data, offset + 4);
                    tickData.LastTradePrice = lastTradedPrice / 100.0;
                    tickData.LastTradeTime = DateTime.Now;
                    tickData.ExchangeTimestamp = DateTime.Now; // Set ExchangeTimestamp for LTP mode
                }
                else if (isQuoteMode || isFullMode || (isMcxSegment && packetLength == 184)) // Ensure MCX 184-byte packets are processed by this block too
                {
                    // Quote or Full mode - more fields
                    int lastTradedPrice = ReadInt32BE(data, offset + 4);
                    tickData.LastTradePrice = lastTradedPrice / 100.0;
                    
                    tickData.LastTradeQty = ReadInt32BE(data, offset + 8);
                    tickData.AverageTradePrice = ReadInt32BE(data, offset + 12) / 100.0;
                    tickData.TotalQtyTraded = ReadInt32BE(data, offset + 16);
                    tickData.BuyQty = ReadInt32BE(data, offset + 20);
                    tickData.SellQty = ReadInt32BE(data, offset + 24);
                    tickData.Open = ReadInt32BE(data, offset + 28) / 100.0;
                    tickData.High = ReadInt32BE(data, offset + 32) / 100.0;
                    tickData.Low = ReadInt32BE(data, offset + 36) / 100.0;
                    tickData.Close = ReadInt32BE(data, offset + 40) / 100.0;

                    // Default timestamps for Quote mode, will be overwritten if Full mode
                    tickData.LastTradeTime = DateTime.Now;
                    tickData.ExchangeTimestamp = DateTime.Now;

                    // Get exchange timestamp if available (only for true Full mode, not MCX full)
                    if (isFullMode) // This isFullMode is now correctly false for MCX full packets due to earlier override
                    {
                        int lastTradedTimestamp = ReadInt32BE(data, offset + 44);
                        tickData.LastTradeTime = lastTradedTimestamp > 0 
                            ? UnixSecondsToLocalTime(lastTradedTimestamp) 
                            : DateTime.Now;

                        tickData.OpenInterest = ReadInt32BE(data, offset + 48);
                        tickData.OpenInterestDayHigh = ReadInt32BE(data, offset + 52);
                        tickData.OpenInterestDayLow = ReadInt32BE(data, offset + 56);
                        
                        int exchangeTimestamp = ReadInt32BE(data, offset + 60);
                        tickData.ExchangeTimestamp = exchangeTimestamp > 0 
                            ? UnixSecondsToLocalTime(exchangeTimestamp) 
                            : DateTime.Now;

                        // Parse market depth if available
                        if (isFullMode)
                        {
                            // Process bids (5 levels)
                            for (int j = 0; j < 5; j++)
                            {
                                int depthOffset = offset + 64 + (j * 12);
                                int qty = ReadInt32BE(data, depthOffset);
                                int price = ReadInt32BE(data, depthOffset + 4);
                                short orders = ReadInt16BE(data, depthOffset + 8);

                                tickData.BidDepth[j] = new Models.DepthEntry
                                {
                                    Quantity = qty,
                                    Price = price / 100.0,
                                    Orders = orders
                                };
                            }

                            // Process asks (5 levels)
                            for (int j = 0; j < 5; j++)
                            {
                                int depthOffset = offset + 124 + (j * 12);
                                int qty = ReadInt32BE(data, depthOffset);
                                int price = ReadInt32BE(data, depthOffset + 4);
                                short orders = ReadInt16BE(data, depthOffset + 8);

                                tickData.AskDepth[j] = new Models.DepthEntry
                                {
                                    Quantity = qty,
                                    Price = price / 100.0,
                                    Orders = orders
                                };
                            }
                        }
                    }
                }

                return tickData;
            }

            return null;
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
