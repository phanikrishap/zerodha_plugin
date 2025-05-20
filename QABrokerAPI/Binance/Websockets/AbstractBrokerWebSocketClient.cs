// Decompiled with JetBrains decompiler
// Type: BinanceExchange.API.Websockets.AbstractBinanceWebSocketClient
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Binance;
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



#nullable disable
namespace QABrokerAPI.Binance.Websockets;

public class AbstractBrokerWebSocketClient
{
  protected string BaseWebsocketUri = "wss://stream.binance.com:9443/ws";
  protected string BaseWebsocketUriUsdMFut = "wss://fstream.binance.com/ws";
  protected string BaseWebsocketUriCoinMFut = "wss://dstream.binance.com/ws";
  protected string CombinedWebsocketUri = "wss://stream.binance.com:9443/stream?streams";
  protected Dictionary<Guid, BrokerWebSocket> ActiveWebSockets;
  protected List<BrokerWebSocket> AllSockets;
  protected readonly IBrokerClient BinanceClient;
  protected ILog Logger;
  protected const string AccountEventType = "outboundAccountInfo";
  protected const string OrderTradeEventType = "executionReport";

  protected SslProtocols SupportedProtocols { get; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

  public string ListenKey { get; private set; }

  public AbstractBrokerWebSocketClient(IBrokerClient binanceClient, ILog logger = null)
  {
    this.BinanceClient = binanceClient;
    this.ActiveWebSockets = new Dictionary<Guid, BrokerWebSocket>();
    this.AllSockets = new List<BrokerWebSocket>();
    this.Logger = logger ?? LogManager.GetLogger(typeof (AbstractBrokerWebSocketClient));
  }

  public async Task<Guid> ConnectToUserDataWebSocket(
    UserDataWebSocketMessages userDataMessageHandlers)
  {
    Guard.AgainstNull((object) this.BinanceClient, "BinanceClient");
    this.Logger.Debug((object) "Connecting to User Data Web Socket");
    this.ListenKey = (await this.BinanceClient.StartUserDataStream()).ListenKey;
    return this.CreateUserDataBinanceWebSocket(new Uri($"{this.BaseWebsocketUri}/{this.ListenKey}"), userDataMessageHandlers);
  }

  public Guid ConnectToKlineWebSocket(
    string symbol,
    KlineInterval interval,
    BrokerWebSocketMessageHandler<BrokerKlineData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbol, nameof (symbol));
    this.Logger.Debug((object) "Connecting to Kline Web Socket");
    return this.CreateBinanceWebSocket<BrokerKlineData>(new Uri($"{this.BaseWebsocketUri}/{symbol.ToLower()}@kline_{EnumExtensions.GetEnumMemberValue<KlineInterval>(interval)}"), messageEventHandler);
  }

  public Guid ConnectToDepthWebSocket(
    string symbol,
    BrokerWebSocketMessageHandler<BrokerDepthData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbol, nameof (symbol));
    this.Logger.Debug((object) "Connecting to Depth Web Socket");
    return this.CreateBinanceWebSocket<BrokerDepthData>(new Uri($"{this.BaseWebsocketUri}/{symbol.ToLower()}@depth"), messageEventHandler);
  }

  public Guid ConnectToPartialDepthWebSocket(
    string symbol,
    PartialDepthLevels levels,
    BrokerWebSocketMessageHandler<BrokerPartialData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbol, nameof (symbol));
    this.Logger.Debug((object) "Connecting to Partial Depth Web Socket");
    return this.CreateBinanceWebSocket<BrokerPartialData>(new Uri($"{this.BaseWebsocketUri}/{symbol.ToLower()}@depth{(int) levels}"), messageEventHandler);
  }

  public Guid ConnectToFastPartialDepthWebSocket(
    string symbol,
    PartialDepthLevels levels,
    BrokerWebSocketMessageHandler<BrokerPartialData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbol, nameof (symbol));
    this.Logger.Debug((object) "Connecting to Fast Partial Depth Web Socket");
    return this.CreateBinanceWebSocket<BrokerPartialData>(new Uri($"{this.BaseWebsocketUri}/{symbol.ToLower()}@depth{(int) levels}@100ms"), messageEventHandler);
  }

  public Guid ConnectToDepthWebSocketCombined(
    string symbols,
    BrokerWebSocketMessageHandler<BrokerCombinedDepthData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbols, nameof (symbols));
    symbols = PrepareCombinedSymbols.CombinedDepth(symbols);
    this.Logger.Debug((object) "Connecting to Combined Depth Web Socket");
    return this.CreateBinanceWebSocket<BrokerCombinedDepthData>(new Uri($"{this.CombinedWebsocketUri}={symbols}"), messageEventHandler);
  }

  public Guid ConnectToDepthWebSocketCombinedPartial(
    string symbols,
    string depth,
    BrokerWebSocketMessageHandler<BrokerPartialDepthData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbols, nameof (symbols));
    Guard.AgainstNullOrEmpty(depth, nameof (depth));
    symbols = PrepareCombinedSymbols.CombinedPartialDepth(symbols, depth);
    this.Logger.Debug((object) "Connecting to Combined Partial Depth Web Socket");
    return this.CreateBinanceWebSocket<BrokerPartialDepthData>(new Uri($"{this.CombinedWebsocketUri}={symbols}"), messageEventHandler);
  }

  public Guid ConnectToTradesWebSocket(
    string symbol,
    BrokerWebSocketMessageHandler<BrokerAggregateTradeData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbol, nameof (symbol));
    this.Logger.Debug((object) "Connecting to Trades Web Socket");
    return this.CreateBinanceWebSocket<BrokerAggregateTradeData>(new Uri($"{this.BaseWebsocketUri}/{symbol.ToLower()}@aggTrade"), messageEventHandler);
  }

  public Guid ConnectToIndividualSymbolTickerWebSocket(
    MarketType mt,
    string symbol,
    BrokerWebSocketMessageHandler<BrokerTradeData> messageEventHandler)
  {
    Guard.AgainstNullOrEmpty(symbol, nameof (symbol));
    this.Logger.Debug((object) "Connecting to Individual Symbol Ticker Web Socket");
    Uri endpoint = new Uri($"{this.BaseWebsocketUri}/{symbol.ToLower()}@ticker");
    if (mt == MarketType.UsdM)
      endpoint = new Uri($"{this.BaseWebsocketUriUsdMFut}/{symbol.ToLower()}@ticker");
    if (mt == MarketType.CoinM)
      endpoint = new Uri($"{this.BaseWebsocketUriCoinMFut}/{symbol.ToLower()}@ticker");
    return this.CreateBinanceWebSocket<BrokerTradeData>(endpoint, messageEventHandler);
  }

  public Guid ConnectToIndividualSymbolTickerWebSocket(
    BrokerWebSocketMessageHandler<BrokerAggregateTradeData> messageEventHandler)
  {
    this.Logger.Debug((object) "Connecting to All Market Symbol Ticker Web Socket");
    return this.CreateBinanceWebSocket<BrokerAggregateTradeData>(new Uri(this.BaseWebsocketUri + "/!ticker@arr"), messageEventHandler);
  }

  private Guid CreateUserDataBinanceWebSocket(
    Uri endpoint,
    UserDataWebSocketMessages userDataWebSocketMessages)
  {
    BrokerWebSocket websocket = new BrokerWebSocket(endpoint.AbsoluteUri, Array.Empty<string>());
    websocket.OnOpen += (EventHandler) ((sender, e) => this.Logger.Debug((object) ("WebSocket Opened:" + endpoint.AbsoluteUri)));
    websocket.OnMessage += (EventHandler<MessageEventArgs>) ((sender, e) =>
    {
      this.Logger.Debug((object) ("WebSocket Message Received on Endpoint: " + endpoint.AbsoluteUri));
      switch (JsonConvert.DeserializeObject<BrokerWebSocketResponse>(e.Data).EventType)
      {
        case "outboundAccountInfo":
          BrokerAccountUpdateData data1 = JsonConvert.DeserializeObject<BrokerAccountUpdateData>(e.Data);
                BrokerWebSocketMessageHandler<BrokerAccountUpdateData> updateMessageHandler1 = userDataWebSocketMessages.AccountUpdateMessageHandler;
          if (updateMessageHandler1 == null)
            break;
          updateMessageHandler1(data1);
          break;
        case "executionReport":
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
    websocket.OnError += (EventHandler<ErrorEventArgs>) ((sender, e) =>
    {
      this.Logger.Error((object) $"WebSocket Error on {endpoint.AbsoluteUri}: ", e.Exception);
      this.CloseWebSocketInstance(websocket.Id, true);
      throw new Exception("Binance UserData WebSocket failed")
      {
        Data = {
          {
            (object) "ErrorEventArgs",
            (object) e
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

  private Guid CreateBinanceWebSocket<T>(
    Uri endpoint,
    BrokerWebSocketMessageHandler<T> messageEventHandler)
    where T : IWebSocketResponse
  {
    BrokerWebSocket websocket = new BrokerWebSocket(endpoint.AbsoluteUri, Array.Empty<string>());
    websocket.OnOpen += (EventHandler) ((sender, e) => this.Logger.Debug((object) ("WebSocket Opened:" + endpoint.AbsoluteUri)));
    websocket.OnMessage += (EventHandler<MessageEventArgs>) ((sender, e) =>
    {
      this.Logger.Debug((object) ("WebSocket Messge Received on: " + endpoint.AbsoluteUri));
      messageEventHandler(JsonConvert.DeserializeObject<T>(e.Data));
    });
    websocket.OnError += (EventHandler<ErrorEventArgs>) ((sender, e) =>
    {
      this.Logger.Debug((object) $"WebSocket Error on {endpoint.AbsoluteUri}:", e.Exception);
      this.CloseWebSocketInstance(websocket.Id, true);
      throw new Exception("Binance WebSocket failed")
      {
        Data = {
          {
            (object) "ErrorEventArgs",
            (object) e
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

  public void CloseWebSocketInstance(Guid id, bool fromError = false)
  {
    BrokerWebSocket binanceWebSocket = this.ActiveWebSockets.ContainsKey(id) ? this.ActiveWebSockets[id] : throw new Exception("No Websocket exists with the Id " + id.ToString());
    this.ActiveWebSockets.Remove(id);
    if (fromError)
      return;
    binanceWebSocket.Close(CloseStatusCode.PolicyViolation);
  }

  public bool IsAlive(Guid id)
  {
    if (this.ActiveWebSockets.ContainsKey(id))
      return this.ActiveWebSockets[id].IsAlive;
    throw new Exception("No Websocket exists with the Id " + id.ToString());
  }
}
