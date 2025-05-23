// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.BinanceAccountUpdateData
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerAccountUpdateData : IWebSocketResponse
{
  [DataMember(Order = 1)]
  [JsonProperty(PropertyName = "e")]
  public string EventType { get; set; }

  [DataMember(Order = 2)]
  [JsonProperty(PropertyName = "E")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EventTime { get; set; }

  [DataMember(Order = 3)]
  [JsonProperty(PropertyName = "m")]
  public int M { get; set; }

  [DataMember(Order = 4)]
  [JsonProperty(PropertyName = "t")]
  public int t { get; set; }

  [DataMember(Order = 5)]
  [JsonProperty(PropertyName = "b")]
  public int B { get; set; }

  [DataMember(Order = 6)]
  [JsonProperty(PropertyName = "s")]
  public int S { get; set; }

  [DataMember(Order = 7)]
  [JsonProperty(PropertyName = "T")]
  public bool T { get; set; }

  [DataMember(Order = 8)]
  [JsonProperty(PropertyName = "W")]
  public bool W { get; set; }

  [DataMember(Order = 9)]
  [JsonProperty(PropertyName = "D")]
  public bool D { get; set; }

  [DataMember(Order = 10)]
  [JsonProperty(PropertyName = "B")]
  public List<BalanceResponseData> Balances { get; set; }
}
