// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Models.Response.Error.BinanceError
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using Newtonsoft.Json;

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceError
{
  public int Code { get; set; }

  [JsonProperty(PropertyName = "msg")]
  public string Message { get; set; }

  public string RequestMessage { get; set; }

  public override string ToString() => $"{this.Code}: {this.Message}";
}
