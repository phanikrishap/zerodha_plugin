# Synthetic Straddle Processor (SSP) for QANinjaAdapter

## Overview

The Synthetic Straddle Processor (SSP) is a middle-layer component integrated into the QANinjaAdapter C# project. It processes real-time tick data for individual Call (CE) and Put (PE) options, calculates the synthetic straddle price (CE price + PE price), and forwards this as a distinct synthetic tick directly to NinjaTrader.

## Features

- **Real-time Synthetic Price Generation**: Calculates and disseminates the synthetic straddle price immediately upon receipt of a new tick for either constituent leg.
- **Dynamic Instrument Definition**: Allows for the dynamic creation and management of synthetic straddle instruments based on external configuration.
- **Asynchronous Tick Handling**: Robustly manages and processes asynchronous tick arrivals from multiple underlying option legs.
- **Seamless NinjaTrader Integration**: Provides synthetic price updates to NinjaTrader in a format it can consume as real-time tick data, enabling immediate charting and analysis.

## Installation

1. Copy the `SyntheticInstruments` folder to your QANinjaAdapter project directory.
2. Copy the `straddles_config.json` file to the `Documents\NinjaTrader 8\QAAdapter\` directory.
3. Add the synthetic straddle instruments to your `mapped_instruments.json` file (typically located in `Documents\NinjaTrader 8\QAAdapter\`).

## Configuration

### Straddle Definitions

The `straddles_config.json` file defines the synthetic straddles to be processed. Each straddle definition includes:

```json
{
  "SyntheticSymbolNinjaTrader": "NIFTY25000STRDL",
  "CESymbol": "NIFTY2460725000CE",
  "PESymbol": "NIFTY2460725000PE",
  "TickSize": 0.05,
  "PointValue": 50.0,
  "Currency": "INR"
}
```

- **SyntheticSymbolNinjaTrader**: The unique symbol under which this synthetic straddle will be known in NinjaTrader.
- **CESymbol**: The full broker/exchange symbol for the Call option leg.
- **PESymbol**: The full broker/exchange symbol for the Put option leg.
- **TickSize**: The minimum price increment for the synthetic straddle in NinjaTrader.
- **PointValue**: The monetary value per point of the synthetic straddle in NinjaTrader.
- **Currency**: The currency in which the synthetic straddle is denominated.

### Instrument Mapping

The synthetic straddle instruments must be defined in the `mapped_instruments.json` file to be recognized by NinjaTrader:

```json
{
  "connector_symbol": "NIFTY25000STRDL",
  "exchange": "SYNTHETICS",
  "instrument_type": "FUTURE",
  "ninjatrader_symbol": "NIFTY25000STRDL",
  "description": "Nifty 25000 Straddle (Synthetic)",
  "tick_size": 0.05,
  "point_value": 50.0,
  "currency": "INR",
  "supported_resolutions": "Tick, Minute, Second",
  "session_template": "NSE Equity",
  "symbol": "NIFTY25000STRDL",
  "underlying": "NIFTY",
  "expiry": "2024-06-27",
  "strike": 25000,
  "option_type": "STRADDLE",
  "segment": "SYNTHETICS",
  "instrument_token": 9000001,
  "exchange_token": 9001,
  "zerodhaSymbol": "NIFTY25000STRDL",
  "lot_size": 50
}
```

## Usage

1. Start NinjaTrader 8 and connect to the QANinjaAdapter.
2. The SSP will automatically load the straddle definitions from the `straddles_config.json` file.
3. Subscribe to the synthetic straddle symbols in NinjaTrader (e.g., "NIFTY25000STRDL").
4. As ticks arrive for the constituent legs (CE and PE), the SSP will calculate the synthetic straddle price and forward it to NinjaTrader.
5. You can chart, trade, and analyze the synthetic straddle just like any other instrument in NinjaTrader.

## How It Works

1. When a tick arrives for either the CE or PE leg of a defined straddle, the QAAdapter identifies it as a straddle leg and forwards it to the SyntheticStraddleService.
2. The SyntheticStraddleService updates the price of the corresponding leg in the StraddleState.
3. If both legs have reported at least one price, the service calculates the synthetic straddle price (CE price + PE price).
4. The service then calls QAAdapter.PublishSyntheticTickData() to send the synthetic price to NinjaTrader.
5. NinjaTrader receives the synthetic tick and updates charts, strategies, and other components accordingly.

## Troubleshooting

- **Synthetic straddle not appearing in NinjaTrader**: Ensure the straddle is properly defined in both `straddles_config.json` and `mapped_instruments.json`.
- **No synthetic ticks being generated**: Check that you are receiving ticks for the constituent legs (CE and PE). The SSP will only generate synthetic ticks when it has received at least one tick for both legs.
- **Error loading straddle configurations**: Verify that the `straddles_config.json` file is in the correct location and has valid JSON syntax.

## Logging

The SSP logs its activities using the QALogger. You can monitor these logs to track the loading of straddle definitions, the processing of leg ticks, and the generation of synthetic ticks.

## Extending the SSP

The SSP is designed to be extensible. You can modify the SyntheticStraddleService to implement more complex synthetic instrument calculations, such as:

- Synthetic spreads (difference between two instruments)
- Synthetic butterflies (combination of multiple options)
- Custom weighting of constituent legs
- Time-based adjustments to synthetic prices

## License

This software is provided as-is with no warranty. Use at your own risk.
