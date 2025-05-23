using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterLotSize : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public Decimal MinQty { get; set; }

  [DataMember(Order = 2)]
  public Decimal MaxQty { get; set; }

  [DataMember(Order = 3)]
  public Decimal StepSize { get; set; }
}

