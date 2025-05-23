// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.Abstract.BaseCreateOrderResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Abstract;

[DataContract]
public abstract class BaseCreateOrderResponse : IResponse
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public long OrderId { get; set; }

  [DataMember(Order = 3)]
  public string ClientOrderId { get; set; }

  [DataMember(Order = 4)]
  [JsonProperty("transactTime")]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime TransactionTime { get; set; }
}
