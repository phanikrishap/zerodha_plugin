// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Classes.Binance.Symbols.RateLimit
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

#nullable disable
namespace QANinjaAdapter.Classes.Binance.Symbols;

public class RateLimit
{
  public string RateLimitType { get; set; }

  public string Interval { get; set; }

  public int IntervalNum { get; set; }

  public int Limit { get; set; }
}
