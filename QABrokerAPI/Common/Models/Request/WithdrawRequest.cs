using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Request.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class WithdrawRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Asset { get; set; }

  [DataMember(Order = 2)]
  public string Address { get; set; }

  [DataMember(Order = 3)]
  public string AddressTag { get; set; }

  [DataMember(Order = 4)]
  [JsonConverter(typeof (StringDecimalConverter))]
  public Decimal Amount { get; set; }

  [DataMember(Order = 5)]
  public string Name { get; set; }
}

