// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.Error.BinanceServerException
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceServerException(BinanceError errorDetails) : BinanceException("Request to BinanceAPI is valid but there was an error on the server side", errorDetails)
{
}
