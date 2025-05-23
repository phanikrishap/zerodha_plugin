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

