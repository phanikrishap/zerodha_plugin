using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbol
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public string Status { get; set; }

  [DataMember(Order = 3)]
  public string BaseAsset { get; set; }

  [DataMember(Order = 4)]
  public int BaseAssetPrecision { get; set; }

  [DataMember(Order = 5)]
  public string QuoteAsset { get; set; }

  [DataMember(Order = 6)]
  public int QuotePrecision { get; set; }

  [DataMember(Order = 7)]
  [JsonProperty(ItemConverterType = typeof (StringEnumConverter))]
  public List<ExchangeInfoOrderType> OrderTypes { get; set; }

  [DataMember(Order = 8)]
  public bool IcebergAllowed { get; set; }

  [DataMember(Order = 9)]
  [JsonProperty(ItemConverterType = typeof (ExchangeInfoSymbolFilterConverter))]
  public List<ExchangeInfoSymbolFilter> Filters { get; set; }
}

