using System;
using System.Collections.Concurrent;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Models.MarketData;

namespace QANinjaAdapter.Services.MarketData.Processing
{
    /// <summary>
    /// Handles logging tick data to CSV files
    /// </summary>
    public class CsvTickLogger
    {
        // Dictionary to track last volume for each symbol to calculate volume delta
        private readonly ConcurrentDictionary<string, int> _lastVolumeMap = new ConcurrentDictionary<string, int>();
        
        /// <summary>
        /// Logs tick data to CSV file
        /// </summary>
        /// <param name="symbol">The symbol to log data for</param>
        /// <param name="parsedData">The parsed tick data</param>
        public void LogTickToCSV(string symbol, ParsedTickData parsedData)
        {
            try
            {
                // Log all input parameters for debugging
                AppLogger.Log($"LogTickToCSV called with: Symbol={symbol ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                if (parsedData != null)
                {
                    AppLogger.Log($"ParsedData for CSV: Token={parsedData.InstrumentToken}, LastPrice={parsedData.LastPrice}, Volume={parsedData.Volume}, Mode={parsedData.Mode ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                }
                else
                {
                    AppLogger.Log("ParsedData for CSV is NULL", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Validate input
                if (string.IsNullOrEmpty(symbol))
                {
                    AppLogger.Log($"Cannot log tick data: Symbol is null or empty", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Calculate volume delta if we have previous volume data
                int volumeDelta = 0;
                if (_lastVolumeMap.TryGetValue(symbol, out int lastVolume))
                {
                    volumeDelta = parsedData.Volume - lastVolume;
                    if (volumeDelta < 0) volumeDelta = 0; // Ensure delta is not negative
                }
                
                // Update the last volume for this symbol
                _lastVolumeMap[symbol] = parsedData.Volume;
                
                // Get the timestamp
                DateTime timestamp = parsedData.Timestamp != DateTime.MinValue ? parsedData.Timestamp : DateTime.Now;
                
                // Log to CSV
                AppLogger.Log($"Logging tick to CSV for {symbol}: LTP={parsedData.LastPrice}, Vol={parsedData.Volume}, Delta={volumeDelta}", QANinjaAdapter.Logging.LogLevel.Debug);
                
                // Use the TickVolumeLogger to log the tick data
                TickVolumeLogger.LogTickVolume(
                    symbol,
                    DateTime.Now,  // Received time
                    timestamp,     // Exchange time
                    DateTime.Now,  // Parsed time
                    parsedData.LastPrice,
                    parsedData.LastQuantity,
                    parsedData.Volume,
                    volumeDelta
                );
                
                AppLogger.Log($"Successfully logged tick to CSV for {symbol}", QANinjaAdapter.Logging.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error logging tick to CSV: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
    }
}
