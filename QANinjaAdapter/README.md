# QANinjaAdapter

A NinjaTrader adapter for connecting to the Zerodha trading platform, enabling real-time market data, historical data, and trading operations.

## Overview

QANinjaAdapter serves as a bridge between NinjaTrader and the Zerodha trading platform. It allows NinjaTrader users to access Indian market data through Zerodha's API, providing real-time market data, historical data, and trading capabilities.

## Features

- Real-time market data streaming via WebSocket
- Historical data retrieval
- Market depth (Level 2) data
- Support for stocks, futures, and options
- Efficient data processing with minimal latency
- Comprehensive market data including Last, Bid, Ask, Volume, Open, High, Low, Close, and Open Interest

## Architecture

The adapter follows a service-oriented architecture with the following key components:

1. **Connector**: The main entry point that orchestrates the interaction between NinjaTrader and the various services
2. **QAAdapter**: The NinjaTrader adapter implementation that connects to the Connector
3. **Services**: Specialized services for configuration, market data, instruments, and WebSocket communication
4. **Models**: Rich data structures for representing market data and other entities

### Data Flow

The adapter uses a rich data structure approach for processing market data:

1. Binary WebSocket messages are parsed into comprehensive `ZerodhaTickData` objects
2. These objects contain all market data fields (Last, Bid, Ask, Volume, etc.)
3. A dedicated method (`ProcessParsedTick`) processes these objects and updates NinjaTrader
4. This approach provides more direct control over how data is fed into NinjaTrader, reducing latency

## Requirements

- NinjaTrader 8
- .NET Framework 4.8
- Zerodha trading account with API access

## Configuration

The adapter requires the following configuration:

- Zerodha API Key
- Zerodha API Secret
- Zerodha Access Token

These can be configured in the adapter settings within NinjaTrader.

## Recent Improvements

Recent architectural improvements have focused on reducing latency between receiving market data and its reflection in NinjaTrader charts and indicators. By adopting a rich data structure approach similar to other high-performance data providers, the adapter now processes market data more efficiently, potentially reducing latency by hundreds of milliseconds.
