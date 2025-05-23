using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class UserDataStreamResponse
{
  [DataMember(Order = 1)]
  public string ListenKey { get; set; }
}

