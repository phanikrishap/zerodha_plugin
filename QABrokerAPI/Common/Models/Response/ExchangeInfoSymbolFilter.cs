using QABrokerAPI.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  [JsonConverter(typeof (StringEnumConverter))]
  public ExchangeInfoSymbolFilterType FilterType { get; set; }
}

