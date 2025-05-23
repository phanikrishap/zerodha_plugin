using System;

namespace QANinjaAdapter.Models
{
    /// <summary>
    /// Represents an instrument definition as typically found in a mapping file like mapped_instruments.json.
    /// This class was originally an inner class in Connector.cs.
    /// </summary>
    public class MappedInstrument
    {
        public string symbol { get; set; } // The symbol name used within NinjaTrader or as a primary identifier
        public string underlying { get; set; } // Underlying asset, e.g., for derivatives
        public DateTime? expiry { get; set; } // Expiry date for derivatives, nullable
        public double? strike { get; set; } // Strike price for options, nullable
        public string option_type { get; set; } // E.g., "CE" or "PE" for options
        public string segment { get; set; } // Market segment, e.g., "NSE", "NFO-FUT", "MCX"
        public long instrument_token { get; set; } // Broker-specific instrument token (e.g., Zerodha's)
        public int? exchange_token { get; set; } // Broker-specific exchange token, nullable
        public string zerodhaSymbol { get; set; } // Explicit field for Zerodha's trading symbol if different from 'symbol'
        public double tick_size { get; set; } // Minimum price movement
        public int lot_size { get; set; } // Lot size for trading, particularly for derivatives
        public string name { get; set; } // Descriptive name of the instrument
        public string exchange { get; set; } // The exchange the instrument trades on (e.g., "NSE", "NFO", "MCX")
        public string instrument_type { get; set; } // Type of instrument, e.g., "EQ", "FUTIDX", "OPTSTK"
    }
}
