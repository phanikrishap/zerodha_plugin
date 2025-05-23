using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Models.MarketData;

namespace QANinjaAdapter.Services.MarketData.Processing
{
    /// <summary>
    /// Represents a mapped instrument from mapped_instruments.json
    /// </summary>
    public class MappedInstrument
    {
        public long instrument_token { get; set; }
        public int exchange_token { get; set; }
        public string symbol { get; set; }
        public string zerodhaSymbol { get; set; }
        public decimal last_price { get; set; }
        public DateTime expiry { get; set; }
        public decimal strike { get; set; }
        public decimal tick_size { get; set; }
        public int lot_size { get; set; }
        public string option_type { get; set; }
        public string segment { get; set; }
        public string underlying { get; set; }
    }
    
    /// <summary>
    /// Represents parsed tick data from Zerodha WebSocket
    /// </summary>
    public class ParsedTickData
    {
        /// <summary>
        /// The instrument token from Zerodha
        /// </summary>
        public uint InstrumentToken { get; set; }
        
        /// <summary>
        /// The last traded price
        /// </summary>
        public float LastPrice { get; set; }
        
        /// <summary>
        /// The last traded quantity
        /// </summary>
        public int LastQuantity { get; set; }
        
        /// <summary>
        /// The total volume traded for the day
        /// </summary>
        public int Volume { get; set; }
        
        /// <summary>
        /// The average traded price
        /// </summary>
        public float AverageTradePrice { get; set; }
        
        /// <summary>
        /// The total buy quantity
        /// </summary>
        public int BuyQuantity { get; set; }
        
        /// <summary>
        /// The total sell quantity
        /// </summary>
        public int SellQuantity { get; set; }
        
        /// <summary>
        /// The open price of the day
        /// </summary>
        public float Open { get; set; }
        
        /// <summary>
        /// The high price of the day
        /// </summary>
        public float High { get; set; }
        
        /// <summary>
        /// The low price of the day
        /// </summary>
        public float Low { get; set; }
        
        /// <summary>
        /// The close price of the day
        /// </summary>
        public float Close { get; set; }
        
        /// <summary>
        /// The mode of the tick data (ltp, quote, index)
        /// </summary>
        public string Mode { get; set; }
        
        /// <summary>
        /// Flag to indicate if this is an index packet
        /// </summary>
        public bool IsIndex { get; set; }
        
        /// <summary>
        /// The timestamp of the tick data
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// The delta volume (current volume - previous volume)
        /// </summary>
        public int DeltaVolume { get; set; }
    }
    
    /// <summary>
    /// Service for processing market data from various sources
    /// </summary>
    public class DataProcessingService
    {
        // Thread-safe dictionary to store last tick data for each instrument token
        private readonly ConcurrentDictionary<uint, MarketDataEventArgs> _lastTickData = 
            new ConcurrentDictionary<uint, MarketDataEventArgs>();
            
        // Dictionaries to track last volume for each symbol to calculate volume delta
        private readonly ConcurrentDictionary<string, int> _lastVolumeMapInjector = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _lastVolumeMapLogger = new ConcurrentDictionary<string, int>();
        
        // Dictionary to store mapped instruments by token
        private readonly ConcurrentDictionary<uint, MappedInstrument> _mappedInstruments = new ConcurrentDictionary<uint, MappedInstrument>();
        
        /// <summary>
        /// Initializes a new instance of the DataProcessingService class
        /// </summary>
        public DataProcessingService()
        {
            // Load mapped instruments from JSON file
            LoadMappedInstruments();
        }
        
        /// <summary>
        /// Loads the mapped instruments from the JSON file
        /// </summary>
        private void LoadMappedInstruments()
        {
            try
            {
                string mappedInstrumentsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "NinjaTrader 8", "QAAdapter", "mapped_instruments.json");
                
                if (File.Exists(mappedInstrumentsPath))
                {
                    string json = File.ReadAllText(mappedInstrumentsPath);
                    var instruments = JsonConvert.DeserializeObject<List<MappedInstrument>>(json);
                    
                    if (instruments != null)
                    {
                        foreach (var instrument in instruments)
                        {
                            _mappedInstruments[(uint)instrument.instrument_token] = instrument;
                        }
                        
                        AppLogger.Log($"Loaded {_mappedInstruments.Count} mapped instruments", QANinjaAdapter.Logging.LogLevel.Information);
                    }
                }
                else
                {
                    AppLogger.Log($"Mapped instruments file not found: {mappedInstrumentsPath}", QANinjaAdapter.Logging.LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error loading mapped instruments: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Gets the symbol name for an instrument token
        /// </summary>
        /// <param name="instrumentToken">The instrument token</param>
        /// <returns>The symbol name, or a fallback if not found</returns>
        private string GetSymbolForToken(uint instrumentToken)
        {
            if (_mappedInstruments.TryGetValue(instrumentToken, out var instrument))
            {
                return instrument.symbol;
            }
            
            return $"TOKEN_{instrumentToken}";
        }
        
        /// <summary>
        /// Processes a WebSocket message and notifies subscribers
        /// </summary>
        /// <param name="data">The binary data from the WebSocket</param>
        /// <param name="subscriptions">The dictionary of active subscriptions</param>
        public void ProcessMessage(byte[] data, ConcurrentDictionary<string, L1Subscription> subscriptions)
        {
            try
            {
                // Parse the binary message from Zerodha
                var parsedTickData = ParseBinaryMessage(data);
                
                if (parsedTickData != null && parsedTickData.Count > 0)
                {
                    AppLogger.Log($"Successfully parsed {parsedTickData.Count} tick data packets", QANinjaAdapter.Logging.LogLevel.Information);
                    
                    // Process each tick in the message
                    foreach (var tickData in parsedTickData)
                    {
                        uint instrumentToken = tickData.InstrumentToken;
                        AppLogger.Log($"Processing tick for instrument token: {instrumentToken}, LastPrice: {tickData.LastPrice}", QANinjaAdapter.Logging.LogLevel.Information);
                        
                        // Convert to MarketDataEventArgs format
                        var eventArgs = ConvertToMarketDataEventArgs(tickData);
                        
                        // Store the latest tick data for this instrument
                        _lastTickData[instrumentToken] = eventArgs;
                        
                        bool foundMatch = false;
                        // Find all subscriptions that match this instrument token and notify them
                        foreach (var subscription in subscriptions.Values)
                        {
                            // Log detailed information about the subscription being checked
                            AppLogger.Log($"Checking subscription: NativeSymbol={subscription.NativeSymbol ?? "<null>"}, InstrumentToken={subscription.InstrumentToken}, Callback={(subscription.Callback != null ? "Present" : "<null>")}", QANinjaAdapter.Logging.LogLevel.Debug);
                            
                            if (subscription.InstrumentToken == (int)instrumentToken)
                            {
                                foundMatch = true;
                                AppLogger.Log($"Found matching subscription for token {instrumentToken}: {subscription.NativeSymbol ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                                
                                // Log more details about the subscription and parsed data
                                AppLogger.Log($"DETAILED SUBSCRIPTION: NativeSymbol='{subscription.NativeSymbol ?? "<null>"}', InstrumentToken={subscription.InstrumentToken}, HasCallback={subscription.Callback != null}", QANinjaAdapter.Logging.LogLevel.Information);
                                AppLogger.Log($"DETAILED TICK DATA: Token={tickData.InstrumentToken}, LastPrice={tickData.LastPrice}, Volume={tickData.Volume}, Mode={tickData.Mode ?? "<null>"}, Timestamp={tickData.Timestamp}", QANinjaAdapter.Logging.LogLevel.Information);
                                
                                try
                                {
                                    // Log before callback invocation
                                    AppLogger.Log($"About to invoke callback for {subscription.NativeSymbol ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Debug);
                                    
                                    // Call the callback for this subscription with the market data
                                    if (subscription.Callback != null)
                                    {
                                        subscription.Callback.Invoke(eventArgs);
                                        AppLogger.Log($"Successfully invoked callback for {subscription.NativeSymbol ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Debug);
                                    }
                                    else
                                    {
                                        AppLogger.Log($"Skipping callback invocation for {subscription.NativeSymbol ?? "<null>"} - callback is null", QANinjaAdapter.Logging.LogLevel.Warning);
                                    }
                                    
                                    // Create a valid symbol key - use NativeSymbol if available, otherwise look up the symbol from mapped instruments
                                    string symbolKey = !string.IsNullOrEmpty(subscription.NativeSymbol) 
                                        ? subscription.NativeSymbol 
                                        : GetSymbolForToken(tickData.InstrumentToken);
                                        
                                    // Get the actual symbol name from mapped instruments
                                    string actualSymbol = GetSymbolForToken(tickData.InstrumentToken);
                
                                    // Log before data injection
                                    AppLogger.Log($"About to inject data for {actualSymbol} (original key: {symbolKey})", QANinjaAdapter.Logging.LogLevel.Debug);
                
                                    // Inject the data into NinjaTrader using the actual symbol name
                                    InjectDataIntoNinjaTrader(actualSymbol, tickData);
                
                                    // Log before CSV logging
                                    AppLogger.Log($"About to log tick to CSV for {actualSymbol} (original key: {symbolKey})", QANinjaAdapter.Logging.LogLevel.Debug);
                
                                    // Log the tick data to CSV using the actual symbol name
                                    LogTickToCSV(actualSymbol, tickData);
                                }
                                catch (Exception ex)
                                {
                                    AppLogger.Log($"Error processing data for subscription {subscription.NativeSymbol ?? "<null>"}: {ex.Message}\nStack trace: {ex.StackTrace}", QANinjaAdapter.Logging.LogLevel.Error);
                                }
                            }
                        }      
                        if (!foundMatch)
                        {
                            AppLogger.Log($"No matching subscription found for instrument token: {instrumentToken}", QANinjaAdapter.Logging.LogLevel.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error processing WebSocket message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Converts ParsedTickData to MarketDataEventArgs
        /// </summary>
        /// <param name="data">The parsed tick data</param>
        /// <returns>MarketDataEventArgs for callbacks</returns>
        private MarketDataEventArgs ConvertToMarketDataEventArgs(ParsedTickData data)
        {
            // Convert parsed tick data to the format expected by callbacks
            var args = new MarketDataEventArgs
            {
                InstrumentToken = (int)data.InstrumentToken,
                LastPrice = data.LastPrice,
                LastQuantity = data.LastQuantity,
                AveragePrice = data.AverageTradePrice,
                Volume = data.Volume,
                BuyQuantity = data.BuyQuantity,
                SellQuantity = data.SellQuantity,
                OpenPrice = data.Open,
                HighPrice = data.High,
                LowPrice = data.Low,
                ClosePrice = data.Close,
                IsIndex = data.IsIndex,
                Timestamp = data.Timestamp != DateTime.MinValue ? data.Timestamp : DateTime.Now
            };
            
            return args;
        }
        
        /// <summary>
        /// Gets the last tick data for a specific instrument
        /// </summary>
        /// <param name="instrumentToken">The instrument token</param>
        /// <returns>The last tick data for the instrument, or null if not found</returns>
        public MarketDataEventArgs GetLastTickData(int instrumentToken)
        {
            if (_lastTickData.TryGetValue((uint)instrumentToken, out var data))
                return data;
            return null;
        }
        
        /// <summary>
        /// Parse a binary message from Zerodha WebSocket
        /// </summary>
        /// <param name="data">Raw binary data from WebSocket</param>
        /// <returns>List of parsed tick data objects</returns>
        private List<ParsedTickData> ParseBinaryMessage(byte[] data)
        {
            var result = new List<ParsedTickData>();
            
            try
            {
                // Check for a valid message (at least 2 bytes)
                if (data == null || data.Length < 2)
                    return result;

                // Parse the number of packets in this message (first 2 bytes)
                int offset = 0;
                int packetCount = BitConverter.ToInt16(new byte[] { data[1], data[0] }, 0); // Convert from big-endian
                offset += 2;
                
                AppLogger.Log($"Message contains {packetCount} packets", QANinjaAdapter.Logging.LogLevel.Information);
                
                // Log the raw binary data for debugging
                string hexData = BitConverter.ToString(data, 0, Math.Min(data.Length, 64)).Replace("-", "");
                AppLogger.Log($"Parsing binary message: length={data.Length}, packetCount={packetCount}, data={hexData}...", QANinjaAdapter.Logging.LogLevel.Information);
                
                // Process each packet in the message
                for (int i = 0; i < packetCount && offset < data.Length; i++)
                {
                    // According to Zerodha's documentation, the next 2 bytes represent the length of the packet
                    int packetLength = BitConverter.ToInt16(new byte[] { data[offset + 1], data[offset] }, 0); // Convert from big-endian
                    offset += 2;
                    
                    // The first byte of the packet is the packet type
                    byte packetType = data[offset];
                    
                    AppLogger.Log($"Packet {i+1}/{packetCount}: Type={packetType}, Length={packetLength}, Offset={offset}", QANinjaAdapter.Logging.LogLevel.Debug);
                    
                    // Process the packet based on its type
                    switch (packetType)
                    {
                        case 0: // LTP mode packet
                            if (packetLength >= 8) // LTP mode has at least 8 bytes
                            {
                                // Extract instrument token (bytes 0-3)
                                uint instrumentToken = ParseInstrumentToken(data, offset);
                                
                                // Extract LTP (bytes 4-7)
                                int ltpInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 7], data[offset + 6], data[offset + 5], data[offset + 4] }, 0);
                                float ltp = ltpInt / 100.0f; // Convert from paise to rupees
                                
                                AppLogger.Log($"LTP packet: Token={instrumentToken}, LTP={ltp}", QANinjaAdapter.Logging.LogLevel.Debug);
                                
                                // Extract volume (bytes 16-19) if available
                                int volume = 0;
                                if (packetLength >= 20)
                                {
                                    volume = BitConverter.ToInt32(new byte[] { 
                                        data[offset + 19], data[offset + 18], data[offset + 17], data[offset + 16] }, 0);
                                }
                                
                                result.Add(new ParsedTickData
                                {
                                    InstrumentToken = instrumentToken,
                                    LastPrice = ltp,
                                    Mode = "ltp",
                                    Volume = volume  // Set the volume from the extracted data
                                });
                            }
                            break;
                            
                        case 1: // Quote/full mode
                            if (packetLength >= 44) // Quote mode has at least 44 bytes
                            {
                                // Extract instrument token (bytes 0-3)
                                uint instrumentToken = ParseInstrumentToken(data, offset);
                                
                                // Extract LTP (bytes 4-7)
                                int ltpInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 7], data[offset + 6], data[offset + 5], data[offset + 4] }, 0);
                                float ltp = ltpInt / 100.0f; // Convert from paise to rupees
                                
                                // Extract volume (bytes 16-19) if available
                                int quoteVolume = 0;
                                if (packetLength >= 20)
                                {
                                    quoteVolume = BitConverter.ToInt32(new byte[] { 
                                        data[offset + 19], data[offset + 18], data[offset + 17], data[offset + 16] }, 0);
                                }
                                
                                var tick = new ParsedTickData
                                {
                                    InstrumentToken = instrumentToken,
                                    LastPrice = ltp,
                                    Mode = "quote",
                                    Volume = quoteVolume  // Set the volume from the extracted data
                                };
                                
                                // Last traded quantity (bytes 8-11)
                                tick.LastQuantity = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 11], data[offset + 10], data[offset + 9], data[offset + 8] }, 0);
                                    
                                // Average traded price (bytes 12-15)
                                int avgPriceInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 15], data[offset + 14], data[offset + 13], data[offset + 12] }, 0);
                                tick.AverageTradePrice = avgPriceInt / 100.0f; // Convert from paise to rupees
                                    
                                // Volume already extracted above
                                // tick.Volume = volume;
                                    
                                // Total buy quantity (bytes 20-23)
                                tick.BuyQuantity = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 23], data[offset + 22], data[offset + 21], data[offset + 20] }, 0);
                                    
                                // Total sell quantity (bytes 24-27)
                                tick.SellQuantity = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 27], data[offset + 26], data[offset + 25], data[offset + 24] }, 0);
                                
                                // Open price (bytes 28-31)
                                int openInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 31], data[offset + 30], data[offset + 29], data[offset + 28] }, 0);
                                tick.Open = openInt / 100.0f; // Convert from paise to rupees
                                    
                                // High price (bytes 32-35)
                                int highInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 35], data[offset + 34], data[offset + 33], data[offset + 32] }, 0);
                                tick.High = highInt / 100.0f; // Convert from paise to rupees
                                    
                                // Low price (bytes 36-39)
                                int lowInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 39], data[offset + 38], data[offset + 37], data[offset + 36] }, 0);
                                tick.Low = lowInt / 100.0f; // Convert from paise to rupees
                                    
                                // Close price (bytes 40-43)
                                int closeInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 43], data[offset + 42], data[offset + 41], data[offset + 40] }, 0);
                                tick.Close = closeInt / 100.0f; // Convert from paise to rupees
                                
                                // If we have exchange timestamp (bytes 60-63)
                                if (packetLength >= 64)
                                {
                                    int exchangeTimestamp = BitConverter.ToInt32(new byte[] { 
                                        data[offset + 63], data[offset + 62], data[offset + 61], data[offset + 60] }, 0);
                                        
                                    if (exchangeTimestamp > 0)
                                    {
                                        // Convert Unix timestamp to DateTime
                                        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                        tick.Timestamp = epoch.AddSeconds(exchangeTimestamp).ToLocalTime();
                                    }
                                }
                                
                                AppLogger.Log($"Quote packet: Token={instrumentToken}, LTP={tick.LastPrice}, Vol={tick.Volume}", QANinjaAdapter.Logging.LogLevel.Debug);
                                result.Add(tick);
                            }
                            break;
                            
                        case 6: // Index packet (for indices like NIFTY 50 and SENSEX)
                            if (packetLength >= 28) // Index packet has at least 28 bytes
                            {
                                // Extract instrument token (bytes 0-3)
                                uint instrumentToken = ParseInstrumentToken(data, offset);
                                
                                // Extract LTP (bytes 4-7)
                                int ltpInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 7], data[offset + 6], data[offset + 5], data[offset + 4] }, 0);
                                float ltp = ltpInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract High (bytes 8-11)
                                int highInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 11], data[offset + 10], data[offset + 9], data[offset + 8] }, 0);
                                float high = highInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract Low (bytes 12-15)
                                int lowInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 15], data[offset + 14], data[offset + 13], data[offset + 12] }, 0);
                                float low = lowInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract Open (bytes 16-19)
                                int openInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 19], data[offset + 18], data[offset + 17], data[offset + 16] }, 0);
                                float open = openInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract Close (bytes 20-23)
                                int closeInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 23], data[offset + 22], data[offset + 21], data[offset + 20] }, 0);
                                float close = closeInt / 100.0f; // Convert from paise to rupees
                                
                                // Extract volume (bytes 24-27) if available for index packets
                                int volume = 0;
                                if (packetLength >= 28)
                                {
                                    volume = BitConverter.ToInt32(new byte[] { 
                                        data[offset + 27], data[offset + 26], data[offset + 25], data[offset + 24] }, 0);
                                }
                                
                                // Create tick data object
                                var tick = new ParsedTickData
                                {
                                    InstrumentToken = instrumentToken,
                                    LastPrice = ltp,
                                    High = high,
                                    Low = low,
                                    Open = open,
                                    Close = close,
                                    Mode = "index",
                                    IsIndex = true,
                                    Volume = volume  // Set the volume from the extracted data
                                };
                                
                                // If we have exchange timestamp (bytes 28-31)
                                if (packetLength >= 32)
                                {
                                    int exchangeTimestamp = BitConverter.ToInt32(new byte[] { 
                                        data[offset + 31], data[offset + 30], data[offset + 29], data[offset + 28] }, 0);
                                        
                                    if (exchangeTimestamp > 0)
                                    {
                                        // Convert Unix timestamp to DateTime
                                        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                        tick.Timestamp = epoch.AddSeconds(exchangeTimestamp).ToLocalTime();
                                    }
                                }
                                
                                AppLogger.Log($"Index packet: Token={instrumentToken}, LTP={tick.LastPrice}, High={tick.High}, Low={tick.Low}", QANinjaAdapter.Logging.LogLevel.Debug);
                                result.Add(tick);
                            }
                            break;
                            
                        case 123: // Heartbeat/ping packet
                            AppLogger.Log($"Received heartbeat packet (type 123)", QANinjaAdapter.Logging.LogLevel.Debug);
                            // Heartbeat packets don't contain market data, they're just to keep the connection alive
                            break;
                            
                        // Add handling for other packet types as needed
                            
                        default:
                            AppLogger.Log($"Unknown packet type: {packetType}", QANinjaAdapter.Logging.LogLevel.Warning);
                            break;
                    }
                    
                    // Move to the next packet
                    // We've already advanced by 2 bytes for the packet length, now advance by the packet length
                    offset += packetLength;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error parsing binary message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
            
            return result;
        }
        
        /// <summary>
        /// Helper method to parse an instrument token from big-endian bytes
        /// </summary>
        private uint ParseInstrumentToken(byte[] data, int offset)
        {
            // According to Zerodha's documentation, the instrument token is the first 4 bytes of the packet
            // The packet starts at offset, so the token is at offset to offset+3
            byte b0 = data[offset];
            byte b1 = data[offset + 1];
            byte b2 = data[offset + 2];
            byte b3 = data[offset + 3];
            
            // Convert from big-endian format (most significant byte first)
            uint tokenUint = ((uint)b0 << 24) | ((uint)b1 << 16) | ((uint)b2 << 8) | b3;
            
            // Log both the raw bytes and the parsed token
            AppLogger.Log($"Token bytes: {b0:X2} {b1:X2} {b2:X2} {b3:X2} = {tokenUint}", QANinjaAdapter.Logging.LogLevel.Information);
            
            // Also log the reversed token for debugging
            uint tokenReversed = ((uint)b3 << 24) | ((uint)b2 << 16) | ((uint)b1 << 8) | b0;
            AppLogger.Log($"Reversed token: {b3:X2} {b2:X2} {b1:X2} {b0:X2} = {tokenReversed}", QANinjaAdapter.Logging.LogLevel.Information);
            
            return tokenUint;
        }
        
        /// <summary>
        /// Finds a NinjaTrader instrument by symbol name
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        /// <returns>The NinjaTrader instrument, or null if not found</returns>
        private NinjaTrader.Cbi.Instrument FindNinjaTraderInstrument(string symbolName)
        {
            try
            {
                // Look for the instrument in NinjaTrader's instrument collection
                foreach (var masterInstrument in NinjaTrader.Cbi.MasterInstrument.All)
                {
                    if (masterInstrument.Name.Equals(symbolName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Create an Instrument instance from the MasterInstrument
                        var instrument = new NinjaTrader.Cbi.Instrument { MasterInstrument = masterInstrument };
                        AppLogger.Log($"Found NinjaTrader instrument for symbol: {symbolName}", QANinjaAdapter.Logging.LogLevel.Debug);
                        return instrument;
                    }
                }
                
                AppLogger.Log($"Could not find NinjaTrader instrument for symbol: {symbolName}", QANinjaAdapter.Logging.LogLevel.Warning);
                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error finding NinjaTrader instrument for {symbolName}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                return null;
            }
        }
        
        /// <summary>
        /// Ensures that the QAAdapter has a subscription for the given instrument
        /// </summary>
        /// <param name="qaAdapter">The QAAdapter instance</param>
        /// <param name="instrument">The NinjaTrader instrument</param>
        /// <param name="symbolName">The symbol name</param>
        private void EnsureSubscription(QAAdapter qaAdapter, NinjaTrader.Cbi.Instrument instrument, string symbolName)
        {
            try
            {
                AppLogger.Log($"Ensuring subscription for symbol: {symbolName}", QANinjaAdapter.Logging.LogLevel.Debug);
                
                // Use the SubscribeMarketData method to create a subscription
                // This will handle all the internal subscription logic in QAAdapter
                qaAdapter.SubscribeMarketData(
                    instrument,
                    (marketDataType, price, volume, time, position) =>
                    {
                        // This callback will be called by QAAdapter.ProcessParsedTick
                        // We don't need to do anything here as we're just ensuring the subscription exists
                        AppLogger.Log($"Callback received for {symbolName}: {marketDataType}, {price}, {volume}", 
                            QANinjaAdapter.Logging.LogLevel.Debug);
                    }
                );
                
                AppLogger.Log($"Subscription created/updated for symbol: {symbolName}", QANinjaAdapter.Logging.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error ensuring subscription for {symbolName}: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Injects the parsed tick data into NinjaTrader
        /// </summary>
        /// <param name="symbol">The symbol to inject data for</param>
        /// <param name="parsedData">The parsed tick data</param>
        private void InjectDataIntoNinjaTrader(string symbol, ParsedTickData parsedData)
        {
            try
            {
                // Log all input parameters for debugging
                AppLogger.Log($"InjectDataIntoNinjaTrader called with: Symbol={symbol ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                if (parsedData != null)
                {
                    AppLogger.Log($"ParsedData: Token={parsedData.InstrumentToken}, LastPrice={parsedData.LastPrice}, Volume={parsedData.Volume}, Mode={parsedData.Mode ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                }
                else
                {
                    AppLogger.Log("ParsedData is NULL", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Validate input
                if (string.IsNullOrEmpty(symbol))
                {
                    AppLogger.Log($"Cannot inject data: Symbol is null or empty", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Create a ZerodhaTickData object from the parsed data
                var zerodhaTickData = new ZerodhaTickData
                {
                    InstrumentToken = (int)parsedData.InstrumentToken,
                    LastTradePrice = parsedData.LastPrice,
                    LastTradeQty = parsedData.DeltaVolume, // Use delta volume as the last trade quantity
                    AverageTradePrice = parsedData.AverageTradePrice,
                    TotalQtyTraded = parsedData.Volume,    // Keep total volume for reference
                    BuyQty = parsedData.BuyQuantity,
                    SellQty = parsedData.SellQuantity,
                    Open = parsedData.Open,
                    High = parsedData.High,
                    Low = parsedData.Low,
                    Close = parsedData.Close,
                    LastTradeTime = parsedData.Timestamp != DateTime.MinValue ? parsedData.Timestamp : DateTime.Now,
                    ExchangeTimestamp = parsedData.Timestamp != DateTime.MinValue ? parsedData.Timestamp : DateTime.Now
                };
                
                // Calculate volume delta if we have previous volume data
                int volumeDelta = 0;
                if (_lastVolumeMapInjector.TryGetValue(symbol, out int lastVolume))
                {
                    volumeDelta = parsedData.Volume - lastVolume;
                    if (volumeDelta < 0) volumeDelta = 0; // Ensure delta is not negative
                }
                else
                {
                    // If this is the first tick, the delta is the same as the total volume
                    volumeDelta = parsedData.Volume;
                }
                
                // Update the last volume for this symbol
                _lastVolumeMapInjector[symbol] = parsedData.Volume;
                
                // Store the delta volume in the parsed data for use in ZerodhaTickData
                parsedData.DeltaVolume = volumeDelta;
                
                // Log the injection with symbol information
                AppLogger.Log($"Injecting data for {symbol}: LTP={parsedData.LastPrice}, Vol={parsedData.Volume}, Delta={volumeDelta}", QANinjaAdapter.Logging.LogLevel.Debug);
                
                // We need to use the NinjaTrader logging directly since we don't have direct access to the QAAdapter instance
                // This will still record the tick data in NinjaTrader
                NinjaTrader.NinjaScript.NinjaScript.Log(
                    $"[TICK-DATA] {symbol}: LTP={zerodhaTickData.LastTradePrice}, DeltaVol={zerodhaTickData.LastTradeQty}, TotalVol={zerodhaTickData.TotalQtyTraded}, Time={zerodhaTickData.LastTradeTime:HH:mm:ss.fff}",
                    NinjaTrader.Cbi.LogLevel.Information);
                
                // Get the QAAdapter instance from the Connector to update NinjaTrader charts
                try
                {
                    // Get the QAAdapter instance from the Connector
                    var qaAdapter = QANinjaAdapter.Connector.Instance.GetAdapter() as QAAdapter;
                    if (qaAdapter != null)
                    {
                        // Check if we have a NinjaTrader Instrument for this symbol
                        var ntInstrument = FindNinjaTraderInstrument(symbol);
                        if (ntInstrument != null)
                        {
                            // Ensure we have a subscription for this instrument in QAAdapter
                            EnsureSubscription(qaAdapter, ntInstrument, symbol);
                            
                            // Now call ProcessParsedTick to update NinjaTrader charts
                            qaAdapter.ProcessParsedTick(symbol, zerodhaTickData);
                            AppLogger.Log($"Successfully called QAAdapter.ProcessParsedTick for {symbol}", QANinjaAdapter.Logging.LogLevel.Debug);
                        }
                        else
                        {
                            AppLogger.Log($"Could not find NinjaTrader instrument for symbol: {symbol}", QANinjaAdapter.Logging.LogLevel.Error);
                        }
                    }
                    else
                    {
                        AppLogger.Log("QAAdapter instance is null, cannot update NinjaTrader charts", QANinjaAdapter.Logging.LogLevel.Error);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error calling QAAdapter.ProcessParsedTick: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                }
                // var qaAdapter = Connector.Instance.GetAdapter() as QAAdapter;
                // qaAdapter?.ProcessParsedTick(symbol, zerodhaTickData);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error injecting data into NinjaTrader: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Logs tick data to CSV file
        /// </summary>
        /// <param name="symbol">The symbol to log data for</param>
        /// <param name="parsedData">The parsed tick data</param>
        private void LogTickToCSV(string symbol, ParsedTickData parsedData)
        {
            try
            {
                // Log all input parameters for debugging
                AppLogger.Log($"LogTickToCSV called with: Symbol={symbol ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                if (parsedData != null)
                {
                    AppLogger.Log($"ParsedData for CSV: Token={parsedData.InstrumentToken}, LastPrice={parsedData.LastPrice}, Volume={parsedData.Volume}, Mode={parsedData.Mode ?? "<null>"}", QANinjaAdapter.Logging.LogLevel.Information);
                }
                else
                {
                    AppLogger.Log("ParsedData for CSV is NULL", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Validate input
                if (string.IsNullOrEmpty(symbol))
                {
                    AppLogger.Log($"Cannot log tick data: Symbol is null or empty", QANinjaAdapter.Logging.LogLevel.Error);
                    return;
                }
                
                // Use the already calculated delta volume from the parsed data
                int volumeDelta = parsedData.DeltaVolume;
                
                // Update the last volume for this symbol for future calculations
                _lastVolumeMapLogger[symbol] = parsedData.Volume;
                
                // Get the timestamp
                DateTime timestamp = parsedData.Timestamp != DateTime.MinValue ? parsedData.Timestamp : DateTime.Now;
                
                // Log to CSV with symbol information
                AppLogger.Log($"Logging tick to CSV for {symbol}: LTP={parsedData.LastPrice}, DeltaVol={volumeDelta}, TotalVol={parsedData.Volume}", QANinjaAdapter.Logging.LogLevel.Debug);
                
                // Get the actual symbol name from mapped instruments for CSV logging
                string actualSymbolForCsv = GetSymbolForToken(parsedData.InstrumentToken);
                
                // Use the TickVolumeLogger to log the tick data with the actual symbol name
                TickVolumeLogger.LogTickVolume(
                    actualSymbolForCsv, // Use the actual symbol name from mapped instruments
                    DateTime.Now,  // Received time
                    timestamp,     // Exchange time
                    DateTime.Now,  // Parsed time
                    parsedData.LastPrice,
                    volumeDelta,   // Use delta volume as the last trade quantity
                    parsedData.Volume,
                    volumeDelta
                );
                
                AppLogger.Log($"Successfully logged tick to CSV for {symbol}", QANinjaAdapter.Logging.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error logging tick to CSV: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
        }
    }
}
