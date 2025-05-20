# Zerodha Adapter Architecture

This document outlines the architecture and file structure of the Zerodha Adapter for NinjaTrader, focusing on the service-based approach implemented during refactoring.

## Project Structure

```
QANinjaAdapter/
├── Connector.cs                 # Main integration point with NinjaTrader
├── L1Subscription.cs            # Manages market data subscriptions and callbacks
├── NinjaTraderInstrumentManager.cs # Handles instrument creation and management
├── InstrumentDefinitionService.cs # Manages instrument definitions
├── Services/
│   ├── IZerodhaHttpApiService.cs  # Interface for HTTP API interactions
│   ├── ZerodhaHttpApiService.cs   # Implementation of HTTP API service
│   ├── IZerodhaWebSocketService.cs # Interface for WebSocket connections
│   ├── ZerodhaWebSocketService.cs  # Implementation of WebSocket service
│   ├── IInstrumentDefinitionService.cs # Interface for instrument definitions
│   ├── InstrumentDefinitionService.cs  # Implementation of instrument definition service
│   ├── ITickDataService.cs        # Interface for tick data processing
│   ├── TickDataService.cs         # Implementation of tick data service
│   ├── ISymbolMappingService.cs   # Interface for symbol mapping
│   └── SymbolMappingService.cs    # Implementation of symbol mapping service
└── ... (other files)
```

## Service Architecture

The refactoring introduced a service-based architecture that follows the Single Responsibility Principle. Each service has a well-defined interface and implementation:

### Core Services

1. **ZerodhaHttpApiService**
   - Responsible for all REST API interactions with Zerodha
   - Handles authentication, historical data retrieval, and other API requests
   - Implements IZerodhaHttpApiService interface

2. **ZerodhaWebSocketService**
   - Manages WebSocket connections to Zerodha
   - Handles real-time market data streaming
   - Processes binary and text messages from the WebSocket
   - Implements IZerodhaWebSocketService interface

### New Services (Refactored)

3. **TickDataService**
   - Manages tick data subscriptions and processing
   - Delegates WebSocket communication to ZerodhaWebSocketService
   - Converts raw tick data to NinjaTrader market data events
   - Implements ITickDataService interface

4. **SymbolMappingService**
   - Handles mapping between NinjaTrader symbols and Zerodha trading symbols
   - Manages instrument token retrieval and caching
   - Implements ISymbolMappingService interface

5. **InstrumentDefinitionService**
   - Manages instrument definitions and metadata
   - Handles loading and parsing of instrument data
   - Provides token lookup functionality
   - Implements IInstrumentDefinitionService interface

## Dependency Flow

```
                  ┌─────────────────┐
                  │                 │
                  │   Connector     │
                  │                 │
                  └───────┬─────────┘
                          │
                          │ depends on
                          ▼
┌─────────────┬───────────┬────────────┬─────────────────┐
│             │           │            │                 │
│ HttpApi     │ WebSocket │ TickData   │ SymbolMapping   │
│ Service     │ Service   │ Service    │ Service         │
│             │           │            │                 │
└─────────────┘           └────┬───────┘                 │
                               │                         │
                               │ depends on              │
                               ▼                         │
                          ┌────────────┐                 │
                          │            │                 │
                          │ WebSocket  │◄────────────────┘
                          │ Service    │
                          │            │
                          └────────────┘
```

## Key Refactoring Benefits

1. **Reduced Connector.cs Size**
   - Moved specialized functionality to dedicated service classes
   - Simplified methods by delegating to appropriate services

2. **Improved Testability**
   - Services with clear interfaces can be mocked for unit testing
   - Dependency injection pattern makes testing individual components easier

3. **Better Separation of Concerns**
   - Each service has a single responsibility
   - Clear boundaries between different parts of the system

4. **Enhanced Maintainability**
   - Smaller, focused classes are easier to understand and modify
   - New features can be added by extending existing services or adding new ones

5. **Simplified Debugging**
   - Issues can be isolated to specific services
   - Clearer error handling and logging boundaries

## Service Interfaces

### ITickDataService

```csharp
public interface ITickDataService
{
    Task SubscribeToTicksAsync(
        string nativeSymbolName, 
        QABrokerAPI.Common.Enums.MarketType marketType, 
        string ntSymbol,
        ConcurrentDictionary<string, L1Subscription> l1Subscriptions, 
        QABrokerAPI.Zerodha.Websockets.WebSocketConnectionFunc connectionFunc);
    
    void ProcessTick(
        string nativeSymbolName, 
        MarketDataEventArgs marketDataEventArgs, 
        ConcurrentDictionary<string, L1Subscription> l1Subscriptions);
}
```

### ISymbolMappingService

```csharp
public interface ISymbolMappingService
{
    string GetTradingSymbolFromNtSymbol(string ntSymbol);
    
    Task<long> GetInstrumentTokenAsync(
        string tradingSymbol, 
        QABrokerAPI.Common.Enums.MarketType marketType);
}
```

## Future Improvements

1. **Complete Service Migration**
   - Move remaining functionality from Connector.cs to appropriate services

2. **Enhanced Error Handling**
   - Implement comprehensive error handling in each service
   - Add retry mechanisms for network operations

3. **Logging Improvements**
   - Add structured logging throughout the services
   - Implement log levels for better debugging

4. **Performance Optimizations**
   - Add caching where appropriate
   - Optimize data processing pipelines

5. **Additional Services**
   - Consider adding an OrderService for trade execution
   - Implement a ConfigurationService for managing settings
