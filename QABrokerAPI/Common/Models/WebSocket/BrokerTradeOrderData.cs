// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.BinanceTradeOrderData
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerTradeOrderData : ISymbolWebSocketResponse, IWebSocketResponse
{
  [JsonProperty(PropertyName = "e")]
  public string EventType { get; set; }

  [JsonProperty(PropertyName = "E")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EventTime { get; set; }

  [JsonProperty(PropertyName = "s")]
  public string Symbol { get; set; }

  [JsonProperty(PropertyName = "c")]
  public string NewClientOrderId { get; set; }

  [JsonProperty(PropertyName = "S")]
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderSide Side { get; set; }

  [JsonProperty(PropertyName = "o")]
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderType Type { get; set; }

  [JsonProperty(PropertyName = "O")]
  public string DEPRECATED_FIELD_TYPE { get; set; }

  [JsonProperty(PropertyName = "f")]
  [JsonConverter(typeof (StringEnumConverter))]
  public TimeInForce TimeInForce { get; set; }

  [JsonProperty(PropertyName = "q")]
  public Decimal Quantity { get; set; }

  [JsonProperty(PropertyName = "p")]
  public Decimal Price { get; set; }

  [JsonProperty(PropertyName = "P")]
  public double P { get; set; }

  [JsonProperty(PropertyName = "F")]
  public double F { get; set; }

  [JsonProperty(PropertyName = "g")]
  public string G { get; set; }

  [JsonProperty(PropertyName = "C")]
  public string C { get; set; }

  [JsonProperty(PropertyName = "x")]
  [JsonConverter(typeof (StringEnumConverter))]
  public ExecutionType ExecutionType { get; set; }

  [JsonProperty(PropertyName = "X")]
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderStatus OrderStatus { get; set; }

  [JsonProperty(PropertyName = "r")]
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderRejectReason OrderRejectReason { get; set; }

  [JsonProperty(PropertyName = "i")]
  public long OrderId { get; set; }

  [JsonProperty(PropertyName = "I")]
  public string DEPRECATED_FIELD_ORDERID { get; set; }

  [JsonProperty(PropertyName = "l")]
  public Decimal QuantityOfLastFilledTrade { get; set; }

  [JsonProperty(PropertyName = "z")]
  public Decimal AccumulatedQuantityOfFilledTradesThisOrder { get; set; }

  [JsonProperty(PropertyName = "L")]
  public Decimal PriceOfLastFilledTrade { get; set; }

  [JsonProperty(PropertyName = "n")]
  public Decimal Commission { get; set; }

  [JsonProperty(PropertyName = "N")]
  public string AssetCommissionTakenFrom { get; set; }

  [JsonProperty(PropertyName = "T")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime TimeStamp { get; set; }

  [JsonProperty(PropertyName = "t")]
  public long TradeId { get; set; }

  [JsonProperty(PropertyName = "w")]
  public bool w { get; set; }

  [JsonProperty(PropertyName = "m")]
  public bool IsBuyerMaker { get; set; }

  [JsonProperty(PropertyName = "M")]
  public bool M { get; set; }
}
