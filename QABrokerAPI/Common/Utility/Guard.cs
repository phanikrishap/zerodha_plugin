// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Utility.Guard
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System;

#nullable disable
namespace QABrokerAPI.Common.Utility;

public class Guard
{
  public static void AgainstNullOrEmpty(string param, string name = null)
  {
    if (string.IsNullOrEmpty(param))
      throw new ArgumentNullException(name ?? "The Guarded argument was null or empty.");
  }

  public static void AgainstNull(object param, string name = null)
  {
    if (param == null)
      throw new ArgumentNullException(name ?? "The Guarded argument was null.");
  }

  public static void AgainstDateTimeMin(DateTime param, string name = null)
  {
    if (param == DateTime.MinValue)
      throw new ArgumentNullException(name ?? "The Guarded argument was DateTime min.");
  }
}
