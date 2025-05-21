// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Classes.Binance.Symbols.Filter
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

#nullable disable
namespace QANinjaAdapter.Classes.Binance.Symbols;

public class Filter
{
  public string FilterType { get; set; }

  public double MinPrice { get; set; }

  public double MaxPrice { get; set; }

  public double TickSize { get; set; }

  public double MultiplierUp { get; set; }

  public double MultiplierDown { get; set; }

  public double AvgPriceMins { get; set; }

  public double MinQty { get; set; }

  public double MaxQty { get; set; }

  public double StepSize { get; set; }

  public double MinNotional { get; set; }

  public bool ApplyToMarket { get; set; }

  public int Limit { get; set; }

  public int MaxNumAlgoOrders { get; set; }

  public int MaxNumOrders { get; set; }
}
