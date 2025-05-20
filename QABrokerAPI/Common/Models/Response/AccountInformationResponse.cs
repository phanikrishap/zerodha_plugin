// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.AccountInformationResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class AccountInformationResponse
{
  [DataMember(Order = 1)]
  public int MakerCommission { get; set; }

  [DataMember(Order = 2)]
  public int TakerCommission { get; set; }

  [DataMember(Order = 3)]
  public int BuyerCommission { get; set; }

  [DataMember(Order = 4)]
  public int SellerCommission { get; set; }

  [DataMember(Order = 5)]
  public bool CanTrade { get; set; }

  [DataMember(Order = 6)]
  public bool CanWithdraw { get; set; }

  [DataMember(Order = 7)]
  public bool CanDeposit { get; set; }

  [DataMember(Order = 8)]
  public List<BalanceResponse> Balances { get; set; }
}
