using System;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

public class KlineCandleStick
{
  public Decimal Open { get; set; }

  public Decimal High { get; set; }

  public Decimal Low { get; set; }

  public Decimal Close { get; set; }

  public Decimal Volume { get; set; }
}

