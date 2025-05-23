using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

#nullable disable
namespace QABrokerAPI.Common.Converter;

public class ExchangeInfoSymbolFilterConverter : JsonConverter
{
  public override bool CanWrite => false;

  public override bool CanConvert(Type objectType) => false;

  public override object ReadJson(
    JsonReader reader,
    Type objectType,
    object existingValue,
    JsonSerializer serializer)
  {
    JObject jobject = JObject.Load(reader);
    ExchangeInfoSymbolFilter infoSymbolFilter = jobject.ToObject<ExchangeInfoSymbolFilter>();
    ExchangeInfoSymbolFilter target = (ExchangeInfoSymbolFilter) null;
    switch (infoSymbolFilter.FilterType)
    {
      case ExchangeInfoSymbolFilterType.PriceFilter:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterPrice();
        break;
      case ExchangeInfoSymbolFilterType.PercentPrice:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterPercentPrice();
        break;
      case ExchangeInfoSymbolFilterType.LotSize:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterLotSize();
        break;
      case ExchangeInfoSymbolFilterType.MinNotional:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterMinNotional();
        break;
      case ExchangeInfoSymbolFilterType.IcebergParts:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterIcebergParts();
        break;
      case ExchangeInfoSymbolFilterType.MarketLotSize:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterMarketLotSize();
        break;
      case ExchangeInfoSymbolFilterType.MaxNumOrders:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterMaxNumOrders();
        break;
      case ExchangeInfoSymbolFilterType.MaxNumAlgoOrders:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterMaxNumAlgoOrders();
        break;
      case ExchangeInfoSymbolFilterType.MaxNumIcebergOrders:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterMaxNumIcebergOrders();
        break;
      case ExchangeInfoSymbolFilterType.MaxPosition:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterMaxPosition();
        break;
      case ExchangeInfoSymbolFilterType.ExchangeMaxNumOrders:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterExchangeMaxNumOrders();
        break;
      case ExchangeInfoSymbolFilterType.ExchangeMaxNumAlgoOrders:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterExchangeMaxNumAlgoOrders();
        break;
      case ExchangeInfoSymbolFilterType.PercentagePrice:
        target = (ExchangeInfoSymbolFilter) new ExchangeInfoSymbolFilterPercentagePrice();
        break;
    }
    serializer.Populate(jobject.CreateReader(), (object) target);
    return (object) target;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    throw new NotImplementedException();
  }
}

