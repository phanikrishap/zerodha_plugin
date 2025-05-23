using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Models.Response;

[DataContract]
public class ExchangeInfoSymbolFilterMaxNumIcebergOrders : ExchangeInfoSymbolFilter
{
  [DataMember(Order = 1)]
  public int MaxNumIcebergOrders { get; set; }
}

