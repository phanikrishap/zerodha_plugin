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

