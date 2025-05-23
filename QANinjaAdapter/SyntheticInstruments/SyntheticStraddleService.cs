using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace QANinjaAdapter.SyntheticInstruments
{
    /// <summary>
    /// The core service for managing synthetic straddle definitions, states, and processing incoming ticks.
    /// </summary>
    public class SyntheticStraddleService
    {
        private readonly QAAdapter _qaAdapterInstance; // Reference to the QAAdapter for publishing
        private readonly ConcurrentDictionary<string, StraddleState> _straddleStatesBySyntheticSymbol;
        private readonly ConcurrentDictionary<string, List<StraddleState>> _legSymbolToAffectedStatesMap;

        /// <summary>
        /// Creates a new instance of the SyntheticStraddleService.
        /// </summary>
        /// <param name="qaAdapterInstance">The QAAdapter instance to use for publishing synthetic ticks.</param>
        public SyntheticStraddleService(QAAdapter qaAdapterInstance)
        {
            _qaAdapterInstance = qaAdapterInstance ?? throw new ArgumentNullException(nameof(qaAdapterInstance));
            _straddleStatesBySyntheticSymbol = new ConcurrentDictionary<string, StraddleState>();
            _legSymbolToAffectedStatesMap = new ConcurrentDictionary<string, List<StraddleState>>();
        }

        /// <summary>
        /// Loads straddle definitions from a JSON configuration file.
        /// </summary>
        /// <param name="configFilePath">Path to the straddles_config.json file.</param>
        public void LoadStraddleConfigs(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                Logger.Error($"Straddle config file not found: {configFilePath}");
                return;
            }

            try
            {
                var configJson = File.ReadAllText(configFilePath);
                var definitions = JsonConvert.DeserializeObject<List<StraddleDefinition>>(configJson);

                if (definitions == null || !definitions.Any())
                {
                    Logger.Warn($"No straddle definitions found in {configFilePath}.");
                    return;
                }

                foreach (var def in definitions)
                {
                    var state = new StraddleState(def);
                    _straddleStatesBySyntheticSymbol.TryAdd(def.SyntheticSymbolNinjaTrader, state);

                    // Map Call leg symbol to its associated StraddleState(s)
                    _legSymbolToAffectedStatesMap.AddOrUpdate(
                        def.CESymbol,
                        new List<StraddleState> { state },
                        (key, existingList) => { existingList.Add(state); return existingList; }
                    );

                    // Map Put leg symbol to its associated StraddleState(s)
                    _legSymbolToAffectedStatesMap.AddOrUpdate(
                        def.PESymbol,
                        new List<StraddleState> { state },
                        (key, existingList) => { existingList.Add(state); return existingList; }
                    );
                    Logger.Info($"Loaded straddle: {def.SyntheticSymbolNinjaTrader} (CE: {def.CESymbol}, PE: {def.PESymbol})");
                }
            }
            catch (JsonException ex)
            {
                Logger.Error($"Error parsing straddle config file {configFilePath}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Logger.Error($"An unexpected error occurred loading straddle configs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if an instrument symbol is a leg of any defined straddle.
        /// </summary>
        /// <param name="instrumentSymbol">The symbol of the incoming tick.</param>
        /// <returns>True if the symbol is a leg, false otherwise.</returns>
        public bool IsLegInstrument(string instrumentSymbol)
        {
            return _legSymbolToAffectedStatesMap.ContainsKey(instrumentSymbol);
        }
        
        /// <summary>
        /// Gets all straddle states for checking synthetic instruments.
        /// </summary>
        /// <returns>A collection of all straddle states.</returns>
        public IEnumerable<StraddleState> GetStraddleStates()
        {
            return _straddleStatesBySyntheticSymbol.Values;
        }

        /// <summary>
        /// Processes an incoming tick for a straddle leg.
        /// </summary>
        /// <param name="tick">The incoming tick data.</param>
        public void ProcessLegTick(Tick tick)
        {
            if (tick == null) return;

            // Find all straddles affected by this tick
            if (!_legSymbolToAffectedStatesMap.TryGetValue(tick.InstrumentSymbol, out List<StraddleState> affectedStates))
            {
                // This case should ideally not happen if IsLegInstrument is called first,
                // but good for defensive programming.
                return;
            }

            foreach (var state in affectedStates)
            {
                bool priceUpdated = false;

                // Update the relevant leg's last price, timestamp, and volume
                bool isCELeg = false;
                if (state.Definition.CESymbol.Equals(tick.InstrumentSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    // Create a copy of the tick to store in the recent ticks list
                    var tickCopy = new Tick
                    {
                        InstrumentSymbol = tick.InstrumentSymbol,
                        Price = tick.Price,
                        Volume = tick.Volume,
                        Timestamp = tick.Timestamp,
                        Type = tick.Type
                    };
                    
                    // Add to recent ticks and limit to 5
                    state.RecentCETicks.Add(tickCopy);
                    if (state.RecentCETicks.Count > 5)
                    {
                        state.RecentCETicks.RemoveAt(0); // Remove oldest tick
                    }
                    
                    state.LastCEPrice = tick.Price;
                    state.LastCETimestamp = tick.Timestamp;
                    state.LastCEVolume = tick.Volume;
                    state.CEVolumeIncorporated = false; // Mark this volume as not yet incorporated
                    state.HasCEData = true;
                    priceUpdated = true;
                    isCELeg = true;
                }
                else if (state.Definition.PESymbol.Equals(tick.InstrumentSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    // Create a copy of the tick to store in the recent ticks list
                    var tickCopy = new Tick
                    {
                        InstrumentSymbol = tick.InstrumentSymbol,
                        Price = tick.Price,
                        Volume = tick.Volume,
                        Timestamp = tick.Timestamp,
                        Type = tick.Type
                    };
                    
                    // Add to recent ticks and limit to 5
                    state.RecentPETicks.Add(tickCopy);
                    if (state.RecentPETicks.Count > 5)
                    {
                        state.RecentPETicks.RemoveAt(0); // Remove oldest tick
                    }
                    
                    state.LastPEPrice = tick.Price;
                    state.LastPETimestamp = tick.Timestamp;
                    state.LastPEVolume = tick.Volume;
                    state.PEVolumeIncorporated = false; // Mark this volume as not yet incorporated
                    state.HasPEData = true;
                    priceUpdated = true;
                    isCELeg = false;
                }

                // Only calculate and publish a synthetic tick if:
                // 1. The price of the current leg tick actually resulted in an update to its stored price.
                // 2. Both legs have reported at least one price (preventing calculation with 0.0 initial values).
                if (priceUpdated && state.HasCEData && state.HasPEData)
                {
                    double syntheticCurrentPrice = state.GetSyntheticPrice();

                    // --- Volume Logic for Tick-by-Tick ---
                    // Use the volume of the current tick as the base
                    long syntheticVolume = tick.Volume;
                    string volumeSource = tick.InstrumentSymbol;
                    
                    // Log detailed information about recent ticks
                    NinjaTrader.NinjaScript.NinjaScript.Log(
                        $"[SYNTHETIC-DETAIL] Recent CE ticks: {state.RecentCETicks.Count}, " +
                        $"Recent PE ticks: {state.RecentPETicks.Count}, " +
                        $"Current tick: {tick.InstrumentSymbol}, Volume: {tick.Volume}",
                        NinjaTrader.Cbi.LogLevel.Information);
                    
                    // Check for ticks that are very close in time (within 50ms)
                    if (state.RecentCETicks.Count > 0 && state.RecentPETicks.Count > 0)
                    {
                        var latestCETick = state.RecentCETicks[state.RecentCETicks.Count - 1];
                        var latestPETick = state.RecentPETicks[state.RecentPETicks.Count - 1];
                        
                        TimeSpan timeDifference = latestCETick.Timestamp > latestPETick.Timestamp
                            ? latestCETick.Timestamp - latestPETick.Timestamp
                            : latestPETick.Timestamp - latestCETick.Timestamp;
                            
                        // If ticks are very close in time (within 50ms), consider them aligned
                        if (timeDifference.TotalMilliseconds < 50)
                        {
                            NinjaTrader.NinjaScript.NinjaScript.Log(
                                $"[SYNTHETIC-VOLUME] Ticks aligned! CE: {latestCETick.Timestamp:HH:mm:ss.fff} ({latestCETick.Volume}), " +
                                $"PE: {latestPETick.Timestamp:HH:mm:ss.fff} ({latestPETick.Volume}), " +
                                $"Difference: {timeDifference.TotalMilliseconds:F2}ms",
                                NinjaTrader.Cbi.LogLevel.Information);
                                
                            // For aligned ticks, use the sum of volumes
                            if (!state.CEVolumeIncorporated && !state.PEVolumeIncorporated)
                            {
                                syntheticVolume = latestCETick.Volume + latestPETick.Volume;
                                volumeSource = $"{latestCETick.InstrumentSymbol}+{latestPETick.InstrumentSymbol}";
                            }
                        }
                    }
                    
                    // Mark the current leg's volume as incorporated
                    if (isCELeg)
                    {
                        state.CEVolumeIncorporated = true;
                    }
                    else
                    {
                        state.PEVolumeIncorporated = true;
                    }
                    
                    // Log the volume we're using for the synthetic tick
                    NinjaTrader.NinjaScript.NinjaScript.Log(
                        $"[SYNTHETIC-VOLUME] Using volume {syntheticVolume} from {volumeSource} for {state.Definition.SyntheticSymbolNinjaTrader}",
                        NinjaTrader.Cbi.LogLevel.Information);
                    
                    // Update the last synthetic volume
                    state.LastSyntheticVolume = syntheticVolume;
                    state.CumulativeSyntheticVolume += syntheticVolume;

                    _qaAdapterInstance.PublishSyntheticTickData(
                        state.Definition.SyntheticSymbolNinjaTrader,
                        syntheticCurrentPrice,
                        tick.Timestamp, // Use the timestamp of the incoming tick as the synthetic tick's timestamp
                        syntheticVolume,
                        tick.Type // Pass the original tick type for NinjaTrader's context
                    );
                }
            }
        }
    }
}
