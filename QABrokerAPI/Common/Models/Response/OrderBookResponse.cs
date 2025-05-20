// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.OrderBookResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

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
