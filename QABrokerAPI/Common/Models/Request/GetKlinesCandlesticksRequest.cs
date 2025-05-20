// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Request.GetKlinesCandlesticksRequest
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Request.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class GetKlinesCandlesticksRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  [JsonConverter(typeof (StringEnumConverter))]
  public KlineInterval Interval { get; set; }

  [DataMember(Order = 3)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime? StartTime { get; set; }

  [DataMember(Order = 4)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime? EndTime { get; set; }

  [DataMember(Order = 5)]
  public int? Limit { get; set; }
}
