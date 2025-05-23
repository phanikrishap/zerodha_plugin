using QABrokerAPI.Common.Models.Response.Interfaces;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class TradeResponse : IResponse
{
  [DataMember(Order = 1)]
  public Decimal Price { get; set; }

  [DataMember(Order = 2)]
  public Decimal Quantity { get; set; }
}

