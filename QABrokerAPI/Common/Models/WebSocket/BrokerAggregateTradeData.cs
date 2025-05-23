// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.BinanceAggregateTradeData
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
public class BrokerAggregateTradeData : ISymbolWebSocketResponse, IWebSocketResponse
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

  [DataMember(Order = 4)]
  [JsonProperty(PropertyName = "a")]
  public long AggregateTradeId { get; set; }

  [DataMember(Order = 2)]
  [JsonProperty(PropertyName = "p")]
  public Decimal Price { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "q")]
  public Decimal Quantity { get; set; }

  [DataMember(Order = 4)]
  [JsonProperty(PropertyName = "f")]
  public long FirstTradeId { get; set; }

  [DataMember(Order = 5)]
  [JsonProperty(PropertyName = "l")]
  public long LastTradeId { get; set; }

  [DataMember(Order = 6)]
  [JsonProperty(PropertyName = "T")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime TradeTime { get; set; }

  [DataMember(Order = 7)]
  [JsonProperty(PropertyName = "m")]
  public bool WasBuyerMaker { get; set; }

  [DataMember(Order = 7)]
  [JsonProperty(PropertyName = "M")]
  public bool WasBestPriceMatch { get; set; }
}
