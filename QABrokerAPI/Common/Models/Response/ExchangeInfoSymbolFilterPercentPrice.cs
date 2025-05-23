using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterPercentPrice : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public Decimal MultiplierUp { get; set; }

  [DataMember(Order = 2)]
  public Decimal MultiplierDown { get; set; }

  [DataMember(Order = 3)]
  public Decimal AvgPriceMins { get; set; }
}

