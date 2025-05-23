# Refactoring MarketDataService: Progress Report

## I. Project Goal
The primary objective is to refactor `MarketDataService.cs` in the `QANinjaAdapter` project. Key aims include:
- Implementing a shared WebSocket connection for all market data subscriptions to improve efficiency.
- Replacing existing logging with a custom logging solution (`QANinjaAdapter.Logger`).
- Improving code organization by moving class definitions like `L1Subscription` and [MarketDataEventArgs](cci:2://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/Models/MarketData/MarketDataEventArgs.cs:5:4-28:5) into their own dedicated files.
- Resolving existing lint errors and ensuring robust error handling.

## II. Key Architectural Changes & Modifications

### 1. Shared WebSocket Connection
- **`MarketDataService.cs` Modified:**
    - Introduced logic to manage a single, shared WebSocket connection (`_sharedWebSocketClient`).
    - Implemented `EnsureSharedConnectionAsync()` to establish and maintain this connection.
    - Added `StartSharedMessageLoopAsync()` to process incoming messages from the shared WebSocket. (Parsing logic for binary data is still pending).
    - Methods like `SubscribeToTicks` and `UnsubscribeFromTicks` were adapted to use the shared connection and manage subscriptions via a `ConcurrentDictionary` (`_l1Subscriptions`).

### 2. Code Organization
- **`L1Subscription.cs` Created:**
    - The `L1Subscription` class definition was moved from `MarketDataService.cs` to its own file: `QANinjaAdapter/Models/MarketData/L1Subscription.cs`.
    - Namespace: `QANinjaAdapter.Models.MarketData`.
- **[MarketDataEventArgs.cs](cci:7://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/Models/MarketData/MarketDataEventArgs.cs:0:0-0:0) Created:**
    - The [MarketDataEventArgs](cci:2://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/Models/MarketData/MarketDataEventArgs.cs:5:4-28:5) class definition was moved from `MarketDataService.cs` to its own file: [QANinjaAdapter/Models/MarketData/MarketDataEventArgs.cs](cci:7://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/Models/MarketData/MarketDataEventArgs.cs:0:0-0:0).
    - Namespace: `QANinjaAdapter.Models.MarketData`.

### 3. Dependency Management & Initialization
- **Singleton Usage in `MarketDataService.cs`:**
    - `InstrumentManager`: Changed from interface injection (`IInstrumentManager`) to using the concrete singleton `InstrumentManager.Instance`.
    - `WebSocketManager`: Changed to use the concrete singleton `WebSocketManager.Instance`.
    - The `MarketDataService` itself is structured as a singleton, initialized with `ConfigurationManager.Instance`.
- **`BrokerManager` Handling:**
    - The source file for `BrokerManager.cs` could not be located.
    - Its usage (field, instantiation, and `using` directive) has been commented out in `MarketDataService.cs` to allow compilation to proceed. The impact of its absence needs to be assessed.

### 4. Project File ([QANinjaAdapter.csproj](cci:7://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/QANinjaAdapter.csproj:0:0-0:0)) Updates
- **File Inclusions:**
    - Added `<Compile Include="Models\MarketData\L1Subscription.cs" />`.
    - Added `<Compile Include="Models\MarketData\MarketDataEventArgs.cs" />`.
    - Added/Verified `<Compile Include="Services\WebSocket\WebSocketManager.cs" />`.
    - Corrected path for `InstrumentManager.cs` to `<Compile Include="Services\Instruments\InstrumentManager.cs" />`.
- **File Exclusions:**
    - Removed the `<Compile Include="..." />` entry for the missing `BrokerManager.cs`.
- **Project References:**
    - Verified the `<ProjectReference Include="..\QABrokerAPI\QABrokerAPI.csproj">` is present and appears correct.

## III. Current Status & Remaining Blockers

- **Primary Blocker: `MarketDataType` Not Found**
    - **Error:** `CS0246: The type or namespace name 'MarketDataType' could not be found (are you missing a using directive or an assembly reference?)`
    - **Location:** [QANinjaAdapter\Models\MarketData\MarketDataEventArgs.cs](cci:7://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/Models/MarketData/MarketDataEventArgs.cs:0:0-0:0) (lines 9 and 20).
    - **Context:** This error persists despite:
        1.  The [QANinjaAdapter.csproj](cci:7://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/QANinjaAdapter.csproj:0:0-0:0) file having a correct `<ProjectReference>` to the `QABrokerAPI` project (which should define `MarketDataType`).
        2.  The [MarketDataEventArgs.cs](cci:7://file:///c:/Users/Hello/CascadeProjects/zerodha_adapter/QANinjaAdapter/Models/MarketData/MarketDataEventArgs.cs:0:0-0:0) file containing the `using QABrokerAPI.Common.Enums;` directive, which is the expected namespace for `MarketDataType`.
    - **Implication:** This indicates a fundamental issue with how the `QANinjaAdapter` project is referencing or compiling the `QABrokerAPI` project, or an issue within `QABrokerAPI` itself.

- **Other Potential Issues (Currently Masked):**
    - Once the `MarketDataType` error is resolved, other compilation errors might surface, particularly related to the members and constructor of `L1Subscription.cs` or the data parsing logic in `MarketDataService.cs`.
    - The functionality previously provided by `BrokerManager` is currently unavailable.

## IV. Next Steps to Resolve Blockers & Complete Refactoring

1.  **Diagnose and Fix `MarketDataType` Resolution Issue:**
    *   **Verify `QABrokerAPI` Project:**
        *   Ensure the `QABrokerAPI` project builds successfully without any errors.
        *   Open the `QABrokerAPI` project and confirm that the `MarketDataType` enum is defined as `public` within the `QABrokerAPI.Common.Enums` namespace.
    *   **Thorough Clean and Rebuild:**
        *   Close Visual Studio.
        *   Manually delete the `bin` and `obj` folders in both the `QANinjaAdapter` and `QABrokerAPI` project directories.
        *   Re-open Visual Studio and perform a full "Rebuild Solution".
    *   **Check Target Frameworks:** Ensure `QANinjaAdapter` and `QABrokerAPI` are targeting compatible .NET Framework versions.

2.  **Address `L1Subscription` and Other Compilation Errors:**
    *   Once `MarketDataType` is resolved, address any subsequent compilation errors in `MarketDataService.cs`, `L1Subscription.cs`, etc.

3.  **Implement Binary Data Parsing in `StartSharedMessageLoopAsync`:**
    *   Develop the logic to parse the binary tick data received from the WebSocket according to the broker's format.

4.  **Resolve `BrokerManager` Status:**
    *   Determine if `BrokerManager` functionality is critical.
    *   If so, locate the correct `BrokerManager.cs` file and integrate it, or re-implement the necessary logic.

5.  **Integrate Custom Logging:**
    *   Ensure `QANinjaAdapter.Logger` is used for all relevant logging within `MarketDataService`.

6.  **Thorough Testing:**
    *   Test market data subscription, unsubscription, data reception, and error handling.
