// Adapted for Zerodha .NET 4.8
using QABrokerAPI.Zerodha;
using QABrokerAPI.Common.Interfaces;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Extensions;
using QABrokerAPI.Common.Models.WebSocket;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using QABrokerAPI.Common.Utility;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using WebSocketSharp;
using System.Net.Http;
using System.Text;

#nullable disable
namespace QABrokerAPI.Zerodha.Websockets
{
    public class AbstractBrokerWebSocketClient
    {
        protected string BaseWebsocketUri = "wss://ws.kite.trade";
        protected Dictionary<Guid, BrokerWebSocket> ActiveWebSockets;
        protected List<BrokerWebSocket> AllSockets;
        protected readonly IBrokerClient ZerodhaClient;
        protected ILog Logger;
        protected const string AccountEventType = "account";
        protected const string OrderTradeEventType = "order";
        private Dictionary<string, long> instrumentTokens = new Dictionary<string, long>();
        protected SslProtocols SupportedProtocols { get; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        public string ApiKey { get; set; }
        public string AccessToken { get; set; }

        public AbstractBrokerWebSocketClient(IBrokerClient zerodhaClient, ILog logger = null)
        {
            this.ZerodhaClient = zerodhaClient;
            this.ActiveWebSockets = new Dictionary<Guid, BrokerWebSocket>();
            this.AllSockets = new List<BrokerWebSocket>();
            this.Logger = logger ?? LogManager.GetLogger(typeof(AbstractBrokerWebSocketClient));
        }
        private async Task LoadInstrumentTokens()
        {
            try
            {
                // Only load if not already loaded
                if (instrumentTokens.Count > 0)
                    return;

                

                using (HttpClient client = new HttpClient())
                {
                    // Set up credentials
                    client.DefaultRequestHeaders.Add("X-Kite-Apikey", ApiKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"token {ApiKey}:{AccessToken}");

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

                            return;
                        }

                        // Get column indices
                        string[] headers = lines[0].Split(',');
                        int tradingSymbolIndex = Array.IndexOf(headers, "tradingsymbol");
                        int instrumentTokenIndex = Array.IndexOf(headers, "instrument_token");
                        int exchangeIndex = Array.IndexOf(headers, "exchange");

                        if (tradingSymbolIndex < 0 || instrumentTokenIndex < 0 || exchangeIndex < 0)
                        {
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

                                if (!instrumentTokens.ContainsKey(key))
                                {
                                    instrumentTokens[key] = instrumentToken;
                                }

                                // Also add just the symbol for convenience
                                if (!instrumentTokens.ContainsKey(tradingSymbol))
                                {
                                    instrumentTokens[tradingSymbol] = instrumentToken;
                                }
                            }
                        }

                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Guid> ConnectToUserDataWebSocket(
          UserDataWebSocketMessages userDataMessageHandlers)
        {
            Guard.AgainstNull((object)this.ZerodhaClient, "ZerodhaClient");
            this.Logger.Debug((object)"Connecting to User Data Web Socket");

            // Instead of getting auth details, we'll use ApiKey and AccessToken that should be set externally
            if (string.IsNullOrEmpty(this.ApiKey) || string.IsNullOrEmpty(this.AccessToken))
            {
                throw new InvalidOperationException("ApiKey and AccessToken must be set before connecting to WebSocket");
            }

            return this.CreateZerodhaUserDataWebSocket(new Uri($"{this.BaseWebsocketUri}?api_key={this.ApiKey}&access_token={this.AccessToken}"), userDataMessageHandlers);
        }

        public Guid ConnectToKlineWebSocket(
          string symbol,
          KlineInterval interval,
          BrokerWebSocketMessageHandler<BrokerKlineData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            this.Logger.Debug((object)"Connecting to Kline Web Socket");
            return this.CreateZerodhaWebSocket<BrokerKlineData>(new Uri($"{this.BaseWebsocketUri}?api_key={this.ApiKey}&access_token={this.AccessToken}"), messageEventHandler, symbol, interval);
        }

        public Guid ConnectToDepthWebSocket(
          string symbol,
          BrokerWebSocketMessageHandler<BrokerDepthData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            this.Logger.Debug((object)"Connecting to Depth Web Socket");
            return this.CreateZerodhaWebSocket<BrokerDepthData>(new Uri($"{this.BaseWebsocketUri}?api_key={this.ApiKey}&access_token={this.AccessToken}"), messageEventHandler, symbol);
        }

        public Guid ConnectToTradesWebSocket(
          string symbol,
          BrokerWebSocketMessageHandler<BrokerAggregateTradeData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            this.Logger.Debug((object)"Connecting to Trades Web Socket");
            return this.CreateZerodhaWebSocket<BrokerAggregateTradeData>(new Uri($"{this.BaseWebsocketUri}?api_key={this.ApiKey}&access_token={this.AccessToken}"), messageEventHandler, symbol);
        }

        public Guid ConnectToIndividualSymbolTickerWebSocket(
          MarketType mt,
          string symbol,
          BrokerWebSocketMessageHandler<BrokerTradeData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            this.Logger.Debug((object)"Connecting to Individual Symbol Ticker Web Socket");
            return this.CreateZerodhaWebSocket<BrokerTradeData>(new Uri($"{this.BaseWebsocketUri}?api_key={this.ApiKey}&access_token={this.AccessToken}"), messageEventHandler, symbol);
        }

        public Guid ConnectToIndividualSymbolTickerWebSocket(
          BrokerWebSocketMessageHandler<BrokerAggregateTradeData> messageEventHandler)
        {
            this.Logger.Debug((object)"Connecting to All Market Symbol Ticker Web Socket");
            return this.CreateZerodhaWebSocket<BrokerAggregateTradeData>(new Uri($"{this.BaseWebsocketUri}?api_key={this.ApiKey}&access_token={this.AccessToken}"), messageEventHandler);
        }

        private Guid CreateZerodhaUserDataWebSocket(
          Uri endpoint,
          UserDataWebSocketMessages userDataWebSocketMessages)
        {
            BrokerWebSocket websocket = new BrokerWebSocket(endpoint.AbsoluteUri, Array.Empty<string>());
            websocket.OnOpen += (EventHandler)((sender, e) => {
                this.Logger.Debug((object)("WebSocket Opened:" + endpoint.AbsoluteUri));
                // Subscribe to user events after connection is open
                websocket.Send(JsonConvert.SerializeObject(new
                {
                    a = "subscribe",
                    v = new[] { "account", "order", "trade" }
                }));
            });
            websocket.OnMessage += (EventHandler<MessageEventArgs>)((sender, e) =>
            {
                this.Logger.Debug((object)("WebSocket Message Received on Endpoint: " + endpoint.AbsoluteUri));
                dynamic data = JsonConvert.DeserializeObject(e.Data);
                string type = data.type;

                switch (type)
                {
                    case "account":
                        BrokerAccountUpdateData data1 = JsonConvert.DeserializeObject<BrokerAccountUpdateData>(e.Data);
                        BrokerWebSocketMessageHandler<BrokerAccountUpdateData> updateMessageHandler1 = userDataWebSocketMessages.AccountUpdateMessageHandler;
                        if (updateMessageHandler1 == null)
                            break;
                        updateMessageHandler1(data1);
                        break;
                    case "order":
                        BrokerTradeOrderData data2 = JsonConvert.DeserializeObject<BrokerTradeOrderData>(e.Data);
                        if (data2.ExecutionType == ExecutionType.Trade)
                        {
                            BrokerWebSocketMessageHandler<BrokerTradeOrderData> updateMessageHandler2 = userDataWebSocketMessages.TradeUpdateMessageHandler;
                            if (updateMessageHandler2 == null)
                                break;
                            updateMessageHandler2(data2);
                            break;
                        }
                        BrokerWebSocketMessageHandler<BrokerTradeOrderData> updateMessageHandler3 = userDataWebSocketMessages.OrderUpdateMessageHandler;
                        if (updateMessageHandler3 == null)
                            break;
                        updateMessageHandler3(data2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
            websocket.OnError += (EventHandler<ErrorEventArgs>)((sender, e) =>
            {
                this.Logger.Error((object)$"WebSocket Error on {endpoint.AbsoluteUri}: ", e.Exception);
                this.CloseWebSocketInstance(websocket.Id, true);
                throw new Exception("Zerodha UserData WebSocket failed")
                {
                    Data = {
                        {
                            (object)"ErrorEventArgs",
                            (object)e
                        }
                    }
                };
            });
            if (!this.ActiveWebSockets.ContainsKey(websocket.Id))
                this.ActiveWebSockets.Add(websocket.Id, websocket);
            this.AllSockets.Add(websocket);
            websocket.SslConfiguration.EnabledSslProtocols = this.SupportedProtocols;
            websocket.Connect();
            return websocket.Id;
        }

        private Guid CreateZerodhaWebSocket<T>(
          Uri endpoint,
          BrokerWebSocketMessageHandler<T> messageEventHandler,
          string symbol = null,
          KlineInterval? interval = null)
          where T : IWebSocketResponse
        {

            BrokerWebSocket websocket = new BrokerWebSocket(endpoint.AbsoluteUri, Array.Empty<string>());
          
            websocket.OnOpen += (sender, e) => {
                this.Logger.Debug((object)("WebSocket Opened:" + endpoint.AbsoluteUri));

                if (!string.IsNullOrEmpty(symbol))
                {
                    // Launch a background task to handle the async work
                    Task.Run(async () => {
                        try
                        {
                            //long instrumentToken = await GetInstrumentToken(symbol);
                            long instrumentToken = 408065;

                            this.Logger.Debug((object)("Subscribing to instrumentToken:" + instrumentToken + " " + symbol));
                            websocket.Send(JsonConvert.SerializeObject(new
                            {
                                a = "subscribe",
                                v = new[] { instrumentToken }
                            }));

                            // Rest of the code...
                            string mode = "full";
                            if (typeof(T) == typeof(BrokerDepthData))
                                mode = "depth";
                            else if (typeof(T) == typeof(BrokerAggregateTradeData))
                                mode = "ltpc";

                            websocket.Send(JsonConvert.SerializeObject(new
                            {
                                a = "mode",
                                v = new[] { mode, JsonConvert.SerializeObject(new[] { instrumentToken }) }
                            }));
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error((object)$"Error getting instrument token: {ex.Message}", ex);
                        }
                    });
                }
            };

            websocket.OnMessage += (EventHandler<MessageEventArgs>)((sender, e) =>
            {
                this.Logger.Debug((object)("WebSocket Message Received on: " + endpoint.AbsoluteUri));

                // Process the data based on type
                try
                {
                    // Process and convert Zerodha's data format to expected format
                    T data = ProcessZerodhaMessage<T>(e.Data, symbol, interval);
                    this.Logger.Debug((object)$"Processed data:   {e.RawData.ToString()} ");
                    messageEventHandler(data);
                }
                catch (Exception ex)
                {
                    this.Logger.Error((object)$"Error processing Zerodha message: {ex.Message}", ex);
                }
            });
            websocket.OnError += (EventHandler<ErrorEventArgs>)((sender, e) =>
            {
                this.Logger.Debug((object)$"WebSocket Error on {endpoint.AbsoluteUri}:", e.Exception);
                this.CloseWebSocketInstance(websocket.Id, true);
                throw new Exception("Zerodha WebSocket failed")
                {
                    Data = {
                        {
                            (object)"ErrorEventArgs",
                            (object)e
                        }
                    }
                };
            });
            if (!this.ActiveWebSockets.ContainsKey(websocket.Id))
                this.ActiveWebSockets.Add(websocket.Id, websocket);
            this.AllSockets.Add(websocket);
            websocket.SslConfiguration.EnabledSslProtocols = this.SupportedProtocols;
            websocket.Connect();
            return websocket.Id;
            }
            
        

        // Helper methods for Zerodha integration
        private async Task<long> GetInstrumentToken(string symbol)
        {
            await LoadInstrumentTokens();
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentException("Symbol cannot be null or empty.");
            }

            if (instrumentTokens.TryGetValue(symbol, out long token))
            {
                return token;
            }

            Logger.Warn($"Instrument token not found for symbol: {symbol}");
            throw new KeyNotFoundException($"No instrument token found for symbol: {symbol}");
        }



        private T ProcessZerodhaMessage<T>(string message, string symbol, KlineInterval? interval) where T : IWebSocketResponse
        {
            try
            {
                // For Zerodha, message is actually a binary data string
                // We need to convert it to byte array first
                byte[] binaryData = Encoding.UTF8.GetBytes(message);
                this.Logger.Debug($"Processing Zerodha binary message, length: {binaryData.Length} bytes");

                if (typeof(T) == typeof(BrokerTradeData))
                {
                    var tradeData = new BrokerTradeData
                    {
                        // Set common IWebSocketResponse properties
                        EventType = "ticker",
                        EventTime = DateTime.Now,
                        Symbol = symbol
                    };

                    // Parse binary data according to Zerodha's format
                    // First, get the number of packets in the message (first 2 bytes)
                    if (binaryData.Length < 4)
                    {
                        this.Logger.Error("Binary message too short to contain valid data");
                        return (T)(object)tradeData; // Return empty object
                    }

                    int numPackets = BitConverter.ToInt16(binaryData, 0);
                    this.Logger.Debug($"Number of packets in message: {numPackets}");

                    if (numPackets <= 0)
                    {
                        this.Logger.Error("No packets found in binary message");
                        return (T)(object)tradeData; // Return empty object
                    }

                    // Get the length of the first packet (next 2 bytes)
                    int packetLength = BitConverter.ToInt16(binaryData, 2);
                    this.Logger.Debug($"First packet length: {packetLength} bytes");

                    if (binaryData.Length < 4 + packetLength)
                    {
                        this.Logger.Error("Binary message too short to contain complete packet");
                        return (T)(object)tradeData; // Return empty object
                    }

                    // Extract the packet data
                    byte[] packetData = new byte[packetLength];
                    Array.Copy(binaryData, 4, packetData, 0, packetLength);

                    // Parse the packet according to Zerodha's structure
                    if (packetData.Length >= 44) // Ensure we have enough data for quote mode
                    {
                        // Extract values from the binary packet
                        int instrumentToken = BitConverter.ToInt32(packetData, 0);
                        int lastPrice = BitConverter.ToInt32(packetData, 4);
                        int lastQty = BitConverter.ToInt32(packetData, 8);
                        int avgPrice = BitConverter.ToInt32(packetData, 12);
                        int volume = BitConverter.ToInt32(packetData, 16);
                        int buyQty = BitConverter.ToInt32(packetData, 20);
                        int sellQty = BitConverter.ToInt32(packetData, 24);
                        int openPrice = BitConverter.ToInt32(packetData, 28);
                        int highPrice = BitConverter.ToInt32(packetData, 32);
                        int lowPrice = BitConverter.ToInt32(packetData, 36);
                        int closePrice = BitConverter.ToInt32(packetData, 40);

                        // Convert to decimal values (divide by 100 as per Zerodha docs)
                        // For currencies, divide by 10000000 to get 4 decimal places
                        decimal divisor = 100m; // Adjust based on instrument type if needed

                        tradeData.LastPrice = lastPrice / divisor;
                        tradeData.LastQuantity = lastQty;
                        tradeData.WeightedAveragePrice = avgPrice / divisor;
                        tradeData.TotalTradedBaseAssetVolume = volume;
                        tradeData.BestBidQuantity = buyQty;
                        tradeData.BestAskQuantity = sellQty;
                        tradeData.OpenPrice = openPrice / divisor;
                        tradeData.HighPrice = highPrice / divisor;
                        tradeData.LowPrice = lowPrice / divisor;
                        tradeData.FirstTrade = closePrice / divisor; // Using as close price, field mismatch

                        // If we have full data (not just quote mode)
                        if (packetData.Length >= 64)
                        {
                            int timestamp = BitConverter.ToInt32(packetData, 44);
                            int oi = BitConverter.ToInt32(packetData, 48);
                            int oiDayHigh = BitConverter.ToInt32(packetData, 52);
                            int oiDayLow = BitConverter.ToInt32(packetData, 56);
                            int exchangeTimestamp = BitConverter.ToInt32(packetData, 60);

                            // Convert Unix timestamp to DateTime if needed
                            DateTime exchangeTime = DateTimeOffset.FromUnixTimeSeconds(exchangeTimestamp).DateTime;
                            tradeData.StatisticsOpenTime = exchangeTime;
                        }

                        // If we have market depth data (full mode)
                        if (packetData.Length >= 184)
                        {
                            // Extract best bid and ask from the market depth
                            // First bid entry starts at offset 64
                            int bidQty = BitConverter.ToInt32(packetData, 64);
                            int bidPrice = BitConverter.ToInt32(packetData, 68);
                            // First ask entry starts at offset 124
                            int askQty = BitConverter.ToInt32(packetData, 124);
                            int askPrice = BitConverter.ToInt32(packetData, 128);

                            tradeData.BestBidPrice = bidPrice / divisor;
                            tradeData.BestBidQuantity = bidQty;
                            tradeData.BestAskPrice = askPrice / divisor;
                            tradeData.BestAskQuantity = askQty;
                        }
                    }

                    this.Logger.Debug($"Processed trade data: Last={tradeData.LastPrice}@{tradeData.LastQuantity}, " +
                                     $"Bid={tradeData.BestBidPrice}@{tradeData.BestBidQuantity}, " +
                                     $"Ask={tradeData.BestAskPrice}@{tradeData.BestAskQuantity}");

                    return (T)(object)tradeData;
                }
                else
                {
                    this.Logger.Error($"Unsupported type for Zerodha binary data: {typeof(T).Name}");
                    // Create a default instance of the requested type
                    return Activator.CreateInstance<T>();
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error($"Error processing Zerodha binary message: {ex.Message}", ex);
                return Activator.CreateInstance<T>(); // Return empty object on error
            }
        }



        //private T ProcessZerodhaMessage<T>(string message, string symbol, KlineInterval? interval) where T : IWebSocketResponse
        //{
        //    // Convert Zerodha's message format to the expected type
        //    // This is a placeholder implementation
        //    dynamic data = JsonConvert.DeserializeObject(message);

        //    // Create appropriate type based on T
        //    if (typeof(T) == typeof(BrokerKlineData))
        //    {
        //        // Convert to kline data
        //        var klineData = new BrokerKlineData();
        //        // Set properties based on Zerodha data
        //        return (T)(object)klineData;
        //    }
        //    else if (typeof(T) == typeof(BrokerDepthData))
        //    {
        //        // Convert to depth data
        //        var depthData = new BrokerDepthData();
        //        // Set properties based on Zerodha data
        //        return (T)(object)depthData;
        //    }
        //    else if (typeof(T) == typeof(BrokerAggregateTradeData))
        //    {
        //        // Convert to trade data
        //        var tradeData = new BrokerAggregateTradeData();
        //        // Set properties based on Zerodha data
        //        return (T)(object)tradeData;
        //    }
        //    else if (typeof(T) == typeof(BrokerTradeData))
        //    {
        //        // Convert to ticker data
        //        var tickerData = new BrokerTradeData();
        //        // Set properties based on Zerodha data
        //        return (T)(object)tickerData;
        //    }

        //    // Default case - deserialize directly
        //    return JsonConvert.DeserializeObject<T>(message);
        //}

        public void CloseWebSocketInstance(Guid id, bool fromError = false)
        {
            BrokerWebSocket zerodhaWebSocket = this.ActiveWebSockets.ContainsKey(id) ? this.ActiveWebSockets[id] : throw new Exception("No Websocket exists with the Id " + id.ToString());
            this.ActiveWebSockets.Remove(id);
            if (fromError)
                return;
            zerodhaWebSocket.Close(CloseStatusCode.PolicyViolation);
        }

        public bool IsAlive(Guid id)
        {
            if (this.ActiveWebSockets.ContainsKey(id))
                return this.ActiveWebSockets[id].IsAlive;
            throw new Exception("No Websocket exists with the Id " + id.ToString());
        }
    }
}