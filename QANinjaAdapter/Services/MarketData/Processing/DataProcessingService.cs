using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using QANinjaAdapter.Models.MarketData;
using QANinjaAdapter.Logging;

namespace QANinjaAdapter.Services.MarketData.Processing
{
    public class DataProcessingService
    {
        // Thread-safe dictionary to store last tick data for each instrument token
        private readonly ConcurrentDictionary<int, MarketDataEventArgs> _lastTickData = 
            new ConcurrentDictionary<int, MarketDataEventArgs>();
            
        public void ProcessMessage(byte[] data, ConcurrentDictionary<string, L1Subscription> subscriptions)
        {
            try
            {
                // Parse the binary message from Zerodha
                var parsedTickData = ParseBinaryMessage(data);
                
                if (parsedTickData != null && parsedTickData.Count > 0)
                {
                    // Process each tick in the message
                    foreach (var tickData in parsedTickData)
                    {
                        int instrumentToken = tickData.InstrumentToken;
                        
                        // Convert to MarketDataEventArgs format
                        var eventArgs = ConvertToMarketDataEventArgs(tickData);
                        
                        // Store the latest tick data for this instrument
                        _lastTickData[instrumentToken] = eventArgs;
                        
                        // Find all subscriptions that match this instrument token and notify them
                        foreach (var subscription in subscriptions.Values)
                        {
                            if (subscription.InstrumentToken == instrumentToken && subscription.Callback != null)
                            {
                                try
                                {
                                    subscription.Callback(eventArgs);
                                }
                                catch (Exception callbackEx)
                                {
                                    AppLogger.Log($"Error in tick callback for token {instrumentToken}: {callbackEx.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error processing WebSocket message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        private List<ParsedTickData> ParseBinaryMessage(byte[] data)
        {
            // Zerodha binary message format parsing
            // This is a simplified implementation - the actual parsing logic
            // should follow Zerodha's binary protocol specification
            
            var result = new List<ParsedTickData>();
            
            try
            {
                // Check for a valid message (at least 2 bytes)
                if (data == null || data.Length < 2)
                    return result;

                // First byte: packet type
                byte packetType = data[0];
                
                // TODO: Implement full binary parsing according to Zerodha's WebSocket protocol
                // This is placeholder code and should be replaced with actual implementation
                
                // Mode-specific parsing based on packet type
                switch (packetType)
                {
                    case 0: // LTP mode packet
                        if (data.Length >= 8)
                        {
                            // Extract instrument token (bytes 2-5)
                            int instrumentToken = BitConverter.ToInt32(data, 2);
                            
                            // Extract LTP (bytes 6-9)
                            float ltp = BitConverter.ToSingle(data, 6);
                            
                            result.Add(new ParsedTickData
                            {
                                InstrumentToken = instrumentToken,
                                LastPrice = ltp,
                                Mode = "ltp"
                            });
                        }
                        break;
                        
                    case 1: // Quote/full mode
                        // Implement quote and full mode parsing
                        // These are more complex with multiple fields
                        
                        // This is simplified placeholder code
                        if (data.Length >= 10)
                        {
                            int instrumentToken = BitConverter.ToInt32(data, 2);
                            float lastPrice = BitConverter.ToSingle(data, 6);
                            
                            var tick = new ParsedTickData
                            {
                                InstrumentToken = instrumentToken,
                                LastPrice = lastPrice,
                                Mode = "quote"
                            };
                            
                            // Add more fields for quote/full mode
                            if (data.Length >= 30)
                            {
                                tick.LastQuantity = BitConverter.ToInt32(data, 10);
                                tick.AveragePrice = BitConverter.ToSingle(data, 14);
                                tick.Volume = BitConverter.ToInt32(data, 18);
                                tick.BuyPrice = BitConverter.ToSingle(data, 22);
                                tick.SellPrice = BitConverter.ToSingle(data, 26);
                            }
                            
                            result.Add(tick);
                        }
                        break;
                        
                    // Add handling for other packet types
                    
                    default:
                        AppLogger.Log($"Unknown packet type: {packetType}", QANinjaAdapter.Logging.LogLevel.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error parsing binary message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
            
            return result;
        }
        
        private MarketDataEventArgs ConvertToMarketDataEventArgs(ParsedTickData data)
        {
            // Convert parsed tick data to the format expected by callbacks
            var args = new MarketDataEventArgs
            {
                InstrumentToken = data.InstrumentToken,
                LastPrice = data.LastPrice,
                LastQuantity = data.LastQuantity,
                AveragePrice = data.AveragePrice,
                Volume = data.Volume,
                BuyQuantity = data.BuyQuantity,
                SellQuantity = data.SellQuantity,
                OpenPrice = data.OpenPrice,
                HighPrice = data.HighPrice,
                LowPrice = data.LowPrice,
                ClosePrice = data.ClosePrice,
                BuyPrice = data.BuyPrice,
                SellPrice = data.SellPrice,
                Timestamp = DateTime.Now // Use actual timestamp from data if available
            };
            
            return args;
        }
        
        // Helper class for parsed tick data
        private class ParsedTickData
        {
            public int InstrumentToken { get; set; }
            public float LastPrice { get; set; }
            public int LastQuantity { get; set; }
            public float AveragePrice { get; set; }
            public int Volume { get; set; }
            public int BuyQuantity { get; set; }
            public int SellQuantity { get; set; }
            public float OpenPrice { get; set; }
            public float HighPrice { get; set; }
            public float LowPrice { get; set; }
            public float ClosePrice { get; set; }
            public float BuyPrice { get; set; }
            public float SellPrice { get; set; }
            public string Mode { get; set; }
        }
        
        // Get the last tick data for a specific instrument
        public MarketDataEventArgs GetLastTickData(int instrumentToken)
        {
            _lastTickData.TryGetValue(instrumentToken, out var tickData);
            return tickData;
        }
    }
}
