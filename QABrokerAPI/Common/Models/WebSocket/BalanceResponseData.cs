using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BalanceResponseData : IBalanceResponse
{
  [JsonProperty(PropertyName = "a")]
  public string Asset { get; set; }

  [JsonProperty(PropertyName = "f")]
  public Decimal Free { get; set; }

  [JsonProperty(PropertyName = "l")]
  public Decimal Locked { get; set; }
}

