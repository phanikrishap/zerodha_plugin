using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ServerTimeResponse : IResponse
{
  [DataMember(Order = 1)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime ServerTime { get; set; }
}

