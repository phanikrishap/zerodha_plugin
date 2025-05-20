// Decompiled with JetBrains decompiler
// Type: BinanceExchange.API.Websockets.BinanceWebSocket
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

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
