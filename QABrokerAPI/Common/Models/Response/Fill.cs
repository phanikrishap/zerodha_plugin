using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class Fill
{
  [DataMember(Order = 1)]
  public Decimal Price { get; set; }

  [DataMember(Order = 2)]
  [JsonProperty(PropertyName = "qty")]
  public Decimal Quantity { get; set; }

  [DataMember(Order = 3)]
  public Decimal Commission { get; set; }

  [DataMember(Order = 5)]
  public string CommissionAsset { get; set; }
}

