using QABrokerAPI.Common.Models.Request.Interfaces;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class DepositAddressRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Asset { get; set; }
}

