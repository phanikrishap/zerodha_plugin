#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket.Interfaces;

public interface ISymbolWebSocketResponse : IWebSocketResponse
{
  string Symbol { get; set; }
}

