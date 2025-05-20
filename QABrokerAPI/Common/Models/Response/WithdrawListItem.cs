// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.WithdrawListItem
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Converter;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Response.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class WithdrawListItem : IResponse
{
  [DataMember(Order = 1)]
  public string Id { get; set; }

  [DataMember(Order = 2)]
  public Decimal Amount { get; set; }

  [DataMember(Order = 3)]
  public string Address { get; set; }

  [DataMember(Order = 4)]
  public string AddressTag { get; set; }

  [DataMember(Order = 5)]
  [JsonProperty(PropertyName = "txId")]
  public string TransactionId { get; set; }

  [DataMember(Order = 6)]
  public string Asset { get; set; }

  [DataMember(Order = 7)]
  [JsonConverter(typeof (EpochTimeConverter))]
  public DateTime ApplyTime { get; set; }

  [DataMember(Order = 8)]
  public WithdrawHistoryStatus Status { get; set; }
}
