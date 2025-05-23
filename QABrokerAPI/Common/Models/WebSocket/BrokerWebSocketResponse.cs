using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerWebSocketResponse : IWebSocketResponse
{
  [DataMember(Order = 1)]
  [JsonProperty(PropertyName = "e")]
  public string EventType { get; set; }

  [DataMember(Order = 2)]
  [JsonProperty(PropertyName = "E")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EventTime { get; set; }
}

