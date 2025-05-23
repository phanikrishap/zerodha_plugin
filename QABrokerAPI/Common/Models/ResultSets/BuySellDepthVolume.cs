using System;

#nullable disable
namespace QABrokerAPI.Common.Models.ResultSets;

public class BuySellDepthVolume
{
  public Decimal BidBase { get; set; }

  public Decimal AskBase { get; set; }

  public Decimal BidQuantity { get; set; }

  public Decimal AskQuantity { get; set; }
}

