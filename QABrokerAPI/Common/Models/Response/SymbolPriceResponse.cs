using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class SymbolPriceResponse
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public Decimal Price { get; set; }
}

