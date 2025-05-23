using QABrokerAPI.Common.Models.Response.Interfaces;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class BalanceResponse : IBalanceResponse
{
  [DataMember(Order = 1)]
  public string Asset { get; set; }

  [DataMember(Order = 2)]
  public Decimal Free { get; set; }

  [DataMember(Order = 3)]
  public Decimal Locked { get; set; }
}

