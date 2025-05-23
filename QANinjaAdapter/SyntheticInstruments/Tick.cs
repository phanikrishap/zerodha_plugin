using System;

namespace QANinjaAdapter.SyntheticInstruments
{
    /// <summary>
    /// Defines the types of ticks that can be processed by the Synthetic Straddle Processor.
    /// </summary>
    public enum TickType 
    { 
        Last, 
        Bid, 
        Ask, 
        Quote, 
        Trade 
    }

    /// <summary>
    /// Represents a standardized tick format within the SSP, simplifying data transfer
    /// regardless of the source MarketDataEventArgs specifics.
    /// </summary>
    public class Tick
    {
        /// <summary>
        /// The symbol of the instrument this tick is for.
        /// </summary>
        public string InstrumentSymbol { get; set; }
        
        /// <summary>
        /// The price of this tick.
        /// </summary>
        public double Price { get; set; }
        
        /// <summary>
        /// The volume associated with this specific tick (e.g., trade size).
        /// </summary>
        public long Volume { get; set; }
        
        /// <summary>
        /// The timestamp of this tick.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// The type of this tick (Last, Bid, Ask, etc.).
        /// </summary>
        public TickType Type { get; set; }
        
        // Optional: Add bid/ask prices/sizes if your adapter receives them and you want to track for advanced synthetic calculations
        // public double BidPrice { get; set; }
        // public double AskPrice { get; set; }
        // public long BidSize { get; set; }
        // public long AskSize { get; set; }
    }
}
