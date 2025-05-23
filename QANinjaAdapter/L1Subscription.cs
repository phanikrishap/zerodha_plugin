// This is a compatibility class that forwards to the comprehensive L1Subscription in Models.MarketData

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using System;
using System.Collections.Generic;
using QANinjaAdapter.Models.MarketData;

#nullable disable
namespace QANinjaAdapter
{
    // This class inherits from the comprehensive L1Subscription class to maintain type compatibility
    // while ensuring all code uses the same underlying implementation
    public class L1Subscription : Models.MarketData.L1Subscription
    {
        // Default constructor that calls the base constructor
        public L1Subscription() : base()
        {
        }

        // Constructor for backward compatibility with existing code
        public L1Subscription(Instrument instrument) : base(instrument)
        {
        }
        
        // Constructor for the MarketData service
        public L1Subscription(string nativeSymbol, string originalSymbol, Action<QANinjaAdapter.Models.MarketData.MarketDataEventArgs> callback, Func<bool> isActiveCheck)
            : base(nativeSymbol, originalSymbol, callback, isActiveCheck)
        {
        }
    }
}
