// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.ResultCreateOrderResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Response.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ResultCreateOrderResponse : BaseCreateOrderResponse
{
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
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderSide Side { get; set; }

  [DataMember(Order = 9)]
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderType Type { get; set; }

  [DataMember(Order = 10)]
  [JsonConverter(typeof (StringEnumConverter))]
  public QABrokerAPI.Common.Enums.TimeInForce? TimeInForce { get; set; }
}
