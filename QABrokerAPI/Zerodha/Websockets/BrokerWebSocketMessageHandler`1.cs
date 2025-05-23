using QABrokerAPI.Common.Models.WebSocket.Interfaces;

#nullable disable
namespace QABrokerAPI.Zerodha.Websockets;

public delegate void BrokerWebSocketMessageHandler<in T>(T data) where T : IWebSocketResponse;

