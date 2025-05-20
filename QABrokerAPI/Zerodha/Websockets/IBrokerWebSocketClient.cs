// Decompiled with JetBrains decompiler
// Type: BinanceExchange.API.Websockets.IBinanceWebSocketClient
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.WebSocket;
using System;
using System.Threading.Tasks;

#nullable disable
namespace QABrokerAPI.Zerodha.Websockets;

public interface IBrokerWebSocketClient
{
  Guid ConnectToKlineWebSocket(
    string symbol,
    KlineInterval interval,
    BrokerWebSocketMessageHandler<BrokerKlineData> messageEventHandler);

  Guid ConnectToDepthWebSocket(
    string symbol,
    BrokerWebSocketMessageHandler<BrokerDepthData> messageEventHandler);

  Guid ConnectToTradesWebSocket(
    string symbol,
    BrokerWebSocketMessageHandler<BrokerAggregateTradeData> messageEventHandler);

  Task<Guid> ConnectToUserDataWebSocket(UserDataWebSocketMessages userDataMessageHandlers);

  void CloseWebSocketInstance(Guid id, bool fromError = false);
}
