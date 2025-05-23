using System;

namespace QANinjaAdapter.Models.MarketData
{
    /// <summary>
    /// Rich data structure for Zerodha market data
    /// </summary>
    public class ZerodhaTickData
    {
        /// <summary>
        /// The instrument token
        /// </summary>
        public int InstrumentToken { get; set; }

        /// <summary>
        /// The last traded price
        /// </summary>
        public double LastTradePrice { get; set; }

        /// <summary>
        /// The last traded quantity
        /// </summary>
        public int LastTradeQty { get; set; }

        /// <summary>
        /// The average traded price
        /// </summary>
        public double AverageTradePrice { get; set; }

        /// <summary>
        /// The total volume traded for the day
        /// </summary>
        public int TotalQtyTraded { get; set; }

        /// <summary>
        /// The total buy quantity
        /// </summary>
        public int BuyQty { get; set; }

        /// <summary>
        /// The total sell quantity
        /// </summary>
        public int SellQty { get; set; }

        /// <summary>
        /// The open price of the day
        /// </summary>
        public double Open { get; set; }

        /// <summary>
        /// The high price of the day
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// The low price of the day
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// The close price
        /// </summary>
        public double Close { get; set; }

        /// <summary>
        /// The last traded timestamp
        /// </summary>
        public DateTime LastTradeTime { get; set; }

        /// <summary>
        /// The open interest
        /// </summary>
        public int OpenInterest { get; set; }

        /// <summary>
        /// The open interest day high
        /// </summary>
        public int OpenInterestDayHigh { get; set; }

        /// <summary>
        /// The open interest day low
        /// </summary>
        public int OpenInterestDayLow { get; set; }

        /// <summary>
        /// The exchange timestamp
        /// </summary>
        public DateTime ExchangeTimestamp { get; set; }

        /// <summary>
        /// The best bid price
        /// </summary>
        public double BuyPrice => BidDepth.Length > 0 ? BidDepth[0].Price : 0;

        /// <summary>
        /// The best ask price
        /// </summary>
        public double SellPrice => AskDepth.Length > 0 ? AskDepth[0].Price : 0;

        /// <summary>
        /// The bid depth entries
        /// </summary>
        public DepthEntry[] BidDepth { get; set; } = new DepthEntry[5];

        /// <summary>
        /// The ask depth entries
        /// </summary>
        public DepthEntry[] AskDepth { get; set; } = new DepthEntry[5];

        /// <summary>
        /// The instrument identifier (symbol name)
        /// </summary>
        public string InstrumentIdentifier { get; set; }

        /// <summary>
        /// Flag indicating if this is a full quote with market depth
        /// </summary>
        public bool HasMarketDepth { get; set; }

        /// <summary>
        /// Flag indicating if this is an index
        /// </summary>
        public bool IsIndex { get; set; }
    }
}
