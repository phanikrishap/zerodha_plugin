using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
[JsonConverter(typeof (KlineCandleSticksConverter))]
public class KlineCandleStickResponse : IResponse
{
  [DataMember(Order = 1)]
  public DateTime OpenTime { get; set; }

  [DataMember(Order = 2)]
  public Decimal Open { get; set; }

  [DataMember(Order = 3)]
  public Decimal High { get; set; }

  [DataMember(Order = 4)]
  public Decimal Low { get; set; }

  [DataMember(Order = 5)]
  public Decimal Close { get; set; }

  [DataMember(Order = 6)]
  public Decimal Volume { get; set; }

  [DataMember(Order = 7)]
  public DateTime CloseTime { get; set; }

  [DataMember(Order = 7)]
  public Decimal QuoteAssetVolume { get; set; }

  [DataMember(Order = 7)]
  public int NumberOfTrades { get; set; }

  [DataMember(Order = 8)]
  public Decimal TakerBuyBaseAssetVolume { get; set; }

  [DataMember(Order = 9)]
  public Decimal TakerBuyQuoteAssetVolume { get; set; }
}

