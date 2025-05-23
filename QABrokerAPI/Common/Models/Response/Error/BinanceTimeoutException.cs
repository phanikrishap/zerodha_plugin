// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.Error.BinanceTimeoutException
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceTimeoutException(BinanceError errorDetails) : BinanceException(" request was valid, the server went to execute but then timed out. This doesn't mean it failed, and should be treated as UNKNOWN.", errorDetails)
{
}
