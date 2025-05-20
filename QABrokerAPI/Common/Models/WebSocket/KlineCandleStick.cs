// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.KlineCandleStick
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

public class KlineCandleStick
{
  public Decimal Open { get; set; }

  public Decimal High { get; set; }

  public Decimal Low { get; set; }

  public Decimal Close { get; set; }

  public Decimal Volume { get; set; }
}
