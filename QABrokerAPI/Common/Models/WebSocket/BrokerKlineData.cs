using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerKlineData : ISymbolWebSocketResponse, IWebSocketResponse
{
  [JsonProperty(PropertyName = "e")]
  [DataMember(Order = 1)]
  public string EventType { get; set; }

  [JsonProperty(PropertyName = "E")]
  [DataMember(Order = 2)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EventTime { get; set; }

  [JsonProperty(PropertyName = "s")]
  [DataMember(Order = 3)]
  public string Symbol { get; set; }

  [JsonProperty(PropertyName = "K")]
  [DataMember(Order = 4)]
  public BrokerKline Kline { get; set; }
}

