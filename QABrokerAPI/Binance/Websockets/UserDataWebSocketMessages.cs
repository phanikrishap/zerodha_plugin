using QABrokerAPI.Common.Models.WebSocket;

#nullable disable
namespace QABrokerAPI.Binance.Websockets;

public class UserDataWebSocketMessages
{
  public BrokerWebSocketMessageHandler<BrokerAccountUpdateData> AccountUpdateMessageHandler { get; set; }

  public BrokerWebSocketMessageHandler<BrokerTradeOrderData> OrderUpdateMessageHandler { get; set; }

  public BrokerWebSocketMessageHandler<BrokerTradeOrderData> TradeUpdateMessageHandler { get; set; }
}

