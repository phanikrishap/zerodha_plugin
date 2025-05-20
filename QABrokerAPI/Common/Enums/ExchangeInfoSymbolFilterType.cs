
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Enums;

public enum ExchangeInfoSymbolFilterType
{
  [EnumMember(Value = "PRICE_FILTER")] PriceFilter,
  [EnumMember(Value = "PERCENT_PRICE")] PercentPrice,
  [EnumMember(Value = "LOT_SIZE")] LotSize,
  [EnumMember(Value = "MIN_NOTIONAL")] MinNotional,
  [EnumMember(Value = "ICEBERG_PARTS")] IcebergParts,
  [EnumMember(Value = "MARKET_LOT_SIZE")] MarketLotSize,
  [EnumMember(Value = "MAX_NUM_ORDERS")] MaxNumOrders,
  [EnumMember(Value = "MAX_NUM_ALGO_ORDERS")] MaxNumAlgoOrders,
  [EnumMember(Value = "MAX_NUM_ICEBERG_ORDERS")] MaxNumIcebergOrders,
  [EnumMember(Value = "MAX_POSITION")] MaxPosition,
  [EnumMember(Value = "EXCHANGE_MAX_NUM_ORDERS")] ExchangeMaxNumOrders,
  [EnumMember(Value = "EXCHANGE_MAX_NUM_ALGO_ORDERS")] ExchangeMaxNumAlgoOrders,
  [EnumMember(Value = "PERCENTAGE_PRICE")] PercentagePrice,
}
