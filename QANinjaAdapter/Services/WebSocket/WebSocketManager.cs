using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
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
            try
            {
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[WS-SEND] Sending WebSocket message: {message}",
                    NinjaTrader.Cbi.LogLevel.Information);
                
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text, true, CancellationToken.None);
                
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[WS-SEND-DONE] WebSocket message sent successfully",
                    NinjaTrader.Cbi.LogLevel.Information);
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[WS-SEND-ERROR] Error sending WebSocket message: {ex.Message}",
                    NinjaTrader.Cbi.LogLevel.Error);
                throw;
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
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[WS-RECV-START] Waiting for WebSocket message, WebSocket state: {ws.State}",
                    NinjaTrader.Cbi.LogLevel.Information);
                
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[WS-RECV-DONE] Received WebSocket message, Type: {result.MessageType}, Count: {result.Count}, EndOfMessage: {result.EndOfMessage}",
                    NinjaTrader.Cbi.LogLevel.Information);
                
                return result;
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[WS-RECV-ERROR] Error receiving WebSocket message: {ex.Message}",
                    NinjaTrader.Cbi.LogLevel.Error);
                throw;
            }
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
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[PARSE-ERROR] Data too small for {nativeSymbolName}, length: {data.Length}",
                    NinjaTrader.Cbi.LogLevel.Error);
                return null;
            }

            // Log the raw binary data for debugging
            string hexData = BitConverter.ToString(data, 0, Math.Min(data.Length, 64)).Replace("-", "");
            NinjaTrader.NinjaScript.NinjaScript.Log(
                $"[PARSE-DEBUG] Parsing binary message for {nativeSymbolName}, token: {expectedToken}, data: {hexData}...",
                NinjaTrader.Cbi.LogLevel.Information);

            try
            {
                int offset = 0;
                int packetCount = ReadInt16BE(data, offset);
                offset += 2;

                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[PARSE-DEBUG] Packet count: {packetCount} for {nativeSymbolName}",
                    NinjaTrader.Cbi.LogLevel.Information);

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
                    if (isMcxSegment && isFullMode)
                    {
                        isFullMode = false; // Force to not parse beyond quote mode for MCX
                    }

                    if (!isLtpMode && !isQuoteMode && !(packetLength == 184))
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

                    // Create a new ZerodhaTickData object with default values
                    var tickData = new Models.MarketData.ZerodhaTickData
                    {
                        InstrumentToken = iToken,
                        InstrumentIdentifier = nativeSymbolName,
                        HasMarketDepth = isFullMode,
                        IsIndex = false,
                        LastTradePrice = 0,
                        LastTradeQty = 0,
                        AverageTradePrice = 0,
                        TotalQtyTraded = 0,
                        BuyQty = 0,
                        SellQty = 0,
                        Open = 0,
                        High = 0,
                        Low = 0,
                        Close = 0,
                        OpenInterest = 0,
                        OpenInterestDayHigh = 0,
                        OpenInterestDayLow = 0,
                        // Initialize with current time in Local kind
                        LastTradeTime = DateTime.Now,
                        ExchangeTimestamp = DateTime.Now
                    };

                    NinjaTrader.NinjaScript.NinjaScript.Log(
                        $"[PARSE-DEBUG] Found matching token {iToken} for {nativeSymbolName}, packet mode: LTP={isLtpMode}, Quote={isQuoteMode}, Full={isFullMode}, Length={packetLength}",
                        NinjaTrader.Cbi.LogLevel.Information);

                    // Parse the packet based on mode
                    if (isLtpMode)
                    {
                        // LTP mode - only last traded price
                        if (offset + 4 + 4 <= data.Length)
                        {
                            int lastTradedPrice = ReadInt32BE(data, offset + 4);
                            tickData.LastTradePrice = lastTradedPrice / 100.0;
                        }
                        
                        // Log LTP mode parsing
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[PARSE-LTP] {nativeSymbolName}: LTP={tickData.LastTradePrice}, Time={tickData.LastTradeTime:HH:mm:ss.fff}",
                            NinjaTrader.Cbi.LogLevel.Information);
                    }
                    else if (isQuoteMode || isFullMode || (isMcxSegment && packetLength == 184))
                    {
                        // Quote or Full mode - more fields
                        if (offset + 4 + 4 <= data.Length)
                        {
                            int lastTradedPrice = ReadInt32BE(data, offset + 4);
                            tickData.LastTradePrice = lastTradedPrice / 100.0;
                        }
                        
                        if (offset + 8 + 4 <= data.Length)
                            tickData.LastTradeQty = ReadInt32BE(data, offset + 8);
                        
                        if (offset + 12 + 4 <= data.Length)
                            tickData.AverageTradePrice = ReadInt32BE(data, offset + 12) / 100.0;
                        
                        if (offset + 16 + 4 <= data.Length)
                            tickData.TotalQtyTraded = ReadInt32BE(data, offset + 16);
                        
                        if (offset + 20 + 4 <= data.Length)
                            tickData.BuyQty = ReadInt32BE(data, offset + 20);
                        
                        if (offset + 24 + 4 <= data.Length)
                            tickData.SellQty = ReadInt32BE(data, offset + 24);
                        
                        if (offset + 28 + 4 <= data.Length)
                            tickData.Open = ReadInt32BE(data, offset + 28) / 100.0;
                        
                        if (offset + 32 + 4 <= data.Length)
                            tickData.High = ReadInt32BE(data, offset + 32) / 100.0;
                        
                        if (offset + 36 + 4 <= data.Length)
                            tickData.Low = ReadInt32BE(data, offset + 36) / 100.0;
                        
                        if (offset + 40 + 4 <= data.Length)
                            tickData.Close = ReadInt32BE(data, offset + 40) / 100.0;

                        // Log Quote mode parsing
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[PARSE-QUOTE] {nativeSymbolName}: LTP={tickData.LastTradePrice}, LTQ={tickData.LastTradeQty}, Vol={tickData.TotalQtyTraded}",
                            NinjaTrader.Cbi.LogLevel.Information);

                        // Get exchange timestamp if available (only for true Full mode, not MCX full)
                        if (isFullMode)
                        {
                            if (offset + 44 + 4 <= data.Length)
                            {
                                int lastTradedTimestamp = ReadInt32BE(data, offset + 44);
                                if (lastTradedTimestamp > 0)
                                {
                                    tickData.LastTradeTime = UnixSecondsToLocalTime(lastTradedTimestamp);
                                }
                            }

                            if (offset + 48 + 4 <= data.Length)
                                tickData.OpenInterest = ReadInt32BE(data, offset + 48);
                            
                            if (offset + 52 + 4 <= data.Length)
                                tickData.OpenInterestDayHigh = ReadInt32BE(data, offset + 52);
                            
                            if (offset + 56 + 4 <= data.Length)
                                tickData.OpenInterestDayLow = ReadInt32BE(data, offset + 56);
                            
                            if (offset + 60 + 4 <= data.Length)
                            {
                                int exchangeTimestamp = ReadInt32BE(data, offset + 60);
                                if (exchangeTimestamp > 0)
                                {
                                    tickData.ExchangeTimestamp = UnixSecondsToLocalTime(exchangeTimestamp);
                                }
                            }

                            // Log Full mode timestamp parsing
                            NinjaTrader.NinjaScript.NinjaScript.Log(
                                $"[PARSE-FULL-TIME] {nativeSymbolName}: LastTradeTime={tickData.LastTradeTime:HH:mm:ss.fff}, ExchangeTime={tickData.ExchangeTimestamp:HH:mm:ss.fff}",
                                NinjaTrader.Cbi.LogLevel.Information);

                            // Parse market depth if available
                            if (isFullMode)
                            {
                                // Initialize depth arrays
                                for (int j = 0; j < 5; j++)
                                {
                                    tickData.BidDepth[j] = new Models.DepthEntry { Quantity = 0, Price = 0, Orders = 0 };
                                    tickData.AskDepth[j] = new Models.DepthEntry { Quantity = 0, Price = 0, Orders = 0 };
                                }
                                
                                // Process bids (5 levels)
                                for (int j = 0; j < 5; j++)
                                {
                                    int depthOffset = offset + 64 + (j * 12);
                                    if (depthOffset + 12 <= data.Length)
                                    {
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
                                }

                                // Process asks (5 levels)
                                for (int j = 0; j < 5; j++)
                                {
                                    int depthOffset = offset + 124 + (j * 12);
                                    if (depthOffset + 12 <= data.Length)
                                    {
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

                                // Count non-null bid and ask entries
                                int bidCount = 0;
                                int askCount = 0;
                                
                                foreach (var bid in tickData.BidDepth)
                                {
                                    if (bid != null && bid.Quantity > 0)
                                        bidCount++;
                                }
                                
                                foreach (var ask in tickData.AskDepth)
                                {
                                    if (ask != null && ask.Quantity > 0)
                                        askCount++;
                                }
                                
                                // Log market depth parsing
                                NinjaTrader.NinjaScript.NinjaScript.Log(
                                    $"[PARSE-DEPTH] {nativeSymbolName}: Parsed market depth with {bidCount} bids and {askCount} asks",
                                    NinjaTrader.Cbi.LogLevel.Information);
                            }
                        }
                    }

                    // Log the parsed tick data
                    NinjaTrader.NinjaScript.NinjaScript.Log(
                        $"[PARSE-SUCCESS] {nativeSymbolName}: LTP={tickData.LastTradePrice}, LTQ={tickData.LastTradeQty}, Vol={tickData.TotalQtyTraded}, Time={tickData.LastTradeTime:HH:mm:ss.fff}",
                        NinjaTrader.Cbi.LogLevel.Information);

                    return tickData;
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[PARSE-ERROR] Exception parsing binary message for {nativeSymbolName}: {ex.Message}",
                    NinjaTrader.Cbi.LogLevel.Error);
            }

            // If we get here, we didn't find a matching packet or there was an error
            // Return a default tick data object with current time
            var defaultTickData = new Models.MarketData.ZerodhaTickData
            {
                InstrumentToken = expectedToken,
                InstrumentIdentifier = nativeSymbolName,
                LastTradePrice = 0,
                LastTradeQty = 0,
                TotalQtyTraded = 0,
                LastTradeTime = DateTime.Now,
                ExchangeTimestamp = DateTime.Now
            };

            NinjaTrader.NinjaScript.NinjaScript.Log(
                $"[PARSE-DEFAULT] Returning default tick data for {nativeSymbolName}",
                NinjaTrader.Cbi.LogLevel.Warning);

            return defaultTickData;
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
            DateTime utcTime = epoch.AddSeconds(unixTimestamp);
            DateTime localTime = utcTime.ToLocalTime();
            
            // Ensure the Kind property is set correctly
            return new DateTime(localTime.Ticks, DateTimeKind.Local);
        }
    }
}
