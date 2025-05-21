# Changelog

## 2025-05-21

### Latency Optimizations and Fixes

- **Implemented Asynchronous Logging**: Modified `MarketDataService.cs` to move tick data logging (`LogTickInformationAsync`) to a background thread using `Task.Run()`. This prevents file I/O operations from blocking the main tick processing pipeline, significantly reducing potential latency.
- **Conditional Verbose Logging**: Introduced a new configuration option `EnableVerboseTickLogging` in `config.json` (under `GeneralSettings`). This allows an admin to toggle detailed tick-by-tick logging. When disabled (default), it reduces logging overhead on the critical path. The `ConfigurationManager.cs` was updated to load and provide this setting.
- **Reviewed Tick Parsing**: Examined `WebSocketManager.cs` and `MarketDataService.cs` for obvious bottlenecks in the tick parsing and processing logic. The primary identified bottleneck was synchronous logging.

**Reasoning for Changes:**

The user reported latency issues where tick data was delayed by 250ms to 1 second between the time it was parsed and when it was received by NinjaTrader. The investigation pointed towards synchronous I/O operations (primarily logging) as the likely cause.

The implemented changes address this by:
1.  Moving all potentially slow logging operations (like writing to a CSV or detailed console logs) to background threads, ensuring the main thread responsible for receiving and dispatching ticks to NinjaTrader remains unblocked.
2.  Providing a mechanism to reduce the volume of logging, as excessive logging itself can contribute to performance degradation.

These optimizations aim to ensure that tick data flows to NinjaTrader with minimal delay, improving the responsiveness and accuracy of the trading adapter.
