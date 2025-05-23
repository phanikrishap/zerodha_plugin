using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class TradeDepthDelta
{
  [DataMember(Order = 1)]
  public Decimal Price { get; set; }

  [DataMember(Order = 2)]
  public Decimal Quanity { get; set; }
}

