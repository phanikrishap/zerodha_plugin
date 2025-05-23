using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterIcebergParts : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public int Limit { get; set; }
}

