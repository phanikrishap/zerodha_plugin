using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.WebSocket;
using System;
using System.Threading.Tasks;

#nullable disable
namespace QABrokerAPI.Binance.Websockets;

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

