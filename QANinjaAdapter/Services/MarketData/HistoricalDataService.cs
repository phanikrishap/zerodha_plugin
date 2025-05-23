using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using QABrokerAPI.Common.Enums;
using QANinjaAdapter.Classes;
using QANinjaAdapter.Services.Instruments;
using QANinjaAdapter.Services.Zerodha;
using QANinjaAdapter.ViewModels;

namespace QANinjaAdapter.Services.MarketData
{
    /// <summary>
    /// Service for retrieving historical market data from Zerodha
    /// </summary>
    public class HistoricalDataService
    {
        private static HistoricalDataService _instance;
        private readonly ZerodhaClient _zerodhaClient;
        private readonly InstrumentManager _instrumentManager;

        /// <summary>
        /// Gets the singleton instance of the HistoricalDataService
        /// </summary>
        public static HistoricalDataService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HistoricalDataService();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private HistoricalDataService()
        {
            _zerodhaClient = ZerodhaClient.Instance;
            _instrumentManager = InstrumentManager.Instance;
        }

        /// <summary>
        /// Gets historical trades for a symbol
        /// </summary>
        /// <param name="barsPeriodType">The bars period type</param>
        /// <param name="symbol">The symbol</param>
        /// <param name="fromDate">The start date</param>
        /// <param name="toDate">The end date</param>
        /// <param name="marketType">The market type</param>
        /// <param name="viewModelBase">The view model for progress updates</param>
        /// <returns>A list of historical records</returns>
        public async Task<List<Record>> GetHistoricalTrades(
            BarsPeriodType barsPeriodType,
            string symbol,
            DateTime fromDate,
            DateTime toDate,
            MarketType marketType,
            ViewModelBase viewModelBase)
        {
            // Log request parameters
            NinjaTrader.NinjaScript.NinjaScript.Log($"Getting historical data for {symbol}, period: {barsPeriodType}, market type: {marketType}, dates: {fromDate} to {toDate}", NinjaTrader.Cbi.LogLevel.Information);

            List<Record> records = new List<Record>();

            try
            {
                // For Zerodha, we need to format the request correctly
                if (barsPeriodType != BarsPeriodType.Tick)
                {
                    // Get the instrument token
                    long instrumentToken = await _instrumentManager.GetInstrumentToken(symbol);

                    if (instrumentToken == 0)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log($"Error: Could not find instrument token for {symbol}", NinjaTrader.Cbi.LogLevel.Error);
                        return records;
                    }

                    string fromDateStr = fromDate.ToString("yyyy-MM-dd HH:mm:ss");
                    string toDateStr = toDate.ToString("yyyy-MM-dd HH:mm:ss");
                    
                    // Determine interval string based on BarsPeriodType
                    string interval = "day";
                    if (barsPeriodType == BarsPeriodType.Minute)
                    {
                        interval = "minute";
                    }

                    // Get historical data
                    records = await GetHistoricalDataChunk(instrumentToken, interval, fromDateStr, toDateStr);
                }
                else
                {
                    // Handle tick data if needed
                    NinjaTrader.NinjaScript.NinjaScript.Log("Tick data not supported for Zerodha", NinjaTrader.Cbi.LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"Exception in GetHistoricalTrades: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
            }

            NinjaTrader.NinjaScript.NinjaScript.Log($"Returning {records.Count} historical records", NinjaTrader.Cbi.LogLevel.Information);
            return records;
        }

        /// <summary>
        /// Gets a chunk of historical data from Zerodha
        /// </summary>
        /// <param name="instrumentToken">The instrument token</param>
        /// <param name="interval">The interval (day, minute, etc.)</param>
        /// <param name="fromDateStr">The start date string</param>
        /// <param name="toDateStr">The end date string</param>
        /// <returns>A list of historical records</returns>
        private async Task<List<Record>> GetHistoricalDataChunk(long instrumentToken, string interval, string fromDateStr, string toDateStr)
        {
            List<Record> records = new List<Record>();

            using (HttpClient client = _zerodhaClient.CreateAuthorizedClient())
            {
                // Format the URL
                string url = $"https://api.kite.trade/instruments/historical/{instrumentToken}/{interval}?from={fromDateStr}&to={toDateStr}";
                
                // Make the request
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Received response with length: {content.Length} from {fromDateStr} to {toDateStr}", NinjaTrader.Cbi.LogLevel.Information);

                    // Parse the JSON response
                    JObject json = JObject.Parse(content);

                    // Check for data
                    if (json["data"] != null && json["data"]["candles"] != null)
                    {
                        JArray candles = (JArray)json["data"]["candles"];

                        foreach (JArray candle in candles.Cast<JArray>())
                        {
                            // Zerodha candle format: [timestamp, open, high, low, close, volume]
                            if (candle.Count >= 6)
                            {
                                // Parse timestamp
                                string timestampStr = candle[0].ToString(); // "2017-12-15T09:15:00+0530"
                                DateTime timestamp;

                                // Use DateTimeOffset to properly capture the timezone information
                                DateTimeOffset dto = DateTimeOffset.Parse(timestampStr);
                                timestamp = dto.DateTime;

                                // Explicitly specify this as IST time
                                timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Local);

                                // Create record
                                records.Add(new Record
                                {
                                    TimeStamp = timestamp,
                                    Open = Convert.ToDouble(candle[1]),
                                    High = Convert.ToDouble(candle[2]),
                                    Low = Convert.ToDouble(candle[3]),
                                    Close = Convert.ToDouble(candle[4]),
                                    Volume = Convert.ToDouble(candle[5])
                                });
                            }
                        }
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Error response: {response.StatusCode}, {errorContent}", NinjaTrader.Cbi.LogLevel.Error);
                }
            }

            return records;
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
