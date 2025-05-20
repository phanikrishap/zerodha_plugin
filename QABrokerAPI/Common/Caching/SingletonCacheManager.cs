namespace QABrokerAPI.Common.Caching.Interfaces;

public static class SingletonCacheManager
{
  public static APICacheManager Instance { get; } = new APICacheManager();
}
