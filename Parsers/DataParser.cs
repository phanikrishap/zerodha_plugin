// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Parsers.DataParser
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using QANinjaAdapter.Classes.Binance.Klines;
using QANinjaAdapter.Classes.Binance.Symbols;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

#nullable disable
namespace QANinjaAdapter.Parsers;

public static class DataParser
{
  public static SymbolObject[] ParseSymbols(string data)
  {
    return JsonConvert.DeserializeObject<SymbolsRoot>(data).Symbols;
  }

  public static Kline[] ParseKlinesEndpointData(string data)
  {
    List<Kline> klineList = new List<Kline>();
    foreach (JToken jtoken1 in JsonConvert.DeserializeObject<JArray>(data))
    {
      if (jtoken1.Type == JTokenType.Array)
      {
        Kline kline = new Kline();
        JArray jarray = JsonConvert.DeserializeObject<JArray>(jtoken1.ToString());
        int num = 0;
        foreach (JToken jtoken2 in jarray)
        {
          switch (num)
          {
            case 0:
              kline.OpenTime = jtoken2.Value<long>();
              break;
            case 1:
              kline.Open = jtoken2.Value<double>();
              break;
            case 2:
              kline.High = jtoken2.Value<double>();
              break;
            case 3:
              kline.Low = jtoken2.Value<double>();
              break;
            case 4:
              kline.Close = jtoken2.Value<double>();
              break;
            case 5:
              kline.Volume = jtoken2.Value<double>();
              break;
            case 6:
              kline.CloseTime = jtoken2.Value<long>();
              break;
            case 7:
              kline.QuoteAssetVolume = jtoken2.Value<double>();
              break;
            case 8:
              kline.NumberOfTrades = jtoken2.Value<int>();
              break;
            case 9:
              kline.TakerBuyBase = jtoken2.Value<double>();
              break;
            case 10:
              kline.TakerBuyQuote = jtoken2.Value<double>();
              break;
            case 11:
              kline.Ignore = jtoken2.Value<double>();
              break;
          }
          ++num;
        }
        klineList.Add(kline);
      }
    }
    return klineList.ToArray();
  }
}
