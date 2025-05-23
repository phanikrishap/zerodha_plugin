using System;

#nullable disable
namespace QABrokerAPI.Common.Models.WebSocket.Interfaces;

public interface IWebSocketResponse
{
  string EventType { get; set; }

  DateTime EventTime { get; set; }
}

