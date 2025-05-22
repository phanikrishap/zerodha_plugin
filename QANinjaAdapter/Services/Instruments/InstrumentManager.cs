using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using QABrokerAPI.Common.Enums;
using QANinjaAdapter.Classes.Binance.Symbols;
using QANinjaAdapter.Services.Zerodha;

namespace QANinjaAdapter.Services.Instruments
{
    /// <summary>
    /// Manages instrument creation, mapping, and management
    /// </summary>
    public class InstrumentManager
    {
        private static InstrumentManager _instance;
        private readonly Dictionary<string, long> _instrumentTokenCache = new Dictionary<string, long>();
        private readonly ZerodhaClient _zerodhaClient;
        
        // Database file path
        private const string DB_FILE_PATH = "NinjaTrader 8\\QAAdapter\\mapped_instruments.json";
        private Dictionary<string, long> _symbolToTokenMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<long, InstrumentData> _tokenToInstrumentDataMap = new Dictionary<long, InstrumentData>();
        private bool _isInitialized = false;

        /// <summary>
        /// Gets the singleton instance of the InstrumentManager
        /// </summary>
        public static InstrumentManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new InstrumentManager();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private InstrumentManager()
        {
            _zerodhaClient = ZerodhaClient.Instance;
            // Load the instrument tokens on initialization
            _ = EnsureInitialized();
        }
        
        private async Task EnsureInitialized()
        {
            if (_isInitialized) return;
            
            string jsonFilePath = string.Empty;
            try 
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                jsonFilePath = Path.Combine(documentsPath, DB_FILE_PATH);

                if (!File.Exists(jsonFilePath))
                {
                    Logger.Info($"InstrumentManager: Instrument mapping file not found at: {jsonFilePath}");
                    return;
                }

                string jsonContent = File.ReadAllText(jsonFilePath);
                var instruments = JsonConvert.DeserializeObject<List<InstrumentData>>(jsonContent);
                
                if (instruments != null)
                {
                    foreach (var instrument in instruments)
                    {
                        if (!string.IsNullOrEmpty(instrument.symbol) && instrument.instrument_token > 0)
                        {
                            _symbolToTokenMap[instrument.symbol] = instrument.instrument_token;
                            _tokenToInstrumentDataMap[instrument.instrument_token] = instrument;
                            
                            // Also map zerodhaSymbol if it's different
                            if (!string.IsNullOrEmpty(instrument.zerodhaSymbol) && 
                                !instrument.zerodhaSymbol.Equals(instrument.symbol, StringComparison.OrdinalIgnoreCase))
                            {
                                _symbolToTokenMap[instrument.zerodhaSymbol] = instrument.instrument_token;
                            }
                        }
                    }
                    
                    _isInitialized = true;
                    Logger.Info($"InstrumentManager: Loaded {_symbolToTokenMap.Count} instrument mappings and {_tokenToInstrumentDataMap.Count} token-to-data mappings from {jsonFilePath}");
                }
            }
            catch (Exception ex)
            {
                string pathForError = string.IsNullOrEmpty(jsonFilePath) ? "[unknown path]" : jsonFilePath;
                Logger.Error($"InstrumentManager: Error loading instrument mappings from {pathForError}. Error: {ex.Message}", ex);
                throw;
            }
        }
        
        private class InstrumentData
        {
            public string symbol { get; set; }
            public string underlying { get; set; }
            public string expiry { get; set; }
            public double strike { get; set; }
            public string option_type { get; set; }
            public string segment { get; set; }
            public long instrument_token { get; set; }
            public long exchange_token { get; set; }
            public string zerodhaSymbol { get; set; }
            public double tick_size { get; set; }
            public int lot_size { get; set; }
        }

        /// <summary>
        /// Gets the instrument token for a symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the token for</param>
        /// <returns>The instrument token</returns>
        public async Task<long> GetInstrumentToken(string symbol)
        {
            try
            {
                await EnsureInitialized();
                
                // First try exact match
                if (_symbolToTokenMap.TryGetValue(symbol, out long token))
                    return token;
                    
                // If not found, try case-insensitive search
                var match = _symbolToTokenMap.FirstOrDefault(kvp => 
                    string.Equals(kvp.Key, symbol, StringComparison.OrdinalIgnoreCase));
                    
                if (!match.Equals(default(KeyValuePair<string, long>)))
                    return match.Value;

                throw new KeyNotFoundException($"Instrument token not found for symbol: {symbol}");
            }
            catch (Exception ex)
            {
                Logger.Error($"InstrumentManager: Error getting instrument token for symbol '{symbol}'. Error: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Loads instrument tokens from the Zerodha API
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task LoadInstrumentTokens()
        {
            try
            {
                // Only load if not already loaded
                if (_instrumentTokenCache.Count > 0)
                    return;

                Logger.Info("InstrumentManager: Attempting to load instrument tokens from Zerodha API...");

                using (HttpClient client = _zerodhaClient.CreateAuthorizedClient())
                {
                    // Get all instruments
                    string url = "https://api.kite.trade/instruments";

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string csvContent = await response.Content.ReadAsStringAsync();

                        // Parse CSV content
                        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        if (lines.Length <= 1)
                        {
                            Logger.Warn("InstrumentManager: No instruments found in CSV from Zerodha API.");
                            return;
                        }

                        // Get column indices
                        string[] headers = lines[0].Split(',');
                        int tradingSymbolIndex = Array.IndexOf(headers, "tradingsymbol");
                        int instrumentTokenIndex = Array.IndexOf(headers, "instrument_token");
                        int exchangeIndex = Array.IndexOf(headers, "exchange");

                        if (tradingSymbolIndex < 0 || instrumentTokenIndex < 0 || exchangeIndex < 0)
                        {
                            Logger.Error("InstrumentManager: Required columns (tradingsymbol, instrument_token, exchange) not found in CSV from Zerodha API.");
                            return;
                        }

                        // Parse data rows
                        for (int i = 1; i < lines.Length; i++)
                        {
                            string[] fields = lines[i].Split(',');
                            if (fields.Length <= Math.Max(Math.Max(tradingSymbolIndex, instrumentTokenIndex), exchangeIndex))
                                continue;

                            string tradingSymbol = fields[tradingSymbolIndex];
                            string exchange = fields[exchangeIndex];
                            long instrumentToken;

                            if (long.TryParse(fields[instrumentTokenIndex], out instrumentToken))
                            {
                                // Use both exchange and symbol to create a unique key
                                string key = $"{exchange}:{tradingSymbol}";

                                if (!_instrumentTokenCache.ContainsKey(key))
                                {
                                    _instrumentTokenCache[key] = instrumentToken;
                                }

                                // Also add just the symbol for convenience
                                if (!_instrumentTokenCache.ContainsKey(tradingSymbol))
                                {
                                    _instrumentTokenCache[tradingSymbol] = instrumentToken;
                                }
                            }
                        }

                        Logger.Info($"InstrumentManager: Loaded {_instrumentTokenCache.Count} instrument tokens");
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Logger.Error($"InstrumentManager: Error loading instruments from Zerodha API. Status code: {response.StatusCode}, Error: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"InstrumentManager: Error loading instrument tokens from Zerodha API. Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets exchange information for all available instruments
        /// </summary>
        /// <returns>A collection of symbol objects</returns>
        public async Task<ObservableCollection<SymbolObject>> GetExchangeInformation()
        {
            return await Task.Run(() =>
            {
                ObservableCollection<SymbolObject> exchangeInformation = new ObservableCollection<SymbolObject>();

                try
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string jsonFilePath = Path.Combine(documentsPath, Path.ChangeExtension(DB_FILE_PATH, ".json"));

                    Logger.Info($"Reading symbols from JSON file: {jsonFilePath}");

                    if (!File.Exists(jsonFilePath))
                    {
                        Logger.Error($"Error: JSON file does not exist at {jsonFilePath}");
                        MessageBox.Show($"Symbol JSON file not found at: {jsonFilePath}",
                                "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return exchangeInformation;
                    }

                    // Read the JSON file
                    string jsonContent = File.ReadAllText(jsonFilePath);

                    // Deserialize JSON to list of mapped instruments
                    var mappedInstruments = JsonConvert.DeserializeObject<List<MappedInstrument>>(jsonContent);

                    Logger.Info($"Successfully read JSON file");

                    int count = 0;

                    foreach (var instrument in mappedInstruments)
                    {
                        try
                        {
                            // Skip empty symbols
                            if (string.IsNullOrEmpty(instrument.symbol))
                                continue;

                            // Create filters for tick size and lot size
                            List<Filter> filters = new List<Filter>();

                            // Add PRICE_FILTER if tick_size is available
                            if (instrument.tick_size > 0)
                            {
                                filters.Add(new Filter
                                {
                                    FilterType = "PRICE_FILTER",
                                    TickSize = instrument.tick_size
                                });
                            }

                            // Add LOT_SIZE if lot_size is available
                            if (instrument.lot_size > 0)
                            {
                                filters.Add(new Filter
                                {
                                    FilterType = "LOT_SIZE",
                                    StepSize = Convert.ToDouble(instrument.lot_size)
                                });
                            }

                            // Extract segment from the segment field (e.g., "NFO-FUT" -> "NFO")
                            string segment = instrument.segment.Split('-')[0];

                            // Create the SymbolObject
                            SymbolObject symbolObject = new SymbolObject
                            {
                                Symbol = instrument.symbol,
                                BaseAsset = instrument.zerodhaSymbol ?? instrument.symbol,
                                QuoteAsset = segment, // Using segment as exchange/quote asset
                                Status = "TRADING",
                                Filters = filters.ToArray()
                            };

                            // Set market type based on segment
                            switch (segment.ToUpper())
                            {
                                case "NSE":
                                case "BSE":
                                    symbolObject.MarketType = MarketType.Spot;
                                    break;
                                case "NFO":
                                case "BFO":
                                    symbolObject.MarketType = MarketType.UsdM;
                                    break;
                                case "MCX":
                                    symbolObject.MarketType = MarketType.Futures;
                                    break;
                                case "CDS":
                                    symbolObject.MarketType = MarketType.CoinM;
                                    break;
                                default:
                                    symbolObject.MarketType = MarketType.Spot;
                                    break;
                            }

                            _instrumentTokenCache[instrument.symbol] = instrument.instrument_token;

                            // Add to the collection
                            exchangeInformation.Add(symbolObject);
                            count++;

                            // Log progress for every 1000 symbols
                            if (count % 1000 == 0)
                            {
                                Logger.Info($"Loaded {count} symbols from JSON file so far");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error parsing symbol from JSON: {ex.Message}");
                            // Continue with next symbol
                        }
                    }

                    Logger.Info($"Successfully loaded {count} symbols from JSON file");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception in GetExchangeInformation: {ex.Message}");
                    MessageBox.Show($"Error loading symbols from JSON file: {ex.Message}",
                        "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return exchangeInformation;
            });
        }

        /// <summary>
        /// Registers Zerodha symbols in NinjaTrader
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RegisterSymbols()
        {
            var symbols = await GetExchangeInformation();
            int createdCount = 0;

            foreach (var symbol in symbols)
            {
                try
                {
                    string ntName;
                    symbol.QuoteAsset = "NSE";
                    bool success = CreateInstrument(symbol, out ntName);
                    if (success)
                    {
                        createdCount++;
                        Logger.Info($"✅ Created NT Instrument: {ntName}");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"❌ Exception Occurred: {e.Message}");
                }
            }

            Logger.Info($"✅ Total symbols created: {createdCount}");
        }

        /// <summary>
        /// Gets the symbol name with market type
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="marketType">The market type</param>
        /// <returns>The symbol name</returns>
        public static string GetSymbolName(string symbol, out MarketType marketType)
        {
            marketType = MarketType.Spot;

            if (symbol == "NIFTY_I")
            {
                return "NIFTY25MAYFUT";
            }

            string[] collection = symbol.Split('_');
            if (collection == null || collection.Length == 0)
                return "";
            if (collection.Length == 1 && collection[0].Contains("MCX")) marketType = MarketType.MCX;
            if (collection.Length == 2)
            {
                string str = "_" + collection[1].ToUpper();
                if (str.Contains("MCX"))
                    marketType = MarketType.MCX;
                if (str == "_NFO" || str == "_FNO")
                    marketType = MarketType.UsdM;
            }
            return collection[0];
        }

        /// <summary>
        /// Gets a valid name for the symbol with market type
        /// </summary>
        /// <param name="value">The symbol value</param>
        /// <param name="marketType">The market type</param>
        /// <returns>The valid name</returns>
        public string GetValidName(string value, MarketType marketType)
        {
            value = value.ToUpperInvariant();
            return value + GetSuffix(marketType);
        }

        /// <summary>
        /// Gets the suffix for a market type
        /// </summary>
        /// <param name="marketType">The market type</param>
        /// <returns>The suffix</returns>
        public static string GetSuffix(MarketType marketType)
        {
            switch (marketType)
            {
                case MarketType.Spot:
                    return "_NSE";
                case MarketType.UsdM:
                    return "_NFO";
                case MarketType.CoinM:
                    return "_MCX";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Creates an instrument in NinjaTrader
        /// </summary>
        /// <param name="instrument">The instrument to create</param>
        /// <param name="ntSymbolName">The NinjaTrader symbol name</param>
        /// <returns>True if the instrument was created successfully, false otherwise</returns>
        public bool CreateInstrument(SymbolObject instrument, out string ntSymbolName)
        {
            ntSymbolName = "";
            InstrumentType instrumentType = InstrumentType.Stock;
            string validName = instrument.Symbol;

            // We'll use the default trading hours for now
            // In a real implementation, we would need to set up proper trading hours
            Logger.Info("Using default trading hours");

            MasterInstrument masterInstrument1 = MasterInstrument.DbGet(validName, instrumentType) ?? MasterInstrument.DbGet(validName, instrumentType);
            string symbol = instrument.Symbol;
            if (masterInstrument1 != null)
            {
                ntSymbolName = validName;
                if (DataContext.Instance.SymbolNames.ContainsKey(validName))
                    return false;
                DataContext.Instance.SymbolNames.Add(validName, symbol);
                int index = 1019;
                List<string> stringList = new List<string>((IEnumerable<string>)masterInstrument1.ProviderNames);
                for (int count = stringList.Count; count <= index; ++count)
                    stringList.Add("");
                masterInstrument1.ProviderNames = stringList.ToArray();
                masterInstrument1.ProviderNames[index] = instrument.Symbol;

                // Set default trading hours if not already set
                if (masterInstrument1.TradingHours == null)
                {
                    // Try to find a trading hours template by name
                    string tradingHoursName = "Default 24 x 7";
                    // Use reflection to get the trading hours to avoid direct namespace reference
                    object tradingHoursObj = null;
                    try {
                        // Try to find the Get method on TradingHours using reflection
                        var tradingHoursType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == "TradingHours");
                            
                        if (tradingHoursType != null) {
                            var getMethod = tradingHoursType.GetMethod("Get", new[] { typeof(string) });
                            if (getMethod != null) {
                                tradingHoursObj = getMethod.Invoke(null, new object[] { tradingHoursName });
                            }
                        }
                    } catch (Exception ex) {
                        Logger.Error($"Error getting trading hours: {ex.Message}");
                    }
                    
                    if (tradingHoursObj != null)
                    {
                        Logger.Info($"Setting trading hours template: {tradingHoursName}");
                        
                        // Use reflection to set the TradingHours property to avoid type conversion issues
                        var tradingHoursProperty = typeof(MasterInstrument).GetProperty("TradingHours");
                        if (tradingHoursProperty != null)
                        {
                            tradingHoursProperty.SetValue(masterInstrument1, tradingHoursObj);
                        }
                    }
                    else
                    {
                        Logger.Error("No trading hours template found. Please create a trading hours template in NinjaTrader.");
                        return false;
                    }
                }

                masterInstrument1.DbUpdate();
                MasterInstrument.DbUpdateCache();
                return true;
            }

            double num = 0.0;
            if (instrument.Filters != null && instrument.Filters.Length != 0)
            {
                Filter filter = ((IEnumerable<Filter>)instrument.Filters).FirstOrDefault<Filter>((Func<Filter, bool>)(x => x.FilterType == "PRICE_FILTER"));
                if (filter != null)
                    num = filter.TickSize;
            }

            int index1 = 1019;
            List<string> stringList1 = new List<string>();
            for (int count = stringList1.Count; count <= index1; ++count)
                stringList1.Add("");

            // We don't set trading hours directly
            // This is similar to the original plugin's approach
            Logger.Info("Using default trading hours");
            
            // Set up trading hours - this is crucial to prevent the TradingHours == null error
            Logger.Info("Setting up trading hours");
            
            // Create the MasterInstrument without setting TradingHours in the constructor
            MasterInstrument masterInstrument2 = new MasterInstrument()
            {
                Description = instrument.Symbol,
                InstrumentType = instrumentType,
                Name = validName,
                PointValue = 1.0,
                TickSize = num > 0 ? num : 0.05, // Default tick size if not specified
                Url = new Uri("https://kite.zerodha.com"),
                Exchanges = {
                    Exchange.Default
                },
                Currency = Currency.IndianRupee,
                ProviderNames = stringList1.ToArray()
            };

            masterInstrument2.ProviderNames[index1] = instrument.Symbol;
            
            // Set the trading hours property based on segment
            try
            {
                // Determine the appropriate trading hours template based on the segment
                string tradingHoursName = "Default 24 x 7";
                
                // Extract segment from the instrument's QuoteAsset or BaseAsset
                string segment = instrument.QuoteAsset;
                if (string.IsNullOrEmpty(segment))
                {
                    segment = instrument.BaseAsset;
                }
                
                // Set specific trading hours template based on segment
                if (!string.IsNullOrEmpty(segment))
                {
                    segment = segment.ToUpper();
                    if (segment.Contains("NSE") || segment.Contains("NFO") || 
                        segment.Contains("BSE") || segment.Contains("BFO"))
                    {
                        tradingHoursName = "Nse";
                    }
                    else if (segment.Contains("MCX"))
                    {
                        tradingHoursName = "MCX";
                    }
                }
                
                Logger.Info($"Setting trading hours template: {tradingHoursName}");
                
                // Use reflection to set the trading hours property
                // This approach avoids direct reference to the TradingHours class which might be causing issues
                var tradingHoursProperty = typeof(MasterInstrument).GetProperty("TradingHours");
                if (tradingHoursProperty != null)
                {
                    // Try to find the TradingHours class using reflection
                    Type tradingHoursType = null;
                    
                    // Try different possible namespaces for TradingHours
                    string[] possibleNamespaces = new[] {
                        "NinjaTrader.Cbi.TradingHours",
                        "NinjaTrader.Core.TradingHours",
                        "NinjaTrader.TradingHours"
                    };
                    
                    foreach (var ns in possibleNamespaces)
                    {
                        try
                        {
                            tradingHoursType = Type.GetType(ns) ?? 
                                              AppDomain.CurrentDomain.GetAssemblies()
                                                .SelectMany(a => a.GetTypes())
                                                .FirstOrDefault(t => t.FullName == ns);
                                                
                            if (tradingHoursType != null)
                                break;
                        }
                        catch
                        {
                            // Ignore errors and try the next namespace
                        }
                    }
                    
                    // If we couldn't find the type, try to get it from a known instance
                    if (tradingHoursType == null)
                    {
                        // Try to get a MasterInstrument that has TradingHours set
                        var existingInstrument = MasterInstrument.All.FirstOrDefault(mi => mi.TradingHours != null);
                        if (existingInstrument != null && existingInstrument.TradingHours != null)
                        {
                            tradingHoursType = existingInstrument.TradingHours.GetType();
                            
                            // Just use this existing trading hours
                            tradingHoursProperty.SetValue(masterInstrument2, existingInstrument.TradingHours);
                            Logger.Info("Using existing trading hours template");
                            return true;
                        }
                    }
                    
                    // If we still couldn't find it, try a direct approach
                    if (tradingHoursType == null)
                    {
                        Logger.Error("Could not find TradingHours type. Using direct property access.");
                        
                        // Try to set the TradingHoursName property instead
                        var tradingHoursNameProperty = typeof(MasterInstrument).GetProperty("TradingHoursName");
                        if (tradingHoursNameProperty != null)
                        {
                            tradingHoursNameProperty.SetValue(masterInstrument2, tradingHoursName);
                            Logger.Info($"Set TradingHoursName to {tradingHoursName}");
                            return true;
                        }
                        
                        return false;
                    }
                    
                    // Get the All property from the TradingHours type
                    var allTradingHoursProperty = tradingHoursType.GetProperty("All", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (allTradingHoursProperty != null)
                    {
                        var allTradingHours = allTradingHoursProperty.GetValue(null) as System.Collections.IList;
                        if (allTradingHours != null && allTradingHours.Count > 0)
                        {
                            // Find the trading hours template by name
                            object selectedTemplate = null;
                            foreach (var template in allTradingHours)
                            {
                                var nameProperty = template.GetType().GetProperty("Name");
                                if (nameProperty != null)
                                {
                                    string name = nameProperty.GetValue(template) as string;
                                    if (name == tradingHoursName)
                                    {
                                        selectedTemplate = template;
                                        break;
                                    }
                                }
                            }
                            
                            // If not found, use the first template
                            if (selectedTemplate == null && allTradingHours.Count > 0)
                            {
                                selectedTemplate = allTradingHours[0];
                            }
                            
                            if (selectedTemplate != null)
                            {
                                tradingHoursProperty.SetValue(masterInstrument2, selectedTemplate);
                                Logger.Info("Successfully set trading hours template");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting trading hours: {ex.Message}");
            }
            
            masterInstrument2.DbAdd(false);

            new Instrument()
            {
                Exchange = Exchange.Default,
                MasterInstrument = masterInstrument2
            }.DbAdd();

            if (!DataContext.Instance.SymbolNames.ContainsKey(validName))
                DataContext.Instance.SymbolNames.Add(validName, instrument.Symbol);

            ntSymbolName = validName;
            return true;
        }

        /// <summary>
        /// Removes an instrument from NinjaTrader
        /// </summary>
        /// <param name="instrument">The instrument to remove</param>
        /// <returns>True if the instrument was removed successfully, false otherwise</returns>
        public bool RemoveInstrument(SymbolObject instrument)
        {
            InstrumentType instrumentType = InstrumentType.Stock;
            string symbol = instrument.Symbol;
            MasterInstrument masterInstrument = MasterInstrument.DbGet(symbol, instrumentType);
            if (masterInstrument == null)
                return false;
            int index = 1019;
            if (masterInstrument.Url.AbsoluteUri == "https://kite.zerodha.com/")
                masterInstrument.DbRemove();
            else if (((IEnumerable<string>)masterInstrument.ProviderNames).ElementAtOrDefault<string>(index) != null)
            {
                masterInstrument.UserData = null;
                masterInstrument.ProviderNames[index] = "";
                masterInstrument.DbUpdate();
            }
            if (DataContext.Instance.SymbolNames.ContainsKey(symbol))
                DataContext.Instance.SymbolNames.Remove(symbol);
            return true;
        }

        /// <summary>
        /// Gets all NinjaTrader symbols
        /// </summary>
        /// <returns>A collection of symbol objects</returns>
        public async Task<ObservableCollection<SymbolObject>> GetNTSymbols()
        {
            return await Task.Run(() =>
            {
                ObservableCollection<SymbolObject> ntSymbols = new ObservableCollection<SymbolObject>();
                IEnumerable<MasterInstrument> source = MasterInstrument.All
                    .Where(x => !string.IsNullOrEmpty(x.ProviderNames.ElementAtOrDefault(1019)));

                foreach (MasterInstrument masterInstrument in source.OrderBy(x => x.Name).ToList())
                {
                    ntSymbols.Add(new SymbolObject()
                    {
                        Symbol = masterInstrument.Name
                    });
                }

                return ntSymbols;
            });
        }

        /// <summary>
        /// Gets the segment for a given instrument token.
        /// </summary>
        /// <param name="token">The instrument token.</param>
        /// <returns>The segment string if found; otherwise, null.</returns>
        public string GetSegmentForToken(long token)
        {
            if (_isInitialized && _tokenToInstrumentDataMap.TryGetValue(token, out InstrumentData data))
            {
                return data.segment;
            }
            Logger.Warn($"InstrumentManager: Instrument data not found for token {token} when trying to get segment.");
            return null;
        }

        /// <summary>
        /// Class to deserialize the JSON data
        /// </summary>
        private class MappedInstrument
        {
            public string symbol { get; set; }
            public string underlying { get; set; }
            public DateTime expiry { get; set; }
            public double strike { get; set; }
            public string option_type { get; set; }
            public string segment { get; set; }
            public long instrument_token { get; set; }
            public int exchange_token { get; set; }
            public string zerodhaSymbol { get; set; }
            public double tick_size { get; set; }
            public int lot_size { get; set; }
        }
    }
}
