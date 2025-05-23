// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.SymbolOrderBookResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class SymbolOrderBookResponse
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public Decimal BidPrice { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "bidQty")]
  public Decimal BidQuantity { get; set; }

  [DataMember(Order = 4)]
  public Decimal AskPrice { get; set; }

  [DataMember(Order = 5)]
  [JsonProperty(PropertyName = "askQty")]
  public Decimal AskQuantity { get; set; }
}
