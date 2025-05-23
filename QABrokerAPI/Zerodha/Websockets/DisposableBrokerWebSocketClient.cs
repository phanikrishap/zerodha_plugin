// Decompiled with JetBrains decompiler
// Type: BinanceExchange.API.Websockets.DisposableBinanceWebSocketClient
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using WebSocketSharp;

#nullable disable
namespace QABrokerAPI.Zerodha.Websockets;

public class DisposableBrokerWebSocketClient(IBrokerClient binanceClient, ILog logger = null) :
  AbstractBrokerWebSocketClient(binanceClient, logger),
  IDisposable,
  IBrokerWebSocketClient
{
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize((object)this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        this.AllSockets.ForEach((Action<BrokerWebSocket>)(ws =>
        {
            if (!ws.IsAlive)
                return;
            ws.Close(CloseStatusCode.Normal);
        }));
        this.AllSockets = new List<BrokerWebSocket>();
        this.ActiveWebSockets = new Dictionary<Guid, BrokerWebSocket>();
    }
}
