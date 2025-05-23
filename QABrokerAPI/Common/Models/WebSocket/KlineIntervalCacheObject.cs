using System.Collections.Generic;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

public class KlineIntervalCacheObject
{
  public Dictionary<long, KlineCandleStick> TimeKlineDictionary { get; set; }
}

