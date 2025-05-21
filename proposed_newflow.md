Synthetic Straddle Processor (SSP) - Detailed Tick-by-Tick Architecture Design
1. Introduction
This document provides a comprehensive architectural design for the Synthetic Straddle Processor (SSP). The SSP is a critical middle-layer component intended for seamless integration with the existing QANinjaAdapter C# project. Its primary function is to process real-time tick data for individual Call (CE) and Put (PE) options. Upon the arrival of a tick for either leg, the SSP will instantly calculate the synthetic straddle price (CE price + PE price) and forward this as a distinct synthetic tick (or real-time price point) directly to NinjaTrader via the QANinjaAdapter. The emphasis is on immediate price propagation rather than bar aggregation.

2. Goals
Real-time Synthetic Price Generation: Calculate and disseminate the synthetic straddle price (sum of CE and PE last prices) immediately upon receipt of a new tick for either constituent leg.
Dynamic Instrument Definition: Allow for the dynamic creation and management of synthetic straddle instruments based on external configuration.
Asynchronous Tick Handling: Robustly manage and process asynchronous tick arrivals from multiple underlying option legs.
Seamless NinjaTrader Integration: Provide synthetic price updates to NinjaTrader in a format it can consume as real-time tick data, enabling immediate charting and analysis.
Performance and Robustness: Design a system capable of handling high tick volumes efficiently and reliably, suitable for a trading environment.
3. System Context & High-Level Flow
The SSP will operate as an internal service within the QANinjaAdapter process. It will not establish its own external data connections but will leverage the QANinjaAdapter's existing market data reception infrastructure (e.g., Connector.cs).

Code snippet

graph TD
    A[External Data Source/Broker] -- Market Data Stream --> B(Connector.cs / QAAdapter.cs: Tick Receiver);
    B -- Raw Ticks (All Instruments) --> C{QAAdapter.cs: Market Data Dispatcher};

    subgraph SSP Subsystem (within QAAdapter.cs process)
        C -- Filtered Leg Ticks --> E(SyntheticStraddleService);
        E -- Manages StraddleState objects --> E;
        E -- Calculates Synthetic Price on Leg Tick --> E;
        E -- Publishes Synthetic Tick Event --> G;
    end

    G(QAAdapter.cs: Synthetic Tick Publisher) -- Formatted Synthetic Tick --> D[NinjaTrader Core: Market Data Engine];
    C -- Unrelated/Regular Instrument Ticks --> D;
Detailed Flow:

External Data Source/Broker: Transmits raw market data ticks.
Connector.cs / QAAdapter.cs (Tick Receiver): The QANinjaAdapter's existing component responsible for receiving all incoming ticks from the broker.
QAAdapter.cs (Market Data Dispatcher):
Receives all raw ticks.
Processes ticks for regular instruments (e.g., futures, equities) and forwards them directly to NinjaTrader.
Crucially: Identifies if an incoming tick's instrument symbol corresponds to a leg of a pre-defined synthetic straddle. If so, it forwards that specific tick to the SyntheticStraddleService.
SyntheticStraddleService (SSP Core):
Receives leg ticks from QAAdapter.cs.
Maintains the StraddleState (last known prices and timestamps) for each configured straddle.
Upon receiving a tick for either leg of a straddle, it updates the corresponding leg's price.
Immediately calculates the synthetic straddle price (CE.LastPrice + PE.LastPrice).
Triggers the QAAdapter.cs to publish this newly calculated synthetic price as a tick.
QAAdapter.cs (Synthetic Tick Publisher):
Receives the synthetic price data from SyntheticStraddleService.
Formats this data into a NinjaTrader-compatible market data event (e.g., NinjaTrader.Cbi.MarketDataEventArgs).
Submits this event to the NinjaTrader Core.
NinjaTrader Core (Market Data Engine): Receives and processes both regular instrument ticks and synthetic straddle ticks, enabling real-time charting, strategy execution, and other functionalities.
4. SSP Detailed Design
4.1. Core Components (C# Classes)
4.1.1. StraddleDefinition.cs (POCO for Configuration)
This class defines the static properties of a synthetic straddle, loaded from a configuration file.

C#

namespace QANinjaAdapter.SyntheticInstruments
{
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
4.1.2. StraddleState.cs (Runtime State Management)
This class holds the dynamic, real-time data for each active synthetic straddle. It specifically tracks the last known price for each leg.

C#

using System;

namespace QANinjaAdapter.SyntheticInstruments
{
    public class StraddleState
    {
        public StraddleDefinition Definition { get; }

        /// <summary>
        /// The last known price of the Call option leg.
        /// </summary>
        public double LastCEPrice { get; set; }

        /// <summary>
        /// The last known price of the Put option leg.
        /// </summary>
        public double LastPEPrice { get; set; }

        /// <summary>
        /// Timestamp of the last received tick for the Call leg.
        /// </summary>
        public DateTime LastCETimestamp { get; set; }

        /// <summary>
        /// Timestamp of the last received tick for the Put leg.
        /// </summary>
        public DateTime LastPETimestamp { get; set; }

        /// <summary>
        /// Indicates if at least one tick has been received for the Call leg.
        /// </summary>
        public bool HasCEData { get; set; }

        /// <summary>
        /// Indicates if at least one tick has been received for the Put leg.
        /// </summary>
        public bool HasPEData { get; set; }

        public StraddleState(StraddleDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            // Initialize prices to 0.0 or a safe 'NaN' equivalent if preferred,
            // but the HasCEData/HasPEData flags will prevent calculation until both are true.
            LastCEPrice = 0.0;
            LastPEPrice = 0.0;
            LastCETimestamp = DateTime.MinValue;
            LastPETimestamp = DateTime.MinValue;
            HasCEData = false;
            HasPEData = false;
        }

        /// <summary>
        /// Gets the current synthetic straddle price (sum of last known leg prices).
        /// Returns 0.0 if both legs haven't reported data yet.
        /// </summary>
        public double GetSyntheticPrice()
        {
            if (HasCEData && HasPEData)
            {
                return LastCEPrice + LastPEPrice;
            }
            return 0.0; // Or throw an exception, or return NaN if preferred
        }
    }
}
4.1.3. Tick.cs (Simplified Data Transfer Object)
This DTO represents a standardized tick format within the SSP, simplifying data transfer regardless of the source MarketDataEventArgs specifics.

C#

using System;

namespace QANinjaAdapter.SyntheticInstruments
{
    // Define a basic TickType enum if not already present in QAAdapter
    public enum TickType { Last, Bid, Ask, Quote, Trade } // 'Trade' can explicitly mean a volume-carrying tick

    public class Tick
    {
        public string InstrumentSymbol { get; set; }
        public double Price { get; set; }
        public long Volume { get; set; } // Volume associated with this specific tick (e.g., trade size)
        public DateTime Timestamp { get; set; }
        public TickType Type { get; set; } // To differentiate Last, Bid, Ask, etc.
        // Optional: Add bid/ask prices/sizes if your adapter receives them and you want to track for advanced synthetic calculations
        // public double BidPrice { get; set; }
        // public double AskPrice { get; set; }
        // public long BidSize { get; set; }
        // public long AskSize { get; set; }
    }
}
4.1.4. SyntheticStraddleService.cs (The SSP Core Logic)
This is the central class managing straddle definitions, states, and processing incoming ticks.

C#

using System;
using System.Collections.Concurrent; // Using ConcurrentDictionary for thread-safety
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json; // For JSON configuration parsing

namespace QANinjaAdapter.SyntheticInstruments
{
    public class SyntheticStraddleService
    {
        private readonly QAAdapter _qaAdapterInstance; // Reference to the QAAdapter for publishing
        private readonly ConcurrentDictionary<string, StraddleState> _straddleStatesBySyntheticSymbol;
        private readonly ConcurrentDictionary<string, List<StraddleState>> _legSymbolToAffectedStatesMap;

        // Event to notify QAAdapter about a new synthetic tick
        // public event Action<string, double, DateTime, long, TickType> OnSyntheticTickReady;

        public SyntheticStraddleService(QAAdapter qaAdapterInstance)
        {
            _qaAdapterInstance = qaAdapterInstance ?? throw new ArgumentNullException(nameof(qaAdapterInstance));
            _straddleStatesBySyntheticSymbol = new ConcurrentDictionary<string, StraddleState>();
            _legSymbolToAffectedStatesMap = new ConcurrentDictionary<string, List<StraddleState>>();
        }

        /// <summary>
        /// Loads straddle definitions from a JSON configuration file.
        /// </summary>
        /// <param name="configFilePath">Path to the straddles_config.json file.</param>
        public void LoadStraddleConfigs(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                QALogger.Log($"Straddle config file not found: {configFilePath}", LogLevel.Error);
                return;
            }

            try
            {
                var configJson = File.ReadAllText(configFilePath);
                var definitions = JsonConvert.DeserializeObject<List<StraddleDefinition>>(configJson);

                if (definitions == null || !definitions.Any())
                {
                    QALogger.Log($"No straddle definitions found in {configFilePath}.", LogLevel.Warning);
                    return;
                }

                foreach (var def in definitions)
                {
                    var state = new StraddleState(def);
                    _straddleStatesBySyntheticSymbol.TryAdd(def.SyntheticSymbolNinjaTrader, state);

                    // Map Call leg symbol to its associated StraddleState(s)
                    _legSymbolToAffectedStatesMap.AddOrUpdate(
                        def.CESymbol,
                        new List<StraddleState> { state },
                        (key, existingList) => { existingList.Add(state); return existingList; }
                    );

                    // Map Put leg symbol to its associated StraddleState(s)
                    _legSymbolToAffectedStatesMap.AddOrUpdate(
                        def.PESymbol,
                        new List<StraddleState> { state },
                        (key, existingList) => { existingList.Add(state); return existingList; }
                    );
                    QALogger.Log($"Loaded straddle: {def.SyntheticSymbolNinjaTrader} (CE: {def.CESymbol}, PE: {def.PESymbol})", LogLevel.Info);
                }
            }
            catch (JsonException ex)
            {
                QALogger.Log($"Error parsing straddle config file {configFilePath}: {ex.Message}", LogLevel.Error);
            }
            catch (Exception ex)
            {
                QALogger.Log($"An unexpected error occurred loading straddle configs: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Checks if an instrument symbol is a leg of any defined straddle.
        /// </summary>
        /// <param name="instrumentSymbol">The symbol of the incoming tick.</param>
        /// <returns>True if the symbol is a leg, false otherwise.</returns>
        public bool IsLegInstrument(string instrumentSymbol)
        {
            return _legSymbolToAffectedStatesMap.ContainsKey(instrumentSymbol);
        }

        /// <summary>
        /// Processes an incoming tick for a straddle leg.
        /// </summary>
        /// <param name="tick">The incoming tick data.</param>
        public void ProcessLegTick(Tick tick)
        {
            if (tick == null) return;

            // Find all straddles affected by this tick
            if (!_legSymbolToAffectedStatesMap.TryGetValue(tick.InstrumentSymbol, out List<StraddleState> affectedStates))
            {
                // This case should ideally not happen if IsLegInstrument is called first,
                // but good for defensive programming.
                return;
            }

            foreach (var state in affectedStates)
            {
                bool priceUpdated = false;

                // Update the relevant leg's last price and timestamp
                if (state.Definition.CESymbol.Equals(tick.InstrumentSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    state.LastCEPrice = tick.Price;
                    state.LastCETimestamp = tick.Timestamp;
                    state.HasCEData = true;
                    priceUpdated = true;
                }
                else if (state.Definition.PESymbol.Equals(tick.InstrumentSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    state.LastPEPrice = tick.Price;
                    state.LastPETimestamp = tick.Timestamp;
                    state.HasPEData = true;
                    priceUpdated = true;
                }

                // Only calculate and publish a synthetic tick if:
                // 1. The price of the current leg tick actually resulted in an update to its stored price.
                // 2. Both legs have reported at least one price (preventing calculation with 0.0 initial values).
                if (priceUpdated && state.HasCEData && state.HasPEData)
                {
                    double syntheticCurrentPrice = state.GetSyntheticPrice();

                    // --- Volume Logic for Tick-by-Tick ---
                    // Recommendation: Use the volume of the incoming leg's tick.
                    // This attributes the "event" of the synthetic price update to the volume of the leg that caused it.
                    long syntheticVolume = tick.Volume;

                    // Alternatively, if volume is strictly "trade volume" and you want to be precise:
                    // If you only want volume from actual trades, check tick.Type == TickType.Trade or TickType.Last
                    // and use tick.Volume. Otherwise, use 0 or 1 for event count.
                    // Example: if (tick.Type == TickType.Trade || tick.Type == TickType.Last) syntheticVolume = tick.Volume; else syntheticVolume = 0;


                    _qaAdapterInstance.PublishSyntheticTickData(
                        state.Definition.SyntheticSymbolNinjaTrader,
                        syntheticCurrentPrice,
                        tick.Timestamp, // Use the timestamp of the incoming tick as the synthetic tick's timestamp
                        syntheticVolume,
                        tick.Type // Pass the original tick type for NinjaTrader's context
                    );
                }
            }
        }
    }
}
4.2. Configuration
straddles_config.json: Located in the adapter's working directory or a specified config path.

JSON

[
  {
    "SyntheticSymbolNinjaTrader": "NIFTY25000STRDL",
    "CESymbol": "NIFTY2460725000CE",
    "PESymbol": "NIFTY2460725000PE",
    "TickSize": 0.05,
    "PointValue": 50.0,
    "Currency": "INR"
  },
  {
    "SyntheticSymbolNinjaTrader": "BANKNIFTY48000STRDL",
    "CESymbol": "BANKNIFTY2460748000CE",
    "PESymbol": "BANKNIFTY2460748000PE",
    "TickSize": 0.05,
    "PointValue": 15.0,
    "Currency": "INR"
  }
]
5. Integration with QANinjaAdapter (C# Specifics)
This section details how QAAdapter.cs interacts with the SSP.

5.1. Folder Structure and Class Placement
Maintain the proposed folder structure:

QANinjaAdapter/SyntheticInstruments/StraddleDefinition.cs
QANinjaAdapter/SyntheticInstruments/StraddleState.cs
QANinjaAdapter/SyntheticInstruments/Tick.cs (if not already defined elsewhere)
QANinjaAdapter/SyntheticInstruments/SyntheticStraddleService.cs
5.2. Initialization & Lifetime in QAAdapter.cs
The SyntheticStraddleService instance should be created and initialized during the QAAdapter's setup phase.

C#

using NinjaTrader.Cbi; // Assuming this namespace for MarketDataEventArgs
using NinjaTrader.Custom.Adapter; // Assuming this is your base adapter namespace
using NinjaTrader.Data; // For Instruments collection
using System;
using System.Linq;
using System.Threading.Tasks; // For potential async operations if needed
using QANinjaAdapter.SyntheticInstruments; // New using directive

// Assuming QALogger and LogLevel exist from your previous design
public static class QALogger
{
    public enum LogLevel { Debug, Info, Warning, Error }
    public static void Log(string message, LogLevel level)
    {
        // Implement your logging logic (e.g., to console, file, NinjaTrader log)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}");
        // For NinjaTrader, you might use AdapterHost.LogMessage or similar
    }
}


// Assuming QAAdapter inherits from a suitable NinjaTrader Adapter base class
// The actual base class might be specific (e.g., NinjaTrader.Custom.Adapter.SomeBaseAdapter)
public partial class QAAdapter // Assuming QAAdapter is already defined
{
    private SyntheticStraddleService _syntheticStraddleService;
    private readonly object _marketDataLock = new object(); // For thread safety on market data processing

    // Constructor or suitable initialization method in QAAdapter
    public QAAdapter()
    {
        // ... existing QAAdapter initialization ...

        // Initialize the SyntheticStraddleService
        _syntheticStraddleService = new SyntheticStraddleService(this); // Pass 'this' instance
    }

    // Call this method when the adapter is connected/initialized and ready for config loading
    public void OnAdapterStarted() // Or equivalent event handler
    {
        // Assuming your config file is in the adapter's assembly directory or a known path
        string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "straddles_config.json");
        _syntheticStraddleService.LoadStraddleConfigs(configFilePath);
        QALogger.Log("SyntheticStraddleService initialized and configurations loaded.", QALogger.LogLevel.Info);
    }

    // You might also need to call this on disconnect to clean up
    public void OnAdapterStopped()
    {
        // Perform any necessary cleanup for _syntheticStraddleService if it holds unmanaged resources
    }
}
5.3. Tick Forwarding from QAAdapter.cs
Modify the method in QAAdapter.cs that receives raw ticks from Connector.cs. This is typically an OnMarketData override or a handler for a MarketData event.

C#

// Inside QAAdapter.cs

// This method is typically an override from a NinjaTrader base adapter or a handler for your connector's market data event.
// The exact signature depends on your QAAdapter's base class.
// Example: From NinjaTrader.Custom.Adapter.Adapter.OnMarketData
protected override void OnMarketData(MarketDataEventArgs marketDataArgs) // Or similar signature
{
    // Ensure thread safety if market data can arrive on multiple threads
    lock (_marketDataLock)
    {
        // --- Existing logic for forwarding regular ticks to NinjaTrader ---
        // base.OnMarketData(marketDataArgs); // If you're overriding a base method and want default handling

        // --- Synthetic Straddle Processing ---
        if (_syntheticStraddleService != null && _syntheticStraddleService.IsLegInstrument(marketDataArgs.Instrument.FullName))
        {
            // Convert NinjaTrader's MarketDataEventArgs to your internal Tick DTO
            var tick = new QANinjaAdapter.SyntheticInstruments.Tick
            {
                InstrumentSymbol = marketDataArgs.Instrument.FullName,
                Price = marketDataArgs.Price,
                Volume = marketDataArgs.Volume,
                Timestamp = marketDataArgs.Time,
                Type = MapNinjaTraderMarketDataType(marketDataArgs.MarketDataType) // Map NT enum to your TickType enum
            };
            _syntheticStraddleService.ProcessLegTick(tick);
        }
        else
        {
            // If it's not a synthetic leg, ensure it's still processed by NinjaTrader's core.
            // This line depends on your adapter's implementation. It might be implicitly handled
            // if you call base.OnMarketData earlier, or if this QAAdapter directly dispatches.
            // Example: FireMarketDataEvent(marketDataArgs); // Your internal method to dispatch to NT
        }
    }
}

// Helper to map NinjaTrader's MarketDataType to your internal TickType
private QANinjaAdapter.SyntheticInstruments.TickType MapNinjaTraderMarketDataType(NinjaTrader.Cbi.MarketDataType ntType)
{
    switch (ntType)
    {
        case NinjaTrader.Cbi.MarketDataType.Last: return QANinjaAdapter.SyntheticInstruments.TickType.Last;
        case NinjaTrader.Cbi.MarketDataType.Bid: return QANinjaAdapter.SyntheticInstruments.TickType.Bid;
        case NinjaTrader.Cbi.MarketDataType.Ask: return QANinjaAdapter.SyntheticInstruments.TickType.Ask;
        case NinjaTrader.Cbi.MarketDataType.Trade: return QANinjaAdapter.SyntheticInstruments.TickType.Trade; // If available
        case NinjaTrader.Cbi.MarketDataType.Quote: return QANinjaAdapter.SyntheticInstruments.TickType.Quote; // If available
        default: return QANinjaAdapter.SyntheticInstruments.TickType.Last; // Default or handle appropriately
    }
}
5.4. Publishing Synthetic Ticks to NinjaTrader (from QAAdapter.cs)
This is the critical integration point. When SyntheticStraddleService generates a synthetic tick, it calls this method on QAAdapter.cs to push the data to NinjaTrader.

C#

// Inside QAAdapter.cs

/// <summary>
/// Publishes a synthetic tick data point to NinjaTrader.
/// This method should create a NinjaTrader-compatible MarketDataEventArgs and pass it to NT's data engine.
/// </summary>
public void PublishSyntheticTickData(string syntheticSymbol, double price, DateTime timestamp, long volume, QANinjaAdapter.SyntheticInstruments.TickType tickType)
{
    // Ensure thread safety as this might be called concurrently
    lock (_marketDataLock)
    {
        // 1. Find the NinjaTrader Instrument object for the synthetic symbol
        // The 'Instruments' collection is typically managed by your QAAdapter's base.
        // It relies on the synthetic instrument being defined in mapped_instruments.json
        // and initialized by NinjaTrader's MasterInstrument system.
        Instrument ntInstrument = null;
        if (Instruments != null) // 'Instruments' is usually a property from a NinjaTrader base class
        {
            ntInstrument = Instruments.FirstOrDefault(instr =>
                instr.FullName.Equals(syntheticSymbol, StringComparison.OrdinalIgnoreCase));
        }

        if (ntInstrument == null)
        {
            QALogger.Log($"ERROR: Synthetic instrument '{syntheticSymbol}' not found in NinjaTrader's instrument collection. " +
                         "Ensure it's correctly defined in mapped_instruments.json and loaded by NT.", QALogger.LogLevel.Error);
            return;
        }

        // 2. Create NinjaTrader's MarketDataEventArgs
        // For synthetic ticks, it's common to treat them as 'Last' price updates.
        // Adjust MarketDataType based on the TickType if you need to differentiate synthetic Bids/Asks.
        NinjaTrader.Cbi.MarketDataType ntMarketDataType = NinjaTrader.Cbi.MarketDataType.Last;
        if (tickType == QANinjaAdapter.SyntheticInstruments.TickType.Bid) ntMarketDataType = NinjaTrader.Cbi.MarketDataType.Bid;
        else if (tickType == QANinjaAdapter.SyntheticInstruments.TickType.Ask) ntMarketDataType = NinjaTrader.Cbi.MarketDataType.Ask;
        // Add more mappings if needed for Quote, Trade, etc.

        MarketDataEventArgs syntheticMarketData = new MarketDataEventArgs(
            ntInstrument.MasterInstrument,  // Always use MasterInstrument for performance and consistency
            ntMarketDataType,               // Type of market data (Last, Bid, Ask)
            price,                          // Price of the synthetic tick
            0, 0, 0, 0,                     // BidPrice, AskPrice, BidSize, AskSize (often 0 for synthetic Last ticks)
            volume,                         // Volume associated with this synthetic tick
            timestamp
        );

        // 3. Publish the tick to NinjaTrader's core data engine
        // The exact method call depends on your QAAdapter's inheritance and architecture.
        // Common scenarios:
        // a) If QAAdapter directly inherits from NinjaTrader.Custom.Adapter.Adapter:
        //    this.OnMarketData(syntheticMarketData); // Call the base adapter's OnMarketData
        // b) If you have an internal event/delegate that NT listens to:
        //    _marketDataEventHandler?.Invoke(this, syntheticMarketData); // Assuming a delegate
        // c) If you need to marshal to a specific UI/data thread (less common for adapter base classes):
        //    Application.Current.Dispatcher.Invoke(() => base.OnMarketData(syntheticMarketData));

        // Assuming a standard adapter pattern where OnMarketData is designed for this purpose:
        base.OnMarketData(syntheticMarketData); // This is the most likely way to push data to NT

        QALogger.Log($"Published synthetic tick for {syntheticSymbol}: P={price:F2}, V={volume}, T={timestamp:HH:mm:ss.fff}", QALogger.LogLevel.Debug);
    }
}
5.5. mapped_instruments.json Configuration
The synthetic straddle symbols must be explicitly defined in your mapped_instruments.json (or whatever mechanism QANinjaAdapter uses to inform NinjaTrader about available instruments). This allows NinjaTrader to create the necessary Instrument objects and associate them with incoming data.

JSON

[
  // ... existing instrument definitions ...
  {
    "connector_symbol": "NIFTY25000STRDL",
    "exchange": "SYNTHETICS",         // Use a unique, custom exchange for synthetic instruments
    "instrument_type": "FUTURE",      // Or "EQUITY", "FOREX" - pick a type NT understands for charting.
                                      // "SYNTHETIC" might not be a recognized built-in type.
    "ninjatrader_symbol": "NIFTY25000STRDL", // This is the symbol NT will use internally
    "description": "Nifty 25000 Straddle (Synthetic)",
    "tick_size": 0.05,                // From straddles_config.json
    "point_value": 50.0,              // From straddles_config.json
    "currency": "INR",
    "supported_resolutions": "Tick, Minute, Second", // Crucially, "Tick" MUST be listed
    "session_template": "NSE Equity"  // Or the appropriate session template for your market
  },
  {
    "connector_symbol": "BANKNIFTY48000STRDL",
    "exchange": "SYNTHETICS",
    "instrument_type": "FUTURE",
    "ninjatrader_symbol": "BANKNIFTY48000STRDL",
    "description": "Bank Nifty 48000 Straddle (Synthetic)",
    "tick_size": 0.05,
    "point_value": 15.0,
    "currency": "INR",
    "supported_resolutions": "Tick, Minute, Second",
    "session_template": "NSE Equity"
  }
]
Important Considerations for mapped_instruments.json:

instrument_type: While you're creating a "synthetic" instrument, NinjaTrader's built-in Instrument types are limited. You might need to map it to an existing type like "FUTURE" or "EQUITY" that allows tick-by-tick data.
supported_resolutions: This must explicitly include "Tick" for NinjaTrader to collect and display raw tick data for the synthetic instrument.
connector_symbol: This is the symbol your Connector.cs would typically use. For synthetic, it should match SyntheticSymbolNinjaTrader.
ninjatrader_symbol: This is the symbol NinjaTrader's Instrument object will hold.
6. Performance and Scalability Considerations
Concurrency: Using ConcurrentDictionary in SyntheticStraddleService for _straddleStatesBySyntheticSymbol and _legSymbolToAffectedStatesMap is good for thread-safety, as ProcessLegTick might be called concurrently if your QAAdapter receives ticks on multiple threads.
Locking: The _marketDataLock in QAAdapter.cs is crucial for thread safety when processing market data and publishing to NinjaTrader. Excessive locking can be a bottleneck, but for critical shared resources like the Instruments collection and the NinjaTrader publishing pipeline, it's necessary.
Logging: Be mindful of logging verbosity. Set LogLevel.Debug for synthetic ticks and only enable LogLevel.Info or LogLevel.Error for production to avoid excessive disk I/O or console writes.
NinjaTrader API Efficiency: The primary performance constraint will likely be the efficiency of NinjaTrader's internal OnMarketData (or equivalent) call. If NinjaTrader's core performs heavy operations (like UI updates, complex calculations for indicators/strategies) on every tick, high tick rates (hundreds/thousands per second) could be challenging.
Garbage Collection: Frequent object creation (e.g., new MarketDataEventArgs objects for every synthetic tick) can increase GC pressure. For extremely high-frequency scenarios, consider object pooling, though for 40 straddles on typical tick rates, it's usually not a major issue.
This detailed architecture provides a solid foundation for building your Synthetic Straddle Processor with tick-by-tick output. Remember to adapt the NinjaTrader specific API calls (MarketDataEventArgs creation and dispatch) to precisely match your QANinjaAdapter's existing structure and the NinjaTrader version you are targeting.