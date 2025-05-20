// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Enums.OrderType
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Enums;

public enum OrderType
{
  [EnumMember(Value = "LIMIT")] Limit,
  [EnumMember(Value = "MARKET")] Market,
  [EnumMember(Value = "STOP_LOSS")] StopLoss,
  [EnumMember(Value = "STOP_LOSS_LIMIT")] StopLossLimit,
  [EnumMember(Value = "TAKE_PROFIT")] TakeProfit,
  [EnumMember(Value = "TAKE_PROFIT_LIMIT")] TakeProfitLimit,
  [EnumMember(Value = "LIMIT_MAKER")] LimitMaker,
}
