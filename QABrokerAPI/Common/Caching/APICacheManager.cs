using QABrokerAPI.Common.Caching.Interfaces;
using QABrokerAPI.Common.Utility;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

#nullable disable
namespace QABrokerAPI.Common.Caching.Interfaces;

public class APICacheManager : IAPICacheManager
{
  private readonly object _lockObject = new object();
  private readonly MemoryCache _cache;
  private readonly IList<string> _cacheKeysList;
  private TimeSpan _defaultExpiryTimespan = new TimeSpan(0, 30, 0);

  public APICacheManager()
  {
    this._cache = new MemoryCache((IOptions<MemoryCacheOptions>) new MemoryCacheOptions());
    this._cacheKeysList = (IList<string>) new List<string>();
  }

  public void Add<T>(T obj, string key, TimeSpan expiry = default (TimeSpan)) where T : class
  {
    Guard.AgainstNullOrEmpty(key);
    if (expiry == new TimeSpan())
      expiry = this._defaultExpiryTimespan;
    if (this.Contains(key))
      return;
    lock (this._lockObject)
    {
      if (this.Contains(key.ToLower()))
        return;
      this._cacheKeysList.Add(key.ToLower());
      this._cache.Set<T>((object) key.ToLower(), obj, new DateTimeOffset(DateTime.UtcNow.Add(expiry)));
    }
  }

  public T Get<T>(string key) where T : class => this._cache.Get((object) key.ToLower()) as T;

  public bool Contains(string key)
  {
    return !string.IsNullOrEmpty(key) && this._cache.Get((object) key.ToLower()) != null;
  }

  public void Remove(string key)
  {
    if (!this.Contains(key))
      return;
    lock (this._lockObject)
    {
      if (!this.Contains(key.ToLower()))
        return;
      this._cache.Remove((object) key.ToLower());
    }
  }

  public void RemoveAll()
  {
    foreach (string cacheKeys in (IEnumerable<string>) this._cacheKeysList)
    {
      Guard.AgainstNullOrEmpty(cacheKeys);
      this.Remove(cacheKeys);
    }
  }
}
