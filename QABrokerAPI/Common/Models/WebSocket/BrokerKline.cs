// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.WebSocket.BinanceKline
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket;

[DataContract]
public class BrokerKline
{
  [JsonProperty(PropertyName = "t")]
  [DataMember(Order = 1)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime StartTime { get; set; }

  [JsonProperty(PropertyName = "T")]
  [DataMember(Order = 2)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime EndTime { get; set; }

  [JsonProperty(PropertyName = "s")]
  [DataMember(Order = 3)]
  public string Symbol { get; set; }

  [JsonProperty(PropertyName = "i")]
  [DataMember(Order = 4)]
  [JsonConverter(typeof (StringEnumConverter))]
  public KlineInterval Interval { get; set; }

  [JsonProperty(PropertyName = "f")]
  [DataMember(Order = 5)]
  public long FirstTradeId { get; set; }

  [JsonProperty(PropertyName = "L")]
  [DataMember(Order = 6)]
  public long LastTradeId { get; set; }

  [JsonProperty(PropertyName = "o")]
  [DataMember(Order = 7)]
  public Decimal Open { get; set; }

  [JsonProperty(PropertyName = "c")]
  [DataMember(Order = 8)]
  public Decimal Close { get; set; }

  [JsonProperty(PropertyName = "h")]
  [DataMember(Order = 9)]
  public Decimal High { get; set; }

  [JsonProperty(PropertyName = "l")]
  [DataMember(Order = 10)]
  public Decimal Low { get; set; }

  [JsonProperty(PropertyName = "v")]
  [DataMember(Order = 11)]
  public Decimal Volume { get; set; }

  [JsonProperty(PropertyName = "n")]
  [DataMember(Order = 12)]
  public int NumberOfTrades { get; set; }

  [JsonProperty(PropertyName = "x")]
  [DataMember(Order = 13)]
  public bool IsBarFinal { get; set; }

  [JsonProperty(PropertyName = "q")]
  [DataMember(Order = 14)]
  public Decimal QuoteVolume { get; set; }

  [JsonProperty(PropertyName = "V")]
  [DataMember(Order = 15)]
  public Decimal VolumeOfActivyBuy { get; set; }

  [JsonProperty(PropertyName = "Q")]
  [DataMember(Order = 16 /*0x10*/)]
  public Decimal QuoteVolumeOfActivyBuy { get; set; }
}
