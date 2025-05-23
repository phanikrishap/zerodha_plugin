// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Extensions.DateTimeExtensions
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System;


namespace QABrokerAPI.Common.Extensions;

public static class DateTimeExtensions
{
  public static long ConvertToUnixTime(this DateTime datetime)
  {
    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    return (long) (datetime - dateTime).TotalMilliseconds;
  }
}
