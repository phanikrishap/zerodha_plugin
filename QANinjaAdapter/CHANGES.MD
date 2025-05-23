# Refactoring Data Handover to NinjaTrader (Inspired by Nimble Plugin)

This document outlines the changes to refactor the `QANinjaAdapter`'s data handover mechanism to NinjaTrader. The goal is to emulate aspects of the Nimble plugin's approach, where the adapter class (`QAAdapter`) has a dedicated method to process a rich market data object. This can offer better control, potentially a more direct update path, and clearer points for performance profiling of the data insertion step.

## Implementation (2025-05-21 15:19:49 IST)

The refactoring has been implemented with the following changes:

1. **Created a Rich Data Structure**
   - Added `ZerodhaTickData.cs` class to hold all market data fields in a single object
   - This is similar to the `DS_RealtimeResult` class in the NimbleData project
   - Note: The implementation uses a custom C# class rather than Protocol Buffers

2. **Enhanced WebSocketManager**
   - Modified `ParseBinaryMessage` method to parse binary messages into the rich data structure
   - Added support for different packet modes (LTP, Quote, Full)
   - Included market depth parsing

3. **Added Direct Update Method in QAAdapter**
   - Added `ProcessParsedTick` method to process the rich data structure
   - This method updates NinjaTrader with all available market data fields
   - Similar to the `UpdateMarketData` method in NimbleMain

4. **Updated MarketDataService**
   - Modified to use the new rich data structure approach
   - Updated both tick data and market depth processing
   - Added adapter access through Connector

5. **Added Adapter Access in Connector**
   - Added `GetAdapter` method to access the QAAdapter instance
   - This allows the MarketDataService to call the ProcessParsedTick method

These changes should help reduce the 250-1000ms delay observed between parsing a tick and its reflection in NinjaTrader, as mentioned in the PROJECT_ARCHITECTURE.md document.

## Update (2025-05-21 16:42:00 IST)

Fixed an issue with TradingHours being null for instruments:

1. **Added Segment-Specific Trading Hours Templates**
   - Updated `InstrumentManager.cs` to set appropriate trading hours templates based on instrument segment
   - For NSE, BSE, NFO, and BFO segments: Uses the "Nse" trading hours template
   - For MCX segments: Uses the "MCX" trading hours template
   - For all other segments: Uses the "Default 24 x 7" template
   - Falls back to any available template if the specific ones aren't found

2. **Applied Logic to Both New and Existing Instruments**
   - Used the same segment-based logic for both new instruments and when updating existing instruments
   - Added detailed logging to show which trading hours template is being used for each instrument

This fixes the "TradingHours==null" assertion failure that occurs when trying to view charts or retrieve historical data for instruments without a trading hours template.

## Core Idea

Shift from primarily using the generic `Action<MarketDataType, double, long, DateTime, long> cb` callback (provided by NinjaTrader during `SubscribeMarketData`) for all tick updates, to calling a new, specific public method on `QAAdapter` with a comprehensive `ParsedTickData` object. This new method will then be responsible for using `AdapterBase.UpdateMarketData(...)` methods.

--- 

## Step-by-Step Changes

### 1. Define a Rich Data Object (`ParsedTickData`)

*   **Action:** Create a new class to hold all relevant fields of a parsed tick.
*   **File to Create:** `QANinjaAdapter\Models\ParsedTickData.cs` (or a similar appropriate location).
*   **Content Example:**
    ```csharp
    // QANinjaAdapter/Models/ParsedTickData.cs
    namespace QANinjaAdapter.Models
    {
        using System;

        public class ParsedTickData
        {
            public double LastPrice { get; set; }
            public int LastQuantity { get; set; }      // Volume of the last trade
            public double Bid { get; set; }
            public int BidSize { get; set; }
            public double Ask { get; set; }
            public int AskSize { get; set; }
            public int CumulativeVolume { get; set; } // Cumulative volume for the day
            public DateTime Timestamp { get; set; }    // Exchange or highly accurate received timestamp
            
            // Optional: Other fields like Open, High, Low, Close, OI, AveragePrice
            // public double OpenPrice { get; set; }
            // public double HighPrice { get; set; }
            // public double LowPrice { get; set; }
            // public double ClosePrice { get; set; }
            // public int OpenInterest { get; set; }

            // Flags to indicate which fields are present in the current update
            public bool HasLast { get; set; } = false;
            public bool HasBid { get; set; } = false;
            public bool HasAsk { get; set; } = false;
            // Add Has flags for other optional fields if you include them
        }
    }
    ```
*   **Nimble Parallel:** This `ParsedTickData` object is conceptually similar to `nimbleStream.DataStream.DS_RealtimeResult` or `DS_HistoryOHLCResult` which are rich objects holding multiple market data fields.
    *   *Reference Nimble File:* `nimbleStreamClient\nimbleStreamClient\DataStream\DS_XXX.cs`

### 2. Modify Parser to Populate `ParsedTickData`

*   **Action:** Update your binary message parser to create and populate an instance of `ParsedTickData`.
*   **File to Modify:** `QANinjaAdapter\Services\WebSocket\WebSocketManager.cs` (likely the `ParseBinaryMessage` method, or its equivalent in your setup if parsing is handled differently for a single multiplexed WebSocket).
*   **Change:** Instead of returning a simple tuple like `(double LastPrice, int LastQuantity, int Volume, DateTime Timestamp)`, the parser should return a fully populated `ParsedTickData` object.
    ```csharp
    // In QANinjaAdapter/Services/WebSocket/WebSocketManager.cs
    // Change the signature and implementation of ParseBinaryMessage (or equivalent)
    using QANinjaAdapter.Models; // Add this

    public ParsedTickData ParseBinaryMessageToTickData(byte[] data, int expectedToken) // Renamed for clarity, or modify existing
    {
        var tickData = new ParsedTickData();
        // Your existing parsing logic to read packetCount, loop through packets...
        // Inside the loop when a matching packet for expectedToken is found:

        // Example for LTP:
        // int lastTradedPriceRaw = ReadInt32BE(data, offset + 4);
        // tickData.LastPrice = lastTradedPriceRaw / 100.0;
        // tickData.HasLast = true;

        // Example for LTQ (assuming it's part of the packet):
        // tickData.LastQuantity = ReadInt32BE(data, offset + X); // Adjust offset

        // Example for Volume (Cumulative):
        // tickData.CumulativeVolume = ReadInt32BE(data, offset + Y); // Adjust offset

        // Example for Exchange Timestamp:
        // int exchangeTimestampRaw = ReadInt32BE(data, offset + Z);
        // if (exchangeTimestampRaw > 0) tickData.Timestamp = UnixSecondsToLocalTime(exchangeTimestampRaw);
        // else tickData.Timestamp = DateTime.Now; // Or a more accurate receive time

        // Populate Bid/Ask if available in the binary packet
        // if (packetLength >= XX) { 
        //    tickData.Bid = ReadInt32BE(data, offset + B_off) / 100.0;
        //    tickData.BidSize = ReadInt32BE(data, offset + BS_off);
        //    tickData.HasBid = true;
        //    // ... and so on for Ask
        // }

        // If the loop processes one tick and returns, this is fine.
        // If it could process multiple, you might need a List<ParsedTickData>.
        return tickData; 
    }
    ```
*   **Nimble Parallel:** In `nimbleStreamClient.Client.ProcPacket`, various `Deserialize_DS_XXX` methods are called to convert byte arrays into specific `DS_` objects.
    *   *Reference Nimble File:* `nimbleStreamClient\nimbleStreamClient\Client.cs` (see `ProcPacket` and calls to `Deserialize_DS_...` methods).

### 3. Add Public Data Processing Method to `QAAdapter.cs`

*   **Action:** Create a new public method in `QAAdapter.cs` that accepts the `L1Subscription` (or just its `Instrument`) and the `ParsedTickData` object.
*   **File to Modify:** `QANinjaAdapter\QAAdapter.cs`.
*   **Content Example:**
    ```csharp
    // QANinjaAdapter/QAAdapter.cs
    using QANinjaAdapter.Models; // Add this

    public partial class QAAdapter // If your class is partial
    {
        // ... existing code ...

        public void ProcessTick(L1Subscription subscription, ParsedTickData tickData)
        {
            if (subscription == null || tickData == null)
            {
                LogMe($"[QAAdapter.ProcessTick] Null subscription or tickData. Symbol: {subscription?.Instrument?.FullName ?? "Unknown"}", NinjaTrader.Cbi.LogLevel.Warning);
                return;
            }

            Instrument instrument = subscription.Instrument;
            if (instrument == null) return;

            // Calculate trade volume (last trade size)
            int tradeVolume = 0;
            if (tickData.HasLast)
            {
                int cumulativeVolumeFromTick = tickData.CumulativeVolume;
                int volumeDelta = 0;
                if (subscription.PreviousVolume > 0 && cumulativeVolumeFromTick > subscription.PreviousVolume)
                {
                    volumeDelta = cumulativeVolumeFromTick - subscription.PreviousVolume;
                }
                // Always update previous volume, even if it's the first tick or no delta
                subscription.PreviousVolume = cumulativeVolumeFromTick;
                tradeVolume = volumeDelta > 0 ? volumeDelta : tickData.LastQuantity; 
            }
            
            // More robust timestamp handling - prefer exchange, fallback to a high-precision receive time
            DateTime finalTimestamp = tickData.Timestamp; // Assuming this is already handled well in parser

            // --- Option A: Comprehensive Update (if all data is usually present) ---
            // This is generally preferred if your ParsedTickData is rich
            // base.UpdateMarketData(
            //     instrument,
            //     tickData.HasAsk ? tickData.Ask : instrument.MarketData.Ask.Price, // Use current if not in tick
            //     tickData.HasAsk ? tickData.AskSize : instrument.MarketData.Ask.Volume,
            //     tickData.HasBid ? tickData.Bid : instrument.MarketData.Bid.Price,
            //     tickData.HasBid ? tickData.BidSize : instrument.MarketData.Bid.Volume,
            //     tickData.HasLast ? tickData.LastPrice : instrument.MarketData.Last.Price,
            //     tickData.HasLast ? (long)tradeVolume : instrument.MarketData.Last.Volume, // cast tradeVolume to long
            //     finalTimestamp
            // );
            // If you also have DailyHigh, DailyLow, DailyOpen, DailyVolume, OpenInterest, pass them here too.

            // --- Option B: Individual Updates (more granular) ---
            if (tickData.HasLast)
            {
                base.UpdateMarketData(instrument, MarketDataType.Last, tickData.LastPrice, tradeVolume, finalTimestamp);
            }
            if (tickData.HasBid)
            {
                base.UpdateMarketData(instrument, MarketDataType.Bid, tickData.Bid, tickData.BidSize, finalTimestamp);
            }
            if (tickData.HasAsk)
            {
                base.UpdateMarketData(instrument, MarketDataType.Ask, tickData.Ask, tickData.AskSize, finalTimestamp);
            }
            // Potentially update cumulative volume separately if it makes sense
            // if (tickData.HasLast) base.UpdateMarketData(instrument, MarketDataType.DailyVolume, tickData.CumulativeVolume, 0, finalTimestamp);

            // Choose Option A or B based on how complete your ParsedTickData usually is.
            // Option A can be more efficient if NT optimizes a single comprehensive update.
        }
    }
    ```
*   **Nimble Parallel:** This new `ProcessTick` method is analogous to public methods in `NimbleMain.cs` like `UpdateMarketData(string uiSymbol, DS_RealtimeResult value)` or `UpdateHistoricalOhlcData(...)`, which are called by `ProviderApi` with rich data objects.
    *   *Reference Nimble File:* `Ninjatrader.NimbleData\NinjaTrader.NimbleData\NimbleMain.cs`

### 4. Modify Central Message Handler to Call `QAAdapter.ProcessTick`

*   **Action:** In your main WebSocket message handling loop (where you receive and dispatch messages for your single WebSocket connection), after parsing the message into `ParsedTickData`, call the new `QAAdapter.ProcessTick` method.
*   **File to Modify:** This depends on your architecture. It could be in `QANinjaAdapter\Services\MarketData\MarketDataService.cs`, `QANinjaAdapter\Connector.cs`, or `QANinjaAdapter\Services\Zerodha\ZerodhaClient.cs` if that's where your central WebSocket `OnMessage` logic resides.
*   **Change Example (Conceptual - assuming the loop is in `MarketDataService`):**
    ```csharp
    // Conceptual: In QANinjaAdapter/Services/MarketData/MarketDataService.cs
    // (Or wherever your single WebSocket's message loop is)
    // You'll need a reference to the QAAdapter instance.
    // private QAAdapter _qaAdapterInstance; // Set this up during initialization

    private async Task ProcessIncomingWebSocketMessages()
    {
        // ... your WebSocket receive loop ...
        // byte[] rawMessage = await _webSocketManager.ReceiveMessageAsync(...);
        // string nativeSymbol = IdentifySymbolFromRawMessage(rawMessage); // Your logic to know which symbol this tick is for
        // ParsedTickData tickData = _webSocketManager.ParseBinaryMessageToTickData(rawMessage, tokenForSymbol); // tokenForSymbol might be part of IdentifySymbol...

        if (_qaAdapterInstance != null && tickData != null)
        {
            if (_qaAdapterInstance._l1Subscriptions.TryGetValue(nativeSymbol, out L1Subscription subscription))
            {
                // Instead of iterating subscription.L1Callbacks and calling the generic cb:
                // foreach (var cb in subscription.L1Callbacks.Values) { ... cb(...) ... }

                // Now, directly call the new method on QAAdapter:
                _qaAdapterInstance.ProcessTick(subscription, tickData);
            }
            else
            {
                // Log: No L1Subscription found for nativeSymbol
            }
        }
        // ... loop continues ...
    }
    ```
*   **Nimble Parallel:** In Nimble's architecture, `ProviderApi` (which subscribes to events from `nimbleStreamClient.Client`) calls methods on the `NimbleMain` adapter instance. E.g., `_adapter.UpdateMarketData(value.InstrumentIdentifier, value);`.
    *   *Reference Nimble File:* `Ninjatrader.NimbleData\NinjaTrader.NimbleData\DataClient\ProviderApi.cs` (see event handlers like `client_RealtimeResult`).

### 5. Ensure `L1Subscription` Class is Adequate

*   **Action:** Verify that your `L1Subscription` class (likely an inner class in `QAAdapter.cs` or in `QANinjaAdapter\Classes\`) contains the necessary fields.
*   **File to Modify:** `QANinjaAdapter\QAAdapter.cs` or `QANinjaAdapter\Classes\L1Subscription.cs`.
*   **Content Check/Update:**
    ```csharp
    // In QAAdapter.cs or QANinjaAdapter/Classes/L1Subscription.cs
    public class L1Subscription
    {
        public Instrument Instrument { get; set; }
        public SortedList<Instrument, Action<MarketDataType, double, long, DateTime, long>> L1Callbacks { get; set; }
        public int PreviousVolume { get; set; } // ESSENTIAL for trade volume calculation
        // public string NativeSymbolName { get; set; } // Recommended for easier logging/debugging
    }
    ```
    The `L1Callbacks` list might still be useful if some NinjaTrader features *only* operate via those callbacks, or if you want to maintain it for other types of data. However, for the primary tick data (Last, Bid, Ask), the `ProcessTick` method becomes the main pathway.

--- 

## Benefits of This Approach

*   **More Direct Control:** `QAAdapter` explicitly handles the decomposition of the `ParsedTickData` and calls `AdapterBase` methods.
*   **Potential for Comprehensive Updates:** Allows using `AdapterBase.UpdateMarketData` overloads that take multiple market data fields at once, which might be more efficient internally in NinjaTrader.
*   **Clearer Profiling Point:** The `QAAdapter.ProcessTick` method is a distinct step whose performance can be easily measured.

## Important Note on Performance

While this refactoring makes the adapter's data handover more controlled and potentially more efficient, it's crucial to remember: **if NinjaTrader's main data processing thread is already saturated, this change alone may not fully resolve high latency.** The primary benefit here is optimizing the adapter's responsibility in the data pipeline. Continued performance profiling, including within NinjaTrader's environment, is key.

---
