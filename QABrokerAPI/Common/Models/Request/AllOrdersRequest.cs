// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Request.AllOrdersRequest
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Models.Request.Interfaces;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class AllOrdersRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }

  [DataMember(Order = 2)]
  public long? OrderId { get; set; }

  [DataMember(Order = 3)]
  public int? Limit { get; set; }
}
