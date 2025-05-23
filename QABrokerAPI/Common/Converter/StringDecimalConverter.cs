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

