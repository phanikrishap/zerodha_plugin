using QAAdapterAddOn.ViewModels;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Zerodha.Websockets;
using QANinjaAdapter.Classes;
using QANinjaAdapter.Controls;
using QANinjaAdapter.Models;
using QANinjaAdapter.Models.MarketData;
using QANinjaAdapter.ViewModels;
using NinjaTrader.Adapter;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Adapters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NinjaTrader.CQG.ProtoBuf;
using System.Data.SQLite;

#nullable disable
namespace QANinjaAdapter
{
    public class QAAdapter : AdapterBase, IAdapter, IDisposable
    {
        private IConnection _ninjaConnection;
        private QAConnectorOptions _options;
        private readonly ConcurrentDictionary<string, L1Subscription> _l1Subscriptions = new ConcurrentDictionary<string, L1Subscription>();
        private readonly ConcurrentDictionary<string, L2Subscription> _l2Subscriptions = new ConcurrentDictionary<string, L2Subscription>();
        private readonly List<string> _marketLiveDataSymbols = new List<string>();
        private readonly List<string> _marketDepthDataSymbols = new List<string>();
        private static readonly object _lockLiveSymbol = new object();
        private static readonly object _lockDepthSymbol = new object();

        public static void LogMe(string text) => NinjaTrader.NinjaScript.NinjaScript.Log(text, NinjaTrader.Cbi.LogLevel.Warning);

        public void Connect(IConnection connection)
        {
            this._ninjaConnection = connection;
            this._options = (QAConnectorOptions)this._ninjaConnection.Options;
            
            // Set the adapter instance in the Connector class
            Connector.SetAdapter(this);
            
            this._ninjaConnection.OrderTypes = new NinjaTrader.Cbi.OrderType[4]
            {
                NinjaTrader.Cbi.OrderType.Market,
                NinjaTrader.Cbi.OrderType.Limit,
                NinjaTrader.Cbi.OrderType.StopMarket,
                NinjaTrader.Cbi.OrderType.StopLimit
            };
            this._ninjaConnection.TimeInForces = new NinjaTrader.Cbi.TimeInForce[3]
            {
                NinjaTrader.Cbi.TimeInForce.Day,
                NinjaTrader.Cbi.TimeInForce.Gtc,
                NinjaTrader.Cbi.TimeInForce.Gtd
            };
            this._ninjaConnection.Features = new NinjaTrader.Cbi.Feature[10]
            {
                NinjaTrader.Cbi.Feature.Bars1Minute,
                NinjaTrader.Cbi.Feature.BarsDaily,
                NinjaTrader.Cbi.Feature.BarsTick,
                NinjaTrader.Cbi.Feature.BarsTickIntraday,
                NinjaTrader.Cbi.Feature.MarketData,
                NinjaTrader.Cbi.Feature.AtmStrategies,
                NinjaTrader.Cbi.Feature.Order,
                NinjaTrader.Cbi.Feature.OrderChange,
                NinjaTrader.Cbi.Feature.CustomOrders,
                NinjaTrader.Cbi.Feature.MarketDepth
            };
            this._ninjaConnection.InstrumentTypes = new InstrumentType[3]
            {
                InstrumentType.Stock,
                InstrumentType.Future,
                InstrumentType.Option
            };
            this._ninjaConnection.MarketDataTypes = new MarketDataType[1]
            {
                MarketDataType.Last
            };
            this.Connect();
        }

        private async void Connect()
        {
            if (this._ninjaConnection.Status == ConnectionStatus.Connecting)
            {
                if (Connector.Instance.CheckConnection())
                {
                    this.SetInstruments();
                    this._ninjaConnection.ConnectionStatusCallback(ConnectionStatus.Connected, ConnectionStatus.Connected, ErrorCode.NoError, "");

                    await Connector.Instance.RegisterBinanceSymbols();
                }
                else
                    this._ninjaConnection.ConnectionStatusCallback(ConnectionStatus.Disconnected, ConnectionStatus.Disconnected, ErrorCode.LogOnFailed, "Unable to connect to provider Zerodha.");
            }
            else
                this.Disconnect();
        }

        private void SetInstruments()
        {
            DataContext.Instance.SymbolNames.Clear();
            // Changed from CryptoCurrency to Stock for Zerodha
            foreach (MasterInstrument masterInstrument in MasterInstrument.All.Where<MasterInstrument>((Func<MasterInstrument, bool>)(x =>
                (x.InstrumentType == InstrumentType.Stock ||
                 x.InstrumentType == InstrumentType.Future ||
                 x.InstrumentType == InstrumentType.Option) &&
                !string.IsNullOrEmpty(((IEnumerable<string>)x.ProviderNames).ElementAtOrDefault<string>(1019)))))
            {
                if (!DataContext.Instance.SymbolNames.ContainsKey(masterInstrument.Name))
                    DataContext.Instance.SymbolNames.Add(masterInstrument.Name, masterInstrument.ProviderNames[1019]);
            }
        }

        protected override void OnStateChange()
        {
            if (this.State == State.SetDefaults)
            {

                this.Name = "QA Adapter";
                //this.DisplayName = "QANinjaAdapter";
                //this.DisplayName = "QANinjaAdapter";
            }
            if (this.State != State.Configure)
                return;
        }

        public void Disconnect()
        {
            lock (QAAdapter._lockLiveSymbol)
                this._marketLiveDataSymbols?.Clear();
            lock (QAAdapter._lockDepthSymbol)
                this._marketDepthDataSymbols?.Clear();
            if (this._ninjaConnection.Status == ConnectionStatus.Disconnected)
                return;
            this._ninjaConnection.ConnectionStatusCallback(ConnectionStatus.Disconnected, ConnectionStatus.Disconnected, ErrorCode.NoError, string.Empty);
        }

        public void ResolveInstrument(
            Instrument instrument,
            Action<Instrument, ErrorCode, string> callback)
        {
        }

        public void SubscribeFundamentalData(
            Instrument instrument,
            Action<FundamentalDataType, object> callback)
        {
        }

        public void UnsubscribeFundamentalData(Instrument instrument)
        {
        }

        public void SubscribeMarketData(
    Instrument instrument,
    Action<MarketDataType, double, long, DateTime, long> callback)
        {
            try
            {
                if (this._ninjaConnection.Trace.MarketData)
                    this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture,
                        $"({this._options.Name}) QAAdapter.SubscribeMarketData: instrument='{instrument.FullName}'"));

                if (this._ninjaConnection.Status == ConnectionStatus.Disconnecting ||
                    this._ninjaConnection.Status == ConnectionStatus.Disconnected)
                    return;

                string name = instrument.MasterInstrument.Name;
                if (string.IsNullOrEmpty(name))
                    return;

                string nativeSymbolName = name;
                MarketType mt = MarketType.Spot;

                // First handle the subscription object to avoid race conditions
                L1Subscription l1Subscription;

                if (!this._l1Subscriptions.TryGetValue(nativeSymbolName, out l1Subscription))
                {
                    // Create a new subscription
                    l1Subscription = new L1Subscription
                    {
                        Instrument = instrument,
                        L1Callbacks = new SortedList<Instrument, Action<MarketDataType, double, long, DateTime, long>>()
                    };

                    // Add the callback
                    l1Subscription.L1Callbacks.Add(instrument, callback);

                    // Add to dictionary
                    this._l1Subscriptions.TryAdd(nativeSymbolName, l1Subscription);
                }
                else
                {
                    // If the callbacks collection is null, initialize it
                    if (l1Subscription.L1Callbacks == null)
                    {
                        l1Subscription.L1Callbacks = new SortedList<Instrument, Action<MarketDataType, double, long, DateTime, long>>();
                    }

                    // Update or add the callback
                    if (l1Subscription.L1Callbacks.ContainsKey(instrument))
                    {
                        l1Subscription.L1Callbacks[instrument] = callback;
                    }
                    else
                    {
                        l1Subscription.L1Callbacks.Add(instrument, callback);
                    }
                }

                // Now handle the websocket subscription
                lock (QAAdapter._lockLiveSymbol)
                {
                    // Only start a new connection if we don't already have one for this symbol
                    if (!this._marketLiveDataSymbols.Contains(name))
                    {
                        string originalSymbol = Connector.GetSymbolName(name, out mt);
                        this._marketLiveDataSymbols.Add(nativeSymbolName);

                        Task.Run(async () => {
                            try
                            {
                                await Connector.Instance.SubscribeToTicks(
                                    nativeSymbolName,
                                    mt,
                                    originalSymbol,
                                    this._l1Subscriptions,
                                    new WebSocketConnectionFunc((Func<bool>)(() =>
                                    {
                                        lock (QAAdapter._lockLiveSymbol)
                                            return !this._marketLiveDataSymbols.Contains(nativeSymbolName);
                                    }))
                                );
                            }
                            catch (Exception ex)
                            {
                                // Log error but also remove the symbol from active symbols
                                // to allow retry on next subscription request
                                lock (QAAdapter._lockLiveSymbol)
                                {
                                    if (this._marketLiveDataSymbols.Contains(nativeSymbolName))
                                        this._marketLiveDataSymbols.Remove(nativeSymbolName);
                                }

                                if (this._ninjaConnection.Trace.Connect)
                                    this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture,
                                        $"({this._options.Name}) QAAdapter.SubscribeToTicks Exception={ex}"));
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (this._ninjaConnection.Trace.Connect)
                    this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture,
                        $"({this._options.Name}) QAAdapter.SubscribeMarketData Exception={ex}"));
            }
        }

        public void UnsubscribeMarketData(Instrument instrument)
        {
            string name = instrument.MasterInstrument.Name;
            lock (QAAdapter._lockLiveSymbol)
            {
                this._marketLiveDataSymbols.Remove(name);
                this._l1Subscriptions.TryRemove(name, out L1Subscription _);
            }
        }

        public void SubscribeMarketDepth(
            Instrument instrument,
            Action<int, string, Operation, MarketDataType, double, long, DateTime> callback)
        {
            NinjaTrader.NinjaScript.NinjaScript.Log($"DEBUG-CALL: SubscribeMarketData called for {instrument.FullName}", NinjaTrader.Cbi.LogLevel.Error); // Use Error level to make it stand out
            try
            {
                if (this._ninjaConnection.Trace.MarketDepth)
                    this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture, $"({this._options.Name}) ZerodhaAdapter.SubscribeMarketDepth: instrument='{instrument.FullName}'"));
                if (this._ninjaConnection.Status == ConnectionStatus.Disconnecting || this._ninjaConnection.Status == ConnectionStatus.Disconnected)
                    return;
                string name = instrument.MasterInstrument.Name;
                MarketType mt = MarketType.Spot;
                if (string.IsNullOrEmpty(name))
                    return;
                string nativeSymbolName = name;
                lock (QAAdapter._lockDepthSymbol)
                {
                    if (!this._marketDepthDataSymbols.Contains(name))
                    {
                        string originalSymbol = Connector.GetSymbolName(name, out mt);
                        this._marketDepthDataSymbols.Add(nativeSymbolName);
                        Task.Run((Func<Task>)(() => Connector.Instance.SubscribeToDepth(nativeSymbolName, mt, originalSymbol, this._l2Subscriptions, new WebSocketConnectionFunc((Func<bool>)(() =>
                        {
                            lock (QAAdapter._lockDepthSymbol)
                                return !this._marketDepthDataSymbols.Contains(nativeSymbolName);
                        })))));
                    }
                }
                L2Subscription l2Subscription1;
                this._l2Subscriptions.TryGetValue(name, out l2Subscription1);
                if (l2Subscription1 == null)
                {
                    ConcurrentDictionary<string, L2Subscription> l2Subscriptions = this._l2Subscriptions;
                    string key = name;
                    L2Subscription l2Subscription2 = new L2Subscription();
                    l2Subscription2.Instrument = instrument;
                    l2Subscription1 = l2Subscription2;
                    l2Subscriptions.TryAdd(key, l2Subscription2);
                }
                l2Subscription1.L2Callbacks = new SortedList<Instrument, Action<int, string, Operation, MarketDataType, double, long, DateTime>>((IDictionary<Instrument, Action<int, string, Operation, MarketDataType, double, long, DateTime>>)l2Subscription1.L2Callbacks)
                {
                    {
                        instrument,
                        callback
                    }
                };
                int status = (int)this._ninjaConnection.Status;
            }
            catch (Exception ex)
            {
                if (!this._ninjaConnection.Trace.Connect)
                    return;
                this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture, $"({this._options.Name}) ZerodhaAdapter.SubscribeMarketDepth Exception={ex.ToString()}"));
            }
        }

        public void UnsubscribeMarketDepth(Instrument instrument)
        {
            string name = instrument.MasterInstrument.Name;
            lock (QAAdapter._lockDepthSymbol)
            {
                this._marketDepthDataSymbols.Remove(name);
                this._l2Subscriptions.TryRemove(name, out L2Subscription _);
            }
        }

        /// <summary>
        /// Processes a parsed tick data object and updates NinjaTrader with all available market data
        /// </summary>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="tickData">The parsed tick data</param>
        public void ProcessParsedTick(string nativeSymbolName, ZerodhaTickData tickData)
        {
            try
            {
                if (this._ninjaConnection.Trace.MarketData)
                    this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture,
                        $"({this._options.Name}) QAAdapter.ProcessParsedTick: instrument='{nativeSymbolName}'"));

                if (this._ninjaConnection.Status == ConnectionStatus.Disconnecting ||
                    this._ninjaConnection.Status == ConnectionStatus.Disconnected)
                    return;

                // Check if we have a valid subscription
                if (!this._l1Subscriptions.TryGetValue(nativeSymbolName, out var l1Subscription))
                {
                    return;
                }

                // Round the price to the instrument's tick size
                double lastPrice = l1Subscription.Instrument.MasterInstrument.RoundToTickSize(tickData.LastTradePrice);
                
                // Calculate volume delta
                int volumeDelta = Math.Max(0, l1Subscription.PreviousVolume == 0 ? 0 : tickData.TotalQtyTraded - l1Subscription.PreviousVolume);
                l1Subscription.PreviousVolume = tickData.TotalQtyTraded;

                // Update all callbacks with the comprehensive market data
                foreach (var cb in l1Subscription.L1Callbacks.Values)
                {
                    // Update Last price and volume
                    cb(MarketDataType.Last, lastPrice, volumeDelta > 0 ? volumeDelta : tickData.LastTradeQty, tickData.LastTradeTime, 0L);
                    
                    // Update Bid/Ask
                    if (tickData.BuyPrice > 0)
                        cb(MarketDataType.Bid, l1Subscription.Instrument.MasterInstrument.RoundToTickSize(tickData.BuyPrice), tickData.BuyQty, tickData.LastTradeTime, 0L);
                    
                    if (tickData.SellPrice > 0)
                        cb(MarketDataType.Ask, l1Subscription.Instrument.MasterInstrument.RoundToTickSize(tickData.SellPrice), tickData.SellQty, tickData.LastTradeTime, 0L);
                    
                    // Update daily statistics
                    cb(MarketDataType.DailyVolume, tickData.TotalQtyTraded, tickData.TotalQtyTraded, tickData.LastTradeTime, 0L);
                    
                    if (tickData.High > 0)
                        cb(MarketDataType.DailyHigh, l1Subscription.Instrument.MasterInstrument.RoundToTickSize(tickData.High), 0L, tickData.LastTradeTime, 0L);
                    
                    if (tickData.Low > 0)
                        cb(MarketDataType.DailyLow, l1Subscription.Instrument.MasterInstrument.RoundToTickSize(tickData.Low), 0L, tickData.LastTradeTime, 0L);
                    
                    if (tickData.Open > 0)
                        cb(MarketDataType.Opening, l1Subscription.Instrument.MasterInstrument.RoundToTickSize(tickData.Open), 0L, tickData.LastTradeTime, 0L);
                    
                    if (tickData.Close > 0)
                        cb(MarketDataType.LastClose, l1Subscription.Instrument.MasterInstrument.RoundToTickSize(tickData.Close), 0L, tickData.LastTradeTime, 0L);
                    
                    // Update open interest if available
                    if (tickData.OpenInterest > 0)
                        cb(MarketDataType.OpenInterest, tickData.OpenInterest, tickData.OpenInterest, tickData.LastTradeTime, 0L);
                }
            }
            catch (Exception ex)
            {
                if (this._ninjaConnection.Trace.Connect)
                    this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture,
                        $"({this._options.Name}) QAAdapter.ProcessParsedTick Exception={ex}"));
            }
        }

        // Trading methods - implement these if Zerodha trading is needed
        public void Cancel(NinjaTrader.Cbi.Order[] orders)
        {
        }

        public void Change(NinjaTrader.Cbi.Order[] orders)
        {
        }

        public void Submit(NinjaTrader.Cbi.Order[] orders)
        {
        }

        public void SubscribeAccount(NinjaTrader.Cbi.Account account)
        {
        }

        public void UnsubscribeAccount(NinjaTrader.Cbi.Account account)
        {
        }

        private int HowManyBarsFromDays(DateTime startDate) => (DateTime.Now - startDate).Days;

        private int HowManyBarsFromMinutes(DateTime startDate)
        {
            return Convert.ToInt32((DateTime.Now - startDate).TotalMinutes);
        }

        private void BarsWorker(QAAdapter.BarsRequest barsRequest)
        {
            if (this._ninjaConnection.Trace.Bars)
                this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture, $"({this._options.Name}) ZerodhaAdapter.BarsWorker"));

            EventHandler eventHandler = (EventHandler)((s, e) => { });

            try
            {
                
                NinjaTrader.NinjaScript.NinjaScript.Log($"Starting bars request for {barsRequest?.Bars?.Instrument?.MasterInstrument?.Name}", NinjaTrader.Cbi.LogLevel.Information);

                if (barsRequest.Progress != null)
                {
                    string shortDatePattern = Globals.GeneralOptions.CurrentCulture.DateTimeFormat.ShortDatePattern;
                    CultureInfo currentCulture = Globals.GeneralOptions.CurrentCulture;
                    barsRequest.Progress.Aborted += eventHandler;
                }

                bool flag = false;
                string name = barsRequest.Bars.Instrument.MasterInstrument.Name;
                //NinjaTrader.Cbi.InstrumentType marketType = barsRequest.Bars.Instrument.GetType();
                MarketType marketType = MarketType.Spot; // Default to Spot market type
                string symbolName = Connector.GetSymbolName(name, out marketType);

                NinjaTrader.NinjaScript.NinjaScript.Log($"Symbol: {symbolName}, Market Type: {marketType}", NinjaTrader.Cbi.LogLevel.Information);

                // Create the loading UI only if needed
                LoadViewModel loadViewModel = new LoadViewModel();
                loadViewModel.Message = "Loading historical data...";
                loadViewModel.SubMessage = "Preparing request";

                // Make the UI visible regardless of chart grid
                loadViewModel.IsBusy = true;

                List<Record> source = null;

                    
                try
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"Requesting bars: Type={barsRequest.Bars.BarsPeriod.BarsPeriodType}, From={barsRequest.Bars.FromDate}, To={barsRequest.Bars.ToDate}", NinjaTrader.Cbi.LogLevel.Information);
                    Task<List<Record>> task = null;

                    DateTime fromDateWithTime = new DateTime(
                        barsRequest.Bars.FromDate.Year,
                        barsRequest.Bars.FromDate.Month,
                        barsRequest.Bars.FromDate.Day,
                        9, 15, 0);  // 9:30:00 AM
                    DateTime toDateWithTime = new DateTime(
                           barsRequest.Bars.ToDate.Year,
                           barsRequest.Bars.ToDate.Month,
                           barsRequest.Bars.ToDate.Day,
                           15, 30, 0);  // 3:30:00 PM

                        if (barsRequest.Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Day && this.HowManyBarsFromDays(barsRequest.Bars.FromDate) > 0)
                    {
                        task = Connector.Instance.GetHistoricalTrades(barsRequest.Bars.BarsPeriod.BarsPeriodType, symbolName, fromDateWithTime, toDateWithTime,marketType, (ViewModelBase)loadViewModel);
                    }
                    else if (barsRequest.Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && this.HowManyBarsFromMinutes(barsRequest.Bars.FromDate) > 0)
                    {
                        task = Connector.Instance.GetHistoricalTrades(barsRequest.Bars.BarsPeriod.BarsPeriodType, symbolName, fromDateWithTime, toDateWithTime, marketType, (ViewModelBase)loadViewModel);
                    }
                    else if (barsRequest.Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Tick)
                    {
                        // For tick data, we'll skip historical data requests and just return an empty list
                        // This is expected behavior since Zerodha doesn't support historical tick data
                        source = new List<Record>();
                        NinjaTrader.NinjaScript.NinjaScript.Log("Historical tick data not available from Zerodha. Using empty history with real-time tick subscription.", NinjaTrader.Cbi.LogLevel.Information);
                        // No task needed, we already have the empty list
                    }

                    if (task != null)
                    {
                        try
                        {
                            source = task.Result;
                            NinjaTrader.NinjaScript.NinjaScript.Log($"Retrieved {source?.Count ?? 0} historical data points", NinjaTrader.Cbi.LogLevel.Information);
                        }
                        catch (AggregateException ae)
                        {
                            // Unwrap aggregate exception to get the real error
                            string errorMsg = ae.InnerException?.Message ?? ae.Message;
                            NinjaTrader.NinjaScript.NinjaScript.Log($"Error retrieving historical data: {errorMsg}", NinjaTrader.Cbi.LogLevel.Error);

                            // Set error flag
                            flag = true;
                        }
                    }
                    else
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log("No historical data request was made", NinjaTrader.Cbi.LogLevel.Warning);
                    }
                }
                finally
                {
                    // Clean up UI state
                    loadViewModel.IsBusy = false;
                    loadViewModel.Message = "";
                    loadViewModel.SubMessage = "";
                }

                // Process the data if available
                if (source != null)
                {
                    if (source.Count == 0)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log("No data returned from historical data request", NinjaTrader.Cbi.LogLevel.Warning);
                    }

                    foreach (Record record in (IEnumerable<Record>)source.OrderBy<Record, DateTime>((Func<Record, DateTime>)(x => x.TimeStamp)))
                    {
                        if (barsRequest.Progress != null && barsRequest.Progress.IsAborted)
                        {
                            flag = true;
                            break;
                        }

                        if (this._ninjaConnection.Status != ConnectionStatus.Disconnecting)
                        {
                            if (this._ninjaConnection.Status != ConnectionStatus.Disconnected)
                            {
                                double open = record.Open;
                                double high = record.High;
                                double low = record.Low;
                                double close = record.Close;

                                if (record.Volume >= 0.0)
                                {
                                    long volume = (long)record.Volume;
                                  
                                    TimeZoneInfo indianZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                                    DateTime displayTime = TimeZoneInfo.ConvertTime(record.TimeStamp, indianZone);

                                    //Check both date and time constraints
                                    if (displayTime >= barsRequest.Bars.FromDate)
                                    {
                                        //Add time of day filter for 9:30 AM to 3:30 PM

                                       TimeSpan timeOfDay = displayTime.TimeOfDay;
                                       TimeSpan marketOpen = new TimeSpan(9, 15, 0);   // 9:30 AM
                                        TimeSpan marketClose = new TimeSpan(15, 30, 0); // 3:30 PM

                                        //Only add bars during market hours
                                        if (timeOfDay >= marketOpen && timeOfDay <= marketClose)
                                            {
                                                barsRequest.Bars.Add(open, high, low, close, displayTime, volume, double.MinValue, double.MinValue);
                                        }
                                    }

                                }
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }
                }

                if (barsRequest == null)
                    return;

                if (barsRequest.Progress != null)
                {
                    barsRequest.Progress.Aborted -= eventHandler;
                    barsRequest.Progress.TearDown();
                }

                IBars bars = barsRequest.Bars;
                NinjaTrader.NinjaScript.NinjaScript.Log("Finishing bars request", NinjaTrader.Cbi.LogLevel.Information);
                barsRequest.BarsCallback(bars, flag ? ErrorCode.UserAbort : ErrorCode.NoError, string.Empty);
                barsRequest = null;
            }
            catch (Exception ex)
            {
                string errorMessage = $"BarsWorker Exception: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner: {ex.InnerException.Message}";
                }

                NinjaTrader.NinjaScript.NinjaScript.Log(errorMessage, NinjaTrader.Cbi.LogLevel.Error);
                NinjaTrader.NinjaScript.NinjaScript.Log($"Stack trace: {ex.StackTrace}", NinjaTrader.Cbi.LogLevel.Error);

                if (this._ninjaConnection.Trace.Bars)
                    this._ninjaConnection.TraceCallback(string.Format((IFormatProvider)CultureInfo.InvariantCulture, $"({this._options.Name}) ZerodhaAdapter.BarsWorker Exception='{ex.ToString()}'"));

                if (barsRequest == null)
                    return;

                if (barsRequest.Progress != null)
                {
                    barsRequest.Progress.Aborted -= eventHandler;
                    barsRequest.Progress.TearDown();
                }

                IBars bars = barsRequest.Bars;
                barsRequest.BarsCallback(bars, ErrorCode.Panic, errorMessage);
            }
        }
        private bool IsIndianMarketInstrument(Instrument instrument)
        {
            // Identify Indian market instruments by exchange or symbol pattern
            string name = instrument.MasterInstrument.Name;
            return name.EndsWith("-NSE") || name.EndsWith("-BSE") ||
                   instrument.Exchange == Exchange.Nse || instrument.Exchange == Exchange.Bse;
        }

        private NinjaTrader.Gui.Chart.Chart FindChartControl(string instrument)
        {
            foreach (Window window in Globals.AllWindows)
            {
                if (window is NinjaTrader.Gui.Chart.Chart chartControl &&
                    chartControl.ChartTrader?.Instrument?.MasterInstrument?.Name == instrument)
                {
                    return chartControl;
                }
            }
            return null;
        }

        public void RequestBars(
            IBars bars,
            Action<IBars, ErrorCode, string> callback,
            IProgress progress)
        {
            try
            {
                QAAdapter.BarsRequest request = new QAAdapter.BarsRequest()
                {
                    Bars = bars,
                    BarsCallback = callback,
                    Progress = progress
                };
                Task.Run((Action)(() => this.BarsWorker(request)));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void OnMarketDepthReceived(Quote quote)
        {
        }

        public void RequestHotlistNames(Action<string[], ErrorCode, string> callback)
        {
        }

        public void SubscribeHotlist(Hotlist hotlist, Action callback)
        {
        }

        public void UnsubscribeHotlist(Hotlist hotlist)
        {
        }

        public void SubscribeNews()
        {
        }

        public void UnsubscribeNews()
        {
        }

        public void Dispose()
        {
        }

        private class BarsRequest
        {
            public IBars Bars { get; set; }

            public Action<IBars, ErrorCode, string> BarsCallback { get; set; }

            public IProgress Progress { get; set; }
        }
    }
}
