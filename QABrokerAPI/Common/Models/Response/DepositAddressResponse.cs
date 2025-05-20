// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.DepositAddressResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Models.Response.Interfaces;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class DepositAddressResponse : IConfirmationResponse, IResponse
{
  [DataMember(Order = 1)]
  public string Address { get; set; }

  [DataMember(Order = 2)]
  public string AddressTag { get; set; }

  [DataMember(Order = 3)]
  public string Assett { get; set; }

  [DataMember(Order = 4)]
  public bool Success { get; set; }
}
