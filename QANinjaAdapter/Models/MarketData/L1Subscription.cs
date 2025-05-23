using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using QANinjaAdapter.Models.MarketData; // For MarketDataEventArgs if it's in the same namespace

namespace QANinjaAdapter.Models.MarketData
{
    public class L1Subscription
    {
        // Properties from Models.MarketData.L1Subscription
        public string NativeSymbol { get; set; }
        public string OriginalSymbol { get; set; } 
        public Action<MarketDataEventArgs> Callback { get; set; }
        public Func<bool> IsActiveCheck { get; set; }
        public int InstrumentToken { get; set; }
        public string Exchange { get; set; }
        public string Mode { get; set; }
        public bool IsActive { get; set; }
        
        // Properties from root namespace L1Subscription
        public SortedList<Instrument, Action<MarketDataType, double, long, DateTime, long>> L1Callbacks { get; set; } 
            = new SortedList<Instrument, Action<MarketDataType, double, long, DateTime, long>>();
        public int PreviousVolume { get; set; }
        public Instrument Instrument { get; set; }

        // Default constructor
        public L1Subscription()
        {
        }

        // Constructor from Models.MarketData.L1Subscription
        public L1Subscription(string nativeSymbol, string originalSymbol, Action<MarketDataEventArgs> callback, Func<bool> isActiveCheck)
        {
            NativeSymbol = nativeSymbol;
            OriginalSymbol = originalSymbol;
            Callback = callback;
            IsActiveCheck = isActiveCheck;
        }

        // Constructor for compatibility with root namespace L1Subscription usage
        public L1Subscription(Instrument instrument)
        {
            Instrument = instrument;
            NativeSymbol = instrument?.MasterInstrument?.Name;
        }
    }
}
