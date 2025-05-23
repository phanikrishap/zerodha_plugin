using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class CompressedAggregateTradeResponse : IResponse
{
  [DataMember(Order = 1)]
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
  [JsonProperty("T")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime Timestamp { get; set; }

  [DataMember(Order = 7)]
  [JsonProperty("m")]
  public bool WasBuyerMaker { get; set; }

  [DataMember(Order = 8)]
  [JsonProperty("M")]
  public bool WasBestPriceMatch { get; set; }
}

