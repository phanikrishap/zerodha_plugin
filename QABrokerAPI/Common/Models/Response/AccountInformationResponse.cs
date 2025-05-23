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

