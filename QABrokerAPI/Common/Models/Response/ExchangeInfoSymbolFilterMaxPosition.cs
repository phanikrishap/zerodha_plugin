using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterMaxPosition : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public Decimal MaxPosition { get; set; }
}

