using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterMaxNumAlgoOrders : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public Decimal MaxNumAlgoOrders { get; set; }
}

