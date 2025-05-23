using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class FullCreateOrderResponse : ResultCreateOrderResponse
{
  [DataMember(Name = "fills")]
  public List<Fill> Fills { get; set; }
}

