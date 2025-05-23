using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerPartialDepthData : IWebSocketResponse
{
  [DataMember(Order = 1)]
  [JsonProperty(PropertyName = "stream")]
  public string Stream { get; set; }

  [DataMember(Order = 2)]
  [JsonProperty(PropertyName = "data")]
  public BrokerPartialData Data { get; set; }

  public string EventType
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }

  public DateTime EventTime
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }
}

