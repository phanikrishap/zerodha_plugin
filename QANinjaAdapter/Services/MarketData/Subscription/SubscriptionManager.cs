using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Models.MarketData;
using QANinjaAdapter.Services.MarketData.Connection;
using QANinjaAdapter.Services.MarketData.Processing;
using QANinjaAdapter.Services.Configuration;
using QANinjaAdapter.Services.Instruments;
using WebSocketConnectionManagerNS = QANinjaAdapter.Services.WebSocket.WebSocketConnectionManager;

namespace QANinjaAdapter.Services.MarketData.Subscription
{
    public class SubscriptionManager : IDisposable
    {
        private readonly QANinjaAdapter.Services.MarketData.Connection.WebSocketConnectionManager _connectionManager;
        private readonly DataProcessingService _dataProcessor;
        private readonly BatchSubscriptionManager _batchManager;
        private readonly ConcurrentDictionary<string, L1Subscription> _subscriptions = new ConcurrentDictionary<string, L1Subscription>();
        private readonly InstrumentManager _instrumentManager;
        private bool _isDisposed = false;
        private readonly object _lock = new object();
        
        public SubscriptionManager(QANinjaAdapter.Services.MarketData.Connection.WebSocketConnectionManager connectionManager, DataProcessingService dataProcessor)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _dataProcessor = dataProcessor ?? throw new ArgumentNullException(nameof(dataProcessor));
            _instrumentManager = InstrumentManager.Instance;
            
            // Initialize batch manager
            try
            {
                var config = ConfigurationManager.Instance;
                string webSocketUrl = "wss://ws.kite.trade/";
                string apiKey = config.ApiKey;
                string accessToken = config.AccessToken;
                
                if (!string.IsNullOrEmpty(webSocketUrl) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(accessToken))
                {
                    _batchManager = new BatchSubscriptionManager(QANinjaAdapter.Services.WebSocket.WebSocketManager.Instance, webSocketUrl, apiKey, accessToken);
                    // Initialize the batch manager immediately
                    _ = _batchManager.InitializeAsync();
                    AppLogger.Log(LoggingLevel.Information, "BatchSubscriptionManager initialized successfully");
                }
                else
                {
                    AppLogger.Log(LoggingLevel.Error, "Failed to initialize BatchSubscriptionManager: Missing configuration values");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error initializing BatchSubscriptionManager: {ex.Message}", ex);
            }
            
            // Subscribe to connection manager events
            _connectionManager.MessageReceived += OnMessageReceived;
        }
        
        private void OnMessageReceived(object sender, byte[] data)
        {
            if (_isDisposed) return;
            
            try
            {
                // Forward to data processor
                _dataProcessor.ProcessMessage(data, _subscriptions);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error processing WebSocket message: {ex.Message}", ex);
            }
        }
        
        public async Task<bool> Subscribe(string symbol, string exchange, Action<MarketDataEventArgs> callback)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SubscriptionManager));
                
            string key = $"{exchange}:{symbol}";
            
            try
            {
                AppLogger.Info($"Subscribing to {symbol} on {exchange}");
                
                // Get instrument token
                long tokenLong = await _instrumentManager.GetInstrumentToken(symbol);
                int tokenInt = (int)tokenLong;
                
                if (tokenInt <= 0)
                {
                    AppLogger.Error($"Failed to get token for {symbol}");
                    return false;
                }
                
                // Determine subscription mode based on symbol
                string mode = DetermineSubscriptionMode(symbol);
                
                // Create subscription object
                var subscription = new L1Subscription
                {
                    InstrumentToken = tokenInt,
                    Callback = callback,
                    Mode = mode,
                    OriginalSymbol = symbol,
                    Exchange = exchange
                };
                
                // Add to subscriptions dictionary
                _subscriptions[key] = subscription;
                
                // Ensure connection is active
                bool connectionResult = await _connectionManager.EnsureConnectionAsync();
                if (!connectionResult)
                {
                    AppLogger.Error($"Failed to ensure WebSocket connection for {symbol}");
                    return false;
                }
                
                // Use batch subscription manager if available
                if (_batchManager != null)
                {
                    lock (_lock)
                    {
                        if (_isDisposed) return false;
                        
                        _batchManager.QueueInstrumentSubscription(tokenInt, symbol, mode);
                        AppLogger.Info($"Queued {symbol} subscription with token {tokenInt} in {mode} mode");
                    }
                    return true;
                }
                else
                {
                    // Fallback to direct subscription
                    AppLogger.Warning($"BatchSubscriptionManager not available, using direct subscription for {symbol}");
                    
                    try {
                        // Format subscription message according to Zerodha WebSocket API
                        string subscribeMsg = $"{{\"a\":\"subscribe\",\"v\":[{tokenInt}]}}";
                        byte[] subscribeData = System.Text.Encoding.UTF8.GetBytes(subscribeMsg);
                        
                        // Send subscription message
                        bool subResult = await _connectionManager.SendMessageAsync(subscribeData);
                        if (!subResult) {
                            AppLogger.Error($"Failed to send subscription message for {symbol}");
                            return false;
                        }
                        
                        // Set mode for the instrument
                        string modeMsg = $"{{\"a\":\"mode\",\"v\":[\"{mode}\",[{tokenInt}]]}}";
                        byte[] modeData = System.Text.Encoding.UTF8.GetBytes(modeMsg);
                        
                        // Send mode message
                        bool modeResult = await _connectionManager.SendMessageAsync(modeData);
                        if (!modeResult) {
                            AppLogger.Error($"Failed to send mode message for {symbol}");
                            return false;
                        }
                        
                        AppLogger.Info($"Successfully sent direct subscription for {symbol} with token {tokenInt} in {mode} mode");
                        return true;
                    } catch (Exception ex) {
                        AppLogger.Error($"Error in direct subscription for {symbol}: {ex.Message}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error subscribing to {symbol}: {ex.Message}", ex);
                return false;
            }
        }
        
        private string DetermineSubscriptionMode(string symbol)
        {
            // Always use full mode for all symbols to ensure complete market data
            return "full";
        }
        
        public Task<bool> Unsubscribe(string symbol, string exchange)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SubscriptionManager));
                
            string key = $"{exchange}:{symbol}";
            
            try
            {
                if (!_subscriptions.TryRemove(key, out var subscription))
                {
                    AppLogger.Warning($"No subscription found for {symbol}");
                    return Task.FromResult(false);
                }
                
                AppLogger.Info($"Unsubscribing from {symbol} on {exchange}");
                
                // Implement unsubscription logic
                // This could involve sending an unsubscribe message through the WebSocket connection
                // For Zerodha, this might mean sending a mode change message or explicit unsubscribe
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error unsubscribing from {symbol}: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            lock (_lock)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                
                try
                {
                    // Unsubscribe from events
                    _connectionManager.MessageReceived -= OnMessageReceived;
                    
                    // Dispose batch manager
                    _batchManager?.Dispose();
                    
                    // Clear subscriptions
                    _subscriptions.Clear();
                    
                    AppLogger.Info("SubscriptionManager disposed");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Error during SubscriptionManager disposal: {ex.Message}", ex);
                }
            }
        }
        
        // Get all active subscriptions
        public ConcurrentDictionary<string, L1Subscription> GetActiveSubscriptions()
        {
            return new ConcurrentDictionary<string, L1Subscription>(_subscriptions);
        }
    }
}
