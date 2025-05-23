using Newtonsoft.Json;
using System;
using System.Xml;

#nullable disable
namespace QABrokerAPI.Common.Converter;

public class TimeSpanConverter : JsonConverter
{
  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    string str = XmlConvert.ToString((TimeSpan) value);
    serializer.Serialize(writer, (object) str);
  }

  public override object ReadJson(
    JsonReader reader,
    Type objectType,
    object existingValue,
    JsonSerializer serializer)
  {
    if (reader.TokenType == JsonToken.Null)
      return (object) null;
    string s = serializer.Deserialize<string>(reader);
    return s == string.Empty ? (object) TimeSpan.MinValue : (object) XmlConvert.ToTimeSpan(s);
  }

  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof (TimeSpan) || objectType == typeof (TimeSpan?);
  }
}

