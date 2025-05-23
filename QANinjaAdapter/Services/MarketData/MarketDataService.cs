using System;
using System.Threading.Tasks;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using QANinjaAdapter.Models.MarketData;
using QANinjaAdapter.Services.MarketData.Subscription;
using QANinjaAdapter.Services.MarketData.Connection;
using QANinjaAdapter.Services.MarketData.Processing;
using QANinjaAdapter.Logging;

namespace QANinjaAdapter.Services.MarketData
{
    /// <summary>
    /// Service for handling market data subscriptions using Zerodha WebSocket API.
    /// Implements a shared WebSocket connection architecture for efficient market data delivery.
    /// </summary>
    public class MarketDataService : IDisposable
    {
        private static readonly Lazy<MarketDataService> _instance = new Lazy<MarketDataService>(() => new MarketDataService());
        public static MarketDataService Instance => _instance.Value;
        
        // Service components
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly DataProcessingService _dataProcessor;
        private bool _isInitialized = false;
        private readonly object _initLock = new object();
        private bool _isDisposed = false;
        
        private MarketDataService()
        {
            try
            {
                // Initialize components
                _dataProcessor = new DataProcessingService();
                _connectionManager = new WebSocketConnectionManager();
                _subscriptionManager = new SubscriptionManager(_connectionManager, _dataProcessor);
                _isInitialized = true;
                
                AppLogger.Log("MarketDataService initialized successfully", QANinjaAdapter.Logging.LogLevel.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error initializing MarketDataService: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Subscribes to market data for the specified symbol.
        /// </summary>
        /// <param name="nativeSymbolName">Native symbol name</param>
        /// <param name="symbol">Symbol name</param>
        /// <param name="tickCallback">Callback to receive market data updates</param>
        /// <param name="isSubscriptionActiveCheck">Optional function to check if subscription is still active</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SubscribeToTicks(
            string nativeSymbolName, 
            string symbol, 
            Action<MarketDataEventArgs> tickCallback,
            Func<bool> isSubscriptionActiveCheck = null)
        {
            if (!EnsureInitialized())
                return;
                
            if (string.IsNullOrEmpty(nativeSymbolName) || string.IsNullOrEmpty(symbol) || tickCallback == null)
            {
                AppLogger.Log($"Invalid parameters for subscription: {nativeSymbolName ?? symbol}", QANinjaAdapter.Logging.LogLevel.Error);
                return;
            }
            
            try
            {
                // Extract exchange from nativeSymbolName if it contains exchange information
                string exchange = "NSE"; // Default exchange
                if (nativeSymbolName.Contains(":"))
                {
                    var parts = nativeSymbolName.Split(':');
                    if (parts.Length >= 2)
                    {
                        exchange = parts[0];
                        nativeSymbolName = parts[1];
                    }
                }
                
                await _subscriptionManager.Subscribe(symbol, exchange, tickCallback);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error subscribing to ticks for {nativeSymbolName}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Unsubscribes from market data for the specified symbol.
        /// </summary>
        /// <param name="nativeSymbolName">Native symbol name</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UnsubscribeFromTicks(string nativeSymbolName)
        {
            if (!EnsureInitialized() || string.IsNullOrEmpty(nativeSymbolName))
                return;
                
            try
            {
                // Extract exchange from nativeSymbolName if it contains exchange information
                string exchange = "NSE"; // Default exchange
                string symbol = nativeSymbolName;
                
                if (nativeSymbolName.Contains(":"))
                {
                    var parts = nativeSymbolName.Split(':');
                    if (parts.Length >= 2)
                    {
                        exchange = parts[0];
                        symbol = parts[1];
                    }
                }
                
                await _subscriptionManager.Unsubscribe(symbol, exchange);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error unsubscribing from ticks for {nativeSymbolName}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Ensures the service is properly initialized.
        /// </summary>
        /// <returns>True if initialized, false otherwise</returns>
        private bool EnsureInitialized()
        {
            if (_isDisposed)
            {
                AppLogger.Log("Cannot perform operation: MarketDataService is disposed", QANinjaAdapter.Logging.LogLevel.Error);
                return false;
            }
            
            if (!_isInitialized)
            {
                AppLogger.Log("MarketDataService is not properly initialized", QANinjaAdapter.Logging.LogLevel.Error);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Cleans up resources used by the MarketDataService.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            lock (_initLock)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                
                try
                {
                    // Dispose all components
                    _subscriptionManager?.Dispose();
                    _connectionManager?.Dispose();
                    
                    AppLogger.Log("MarketDataService disposed", QANinjaAdapter.Logging.LogLevel.Information);
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error during MarketDataService disposal: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                }
            }
        }
        
        /// <summary>
        /// Shuts down the market data service.
        /// </summary>
        public void Shutdown()
        {
            Dispose();
        }
    }
}
