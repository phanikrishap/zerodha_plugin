using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Utility;
using QABrokerAPI.Common.Models.Request;
using QABrokerAPI.Common.Models.Response;
using QABrokerAPI.Common.Models.WebSocket;
using QABrokerAPI.Zerodha;
using QABrokerAPI.Zerodha.Websockets;
using QABrokerAPI.Common.Interfaces;
using System;
using System.Linq;
//using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
//using Websocket.Client;
//using StreamingService.Services;
//using StreamingService.Repositories;
//using System.Buffers.Binary;
using System.Collections.Concurrent;



using QANinjaAdapter.Annotations;
using QANinjaAdapter.Classes;
using QANinjaAdapter.Classes.Binance.Symbols;
using QANinjaAdapter.Parsers;
using QANinjaAdapter.ViewModels;
using log4net;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;


using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;

using System.Windows;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Data.SQLite;
using System.Buffers.Binary;

//using NinjaTrader.CQG.ProtoBuf;



#nullable disable
namespace QANinjaAdapter
{
    public class Connector : INotifyPropertyChanged
    {
        private bool _connected;
        private static string apiKey = "6g794lmwuo1wdmr7";
        private static string secretKey = "hrof7sxzmow48d7snklkt6eekhgw0869";
        private static string accessToken = "nOggvCqOYCBVWDeb7LHYfw7LQbksumra"; // Added for Zerodha
        private bool _isClearStocks;
        private Dictionary<string, long> _instrumentTokenCache = new Dictionary<string, long>();
        private ConcurrentDictionary<string, int> _previousVolumes = new ConcurrentDictionary<string, int>();
        // Configuration file path
        // Configuration file path
        private const string CONFIG_FILE_PATH = "NinjaTrader 8\\QAAdapter\\config.json";
        //private const string Db_FILE_PATH = "NinjaTrader 8\\QAAdapter\\mapped_instruments.db";
        private const string Db_FILE_PATH = "NinjaTrader 8\\QAAdapter\\mapped_instruments.json";

        private static BrokerClient _client;
        private static Connector _instance;
        private ControlCenter _controlCenter;

        private WebSocketBroker _webSocketBroker = WebSocketBroker.Zerodha;
        private HistoricalBroker _historicalBroker = HistoricalBroker.Zerodha;

        private enum WebSocketBroker
        {
            Zerodha,
            Upstox,
            TrueData,
            Binance
        }

        private enum HistoricalBroker
        {
            Zerodha,
            Upstox,
            TrueData,
            Binance
        }

        public string Version { get; } = "2.0.1";

        // Method to load configuration
        private bool LoadConfiguration()
        {
            try
            {
                // Get the user's Documents folder path
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fullConfigPath = Path.Combine(documentsPath, CONFIG_FILE_PATH);

                // Check if config file exists
                if (!File.Exists(fullConfigPath))
                {
                    MessageBox.Show($"Configuration file not found at: {fullConfigPath}",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Read JSON configuration
                string jsonConfig = File.ReadAllText(fullConfigPath);
                JObject config = JObject.Parse(jsonConfig);

                // Get active broker configurations
                JObject activeBrokers = config["Active"] as JObject;

                if (activeBrokers == null)
                {
                    MessageBox.Show("No active broker specified in the configuration file.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Get websocket and historical broker names
                string webSocketBrokerName = activeBrokers["Websocket"]?.ToString();
                string historicalBrokerName = activeBrokers["Historical"]?.ToString();

                if (string.IsNullOrEmpty(webSocketBrokerName) || string.IsNullOrEmpty(historicalBrokerName))
                {
                    MessageBox.Show("Websocket or Historical broker not specified in Active configuration.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Set broker types
                if (Enum.TryParse(webSocketBrokerName, true, out WebSocketBroker wsb))
                {
                    _webSocketBroker = wsb;
                }

                if (Enum.TryParse(historicalBrokerName, true, out HistoricalBroker hb))
                {
                    _historicalBroker = hb;
                }

                // Load websocket broker configuration
                JObject webSocketBrokerConfig = config[webSocketBrokerName] as JObject;

                Logger.Info($"Loading configuration for websocket broker: {webSocketBrokerName}, {webSocketBrokerConfig} ");

                if (webSocketBrokerConfig != null)
                {
                    // Update API keys for websocket
                    LoadBrokerCredentials(webSocketBrokerConfig);
                }
                else
                {
                    MessageBox.Show($"Configuration for websocket broker '{webSocketBrokerName}' not found.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // If historical broker is different, load its configuration too
                if (webSocketBrokerName != historicalBrokerName)
                {
                    JObject historicalBrokerConfig = config[historicalBrokerName] as JObject;
                    if (historicalBrokerConfig != null)
                    {
                        // You might need separate variables for historical broker
                        // This is just updating the same variables
                        LoadBrokerCredentials(historicalBrokerConfig);
                    }
                    else
                    {
                        MessageBox.Show($"Configuration for historical broker '{historicalBrokerName}' not found.",
                            "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}",
                    "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void LoadBrokerCredentials(JObject brokerConfig)
        {
            apiKey = brokerConfig["Api"]?.ToString() ?? apiKey;
            secretKey = brokerConfig["Secret"]?.ToString() ?? secretKey;
            accessToken = brokerConfig["AccessToken"]?.ToString() ?? accessToken;

            Logger.Info($"Loaded Configration for the broker {brokerConfig}, apiKey:{apiKey}, secretKey:{secretKey}");
        }

        public (string ApiKey, string SecretKey, string AccessToken) GetCredentialsForBroker(string brokerName)
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fullConfigPath = Path.Combine(documentsPath, CONFIG_FILE_PATH);

                if (!File.Exists(fullConfigPath))
                {
                    return (apiKey, secretKey, accessToken);
                }

                string jsonConfig = File.ReadAllText(fullConfigPath);
                JObject config = JObject.Parse(jsonConfig);

                JObject brokerConfig = config[brokerName] as JObject;
                if (brokerConfig == null)
                {
                    return (apiKey, secretKey, accessToken);
                }

                string bApiKey = brokerConfig["Api"]?.ToString() ?? apiKey;
                string bSecretKey = brokerConfig["Secret"]?.ToString() ?? secretKey;
                string bAccessToken = brokerConfig["AccessToken"]?.ToString() ?? accessToken;

                return (bApiKey, bSecretKey, bAccessToken);
            }
            catch
            {
                return (apiKey, secretKey, accessToken);
            }
        }

        public Connector()
        {
            Logger.Initialize();

            Logger.Info($"QANinjaAdapter v{Version} initializing...");

            // Load configuration
            if (!LoadConfiguration())
            {
                // Handle configuration failure
                MessageBox.Show("Using default API keys. Please check your configuration file.",
                    "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public bool IsConnected
        {
            get => this._connected;
            private set
            {
                if (this._connected == value)
                    return;
                this._connected = value;
                this.OnPropertyChanged(nameof(IsConnected));
            }
        }

        public static BrokerClient Client
        {
            get
            {
                if (Connector._client == null)
                {
                    ILog logger = LogManager.GetLogger(typeof(Connector));
                    logger.Debug((object)"Connector Debug");

                    // Initialize Zerodha client with access token
                    Connector._client = new BrokerClient(new ClientConfiguration()
                    {
                        ApiKey = Connector.apiKey,
                        SecretKey = Connector.secretKey,
                        AccessToken = Connector.accessToken,
                        Logger = logger
                    });
                }
                return Connector._client;
            }
        }

        public static Connector Instance
        {
            get
            {
                if (Connector._instance == null)
                    Connector._instance = new Connector();
                return Connector._instance;
            }
        }

        public bool CheckConnection()
        {
            // Check connection to Zerodha API instead of Binance
            if (!this.CheckConnection("https://api.kite.trade", "/ping"))
                return false;

            this.IsConnected = true;
            return true;
        }

        private bool CheckConnection(string baseUrl, string ping)
        {
            using (HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            })
            {
                try
                {
                    // For Zerodha, use a valid endpoint
                    // The valid endpoint for a basic check is "/session/token" 
                    // or try "/api/ticks" which is a common endpoint

                    httpClient.DefaultRequestHeaders.Add("X-Kite-Apikey", apiKey);
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");

                    // Try a valid endpoint - for example, the user profile endpoint
                    var x = httpClient.GetAsync("/user/profile").Result;

                    // Log response details for troubleshooting
                    Logger.Info($"Zerodha API connection response: {x.StatusCode} - {x.ReasonPhrase}");

                    if (!x.IsSuccessStatusCode)
                    {
                        string content = x.Content.ReadAsStringAsync().Result;
                        Logger.Info($"Response content: {content}");

                        // Parse JSON response to check for token errors
                        try
                        {
                            JObject errorJson = JObject.Parse(content);
                            string errorType = errorJson["error_type"]?.ToString();
                            string errorMessage = errorJson["message"]?.ToString();

                            if (errorType == "TokenException" ||
                                errorMessage?.Contains("token") == true ||
                                errorMessage?.Contains("authorization") == true ||
                                errorMessage?.Contains("access") == true)
                            {
                                // Specific log for token errors
                                Logger.Error($"Access token invalid or expired: {errorMessage}");
                                NinjaTrader.NinjaScript.NinjaScript.Log($"Authentication Error: Access token invalid or expired. Please update your token.", NinjaTrader.Cbi.LogLevel.Error);
                            }
                            else
                            {
                                // General API error
                                NinjaTrader.NinjaScript.NinjaScript.Log(content, NinjaTrader.Cbi.LogLevel.Error);
                            }
                        }
                        catch
                        {
                            // If JSON parsing fails, log the raw content
                            NinjaTrader.NinjaScript.NinjaScript.Log(content, NinjaTrader.Cbi.LogLevel.Error);
                        }
                    }

                    return x.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Connection check error: {ex.Message}");
                    return false;
                }
            }
        }

        // Method to populate instrument tokens from the CSV data
        private async Task LoadInstrumentTokens()
        {
            try
            {
                // Only load if not already loaded
                if (_instrumentTokenCache.Count > 0)
                    return;

                NinjaTrader.NinjaScript.NinjaScript.Log("Loading instrument tokens from Zerodha...", NinjaTrader.Cbi.LogLevel.Information);

                using (HttpClient client = new HttpClient())
                {
                    // Set up credentials
                    client.DefaultRequestHeaders.Add("X-Kite-Apikey", apiKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");

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

        //public async Task<ObservableCollection<SymbolObject>> GetExchangeInformation()
        //{
        //    return await Task.Run(() =>
        //    {
        //        ObservableCollection<SymbolObject> exchangeInformation = new ObservableCollection<SymbolObject>();

        //        try
        //        {
        //            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        //            string dbFilePath = Path.Combine(documentsPath, Db_FILE_PATH);

        //            Logger.Info($"Reading symbols from database: {dbFilePath}");
        //            NinjaTrader.NinjaScript.NinjaScript.Log($"Reading symbols from database", NinjaTrader.Cbi.LogLevel.Information);

        //            if (!File.Exists(dbFilePath))
        //            {
        //                NinjaTrader.NinjaScript.NinjaScript.Log($"Error Db does not exist", NinjaTrader.Cbi.LogLevel.Error);
        //                MessageBox.Show($"Symbol database file not found at: {dbFilePath}",
        //                        "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                return exchangeInformation;
        //            }

        //            // Using System.Data.SQLite instead
        //            string connectionString = $"Data Source={dbFilePath};Version=3;Read Only=True;";

        //            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        //            {
        //                connection.Open();
        //                NinjaTrader.NinjaScript.NinjaScript.Log($"Connection Open to DB", NinjaTrader.Cbi.LogLevel.Information);

        //                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM mapped_instruments", connection))
        //                {
        //                    using (SQLiteDataReader reader = command.ExecuteReader())
        //                    {
        //                        int count = 0;

        //                        while (reader.Read())
        //                        {
        //                            try
        //                            {
        //                                // Extract data from row
        //                                string symbol = reader["symbol"] as string ?? string.Empty;
        //                                string underlying = reader["underlying"] as string ?? string.Empty;
        //                                string segment = reader["segment"] as string ?? string.Empty;
        //                                long instrumentToken = reader["instrument_token"] != DBNull.Value ? Convert.ToInt64(reader["instrument_token"]) : 0;
        //                                string zerodhaSymbol = reader["zerodhaSymbol"] as string ?? symbol;
        //                                double tickSize = reader["tick_size"] != DBNull.Value ? Convert.ToDouble(reader["tick_size"]) : 0.05;
        //                                int lotSize = reader["lot_size"] != DBNull.Value ? Convert.ToInt32(reader["lot_size"]) : 1;

        //                                // Skip empty symbols
        //                                if (string.IsNullOrEmpty(symbol))
        //                                    continue;

        //                                // Create filters for tick size and lot size
        //                                List<Filter> filters = new List<Filter>();

        //                                // Add PRICE_FILTER if tick_size is available
        //                                if (tickSize > 0)
        //                                {
        //                                    filters.Add(new Filter
        //                                    {
        //                                        FilterType = "PRICE_FILTER",
        //                                        TickSize = tickSize
        //                                    });
        //                                }

        //                                // Add LOT_SIZE if lot_size is available
        //                                if (lotSize > 0)
        //                                {
        //                                    filters.Add(new Filter
        //                                    {
        //                                        FilterType = "LOT_SIZE",
        //                                        StepSize = Convert.ToDouble(lotSize)
        //                                    });
        //                                }

        //                                // Create the SymbolObject
        //                                SymbolObject symbolObject = new SymbolObject
        //                                {
        //                                    Symbol = zerodhaSymbol,
        //                                    BaseAsset = zerodhaSymbol,
        //                                    QuoteAsset = segment, // Using segment as exchange/quote asset
        //                                    Status = "TRADING",
        //                                    Filters = filters.ToArray()
        //                                };

        //                                // Set market type based on segment/exchange
        //                                switch (segment.ToUpper())
        //                                {
        //                                    case "NSE":
        //                                    case "BSE":
        //                                        symbolObject.MarketType = MarketType.Spot;
        //                                        break;
        //                                    case "NFO":
        //                                    case "BFO":
        //                                        symbolObject.MarketType = MarketType.UsdM;
        //                                        break;
        //                                    case "MCX":
        //                                        symbolObject.MarketType = MarketType.Futures;
        //                                        break;
        //                                    case "CDS":
        //                                        symbolObject.MarketType = MarketType.CoinM;
        //                                        break;
        //                                    default:
        //                                        symbolObject.MarketType = MarketType.Spot;
        //                                        break;
        //                                }
        //                                _instrumentTokenCache[symbol] = instrumentToken;
        //                                // Add to the collection
        //                                exchangeInformation.Add(symbolObject);
        //                                count++;

        //                                // Log progress for every 1000 symbols
        //                                if (count % 1000 == 0)
        //                                {
        //                                    Logger.Info($"Loaded {count} symbols from database so far");
        //                                }
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                Logger.Error($"Error parsing symbol from database: {ex.Message}");
        //                                // Continue with next symbol
        //                            }
        //                        }

        //                        Logger.Info($"Successfully loaded {count} symbols from database");
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error($"Exception in GetExchangeInformation: {ex.Message}");
        //            NinjaTrader.NinjaScript.NinjaScript.Log(ex.Message, NinjaTrader.Cbi.LogLevel.Error);
        //            NinjaTrader.NinjaScript.NinjaScript.Log($"{ex.Message} + {ex.Source} + {ex.StackTrace}", NinjaTrader.Cbi.LogLevel.Error);
        //            MessageBox.Show($"Error loading symbols from database: {ex.Message}",
        //                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }

        //        return exchangeInformation;
        //    });
        //}

        public async Task<ObservableCollection<SymbolObject>> GetExchangeInformation()
        {
            return await Task.Run(() =>
            {
                ObservableCollection<SymbolObject> exchangeInformation = new ObservableCollection<SymbolObject>();

                try
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string jsonFilePath = Path.Combine(documentsPath, Path.ChangeExtension(Db_FILE_PATH, ".json"));

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
                    var mappedInstruments = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MappedInstrument>>(jsonContent);

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
                                Symbol = instrument.symbol ,
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

        // Class to deserialize the JSON data
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
        //public async Task<ObservableCollection<SymbolObject>> GetExchangeInformation()
        //{
        //    return await Task.Run(() =>
        //    {
        //        ObservableCollection<SymbolObject> exchangeInformation = new ObservableCollection<SymbolObject>();
        //        using (HttpClient httpClient = new HttpClient()
        //        {
        //            BaseAddress = new Uri("https://api.kite.trade")
        //        })
        //        {
        //            try
        //            {
        //                // Add Zerodha authentication headers
        //                httpClient.DefaultRequestHeaders.Add("X-Kite-Apikey", apiKey);
        //                httpClient.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");

        //                // Get all instruments in one call
        //                HttpResponseMessage result = httpClient.GetAsync("/instruments").Result;

        //                if (result.IsSuccessStatusCode)
        //                {
        //                    string csvContent = result.Content.ReadAsStringAsync().Result;

        //                    // Parse all symbols
        //                    List<SymbolObject> symbols = ZerodhaDataParser.ParseSymbols(csvContent);

        //                    // Add symbols to the result collection with appropriate market types
        //                    foreach (SymbolObject symbol in symbols)
        //                    {
        //                        // Set market type based on exchange
        //                        switch (symbol.QuoteAsset.ToUpper())
        //                        {
        //                            case "NSE":
        //                            case "BSE":
        //                                symbol.MarketType = MarketType.Spot;
        //                                break;
        //                            case "NFO":
        //                            case "BFO":
        //                                symbol.MarketType = MarketType.UsdM;
        //                                break;
        //                            case "MCX":
        //                                symbol.MarketType = MarketType.Futures;
        //                                break;
        //                            case "CDS":
        //                                symbol.MarketType = MarketType.CoinM;
        //                                break;
        //                            default:
        //                                symbol.MarketType = MarketType.Spot;
        //                                break;
        //                        }

        //                        exchangeInformation.Add(symbol);
        //                    }

        //                    Logger.Info($"Added {exchangeInformation.Count} instruments from Zerodha");
        //                }
        //                else
        //                {
        //                    string errorContent = result.Content.ReadAsStringAsync().Result;
        //                    Logger.Error($"Failed to get instruments: {result.StatusCode} - {errorContent}");
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Logger.Error($"Exception in GetExchangeInformation: {ex.Message}");
        //                return null;
        //            }
        //        }
        //        return exchangeInformation;
        //    });
        //}

        public async Task RegisterBinanceSymbols()
        {
            var symbols = await GetExchangeInformation();
            int createdCount = 0;

            foreach (var symbol in symbols)
            {
                //Check if the symbol contains "SENSEX"
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
                    NinjaTrader.NinjaScript.NinjaScript.Log($"❌ Exception Occured: {e.Message}", NinjaTrader.Cbi.LogLevel.Information);
                }

            }
        }

        //    NinjaTrader.NinjaScript.NinjaScript.Log($"✅ Total symbols created: {createdCount}", (NinjaTrader.Cbi.LogLevel)2);
        //}
        //public async Task RegisterBinanceSymbols()
        //{
        //    //var symbols = await GetExchangeInformation();
        //    var symbol = new SymbolObject();
        //    List<Filter> filters = new List<Filter>();


            //    decimal tickSize = 0.1M;
            //    filters.Add(new Filter
            //    {
            //        FilterType = "PRICE_FILTER",
            //        TickSize = Convert.ToDouble(tickSize)
            //    });

            //    decimal lotSize = 75;
            //    filters.Add(new Filter
            //    {
            //        FilterType = "LOT_SIZE",
            //        StepSize = Convert.ToDouble(lotSize)
            //    });

            //    symbol = new SymbolObject
            //    {

            //        Symbol = "NIFTY_I",
            //        BaseAsset = "NIFTY25MAYFUT",
            //        QuoteAsset = "NIFTY25MAYFUT",
            //        Status = "TRADING",
            //        Filters = filters.ToArray()
            //    };
            //    int createdCount = 0;

            //    //if (symbol == null || symbols.Count == 0)
            //    if (symbol == null)
            //    {
            //        NinjaTrader.NinjaScript.NinjaScript.Log("❌ No symbols received from Binance", (NinjaTrader.Cbi.LogLevel)2);
            //        return;
            //    }

            //    //foreach (var sym in symbols)
            //    //{
            //    // Check if the symbol contains "SENSEX"
            //    try
            //    {

            //        string ntName;
            //        symbol.QuoteAsset = "NSE";
            //        bool success = CreateInstrument(symbol, out ntName);
            //        if (success)
            //        {
            //            createdCount++;
            //            NinjaTrader.NinjaScript.NinjaScript.Log($"✅ Created NT Instrument: {ntName}", NinjaTrader.Cbi.LogLevel.Information);
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        NinjaTrader.NinjaScript.NinjaScript.Log($"❌ Exception Occured: {e.Message}", NinjaTrader.Cbi.LogLevel.Information);
            //    }

            //    //}

            //    NinjaTrader.NinjaScript.NinjaScript.Log($"✅ Total symbols created: {createdCount}", (NinjaTrader.Cbi.LogLevel)2);
            //}

        public static string GetSymbolName(string symbol, out MarketType marketType)
        {


            marketType = MarketType.Spot;

            if(symbol == "NIFTY_I")
            {
                return "NIFTY25MAYFUT";
            }

            string[] collection = symbol.Split('_');
            if (collection.IsNullOrEmpty())
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

        public string GetValidName(string value, MarketType marketType)
        {
            value = value.ToUpperInvariant();
            return value + Connector.GetSuffix(marketType);
        }

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

        public bool CreateInstrument(SymbolObject instrument, out string ntSymbolName)
        {
            //string validName = this.GetValidName(instrument.Symbol, instrument.MarketType);
            ntSymbolName = "";
            InstrumentType instrumentType = InstrumentType.Stock;
            string validName = instrument.Symbol;

            // Get the NSE trading hours template that already exists
            TradingHours tradingHours = TradingHours.Get("Nse");
            
            // Fallback only if needed
            if (tradingHours == null)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log("Warning: Could not find NSE trading hours template, using default", NinjaTrader.Cbi.LogLevel.Warning);
                tradingHours = TradingHours.Get("Default 24 x 5");
            }
            else
            {
                NinjaTrader.NinjaScript.NinjaScript.Log("Using NSE trading hours template", NinjaTrader.Cbi.LogLevel.Information);
            }

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

                // Update trading hours to NSE
                masterInstrument1.TradingHours = tradingHours;

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
                TradingHours = tradingHours, // Always use NSE trading hours
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

        public void ClearWrongStocks() => this.FindCCControl();




        private NinjaTrader.Gui.Chart.Chart FindCCControl()
        {
            foreach (Window allWindow in NinjaTrader.Core.Globals.AllWindows)
            {
                if (allWindow is ControlCenter controlCenter)
                {
                    this._controlCenter = controlCenter;
                    this._controlCenter.GotFocus += new RoutedEventHandler(this.Cc_GotFocus);
                }
            }
            return (NinjaTrader.Gui.Chart.Chart)null;
        }

        private void Cc_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this._isClearStocks)
                    return;
                this.ClearStocks();
                this._controlCenter.GotFocus -= new RoutedEventHandler(this.Cc_GotFocus);
            }
            catch
            {
            }
        }

        public void ClearStocks()
        {
            foreach (MasterInstrument masterInstrument in MasterInstrument.All.Where<MasterInstrument>((Func<MasterInstrument, bool>)(x =>
            {
                if (x.InstrumentType != InstrumentType.Stock)
                    return false;
                return x.Name.EndsWith("_NSE") || x.Name.EndsWith("_MCX") || x.Name.EndsWith("_NFO");
            })).ToArray<MasterInstrument>())
            {
                if (string.IsNullOrEmpty(masterInstrument.Description))
                    masterInstrument.DbRemove();
            }
        }

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
                masterInstrument.UserData = (XDocument)null;
                masterInstrument.ProviderNames[index] = "";
                masterInstrument.DbUpdate();
            }
            if (DataContext.Instance.SymbolNames.ContainsKey(symbol))
                DataContext.Instance.SymbolNames.Remove(symbol);
            return true;
        }

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

        // In your Connector.cs file
            public async Task<List<Record>> GetHistoricalTrades(
        BarsPeriodType barsPeriodType,
        string symbol,
        DateTime fromDate,
        DateTime toDate,
        MarketType marketType,
        ViewModelBase viewModelBase)
            {
                // Log request parameters
                NinjaTrader.NinjaScript.NinjaScript.Log($"Getting historical data for {symbol}, period: {barsPeriodType}, market type: {marketType}, dates: {fromDate} to {toDate}", NinjaTrader.Cbi.LogLevel.Information);

                List<Record> records = new List<Record>();

                try
                {
                    // For Zerodha, we need to format the request correctly
                    if (barsPeriodType != BarsPeriodType.Tick)
                    {
                        // Get the instrument token
                        long instrumentToken = await GetInstrumentToken(symbol);

                        if (instrumentToken == 0)
                        {
                            NinjaTrader.NinjaScript.NinjaScript.Log($"Error: Could not find instrument token for {symbol}", NinjaTrader.Cbi.LogLevel.Error);
                            return records;
                        }

                    
                        //string fromDateStr = fromDate.ToString("dd/MM/yyyy HH:mm:ss");
                        //string toDateStr =   toDate.ToString("dd/MM/yyyy HH:mm:ss");
                        string fromDateStr = fromDate.ToString("yyyy-MM-dd HH:mm:ss");
                        string toDateStr = toDate.ToString("yyyy-MM-dd HH:mm:ss");
                        // Determine interval string based on BarsPeriodType
                        string interval = "day";
                        if (barsPeriodType == BarsPeriodType.Minute)
                        {
                            interval = "minute";
                        }
                    

                            // For MCX, we need to make multiple requests to get complete data
                            //if(marketType == MarketType.Futures || symbol.StartsWith("MCX"))
                            //{
                            //    NinjaTrader.NinjaScript.NinjaScript.Log($"Processing MCX data with extended hours", NinjaTrader.Cbi.LogLevel.Information);

                            //    // Zerodha API may limit how much data you can get in one request
                            //    // You might need to split the date range into smaller chunks
                            //    DateTime currentFrom = fromDate;
                            //    DateTime currentTo = (toDate - currentFrom).TotalDays > 60 ? currentFrom.AddDays(60) : toDate;

                            //    while (currentFrom < toDate)
                            //    {
                            //        string currentFromStr = currentFrom.ToString("yyyy-MM-dd");
                            //        string currentToStr = currentTo.ToString("yyyy-MM-dd");

                            //        NinjaTrader.NinjaScript.NinjaScript.Log($"Requesting MCX data chunk: {currentFromStr} to {currentToStr}", NinjaTrader.Cbi.LogLevel.Information);

                            //        // Get data for this chunk
                            //        var chunkRecords = await GetHistoricalDataChunk(instrumentToken, interval, currentFromStr, currentToStr);
                            //        records.AddRange(chunkRecords);

                            //        // Move to the next chunk
                            //        currentFrom = currentTo.AddDays(1);
                            //        currentTo = (toDate - currentFrom).TotalDays > 60 ? currentFrom.AddDays(60) : toDate;
                            //    }
                            //}
                            //else

                            // Use the original single request approach for non-MCX
                            records = await GetHistoricalDataChunk(instrumentToken, interval, fromDateStr, toDateStr);
                    
                    }
                    else
                    {
                        // Handle tick data if needed
                        NinjaTrader.NinjaScript.NinjaScript.Log("Tick data not supported for Zerodha", NinjaTrader.Cbi.LogLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Exception in GetHistoricalTrades: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                }

                NinjaTrader.NinjaScript.NinjaScript.Log($"Returning {records.Count} historical records", NinjaTrader.Cbi.LogLevel.Information);
                return records;
            }

        // Helper method to get a chunk of historical data
        private async Task<List<Record>> GetHistoricalDataChunk(long instrumentToken, string interval, string fromDateStr, string toDateStr)
        {
            List<Record> records = new List<Record>();

            using (HttpClient client = new HttpClient())
            {
                // Set up credentials
                client.DefaultRequestHeaders.Add("X-Kite-Apikey", apiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");

                // Format the URL
                string url = $"https://api.kite.trade/instruments/historical/{instrumentToken}/{interval}?from={fromDateStr}&to={toDateStr}";
                // Make the request
                HttpResponseMessage response = await client.GetAsync(url);
                //NinjaTrader.NinjaScript.NinjaScript.Log("Fetched Data ", NinjaTrader.Cbi.LogLevel.Information);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Received response with length: {content.Length} from {fromDateStr} to {toDateStr}", NinjaTrader.Cbi.LogLevel.Information);

                    // Parse the JSON response
                    JObject json = JObject.Parse(content);

                    // Check for data
                    if (json["data"] != null && json["data"]["candles"] != null)
                    {
                        JArray candles = (JArray)json["data"]["candles"];

                        foreach (JArray candle in candles.Cast<JArray>())
                        {
                            // Zerodha candle format: [timestamp, open, high, low, close, volume]
                            if (candle.Count >= 6)
                            {
                                // Parse timestamp
                                string timestampStr = candle[0].ToString(); // "2017-12-15T09:15:00+0530"
                                DateTime timestamp;

                                // Use DateTimeOffset to properly capture the timezone information
                                DateTimeOffset dto = DateTimeOffset.Parse(timestampStr);
                                timestamp = dto.DateTime;

                                // Explicitly specify this as IST time
                                timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Local);

                                NinjaTrader.NinjaScript.NinjaScript.Log($"Original timestamp: {timestampStr}", NinjaTrader.Cbi.LogLevel.Information);
                                NinjaTrader.NinjaScript.NinjaScript.Log($"Parsed timestamp: {timestamp} (Kind: {timestamp.Kind})", NinjaTrader.Cbi.LogLevel.Information);

                                // Create record
                                records.Add(new Record
                                {
                                    TimeStamp = timestamp,
                                    Open = Convert.ToDouble(candle[1]),
                                    High = Convert.ToDouble(candle[2]),
                                    Low = Convert.ToDouble(candle[3]),
                                    Close = Convert.ToDouble(candle[4]),
                                    Volume = Convert.ToDouble(candle[5])
                                });
                            }
                        }
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Error response: {response.StatusCode}, {errorContent} , {errorContent.ToLower()}", NinjaTrader.Cbi.LogLevel.Error);
                }
            }

            return records;
        }

        // Helper method to get instrument token
        private async Task<long> GetInstrumentToken(string symbol)
        {
            try
            {
                // Simulate asynchronous behavior to fix CS1998
                return await Task.Run(() =>
                {
                    if (symbol == "NIFTY25MAYFUT" || symbol == "NIFTY_I")
                        return 14626050;

                    // Ensure instrument tokens are loaded
                    if (_instrumentTokenCache.Count == 0)
                        throw new InvalidOperationException("Instrument tokens are not loaded.");

                    if (_instrumentTokenCache.TryGetValue(symbol, out long token))
                        return token;

                    throw new KeyNotFoundException($"Instrument token not found for symbol: {symbol}");
                });
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"Error getting instrument token: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                return 0;
            }
        }
        



        




        private static async Task<List<Record>> GetZerodhaKlines(
            IBrokerClient zerodhaClient,
            string symbol,
            DateTime fromDate,
            GetKlinesCandlesticksRequest klinesCandlesticksRequest,
            WebSocketConnectionFunc webSocketConnectionFunc,
            List<Record> records,
            MarketType marketType,
            ViewModelBase viewModelBase)
        {
            Guard.AgainstNullOrEmpty(symbol);
            Guard.AgainstNull((object)webSocketConnectionFunc);
            Guard.AgainstNull((object)klinesCandlesticksRequest);

            try
            {
                // Log request details
                NinjaTrader.NinjaScript.NinjaScript.Log($"Requesting historical data for {symbol} from {fromDate}", NinjaTrader.Cbi.LogLevel.Information);

                // Make the API call
                List<KlineCandleStickResponse> klinesCandlesticks = await zerodhaClient.GetKlinesCandlesticks(klinesCandlesticksRequest, marketType);

                // Log response
                NinjaTrader.NinjaScript.NinjaScript.Log($"Received {klinesCandlesticks?.Count ?? 0} candlesticks", NinjaTrader.Cbi.LogLevel.Information);

                if (klinesCandlesticks != null && klinesCandlesticks.Any())
                {
                    // Process data
                    klinesCandlesticks.ForEach((Action<KlineCandleStickResponse>)(k => records.Add(new Record()
                    {
                        Close = (double)k.Close,
                        High = (double)k.High,
                        Low = (double)k.Low,
                        Open = (double)k.Open,
                        Volume = (double)k.Volume,
                        TimeStamp = k.CloseTime
                    })));

                    return records;
                }
                else
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log("No data returned from API", NinjaTrader.Cbi.LogLevel.Warning);
                    return new List<Record>();
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"Error in GetZerodhaKlines: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                throw; // Rethrow to let caller handle it
            }
        }

        private static async Task GetZerodhaTrades(
            IBrokerClient zerodhaClient,
            string symbol,
            DateTime fromDate,
            GetCompressedAggregateTradesRequest aggTradesRequest,
            WebSocketConnectionFunc webSocketConnectionFunc,
            List<Record> records,
            MarketType marketType,
            ViewModelBase viewModelBase)
        {
            Guard.AgainstNullOrEmpty(symbol);
            Guard.AgainstNull((object)webSocketConnectionFunc);
            Guard.AgainstNull((object)aggTradesRequest);

            DateTime stopDate = fromDate.ToUniversalTime();
            DateTime? nullable1 = aggTradesRequest.EndTime;
            int i = 0;
            double total = 0.0;
            DateTime startDate = DateTime.MinValue;
            int percent = 0;

            try
            {
                while (true)
                {
                    if (nullable1.HasValue)
                        aggTradesRequest.EndTime = nullable1;

                    // Call Zerodha API to get historical trades
                    List<CompressedAggregateTradeResponse> aggTradesResults = await zerodhaClient.GetCompressedAggregateTrades(aggTradesRequest, marketType);
                    await Task.Delay(250);

                    aggTradesResults.ForEach((Action<CompressedAggregateTradeResponse>)(k => records.Add(new Record()
                    {
                        Close = (double)k.Price,
                        High = (double)k.Price,
                        Low = (double)k.Price,
                        Open = (double)k.Price,
                        Volume = (double)k.Quantity,
                        TimeStamp = k.Timestamp
                    })));

                    if (aggTradesResults.Any<CompressedAggregateTradeResponse>())
                    {
                        DateTime dateTime = aggTradesResults.Min<CompressedAggregateTradeResponse, DateTime>((Func<CompressedAggregateTradeResponse, DateTime>)(x => x.Timestamp));
                        nullable1 = new DateTime?(dateTime.AddMilliseconds(-1.0));
                        TimeSpan timeSpan;
                        if (i == 0)
                        {
                            startDate = aggTradesResults.Max<CompressedAggregateTradeResponse, DateTime>((Func<CompressedAggregateTradeResponse, DateTime>)(x => x.Timestamp));
                            timeSpan = startDate - stopDate;
                            total = timeSpan.TotalMilliseconds;
                        }
                        dateTime = startDate;
                        DateTime? nullable2 = nullable1;
                        timeSpan = (nullable2.HasValue ? new TimeSpan?(dateTime - nullable2.GetValueOrDefault()) : new TimeSpan?()).Value;
                        double num1 = total / (timeSpan.TotalMilliseconds / (double)(i + 1));
                        ++i;
                        int num2 = (int)((double)(i * 100) / num1);
                        if (num2 > percent)
                            percent = num2;
                        viewModelBase.Message = "Don't close the chart. QA Adapter is downloading data...";
                        viewModelBase.SubMessage = $"Download date - {nullable1} ({percent} %)";
                        nullable2 = nullable1;
                        dateTime = stopDate;
                        if ((nullable2.HasValue ? (nullable2.GetValueOrDefault() < dateTime ? 1 : 0) : 0) == 0)
                            aggTradesResults = (List<CompressedAggregateTradeResponse>)null;
                        else
                            goto label_15;
                    }
                    else
                        break;
                }
                return;
            label_15:;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //    public async Task SubscribeToTicks(
        //string nativeSymbolName,
        //MarketType marketType,
        //string symbol,
        //ConcurrentDictionary<string, L1Subscription> l1Subscriptions,
        //WebSocketConnectionFunc webSocketConnectionFunc)
        //    {
        //        Guard.AgainstNullOrEmpty(symbol);
        //        Guard.AgainstNull(webSocketConnectionFunc);

        //        ClientWebSocket ws = null;
        //        var cts = new CancellationTokenSource();

        //        try
        //        {
        //            // Connect to WebSocket
        //            string wsUrl = $"wss://ws.kite.trade?api_key={apiKey}&access_token={accessToken}";
        //            ws = new ClientWebSocket();
        //            await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

        //            // Subscribe in full mode
        //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Subscribing to token  in full mode", NinjaTrader.Cbi.LogLevel.Information);
        //            int tokenInt = (int)(await GetInstrumentToken(symbol));
        //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Subscribing to token {tokenInt} in full mode", NinjaTrader.Cbi.LogLevel.Information);

        //            // Make sure to subscribe in full mode to get market depth
        //            //string subscribeMsg = $@"{{""a"":""subscribe"",""v"":[{tokenInt}]}}";
        //            //await ws.SendAsync(
        //            //    new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscribeMsg)),
        //            //    WebSocketMessageType.Text, true, CancellationToken.None);
        //            // Set the mode to full
        //            string modeMsg = $@"{{""a"":""mode"",""v"":[""full"",[{tokenInt}]]}}";
        //            await ws.SendAsync(
        //                new ArraySegment<byte>(Encoding.UTF8.GetBytes(modeMsg)),
        //                WebSocketMessageType.Text, true, CancellationToken.None);

        //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Subscribed to token {tokenInt} in full mode", NinjaTrader.Cbi.LogLevel.Information);

        //            // Fire-and-forget task to detect chart-close
        //            _ = Task.Run(async () =>
        //            {
        //                while (!cts.IsCancellationRequested)
        //                {
        //                    if (webSocketConnectionFunc.ExitFunction())
        //                    {
        //                        cts.Cancel();
        //                        break;
        //                    }
        //                    await Task.Delay(500, cts.Token);
        //                }
        //            });

        //            // Read and process WebSocket messages
        //            var buffer = new byte[4096];
        //            while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
        //            {
        //                // Coalesce WebSocket frames into a single MemoryStream
        //                using var ms = new MemoryStream();
        //                WebSocketReceiveResult frame;
        //                do
        //                {
        //                    frame = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        //                    if (frame.MessageType == WebSocketMessageType.Close)
        //                        throw new OperationCanceledException("WebSocket closed by server.");
        //                    if (frame.MessageType == WebSocketMessageType.Text)
        //                        continue; // Ignore JSON heartbeats/postbacks
        //                    ms.Write(buffer, 0, frame.Count);
        //                }
        //                while (!frame.EndOfMessage);

        //                // Parse the assembled binary payload
        //                ms.Position = 0;
        //                using var reader = new BinaryReader(ms);

        //                // Read number of packets
        //                if (ms.Length < 2) continue;
        //                short packetCount = ReadBEInt16(reader);

        //                for (int p = 0; p < packetCount; p++)
        //                {
        //                    // Read packet length
        //                    if (ms.Position + 2 > ms.Length) break;
        //                    short pktLen = ReadBEInt16(reader);

        //                    // Skip packets with unexpected length
        //                    if (pktLen != 8 && pktLen != 44 && pktLen != 184)
        //                    {
        //                        NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Skipping packet with unexpected length {pktLen}", NinjaTrader.Cbi.LogLevel.Warning);
        //                        reader.ReadBytes(pktLen); // Skip this packet
        //                        continue;
        //                    }

        //                    byte[] pkt = reader.ReadBytes(pktLen);
        //                    if (pkt.Length != pktLen) continue;

        //                    // Parse instrument token first
        //                    int iToken = ReadInt32BE(pkt, 0);
        //                    if (iToken != tokenInt) continue; // Skip if not our subscribed token

        //                    int lastTradedPrice = ReadInt32BE(pkt, 4);
        //                    double ltp = lastTradedPrice / 100.0;

        //                    // Get current time in India Standard Time
        //                    DateTime now;
        //                    try
        //                    {
        //                        var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        //                        now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
        //                    }
        //                    catch
        //                    {
        //                        now = DateTime.Now;
        //                    }

        //                    // Process based on packet length
        //                    if (pktLen == 8)
        //                    {
        //                        // LTP mode - minimal data
        //                        var systemTime = now;
        //                        NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK LTP] {nativeSymbolName} LTP={ltp:F2} | System: {systemTime:HH:mm:ss.fff}",
        //                            NinjaTrader.Cbi.LogLevel.Information);

        //                        if (l1Subscriptions.TryGetValue(nativeSymbolName, out var sub) && !double.IsNaN(ltp))
        //                        {
        //                            ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);
        //                            foreach (var cb in sub.L1Callbacks.Values)
        //                            {
        //                                cb(MarketDataType.Last, ltp, 1, now, 0L);
        //                            }
        //                        }
        //                    }
        //                    else if (pktLen == 44)
        //                    {
        //                        // QUOTE mode
        //                        int lastTradedQty = ReadInt32BE(pkt, 8);
        //                        int avgTradedPrice = ReadInt32BE(pkt, 12);
        //                        int volume = ReadInt32BE(pkt, 16);
        //                        int buyQty = ReadInt32BE(pkt, 20);
        //                        int sellQty = ReadInt32BE(pkt, 24);
        //                        int open = ReadInt32BE(pkt, 28);
        //                        int high = ReadInt32BE(pkt, 32);
        //                        int low = ReadInt32BE(pkt, 36);
        //                        int close = ReadInt32BE(pkt, 40);

        //                        // Calculate volume delta
        //                        int prevVolume = _previousVolumes.GetOrAdd(nativeSymbolName, 0);
        //                        int volumeDelta = Math.Max(0, volume - prevVolume);

        //                        // Update previous volume for next tick
        //                        _previousVolumes[nativeSymbolName] = volume;

        //                        // Log tick information
        //                        var systemTime = now;
        //                        // Try to extract tick time from offset 28 (might be exchange timestamp)
        //                        int tickTimeRaw = ReadInt32BE(pkt, 28);

        //                        if (tickTimeRaw > 0)
        //                        {
        //                            var tickTime = UnixSecondsToLocalTime(tickTimeRaw);
        //                            var deltaMs = (systemTime - tickTime).TotalMilliseconds;

        //                            NinjaTrader.NinjaScript.NinjaScript.Log(
        //                                $"[TICK QUOTE] {nativeSymbolName} LTP={ltp:F2}, Qty={lastTradedQty}, Avg={avgTradedPrice / 100.0:F2}, " +
        //                                $"Vol={volume}, BuyQty={buyQty}, SellQty={sellQty}, O={open / 100.0:F2}, H={high / 100.0:F2}, " +
        //                                $"L={low / 100.0:F2}, C={close / 100.0:F2} | System: {systemTime:HH:mm:ss.fff} | " +
        //                                $"Tick: {tickTime:HH:mm:ss.fff} | Diff: {deltaMs:F3} ms",
        //                                NinjaTrader.Cbi.LogLevel.Information);
        //                        }
        //                        else
        //                        {
        //                            NinjaTrader.NinjaScript.NinjaScript.Log(
        //                                $"[TICK QUOTE] {nativeSymbolName} LTP={ltp:F2}, Qty={lastTradedQty}, Avg={avgTradedPrice / 100.0:F2}, " +
        //                                $"Vol={volume}, BuyQty={buyQty}, SellQty={sellQty}, O={open / 100.0:F2}, H={high / 100.0:F2}, " +
        //                                $"L={low / 100.0:F2}, C={close / 100.0:F2} | System: {systemTime:HH:mm:ss.fff}",
        //                                NinjaTrader.Cbi.LogLevel.Information);
        //                        }

        //                        // Update NinjaTrader
        //                        if (l1Subscriptions.TryGetValue(nativeSymbolName, out var sub) && !double.IsNaN(ltp))
        //                        {
        //                            ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);

        //                            foreach (var cb in sub.L1Callbacks.Values)
        //                            {
        //                                // Update Last Price with volume delta or last traded quantity
        //                                cb(MarketDataType.Last, ltp, volumeDelta > 0 ? volumeDelta : lastTradedQty, now, 0L);

        //                                // Update other Level 1 data types
        //                                if (open > 0) cb(MarketDataType.Opening, open / 100.0, 0, now, 0L);
        //                                if (high > 0) cb(MarketDataType.DailyHigh, high / 100.0, 0, now, 0L);
        //                                if (low > 0) cb(MarketDataType.DailyLow, low / 100.0, 0, now, 0L);
        //                                if (close > 0) cb(MarketDataType.LastClose, close / 100.0, 0, now, 0L);
        //                            }
        //                        }
        //                    }
        //                    else if (pktLen == 184)
        //                    {
        //                        // FULL mode - includes market depth
        //                        int lastTradedQty = ReadInt32BE(pkt, 8);
        //                        int avgTradedPrice = ReadInt32BE(pkt, 12);
        //                        int volume = ReadInt32BE(pkt, 16);
        //                        int buyQty = ReadInt32BE(pkt, 20);
        //                        int sellQty = ReadInt32BE(pkt, 24);
        //                        int open = ReadInt32BE(pkt, 28);
        //                        int high = ReadInt32BE(pkt, 32);
        //                        int low = ReadInt32BE(pkt, 36);
        //                        int close = ReadInt32BE(pkt, 40);
        //                        int lastTradedTimestamp = ReadInt32BE(pkt, 44);
        //                        int oi = ReadInt32BE(pkt, 48);
        //                        int oiDayHigh = ReadInt32BE(pkt, 52);
        //                        int oiDayLow = ReadInt32BE(pkt, 56);
        //                        int exchangeTimestamp = ReadInt32BE(pkt, 60);

        //                        // Calculate volume delta
        //                        int prevVolume = _previousVolumes.GetOrAdd(nativeSymbolName, 0);
        //                        int volumeDelta = Math.Max(0, volume - prevVolume);

        //                        // Update previous volume for next tick
        //                        _previousVolumes[nativeSymbolName] = volume;

        //                        // Market depth parsing (5 bids + 5 asks)
        //                        double[] bidPrices = new double[5];
        //                        int[] bidVolumes = new int[5];
        //                        int[] bidOrders = new int[5];
        //                        double[] askPrices = new double[5];
        //                        int[] askVolumes = new int[5];
        //                        int[] askOrders = new int[5];

        //                        string bidsLog = "";
        //                        string asksLog = "";

        //                        // Parse bids (buy orders)
        //                        for (int i = 0; i < 5; i++)
        //                        {
        //                            int baseOffset = 64 + i * 12;
        //                            int qty = ReadInt32BE(pkt, baseOffset);
        //                            int price = ReadInt32BE(pkt, baseOffset + 4);
        //                            short orders = ReadInt16BE(pkt, baseOffset + 8);

        //                            // Store for callbacks
        //                            bidVolumes[i] = qty;
        //                            bidPrices[i] = price / 100.0;
        //                            bidOrders[i] = orders;

        //                            // Format for logging
        //                            bidsLog += $"[{qty}@{price / 100.0:F2} ({orders})] ";
        //                        }

        //                        // Parse asks (sell orders)
        //                        for (int i = 0; i < 5; i++)
        //                        {
        //                            int baseOffset = 124 + i * 12;
        //                            int qty = ReadInt32BE(pkt, baseOffset);
        //                            int price = ReadInt32BE(pkt, baseOffset + 4);
        //                            short orders = ReadInt16BE(pkt, baseOffset + 8);

        //                            // Store for callbacks
        //                            askVolumes[i] = qty;
        //                            askPrices[i] = price / 100.0;
        //                            askOrders[i] = orders;

        //                            // Format for logging
        //                            asksLog += $"[{qty}@{price / 100.0:F2} ({orders})] ";
        //                        }

        //                        // Log the tick data
        //                        var systemTime = now;
        //                        var tickTime = UnixSecondsToLocalTime(exchangeTimestamp);
        //                        var deltaMs = (systemTime - tickTime).TotalMilliseconds;

        //                        //NinjaTrader.NinjaScript.NinjaScript.Log(
        //                        //    $"[TICK FULL] {nativeSymbolName} LTP={ltp:F2}, Qty={lastTradedQty}, Vol={volume}, OI={oi} | " +
        //                        //    $"System: {systemTime:HH:mm:ss.fff} | Tick: {exchangeTimestamp:HH:mm:ss.fff} | Diff: {deltaMs:F3} ms",
        //                        //    NinjaTrader.Cbi.LogLevel.Information);

        //                        //// Log market depth separately to avoid too long lines
        //                        //NinjaTrader.NinjaScript.NinjaScript.Log(
        //                        //    $"[MARKET DEPTH] BIDS: {bidsLog} | ASKS: {asksLog}",
        //                        //    NinjaTrader.Cbi.LogLevel.Information);

        //                        // Update NinjaTrader
        //                        if (l1Subscriptions.TryGetValue(nativeSymbolName, out var sub) && !double.IsNaN(ltp))
        //                        {
        //                            ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);

        //                            foreach (var cb in sub.L1Callbacks.Values)
        //                            {
        //                                // Update Last Price with volume delta or last traded quantity
        //                                cb(MarketDataType.Last, ltp, volumeDelta > 0 ? volumeDelta : lastTradedQty, now, 0L);

        //                                // Update other Level 1 data types
        //                                if (open > 0) cb(MarketDataType.Opening, open / 100.0, 0, now, 0L);
        //                                if (high > 0) cb(MarketDataType.DailyHigh, high / 100.0, 0, now, 0L);
        //                                if (low > 0) cb(MarketDataType.DailyLow, low / 100.0, 0, now, 0L);
        //                                if (close > 0) cb(MarketDataType.LastClose, close / 100.0, 0, now, 0L);

        //                                // Update best bid/ask prices from market depth
        //                                if (bidPrices[0] > 0 && bidVolumes[0] > 0)
        //                                    cb(MarketDataType.Bid, bidPrices[0], bidVolumes[0], now, 0L);

        //                                if (askPrices[0] > 0 && askVolumes[0] > 0)
        //                                    cb(MarketDataType.Ask, askPrices[0], askVolumes[0], now, 0L);

        //                                // Add more market depth levels if your platform supports it
        //                                // For example:
        //                                // if (bidPrices[1] > 0) cb(MarketDataType.Bid2, bidPrices[1], bidVolumes[1], now, 0L);
        //                                // if (askPrices[1] > 0) cb(MarketDataType.Ask2, askPrices[1], askVolumes[1], now, 0L);
        //                            }
        //                        }
        //                    }

        //                    // Log volumes for debug purposes, but only for the first packet to avoid spamming
        //                    if (p == 0 && pktLen > 8) // Skip for LTP mode which doesn't have volume
        //                    {
        //                        int ltqRaw = pktLen >= 12 ? ReadInt32BE(pkt, 8) : 0;
        //                        int volRaw = pktLen >= 20 ? ReadInt32BE(pkt, 16) : 0;

        //                        NinjaTrader.NinjaScript.NinjaScript.Log(
        //                            $"[TICK-VOLUME] {nativeSymbolName} System Time: {now.ToLocalTime():HH:mm:ss.fff} " +
        //                            $"LTQ: {ltqRaw}, Vol: {volRaw}",
        //                            NinjaTrader.Cbi.LogLevel.Information);
        //                    }
        //                }
        //            }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            // Normal teardown
        //            //NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Error: {OperationCanceledException}", NinjaTrader.Cbi.LogLevel.Error);
        //        }
        //        catch (Exception ex)
        //        {
        //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Error: {ex.Message} {ex.StackTrace}", NinjaTrader.Cbi.LogLevel.Error);
        //        }
        //        finally
        //        {
        //            // Unsubscribe and cleanup
        //            if (ws != null && ws.State == WebSocketState.Open)
        //            {
        //                try
        //                {
        //                    string unsub = $@"{{""a"":""unsubscribe"",""v"":[{(int)(await GetInstrumentToken(symbol))}]}}";
        //                    await ws.SendAsync(
        //                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(unsub)),
        //                        WebSocketMessageType.Text, true, CancellationToken.None);
        //                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Chart closed", CancellationToken.None);
        //                }
        //                catch { }
        //                ws.Dispose();
        //            }
        //            cts.Cancel();
        //            cts.Dispose();
        //        }
        //    }

        // Add these helper methods if not already available
        //    public async Task SubscribeToTicks(
        //string nativeSymbolName,
        //MarketType marketType,
        //string symbol,
        //ConcurrentDictionary<string, L1Subscription> l1Subscriptions,
        //WebSocketConnectionFunc webSocketConnectionFunc)
        //    {
        //        Guard.AgainstNullOrEmpty(symbol);
        //        Guard.AgainstNull(webSocketConnectionFunc);

        //        ClientWebSocket ws = null;
        //        var cts = new CancellationTokenSource();

        //        try
        //        {
        //            // Connect to WebSocket
        //            string wsUrl = $"wss://ws.kite.trade?api_key={apiKey}&access_token={accessToken}";
        //            ws = new ClientWebSocket();
        //            await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);


        //            int tokenInt = (int)(await GetInstrumentToken(symbol));

        //            // Make sure to subscribe in full mode to get market depth
        //            //string subscribeMsg = $@"{{""a"":""subscribe"",""v"":[{tokenInt}]}}";
        //            //await ws.SendAsync(
        //            //    new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscribeMsg)),
        //            //    WebSocketMessageType.Text, true, CancellationToken.None);
        //            // Set the mode to full
        //            string modeMsg = $@"{{""a"":""mode"",""v"":[""quote"",[{tokenInt}]]}}";
        //            await ws.SendAsync(
        //                new ArraySegment<byte>(Encoding.UTF8.GetBytes(modeMsg)),
        //                WebSocketMessageType.Text, true, CancellationToken.None);

        //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Subscribed to token {tokenInt} in full mode", NinjaTrader.Cbi.LogLevel.Information);

        //            // Fire-and-forget task to detect chart-close
        //            _ = Task.Run(async () =>
        //            {
        //                while (!cts.IsCancellationRequested)
        //                {
        //                    if (webSocketConnectionFunc.ExitFunction())
        //                    {
        //                        cts.Cancel();
        //                        break;
        //                    }
        //                    await Task.Delay(500, cts.Token);
        //                }
        //            });

        //            // Read and process WebSocket messages
        //            var buffer = new byte[4096];
        //            while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
        //            {
        //                // Coalesce WebSocket frames into a single MemoryStream
        //                using var ms = new MemoryStream();
        //                WebSocketReceiveResult frame;
        //                do
        //                {
        //                    frame = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        //                    if (frame.MessageType == WebSocketMessageType.Close)
        //                        throw new OperationCanceledException("WebSocket closed by server.");
        //                    if (frame.MessageType == WebSocketMessageType.Text)
        //                        continue; // Ignore JSON heartbeats/postbacks
        //                    ms.Write(buffer, 0, frame.Count);
        //                }
        //                while (!frame.EndOfMessage);

        //                // Parse the assembled binary payload
        //                ms.Position = 0;
        //                using var reader = new BinaryReader(ms);

        //                // Read number of packets
        //                if (ms.Length < 2) continue;
        //                short packetCount = ReadBEInt16(reader);
        //                var receivedTime = DateTime.Now;
        //                for (int p = 0; p < packetCount; p++)
        //                {
        //                    // Read packet length
        //                    if (ms.Position + 2 > ms.Length) break;
        //                    short pktLen = ReadBEInt16(reader);

        //                    // Skip packets with unexpected length
        //                    if (pktLen != 8 && pktLen != 44 && pktLen != 184)
        //                    {
        //                        NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Skipping packet with unexpected length {pktLen}", NinjaTrader.Cbi.LogLevel.Warning);
        //                        reader.ReadBytes(pktLen); // Skip this packet
        //                        continue;
        //                    }

        //                    byte[] pkt = reader.ReadBytes(pktLen);
        //                    if (pkt.Length != pktLen) continue;

        //                    // Parse instrument token first
        //                    int iToken = ReadInt32BE(pkt, 0);
        //                    if (iToken != tokenInt) continue; // Skip if not our subscribed token

        //                    int lastTradedPrice = ReadInt32BE(pkt, 4);
        //                    double ltp = lastTradedPrice / 100.0;

        //                    // Get current time in India Standard Time
        //                    DateTime now;
        //                    try
        //                    {
        //                        var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        //                        now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
        //                    }
        //                    catch
        //                    {
        //                        now = DateTime.Now;
        //                    }

        //                    // Process based on packet length
        //                    if (pktLen == 8)
        //                    {
        //                        // LTP mode - minimal data
        //                        var systemTime = now;
        //                        NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK LTP] {nativeSymbolName} LTP={ltp:F2} | System: {systemTime:HH:mm:ss.fff}",
        //                            NinjaTrader.Cbi.LogLevel.Information);

        //                        if (l1Subscriptions.TryGetValue(nativeSymbolName, out var sub) && !double.IsNaN(ltp))
        //                        {
        //                            ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);
        //                            foreach (var cb in sub.L1Callbacks.Values)
        //                            {
        //                                cb(MarketDataType.Last, ltp, 1, now, 0L);
        //                            }
        //                        }
        //                    }
        //                    if (pktLen >= 44)
        //                    {
        //                        int lastTradedQty = ReadInt32BE(pkt, 8);
        //                        int volume = ReadInt32BE(pkt, 16);

        //                        // Get exchange timestamp based on packet format
        //                        // For indices, the timestamp is at offset 28
        //                        // For normal instruments in full mode, it's at offset 60
        //                        int exchangeTimestampOffset = IsIndexPacket(iToken) ? 28 : (pktLen >= 64 ? 60 : 44);
        //                        int exchangeTimestamp = 0;

        //                        if (pktLen >= exchangeTimestampOffset + 4)
        //                        {
        //                            exchangeTimestamp = ReadInt32BE(pkt, exchangeTimestampOffset);
        //                        }

        //                        DateTime tickTime = exchangeTimestamp > 0
        //                            ? UnixSecondsToLocalTime(exchangeTimestamp)
        //                            : receivedTime;
        //                        // QUOTE mode
        //                        //int lastTradedQty = ReadInt32BE(pkt, 8);
        //                        //int avgTradedPrice = ReadInt32BE(pkt, 12);
        //                        //int volume = ReadInt32BE(pkt, 16);
        //                        //int buyQty = ReadInt32BE(pkt, 20);
        //                        //int sellQty = ReadInt32BE(pkt, 24);
        //                        //int open = ReadInt32BE(pkt, 28);
        //                        //int high = ReadInt32BE(pkt, 32);
        //                        //int low = ReadInt32BE(pkt, 36);
        //                        //int close = ReadInt32BE(pkt, 40);

        //                        // Calculate volume delta
        //                        int prevVolume = _previousVolumes.GetOrAdd(nativeSymbolName, 0);
        //                        int volumeDelta = Math.Max(0, volume - prevVolume);

        //                        // Update previous volume for next tick
        //                        _previousVolumes[nativeSymbolName] = volume;

        //                        // Log tick information
        //                        var systemTime = now;
        //                        // Try to extract tick time from offset 28 (might be exchange timestamp)
        //                        //int tickTimeRaw = ReadInt32BE(pkt, 28);

        //                        //if (tickTimeRaw > 0)
        //                        //{
        //                            var deltaMs = (systemTime - tickTime).TotalMilliseconds;

        //                            NinjaTrader.NinjaScript.NinjaScript.Log(
        //                                $"[TICK] {symbol} LTP={lastTradedPrice:F2}, Qty={lastTradedQty} | Sys: {receivedTime:HH:mm:ss.fff} | Exch: {tickTime:HH:mm:ss.fff} | Latency: {deltaMs:F1}ms",
        //                                NinjaTrader.Cbi.LogLevel.Information);
        //                        //}
        //                        //else
        //                        //{
        //                        //    NinjaTrader.NinjaScript.NinjaScript.Log(
        //                        //        $"[TICK QUOTE] {nativeSymbolName} LTP={ltp:F2}, Qty={lastTradedQty}, " +
        //                        //        $"Vol={volume} " +
        //                        //        "System: {systemTime:HH:mm:ss.fff}",
        //                        //        NinjaTrader.Cbi.LogLevel.Information);
        //                        //}

        //                        // Update NinjaTrader
        //                        if (l1Subscriptions.TryGetValue(nativeSymbolName, out var sub) && !double.IsNaN(ltp))
        //                        {
        //                            ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);

        //                            foreach (var cb in sub.L1Callbacks.Values)
        //                            {
        //                                // Update Last Price with volume delta or last traded quantity
        //                                cb(MarketDataType.Last, ltp, volumeDelta > 0 ? volumeDelta : lastTradedQty, now, 0L);

        //                                // Update other Level 1 data types
        //                                //if (open > 0) cb(MarketDataType.Opening, open / 100.0, 0, now, 0L);
        //                                //if (high > 0) cb(MarketDataType.DailyHigh, high / 100.0, 0, now, 0L);
        //                                //if (low > 0) cb(MarketDataType.DailyLow, low / 100.0, 0, now, 0L);
        //                                //if (close > 0) cb(MarketDataType.LastClose, close / 100.0, 0, now, 0L);
        //                            }
        //                        }
        //                    }
        //                    //else if (pktLen == 184)
        //                    //{
        //                    //    // FULL mode - includes market depth
        //                    //    int lastTradedQty = ReadInt32BE(pkt, 8);
        //                    //    int avgTradedPrice = ReadInt32BE(pkt, 12);
        //                    //    int volume = ReadInt32BE(pkt, 16);
        //                    //    int buyQty = ReadInt32BE(pkt, 20);
        //                    //    int sellQty = ReadInt32BE(pkt, 24);
        //                    //    int open = ReadInt32BE(pkt, 28);
        //                    //    int high = ReadInt32BE(pkt, 32);
        //                    //    int low = ReadInt32BE(pkt, 36);
        //                    //    int close = ReadInt32BE(pkt, 40);
        //                    //    int lastTradedTimestamp = ReadInt32BE(pkt, 44);
        //                    //    int oi = ReadInt32BE(pkt, 48);
        //                    //    int oiDayHigh = ReadInt32BE(pkt, 52);
        //                    //    int oiDayLow = ReadInt32BE(pkt, 56);
        //                    //    int exchangeTimestamp = ReadInt32BE(pkt, 60);

        //                    //    // Calculate volume delta
        //                    //    int prevVolume = _previousVolumes.GetOrAdd(nativeSymbolName, 0);
        //                    //    int volumeDelta = Math.Max(0, volume - prevVolume);

        //                    //    // Update previous volume for next tick
        //                    //    _previousVolumes[nativeSymbolName] = volume;

        //                    //    // Market depth parsing (5 bids + 5 asks)
        //                    //    double[] bidPrices = new double[5];
        //                    //    int[] bidVolumes = new int[5];
        //                    //    int[] bidOrders = new int[5];
        //                    //    double[] askPrices = new double[5];
        //                    //    int[] askVolumes = new int[5];
        //                    //    int[] askOrders = new int[5];

        //                    //    string bidsLog = "";
        //                    //    string asksLog = "";

        //                    //    // Parse bids (buy orders)
        //                    //    for (int i = 0; i < 5; i++)
        //                    //    {
        //                    //        int baseOffset = 64 + i * 12;
        //                    //        int qty = ReadInt32BE(pkt, baseOffset);
        //                    //        int price = ReadInt32BE(pkt, baseOffset + 4);
        //                    //        short orders = ReadInt16BE(pkt, baseOffset + 8);

        //                    //        // Store for callbacks
        //                    //        bidVolumes[i] = qty;
        //                    //        bidPrices[i] = price / 100.0;
        //                    //        bidOrders[i] = orders;

        //                    //        // Format for logging
        //                    //        bidsLog += $"[{qty}@{price / 100.0:F2} ({orders})] ";
        //                    //    }

        //                    //    // Parse asks (sell orders)
        //                    //    for (int i = 0; i < 5; i++)
        //                    //    {
        //                    //        int baseOffset = 124 + i * 12;
        //                    //        int qty = ReadInt32BE(pkt, baseOffset);
        //                    //        int price = ReadInt32BE(pkt, baseOffset + 4);
        //                    //        short orders = ReadInt16BE(pkt, baseOffset + 8);

        //                    //        // Store for callbacks
        //                    //        askVolumes[i] = qty;
        //                    //        askPrices[i] = price / 100.0;
        //                    //        askOrders[i] = orders;

        //                    //        // Format for logging
        //                    //        asksLog += $"[{qty}@{price / 100.0:F2} ({orders})] ";
        //                    //    }

        //                    //    // Log the tick data
        //                    //    var systemTime = now;
        //                    //    var tickTime = UnixSecondsToLocalTime(exchangeTimestamp);
        //                    //    var deltaMs = (systemTime - tickTime).TotalMilliseconds;

        //                    //    //NinjaTrader.NinjaScript.NinjaScript.Log(
        //                    //    //    $"[TICK FULL] {nativeSymbolName} LTP={ltp:F2}, Qty={lastTradedQty}, Vol={volume}, OI={oi} | " +
        //                    //    //    $"System: {systemTime:HH:mm:ss.fff} | Tick: {exchangeTimestamp:HH:mm:ss.fff} | Diff: {deltaMs:F3} ms",
        //                    //    //    NinjaTrader.Cbi.LogLevel.Information);

        //                    //    //// Log market depth separately to avoid too long lines
        //                    //    //NinjaTrader.NinjaScript.NinjaScript.Log(
        //                    //    //    $"[MARKET DEPTH] BIDS: {bidsLog} | ASKS: {asksLog}",
        //                    //    //    NinjaTrader.Cbi.LogLevel.Information);

        //                    //    // Update NinjaTrader
        //                    //    if (l1Subscriptions.TryGetValue(nativeSymbolName, out var sub) && !double.IsNaN(ltp))
        //                    //    {
        //                    //        ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);

        //                    //        foreach (var cb in sub.L1Callbacks.Values)
        //                    //        {
        //                    //            // Update Last Price with volume delta or last traded quantity
        //                    //            cb(MarketDataType.Last, ltp, volumeDelta > 0 ? volumeDelta : lastTradedQty, now, 0L);

        //                    //            // Update other Level 1 data types
        //                    //            if (open > 0) cb(MarketDataType.Opening, open / 100.0, 0, now, 0L);
        //                    //            if (high > 0) cb(MarketDataType.DailyHigh, high / 100.0, 0, now, 0L);
        //                    //            if (low > 0) cb(MarketDataType.DailyLow, low / 100.0, 0, now, 0L);
        //                    //            if (close > 0) cb(MarketDataType.LastClose, close / 100.0, 0, now, 0L);

        //                    //            // Update best bid/ask prices from market depth
        //                    //            if (bidPrices[0] > 0 && bidVolumes[0] > 0)
        //                    //                cb(MarketDataType.Bid, bidPrices[0], bidVolumes[0], now, 0L);

        //                    //            if (askPrices[0] > 0 && askVolumes[0] > 0)
        //                    //                cb(MarketDataType.Ask, askPrices[0], askVolumes[0], now, 0L);

        //                    //            // Add more market depth levels if your platform supports it
        //                    //            // For example:
        //                    //            // if (bidPrices[1] > 0) cb(MarketDataType.Bid2, bidPrices[1], bidVolumes[1], now, 0L);
        //                    //            // if (askPrices[1] > 0) cb(MarketDataType.Ask2, askPrices[1], askVolumes[1], now, 0L);
        //                    //        }
        //                    //    }
        //                    //}

        //                    // Log volumes for debug purposes, but only for the first packet to avoid spamming
        //                    if (p == 0 && pktLen > 8) // Skip for LTP mode which doesn't have volume
        //                    {
        //                        int ltqRaw = pktLen >= 12 ? ReadInt32BE(pkt, 8) : 0;
        //                        int volRaw = pktLen >= 20 ? ReadInt32BE(pkt, 16) : 0;

        //                        NinjaTrader.NinjaScript.NinjaScript.Log(
        //                            $"[TICK-VOLUME] {nativeSymbolName} System Time: {now.ToLocalTime():HH:mm:ss.fff} " +
        //                            $"LTQ: {ltqRaw}, Vol: {volRaw}",
        //                            NinjaTrader.Cbi.LogLevel.Information);
        //                    }
        //                }
        //            }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            // Normal teardown
        //            //NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Error: {OperationCanceledException}", NinjaTrader.Cbi.LogLevel.Error);
        //        }
        //        catch (Exception ex)
        //        {
        //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Error: {ex.Message} {ex.StackTrace}", NinjaTrader.Cbi.LogLevel.Error);
        //        }
        //        finally
        //        {
        //            // Unsubscribe and cleanup
        //            if (ws != null && ws.State == WebSocketState.Open)
        //            {
        //                try
        //                {
        //                    string unsub = $@"{{""a"":""unsubscribe"",""v"":[{(int)(await GetInstrumentToken(symbol))}]}}";
        //                    await ws.SendAsync(
        //                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(unsub)),
        //                        WebSocketMessageType.Text, true, CancellationToken.None);
        //                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Chart closed", CancellationToken.None);
        //                }
        //                catch { }
        //                ws.Dispose();
        //            }
        //            cts.Cancel();
        //            cts.Dispose();
        //        }
        //    }
        public async Task SubscribeToTicks(
    string nativeSymbolName,
    MarketType marketType,
    string symbol,
    ConcurrentDictionary<string, L1Subscription> l1Subscriptions,
    WebSocketConnectionFunc webSocketConnectionFunc)
        {
            Guard.AgainstNullOrEmpty(symbol);
            Guard.AgainstNull(webSocketConnectionFunc);

            ClientWebSocket ws = null;
            var cts = new CancellationTokenSource();

            // Preallocate buffers to reduce GC pressure
            byte[] buffer = new byte[16384]; // Larger buffer for better network efficiency

            // Cache frequently used objects
            TimeZoneInfo indianTimeZone = null;
            try
            {
                indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch { }

            // Pre-calculate constants
            int tokenInt = 0;

            try
            {
                // Connect to WebSocket
                string wsUrl = $"wss://ws.kite.trade?api_key={apiKey}&access_token={accessToken}";
                ws = new ClientWebSocket();

                // Set WebSocket options for performance
                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                ws.Options.SetBuffer(16384, 16384); // Increase buffer sizes

                await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

                tokenInt = (int)(await GetInstrumentToken(symbol));

                // Set the mode to quote (prepare message bytes once)
                string modeMsg = $@"{{""a"":""mode"",""v"":[""quote"",[{tokenInt}]]}}";
                byte[] modeMsgBytes = Encoding.UTF8.GetBytes(modeMsg);
                await ws.SendAsync(
                    new ArraySegment<byte>(modeMsgBytes),
                    WebSocketMessageType.Text, true, CancellationToken.None);

                // Start monitoring chart-close in separate task
                StartExitMonitoringTask(webSocketConnectionFunc, cts);

                // Logging outside the critical path
                NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Subscribed to token {tokenInt} in quote mode", NinjaTrader.Cbi.LogLevel.Information);

                // Main message processing loop
                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                        throw new OperationCanceledException("WebSocket closed by server.");

                    if (result.MessageType == WebSocketMessageType.Text)
                        continue; // Ignore JSON heartbeats/postbacks

                    // Skip processing if no valid data
                    if (result.Count < 2) continue;

                    // Get entire message even if split across frames
                    int totalBytes = result.Count;
                    //while (!result.EndOfMessage && !cts.Token.IsCancellationRequested)
                    //{
                    //    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes), cts.Token);
                    //    if (result.MessageType == WebSocketMessageType.Binary)
                    //    {
                    //        totalBytes += result.Count;
                    //        if (totalBytes >= buffer.Length) break; // Prevent buffer overflow
                    //    }
                    //}

                    // Timestamp message receipt immediately to reduce timing errors
                    var receivedTime = DateTime.Now;
                    DateTime now = indianTimeZone != null
                        ? TimeZoneInfo.ConvertTime(receivedTime, indianTimeZone)
                        : receivedTime;

                    // Use direct array access for initial parsing - faster than helper methods
                    int packetCount = (buffer[0] << 8) | buffer[1];

                    int offset = 2; // Start after the packet count
                    bool volumeLogged = false;


                    if (!l1Subscriptions.TryGetValue(nativeSymbolName, out var sub))
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log(
                            $"[TICK-SUBSCRIBE] No subscription found for {nativeSymbolName}",
                            NinjaTrader.Cbi.LogLevel.Warning);
                        continue; // Skip this message if no subscription exists
                    }

                    for (int p = 0; p < packetCount && offset + 2 <= totalBytes; p++)
                    {
                        // Direct array access for packet length - faster than helper method
                        short pktLen = (short)((buffer[offset] << 8) | buffer[offset + 1]);
                        offset += 2;

                        if (offset + pktLen > totalBytes) break;

                        // Skip packets with unexpected length
                        if (pktLen != 8 && pktLen != 44 && pktLen != 184)
                        {
                            offset += pktLen; // Skip this packet
                            continue;
                        }

                        // Read instrument token directly - faster than helper method for hot path
                        int iToken = (buffer[offset] << 24) |
                                     (buffer[offset + 1] << 16) |
                                     (buffer[offset + 2] << 8) |
                                      buffer[offset + 3];

                        // Skip non-matching tokens immediately for efficiency
                        if (iToken != tokenInt)
                        {
                            offset += pktLen;
                            continue;
                        }

                        // Read LTP directly for efficiency
                        int lastTradedPrice = (buffer[offset + 4] << 24) |
                                             (buffer[offset + 5] << 16) |
                                             (buffer[offset + 6] << 8) |
                                              buffer[offset + 7];
                        double ltp = lastTradedPrice / 100.0;

                        // Process based on packet length
                        if (pktLen == 8)
                        {
                            // LTP mode - minimal data, minimal processing
                            if (!double.IsNaN(ltp))
                            {
                                ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);
                                foreach (var cb in sub.L1Callbacks.Values)
                                {
                                    cb(MarketDataType.Last, ltp, 1, now, 0L);
                                }
                            }
                        }
                        else if (pktLen >= 44)
                        {
                            // Read fields directly with optimized code path
                            int lastTradedQty = (buffer[offset + 8] << 24) |
                                               (buffer[offset + 9] << 16) |
                                               (buffer[offset + 10] << 8) |
                                                buffer[offset + 11];

                            int volume = (buffer[offset + 16] << 24) |
                                        (buffer[offset + 17] << 16) |
                                        (buffer[offset + 18] << 8) |
                                         buffer[offset + 19];

                            // Get exchange timestamp with optimized offset calculation
                            int exchangeTimestampOffset = offset + (IsIndexPacket(iToken) ? 28 : (pktLen >= 64 ? 60 : 44));
                            int exchangeTimestamp = 0;

                            if (exchangeTimestampOffset + 4 <= offset + pktLen)
                            {
                                exchangeTimestamp = (buffer[exchangeTimestampOffset] << 24) |
                                                   (buffer[exchangeTimestampOffset + 1] << 16) |
                                                   (buffer[exchangeTimestampOffset + 2] << 8) |
                                                    buffer[exchangeTimestampOffset + 3];
                            }

                            DateTime tickTime = exchangeTimestamp > 0
                                ? UnixSecondsToLocalTime(exchangeTimestamp)
                                : receivedTime;

                            // Calculate volume delta with null-coalescing for efficiency
                            
                            int volumeDelta = Math.Max(0, sub.PreviousVolume == 0 ? 0 : volume - sub.PreviousVolume);
                            sub.PreviousVolume = volume;

                            // Update previous volume for next tick
                            
                            // Update NinjaTrader with price and volume
                            if (!double.IsNaN(ltp))
                            {
                                ltp = sub.Instrument.MasterInstrument.RoundToTickSize(ltp);
                                foreach (var cb in sub.L1Callbacks.Values)
                                {
                                    cb(MarketDataType.Last, ltp, volumeDelta , now, 0L);
                                }
                            }

                            // Log volume data only once per message batch to reduce overhead
                            if (!volumeLogged && p == 0)
                            {
                                NinjaTrader.NinjaScript.NinjaScript.Log(
                                    $"[TICK-VOLUME] {nativeSymbolName} LTP={ltp:F2}, LTQ={lastTradedQty}, Vol={volume}",
                                    NinjaTrader.Cbi.LogLevel.Information);
                                volumeLogged = true;
                            }
                        }

                        offset += pktLen;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal teardown - no logging needed
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Error: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
            }
            finally
            {
                // Clean up resources
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        // Prepare unsubscribe message once
                        string unsub = $@"{{""a"":""unsubscribe"",""v"":[{tokenInt}]}}";
                        byte[] unsubBytes = Encoding.UTF8.GetBytes(unsub);
                        await ws.SendAsync(
                            new ArraySegment<byte>(unsubBytes),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Chart closed", CancellationToken.None);
                    }
                    catch { }
                    ws.Dispose();
                }
                cts.Cancel();
                cts.Dispose();
            }
        }

        // Helper methods moved outside the hot path
        private void StartExitMonitoringTask(WebSocketConnectionFunc webSocketConnectionFunc, CancellationTokenSource cts)
        {
            // Fire-and-forget task to detect chart-close
            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (webSocketConnectionFunc.ExitFunction())
                    {
                        cts.Cancel();
                        break;
                    }
                    await Task.Delay(500, cts.Token);
                }
            });
        }

        // Pre-computed Unix epoch for faster timestamp conversion
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private DateTime UnixSecondsToLocalTime(int unixSeconds)
        {
            return UnixEpoch.AddSeconds(unixSeconds).ToLocalTime();
        }

        // Helper methods for byte array manipulation
        private static short ReadInt16BE(byte[] buffer, int offset)
        {
            return (short)((buffer[offset] << 8) | buffer[offset + 1]);
        }

        private static int ReadInt32BE(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) |
                   (buffer[offset + 1] << 16) |
                   (buffer[offset + 2] << 8) |
                   buffer[offset + 3];
        }





        private bool IsIndexPacket(int instrumentToken)
        {
            return instrumentToken >= 260000 && instrumentToken < 270000;
        }


        // Add these helper methods if not already available
        private static int ReadInt32BE(ReadOnlySpan<byte> buffer, int offset)
        {
            return BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, 4));
        }

        private static short ReadInt16BE(ReadOnlySpan<byte> buffer, int offset)
        {
            return BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(offset, 2));
        }

        private short ReadBEInt16(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(2);
            return (short)((bytes[0] << 8) | bytes[1]);
        }

        private int ReadBEInt32(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        }

        //private DateTime UnixSecondsToLocalTime(int unixTimestamp)
        //{
        //    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //    return epoch.AddSeconds(unixTimestamp).ToLocalTime();
        //}

        // --- Helpers & types ---
       


        /// <summary>Simple POCO to hold one depth entry.</summary>
        public class DepthEntry
        {
            public long Quantity { get; set; }
            public double Price { get; set; }
            public int Orders { get; set; }
        }







    //    public async Task SubscribeToTicks(
    //string nativeSymbolName,
    //MarketType marketType,
    //string symbol,
    //ConcurrentDictionary<string, L1Subscription> l1Subscriptions,
    //WebSocketConnectionFunc webSocketConnectionFunc)
    //    {
    //        Guard.AgainstNullOrEmpty(symbol);
    //        Guard.AgainstNull((object)webSocketConnectionFunc);

    //        NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Starting subscription for {symbol}, native: {nativeSymbolName}", NinjaTrader.Cbi.LogLevel.Information);

    //        ClientWebSocket ws = null;
    //        CancellationTokenSource cts = new CancellationTokenSource();

    //        try
    //        {
    //            // Connect to Zerodha WebSocket
    //            string wsUrl = $"wss://ws.kite.trade?api_key={apiKey}&access_token={accessToken}";
    //            ws = new ClientWebSocket();
    //            await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

    //            // Subscribe to instrument - use "mode":"ltp" to get only last price data
    //            long instrumentToken = await GetInstrumentToken(symbol);
    //            int instrumentToInt = (int)instrumentToken;
    //            string subscriptionMessage = $"{{\"a\": \"subscribe\", \"v\": [{instrumentToInt}], \"m\": \"ltp\"}}";
    //            await ws.SendAsync(
    //                new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscriptionMessage)),
    //                WebSocketMessageType.Text,
    //                true,
    //                CancellationToken.None);

    //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Subscribed to instrument token {instrumentToInt} in LTP mode", NinjaTrader.Cbi.LogLevel.Information);

    //            // Start a background task to check if we should close the connection
    //            Task connectionMonitorTask = Task.Run(async () =>
    //            {
    //                while (!cts.Token.IsCancellationRequested)
    //                {
    //                    // Check if we should disconnect (chart closed)
    //                    // Use ExitFunction() method instead of Invoke
    //                    if (webSocketConnectionFunc != null && webSocketConnectionFunc.ExitFunction())
    //                    {
    //                        NinjaTrader.NinjaScript.NinjaScript.Log(
    //                            $"[TICK-SUBSCRIBE] Chart closed detected for {symbol}, initiating WebSocket close",
    //                            NinjaTrader.Cbi.LogLevel.Information);

    //                        // Signal cancellation to stop the main loop
    //                        cts.Cancel();
    //                        break;
    //                    }

    //                    // Check every 1 second to avoid excessive CPU usage
    //                    await Task.Delay(1000);
    //                }
    //            });

    //            byte[] buffer = new byte[4096];

    //            // Main WebSocket receive loop - now using the cancellation token
    //            while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
    //            {
    //                WebSocketReceiveResult result;

    //                try
    //                {
    //                    // Use the cancellation token here so we can exit when requested
    //                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
    //                }
    //                catch (OperationCanceledException)
    //                {
    //                    // This is expected when cancellation is requested
    //                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                        $"[TICK-SUBSCRIBE] WebSocket receive cancelled for {symbol}",
    //                        NinjaTrader.Cbi.LogLevel.Information);
    //                    break;
    //                }
    //                catch (Exception ex)
    //                {
    //                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                        $"[TICK-SUBSCRIBE] WebSocket receive error: {ex.Message}",
    //                        NinjaTrader.Cbi.LogLevel.Error);
    //                    break;
    //                }

    //                // Handle text messages
    //                if (result.MessageType == WebSocketMessageType.Text)
    //                {
    //                    string textMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
    //                    NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-DATA] Received text message: {textMessage}", NinjaTrader.Cbi.LogLevel.Information);
    //                    continue;
    //                }

    //                // Skip if not binary or if connection is closing
    //                if (result.MessageType != WebSocketMessageType.Binary || result.CloseStatus.HasValue)
    //                {
    //                    continue;
    //                }

    //                // Process binary data (last traded price)
    //                try
    //                {
    //                    using (var ms = new MemoryStream(buffer, 0, result.Count))
    //                    using (var reader = new BinaryReader(ms))
    //                    {
    //                        //if (result.Count < 2)
    //                        //{
    //                        //    NinjaTrader.NinjaScript.NinjaScript.Log("[TICK-DATA] Message too small", NinjaTrader.Cbi.LogLevel.Warning);
    //                        //    continue;
    //                        //}

    //                        short numPackets = ReadBigEndianInt16(reader);

    //                        if (numPackets <= 0 || numPackets > 50)
    //                        {
    //                            NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                $"[TICK-DATA] Invalid packet count: {numPackets}",
    //                                NinjaTrader.Cbi.LogLevel.Warning);
    //                            continue;
    //                        }

    //                        for (int i = 0; i < numPackets && ms.Position < ms.Length; i++)
    //                        {
    //                            try
    //                            {
    //                                if (ms.Position + 2 > ms.Length)
    //                                {
    //                                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                        $"[TICK-DATA] Not enough data to read packet {i} length",
    //                                        NinjaTrader.Cbi.LogLevel.Warning);
    //                                    break;
    //                                }

    //                                short packetLength = ReadBigEndianInt16(reader);

    //                                if (packetLength <= 0 || packetLength > 1024)
    //                                {
    //                                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                        $"[TICK-DATA] Invalid packet length: {packetLength}",
    //                                        NinjaTrader.Cbi.LogLevel.Warning);
    //                                    break;
    //                                }

    //                                if (ms.Position + packetLength > ms.Length)
    //                                {
    //                                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                        $"[TICK-DATA] Not enough data for packet {i}",
    //                                        NinjaTrader.Cbi.LogLevel.Warning);
    //                                    break;
    //                                }

    //                                byte[] packetData = reader.ReadBytes(packetLength);

    //                                if (packetData.Length < 8)
    //                                {
    //                                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                        $"[TICK-DATA] Packet {i} too small",
    //                                        NinjaTrader.Cbi.LogLevel.Warning);
    //                                    continue;
    //                                }

    //                                using (var packetMs = new MemoryStream(packetData))
    //                                using (var packetReader = new BinaryReader(packetMs))
    //                                {
    //                                    int receivedToken = ReadBigEndianInt32(packetReader);

    //                                    if (receivedToken != instrumentToInt)
    //                                    {
    //                                        NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                            $"[TICK-DATA] Unexpected instrument token: {receivedToken}",
    //                                            NinjaTrader.Cbi.LogLevel.Warning);
    //                                        continue;
    //                                    }

    //                                    int lastPriceInt = ReadBigEndianInt32(packetReader);
    //                                    double lastPriceD = lastPriceInt / 100.0;

    //                                    // Check for subscription BEFORE processing
    //                                    if (!l1Subscriptions.TryGetValue(nativeSymbolName, out L1Subscription l1Sub))
    //                                    {
    //                                        // We no longer have this subscription - chart was closed
    //                                        NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                            $"[TICK-DATA] Subscription no longer exists for {nativeSymbolName} - chart likely closed",
    //                                            NinjaTrader.Cbi.LogLevel.Information);

    //                                        // Signal cancellation as we don't need this connection anymore
    //                                        if (!cts.IsCancellationRequested)
    //                                            cts.Cancel();

    //                                        continue;
    //                                    }

    //                                    if (!double.IsNaN(lastPriceD))
    //                                        lastPriceD = l1Sub.Instrument.MasterInstrument.RoundToTickSize(lastPriceD);

    //                                    //DateTime now = DateTime.Now;

    //                                    // With this:
    //                                    DateTime now;
    //                                    try
    //                                    {
    //                                        TimeZoneInfo indianZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
    //                                        now = TimeZoneInfo.ConvertTime(DateTime.Now, indianZone);
    //                                        NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                            $"[TIMESTAMP] Current Time {now} ",
    //                                            NinjaTrader.Cbi.LogLevel.Information);
    //                                    }
    //                                    catch
    //                                    {
    //                                        // Fallback if time zone not found
    //                                        now = DateTime.Now;
    //                                    }
    //                                    for (int index = 0; index < l1Sub.L1Callbacks.Count; ++index)
    //                                    {
    //                                        try
    //                                        {
    //                                            l1Sub.L1Callbacks.Values[index](MarketDataType.Last, lastPriceD, 1L, now, 0L);
    //                                        }
    //                                        catch (Exception ex)
    //                                        {
    //                                            NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                                $"[TICK-DATA] Error in callback {index}: {ex.Message}",
    //                                                NinjaTrader.Cbi.LogLevel.Error);
    //                                        }
    //                                    }
    //                                }
    //                            }
    //                            catch (Exception ex)
    //                            {
    //                                NinjaTrader.NinjaScript.NinjaScript.Log(
    //                                    $"[TICK-DATA] Error processing packet {i}: {ex.Message}",
    //                                    NinjaTrader.Cbi.LogLevel.Error);
    //                            }
    //                        }
    //                    }
    //                }
    //                catch (Exception ex)
    //                {
    //                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                        $"[TICK-DATA] Error processing message: {ex.Message}",
    //                        NinjaTrader.Cbi.LogLevel.Error);
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Error: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
    //        }
    //        finally
    //        {
    //            // Critical cleanup section - proper WebSocket disposal
    //            NinjaTrader.NinjaScript.NinjaScript.Log(
    //                $"[TICK-SUBSCRIBE] Cleaning up WebSocket for {symbol}",
    //                NinjaTrader.Cbi.LogLevel.Information);

    //            // Cancel the token if not already cancelled
    //            if (!cts.IsCancellationRequested)
    //                cts.Cancel();

    //            // Properly close the WebSocket if it's still open
    //            if (ws != null && ws.State == WebSocketState.Open)
    //            {
    //                try
    //                {
    //                    // Send unsubscribe message to Zerodha to properly clean up server-side
    //                    int instrumentToInt = (int)await GetInstrumentToken(symbol);
    //                    string unsubscribeMessage = $"{{\"a\": \"unsubscribe\", \"v\": [{instrumentToInt}]}}";

    //                    await ws.SendAsync(
    //                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(unsubscribeMessage)),
    //                        WebSocketMessageType.Text,
    //                        true,
    //                        CancellationToken.None);

    //                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                        $"[TICK-SUBSCRIBE] Sent unsubscribe message for {symbol}",
    //                        NinjaTrader.Cbi.LogLevel.Information);

    //                    // Properly close the WebSocket with a normal closure status
    //                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Chart closed", CancellationToken.None);
    //                }
    //                catch (Exception ex)
    //                {
    //                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                        $"[TICK-SUBSCRIBE] Error during WebSocket cleanup: {ex.Message}",
    //                        NinjaTrader.Cbi.LogLevel.Error);
    //                }
    //                finally
    //                {
    //                    // Always dispose the WebSocket
    //                    ws.Dispose();
    //                    cts.Dispose();

    //                    NinjaTrader.NinjaScript.NinjaScript.Log(
    //                        $"[TICK-SUBSCRIBE] WebSocket closed and disposed for {symbol}",
    //                        NinjaTrader.Cbi.LogLevel.Information);
    //                }
    //            }
    //        }

    //        NinjaTrader.NinjaScript.NinjaScript.Log($"[TICK-SUBSCRIBE] Completed subscription for {symbol}", NinjaTrader.Cbi.LogLevel.Information);
    //    }

    //    // Helper methods for big-endian reading
    //    private short ReadBigEndianInt16(BinaryReader reader)
    //    {
    //        byte[] bytes = reader.ReadBytes(2);
    //        if (BitConverter.IsLittleEndian)
    //            Array.Reverse(bytes);
    //        return BitConverter.ToInt16(bytes, 0);
    //    }

    //    private int ReadBigEndianInt32(BinaryReader reader)
    //    {
    //        byte[] bytes = reader.ReadBytes(4);
    //        if (BitConverter.IsLittleEndian)
    //            Array.Reverse(bytes);
    //        return BitConverter.ToInt32(bytes, 0);
    //    }
    //    Helper method to get instrument token from Zerodha
        private async Task<long> GetInstrumentTokenAsync(string symbol)
        {
            NinjaTrader.NinjaScript.NinjaScript.Log($"[INSTRUMENT] Getting instrument token for {symbol}", NinjaTrader.Cbi.LogLevel.Information);

            try
            {
                // For testing/debugging, you could hard-code known instrument tokens
                // Example: If you know NSE:RELIANCE is 738561, you could return that directly

                // You should implement a proper API call to Zerodha to get the instrument token
                // This could be from their instruments API or from a local cache

                // Example REST API call to get instrument token
                using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                {
                    // Set up API authentication
                    string apiKey = "YOUR_API_KEY"; // Replace with actual API key
                    string accessToken = "YOUR_ACCESS_TOKEN"; // Replace with actual access token

                    client.DefaultRequestHeaders.Add("X-Kite-Version", "3");
                    client.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");

                    // Call the instruments API
                    string instrumentsUrl = "https://api.kite.trade/instruments";
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[INSTRUMENT] Fetching instruments from {instrumentsUrl}", NinjaTrader.Cbi.LogLevel.Information);

                    var response = await client.GetStringAsync(instrumentsUrl);

                    // Parse the CSV response to find the matching instrument
                    // This is a simplified example - you'd need proper CSV parsing
                    foreach (string line in response.Split('\n'))
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 3 && parts[2].Equals(symbol, StringComparison.OrdinalIgnoreCase))
                        {
                            if (long.TryParse(parts[0], out long token))
                            {
                                NinjaTrader.NinjaScript.NinjaScript.Log($"[INSTRUMENT] Found instrument token: {token} for {symbol}", NinjaTrader.Cbi.LogLevel.Information);
                                return token;
                            }
                        }
                    }

                    // Symbol not found in instruments list
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[INSTRUMENT] Could not find instrument token for {symbol}", NinjaTrader.Cbi.LogLevel.Warning);
                }

                // Return a default value or throw an exception
                throw new Exception($"Instrument token not found for {symbol}");
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[INSTRUMENT] Error getting instrument token: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                throw;
            }
        }

        
        private MarketData ProcessZerodhaMessage(string messageData, long instrumentToken)
        {
            try
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[PROCESS] Processing message: {messageData.Substring(0, Math.Min(messageData.Length, 100))}", NinjaTrader.Cbi.LogLevel.Information);

                // Parse the JSON message
                var jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(messageData);

                // Check if it's a tick data message
                if (jsonData != null)
                {
                    // Zerodha tick data format may vary, adapt this based on actual response format
                    // Example for full mode data:
                    MarketData marketData = new MarketData();

                    // Sample parsing logic - adjust based on actual Zerodha message format
                    if (jsonData["a"] != null && jsonData["a"].ToString() == "tick")
                    {
                        var tickData = jsonData["v"][0]; // Get the first item in the array

                        // Make sure this is for the instrument we're interested in
                        if (tickData["instrument_token"] != null &&
                            (long)tickData["instrument_token"] == instrumentToken)
                        {
                            // Extract market data fields
                            marketData.LastPrice = (double)(tickData["last_price"] ?? 0);
                            marketData.LastQuantity = (long)(tickData["last_quantity"] ?? 0);

                            // Get best bid/ask if available
                            if (tickData["depth"] != null)
                            {
                                var buyDepth = tickData["depth"]["buy"];
                                var sellDepth = tickData["depth"]["sell"];

                                if (buyDepth != null && buyDepth.Count > 0)
                                {
                                    marketData.BestBidPrice = (double)(buyDepth[0]["price"] ?? 0);
                                    marketData.BestBidQuantity = (long)(buyDepth[0]["quantity"] ?? 0);
                                }

                                if (sellDepth != null && sellDepth.Count > 0)
                                {
                                    marketData.BestAskPrice = (double)(sellDepth[0]["price"] ?? 0);
                                    marketData.BestAskQuantity = (long)(sellDepth[0]["quantity"] ?? 0);
                                }
                            }

                            NinjaTrader.NinjaScript.NinjaScript.Log($"[PROCESS] Successfully parsed market data", NinjaTrader.Cbi.LogLevel.Information);
                            return marketData;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[PROCESS] Error processing message: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                return null;
            }
        }

        // Simple market data class to store parsed values
        private class MarketData
        {
            public double LastPrice { get; set; }
            public long LastQuantity { get; set; }
            public double BestBidPrice { get; set; }
            public long BestBidQuantity { get; set; }
            public double BestAskPrice { get; set; }
            public long BestAskQuantity { get; set; }
        }

        public async Task SubscribeToDepth(
    string nativeSymbolName,
    MarketType mtNative,
    string symbol,
    ConcurrentDictionary<string, L2Subscription> l2Subscriptions,
    WebSocketConnectionFunc webSocketConnectionFunc)
        {
            Guard.AgainstNullOrEmpty(symbol);
            Guard.AgainstNull(webSocketConnectionFunc);

            ClientWebSocket ws = null;
            var cts = new CancellationTokenSource();
            int tokenInt = (int)(await GetInstrumentToken(symbol));

            try
            {
                // Connect to Zerodha WebSocket
                string wsUrl = $"wss://ws.kite.trade?api_key={apiKey}&access_token={accessToken}";
                ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

                // Subscribe to the instrument 
                string subscribeMsg = $@"{{""a"":""subscribe"",""v"":[{tokenInt}]}}";
                await ws.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscribeMsg)),
                    WebSocketMessageType.Text, true, CancellationToken.None);

                // Set mode to full to get market depth data
                string modeMsg = $@"{{""a"":""mode"",""v"":[""full"",[{tokenInt}]]}}";
                await ws.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(modeMsg)),
                    WebSocketMessageType.Text, true, CancellationToken.None);

                NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH-SUBSCRIBE] Subscribed to token {tokenInt} in full mode", NinjaTrader.Cbi.LogLevel.Information);

                // Monitor exit condition
                _ = Task.Run(async () =>
                {
                    if (webSocketConnectionFunc.IsTimeout)
                    {
                        await Task.Delay(webSocketConnectionFunc.Timeout, cts.Token);
                        cts.Cancel();
                    }
                    else
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            if (webSocketConnectionFunc.ExitFunction())
                            {
                                cts.Cancel();
                                break;
                            }
                            await Task.Delay(100, cts.Token);
                        }
                    }
                });

                // Process WebSocket messages
                var buffer = new byte[4096];
                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    // Coalesce frames into a single message
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult frame;
                    do
                    {
                        frame = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (frame.MessageType == WebSocketMessageType.Close)
                            throw new OperationCanceledException("WebSocket closed by server.");
                        if (frame.MessageType == WebSocketMessageType.Text)
                            continue; // Ignore heartbeats or text messages
                        ms.Write(buffer, 0, frame.Count);
                    }
                    while (!frame.EndOfMessage);

                    // Get the complete message as a byte array
                    byte[] data = ms.ToArray();

                    // Process the binary message - similar to ZerodhaWebSocketClient
                    ParseAndProcessDepthData(data, tokenInt, nativeSymbolName, l2Subscriptions);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH-SUBSCRIBE] Error: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
            }
            finally
            {
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        string unsub = $@"{{""a"":""unsubscribe"",""v"":[{tokenInt}]}}";
                        await ws.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(unsub)),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Depth subscription closed", CancellationToken.None);
                    }
                    catch { }
                    ws.Dispose();
                }
                cts.Cancel();
                cts.Dispose();
            }
        }

        // Process depth data using direct byte array parsing approach
        private void ParseAndProcessDepthData(byte[] data, int tokenInt, string nativeSymbolName,
            ConcurrentDictionary<string, L2Subscription> l2Subscriptions)
        {
            if (data.Length < 2)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log("[DEPTH PARSER] Packet too small", NinjaTrader.Cbi.LogLevel.Warning);
                return;
            }

            int offset = 0;
            int packetCount = ReadInt16BE(data, offset);
            NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PARSER] Received {packetCount} packets", NinjaTrader.Cbi.LogLevel.Information);

            offset += 2;
            for (int i = 0; i < packetCount; i++)
            {
                // Check if we have enough data for packet length
                if (offset + 2 > data.Length)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PARSER] Not enough data for packet length at packet {i}",
                        NinjaTrader.Cbi.LogLevel.Warning);
                    break;
                }

                int packetLength = ReadInt16BE(data, offset);
                offset += 2;

                // Check if we have enough data for the packet content
                if (offset + packetLength > data.Length)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PARSER] Not enough data for packet content at packet {i}",
                        NinjaTrader.Cbi.LogLevel.Warning);
                    break;
                }

                // Only process packets with valid length (we need 184 bytes for market depth)
                if (packetLength != 44 && packetLength != 184)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PARSER] Unknown packet length: {packetLength}",
                        NinjaTrader.Cbi.LogLevel.Warning);
                    offset += packetLength; // Skip this packet
                    continue;
                }

                // Check if this is our subscribed token
                int iToken = ReadInt32BE(data, offset);
                if (iToken != tokenInt)
                {
                    offset += packetLength; // Skip this packet
                    continue;
                }

                // Process the packet
                ProcessDepthPacket(data, offset, packetLength, nativeSymbolName, l2Subscriptions);

                // Move to next packet
                offset += packetLength;
            }
        }

        private void ProcessDepthPacket(byte[] data, int offset, int packetLength, string nativeSymbolName,
            ConcurrentDictionary<string, L2Subscription> l2Subscriptions)
        {
            try
            {
                int instrumentToken = ReadInt32BE(data, offset);
                int lastTradedPrice = ReadInt32BE(data, offset + 4);
                double ltp = lastTradedPrice / 100.0;

                // Get current time
                DateTime now = DateTime.Now;

                if (packetLength == 44)
                {
                    // 44-byte packet doesn't contain market depth
                    // Just log that we received it
                    NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PACKET] Received 44-byte packet with LTP: {ltp:F2}",
                        NinjaTrader.Cbi.LogLevel.Information);
                    return;
                }

                if (packetLength == 184)
                {
                    // 184-byte packet with market depth
                    int lastTradedQty = ReadInt32BE(data, offset + 8);
                    int avgTradedPrice = ReadInt32BE(data, offset + 12);
                    int volume = ReadInt32BE(data, offset + 16);
                    int buyQty = ReadInt32BE(data, offset + 20);
                    int sellQty = ReadInt32BE(data, offset + 24);
                    int open = ReadInt32BE(data, offset + 28);
                    int high = ReadInt32BE(data, offset + 32);
                    int low = ReadInt32BE(data, offset + 36);
                    int close = ReadInt32BE(data, offset + 40);
                    int lastTradedTimestamp = ReadInt32BE(data, offset + 44);
                    int oi = ReadInt32BE(data, offset + 48);
                    int oiDayHigh = ReadInt32BE(data, offset + 52);
                    int oiDayLow = ReadInt32BE(data, offset + 56);
                    int exchangeTimestamp = ReadInt32BE(data, offset + 60);

                    // Convert exchange timestamp to local time
                    DateTime localTime;
                    try
                    {
                        var tickTime = UnixSecondsToLocalTime(exchangeTimestamp);
                        var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        localTime = TimeZoneInfo.ConvertTime(tickTime, tz);
                    }
                    catch
                    {
                        localTime = now; // Fallback to system time if conversion fails
                    }

                    // Parse bids and asks for market depth
                    var newBids = new List<(double price, long quantity)>();
                    var newAsks = new List<(double price, long quantity)>();

                    // Log for market depth visualization
                    string bidsLog = "";
                    string asksLog = "";

                    // Process bids (5 levels)
                    for (int i = 0; i < 5; i++)
                    {
                        int depthOffset = offset + 64 + (i * 12);
                        int qty = ReadInt32BE(data, depthOffset);
                        int price = ReadInt32BE(data, depthOffset + 4);
                        short orders = ReadInt16BE(data, depthOffset + 8);

                        double bidPrice = price / 100.0;

                        if (qty > 0)
                        {
                            newBids.Add((bidPrice, qty));
                            bidsLog += $"[{qty}@{bidPrice:F2} ({orders})] ";
                        }
                    }

                    // Process asks (5 levels)
                    for (int i = 0; i < 5; i++)
                    {
                        int depthOffset = offset + 124 + (i * 12);
                        int qty = ReadInt32BE(data, depthOffset);
                        int price = ReadInt32BE(data, depthOffset + 4);
                        short orders = ReadInt16BE(data, depthOffset + 8);

                        double askPrice = price / 100.0;

                        if (qty > 0)
                        {
                            newAsks.Add((askPrice, qty));
                            asksLog += $"[{qty}@{askPrice:F2} ({orders})] ";
                        }
                    }

                    // Log market depth information
                    NinjaTrader.NinjaScript.NinjaScript.Log(
                        $"[DEPTH FULL] {nativeSymbolName} | LTP: {ltp:F2} | Bids: {bidsLog} | Asks: {asksLog} | Time: {localTime:HH:mm:ss.fff}",
                        NinjaTrader.Cbi.LogLevel.Information);

                    // Update market depth in NinjaTrader
                    if (l2Subscriptions.TryGetValue(nativeSymbolName, out var l2Subscription))
                    {
                        for (int index = 0; index < l2Subscription.L2Callbacks.Count; ++index)
                        {
                            // Process asks (offers)
                            foreach (var ask in newAsks)
                            {
                                double price = ask.price;
                                long quantity = ask.quantity;
                                l2Subscription.L2Callbacks.Keys[index].UpdateMarketDepth(
                                    MarketDataType.Ask, price, quantity, Operation.Update, localTime, l2Subscription.L2Callbacks.Values[index]);
                            }

                            // Process bids
                            foreach (var bid in newBids)
                            {
                                double price = bid.price;
                                long quantity = bid.quantity;
                                l2Subscription.L2Callbacks.Keys[index].UpdateMarketDepth(
                                    MarketDataType.Bid, price, quantity, Operation.Update, localTime, l2Subscription.L2Callbacks.Values[index]);
                            }
                        }
                    }
                    else
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PACKET] No L2 subscription found for {nativeSymbolName}",
                            NinjaTrader.Cbi.LogLevel.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"[DEPTH PACKET] Exception: {ex.Message}",
                    NinjaTrader.Cbi.LogLevel.Error);
            }
        }
        private static void SendRequest(Stream stream, IEnumerable<string> request)
        {
            foreach (string s in request)
            {
                byte[] bytes = Encoding.Default.GetBytes(s);
                stream.Write(bytes, 0, bytes.Length);
                stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine), 0, 2);
            }
            stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine), 0, 2);
            Connector.ReadLine(stream);
        }

        private static string ReadLine(Stream stream)
        {
            List<byte> byteList = new List<byte>();
            while (true)
            {
                int num;
                do
                {
                    num = stream.ReadByte();
                    if (num == -1)
                        return (string)null;
                    if (num == 10)
                        goto label_6;
                }
                while (num == 13);
                byteList.Add((byte)num);
            }
        label_6:
            return Encoding.Default.GetString(byteList.ToArray());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }
    }

    
    public static class ZerodhaDataParser
    {
        public static List<SymbolObject> ParseSymbols(string csvData)
        {
            List<SymbolObject> symbols = new List<SymbolObject>();

            try
            {
                // Log CSV data length for debugging
                Logger.Info($"Received CSV data of length: {csvData.Length}");

                // Split CSV into lines and skip header
                string[] lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 1)
                {
                    Logger.Warn("CSV data contains no instrument records");
                    return symbols;
                }

                // Get column headers
                string[] headers = lines[0].Split(',');

                // Find indices for important columns
                int tradingSymbolIndex = Array.IndexOf(headers, "tradingsymbol");
                int nameIndex = Array.IndexOf(headers, "name");
                int exchangeIndex = Array.IndexOf(headers, "exchange");
                int instrumentTypeIndex = Array.IndexOf(headers, "instrument_type");
                int tickSizeIndex = Array.IndexOf(headers, "tick_size");
                int lotSizeIndex = Array.IndexOf(headers, "lot_size");

                if (tradingSymbolIndex < 0 || exchangeIndex < 0)
                {
                    Logger.Error("Required columns not found in CSV data");
                    return symbols;
                }

                // Parse data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        string[] fields = lines[i].Split(',');
                        if (fields.Length <= Math.Max(tradingSymbolIndex, exchangeIndex))
                            continue;


                        string tradingSymbol = fields[tradingSymbolIndex];
                        string exchange = fields[exchangeIndex];
                        
                        // Skip empty symbols
                        if (string.IsNullOrEmpty(tradingSymbol))
                            continue;

                        // Get optional fields
                        string name = (nameIndex >= 0 && fields.Length > nameIndex) ? fields[nameIndex] : tradingSymbol;
                        string instrumentType = (instrumentTypeIndex >= 0 && fields.Length > instrumentTypeIndex) ? fields[instrumentTypeIndex] : "";

                        // Create filters for tick size and lot size
                        List<Filter> filters = new List<Filter>();

                        if (tickSizeIndex >= 0 && fields.Length > tickSizeIndex)
                        {
                            decimal tickSize;
                            if (decimal.TryParse(fields[tickSizeIndex], out tickSize))
                            {
                                filters.Add(new Filter
                                {
                                    FilterType = "PRICE_FILTER",
                                    TickSize = Convert.ToDouble(tickSize)
                                });
                            }
                        }

                        if (lotSizeIndex >= 0 && fields.Length > lotSizeIndex)
                        {
                            decimal lotSize;
                            if (decimal.TryParse(fields[lotSizeIndex], out lotSize))
                            {
                                filters.Add(new Filter
                                {
                                    FilterType = "LOT_SIZE",
                                    StepSize = Convert.ToDouble(lotSize)
                                });
                            }
                        }

                        SymbolObject symbolObject = new SymbolObject
                        {
                            Symbol = tradingSymbol,
                            BaseAsset = tradingSymbol,
                            QuoteAsset = exchange,
                            Status = "TRADING",
                            Filters = filters.ToArray()
                        };

                        symbols.Add(symbolObject);

                        // Log progress for every 1000 symbols
                        if (symbols.Count % 1000 == 0)
                        {
                            Logger.Info($"Parsed {symbols.Count} symbols so far");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error parsing symbol at line {i}: {ex.Message}");
                        // Continue with next symbol
                    }
                }

                Logger.Info($"Successfully parsed {symbols.Count} symbols from CSV data");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error parsing CSV data: {ex.Message}");
            }

            return symbols;
        }
    }
}