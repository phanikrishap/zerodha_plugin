using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterMinNotional : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public Decimal MinNotional { get; set; }
}

