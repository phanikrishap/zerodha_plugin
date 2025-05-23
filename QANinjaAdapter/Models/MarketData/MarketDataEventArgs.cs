using System;
using NinjaTrader.Data;

namespace QANinjaAdapter.Models.MarketData
{
    public class MarketDataEventArgs : EventArgs
    {
        public string Symbol { get; }
        public MarketDataType Type { get; }
        public double Price { get; }
        public long Volume { get; } 
        public DateTime Time { get; }
        public int MarketDepthLevel { get; } 
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long CumulativeVolume { get; set; }

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
