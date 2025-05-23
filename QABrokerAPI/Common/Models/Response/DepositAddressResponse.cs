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

