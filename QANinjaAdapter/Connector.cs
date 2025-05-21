using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Utility;
using QABrokerAPI.Zerodha;
using QABrokerAPI.Zerodha.Websockets;
using QANinjaAdapter.Annotations;
using QANinjaAdapter.Classes;
using QANinjaAdapter.Classes.Binance.Symbols;
using QANinjaAdapter.Services.Configuration;
using QANinjaAdapter.Services.Instruments;
using QANinjaAdapter.Services.MarketData;
using QANinjaAdapter.Services.Zerodha;
using QANinjaAdapter.ViewModels;
using log4net;
using NinjaTrader.Adapter;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

#nullable disable
namespace QANinjaAdapter
{
    /// <summary>
    /// Main connector class for the QA Ninja Adapter
    /// </summary>
    public class Connector : INotifyPropertyChanged
    {
        private bool _connected;
        private bool _isClearStocks;
        private static BrokerClient _client;
        private static Connector _instance;
        private ControlCenter _controlCenter;

        private readonly ConfigurationManager _configManager;
        private readonly ZerodhaClient _zerodhaClient;
        private readonly InstrumentManager _instrumentManager;
        private readonly HistoricalDataService _historicalDataService;
        private readonly MarketDataService _marketDataService;

        /// <summary>
        /// Gets the version of the adapter
        /// </summary>
        public string Version { get; } = "2.0.1";

        /// <summary>
        /// Gets whether the adapter is connected
        /// </summary>
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

        /// <summary>
        /// Gets the broker client
        /// </summary>
        public static BrokerClient Client
        {
            get
            {
                if (Connector._client == null)
                {
                    ILog logger = LogManager.GetLogger(typeof(Connector));
                    logger.Debug((object)"Connector Debug");

                    // Initialize Zerodha client with access token
                    var configManager = ConfigurationManager.Instance;
                    Connector._client = new BrokerClient(new ClientConfiguration()
                    {
                        ApiKey = configManager.ApiKey,
                        SecretKey = configManager.SecretKey,
                        AccessToken = configManager.AccessToken,
                        Logger = logger
                    });
                }
                return Connector._client;
            }
        }

        private static QAAdapter _qaAdapter;

        /// <summary>
        /// Sets the QAAdapter instance
        /// </summary>
        /// <param name="adapter">The QAAdapter instance</param>
        public static void SetAdapter(QAAdapter adapter)
        {
            _qaAdapter = adapter;
        }

        /// <summary>
        /// Gets the QAAdapter instance
        /// </summary>
        /// <returns>The QAAdapter instance</returns>
        public IAdapter GetAdapter()
        {
            return _qaAdapter;
        }

        /// <summary>
        /// Gets the singleton instance of the Connector
        /// </summary>
        public static Connector Instance
        {
            get
            {
                if (Connector._instance == null)
                    Connector._instance = new Connector();
                return Connector._instance;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Connector()
        {
            Logger.Initialize();
            Logger.Info($"QANinjaAdapter v{Version} initializing...");

            _configManager = ConfigurationManager.Instance;
            _zerodhaClient = ZerodhaClient.Instance;
            _instrumentManager = InstrumentManager.Instance;
            _historicalDataService = HistoricalDataService.Instance;
            _marketDataService = MarketDataService.Instance;

            // Load configuration
            if (!_configManager.LoadConfiguration())
            {
                // Handle configuration failure
                MessageBox.Show("Using default API keys. Please check your configuration file.",
                    "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Checks the connection to the Zerodha API
        /// </summary>
        /// <returns>True if the connection is valid, false otherwise</returns>
        public bool CheckConnection()
        {
            if (!_zerodhaClient.CheckConnection())
                return false;

            this.IsConnected = true;
            return true;
        }

        /// <summary>
        /// Gets the symbol name with market type
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="marketType">The market type</param>
        /// <returns>The symbol name</returns>
        public static string GetSymbolName(string symbol, out MarketType marketType)
        {
            return InstrumentManager.GetSymbolName(symbol, out marketType);
        }

        /// <summary>
        /// Gets the suffix for a market type
        /// </summary>
        /// <param name="marketType">The market type</param>
        /// <returns>The suffix</returns>
        public static string GetSuffix(MarketType marketType)
        {
            return InstrumentManager.GetSuffix(marketType);
        }

        /// <summary>
        /// Registers Zerodha symbols in NinjaTrader
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RegisterBinanceSymbols()
        {
            await _instrumentManager.RegisterSymbols();
        }

        /// <summary>
        /// Gets exchange information for all available instruments
        /// </summary>
        /// <returns>A collection of symbol objects</returns>
        public async Task<ObservableCollection<SymbolObject>> GetExchangeInformation()
        {
            return await _instrumentManager.GetExchangeInformation();
        }

        /// <summary>
        /// Creates an instrument in NinjaTrader
        /// </summary>
        /// <param name="instrument">The instrument to create</param>
        /// <param name="ntSymbolName">The NinjaTrader symbol name</param>
        /// <returns>True if the instrument was created successfully, false otherwise</returns>
        public bool CreateInstrument(SymbolObject instrument, out string ntSymbolName)
        {
            return _instrumentManager.CreateInstrument(instrument, out ntSymbolName);
        }

        /// <summary>
        /// Removes an instrument from NinjaTrader
        /// </summary>
        /// <param name="instrument">The instrument to remove</param>
        /// <returns>True if the instrument was removed successfully, false otherwise</returns>
        public bool RemoveInstrument(SymbolObject instrument)
        {
            return _instrumentManager.RemoveInstrument(instrument);
        }

        /// <summary>
        /// Gets all NinjaTrader symbols
        /// </summary>
        /// <returns>A collection of symbol objects</returns>
        public async Task<ObservableCollection<SymbolObject>> GetNTSymbols()
        {
            return await _instrumentManager.GetNTSymbols();
        }

        /// <summary>
        /// Gets historical trades for a symbol
        /// </summary>
        /// <param name="barsPeriodType">The bars period type</param>
        /// <param name="symbol">The symbol</param>
        /// <param name="fromDate">The start date</param>
        /// <param name="toDate">The end date</param>
        /// <param name="marketType">The market type</param>
        /// <param name="viewModelBase">The view model for progress updates</param>
        /// <returns>A list of historical records</returns>
        public async Task<List<Record>> GetHistoricalTrades(
            BarsPeriodType barsPeriodType,
            string symbol,
            DateTime fromDate,
            DateTime toDate,
            MarketType marketType,
            ViewModelBase viewModelBase)
        {
            return await _historicalDataService.GetHistoricalTrades(
                barsPeriodType,
                symbol,
                fromDate,
                toDate,
                marketType,
                viewModelBase);
        }

        /// <summary>
        /// Subscribes to real-time ticks for a symbol
        /// </summary>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="marketType">The market type</param>
        /// <param name="symbol">The symbol</param>
        /// <param name="l1Subscriptions">The L1 subscriptions dictionary</param>
        /// <param name="webSocketConnectionFunc">The WebSocket connection function</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SubscribeToTicks(
            string nativeSymbolName,
            MarketType marketType,
            string symbol,
            ConcurrentDictionary<string, L1Subscription> l1Subscriptions,
            WebSocketConnectionFunc webSocketConnectionFunc)
        {
            await _marketDataService.SubscribeToTicks(
                nativeSymbolName,
                marketType,
                symbol,
                l1Subscriptions,
                webSocketConnectionFunc);
        }

        /// <summary>
        /// Subscribes to market depth for a symbol
        /// </summary>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="marketType">The market type</param>
        /// <param name="symbol">The symbol</param>
        /// <param name="l2Subscriptions">The L2 subscriptions dictionary</param>
        /// <param name="webSocketConnectionFunc">The WebSocket connection function</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SubscribeToDepth(
            string nativeSymbolName,
            MarketType marketType,
            string symbol,
            ConcurrentDictionary<string, L2Subscription> l2Subscriptions,
            WebSocketConnectionFunc webSocketConnectionFunc)
        {
            await _marketDataService.SubscribeToDepth(
                nativeSymbolName,
                marketType,
                symbol,
                l2Subscriptions,
                webSocketConnectionFunc);
        }

        /// <summary>
        /// Clears wrong stocks
        /// </summary>
        public void ClearWrongStocks() => this.FindCCControl();

        /// <summary>
        /// Finds the control center
        /// </summary>
        /// <returns>The chart control</returns>
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

        /// <summary>
        /// Handles the control center got focus event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event args</param>
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

        /// <summary>
        /// Clears stocks
        /// </summary>
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

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the property changed event
        /// </summary>
        /// <param name="propertyName">The property name</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
