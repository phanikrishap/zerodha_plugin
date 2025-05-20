# QA NinjaAdapter

A custom adapter for connecting NinjaTrader 8 to Indian trading platforms, primarily Zerodha, with additional support for other brokers.

## Overview

QA NinjaAdapter is a comprehensive bridge solution that enables NinjaTrader 8 to seamlessly connect with Zerodha and other Indian brokers. The adapter provides real-time market data integration, historical data access, and a flexible broker configuration system designed for the Indian markets.

## Features

- **Multi-Broker Support**: Connect to multiple Indian brokers including Zerodha, Upstox, and TrueData
- **Dual Connection Types**: 
  - WebSocket for real-time market data streaming
  - REST API for historical data retrieval
- **External Configuration**: User-editable JSON configuration for API credentials
- **Standalone UI**: Dedicated configuration interface for credential management
- **Dynamic Broker Switching**: Switch between different brokers without restarting NinjaTrader
- **Multiple Symbol Formats**: Support for various symbol formats across different brokers

## Project Structure

The solution consists of multiple projects:

- **QANinjaAdapter**: Core adapter implementation for NinjaTrader integration
- **QABrokerAPI**: Broker-specific API implementations (Zerodha, Upstox, Binance)
- **QAAddOnUI**: Standalone UI components for configuration
- **QA.Tests**: Unit and integration tests
- **Test Applications**: Development test harnesses

## Architecture

```
┌──────────────────┐           ┌───────────────────────────┐
│                  │           │      QANinjaAdapter       │
│   NinjaTrader    │◄─────────►│                           │
│                  │           │  ┌─────────┐ ┌─────────┐  │
└──────────────────┘           │  │Connector│ │ Parsers │  │
                               │  └─────────┘ └─────────┘  │
                               │        │          │       │
                               │        ▼          ▼       │
                               │  ┌─────────┐ ┌─────────┐  │
                               │  │Controls │ │ViewModels│  │
                               │  └─────────┘ └─────────┘  │
                               └───────┬───────────────────┘
                                       │
                                       │
                                       ▼
┌───────────────────────────────────────────────────────────────┐
│                         QABrokerAPI                           │
│                                                               │
│  ┌───────────────────────────────────────────────────────┐    │
│  │                        Common                         │    │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────────────┐ │    │
│  │  │   Models   │ │   Enums    │ │     Interfaces     │ │    │
│  │  └────────────┘ └────────────┘ └────────────────────┘ │    │
│  └───────────────────────────────────────────────────────┘    │
│                                                               │
│  ┌───────────────┐   ┌───────────────┐   ┌───────────────┐    │
│  │    Zerodha    │   │    Upstox     │   │    Binance    │    │
│  │ ┌───────────┐ │   │ ┌───────────┐ │   │ ┌───────────┐ │    │
│  │ │Websockets │ │   │ │REST Client│ │   │ │Websockets │ │    │
│  │ └───────────┘ │   │ └───────────┘ │   │ └───────────┘ │    │
│  │ ┌───────────┐ │   │ ┌───────────┐ │   │ ┌───────────┐ │    │
│  │ │REST Client│ │   │ │  Models   │ │   │ │  Models   │ │    │
│  │ └───────────┘ │   │ └───────────┘ │   │ └───────────┘ │    │
│  └───────────────┘   └───────────────┘   └───────────────┘    │
│                                                               │
└───────────────────────────────────────────────────────────────┘
            │                  │                  │
            ▼                  ▼                  ▼
   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
   │  Zerodha API    │ │   Upstox API    │ │   Binance API   │
   │                 │ │                 │ │                 │
   └─────────────────┘ └─────────────────┘ └─────────────────┘
```

### Core Components

1. **Connector Class**: Main integration point with NinjaTrader
2. **QABrokerAPI**: Unified API layer for different brokers
   - Common interfaces and models
   - Broker-specific implementations (Zerodha, Upstox, Binance)
3. **WebSocket Managers**: Real-time data streaming for each broker
4. **Parsers**: Data format converters between broker APIs and NinjaTrader
5. **Controls & ViewModels**: UI components for configuration
6. **AddOn UI**: Standalone configuration and management interface

## Installation

1. Build the solution in Visual Studio
2. Copy the generated DLL to:
   ```
   %UserProfile%\Documents\NinjaTrader 8\bin\Custom
   ```
3. Create the configuration folder:
   ```
   %UserProfile%\Documents\NinjaTrader 8\QAAdapter
   ```
4. Copy the sample configuration file to the above folder
5. Restart NinjaTrader 8
6. Add the Zerodha data connection in NinjaTrader's Connections menu

## Configuration

Create a `config.json` file with the following structure:

```json
{
  "Active": {
    "Websocket": "Zerodha",
    "Historical": "Zerodha"
  },
  "Zerodha": {
    "Api": "your_api_key",
    "Secret": "your_secret_key",
    "AccessToken": "your_access_token"
  },
  "Upstox": {
    "Api": "your_api_key",
    "Secret": "your_secret_key",
    "AccessToken": "your_access_token",
    "TOTP": "your_totp_secret"
  }
}
```

## Development & Build Process

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8 (for NinjaTrader compatibility)
- NinjaTrader 8 installed

### Build Process

1. Clone the repository
2. Open QANinjaAdapter.sln in Visual Studio
3. Build the solution in Debug or Release mode
4. The build process will:
   - Compile all projects in the solution
   - Copy necessary DLLs to the output directory
   - Generate XML documentation



### Development Tips

- Use the FileSystemWatcher to monitor configuration changes without restarting NinjaTrader
- Create unit tests for broker-specific code in the QA.Tests project
- Use Test Applications project for standalone testing outside of NinjaTrader environment

## Project Components

### QANinjaAdapter
- **Classes**: Core implementation classes
  - **Binance**: Implementation for Binance connectivity
  - **Klines**: Candlestick data processing
  - **Symbols**: Symbol mapping and management
- **Controls**: UI elements for NinjaTrader integration
- **Parsers**: Data transformation between broker formats and NinjaTrader
- **QAAdapterAddOn**: NinjaTrader AddOn implementation
  - **ViewModels**: MVVM pattern implementation 
- **ViewModels**: Data binding models

### QABrokerAPI
- **Common**: Shared functionality across brokers
  - **Caching**: Data caching mechanisms
  - **Converter**: Data format converters
  - **Enums**: Enumeration types
  - **Extensions**: Extension methods
  - **Interfaces**: API contracts
  - **Models**: Data models
- **Zerodha**: Zerodha-specific implementation
  - **Websockets**: Real-time data streaming
- **Upstox**: Upstox-specific implementation
- **Binance**: Binance-specific implementation

### QAAddOnUI
- Standalone UI for configuration outside NinjaTrader

### QA.Tests
- Unit and integration tests for all components

## Roadmap

- [ ] Complete Zerodha connection implementation
  - [ ] WebSocket connectivity for real-time data
  - [ ] Historical data API integration
  - [ ] Symbol mapping
- [ ] Develop standalone configuration UI
  - [ ] Credential management interface
  - [ ] Broker selection
  - [ ] Connection testing
- [ ] Add support for additional Indian brokers
  - [ ] Expand Upstox integration
  - [ ] Add TrueData support
- [ ] Implement order execution functionality
- [ ] Add automated testing framework
  - [ ] Unit tests for all components
  - [ ] Integration tests with broker APIs

## License

This project is proprietary software.

## Acknowledgements

- NinjaTrader developer community
- Zerodha Kite Connect API documentation
- Upstox API documentation
- Open-source libraries used in the project
