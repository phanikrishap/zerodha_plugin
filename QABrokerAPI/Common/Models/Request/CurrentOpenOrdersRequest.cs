using QABrokerAPI.Common.Models.Request.Interfaces;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Request;

[DataContract]
public class CurrentOpenOrdersRequest : IRequest
{
  [DataMember(Order = 1)]
  public string Symbol { get; set; }
}

