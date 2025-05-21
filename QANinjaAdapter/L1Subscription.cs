// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.L1Subscription
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using System;
using System.Collections.Generic;

#nullable disable
namespace QANinjaAdapter;

public class L1Subscription
{
  public SortedList<Instrument, Action<MarketDataType, double, long, DateTime, long>> L1Callbacks = new SortedList<Instrument, Action<MarketDataType, double, long, DateTime, long>>();

    public int PreviousVolume { get; set; }
    public Instrument Instrument { get; set; }
}
