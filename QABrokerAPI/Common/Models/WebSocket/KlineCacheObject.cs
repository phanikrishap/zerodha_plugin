using QABrokerAPI.Common.Enums;
using System.Collections.Generic;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

public class KlineCacheObject
{
  public Dictionary<KlineInterval, KlineIntervalCacheObject> KlineInterDictionary { get; set; }
}

