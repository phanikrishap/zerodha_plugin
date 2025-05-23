// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Enums.OrderRejectReason
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System.Runtime.Serialization;

namespace QABrokerAPI.Common.Enums;
public enum OrderRejectReason
{
  [EnumMember(Value = "NONE")] None,
  [EnumMember(Value = "UNKNOWN_INSTRUMENT")] UnknownInstrument,
  [EnumMember(Value = "MARKET_CLOSED")] MarketClosed,
  [EnumMember(Value = "PRICE_QTY_EXCEED_HARD_LIMITS")] PriceQuantityExceededHardLimits,
  [EnumMember(Value = "UNKNOWN_ORDER")] UnknownOrder,
  [EnumMember(Value = "DUPLICATE_ORDER")] DuplicateOrder,
  [EnumMember(Value = "UNKNOWN_ACCOUNT")] UnknownAccount,
  [EnumMember(Value = "INSUFFICIENT_BALANCE")] InsufficientBalance,
  [EnumMember(Value = "ACCOUNT_INACTIVE")] AccountInactive,
  [EnumMember(Value = "ACCOUNT_CANNOT_SETTLE")] AccountCannotSettle,
}
