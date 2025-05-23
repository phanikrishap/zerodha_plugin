using QABrokerAPI.Common.Models.Response.Interfaces;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class WithdrawListResponse : IConfirmationResponse, IResponse
{
  [DataMember(Order = 1)]
  public List<WithdrawListItem> WithdrawList { get; set; }

  [DataMember(Order = 2)]
  public bool Success { get; set; }
}

