using QABrokerAPI.Common.Converter;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class AccountTradeReponse
{
  [DataMember(Order = 1)]
  public long Id { get; set; }

  [DataMember(Order = 2)]
  public Decimal Price { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "qty")]
  public Decimal Quantity { get; set; }

  [DataMember(Order = 4)]
  public Decimal Commission { get; set; }

  [DataMember(Order = 5)]
  public string CommissionAsset { get; set; }

  [DataMember(Order = 5)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime Time { get; set; }

  [DataMember(Order = 6)]
  public bool IsBuyer { get; set; }

  [DataMember(Order = 7)]
  public bool IsMaker { get; set; }

  [DataMember(Order = 8)]
  public bool IsBestMatch { get; set; }

  [DataMember(Order = 9)]
  public long OrderId { get; set; }
}

