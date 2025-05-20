// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Enums.OrderStatus
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Enums;

public enum OrderStatus
{
  [EnumMember(Value = "NEW")] New,
  [EnumMember(Value = "PARTIALLY_FILLED")] PartiallyFilled,
  [EnumMember(Value = "FILLED")] Filled,
  [EnumMember(Value = "CANCELED")] Cancelled,
  [EnumMember(Value = "PENDING_CANCEL")] PendingCancel,
  [EnumMember(Value = "REJECTED")] Rejected,
  [EnumMember(Value = "EXPIRED")] Expired,
}
