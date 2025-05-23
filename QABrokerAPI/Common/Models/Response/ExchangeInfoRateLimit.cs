using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoRateLimit
{
  [DataMember(Order = 1)]
  public string RateLimitType { get; set; }

  [DataMember(Order = 2)]
  public string Interval { get; set; }

  [DataMember(Order = 3)]
  public int Limit { get; set; }
}

