// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Converter.StringDecimalConverter
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using Newtonsoft.Json;
using System;
using System.Globalization;

#nullable disable
namespace QABrokerAPI.Common.Converter;

public class StringDecimalConverter : JsonConverter
{
  public override bool CanRead => false;

  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof (Decimal) || objectType == typeof (Decimal?);
  }

  public override object ReadJson(
    JsonReader reader,
    Type objectType,
    object existingValue,
    JsonSerializer serializer)
  {
    throw new NotImplementedException();
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    writer.WriteValue(((Decimal) value).ToString((IFormatProvider) CultureInfo.InvariantCulture));
  }
}
