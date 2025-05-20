// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.BinanceTradeData
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerTradeData : ISymbolWebSocketResponse, IWebSocketResponse
{
  [JsonProperty(PropertyName = "e")]
  [DataMember(Order = 1)]
  public string EventType { get; set; }

  [JsonProperty(PropertyName = "E")]
  [DataMember(Order = 2)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EventTime { get; set; }

  [JsonProperty(PropertyName = "s")]
  [DataMember(Order = 3)]
  public string Symbol { get; set; }

  [JsonProperty(PropertyName = "p")]
  [DataMember(Order = 4)]
  public Decimal PriceChange { get; set; }

  [JsonProperty(PropertyName = "P")]
  [DataMember(Order = 5)]
  public Decimal PriceChangePercent { get; set; }

  [JsonProperty(PropertyName = "w")]
  [DataMember(Order = 6)]
  public Decimal WeightedAveragePrice { get; set; }

  [JsonProperty(PropertyName = "x")]
  [DataMember(Order = 7)]
  public Decimal FirstTrade { get; set; }

  [JsonProperty(PropertyName = "c")]
  [DataMember(Order = 8)]
  public Decimal LastPrice { get; set; }

  [JsonProperty(PropertyName = "Q")]
  [DataMember(Order = 9)]
  public Decimal LastQuantity { get; set; }

  [JsonProperty(PropertyName = "b")]
  [DataMember(Order = 10)]
  public Decimal BestBidPrice { get; set; }

  [JsonProperty(PropertyName = "B")]
  [DataMember(Order = 11)]
  public Decimal BestBidQuantity { get; set; }

  [JsonProperty(PropertyName = "a")]
  [DataMember(Order = 12)]
  public Decimal BestAskPrice { get; set; }

  [JsonProperty(PropertyName = "A")]
  [DataMember(Order = 13)]
  public Decimal BestAskQuantity { get; set; }

  [JsonProperty(PropertyName = "o")]
  [DataMember(Order = 14)]
  public Decimal OpenPrice { get; set; }

  [JsonProperty(PropertyName = "h")]
  [DataMember(Order = 15)]
  public Decimal HighPrice { get; set; }

  [JsonProperty(PropertyName = "l")]
  [DataMember(Order = 16 /*0x10*/)]
  public Decimal LowPrice { get; set; }

  [JsonProperty(PropertyName = "v")]
  [DataMember(Order = 17)]
  public Decimal TotalTradedBaseAssetVolume { get; set; }

  [JsonProperty(PropertyName = "q")]
  [DataMember(Order = 18)]
  public Decimal TotalTradedQuoteAssetVolume { get; set; }

  [JsonProperty(PropertyName = "O")]
  [DataMember(Order = 19)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime StatisticsOpenTime { get; set; }

  [JsonProperty(PropertyName = "C")]
  [DataMember(Order = 20)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime StatisticsCloseTime { get; set; }

  [JsonProperty(PropertyName = "F")]
  [DataMember(Order = 21)]
  public long FirstTradeId { get; set; }

  [JsonProperty(PropertyName = "L")]
  [DataMember(Order = 22)]
  public long LastTradeId { get; set; }

  [JsonProperty(PropertyName = "n")]
  [DataMember(Order = 23)]
  public long TotalNumberOfTrades { get; set; }
}
