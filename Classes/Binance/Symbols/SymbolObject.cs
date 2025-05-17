// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Classes.Binance.Symbols.SymbolObject
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using QABrokerAPI.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable
namespace QANinjaAdapter.Classes.Binance.Symbols;

public class SymbolObject
{
  public string Symbol { get; set; }

  public string Status { get; set; }

  public string BaseAsset { get; set; }

  public int BaseAssetPrecision { get; set; }

  public string QuoteAsset { get; set; }

  public int QuotePrecision { get; set; }

  public int QuoteAssetPrecision { get; set; }

  public int BaseCommissionPrecision { get; set; }

  public int QuoteCommissionPrecision { get; set; }

  public string[] OrderTypes { get; set; }

  public bool IcebergAllowed { get; set; }

  public bool OcoAllowed { get; set; }

  public bool QuoteOrderQtyMarketAllowed { get; set; }

  public bool IsSpotTradingAllowed { get; set; }

  public bool IsMarginTradingAllowed { get; set; }

  public Filter[] Filters { get; set; }

  public string[] Permissions { get; set; }

  public MarketType MarketType { get; set; }

  public string TickSize
  {
    get
    {
      double num = 0.0;
      if (this.Filters != null && this.Filters.Length != 0)
      {
        Filter filter = ((IEnumerable<Filter>) this.Filters).FirstOrDefault<Filter>((Func<Filter, bool>) (x => x.FilterType == "PRICE_FILTER"));
        if (filter != null)
          num = filter.TickSize;
      }
      return $"{num:F20}";
    }
  }
}
