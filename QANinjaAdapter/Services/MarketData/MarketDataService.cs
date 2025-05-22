using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Zerodha.Websockets;
using QANinjaAdapter.Models;
using QANinjaAdapter.Models.MarketData;
using QANinjaAdapter.Services.Configuration;
using QANinjaAdapter.Services.Instruments;
using QANinjaAdapter.Services.WebSocket;
using QANinjaAdapter;

namespace QANinjaAdapter.Services.MarketData
{
    /// <summary>
    /// Service for handling real-time market data subscriptions
    /// </summary>
    public class MarketDataService
    {
        private static MarketDataService _instance;
        private readonly InstrumentManager _instrumentManager;
        private readonly WebSocketManager _webSocketManager;
        private readonly ConfigurationManager _configManager;
        private readonly ConcurrentDictionary<string, L1Subscription> _l1Subscriptions; 
        private readonly ConcurrentDictionary<string, L2Subscription> _l2Subscriptions;
        private readonly ConcurrentDictionary<string, int> _lastVolumeMap = new ConcurrentDictionary<string, int>(); // Added for volumeDelta

        /// <summary>
        /// Gets the singleton instance of the MarketDataService
        /// </summary>
        public static MarketDataService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MarketDataService();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private MarketDataService()
        {
            _instrumentManager = InstrumentManager.Instance;
            _webSocketManager = WebSocketManager.Instance;
            _configManager = ConfigurationManager.Instance;
        }

        /// <summary>
        /// Subscribes to real-time ticks for a symbol
        /// </summary>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="marketType">The market type</param>
        /// <param name="symbol">The symbol</param>
        /// <param name="l1Subscriptions">The L1 subscriptions dictionary</param>
        /// <param name="webSocketConnectionFunc">The WebSocket connection function</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SubscribeToTicks(
            string nativeSymbolName,
            MarketType marketType,
            string symbol,
            ConcurrentDictionary<string, L1Subscription> l1Subscriptions,
            WebSocketConnectionFunc webSocketConnectionFunc)
        {
            if (string.IsNullOrEmpty(symbol) || webSocketConnectionFunc == null)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Invalid parameters for {symbol}", NinjaTrader.Cbi.LogLevel.Error);
                return;
            }

            ClientWebSocket ws = null;
            var cts = new CancellationTokenSource();

            // Preallocate buffers to reduce GC pressure
            byte[] buffer = new byte[16384]; // Larger buffer for better network efficiency

            // Pre-calculate constants
            int tokenInt = 0;

            try
            {
                // Create and connect WebSocket
                ws = _webSocketManager.CreateWebSocketClient();
                await _webSocketManager.ConnectAsync(ws);

                // Get instrument token
                tokenInt = (int)(await _instrumentManager.GetInstrumentToken(symbol));

                // Subscribe in quote mode
                await _webSocketManager.SubscribeAsync(ws, tokenInt, "quote");

                // Start monitoring chart-close in separate task
                StartExitMonitoringTask(webSocketConnectionFunc, cts);

                // Main message processing loop
                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await _webSocketManager.ReceiveMessageAsync(ws, buffer, cts.Token);

                    // Log raw WebSocket message before any processing
                    if (result.Count > 0) // Only log if there's data
                    {
                        string rawMessageContent = "";
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            rawMessageContent = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            // Convert binary data to hex string for logging
                            rawMessageContent = BitConverter.ToString(buffer, 0, result.Count).Replace("-", "");
                        }

                        if (!string.IsNullOrEmpty(rawMessageContent))
                        {
                            NinjaTrader.NinjaScript.NinjaScript.Log(
                                $"[RAW-WS-RECV] Symbol: {nativeSymbolName}, Type: {result.MessageType}, Count: {result.Count}, Data: {rawMessageContent}",
                                NinjaTrader.Cbi.LogLevel.Information); // Or LogLevel.Debug if preferred
                        }
                        else if (result.MessageType != WebSocketMessageType.Close) // Log if not empty and not a close message already handled below
                        {
                             NinjaTrader.NinjaScript.NinjaScript.Log(
                                $"[RAW-WS-RECV] Symbol: {nativeSymbolName}, Type: {result.MessageType}, Count: {result.Count}, Data: [Non-text/binary or empty data received but not a Close message]",
                                NinjaTrader.Cbi.LogLevel.Warning);
                        }
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[TICK-SUBSCRIBE] WebSocket closed by server for {nativeSymbolName}. Status: {result.CloseStatus}, Description: {result.CloseStatusDescription}",
                            NinjaTrader.Cbi.LogLevel.Warning);
                        break; // Exit the loop gracefully
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                        continue; // Ignore JSON heartbeats/postbacks

                    // Specific logging for 1-byte binary messages
                    if (result.MessageType == WebSocketMessageType.Binary && result.Count == 1)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[WS-DEBUG] Received 1-byte binary message for {nativeSymbolName}. Data: {BitConverter.ToString(buffer, 0, result.Count)}. Potentially significant.",
                            NinjaTrader.Cbi.LogLevel.Warning);
                        // Let it fall through to be handled by the next check or parser, which should reject it.
                    }

                    // Skip processing if no valid data, ensuring Close messages are not caught here
                    if (result.Count < 2 && result.MessageType != WebSocketMessageType.Close) 
                    {
                         NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[WS-DEBUG] Skipping processing for {nativeSymbolName} due to insufficient data (Count: {result.Count}, Type: {result.MessageType}).",
                            NinjaTrader.Cbi.LogLevel.Information);
                        continue;
                    }

                    // Timestamp message receipt immediately to reduce timing errors
                    var receivedTime = DateTime.Now;
                    DateTime now = GetIndianTime(receivedTime);

                    // Log message receipt with timestamp
                    NinjaTrader.NinjaScript.NinjaScript.Log(
                        $"[WS-RECV] Received WebSocket message for {nativeSymbolName} at {receivedTime:HH:mm:ss.fff}, size: {result.Count} bytes",
                        NinjaTrader.Cbi.LogLevel.Information);

                    // Check if we have a valid subscription
                    if (!l1Subscriptions.TryGetValue(nativeSymbolName, out var sub))
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[TICK-SUBSCRIBE] No subscription found for {nativeSymbolName}",
                            NinjaTrader.Cbi.LogLevel.Warning);
                        continue;
                    }

                    // Fetch segment
                    string segment = _instrumentManager.GetSegmentForToken(tokenInt);
                    bool isMcxSegment = !string.IsNullOrEmpty(segment) && segment.Equals("MCX", StringComparison.OrdinalIgnoreCase);

                    // Log before parsing
                    NinjaTrader.NinjaScript.NinjaScript.Log(
                        $"[WS-PARSE] Parsing binary message for {nativeSymbolName}, token: {tokenInt}, segment: {segment}",
                        NinjaTrader.Cbi.LogLevel.Information);

                    // Parse the binary message into a rich data structure
                    var tickData = _webSocketManager.ParseBinaryMessage(buffer, tokenInt, nativeSymbolName, isMcxSegment);
                    DateTime parsedTime = DateTime.Now; // Capture time immediately after parsing
                    
                    // Log parsing result
                    if (tickData != null)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[WS-PARSE-SUCCESS] Successfully parsed tick for {nativeSymbolName}, LTP: {tickData.LastTradePrice}, parsing took {(parsedTime - receivedTime).TotalMilliseconds:F2}ms",
                            NinjaTrader.Cbi.LogLevel.Information);

                        // Use the QAAdapter's ProcessParsedTick method to update NinjaTrader with all available market data
                        // This is similar to how NimbleMain.UpdateMarketData works in the NimbleData project
                        QAAdapter adapter = Connector.Instance.GetAdapter() as QAAdapter;
                        if (adapter != null)
                        {
                            NinjaTrader.NinjaScript.NinjaScript.Log(
                                $"[WS-PROCESS] Calling ProcessParsedTick for {nativeSymbolName}",
                                NinjaTrader.Cbi.LogLevel.Information);
                            
                            adapter.ProcessParsedTick(nativeSymbolName, tickData);
                            
                            NinjaTrader.NinjaScript.NinjaScript.Log(
                                $"[WS-PROCESS-DONE] ProcessParsedTick completed for {nativeSymbolName}, total processing time: {(DateTime.Now - receivedTime).TotalMilliseconds:F2}ms",
                                NinjaTrader.Cbi.LogLevel.Information);
                        }
                        else
                        {
                            NinjaTrader.NinjaScript.NinjaScript.Log(
                                $"[WS-PROCESS-ERROR] QAAdapter instance is null, cannot process tick for {nativeSymbolName}",
                                NinjaTrader.Cbi.LogLevel.Error);
                        }

                        // Log to TickVolumeLogger CSV
                        int currentVolume = tickData.TotalQtyTraded;
                        _lastVolumeMap.TryGetValue(nativeSymbolName, out int previousVolume);
                        int volumeDelta = currentVolume - previousVolume;
                        _lastVolumeMap[nativeSymbolName] = currentVolume; // Update last volume

                        // Assuming tickData.ExchangeTimestamp is a DateTime. If it's long (Unix), it needs conversion.
                        // For now, passing as is. If ZerodhaTickData.ExchangeTimestamp is not DateTime, this will need adjustment.
                        DateTime exchangeTime = tickData.ExchangeTimestamp; // Ensure this is a valid DateTime in ZerodhaTickData
                        
                        TickVolumeLogger.LogTickVolume(
                            nativeSymbolName,
                            receivedTime,      // Time WS message was received
                            exchangeTime,      // Exchange timestamp from tick data
                            parsedTime,        // Time after parsing
                            tickData.LastTradePrice,
                            tickData.LastTradeQty,
                            currentVolume,
                            volumeDelta
                        );

                        // Log tick information periodically
                        if (_configManager.EnableVerboseTickLogging)
                        {
                            // Fire and forget the logging task
                            _ = LogTickInformationAsync(nativeSymbolName, tickData.LastTradePrice, tickData.LastTradeQty, 
                                tickData.TotalQtyTraded, tickData.LastTradeTime, receivedTime);
                        }
                    }
                    else
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[WS-PARSE-ERROR] Failed to parse tick for {nativeSymbolName}, parsing took {(parsedTime - receivedTime).TotalMilliseconds:F2}ms",
                            NinjaTrader.Cbi.LogLevel.Warning);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal teardown - no logging needed
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Error: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
            }
            finally
            {
                // Clean up resources
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await _webSocketManager.UnsubscribeAsync(ws, tokenInt);
                        await _webSocketManager.CloseAsync(ws);
                    }
                    catch { }
                }
                cts.Cancel();
                cts.Dispose();
            }
        }

        /// <summary>
        /// Subscribes to market depth for a symbol
        /// </summary>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="marketType">The market type</param>
        /// <param name="symbol">The symbol</param>
        /// <param name="l2Subscriptions">The L2 subscriptions dictionary</param>
        /// <param name="webSocketConnectionFunc">The WebSocket connection function</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SubscribeToDepth(
            string nativeSymbolName,
            MarketType marketType,
            string symbol,
            ConcurrentDictionary<string, L2Subscription> l2Subscriptions,
            WebSocketConnectionFunc webSocketConnectionFunc)
        {
            if (string.IsNullOrEmpty(symbol) || webSocketConnectionFunc == null)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH-SUBSCRIBE] Invalid parameters for {symbol}", NinjaTrader.Cbi.LogLevel.Error);
                return;
            }

            ClientWebSocket ws = null;
            var cts = new CancellationTokenSource();
            int tokenInt = (int)(await _instrumentManager.GetInstrumentToken(symbol));

            try
            {
                // Create and connect WebSocket
                ws = _webSocketManager.CreateWebSocketClient();
                await _webSocketManager.ConnectAsync(ws);

                // Subscribe in full mode to get market depth
                await _webSocketManager.SubscribeAsync(ws, tokenInt, "full");

                NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH-SUBSCRIBE] Subscribed to token {tokenInt} in full mode", NinjaTrader.Cbi.LogLevel.Information);

                // Monitor exit condition
                _ = Task.Run(async () =>
                {
                    if (webSocketConnectionFunc.IsTimeout)
                    {
                        await Task.Delay(webSocketConnectionFunc.Timeout, cts.Token);
                        cts.Cancel();
                    }
                    else
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            if (webSocketConnectionFunc.ExitFunction())
                            {
                                cts.Cancel();
                                break;
                            }
                            await Task.Delay(100, cts.Token);
                        }
                    }
                });

                // Process WebSocket messages
                var buffer = new byte[4096];
                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    // Receive message
                    WebSocketReceiveResult result = await _webSocketManager.ReceiveMessageAsync(ws, buffer, cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[DEPTH-SUBSCRIBE] WebSocket closed by server for {nativeSymbolName}. Status: {result.CloseStatus}, Description: {result.CloseStatusDescription}",
                            NinjaTrader.Cbi.LogLevel.Warning);
                        break; // Exit the loop gracefully
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                        continue; // Ignore JSON heartbeats/postbacks

                    // Process the binary message for market depth
                    ProcessDepthData(buffer, tokenInt, nativeSymbolName, l2Subscriptions);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH-SUBSCRIBE] Error: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
            }
            finally
            {
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await _webSocketManager.UnsubscribeAsync(ws, tokenInt);
                        await _webSocketManager.CloseAsync(ws);
                    }
                    catch { }
                }
                cts.Cancel();
                cts.Dispose();
            }
        }

        /// <summary>
        /// Processes market depth data
        /// </summary>
        /// <param name="data">The binary data</param>
        /// <param name="tokenInt">The instrument token</param>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="l2Subscriptions">The L2 subscriptions dictionary</param>
        private void ProcessDepthData(byte[] data, int tokenInt, string nativeSymbolName,
            ConcurrentDictionary<string, L2Subscription> l2Subscriptions)
        {
            if (data.Length < 2)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log("[DEPTH PARSER] Packet too small", NinjaTrader.Cbi.LogLevel.Warning);
                return;
            }

            int offset = 0;
            int packetCount = WebSocketManager.ReadInt16BE(data, offset);
            offset += 2;

            for (int i = 0; i < packetCount; i++)
            {
                // Check if we have enough data for packet length
                if (offset + 2 > data.Length)
                    break;

                int packetLength = WebSocketManager.ReadInt16BE(data, offset);
                offset += 2;

                // Check if we have enough data for the packet content
                if (offset + packetLength > data.Length)
                    break;

                // Only process packets with valid length (we need 184 bytes for market depth)
                if (packetLength != 184)
                {
                    offset += packetLength; // Skip this packet
                    continue;
                }

                // Check if this is our subscribed token
                int iToken = WebSocketManager.ReadInt32BE(data, offset);
                if (iToken != tokenInt)
                {
                    offset += packetLength; // Skip this packet
                    continue;
                }

                // Process market depth packet
                ProcessDepthPacket(data, offset, packetLength, nativeSymbolName, l2Subscriptions);

                // Move to next packet
                offset += packetLength;
            }
        }

        /// <summary>
        /// Processes a market depth packet
        /// </summary>
        /// <param name="data">The binary data</param>
        /// <param name="offset">The offset in the data</param>
        /// <param name="packetLength">The packet length</param>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="l2Subscriptions">The L2 subscriptions dictionary</param>
        private void ProcessDepthPacket(byte[] data, int offset, int packetLength, string nativeSymbolName,
            ConcurrentDictionary<string, L2Subscription> l2Subscriptions)
        {
            try
            {
                // Get the instrument token
                int iToken = WebSocketManager.ReadInt32BE(data, offset);
                
                // Get segment information for MCX check
                string segment = _instrumentManager.GetSegmentForToken(iToken);
                bool isMcxSegment = !string.IsNullOrEmpty(segment) && segment.Equals("MCX", StringComparison.OrdinalIgnoreCase);
                
                // Parse the binary message into a rich data structure
                var tickData = _webSocketManager.ParseBinaryMessage(data, iToken, nativeSymbolName, isMcxSegment);
                
                if (tickData == null || !tickData.HasMarketDepth)
                {
                    return;
                }

                // Get the current time in Indian Standard Time
                DateTime now = DateTime.Now;
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    now = TimeZoneInfo.ConvertTime(now, tz);
                }
                catch
                {
                    // If timezone conversion fails, use local time
                }

                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[DEPTH-TIME] Using time {now:HH:mm:ss.fff} with Kind={now.Kind} for market depth updates",
                    NinjaTrader.Cbi.LogLevel.Information);

                // Update market depth in NinjaTrader
                if (l2Subscriptions.TryGetValue(nativeSymbolName, out var l2Subscription))
                {
                    for (int index = 0; index < l2Subscription.L2Callbacks.Count; ++index)
                    {
                        // Process asks (offers)
                        foreach (var ask in tickData.AskDepth)
                        {
                            if (ask != null && ask.Quantity > 0)
                            {
                                l2Subscription.L2Callbacks.Keys[index].UpdateMarketDepth(
                                    MarketDataType.Ask, ask.Price, ask.Quantity, Operation.Update, now, l2Subscription.L2Callbacks.Values[index]);
                            }
                        }

                        // Process bids
                        foreach (var bid in tickData.BidDepth)
                        {
                            if (bid != null && bid.Quantity > 0)
                            {
                                l2Subscription.L2Callbacks.Keys[index].UpdateMarketDepth(
                                    MarketDataType.Bid, bid.Price, bid.Quantity, Operation.Update, now, l2Subscription.L2Callbacks.Values[index]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PACKET] Exception: {ex.Message}",
                    NinjaTrader.Cbi.LogLevel.Error);
            }
        }

        /// <summary>
        /// Starts a task to monitor the exit condition
        /// </summary>
        /// <param name="webSocketConnectionFunc">The WebSocket connection function</param>
        /// <param name="cts">The cancellation token source</param>
        private void StartExitMonitoringTask(WebSocketConnectionFunc webSocketConnectionFunc, CancellationTokenSource cts)
        {
            // Fire-and-forget task to detect chart-close
            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (webSocketConnectionFunc.ExitFunction())
                    {
                        cts.Cancel();
                        break;
                    }
                    await Task.Delay(500, cts.Token);
                }
            });
        }

        /// <summary>
        /// Logs tick information
        /// </summary>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="lastPrice">The last traded price</param>
        /// <param name="lastQuantity">The last traded quantity</param>
        /// <param name="volume">The volume</param>
        /// <param name="timestamp">The timestamp</param>
        /// <param name="receivedTime">The time the message was received</param>
        private async Task LogTickInformationAsync(string nativeSymbolName, double lastPrice, int lastQuantity, int volume, DateTime timestamp, DateTime receivedTime)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Format the log message
                    string logMessage = string.Format(
                        "{0:HH:mm:ss.fff},{1},{2:HH:mm:ss.fff},{3:HH:mm:ss.fff},{4:HH:mm:ss.fff},{5},{6},{7}",
                        receivedTime, // System Time (when received by adapter)
                        nativeSymbolName,
                        receivedTime, // Placeholder for original received time before parsing, if available
                        timestamp,    // ExchangeTime (from tick data)
                        DateTime.Now, // ParsedTime (current time, assuming parsing is quick)
                        lastPrice,
                        lastQuantity,
                        volume);

                    // Log to NinjaTrader's log (consider if this is needed or too verbose)
                    // NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-LOG] {logMessage}", NinjaTrader.Cbi.LogLevel.Information);

                    // Append to CSV - Ensure this path is configurable and accessible
                    // string logFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", "log", "TickDataLog.csv");
                    // System.IO.File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[LOG-TICK-ERROR] Failed to log tick: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                }
            });
        }

        /// <summary>
        /// Gets the current time in Indian Standard Time
        /// </summary>
        /// <param name="dateTime">The date time</param>
        /// <returns>The date time in Indian Standard Time</returns>
        private DateTime GetIndianTime(DateTime dateTime)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return TimeZoneInfo.ConvertTime(dateTime, tz);
            }
            catch
            {
                return dateTime;
            }
        }

        /// <summary>
        /// Converts a Unix timestamp to local time
        /// </summary>
        /// <param name="unixTimestamp">The Unix timestamp</param>
        /// <returns>The local DateTime</returns>
        private DateTime UnixSecondsToLocalTime(int unixTimestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTimestamp).ToLocalTime();
        }
    }
}
