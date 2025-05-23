using System;
using NinjaTrader.Data;

namespace QANinjaAdapter.Models.MarketData
{
    public class MarketDataEventArgs : EventArgs
    {
        // Original properties
        public string Symbol { get; set; }
        public MarketDataType Type { get; set; }
        public double Price { get; set; }
        public long Volume { get; set; } 
        public DateTime Time { get; set; }
        public int MarketDepthLevel { get; set; } 
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long CumulativeVolume { get; set; }
        
        // Additional properties needed by DataProcessingService
        public long InstrumentToken { get; set; }
        public double LastPrice { get; set; }
        public long LastQuantity { get; set; }
        public double AveragePrice { get; set; }
        public long BuyQuantity { get; set; }
        public long SellQuantity { get; set; }
        public double OpenPrice { get; set; }
        public double HighPrice { get; set; }
        public double LowPrice { get; set; }
        public double ClosePrice { get; set; }
        public double BuyPrice { get; set; }
        public double SellPrice { get; set; }
        public DateTime Timestamp { get; set; }
        
        // Default constructor for object initializer syntax
        public MarketDataEventArgs()
        {
        }

        // Original constructor
        public MarketDataEventArgs(string symbol, MarketDataType type, double price, long volume, DateTime time, int marketDepthLevel)
        {
            Symbol = symbol;
            Type = type;
            Price = price;
            Volume = volume; 
            Time = time;
            MarketDepthLevel = marketDepthLevel;
        }
    }
}
