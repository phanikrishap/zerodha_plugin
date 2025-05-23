using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class OrderBookResponse : IResponse
{
  [DataMember(Order = 1)]
  public long LastUpdateId { get; set; }

  [DataMember(Order = 2)]
  [JsonConverter(typeof (TraderPriceConverter))]
  public List<TradeResponse> Bids { get; set; }

  [DataMember(Order = 3)]
  [JsonConverter(typeof (TraderPriceConverter))]
  public List<TradeResponse> Asks { get; set; }
}

