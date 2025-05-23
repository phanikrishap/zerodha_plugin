using System;

namespace QANinjaAdapter.Models.MarketData
{
    /// <summary>
    /// Represents different types of market data events in QAAdapter.
    /// Renamed to avoid conflict with NinjaTrader.Data.MarketDataType
    /// </summary>
    public enum QAMarketDataType
    {
        /// <summary>
        /// Unknown market data type
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// Best bid price
        /// </summary>
        Bid = 1,
        
        /// <summary>
        /// Best ask price
        /// </summary>
        Ask = 2,
        
        /// <summary>
        /// Last trade price
        /// </summary>
        Last = 3,
        
        /// <summary>
        /// Market depth update
        /// </summary>
        MarketDepth = 4,
        
        /// <summary>
        /// Open price
        /// </summary>
        Open = 5,
        
        /// <summary>
        /// High price
        /// </summary>
        High = 6,
        
        /// <summary>
        /// Low price
        /// </summary>
        Low = 7,
        
        /// <summary>
        /// Close price
        /// </summary>
        Close = 8,
        
        /// <summary>
        /// Volume update
        /// </summary>
        Volume = 9,
        
        /// <summary>
        /// Open interest update
        /// </summary>
        OpenInterest = 10
    }
}
