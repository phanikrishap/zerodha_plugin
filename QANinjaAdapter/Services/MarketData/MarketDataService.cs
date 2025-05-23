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
using QABrokerAPI;
using QABrokerAPI.Zerodha.Websockets; // For Tick object if it exists, or use a generic structure
using QANinjaAdapter.Models; 
using QANinjaAdapter.Models.MarketData; // Ensured for L1Subscription, MarketDataEventArgs, and MarketDataType
using QANinjaAdapter.Services.Configuration;
using QANinjaAdapter.Services.Instruments; // Corrected namespace
// using QANinjaAdapter.Services.Broker; // Commented out due to missing BrokerManager.cs
using QANinjaAdapter.Services.WebSocket; // Added for WebSocketManager

namespace QANinjaAdapter.Services.MarketData
{
    public class MarketDataService
    {
        private static readonly Lazy<MarketDataService> _instance = new Lazy<MarketDataService>(() => new MarketDataService());
        public static MarketDataService Instance => _instance.Value;

        private readonly WebSocketManager _webSocketManager;
        private readonly InstrumentManager _instrumentManager;

        // Shared WebSocket connection resources
        private static ClientWebSocket _sharedWebSocketClient;
        private static CancellationTokenSource _sharedConnectionCts;
        private static Task _sharedMessageLoopTask;
        private static bool _isSharedConnectionActive = false;
        private static readonly object _sharedConnectionLock = new object();
        private static readonly ConcurrentDictionary<string, L1Subscription> _l1Subscriptions = new ConcurrentDictionary<string, L1Subscription>();
        
        // Subscription buffering
        private class SubscriptionRequest
        {
            public int InstrumentToken { get; set; }
            public string NativeSymbolName { get; set; }
            public string Mode { get; set; } // "ltp", "quote", or "full"
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
        
        private readonly ConcurrentQueue<SubscriptionRequest> _pendingSubscriptions = new ConcurrentQueue<SubscriptionRequest>();
        private Timer _subscriptionBatchTimer;
        private readonly object _batchTimerLock = new object();
        private bool _isProcessingBatch = false;

        private MarketDataService()
        {
            _webSocketManager = WebSocketManager.Instance;
            _instrumentManager = InstrumentManager.Instance;
            
            // Initialize the subscription batch timer (100ms interval)
            _subscriptionBatchTimer = new Timer(ProcessPendingSubscriptions, null, 100, 100);
        }
        
        /// <summary>
        /// Processes pending subscription requests in batches
        /// </summary>
        private void ProcessPendingSubscriptions(object state)
        {
            if (_pendingSubscriptions.IsEmpty || _isProcessingBatch)
                return;
                
            lock (_batchTimerLock)
            {
                if (_isProcessingBatch) return;
                _isProcessingBatch = true;
            }
            
            try
            {
                // Group pending subscriptions by mode (ltp, quote, full)
                var ltpTokens = new List<int>();
                var quoteTokens = new List<int>();
                var fullTokens = new List<int>();
                var completionSources = new Dictionary<int, TaskCompletionSource<bool>>();
                
                // Process up to 50 subscriptions at a time
                int count = 0;
                while (count < 50 && _pendingSubscriptions.TryDequeue(out var request))
                {
                    count++;
                    completionSources[request.InstrumentToken] = request.CompletionSource;
                    
                    // Determine appropriate mode based on symbol name
                    if (request.NativeSymbolName == "NIFTY_I")
                    {
                        // NIFTY_I always uses full mode
                        fullTokens.Add(request.InstrumentToken);
                        Logger.Info($"Using full mode for NIFTY_I (token: {request.InstrumentToken})");
                    }
                    else if (request.Mode.ToLower() == "ltp")
                    {
                        ltpTokens.Add(request.InstrumentToken);
                    }
                    else if (request.Mode.ToLower() == "full")
                    {
                        // Honor explicit full mode request for other instruments
                        fullTokens.Add(request.InstrumentToken);
                        Logger.Info($"Using full mode for {request.NativeSymbolName} (token: {request.InstrumentToken}) as explicitly requested");
                    }
                    else
                    {
                        // Default to quote mode for all other instruments
                        quoteTokens.Add(request.InstrumentToken);
                        Logger.Info($"Using quote mode for {request.NativeSymbolName} (token: {request.InstrumentToken})");
                    }
                }
                
                // Process batches asynchronously
                if (count > 0)
                {
                    Logger.Info($"[SUBSCRIPTION-BATCH] Processing {count} subscriptions (LTP: {ltpTokens.Count}, Quote: {quoteTokens.Count}, Full: {fullTokens.Count})");
                    
                    // Fire and forget the async processing
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await EnsureSharedConnectionAsync();
                            

                            // Process each batch
                            if (ltpTokens.Count > 0)
                            {
                                await _webSocketManager.BatchSubscribeAsync(_sharedWebSocketClient, ltpTokens, "ltp");
                            }
                                 
                            if (quoteTokens.Count > 0)
                            {
                                await _webSocketManager.BatchSubscribeAsync(_sharedWebSocketClient, quoteTokens, "quote");
                            }
                                 
                            if (fullTokens.Count > 0)
                            {
                                await _webSocketManager.BatchSubscribeAsync(_sharedWebSocketClient, fullTokens, "full");
                            }
                            

                            // Mark all requests as completed
                            foreach (var token in completionSources.Keys)
                            {
                                completionSources[token].TrySetResult(true);
                            }
                            

                            Logger.Info($"[SUBSCRIPTION-BATCH] Successfully processed {count} subscriptions");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[SUBSCRIPTION-BATCH] Error processing subscription batch: {ex.Message}", ex);
                            

                            // Mark all requests as failed
                            foreach (var token in completionSources.Keys)
                            {
                                completionSources[token].TrySetException(ex);
                            }
                        }
                    });
                }
            }
            finally
            {
                lock (_batchTimerLock)
                {
                    _isProcessingBatch = false;
                }
            }
        }

        private async Task EnsureSharedConnectionAsync()
        {
            if (_isSharedConnectionActive && _sharedWebSocketClient != null && _sharedWebSocketClient.State == WebSocketState.Open)
                return;

            // Lock to prevent multiple concurrent attempts to establish the connection
            // Monitor.Enter(_sharedConnectionLock); // Replaced with lock statement for RAII
            lock (_sharedConnectionLock) // Ensures only one thread attempts to connect if multiple calls happen near-simultaneously
            {
                // Double-check after acquiring the lock
                if (_isSharedConnectionActive && _sharedWebSocketClient != null && _sharedWebSocketClient.State == WebSocketState.Open)
                    return; // return Task.CompletedTask;

                // Attempt to connect (or re-connect)
                try
                {
                    Logger.Info("[SHARED-WS] Attempting to ensure shared WebSocket connection...");
                    if (_sharedWebSocketClient != null)
                    {
                        _sharedWebSocketClient.Dispose();
                    }
                    if (_sharedConnectionCts != null)
                    {
                        _sharedConnectionCts.Cancel();
                        _sharedConnectionCts.Dispose();
                    }

                    _sharedWebSocketClient = _webSocketManager.CreateWebSocketClient();
                    _sharedConnectionCts = new CancellationTokenSource();
                    
                    Logger.Info("[SHARED-WS] Connecting shared WebSocket...");
                    // Correcting ConnectAsync call - assuming it takes only ClientWebSocket based on typical usage.
                    // If WebSocketManager.ConnectAsync has a different signature (e.g. needs a URI or CancellationToken for connection attempt), this needs to match it.
                    // await _webSocketManager.ConnectAsync(_sharedWebSocketClient); // This was an async call, should be awaited if the lock is to be async-compatible or released before await
                    // For simplicity with lock, if ConnectAsync is truly async and long, consider SemaphoreSlim.WaitAsync
                    // For now, assuming ConnectAsync can be called synchronously or is quick enough for a brief lock.
                    // To keep the lock simple, if ConnectAsync must be awaited, the lock needs to be handled differently (e.g. SemaphoreSlim)
                    // Let's assume for now the original intent was to await it outside the immediate lock or that it's quick.
                    // To keep the lock simple, if ConnectAsync must be awaited, the lock needs to be handled differently (e.g. SemaphoreSlim)
                    // The original code awaited it, so let's try to maintain that pattern if possible, but lock makes it tricky.
                    // For now, let's call it and then log, then start the task. The lock is on the setup part.
                    // The await was outside the lock in the original thought process, let's stick to that for now.
                }
                catch (Exception ex)
                {
                    Logger.Error($"[SHARED-WS] Error during initial setup for shared connection: {ex.Message} - {ex.StackTrace}", ex);
                    _isSharedConnectionActive = false; 
                    if (_sharedWebSocketClient != null) { _sharedWebSocketClient.Dispose(); _sharedWebSocketClient = null; }
                    if (_sharedConnectionCts != null) { _sharedConnectionCts.Dispose(); _sharedConnectionCts = null; }
                    // Monitor.Exit(_sharedConnectionLock); // Not needed with lock statement
                    // throw; // Re-throw if critical
                    return; // return Task.FromException(ex);
                }
                // Monitor.Exit(_sharedConnectionLock); // Not needed with lock statement
            }

            // This part should be outside the simple lock if ConnectAsync is truly blocking or long-running async
            try
            {
                await _webSocketManager.ConnectAsync(_sharedWebSocketClient); 
                Logger.Info("[SHARED-WS] Shared WebSocket connected.");

                _sharedMessageLoopTask = Task.Run(() => StartSharedMessageLoopAsync(_sharedConnectionCts.Token), _sharedConnectionCts.Token);
                _isSharedConnectionActive = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[SHARED-WS] Error ensuring shared connection: {ex.Message} - {ex.StackTrace}", ex);
                _isSharedConnectionActive = false; 
                if (_sharedWebSocketClient != null) { _sharedWebSocketClient.Dispose(); _sharedWebSocketClient = null; }
                if (_sharedConnectionCts != null) { _sharedConnectionCts.Dispose(); _sharedConnectionCts = null; }
                // throw; // Propagate if necessary
            }
        }

        private async Task StartSharedMessageLoopAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[16384]; // Larger buffer for better network efficiency
            Logger.Info("[SHARED-WS-LOOP] Starting shared message loop...");

            try
            {
                while (_sharedWebSocketClient.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await _webSocketManager.ReceiveMessageAsync(_sharedWebSocketClient, buffer, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) break;

                    // Log raw WebSocket message before any processing
                    if (result.Count > 0) // Only log if there's data
                    {
                        string rawMessageContent = "";
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            rawMessageContent = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            Logger.Debug($"[SHARED-WS-RECV-TEXT] Data: {rawMessageContent}"); // Log text messages at debug level
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            // For binary data, only log the length to avoid flooding logs
                            Logger.Debug($"[SHARED-WS-RECV-BINARY] Received binary data. Length: {result.Count}");
                        }
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Logger.Warn($"[SHARED-WS-LOOP] WebSocket closed by server. Status: {result.CloseStatus}, Desc: {result.CloseStatusDescription}");
                        _isSharedConnectionActive = false; 
                        break; 
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Just continue - we've already logged it above and don't need to process text messages
                        continue; 
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        if (result.Count == 0) continue;

                        // Timestamp message receipt immediately to reduce timing errors
                        var receivedTime = DateTime.Now;
                        
                        // Process the binary message in batch mode
                        try {
                            // Dump raw binary data for debugging if this is short enough
                            if (result.Count <= 100) 
                            {
                                string hexDump = BitConverter.ToString(buffer, 0, result.Count);
                                Logger.Debug($"[SHARED-WS-LOOP] Raw binary data: {hexDump}");
                            }
                            

                            // Handle single-byte messages (keep-alive packets)
                            if (result.Count == 1 && buffer[0] == 0)
                            {
                                Logger.Debug("[SHARED-WS-LOOP] Received keep-alive packet (0x00), ignoring");
                                continue;
                            }
                            

                            // Ensure we have at least 2 bytes for packet count
                            if (result.Count < 2)
                            {
                                Logger.Warn($"[SHARED-WS-LOOP] Binary message too short, only {result.Count} bytes");
                                continue;
                            }
                            

                            // Read the number of packets from the first 2 bytes (big-endian conversion)
                            int packetCount = (buffer[0] << 8) | buffer[1];
                            int offset = 2;

                            Logger.Debug($"[SHARED-WS-LOOP] Processing {packetCount} packets from binary message (length: {result.Count})");
                            

                            var processedTickData = new System.Collections.Generic.List<QANinjaAdapter.Models.MarketData.ZerodhaTickData>();
                            

                            // Process each packet in the message
                            for (int i = 0; i < packetCount && offset + 2 <= result.Count; i++)
                            {
                                // Need at least 2 bytes for packet length
                                if (offset + 2 > result.Count)
                                {
                                    Logger.Warn($"[SHARED-WS-LOOP] Not enough data for packet {i+1} length. Offset: {offset}, Buffer length: {result.Count}");
                                    break;
                                }
                                

                                // Read packet length (big-endian conversion)
                                int packetLength = (buffer[offset] << 8) | buffer[offset + 1];
                                offset += 2;

                                // Skip if we don't have enough data for this packet
                                if (offset + packetLength > result.Count)
                                {
                                    Logger.Warn($"[SHARED-WS-LOOP] Packet {i+1} exceeds buffer bounds. Offset: {offset}, Length: {packetLength}, Buffer length: {result.Count}");
                                    break;
                                }
                                

                                if (packetLength < 8) // We need at least 8 bytes for instrument token + LTP
                                {
                                    Logger.Warn($"[SHARED-WS-LOOP] Packet {i+1} too small ({packetLength} bytes). Skipping.");
                                    offset += packetLength;
                                    continue;
                                }

                                // Read instrument token (first 4 bytes of packet, big-endian)
                                int instrumentToken = (buffer[offset] << 24) | (buffer[offset+1] << 16) | (buffer[offset+2] << 8) | buffer[offset+3];

                                // Find the subscription for this token
                                string nativeSymbolName = null;
                                string mode = "quote"; // Default mode
                                

                                foreach (var subscription in _l1Subscriptions)
                                {
                                    if (subscription.Value.InstrumentToken == instrumentToken)
                                    {
                                        nativeSymbolName = subscription.Key;
                                        mode = subscription.Value.Mode ?? "quote";
                                        break;
                                    }
                                }

                                if (string.IsNullOrEmpty(nativeSymbolName))
                                {
                                    Logger.Debug($"[SHARED-WS-LOOP] No subscription found for token {instrumentToken}, skipping packet {i+1}");
                                    offset += packetLength; // Skip this packet
                                    continue;
                                }
                                

                                Logger.Debug($"[PARSE-DEBUG] Found matching token {instrumentToken} for {nativeSymbolName}, packet mode: LTP={mode=="ltp"}, Quote={mode=="quote"}, Full={mode=="full"}, Length={packetLength}");
                                Logger.Debug($"[PARSE-DEBUG] Packet count: {packetCount} for {nativeSymbolName}");

                                try
                                {
                                    // LTP data is at bytes 4-7 (offset+4 through offset+7)
                                    // Convert 4 bytes to float using IEEE 754 format (big-endian)
                                    int ltpBits = (buffer[offset+4] << 24) | (buffer[offset+5] << 16) | (buffer[offset+6] << 8) | buffer[offset+7];
                                    float lastTradePrice = BitConverter.ToSingle(BitConverter.GetBytes(ltpBits), 0);
                                    

                                    int lastTradeQty = 0;
                                    int totalQtyTraded = 0;
                                    

                                    // Extract additional data if in quote or full mode and we have enough bytes
                                    if ((mode == "quote" || mode == "full") && packetLength >= 44)
                                    {
                                        // Last traded quantity (bytes 8-11)
                                        lastTradeQty = (buffer[offset+8] << 24) | (buffer[offset+9] << 16) | (buffer[offset+10] << 8) | buffer[offset+11];
                                        
                                        // Volume (bytes 12-15)
                                        totalQtyTraded = (buffer[offset+12] << 24) | (buffer[offset+13] << 16) | (buffer[offset+14] << 8) | buffer[offset+15];
                                    }
                                    

                                    // Create tick data with the extracted values
                                    var tickData = new QANinjaAdapter.Models.MarketData.ZerodhaTickData
                                    {
                                        InstrumentToken = instrumentToken,
                                        LastTradePrice = lastTradePrice,
                                        LastTradeTime = DateTime.Now,
                                        LastTradeQty = lastTradeQty,
                                        TotalQtyTraded = totalQtyTraded
                                    };
                                    

                                    Logger.Debug($"[PARSE-QUOTE] {nativeSymbolName}: LTP={lastTradePrice}, LTQ={lastTradeQty}, Vol={totalQtyTraded}");
                                    

                                    long startTime = DateTime.Now.Ticks;
                                    processedTickData.Add(tickData);
                                    Logger.Debug($"[WS-PARSE-SUCCESS] Successfully parsed tick for {nativeSymbolName}, LTP: {lastTradePrice}, parsing took {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond:0.00}ms");
                                    Logger.Debug($"[PARSE-SUCCESS] {nativeSymbolName}: LTP={lastTradePrice}, LTQ={lastTradeQty}, Vol={totalQtyTraded}, Time={DateTime.Now.ToString("HH:mm:ss.fff")}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error($"[PARSE-ERROR] Error parsing data for {nativeSymbolName}: {ex.Message}");
                                }
                                

                                offset += packetLength;
                            }

                            if (processedTickData.Count > 0)
                            {
                                Logger.Info($"[SHARED-WS-LOOP] Successfully parsed {processedTickData.Count} instruments from binary message");
                                
                                // Get the QAAdapter instance for processing ticks
                                QAAdapter adapter = Connector.Instance.GetAdapter() as QAAdapter;
                                if (adapter != null)
                                {
                                    // Process each parsed tick
                                    foreach (var tickData in processedTickData)
                                    {
                                        string nativeSymbolName = null;
                                        
                                        // Find the subscription for this token
                                        foreach (var sub in _l1Subscriptions)
                                        {
                                            if (sub.Value.InstrumentToken == tickData.InstrumentToken)
                                            {
                                                nativeSymbolName = sub.Key;
                                                break;
                                            }
                                        }
                                        
                                        if (!string.IsNullOrEmpty(nativeSymbolName))
                                        {
                                            // Process the tick data using QAAdapter
                                            adapter.ProcessParsedTick(nativeSymbolName, tickData);
                                            
                                            // Also invoke the callback directly if the subscription has one
                                            if (_l1Subscriptions.TryGetValue(nativeSymbolName, out var sub) && sub.Callback != null)
                                            {
                                                try
                                                {
                                                    // Create a MarketDataEventArgs from the tick data
                                                    var marketDataEvent = new QANinjaAdapter.Models.MarketData.MarketDataEventArgs(
                                                        nativeSymbolName, 
                                                        MarketDataType.Last,  // Default to Last, can be refined based on tickData
                                                        tickData.LastTradePrice, 
                                                        tickData.LastTradeQty, 
                                                        tickData.LastTradeTime, 
                                                        0  // Market depth level 0 for L1 data
                                                    );
                                                    
                                                    // Invoke the callback
                                                    sub.Callback(marketDataEvent);
                                                }
                                                catch (Exception callbackEx)
                                                {
                                                    Logger.Error($"[SHARED-WS-LOOP] Error invoking callback for {nativeSymbolName}: {callbackEx.Message}", callbackEx);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.Error("[SHARED-WS-LOOP] QAAdapter instance is null, cannot process ticks");
                                }
                            }
                            else
                            {
                                Logger.Debug("[SHARED-WS-LOOP] No instruments parsed from binary message");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[SHARED-WS-LOOP] Error processing binary message: {ex.Message}", ex);
                        }
                    }
                }
            }
            // Corrected WebSocketException handling for cancellation
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                 Logger.Info("[SHARED-WS-LOOP] WebSocket operation aborted due to cancellation (expected during shutdown).");
            }
            catch (WebSocketException wsEx)
            {
                Logger.Error($"[SHARED-WS-LOOP] WebSocketException: {wsEx.Message}. ErrorCode: {wsEx.WebSocketErrorCode}", wsEx);
                _isSharedConnectionActive = false; 
            }
            // Removed the redundant OperationCanceledException catch block as the one above is more specific.
            catch (Exception ex)
            {
                Logger.Error($"[SHARED-WS-LOOP] Unhandled exception: {ex.Message} - {ex.StackTrace}", ex);
                _isSharedConnectionActive = false; 
            }
            finally
            {
                Logger.Info("[SHARED-WS-LOOP] Exiting shared message loop.");
                if (!cancellationToken.IsCancellationRequested) _isSharedConnectionActive = false; 
            }
        }

        /// <summary>
        /// Subscribes to real-time ticks for a symbol using the shared WebSocket connection.
        /// </summary>
        public async Task SubscribeToTicks(
            string nativeSymbolName,      // e.g., from NinjaTrader Instrument object
            string symbol,                // Symbol for token lookup (e.g., NIFTY24MAYFUT)
            Action<QANinjaAdapter.Models.MarketData.MarketDataEventArgs> tickCallback, // Fully qualified
            Func<bool> isSubscriptionActiveCheck)
        {
            if (string.IsNullOrEmpty(nativeSymbolName) || string.IsNullOrEmpty(symbol) || tickCallback == null || isSubscriptionActiveCheck == null)
            {
                Logger.Error($"[TICK-SUBSCRIBE-SHARED] Invalid parameters for {nativeSymbolName ?? symbol}");
                return;
            }

            try
            {
                await EnsureSharedConnectionAsync();

                int tokenInt = (int)(await _instrumentManager.GetInstrumentToken(symbol));
                if (tokenInt == 0)
                {
                    Logger.Error($"[TICK-SUBSCRIBE-SHARED] Could not get instrument token for {symbol}");
                    return;
                }
                
                // Create the subscription object
                var l1Sub = new L1Subscription(nativeSymbolName, symbol, tickCallback, isSubscriptionActiveCheck) 
                { 
                    InstrumentToken = tokenInt 
                };
                
                // Determine the appropriate mode based on the instrument type
                string mode = "quote"; // Default to quote mode for most instruments
                
                if (symbol.Contains("NIFTY_I") || symbol.EndsWith("_I"))
                {
                    // NIFTY_I and other indices (ending with _I) should use full mode
                    mode = "full";
                    Logger.Info($"[TICK-SUBSCRIBE-SHARED] Using FULL mode for index {symbol}");
                }
                else
                {
                    // All other instruments use quote mode
                    Logger.Info($"[TICK-SUBSCRIBE-SHARED] Using QUOTE mode for instrument {symbol}");
                }
                
                // Set the exchange - parse from the symbol format (e.g., "NSE:NIFTY")
                string exchange = "NSE"; // Default exchange
                if (symbol.Contains(":"))
                {
                    string[] parts = symbol.Split(':');
                    if (parts.Length > 1) 
                    {
                        exchange = parts[0];
                    }
                }
                
                // Update the subscription with the exchange and mode
                l1Sub.Exchange = exchange;
                l1Sub.Mode = mode;
                
                // Add or update the subscription in the dictionary
                _l1Subscriptions.AddOrUpdate(nativeSymbolName, l1Sub, (key, oldSub) => l1Sub);
                
                Logger.Info($"[TICK-SUBSCRIBE-SHARED] Registering subscription for {nativeSymbolName} (Token: {tokenInt}). Queueing subscription...");
                
                // Use the batch subscription system
                bool result = await SubscribeAsync(symbol, exchange, tokenInt, MarketDataType.Last, mode);
                
                if (result)
                {
                    Logger.Info($"[TICK-SUBSCRIBE-SHARED] Successfully queued subscription for {nativeSymbolName} (Token: {tokenInt}) with mode {mode}");
                }
                else
                {
                    Logger.Error($"[TICK-SUBSCRIBE-SHARED] Failed to queue subscription for {nativeSymbolName} (Token: {tokenInt})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TICK-SUBSCRIBE-SHARED] Error subscribing {nativeSymbolName}: {ex.Message}", ex);
            }
        }

        public async Task UnsubscribeFromTicks(string nativeSymbolName)
        {
            if (string.IsNullOrEmpty(nativeSymbolName))
            {
                Logger.Warn("[TICK-UNSUBSCRIBE-SHARED] Native symbol name is null or empty.");
                return;
            }

            if (_l1Subscriptions.TryGetValue(nativeSymbolName, out var sub))
            {
                if (sub.InstrumentToken != 0 && _sharedWebSocketClient != null && _sharedWebSocketClient.State == WebSocketState.Open)
                {
                    try
                    {
                        Logger.Info($"[TICK-UNSUBSCRIBE-SHARED] Unsubscribing from token {sub.InstrumentToken} ({nativeSymbolName}) on shared WebSocket.");
                        
                        // Use the UnsubscribeAsync method
                        string symbol = sub.OriginalSymbol;
                        string exchange = sub.Exchange ?? "NSE";
                        
                        bool result = await UnsubscribeAsync(symbol, exchange, sub.InstrumentToken);
                        
                        if (result)
                        {
                            Logger.Info($"[TICK-UNSUBSCRIBE-SHARED] Successfully unsubscribed from {nativeSymbolName} (Token: {sub.InstrumentToken}).");
                            // Remove from the subscriptions dictionary only after successful unsubscription
                            _l1Subscriptions.TryRemove(nativeSymbolName, out _);
                        }
                        else
                        {
                            Logger.Error($"[TICK-UNSUBSCRIBE-SHARED] Failed to unsubscribe from {nativeSymbolName} (Token: {sub.InstrumentToken}).");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[TICK-UNSUBSCRIBE-SHARED] Error sending unsubscription for {nativeSymbolName}: {ex.Message}", ex);
                    }
                }
                else
                {
                    // If we can't unsubscribe properly (e.g., WebSocket is closed), just remove it from our dictionary
                    _l1Subscriptions.TryRemove(nativeSymbolName, out _);
                    Logger.Info($"[TICK-UNSUBSCRIBE-SHARED] Removed subscription for {nativeSymbolName} without WebSocket unsubscription.");
                }
            }
            else
            {
                Logger.Info($"[TICK-UNSUBSCRIBE-SHARED] No subscription found for {nativeSymbolName}.");
            }
        }

        /// <summary>
        /// Subscribes to market data for the specified symbol and mode using the batch subscription system
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="exchange">Exchange code</param>
        /// <param name="instrumentToken">Instrument token</param>
        /// <param name="dataType">Market data type</param>
        /// <param name="mode">Subscription mode (ltp, quote, or full)</param>
        /// <returns>True if the subscription was successful, false otherwise</returns>
        public async Task<bool> SubscribeAsync(string symbol, string exchange, int instrumentToken, MarketDataType dataType, string mode = "quote")
        {
            try
            {
                // Get or create a new subscription instance
                var subscription = _l1Subscriptions.GetOrAdd($"{exchange}:{symbol}", _ => new L1Subscription
                {
                    InstrumentToken = instrumentToken,
                    Exchange = exchange,
                    OriginalSymbol = symbol
                });

                // Don't subscribe if already subscribed
                if (subscription.IsActive)
                    return true;

                // Ensure the shared WebSocket connection is active
                await EnsureSharedConnectionAsync();

                // Create a task completion source to track the subscription result
                var subscriptionTcs = new TaskCompletionSource<bool>();
                
                // Add this subscription to the pending queue
                _pendingSubscriptions.Enqueue(new SubscriptionRequest
                {
                    InstrumentToken = instrumentToken,
                    NativeSymbolName = $"{exchange}:{symbol}",
                    Mode = mode,
                    CompletionSource = subscriptionTcs
                });
                
                Logger.Info($"[SUBSCRIBE] Queued subscription for {exchange}:{symbol} ({instrumentToken}) with mode {mode}");
                
                // Wait for the subscription to be processed
                bool result = await subscriptionTcs.Task;
                
                // Mark as active if successful
                if (result)
                {
                    subscription.IsActive = true;
                    subscription.Mode = mode;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error subscribing to {exchange}:{symbol} - {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Unsubscribes from market data for the specified symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="exchange">Exchange code</param>
        /// <param name="instrumentToken">Instrument token</param>
        /// <returns>True if the unsubscription was successful, false otherwise</returns>
        public async Task<bool> UnsubscribeAsync(string symbol, string exchange, int instrumentToken)
        {
            try
            {
                // Check if the subscription exists
                if (!_l1Subscriptions.TryGetValue($"{exchange}:{symbol}", out var subscription) || !subscription.IsActive)
                    return true; // Already unsubscribed

                // Send the unsubscribe message - we don't batch unsubscribes for now as they're less frequent
                await _webSocketManager.UnsubscribeAsync(_sharedWebSocketClient, instrumentToken);

                // Mark as inactive
                subscription.IsActive = false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error unsubscribing from {exchange}:{symbol} - {ex.Message}", ex);
                return false;
            }
        }

        public static async Task ShutdownAsync()
        {
            Logger.Info("[SHARED-WS] ShutdownAsync called.");
            if (_sharedConnectionCts != null)
            {
                if (!_sharedConnectionCts.IsCancellationRequested) _sharedConnectionCts.Cancel();
            }

            if (_sharedMessageLoopTask != null)
            {
                try { await Task.WhenAny(_sharedMessageLoopTask, Task.Delay(TimeSpan.FromSeconds(5))); } // Wait for loop with timeout
                catch (OperationCanceledException) { /* Expected */ }
                catch (Exception ex) { Logger.Error($"[SHARED-WS] Exception waiting for message loop: {ex.Message}", ex); }
            }

            if (_sharedWebSocketClient != null)
            {
                if (_sharedWebSocketClient.State == WebSocketState.Open || _sharedWebSocketClient.State == WebSocketState.CloseReceived)
                {
                    try { await _sharedWebSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Shutting down", CancellationToken.None); }
                    catch (Exception ex) { Logger.Error($"[SHARED-WS] Exception during CloseOutputAsync: {ex.Message}", ex); }
                }
                _sharedWebSocketClient.Dispose();
                _sharedWebSocketClient = null;
            }
            if (_sharedConnectionCts != null) { _sharedConnectionCts.Dispose(); _sharedConnectionCts = null; }
            _isSharedConnectionActive = false;
            Logger.Info("[SHARED-WS] Shutdown complete.");
        }

        // The old SubscribeToTicks method (with WebSocketConnectionFunc parameter) should be reviewed and removed/refactored later.
        // ... (rest of the old methods, if any, can be cleaned up in a subsequent step)
        // For example, the old SubscribeToTicks and ProcessWebSocketMessagesAsync methods that created individual connections.

        // Old methods that are being replaced by the shared connection logic:
        // public async Task SubscribeToTicks(...)
        // private async Task ProcessWebSocketMessagesAsync(...)
        // public async Task SubscribeToMarketDepth(...)
        // private async Task ProcessMarketDepthMessagesAsync(...)
        // public void UpdateMarketData(...)
        // public void LogTickDataToFile(...)

    }
}
