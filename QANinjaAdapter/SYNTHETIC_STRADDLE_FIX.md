# Synthetic Straddle Subscription Fix

## Problem
When synthetic straddle instruments (STRDL) are subscribed in the Market Analyzer, the system was only logging "No direct WebSocket subscription needed" but not automatically subscribing to the constituent CE/PE legs. This resulted in no market data for the individual options that make up the straddle.

## Root Cause
The `SubscribeMarketData` method in `QAAdapter.cs` correctly identified synthetic instruments but did not automatically subscribe to their constituent legs using the instrument tokens from `mapped_instruments.json`.

## Solution
Add two new methods and modify the synthetic instrument handling in `SubscribeMarketData`:

### 1. Add New Methods

```csharp
/// <summary>
/// Subscribes to constituent legs of a synthetic straddle instrument
/// </summary>
private async Task SubscribeToStraddleLegs(string syntheticSymbol)
{
    if (_syntheticStraddleService == null)
        return;

    // Find the straddle definition for this synthetic symbol
    foreach (var state in _syntheticStraddleService.GetStraddleStates())
    {
        if (state.Definition.SyntheticSymbolNinjaTrader.Equals(syntheticSymbol, StringComparison.OrdinalIgnoreCase))
        {
            var ceSymbol = state.Definition.CESymbol;
            var peSymbol = state.Definition.PESymbol;
            
            Logger.Info($"QAAdapter: Subscribing to straddle legs for {syntheticSymbol}: CE={ceSymbol}, PE={peSymbol}");
            
            // Subscribe to CE leg
            await SubscribeToLegInstrument(ceSymbol);
            
            // Subscribe to PE leg
            await SubscribeToLegInstrument(peSymbol);
            
            break;
        }
    }
}

/// <summary>
/// Subscribes to a single leg instrument (CE or PE)
/// </summary>
private async Task SubscribeToLegInstrument(string legSymbol)
{
    try
    {
        // Check if already subscribed
        lock (QAAdapter._lockLiveSymbol)
        {
            if (this._marketLiveDataSymbols.Contains(legSymbol))
            {
                Logger.Info($"QAAdapter: Leg {legSymbol} already subscribed.");
                return;
            }
            this._marketLiveDataSymbols.Add(legSymbol);
        }

        // Get instrument token for the leg
        var instrumentManager = InstrumentManager.Instance;
        if (instrumentManager == null)
        {
            Logger.Error($"QAAdapter: InstrumentManager.Instance is null. Cannot get token for {legSymbol}.");
            return;
        }

        int instrumentToken = (int)await instrumentManager.GetInstrumentToken(legSymbol);
        
        if (instrumentToken == 0)
        {
            Logger.Error($"QAAdapter: Failed to get instrument token for leg {legSymbol}. Subscription not queued.");
            return;
        }

        if (_batchSubscriptionManager != null)
        {
            _batchSubscriptionManager.QueueInstrumentSubscription(instrumentToken);
            Logger.Info($"QAAdapter: Successfully queued token {instrumentToken} ({legSymbol}) for batch subscription (straddle leg).");
        }
        else
        {
            Logger.Error($"QAAdapter: BatchSubscriptionManager is null. Cannot queue token for leg {legSymbol}.");
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"QAAdapter: Error subscribing to leg instrument {legSymbol}: {ex.Message}");
    }
}
```

### 2. Modify Synthetic Instrument Handling

Replace the existing synthetic instrument handling block in `SubscribeMarketData`:

```csharp
if (isSynthetic)
{
    // For synthetic instruments, we don't subscribe to the WebSocket directly
    // Instead, we automatically subscribe to the constituent legs
    Logger.Info($"Detected synthetic instrument subscription: {name}. Subscribing to constituent legs.");
    
    // We still need to add it to _marketLiveDataSymbols to track the subscription
    lock (QAAdapter._lockLiveSymbol)
    {
        if (!this._marketLiveDataSymbols.Contains(name))
        {
            this._marketLiveDataSymbols.Add(nativeSymbolName);
        }
    }
    
    // Subscribe to the constituent legs asynchronously
    Task.Run(async () =>
    {
        try
        {
            await SubscribeToStraddleLegs(name);
        }
        catch (Exception ex)
        {
            Logger.Error($"QAAdapter: Error subscribing to straddle legs for {name}: {ex.Message}");
        }
    });
    
    return;
}
```

## Expected Behavior After Fix

1. When a synthetic straddle instrument (e.g., `NIFTY25052923650STRDL`) is subscribed:
   - The system will log: "Detected synthetic instrument subscription: NIFTY25052923650STRDL. Subscribing to constituent legs."
   - It will automatically find the CE and PE symbols from the straddle configuration
   - It will subscribe to both legs using their instrument tokens from `mapped_instruments.json`
   - You should see logs like: "Successfully queued token XXXXX (NIFTY25052923650CE) for batch subscription (straddle leg)."

2. The BatchSubscriptionManager will then include all the individual CE/PE tokens in its batch subscription requests to Zerodha's WebSocket.

3. Market data will flow for the individual options, which will then be used by the SyntheticStraddleService to calculate and publish synthetic straddle prices.

## Files to Modify
- `QAAdapter.cs` - Add the two new methods and modify the synthetic instrument handling as shown above.

## Testing
After applying this fix, you should see:
1. Logs showing subscription to individual CE/PE legs when straddles are added to Market Analyzer
2. Instrument tokens being queued for batch subscription
3. Market data flowing for the individual options
4. Synthetic straddle prices being calculated and displayed
