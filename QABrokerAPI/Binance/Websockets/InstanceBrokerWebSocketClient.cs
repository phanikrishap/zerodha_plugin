using QABrokerAPI.Common.Interfaces;
using log4net;

#nullable disable
namespace QABrokerAPI.Binance.Websockets;

public class InstanceBrokerWebSocketClient(IBrokerClient brokerClient, ILog logger = null) : 
  AbstractBrokerWebSocketClient(brokerClient, logger),
  IBrokerWebSocketClient
{
}

