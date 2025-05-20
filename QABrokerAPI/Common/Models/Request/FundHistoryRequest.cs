// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Request.FundHistoryRequest
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Request.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class FundHistoryRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Asset { get; set; }

  [DataMember(Order = 2)]
  public DepositHistoryStatus? Status { get; set; }

  [DataMember(Order = 3)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime? StartTime { get; set; }

  [DataMember(Order = 4)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime? EndTime { get; set; }
}
