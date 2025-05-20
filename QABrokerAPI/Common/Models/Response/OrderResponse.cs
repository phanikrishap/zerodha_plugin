// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.OrderResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class OrderResponse : IResponse
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public long OrderId { get; set; }

  [DataMember(Order = 3)]
  public string ClientOrderId { get; set; }

  [DataMember(Order = 4)]
  public Decimal Price { get; set; }

  [DataMember(Order = 5)]
  [JsonProperty(PropertyName = "origQty")]
  public Decimal OriginalQuantity { get; set; }

  [DataMember(Order = 6)]
  [JsonProperty(PropertyName = "executedQty")]
  public Decimal ExecutedQuantity { get; set; }

  [JsonConverter(typeof (StringEnumConverter))]
  [DataMember(Order = 7)]
  public OrderStatus Status { get; set; }

  [DataMember(Order = 8)]
  public TimeInForce TimeInForce { get; set; }

  [JsonConverter(typeof (StringEnumConverter))]
  [DataMember(Order = 9)]
  public OrderType Type { get; set; }

  [DataMember(Order = 10)]
  public OrderSide Side { get; set; }

  [DataMember(Order = 11)]
  public Decimal StopPrice { get; set; }

  [DataMember(Order = 12)]
  [JsonProperty(PropertyName = "icebergQty")]
  public Decimal IcebergQuantity { get; set; }

  [DataMember(Order = 13)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime Time { get; set; }

  [DataMember(Order = 14)]
  public bool IsWorking { get; set; }

  [DataMember(Order = 15)]
  [JsonProperty(PropertyName = "cummulativeQuoteQty")]
  public Decimal CummulativeQuoteQuantity { get; set; }
}
