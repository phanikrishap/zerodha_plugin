using System;

namespace QANinjaAdapter.Models.MarketData
{
    /// <summary>
    /// Represents parsed tick data from Zerodha WebSocket
    /// </summary>
    public class ParsedTickData
    {
        /// <summary>
        /// The instrument token from Zerodha
        /// </summary>
        public uint InstrumentToken { get; set; }
        
        /// <summary>
        /// The last traded price
        /// </summary>
        public float LastPrice { get; set; }
        
        /// <summary>
        /// The last traded quantity
        /// </summary>
        public int LastQuantity { get; set; }
        
        /// <summary>
        /// The total volume traded for the day
        /// </summary>
        public int Volume { get; set; }
        
        /// <summary>
        /// The average traded price
        /// </summary>
        public float AverageTradePrice { get; set; }
        
        /// <summary>
        /// The total buy quantity
        /// </summary>
        public int BuyQuantity { get; set; }
        
        /// <summary>
        /// The total sell quantity
        /// </summary>
        public int SellQuantity { get; set; }
        
        /// <summary>
        /// The open price of the day
        /// </summary>
        public float Open { get; set; }
        
        /// <summary>
        /// The high price of the day
        /// </summary>
        public float High { get; set; }
        
        /// <summary>
        /// The low price of the day
        /// </summary>
        public float Low { get; set; }
        
        /// <summary>
        /// The close price of the day
        /// </summary>
        public float Close { get; set; }
        
        /// <summary>
        /// The mode of the tick data (ltp, quote, index)
        /// </summary>
        public string Mode { get; set; }
        
        /// <summary>
        /// Flag to indicate if this is an index packet
        /// </summary>
        public bool IsIndex { get; set; }
        
        /// <summary>
        /// The timestamp of the tick data
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
