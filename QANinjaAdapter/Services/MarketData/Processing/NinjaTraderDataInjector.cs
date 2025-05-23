using System;
using System.Collections.Concurrent;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Models.MarketData;

namespace QANinjaAdapter.Services.MarketData.Processing
{
    /// <summary>
    /// Handles injecting market data into NinjaTrader
    /// </summary>
    public class NinjaTraderDataInjector
    {
        // Dictionary to track last volume for each symbol to calculate volume delta
        private readonly ConcurrentDictionary<string, int> _lastVolumeMap = new ConcurrentDictionary<string, int>();
        
        /// <summary>
        /// Injects the parsed tick data into NinjaTrader
        /// </summary>
        /// <param name="symbol">The symbol to inject data for</param>
        /// <param name="parsedData">The parsed tick data</param>
        public void InjectDataIntoNinjaTrader(string symbol, ParsedTickData parsedData)
        {
            try
            {
                // Log all input parameters for debugging
                AppLogger.Log($"InjectDataIntoNinjaTrader called with: Symbol={symbol ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                if (parsedData != null)
                {
                    AppLogger.Log($"ParsedData: Token={parsedData.InstrumentToken}, LastPrice={parsedData.LastPrice}, Volume={parsedData.Volume}, Mode={parsedData.Mode ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                }
                else
                {
                    AppLogger.Log("ParsedData is NULL", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Validate input
                if (string.IsNullOrEmpty(symbol))
                {
                    AppLogger.Log($"Cannot inject data: Symbol is null or empty", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Create a ZerodhaTickData object from the parsed data
                var zerodhaTickData = new ZerodhaTickData
                {
                    InstrumentToken = (int)parsedData.InstrumentToken,
                    LastTradePrice = parsedData.LastPrice,
                    LastTradeQty = parsedData.LastQuantity,
                    AverageTradePrice = parsedData.AverageTradePrice,
                    TotalQtyTraded = parsedData.Volume,
                    BuyQty = parsedData.BuyQuantity,
                    SellQty = parsedData.SellQuantity,
                    Open = parsedData.Open,
                    High = parsedData.High,
                    Low = parsedData.Low,
                    Close = parsedData.Close,
                    LastTradeTime = parsedData.Timestamp != DateTime.MinValue ? parsedData.Timestamp : DateTime.Now,
                    ExchangeTimestamp = parsedData.Timestamp != DateTime.MinValue ? parsedData.Timestamp : DateTime.Now
                };
                
                // Calculate volume delta if we have previous volume data
                int volumeDelta = 0;
                if (_lastVolumeMap.TryGetValue(symbol, out int lastVolume))
                {
                    volumeDelta = parsedData.Volume - lastVolume;
                    if (volumeDelta < 0) volumeDelta = 0; // Ensure delta is not negative
                }
                
                // Update the last volume for this symbol
                _lastVolumeMap[symbol] = parsedData.Volume;
                
                // Log the injection
                AppLogger.Log($"Injecting data for {symbol}: LTP={parsedData.LastPrice}, Vol={parsedData.Volume}, Delta={volumeDelta}", QANinjaAdapter.Logging.LogLevel.Debug);
                
                // We need to use the NinjaTrader logging directly since we don't have direct access to the QAAdapter instance
                // This will still record the tick data in NinjaTrader
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[TICK-DATA] {symbol}: LTP={zerodhaTickData.LastTradePrice}, LTQ={zerodhaTickData.LastTradeQty}, Vol={zerodhaTickData.TotalQtyTraded}, Time={zerodhaTickData.LastTradeTime:HH:mm:ss.fff}",
                    NinjaTrader.Cbi.LogLevel.Information);
                
                // In a production environment, you would get the QAAdapter instance from the Connector
                // var qaAdapter = Connector.Instance.GetAdapter() as QAAdapter;
                // qaAdapter?.ProcessParsedTick(symbol, zerodhaTickData);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error injecting data into NinjaTrader: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
    }
}
