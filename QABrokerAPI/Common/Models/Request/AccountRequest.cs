using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Request.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class AccountRequest : IRequest
{
  [DataMember(Order = 1)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime TimeStamp { get; set; }
}

