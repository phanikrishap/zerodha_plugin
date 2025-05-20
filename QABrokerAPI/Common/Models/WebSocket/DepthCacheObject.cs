// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.DepthCacheObject
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System;
using System.Collections.Generic;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

public class DepthCacheObject
{
  public Dictionary<Decimal, Decimal> Asks { get; set; }

  public Dictionary<Decimal, Decimal> Bids { get; set; }
}
