using QABrokerAPI.Common.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable
namespace QABrokerAPI.Common.Converter;

public class TraderPriceConverter : JsonConverter
{
  public override bool CanWrite => false;

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
    JArray jarray = JArray.Load(reader);
    List<TradeResponse> tradeResponseList = new List<TradeResponse>();
    foreach (JToken source in jarray)
    {
      Decimal num1 = source.ElementAt<JToken>(0).ToObject<Decimal>();
      Decimal num2 = source.ElementAt<JToken>(1).ToObject<Decimal>();
      tradeResponseList.Add(new TradeResponse()
      {
        Price = num1,
        Quantity = num2
      });
    }
    return (object) tradeResponseList;
  }

  public override bool CanConvert(Type objectType) => throw new NotImplementedException();
}

