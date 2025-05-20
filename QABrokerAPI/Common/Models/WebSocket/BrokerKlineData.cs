// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.BinanceKlineData
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerKlineData : ISymbolWebSocketResponse, IWebSocketResponse
{
  [JsonProperty(PropertyName = "e")]
  [DataMember(Order = 1)]
  public string EventType { get; set; }

  [JsonProperty(PropertyName = "E")]
  [DataMember(Order = 2)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EventTime { get; set; }

  [JsonProperty(PropertyName = "s")]
  [DataMember(Order = 3)]
  public string Symbol { get; set; }

  [JsonProperty(PropertyName = "K")]
  [DataMember(Order = 4)]
  public BrokerKline Kline { get; set; }
}
