using System;

#nullable disable
namespace QABrokerAPI.Binance.Websockets;

public class WebSocketConnectionFunc
{
  public Func<bool> ExitFunction;

  public int Timeout { get; }

  public bool IsTimout => this.Timeout > 0;

  public WebSocketConnectionFunc(int timeout = 5000) => this.Timeout = timeout;

  public WebSocketConnectionFunc(Func<bool> exitFunction) => this.ExitFunction = exitFunction;
}
