using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

public class WithdrawResponse : IConfirmationResponse, IResponse
{
  [DataMember(Order = 1)]
  [JsonProperty(PropertyName = "msg")]
  public string Message { get; set; }

  [DataMember(Order = 2)]
  public bool Success { get; set; }

  [DataMember(Order = 3)]
  public string Id { get; set; }
}

