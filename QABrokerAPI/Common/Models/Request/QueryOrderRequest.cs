using QABrokerAPI.Common.Models.Request.Interfaces;
using Newtonsoft.Json;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class QueryOrderRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public long? OrderId { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "origClientOrderId")]
  public string OriginalClientOrderId { get; set; }
}

