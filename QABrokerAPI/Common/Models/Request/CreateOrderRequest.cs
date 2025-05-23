using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Request.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class CreateOrderRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderSide Side { get; set; }

  [DataMember(Order = 3)]
  [JsonConverter(typeof (StringEnumConverter))]
  public OrderType Type { get; set; }

  [DataMember(Order = 4)]
  [JsonConverter(typeof (StringEnumConverter))]
  public QABrokerAPI.Common.Enums.TimeInForce? TimeInForce { get; set; }

  [DataMember(Order = 5)]
  [JsonConverter(typeof (StringDecimalConverter))]
  public Decimal Quantity { get; set; }

  [DataMember(Order = 6)]
  [JsonConverter(typeof (StringDecimalConverter))]
  public Decimal? Price { get; set; }

  [DataMember(Order = 7)]
  public string NewClientOrderId { get; set; }

  [DataMember(Order = 8)]
  [JsonConverter(typeof (StringDecimalConverter))]
  public Decimal? StopPrice { get; set; }

  [DataMember(Order = 9)]
  [JsonProperty("icebergQty")]
  [JsonConverter(typeof (StringDecimalConverter))]
  public Decimal? IcebergQuantity { get; set; }

  [DataMember(Order = 10)]
  [JsonProperty("newOrderRespType")]
  [JsonConverter(typeof (StringEnumConverter))]
  public NewOrderResponseType NewOrderResponseType { get; set; }
}

