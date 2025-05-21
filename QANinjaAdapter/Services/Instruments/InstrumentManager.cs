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
            
            try 
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string jsonFilePath = Path.Combine(documentsPath, DB_FILE_PATH);

                if (!File.Exists(jsonFilePath))
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Instrument mapping file not found at: {jsonFilePath}", NinjaTrader.Cbi.LogLevel.Error);
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
                            
                            // Also map zerodhaSymbol if it's different
                            if (!string.IsNullOrEmpty(instrument.zerodhaSymbol) && 
                                !instrument.zerodhaSymbol.Equals(instrument.symbol, StringComparison.OrdinalIgnoreCase))
                            {
                                _symbolToTokenMap[instrument.zerodhaSymbol] = instrument.instrument_token;
                            }
                        }
                    }
                    
                    _isInitialized = true;
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Loaded {_symbolToTokenMap.Count} instrument mappings from {jsonFilePath}", NinjaTrader.Cbi.LogLevel.Information);
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"Error loading instrument mappings: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
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
                NinjaTrader.NinjaScript.NinjaScript.Log($"Error getting instrument token: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
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

                NinjaTrader.NinjaScript.NinjaScript.Log("Loading instrument tokens from Zerodha...", NinjaTrader.Cbi.LogLevel.Information);

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
                            NinjaTrader.NinjaScript.NinjaScript.Log("No instruments found in CSV", NinjaTrader.Cbi.LogLevel.Warning);
                            return;
                        }

                        // Get column indices
                        string[] headers = lines[0].Split(',');
                        int tradingSymbolIndex = Array.IndexOf(headers, "tradingsymbol");
                        int instrumentTokenIndex = Array.IndexOf(headers, "instrument_token");
                        int exchangeIndex = Array.IndexOf(headers, "exchange");

                        if (tradingSymbolIndex < 0 || instrumentTokenIndex < 0 || exchangeIndex < 0)
                        {
                            NinjaTrader.NinjaScript.NinjaScript.Log("Required columns not found in CSV", NinjaTrader.Cbi.LogLevel.Error);
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

                        NinjaTrader.NinjaScript.NinjaScript.Log($"Loaded {_instrumentTokenCache.Count} instrument tokens", NinjaTrader.Cbi.LogLevel.Information);
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        NinjaTrader.NinjaScript.NinjaScript.Log($"Error loading instruments: {response.StatusCode}, {errorContent}", NinjaTrader.Cbi.LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"Error loading instrument tokens: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
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
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Reading symbols from JSON file", NinjaTrader.Cbi.LogLevel.Information);

                    if (!File.Exists(jsonFilePath))
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log($"Error: JSON file does not exist", NinjaTrader.Cbi.LogLevel.Error);
                        MessageBox.Show($"Symbol JSON file not found at: {jsonFilePath}",
                                "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return exchangeInformation;
                    }

                    // Read the JSON file
                    string jsonContent = File.ReadAllText(jsonFilePath);

                    // Deserialize JSON to list of mapped instruments
                    var mappedInstruments = JsonConvert.DeserializeObject<List<MappedInstrument>>(jsonContent);

                    NinjaTrader.NinjaScript.NinjaScript.Log($"Successfully read JSON file", NinjaTrader.Cbi.LogLevel.Information);

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

                            // Set market type based on segment/exchange
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
                    NinjaTrader.NinjaScript.NinjaScript.Log(ex.Message, NinjaTrader.Cbi.LogLevel.Error);
                    NinjaTrader.NinjaScript.NinjaScript.Log($"{ex.Message} + {ex.Source} + {ex.StackTrace}", NinjaTrader.Cbi.LogLevel.Error);
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
                        NinjaTrader.NinjaScript.NinjaScript.Log($"✅ Created NT Instrument: {ntName}", NinjaTrader.Cbi.LogLevel.Information);
                    }
                }
                catch (Exception e)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"❌ Exception Occurred: {e.Message}", NinjaTrader.Cbi.LogLevel.Information);
                }
            }

            NinjaTrader.NinjaScript.NinjaScript.Log($"✅ Total symbols created: {createdCount}", NinjaTrader.Cbi.LogLevel.Information);
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
            NinjaTrader.NinjaScript.NinjaScript.Log("Using default trading hours", NinjaTrader.Cbi.LogLevel.Information);

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

                // We don't set trading hours directly
                // masterInstrument1.TradingHours = tradingHours;

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
                // We don't set trading hours directly
                // TradingHours = tradingHours,
                ProviderNames = stringList1.ToArray()
            };

            masterInstrument2.ProviderNames[index1] = instrument.Symbol;
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
