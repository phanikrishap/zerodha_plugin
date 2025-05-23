using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class CancelOrderResponse : IResponse
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public long OrderId { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "origClientOrderId")]
  public string OriginalClientOrderId { get; set; }

  [DataMember(Order = 4)]
  public string ClientOrderId { get; set; }
}

