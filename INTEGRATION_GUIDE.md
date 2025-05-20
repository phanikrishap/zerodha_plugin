# Zerodha Adapter Integration Guide

This document provides guidance on integrating the refactored service classes into the existing Zerodha Adapter codebase.

## Service Integration Approach

### 1. Interface Definitions

The new service interfaces (`ITickDataService` and `ISymbolMappingService`) are currently defined in the same files as their implementations. To properly integrate them:

1. **Option A**: Keep the interfaces in the same files as their implementations, but ensure proper namespace imports in Connector.cs.

2. **Option B**: Extract interfaces to separate files (recommended for larger projects):
   - Create `ITickDataService.cs` and `ISymbolMappingService.cs` files
   - Move interface definitions to these files
   - Update references in implementation classes

### 2. Connector.cs Integration

To integrate the services with Connector.cs:

1. **Add proper using directives**:
   ```csharp
   using QANinjaAdapter.Services;
   ```

2. **Define service fields**:
   ```csharp
   private readonly ITickDataService _tickDataService;
   private readonly ISymbolMappingService _symbolMappingService;
   ```

3. **Initialize services in constructor**:
   ```csharp
   public Connector(/* other dependencies */)
   {
       // Initialize other services
       _symbolMappingService = new SymbolMappingService(
           _instrumentDefinitionService, 
           _httpApiService, 
           _configurationService);
           
       _tickDataService = new TickDataService(
           _webSocketService, 
           _symbolMappingService);
   }
   ```

4. **Delegate functionality to services**:
   ```csharp
   // Example: Subscribing to ticks
   public async Task SubscribeToTicks(string symbol, MarketType marketType)
   {
       string nativeSymbolName = _symbolMappingService.GetTradingSymbolFromNtSymbol(symbol);
       await _tickDataService.SubscribeToTicksAsync(
           nativeSymbolName,
           marketType,
           symbol,
           _l1Subscriptions,
           WebSocketConnectionFunc);
   }
   ```

### 3. Resolving Common Errors

#### Missing Field References

If you encounter errors like `The name '_l1Subscriptions' does not exist in the current context`:

1. Ensure the field is defined in the Connector class:
   ```csharp
   private readonly ConcurrentDictionary<string, L1Subscription> _l1Subscriptions 
       = new ConcurrentDictionary<string, L1Subscription>();
   ```

#### WebSocketConnectionFunc Issues

If you encounter errors like `'WebSocketConnectionFunc' is a type, which is not valid in the given context`:

1. Define a connection function delegate:
   ```csharp
   private WebSocketConnectionFunc WebSocketConnectionFunc => 
       (uri, headers) => _webSocketService.ConnectAsync(uri, headers, _globalCts.Token);
   ```

#### Interface Implementation Issues

If you encounter errors like `'Connector' does not implement interface member 'INotifyPropertyChanged.PropertyChanged'`:

1. Implement the missing interface member:
   ```csharp
   public event PropertyChangedEventHandler PropertyChanged;

   protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
   {
       PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
   }
   ```

## Testing the Integration

After implementing the changes:

1. **Compile the project** to check for any remaining errors
2. **Test basic functionality**:
   - Connection to Zerodha API
   - Symbol mapping
   - Tick data subscription
   - Historical data retrieval

## Troubleshooting

### Common Issues

1. **Ambiguous References**: If you encounter ambiguous reference errors, ensure you're using fully qualified type names:
   ```csharp
   QABrokerAPI.Common.Enums.MarketType marketType
   ```

2. **Missing Implementations**: If services are missing methods required by interfaces, implement them:
   ```csharp
   public void UnsubscribeFromTicks(string nativeSymbolName, ConcurrentDictionary<string, L1Subscription> l1Subscriptions)
   {
       // Implementation
   }
   ```

3. **Duplicate Definitions**: If you encounter errors about duplicate definitions, check for:
   - Multiple interface definitions
   - Multiple class implementations
   - Namespace conflicts

## Next Steps

1. **Complete the refactoring** by addressing all compilation errors
2. **Add unit tests** for the new service classes
3. **Document the changes** in the project documentation
4. **Consider further refactoring** opportunities:
   - Historical data service
   - Configuration service improvements
   - Error handling enhancements
