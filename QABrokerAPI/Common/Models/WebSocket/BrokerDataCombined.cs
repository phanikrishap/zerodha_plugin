// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.BinanceDataCombined
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Response;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerDataCombined : IWebSocketResponse
{
  [DataMember(Order = 1)]
  [JsonProperty(PropertyName = "e")]
  public string EventType { get; set; }

  [DataMember(Order = 2)]
  [JsonProperty(PropertyName = "E")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EventTime { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "s")]
  public string Symbol { get; set; }

  [DataMember(Order = 4)]
  [JsonProperty(PropertyName = "u")]
  public long UpdateId { get; set; }

  [DataMember(Order = 5)]
  [JsonProperty(PropertyName = "b")]
  [JsonConverter(typeof (TraderPriceConverter))]
  public List<TradeResponse> BidDepthDeltas { get; set; }

  [DataMember(Order = 6)]
  [JsonProperty(PropertyName = "a")]
  [JsonConverter(typeof (TraderPriceConverter))]
  public List<TradeResponse> AskDepthDeltas { get; set; }
}
