using QABrokerAPI.Common.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using WebSocketSharp;

#nullable disable
namespace QABrokerAPI.Binance.Websockets;

public class DisposableBrokerWebSocketClient(IBrokerClient binanceClient, ILog logger = null) : 
  AbstractBrokerWebSocketClient(binanceClient, logger),
  IDisposable,
  IBrokerWebSocketClient
{
  public void Dispose()
  {
    this.Dispose(true);
    GC.SuppressFinalize((object) this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposing)
      return;
    this.AllSockets.ForEach((Action<BrokerWebSocket>) (ws =>
    {
      if (!ws.IsAlive)
        return;
      ws.Close(CloseStatusCode.Normal);
    }));
    this.AllSockets = new List<BrokerWebSocket>();
    this.ActiveWebSockets = new Dictionary<Guid, BrokerWebSocket>();
  }
}

