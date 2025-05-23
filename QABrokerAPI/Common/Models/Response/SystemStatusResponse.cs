using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class SystemStatusResponse : IResponse
{
  [DataMember(Order = 1)]
  [JsonConverter(typeof (StringEnumConverter))]
  public SystemStatus Status { get; set; }

  [DataMember(Order = 2)]
  [JsonProperty(PropertyName = "msg")]
  public string Message { get; set; }
}

