// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.SymbolPriceChangeTickerResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class SymbolPriceChangeTickerResponse
{
  [DataMember(Order = 1)]
  public Decimal PriceChange { get; set; }

  [DataMember(Order = 2)]
  public Decimal PriceChangePercent { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "weightedAvgPrice")]
  public Decimal WeightedAveragePercent { get; set; }

  [DataMember(Order = 4)]
  [JsonProperty(PropertyName = "prevClosePrice")]
  public Decimal PreviousClosePrice { get; set; }

  [DataMember(Order = 5)]
  public Decimal LastPrice { get; set; }

  [DataMember(Order = 6)]
  public Decimal BidPrice { get; set; }

  [DataMember(Order = 7)]
  public Decimal AskPrice { get; set; }

  [DataMember(Order = 8)]
  public Decimal OpenPrice { get; set; }

  [DataMember(Order = 9)]
  public Decimal HighPrice { get; set; }

  [DataMember(Order = 10)]
  public Decimal LowPrice { get; set; }

  [DataMember(Order = 11)]
  public Decimal Volume { get; set; }

  [DataMember(Order = 12)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime OpenTime { get; set; }

  [DataMember(Order = 13)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime CloseTime { get; set; }

  [DataMember(Order = 14)]
  [JsonProperty(PropertyName = "firstId")]
  public long FirstTradeId { get; set; }

  [DataMember(Order = 15)]
  [JsonProperty(PropertyName = "lastId")]
  public long LastId { get; set; }

  [DataMember(Order = 16 /*0x10*/)]
  [JsonProperty(PropertyName = "count")]
  public int TradeCount { get; set; }
}
