// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Converter.EpochTimeConverter
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

#nullable disable
namespace QABrokerAPI.Common.Converter;

public class EpochTimeConverter : DateTimeConverterBase
{
  private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    if ((DateTime) value == DateTime.MinValue)
      writer.WriteNull();
    else
      writer.WriteRawValue(Math.Floor(((DateTime) value - EpochTimeConverter.Epoch).TotalMilliseconds).ToString());
  }

  public override object ReadJson(
    JsonReader reader,
    Type objectType,
    object existingValue,
    JsonSerializer serializer)
  {
    return reader.Value == null ? (object) null : (object) EpochTimeConverter.Epoch.AddMilliseconds((double) (long) reader.Value);
  }
}
