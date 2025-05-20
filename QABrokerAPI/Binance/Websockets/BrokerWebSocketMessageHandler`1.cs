// Decompiled with JetBrains decompiler
// Type: BinanceExchange.API.Websockets.BinanceWebSocketMessageHandler`1
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Common.Models.WebSocket.Interfaces;

#nullable disable
namespace QABrokerAPI.Binance.Websockets;

public delegate void BrokerWebSocketMessageHandler<in T>(T data) where T : IWebSocketResponse;
