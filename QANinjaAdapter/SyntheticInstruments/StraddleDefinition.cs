using System;

namespace QANinjaAdapter.SyntheticInstruments
{
    /// <summary>
    /// Defines the static properties of a synthetic straddle, loaded from a configuration file.
    /// </summary>
    public class StraddleDefinition
    {
        /// <summary>
        /// The unique symbol under which this synthetic straddle will be known in NinjaTrader.
        /// (e.g., "NIFTY25000STRDL", "BANKNIFTY48000STRDL")
        /// </summary>
        public string SyntheticSymbolNinjaTrader { get; set; }

        /// <summary>
        /// The full broker/exchange symbol for the Call option leg.
        /// (e.g., "NIFTY2460725000CE")
        /// </summary>
        public string CESymbol { get; set; }

        /// <summary>
        /// The full broker/exchange symbol for the Put option leg.
        /// (e.g., "NIFTY2460725000PE")
        /// </summary>
        public string PESymbol { get; set; }

        /// <summary>
        /// The minimum price increment for the synthetic straddle in NinjaTrader.
        /// This should generally match the tick size of the underlying legs.
        /// </summary>
        public double TickSize { get; set; }

        /// <summary>
        /// The monetary value per point of the synthetic straddle in NinjaTrader.
        /// Crucial for PnL calculations and sizing.
        /// </summary>
        public double PointValue { get; set; }

        /// <summary>
        /// The currency in which the synthetic straddle is denominated (e.g., "INR", "USD").
        /// </summary>
        public string Currency { get; set; } = "INR";
    }
}
