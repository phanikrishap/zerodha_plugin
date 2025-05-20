// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Enums.NewOrderResponseType
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Enums;

public enum NewOrderResponseType
{
  [EnumMember(Value = "RESULT")] Result,
  [EnumMember(Value = "ACK")] Acknowledge,
  [EnumMember(Value = "FULL")] Full,
}
