// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.ExchangeInfoSymbolFilterPrice
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterPrice : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public Decimal MinPrice { get; set; }

  [DataMember(Order = 2)]
  public Decimal MaxPrice { get; set; }

  [DataMember(Order = 3)]
  public Decimal TickSize { get; set; }
}
