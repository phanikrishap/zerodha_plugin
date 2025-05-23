
using System;

#nullable disable
namespace QABrokerAPI.Common.Caching.Interfaces;

public interface IAPICacheManager
{
  void Add<T>(T obj, string key, TimeSpan expiry = default (TimeSpan)) where T : class;

  T Get<T>(string key) where T : class;

  bool Contains(string key);

  void Remove(string key);

  void RemoveAll();
}
