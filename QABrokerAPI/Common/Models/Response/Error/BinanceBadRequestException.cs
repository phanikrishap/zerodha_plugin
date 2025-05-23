// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.Error.BinanceBadRequestException
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceBadRequestException(BinanceError errorDetails) : BinanceException("Malformed requests are sent to the server. Please review the request object/string", errorDetails)
{
}
