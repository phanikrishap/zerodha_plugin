using System;
using System.Collections.Generic;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

public class DepthCacheObject
{
  public Dictionary<Decimal, Decimal> Asks { get; set; }

  public Dictionary<Decimal, Decimal> Bids { get; set; }
}

