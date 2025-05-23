using QABrokerAPI.Common.Interfaces;
using log4net;

#nullable disable
namespace QABrokerAPI.Zerodha.Websockets;

public class InstanceBrokerWebSocketClient(IBrokerClient brokerClient, ILog logger = null) : 
  AbstractBrokerWebSocketClient(brokerClient, logger),
  IBrokerWebSocketClient
{
}

