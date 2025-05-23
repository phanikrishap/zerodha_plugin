# Batch Subscription Binary Data Parsing Fix

## Problem Summary

After implementing batch subscription functionality, binary data parsing was broken. The logs showed:

1. **BatchSubscriptionManager was working correctly** - tokens were being queued and sent in batches
2. **Binary data was being received** - but showing as empty or garbled in logs
3. **The core issue**: The existing `ParseBinaryMessage` method was designed for individual subscriptions, not batch data

## Root Cause Analysis

The problem was in the `WebSocketManager.ParseBinaryMessage` method:

```csharp
// Check if this is our subscribed token
int iToken = ReadInt32BE(data, offset);
if (iToken != expectedToken)
{
    offset += packetLength; // Skip this packet
    continue;
}
```

In **individual subscription mode**, each WebSocket connection handles one instrument, so the parser expects data for that specific token.

In **batch subscription mode**, one WebSocket connection handles multiple instruments, so the binary data contains packets for multiple tokens in a single message. The parser was skipping all packets that didn't match the `expectedToken`, which meant most data was being ignored.

## Solution Implemented

### 1. Enhanced WebSocketManager

**Added new method for batch parsing:**
- `ParseBatchBinaryMessage()` - Processes all packets in a message and routes them to appropriate subscriptions
- `ParseSinglePacket()` - Extracted common parsing logic for individual packets

**Enhanced existing method:**
- `ParseBinaryMessage()` - Added better logging to show token mismatches for debugging

### 2. Updated L1Subscription Class

**Added InstrumentToken property:**
```csharp
public class L1Subscription
{
    // ... existing properties ...
    public int InstrumentToken { get; set; }
}
```

### 3. Updated QAAdapter

**Enhanced subscription creation:**
- Now retrieves and stores the instrument token when creating L1 subscriptions
- This enables the batch parser to match tokens to symbol names

## Key Changes Made

### Services/WebSocket/WebSocketManager.cs
- Added `ParseBatchBinaryMessage()` method for handling multiple instruments in one message
- Added `ParseSinglePacket()` helper method to avoid code duplication
- Enhanced logging in `ParseBinaryMessage()` to show token matching details
- Added proper using statements for Collections

### L1Subscription.cs
- Added `InstrumentToken` property to store the instrument token for each subscription

### QAAdapter.cs
- Modified `SubscribeMarketData()` to retrieve and store instrument tokens when creating subscriptions
- Added error handling for token retrieval failures

## How the Fix Works

### Before (Individual Subscription Mode)
1. Each symbol gets its own WebSocket connection
2. Binary data contains packets for only that symbol
3. Parser expects and finds the matching token

### After (Batch Subscription Mode)
1. One WebSocket connection handles multiple symbols
2. Binary data contains packets for multiple symbols
3. **New batch parser** processes all packets and routes them to appropriate subscriptions
4. **Token-to-symbol mapping** enables correct routing

### Batch Parsing Flow
1. `ParseBatchBinaryMessage()` receives binary data with multiple packets
2. For each packet:
   - Extract the instrument token
   - Look up the symbol name in L1 subscriptions using the token
   - Parse the packet data using `ParseSinglePacket()`
   - Return parsed data for all found instruments
3. Each parsed tick data is then processed by the existing `ProcessParsedTick()` method

## Benefits

1. **Backward Compatibility**: Individual subscription mode still works with the original `ParseBinaryMessage()` method
2. **Batch Support**: New `ParseBatchBinaryMessage()` method handles multiple instruments efficiently
3. **Better Debugging**: Enhanced logging shows exactly which tokens are being processed/skipped
4. **Proper Routing**: Token-to-symbol mapping ensures data reaches the correct subscriptions

## Testing Recommendations

1. **Test individual subscriptions** to ensure backward compatibility
2. **Test batch subscriptions** with the new parsing logic
3. **Monitor logs** for token matching and parsing success messages
4. **Verify data flow** from binary parsing through to NinjaTrader callbacks

## Future Considerations

1. The BatchSubscriptionManager implementation is likely in an external DLL or library
2. Consider implementing automatic detection of batch vs individual mode
3. May need to modify MarketDataService to use the new batch parsing method when appropriate
4. Consider adding configuration options for batch size and timing

## Log Messages to Monitor

- `[BATCH-PARSE-DEBUG]` - Shows packet processing in batch mode
- `[PARSE-TOKEN]` - Shows token matching in individual mode  
- `[PARSE-SKIP]` - Shows when packets are skipped due to token mismatch
- `[BATCH-PARSE-SUCCESS]` - Shows successful parsing of individual packets in batch
- `[BATCH-PARSE-COMPLETE]` - Shows summary of batch parsing results

This fix addresses the core issue where batch subscription binary data was not being parsed correctly due to the single-token expectation in the original parser.
