using QABrokerAPI.Common.Models.ResultSets;
using QABrokerAPI.Common.Models.WebSocket;
using System;
using System.Collections.Generic;

#nullable disable
namespace QABrokerAPI;

public static class ResultTransformations
{
  public static BuySellDepthVolume CalculateTradeVolumeFromDepth(
    string symbol,
    Dictionary<string, DepthCacheObject> depthCacheObject)
  {
    DepthCacheObject depthCacheObject1 = depthCacheObject.ContainsKey(symbol) ? depthCacheObject[symbol] : throw new Exception($"No such symbol found in DepthCache: '{symbol}'");
    Decimal num1 = 0M;
    Decimal num2 = 0M;
    Decimal num3 = 0M;
    Decimal num4 = 0M;
    foreach (Decimal key in depthCacheObject1.Bids.Keys)
    {
      Decimal bid = depthCacheObject1.Bids[key];
      num2 += num1 * key;
      num1 += bid;
    }
    foreach (Decimal key in depthCacheObject1.Asks.Keys)
    {
      Decimal ask = depthCacheObject1.Asks[key];
      num3 += num4 * key;
      num4 += ask;
    }
    return new BuySellDepthVolume()
    {
      AskQuantity = num4,
      AskBase = num3,
      BidBase = num2,
      BidQuantity = num1
    };
  }
}

