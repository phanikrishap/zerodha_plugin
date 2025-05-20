// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.Interfaces.IBalanceResponse
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System;

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Interfaces;

public interface IBalanceResponse
{
  string Asset { get; set; }

  Decimal Free { get; set; }

  Decimal Locked { get; set; }
}
