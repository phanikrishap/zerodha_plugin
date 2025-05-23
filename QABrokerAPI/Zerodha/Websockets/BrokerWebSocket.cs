using System;
using WebSocketSharp;

#nullable disable
namespace QABrokerAPI.Zerodha.Websockets;

public class BrokerWebSocket : WebSocket
{
  public Guid Id;

  public BrokerWebSocket(string url, params string[] protocols)
    : base(url, protocols)
  {
    this.Id = Guid.NewGuid();
  }
}

