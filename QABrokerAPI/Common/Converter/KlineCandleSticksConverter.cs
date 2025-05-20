// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Converter.KlineCandleSticksConverter
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

#nullable disable
namespace QABrokerAPI.Common.Converter;

public class KlineCandleSticksConverter : JsonConverter
{
  private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    throw new NotImplementedException();
  }

  public override object ReadJson(
    JsonReader reader,
    Type objectType,
    object existingValue,
    JsonSerializer serializer)
  {
    JArray source = JArray.Load(reader);
    return (object) new KlineCandleStickResponse()
    {
      OpenTime = KlineCandleSticksConverter.Epoch.AddMilliseconds((double) (long) source.ElementAt<JToken>(0)),
      Open = (Decimal) source.ElementAt<JToken>(1),
      High = (Decimal) source.ElementAt<JToken>(2),
      Low = (Decimal) source.ElementAt<JToken>(3),
      Close = (Decimal) source.ElementAt<JToken>(4),
      Volume = (Decimal) source.ElementAt<JToken>(5),
      CloseTime = KlineCandleSticksConverter.Epoch.AddMilliseconds((double) (long) source.ElementAt<JToken>(6)),
      QuoteAssetVolume = (Decimal) source.ElementAt<JToken>(7),
      NumberOfTrades = (int) source.ElementAt<JToken>(8),
      TakerBuyBaseAssetVolume = (Decimal) source.ElementAt<JToken>(9),
      TakerBuyQuoteAssetVolume = (Decimal) source.ElementAt<JToken>(10)
    };
  }

  public override bool CanConvert(Type objectType) => throw new NotImplementedException();
}
